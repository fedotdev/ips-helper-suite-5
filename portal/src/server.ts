/**
 * Portal HTTP server.
 *
 * The portal exposes EXACTLY these routes and nothing else. Everything that is
 * not on the list answers with 404; the portal never proxies an arbitrary
 * OpenCode route.
 *
 *   GET    /api/agents
 *   POST   /api/sessions
 *   GET    /api/sessions
 *   GET    /api/sessions/:id/messages
 *   POST   /api/sessions/:id/messages
 *   GET    /api/sessions/:id/events
 *   GET    /api/sessions/:id/files
 *   GET    /api/sessions/:id/files/:name
 *   DELETE /api/sessions/:id
 */

import { createReadStream, existsSync, statSync } from "node:fs"
import { createServer, type IncomingMessage, type Server, type ServerResponse } from "node:http"
import { extname, join, normalize, resolve } from "node:path"
import type { AddressInfo } from "node:net"
import type { PortalConfig } from "./config.ts"
import { PORTAL_DIR } from "./config.ts"
import type { Logger } from "./logger.ts"
import { HeaderUserAuth, type AuthProvider, type AuthenticatedUser } from "./auth.ts"
import type { PortalStore } from "./db.ts"
import { OpenCodeClient, type OpenCodeEvent } from "./opencode.ts"
import { EventBroker } from "./events.ts"
import { PermissionGuard } from "./permissions.ts"
import { RateLimiter } from "./ratelimit.ts"
import { PortalError, SessionService } from "./sessions.ts"
import { FileAccessError } from "./files.ts"
import { HttpError, pathSegments, readJsonBody, sendError, sendJson } from "./http.ts"

export interface PortalOptions {
  config: PortalConfig
  client: OpenCodeClient
  store: PortalStore
  logger: Logger
  /** Replace for AD / SSO. */
  auth?: AuthProvider
}

export interface Portal {
  server: Server
  config: PortalConfig
  sessions: SessionService
  broker: EventBroker
  store: PortalStore
  listen(port?: number, host?: string): Promise<{ port: number; host: string }>
  close(): Promise<void>
}

const STATIC_FILES: Record<string, { file: string; type: string }> = {
  "/": { file: "index.html", type: "text/html; charset=utf-8" },
  "/index.html": { file: "index.html", type: "text/html; charset=utf-8" },
  "/app.js": { file: "app.js", type: "text/javascript; charset=utf-8" },
  "/styles.css": { file: "styles.css", type: "text/css; charset=utf-8" },
}

