/**
 * Portal integration tests.
 *
 * The portal runs against the mock OpenCode server from ./mock-opencode.ts, so
 * the tests exercise the real HTTP surface of the portal without touching a
 * live OpenCode instance.
 */

import { test, describe, before, after } from "node:test"
import assert from "node:assert/strict"
import { mkdirSync, writeFileSync, symlinkSync, existsSync } from "node:fs"
import { join } from "node:path"
import { createHarness, readSse, waitFor, type Harness } from "./harness.ts"

/** Every OpenCode route the portal is allowed to touch. */
const ALLOWED_UPSTREAM = [
  /^\/api\/agent$/,
  /^\/api\/model$/,
  /^\/api\/session$/,
  /^\/api\/session\/[^/]+$/,
  /^\/api\/session\/[^/]+\/message$/,
  /^\/api\/session\/[^/]+\/prompt$/,
  /^\/api\/session\/[^/]+\/agent$/,
  /^\/api\/session\/[^/]+\/permission$/,
  /^\/api\/session\/[^/]+\/permission\/[^/]+\/reply$/,
  /^\/api\/experimental\/session\/[^/]+\/instructions\/entries\/[^/]+$/,
  /^\/api\/event$/,
]

describe("portal", () => {
  let h: Harness

  before(async () => {
    h = await createHarness()
  })

  after(async () => {
    await h.cleanup()
  })

  // ------------------------------------------------------------------ auth

  test("без заголовка X-User запрос отклоняется", async () => {
    const response = await h.request("GET", "/api/agents", { user: null })
    assert.equal(response.status, 401)
    const body = await response.json()
    assert.equal(body.error.code, "missing_login")
  })

  test("некорректный логин отклоняется", async () => {
    const response = await h.request("GET", "/api/agents", { user: "bad user!!" })
    assert.equal(response.status, 401)
    const body = await response.json()
    assert.equal(body.error.code, "invalid_login")
  })

  // ---------------------------------------------------------------- agents

  test("GET /api/agents возвращает три режима с описаниями", async () => {
    const response = await h.request("GET", "/api/agents", { user: "fedor" })
    assert.equal(response.status, 200)
    const body = await response.json()
    assert.equal(body.data.length, 3)
    assert.deepEqual(
      body.data.map((mode: { id: string }) => mode.id),
      ["ips-help", "ips-designer", "ips-technologist"],
    )
    for (const mode of body.data) {
      assert.ok(mode.name.length > 0, "у режима есть название")
      assert.ok(mode.description.length > 0, "у режима есть описание")
    }
  })

  // -------------------------------------------------------------- sessions

  test("POST /api/sessions создаёт сессию и передаёт правило вывода файлов", async () => {
    const response = await h.request("POST", "/api/sessions", {
      user: "fedor",
      body: { agent: "ips-help" },
    })
    assert.equal(response.status, 201)
    const body = await response.json()
    assert.match(body.data.id, /^ses_/)
    assert.equal(body.data.agent, "ips-help")

    // The per-session instruction carries the output folder of the session.
    const entries = h.mock.state.instructionEntries.get(body.data.id)
    assert.ok(entries, "instruction entry создан")
    const rule = entries!.get("portal-output")!
    assert.ok(rule.includes(join(h.config.output.root, body.data.id)), "в правиле указан каталог результатов")
    assert.ok(rule.includes("output\\" + body.data.id), "в правиле указан относительный путь output/<id>")
  })

  test("GET /api/sessions возвращает только мои сессии", async () => {
    const mine = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-designer" } })
    const mineId = (await mine.json()).data.id
    const other = await h.request("POST", "/api/sessions", { user: "ivan", body: { agent: "ips-help" } })
    const otherId = (await other.json()).data.id

    const response = await h.request("GET", "/api/sessions", { user: "fedor" })
    const body = await response.json()
    const ids = body.data.map((session: { id: string }) => session.id)
    assert.ok(ids.includes(mineId), "своя сессия видна")
    assert.ok(!ids.includes(otherId), "чужая сессия не видна")
  })

  test("POST /api/sessions/:id/messages отправляет сообщение агенту", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id

    const response = await h.request("POST", `/api/sessions/${id}/messages`, {
      user: "fedor",
      body: { text: "Как оформить штамп по ГОСТ?" },
    })
    assert.equal(response.status, 202)

    const session = h.mock.state.sessions.get(id)!
    const userMessage = session.messages.find((message) => message.type === "user") as { text: string }
    assert.ok(userMessage.text.includes("Как оформить штамп по ГОСТ?"))
  })

  test("сообщение длиннее лимита отклоняется", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const response = await h.request("POST", `/api/sessions/${id}/messages`, {
      user: "fedor",
      body: { text: "x".repeat(h.config.limits.maxMessageChars + 1) },
    })
    assert.equal(response.status, 413)
  })

  test("слишком частое сообщение отклоняется (429)", async () => {
    const limited = await createHarness({
      limits: {
        maxMessageChars: 20_000,
        requestsPerMinute: 10_000,
        messagesPerMinute: 1,
        maxBodyBytes: 256 * 1024,
      },
    })
    try {
      const created = await limited.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
      const id = (await created.json()).data.id
      const first = await limited.request("POST", `/api/sessions/${id}/messages`, {
        user: "fedor",
        body: { text: "первое" },
      })
      assert.equal(first.status, 202)
      const second = await limited.request("POST", `/api/sessions/${id}/messages`, {
        user: "fedor",
        body: { text: "второе" },
      })
      assert.equal(second.status, 429)
    } finally {
      await limited.cleanup()
    }
  })

  // ----------------------------------------------------------------- model

  test("недопустимая модель отклоняется", async () => {
    const response = await h.request("POST", "/api/sessions", {
      user: "fedor",
      body: { agent: "ips-help", model: "opencode/not-allowed-model" },
    })
    assert.equal(response.status, 400)
    const body = await response.json()
    assert.equal(body.error.code, "model_not_allowed")
  })

  test("неизвестный агент отклоняется", async () => {
    const response = await h.request("POST", "/api/sessions", {
      user: "fedor",
      body: { agent: "not-an-agent" },
    })
    assert.equal(response.status, 400)
    assert.equal((await response.json()).error.code, "unknown_agent")
  })

  // ------------------------------------------------------- A vs B isolation

  test("A не видит сессии B: list, messages, events, delete, files", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "user-b", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id

    // list
    const list = await h.request("GET", "/api/sessions", { user: "user-a" })
    const ids = (await list.json()).data.map((session: { id: string }) => session.id)
    assert.ok(!ids.includes(id), "в списке A сессии B нет")

    // messages
    const messages = await h.request("GET", `/api/sessions/${id}/messages`, { user: "user-a" })
    assert.equal(messages.status, 404)

    // events
    const events = await h.request("GET", `/api/sessions/${id}/events`, { user: "user-a" })
    assert.equal(events.status, 404)

    // files
    const files = await h.request("GET", `/api/sessions/${id}/files`, { user: "user-a" })
    assert.equal(files.status, 404)

    const download = await h.request("GET", `/api/sessions/${id}/files/report.md`, { user: "user-a" })
    assert.equal(download.status, 404)

    // delete
    const remove = await h.request("DELETE", `/api/sessions/${id}`, { user: "user-a" })
    assert.equal(remove.status, 404)

    // the session still exists for its owner
    const stillThere = await h.request("GET", `/api/sessions/${id}/messages`, { user: "user-b" })
    assert.equal(stillThere.status, 200)
  })

  test("чужая сессия не отдаёт события: поток A закрыт сразу", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "user-b", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const response = await h.request("GET", `/api/sessions/${id}/events`, { user: "user-a" })
    assert.equal(response.status, 404)
    assert.equal(response.headers.get("content-type")?.includes("text/event-stream"), false)
  })

  // ------------------------------------------------------- forbidden routes

  test("запрещённые маршруты не пробрасываются", async () => {
    const forbidden = [
      "/api/project",
      "/api/project/prj_1",
      "/api/config",
      "/api/credential",
      "/api/integration",
      "/api/provider",
      "/api/mcp",
      "/api/mcp/resource",
      "/api/session",
      "/api/session/active",
      "/api/session/ses_mock0001/shell",
      "/api/session/ses_mock0001/command",
      "/api/session/ses_mock0001/inbox",
      "/api/fs/list",
      "/api/fs/find",
      "/api/fs/read/secret.txt",
      "/api/pty",
      "/api/pty/pty_1",
      "/api/plugin",
      "/api/worktree",
      "/api/vcs/status",
      "/api/command",
      "/api/skill",
      "/api/rpc/rpc_1/method",
      "/api/reference",
      "/api/shell",
      "/api/form",
      "/api/permission/request",
      "/api/permission/saved",
      "/api/websearch",
      "/api/debug/location",
      "/api/experimental/migration/v1",
      "/api/experimental/session/ses_mock0001/log",
      "/api/experimental/generate",
      "/api/model/default",
      "/api/agent/build",
    ]
    for (const path of forbidden) {
      const response = await h.request("GET", path, { user: "fedor" })
      assert.equal(response.status, 404, `${path} должен отвечать 404`)
    }

    // маршруты v1, которых нет в v2, тоже не пробрасываются
    for (const path of [
      "/project",
      "/project/current",
      "/config",
      "/config/providers",
      "/auth/anthropic",
      "/provider",
      "/mcp",
      "/instance/dispose",
      "/session",
      "/session/status",
      "/session/ses_mock0001/shell",
      "/file",
      "/file/content?path=secret.txt",
      "/find/file?query=*",
      "/tui/append-prompt",
      "/global/event",
    ]) {
      const response = await h.request("GET", path, { user: "fedor" })
      assert.equal(response.status, 404, `${path} должен отвечать 404`)
    }
  })

  test("портал вызывает только разрешённые маршруты OpenCode", async () => {
    for (const call of h.mock.state.calls) {
      assert.ok(
        ALLOWED_UPSTREAM.some((pattern) => pattern.test(call.path)),
        `недопустимый вызов OpenCode: ${call.method} ${call.path}`,
      )
    }
  })

  // ------------------------------------------------------------ permissions

  test("запрос разрешения получает автоматический отказ", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id

    // The owner listens on the session stream.
    const stream = await h.request("GET", `/api/sessions/${id}/events`, { user: "fedor" })
    assert.equal(stream.status, 200)

    h.mock.raisePermission(id, { action: "bash", resources: ["C:\\Windows\\System32\\cmd.exe"] })

    const rejected = await waitFor(() => h.mock.state.replies.length > 0)
    assert.ok(rejected, "портал ответил на запрос разрешения")
    const reply = h.mock.state.replies.at(-1)!
    assert.equal(reply.sessionID, id)
    assert.equal(reply.decision, "reject")
    assert.ok(reply.message && reply.message.length > 0, "в отказе есть понятное сообщение")

    // The user sees a clear notification instead of a permission dialog.
    const event = await readSse(stream, (e) => e.type === "portal.permission.rejected")
    assert.ok(event, "пользователь получил уведомление об отказе")
    assert.match(String(event!.data.message), /отклон/i)
  })

  test("запрос разрешения обрабатывается даже без активного слушателя", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const before = h.mock.state.replies.length
    h.mock.raisePermission(id)
    const handled = await waitFor(() => h.mock.state.replies.length > before)
    assert.ok(handled, "отказ отправлен без подключённого клиента")
  })

  // ----------------------------------------------------------------- files

  test("скачивание файла сессии работает и отдаётся как вложение", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const dir = join(h.config.output.root, id)
    mkdirSync(dir, { recursive: true })
    writeFileSync(join(dir, "report.md"), "# Отчёт\n", "utf8")

    const list = await h.request("GET", `/api/sessions/${id}/files`, { user: "fedor" })
    assert.equal(list.status, 200)
    const files = (await list.json()).data
    assert.equal(files.length, 1)
    assert.equal(files[0].name, "report.md")

    const download = await h.request("GET", `/api/sessions/${id}/files/report.md`, { user: "fedor" })
    assert.equal(download.status, 200)
    assert.match(download.headers.get("content-disposition") ?? "", /^attachment;/)
    assert.equal(await download.text(), "# Отчёт\n")
  })

  test("обход пути при скачивании отклоняется", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const dir = join(h.config.output.root, id)
    mkdirSync(dir, { recursive: true })
    // Файл вне папки сессии.
    writeFileSync(join(h.config.output.root, "secret.txt"), "TOP SECRET", "utf8")

    const attempts = [
      `${id}/files/..%2Fsecret.txt`,
      `${id}/files/..%2F..%2Fsecret.txt`,
      `${id}/files/%2e%2e%2fsecret.txt`,
      `${id}/files/%2e%2e%2f%2e%2e%2fsecret.txt`,
    ]
    for (const path of attempts) {
      const response = await h.request("GET", `/api/sessions/${path}`, { user: "fedor" })
      assert.ok(response.status === 400 || response.status === 404, `обход пути отклонён (${path})`)
      const text = await response.text()
      assert.ok(!text.includes("TOP SECRET"), "содержимое файла вне папки не утекает")
    }

    // Необработанный (не нормализованный клиентом) путь с "..".
    const raw = await h.rawRequest("GET", `/api/sessions/${id}/files/../secret.txt`, { user: "fedor" })
    assert.ok(raw.status === 400 || raw.status === 404, "необработанный обход пути отклонён")
    assert.ok(!raw.body.includes("TOP SECRET"), "содержимое файла вне папки не утекает")
  })

  test("абсолютный путь при скачивании отклоняется", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const response = await h.request("GET", `/api/sessions/${id}/files/C%3A%5CWindows%5Cwin.ini`, { user: "fedor" })
    assert.ok(response.status === 400 || response.status === 404)
  })

  test("недопустимое расширение отклоняется", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const dir = join(h.config.output.root, id)
    mkdirSync(dir, { recursive: true })
    writeFileSync(join(dir, "payload.exe"), "MZ", "utf8")
    writeFileSync(join(dir, "secret.env"), "TOKEN=1", "utf8")

    for (const name of ["payload.exe", "secret.env"]) {
      const response = await h.request("GET", `/api/sessions/${id}/files/${name}`, { user: "fedor" })
      assert.equal(response.status, 400, `${name} должен быть отклонён`)
      const body = await response.json()
      assert.equal(body.error.code, "extension")
    }

    // Файл с запрещённым расширением не попадает и в список.
    const list = await h.request("GET", `/api/sessions/${id}/files`, { user: "fedor" })
    const names = (await list.json()).data.map((file: { name: string }) => file.name)
    assert.deepEqual(names, [])
  })

  test("симлинк наружу отклоняется", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const dir = join(h.config.output.root, id)
    mkdirSync(dir, { recursive: true })
    const outside = join(h.config.output.root, "outside.md")
    writeFileSync(outside, "outside", "utf8")
    try {
      symlinkSync(outside, join(dir, "link.md"))
    } catch {
      return // симлинки недоступны в этой системе - тест пропускается
    }
    const response = await h.request("GET", `/api/sessions/${id}/files/link.md`, { user: "fedor" })
    assert.ok(response.status === 400 || response.status === 404)
    assert.ok(!(await response.text()).includes("outside"))
  })

  test("файл больше лимита не отдаётся", async () => {
    const small = await createHarness({
      files: { allowedExtensions: ["md", "txt", "csv"], maxBytes: 10 },
    })
    try {
      const created = await small.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
      const id = (await created.json()).data.id
      const dir = join(small.config.output.root, id)
      mkdirSync(dir, { recursive: true })
      writeFileSync(join(dir, "big.txt"), "x".repeat(100), "utf8")
      const response = await small.request("GET", `/api/sessions/${id}/files/big.txt`, { user: "fedor" })
      assert.equal(response.status, 413)
    } finally {
      await small.cleanup()
    }
  })

  // --------------------------------------------------------------- delete

  test("DELETE /api/sessions удаляет сессию и её файлы", async () => {
    const created = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const id = (await created.json()).data.id
    const dir = join(h.config.output.root, id)
    mkdirSync(dir, { recursive: true })
    writeFileSync(join(dir, "report.md"), "x", "utf8")

    const response = await h.request("DELETE", `/api/sessions/${id}`, { user: "fedor" })
    assert.equal(response.status, 200)
    assert.equal(existsSync(dir), false, "папка сессии удалена")
    assert.equal(h.mock.state.sessions.has(id), false, "сессия удалена в OpenCode")

    const list = await h.request("GET", "/api/sessions", { user: "fedor" })
    const ids = (await list.json()).data.map((session: { id: string }) => session.id)
    assert.ok(!ids.includes(id))
  })

  // ---------------------------------------------------------------- events

  test("SSE отдаёт только события своей сессии", async () => {
    const createdA = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-help" } })
    const idA = (await createdA.json()).data.id
    const createdB = await h.request("POST", "/api/sessions", { user: "fedor", body: { agent: "ips-designer" } })
    const idB = (await createdB.json()).data.id

    const streamA = await h.request("GET", `/api/sessions/${idA}/events`, { user: "fedor" })
    const reader = streamA.body!.getReader()
    const decoder = new TextDecoder()
    let buffer = ""

    const readUntil = async (predicate: (event: { type: string; data: Record<string, unknown> }) => boolean) => {
      const deadline = Date.now() + 4000
      while (Date.now() < deadline) {
        const { value, done } = await reader.read()
        if (done) break
        buffer += decoder.decode(value, { stream: true })
        let index: number
        while ((index = buffer.indexOf("\n\n")) !== -1) {
          const frame = buffer.slice(0, index)
          buffer = buffer.slice(index + 2)
          for (const line of frame.split("\n")) {
            if (!line.startsWith("data:")) continue
            try {
              const event = JSON.parse(line.slice(5).trim())
              if (predicate(event)) return event
            } catch {
              /* ignore */
            }
          }
        }
      }
      return null
    }

    // Событие сессии B не должно попасть в поток A.
    h.mock.emit("session.text.delta", {
      sessionID: idB,
      assistantMessageID: "msg_a1",
      ordinal: 0,
      delta: "секрет B",
    })
    h.mock.emit("session.text.delta", {
      sessionID: idA,
      assistantMessageID: "msg_a1",
      ordinal: 0,
      delta: "привет A",
    })

    const event = await readUntil((e) => e.type === "session.text.delta")
    assert.ok(event, "получено событие")
    assert.equal(event!.data.sessionID, idA)
    assert.ok(String(event!.data.delta).includes("привет A"))
    await reader.cancel().catch(() => {})
  })

  // ------------------------------------------------------------- frontend

  test("фронтенд раздаётся и содержит три плитки режимов", async () => {
    const index = await h.request("GET", "/", { user: null })
    assert.equal(index.status, 200)
    const html = await index.text()
    assert.ok(html.includes('id="modes"'))

    const css = await h.request("GET", "/styles.css", { user: null })
    assert.equal(css.status, 200)
    const cssText = await css.text()
    assert.ok(cssText.includes("--color-accent: #c62828"), "красный акцент вынесен в переменную")
    assert.ok(cssText.includes(":root"), "цвета собраны в одном файле переменных")
  })
})
