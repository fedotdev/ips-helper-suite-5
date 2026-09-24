using System;
using Intermech.Interfaces;
using Intermech.Interfaces.Workflow;
using Intermech.Interfaces.Client;

public class Script
{
    public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(IActivity activity)
    {
        // Тема письма
        string subject = String.Empty;
        IVariable v = activity.Variables.Find("SUBJECT");
        if (v != null)
            subject = v.Value;
        // Текст письма
        string message = String.Empty;
        v = activity.Variables.Find("MESSAGE");
        if (v != null)
            message = v.Value;
        // Список адресатов
        string toEmail = String.Empty;
        v = activity.Variables.Find("TO_EMAIL");
        if (v != null)
            toEmail = v.Value;
        // От кого
        string fromEmail = String.Empty;
        v = activity.Variables.Find("FROM_EMAIL");
        if (v != null)
            fromEmail = v.Value;
        // Список индексов файлов документа для вложения в письмо
        int[] fileIndexes = null;
        // Идентификатор документа, у которого берутся файлы для вложения в письмо
        Int64 documentID = Intermech.Consts.UnknownObjectId;
        // Если есть вложения
        if (activity.Attachments != null && activity.Attachments.Count > 0)
        {
            v = activity.Variables.Find("ATTACHMENT_FILEINDEXES");
            if (v != null && v.Value != String.Empty)
            {
                string[] strIndexes = v.Value.Split(';');
                fileIndexes = new int[strIndexes.Length];
                for (int i = 0; i < strIndexes.Length; i++)
                    fileIndexes[i] = Convert.ToInt32(strIndexes[i]);
                // Укажем идентификатор документа с файлами
                documentID = activity.Attachments[0].ObjectID;
            }
        }
        using (SessionKeeper keeper = new SessionKeeper())
        {
            IEmailService emailService = (IEmailService)keeper.Session.GetCustomService(typeof(IEmailService));
            // Отправим письмо
            string messageID = emailService.SendMessage(keeper.Session.SessionGUID, fromEmail, toEmail, subject, message, documentID, fileIndexes);
            if (messageID != String.Empty && activity.Attachments != null && activity.Attachments.Count > 0)
            {
                // Добавим документу идентификатор письма
                IDBObject document = keeper.Session.GetObject(activity.Attachments[0].ObjectID);
                document.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID(new Guid("cadd92d5-306c-11d8-b4e9-00304f19f545")), false, new object[] { messageID });
            }
        }
    }
}