export function createPortal(options: PortalOptions): Portal {
  const { config, client, store, logger } = options
  const auth = options.auth ?? new HeaderUserAuth(config)
  const rateLimiter = new RateLimiter({
    requestsPerMinute: config.limits.requestsPerMinute,
    messagesPerMinute: config.limits.messagesPerMinute,
  })
  const broker = new EventBroker(client, logger)
  const sessions = new SessionService(config, client, store, logger)

  const permissions = new PermissionGuard({
    client,
    broker,
    logger,
    knownSessions: () => sessions.knownSessionIds(),
    onRejected: (sessionId, request) => {
      broker.publishLocal(sessionId, "portal.permission.rejected", {
        requestID: request.id,
        action: request.action,
        resources: request.resources ?? [],
        message:
          "Запрос на разрешение отклонён автоматически: портал работает без интерактивных подтверждений. " +
          "Попросите агента выполнить задачу без дополнительных разрешений.",
      })
    },
  })

  const server = createServer((req, res) => {
    void handle(req, res).catch((err) => {
      logger.error({ event: "request.unhandled", error: (err as Error).message })
      if (!res.headersSent) sendError(res, 500, "internal_error", "Internal error")
      else res.end()
    })
  })

  async function handle(req: IncomingMessage, res: ServerResponse): Promise<void> {
    const started = Date.now()
    const method = (req.method ?? "GET").toUpperCase()
    const segments = pathSegments(req.url ?? "/")
    const requestId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`

    // ----- static frontend -------------------------------------------------
    if (method === "GET" && segments.length === 0) {
      logger.debug({ event: "request", method, path: req.url ?? "/", status: 200 })
      return serveStatic("/", res)
    }
    if (method === "GET" && segments.length === 1 && STATIC_FILES[`/${segments[0]}`]) {
      logger.debug({ event: "request", method, path: req.url ?? "/", status: 200 })
      return serveStatic(`/${segments[0]}`, res)
    }

    // ----- everything below /api requires a login --------------------------
    if (segments[0] !== "api") {
      logger.info({ event: "request", method, path: req.url ?? "/", status: 404, requestId })
      return sendError(res, 404, "not_found", "Not found")
    }

    const authResult = await auth.authenticate(req)
    if (!authResult.user) {
      const reason = authResult.reason === "invalid" ? "invalid_login" : "missing_login"
      logger.info({ event: "request", method, path: req.url ?? "/", status: 401, requestId })
      return sendError(res, 401, reason, `Login required. Send the ${config.auth.header} header.`)
    }
    const user: AuthenticatedUser = authResult.user
    store.touchUser(user.id)

    const limit = rateLimiter.takeRequest(user.id)
    if (!limit.allowed) {
      res.setHeader("Retry-After", String(limit.retryAfterSeconds))
      logger.warn({ event: "ratelimit", user: user.id, path: req.url ?? "/", status: 429, requestId })
      return sendError(res, 429, "rate_limited", "Too many requests, please slow down")
    }

    try {
      await route(req, res, segments, user, requestId)
    } catch (err) {
      handleError(res, err, logger, { user: user.id, path: req.url ?? "/", requestId })
    } finally {
      logger.info({
        event: "request",
        user: user.id,
        method,
        path: req.url ?? "/",
        status: res.statusCode,
        durationMs: Date.now() - started,
        requestId,
      })
    }
  }

  async function route(
    req: IncomingMessage,
    res: ServerResponse,
    segments: string[],
    user: AuthenticatedUser,
    requestId: string,
  ): Promise<void> {
    const method = (req.method ?? "GET").toUpperCase()
    const [head, second, third, fourth] = segments

    // GET /api/agents
    if (head === "api" && second === "agents" && segments.length === 2) {
      if (method !== "GET") return methodNotAllowed(res, "GET")
      const modes = await sessions.listModes()
      return sendJson(res, 200, { data: modes })
    }

    // /api/sessions
    if (head === "api" && second === "sessions" && segments.length === 2) {
      if (method === "GET") {
        const list = await sessions.listSessions(user.id)
        return sendJson(res, 200, { data: list })
      }
      if (method === "POST") {
        const body = await readJsonBody(req, config.limits.maxBodyBytes)
        const agent = typeof body.agent === "string" ? body.agent.trim() : ""
        if (!agent) throw new PortalError(400, "missing_agent", "Field 'agent' is required")
        const model = typeof body.model === "string" ? body.model : undefined
        const title = typeof body.title === "string" ? body.title : undefined
        const created = await sessions.createSession(user.id, { agent, model, title })
        return sendJson(res, 201, { data: created })
      }
      return methodNotAllowed(res, "GET, POST")
    }

    // /api/sessions/:id/...
    if (head === "api" && second === "sessions" && segments.length >= 3) {
      const sessionId = third!

      if (segments.length === 3) {
        if (method === "DELETE") {
          await sessions.deleteSession(user.id, sessionId)
          return sendJson(res, 200, { data: { id: sessionId, deleted: true } })
        }
        return methodNotAllowed(res, "DELETE")
      }

      if (fourth === "messages" && segments.length === 4) {
        if (method === "GET") {
          const messages = await sessions.listMessages(user.id, sessionId)
          return sendJson(res, 200, { data: messages })
        }
        if (method === "POST") {
          const messageLimit = rateLimiter.takeMessage(user.id)
          if (!messageLimit.allowed) {
            res.setHeader("Retry-After", String(messageLimit.retryAfterSeconds))
            throw new PortalError(429, "rate_limited", "Too many messages, please slow down")
          }
          const body = await readJsonBody(req, config.limits.maxBodyBytes)
          const text = typeof body.text === "string" ? body.text : ""
          if (!text.trim()) throw new PortalError(400, "empty_message", "Field 'text' is required")
          const agent = typeof body.agent === "string" && body.agent.trim() ? body.agent.trim() : undefined
          const result = await sessions.sendMessage(user.id, sessionId, { text, agent })
          return sendJson(res, 202, { data: result })
        }
        return methodNotAllowed(res, "GET, POST")
      }

      if (fourth === "events" && segments.length === 4) {
        if (method !== "GET") return methodNotAllowed(res, "GET")
        return streamEvents(req, res, user, sessionId, requestId)
      }

      if (fourth === "files" && segments.length === 4) {
        if (method !== "GET") return methodNotAllowed(res, "GET")
        const files = sessions.listFiles(user.id, sessionId)
        return sendJson(res, 200, { data: files })
      }

      if (fourth === "files" && segments.length === 5) {
        if (method !== "GET") return methodNotAllowed(res, "GET")
        const name = segments[4]!
        const file = sessions.resolveFile(user.id, sessionId, name)
        return sendFile(res, file.path, file.size, name)
      }
    }

    // Unknown route - nothing is ever proxied to OpenCode.
    logger.info({ event: "route.blocked", user: user.id, path: req.url ?? "/", requestId })
    return sendError(res, 404, "not_found", "Not found")
  }

  function serveStatic(pathname: string, res: ServerResponse): void {
    const entry = STATIC_FILES[pathname]
    if (!entry) return sendError(res, 404, "not_found", "Not found")
    const file = resolve(join(PORTAL_DIR, "public", normalize(entry.file)))
    if (!file.startsWith(resolve(join(PORTAL_DIR, "public")))) return sendError(res, 404, "not_found", "Not found")
    if (!existsSync(file)) return sendError(res, 404, "not_found", "Not found")
    const body = statSync(file)
    res.writeHead(200, {
      "Content-Type": entry.type,
      "Content-Length": body.size,
      "Cache-Control": "no-cache",
      "X-Content-Type-Options": "nosniff",
    })
    createReadStream(file).pipe(res)
  }

  function streamEvents(
    req: IncomingMessage,
    res: ServerResponse,
    user: AuthenticatedUser,
    sessionId: string,
    requestId: string,
  ): void {
    // Ownership is checked before the stream is opened: a foreign session id
    // must answer 404 and must not leak any event.
    sessions.assertOwner(user.id, sessionId)

    res.writeHead(200, {
      "Content-Type": "text/event-stream; charset=utf-8",
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
      "X-Accel-Buffering": "no",
    })
    res.write(`retry: 2000\n\n`)

    const send = (event: OpenCodeEvent) => {
      if (res.writableEnded) return
      res.write(`data: ${JSON.stringify({ id: event.id, type: event.type, data: event.data })}\n\n`)
    }

    const unsubscribe = broker.subscribe(sessionId, send)
    const heartbeat = setInterval(() => {
      if (!res.writableEnded) res.write(": hb\n\n")
    }, 15_000)
    heartbeat.unref?.()

    const cleanup = () => {
      clearInterval(heartbeat)
      unsubscribe()
      if (!res.writableEnded) res.end()
    }
    req.on("close", cleanup)
    req.on("aborted", cleanup)
    res.on("close", cleanup)

    logger.info({ event: "sse.opened", user: user.id, session: sessionId, requestId })
    send({ type: "portal.connected", data: { sessionID: sessionId } })
  }

  function sendFile(res: ServerResponse, path: string, size: number, name: string): void {
    const fallback = name.replace(/[^\x20-\x7e]/g, "_").replace(/["\\]/g, "_")
    res.writeHead(200, {
      "Content-Type": contentTypeFor(name),
      "Content-Length": size,
      "Content-Disposition": `attachment; filename="${fallback}"; filename*=UTF-8''${encodeURIComponent(name)}`,
      "Cache-Control": "no-store",
      "X-Content-Type-Options": "nosniff",
    })
    createReadStream(path).pipe(res)
  }

  function methodNotAllowed(res: ServerResponse, allow: string): void {
    res.setHeader("Allow", allow)
    return sendError(res, 405, "method_not_allowed", "Method not allowed")
  }

  permissions.start()
  broker.start()
  sessions.cleanupExpiredOutputs()
  const retentionTimer = setInterval(() => sessions.cleanupExpiredOutputs(), 24 * 60 * 60 * 1000)
  retentionTimer.unref?.()

  return {
    server,
    config,
    sessions,
    broker,
    store,
    async listen(port = config.server.port, host = config.server.host) {
      await new Promise<void>((resolveListen, rejectListen) => {
        server.once("error", rejectListen)
        server.listen(port, host, () => {
          server.removeListener("error", rejectListen)
          resolveListen()
        })
      })
      const address = server.address() as AddressInfo
      return { port: address.port, host }
    },
    async close() {
      clearInterval(retentionTimer)
      permissions.stop()
      broker.stop()
      await new Promise<void>((resolveClose) => server.close(() => resolveClose()))
      store.close()
    },
  }
}

function contentTypeFor(name: string): string {
  switch (extname(name).toLowerCase()) {
    case ".md":
      return "text/markdown; charset=utf-8"
    case ".txt":
      return "text/plain; charset=utf-8"
    case ".csv":
      return "text/csv; charset=utf-8"
    default:
      return "application/octet-stream"
  }
}

function handleError(
  res: ServerResponse,
  err: unknown,
  logger: Logger,
  context: { user?: string; path?: string; requestId?: string },
): void {
  if (err instanceof PortalError) {
    logger.warn({ event: "request.failed", user: context.user, path: context.path, status: err.status, code: err.code })
    return sendError(res, err.status, err.code, err.message)
  }
  if (err instanceof HttpError) {
    logger.warn({ event: "request.failed", user: context.user, path: context.path, status: err.status, code: err.code })
    return sendError(res, err.status, err.code, err.message)
  }
  if (err instanceof FileAccessError) {
    const status = err.reason === "too-large" ? 413 : 400
    logger.warn({ event: "request.failed", user: context.user, path: context.path, status, code: err.reason })
    return sendError(res, status, err.reason, err.message)
  }
  const status = (err as { status?: number }).status
  if (typeof status === "number" && status >= 400 && status < 600) {
    logger.warn({ event: "request.failed", user: context.user, path: context.path, status, code: "upstream_error" })
    return sendError(res, status, "upstream_error", (err as Error).message)
  }
  logger.error({ event: "request.error", user: context.user, path: context.path, error: (err as Error).message })
  return sendError(res, 500, "internal_error", "Internal error")
}
