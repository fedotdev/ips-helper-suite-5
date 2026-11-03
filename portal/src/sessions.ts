/**
 * Session service: the business logic between the portal HTTP API and the
 * shared OpenCode server.
 *
 * Ownership rules:
 *   - a session belongs to exactly one portal user, recorded in SQLite;
 *   - a request for a session that the caller does not own always fails with
 *     404 - never with 403 - so that session ids cannot be probed.
 */

import { existsSync, mkdirSync, rmSync, statSync, writeFileSync, readdirSync } from "node:fs"
import { join } from "node:path"
import type { PortalConfig } from "./config.ts"
import type { OpenCodeClient, AgentInfo, SessionInfo, SessionMessage } from "./opencode.ts"
import type { PortalStore, SessionRow } from "./db.ts"
import type { Logger } from "./logger.ts"
import { FileAccessError, listSessionFiles, resolveSessionFile, type FileEntry } from "./files.ts"

export class PortalError extends Error {
  readonly status: number
  readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = "PortalError"
    this.status = status
    this.code = code
  }
}

export interface ModeDescription {
  id: string
  name: string
  description: string
}

export interface PortalSession {
  id: string
  agent: string
  title: string
  createdAt: number
  updatedAt: number
  outputDir: string
}

export interface ModelChoice {
  providerID: string
  id: string
  /** "provider/model" as written in config.json. */
  full: string
}

const SESSION_ID_PATTERN = /^ses_[A-Za-z0-9]+$/

/**
 * HTTP status for a file access rejection.
 * - malformed name, wrong extension, path outside the session folder -> 400
 * - missing file, symlink, not a regular file                    -> 404
 * - file larger than the limit                                   -> 413
 */
function fileErrorStatus(reason: string): number {
  switch (reason) {
    case "not-found":
    case "not-a-file":
    case "symlink":
      return 404
    case "too-large":
      return 413
    default:
      return 400
  }
}

export class SessionService {
  private readonly cfg: PortalConfig
  private readonly client: OpenCodeClient
  private readonly store: PortalStore
  private readonly logger: Logger
  private modelCache: { at: number; models: ModelChoice[] } | null = null
  /** Sessions whose instruction entry could not be written - they get the prompt prefix. */
  private readonly instructionFailed = new Set<string>()

  constructor(cfg: PortalConfig, client: OpenCodeClient, store: PortalStore, logger: Logger) {
    this.cfg = cfg
    this.client = client
    this.store = store
    this.logger = logger
  }

  // ------------------------------------------------------------------ modes

  /** The mode tiles of the start screen: OpenCode agents with their descriptions. */
  async listModes(): Promise<ModeDescription[]> {
    const agents = await this.client.listAgents(this.cfg.opencode.projectDirectory)
    const allowed = this.cfg.agents.allow
    const modes = agents.filter((agent) => {
      if (agent.hidden) return false
      if (allowed.length > 0) return allowed.includes(agent.id)
      if (agent.mode && !this.cfg.agents.modes.includes(agent.mode)) return false
      return true
    })
    return modes.slice(0, this.cfg.agents.max).map((agent) => ({
      id: agent.id,
      name: agent.name,
      description: agent.description ?? "",
    }))
  }

  /** Check that the given agent id is one of the exposed modes. */
  async assertAgent(agentId: string): Promise<AgentInfo> {
    const agents = await this.client.listAgents(this.cfg.opencode.projectDirectory)
    const agent = agents.find((a) => a.id === agentId)
    if (!agent) throw new PortalError(400, "unknown_agent", `Unknown agent: ${agentId}`)
    const allowed = this.cfg.agents.allow
    if (allowed.length > 0 && !allowed.includes(agentId))
      throw new PortalError(400, "unknown_agent", `Agent is not allowed: ${agentId}`)
    if (agent.hidden) throw new PortalError(400, "unknown_agent", `Agent is not allowed: ${agentId}`)
    return agent
  }

  // ----------------------------------------------------------------- models

  /** All models OpenCode currently exposes, matched against `allowed_models`. */
  async allowedModels(): Promise<ModelChoice[]> {
    const now = Date.now()
    if (this.modelCache && now - this.modelCache.at < 60_000) return this.modelCache.models
    let models: ModelChoice[] = []
    try {
      const res = await this.client.request<{ data?: Array<{ id: string; modelID: string; providerID: string }> }>(
        "/api/model",
      )
      models = (res.data ?? [])
        .map((model) => ({
          providerID: model.providerID,
          id: model.modelID,
          full: `${model.providerID}/${model.modelID}`,
        }))
        .filter((model) => this.cfg.allowed_models.includes(model.full))
    } catch (err) {
      this.logger.warn({ event: "model.list.failed", error: (err as Error).message })
      models = this.cfg.allowed_models.map((full) => {
        const separator = full.indexOf("/")
        return { providerID: full.slice(0, separator), id: full.slice(separator + 1), full }
      })
    }
    this.modelCache = { at: now, models }
    return models
  }

