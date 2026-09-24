using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Intermech.Interfaces;
using Intermech.Expert.Scenarios;
using Intermech.Interfaces.Document;
using Intermech.Interfaces.PdmConfigurator;
using Intermech.Document.Model;

public class Script
{
	public ICSharpScriptContext ScriptContext { get; set; }
	
	// основной метод выполнения скрипта
	public ScriptResult Execute(IUserSession session, ImDocumentData document, Int64[] objectIDs)
	{
		if (objectIDs.Length > 1)
		{
			MessageBox.Show("Для генерации ведомости необходимо указать только одну сборочную единицу!");
			return new ScriptResult(false, document);
		}
		
		// получение данных для отчета
		Dictionary<string, List<Tuple<string, string, string>>> objectOptions  = ReportHelper.GetReportData(session, objectIDs[0]);
		
		if (objectOptions.Count == 0)
		{
			MessageBox.Show("Отсутствуют данные для создания отчета!");
			return new ScriptResult(false, document);
		}
		
		// Атрибуты отчета
		document.Designation = "Перечень допустимых опций конфигуратора";
		document.Name = "Перечень допустимых опций конфигуратора для " + session.GetObject(objectIDs[0], false).Caption;
		
		// получение таблицы куда будут выводиться данные 
		TableElement main_table = document.FindFirstNodeFromTemplate_Recursive("main_table") as TableElement;
		
		foreach (KeyValuePair<string, List<Tuple<string, string, string>>> option in objectOptions)
		{
			TableElement option_row = InsertOptionName(document, main_table, option.Key);
			
			foreach (Tuple<string, string, string> optionValue in option.Value)
			{
				InsertOptionValue(document, option_row, optionValue);
			}
		}
		
		document.UpdateLayout(true, true);
		
		return new ScriptResult(true, document);
	}
	
	private TableElement InsertOptionName(ImDocumentData document, TableElement main_table, string optionName)
	{
		TableElement option_row_template = document.Template.FindNode("option_row") as TableElement;
		
		TableElement option_row = (TableElement)option_row_template.CloneFromTemplate(true, true);
		
		main_table.AddChildNode(option_row, false, false);
		
		TextData option_name = (TextData)option_row.FindFirstNodeFromTemplate_Recursive("option_name");
		option_name.AssignText(optionName, false, false, false);
		return option_row;
	}
	
	private void InsertOptionValue(ImDocumentData document, TableElement option_row, Tuple<string, string, string> optionValue)
	{
		TableElement option_val_row_tempalte = (TableElement)document.Template.FindNode("option_val_row");
		
		TableElement option_val_row = (TableElement)option_val_row_tempalte.CloneFromTemplate(true, true);
		
		TableElement option_val_cell = (TableElement)option_row.FindFirstNodeFromTemplate_Recursive("option_val_cell");
		
		option_val_cell.AddChildNode(option_val_row, false, false);
		
		TextData option_val_value = (TextData)option_val_row.FindFirstNodeFromTemplate_Recursive("option_val_value");
		option_val_value.AssignText(optionValue.Item1, false, false, false);
		
		TextData option_val_code = (TextData)option_val_row.FindFirstNodeFromTemplate_Recursive("option_val_code");
		option_val_code.AssignText(optionValue.Item2, false, false, false);
		
		TextData option_val_description = (TextData)option_val_row.FindFirstNodeFromTemplate_Recursive("option_val_description");
		option_val_description.AssignText(optionValue.Item3, false, false, false);
	}
}


/// <summary>
/// Класс для получения данных для отчета
/// </summary>
class ReportHelper
{
	/// <summary>
	/// Метод получения данных для отчета
	/// </summary>
	/// <param name="session">сессия</param>
	/// <param name="ObjectID">обьъект по которому получать данные</param>
	/// <returns>данные для отчета в виде Dictionary</returns>
	/// Key = название опции
	/// Value = List<Tuple<значение опции, код значения, описание значения>>
	public static Dictionary<string, List<Tuple<string, string, string>>> GetReportData(IUserSession session, long ObjectID)
	{
		// результат работы метода
		Dictionary<string, List<Tuple<string, string, string>>> result = new Dictionary<string, List<Tuple<string, string, string>>>();
		
		// получение опций объекта
		ObjectOptionsHolder objConfigOptions = PdmConfiguratorObjectOptionsCache.GetOrLoadObjectOptions(session, ObjectID);
		
		// получение видимых опций объекта
		VisibleOptionValues visibleConfigOptionValues = objConfigOptions.VisibleOptionValues;
		
		foreach (long opId in objConfigOptions.Options)
		{
			// получение конкретной опции
			IDBConfiguratorOption option = session.GetObject(opId, false) as IDBConfiguratorOption;
			if (option != null)
			{
				// Guid опции
				Guid optionGuid = option.ObjectGUID;
				// Название опции
				string optionName = option.Caption;
				
				// List значений опции
				// Tuple<значение опции, код значения, описание значения> 
				List<Tuple<string, string, string>> optionValues = new List<Tuple<string, string, string>>();
				
				// видимые значения опции
				List<string> visibleValues;
				
				// если значение опции видимое
				if (visibleConfigOptionValues.Items.TryGetValue(optionGuid, out visibleValues))
				{
					OptionValuesCollection optionValueCollection = option.OptionValues;
					// цикл по всем значениям опции
					for (int opvalId = 0; opvalId < optionValueCollection.Count; opvalId++)
					{
						// получение конкретного значения опции
						OptionValue optionValue = optionValueCollection[opvalId];
						// ID опции
						string optionValueID = optionValue.ID;
						
						// если данного значения нет в List 
						if (visibleValues.Exists(visValue => visValue == optionValueID))
						{
							// значение опции
							string optionValueStr = optionValue.Value;
							// код значения
							string optionValueCode = optionValue.Code;
							// описание значения
							string optionValueDescription = optionValue.Description;
							
							// добавление в Lsit
							optionValues.Add(new Tuple<string, string, string>(optionValueStr, optionValueCode, optionValueDescription));
						}
						
					}
					
					// если у опции есть видимые значения и ее нету в Dictionary
					if (optionValues.Count > 0 && !result.ContainsKey(optionName))
						// добавление в Dictionary
						result.Add(optionName, optionValues);
					
				}
				
			}
		}
		
		// Возврат результата
		return result;
	}
}