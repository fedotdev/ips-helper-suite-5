/**
 * Configuration tests and retention tests.
 */

import { test, describe } from "node:test"
import assert from "node:assert/strict"
import { mkdirSync, writeFileSync, existsSync, utimesSync } from "node:fs"
import { tmpdir } from "node:os"
import { join } from "node:path"
import { loadConfig, ConfigError } from "../src/config.ts"
import { createHarness } from "./harness.ts"

const SCRATCH = join(tmpdir(), "ips-portal-config-test")

describe("configuration", () => {
  test("несуществующий файл - работают значения по умолчанию", () => {
    const config = loadConfig(join(SCRATCH, "does-not-exist.json"))
    assert.equal(config.server.port, 8099)
    assert.equal(config.output.retentionDays, 7)
    assert.deepEqual(config.allowed_models, ["opencode/mimo-v2.6-flash-free"])
    assert.deepEqual(config.files.allowedExtensions, ["md", "txt", "csv"])
    assert.equal(config.systemRule.mode, "instruction")
  })

  test("битый JSON даёт понятную ошибку", () => {
    const path = join(SCRATCH, "broken-config.json")
    mkdirSync(SCRATCH, { recursive: true })
    writeFileSync(path, "{ this is not json", "utf8")
    assert.throws(() => loadConfig(path), (err: unknown) => {
      assert.ok(err instanceof ConfigError)
      assert.match(err.message, /not valid JSON/)
      return true
    })
  })

  test("default_model вне allowed_models отклоняется", () => {
    const path = join(SCRATCH, "bad-model-config.json")
    mkdirSync(SCRATCH, { recursive: true })
    writeFileSync(path, JSON.stringify({ default_model: "opencode/other" }), "utf8")
    assert.throws(() => loadConfig(path), /default_model/)
  })

  test("пустой allowed_models отклоняется", () => {
    const path = join(SCRATCH, "empty-models-config.json")
    mkdirSync(SCRATCH, { recursive: true })
    writeFileSync(path, JSON.stringify({ allowed_models: [] }), "utf8")
    assert.throws(() => loadConfig(path), /allowed_models/)
  })

  test("JSONC с комментариями читается", () => {
    const path = join(SCRATCH, "jsonc-config.json")
    mkdirSync(SCRATCH, { recursive: true })
    writeFileSync(path, "{\n // комментарий\n \"server\": { \"port\": 9111 },\n}\n", "utf8")
    const config = loadConfig(path)
    assert.equal(config.server.port, 9111)
  })
})

describe("retention", () => {
  test("старые папки output удаляются по сроку хранения", async () => {
    const h = await createHarness({
      output: { root: join(process.cwd(), "data", "retention-output"), retentionDays: 7, writeGitignore: true },
    })
    try {
      const root = h.config.output.root
      mkdirSync(root, { recursive: true })

      // Свежая папка - остаётся.
      const fresh = join(root, "ses_fresh01")
      mkdirSync(fresh, { recursive: true })
      writeFileSync(join(fresh, "a.md"), "a", "utf8")

      // Старая папка - удаляется.
      const old = join(root, "ses_old00001")
      mkdirSync(old, { recursive: true })
      writeFileSync(join(old, "b.md"), "b", "utf8")
      const oldTime = new Date(Date.now() - 10 * 24 * 60 * 60 * 1000)
      utimesSync(old, oldTime, oldTime)

      const removed = h.portal.sessions.cleanupExpiredOutputs()
      assert.equal(removed, 1)
      assert.equal(existsSync(old), false, "старая папка удалена")
      assert.equal(existsSync(fresh), true, "свежая папка на месте")
    } finally {
      await h.cleanup()
    }
  })

  test("правило вывода файлов попадает в текст сообщения при режиме prompt", async () => {
    const h = await createHarness({ systemRule: { mode: "prompt", key: "portal-output" } })
    try {
      const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
      const id = (await created.json()).data.id
      await h.request("POST", `/api/sessions/${id}/messages`, { user: "fedor", body: { text: "сделай отчёт" } })
      const session = h.mock.state.sessions.get(id)!
      const userMessage = session.messages.find((message) => message.type === "user") as { text: string }
      assert.ok(userMessage.text.includes("output\\" + id), "в тексте сообщения есть путь output/<id>")
      assert.ok(userMessage.text.includes("сделай отчёт"))
      // В режиме prompt instruction entry не создаётся.
      assert.equal(h.mock.state.instructionEntries.has(id), false)
    } finally {
      await h.cleanup()
    }
  })
})
