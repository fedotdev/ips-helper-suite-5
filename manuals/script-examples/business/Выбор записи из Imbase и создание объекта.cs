using System;
using System.Threading;
using System.Data;
using System.Xml;
using System.Collections.Generic;
using Intermech.Interfaces;
using Intermech.Interfaces.Client;
using Intermech.Interfaces.Workflow;
using Intermech.Interfaces.Imbase;
using Intermech.Imbase;
using Intermech.Imbase.Selection;
using Intermech.Workflow;
using System.Windows.Forms;

public class Script
{
	/// <summary>
	/// Основной метод
	/// </summary>
	/// <param name="activity"></param>
	public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(IActivity activity)
	{
		// запись - Класс, Размеры и параметры, ГОСТ
		string RecordText;
		// id ярлыка таблицы
		long LinkID;
		// id записи в таблице
		long RecordID;
		// путь к ярлыку
		string PathVal;
		// ключ выбранной записи
		string ImKey = SelectImbaseRecord(out RecordText, out LinkID,out RecordID, out PathVal);
		
		// если отказались от выбора
		if (ImKey.Equals(String.Empty)) throw new NotificationException("Вы не выбрали запись из каталога! Выберите запись и нажмите ОК!");
		
		// сервис Imbase
		IImbaseServer imbaseServer = activity.Session.GetCustomService(typeof(IImbaseServer)) as IImbaseServer;
		// созданный по записи объект
		Int64 objectId = imbaseServer.CreateObject(activity.Session.SessionGUID, -1, LinkID, RecordID, true, Intermech.Consts.NoType);
		
		// присвоение данных заявке НСИ
		SetData(activity, PathVal, activity.Session.GetObject(objectId).Caption, objectId);
		
		// получем сервис для отправки сообщений
		IRouterService router = activity.Session.GetCustomService(typeof(IRouterService)) as IRouterService;
		
		// ID пользователя, кому отправлять - инициатор процесса
		long toUserID = activity.Process.StartActivity.ParticipantID;
		// ID пользователя, от кого отправлять
		long fromUserID = activity.Session.UserID;
		// тема сообщения
		string subject = String.Format("Заявка на ввод данных НСИ {0}", activity.Attachments[0].Object.Caption);
		// текст сообщения
		string text = String.Format("По Вашей заявке <a href=\"#object={0}\">{1}</a> был создан объект: <a href=\"#object={2}\">{3}</a>", activity.Attachments[0].ObjectID, activity.Attachments[0].Object.Caption, objectId, activity.Session.GetObject(objectId).Caption);
		
		// отправка ссобщения инициатору
		router.CreateMessage(activity.Session.SessionGUID, toUserID, subject, text, fromUserID);
	}
	
