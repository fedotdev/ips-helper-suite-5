/**
 * Permission guard.
 *
 * Policy: the portal never shows a permission request to the user. When
 * OpenCode asks for a permission, the portal answers "reject" itself through
 * `POST /api/session/{id}/permission/{requestID}/reply` and tells the user
 * about it in plain language.
 *
 * Two mechanisms:
 *  1. reactive - the `permission.asked` event arrives on the event stream;
 *  2. periodic sweep - pending requests are re-checked for every known session,
 *     which covers requests that were raised while the portal was down
 *     (the OpenCode event stream is volatile and does not replay).
 */

import type { OpenCodeClient, PermissionRequest } from "./opencode.ts"
import type { EventBroker } from "./events.ts"
import type { Logger } from "./logger.ts"

export interface PermissionGuardOptions {
  client: OpenCodeClient
  broker: EventBroker
  logger: Logger
  /** Session ids the portal currently knows about. */
  knownSessions: () => string[]
  /** Called after a request was rejected, so the owner can be informed. */
  onRejected: (sessionId: string, request: PermissionRequest) => void
  /** Message sent to OpenCode together with the rejection. */
  message?: string
  sweepIntervalMs?: number
}

export class PermissionGuard {
  private readonly options: PermissionGuardOptions
  private timer: NodeJS.Timeout | null = null
  private readonly handled = new Set<string>()

  constructor(options: PermissionGuardOptions) {
    this.options = options
  }

  start(): void {
    this.options.broker.subscribeAll((event) => {
      if (event.type !== "permission.asked") return
      const request = event.data as unknown as PermissionRequest
      if (!request || typeof request.id !== "string" || typeof request.sessionID !== "string") return
      void this.reject(request)
    })
    const interval = this.options.sweepIntervalMs ?? 30_000
    this.timer = setInterval(() => {
      void this.sweep()
    }, interval)
    this.timer.unref?.()
    void this.sweep()
  }

  stop(): void {
    if (this.timer) clearInterval(this.timer)
    this.timer = null
  }

  /** Reject every pending permission request of every known session. */
  async sweep(): Promise<void> {
    for (const sessionId of this.options.knownSessions()) {
      let requests: PermissionRequest[]
      try {
        requests = await this.options.client.listPermissions(sessionId)
      } catch (err) {
        this.options.logger.debug({ event: "permission.sweep.failed", session: sessionId, error: (err as Error).message })
        continue
      }
      for (const request of requests) {
        if (!request?.id) continue
        if (this.handled.has(request.id)) continue
        await this.reject(request)
      }
    }
  }

  private async reject(request: PermissionRequest): Promise<void> {
    const key = `${request.sessionID}:${request.id}`
    if (this.handled.has(key)) return
    this.handled.add(key)
    const message =
      this.options.message ??
      "Портал работает в автоматическом режиме: запросы на разрешение отклоняются автоматически."
    try {
      await this.options.client.replyPermission(request.sessionID, request.id, "reject", message)
      this.options.logger.warn({
        event: "permission.rejected",
        session: request.sessionID,
        action: request.action,
        requestId: request.id,
      })
      this.options.onRejected(request.sessionID, request)
    } catch (err) {
      this.handled.delete(key)
      this.options.logger.warn({
        event: "permission.reject.failed",
        session: request.sessionID,
        requestId: request.id,
        error: (err as Error).message,
      })
    }
  }
}
