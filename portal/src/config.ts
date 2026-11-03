/**
 * Portal configuration: loading, defaults and validation.
 *
 * The effective configuration is the merge of (1) built-in defaults, (2) the JSON
 * file passed to `loadConfig`. The file lives at `portal/config.json` and is NOT
 * committed to git (see `portal/.gitignore`); `portal/config.example.json` is the
 * committed template.
 */

import { readFileSync, existsSync } from "node:fs"
import { isAbsolute, resolve, dirname, join } from "node:path"
import { fileURLToPath } from "node:url"

export type SystemRuleMode = "instruction" | "prompt" | "both"

export interface PortalConfig {
  /** Portal HTTP listener. */
  server: { host: string; port: number }
  /** Connection to the single shared `opencode serve` instance. */
  opencode: {
    baseUrl: string
    username: string
    /** Environment variable that holds OPENCODE_SERVER_PASSWORD. */
    passwordEnv: string
    /** Optional file that holds the password (alternative to the env var). */
    passwordFile: string | null
    /** The one and only OpenCode project directory. */
    projectDirectory: string
    /** Per-request timeout for the OpenCode API, ms. */
    timeoutMs: number
  }
  /** Whitelist of models. Anything else is rejected by the portal. */
  allowed_models: string[]
  /** Model used when the caller does not pick one. Must be in `allowed_models`. */
  default_model: string
  /** Which OpenCode agents are exposed as "modes" on the start screen. */
  agents: { allow: string[]; modes: string[]; max: number }
  /** Where session result files live: `<output.root>/<session id>`. */
  output: { root: string; retentionDays: number; writeGitignore: boolean }
  /** Abuse limits, per user. */
  limits: {
    maxMessageChars: number
    requestsPerMinute: number
    messagesPerMinute: number
    maxBodyBytes: number
  }
  /** Rules for serving session result files from disk. */
  files: { allowedExtensions: string[]; maxBytes: number }
  /** Authentication stub (swap for AD/SSO in src/auth.ts). */
  auth: { header: string; userPattern: string }
  /** Logging. Message bodies are never logged. */
  logging: { level: "debug" | "info" | "warn" | "error"; file: string | null; console: boolean }
  /**
   * How the per-session "write results only to output/<id>" rule reaches the model.
   * - "instruction": PUT /api/experimental/session/{id}/instructions/entries/{key} (v2 per-session instruction)
   * - "prompt":     prefix the rule to the user text
   * - "both":       instruction first, prompt prefix as fallback
   */
  systemRule: { mode: SystemRuleMode; key: string }
}

export class ConfigError extends Error {}

const HERE = dirname(fileURLToPath(import.meta.url))
/** portal/ */
export const PORTAL_DIR = resolve(HERE, "..")
/** repository root that holds portal/ */
export const REPO_DIR = resolve(PORTAL_DIR, "..")

function defaults(): PortalConfig {
  return {
    server: { host: "127.0.0.1", port: 8099 },
    opencode: {
      baseUrl: "http://127.0.0.1:4096",
      username: "opencode",
      passwordEnv: "OPENCODE_SERVER_PASSWORD",
      passwordFile: null,
      projectDirectory: REPO_DIR,
      timeoutMs: 60_000,
    },
    allowed_models: ["opencode/mimo-v2.6-flash-free"],
    default_model: "opencode/mimo-v2.6-flash-free",
    agents: { allow: [], modes: ["primary", "all"], max: 3 },
    output: { root: "output", retentionDays: 7, writeGitignore: true },
    limits: {
      maxMessageChars: 20_000,
      requestsPerMinute: 60,
      messagesPerMinute: 20,
      maxBodyBytes: 256 * 1024,
    },
    files: { allowedExtensions: ["md", "txt", "csv"], maxBytes: 10 * 1024 * 1024 },
    auth: { header: "X-User", userPattern: "^[A-Za-z0-9._\\\\@ -]{1,64}$" },
    logging: { level: "info", file: "logs/portal.log", console: true },
    systemRule: { mode: "instruction", key: "portal-output" },
  }
}

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return typeof v === "object" && v !== null && !Array.isArray(v)
}