	/// <summary>
	/// Запись атрибутов для заявки НСИ
	/// </summary>
	/// <param name="activity">действие</param>
	/// <param name="Path">путь к ярлыку Imbase</param>
	/// <param name="Val">значение из таблицы</param>
	/// <param name="objID">id созданного объекта</param>
	private static void SetData(IActivity activity, string Path, string Val, long objID)
	{
		if (activity.Attachments.Count == 1 && activity.Attachments[0].Object.ObjectType == MetaDataHelper.GetObjectTypeID(new Guid( "861840b7-9334-41d0-84a5-a3c0d2e27ac5" /*Заявки на ввод/изменение НСИ*/)))
		{
			activity.Attachments[0].Object.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID("dd5d2493-ec3c-4487-ade8-5c19ff44d9ba" /*Оператор НСИ*/), false).Value = activity.ParticipantID;
			activity.Attachments[0].Object.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID("8563712c-474b-4b96-91d9-14d8808b1ece" /*Полный путь НСИ*/), false).Value = Path;
			activity.Attachments[0].Object.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID("60320b60-4d30-4340-8be5-dca5a1140b8b" /*Запись НСИ*/), false).Value = Val;
			activity.Attachments[0].Object.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID( "6759eebf-5e16-4697-a908-51642d086dc4" /*Ссылка на объект НСИ*/), false).Value = objID;
		}
	}
	
	/// <summary>
	/// Выбор записи из Imbase
	/// </summary>
	/// <param name="RecordText">текст записи</param>
	/// <param name="LinkId">id ярлыка таблицы</param>
	/// <param name="RecordID">id записи таблицы</param>
	/// <param name="PathVal">путь к ярлыку таблицы</param>
	/// <returns></returns>
	private static string SelectImbaseRecord(out string RecordText, out long LinkId, out long RecordID, out string PathVal)
	{
		string ImKey = String.Empty;
		
		RecordText = String.Empty;
		LinkId = Intermech.Consts.UnknownObjectId;
		RecordID = 0;
		PathVal = String.Empty;
		
		//Вставьте ваш код сценария здесь
		using (SessionKeeper sk = new SessionKeeper())
		{
			// получаем ссылку на интерфейс IImbaseSelector (реализован в клиентском плагине), в котором доступен метод для выбора из Imbase
			IImbaseSelector selector = ServicesManager.GetService(typeof(IImbaseSelector)) as IImbaseSelector;
			
			// покажем окно выбора Imbase
			ImKey = selector.SelectRecord("", false);
			
			// если не выбрали запись - возврат
			if (ImKey.Equals(String.Empty)) return ImKey;
			
			// из ключа Imbase извлечем id объекта ярлык таблицы Imbase, из которого выбрана запись
			String linkIdStr = ImKey.Substring(2, ImKey.IndexOf(".") - 2);
			LinkId = Convert.ToInt64(linkIdStr);
			// из ключа Imbase извлечем номер записи в таблице
			String recIdStr = ImKey.Substring(ImKey.IndexOf(".") + 1, ImKey.Length - ImKey.IndexOf(".") - 1);
			RecordID = Convert.ToInt64(recIdStr); ;
			
			// для получения данных из таблицы по ключу Imbase, запросим ссылку на интерфейс IImbaseServer (реализован в серверном плагине)
			IImbaseServer server = sk.Session.GetCustomService(typeof(IImbaseServer)) as IImbaseServer;
			
			// получаем данные о папках, в которых находиться таблица, в виде DataTable 
			DataTable dt = server.GetFoldersForObjects(sk.Session.SessionGUID, new long[] { LinkId }, null);
			// сортируем
			dt.DefaultView.Sort = "F_PATH ASC";
			dt = dt.DefaultView.ToTable();
			
			List<string> lst = new List<string>();
			// заполняем List значениями из колонки CAPTION
			foreach (DataRow r in dt.Rows)
			{
				lst.Add((string)r["CAPTION"]);
			}
			// преобразовываем list в string и получаем полный путь к таблице
			PathVal = string.Join("/", lst);
			
			AttributeTypeProperties[] atts = null;
			DataTable recsTable = null;
			// формируем фильтр для таблицы по выбранной записи
			string filter = string.Format("[-2]={0}", RecordID.ToString());
			ImbaseKeyInfo ki = new ImbaseKeyInfo(-1);
			// полуаем данные из таблицы
			server.LoadRecords(sk.Session.SessionGUID, LinkId, filter, Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator, out recsTable, out atts, out ki);
			
			string attrclassstr = "", attrnaimstr = "", atrgoststr = "";
			
			foreach (AttributeTypeProperties at in atts)
			{
				// получаем id колонки таблицы, где храниться поле Класс
				if (at.Name.ToUpper() == "Класс".ToUpper())
				{
					int atrclassid = recsTable.Columns.IndexOf(Convert.ToString(at.AttributeID));
					if (atrclassid >= 0)
					{
						// получаем значение поля Класс
						attrclassstr = Convert.ToString(recsTable.Rows[0][atrclassid]);
					}
				}
				
				if (at.Name.ToUpper() == "Размеры и параметры".ToUpper())
				{
					// получаем id колонки таблицы, где храниться поле Размеры и параметры
					int atrnaimid = recsTable.Columns.IndexOf(Convert.ToString(at.AttributeID));
					if (atrnaimid >= 0)
					{
						// получаем значение поля Размеры и параметры
						attrnaimstr = Convert.ToString(recsTable.Rows[0][atrnaimid]);
					}
				}
				
				if (at.Name.ToUpper() == "Гост".ToUpper())
				{
					// получаем id колонки таблицы, где храниться поле Гост
					int atrgostid = recsTable.Columns.IndexOf(Convert.ToString(at.AttributeID));
					if (atrgostid >= 0)
					{
						// получаем значение поля Гост
						atrgoststr = Convert.ToString(recsTable.Rows[0][atrgostid]);
					}
				}
				
			}
			
			// складываем полученные значения полей таблицы
			RecordText = attrclassstr + " " + attrnaimstr + " " + atrgoststr;
			
		}
		return ImKey;
	}
	
}