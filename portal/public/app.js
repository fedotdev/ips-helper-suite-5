/*
 * IPS Portal - frontend.
 * Vanilla ES modules, no build step.
 *
 * The portal API is the only backend: the browser never talks to OpenCode.
 */

const USER_KEY = "ips-portal.user"

const state = {
  user: localStorage.getItem(USER_KEY) || "",
  modes: [],
  sessions: [],
  current: null, // { id, agent, title }
  messages: [],
  live: new Map(), // assistantMessageID -> { text, tools: Map }
  stream: null, // { abort }
  files: [],
  filesTimer: null,
  refreshTimer: null,
}

/* ------------------------------------------------------------------ api */

async function api(path, options = {}) {
  const headers = { "X-User": state.user, ...(options.headers || {}) }
  if (options.body !== undefined) headers["Content-Type"] = "application/json"
  const response = await fetch(path, {
    method: options.method || "GET",
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  })
  const text = await response.text()
  let payload = null
  if (text) {
    try {
      payload = JSON.parse(text)
    } catch {
      payload = { error: { code: "bad_response", message: text.slice(0, 200) } }
    }
  }
  if (!response.ok) {
    const error = new Error(payload?.error?.message || `HTTP ${response.status}`)
    error.code = payload?.error?.code || String(response.status)
    error.status = response.status
    throw error
  }
  return payload
}

/* --------------------------------------------------------------- screens */

function show(id) {
  for (const screen of document.querySelectorAll(".screen")) screen.classList.add("hidden")
  document.getElementById(id).classList.remove("hidden")
}

function setChatError(message) {
  const element = document.getElementById("chat-error")
  if (!message) {
    element.classList.add("hidden")
    element.textContent = ""
    return
  }
  element.textContent = message
  element.classList.remove("hidden")
}

/* ---------------------------------------------------------------- login */

document.getElementById("login-form").addEventListener("submit", async (event) => {
  event.preventDefault()
  const value = document.getElementById("login-input").value.trim()
  const error = document.getElementById("login-error")
  if (!value) return
  state.user = value
  try {
    await api("/api/agents")
    localStorage.setItem(USER_KEY, value)
    error.classList.add("hidden")
    await enterHome()
  } catch (err) {
    state.user = localStorage.getItem(USER_KEY) || ""
    error.textContent = err.message
    error.classList.remove("hidden")
  }
})

document.getElementById("logout-button").addEventListener("click", () => {
  localStorage.removeItem(USER_KEY)
  state.user = ""
  state.sessions = []
  stopStream()
  show("login-screen")
})

/* ----------------------------------------------------------------- home */

async function enterHome() {
  show("home-screen")
  document.getElementById("current-user").textContent = state.user
  await Promise.all([loadModes(), loadSessions()])
}

async function loadModes() {
  const container = document.getElementById("modes")
  container.innerHTML = ""
  try {
    const payload = await api("/api/agents")
    state.modes = payload.data || []
  } catch (err) {
    container.innerHTML = `<div class="msg error">${escapeHtml(err.message)}</div>`
    return
  }
  if (state.modes.length === 0) {
    container.innerHTML = `<div class="msg system">Сервер не вернул ни одного режима.</div>`
    return
  }
  for (const mode of state.modes) {
    const tile = document.createElement("button")
    tile.className = "tile"
    tile.type = "button"
    tile.innerHTML = `<h3>${escapeHtml(mode.name || mode.id)}</h3><p>${escapeHtml(mode.description || "")}</p>`
    tile.addEventListener("click", () => createSession(mode.id))
    container.appendChild(tile)
  }
}

async function loadSessions() {
  const container = document.getElementById("sessions")
  try {
    const payload = await api("/api/sessions")
    state.sessions = payload.data || []
  } catch (err) {
    container.innerHTML = `<div class="msg error">${escapeHtml(err.message)}</div>`
    return
  }
  container.innerHTML = ""
  if (state.sessions.length === 0) {
    container.innerHTML = `<div class="msg system">Пока нет сессий. Выберите режим выше.</div>`
    return
  }
  for (const session of state.sessions) {
    const row = document.createElement("div")
    row.className = "session-row"

    const main = document.createElement("button")
    main.className = "session-main"
    main.type = "button"
    main.innerHTML = `<span class="session-title">${escapeHtml(session.title || session.id)}</span>
      <span class="session-meta">${escapeHtml(session.id)} · ${formatTime(session.updatedAt)} · ${escapeHtml(session.agent)}</span>`
    main.addEventListener("click", () => openSession(session))

    const badge = document.createElement("span")
    badge.className = "badge"
    badge.textContent = session.agent

    const remove = document.createElement("button")
    remove.className = "ghost danger"
    remove.type = "button"
    remove.textContent = "Удалить"
    remove.addEventListener("click", () => deleteSession(session))

    row.append(main, badge, remove)
    container.appendChild(row)
  }
}