function merge<T>(base: T, patch: unknown, path: string): T {
  if (patch === undefined) return base
  if (!isPlainObject(patch)) throw new ConfigError(`${path} must be an object`)
  if (!isPlainObject(base)) return patch as T
  const out: Record<string, unknown> = { ...base }
  for (const [key, value] of Object.entries(patch)) {
    if (value === undefined) continue
    const child = (base as Record<string, unknown>)[key]
    out[key] = isPlainObject(child) && isPlainObject(value) ? merge(child, value, `${path}.${key}`) : value
  }
  return out as T
}

function requireString(value: unknown, path: string): string {
  if (typeof value !== "string" || value.trim() === "") throw new ConfigError(`${path} must be a non-empty string`)
  return value
}

function requireNumber(value: unknown, path: string, min = 0): number {
  if (typeof value !== "number" || !Number.isFinite(value) || value < min)
    throw new ConfigError(`${path} must be a number >= ${min}`)
  return value
}

function requireStringArray(value: unknown, path: string): string[] {
  if (!Array.isArray(value) || value.some((v) => typeof v !== "string"))
    throw new ConfigError(`${path} must be an array of strings`)
  return value as string[]
}

/**
 * Load and validate the configuration.
 * @param path absolute or relative path to config.json; when missing, defaults are used.
 */
export function loadConfig(path: string): PortalConfig {
  let raw: unknown = {}
  if (existsSync(path)) {
    let text: string
    try {
      text = readFileSync(path, "utf8")
    } catch (err) {
      throw new ConfigError(`cannot read ${path}: ${(err as Error).message}`)
    }
    try {
      raw = JSON.parse(stripJsonComments(text))
    } catch (err) {
      throw new ConfigError(`${path} is not valid JSON: ${(err as Error).message}`)
    }
  }

  const cfg = merge(defaults(), raw, "config")

  // --- server -------------------------------------------------------------
  cfg.server.host = requireString(cfg.server.host, "server.host")
  cfg.server.port = requireNumber(cfg.server.port, "server.port", 1)

  // --- opencode -----------------------------------------------------------
  cfg.opencode.baseUrl = requireString(cfg.opencode.baseUrl, "opencode.baseUrl").replace(/\/+$/, "")
  cfg.opencode.username = requireString(cfg.opencode.username, "opencode.username")
  cfg.opencode.passwordEnv = requireString(cfg.opencode.passwordEnv, "opencode.passwordEnv")
  cfg.opencode.projectDirectory = resolve(requireString(cfg.opencode.projectDirectory, "opencode.projectDirectory"))
  cfg.opencode.timeoutMs = requireNumber(cfg.opencode.timeoutMs, "opencode.timeoutMs", 1000)
  if (!cfg.opencode.passwordFile) cfg.opencode.passwordFile = null
  else cfg.opencode.passwordFile = resolve(cfg.opencode.passwordFile)

  // --- models -------------------------------------------------------------
  cfg.allowed_models = requireStringArray(cfg.allowed_models, "allowed_models")
  if (cfg.allowed_models.length === 0) throw new ConfigError("allowed_models must not be empty")
  cfg.default_model = requireString(cfg.default_model, "default_model")
  if (!cfg.allowed_models.includes(cfg.default_model))
    throw new ConfigError(`default_model "${cfg.default_model}" is not in allowed_models`)

  // --- agents -------------------------------------------------------------
  cfg.agents.allow = requireStringArray(cfg.agents.allow, "agents.allow")
  cfg.agents.modes = requireStringArray(cfg.agents.modes, "agents.modes")
  cfg.agents.max = requireNumber(cfg.agents.max, "agents.max", 1)

  // --- output -------------------------------------------------------------
  cfg.output.root = requireString(cfg.output.root, "output.root")
  if (!isAbsolute(cfg.output.root)) cfg.output.root = resolve(cfg.opencode.projectDirectory, cfg.output.root)
  cfg.output.retentionDays = requireNumber(cfg.output.retentionDays, "output.retentionDays", 0)

  // --- limits -------------------------------------------------------------
  cfg.limits.maxMessageChars = requireNumber(cfg.limits.maxMessageChars, "limits.maxMessageChars", 1)
  cfg.limits.requestsPerMinute = requireNumber(cfg.limits.requestsPerMinute, "limits.requestsPerMinute", 1)
  cfg.limits.messagesPerMinute = requireNumber(cfg.limits.messagesPerMinute, "limits.messagesPerMinute", 1)
  cfg.limits.maxBodyBytes = requireNumber(cfg.limits.maxBodyBytes, "limits.maxBodyBytes", 1024)

  // --- files --------------------------------------------------------------
  cfg.files.allowedExtensions = requireStringArray(cfg.files.allowedExtensions, "files.allowedExtensions").map((e) =>
    e.toLowerCase().replace(/^\./, ""),
  )
  cfg.files.maxBytes = requireNumber(cfg.files.maxBytes, "files.maxBytes", 1)

  // --- auth ---------------------------------------------------------------
  cfg.auth.header = requireString(cfg.auth.header, "auth.header")
  cfg.auth.userPattern = requireString(cfg.auth.userPattern, "auth.userPattern")
  try {
    // eslint-disable-next-line no-new
    new RegExp(cfg.auth.userPattern)
  } catch (err) {
    throw new ConfigError(`auth.userPattern is not a valid regex: ${(err as Error).message}`)
  }

  // --- logging ------------------------------------------------------------
  if (!["debug", "info", "warn", "error"].includes(cfg.logging.level))
    throw new ConfigError("logging.level must be one of debug|info|warn|error")
  if (cfg.logging.file) cfg.logging.file = resolve(PORTAL_DIR, cfg.logging.file)

  // --- system rule --------------------------------------------------------
  if (!["instruction", "prompt", "both"].includes(cfg.systemRule.mode))
    throw new ConfigError("systemRule.mode must be one of instruction|prompt|both")
  cfg.systemRule.key = requireString(cfg.systemRule.key, "systemRule.key")
  if (!/^[a-z0-9][a-z0-9._-]*$/.test(cfg.systemRule.key))
    throw new ConfigError("systemRule.key must match ^[a-z0-9][a-z0-9._-]*$")

  return cfg
}

