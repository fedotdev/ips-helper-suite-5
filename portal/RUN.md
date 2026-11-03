# RUN — запуск портала на Windows 11

Портал лежит в `<корень репозитория>\portal`.
Ниже — команды для текущей машины; в других местах подставьте свой путь.

| Что | Значение на этой машине |
|---|---|
| Корень репозитория | `D:\parsing\IPS-Durdom-5\ips-helper-suite-5` |
| Каталог портала | `D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal` |
| Node.js | v26.5.0 (нужен **≥ 22.6**, проверено на 26.5.0) |
| OpenCode | v2.1.2 |
| Адрес портала | `http://127.0.0.1:8099` |

> **Ничего доустанавливать не нужно.** У портала **ноль** npm-зависимостей:
> HTTP — `node:http`, база — `node:sqlite`, тесты — `node:test`, TypeScript
> запускается нативно (Node удаляет типы сам, сборки нет).

---

## 0. Быстрая проверка, что всё на месте

```powershell
node --version        # v26.5.0  (должно быть >= 22.6)
opencode --version    # opencode v2.1.2
cd D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal
```

Если `node` не найден — поставьте Node.js 22.6+ (или новее) и перезапустите
терминал.

---

## 1. Разовая настройка

### 1.1. Конфигурация

```powershell
cd D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal
Copy-Item config.example.json config.json
```

`config.json` **не попадает в git** (он в `portal\.gitignore`). Откройте его и
проверьте:

```jsonc
{
  "server": { "host": "127.0.0.1", "port": 8099 },
  "opencode": {
    "baseUrl": "auto",                                   // спросить у opencode service status
    "username": "opencode",
    "passwordEnv": "OPENCODE_SERVER_PASSWORD",
    "passwordFile": "C:\\Users\\fedor\\.config\\opencode\\service.json",
    "projectDirectory": "D:\\parsing\\IPS-Durdom-5\\ips-helper-suite-5",
    "timeoutMs": 60000
  },
  "allowed_models": ["opencode/mimo-v2.6-flash-free"],   // единственная разрешённая модель
  "default_model": "opencode/mimo-v2.6-flash-free",
  "agents": { "allow": [], "modes": ["primary", "all"], "max": 3 },
  "output": { "root": "output", "retentionDays": 7, "writeGitignore": true },
  "limits": { "maxMessageChars": 20000, "requestsPerMinute": 60, "messagesPerMinute": 20, "maxBodyBytes": 262144 },
  "files": { "allowedExtensions": ["md", "txt", "csv"], "maxBytes": 10485760 },
  "auth": { "header": "X-User", "userPattern": "^[A-Za-z0-9._\\\\@ -]{1,64}$" },
  "logging": { "level": "info", "file": "logs/portal.log", "console": true },
  "systemRule": { "mode": "instruction", "key": "portal-output" }
}
```

Ключевые моменты:

* `opencode.baseUrl: "auto"` — фоновый сервер OpenCode выбирает **случайный
  порт**, поэтому портал сам спрашивает адрес командой `opencode service status`.
  Можно указать и фиксированный адрес, например `"http://127.0.0.1:4096"`, если
  поднимать свой `opencode serve --port 4096`.
* `opencode.projectDirectory` — единственный проект; выбор проекта в интерфейсе
  не показывается.
* `output.root` — относительный путь считается от каталога проекта, то есть
  результаты пишутся в `<корень проекта>\output\<id_сессии>\`.

### 1.2. Пароль сервера OpenCode

Портал должен предъявлять серверу OpenCode тот же пароль, что и `OPENCODE_SERVER_PASSWORD`.
Два способа (секрет в репозиторий не попадает ни в одном):

**Способ A (рекомендуется) — переиспользовать файл OpenCode.**
Уже работает «из коробки»: `opencode.passwordFile` указывает на
`%USERPROFILE%\.config\opencode\service.json`. Файл содержит JSON с полем
`password` — портал читает его напрямую (секрет не дублируется).

**Способ B — переменная окружения.**

```powershell
# на текущий сеанс
$env:OPENCODE_SERVER_PASSWORD = "<пароль>"
# постоянно для пользователя (реестр, не репозиторий)
[Environment]::SetEnvironmentVariable("OPENCODE_SERVER_PASSWORD", "<пароль>", "User")
```

> Постоянную переменную для **службы** (Планировщик задач) удобнее задать
> машинно: `[Environment]::SetEnvironmentVariable("OPENCODE_SERVER_PASSWORD", "<пароль>", "Machine")`
> — тогда её увидят и службы, и все пользователи машины.

### 1.3. Каталоги

Портал создаёт их сам при первом запуске:

```
portal\data\portal.db      база (SQLite)
portal\logs\portal.log     журнал
<корень проекта>\output\   папки результатов сессий
```

---

## 2. Запуск вручную (для проверки)

### 2.1. Поднять общий сервер OpenCode

Он уже может быть запущен — проверьте:

```powershell
opencode service status
# http://127.0.0.1:45278
```

Если пусто или сервера нет:

```powershell
opencode service start
opencode service status
```

### 2.2. Запустить портал

```powershell
cd D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal
node src/index.ts
```

Ожидаемый вывод:

```
{"ts":"...","level":"info","event":"portal.started","host":"127.0.0.1","port":8099,"opencode":"http://127.0.0.1:45278",...}
Portal listening on http://127.0.0.1:8099
```

Остановить — `Ctrl+C` (обработчик SIGINT корректно закрывает сервер,
останавливает SSE и закрывает базу).

Другие варианты запуска:

```powershell
node src/index.ts --config D:\...\portal\config.json --port 9000 --host 127.0.0.1
```

### 2.3. Проверить

```powershell
# портал отвечает (заголовок X-User обязателен)
Invoke-RestMethod http://127.0.0.1:8099/api/agents -Headers @{ "X-User" = "fedor" } | ConvertTo-Json -Depth 4

