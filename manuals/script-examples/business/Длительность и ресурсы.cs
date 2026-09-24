using System;
using System.Data;
using System.Xml;
using Intermech.Interfaces;
using Intermech.Project;
using Intermech.Interfaces.Workflow;
using Intermech.Interfaces.Imbase;
using Intermech.Interfaces.Client;
using System.Collections.Generic;
using Intermech.Kernel.Search;
using  Intermech.Expert;
using Intermech.Workflow;
public class Script
{
	public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(Task t, IDBObject obj)
	{
		
		IUserSession session = obj.Session;
		
		if (t.Tag is Task)
		{
			
			return; //создана по прототипу, пропустим
		}
		
		t.Name = obj.Caption+" Создать ТП";//заголовок задачи
		t.Duration = 3;
		t.Notes = "Создать ТП на изделие во вложении";
		Assignment asm = new Assignment(new Resource(t, 4, "Системный администратор", 1));
		t.Assignments.Add(asm);
		
	}
}