/** Remove `//` and `/* *\/` comments and trailing commas from a JSONC document. */
function stripJsonComments(text: string): string {
  let out = ""
  let inString = false
  let inLine = false
  let inBlock = false
  for (let i = 0; i < text.length; i++) {
    const ch = text[i]
    const next = text[i + 1]
    if (inLine) {
      if (ch === "\n") {
        inLine = false
        out += ch
      }
      continue
    }
    if (inBlock) {
      if (ch === "*" && next === "/") {
        inBlock = false
        i++
      }
      continue
    }
    if (inString) {
      out += ch
      if (ch === "\\") {
        out += next ?? ""
        i++
      } else if (ch === '"') inString = false
      continue
    }
    if (ch === '"') {
      inString = true
      out += ch
      continue
    }
    if (ch === "/" && next === "/") {
      inLine = true
      i++
      continue
    }
    if (ch === "/" && next === "*") {
      inBlock = true
      i++
      continue
    }
    out += ch
  }
  // JSONC allows a trailing comma before } or ].
  return out.replace(/,(\s*[}\]])/g, "$1")
}

/**
 * Resolve the OpenCode password from the environment or the configured file.
 *
 * The file may be a plain text password or a JSON document with a `password`
 * field - the latter is exactly the format of OpenCode's own
 * `~/.config/opencode/service.json`, so the portal can reuse it without
 * duplicating the secret.
 */
export function resolvePassword(cfg: PortalConfig, env: NodeJS.ProcessEnv = process.env): string {
  const fromEnv = env[cfg.opencode.passwordEnv]
  if (fromEnv && fromEnv.trim() !== "") return fromEnv.trim()
  if (cfg.opencode.passwordFile) {
    try {
      const text = readFileSync(cfg.opencode.passwordFile, "utf8").trim()
      if (text.startsWith("{")) {
        const parsed = JSON.parse(text) as { password?: unknown }
        if (typeof parsed.password === "string" && parsed.password.trim() !== "") return parsed.password.trim()
      } else if (text) {
        return text
      }
    } catch {
      /* fall through to the error below */
    }
  }
  throw new ConfigError(
    `OpenCode server password not found: set ${cfg.opencode.passwordEnv} or point opencode.passwordFile at a file`,
  )
}

/** Default location of the config file. */
export function defaultConfigPath(): string {
  return join(PORTAL_DIR, "config.json")
}
