# Версия справки IPS

**version: unknown** — номер версии IPS в метаданных не найден.

## Что проверено (вывод команд)

- файлов `.hhp` в `D:\parsing`: 0
- файлы `.hhc`/`.hhk`: ['IPSHelp.hhc', 'IPSHelp.hhk']
- CHM `resources/IPSHelp.chm`: сигнатура b'ITSF', поле версии формата = 3 (версия формата CHM, не версия IPS)
- `.hhc`/`.hhk` (cp1251) содержат только `1.0` и `5.0` — это версии формата sitemap/HTML, не IPS
- `properties.xml`: только `1.0` и `8.25` (размер шрифта)
- `core/index.md`: `# Documentation Index - Version IPSHelp` — метка `IPSHelp`, не номер

- поиск `IPS 9.x` по 2287 исходникам: 0 вхождений

## Состояние файлов

- `version: "IPSHelp"` — 2290 файлов
- `version: "unknown"` — 0 файлов

Чтобы привести к `version: unknown`, нужен прогон build-скрипта; файлы не изменялись.