# без логина - 401
Invoke-WebRequest http://127.0.0.1:8099/api/agents

# фронтенд
Start-Process http://127.0.0.1:8099
```

В браузере откроется окно входа. Введите логин (например `fedor`) — он
отправляется в заголовке `X-User`. Дальше: три плитки режимов, список «моих
сессий», чат со стримингом и ссылками на файлы.

### 2.4. Готовая диагностика

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check.ps1
```

Покажет адрес и статус сервера OpenCode, число агентов, конфигурацию портала
и ответит ли портал на порту.

---

## 3. Автозапуск службами (Windows 11)

Используется **Планировщик задач Windows** — он в системе уже есть, ничего
ставить не нужно.

Скрипт `scripts\install-autostart.ps1` регистрирует две задачи:

| Задача | Что делает |
|---|---|
| `IPS-OpenCode-Service` | `opencode service start` — поднимает общий фоновый сервер |
| `IPS-Portal` | `node <portal>\src\index.ts --config <portal>\config.json --port 8099` |

### 3.1. Вход в Windows (без прав администратора)

```powershell
cd D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal
powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1 -Mode Logon
```

Задачи запустятся при каждом входе пользователя в Windows.

### 3.2. Старт Windows (нужны права администратора)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1 -Mode Startup
```

Запускается **PowerShell от имени администратора**. Задачи выполняются от
системной учётной записи — убедитесь, что у неё есть доступ к каталогу
репозитория и к `service.json` OpenCode.

### 3.3. Варианты

```powershell
# фиксированный адрес OpenCode вместо автоопределения
powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1 -OpenCodeUrl http://127.0.0.1:4096

# другой порт портала
powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1 -PortalPort 9000

# другой node.exe
powershell -ExecutionPolicy Bypass -File .\scripts\install-autostart.ps1 -NodePath "C:\Program Files\nodejs\node.exe"
```

Скрипт сам создаст `config.json` из `config.example.json`, если его ещё нет,
и пропишет в него `opencode.passwordFile` (путь к `service.json`) и
`opencode.baseUrl = "auto"`.

### 3.4. Управление задачами

```powershell
Get-ScheduledTask -TaskName 'IPS-Portal','IPS-OpenCode-Service'
Start-ScheduledTask -TaskName 'IPS-OpenCode-Service'
Start-ScheduledTask -TaskName 'IPS-Portal'
Stop-ScheduledTask  -TaskName 'IPS-Portal'
Get-ScheduledTaskInfo -TaskName 'IPS-Portal'
```

### 3.5. Снять автозапуск

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-autostart.ps1
```

---

## 4. Тесты

```powershell
cd D:\parsing\IPS-Durdom-5\ips-helper-suite-5\portal
npm test
# или без npm:
node --test "test/**/*.test.ts"
```

Тесты поднимают портал в памяти против mock-сервера OpenCode (реальный
OpenCode не трогается). Подробности — в `TESTS.md`.

---

## 5. Где смотреть проблемы

| Что | Где |
|---|---|
| Журнал портала | `portal\logs\portal.log` (JSONL, без текста сообщений) |
| Журнал OpenCode | `%LOCALAPPDATA%\opencode\log\opencode.log` (в v2 — `~/.local/share/opencode/log/opencode.log`) |
| Состояние сервиса OpenCode | `opencode service status` / `opencode service restart` |
| Состояние базы портала | `portal\data\portal.db` (SQLite) |

Полезные команды:

```powershell
opencode service restart      # сервер OpenCode поднялся с новым портом - портал сам его переузнает
opencode api get /api/info    # быстрая проверка API
Get-Content .\logs\portal.log -Tail 20
```

---

## 6. Частые проблемы

| Симптом | Причина и решение |
|---|---|
| `Configuration error: ...` | невалидный `config.json` — портал называет проблемное поле |
| `OpenCode server password not found` | не задана `OPENCODE_SERVER_PASSWORD` и неверный `opencode.passwordFile` (§1.2) |
| `Cannot find the OpenCode service` | не запущен фоновый сервер: `opencode service start`, либо укажите `opencode.baseUrl` явно |
| портал отвечает 401 | не передан заголовок `X-User` — это ожидаемо |
| плиток режимов меньше трёх | OpenCode отдаёт столько первичных агентов, сколько есть в проекте; перечень закрепляется в `agents.allow` |
| 429 при отправке сообщений | сработал `limits.messagesPerMinute` |
| агент не может создать файл | он пишет вне `output\<id>` — правило передаётся instruction entry, проверьте `opencode.service status` и лог OpenCode |
