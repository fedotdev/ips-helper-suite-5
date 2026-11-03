/**
 * Structured logger.
 *
 * Rule: never log message bodies, prompts or file contents. Only metadata that
 * identifies who did what, when.
 */

import { appendFileSync, mkdirSync } from "node:fs"
import { dirname } from "node:path"
import type { PortalConfig } from "./config.ts"

export type LogLevel = "debug" | "info" | "warn" | "error"

export interface LogFields {
  user?: string | null
  session?: string | null
  agent?: string | null
  event?: string
  method?: string
  path?: string
  status?: number
  durationMs?: number
  requestId?: string
  [key: string]: unknown
}

const LEVELS: Record<LogLevel, number> = { debug: 10, info: 20, warn: 30, error: 40 }

/** Keys that must never reach the log file. */
const FORBIDDEN_KEYS = new Set(["text", "body", "content", "prompt", "message", "messages", "parts", "system", "value"])

export class Logger {
  private readonly level: LogLevel
  private readonly file: string | null
  private readonly useConsole: boolean

  constructor(cfg: PortalConfig) {
    this.level = cfg.logging.level
    this.file = cfg.logging.file
    this.useConsole = cfg.logging.console
    if (this.file) {
      try {
        mkdirSync(dirname(this.file), { recursive: true })
      } catch {
        this.file = null
      }
    }
  }

  debug(fields: LogFields): void {
    this.write("debug", fields)
  }
  info(fields: LogFields): void {
    this.write("info", fields)
  }
  warn(fields: LogFields): void {
    this.write("warn", fields)
  }
  error(fields: LogFields): void {
    this.write("error", fields)
  }

  private write(level: LogLevel, fields: LogFields): void {
    if (LEVELS[level] < LEVELS[this.level]) return
    const record: Record<string, unknown> = { ts: new Date().toISOString(), level }
    for (const [key, value] of Object.entries(fields)) {
      if (FORBIDDEN_KEYS.has(key)) continue
      record[key] = value
    }
    const line = JSON.stringify(record)
    if (this.file) {
      try {
        appendFileSync(this.file, line + "\n")
      } catch {
        /* logging must never break the request */
      }
    }
    if (this.useConsole) {
      const stream = level === "error" || level === "warn" ? process.stderr : process.stdout
      stream.write(line + "\n")
    }
  }
}
