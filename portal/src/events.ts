/**
 * Event broker.
 *
 * The portal opens ONE upstream SSE connection to OpenCode (`GET /api/event`)
 * and fans the events out to the per-session SSE subscribers. The upstream
 * stream is global (all locations, all sessions), so every event is filtered
 * by `data.sessionID` before it reaches a browser.
 *
 * The upstream stream is volatile by contract: it is re-established after a
 * disconnect with a small backoff.
 */

import type { OpenCodeClient, OpenCodeEvent } from "./opencode.ts"
import type { Logger } from "./logger.ts"

export type EventListener = (event: OpenCodeEvent) => void

interface Subscription {
  sessionId: string
  listener: EventListener
}

export class EventBroker {
  private readonly client: OpenCodeClient
  private readonly logger: Logger
  private readonly subscriptions = new Set<Subscription>()
  private readonly globalListeners = new Set<EventListener>()
  private controller: AbortController | null = null
  private running = false
  private reconnectDelayMs = 500

  constructor(client: OpenCodeClient, logger: Logger) {
    this.client = client
    this.logger = logger
  }

  /** Open the upstream connection (idempotent). */
  start(): void {
    if (this.running) return
    this.running = true
    void this.loop()
  }

  /** Close the upstream connection. */
  stop(): void {
    this.running = false
    this.controller?.abort()
    this.controller = null
  }

  /**
   * Subscribe to the events of one session.
   * @returns an unsubscribe function.
   */
  subscribe(sessionId: string, listener: EventListener): () => void {
    const subscription: Subscription = { sessionId, listener }
    this.subscriptions.add(subscription)
    return () => {
      this.subscriptions.delete(subscription)
    }
  }

  /** Subscribe to every upstream event (used by the permission guard). */
  subscribeAll(listener: EventListener): () => void {
    this.globalListeners.add(listener)
    return () => {
      this.globalListeners.delete(listener)
    }
  }

  /**
   * Publish a portal-local event to the subscribers of one session.
   * Used for events the portal itself produces (e.g. an auto-rejected
   * permission request).
   */
  publishLocal(sessionId: string, type: string, data: Record<string, unknown>): void {
    const event: OpenCodeEvent = { type, data: { sessionID: sessionId, ...data } }
    for (const subscription of this.subscriptions) {
      if (subscription.sessionId !== sessionId) continue
      try {
        subscription.listener(event)
      } catch (err) {
        this.logger.warn({ event: "sse.deliver.failed", error: (err as Error).message })
      }
    }
  }

  private async loop(): Promise<void> {
    while (this.running) {
      const controller = new AbortController()
      this.controller = controller
      try {
        this.logger.debug({ event: "sse.upstream.connect" })
        for await (const event of this.client.streamEvents(controller.signal)) {
          this.reconnectDelayMs = 500
          this.dispatch(event)
        }
        if (!this.running) break
        this.logger.warn({ event: "sse.upstream.closed" })
      } catch (err) {
        if (!this.running) break
        this.logger.warn({ event: "sse.upstream.error", error: (err as Error).message })
      } finally {
        controller.abort()
      }
      if (!this.running) break
      await delay(this.reconnectDelayMs)
      this.reconnectDelayMs = Math.min(this.reconnectDelayMs * 2, 15_000)
    }
    this.logger.info({ event: "sse.upstream.stopped" })
  }

  private dispatch(event: OpenCodeEvent): void {
    for (const listener of this.globalListeners) {
      try {
        listener(event)
      } catch (err) {
        this.logger.warn({ event: "sse.global.failed", error: (err as Error).message })
      }
    }
    const sessionId = event.data?.sessionID
    if (typeof sessionId !== "string") return
    for (const subscription of this.subscriptions) {
      if (subscription.sessionId !== sessionId) continue
      try {
        subscription.listener(event)
      } catch (err) {
        this.logger.warn({ event: "sse.deliver.failed", session: sessionId, error: (err as Error).message })
      }
    }
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms))
}
