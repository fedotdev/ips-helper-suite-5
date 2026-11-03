/**
 * Discovery of the shared OpenCode server.
 *
 * The background service (`opencode serve --service`) picks a random port, so
 * the portal can either use a fixed URL from the configuration or ask OpenCode
 * where the service currently listens (`opencode service status`).
 */

import { spawnSync } from "node:child_process"

export interface DiscoveryOptions {
  /** Command that starts the OpenCode CLI. */
  command?: string
  /** Timeout for the helper command, ms. */
  timeoutMs?: number
}

/** Ask OpenCode for the URL of the background service. Returns null when unavailable. */
export function discoverServiceUrl(options: DiscoveryOptions = {}): string | null {
  const command = options.command ?? "opencode"
  try {
    // On Windows the npm shim is a .cmd file, so the command has to go through the shell.
    const useShell = process.platform === "win32"
    const result = useShell
      ? spawnSync(`${command} service status`, {
          encoding: "utf8",
          timeout: options.timeoutMs ?? 10_000,
          windowsHide: true,
          shell: true,
        })
      : spawnSync(command, ["service", "status"], {
          encoding: "utf8",
          timeout: options.timeoutMs ?? 10_000,
          windowsHide: true,
        })
    if (result.error || result.status !== 0) return null
    const match = `${result.stdout ?? ""}\n${result.stderr ?? ""}`.match(/https?:\/\/[^\s"']+/)
    return match ? match[0].trim() : null
  } catch {
    return null
  }
}
