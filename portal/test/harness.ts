/**
 * Test harness: a portal instance wired to a mock OpenCode server.
 */

import { mkdtempSync, rmSync } from "node:fs"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { request as httpRequest } from "node:http"
import type { PortalConfig } from "../src/config.ts"
import { Logger } from "../src/logger.ts"
import { PortalStore } from "../src/db.ts"
import { OpenCodeClient } from "../src/opencode.ts"
import { createPortal, type Portal } from "../src/server.ts"
import { startMockOpenCode, type MockOpenCode } from "./mock-opencode.ts"

export interface Harness {
  mock: MockOpenCode
  portal: Portal
  base: string
  config: PortalConfig
  outputRoot: string
  tmp: string
  request(method: string, path: string, options?: { user?: string | null; body?: unknown; headers?: Record<string, string> }): Promise<Response>
  /** Raw request: the path is sent verbatim, without URL normalisation. */
  rawRequest(method: string, rawPath: string, options?: { user?: string | null }): Promise<{ status: number; body: string }>
  cleanup(): Promise<void>
}

export async function createHarness(overrides: Partial<PortalConfig> = {}): Promise<Harness> {
  const tmp = mkdtempSync(join(tmpdir(), "ips-portal-test-"))
  const outputRoot = join(tmp, "output")

  const mock = await startMockOpenCode({ password: "test-password" })

  const config: PortalConfig = {
    server: { host: "127.0.0.1", port: 0 },
    opencode: {
      baseUrl: mock.url,
      username: "opencode",
      passwordEnv: "OPENCODE_SERVER_PASSWORD",
      passwordFile: null,
      projectDirectory: tmp,
      timeoutMs: 10_000,
    },
    allowed_models: ["opencode/mimo-v2.6-flash-free"],
    default_model: "opencode/mimo-v2.6-flash-free",
    agents: { allow: ["ips-help", "ips-designer", "ips-technologist"], modes: ["primary", "all"], max: 3 },
    output: { root: outputRoot, retentionDays: 7, writeGitignore: true },
    limits: {
      maxMessageChars: 20_000,
      requestsPerMinute: 10_000,
      messagesPerMinute: 10_000,
      maxBodyBytes: 256 * 1024,
    },
    files: { allowedExtensions: ["md", "txt", "csv"], maxBytes: 1024 * 1024 },
    auth: { header: "X-User", userPattern: "^[A-Za-z0-9._\\\\@ -]{1,64}$" },
    logging: { level: "error", file: null, console: false },
    systemRule: { mode: "instruction", key: "portal-output" },
    ...overrides,
  }

  const logger = new Logger(config)
  const store = new PortalStore(join(tmp, "portal.db"))
  const client = new OpenCodeClient({
    baseUrl: config.opencode.baseUrl,
    username: config.opencode.username,
    password: "test-password",
    timeoutMs: config.opencode.timeoutMs,
  })

  const portal = createPortal({ config, client, store, logger })
  const address = await portal.listen(0, "127.0.0.1")
  const base = `http://127.0.0.1:${address.port}`

  async function request(
    method: string,
    path: string,
    options: { user?: string | null; body?: unknown; headers?: Record<string, string> } = {},
  ): Promise<Response> {
    const headers: Record<string, string> = { ...(options.headers ?? {}) }
    if (options.user !== null && options.user !== undefined) headers[config.auth.header] = options.user
    if (options.body !== undefined) headers["Content-Type"] = "application/json"
    return fetch(`${base}${path}`, {
      method,
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    })
  }

  return {
    mock,
    portal,
    base,
    config,
    outputRoot,
    tmp,
    request,
    rawRequest(method, rawPath, options = {}) {
      return new Promise((resolvePromise, rejectPromise) => {
        const target = new URL(base)
        const headers: Record<string, string> = {}
        if (options.user) headers[config.auth.header] = options.user
        const request = httpRequest(
          {
            host: target.hostname,
            port: target.port,
            method,
            path: rawPath,
            headers,
          },
          (response) => {
            let body = ""
            response.setEncoding("utf8")
            response.on("data", (chunk) => (body += chunk))
            response.on("end", () => resolvePromise({ status: response.statusCode ?? 0, body }))
          },
        )
        request.on("error", rejectPromise)
        request.end()
      })
    },
    async cleanup() {
      await portal.close()
      await mock.close()
      rmSync(tmp, { recursive: true, force: true })
    },
  }
}

/** Read an SSE stream until `predicate` is satisfied or the timeout expires. */
export async function readSse(
  response: Response,
  predicate: (event: { type: string; data: Record<string, unknown> }) => boolean,
  timeoutMs = 5000,
): Promise<{ type: string; data: Record<string, unknown> } | null> {
  const reader = response.body!.getReader()
  const decoder = new TextDecoder()
  let buffer = ""
  const deadline = Date.now() + timeoutMs
  try {
    while (Date.now() < deadline) {
      const { value, done } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })
      let index: number
      while ((index = buffer.indexOf("\n\n")) !== -1) {
        const frame = buffer.slice(0, index)
        buffer = buffer.slice(index + 2)
        for (const line of frame.split("\n")) {
          if (!line.startsWith("data:")) continue
          try {
            const event = JSON.parse(line.slice(5).trim())
            if (predicate(event)) return event
          } catch {
            /* ignore */
          }
        }
      }
    }
  } finally {
    await reader.cancel().catch(() => {})
  }
  return null
}

/** Wait until `check` returns true. */
export async function waitFor(check: () => boolean, timeoutMs = 5000): Promise<boolean> {
  const deadline = Date.now() + timeoutMs
  while (Date.now() < deadline) {
    if (check()) return true
    await new Promise((resolve) => setTimeout(resolve, 25))
  }
  return check()
}
