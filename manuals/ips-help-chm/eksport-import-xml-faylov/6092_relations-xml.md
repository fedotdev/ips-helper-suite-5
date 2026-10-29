---
title: "Relations.xml"
source: "IPSHelp/data/6092.md"
version: "unknown"
topic_id: "6092"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Описание формата файлов XML > Перечень файлов данных XML > Relations.xml"
parent_id: "6089"
keywords: ["relations", "xml", "relations.xml"]
---
# Relations.xml
Файл, содержащий информацию по связям между объектами.
Главным элементом XML-документа является тег **<RELATIONSDATASET> ,** внутри которого располагаются секции с данными связей.
<!–– Секция с данными связей ––>
<RELATIONSDATASET>
<!–– Информация о связи ––>
<RELATION>
....
</RELATION>
...
<!–– Информация о связи ––>
<RELATION>
....
</RELATION >
</RELATIONSDATASET>
Между тэгами **< RELATION> **и **< /RELATION> **находятся значения атрибутов связи.
...
<!–– Информация о связи ––>
<RELATION>
<F_PRJLINK_ID>160836</F_PRJLINK_ID>
<F_RELATION_TYPE>1014</F_RELATION_TYPE>
<F_PROJ_OBJ>92655</F_PROJ_OBJ>
<F_PART_OBJ>160832</F_PART_OBJ>
<F_PROJ_ID>0232dc7c-4774-47f0-9f73-f990df52bc88</F_PROJ_ID>
<F_PART_ID>4b89e26e-aabf-4cdc-95c3-85bc7d2042fe</F_PART_ID>
<F_CREATE_DATE>10/04/2006 06:03:29</F_CREATE_DATE>
<!–– Атрибуты / параметры связи ––>
<ATTRIBUTES>
....
</ATTRIBUTES>
</RELATION >
....
Параметры:
Обязательные:
F_PRJLINK_ID – уникальный идентификатор типа связи в документе.
F_RELATION_TYPE – идентификатор типа связи (в файле **MetadataBrief.xml**).
F_PROJ_OBJ – локальный идентификатор версии родительского объекта.
F_PART_OBJ – локальный идентификатор версии дочернего объекта.
Необязательные:
F_PRJ_GUID – глобальный идентификатор связи в IPS.
F_PROJ_ID – глобальный идентификатор версии родительского объекта в IPS.
F_PART_ID – глобальный идентификатор дочернего объекта (не версии) в IPS.
F_CREATE_DATE – дата создания связи.
F_DELETE_DATE – дата удаления связи.
