

# **1) Почему при регистрации модели и чертежа, они не связаны друг с другом? / Почему чертеж не входит в состав изделия?**

![Screenshot of the 'Конструкторские документы' (Engineering Documents) module in the 'Информационное пространство' (Information Space) of the 'Объекты' (Objects) database. The interface shows a tree view of document types on the left, a list of objects in the center, and a detailed view of the selected object 'IM1000.00.00.001 (Деталь)' on the right. The selected object is a 'Деталь' (Detail) for 'IM1000.00.00.001 МД (Деталь)' (IM1000.00.00.001 MD (Detail)). The interface includes a toolbar at the top with various icons and a status bar at the bottom showing 'Отношено: 0' and 'Всего: 2'.](935eed7aa61f7777f62cfc032e11bee9_img.jpg)

Screenshot of the 'Конструкторские документы' (Engineering Documents) module in the 'Информационное пространство' (Information Space) of the 'Объекты' (Objects) database. The interface shows a tree view of document types on the left, a list of objects in the center, and a detailed view of the selected object 'IM1000.00.00.001 (Деталь)' on the right. The selected object is a 'Деталь' (Detail) for 'IM1000.00.00.001 МД (Деталь)' (IM1000.00.00.001 MD (Detail)). The interface includes a toolbar at the top with various icons and a status bar at the bottom showing 'Отношено: 0' and 'Всего: 2'.

**Ответ:** Имена файла модели и чертежа должны совпадать посимвольно до расширения файла. В этом случае, при выполнении команды «Сохранить изменения» на модели, чертеж зарегистрируется в IPS автоматически.

# **2) У меня есть не ассоциативный чертеж (выполнен только в Компас-график), я зарегистрировал его в IPS, но изделие по этому чертежу не создалось. Почему так происходит?**

**Ответ:** Если Вы хотите работать с чертежами, которые не имеют ассоциативных связей с моделью, необходимо в интеграторе с КОМПАС-3D включить поддержку чертежей 2D. Обязательно нужно заполнить списки типов объектов чертежей 2D. В IPS есть уже необходимые типы объектов «Чертежи КОМПАС 2D» и «Сборочные чертежи КОМПАС 2D». После включения настройки, необходимо перезапустить клиент IPS.

![Screenshot of the 'Настройки' (Settings) window for 'Свойства: Интегратор с КОМПАС-3D'. The window shows a list of settings under 'Группы типов документов', 'Сохранение изменений', 'Завершение редактирования', 'Управление созданием версий', 'Нейтральные форматы документов', and 'Поддержка чертежей 2D'. The 'Включить поддержку чертежей 2D' option is selected and set to 'да' (Yes). A description at the bottom explains that this option includes or excludes support for integrator CAD system drawings that do not contain a link to a 3D model.](390120de4fe440c42fea8154fcaad334_img.jpg)

Screenshot of the 'Настройки' (Settings) window for 'Свойства: Интегратор с КОМПАС-3D'. The window shows a list of settings under 'Группы типов документов', 'Сохранение изменений', 'Завершение редактирования', 'Управление созданием версий', 'Нейтральные форматы документов', and 'Поддержка чертежей 2D'. The 'Включить поддержку чертежей 2D' option is selected and set to 'да' (Yes). A description at the bottom explains that this option includes or excludes support for integrator CAD system drawings that do not contain a link to a 3D model.

# **3) Как вывести в штамп чертежа дополнительные атрибуты, например, номер изменения, извещение, контрольную сумму и т.д.?**

Приведу пример на атрибутах «Контрольная сумма», «Номер изменения» и «Извещение». Остальные атрибуты выводятся аналогично.

**Ответ:** Настройка для вывода атрибутов производится в несколько шагов.

1. В файл ConvertKs.ini (C:\IM\CAD) в раздел [Convert] необходимо добавить атрибуты, которые отличаются по названию в IPS и в CAD.

```
[Convert]
;ПАРАМЕТР_В_ПДМ=ПАРАМЕТР_В_КАД
Н.контр.= Н. контр.
Т.контр. = Т. контр.
Номер изменения = Изменение
Извещение = N извещения
```

А в раздел [StampData] записываем в какие ячейки будут передаваться эти атрибуты. Название атрибута в этом разделе должно быть такое же, как в CAD.

```
[StampData]
;ПАРАМЕТР_В_КАД
Изменение=140
N извещения=160
CRC32 = 163
```

«CRC32» - это атрибут «Контрольная сумма», его необходимо записывать только в раздел [StampData]. Вместо «CRC32» Вы можете задать свое название через параметры IPS:

![Screenshot of the 'IPSParameters' dialog box in Russian. The left pane shows a tree view of settings. The right pane is a table of parameters. The 'Control Sum' property is selected, and its value is set to 'CRC32'. A description of the property is shown at the bottom.](b6cb8677b4ffb35c6468fa5c24091bff_img.jpg)

**Параметры IPS**

подписи

Найти

- Пользователи
  - Текущий пользователь
    - Электронные подписи
      - Избранные сертификаты
- Система
  - Просмотр документов
  - Удостоверяющие листы
  - Электронные подписи
    - Дополнительные параметры вывода подписей
    - Настройка отображения колонок подписей
    - Общие настройки
    - Параметры вывода подписи**

