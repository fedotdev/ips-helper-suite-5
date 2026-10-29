---
title: "Objects.xml"
source: "IPSHelp/data/6091.md"
version: "unknown"
topic_id: "6091"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Описание формата файлов XML > Перечень файлов данных XML > Objects.xml"
parent_id: "6089"
keywords: ["objects", "xml", "objects.xml"]
---
# Objects.xml
Файл, содержащий информацию по объектам и версиям объектов.
Главным элементом XML-документа является тег **<OBJECTSDATASET> ,** внутри которого располагаются секции для описания объектов.
<!–– Секция с данными объектов ––>
<OBJECTSDATASET>
<!–– Информация об объекте ––>
<OBJECT>
....
</OBJECT>
...
<!–– Информация об объекте ––>
<OBJECT>
....
</OBJECT >
</OBJECTSDATASET>
Внутри тега **<OBJECT > **располагается информация о данных объекта/версии объекта.
...
<!–– Информация об объекте ––>
<OBJECT>
<F_OBJECT_ID>160832</F_OBJECT_ID>
<F_OBJECT_TYPE>1002</F_OBJECT_TYPE>
<F_OBJECTGUID>0232dc7c-4774-47f0-9f73-f990df52bc88</F_OBJECTGUID>
<F_ID>160833</F_ID>
<F_IDGUID>e7cd9c57-e3e6-46d6-bf5c-e521ddb8afe2</F_IDGUID>
<F_LC_STEP>10028</F_LC_STEP>
<F_CHKOUT_BY></F_CHKOUT_BY>
<F_CHKOUTGUID></F_CHKOUTGUID>
<F_VERSION_ID>0</F_VERSION_ID>
<F_OBJECT_VER_TYPE>0</F_OBJECT_VER_TYPE>
<F_OWNER_ID>58477</F_OWNER_ID>
<F_OWNERGUID>9b560d97-16f7-4ecb-9bde-4cb393e4d83b</F_OWNERGUID>
<F_MODIFY_DATE>11/22/2006 13:26:06</F_MODIFY_DATE>
<F_LEVEL_ID>2</F_LEVEL_ID>
<F_OBJ_CREATE>10/04/2006 06:05:02</F_OBJ_CREATE>
<CAPTION>Объект </CAPTION>
<!–– Атрибуты/параметры объекта ––>
<ATTRIBUTES>
....
</ATTRIBUTES>
</OBJECT>
....
Параметры:
Обязательные:
F_OBJECT_ID – локальный идентификатор версии объекта.
F_OBJECT_TYPE – идентификатор типа объекта (в файле **MetadataBrief.xml**).
Необязательные:
F_OBJECTGUID – глобальный идентификатор версии объекта в IPS.
F_ID – локальный идентификатор объекта.
F_IDGUID – глобальный уникальный идентификатор объекта в IPS.
F_LC_STEP – идентификатор этапа жизненного цикла.
F_CHKOUT_BY – локальный идентификатор пользователя, взявшего версию объекта на редактирование (может не указываться, если не взят на изменение).
F_CHKOUTGUID – глобальный идентификатор пользователя в IPS, взявшего версию объекта на редактирование (может не указываться, если не взят на изменение).
F_VERSION_ID – порядковый номер версии объекта.
F_OBJECT_VER_TYPE – признак версии/экземпляра/актуальной версии.
F_OWNER_ID – идентификатор объекта-владельца.
F_OWNERGUID – уникальный идентификатор объекта-владельца в IPS.
F_MODIFY_DATE – дата последней модификации объекта;
F_LEVEL_ID – идентификатор уровня продвижения.
F_OBJ_CREATE – дата создания версии объекта.
F_PARENT_ID – идентификатор родительской версии объекта.
F_PROJECT_ID – идентификатор проекта.
F_PROJECTGUID – глобальный идентификатор проекта.
CAPTION – заголовок объекта.
Подробное описание секции **< ATTRIBUTES>** находится в п. 
