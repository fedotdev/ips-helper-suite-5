/**
 * Mock of the OpenCode v2 HTTP API.
 *
 * It implements exactly the routes the portal is allowed to use, with the same
 * request/response shapes as the OpenAPI document of OpenCode v2.1.2, so the
 * portal can be tested without touching a real server.
 *
 * The mock also records every request path it receives. The tests assert that
 * the portal only ever calls the whitelisted routes.
 */

import { createServer, type IncomingMessage, type Server, type ServerResponse } from "node:http"
import type { AddressInfo } from "node:net"

export interface MockAgent {
  id: string
  name: string
  description?: string
  mode?: "subagent" | "primary" | "all"
  hidden?: boolean
  request?: Record<string, unknown>
  permissions?: unknown[]
}

export interface MockModel {
  id: string
  modelID: string
  providerID: string
  name: string
  enabled: boolean
}

export interface MockSession {
  id: string
  projectID: string
  agent?: string
  model?: { id: string; providerID: string }
  title?: string
  time: { created: number; updated: number }
  location: { directory: string }
  messages: Array<Record<string, unknown>>
}

export interface MockPermissionRequest {
  id: string
  sessionID: string
  action: string
  resources: string[]
  message?: string
  source?: { type: string; messageID: string; id: string }
}

export interface MockState {
  agents: MockAgent[]
  models: MockModel[]
  sessions: Map<string, MockSession>
  permissions: Map<string, MockPermissionRequest[]>
  instructionEntries: Map<string, Map<string, string>>
  replies: Array<{ sessionID: string; requestID: string; decision: string; message?: string }>
  /** Every path the portal requested. */
  requests: string[]
  /** Every method+path the portal requested. */
  calls: Array<{ method: string; path: string }>
  username: string
  password: string
}

export interface MockOpenCode {
  url: string
  state: MockState
  /** Push an event into the SSE stream. */
  emit(type: string, data: Record<string, unknown>): void
  /** Raise a permission request and emit `permission.asked`. */
  raisePermission(sessionID: string, request?: Partial<MockPermissionRequest>): MockPermissionRequest
  close(): Promise<void>
}

export const DEFAULT_AGENTS: MockAgent[] = [
  {
    id: "ips-help",
    name: "Помощник по IPS",
    description: "Отвечает на вопросы по интерфейсу IPS, ЕСТД/ЕСКД и оформлению документов.",
    mode: "primary",
    request: {},
    permissions: [],
  },
  {
    id: "ips-designer",
    name: "Конструктор",
    description: "Помогает конструктору: спецификации, чертежи, оформление по ГОСТ.",
    mode: "primary",
    request: {},
    permissions: [],
  },
  {
    id: "ips-technologist",
    name: "Технолог",
    description: "Read-only анализ живой базы ЕТП: маршруты, операции, ресурсы.",
    mode: "primary",
    request: {},
    permissions: [],
  },
  {
    id: "general",
    name: "General",
    description: "Универсальный субагент.",
    mode: "subagent",
    request: {},
    permissions: [],
  },
]

export const DEFAULT_MODELS: MockModel[] = [
  {
    id: "mimo-v2.6-flash-free",
    modelID: "mimo-v2.6-flash-free",
    providerID: "opencode",
    name: "MiMo-V2.6-Flash Free",
    enabled: true,
  },
  {
    id: "not-allowed-model",
    modelID: "not-allowed-model",
    providerID: "opencode",
    name: "Not Allowed",
    enabled: true,
  },
]

