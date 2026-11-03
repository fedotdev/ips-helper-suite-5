/**
 * Thin client for the shared OpenCode server (HTTP API v2).
 *
 * The routes below were verified against the OpenAPI document served by the
 * running instance (`GET /openapi.json`, OpenCode v2.1.2) and against
 * https://opencode.ai/v2/docs/api. Nothing outside this whitelist is ever
 * called.
 *
 * Used routes (v2 names):
 *   GET    /api/agent?location[directory]=<dir>
 *   POST   /api/session
 *   GET    /api/session?directory=<dir>
 *   GET    /api/session/{id}
 *   DELETE /api/session/{id}
 *   GET    /api/session/{id}/message
 *   POST   /api/session/{id}/prompt
 *   PUT    /api/experimental/session/{id}/instructions/entries/{key}
 *   GET    /api/session/{id}/permission
 *   POST   /api/session/{id}/permission/{requestID}/reply
 *   GET    /api/event                              (SSE)
 */

export interface AgentInfo {
  id: string
  name: string
  description?: string
  mode?: "subagent" | "primary" | "all"
  hidden?: boolean
  model?: { id: string; providerID: string; variant?: string }
}

export interface ModelRef {
  id: string
  providerID: string
  variant?: string
}

export interface SessionInfo {
  id: string
  projectID?: string
  agent?: string
  model?: ModelRef
  title?: string
  parentID?: string
  time?: { created?: number; updated?: number; idle?: number; viewed?: number; archived?: number }
  metadata?: Record<string, unknown>
  location?: { directory: string }
}

export interface SessionMessage {
  id: string
  type: string
  time?: { created?: number; completed?: number; streamed?: number }
  text?: string
  description?: string
  agent?: string
  model?: ModelRef
  content?: Array<Record<string, unknown>>
  error?: { name?: string; message?: string }
  finish?: string
}

export interface PermissionRequest {
  id: string
  sessionID: string
  action: string
  resources: string[]
  save?: string[]
  metadata?: Record<string, unknown>
  source?: { type: string; messageID?: string; id?: string }
  message?: string
}

export interface OpenCodeEvent {
  id?: string
  type: string
  data: Record<string, unknown>
}

export class OpenCodeError extends Error {
  readonly status: number
  readonly tag: string | undefined
  readonly body: unknown

  constructor(status: number, tag: string | undefined, message: string, body?: unknown) {
    super(message)
    this.name = "OpenCodeError"
    this.status = status
    this.tag = tag
    this.body = body
  }
}

export interface OpenCodeClientOptions {
  baseUrl: string
  username: string
  password: string
  timeoutMs: number
  /** Injected for tests. */
  fetchImpl?: typeof fetch
  /**
   * Re-resolves the base URL. The shared OpenCode service uses a random port,
   * so the portal asks OpenCode where it currently listens.
   */
  resolveBaseUrl?: () => string | null
}

interface RequestOptions {
  method?: string
  body?: unknown
  query?: Record<string, string | undefined>
  /** `location[directory]=...` style deep object query. */
  locationQuery?: Record<string, string | undefined>
  timeoutMs?: number
}

export class OpenCodeClient {
  private baseUrl: string
  private readonly authHeader: string
  private readonly timeoutMs: number
  private readonly fetchImpl: typeof fetch
  private readonly resolveBaseUrl?: () => string | null

  constructor(opts: OpenCodeClientOptions) {
    this.baseUrl = opts.baseUrl.replace(/\/+$/, "")
    this.authHeader = "Basic " + Buffer.from(`${opts.username}:${opts.password}`).toString("base64")
    this.timeoutMs = opts.timeoutMs
    this.fetchImpl = opts.fetchImpl ?? fetch
    this.resolveBaseUrl = opts.resolveBaseUrl
  }

  /** Current base URL of the OpenCode server. */
  get url(): string {
    return this.baseUrl
  }

  // ---------------------------------------------------------------- helpers

