/**
 * Portal persistence.
 *
 * SQLite via the built-in `node:sqlite` module (Node >= 22.5, stable in Node 24+).
 * No native modules, no npm dependencies.
 *
 * The portal stores only the ownership mapping (user -> OpenCode session id).
 * Chat history itself lives inside OpenCode; files live in `output/<session id>`.
 */

import { DatabaseSync } from "node:sqlite"
import { mkdirSync } from "node:fs"
import { dirname } from "node:path"

export interface SessionRow {
  session_id: string
  user: string
  agent: string
  created_at: number
}

export interface UserRow {
  id: string
  created_at: number
  last_seen_at: number
}

export class PortalStore {
  private readonly db: DatabaseSync

  constructor(path: string) {
    if (path !== ":memory:") mkdirSync(dirname(path), { recursive: true })
    this.db = new DatabaseSync(path)
    this.db.exec("PRAGMA journal_mode = WAL")
    this.db.exec("PRAGMA foreign_keys = ON")
    this.migrate()
  }

  private migrate(): void {
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS users (
        id           TEXT PRIMARY KEY,
        created_at   INTEGER NOT NULL,
        last_seen_at INTEGER NOT NULL
      );

      CREATE TABLE IF NOT EXISTS sessions (
        session_id TEXT PRIMARY KEY,
        user       TEXT NOT NULL,
        agent      TEXT NOT NULL,
        created_at INTEGER NOT NULL,
        FOREIGN KEY (user) REFERENCES users(id) ON DELETE CASCADE
      );

      CREATE INDEX IF NOT EXISTS idx_sessions_user ON sessions(user);
      CREATE INDEX IF NOT EXISTS idx_sessions_created ON sessions(created_at);
    `)
  }

  /** Register the user on first sight and refresh `last_seen_at`. */
  touchUser(userId: string, now = Date.now()): void {
    this.db
      .prepare(
        `INSERT INTO users (id, created_at, last_seen_at) VALUES (?, ?, ?)
         ON CONFLICT(id) DO UPDATE SET last_seen_at = excluded.last_seen_at`,
      )
      .run(userId, now, now)
  }

  getUser(userId: string): UserRow | undefined {
    return this.db.prepare(`SELECT id, created_at, last_seen_at FROM users WHERE id = ?`).get(userId) as
      | UserRow
      | undefined
  }

  listUsers(): UserRow[] {
    return this.db.prepare(`SELECT id, created_at, last_seen_at FROM users ORDER BY id`).all() as UserRow[]
  }

  /** Record ownership of a session. Re-assigning a session id overwrites the owner. */
  createSession(sessionId: string, user: string, agent: string, now = Date.now()): void {
    this.db
      .prepare(
        `INSERT INTO sessions (session_id, user, agent, created_at) VALUES (?, ?, ?, ?)
         ON CONFLICT(session_id) DO UPDATE SET user = excluded.user, agent = excluded.agent`,
      )
      .run(sessionId, user, agent, now)
  }

  getSession(sessionId: string): SessionRow | undefined {
    return this.db.prepare(`SELECT session_id, user, agent, created_at FROM sessions WHERE session_id = ?`).get(
      sessionId,
    ) as SessionRow | undefined
  }

  listSessions(user: string): SessionRow[] {
    return this.db
      .prepare(`SELECT session_id, user, agent, created_at FROM sessions WHERE user = ? ORDER BY created_at DESC`)
      .all(user) as SessionRow[]
  }

  /** All sessions, regardless of owner. Used by the retention job. */
  listAllSessions(): SessionRow[] {
    return this.db
      .prepare(`SELECT session_id, user, agent, created_at FROM sessions ORDER BY created_at DESC`)
      .all() as SessionRow[]
  }

  deleteSession(sessionId: string): void {
    this.db.prepare(`DELETE FROM sessions WHERE session_id = ?`).run(sessionId)
  }

  close(): void {
    try {
      this.db.close()
    } catch {
      /* ignore */
    }
  }
}
