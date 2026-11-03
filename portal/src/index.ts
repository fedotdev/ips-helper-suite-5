/**
 * Portal entry point.
 *
 *   node src/index.ts [--config <path>] [--port <n>] [--host <addr>]
 *
 * The OpenCode password is read from the environment variable named in
 * `opencode.passwordEnv` (default OPENCODE_SERVER_PASSWORD) - it is never
 * stored in the repository.
 */

import { loadConfig, resolvePassword, defaultConfigPath, ConfigError, PORTAL_DIR } from "./config.ts"
import { Logger } from "./logger.ts"
import { PortalStore } from "./db.ts"
import { OpenCodeClient } from "./opencode.ts"
import { createPortal } from "./server.ts"
import { discoverServiceUrl } from "./discovery.ts"
import { join } from "node:path"

function parseArgs(argv: string[]): { config: string; port?: number; host?: string } {
  const result: { config: string; port?: number; host?: string } = { config: defaultConfigPath() }
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i]
    if (arg === "--config" && argv[i + 1]) result.config = argv[++i]!
    else if (arg === "--port" && argv[i + 1]) result.port = Number(argv[++i])
    else if (arg === "--host" && argv[i + 1]) result.host = argv[++i]!
  }
  return result
}

async function main(): Promise<void> {
  const args = parseArgs(process.argv.slice(2))

  let config
  try {
    config = loadConfig(args.config)
  } catch (err) {
    if (err instanceof ConfigError) {
      process.stderr.write(`Configuration error: ${err.message}\n`)
      process.exit(78) // EX_CONFIG
    }
    throw err
  }
  if (args.port) config.server.port = args.port
  if (args.host) config.server.host = args.host

  const logger = new Logger(config)
  const password = resolvePassword(config)

  // The shared OpenCode service listens on a random port: "auto" asks OpenCode
  // where it currently runs.
  let baseUrl = config.opencode.baseUrl
  if (baseUrl === "auto" || baseUrl === "") {
    const discovered = discoverServiceUrl()
    if (!discovered) {
      process.stderr.write(
        "Cannot find the OpenCode service. Set opencode.baseUrl in config.json or run `opencode service start`.\n",
      )
      process.exit(69) // EX_UNAVAILABLE
    }
    baseUrl = discovered
    config.opencode.baseUrl = discovered
  }

  const store = new PortalStore(join(PORTAL_DIR, "data", "portal.db"))
  const client = new OpenCodeClient({
    baseUrl,
    username: config.opencode.username,
    password,
    timeoutMs: config.opencode.timeoutMs,
    resolveBaseUrl: () => discoverServiceUrl(),
  })

  const portal = createPortal({ config, client, store, logger })
  const address = await portal.listen()
  logger.info({
    event: "portal.started",
    host: address.host,
    port: address.port,
    opencode: config.opencode.baseUrl,
    project: config.opencode.projectDirectory,
    output: config.output.root,
    models: config.allowed_models,
  })
  process.stdout.write(`Portal listening on http://${address.host}:${address.port}\n`)

  const shutdown = async (signal: string) => {
    logger.info({ event: "portal.stopping", signal })
    await portal.close()
    process.exit(0)
  }
  process.on("SIGINT", () => void shutdown("SIGINT"))
  process.on("SIGTERM", () => void shutdown("SIGTERM"))
}

main().catch((err) => {
  process.stderr.write(`Fatal: ${(err as Error).message}\n`)
  process.exit(1)
})
