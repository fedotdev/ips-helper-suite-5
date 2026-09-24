using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Intermech;
using Intermech.Interfaces;
using Intermech.Expert.Scenarios;
using Intermech.Interfaces.Document;
using Intermech.Kernel.Search;
using Intermech.MRP2;
using Intermech.Interfaces.MRP;
using Intermech.Interfaces.Client;

namespace Consulting.Scripts.Report.ReportAnd_1902049
{
    public class Script
    {
        private int _objtypeIdPartWithoutDrawing = MetaDataHelper.GetObjectTypeID("565e13c4-dc5e-49f8-b677-42ba8d9d17ed" /*ПК БЧ*/);

        public ICSharpScriptContext ScriptContext { get; set; }

        public ScriptResult Execute(IUserSession session, ImDocumentData document, long[] objectIDs)
        {
            //if (Debugger.IsAttached)
            //{
            //    Debugger.Break();
            //}

            IObjectsInfoCache objectsInfoCache = ApplicationServices.Container.GetService(typeof(IObjectsInfoCache)) as IObjectsInfoCache;

            if (objectIDs.Length != 1)
            {
                return new ScriptResult(false, document);
            }

            // Рутовый объект генерации ведомости
            IDBObject rootObject = session.GetObject(objectIDs[0], true);

            // Если головной объект не Производственная ведомость
            if (rootObject.ObjectType != MRP2Consts.objtypeIdProductionLists)
            {
                return new ScriptResult(false, document);
            }

            // запишем обозначение документа
            document.Designation = string.Format("{0} ВИ", rootObject.Caption);
            document.DocumentName = "Ведомость И";

            string numberPv = rootObject.GetAttributeByGuid(new Guid(MRP2Consts.attributeProductionListNumber/*Ведомость №*/), true).AsString;

            // Пишем атрибуты ПВ в шапку ведомости
            AssignTextFromTemplate(document, "Zak1", string.Format("Ведомость № {0}", numberPv));
            //Шифр производственных затрат Атрибут: Заказ для ПВ
            WriteDocumentAttributeData(document, rootObject, new Guid("cadd9a7c-306c-11d8-b4e9-00304f19f545"/*Заказ для ПВ*/), "ШПЗ");
            //Индекс изделия Атрибут: Индекс изделия
            WriteDocumentAttributeData(document, rootObject, new Guid("cadd9a7d-306c-11d8-b4e9-00304f19f545"/*Индекс изделия*/), "ОбИз");
            //Конструктор
            WriteDocumentAttributeData(document, rootObject, new Guid("cadd9a7b-306c-11d8-b4e9-00304f19f545"/*ФИО конструктор*/), "ФдВ");
            //Текущая дата
            AssignTextFromTemplate(document, "DATE", DateTime.Now.ToShortDateString());

            //Телефон (зак)
            IDBAttribute constr = rootObject.GetAttributeByGuid(new Guid("cadd9a7b-306c-11d8-b4e9-00304f19f545"), false);

            if (constr != null)
            {
                WriteDocumentAttributeData(document, session.GetObject(constr.AsInteger), new Guid("cad002da-306c-11d8-b4e9-00304f19f545"), "теЛ");
            }

            //набор дополнительных колонок для получения данных
            List<ColumnDescriptor> columns = new List<ColumnDescriptor>
            {
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeDesignation /*Обозначение*/), AttributeSourceTypes.Object, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.ASC, 0),
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName /*Наименование*/), AttributeSourceTypes.Object, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0),
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(MRP2Consts.attributeCountForExitAssembly /*Атрибут связи «Количество на выходную сборку»*/), AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0),
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(MRP2Consts.attributeCountForPL /*Атрибут связи «Количество на всю ПВ»*/), AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0),
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeSortAVS /*Атрибут Сортировка AVS*/), AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0)
            };

            //Получим состав ПВ по объектам Выходные сборки
            IReportAndData reportEndData = new ReportAndData();

            if (reportEndData.LoadData(session, rootObject.ObjectID, columns) == false)
            {
                return new ScriptResult(true, document);
            }

            List<IPathProductionReportCompositionItem> reportItemsData = reportEndData.ReportEndRelationsData();

            if (reportItemsData.Count == 0)
            {
                return new ScriptResult(true, document);
            }

            //все уникальные элементы состава всех веток
            List<RelObjInfoAttrValuesItem> allData = reportItemsData.SelectMany(x => x.PathRelationItems).Distinct().ToList();

            // словарь индексов выходных сборок
            Dictionary<long, int> numericExitAssembly = new Dictionary<long, int>();

            int index = 1;

            // Находим рабочую область в отчете
            DocumentTreeNode workTable = document.FindNode("Рабочая область");

            // Заполним страницы выходных сборок
            foreach (RelObjInfoAttrValuesItem exitAssembly in allData.Where(a => a.PartInfo.ObjTypeID == MRP2Consts.objtypeIdExitAssembly))
            {
                numericExitAssembly.Add(exitAssembly.PartInfo.ObjectID, index);

                //Строка выходной сборки
                DocumentTreeNode node = document.Template.FindNode("всб").CloneFromTemplate(true, true);

                ScenarioFunc.WriteNodeRow(node, "Nпп", index.ToString());
                ScenarioFunc.WriteNodeRow(node, "RCDn", exitAssembly.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeDesignation)).AsString());
                ScenarioFunc.WriteNodeRow(node, "RCNm", exitAssembly.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName)).AsString());
                ScenarioFunc.WriteNodeRow(node, "RCFC", exitAssembly.GetAttrValue(MRP2Consts.attrIdCount).AsString());

                workTable.AddChildNode(node, false, false);

                index++;
            }

            //мапинг объектов производственных копий с типами объектов, последовательность влияет на порядок вывода типов объектов состава ПВ
            ProductionCopyObjectTypeMap[] productionCopyObjectTypes = new ProductionCopyObjectTypeMap[]
            {
                new ProductionCopyObjectTypeMap(MRP2Consts.objtypeIdAssemblyCopy /*Производственные копии сборочных единиц*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeAssemblyUnit)),
                new ProductionCopyObjectTypeMap(MRP2Consts.objtypeIdPartCopy /*Производственные копии деталей*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypePart)),
                new ProductionCopyObjectTypeMap(_objtypeIdPartWithoutDrawing /*ПК БЧ*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypePartWithoutDrawing)),
                new ProductionCopyObjectTypeMap(MRP2Consts.objtypeIdStandardCopy/*Производственные копии стандартных изделий*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeStandardProduct)),
                new ProductionCopyObjectTypeMap(MRP2Consts.objtypeIdOthersCopy/*Производственные копии прочих изделий*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeOtherProducts)),
                new ProductionCopyObjectTypeMap(MRP2Consts.objtypeIdMaterialCopy /*Производственные копии материалов*/, MetaDataHelper.GetObjectTypeID(SystemGUIDs.objtypeMaterial))
            };

            index = 1;

            foreach (ProductionCopyObjectTypeMap productionCopy in productionCopyObjectTypes)
            {
                List<IPathProductionReportCompositionItem> childPvItems = GetSortedPvItems(reportItemsData, productionCopy.PVObjectType);

                if (childPvItems.Count == 0)
                {
                    continue;
                }

                // Заполнение листа 2 и последующих
                PageData pageDataL2 = document.ClonePageFromTemplate("L2", false);

                document.AddChildNode(pageDataL2, false, false);

                // Номер заказа, раздел
                AssignTextFromTemplate(pageDataL2, "Zak1 #3", string.Format("Ведомость № {0}, Вид изделия {1}", numberPv, MetaDataHelper.GetObjectTypeName(productionCopy.ObjectType)));
                // Текущая дата
                AssignTextFromTemplate(document, "DATE", DateTime.Now.ToShortDateString());

                workTable = pageDataL2.FindFirstNodeFromTemplate_Recursive("Рабочая область 2");

                foreach (PathProductionReportCompositionItem childItem in childPvItems.GroupBy(a => a.LastCompositionItem.PartInfo.ObjectID).SelectMany(a => a))
                {
                    //Строка элемента состава ПВ
                    DocumentTreeNode nodeProjItem = document.Template.FindNode("стрП").CloneFromTemplate(true, true);

                    int indexExitAssembly;

                    if (numericExitAssembly.TryGetValue(childItem.FirstCompositionItem.PartInfo.ObjectID, out indexExitAssembly))
                    {
                        //Номер по порядку выходной сборки (Из списка выходных сборок), куда входит родительская ПК ДСЕ
                        ScenarioFunc.WriteNodeRow(nodeProjItem, "Nпп #2", indexExitAssembly.ToString());
                    }

                    // Обозначение подузла (куда входит) для ведомости
                    ScenarioFunc.WriteNodeRow(nodeProjItem, "RPDn", objectsInfoCache.GetObjectCaption(childItem.LastCompositionItem.ProjInfo.ObjectID));
                    //Количество изделий на подузел(подсборку) для ведомости
                    ScenarioFunc.WriteNodeRow(nodeProjItem, "RACn", childItem.LastCompositionItem.GetAttrValue(MRP2Consts.attrIdCount).AsString());
                    //Количество на выходную сборку
                    ScenarioFunc.WriteNodeRow(nodeProjItem, "RAFc", childItem.LastCompositionItem.GetAttrValue(MRP2Consts.attrIdCountForExitAssembly).AsString());
                    //Количество на всю ПВ
                    ScenarioFunc.WriteNodeRow(nodeProjItem, "кДк", childItem.LastCompositionItem.GetAttrValue(MRP2Consts.attrIdCountForPL).AsString());

                    workTable.AddChildNode(nodeProjItem, false, false);

                    //Строка элемента состава ПВ
                    DocumentTreeNode nodePartItem = document.Template.FindNode("стрС").CloneFromTemplate(true, true);

                    string designationColumnValue = childItem.LastCompositionItem.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeDesignation)).AsString();

                    if (designationColumnValue == string.Empty)
                    {
                        //"Идентификатор версии объекта"
                        designationColumnValue = childItem.LastCompositionItem.PartInfo.ObjectID.ToString();
                    }

                    ScenarioFunc.WriteNodeRow(nodePartItem, "Nпп2", index.ToString());
                    ScenarioFunc.WriteNodeRow(nodePartItem, "RCDn2", designationColumnValue);
                    ScenarioFunc.WriteNodeRow(nodePartItem, "RCNm2", childItem.LastCompositionItem.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName)).AsString());

                    workTable.AddChildNode(nodePartItem, false, false);

                    index++;
                }

                if (workTable.NodesCount == 0)
                {
                    document.RemoveChildNode(pageDataL2, false, false);
                }
            }

            return new ScriptResult(true, document);
        }

        public void WriteDocumentAttributeData(VisualNode node, IDBObject obj, Guid attrGuid, string templateName)
        {
            IDBAttribute attribute = obj.GetAttributeByGuid(attrGuid, false);

            if (attribute != null)
            {
                AssignTextFromTemplate(node, templateName, attribute.AsString);
            }
        }

        private List<IPathProductionReportCompositionItem> GetSortedPvItems(IEnumerable<IPathProductionReportCompositionItem> sostavPv, int objType)
        {
            List<IPathProductionReportCompositionItem> sortedItems = sostavPv.Where(a => a.LastCompositionItem.PartInfo.ObjTypeID == objType).ToList();

            int[] sortedDesignationType = new[] { MRP2Consts.objtypeIdAssemblyCopy, MRP2Consts.objtypeIdPartCopy, _objtypeIdPartWithoutDrawing };
            int[] sortedSortAvsType = new[] { MRP2Consts.objtypeIdStandardCopy, MRP2Consts.objtypeIdOthersCopy, MRP2Consts.objtypeIdMaterialCopy };

            if (sortedDesignationType.Contains(objType))
            {
                return sortedItems.OrderBy(a => a.LastCompositionItem.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeDesignation)).AsString()).ToList();
            }

            if (sortedSortAvsType.Contains(objType))
            {
                return sortedItems.OrderBy(a => a.LastCompositionItem.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeSortAVS)).AsString())
                    .ThenBy(a => a.LastCompositionItem.GetAttrValue(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName)).AsString()).ToList();
            }

            return sortedItems;
        }

        private void AssignTextFromTemplate(VisualNode node, string templateName, string value)
        {
            TextData textDate = node.FindFirstNodeFromTemplate_Recursive(templateName) as TextData;

            if (textDate != null)
            {
                textDate.AssignText(value, false, false, false);
            }
        }
    }

    public class ProductionCopyObjectTypeMap
    {
        public ProductionCopyObjectTypeMap(int pVObjectType, int objectType)
        {
            PVObjectType = pVObjectType;
            ObjectType = objectType;
        }

        public int PVObjectType { get; private set; }
        public int ObjectType { get; private set; }
    }
}