async function createSession(agentId) {
  try {
    const payload = await api("/api/sessions", { method: "POST", body: { agent: agentId } })
    await loadSessions()
    openSession(payload.data)
  } catch (err) {
    alert(err.message)
  }
}

async function deleteSession(session) {
  if (!confirm(`Удалить сессию «${session.title || session.id}» и её файлы?`)) return
  try {
    await api(`/api/sessions/${encodeURIComponent(session.id)}`, { method: "DELETE" })
    if (state.current?.id === session.id) {
      stopStream()
      show("home-screen")
    }
    await loadSessions()
  } catch (err) {
    alert(err.message)
  }
}

/* ----------------------------------------------------------------- chat */

async function openSession(session) {
  state.current = session
  state.messages = []
  state.live = new Map()
  show("chat-screen")
  document.getElementById("chat-title-text").textContent = session.title || session.id
  document.getElementById("chat-agent").textContent = session.agent
  setChatError(null)
  await refreshMessages()
  await refreshFiles()
  startStream(session.id)
  state.filesTimer = setInterval(() => void refreshFiles(), 5000)
}

function stopStream() {
  if (state.stream) {
    state.stream.abort()
    state.stream = null
  }
  if (state.filesTimer) {
    clearInterval(state.filesTimer)
    state.filesTimer = null
  }
  if (state.refreshTimer) {
    clearTimeout(state.refreshTimer)
    state.refreshTimer = null
  }
}

document.getElementById("back-button").addEventListener("click", async () => {
  stopStream()
  await enterHome()
})

document.getElementById("delete-button").addEventListener("click", async () => {
  if (!state.current) return
  await deleteSession(state.current)
})

async function refreshMessages() {
  if (!state.current) return
  try {
    const payload = await api(`/api/sessions/${encodeURIComponent(state.current.id)}/messages`)
    state.messages = payload.data || []
    renderMessages()
  } catch (err) {
    setChatError(err.message)
  }
}

function scheduleRefresh() {
  if (state.refreshTimer) clearTimeout(state.refreshTimer)
  state.refreshTimer = setTimeout(() => {
    state.refreshTimer = null
    void refreshMessages()
    void refreshFiles()
  }, 400)
}

async function refreshFiles() {
  if (!state.current) return
  try {
    const payload = await api(`/api/sessions/${encodeURIComponent(state.current.id)}/files`)
    state.files = payload.data || []
    renderMessages()
  } catch {
    /* the file list is auxiliary - never block the chat on it */
  }
}

/* ------------------------------------------------------------ rendering */

function renderMessages() {
  const container = document.getElementById("messages")
  const atBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 120
  container.innerHTML = ""

  let lastAssistant = null
  for (const message of state.messages) {
    if (message.type === "user") {
      container.appendChild(bubble("user", message.text || ""))
      continue
    }
    if (message.type === "assistant") {
      lastAssistant = renderAssistant(message)
      container.appendChild(lastAssistant)
      continue
    }
    if (message.type === "system" || message.type === "synthetic") {
      const node = bubble("system", message.text || message.description || "")
      container.appendChild(node)
      continue
    }
    if (message.type === "shell") {
      const node = bubble("system", `$ ${message.command || ""}`)
      container.appendChild(node)
      continue
    }
  }

  // Portal notifications (e.g. an automatically rejected permission request).
  for (const notice of state.notices || []) {
    container.appendChild(bubble("error", notice))
  }

  // Links to the session files under the last answer.
  if (lastAssistant && state.files.length > 0) {
    lastAssistant.appendChild(fileList("files inline"))
  }

  const filesNode = document.getElementById("files")
  filesNode.innerHTML = ""
  for (const file of state.files) {
    const item = document.createElement("li")
    const link = document.createElement("a")
    link.href = `/api/sessions/${encodeURIComponent(state.current.id)}/files/${encodeURIComponent(file.name)}`
    link.textContent = file.name
    const size = document.createElement("span")
    size.className = "file-size"
    size.textContent = formatSize(file.size)
    link.appendChild(size)
    item.appendChild(link)
    filesNode.appendChild(item)
  }

  if (atBottom) container.scrollTop = container.scrollHeight
}

