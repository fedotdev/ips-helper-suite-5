---
title: "Пример содержания файла импорта"
source: "IPSHelp/data/6066.md"
version: "unknown"
topic_id: "6066"
section: "Экспорт-импорт XML-файлов"
path: "Экспорт-импорт XML-файлов > Настройка конфигурации экспорта/импорта > Ручное редактирование настроек конфигурации > Описание формата настроек импорта > Пример содержания файла импорта"
parent_id: "6053"
keywords: ["пример", "содержания", "файла", "импорта", "пример содержания файла импорта"]
---
# Пример содержания файла импорта
<?xml version="1.0" encoding="utf-8" ?>
<!–– Настройки импорта пакетов XML ––>
<XMLImportSettings>
<!–– Правила поиска объектов ––>
<SearchRules>
<!–– Общее правило поиска объектов ––>
<!–– object_type guid="00000000-0000-0000-0000-000000000000" name="Все объекты" operation="AND" ––>
<!–– attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="0" skipspaces="0" name="Обозначение" / ––>
<!–– /object_type ––>
<!–– Правило поиска "Детали" ––>
<object_type guid="cad00250-306c-11d8-b4e9-00304f19f545" name="Детали" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "МО" ––>
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Маршрут обработки" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "Заготовки" ––>
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Заготовка" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "ТП единичного" ––>
<object_type guid="cad00187-306c-11d8-b4e9-00304f19f545" name="Техпроцесс единичный" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "Цехозахода" ––>
<object_type guid="cad001ff-306c-11d8-b4e9-00304f19f545" name="Цехозаход" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "Операции" ––>
<object_type guid="cad00178-306c-11d8-b4e9-00304f19f545" name="Операция" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "Оборудования" ––>
<object_type guid="cad00177-306c-11d8-b4e9-00304f19f545" name="Оборудование" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
<!–– Правило поиска "Перехода" ––>
<object_type guid="cad0017d-306c-11d8-b4e9-00304f19f545" name="Переход" operation="AND">
<attribute guid="cad0001f-306c-11d8-b4e9-00304f19f545" casesensitive="1" skipspaces="0" name="Обозначение" />
</object_type>
</SearchRules>
<!–– Правила импорта объектов ––>
<ObjectImportRules>
<!–– object_type guid="00000000-0000-0000-0000-000000000000" name="Все объекты" rule="renew" / ––>
<object_type guid="cad00250-306c-11d8-b4e9-00304f19f545" name="Детали" rule="renew" />
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Маршрут обработки" rule="renew" />
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Заготовка" rule="renew" />
<object_type guid="cad00187-306c-11d8-b4e9-00304f19f545" name="Техпроцесс единичный" rule="renew" />
<object_type guid="cad001ff-306c-11d8-b4e9-00304f19f545" name="Цехозаход" rule="renew" />
<object_type guid="cad00178-306c-11d8-b4e9-00304f19f545" name="Операция" rule="renew" />
<object_type guid="cad00177-306c-11d8-b4e9-00304f19f545" name="Оборудование" rule="renew" />
<object_type guid="cad0017d-306c-11d8-b4e9-00304f19f545" name="Переход" rule="renew" />
</ObjectImportRules>
<!–– Правила создания объектов ––>
<ObjectCreationRules>
<!–– object_type guid="00000000-0000-0000-0000-000000000000" name="Все объекты" rule="createVersion" / ––>
<object_type guid="cad00250-306c-11d8-b4e9-00304f19f545" name="Детали" rule="createVersion" />
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Маршрут обработки" rule="createVersion" />
<object_type guid="cad0016f-306c-11d8-b4e9-00304f19f545" name="Заготовка" rule="createVersion" />
<object_type guid="cad00187-306c-11d8-b4e9-00304f19f545" name="Техпроцесс единичный" rule="createVersion" />
<object_type guid="cad001ff-306c-11d8-b4e9-00304f19f545" name="Цехозаход" rule="createVersion" />
<object_type guid="cad00178-306c-11d8-b4e9-00304f19f545" name="Операция" rule="createVersion" />
<object_type guid="cad00177-306c-11d8-b4e9-00304f19f545" name="Оборудование" rule="createVersion" />
<object_type guid="cad0017d-306c-11d8-b4e9-00304f19f545" name="Переход" rule="createVersion" />
</ObjectCreationRules>
<!–– Заимствованные настройки экспорта ––>
<xmlexportsettings task="0" extradata="1" defattr="0" compress="1">
<!–– Выгрузка "Детали" с технологическим составом (определенные объекты) ––>
<object_types>
<!–– Выгрузка "Детали" ––>
<object_type id="1052" guid="cad00250-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Детали" />
<attributes />
<!–– Выгрузка "МО" ––>
<object_type id="1037" guid="cad0016f-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Маршрут обработки" />
<attributes />
<!–– Выгрузка "Заготовки" ––>
<object_type id="1037" guid="cad0016f-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Заготовка" />
<attributes />
<!–– Выгрузка "ТП единичный" ––>
<object_type id="1237" guid="cad00187-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Техпроцесс единичный" />
<attributes />
<!–– Выгрузка "Цехозахода" ––>
<object_type id="1110" guid="cad001ff-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Цехозаход" />
<attributes />
<!–– Выгрузка "Операций" ––>
<object_type id="1075" guid="cad00178-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Операция" />
<attributes />
<!–– Выгрузка "Оборудования" ––>
<object_type id="1118" guid="cad00177-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Оборудование" />
<attributes />
<!–– Выгрузка "Переходов" ––>
<object_type id="1142" guid="cad0017d-306c-11d8-b4e9-00304f19f545" attrmode="2" objmodes="0" name="Переход" />
<attributes />
</object_types>
<relation_types>
<relation_type id="1002" guid="cad0019f-306c-11d8-b4e9-00304f19f545" attrmode="2">
<attributes />
</relation_type>
</relation_types>
<applsettings>
<!–– С "Детали" на "МО" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1052" projtype_guid="cad00250-306c-11d8-b4e9-00304f19f545" parttypeid="1037" parttype_guid="cad0016f-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "МО" на "Заготовку" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1037" projtype_guid="cad0016f-306c-11d8-b4e9-00304f19f545" parttypeid="1090" parttype_guid="cad001da-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "МО" на "ТП единичный" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1037" projtype_guid="cad0016f-306c-11d8-b4e9-00304f19f545" parttypeid="1237" parttype_guid="cad00187-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "ТП единичный" на "Цехозаход" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1237" projtype_guid="cad00187-306c-11d8-b4e9-00304f19f545" parttypeid="1110" parttype_guid="cad001ff-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "Цехозаход" на "Операции" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1110" projtype_guid="cad001ff-306c-11d8-b4e9-00304f19f545" parttypeid="1075" parttype_guid="cad00178-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "Операции" на "Оборудование" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1075" projtype_guid="cad00178-306c-11d8-b4e9-00304f19f545" parttypeid="1118" parttype_guid="cad00177-306c-11d8-b4e9-00304f19f545" applmode="0" />
<!–– С "Операции" на "Переходы" ––>
<applicability reltypeid="1002" reltype_guid="cad0019f-306c-11d8-b4e9-00304f19f545" projtypeid="1075" projtype_guid="cad00178-306c-11d8-b4e9-00304f19f545" parttypeid="1142" parttype_guid="cad0017d-306c-11d8-b4e9-00304f19f545" applmode="0" />
</applsettings>
</xmlexportsettings>
</XMLImportSettings>
