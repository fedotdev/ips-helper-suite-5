

# Введение

Это инструкция по использованию IPS SDK для IPS 9 или новее. Пожалуйста, перед началом работы прочтите инструкцию полностью.

## Критические изменения

Начиная с IPS 8 используется новое SDK, несовместимое с SDK предыдущих версий IPS. Это связано с тем, что в серверной части IPS 8 появилась поддержка .NET 6 и работы на Linux. Для этого потребовалось адаптировать исходный код IPS под требования .NET 6 SDK и Visual Studio 2022, которые опираются на широкое использование NuGet-пакетов, обновленный формат csproj-проектов, а также на повсеместное редактирование csproj-проектов вручную.

Новое SDK устанавливается и обновляется вместе с IPS и не требует запуска отдельного инсталлятора. Теперь, просто установив или обновив IPS, вы можете быть уверены, что используете актуальную версию SDK.

Старое и новое SDK могут быть установлены на компьютере одновременно и не будут мешать друг другу.

## Системные требования

Требования к компьютеру разработчика:

1. Архитектура CPU: 64 бита
2. Число ядер CPU: 4 или больше
3. Оперативная память: 12 Gb или больше
4. Система хранения: SSD-диск с емкостью, достаточной для установки на него ОС, компиляторов, IDE и исходных кодов проектов.

Требования к ПО:

1. Windows 10 версии 1909 или новее, Windows Server 2016 или новее в редакции Standard или Datacenter;
2. IPS версии 9 или новее
3. Разработка на C#:
  - Visual Studio 2022 (только Windows) или JetBrains Rider 2023.3.3 (кросс-платформенный)
4. Разработка на Typescript:
  - Node.js версии 20.18 LTS
  - npm версии 10.8.2 (поставляется в составе Node.js)
  - Visual Studio 2022 (только Windows) или VSCode (кросс-платформенный)

## Разработка на C#

Подготовка окружения разработчика на C#

На компьютере разработчика IPS должен быть установлен локально в варианте **сервер+клиент**. Использование сетевых инсталляций IPS не поддерживается.

Все шаблоны проектов и примеры использования IPS SDK рассчитаны на то, что IPS установлен в расположение по умолчанию - **C:\Program Files\IPS**, и порты сервера приложений также оставлены по умолчанию - **8008** (для Remoting) и **8080** (для WebAPI). Если вы использовали другое расположение или номера портов сервера приложений, то при создании проектов с помощью IPS SDK необходимо будет корректировать их настройки.

Основной IDE для разработки на C# является Visual Studio, так как она является наиболее удобной.

VSCode (Visual Studio Code) на данный момент не поддерживается, так как она не обладает необходимыми возможностями по интеграции со сценариями MSBuild и по отладке Library-проектов, которыми являются все типы модулей расширения IPS.

## Шаблоны проектов на C#

Для корректной работы IPS SDK требуется использовать только *специальные* шаблоны проектов и солюшенов на C#, разработанные в Intermech. Шаблоны проектов из Visual Studio или .NET SDK работать не будут.

Специальные шаблоны проектов на C# сами являются частью IPS SDK, они устанавливаются и обновляются вместе с IPS. Доступ к ним осуществляется через утилиту командной строки **dotnet**. Посмотреть список всех шаблонов и их краткие описания можно с помощью следующей команды в консоли ОС:

```
dotnet new list ipssdk -lang C#
```

