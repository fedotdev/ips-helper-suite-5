---
title: "MetadataBrief.xml"
source: "IPSHelp/data/6090.md"
version: "unknown"
topic_id: "6090"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Описание формата файлов XML > Перечень файлов данных XML > MetadataBrief.xml"
parent_id: "6089"
keywords: ["metadatabrief", "xml", "metadatabrief.xml"]
---
# MetadataBrief.xml
Файл, содержащий краткое описание экспортируемых метаданных: типов атрибутов, объектов, связей.
Главным элементом XML-документа является тег **< METADATABRIEF>**, который содержит всю информацию. Внутри основного элемента располагаются секции для описания типов атрибутов/параметров, объектов, связей соответствующие тегам **< ATTRIBUTE_TYPES>, <OBJECT_TYPES>, <RELATION_TYPES>.**
<!–– Секция с информацией о типах ––>
<METADATABRIEF>
<!–– Информация о типах атрибутах/параметрах объектов/связей ––>
<ATTRIBUTE_TYPES>
<ATTRIBUTE_TYPE .... />
.... 
</ATTRIBUTE_TYPES>
<!–– Информация о типах объектов ––>
<OBJECT_TYPES>
<OBJECT_TYPE .... />
....
</OBJECT_TYPES>
<!–– Информация о типах связей ––>
<RELATION_TYPES>
<RELATION_TYPE .... />
....
</RELATION_TYPES>
</METADATABRIEF>
В элементе **< ATTRIBUTE_TYPE> **хранятся описания именованных атрибутов/параметров для объектов, связей.
.... 
<ATTRIBUTE_TYPE>
<F_ATTRIBUTE_ID>15054</F_ATTRIBUTE_ID>
<F_NAME>Обозначение</F_NAME>
<F_ALIAS> </F_ALIAS>
<F_ATTRIBUTE_TYPE>1</F_ATTRIBUTE_TYPE>
<F_GUID>cad0001f-306c-11d8-b4e9-00304f19f545</F_GUID>
</ATTRIBUTE_TYPE>
<ATTRIBUTE_TYPE>
<F_ATTRIBUTE_ID>15055</F_ATTRIBUTE_ID>
<F_NAME>Марка материала Ммтр</F_NAME>
<F_ALIAS>Ммтр</F_ALIAS>
<F_ATTRIBUTE_TYPE>1</F_ATTRIBUTE_TYPE>
<F_GUID>cae0f0e1-99ea-462c-9311-06f21a525a44</F_GUID>
</ATTRIBUTE_TYPE>
....
Параметры:
Обязательные:
F_ATTRIBUTE_ID**–** уникальный идентификатор атрибута в документе XML (целочисленное значение).
F_NAME**–** наименование атрибута/параметра.
Необязательные:
F_ALIAS**–** псевдоним атрибута.
F_GUID**–** глобальный идентификатор атрибута в системе IPS.
F_ATTRIBUTE_TYPE**–** тип данных атрибута (строковое, целочисленное, файл, ссылка на объект). В зависимости от типа атрибута, набор значащих полей у параметров объектов, связей будет разный. Если параметр не задан, то считается, что атрибут является строковым (**F_ATTRIBUTE_TYPE = 1**). Перечень допустимых значений параметра приведен в п. .
Элемент <**OBJECT_TYPE > **используется для хранения описания типов объектов.
.... 
<IMS_OBJECT_TYPES>
<F_OBJ_TYPE>1094</F_OBJECT_TYPE>
<F_OBJ_TYPE_NAME>Оборудование</F_OBJ_TYPE_NAME>
<F_ICON>icons\o_1002.ico </F_ICON>
<F_GUID>cad00121-306c-11d8-b4e9-00304f19f545</F_GUID>
</IMS_OBJECT_TYPES>
....
Параметры:
Обязательные:
F_OBJ_TYPE**–** уникальный идентификатор типа объекта в документе (целочисленное значение).
F_OBJ_TYPE_NAME**–** наименование типа объекта.
Необязательные:
F_GUID – глобальный идентификатор типа объекта в системе IPS.
F_ICON – имя файла для иконки типа объекта.
Элемент** <RELATION_TYPE> **содержит описания типов связей в документе.
....
<RELATION_TYPE>
<F_RELATION_TYPE>1</F_RELATION_TYPE>
<F_TYPE_NAME>Состав изделий</F_TYPE_NAME>
<F_GUID>cad00023-306c-11d8-b4e9-00304f19f545</F_GUID>
</RELATION_TYPE>
....
Параметры:
Обязательные:
F_RELATION_TYPE**–** уникальный идентификатор типа связи в документе (целочисленное значение).
F_TYPE_NAME**–** наименование типа объекта.
Необязательные:
F_GUID**–** глобальный идентификатор типа объекта в системе IPS.