/** Build a list of download links for the files of the current session. */
function fileList(className) {
  const list = document.createElement("ul")
  list.className = `files ${className}`
  for (const file of state.files) {
    const item = document.createElement("li")
    const link = document.createElement("a")
    link.href = `/api/sessions/${encodeURIComponent(state.current.id)}/files/${encodeURIComponent(file.name)}`
    link.textContent = file.name
    const size = document.createElement("span")
    size.className = "file-size"
    size.textContent = formatSize(file.size)
    link.appendChild(size)
    item.appendChild(link)
    list.appendChild(item)
  }
  return list
}

function bubble(kind, text) {
  const node = document.createElement("div")
  node.className = `msg ${kind}`
  node.textContent = text
  return node
}

function renderAssistant(message) {
  const node = document.createElement("div")
  node.className = "msg assistant"
  const live = state.live.get(message.id)

  const projectionText = (message.content || [])
    .filter((part) => part.type === "text")
    .map((part) => part.text || "")
    .join("")

  let text = projectionText
  if (!text && live?.text) text = live.text
  if (text) {
    const block = document.createElement("div")
    block.textContent = text
    if (live?.text && live.text !== projectionText) block.classList.add("caret")
    node.appendChild(block)
  }

  for (const part of message.content || []) {
    if (part.type === "reasoning" && part.text) {
      node.appendChild(collapsible("Размышления", part.text, "reasoning"))
    }
    if (part.type === "tool") {
      node.appendChild(toolLine(part))
    }
  }

  // Live tool calls that are not part of the projection yet.
  const projected = new Set((message.content || []).filter((p) => p.type === "tool").map((p) => p.id))
  if (live?.tools) {
    for (const [id, tool] of live.tools) {
      if (projected.has(id)) continue
      node.appendChild(toolLine({ id, name: tool.name, state: { status: tool.status } }))
    }
  }

  if (!node.childNodes.length) node.appendChild(bubble("system", "…"))
  return node
}

function collapsible(title, body, className = "") {
  const details = document.createElement("details")
  const summary = document.createElement("summary")
  summary.className = className
  summary.textContent = title
  const pre = document.createElement("pre")
  pre.textContent = body
  details.append(summary, pre)
  return details
}

function toolLine(part) {
  const status = part.state?.status || "running"
  const details = document.createElement("details")
  const summary = document.createElement("summary")
  summary.className = "tool-line"
  summary.innerHTML = `<span>⚙</span><span class="tool-name">${escapeHtml(part.name || "tool")}</span>
    <span class="tool-status ${escapeHtml(status)}">${escapeHtml(statusLabel(status))}</span>`
  details.appendChild(summary)
  const input = part.state?.input
  if (input && Object.keys(input).length) {
    const pre = document.createElement("pre")
    pre.textContent = JSON.stringify(input, null, 2).slice(0, 4000)
    details.appendChild(pre)
  }
  return details
}

function statusLabel(status) {
  switch (status) {
    case "completed":
      return "готово"
    case "error":
      return "ошибка"
    case "running":
      return "выполняется"
    default:
      return status
  }
}

/* ----------------------------------------------------------------- SSE */

async function startStream(sessionId) {
  stopStream()
  const controller = new AbortController()
  state.stream = controller
  try {
    const response = await fetch(`/api/sessions/${encodeURIComponent(sessionId)}/events`, {
      headers: { "X-User": state.user, Accept: "text/event-stream" },
      signal: controller.signal,
    })
    if (!response.ok || !response.body) throw new Error(`SSE failed: ${response.status}`)
    const reader = response.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ""
    for (;;) {
      const { value, done } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })
      let index
      while ((index = buffer.indexOf("\n\n")) !== -1) {
        const frame = buffer.slice(0, index)
        buffer = buffer.slice(index + 2)
        for (const line of frame.split("\n")) {
          if (!line.startsWith("data:")) continue
          try {
            handleEvent(JSON.parse(line.slice(5).trim()))
          } catch {
            /* ignore malformed frames */
          }
        }
      }
    }
  } catch (err) {
    if (err.name !== "AbortError") setChatError(`Поток событий прерван: ${err.message}`)
  }
}