  /** Validate a model string against `allowed_models`. Throws 400 when not allowed. */
  async resolveModel(requested?: string): Promise<ModelChoice> {
    const models = await this.allowedModels()
    const wanted = (requested ?? this.cfg.default_model).trim()
    const match = models.find((model) => model.full === wanted)
    if (!match) {
      throw new PortalError(
        400,
        "model_not_allowed",
        `Model is not allowed. Allowed models: ${this.cfg.allowed_models.join(", ")}`,
      )
    }
    return match
  }

  // --------------------------------------------------------------- sessions

  /** Create a session for a user. */
  async createSession(
    user: string,
    input: { agent: string; model?: string; title?: string },
  ): Promise<PortalSession> {
    await this.assertAgent(input.agent)
    const model = await this.resolveModel(input.model)

    const title =
      input.title?.trim() ||
      `${input.agent} - ${user} - ${new Date().toISOString().slice(0, 16).replace("T", " ")}`

    const session = await this.client.createSession({
      title,
      agent: input.agent,
      model: { id: model.id, providerID: model.providerID },
      directory: this.cfg.opencode.projectDirectory,
    })

    const outputDir = this.sessionDir(session.id)
    mkdirSync(outputDir, { recursive: true })
    this.ensureOutputGitignore()

    await this.applySystemRule(session.id, outputDir)

    this.store.createSession(session.id, user, input.agent)
    this.logger.info({ event: "session.created", session: session.id, user, agent: input.agent })

    return this.toPortalSession(session.id, input.agent, session)
  }

  /** All sessions of one user, enriched with the OpenCode side information. */
  async listSessions(user: string): Promise<PortalSession[]> {
    const rows = this.store.listSessions(user)
    if (rows.length === 0) return []
    let remote: SessionInfo[] = []
    try {
      remote = await this.client.listSessions(this.cfg.opencode.projectDirectory)
    } catch (err) {
      this.logger.warn({ event: "session.list.failed", error: (err as Error).message })
    }
    const byId = new Map(remote.map((session) => [session.id, session]))
    return rows.map((row) => {
      const info = byId.get(row.session_id)
      return this.toPortalSession(row.session_id, row.agent, info, row.created_at)
    })
  }

  /** Ownership check. Throws 404 for unknown and for foreign sessions. */
  private requireOwned(user: string, sessionId: string): SessionRow {
    if (!SESSION_ID_PATTERN.test(sessionId)) throw new PortalError(404, "not_found", "Session not found")
    const row = this.store.getSession(sessionId)
    if (!row || row.user !== user) throw new PortalError(404, "not_found", "Session not found")
    return row
  }

  /**
   * Public ownership check.
   * A session that does not exist and a session that belongs to somebody else
   * both answer 404, so session ids cannot be probed.
   */
  assertOwner(user: string, sessionId: string): SessionRow {
    return this.requireOwned(user, sessionId)
  }

  /** Messages of a session the user owns. */
  async listMessages(user: string, sessionId: string): Promise<SessionMessage[]> {
    this.requireOwned(user, sessionId)
    return this.client.listMessages(sessionId, 500, "asc")
  }

  /** Send a message to a session the user owns. */
  async sendMessage(
    user: string,
    sessionId: string,
    input: { text: string; agent?: string },
  ): Promise<{ text: string; agent?: string }> {
    const row = this.requireOwned(user, sessionId)

    const text = input.text.trim()
    if (text.length === 0) throw new PortalError(400, "empty_message", "Message must not be empty")
    if (text.length > this.cfg.limits.maxMessageChars)
      throw new PortalError(
        413,
        "message_too_large",
        `Message is longer than ${this.cfg.limits.maxMessageChars} characters`,
      )

    if (input.agent) {
      await this.assertAgent(input.agent)
      try {
        await this.client.request(`/api/session/${encodeURIComponent(sessionId)}/agent`, {
          method: "POST",
          body: { agent: input.agent },
        })
      } catch (err) {
        throw new PortalError(400, "agent_switch_failed", (err as Error).message)
      }
    }

    const outputDir = this.sessionDir(sessionId)
    const payload = this.buildPromptText(text, outputDir, sessionId)
    await this.client.prompt(sessionId, { text: payload })

    this.logger.info({ event: "message.sent", session: sessionId, user, agent: row.agent })
    return { text, agent: input.agent ?? row.agent }
  }

  /** Delete a session (OpenCode side, files on disk and the ownership row). */
  async deleteSession(user: string, sessionId: string): Promise<void> {
    this.requireOwned(user, sessionId)
    try {
      await this.client.deleteSession(sessionId)
    } catch (err) {
      const status = (err as { status?: number }).status
      if (status !== 404) throw err
    }
    this.removeSessionFiles(sessionId)
    this.store.deleteSession(sessionId)
    this.logger.info({ event: "session.deleted", session: sessionId, user })
  }

  // ------------------------------------------------------------------ files

  /** Directory that holds the result files of a session. */
  sessionDir(sessionId: string): string {
    return join(this.cfg.output.root, sessionId)
  }

  /** List the result files of a session the user owns. */
  listFiles(user: string, sessionId: string): FileEntry[] {
    this.requireOwned(user, sessionId)
    return listSessionFiles(this.sessionDir(sessionId), {
      allowedExtensions: this.cfg.files.allowedExtensions,
      maxBytes: this.cfg.files.maxBytes,
    })
  }

