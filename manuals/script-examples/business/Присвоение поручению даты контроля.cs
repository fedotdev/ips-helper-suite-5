using System;
using System.Text;
using System.Windows.Forms;
using Intermech.Interfaces;
using Intermech.Interfaces.Workflow;

public class Script
{
 public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(IActivity activity)
 {
		 // Находим у процесса идентификатор поручения
        IDBAttribute attrResolutionID = activity.Process.GetAttributeByGuid(new Guid("cadd92dd-306c-11d8-b4e9-00304f19f545"), true);
        if (attrResolutionID.AsInteger != Intermech.Consts.UnknownObjectId)
        {
            using (SessionKeeper keeper = new SessionKeeper())
            {
				Guid controllDate = new Guid("cadd9290-306c-11d8-b4e9-00304f19f545");
				
				// Поручению присваиваем значение текущей даты в атрибут Дата контроля
                IDBObject resolution = keeper.Session.GetObject(attrResolutionID.AsInteger);
                
				IDBAttribute attr = resolution.GetAttributeByGuid(controllDate);
				
                if (attr == null)
                    attr = resolution.Attributes.AddAttribute(MetaDataHelper.GetAttributeTypeID(controllDate), false);

                attr.AsDateTime = DateTime.Now;
            }
        }
 }
}