|                                                      |                                                      |
|------------------------------------------------------|------------------------------------------------------|
| Контрольная сумма                                    | CRC32                                                |
| Передавать дату подписи в параметр                   | [Графа для подписи]_Дата                             |
| Передавать должность по штатному расписанию          | [Графа для подписи]_Должность по штатному расписанию |
| Передавать должность подписавшего в параметр         | [Графа для подписи]_Должность                        |
| Передавать значение подписи в параметр               | [Графа для подписи]_ЭЦП                              |
| Передавать наименование графы для подписи в параметр | [Графа для подписи]_Графа                            |
| Передавать подписи в файлы форматов Интерн           | Да                                                   |
| Передавать фамилию из графы для подписи в параметр   | [Графа для подписи]                                  |
| Разрешить передачу подписей и контрольных сумм       | Да                                                   |
| <b>Свойство для контрольной суммы</b>                | CRC32                                                |
| Формат вывода даты подписи в документы               | dd.MM.yyyy                                           |

**Свойство для контрольной суммы**  
Наименование свойства, в которое передается сумма

OK Отмена

Screenshot of the 'IPSParameters' dialog box in Russian. The left pane shows a tree view of settings. The right pane is a table of parameters. The 'Control Sum' property is selected, and its value is set to 'CRC32'. A description of the property is shown at the bottom.

Цифра «163» - номер ячейки, куда нужно вывести этот атрибут.

Некоторые номера ячеек жестко определены разработчиками Компас. Их нельзя изменять.

## *Диапазон рекомендуемых значений пользовательских идентификаторов*

| <i>Диапазон</i>              | <i>Тип ячейки</i>                           |
|------------------------------|---------------------------------------------|
| <b>1000–2000, 2500-16383</b> | <b>Общий для листа, Общий для документа</b> |
| <b>100–150</b>               | <b>Для таблицы спецификации</b>             |

Смотреть или задавать номер ячейки нужно в Компасе. Настройки -> Библиотеки стилей -> Основные надписи.

В таблице станьте на нужную ячейку и выполните «Расширенный формат ячейки»

![Screenshot of the AutoCAD interface showing the 'Расширенный формат ячейки...' (Advanced cell format) dialog box. The dialog box is open over a table with columns for 'Изм.', 'Лист', '№ докум.', and 'Подп.'. The 'Стиль' (Style) is set to '* Default', 'Гарнитура' (Font) to 'GOST type A', and 'Начертание' (Font style) to 'B' (bold). The 'Цвет' (Color) is set to black. The 'Шрифт' (Font) is set to '12'.](48f188337e3ba41df38fab9ac0afb1bd_img.jpg)

Screenshot of the AutoCAD interface showing the 'Расширенный формат ячейки...' (Advanced cell format) dialog box. The dialog box is open over a table with columns for 'Изм.', 'Лист', '№ докум.', and 'Подп.'. The 'Стиль' (Style) is set to '\* Default', 'Гарнитура' (Font) to 'GOST type A', and 'Начертание' (Font style) to 'B' (bold). The 'Цвет' (Color) is set to black. The 'Шрифт' (Font) is set to '12'.

**Важно:** Файл ConvertKs.ini перечитывается только в момент запуска Компас. Если Вы провели в нем какие-либо изменения, когда компас был запущен, его необходимо перезапустить.

2. В настройках инструментов, на странице «Просмотр и печать файлов» для типов объектов, поставьте флаги на записи контрольной суммы и атрибутов. В список атрибутов добавить те, которые нужно выводить.

![Screenshot of the 'Настройка инструментов' (Tools Settings) window in the 'Записи' (Records) module. The window has a title bar with a question mark and a close button. Below the title bar is a 'Страница: Просмотр и печать файлов' (Page: View and Print Files) dropdown. A blue icon of a pencil writing on a document is on the left. The main text area explains that this page allows setting parameters for saving files in IPS objects, including actual signatures and file control sums. A tree view on the left shows a list of file types, with 'Чертежи КОМПАС' (COMPAQ drawings) selected. The right side has three sections: 'Запись подписей объекта в файл' (Object signatures to file) with a checked 'Использовать настройки родительского типа объектов' (Use parent type settings) and an unchecked 'Записывать подписи объекта' (Save object signatures); 'Запись контрольной суммы в файл' (File control sum) with an unchecked 'Использовать настройки родительского типа объектов' (Use parent type settings) and a checked 'Записывать контрольную сумму файла' (Save file control sum); and 'Запись атрибутов объекта в файл' (Object attributes to file) with an unchecked 'Использовать настройки родительского типа объектов' (Use parent type settings) and a checked 'Включить запись атрибутов' (Enable attribute saving). A list box shows 'Извещение' (Notice) as the selected attribute. There are 'Добавить' (Add) and 'Удалить' (Delete) buttons, and 'Применить' (Apply), 'Отменить' (Undo), and 'Закрыть' (Close) buttons at the bottom right.](df9888cdd1e5419ec477e8775e234f33_img.jpg)

Screenshot of the 'Настройка инструментов' (Tools Settings) window in the 'Записи' (Records) module. The window has a title bar with a question mark and a close button. Below the title bar is a 'Страница: Просмотр и печать файлов' (Page: View and Print Files) dropdown. A blue icon of a pencil writing on a document is on the left. The main text area explains that this page allows setting parameters for saving files in IPS objects, including actual signatures and file control sums. A tree view on the left shows a list of file types, with 'Чертежи КОМПАС' (COMPAQ drawings) selected. The right side has three sections: 'Запись подписей объекта в файл' (Object signatures to file) with a checked 'Использовать настройки родительского типа объектов' (Use parent type settings) and an unchecked 'Записывать подписи объекта' (Save object signatures); 'Запись контрольной суммы в файл' (File control sum) with an unchecked 'Использовать настройки родительского типа объектов' (Use parent type settings) and a checked 'Записывать контрольную сумму файла' (Save file control sum); and 'Запись атрибутов объекта в файл' (Object attributes to file) with an unchecked 'Использовать настройки родительского типа объектов' (Use parent type settings) and a checked 'Включить запись атрибутов' (Enable attribute saving). A list box shows 'Извещение' (Notice) as the selected attribute. There are 'Добавить' (Add) and 'Удалить' (Delete) buttons, and 'Применить' (Apply), 'Отменить' (Undo), and 'Закрыть' (Close) buttons at the bottom right.

