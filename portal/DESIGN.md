# DESIGN — IPS Portal V2

Веб-портал для коллег поверх **одного общего экземпляра `opencode serve`**.
Браузер ходит только в портал; портал — единственный, кто ходит в OpenCode.

---

## 1. Что проверено в окружении (не выдумано)

Всё ниже снято с живой машины, а не взято из документации вслепую.

| Что | Значение | Как проверено |
|---|---|---|
| OpenCode | **v2.1.2** | `opencode --version` |
| Node.js | **v26.5.0** (есть и Bun 1.4.0) | `node --version` |
| Фоновый сервер OpenCode | уже запущен: `opencode serve --service`, PID 19544 | `Get-CimInstance Win32_Process` |
| Адрес сервера | `http://127.0.0.1:45278` (порт **случайный**, меняется при перезапуске) | `opencode service status` |
| Спецификация API | `GET /openapi.json` на живом сервере (OpenAPI 3.1, 144 операции) | скачана и разобрана |
| Документация | `https://opencode.ai/v2/docs/api` (v2) и `https://opencode.ai/docs/server/`, `/docs/sdk/` (v1) | прочитаны |
| Модель | `opencode/mimo-v2.6-flash-free` → `providerID=opencode`, `modelID=mimo-v2.6-flash-free`, имя «MiMo‑V2.6‑Flash Free», `enabled=true`, контекст 200 000 | `GET /api/model` |
| Проект | `D:\parsing\IPS-Durdom-5\ips-helper-suite-5` | репозиторий |
| Агенты проекта | `build`, `plan` (`primary`, не скрыты), `general`, `explore` (`subagent`), `compaction`/`title`/`summary` (`primary`, скрыты) | `GET /api/agent?location[directory]=<проект>` |

### 1.1. Расхождения ТЗ и фактического окружения (важно)

| В ТЗ | Факт | Решение в портале |
|---|---|---|
| Путь `C:\ips-helper-suite-5` | такого каталога нет; репозиторий лежит в `D:\parsing\IPS-Durdom-5\ips-helper-suite-5` | портал написан в `<репозиторий>\portal`; в ТЗ путь указан как `C:\ips-helper-suite-5` |
| Агенты V1 `ips-help`, `ips-designer`, `ips-technologist` | таких агентов **нет** ни в `opencode.json`, ни в `.opencode/agent/`, ни в репозитории. Есть только *скиллы* `skills/ips-system-helper`, `skills/ips-programmer`, `skills/ips-technologist` | портал берёт режимы **из списка агентов OpenCode** (как и требует ТЗ: «режимы из списка агентов OpenCode с описаниями»). Идентификаторы можно зафиксировать в `config.json → agents.allow`, когда агенты появятся |
| Модель «точ��ый id возьми из opencode.json V1» | в `opencode.json` V1 модели нет (в нём только MCP) | id взят из живого сервера: `opencode/mimo-v2.6-flash-free` |
| Маршруты `/session`, `/config`, `/file`, `/find`, `/tui/*`, `/instance/dispose` | это **API v1**. Сервер v2.1.2 их не отдаёт вообще: живой `GET /session` возвращает HTML-заглушку SPA, а не JSON | запрещённый список пересчитан на v2 (см. §6) |
| «в поле `system`» в теле сообщения | в **v2 нет поля `system`**. Тело `POST /api/session/{id}/prompt` — строго `{id?, text, files?, agents?, skills?, metadata?, delivery?, resume?}` с `additionalProperties:false` | используется штатный механизм v2 — per-session instruction entry (§8.2). Есть запасной режим `systemRule.mode = "prompt"` |

> Вывод: бриф написан под **v1**, а на машине стоит **v2.1.2**. Портал реализован под фактический API v2.1.2, а соответствие «v1-имена → v2-маршруты» сведено в таблице (§6.2).

---

## 2. Архитектура

