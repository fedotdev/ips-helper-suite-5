using System;
using Intermech.Interfaces;
using Intermech.Interfaces.Workflow;

public class Script
{
 public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(IActivity activity)
 {
     // Ищем атрибут трибут "Идентификатор поручения"
     IDBAttribute attrResolutionID =  activity.Process.GetAttributeByGuid(new Guid("cadd92dd-306c-11d8-b4e9-00304f19f545"));
     if (attrResolutionID != null && attrResolutionID.AsInteger != Intermech.Consts.UnknownObjectId)
     {
            // Переводим поручение на шаг ЖЦ "Поручено"
         IDBObject resolution = activity.Session.GetObject(  attrResolutionID.AsInteger);
         resolution.LCStep = MetaDataHelper.GetLCStepID("cadd93a1-306c-11d8-b4e9-00304f19f545");
     }
 }
}