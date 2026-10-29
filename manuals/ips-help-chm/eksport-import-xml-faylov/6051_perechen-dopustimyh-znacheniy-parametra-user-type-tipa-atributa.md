---
title: "Перечень допустимых значений параметра <user_type> типа атрибута <ATTRIBUTE_VALUE>"
source: "IPSHelp/data/6051.md"
version: "unknown"
topic_id: "6051"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Настройка конфигурации экспорта/импорта > Ручное редактирование настроек конфигурации > Описание формата настроек экспорта > Перечень допустимых значений параметра <user_type> типа атрибута <ATTRIBUTE_VALUE>"
parent_id: "6042"
keywords: ["перечень", "допустимых", "значений", "параметра", "user_type", "типа", "атрибута", "attribute_value", "перечень допустимых значений параметра <user_type> типа атрибута <attribute_value>"]
---
# Перечень допустимых значений параметра \<user\_type\> типа атрибута \<ATTRIBUTE\_VALUE\>
| **Тип данных** | **Значение** | **Наименование** | **Набор полей** |
| --- | --- | --- | --- |
| ftUnknown | 0 | Неизвестный тип |
| ftString | 1 | Строка | F_STRING_VALUE – строка F_VALUE = F_STRING_VALUE |
| ftInteger | 2 | Целое число | F_INTEGER_VALUE – число F_VALUE = F_INTEGER_VALUE |
| ftDouble | 3 | Вещественное число | F_DOUBLE_VALUE – число F_VALUE = F_DOUBLE_VALUE |
| ftDateTime | 4 | Дата | F_DATE_VALUE – дата и время по Гринвичу F_VALUE = F_DATE_VALUE |
| ftShortBlob | 5 | Короткие двоичные данные | F_STRING_VALUE – комментарии к блобуF_DATE_VALUE - дата последней модификации блобаF_FILESIZE – реальный размер файлаF_ARC_METHOD - метод запаковки файла (0 – не упакован, 1 – zip)F_FILE – название файла F_VALUE = F_FILE |
| ftFile | 6 | Файл | F_STRING_VALUE – настоящее имя файлаF_DATE_VALUE - дата последней модификации файлаF_INTEGER_VALUE – реальный размер файлаF_ARC_METHOD - метод запаковки файла (0 – не упакован, 1 – zip)F_FILE – название файла F_VALUE = F_FILE |
| ftExternalLink | 7 | Внешняя ссылка | В XML не используется |
| ftObjectLink | 8 | Ссылка на объект | F_STRING_VALUE – строковое представление ссылки F_INTEGER_VALUE - идентификатор объектаF_GUID – глобальный идентификатор объекта в IPS F_VALUE = F_INTEGER_VALUE |
| ftPassword | 9 | Пароль | F_STRING_VALUE - хэш пароляF_DATE_VALUE - дата назначения пароля F_VALUE = F_STRING_VALUE |
| ftMemo | 10 | Текст | F_STRING_VALUE - первые 450 символов текстаF_DATE_VALUE - дата последней модификации текста F_INTEGER_VALUE – реальный размер файлаF_ARC_METHOD - метод запаковки файла (0 – не упакован, 1 – zip)F_FILE – название файла F_VALUE = F_FILE |
| ftBlob | 11 | Двоичные данные | F_STRING_VALUE - комментарии к блобуF_DATE_VALUE - дата последней модификации блобаF_INTEGER_VALUE – реальный размер файлаF_ARC_METHOD - метод запаковки файла (0 – не упакован, 1 – zip)F_FILE – название файла F_VALUE = F_FILE |
| ftBoolean | 12 | Логический | F_INTEGER_VALUE – значение в числовом виде (True = 1, False = 0) F_VALUE = F_INTEGER_VALUE |
| ftMeasured | 13 | Вещественное число, выраженное в единицах измерения | F_GUID – глобальный идентификатор базовой единицы измеренияF_DOUBLE_VALUE – значение в базовой единице измеренияF_STRING_VALUE – строковое представление значения F_VALUE = F_STRING_VALUE |
| ftAutoInc | 14 | Автоинкрементное целое число | F_INTEGER_VALUE – число F_VALUE = F_INTEGER_VALUE |
| ftSystem | 15 | Системный атрибут | В XML не используется |
| ftGuid | 16 | Глобальный идентификатор | F_STRING_VALUE – строковое представление глобального идентификатора F_VALUE = F_STRING_VALUE |