  /** Resolve a downloadable file of a session the user owns. */
  resolveFile(user: string, sessionId: string, name: string): { path: string; size: number } {
    this.requireOwned(user, sessionId)
    try {
      return resolveSessionFile(this.sessionDir(sessionId), name, {
        allowedExtensions: this.cfg.files.allowedExtensions,
        maxBytes: this.cfg.files.maxBytes,
      })
    } catch (err) {
      if (err instanceof FileAccessError) throw new PortalError(fileErrorStatus(err.reason), err.reason, err.message)
      throw err
    }
  }

  private removeSessionFiles(sessionId: string): void {
    const dir = this.sessionDir(sessionId)
    try {
      rmSync(dir, { recursive: true, force: true })
    } catch (err) {
      this.logger.warn({ event: "session.files.remove.failed", session: sessionId, error: (err as Error).message })
    }
  }

  // --------------------------------------------------------------- retention

  /**
   * Remove result folders that are older than `output.retentionDays`.
   * Runs on start-up and then once a day.
   */
  cleanupExpiredOutputs(): number {
    const root = this.cfg.output.root
    if (!existsSync(root)) return 0
    const cutoff = Date.now() - this.cfg.output.retentionDays * 24 * 60 * 60 * 1000
    let removed = 0
    let entries: string[]
    try {
      entries = readdirSync(root)
    } catch {
      return 0
    }
    for (const name of entries) {
      const dir = join(root, name)
      try {
        if (!statSync(dir).isDirectory()) continue
        const mtime = statSync(dir).mtimeMs
        if (mtime > cutoff) continue
        rmSync(dir, { recursive: true, force: true })
        removed++
        this.logger.info({ event: "output.retention.removed", session: name })
      } catch {
        /* ignore */
      }
    }
    return removed
  }

  // ------------------------------------------------------------ system rule

  /**
   * Deliver the "write results only into output/<id>" rule to the model.
   *
   * OpenCode v2 has no `system` field on the prompt route (verified against the
   * OpenAPI document of the running server). The v2 equivalent is a durable
   * per-session instruction entry; the prompt prefix is the fallback.
   */
  private async applySystemRule(sessionId: string, outputDir: string): Promise<void> {
    const rule = this.systemRuleText(outputDir, sessionId)
    if (this.cfg.systemRule.mode === "instruction" || this.cfg.systemRule.mode === "both") {
      try {
        await this.client.putInstruction(sessionId, this.cfg.systemRule.key, rule)
        return
      } catch (err) {
        this.logger.warn({
          event: "systemRule.instruction.failed",
          session: sessionId,
          error: (err as Error).message,
        })
        // Fall back to the prompt prefix so that the rule is still delivered.
        this.instructionFailed.add(sessionId)
      }
    }
  }

  /** The rule text that is sent to the model. */
  systemRuleText(outputDir: string, sessionId: string): string {
    return [
      `Каталог результатов этой сессии: ${outputDir}`,
      `Правила работы с файлами для сессии ${sessionId}:`,
      `- Все файлы результата писать ТОЛЬКО в каталог ${outputDir} (output\\${sessionId}\\).`,
      "- Никакие другие файлы не создавать и не изменять.",
      "- Ответ в чат обязательно продублировать текстом: пользователь видит только текст ответа, файлы он скачивает отдельно.",
      "- В ответе указать имена созданных файлов.",
    ].join("\n")
  }

  /** Build the text that is actually sent to OpenCode. */
  private buildPromptText(text: string, outputDir: string, sessionId: string): string {
    const usePrefix =
      this.cfg.systemRule.mode === "prompt" ||
      this.cfg.systemRule.mode === "both" ||
      this.instructionFailed.has(sessionId)
    if (!usePrefix) return text
    const rule = this.systemRuleText(outputDir, sessionId)
    return `[system]\n${rule}\n[/system]\n\n${text}`
  }

  // ----------------------------------------------------------------- helpers

  private toPortalSession(
    sessionId: string,
    agent: string,
    info?: SessionInfo,
    fallbackCreated?: number,
  ): PortalSession {
    const created = info?.time?.created ?? fallbackCreated ?? Date.now()
    const updated = info?.time?.updated ?? created
    return {
      id: sessionId,
      agent,
      title: info?.title?.trim() || agent,
      createdAt: created,
      updatedAt: updated,
      outputDir: this.sessionDir(sessionId),
    }
  }

  /** Keep the result folder out of git. */
  private ensureOutputGitignore(): void {
    if (!this.cfg.output.writeGitignore) return
    const marker = join(this.cfg.output.root, ".gitignore")
    if (existsSync(marker)) return
    try {
      mkdirSync(this.cfg.output.root, { recursive: true })
      writeFileSync(marker, "*\n!.gitignore\n", "utf8")
    } catch {
      /* best effort */
    }
  }

  /** All session ids the portal knows about (used by the permission sweep). */
  knownSessionIds(): string[] {
    return this.store.listAllSessions().map((row) => row.session_id)
  }
}
