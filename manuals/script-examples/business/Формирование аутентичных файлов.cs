using System;
using System.Text;
using System.Data;
using System.Xml;
using System.Collections.Generic;
using System.Windows.Forms;
using Intermech;
using Intermech.Interfaces;
using Intermech.Interfaces.Workflow;
using Intermech.Interfaces.Client;
using Intermech.Kernel.Search;

public class Script
{
    public ICSharpScriptContext ScriptContext {get; private set;}
    
    public void Execute(IActivity activity)
    {
        // получение сессии
        IUserSession session = activity.Session;
        // получение списков связи Изменяемые объекты
        IDBRelationCollection relCollection = session.GetRelationCollection(MetaDataHelper.GetRelationTypeID(new Guid("cad0036b-306c-11d8-b4e9-00304f19f545" /*Изменяемые объекты*/)));
        // текст ошибки
        StringBuilder errObjs = new StringBuilder();
        // коллекция ids объектов для формирования аутентичных файлов
        List<long> objIDsList = new List<long>();
        
        for (int i = 0; i < activity.Attachments.Count; i++)
        {
            IDBObject obj = activity.Attachments[i].Object;
            // добавление в коллекцию вложения
            objIDsList.Add(obj.ObjectID);
            // если вложение ИИ - раскривается его состав
            if (MetaDataHelper.GetObjectTypeChildrenIDRecursive(new Guid("cad00348-306c-11d8-b4e9-00304f19f545" /*Извещения*/)).IndexOf(obj.ObjectType) > 0)
            {
                // получение состава ИИ
                DataTable dtRevConsist = relCollection.ConsistFrom(new DBRecordSetParams(null, new object[] {(int)ObligatoryObjectAttributes.F_OBJECT_ID}), obj.ObjectID);
                foreach(DataRow row in dtRevConsist.Rows)
                {
                    // добавление в коллекцию  из состава ИИ
                    objIDsList.Add( Convert.ToInt64(row[0]));
                }
            }
        }
        
        // формирование аутентичных файлов
        AuthFileHelper.Create(session, objIDsList.ToArray(), errObjs);
        
        // если есть ошибки - вывод диалога
        if (errObjs.Length > 0)
        {
            if (MessageBox.Show("Для следующих документов не удалось сформировать аутентичные документы: \n" + errObjs.ToString() + "\n Продолжить выполнение процесса?", "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
            {
                // пользователь нажал нет - отправка далее не происходит
                throw new AbortException();
            }
        }
    }
}

/// <summary>
/// Класс для формирования аутентичных файлов
/// </summary>
class AuthFileHelper
{
    /// <summary>
    /// Метод формирования аутентичных файлов
    /// </summary>
    /// <param name="session">сессия</param>
    /// <param name="objIDs">массив ID для формирования аутентичных файлов</param>
    /// <param name="errsTxt">текст ошибки</param>
    public static void Create(IUserSession session, long[] objIDs, StringBuilder errsTxt)
    {
        // получение сервиса для формирования аутентичных файлов
        IAuthFilesService iAuthFilesService = ServicesManager.GetService(typeof(IAuthFilesService)) as IAuthFilesService;
        // сервис не найден - выход
        if (iAuthFilesService == null) return;
        
        foreach(long objID in objIDs)
        {
            // получение конкретного объекта
            IDBObject obj = session.GetObject(objID);
            // если является документом
            if (MetaDataHelper.GetObjectTypeChildrenIDRecursive(new Guid("cad00070-306c-11d8-b4e9-00304f19f545")).IndexOf(obj.ObjectType) >= 0 &&
            obj.ObjectType != MetaDataHelper.GetObjectTypeID(new Guid("cadd9baf-306c-11d8-b4e9-00304f19f545")) &&
            obj.ObjectType != MetaDataHelper.GetObjectTypeID(new Guid("cad00768-306c-11d8-b4e9-00304f19f545")))
            {
                try
                {
                    // аргумент для формирования аутентичного файла
                    AuthFileAssignEventArgs authFileAssignEventArgs = new AuthFileAssignEventArgs(obj.ObjectType, obj.ObjectID, true);
                    // инициирование события генерации аутентичного файла
                    iAuthFilesService.FireEventAuthFileAssign(authFileAssignEventArgs);
                    // не удалось сформировать аутентичный файл
                    if (!authFileAssignEventArgs.IsHandled)
                        // текст ошибки
                    errsTxt.AppendLine(" - " + obj.ObjectID.ToString() + " " + obj.Caption + " " + MetaDataHelper.GetObjectType(obj.ObjectType).ObjectName);
                }
                catch
                {
                    // ошибка во время формирования аутентичного файла
                    errsTxt.AppendLine(" - " + obj.ObjectID.ToString() + " " + obj.Caption + " " + MetaDataHelper.GetObjectType(obj.ObjectType).ObjectName);
                }		
            }
        }
    }
}