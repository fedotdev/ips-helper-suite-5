using System;
using Intermech.Interfaces;
using Intermech.Expert.Scenarios;
using Intermech.Interfaces.Document;
using Intermech;
using Intermech.Interfaces.Client;
using System.Windows.Forms;
using System.Data;
using System.Collections.Generic;
using Intermech.Interfaces.Compositions;
using Intermech.Kernel.Search;
using System.Collections.Specialized;

public class Script
{
    public ICSharpScriptContext ScriptContext {get; private set;}
    
    public ScriptResult Execute(IUserSession session, ImDocumentData document, Int64[] objectIDs)
    {
        if (objectIDs.Length > 1)
        {
            MessageBox.Show("Для генерации ведомости необходимо указать только одну сборочную единицу!");
            return new ScriptResult(false, document);
        }
        // Настройки фильтрации состава в текущем окне "Навигатора"
        IFiltrationService filtrationService = ServicesManager.GetService(typeof(IFiltrationService)) as IFiltrationService;
        // Сервис для получения составов
        ICompositionLoadService compositionService = (ICompositionLoadService)session.GetCustomService(typeof(ICompositionLoadService));
        // Рутовая сборочная единица
        IDBObject rootObject = session.GetObject(objectIDs[0]);
        
        // Пишем обозначение в шапку ведомости
        IDBAttribute attrDes = rootObject.GetAttributeByGuid(new Guid(SystemGUIDs.attributeDesignation));
        TextData desText = document.FindFirstNodeFromTemplate_Recursive("ОБОЗ") as TextData;
        desText.AssignText(attrDes.AsString, false, false, false);
        
        // Получим конструкторский состав на сборку
        // ****************************************
        // Необходимые колонки
        ColumnDescriptor[] columns = new ColumnDescriptor[]{
        new ColumnDescriptor((int)ObligatoryObjectAttributes.F_OBJECT_ID, AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Index, SortOrders.NONE, 0),
        new ColumnDescriptor((int)ObligatoryObjectAttributes.F_OBJECT_TYPE, AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Index, SortOrders.NONE, 0),
        new ColumnDescriptor((int)ObligatoryObjectAttributes.CAPTION, AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Index, SortOrders.NONE, 0),
        new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeDesignation), AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Index, SortOrders.NONE, 0),
        new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName), AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Index, SortOrders.NONE, 0)
        
        // TODO дополнительные колонки для запроса состава
        
    };
    // Сформируем список связей по которым будем конструкторский состав
    List<int> searchRelationTypes = new List<int>();
    // Добавим в список идентификатор типа связей Состав изделий
    searchRelationTypes.Add(MetaDataHelper.GetRelationTypeID(SystemGUIDs.reltypeSP));
    // Ищем все типы дочерние от типа объектов Изделия
    List<int> objTypesProduct = MetaDataHelper.GetObjectTypeChildrenIDRecursive(new Guid(SystemGUIDs.objtypeProduct));
    // В составе показываем актуальные заменители
    HybridDictionary tags = new HybridDictionary(1);
    tags[PDMPluginGuids.buttonSubstitutesGuid] = true;
    // Поиск состава
    DataTable articlesCmposition = compositionService.LoadComposition(session.SessionGUID, rootObject.ObjectID, rootObject.ObjectType, searchRelationTypes,
    objTypesProduct, new List<ColumnDescriptor>(columns), true, false, null, null, filtrationService.Filtration.OwnerID, tags, -1);
    // ****************************************
    // Если состава нет - выходим
    if (articlesCmposition == null || articlesCmposition.Rows.Count == 0) return new ScriptResult(true, document);
    // Находим рабочую область в отчете
    DocumentTreeNode table = document.FindNode("Рабочая область");
    // Обработаем результаты
    // ****************************************
    // Ищем и записываем в отчет сначала стандартные изделия
    DataRow[] standardProductRows = articlesCmposition.Select(String.Format("[1]={0}", MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeStandardProduct)));
    for (int i = 0; i < standardProductRows.Length; i++)
    {
        // заполняем информацию по стандартному изделию
        DocumentTreeNode node = document.Template.FindNode("VC").CloneFromTemplate(true, true);
        table.AddChildNode(node, false, false);
        ScenarioFunc.WriteNodeRow(node, "RANm", Convert.ToString(standardProductRows[i][2]));
        
        // TODO дополнительная информация по стандартному изделию
        
    }
    // *****************************************
    // Далее запишем все остальные изделия из конструкторского состава и для каждого из них поищем в составе технологическую документацию
    // Сформируем условия для запроса в базу
    // Тип искомых объектов: Документы ТП
    List<int> objTypesDocTP = new List<int>();
    objTypesDocTP.Add(MetaDataHelper.GetObjectTypeID("cad00198-306c-11d8-b4e9-00304f19f545"));
    // Связи для поиска технологических документов
    searchRelationTypes.Clear();
    searchRelationTypes.Add(MetaDataHelper.GetRelationTypeID("cad0019f-306c-11d8-b4e9-00304f19f545")); // Технологический состав
    searchRelationTypes.Add(session.IdentHelper.SortedRelationTypeID); // Простая связь с сортировкой
    searchRelationTypes.Add(MetaDataHelper.GetRelationTypeID("cad005b0-306c-11d8-b4e9-00304f19f545")); // Технологическая связь с расцеховкой
    // Цикл по изделиям из состава кроме стандартных
    DataRow[] productRows = articlesCmposition.Select(String.Format("[1]<>{0}", MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeStandardProduct)));
    for (int i = 0; i < productRows.Length; i++)
    {
        // заполняем информацию по изделию
        DocumentTreeNode node = document.Template.FindNode("VC").CloneFromTemplate(true, true);
        table.AddChildNode(node, false, false);
        ScenarioFunc.WriteNodeRow(node, "RADn", Convert.ToString(productRows[i][3]), "RANm", Convert.ToString(productRows[i][4]));
        
        // TODO дополнительная информация по изделию
        
        // Поиск в составе изделия технологических документов
        DataTable techDocCоmposition = compositionService.LoadComposition(session.SessionGUID, Convert.ToInt64(productRows[i][0]), Convert.ToInt32(productRows[i][1]), searchRelationTypes,
        objTypesDocTP, new List<ColumnDescriptor>(columns), true, false, null, null, filtrationService.Filtration.OwnerID, tags, -1);
        
        // Если документы найдены - записываем их в ведомость
        if(techDocCоmposition != null)
        {
            for (int j = 0; j < techDocCоmposition.Rows.Count; j++)
            {
                DocumentTreeNode nodeG = document.Template.FindNode("VГ").CloneFromTemplate(true, true);
                table.AddChildNode(nodeG, false, false);
                ScenarioFunc.WriteNodeRow(nodeG, "О_ТД #3", Convert.ToString(techDocCоmposition.Rows[j][3]));
                
                // TODO дополнительная информация по  технологическому документу
                
            }
        }
    }
    // Обновляем документ
    document.UpdateLayout(0, true, false);
    
    return new ScriptResult(true, document);
}
}