**Дополнительная информация:** Выводить подписи гораздо проще. ConvertKs.ini уже настроен. Вам нужно только убедится, что в параметрах IPS стоит «Разрешить передачу подписей и контрольных сумм в файлы документов = Да» (первый рисунок в этом ответе) и в настройках инструментов поставить флаг на «Записывать подписи объекта».

Если все сделано правильно, то при просмотре документа, в штампе будет примерно следующее:

![Screenshot of the 'Дерево чертежа' (Tree of the drawing) in the 'Системный слой' (System layer). The tree shows 'Вид 1 (1:1)' selected, with 'Пысты' (Pusty) and 'Системный вид (1:1)' and 'Вид 1 (1:1)' listed below it. The drawing area shows a table with a red box around the first row. The table has columns for 'Инв. № подл.', 'Подп. и дата', 'Взам. инв. №', and 'Инв. инд. №'. The first row of the table contains the text 'D938F09F' and '1 Номер ИИ (123)'. To the right of the table are fields for 'Лит.', 'Масс', 'Лист', and 'Л/с'.](ddfe517a5ad9c77c89a57a5e780b24ca_img.jpg)

Screenshot of the 'Дерево чертежа' (Tree of the drawing) in the 'Системный слой' (System layer). The tree shows 'Вид 1 (1:1)' selected, with 'Пысты' (Pusty) and 'Системный вид (1:1)' and 'Вид 1 (1:1)' listed below it. The drawing area shows a table with a red box around the first row. The table has columns for 'Инв. № подл.', 'Подп. и дата', 'Взам. инв. №', and 'Инв. инд. №'. The first row of the table contains the text 'D938F09F' and '1 Номер ИИ (123)'. To the right of the table are fields for 'Лит.', 'Масс', 'Лист', and 'Л/с'.

**Важно:** Атрибуты будут передаваться в документы, только при просмотре и на шагах ЖЦ, на которых «Способ модификации объектов» = НЕ «Через рабочую копию объекта». Т.е. на шаге «В разработке» атрибуты не будут выводиться. При формировании аутентичных файлов эти атрибуты будут отображаться.

# **4) Мы создали новый формат чертежа с дополнительными полями\добавили новое поле в существующий формат\хотим использовать стандартное поле, которое на занято. Как сделать, чтобы созданные нами атрибуты в базе IPS синхронизировались с этими полями во время редактирования чертежа?**

Приведу пример на атрибуте «Подразделение» (списковый атрибут). Этот атрибут будет синхронизироваться с объектом «Чертежи КОМПАС».

**Важно:** Если необходимо какой-то атрибут синхронизировать с изделием, то его нужно будет заполнять в модели (кроме случаев, если Вы работаете с чертежами 2D).

**Ответ:** Настройка для синхронизации атрибутов с документом производится в несколько шагов:

1. Создаем новые поля и задаем им идентификатор ячейки (если используйте стандартную ячейку, зайдите, чтобы узнать ее идентификатор).

![Screenshot of the 'Расширенный формат ячейки' (Advanced Cell Format) dialog box in a spreadsheet application. The dialog is open to the 'Тип ячейки' (Cell Type) section. The 'Общий для документа' (General for document) radio button is selected. The 'Идентификатор ячейки' (Cell Identifier) field contains '300'. The 'Запрос при вводе текста в ячейку' (Prompt when entering text in cell) field contains 'Подразделение'. The background shows the spreadsheet interface with a formula bar containing 'Подразделение' and a sidebar with formatting options like 'Стиль', 'Символ', and 'Вставка'.](4b9457ad2400572dbf0c0c9f7c825643_img.jpg)

Screenshot of the 'Расширенный формат ячейки' (Advanced Cell Format) dialog box in a spreadsheet application. The dialog is open to the 'Тип ячейки' (Cell Type) section. The 'Общий для документа' (General for document) radio button is selected. The 'Идентификатор ячейки' (Cell Identifier) field contains '300'. The 'Запрос при вводе текста в ячейку' (Prompt when entering text in cell) field contains 'Подразделение'. The background shows the spreadsheet interface with a formula bar containing 'Подразделение' and a sidebar with formatting options like 'Стиль', 'Символ', and 'Вставка'.

- 2. В файл ConvertKs.ini (C:\IM\CAD) в раздел [StampParameters] необходимо добавить атрибуты

[StampParameters]

;ПАРАМЕТР\_V\_КАД

;поля, при установке которых в параметры документа 2Д, также будут дублироваться и в штамп в соответствии с секцией [StampData]

Подразделение

А в раздел [StampData] записываем в какие ячейки будет передаваться этот атрибут. Название атрибута в этом разделе должно быть такое же, как в CAD.

[StampData]

Подразделение = 300

Если Вам необходимо записывать собственные атрибуты в стандартные ячейки, в которых подразумевается наличие другого атрибута (уже определено компасом), то используйте дополнительно секцию [Convert]. Об этом описано в предыдущем вопросе.

**Важно:** Файл ConvertKs.ini пересчитывается только в момент запуска Компас. Если Вы провели в нем какие-либо изменения, когда компас был запущен, его необходимо перезапустить.

