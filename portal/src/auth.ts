/**
 * Authentication.
 *
 * This is the ONLY module that decides who the caller is. It is deliberately
 * self-contained so it can be replaced by AD / SSO without touching any other
 * module: implement `AuthProvider` and pass it to `createServer`.
 *
 * Current implementation: a stub that trusts the `X-User` request header.
 * The header value is validated against `auth.userPattern` and normalized to
 * lower case so that "Fedor" and "fedor" are the same portal user.
 */

import type { IncomingMessage } from "node:http"
import type { PortalConfig } from "./config.ts"

export interface AuthenticatedUser {
  /** Normalized user id, used as the key in the `users` table. */
  id: string
  /** Human readable name for the UI. */
  display: string
}

export interface AuthResult {
  user: AuthenticatedUser | null
  /** Short machine readable reason, safe to return to the client. */
  reason?: "missing" | "invalid"
}

export interface AuthProvider {
  /** Name of the scheme, used in logs. */
  readonly name: string
  authenticate(req: IncomingMessage): Promise<AuthResult> | AuthResult
}

/** Reads the login from a request header. Replace this class for AD / SSO. */
export class HeaderUserAuth implements AuthProvider {
  readonly name = "header-stub"
  private readonly header: string
  private readonly pattern: RegExp

  constructor(cfg: PortalConfig) {
    this.header = cfg.auth.header.toLowerCase()
    this.pattern = new RegExp(cfg.auth.userPattern)
  }

  authenticate(req: IncomingMessage): AuthResult {
    const raw = req.headers[this.header]
    const value = Array.isArray(raw) ? raw[0] : raw
    if (typeof value !== "string" || value.trim() === "") return { user: null, reason: "missing" }
    const display = value.trim()
    if (!this.pattern.test(display)) return { user: null, reason: "invalid" }
    return { user: { id: display.toLowerCase(), display } }
  }
}

/**
 * Normalize a user id the same way `HeaderUserAuth` does.
 * Used by the store so that a user is always the same row.
 */
export function normalizeUserId(id: string): string {
  return id.trim().toLowerCase()
}