  private buildUrl(path: string, query?: RequestOptions["query"], locationQuery?: RequestOptions["locationQuery"]): string {
    const params = new URLSearchParams()
    for (const [key, value] of Object.entries(query ?? {})) {
      if (value !== undefined) params.set(key, value)
    }
    for (const [key, value] of Object.entries(locationQuery ?? {})) {
      if (value !== undefined) params.set(`${key}[${key}]`, value)
    }
    const qs = params.toString()
    return `${this.baseUrl}${path}${qs ? `?${qs}` : ""}`
  }

  async request<T>(path: string, options: RequestOptions = {}): Promise<T> {
    let response: Response
    try {
      response = await this.send(path, options)
    } catch (err) {
      // The shared service may have restarted on a new port - resolve it once more.
      const refreshed = this.resolveBaseUrl?.()
      if (!refreshed || refreshed === this.baseUrl) {
        throw new OpenCodeError(0, undefined, `OpenCode request failed: ${(err as Error).message}`)
      }
      this.baseUrl = refreshed.replace(/\/+$/, "")
      response = await this.send(path, options)
    }

    if (response.status === 204 || response.status === 205) return undefined as T

    const text = await response.text()
    let parsed: unknown
    if (text) {
      try {
        parsed = JSON.parse(text)
      } catch {
        parsed = text
      }
    }

    if (!response.ok) {
      const tag =
        parsed && typeof parsed === "object" && "_tag" in (parsed as Record<string, unknown>)
          ? String((parsed as Record<string, unknown>)._tag)
          : undefined
      const message =
        parsed && typeof parsed === "object" && "message" in (parsed as Record<string, unknown>)
          ? String((parsed as Record<string, unknown>).message)
          : `OpenCode responded ${response.status}`
      throw new OpenCodeError(response.status, tag, message, parsed)
    }
    return parsed as T
  }

  private async send(path: string, options: RequestOptions): Promise<Response> {
    const url = this.buildUrl(path, options.query, options.locationQuery)
    const controller = new AbortController()
    const timeout = setTimeout(() => controller.abort(), options.timeoutMs ?? this.timeoutMs)
    try {
      return await this.fetchImpl(url, {
        method: options.method ?? "GET",
        headers: {
          Authorization: this.authHeader,
          ...(options.body !== undefined ? { "Content-Type": "application/json" } : {}),
        },
        body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
        signal: controller.signal,
      })
    } finally {
      clearTimeout(timeout)
    }
  }

  // ------------------------------------------------------------------ calls

  /** GET /api/agent?location[directory]=<dir> */
  async listAgents(directory: string): Promise<AgentInfo[]> {
    const res = await this.request<{ data?: AgentInfo[] }>("/api/agent", { locationQuery: { directory } })
    return res?.data ?? []
  }

  /** POST /api/session */
  async createSession(input: {
    title?: string
    agent?: string
    model?: ModelRef
    directory: string
  }): Promise<SessionInfo> {
    const body: Record<string, unknown> = { location: { directory: input.directory } }
    if (input.title !== undefined) body.title = input.title
    if (input.agent !== undefined) body.agent = input.agent
    if (input.model !== undefined) body.model = input.model
    const res = await this.request<{ data: SessionInfo }>("/api/session", { method: "POST", body })
    return res.data
  }

  /** GET /api/session?directory=<dir> */
  async listSessions(directory: string, limit = 200): Promise<SessionInfo[]> {
    const res = await this.request<{ data?: SessionInfo[] }>("/api/session", {
      query: { directory, limit: String(limit), order: "desc" },
    })
    return res?.data ?? []
  }

  /** GET /api/session/{id} */
  async getSession(sessionId: string): Promise<SessionInfo> {
    const res = await this.request<{ data: SessionInfo }>(`/api/session/${encodeURIComponent(sessionId)}`)
    return res.data
  }

  /** DELETE /api/session/{id} */
  async deleteSession(sessionId: string): Promise<void> {
    await this.request<void>(`/api/session/${encodeURIComponent(sessionId)}`, { method: "DELETE" })
  }

  /** GET /api/session/{id}/message */
  async listMessages(sessionId: string, limit = 200, order: "asc" | "desc" = "asc"): Promise<SessionMessage[]> {
    const res = await this.request<{ data?: SessionMessage[] }>(`/api/session/${encodeURIComponent(sessionId)}/message`, {
      query: { limit: String(limit), order },
    })
    return res?.data ?? []
  }