- 3. В IPS к типу объекта «Чертежи КОМПАС» добавляем необходимый атрибут.

В настройках инструментов, на странице «Интеграторы с приложениями» -> Интегратор с КОМПАС-3D в список свойства «Синхронизируемые атрибуты документов» добавляем атрибут «Подразделение»

![Screenshot of the 'Настройка инструментов' (Tool Settings) window in Russian. The 'Интеграторы с приложениями' (Integrators with applications) tab is selected. A sub-window 'Свойства: Интегратор с КОМПАС-3D' (Properties: Integrator with COMPAS-3D) is open, showing the 'Настройки' (Settings) tab. A dialog box 'Список типов атрибутов' (List of attribute types) is open over the '6. Поддержка чертежей 2D' (2D drawing support) section, showing a list of attribute types with 'Подразделение' (Division) selected.](c77403039b2f646be3509139acc05f6f_img.jpg)

Настройка инструментов

Страница: **Интеграторы с приложениями**

Для каждого интегратора должен бы...

Конфигурационные объекты интеграторов

Интегратор с Altium Desig... | Интегратор с AutoCAD | Интегратор с Autodesk In...

**Свойства: Интегратор с КОМПАС-3D**

**Настройки**

**1. Группы типов документов**

|                            |                         |
|----------------------------|-------------------------|
| Модели сборочных единиц    | (Список типов объектов) |
| Модели деталей             | (Список типов объектов) |
| Модели стандартных изделий | Стандартные КОМПАС      |
| Чертежи сборочных единиц   | (Список типов объектов) |
| Чертежи деталей            | (Список типов объектов) |

**2. Сохранение изменений**

|                                                |          |
|------------------------------------------------|----------|
| Включить поддержку файлов IMViewer             | нет      |
| Суффиксы имен файлов чертежей                  | (Список) |
| Сохранять чертежи как документы                | да       |
| Передавать в IPS информацию о допзаменах       | да       |
| Передавать в IPS типы связей между 3D-моделями | нет      |
| Синхронизируемые атрибуты документов           | (Список) |
| Синхронизируемые атрибуты изделий              |          |

**3. Завершение редактирования**

Обновлять аутентичные .pdf-файлы для моделей

Обновлять аутентичные .pdf-файлы для чертежей

**4. Управление созданием версий**

Непарные типы документов

**5. Нейтральные форматы документов**

Нейтральные форматы для вставки в сборочные

**6. Поддержка чертежей 2D**

|                                |  |
|--------------------------------|--|
| Включить поддержку чертежей 2D |  |
| Чертежи деталей 2D             |  |
| Сборочные чертежи 2D           |  |

**Синхронизируемые атрибуты документов**

Содержит список атрибутов, синхронизируемых между

**Список типов атрибутов**

| Тип атрибута         |  |
|----------------------|--|
| Разработал           |  |
| Проверил             |  |
| Утвердил             |  |
| Н.контр.             |  |
| Т.контр.             |  |
| Листов               |  |
| Литера               |  |
| Масштаб              |  |
| Формат               |  |
| <b>Подразделение</b> |  |

Добавить

Удалить

OK

Отмена

Применить

Отменить

Закрыть

Screenshot of the 'Настройка инструментов' (Tool Settings) window in Russian. The 'Интеграторы с приложениями' (Integrators with applications) tab is selected. A sub-window 'Свойства: Интегратор с КОМПАС-3D' (Properties: Integrator with COMPAS-3D) is open, showing the 'Настройки' (Settings) tab. A dialog box 'Список типов атрибутов' (List of attribute types) is open over the '6. Поддержка чертежей 2D' (2D drawing support) section, showing a list of attribute types with 'Подразделение' (Division) selected.

**Важно:** Один атрибут может синхронизироваться либо с документом, либо с изделием. В списках синхронизируемых атрибутов не должно быть повторяющихся записей. Если Вам нужно, чтобы атрибут

синхронизировался и в документ, и в изделие, это настраивается в Параметры IPS -> Система -> Управление информацией об изделиях -> Настройки синхронизации атрибутов документов и изделий.

4. Добавим данный атрибут в PDMBrowser. Т.к. у нас «Подразделение» списковый атрибут, в чертеже, в PDMBrowser, зайдите в настройки. На вкладке «Кнопки» добавьте необходимый атрибут. Для атрибута «Подразделение» необходимо задать список в соответствии со списком в IPS.

![Screenshot of the IPS software interface showing the 'Tree Structure' (Дерево состава) window. A table is visible with columns for Name (Наименование), Designation (Обозначение), Quantity (Кол...), Division (Раздел СП), Mass (Масса), and PDM Flag (Флаг PDM). The 'IM1000.00.00.004_004' row is selected. Three dialog boxes are open: 'Settings' (Настройки) for column configuration, 'Attribute' (Атрибут) for setting the 'Sub-division' (Подразделение) attribute to 'Document/Execution' (Документ/Исполнение), and 'List of possible values' (Список возможных значений) showing 'Design Bureau KB-1' and 'Design Bureau KB-2'.](8e592c58b5074d79831ff650c2c636df_img.jpg)

Screenshot of the IPS software interface showing the 'Tree Structure' (Дерево состава) window. A table is visible with columns for Name (Наименование), Designation (Обозначение), Quantity (Кол...), Division (Раздел СП), Mass (Масса), and PDM Flag (Флаг PDM). The 'IM1000.00.00.004\_004' row is selected. Three dialog boxes are open: 'Settings' (Настройки) for column configuration, 'Attribute' (Атрибут) for setting the 'Sub-division' (Подразделение) attribute to 'Document/Execution' (Документ/Исполнение), and 'List of possible values' (Список возможных значений) showing 'Design Bureau KB-1' and 'Design Bureau KB-2'.

