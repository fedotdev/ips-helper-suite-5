/**
 * Per-user rate limiting (token bucket).
 *
 * Two independent buckets per user:
 *  - `requests`  - every authenticated API call,
 *  - `messages`  - message sends (POST /api/sessions/:id/messages).
 */

interface Bucket {
  tokens: number
  updatedAt: number
}

export interface RateLimitOptions {
  /** Requests allowed per minute. */
  requestsPerMinute: number
  /** Messages allowed per minute. */
  messagesPerMinute: number
}

export interface RateLimitResult {
  allowed: boolean
  /** Seconds until the bucket refills enough for one more call. */
  retryAfterSeconds: number
  limit: number
  remaining: number
}

export class RateLimiter {
  private readonly options: RateLimitOptions
  private readonly requests = new Map<string, Bucket>()
  private readonly messages = new Map<string, Bucket>()

  constructor(options: RateLimitOptions) {
    this.options = options
  }

  /** Check and consume one request token. */
  takeRequest(user: string): RateLimitResult {
    return this.take(this.requests, user, this.options.requestsPerMinute)
  }

  /** Check and consume one message token. */
  takeMessage(user: string): RateLimitResult {
    return this.take(this.messages, user, this.options.messagesPerMinute)
  }

  private take(buckets: Map<string, Bucket>, user: string, limit: number): RateLimitResult {
    const now = Date.now()
    const bucket = buckets.get(user) ?? { tokens: limit, updatedAt: now }
    const elapsedMinutes = (now - bucket.updatedAt) / 60_000
    bucket.tokens = Math.min(limit, bucket.tokens + elapsedMinutes * limit)
    bucket.updatedAt = now
    if (bucket.tokens < 1) {
      buckets.set(user, bucket)
      const missing = 1 - bucket.tokens
      return {
        allowed: false,
        retryAfterSeconds: Math.max(1, Math.ceil((missing / limit) * 60)),
        limit,
        remaining: 0,
      }
    }
    bucket.tokens -= 1
    buckets.set(user, bucket)
    return { allowed: true, retryAfterSeconds: 0, limit, remaining: Math.floor(bucket.tokens) }
  }

  /** Drop all state of a user (used on logout / tests). */
  reset(user: string): void {
    this.requests.delete(user)
    this.messages.delete(user)
  }

  /** Drop buckets that have been idle for more than an hour. */
  prune(maxIdleMs = 3_600_000): void {
    const cutoff = Date.now() - maxIdleMs
    for (const [key, bucket] of this.requests) if (bucket.updatedAt < cutoff) this.requests.delete(key)
    for (const [key, bucket] of this.messages) if (bucket.updatedAt < cutoff) this.messages.delete(key)
  }
}