![Screenshot of a Windows PowerShell window showing the output of the 'dotnet new list ipssdk -lang C#' command. The command lists four templates: IPS SDK Client Module, IPS SDK Empty Solution, IPS SDK Server Module, and IPS SDK WebAPI Solution, all in C#.](b323c11adb72a798bdfd7df215e26ad1_img.jpg)

```
PS C:\Users\pmaximov> dotnet new list ipssdk -lang C#
These templates matched your input: 'ipssdk', -lang='C#'

Template Name                Short Name            Language  Tags
-----
IPS SDK Client Module         ipssdkclientmodule     C#        IPS SDK/Library
IPS SDK Empty Solution        ipssdkemptysolution    C#        IPS SDK/Solution
IPS SDK Server Module         ipssdkservermodule     C#        IPS SDK/Library
IPS SDK WebAPI Solution       ipssdkwebapisolution   C#        IPS SDK/WebAPI/Solution

PS C:\Users\pmaximov>
```

При работе в Visual Studio доступ к шаблонам осуществляется через стандартный диалог создания нового проекта. Для этого введите в поле поиска текст **IPS SDK**, а язык разработки выберите **C#**:

![Screenshot of the 'Create a new project' page in Visual Studio. The 'IPS SDK' is selected in the top dropdown. The 'C#' dropdown is selected, and 'All platforms' and 'All project types' are selected in their respective dropdowns. A list of project templates is shown below, including 'IPS SDK Client Module (Intermech)', 'IPS SDK Empty Solution (Intermech)', 'IPS SDK Server Module (Intermech)', 'IPS SDK WebAPI Solution (Intermech)', and 'VSIX Project'.](1df14e8a75f3f3fcf750749dbbd2c3f1_img.jpg)

Create a new project

Recent project templates

A list of your recently accessed templates will be displayed here.

IPS SDK Client Module (Intermech)  
A project template for creating a custom client module  
C# IPS SDK Library

IPS SDK Empty Solution (Intermech)  
A solution template for creating custom server and client modules  
C# IPS SDK Solution

IPS SDK Server Module (Intermech)  
A project template for creating a custom server module  
C# IPS SDK Library

IPS SDK WebAPI Solution (Intermech)  
A project template for creating a custom WebAPI server module and WebAPI client libraries  
C# IPS SDK Solution WebAPI

VSIX Project  
Build Visual Studio extensions using the Visual Studio SDK. Start with this template to create an extension project with an async package. Further extend functionality of Visual Studio by adding editor items, commands and tool windows.

Next

Screenshot of the 'Create a new project' page in Visual Studio. The 'IPS SDK' is selected in the top dropdown. The 'C#' dropdown is selected, and 'All platforms' and 'All project types' are selected in their respective dropdowns. A list of project templates is shown below, including 'IPS SDK Client Module (Intermech)', 'IPS SDK Empty Solution (Intermech)', 'IPS SDK Server Module (Intermech)', 'IPS SDK WebAPI Solution (Intermech)', and 'VSIX Project'.

## Создание нового солюшена

**Внимание!** Не используйте пробелы в именах создаваемых проектов, солюшенов или папок на диске. Из-за ошибки в .NET SDK создание проектов будет работать неправильно.

### **Visual Studio**

При работе в Visual Studio:

1. Запустить Visual Studio
2. На стартовом экране выбрать "Create a new project"
3. В поле "Search for templates" ввести "IPS SDK", либо в выпадающем списке "Project types" выбрать "IPS SDK"
4. Выбрать шаблон солюшена
5. Заполнить имя солюшена (например, MyIPSMModule)
6. Убедиться, что установлен флажок "Place solution and project in the same directory"
7. Завершить создание, нажав кнопку "Create"

В результате в Visual Studio будет открыт созданный солюшен. Теперь можно переходить к конфигурированию IPS SDK для солюшена.

### **JetBrains Rider**

При работе в JetBrains Rider следует учитывать, что он некорректно обрабатывает шаблоны солюшенов из-за ошибки **RIDER-71014** (<https://youtrack.jetbrains.com/issue/RIDER-71014>). Обойти эту ошибку можно следующим способом:

1. Запустить Rider
2. На стартовом экране выбрать "New Solution"
3. В поле поиска ввести "IPS SDK"
4. Выбрать шаблон
5. Заполнить имя солюшена в поле "Solution name" (например, MyIPSMModule)
6. Заполнить поле "Project name", указав то же имя, что и для солюшена (например, MyIPSMModule)
7. Убедиться, что установлен флажок "Put solution and project in the same directory"
8. Завершить создание, нажав кнопку "Create"

В процессе создания солюшена Rider покажет диалоговое окно с ошибкой *Solution 'MyIPSMModule.sln' already exists*. Это диалоговое окно следует просто закрыть. На самом деле солюшен *MyIPSMModule.sln* был успешно создан, и его можно открыть в Rider с диска.

Теперь можно переходить к конфигурированию IPS SDK для солюшена.

### Добавление проектов в солюшен

Добавление проекта выполняется аналогично созданию нового солюшена: используя шаблоны проектов IPS SDK. Главное, чтобы и сам солюшен был создан по шаблону. Если же использовать пустой солюшен, который по умолчанию создает IDE, то потом невозможно будет сконфигурировать IPS SDK.

**Внимание!** Не используйте пробелы в именах создаваемых проектов, солюшенов или папок на диске. Из-за ошибки в .NET SDK создание проектов будет работать неправильно.

## Конфигурирование IPS SDK для солюшена

После создания солюшена из шаблона необходимо сконфигурировать IPS SDK. Это делается с помощью файла **Directory.Build.props**, который расположен в корневой папке солюшена. В IDE этот файл можно найти в папке *Solution configuration* в дереве солюшена.

![Screenshot of the Visual Studio IDE showing the code editor and the Solution Explorer. The code editor displays the Directory.Build.props file with XML configuration for the IPS SDK. The Solution Explorer on the right shows the project structure, with the 'Directory.Build.props' file circled in red. The status bar at the bottom indicates 'No issues found' and 'Ln: 1 Ch: 1'.](ab5392ddbf4af970d4ef7bff2e4df925_img.jpg)

Screenshot of the Visual Studio IDE showing the code editor and the Solution Explorer. The code editor displays the Directory.Build.props file with XML configuration for the IPS SDK. The Solution Explorer on the right shows the project structure, with the 'Directory.Build.props' file circled in red. The status bar at the bottom indicates 'No issues found' and 'Ln: 1 Ch: 1'.

Файлы **Directory.Build.props** и **Directory.Build.targets** не являются частью IPS SDK. Они относятся к MSBuild и Visual Studio, являются сценариями MSBuild и используются для расширения стандартных

возможностей MSBuild. Подробнее о них можно прочитать здесь: <https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build?view=vs-2022>

Подробнее о формате файлов сценариев MSBuild можно прочитать здесь:

1. <https://learn.microsoft.com/en-us/visualstudio/msbuild/walkthrough-creating-an-msbuild-project-file-from-scratch?view=vs-2022>
2. <https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-project-file-schema-reference?view=vs-2022>

В файле **Directory.Build.props** следует указать значения для следующих параметров:

1. **IPSServerDirectory** - путь к папке с установленной серверной частью IPS
2. **IPSClietDirectory** - путь к папке с установленной клиентской частью IPS

**Важно!** Если эти параметры не заданы или заданы неверно, то солюшен собираться не будет.

После изменения параметров в файле **Directory.Build.props** его следует просто сохранить. IDE автоматически подхватит изменения и применит их к солюшену.

### Конфигурирование проектов в солюшене

Каждый проект модуля расширения IPS в солюшене содержит файл **IPS.SDK.props**. Он позволяет задать тип модуля IPS, а также указать ссылки на используемые сборки IPS. Этот файл также является сценарием MSBuild и имеет тот же формат, что и файлы **Directory.Build.props** и **Directory.Build.targets**

![Screenshot of the Visual Studio IDE showing the MyIPSModule1 project. The main window displays the IPS.SDK.props file with XML configuration for the IPS SDK. The Solution Explorer on the right shows the project structure, with the IPS.SDK.props file circled in red and selected in the tree view.](04519be9bd73b202914a1bb3da732edd_img.jpg)

The screenshot shows the Visual Studio interface for the 'MyIPSModule1' project. The main editor window displays the 'IPS.SDK.props' file with the following XML content:

```
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <!--Настройки IPS SDK для текущего проекта (серверный модуль с пользовательским Web API)-->
  <!--Такой проект в солюшене может быть только один-->
  <PropertyGroup>
    <IPSServerModule>True</IPSServerModule>
    <IPSServerWebApiModule>true</IPSServerWebApiModule>
  </PropertyGroup>

  <!--Подключаем сборки из серверной части IPS (пути и расширения сборок указывать не нужно)-->
  <!--Используем атрибут Private="False", чтобы указать, что подключаемые сборки IPS не нужно копиро>
  <ItemGroup>
    <IPSReference Include="Intermech.Bcl" Private="False" />
    <IPSReference Include="Intermech.Interfaces" Private="False" />
    <IPSReference Include="Intermech.Interfaces.Server" Private="False" />
    <IPSReference Include="Intermech.Kernel" Private="False" />
  </ItemGroup>
</Project>
```

The Solution Explorer on the right side shows the project structure. The 'IPS.SDK.props' file is circled in red and is selected in the tree view. The project structure includes:

- Solution configuration
  - Directory.Build.props
  - Directory.Build.targets
  - NuGet.config
  - readme.md
- MyIPSModule.ClientLib
  - Dependencies
  - AutoGenerated
  - IPS.SDK.props
- MyIPSModule.ConsoleTest
  - Dependencies
  - IPS.SDK.props
  - C# Program.cs
- MyIPSModule.WebApi
  - Connected Services
  - Dependencies
  - Properties
  - Controllers
  - IPS.SDK.props
  - C# Package.cs

The status bar at the bottom indicates 'No issues found' and 'Ln: 1 Ch: 1 SPC CRLF'. The 'Output' tab is also visible, showing 'Show output from: Package Manager'.

Screenshot of the Visual Studio IDE showing the MyIPSModule1 project. The main window displays the IPS.SDK.props file with XML configuration for the IPS SDK. The Solution Explorer on the right shows the project structure, with the IPS.SDK.props file circled in red and selected in the tree view.

После любых изменений в файле **IPS.SDK.props** его следует просто сохранить. IDE автоматически подхватит изменения и применит их к проекту.

### **Тип модуля расширения**

Чтобы указать тип модуля расширения IPS создайте один из следующих параметров со значением True:

1. **IPSServerModule** - серверный модуль расширения
2. **IPSClietModule** - клиентский модуль расширения

**Важно!** Не указывайте оба параметра одновременно.

Пример для серверного модуля расширения

```
<PropertyGroup>
  <IPSServerModule>True</IPSServerModule>
</PropertyGroup>
```

Пример для клиентского модуля расширения

```
<PropertyGroup>
  <IPSClientModule>True</IPSClientModule>
</PropertyGroup>
```

### **Ссылки на сборки IPS**

Для создания ссылки на сборку IPS создайте элемент *IPSReference* с атрибутом *Include* в группе *ItemGroup*. В атрибуте *Include* укажите имя сборки без пути и без расширения.

Дополнительно у элемента *IPSReference* может быть задан необязательный атрибут *Private*, который управляет копированием сборки IPS в папку модуля расширения при компиляции:

- Если атрибут *Private* не задан или имеет значение True, то сборка IPS будет скопирована в папку модуля расширения
- Если же атрибут *Private* задан и имеет значение False, то сборка IPS не будет скопирована в папку модуля расширения

Например:

```
<ItemGroup>
  <!--Эти сборки используются модулем расширения, но не копируются при компиляции
в папку модуля-->
  <IPSReference Include="Intermech.Bcl" Private="False" />
  <IPSReference Include="Intermech.Interfaces" Private="False" />

  <!--Эти сборки используются модулем расширения и копируются при компиляции в
папку модуля-->
  <IPSReference Include="ICSharpCode.SharpZipLib" />
</ItemGroup>
```

### **Отладка проектов в солюшене**

Шаблоны проектов IPS SDK предоставляют простой и удобный способ отладки кода для всех типов модулей расширения IPS. Больше не требуется использовать post-build event для копирования .dll-файлов модулей расширения в папки клиента или сервера IPS. Также для отладки модулей расширения больше не требуется создавать объекты типа "Загружаемый модуль" в базе данных IPS.

#### Отладка одного модуля расширения

В Visual Studio для начала отладки любого модуля расширения IPS достаточно воспользоваться обычной командной **Debug\Start Debugging** из главного меню (горячая клавиша - **F5**), или нажать аналогичную кнопку на панели инструментов.

Кнопка на панели инструментов меняет свое название в зависимости от типа модуля расширения. Для серверных модулей она будет называться **Via ConsoleServer**, а для клиентский модулей - **Via IMClient**.

![Screenshot of Visual Studio showing the Debug menu open with 'Start Debugging' selected. Red circles highlight the 'Debug' menu and the 'Via ConsoleServer' option in the Extensions menu. The code editor shows the 'MyIPSMModule.WebApi' class with a 'DoLoad' method. The Solution Explorer shows the project structure. The status bar at the bottom indicates 'No issues found' and 'Ln: 1 Ch: 1 SPC CRLF'.](c1c7af7ea36be0323047962df57d75b0_img.jpg)

The screenshot shows the Visual Studio interface with the following elements visible:

- Menu Bar:** File, Edit, View, Project, Build, **Debug** (circled in red), Architecture, Test, Analyze, Tools, VisualSVN, Extensions, Window, Help. The Extensions menu is open, showing 'Via ConsoleServer' (circled in red).
- Toolbar:** Standard toolbar with various icons for build, debug, and navigation.
- Code Editor:** Displays the 'MyIPSMModule...Api: Overview' for 'MyIPSMModule.WebApi'. The code shown is:
 

```
1  namespace M
2  {
3      using
4      using I
5      using
6      using M
7
8      interna
9  {
10     pub
11     {
12     }
13     }
14
15     ///
16     pro
17     {
18         base.DoInitializePackage();
19     }
20
21     /// <inheritdoc>
22     protected override void DoLoad()
23     {
24         base.DoLoad();
25     }
```
- Solution Explorer:** Shows the project 'MyIPSMModule' (3 of 3 projects) with 'MyIPSMModule.WebApi' selected. The 'Package.cs' file is open in the editor.
- Solution Explorer Notifications:** A panel at the bottom right titled 'Solution Explorer Notifications' with a 'Build Action' section:
 

|                          |             |
|--------------------------|-------------|
| Copy to Output Directory | Do not copy |
|--------------------------|-------------|

 The description says: "How the file relates to the build and deployment processes."
- Status Bar:** Shows '121 %', 'No issues found', and 'Ln: 1 Ch: 1 SPC CRLF'.
- Bottom Menu:** Bookmarks, Package Manager Console, C# Interactive, Error List, Task List, Output, Breakpoints, Exception Settings, Code Coverage Results, Call Hierarchy.
- Window Title:** 'MyIPSMModule' with a notification badge '1'.

Screenshot of Visual Studio showing the Debug menu open with 'Start Debugging' selected. Red circles highlight the 'Debug' menu and the 'Via ConsoleServer' option in the Extensions menu. The code editor shows the 'MyIPSMModule.WebApi' class with a 'DoLoad' method. The Solution Explorer shows the project structure. The status bar at the bottom indicates 'No issues found' and 'Ln: 1 Ch: 1 SPC CRLF'.

После запуска отладки будет сделано следующее:

1. Visual Studio запустит основное приложение IPS: ConsoleServer или IMClient
2. Основное приложение IPS загрузит отлаживаемый модуль расширения непосредственно из папки **bin\Debug** или **bin\Release**, в которую его выкладывает Visual Studio в результате сборки

#### Отладка нескольких модулей расширения одновременно

Если же ваш солюшен содержит серверные и клиентские модули расширения, которые необходимо отлаживать одновременно, воспользуйтесь командой контекстного меню **Debug\Start New Instance** из дерева солюшена (**Solution Explorer**). Сначала запустите отладку серверного модуля расширения, а затем клиентского.

![Screenshot of the Visual Studio Solution Explorer and Build menu. The Solution Explorer shows the project 'MyIPSMModule' with the 'MyIPSMModule.WebApi' project selected. The Build menu is open, and the 'Debug' option is selected, with the 'Start New Instance' option circled in red.](c834b9abb4ddf70e5d10641f87d5ff5b_img.jpg)

The screenshot shows the Visual Studio interface. On the left, the Solution Explorer is open, showing the project 'MyIPSMModule' with the 'MyIPSMModule.WebApi' project selected. The Build menu is open on the right, and the 'Debug' option is selected. The 'Start New Instance' option is circled in red.

Screenshot of the Visual Studio Solution Explorer and Build menu. The Solution Explorer shows the project 'MyIPSMModule' with the 'MyIPSMModule.WebApi' project selected. The Build menu is open, and the 'Debug' option is selected, with the 'Start New Instance' option circled in red.

## Особенности пользовательских WebAPI-модулей

IPS SDK позволяет создавать серверные модули расширения, которые содержат пользовательское WebAPI. Для этого следует воспользоваться шаблоном солюшена "IPS SDK WebAPI Solution".

Например, если по этому шаблону создать солюшен MyIPSMModule, то он будет содержать 3 проекта:

1. MyIPSMModule.WebAPI - серверный модуль расширения с API-контроллерами ASP.NET
2. MyIPSMModule.ClientLib - клиентская библиотека на C# для обращения к пользовательскому WebAPI
3. IPSModule.ConsoleTest - пример консольного приложения, которое использует пользовательское WebAPI

**Важно!** В солюшене может быть только 1 серверный модуль расширения с пользовательскими API-контроллерами ASP.NET.

### Просмотр списка доступных эндпоинтов и документации по WebAPI через Swagger UI

Страница Swagger UI для сервера, установленного по умолчанию и с портами по умолчанию доступна по ссылке: <http://localhost:8080/swagger/index.html>. В случае изменения настроек по умолчанию скорректируйте имя сервера и порт.

#### IPS Server Web API 1.0 OAS3

<http://localhost:8080/swagger/1.0/swagger.json>

Authorize ![](71ceb62b681518c82e95d615e7265d66_img.jpg)

#### Archives

#### AttributeHistory

#### Auth

### Взаимодействие с WebApi через Swagger UI

Большая часть доступных эндпоинтов требует авторизации. Для получения JWT-токена можно воспользоваться Swagger UI. Для этого необходимо вызвать эндпоинт **Auth/authenticate**.

1. Откройте в браузере страницу Swagger UI
2. Разверните группу **Auth**
3. В группе **Auth** разверните эндпоинт **/core/api/Auth/authenticate**
4. Нажмите кнопку **"Try it out"**
5. В секции **Request body** заполните данные о пользователе
6. Нажмите кнопку **"Execute"**.

![Screenshot of the Swagger UI interface for the 'Auth' endpoint. The interface shows a GET request for '/core/api/Auth/logins/{loginName}/options' and a POST request for '/core/api/Auth/authenticate'. The POST request is selected, showing a 'Parameters' section with 'No parameters', a 'Request body' section set to 'application/json', and a JSON code block containing the request payload. At the bottom, there are 'Execute' and 'Clear' buttons.](a3f0a26e9fea6033e41af2e2b61738d2_img.jpg)

**Auth**

**GET** /core/api/Auth/logins/{loginName}/options Возвращает опции аутентификации пользователя

**POST** /core/api/Auth/authenticate Выполняет аутентификацию пользователя и возвращает новый токен для доступа к API

Parameters Cancel Reset

No parameters

Request body required application/json

Запрос аутентификации пользователя

```
{
  "loginName": "sysdba",
  "password": "intermech",
  "passwordType": "plainText",
  "roleID": 10,
  "accessLevelID": 0
}
```

**Execute** Clear

В результате выполнения запроса вернется пара токенов:

- **accessToken** - для доступа к API

- **refreshToken** - для получения новой пары. Используется для продления времени действия токена доступа

```
Curl
curl -X 'POST' \
'http://localhost:8080/core/api/Auth/authenticate' \
-H 'accept: application/json' \
-H 'Content-Type: application/json' \
-d '{
  "loginName": "sysdba",
  "password": "intermech",
  "passwordType": "plainText",
  "roleID": 10,
  "accessLevelID": 0
}'
```

Request URL

http://localhost:8080/core/api/Auth/authenticate

Server response

| Code | Details                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
|------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| 200  | Response body <pre>{ "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvc2VvSUQi0jQsIIJvbGVJRC16MTAsIkFjV2Vz0xldmVsSUQi0jAsIk1zQmRtaW4iOnRydWUsIkxvZ21uSUQi0iI3MDM3MjY2OS0yMGYwLTQ5MGYtODg3My1kZTI5MTBkYWN1MjQ1LCJFYk1E1joiM0Qz0T1xM0JFRkFCRjREMTAwNDE2QzVBOURGNzN5RTA1LCJuYmYi0jE3NDM3NTkyOTYsImV4cCI6MTc0Mzc1OTg5NiwiaWF0IjoxNzQzNzU5Mjk2fQ.NhMDmtJF1q2aRIuHuI6115i_Vw5zzyMt3dBuxuRxANM", "refreshToken": "e24be7e53d5e4e20b2f715ea8fbe6e87", "expireTime": "2025-04-04T09:44:56.2247877Z" }</pre> <span>Download</span> |

Скопируйте значение **accessToken**. Вверху страницы нажмите кнопку **"Authorize"**. В открывшемся окне вставьте скопированный токен в поле для ввода, после чего нажмите кнопку **"Authorize"**.

![Screenshot of the Swagger UI interface for the IPS Server Web API. A modal window titled 'Available authorizations' is open, showing a 'Bearer (http, Bearer)' option with a JWT Bearer authentication scheme. A text input field contains the token 'eyJhbGciOiJIUzI1NiIsInR5cC'. Below the input are 'Authorize' and 'Close' buttons. The 'Authorize' button is highlighted with a red box. In the background, the main interface shows the API name 'IPS Server Web API' with version '1.0' and 'OAS3'. A 'Select a definition' dropdown is set to '1.0'. At the bottom right, there is a button labeled 'Authorize' with a lock icon, also highlighted with a red box.](07f537f57749b75157f742525e6a8dbc_img.jpg)

Screenshot of the Swagger UI interface for the IPS Server Web API. A modal window titled 'Available authorizations' is open, showing a 'Bearer (http, Bearer)' option with a JWT Bearer authentication scheme. A text input field contains the token 'eyJhbGciOiJIUzI1NiIsInR5cC'. Below the input are 'Authorize' and 'Close' buttons. The 'Authorize' button is highlighted with a red box. In the background, the main interface shows the API name 'IPS Server Web API' with version '1.0' and 'OAS3'. A 'Select a definition' dropdown is set to '1.0'. At the bottom right, there is a button labeled 'Authorize' with a lock icon, also highlighted with a red box.

Проверить, что **accessToken** используется в Swagger UI, можно, вызвав пример эндпоинта из пользовательского WebAPI-модуля MyIPSMModule.

![Screenshot of the API documentation interface for the 'MyIPSMModule' module. The interface shows a 'GET' request to '/samples/api/MyIPSMModule/version' and a 'POST' request to '/samples/api/MyIPSMModule/ping/{value}'. The 'Parameters' section includes a table with 'Name' and 'Description' columns. The 'value' parameter is a required string (path) with a value of 'test'. Below the parameters are 'Execute' and 'Clear' buttons. The 'Responses' section shows a 'Curl' command for the POST request, a 'Request URL' field with the URL 'http://localhost:8080/samples/api/MyIPSMModule/ping/test', and a 'Server response' section showing a 'Code' of 200 and a 'Response body' of '{\](967c30813761a8952ecc5e16bf42ea45_img.jpg)

Screenshot of the API documentation interface for the 'MyIPSMModule' module. The interface shows a 'GET' request to '/samples/api/MyIPSMModule/version' and a 'POST' request to '/samples/api/MyIPSMModule/ping/{value}'. The 'Parameters' section includes a table with 'Name' and 'Description' columns. The 'value' parameter is a required string (path) with a value of 'test'. Below the parameters are 'Execute' and 'Clear' buttons. The 'Responses' section shows a 'Curl' command for the POST request, a 'Request URL' field with the URL 'http://localhost:8080/samples/api/MyIPSMModule/ping/test', and a 'Server response' section showing a 'Code' of 200 and a 'Response body' of '{\

Запрос обработан успешно. Swagger UI корректно подставил **accessToken** в заголовок запроса

### Адаптация модулей расширения прежних версий IPS под новое IPS SDK

На сегодняшний день не существует каких-либо инструментов автоматической миграции исходного кода на новое IPS SDK для проектов модулей расширения, разработанных под прежние версии IPS. Всю работу приходится выполнять вручную.

Примерный план действий:

1. Создать новый пустой солюшен на базе шаблона "IPS SDK Empty Solution";
2. В новом солюшене создать заготовки всех проектов, как в существующем солюшене.  
Используйте шаблоны "IPS SDK Server Module" и "IPS SDK Client Module";
3. Для каждого нового проекта скопировать из существующего одноименного проекта все файлы исходного кода, ресурсов, ссылки на сборки IPS, настройки компиляции;
4. Скомпилировать и отладить проекты в новом солюшене.

## Разработка на Typescript

### Подготовка окружения разработчика на Typescript

На компьютере разработчика IPS должен быть установлен локально в варианте **только сервер**. Использование сетевых инсталляций IPS не поддерживается.

Все шаблоны проектов и примеры использования IPS SDK рассчитаны на то, что IPS установлен в расположение по умолчанию - **C:\Program Files\IPS**, и порты сервера приложений также оставлены по умолчанию - **8008** (для Remoting) и **8080** (для WebAPI). Если вы использовали другое расположение или

номера портов сервера приложений, то при создании проектов с помощью IPS SDK необходимо будет корректировать их настройки.

Основной IDE для разработки на Typescript является VSCode (Visual Studio Code), так как она является наиболее удобной.

Также можно использовать и Visual Studio, если используется гибридный солюшен, в котором серверные модули расширения на C# сочетаются с клиентскими модулями на Typescript. Но следует помнить, что отладка Typescript-кода в Visual Studio будет работать только в браузерах на основе Chrome, а отладка в Firefox работать не будет.

### Шаблоны проектов на Typescript

Для корректной работы IPS SDK требуется использовать только *специальные* шаблоны проектов на Typescript, разработанные в Intermech. Шаблоны проектов из VSCode, Visual Studio или npm работать не будут.

Специальные шаблоны проектов на Typescript сами являются частью IPS SDK, они устанавливаются и обновляются вместе с IPS. Как и в случае с проектами на C#, доступ к шаблонам на Typescript осуществляется через утилиту командной строки **dotnet**. Посмотреть список всех шаблонов и их краткие описания можно с помощью следующей команды в консоли ОС:

```
dotnet new list ipssdk -lang Typescript
```

![Screenshot of a Windows PowerShell window showing the command 'dotnet new list ipssdk -lang Typescript' and its output. The output is a table with columns: Template Name, Short Name, Language, and Tags. The table shows one entry: IPS SDK WebInterface Module, ipssdkwimodule, TypeScript, and IPS SDK/Web/WebInterface/Library.](2c9f15d775ecacb1c3cda931d1922206_img.jpg)

```
PS C:\Users\pmaximov> dotnet new list ipssdk -lang Typescript
These templates matched your input: 'ipssdk', -lang='Typescript'

Template Name                Short Name      Language    Tags
-----
IPS SDK WebInterface Module  ipssdkwimodule  TypeScript  IPS SDK/Web/WebInterface/Library

PS C:\Users\pmaximov>
```

При работе в Visual Studio доступ к шаблонам осуществляется через стандартный диалог создания нового проекта. Для этого введите в поле поиска текст **IPS SDK**, а язык разработки выберите **Typescript**:

![Screenshot of the 'Create a new project' page in Visual Studio. The 'IPS SDK' is selected in the search bar. The 'TypeScript' and 'All platforms' dropdowns are also selected. The 'Library' dropdown is selected, and it is circled in red. A list of project templates is shown below, with the first one, 'IPS SDK WebInterface Module (Intermech)', being highlighted.](0f2a1e4a7b12fe5b8749882ecd636f5c_img.jpg)

Screenshot of the 'Create a new project' page in Visual Studio. The 'IPS SDK' is selected in the search bar. The 'TypeScript' and 'All platforms' dropdowns are also selected. The 'Library' dropdown is selected, and it is circled in red. A list of project templates is shown below, with the first one, 'IPS SDK WebInterface Module (Intermech)', being highlighted.

### npm-пакеты для IPS SDK

Как это принято в мире Web-разработки, клиентское IPS API для Typescript поставляется в виде npm-пакетов. Они отличаются от традиционных пакетов тем, что репозиторием для npm-пакетов с IPS API является не <https://npmjs.com>, а сам сервер приложений IPS.

Вы можете увидеть все версии npm-пактов, поддерживаемые установленной версией IPS, открыв в любом браузере ссылку вида <http://localhost:8080/sdk/packages>. Если IPS установлен не в расположение по умолчанию или не с портами по умолчанию, то *скорректируйте имя сервера и порт*:

| <a href="#">im-plugins-core-1.0.734.tgz</a> | 185 994 | 11.11.2024 14:29:30 +00:00 |
|---------------------------------------------|---------|----------------------------|
| <a href="#">im-plugins-core-1.0.753.tgz</a> | 195 707 | 11.11.2024 14:29:30 +00:00 |
| <a href="#">im-plugins-core-1.0.802.tgz</a> | 197 565 | 12.11.2024 12:57:51 +00:00 |
| <a href="#">im-plugins-core-1.0.806.tgz</a> | 197 680 | 12.11.2024 13:09:05 +00:00 |
| <a href="#">im-webapi-1.0.374.tgz</a>       | 331 783 | 04.11.2024 10:54:32 +00:00 |
| <a href="#">im-webapi-1.0.458.tgz</a>       | 331 922 | 04.11.2024 21:09:52 +00:00 |
| <a href="#">im-webapi-1.0.540.tgz</a>       | 345 740 | 07.11.2024 9:18:28 +00:00  |
| <a href="#">im-webapi-1.0.617.tgz</a>       | 346 208 | 07.11.2024 18:08:27 +00:00 |
| <a href="#">im-webapi-1.0.806.tgz</a>       | 347 593 | 12.11.2024 13:09:19 +00:00 |

Итак, разработчику на Typescript под IPS требуется:

1. Доступ к <https://npmjs.com> - для получения пакетов typescript, webpack, react и т.д.
2. Доступ к работающему серверу приложений IPS - для получения пакетов IPS API.

### Создание клиентского модуля расширения для IPS WebInterface

Клиентский модуль расширения для IPS WebInterface - это аналог клиентского модуля на C#, загружаемого в IMClient. С его помощью вы сможете:

- добавлять новые команды для объектов IPS или узлов навигатора на панели инструментов и в контекстные меню;
- создавать новые виды навигатора;
- изменять или расширять поведение мастера создания объектов;
- обрабатывать события в UI или в клиентской бизнес-логике.

В клиентских модулях для IPS WebInterface для доступа к серверу приложений IPS вместо Remoting используется новое WebAPI.

Рассмотрим создание нового проекта в среде VSCode и Visual Studio.

**Внимание!** Не используйте пробелы в именах создаваемых проектов или папок на диске. Из-за ошибки в .NET SDK создание проектов будет работать неправильно.

### **VSCode**

При работе в VSCode:

1. Запустить сервер приложений IPS, если это еще не было сделано;
2. Создать папку проекта на локальном диске. Имя папки будет являться и именем проекта, поэтому не используйте пробелы в имени. Например, пусть это будет папка **my-wi-module** по пути `c:\src\my-wi-module`;
3. Запустить VSCode, если это еще не было сделано. Все дальнейшие действия выполняются в VSCode;
4. Открыть вкладку **Terminal** с помощью команды **View/Terminal**;
5. На вкладке **Terminal** выполнить следующие команды для создания проекта:

```
cd c:\src\my-wi-module  
dotnet new ipssdkwimodule
```

6. Открыть файл **my-wi-module.code-workspace** по пути `c:\src\my-wi-module` с помощью команды **File/Open Workspace from File**;
7. Если сервер приложений IPS установлен и запущен не в расположении по умолчанию (т.е. <http://localhost:8080>), то необходимо сделать следующее:
  - открыть файл **package.json** и исправить ссылки на npm-пакеты IPS SDK в tarball-формате. Такие ссылки начинаются на <http://localhost:8080/sdk/packages>

- открыть файл **.vscode/launch.json** и исправить ссылки на сервер приложений IPS в конфигурациях отладчика Typescript

#### 8. Перейти на вкладку **Terminal** и выполнить команду установки необходимых пакетов прт:

```
npm install
```

#### 9. Выполнить команду **build**, чтобы убедиться, что проект успешно собирается. Сделать это можно либо с помощью таба **Explorer/NPM Scripts**, либо на вкладке **Terminal** с помощью команды:

```
npm run build
```

Обратите внимание, что хотя проект **my-wi-module** и был создан в среде VSCode, с ним можно работать и в Visual Studio. Для этого следует открыть файл **my-wi-module.esproj** по пути `c:\src\my-wi-module` с помощью команды **File/Open/Project/Solution**.

![Screenshot of a Windows file explorer window showing the 'src' directory. The 'my-wi-module' file is selected. Red circles highlight the 'Новая папка' (New folder) button in the toolbar and the 'my-wi-module' file in the directory list.](ace13edeb79bdfa129ed84fbb4ac44e5_img.jpg)

Screenshot of a Windows file explorer window showing the 'src' directory. The 'my-wi-module' file is selected. Red circles highlight the 'Новая папка' (New folder) button in the toolbar and the 'my-wi-module' file in the directory list.

![Screenshot of Visual Studio Code interface showing the 'View' menu open with the 'Terminal' option highlighted by a red circle. The 'Terminal' submenu is also open, showing 'Terminal' (Ctrl+`), 'Word Wrap', and 'Alt+Z'.](0a5acc4b370bb711096e04f25b8b3feb_img.jpg)

Screenshot of Visual Studio Code interface showing the 'View' menu open with the 'Terminal' option highlighted by a red circle. The 'Terminal' submenu is also open, showing 'Terminal' (Ctrl+`), 'Word Wrap', and 'Alt+Z'.

![Screenshot of Visual Studio Code showing the 'Welcome' page. The 'TERMINAL' tab is selected and active, showing a command prompt window. The user has run 'dotnet new ipssdk' and the output indicates a successful creation of the 'IPSSDK Webinterface Module'.](15ee1fd7e4011d0d5dcb11b291fb91d7_img.jpg)

The screenshot shows the Visual Studio Code interface. The 'Welcome' page is visible on the left, and the 'TERMINAL' tab is selected and active. The terminal output is as follows:

```
PS C:\Users\pmaximov> cd c:\src\my-wi-module
PS C:\src\my-wi-module> dotnet new ipssdkmodule
The template "IPSSDK Webinterface Module" was created successfully.
PS C:\src\my-wi-module>
```

Screenshot of Visual Studio Code showing the 'Welcome' page. The 'TERMINAL' tab is selected and active, showing a command prompt window. The user has run 'dotnet new ipssdk' and the output indicates a successful creation of the 'IPSSDK Webinterface Module'.

![Screenshot of Visual Studio Code with the File menu open. The 'Open Workspace from File...' option is highlighted with a red circle. In the background, a terminal window shows the command 'npm run build' and the output 'dist/index.html was created successfully.'](349cffebeefaae56d9034d3fe65bf7c6_img.jpg)

Screenshot of Visual Studio Code with the File menu open. The 'Open Workspace from File...' option is highlighted with a red circle. In the background, a terminal window shows the command 'npm run build' and the output 'dist/index.html was created successfully.'

![Screenshot of the 'Open Workspace from File' dialog box in Visual Studio. The path bar shows 'M:\ > SYSTEM (C:) > src > my-wi-module >'. The file list shows 'my-wi-module.code-workspace' selected. Red circles highlight the path bar and the selected file. At the bottom, the 'File name' field contains 'my-wi-module.code-workspace', and the 'Open' button is circled in red.](2224b82995b3ba31aa82f20fb06f4c9c_img.jpg)

Screenshot of the 'Open Workspace from File' dialog box in Visual Studio. The path bar shows 'M:\ > SYSTEM (C:) > src > my-wi-module >'. The file list shows 'my-wi-module.code-workspace' selected. Red circles highlight the path bar and the selected file. At the bottom, the 'File name' field contains 'my-wi-module.code-workspace', and the 'Open' button is circled in red.

![Screenshot of a Visual Studio Code window showing the 'package.json' file in the 'my-wi-module' workspace. The 'package.json' file is open in the editor, and the 'dependencies' section is visible. The 'im-plugins-core' dependency is highlighted with a red box. The 'EXPLORER' tab is also circled in red.](ded1520e45c0c3e4c46c58984602bf0e_img.jpg)

Screenshot of a Visual Studio Code window showing the 'package.json' file in the 'my-wi-module' workspace. The 'package.json' file is open in the editor, and the 'dependencies' section is visible. The 'im-plugins-core' dependency is highlighted with a red box. The 'EXPLORER' tab is also circled in red.

![Screenshot of Visual Studio Code showing the launch.json file for a my-wi-module workspace. The file contains a JSON object with 'version' set to '0.2.0' and 'configurations' containing three entries: 'chrome', 'launch', and 'firefox'. The 'url' field in each configuration is set to 'http://localhost:8080', which is highlighted with a red underline. The 'workspaceFolder' field is set to '${workspaceFolder}'. A red circle highlights the 'launch.json' file in the Explorer sidebar. A red circle also highlights the 'launch' configuration in the JSON object. A button 'Add Configuration...' is visible at the bottom right of the editor area.](b8d3d85b1575ec6abdcf1bd9ce438701_img.jpg)

Screenshot of Visual Studio Code showing the launch.json file for a my-wi-module workspace. The file contains a JSON object with 'version' set to '0.2.0' and 'configurations' containing three entries: 'chrome', 'launch', and 'firefox'. The 'url' field in each configuration is set to 'http://localhost:8080', which is highlighted with a red underline. The 'workspaceFolder' field is set to '\${workspaceFolder}'. A red circle highlights the 'launch.json' file in the Explorer sidebar. A red circle also highlights the 'launch' configuration in the JSON object. A button 'Add Configuration...' is visible at the bottom right of the editor area.

![Screenshot of Visual Studio Code showing the 'my-wi-module (Workspace)' project. The 'index.ts' file is open in the editor, and the 'npm install' command is being run in the integrated terminal. A red circle highlights the 'npm install' command in the terminal.](662933fe00899933b36b5a214ac6095c_img.jpg)

The screenshot shows the Visual Studio Code interface. The project name is 'my-wi-module (Workspace)'. The file explorer on the left shows the 'MY-WI-MODULE (WORKSPACE)' directory with various files like 'index.ts', 'package-lock.json', and 'tsconfig.json'. The editor area shows the 'index.ts' file with some code. The terminal at the bottom shows the command 'npm install' being run, which is highlighted with a red circle. The status bar at the bottom indicates the file is 'index.ts' in the 'my-wi-module' directory.

Screenshot of Visual Studio Code showing the 'my-wi-module (Workspace)' project. The 'index.ts' file is open in the editor, and the 'npm install' command is being run in the integrated terminal. A red circle highlights the 'npm install' command in the terminal.

![Screenshot of the Visual Studio Code interface showing the Explorer, Terminal, and a red circle around the 'Run' button in the package.json context menu.](83926108dd52e3998fda7a99a91cdf3b_img.jpg)

The screenshot shows the Visual Studio Code interface. On the left is the Explorer pane with the 'my-wi-module' workspace selected. The 'index.ts' file is open in the editor. The terminal at the bottom shows the command 'npm audit' and its output. A red circle highlights the 'Run' button in the context menu for the 'package.json' file.

Screenshot of the Visual Studio Code interface showing the Explorer, Terminal, and a red circle around the 'Run' button in the package.json context menu.

![Screenshot of Visual Studio Code showing the 'my-wi-module' workspace. The 'index.ts' file is open in the editor, and the 'TERMINAL' panel is open at the bottom. A red circle highlights the 'TERMINAL' tab in the sidebar. The terminal shows the command 'npm run build' being executed in the 'my-wi-module' directory. The output shows the bundle size and a message that the terminal will be reused by tasks.](555df5c0300cb1fca5dc028fec5ec6be_img.jpg)

The screenshot shows the Visual Studio Code interface. In the Explorer pane on the left, the 'my-wi-module (WORKSPACE)' is selected, and the 'index.ts' file is open. The editor shows the following code:

```
1 import { in } from "in-plugins-core";
2 import React from "react";
3
4 /*
5 Для отладки этого модуля нужно в IPS WebInterface открыть окно Настройки/Параметры IPS,
6 перейти в дереве к элементу Пользователи/Текущий пользователь/Отладка загруженных модулей и
7 добавить url http://localhost:3991/bundle.js, включить флажок загрузки модуля и обновить
8 вкладку браузера по F5
9 */
10
11 in.Plugins.register(() => {
12   return {
13     name: "my-wi-module",
14     extensions: [],
15   };
16 });
17
18
```

In the Terminal panel at the bottom, the command `npm run build` is being executed. The output shows:

```
> my-wi-module@0.0.0 build
> webpack --mode production

asset bundle.js 23.2 KiB [compared for emit] [minimized] (name: main) 1 re
orphan modules 64.7 KiB [perman] 20 modules
./index.ts + 62 modules 64.6 KiB [built] [code generated]
webpack 5.9.5 compiled successfully in 711 ms

Terminal will be reused by tasks, press any key to close it.
```

A red circle highlights the 'TERMINAL' tab in the sidebar, indicating that the terminal is open and active.

Screenshot of Visual Studio Code showing the 'my-wi-module' workspace. The 'index.ts' file is open in the editor, and the 'TERMINAL' panel is open at the bottom. A red circle highlights the 'TERMINAL' tab in the sidebar. The terminal shows the command 'npm run build' being executed in the 'my-wi-module' directory. The output shows the bundle size and a message that the terminal will be reused by tasks.

## Visual Studio

При работе в Visual Studio:

1. Запустить сервер приложений IPS, если это еще не было сделано;
2. Запустить Visual Studio, если это еще не было сделано;
3. Открыть существующий солюшен или создать новый на базе шаблона **IPS SDK Empty Solution**;
4. Открыть мастер добавления нового проекта с помощью команды **Add/New Project** в дереве солюшена;
5. Выбрать шаблон **IPS SDK WebInterface Module** и указать имя проекта. Не используйте пробелы в имени проекта. Например, пусть это будет **my-wi-module**;
6. Если сервер приложений IPS установлен не в расположении по умолчанию (т.е. <http://localhost:8080>), то необходимо сделать следующее:
  - открыть файл **package.json** и исправить ссылки на npm-пакеты IPS SDK в tarball-формате. Такие ссылки начинаются на <http://localhost:8080/sdk/packages>
  - открыть файл **.vscode/launch.json** и исправить ссылки на сервер приложений IPS в конфигурациях отладчика Typescript
7. Выполнить команду **Project/Build**, чтобы убедиться, что проект успешно собирается.

Обратите внимание, что хотя проект **my-wi-module** и был создан в среде Visual Studio, с ним можно работать и в VSCode. Для этого следует открыть файл **my-wi-module.code-workspace** по пути `c:\src\my-wi-module` с помощью команды **File/Open Workspace from File**.

![Screenshot of the 'Create a new project' page in the Azure portal. The 'IPS SDK' is selected in the search bar. The 'Solution' tab is active under 'All platforms'. Three project templates are listed: 'IPS SDK Empty Solution (Intermech)', 'IPS SDK WebAPI Solution (Intermech)', and 'Blank Solution'. The first two are highlighted with orange boxes and 'C#' is shown. The third is a 'Blank Solution'. A 'Next' button is at the bottom right, circled in red. WebInterface Module icon IPS SDK Empty Solution icon Blank Solution icon IPS SDK Client Module icon IPS SDK Server Module icon WebInterface Module icon](ac852a162572ca8a8c8478c49b571af5_img.jpg)

Create a new project

Recent project templates

|                                    |            |
|------------------------------------|------------|
| WebInterface Module (Intermech)    | TypeScript |
| IPS SDK Empty Solution (Intermech) | C#         |
| Blank Solution                     |            |

Other results based on your search

|                                                                                                    |                           |
|----------------------------------------------------------------------------------------------------|---------------------------|
| <b>IPS SDK</b> Client Module (Intermech)<br>A project template for creating a custom client module | C# <b>IPS SDK</b> Library |
| <b>IPS SDK</b> Server Module (Intermech)<br>A project template for creating a custom server module | C# <b>IPS SDK</b> Library |
| <b>IPS SDK</b> WebInterface Module (Intermech)                                                     |                           |

Next

Screenshot of the 'Create a new project' page in the Azure portal. The 'IPS SDK' is selected in the search bar. The 'Solution' tab is active under 'All platforms'. Three project templates are listed: 'IPS SDK Empty Solution (Intermech)', 'IPS SDK WebAPI Solution (Intermech)', and 'Blank Solution'. The first two are highlighted with orange boxes and 'C#' is shown. The third is a 'Blank Solution'. A 'Next' button is at the bottom right, circled in red. WebInterface Module icon IPS SDK Empty Solution icon Blank Solution icon IPS SDK Client Module icon IPS SDK Server Module icon WebInterface Module icon

![Screenshot of the 'Configure your new project' dialog box in Visual Studio. The 'Project name' field contains 'wi-tutorial' and the 'Location' field contains 'C:\src'. Both fields are circled in red. The 'Solution name' is 'wi-tutorial' and the 'Place solution and project in the same directory' checkbox is checked. At the bottom right, there are 'Back' and 'Create' buttons, with the 'Create' button circled in red.](7b1cea8c45c49887a750926f2135fa84_img.jpg)

Screenshot of the 'Configure your new project' dialog box in Visual Studio. The 'Project name' field contains 'wi-tutorial' and the 'Location' field contains 'C:\src'. Both fields are circled in red. The 'Solution name' is 'wi-tutorial' and the 'Place solution and project in the same directory' checkbox is checked. At the bottom right, there are 'Back' and 'Create' buttons, with the 'Create' button circled in red.

![Screenshot of the Visual Studio interface showing the Solution Explorer and the New Project dialog. The 'wi-tutorial' project is selected in the Solution Explorer, and the 'New Project...' option is highlighted in the dialog. A tooltip for the 'wi-tutorial' project shows 'Solution Properties' and 'Name: wi-tutorial'.](93afce28d7dec5b2202789b31b4ef8ab_img.jpg)

The screenshot shows the Visual Studio IDE with the 'wi-tutorial' project selected in the Solution Explorer. The 'New Project...' option is highlighted in the dialog, and a tooltip for the 'wi-tutorial' project shows 'Solution Properties' and 'Name: wi-tutorial'.

Screenshot of the Visual Studio interface showing the Solution Explorer and the New Project dialog. The 'wi-tutorial' project is selected in the Solution Explorer, and the 'New Project...' option is highlighted in the dialog. A tooltip for the 'wi-tutorial' project shows 'Solution Properties' and 'Name: wi-tutorial'.

![Screenshot of the 'Create a new project' dialog box in Visual Studio. The 'IPS SDK' is selected in the 'Type' dropdown. The 'All platforms' radio button is selected. The 'Library' tab is active, showing a list of project templates. The 'IPS SDK WebInterface Module (Intermehc)' template is highlighted. A red circle is drawn around the 'IPS SDK' text in the template name. At the bottom right, a 'Next' button is visible, also circled in red.](6cc4a2d5ea0462e4825d57bd689bd2b3_img.jpg)

Create a new project

IPS SDK

Recent project templates

TypeScript

All platforms

Library

IPS SDK WebInterface Module (Intermehc)  
A project template for creating a custom **IPS** WebInterface client module

TypeScript **IPS SDK** Library Web WebInterface

Other results based on your search

**IPS SDK** Client Module (Intermehc)  
A project template for creating a custom client module

C# **IPS SDK** Library

**IPS SDK** Empty Solution (Intermehc)  
A solution template for creating custom server and client modules

C# **IPS SDK** Solution

**IPS SDK** Server Module (Intermehc)  
A project template for creating a custom server module

C# **IPS SDK** Library

**IPS SDK** WebAPI Solution (Intermehc)

A project template for creating a custom server and client module.

Next

Screenshot of the 'Create a new project' dialog box in Visual Studio. The 'IPS SDK' is selected in the 'Type' dropdown. The 'All platforms' radio button is selected. The 'Library' tab is active, showing a list of project templates. The 'IPS SDK WebInterface Module (Intermehc)' template is highlighted. A red circle is drawn around the 'IPS SDK' text in the template name. At the bottom right, a 'Next' button is visible, also circled in red.

![Screenshot of the 'Configure your new project' dialog box for the 'IPS SDK WebInterface Module (Intermech)'. The 'Project name' field contains 'my-wi-module' and is circled in red. The 'Location' field contains 'C:\src\wi-tutorial' and has a dropdown arrow. At the bottom right, there are 'Back' and 'Create' buttons, with the 'Create' button circled in red.](41245ac07db266bea228735b9e8c8b73_img.jpg)

Screenshot of the 'Configure your new project' dialog box for the 'IPS SDK WebInterface Module (Intermech)'. The 'Project name' field contains 'my-wi-module' and is circled in red. The 'Location' field contains 'C:\src\wi-tutorial' and has a dropdown arrow. At the bottom right, there are 'Back' and 'Create' buttons, with the 'Create' button circled in red.

![Screenshot of a web browser showing the package.json file for a React project. The file is opened in the VS Code editor. The 'dependencies' section is visible, and the 'react' dependency is highlighted with a red underline and a red box, showing the version '18.2.0'.](53cb558495f8cc93c4202314ef492966_img.jpg)

The screenshot shows a web browser window with the URL `https://pouchesstore.one/package.json`. The file is a JSON object representing the project's dependencies. The 'dependencies' section lists several packages, including 'react' at version '18.2.0', which is highlighted with a red box. The 'react-dom' package is also listed at version '18.2.0'.

```
{
  "name": "my-ui-module",
  "version": "1.0.8",
  "description": "",
  "private": true,
  "scripts": {
    "start": "webpack serve --mode development",
    "build": "webpack --mode production"
  },
  "keywords": [],
  "author": "",
  "license": "ISC",
  "devDependencies": {
    "@mmmmma/react-refresh-webpack-plugin": "^0.5.11",
    "@types/react": "^18.2.51",
    "@types/react-dom": "^18.2.18",
    "esbuild-loader": "^4.0.3",
    "webpack": "^5.90.1",
    "webpack-cli": "^5.1.4",
    "webpack-dev-server": "^4.15.1"
  },
  "dependencies": {
    "im-plugins-react": "http://localhost:8080/sd/packages/im-plugins-react-i-0.634.tar.gz",
    "react": "18.2.0",
    "react-dom": "18.2.0"
  }
}
```

Screenshot of a web browser showing the package.json file for a React project. The file is opened in the VS Code editor. The 'dependencies' section is visible, and the 'react' dependency is highlighted with a red underline and a red box, showing the version '18.2.0'.

![Screenshot of a Visual Studio Code window showing a JSON file named 'launch.json' in the 'Workspace' tab. The file contains a JSON object with a 'version' of '0.2.0' and a 'configurations' array with two objects: 'chrome' and 'firefox'. The 'chrome' configuration has 'request' set to 'launch', 'name' to 'Launch Chrome against localhost', and 'url' to 'http://localhost:8080', which is underlined in red. The 'firefox' configuration has 'request' set to 'launch', 'name' to 'Launch Firefox against localhost', and 'url' to 'http://localhost:8080', also underlined in red. Both configurations have 'webroot' set to '${workspaceFolder}', 'rebuild' set to true, and 'prettify' set to true. The 'Solution Explorer' on the right shows the project structure, and the 'Properties' panel at the bottom right shows the 'Launch' configuration selected.](c3ee41c6d46565ab4198d0a9c69108c5_img.jpg)

Screenshot of a Visual Studio Code window showing a JSON file named 'launch.json' in the 'Workspace' tab. The file contains a JSON object with a 'version' of '0.2.0' and a 'configurations' array with two objects: 'chrome' and 'firefox'. The 'chrome' configuration has 'request' set to 'launch', 'name' to 'Launch Chrome against localhost', and 'url' to 'http://localhost:8080', which is underlined in red. The 'firefox' configuration has 'request' set to 'launch', 'name' to 'Launch Firefox against localhost', and 'url' to 'http://localhost:8080', also underlined in red. Both configurations have 'webroot' set to '\${workspaceFolder}', 'rebuild' set to true, and 'prettify' set to true. The 'Solution Explorer' on the right shows the project structure, and the 'Properties' panel at the bottom right shows the 'Launch' configuration selected.

![Screenshot of the PowerShell ISE interface showing the build of a my-ui-module. The top pane shows the file structure with 'my-ui-module' selected. The middle pane shows the code for 'my-ui-module' with a red circle around the 'Build my-ui-module' option in the context menu. The bottom pane shows the build output, which was successful, taking 19.518 seconds.](f7bc9b0327ed4589a3faf9a7b3c92712_img.jpg)

The screenshot shows the PowerShell ISE interface. At the top, the file explorer shows the project structure. The 'my-ui-module' folder is selected. In the middle pane, the code for 'my-ui-module' is visible. A context menu is open on the 'my-ui-module' file, and the 'Build my-ui-module' option is highlighted with a red circle. The bottom pane shows the build output, which was successful, taking 19.518 seconds. The output also shows that the build started at 17:16 and completed at 17:17.

Screenshot of the PowerShell ISE interface showing the build of a my-ui-module. The top pane shows the file structure with 'my-ui-module' selected. The middle pane shows the code for 'my-ui-module' with a red circle around the 'Build my-ui-module' option in the context menu. The bottom pane shows the build output, which was successful, taking 19.518 seconds.

## Отладка

В режиме отладки плагин работает как отдельное приложение, с которым IPS WebInterface взаимодействует по сети, используя адрес <http://localhost:3091/bundle.js>. Используемый для отладки

порт **3091** не является жестко зафиксированным, и, при необходимости, порт можно изменить в файле **webpack.config.js**.

### Запуск отладочного сервера

Первым шагом для отладки плагина всегда будет запуск отладочного сервера webpack с помощью команды **serve**.

При работе в **VSCode** это можно сделать либо с помощью таба **Explorer/NPM Scripts**, либо на вкладке **Terminal** с помощью команды:

```
npm run serve
```

При работе в **Visual Studio** вкладку **Terminal** для плагина можно открыть с помощью контекстного меню проекта плагина в дереве солюшена, выбрав пункт **Open in Terminal**, а затем выполнить приведенную выше команду.

### Запуск браузера в режиме отладки

Теперь можно запустить браузер в режиме отладки кода плагина. При работе в **VSCode** воспользуйтесь табом **Run**, в поле **Run and Debug** выберите профиль отладки для вашего браузера. При работе в **Visual Studio** выберите профиль отладки для вашего браузера на панели инструментов **Standard**.

В результате запустится выбранный браузер, в котором будет открыт IPS WebInterface.

При отладке точки останова (breakpoints) можно расставлять непосредственно в IDE. Но также можно использовать и Dev Tools самого браузера.

![Screenshot of two side-by-side windows showing the debugging process in Visual Studio Code and Visual Studio 2022. The left window is VS Code with a file editor showing JavaScript code and a terminal. The right window is Visual Studio 2022 with a file editor showing the same code, a Solution Explorer, and a PowerShell terminal. Red circles and arrows highlight the 'Launch Chrome against localhost' button in VS Code and the 'Launch Chrome against localhost' button in Visual Studio. A red dot is also placed on the 'im.Plugins.register()' function in the Visual Studio code editor.](267996f84ec3febbb1e5fda4e0e91756_img.jpg)

Screenshot of two side-by-side windows showing the debugging process in Visual Studio Code and Visual Studio 2022. The left window is VS Code with a file editor showing JavaScript code and a terminal. The right window is Visual Studio 2022 with a file editor showing the same code, a Solution Explorer, and a PowerShell terminal. Red circles and arrows highlight the 'Launch Chrome against localhost' button in VS Code and the 'Launch Chrome against localhost' button in Visual Studio. A red dot is also placed on the 'im.Plugins.register()' function in the Visual Studio code editor.

### Однократная настройка IPS WebInterface для отладки плагина

Также нужно однократно выполнить настройку IPS WebInterface:

1. В главном меню выбрать **Настройка → Параметры IPS**.
2. В папке **Пользователи** выбрать **Текущий пользователь → Отладка загружаемых модулей**.
3. С помощью кнопки **+** добавить пользовательский плагин, в поле **URL загружаемого модуля** ввести адрес <http://localhost:3091/bundle.js> и поставить флажок в колонке **Статус**.
4. Нажать кнопку **Применить**.

5. Перезагрузить страницу. В консоли браузера появится следующее сообщение:

![Screenshot of a browser developer tools console showing a warning about a custom plugin loading. The warning message is in Russian: 'Для текущего браузера настроена загрузка дополнительных плагинов'. The plugin object is an array with one element. The element has a 'url' property set to 'http://localhost:3091/bundle.js' and a 'length' of 1. The console also shows the prototype of the object as 'Object' and the prototype of the array as 'Array(0)'.](b0d9faaee53565a4a333764297aa7b97_img.jpg)

Screenshot of a browser developer tools console showing a warning about a custom plugin loading. The warning message is in Russian: 'Для текущего браузера настроена загрузка дополнительных плагинов'. The plugin object is an array with one element. The element has a 'url' property set to 'http://localhost:3091/bundle.js' and a 'length' of 1. The console also shows the prototype of the object as 'Object' and the prototype of the array as 'Array(0)'.