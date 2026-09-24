using System;
using Intermech.Interfaces;
using Intermech.Interfaces.Workflow;

public class Script
{
	public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(IActivity activity)
	{
		// переменная Нач.КБ
		IVariable nachKb = activity.Variables.Find("Нач.КБ");
		// переменная Нач.КБ.вых
		IVariable nachKbOut = activity.Variables.Find("Нач.КБ.вых");
		//если переменные существуют
		if (nachKb != null && nachKbOut != null)
		{
			// если включен Нач.КБ, но не выбран исполнитель
			if ((bool)nachKb.TypedValue && nachKbOut.Value.Equals(String.Empty))
			{
				throw new NotificationException("Не указан Начальник КБ! Укажите на форме Начальника КБ");
			}
		}
		
		// нет вложений
		if (activity.Attachments.Count == 0)
		{
			throw new NotificationException("Во вложении отсутствуют документы. Запуск процесса без документов запрещен!");
		}
	}
}