  /** POST /api/session/{id}/prompt */
  async prompt(
    sessionId: string,
    input: { text: string; files?: unknown[]; agents?: unknown[]; skills?: unknown[]; metadata?: Record<string, unknown> },
  ): Promise<unknown> {
    const body: Record<string, unknown> = { text: input.text }
    if (input.files) body.files = input.files
    if (input.agents) body.agents = input.agents
    if (input.skills) body.skills = input.skills
    if (input.metadata) body.metadata = input.metadata
    return this.request(`/api/session/${encodeURIComponent(sessionId)}/prompt`, { method: "POST", body })
  }

  /**
   * PUT /api/experimental/session/{id}/instructions/entries/{key}
   *
   * This is the v2 mechanism for a durable, per-session instruction. It is the
   * closest equivalent of the v1 `system` field of POST /session/{id}/message.
   */
  async putInstruction(sessionId: string, key: string, value: string): Promise<void> {
    await this.request<void>(
      `/api/experimental/session/${encodeURIComponent(sessionId)}/instructions/entries/${encodeURIComponent(key)}`,
      { method: "PUT", body: { value } },
    )
  }

  /** GET /api/session/{id}/permission */
  async listPermissions(sessionId: string): Promise<PermissionRequest[]> {
    const res = await this.request<{ data?: PermissionRequest[] }>(
      `/api/session/${encodeURIComponent(sessionId)}/permission`,
    )
    return res?.data ?? []
  }

  /** POST /api/session/{id}/permission/{requestID}/reply */
  async replyPermission(
    sessionId: string,
    requestId: string,
    decision: "once" | "always" | "reject",
    message?: string,
  ): Promise<void> {
    const body: Record<string, unknown> = { decision }
    if (message) body.message = message
    await this.request<void>(
      `/api/session/${encodeURIComponent(sessionId)}/permission/${encodeURIComponent(requestId)}/reply`,
      { method: "POST", body },
    )
  }

  /**
   * GET /api/event - server sent events.
   * The stream carries every location; callers filter by `data.sessionID`.
   */
  async *streamEvents(signal?: AbortSignal): AsyncGenerator<OpenCodeEvent, void, unknown> {
    const headers: Record<string, string> = { Authorization: this.authHeader, Accept: "text/event-stream" }
    const response = await this.fetchImpl(`${this.baseUrl}/api/event`, { headers, signal })
    if (!response.ok || !response.body) {
      throw new OpenCodeError(response.status, undefined, `cannot open event stream (${response.status})`)
    }
    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ""
    try {
      for (;;) {
        const { value, done } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        let index: number
        while ((index = buffer.indexOf("\n\n")) !== -1) {
          const frame = buffer.slice(0, index)
          buffer = buffer.slice(index + 2)
          const event = parseSseFrame(frame)
          if (event) yield event
        }
      }
    } finally {
      await reader.cancel().catch(() => {})
    }
  }
}

/** Parse one SSE frame into an OpenCode event, or null when the frame carries no data. */
export function parseSseFrame(frame: string): OpenCodeEvent | null {
  const dataLines: string[] = []
  let eventName: string | undefined
  for (const rawLine of frame.split("\n")) {
    const line = rawLine.replace(/\r$/, "")
    if (!line || line.startsWith(":")) continue
    const separator = line.indexOf(":")
    const field = separator === -1 ? line : line.slice(0, separator)
    const value = separator === -1 ? "" : line.slice(separator + 1).replace(/^ /, "")
    if (field === "data") dataLines.push(value)
    else if (field === "event") eventName = value
  }
  if (dataLines.length === 0) return null
  const payload = dataLines.join("\n")
  try {
    const parsed = JSON.parse(payload) as OpenCodeEvent & { type?: string }
    const type = eventName ?? parsed.type ?? "unknown"
    return { id: parsed.id, type, data: parsed.data ?? {} }
  } catch {
    return { type: eventName ?? "unknown", data: { raw: payload } }
  }
}