Редактирование спискового атрибута лучше делайте через PDMBrowser. Если Вы будете заполнять это поле руками и ошибетесь где-то в написании, атрибут в IPS не будет заполнен.

После выполнения всех настроек, перезапустите Компас и IPS.

# 5) Возможно ли автоматическое создание аутентичных .pdf-файлов?

**Ответ:** Можно настроить, чтобы аутентичный файл .pdf формировался при завершении редактирования документа. Для этого в интеграторе включите обновление аутентичных .pdf-файлов.

![Screenshot of the 'Свойства: Интегратор с КОМПАС-3D' (Properties: Integrator with COMPAS-3D) dialog box. The 'Настройки' (Settings) tab is active. A warning icon and text state that changes will be applied immediately for the user and within 30 minutes for other users. A table lists various settings with their current values. The row 'Обновлять аутентичные .pdf-файлы для чертежей' is selected and set to 'да' (Yes). A description section at the bottom explains that if enabled, the integrator will create and update .pdf files for drawings upon completion of editing.](5549f7bbf28f047575f40f7da371a217_img.jpg)

Screenshot of the 'Свойства: Интегратор с КОМПАС-3D' (Properties: Integrator with COMPAS-3D) dialog box. The 'Настройки' (Settings) tab is active. A warning icon and text state that changes will be applied immediately for the user and within 30 minutes for other users. A table lists various settings with their current values. The row 'Обновлять аутентичные .pdf-файлы для чертежей' is selected and set to 'да' (Yes). A description section at the bottom explains that if enabled, the integrator will create and update .pdf files for drawings upon completion of editing.

# **6) Нам необходимо создавать детали БЧ из модели компаса. Какие настройки необходимо сделать?**

**Ответ:** Необходимо в конфигураторе базы данных настроить возможность выпуска детали БЧ по документу «Детали КОМПАС».

