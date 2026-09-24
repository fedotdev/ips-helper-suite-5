using System;
using System.Data;
using System.Xml;
using System.Collections.Generic;
using System.Windows.Forms;
using Intermech.Interfaces;
using Intermech.Expert.Scenarios;
using Intermech.Interfaces.Document;
using Intermech.Interfaces.PdmConfigurator;
using Intermech.Document.Model;
using Intermech.Kernel.Search;

public class Script
{
	public ICSharpScriptContext ScriptContext { get; set; }
	
	public ScriptResult Execute(IUserSession session, ImDocumentData document, Int64[] objectIDs)
	{
		if (objectIDs.Length > 1)
		{
			MessageBox.Show("Для генерации ведомости необходимо указать только один заказ!");
			return new ScriptResult(false, document);
		}
		
		Dictionary<long, List<Tuple<string, string, string, string>>> repData = Helper.GetReportData(session, objectIDs[0]);
		
		if (repData.Count == 0)
		{
			MessageBox.Show("Отсутствуют данные для создания отчета!");
			return new ScriptResult(false, document);
		}
		
		document.Designation = "Перечень примененных опций в заказе";
		document.Name = "Перечень примененных опций в заказе " + session.GetObject(objectIDs[0], false).Caption;
		
		foreach(KeyValuePair<long, List<Tuple<string, string, string, string>>> item in repData)
		{
			Page page;
			
			Page pageTemplate = (Page)document.Template.FindNode("mainpage");
			page = (Page)pageTemplate.CloneFromTemplate();
			document.AddChildNode(page, false, false);
			
			TextData assemblyName = (TextData)page.FindFirstNodeFromTemplate_Recursive("assembly");
			if (assemblyName != null)
			{
				assemblyName.AssignText(session.GetObject(item.Key, false).Caption, false, false, false);
			}
			
			TableElement mainTable = (TableElement)page.FindFirstNodeFromTemplate_Recursive("tableoptions");
			
			foreach(Tuple<string, string, string, string> optionTuple in item.Value)
			{
				TableElement option_row_template = document.Template.FindNode("tablerow") as TableElement;
				TableElement option_row = (TableElement)option_row_template.CloneFromTemplate(true, true);
				
				mainTable.AddChildNode(option_row, false, false);
				
				TextData option_code = (TextData)option_row.FindFirstNodeFromTemplate_Recursive("optioncode");
				option_code.AssignText(optionTuple.Item1, false, false, false);
				
				TextData option_name = (TextData)option_row.FindFirstNodeFromTemplate_Recursive("optionname");
				option_name.AssignText(optionTuple.Item2, false, false, false);
				
				TextData option_value = (TextData)option_row.FindFirstNodeFromTemplate_Recursive("optionvalue");
				option_value.AssignText(optionTuple.Item3, false, false, false);
				
				TextData option_description = (TextData)option_row.FindFirstNodeFromTemplate_Recursive("optiondescription");
				option_description.AssignText(optionTuple.Item4, false, false, false);
				
			}
		}
		
		// Удаляю пустую первую страницу
		document.Nodes[0].Remove(false, false);
		
		document.UpdateLayout(true, true);
		
		return new ScriptResult(true, document);
	}
}

class Helper
{
	
	public static Dictionary<long, List<Tuple<string, string, string, string>>> GetReportData(IUserSession session, long objectID)
	{
		Dictionary<long, List<Tuple<string, string, string, string>>> result = new Dictionary<long, List<Tuple<string, string, string, string>>>();
		
		IDBRelationCollection relColl = session.GetRelationCollection(MetaDataHelper.GetRelationTypeID(new Guid( "cad00023-306c-11d8-b4e9-00304f19f545" /*Состоит из*/)));
		
		DataTable data = relColl.Select(new DBRecordSetParams(null, new object[] { (int)ObligatoryObjectAttributes.F_PRJLINK_ID, (int)ObligatoryObjectAttributes.F_OBJECT_ID} ), objectID, -1, DateTime.Now);
		foreach(DataRow row in data.Rows)
		{
			long relID = Convert.ToInt64(row[0]);
			IDBRelation relation = session.GetRelation(relID);
			PdmConfiguratorContext context = new PdmConfiguratorContext(relation);
			Dictionary<Guid, string> optionValsDictionary = context.OptionsValues;
			
			List<Tuple<string, string, string, string>> optValsList = new List<Tuple<string, string, string, string>>();
			
			foreach(KeyValuePair<Guid, string> optionVal in optionValsDictionary) 
			{
				// ищем опцию в кэше
				OptionHolder option = PdmConfiguratorCache.CacheFindOption(optionVal.Key);
				
				if (option != null)
				{
					// id значения опции
					string valueID = context.OptionsValues[option.OptionGuid];
					
					// ищем описание значения опции
					OptionValue optionValue = option.OptionValues.FindValue(valueID);
					if (optionValue != null)
						optValsList.Add(new Tuple<string, string, string, string>(optionValue.Code,option.OptionCaption,optionValue.Value, optionValue.Description));
					
				}
				
			}
			
			if (optValsList.Count > 0 && !result.ContainsKey(Convert.ToInt64(row[1])))
			{
				result.Add(Convert.ToInt64(row[1]), optValsList);
			}
			
		}
		
		return result;
	}
	
}