```
                браузер (vanilla JS, без сборки)
                          │  HTTP  + заголовок X-User
                          ▼
        ┌──────────────────────────────────────────┐
        │            PORTAL (Node.js, TypeScript)  │
        │                                          │
        │  http.ts    маршрутизация + лимиты       │
        │  auth.ts    X-User  ──► точка замены      │
        │                на AD/SSO                 │
        │  sessions.ts бизнес-логика и владение     │
        │  opencode.ts  клиент OpenCode v2 (fetch)  │
        │  events.ts  один SSE → разветвление      │
        │  permissions.ts авто-отказ                │
        │  files.ts   безопасная выдача с диска      │
        │  ratelimit.ts  token bucket на пользователя│
        │  logger.ts  JSONL без текста сообщений    │
        │  db.ts      SQLite (node:sqlite)          │
        └───────┬──────────────────────────┬───────┘
                │ Basic auth + /api/*      │ чтение/удаление
                ▼                          ▼
   opencode serve --service        output\<session id>\
   127.0.0.1:<случайный порт>      (файлы результатов)
```

**Почему один входной URL.** Браузер не знает ни адреса, ни пароля OpenCode.
Пароль сервера живёт только в окружении портала.

**Почему один SSE.** `/api/event` — глобальный поток всех локаций. Портал открывает
**одно** соединение и раздаёт события подписчикам только своей сессии
(фильтр по `data.sessionID`).

---

## 3. Стек

**Node.js + TypeScript, без единой npm-зависимости.**

| Что | Чем | Почему |
|---|---|---|
| HTTP-сервер | `node:http` | встроенный |
| SQLite | `node:sqlite` (`DatabaseSync`) | встроенный, Node ≥ 22.5; проверено на Node 26.5.0 |
| Тесты | `node:test` | встроенный |
| HTTP-клиент | глобальный `fetch` | встроенный |
| TypeScript | нативное удаление типов Node 22.6+ | `node src/index.ts` — без сборки и без `tsc` |

**Почему Node, а не Bun.** Документация SDK (`opencode.ai/docs/sdk/`, `v2/docs/build/client`)
описывает обычный JS/TS-клиент; Bun даёт то же самое, но в Bun пришлось бы
заводить `bun.lock` и ставить пакеты, а Node даёт всё нужное из коробки.
Bun остаётся запасным вариантом: код не использует ничего, чего нет в Bun
(`node:sqlite` → `bun:sqlite`, `node:test` → `bun:test`), но проверялся на Node.

**Почему не `@opencode/client`.** Официальный сгенерированный клиент v2
(`@opencode/client`) требует установки пакета, а его типы в npm-версии 1.19.3
**ещё не совпадают** с живым сервером 2.1.2 (`body: {prompt: PromptInput}` против
живого плоского `{text, files, agents, skills, metadata, delivery, resume}`).
Поэтому в портале свой тонкий клиент (`src/opencode.ts`, ~330 строк), сверенный
с `GET /openapi.json` живого сервера. Замена на `@opencode/client` — точечная
правка одного файла, когда выйдет совпадающая версия.

---

## 4. Данные портала

SQLite в `portal\data\portal.db` (`node:sqlite`, WAL).

```sql
CREATE TABLE users (
  id           TEXT PRIMARY KEY,   -- нормализованный логин (lower case)
  created_at   INTEGER NOT NULL,
  last_seen_at INTEGER NOT NULL
);

CREATE TABLE sessions (
  session_id TEXT PRIMARY KEY,     -- id сессии OpenCode
  user       TEXT NOT NULL,        -- владелец
  agent      TEXT NOT NULL,
  created_at INTEGER NOT NULL,
  FOREIGN KEY (user) REFERENCES users(id) ON DELETE CASCADE
);
CREATE INDEX idx_sessions_user ON sessions(user);
```