![Screenshot of the 'Конфигуратор базы данных' (Database Configurator) window in the 'Навигатор' (Navigator) tab. The left tree view shows a hierarchy of data objects. The main pane shows the 'Тип ](1dba94e9a65ea5fbd805e44a5a2c8cd5_img.jpg)

Screenshot of the 'Конфигуратор базы данных' (Database Configurator) window in the 'Навигатор' (Navigator) tab. The left tree view shows a hierarchy of data objects. The main pane shows the 'Тип

Далее в Компасе запускаем PDMBrowser и переходим в Настройки. Выбираем поле «Раздел СП» и редактируем его. В Список этого поля добавляем запись «Детали БЧ».

![Screenshot of the 'Tree of Composition' (Дерево состава) window in a CAD software. It shows a table with columns for Name, Designation, Quantity, Division, Mass, PDM Flag, Replacement, Material, and Independent. A 'Settings' (Настройки) dialog is open on the left, showing a list of columns with 'Division' (Раздел СП) selected. A 'Attribute' (Атрибут) dialog is in the center, with 'List' (Список) selected as the value type. A 'List of Possible Values' (Список возможных значений) dialog is on the right, showing 'Details 64' (Детали 64) selected. Red circles and numbers 1 through 8 highlight the steps: 1. Select the 'Division' column in the main table. 2. In the 'Settings' dialog, select the 'Division' checkbox. 3. Click the 'Pencil' icon to edit the column. 4. In the 'Attribute' dialog, select 'List' from the dropdown. 5. In the 'List of Possible Values' dialog, select 'Details 64'. 6. Click 'OK' in the 'List of Possible Values' dialog. 7. Click 'OK' in the 'Attribute' dialog. 8. Click 'OK' in the 'Settings' dialog.](0872c27ab0a48c6e88ef4f09f773872f_img.jpg)

Screenshot of the 'Tree of Composition' (Дерево состава) window in a CAD software. It shows a table with columns for Name, Designation, Quantity, Division, Mass, PDM Flag, Replacement, Material, and Independent. A 'Settings' (Настройки) dialog is open on the left, showing a list of columns with 'Division' (Раздел СП) selected. A 'Attribute' (Атрибут) dialog is in the center, with 'List' (Список) selected as the value type. A 'List of Possible Values' (Список возможных значений) dialog is on the right, showing 'Details 64' (Детали 64) selected. Red circles and numbers 1 through 8 highlight the steps: 1. Select the 'Division' column in the main table. 2. In the 'Settings' dialog, select the 'Division' checkbox. 3. Click the 'Pencil' icon to edit the column. 4. In the 'Attribute' dialog, select 'List' from the dropdown. 5. In the 'List of Possible Values' dialog, select 'Details 64'. 6. Click 'OK' in the 'List of Possible Values' dialog. 7. Click 'OK' in the 'Attribute' dialog. 8. Click 'OK' in the 'Settings' dialog.

У создаваемой\Регистрируемой детали выберете «Раздел СП» = «Детали БЧ» и выполните «Расширенное сохранение». Созданное по документу изделие в IPS будет «Деталь БЧ».

# **7) Необходимо, чтобы в изделие IPS записывалась масса с точностью до 2, 3, 4 и тд. знаков после запятой, возможно даже с отличной от «кг» единицей измерения.**

В файле Stamp.ini есть параметр [MassaDefaults], в этом параметре, в левой колонке, указаны диапазоны фактической массы в «кг». В правой колонке указано количество знаков после\перед «запятой», для массы, попавшей в данный диапазон (если в правой колонке отрицательное значение, округление будет перед «запятой»). Через запятую так же можно указать, какая будет использоваться единица измерения. Если ничего не указать, будет использоваться «кг».

Пример:

**[MassaDefaults]**

**0,001= 1,г**

**0,01 = 2**

**0,1 = 0,г**

**1 = 2**

**10 = 2**

**100 = -1**

**1000 = -2**

**>1000 =0,т**

0,001= 1,г – если масса в диапазоне от 0 до 0,001 кг, то округление будет до одного знака после запятой и выражено это будет в граммах. Т.е. фактическая масса детали 0,000764 кг

![Screenshot of the 'Список свойств' (Properties List) window in a CAD software. The window shows a table of properties for a selected object. The 'Масса' (Mass) property is highlighted with a red line, showing a value of 0.000764 and a unit of 'кг' (kg). The 'Зеркальность' (Reflectivity) is set to 80, 'Блеск' (Gloss) to 80, and 'Излучение' (Emission) to 50. To the right, a 3D model of a green rectangular plate with a blue and yellow wireframe is shown.](892f25e3d71d8e315a2a51092a8a8da7_img.jpg)

Screenshot of the 'Список свойств' (Properties List) window in a CAD software. The window shows a table of properties for a selected object. The 'Масса' (Mass) property is highlighted with a red line, showing a value of 0.000764 and a unit of 'кг' (kg). The 'Зеркальность' (Reflectivity) is set to 80, 'Блеск' (Gloss) to 80, and 'Излучение' (Emission) to 50. To the right, a 3D model of a green rectangular plate with a blue and yellow wireframe is shown.

PDMBrowser в этом случае покажет следующее:

![Screenshot of the 'Tree of Contents' (Дерево состава) window in the software. The table shows a row for 'Ks2025.03.18.002_Деталь' (Detail) with a mass of 0.8 g. A red box highlights the 'Mass' (Масса) column. A status bar at the bottom left indicates 'No available images' (Нет доступного изображения).](efca2dce0095c9dc2a68e9af6b2bfd40_img.jpg)

| Наименование            | Обозначение      | Кол-во | Раздел СП | Масса | Флаг PDM | Материал                |
|-------------------------|------------------|--------|-----------|-------|----------|-------------------------|
| Ks2025.03.18.002_Деталь | Ks2025.03.18.002 |        | Детали    | 0.8 г |          | Сталь 10 ГОСТ 1050-2013 |

Обозначение: Ks2025.03.18.002  
Наименование: Деталь  
Материал: Сталь 10 ГОСТ 1050-2013  
Файл: Ks2025.03.18.002\_Деталь.m3d  
Рабочая папка: D:\PS Vault\_9\4\Workspace\

Screenshot of the 'Tree of Contents' (Дерево состава) window in the software. The table shows a row for 'Ks2025.03.18.002\_Деталь' (Detail) with a mass of 0.8 g. A red box highlights the 'Mass' (Масса) column. A status bar at the bottom left indicates 'No available images' (Нет доступного изображения).

В IPS будет записано 0,8 г

![Screenshot of the 'Карточка объекта' (Object Card) window in the 'Компас-21' CAD software. The window title is 'Карточка объекта [-1402874] "Ks2025.03.18.002 МД (Деталь)" ("Детали КОМПАС")'. The left pane shows a tree view of objects with 'Ks2025.03.18.002 (Деталь)' selected. The main pane shows the 'Деталь' (Detail) tab with various fields. A red box highlights the 'Масса' (Mass) field, which contains the value '0,8 г' (0.8 g). Other fields include 'Обозначение' (Designation) with 'Ks2025.03.18.002', 'Наименование' (Name) with 'Деталь', 'Материал' with 'Сталь 10 ГОСТ 1050-2013', 'Учет в производстве' (Accounting in production) with 'Партиями' (By batches), and 'Код ОКП' (OKP Code) which is empty. There are buttons for 'Применить' (Apply) and 'Отмена' (Cancel) at the bottom right.](af54a0b30d50f3f8620b331cdc59ae3a_img.jpg)

Screenshot of the 'Карточка объекта' (Object Card) window in the 'Компас-21' CAD software. The window title is 'Карточка объекта [-1402874] "Ks2025.03.18.002 МД (Деталь)" ("Детали КОМПАС")'. The left pane shows a tree view of objects with 'Ks2025.03.18.002 (Деталь)' selected. The main pane shows the 'Деталь' (Detail) tab with various fields. A red box highlights the 'Масса' (Mass) field, which contains the value '0,8 г' (0.8 g). Other fields include 'Обозначение' (Designation) with 'Ks2025.03.18.002', 'Наименование' (Name) with 'Деталь', 'Материал' with 'Сталь 10 ГОСТ 1050-2013', 'Учет в производстве' (Accounting in production) with 'Партиями' (By batches), and 'Код ОКП' (OKP Code) which is empty. There are buttons for 'Применить' (Apply) and 'Отмена' (Cancel) at the bottom right.

**0,01 = 2** – если масса в диапазоне от 0,001 кг до 0,01 кг, то округление будет до двух знаков после запятой и выражено в килограммах. Т.е. фактическая масса 0,007643 кг, а PDMBrowser и IPS запишут массу 0,01 кг.

![Screenshot of the 'Tree of Structure' (Дерево состава) window in the KOMPAS CAD software. A red arrow points from the 'Mass' (Масса) column in the table to the 'Mass' (Масса) property value of 0.007643 kg in the 'List of Properties' (Список свойств) window on the left. The table shows a part 'K2025.03.18.002_Деталь' with a mass of 0.01, and the properties window shows the actual calculated mass as 0.007643.](d4e9f8f6bf5d7853ecae9c9633900af1_img.jpg)

| Свойство                            | Значение                                   | Единица изм |
|-------------------------------------|--------------------------------------------|-------------|
| Обозначение                         | K2025.03.18.002_МД                         | -           |
| Наименование                        | Деталь                                     | -           |
| Материал                            | Сталь 10 ГОСТ 1050-2013                    | -           |
| Масса                               | 0.007643                                   | кг          |
| <a href="#">Раздел спецификации</a> |                                            | -           |
| Форматы листов документа            |                                            | -           |
| Примечание                          |                                            | -           |
| Тип документа                       | Детали КОМПАС                              | -           |
| Внешний ключ изделия                | 01_DB3F6A64-25CD-4C54-ABA1-977D6FC8014D... | -           |
| Раздел СП                           | Детали                                     | -           |

|                                   | Обозначение: K2025.03.18.002     | Наименование: Деталь                      | Материал: Сталь 10 ГОСТ 1050-2013 |
|-----------------------------------|----------------------------------|-------------------------------------------|-----------------------------------|
| <b>Нет доступного изображения</b> | Файл: K2025.03.18.002_Деталь.m3d | Рабочая папка: D:\PS Vault_9\4\Workspace\ |                                   |

Screenshot of the 'Tree of Structure' (Дерево состава) window in the KOMPAS CAD software. A red arrow points from the 'Mass' (Масса) column in the table to the 'Mass' (Масса) property value of 0.007643 kg in the 'List of Properties' (Список свойств) window on the left. The table shows a part 'K2025.03.18.002\_Деталь' with a mass of 0.01, and the properties window shows the actual calculated mass as 0.007643.

**0,1 = 0,г** – если масса в диапазоне от 0,01 кг до 0,1 кг, то округление будет до целого числа, выраженного в граммах. Т.е. фактическая масса 0,076431 кг, а PDMBrowser и IPS запишут массу 76 г.

![Screenshot of the 'Свойства' (Properties) and 'Дерево состава' (BOM Tree) windows in a CAD program. The 'Свойства' window shows the 'Масса' (Mass) property set to 0.076431 кг. The 'Дерево состава' window shows a table with a row for 'Кз2025.03.18.002_Деталь' with a mass of 76 г. A red arrow points from the 'Масса' value in the properties to the 'Масса' column in the BOM table. A red box highlights the 'Масса' value in the properties window.](c37fe03d7cad74ad675a0eb16aa43821_img.jpg)

Свойства

| Свойство                            | Значение                                 | Единица изм |
|-------------------------------------|------------------------------------------|-------------|
| Обозначение                         | Кз2025.03.18.002 МД                      | -           |
| Наименование                        | Деталь                                   | -           |
| Материал                            | Сталь 10 ГОСТ 1050-2013                  | -           |
| Масса                               | 0.076431                                 | кг          |
| <a href="#">Раздел спецификации</a> | -                                        | -           |
| Форматы листов документа            | -                                        | -           |
| Примечание                          | -                                        | -           |
| Тип документа                       | Детали КОМПАС                            | -           |
| Внешний ключ изделия                | 01_DB3F6A64-25CD-4C54-ABA1-977D6FC8014D_ | -           |
| Раздел СП                           | Детали                                   | -           |

Дерево состава

| Наименование            | Обозначение | Кол-во           | Раздел СП | Масса |
|-------------------------|-------------|------------------|-----------|-------|
| Кз2025.03.18.002_Деталь | Деталь      | Кз2025.03.18.002 | Детали    | 76 г  |

**Нет доступного изображения**

Обозначение: Кз2025.03.18.002  
Наименование: Деталь  
Материал: Сталь 10 ГОСТ 1050-2013  
Файл: Кз2025.03.18.002\_Деталь.m3d  
Рабочая папка: D:\PS Vault\_9\1\1\Workspace\

Screenshot of the 'Свойства' (Properties) and 'Дерево состава' (BOM Tree) windows in a CAD program. The 'Свойства' window shows the 'Масса' (Mass) property set to 0.076431 кг. The 'Дерево состава' window shows a table with a row for 'Кз2025.03.18.002\_Деталь' with a mass of 76 г. A red arrow points from the 'Масса' value in the properties to the 'Масса' column in the BOM table. A red box highlights the 'Масса' value in the properties window.

**100 = -1** – если масса в диапазоне от 10 кг до 100 кг, то округление будет выполнено до десятой, перед запятой, в килограммах. Т.е. фактическая масса 76,431024 кг, а PDMBrowser и IPS запишут массу 80 кг.

![Screenshot of the 'Tree of composition' window in the software. A red arrow points from the 'Mass' column in the table to the '76.431024' value in the 'Свойство' (Property) table on the left. The 'Дерево состава' window shows a table with columns: Наименование, Обозначение, Кол-во, Раздел СП, and Масса. The row for 'К22025.03.18.002_Деталь' has a value of '80' in the 'Масса' column. The 'Свойство' table on the left shows the 'Масса' property with a value of '76.431024' and a unit of 'кг'.](aaec7c7044d764c324cdb35c0201da6f_img.jpg)

| Свойство                            | Значение                                 | Единица изм |
|-------------------------------------|------------------------------------------|-------------|
| Обозначение                         | К22025.03.18.002 МД                      | -           |
| Наименование                        | Деталь                                   | -           |
| Материал                            | Сталь 10 ГОСТ 1050-2013                  | -           |
| Масса                               | 76.431024                                | кг          |
| <a href="#">Раздел спецификации</a> |                                          | -           |
| Форматы листов документа            |                                          | -           |
| Примечание                          |                                          | -           |
| Тип документа                       | Детали КОМПАС                            | -           |
| Внешний клон изделия                | 01_DB3F6A64-25CD-4C54-ABA1-977D6FC8014D_ | -           |
| Раздел СП                           | Детали                                   | -           |

| Наименование            | Обозначение | Кол-во | Раздел СП | Масса |
|-------------------------|-------------|--------|-----------|-------|
| К22025.03.18.002_Деталь | Деталь      |        | Детали    | 80    |

|                            |                                                                                                                                                                              |
|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Нет доступного изображения | Обозначение: К22025.03.18.002<br>Наименование: Деталь<br>Материал: Сталь 10 ГОСТ 1050-2013<br>Файл: К22025.03.18.002_Деталь.m3d<br>Рабочая папка: D:\PS Vault_9\H\Workspace\ |
|----------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|

Screenshot of the 'Tree of composition' window in the software. A red arrow points from the 'Mass' column in the table to the '76.431024' value in the 'Свойство' (Property) table on the left. The 'Дерево состава' window shows a table with columns: Наименование, Обозначение, Кол-во, Раздел СП, and Масса. The row for 'К22025.03.18.002\_Деталь' has a value of '80' in the 'Масса' column. The 'Свойство' table on the left shows the 'Масса' property with a value of '76.431024' and a unit of 'кг'.

**>1000 =0,т** – если масса более 1000 кг, то округление будет до целого числа, выраженного в тоннах. Т.е. фактическая масса 7643,1024 кг, а PDMBrowser и IPS запишут массу 8 т.

![Screenshot of the 'List of properties' and 'Tree of structure' windows in a CAD program. The 'List of properties' window on the left shows a table with columns: Свойство, Значение, and Единица изм. A red box highlights the 'Масса' (Mass) row, where the 'Значение' is '7643.1024' and the 'Единица изм.' is 'кг' (kg). A red arrow points from this 'кг' value to the 'Масса' (Mass) column in the 'Tree of structure' window on the right. The 'Tree of structure' window shows a table with columns: Наименование, Обозначение, Кол-во, Раздел СП, and Масса. The 'Масса' column contains the value '8 t' for the item 'Ks2025.03.18.002_Деталь'.](625e10f48104ba2b06b2220a9b224712_img.jpg)

| Свойство                            | Значение                                    | Единица изм. |
|-------------------------------------|---------------------------------------------|--------------|
| Обозначение                         | Ks2025.03.18.002_МД                         | -            |
| Наименование                        | Деталь                                      | -            |
| Материал                            | Сталь 10 ГОСТ 1050-2013                     | -            |
| Масса                               | 7643.1024                                   | кг           |
| <a href="#">Раздел спецификации</a> |                                             | -            |
| Форматы листов документа            |                                             | -            |
| Примечание                          |                                             | -            |
| Тип документа                       | Детали КОМПАС                               | -            |
| Внешний ключ изделия                | 01_D83F6A64-25CD-4C54-ABA1-977D6FC8014D_... | -            |
| Раздел СП                           | Детали                                      | -            |

| Наименование                 | Обозначение      | Кол-во | Раздел СП | Масса |
|------------------------------|------------------|--------|-----------|-------|
| Ks2025.03.18.002_Деталь      | Ks2025.03.18.002 |        | Детали    | 8 t   |
| <a href="#">Конфигурации</a> |                  |        |           |       |

Обозначение: Ks2025.03.18.002  
Наименование: Деталь  
Материал: Сталь 10 ГОСТ 1050-2013  
Файл: Ks2025.03.18.002\_Деталь.m3d  
Рабочая папка: D:\PS Vault\_9\1\Workspace\

Screenshot of the 'List of properties' and 'Tree of structure' windows in a CAD program. The 'List of properties' window on the left shows a table with columns: Свойство, Значение, and Единица изм. A red box highlights the 'Масса' (Mass) row, where the 'Значение' is '7643.1024' and the 'Единица изм.' is 'кг' (kg). A red arrow points from this 'кг' value to the 'Масса' (Mass) column in the 'Tree of structure' window on the right. The 'Tree of structure' window shows a table with columns: Наименование, Обозначение, Кол-во, Раздел СП, and Масса. The 'Масса' column contains the value '8 t' for the item 'Ks2025.03.18.002\_Деталь'.

Единицы измерения, к которым можно привести массу: «г», «кг» и «т».

# **8) При заполнении атрибута в PDMBrowser, колонка остается пустой. ИЛИ**

**Не приходит атрибут при просмотре (при создании аутентичного файла) в документ. ИЛИ при открытии PDMBrowser компас зависает.**

Проблема из-за того, что IPS или PDMBrowser не может добавить в документ Компаса атрибут. Добавление атрибутов из PDMBrowser и IPS происходит через файл IMProperties.lpt. Этот файл создается автоматически либо в системной папке (в Компас «Приложения -> Cadmech-Оформление -> Установки»), либо по настройке в файле ConvertKs.ini , в разделе [Common], PROPERTIES\_SOURCE\_FILE  
Путь, записанный в этот параметр должен быть полный с расширением файла, например:  
`PROPERTIES_SOURCE_FILE= \\Server\IPS\IPS.InstClient\CADSystem\CAD\Kompas\IMProperties.lpt`

## **Важно:**

Указывать можно серверный путь, можно локальный, самое главное, чтобы у всех пользователей, работающих с Компас, был полный доступ к этому файлу.