export async function startMockOpenCode(
  options: { username?: string; password?: string; agents?: MockAgent[]; models?: MockModel[] } = {},
): Promise<MockOpenCode> {
  const state: MockState = {
    agents: options.agents ?? DEFAULT_AGENTS,
    models: options.models ?? DEFAULT_MODELS,
    sessions: new Map(),
    permissions: new Map(),
    instructionEntries: new Map(),
    replies: [],
    requests: [],
    calls: [],
    username: options.username ?? "opencode",
    password: options.password ?? "test-password",
  }

  const streams = new Set<ServerResponse>()
  let counter = 0

  const server: Server = createServer((req, res) => {
    void handle(req, res)
  })

  async function handle(req: IncomingMessage, res: ServerResponse): Promise<void> {
    const url = new URL(req.url ?? "/", "http://localhost")
    const path = url.pathname
    const method = (req.method ?? "GET").toUpperCase()
    state.requests.push(path)
    state.calls.push({ method, path })

    // Basic auth, exactly like the real server.
    const header = req.headers.authorization ?? ""
    const expected = "Basic " + Buffer.from(`${state.username}:${state.password}`).toString("base64")
    if (header !== expected) {
      res.writeHead(401, { "Content-Type": "application/json" })
      res.end(JSON.stringify({ _tag: "UnauthorizedError", message: "Unauthorized" }))
      return
    }

    // --- SSE -------------------------------------------------------------
    if (path === "/api/event" && method === "GET") {
      res.writeHead(200, {
        "Content-Type": "text/event-stream; charset=utf-8",
        "Cache-Control": "no-cache",
        Connection: "keep-alive",
      })
      res.write(`data: ${JSON.stringify({ id: "evt_conn", type: "server.connected", data: {} })}\n\n`)
      streams.add(res)
      req.on("close", () => streams.delete(res))
      return
    }

    // --- models ----------------------------------------------------------
    if (path === "/api/model" && method === "GET") {
      return json(res, 200, { data: state.models })
    }

    // --- agents ----------------------------------------------------------
    if (path === "/api/agent" && method === "GET") {
      const directory = url.searchParams.get("location[directory]") ?? "C:\\"
      return json(res, 200, { location: { directory }, data: state.agents })
    }

    // --- sessions --------------------------------------------------------
    if (path === "/api/session" && method === "GET") {
      const directory = url.searchParams.get("directory")
      const data = [...state.sessions.values()].filter((session) => !directory || session.location.directory === directory)
      return json(res, 200, { data, cursor: { previous: null, next: null } })
    }
    if (path === "/api/session" && method === "POST") {
      const body = await readJson(req)
      counter += 1
      const id = `ses_mock${counter.toString().padStart(4, "0")}`
      const now = Date.now()
      const session: MockSession = {
        id,
        projectID: "prj_mock",
        agent: typeof body.agent === "string" ? body.agent : undefined,
        model: body.model as MockSession["model"],
        title: typeof body.title === "string" ? body.title : "Mock session",
        time: { created: now, updated: now },
        location: (body.location as { directory: string }) ?? { directory: "C:\\" },
        messages: [],
      }
      state.sessions.set(id, session)
      return json(res, 200, { data: session })
    }

    const sessionMatch = /^\/api\/session\/([^/]+)(?:\/(.*))?$/.exec(path)
    if (sessionMatch) {
      const sessionID = decodeURIComponent(sessionMatch[1])
      const rest = sessionMatch[2] ?? ""
      const session = state.sessions.get(sessionID)
      if (!session) return json(res, 404, { _tag: "SessionNotFoundError", sessionID, message: "Session not found" })

      if (rest === "" && method === "GET") return json(res, 200, { data: session })
      if (rest === "" && method === "DELETE") {
        state.sessions.delete(sessionID)
        state.permissions.delete(sessionID)
        return json(res, 204, undefined)
      }
      if (rest === "message" && method === "GET") {
        return json(res, 200, { data: session.messages, cursor: { previous: null, next: null } })
      }
      if (rest === "prompt" && method === "POST") {
        const body = await readJson(req)
        const text = String(body.text ?? "")
        const now = Date.now()
        const userMessage = {
          id: `msg_u${session.messages.length + 1}`,
          type: "user",
          time: { created: now },
          text,
        }
        session.messages.push(userMessage)
        const assistantID = `msg_a${session.messages.length + 1}`
        const assistantMessage = {
          id: assistantID,
          type: "assistant",
          time: { created: now },
          agent: session.agent,
          model: session.model,
          content: [{ type: "text", text: `echo: ${text}` }],
          finish: "stop",
        }
        session.messages.push(assistantMessage)
        session.time.updated = now
        emit("session.text.started", { sessionID, assistantMessageID: assistantID, ordinal: 0 })
        emit("session.text.delta", { sessionID, assistantMessageID: assistantID, ordinal: 0, delta: `echo: ${text}` })
        emit("session.text.ended", { sessionID, assistantMessageID: assistantID, ordinal: 0, text: `echo: ${text}` })
        emit("session.execution.succeeded", { sessionID })
        return json(res, 200, { data: { id: assistantID, sessionID, admittedSeq: 1, delivery: "queue", timeCreated: now } })
      }
      if (rest === "agent" && method === "POST") {
        const body = await readJson(req)
        session.agent = String(body.agent ?? "")
        return json(res, 204, undefined)
      }
      if (rest === "permission" && method === "GET") {
        return json(res, 200, { data: state.permissions.get(sessionID) ?? [] })
      }
      if (rest === "permission" && method === "POST") {
        const body = await readJson(req)
        const request = {
          id: `per_mock${counter++}`,
          sessionID,
          action: String(body.action ?? "bash"),
          resources: Array.isArray(body.resources) ? body.resources : [],
        }
        const list = state.permissions.get(sessionID) ?? []
        list.push(request)
        state.permissions.set(sessionID, list)
        emit("permission.asked", { ...request })
        return json(res, 200, { data: { id: request.id, effect: "ask" } })
      }
      const replyMatch = /^permission\/([^/]+)\/reply$/.exec(rest)
      if (replyMatch && method === "POST") {
        const body = await readJson(req)
        const requestID = decodeURIComponent(replyMatch[1])
        state.replies.push({
          sessionID,
          requestID,
          decision: String(body.decision ?? ""),
          message: typeof body.message === "string" ? body.message : undefined,
        })
        const list = (state.permissions.get(sessionID) ?? []).filter((item) => item.id !== requestID)
        state.permissions.set(sessionID, list)
        emit("permission.replied", { sessionID, id: requestID, decision: body.decision })
        return json(res, 204, undefined)
      }
      return json(res, 404, { _tag: "NotFoundError", message: `No route ${method} ${path}` })
    }

    const instructionMatch = /^\/api\/experimental\/session\/([^/]+)\/instructions\/entries\/([^/]+)$/.exec(path)
    if (instructionMatch && method === "PUT") {
      const sessionID = decodeURIComponent(instructionMatch[1])
      const key = decodeURIComponent(instructionMatch[2])
      const body = await readJson(req)
      const entries = state.instructionEntries.get(sessionID) ?? new Map<string, string>()
      entries.set(key, String(body.value ?? ""))
      state.instructionEntries.set(sessionID, entries)
      return json(res, 204, undefined)
    }

    return json(res, 404, { _tag: "NotFoundError", message: `No route ${method} ${path}` })
  }

  function emit(type: string, data: Record<string, unknown>): void {
    const payload = `data: ${JSON.stringify({ id: `evt_${Math.random().toString(36).slice(2, 10)}`, type, data })}\n\n`
    for (const stream of streams) {
      if (!stream.writableEnded) stream.write(payload)
    }
  }

  await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", () => resolve()))
  const address = server.address() as AddressInfo

  return {
    url: `http://127.0.0.1:${address.port}`,
    state,
    emit,
    raisePermission(sessionID, request = {}) {
      counter += 1
      const full: MockPermissionRequest = {
        id: request.id ?? `per_mock${counter}`,
        sessionID,
        action: request.action ?? "bash",
        resources: request.resources ?? ["C:\\Windows\\System32"],
        message: request.message ?? "Разрешить выполнение команды?",
        source: request.source ?? { type: "tool", messageID: "msg_a1", id: "tool_1" },
      }
      const list = state.permissions.get(sessionID) ?? []
      list.push(full)
      state.permissions.set(sessionID, list)
      emit("permission.asked", { ...full })
      return full
    },
    async close() {
      for (const stream of streams) if (!stream.writableEnded) stream.end()
      await new Promise<void>((resolve) => server.close(() => resolve()))
    },
  }
}

function json(res: ServerResponse, status: number, body: unknown): void {
  if (status === 204 || body === undefined) {
    res.writeHead(204)
    res.end()
    return
  }
  const payload = JSON.stringify(body)
  res.writeHead(status, { "Content-Type": "application/json", "Content-Length": Buffer.byteLength(payload) })
  res.end(payload)
}

async function readJson(req: IncomingMessage): Promise<Record<string, unknown>> {
  const chunks: Buffer[] = []
  for await (const chunk of req) chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk as string))
  if (chunks.length === 0) return {}
  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8"))
  } catch {
    return {}
  }
}
