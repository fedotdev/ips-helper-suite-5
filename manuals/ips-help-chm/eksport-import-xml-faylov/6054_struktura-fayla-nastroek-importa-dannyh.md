---
title: "Структура файла настроек импорта данных"
source: "IPSHelp/data/6054.md"
version: "unknown"
topic_id: "6054"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Настройка конфигурации экспорта/импорта > Ручное редактирование настроек конфигурации > Описание формата настроек импорта > Структура файла настроек импорта данных"
parent_id: "6053"
keywords: ["структура", "файла", "настроек", "импорта", "данных", "структура файла настроек импорта данных"]
---
# Структура файла настроек импорта данных
Главным элементом XML-документа является тег **< XMLImportSettings>**, который содержит всю информацию. Внутри основного элемента располагаются секции скриптов импорта, правил модификации данных XML, правил поиска объектов, правил импорта объектов, правила модификации метаданных, нормативно–справочная информация, настройки модулей расширения импорта / экспорта, заимствованные настройки экспорта, cоответствующие тегам **< Scripts>, <ModificationRules>, <SearchRules>, <ObjectImportRules>, <ObjectCreationRules>, <MetadataChangingRules>, <ImBase>, <Extentions>, <xmlexportsettings>.**
<!-- Секция с информацией о настройках импорта -->
<XMLImportSettings>
<!-- Секция скриптов импорта на языке C# -->
<Scripts >
<Script .... />
.... 
</Scripts>
<!--Общая секция правил модификации XML -->
<ModificationRules>
<ModificationRule..../>
....
</ModificationRules>
<!-- Правила поиска объектов \-->
<SearchRules>
<SearchRule .... />
....
</SearchRules>
<!-- Правила импорта объектов \-->
<ObjectImportRules>
<ObjectImportRule.... />
....
</ObjectImportRules>
<!-- Правила создания объектов \-->
<ObjectCreationRules>
<ObjectCreationRule.... />
....
</ObjectCreationRules>
<!-- Правила изменения метаданных -->
<MetadataChangingRules>
<MetadataChangingRule.... />
....
</MetadataChangingRules>
<!-- Нормативно-справочная информация -->
<ImBase>
....
</ImBase>
<!-- Настройки модулей расширения импорта -->
<Extentions>
....
</Extentions>
<!-- Заимствованные настройки экспорта -->
<xmlexportsettings..../>
</XMLImportSettings>
