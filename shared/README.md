# Общие ресурсы IPS Helper Suite

## Структура

```
ips-helper-suite/
├── manuals/          # Руководства IPS, транскрипты, документация
│   ├── IPS. Руководство пользователя.md
│   ├── IPS. Руководство конструктора.md
│   ├── IPS. Руководство администратора.md
│   ├── IPS. Руководство программиста.md
│   ├── lessons/      # Транскрипты видеоуроков GMT*.txt + README-индекс
│   └── ...
├── references/       # Проверенные переписки, примеры ответов
│   ├── from-perplexity/  # Разборы из Perplexity (хорошие/плохие кейсы помощи)
│   ├── script-workflow/ # Анализы бесед по разработке (README.md — скрипты Workflow)
│   ├── from-helpdesk/    # Q&A техподдержки ИНТЕРМЕХ по заявкам RE-XXXXX + README-индекс
│   └── Zimbra.mhtml      # Исходник выгрузки from-helpdesk (там же скриншоты)
├── skills/
│   ├── ips-system-helper/   # Помощник по интерфейсу
│   ├── ips-programmer/      # Программист
│   └── ips-technologist/    # Технолог
└── shared/           # Этот файл
```

## Использование

Корень ресурсов (абсолютный, не зависит от места установки): `D:\parsing\IPS-Durdom-5\ips-helper-suite-5`.

- **manuals/** — основной источник информации. Содержит все руководства IPS и транскрипты видеоуроков (`lessons\`, GMT*.txt, индекс — `lessons\README.md`); путь `D:\parsing\IPS-Durdom-5\ips-helper-suite-5\manuals\`.
- **references/** — проверенные примеры прошлых переписок. Используйте как ориентир для формата и содержания ответов; путь `D:\parsing\IPS-Durdom-5\ips-helper-suite-5\references\`. Подпапки: `from-perplexity\` (разборы Perplexity), `script-workflow\` (разработка под задачи IPS), `from-helpdesk\` (Q&A техподдержки по заявкам RE-XXXXX, индекс — `README.md`; скриншоты — только в `Zimbra.mhtml`).
- Общие правила MCP: `D:\parsing\IPS-Durdom-5\ips-helper-suite-5\shared\mcp.md`.

## Проверочный чек-лист

- Имя навыка совпадает с именем каталога.
- В латинских именах навыков нет кириллических символов.
- Версии metadata актуальны.
- `evals/` покрывает изменённое поведение.
- Acceptance-кейсы: нет ID; MCP недоступен; 403 отличается от 404; mojibake; большой состав.