function handleEvent(event) {
  if (!event || typeof event.type !== "string") return
  const data = event.data || {}
  state.notices = state.notices || []

  switch (event.type) {
    case "portal.permission.rejected": {
      const notice = `Запрос на разрешение «${data.action || "операция"}» отклонён автоматически. Портал работает без интерактивных подтверждений — попросите агента обойтись без дополнительных прав.`
      if (!state.notices.includes(notice)) state.notices.push(notice)
      renderMessages()
      return
    }
    case "session.text.started": {
      const id = data.assistantMessageID
      if (!state.live.has(id)) state.live.set(id, { text: "", tools: new Map() })
      renderMessages()
      return
    }
    case "session.text.delta": {
      const id = data.assistantMessageID
      const entry = state.live.get(id) || { text: "", tools: new Map() }
      entry.text += data.delta || ""
      state.live.set(id, entry)
      renderMessages()
      return
    }
    case "session.text.ended": {
      const id = data.assistantMessageID
      const entry = state.live.get(id) || { text: "", tools: new Map() }
      entry.text = data.text || entry.text
      state.live.set(id, entry)
      renderMessages()
      return
    }
    case "session.tool.input.started": {
      const id = data.assistantMessageID
      const entry = state.live.get(id) || { text: "", tools: new Map() }
      entry.tools.set(data.id, { name: data.name || "tool", status: "running" })
      state.live.set(id, entry)
      renderMessages()
      return
    }
    case "session.tool.success": {
      const id = data.assistantMessageID
      const entry = state.live.get(id)
      const tool = entry?.tools.get(data.id)
      if (tool) tool.status = "completed"
      renderMessages()
      return
    }
    case "session.tool.failed": {
      const id = data.assistantMessageID
      const entry = state.live.get(id)
      const tool = entry?.tools.get(data.id)
      if (tool) tool.status = "error"
      renderMessages()
      return
    }
    case "session.execution.failed": {
      const notice = `Выполнение прервано: ${data.error?.message || "неизвестная ошибка"}`
      if (!state.notices.includes(notice)) state.notices.push(notice)
      renderMessages()
      scheduleRefresh()
      return
    }
    case "session.step.ended":
    case "session.execution.succeeded":
    case "session.execution.interrupted": {
      scheduleRefresh()
      return
    }
    case "session.message.content.updated": {
      scheduleRefresh()
      return
    }
    default:
      return
  }
}

/* ------------------------------------------------------------- composer */

const composer = document.getElementById("composer")
const composerInput = document.getElementById("composer-input")

composer.addEventListener("submit", async (event) => {
  event.preventDefault()
  if (!state.current) return
  const text = composerInput.value.trim()
  if (!text) return
  composerInput.value = ""
  setChatError(null)
  const button = document.getElementById("send-button")
  button.disabled = true
  try {
    await api(`/api/sessions/${encodeURIComponent(state.current.id)}/messages`, {
      method: "POST",
      body: { text },
    })
    await refreshMessages()
  } catch (err) {
    setChatError(err.message)
  } finally {
    button.disabled = false
    composerInput.focus()
  }
})

composerInput.addEventListener("keydown", (event) => {
  if (event.key === "Enter" && !event.shiftKey) {
    event.preventDefault()
    composer.requestSubmit()
  }
})

/* --------------------------------------------------------------- helpers */

function escapeHtml(value) {
  return String(value ?? "").replace(/[&<>"']/g, (char) => {
    switch (char) {
      case "&":
        return "&amp;"
      case "<":
        return "&lt;"
      case ">":
        return "&gt;"
      case '"':
        return "&quot;"
      case "'":
        return "&#39;"
      default:
        return char
    }
  })
}

function formatTime(value) {
  if (!value) return ""
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ""
  return date.toLocaleString("ru-RU", { dateStyle: "short", timeStyle: "short" })
}

function formatSize(bytes) {
  if (!Number.isFinite(bytes)) return ""
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

/* ----------------------------------------------------------------- boot */

if (state.user) {
  api("/api/agents")
    .then(() => enterHome())
    .catch(() => {
      state.user = ""
      localStorage.removeItem(USER_KEY)
      show("login-screen")
    })
} else {
  show("login-screen")
}