**История сессий в портале не хранится.** Переписка живёт в OpenCode
(`GET /api/session/{id}/message`), портал держит только соответствие
«пользователь → id сессии» и метаданные агента.

---

## 5. API портала (белый список)

Ровно девять маршрутов. Всё остальное — 404 (см. §6).

| Маршрут портала | Что делает | Маршрут OpenCode v2 |
|---|---|---|
| `GET /api/agents` | режимы (агенты OpenCode) с описаниями | `GET /api/agent?location[directory]=<проект>` |
| `POST /api/sessions` `{agent, model?, title?}` | создать сессию, записать владельца, создать папку вывода, отдать правило модели | `POST /api/session` → `PUT /api/experimental/session/{id}/instructions/entries/{key}` |
| `GET /api/sessions` | только мои сессии | `GET /api/session?directory=<проект>` (сводка с локальной БД) |
| `GET /api/sessions/:id/messages` | история переписки | `GET /api/session/{id}/message?limit=500&order=asc` |
| `POST /api/sessions/:id/messages` `{text, agent?}` | отправить сообщение выбранному агенту | `POST /api/session/{id}/prompt` (+ `POST /api/session/{id}/agent` при смене агента) |
| `GET /api/sessions/:id/events` | SSE только этой сессии | `GET /api/event` (глобальный) → фильтр по `data.sessionID` |
| `GET /api/sessions/:id/files` | список файлов результата | файловая система, `output\<id>\` |
| `GET /api/sessions/:id/files/:name` | скачивание с диска | файловая система (НЕ `GET /api/fs/read/*`) |
| `DELETE /api/sessions/:id` | удалить сессию и её файлы | `DELETE /api/session/{id}` (204) |

Дополнительно, вне белого списка API:

* `GET /`, `/index.html`, `/app.js`, `/styles.css` — статика фронтенда;
* `GET /api/agents` и прочие требуют заголовка `X-User`, иначе **401**.

### 5.1. Коды ответов

| Код | Когда |
|---|---|
| 400 | неизвестный агент, недопустимая модель, пустое сообщение, плохое имя/расширение файла |
| 401 | нет или невалиден `X-User` |
| 404 | **чужая или несуществующая сессия**; неизвестный маршрут; файл не найден |
| 405 | известный путь, но неподдерживаемый метод (заголовок `Allow`) |
| 413 | тело запроса, сообщение или файл больше лимита |
| 429 | превышен лимит частоты (`Retry-After`) |

> Про чужие сессии: **неизвестная сессия и чужая сессия дают одинаковый 404**,
> поэтому по id нельзя проверить, существует ли чужая сессия.

---

## 6. Что не пробрасывается

### 6.1. Реализация

Портал **не является прокси**. У него нет ни одного «универсального» маршрута:
обработчик `route()` в `src/server.ts` перечисляет девять путей, всё остальное
заканчивается `404 not_found`. Никакой строки запроса, метода или тела
пользователя не пересылается в OpenCode «как есть».

### 6.2. Соответствие запрещённого списка ТЗ (имена v1 → маршруты v2)

| В ТЗ (v1) | В v2.1.2 | Статус |
|---|---|---|
| `/project`, `/project/current` | `/api/project`, `/api/project/{projectID}` | заблокирован |
| `/config`, `/config/providers` | `/api/config`, `/api/model`, `/api/model/default` | заблокирован |
| `/auth` | `/api/credential`, `/api/integration`, `/auth/connect/{code}` | заблокирован |
| `/provider` | `/api/provider`, `/api/provider/{providerID}` | заблокирован |
| `/mcp` | `/api/mcp`, `/api/mcp/resource` | заблокирован |
| `/instance/dispose` | в v2 маршрута нет вообще | заблокирован (не существует) |
| `/session`, `/session/status` | `/api/session`, `/api/session/active` | заблокирован (нужны `/api/sessions*`) |
| `/session/:id/shell` | `/api/session/{id}/shell`, `/api/shell*` | заблокирован |
| `/file`, `/file/content`, `/file/status` | `/api/fs/list`, `/api/fs/read/*` | заблокирован |
| `/find`, `/find/file`, `/find/symbol` | `/api/fs/find` | заблокирован |
| `/tui/*` | `/api/tui`-эквивалента в v2 нет (управление TUI идёт через события) | заблокирован |

Дополнительно заблокированы (в v2 они есть, но порталу не нужны):
`/api/project`, `/api/credential`, `/api/integration`, `/api/plugin`, `/api/worktree`,
`/api/vcs`, `/api/pty`, `/api/persistent-pty`, `/api/shell`, `/api/command`, `/api/skill`,
`/api/rpc/*`, `/api/reference`, `/api/form`, `/api/permission/request`,
`/api/permission/saved`, `/api/websearch`, `/api/debug/location`, `/api/experimental/*`.

Два исключения, которые **разрешены** осознанно:

* `GET /api/event` — единственный источник событий (SSE);
* `PUT /api/experimental/session/{id}/instructions/entries/{key}` — доставка
  правила «писать только в output\<id>» (§8.2).

### 6.3. Автопроверка

Тест `портал вызывает только разрешённые маршруты OpenCode` смотрит **все**
фактические вызовы, сделанные порталом к mock-серверу, и сверяет их с
whitelist-регексом. Регрессия (новый вызов OpenCode) валит тест.

---

## 7. Владение сессиями

1. `POST /api/sessions` → `POST /api/session` в OpenCode → id сессии пишется в
   `sessions` вместе с владельцем (`users.id`).
2. Любой запрос к `/api/sessions/:id/...` сначала проверяет владельца:
   `row = store.getSession(id); if (!row || row.user !== user) → 404`.
   Проверка выполняется **до** любых обращений к OpenCode и к диску.
3. `GET /api/sessions` отдаёт только строки с `user = текущий`.
4. `DELETE /api/sessions/:id` удаляет сессию в OpenCode, папку `output\<id>\`
   и строку в БД.

Логин нормализуется в lower case, поэтому `Fedor` и `fedor` — один пользователь.

---

## 8. Правила и модель

### 8.1. Модель

`config.json → allowed_models` — единственный источник правды.
Тело `POST /api/sessions` может содержать `model`; если он есть, он обязан
**дословно** совпадать с элементом `allowed_models` (формат `provider/model`),
иначе — **400 `model_not_allowed`**. Если `model` не задан, берётся
`default_model` (он тоже обязан быть в `allowed_models` — проверяется при старте).

Сверка идёт по живому списку `GET /api/model`: строка `provider/model`
сопоставляется с `providerID + "/" + modelID`. Так «opencode/mimo-v2.6-flash-free»
не перепутается с «openrouter/xiaomi/mimo-v2.6-flash» (у второго в modelID
есть слеш).

Модель подставляется при создании сессии: `model = {id, providerID}` в теле
`POST /api/session`. Дополнительно портал может переключить модель
(`POST /api/session/{id}/model`) — сейчас не используется, список остаётся
единственным источником.

### 8.2. Правило «все файлы — только в output\<id>»

В **v2 нет поля `system`** (это подтверждено и OpenAPI-схемой живого сервера,
и исходником `packages/server/src/handlers/session.ts`: обработчик `session.prompt`
принимает ровно `id, text, files, agents, skills, metadata, delivery, resume`).

Штатный эквивалент в v2 — **instruction entry**: долговременная инструкция,
привязанная к сессии, которая объявляется модели на ближайшей границе шага.
Модель видит её как

```
<context key="portal-output">
Каталог результатов этой сессии: D:\...\output\ses_xxx
Правила работы с файлами для сессии ses_xxx:
- Все файлы результата писать ТОЛЬКО в каталог D:\...\output\ses_xxx (output\ses_xxx\).
- Никакие другие файлы не создавать и не изменять.
- Ответ в чат обязательно продублировать текстом: пользователь видит только текст ответа, файлы он скачивает отдельно.
- В ответе указать имена созданных файлов.
</context>
```

Отправляется при создании сессии:
`PUT /api/experimental/session/{sessionID}/instructions/entries/portal-output`
с телом `{"value": "<текст правила>"}` → 204.

**Режимы (`config.json → systemRule.mode`):**

| mode | Что делает |
|---|---|
| `instruction` (по умолчанию) | только instruction entry |
| `prompt` | только префикс к тексту сообщения (`[system]…[/system]`) |
| `both` | instruction entry + префикс (страховка) |

Текст правила содержит и **абсолютный** путь (чтобы модель не гадала), и
относительную форму `output\<id>\` — ровно как в ТЗ.

### 8.3. Где лежат файлы результата

`config.json → output.root` (по умолчанию `output`) — относительный путь
считается от `opencode.projectDirectory`, то есть каталог результатов =
`<корень проекта>\output\<id_сессии>\`.

Это сделано именно так, потому что **агент пишет файлы в пределах проекта**:
если положить каталог за пределами проекта, инструмент записи агента
заблокирует путь. Путь настраивается, если ограничения OpenCode это позволяют.

Портал при старте создаёт в `output\` файл `.gitignore` с содержимым
`*` + `!.gitignore` — рабочий каталог репозитория остаётся чистым
(в git попадает только сам этот файл `.gitignore`).

---

## 9. Файлы: правила выдачи

`GET /api/sessions/:id/files/:name` читает файл **с диска напрямую**
(никакого `GET /api/fs/read/*` OpenCode). Проверки по порядку:

1. **владение** сессией (иначе 404);
2. имя — **только имя файла**: запрещены `/`, `\`, `..`, абсолютные пути,
   UNC, буква диска (`C:`), NUL, зарезервированные имена Windows
   (`CON`, `PRN`, `AUX`, `NUL`, `COM1…9`, `LPT1…9`), завершающие точка/пробел,
   символы `<>:"|?*`;
3. расширение — только из `files.allowedExtensions` (по умолчанию `md`, `txt`, `csv`);
4. `lstat` → файл обязан быть **обычным файлом**, симлинк отклоняется;
5. `realpath` файла обязан лежать **внутри** каталога сессии (ловушка для
   жёстких ссылок и junction);
6. размер — не больше `files.maxBytes`;
7. ответ: `Content-Disposition: attachment; filename="…"; filename*=UTF-8''…`,
   `X-Content-Type-Options: nosniff`, `Cache-Control: no-store`.

Список файлов (`GET /api/sessions/:id/files`) проходит те же фильтры и
показывает только `name`, `size`, `modifiedAt`.

---

## 10. Запросы разрешений

Пользователю запросы разрешений **не показываются**. Портал сам отвечает отказом:

```
POST /api/session/{sessionID}/permission/{requestID}/reply
{ "decision": "reject", "message": "Портал работает в автоматическом режиме: запросы на разрешение отклоняются автоматически." }
```

Два механизма:

1. **реактивный** — событие `permission.asked` в общем потоке `GET /api/event`;
2. **периодический** — раз в 30 секунд портал перебирает `GET /api/session/{id}/permission`
   по всем известным сессиям и отклоняет всё, что осталось (поток событий
   OpenCode **volatile**: события, возникшие во время простоя портала, не
   воспроизводятся).

Пользователю в чат уходит событие `portal.permission.rejected` с понятным
текстом («запрос отклонён автоматически, попросите агента обойтись без
дополнительных прав»).

---

## 11. Срок хранения

`config.json → output.retentionDays` (по умолчанию **7 дней**).

* При старте и раз в сутки портал обходит `output\`, и папки старше срока
  удаляются целиком (`rm -rf` по содержимому, `rmSync(..., {recursive:true})`).
* Сама сессия в OpenCode и строка в БД **не удаляются** — ТЗ требует удалять
  именно папки с результатами. Из-за этого у старой сессии просто не будет файлов.

---

## 12. Безопасность

| Требование ТЗ | Реализация |
|---|---|
| Лимит размера сообщения | `limits.maxMessageChars` (20 000) → 413 |
| Лимит частоты запросов | `limits.requestsPerMinute` (60) на пользователя, token bucket → 429 + `Retry-After` |
| Лимит частоты сообщений | `limits.messagesPerMinute` (20) → 429 |
| Лимит тела запроса | `limits.maxBodyBytes` (256 КБ) → 413 |
| Логи: пользователь, сессия, агент, время | `src/logger.ts`, JSONL в `portal\logs\portal.log` |
| Логи без текста сообщений | из записи вырезаются ключи `text`, `body`, `content`, `prompt`, `message`, `messages`, `parts`, `system`, `value` |
| Секреты вне репозитория | пароль только из `OPENCODE_SERVER_PASSWORD` или из файла (`opencode.passwordFile`); `config.json` в `.gitignore`; `config.example.json` — шаблон без секретов |
| Вход | заголовок `X-User`, валидируется регуляркой `auth.userPattern`; без него 401 |
| Выбор проекта | не показывается: `opencode.projectDirectory` фиксирован в конфиге |

### 12.1. Точка замены аутентификации

`src/auth.ts` — единственный модуль, который решает «кто это». Он реализует
интерфейс `AuthProvider`:

```ts
export interface AuthProvider {
  readonly name: string
  authenticate(req: IncomingMessage): Promise<AuthResult> | AuthResult
}
```

Текущая реализация — `HeaderUserAuth` (заглушка `X-User`). Для AD/SSO
достаточно реализовать интерфейс и передать провайдер в
`createPortal({ auth: ... })` — больше нигде менять ничего не нужно.

---

## 13. Фронтенд

`public/index.html` + `public/app.js` + `public/styles.css` — ванильный JS,
**без сборки и без зависимостей**.

* **Стартовый экран**: три плитки режимов (название + описание из
  `GET /api/agents`), ниже — список «моих сессий» с удалением.
* **Чат**: сообщения пользователя справа, ответы агента слева; вызовы
  инструментов — **одна свёрнутая строка** (`⚙ имя — статус/готово/ошибка`),
  раскрывается по клику и показывает входные данные инструмента;
  «размышления» — тоже свёрнуты.
* **Стриминг**: SSE (`session.text.delta` → дописываем текст;
  `session.message.content.updated` и `session.step.ended` → перезапрос
  проекции сообщений).
* **Файлы**: ссылки на скачивание под сообщениями и отдельной панелью
  «Файлы сессии».
* **Оформление**: белый фон, красный акцент `#c62828`; **все цвета — в одном
  файле** `public/styles.css`, блок `:root` с переменными `--color-*`.

SSE в браузере читается вручную через `fetch` + `ReadableStream`, а не через
`EventSource`: `EventSource` не умеет передавать заголовок `X-User`.

---

## 14. Структура каталога

```
portal/
  DESIGN.md  RUN.md  TESTS.md
  package.json          type: module, 0 зависимостей
  tsconfig.json         только для редактора (noEmit)
  config.json           локальные настройки (в .gitignore)
  config.example.json   шаблон настроек
  .gitignore
  src/
    index.ts        точка входа
    config.ts       загрузка и проверка конфигурации (JSONC)
    auth.ts         X-User; точка замены на AD/SSO
    db.ts           SQLite: users, sessions
    opencode.ts     клиент OpenCode v2 (fetch) + разбор SSE
    events.ts       брокер событий: один upstream → много подписчиков
    permissions.ts  авто-отказ на запросы разрешений
    sessions.ts     бизнес-логика: режимы, сессии, сообщения, правило, retention
    files.ts        безопасный список/скачивание файлов
    ratelimit.ts    token bucket на пользователя
    logger.ts       JSONL-логи без текста сообщений
    http.ts         помощники HTTP
    discovery.ts    адрес фонового сервиса OpenCode
  public/
    index.html  app.js  styles.css
  test/
    harness.ts        запуск портала против mock-сервера
    mock-opencode.ts  mock API OpenCode v2
    portal.test.ts    тесты безопасности и прав
    config.test.ts    конфигурация и retention
  scripts/
    install-autostart.ps1   автозапуск через Планировщик задач
    uninstall-autostart.ps1 снятие автозапуска
    check.ps1              диагностика
```

---

## 15. Допущения

1. **Адрес сервера OpenCode меняется.** Фоновый сервер выбирает случайный
   порт. Поэтому `opencode.baseUrl` умеет значение `"auto"`: портал вызывает
   `opencode service status` и берёт URL оттуда. При обрыве соединения портал
   один раз перезапрашивает адрес и повторяет запрос.
2. **`GET /api/agent` требует `location[directory]`.** Без него сервер отдаёт
   агенты своей текущей директории. Портал всегда передаёт каталог проекта.
3. **`POST /api/session` без `location` берёт `process.cwd()` сервера.**
   Портал всегда передаёт `location: {directory: <проект>}`.
4. **События приходят по всем локациям.** Фильтр — по `data.sessionID`.
5. **Поток событий не переигрывается.** Отсюда периодический опрос разрешений
   и перезапрос проекции сообщений по `session.step.ended`.
6. **Папка `output\<id>` создаётся порталом** сразу при создании сессии, чтобы
   агент мог писать туда без дополнительных прав.
7. **Права на файлы в `output\<id>`** определяются процессом OpenCode. Портал
   работает с файлами от того же пользователя Windows.

---

## 16. Открытые вопросы

1. **Три режима.** В окружении нет агентов `ips-help` / `ips-designer` /
   `ips-technologist` — есть скиллы `ips-system-helper` / `ips-programmer` /
   `ips-technologist` в `skills/`. Нужно решить: (а) завести настоящих агентов
   в `opencode.json → agents`, (б) оставить как есть и описать режимы скиллами,
   (в) зашить конкретные id в `config.json → agents.allow`.
   Сейчас портал показывает то, что реально отдаёт OpenCode (в этом проекте —
   `build` и `plan`).
2. **Каталог `output\` в корне репозитория.** Так требует ТЗ (`output\<id>`)
   и так нужно агенту (писать можно только внутри проекта). Готов ли
   владелец репозитория видеть эту папку (в git попадает только
   `output/.gitignore`)?
3. **Пароль OpenCode.** Сейчас портал читает его из
   `~/.config/opencode/service.json` (тот же файл, которым пользуется сам
   OpenCode) либо из `OPENCODE_SERVER_PASSWORD`. Устраивает ли такой способ
   или нужен отдельный секрет в `C:\ProgramData`?
4. **Требование «ответ дублировать текстом в чат».** Реализовано как
   инструкция модели. Нужно ли дополнительно проверять факт дублирования
   (например, требовать вставки `<result>` в ответ) и показывать предупреждение,
   если модель ответила файлом без текста?
5. **Авто-отказ на разрешения.** Отказ означает, что агент не сможет, например,
   выполнить внешнюю команду. Достаточно ли режима «только output\<id>»,
   или нужно расширить белый список разрешений (сейчас любой запрос → отказ)?
6. **AD/SSO.** Какой именно (AD/LDAP, Keycloak, Entra ID) и какой формат
   логина должен быть у пользователя — пока реализована заглушка `X-User`.
7. **Публикация портала.** Сейчас портал слушает `127.0.0.1`. Если коллеги
   должны заходить по сети — нужен обратный прокси (IIS/nginx) с TLS и
   доменной аутентификацией.
