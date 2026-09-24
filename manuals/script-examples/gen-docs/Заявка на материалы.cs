//ЗАЯВКА НА МАТЕРИАЛЫ
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml;
using Intermech;
using Intermech.Interfaces;
using Intermech.Expert.Scenarios;
using Intermech.Interfaces.Compositions;
using Intermech.Interfaces.Document;
using Intermech.Kernel.Search;
using System.Diagnostics;
using Intermech.Expert;
using Intermech.MRP2;
using Intermech.Interfaces.MRP;

namespace ZM
{
    public class Script
    {
        private DocumentTreeNode _workTable;
        private ImDocumentData _document;
        private int _numberLine = 0;
        readonly List<string> errorMessList = new List<string>();
        public ICSharpScriptContext ScriptContext { get; set; }

        public ScriptResult Execute(IUserSession session, ImDocumentData document, Int64[] objectIDs)
        {
            _document = document;
            _workTable = document.FindNode("WorkTable");
            //отладка
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            if (objectIDs == null || objectIDs.Length == 0)
            {
                return null;
            }

            long idObjPV = objectIDs[0];
            QuickObjectInfo objectInfo = session.GetObjectInfo(idObjPV);

            if (objectInfo.Empty || objectInfo.ObjectTypeID != MRP2Consts.objtypeIdProductionLists)
            {
                return null;
            }

            //запишем в документ номер заказа и обозначение
            WriteProductionListData(session.GetObject(idObjPV));

            //Состав производственного заказа
            List<DataUnitReport> reportDataUnits = LoadComposition(session, idObjPV,
                new[] { MetaDataHelper.GetRelationTypeID(MRP2Consts.reltypeProductComposition) }, true);
            //нет состава, выходим
            if (reportDataUnits.Count == 0)
            {
                return null;
            }

            //проставим группу сортировки
            foreach (DataUnitReport unitReport in reportDataUnits)
            {
                // Тип объекта "Производственные копии материалов"
                if (unitReport.TypeID == MRP2Consts.objtypeIdMaterialCopy)
                {
                    unitReport.GroupForOrder = GroupForOrderMaterial.PVMaterial;
                }
            }

            Dictionary<long, DataUnit> dataUnits = GetDataUnit(session,
                reportDataUnits/*.Where(a => a.GroupForOrder != GroupForOrderMaterial.None)*/.Select(a => a.RefInProduct).Distinct().ToList(), new Dictionary<int, List<string>>());

            // словарь всех элементов дерева отчета для подсчета общего количества
            var groupObjIds = reportDataUnits.GroupBy(a => a.ObjectID);
            Dictionary<long, List<DataUnitReport>> objIdReportDictionary = new Dictionary<long, List<DataUnitReport>>();
            foreach (IGrouping<long, DataUnitReport> groupObjId in groupObjIds)
            {
                objIdReportDictionary.Add(groupObjId.Key, groupObjId.ToList());
            }

            //
            //БЛОК ПРОВЕРКИ ДАННЫХ!!!
            //

            // создадим коллекцию заменителей
            List<DataUnitReport> zamenReportUnits =
                reportDataUnits.Where(a => a.GroupZamen > 0 && a.NumberZamen > 0).ToList();
            //сортируем по типу группы
            var groupUnits = reportDataUnits.GroupBy(a => a.GroupForOrder).OrderBy(a => (int)a.Key);


            //****************Заполнение документа*****************//
            foreach (IGrouping<GroupForOrderMaterial, DataUnitReport> dataUnitReports in groupUnits)
            {
                if (dataUnitReports.Key == GroupForOrderMaterial.None)
                {
                    continue;
                }

                List<string> attrSorded = new List<string>();
                attrSorded.Add("cad003de-306c-11d8-b4e9-00304f19f545"); //ГОСТ
                attrSorded.Add(SystemGUIDs.attributeName); //Наименование
                //отсортируем коллекцию элементов типа материалы, стандартные, прочие, из состава ПВ
                SortedDictionary<DataUnit, List<DataUnitReport>> sortedDataUnit =
                    SortedDataUnit(session, dataUnitReports, dataUnits, attrSorded);
                //выводим в отчет
                ReportedGroup(sortedDataUnit, objIdReportDictionary, zamenReportUnits, dataUnits);
            }

            foreach (string error in errorMessList)
            {
                WriteWorkTableErrorLine(error);
            }

            return new ScriptResult(true, document);
        }

        public List<DataUnitReport> LoadComposition(IUserSession session, long projId, IEnumerable<int> relations,
            bool recursive, string attrCountGuid = SystemGUIDs.attributeCount)
        {
            List<DataUnitReport> reportDataList = new List<DataUnitReport>();
            List<ColumnDescriptor> column = new List<ColumnDescriptor>();
            //Количество
            column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(attrCountGuid),
                AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0));
            //Ссылка на изделие для ПВ
            column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(MRP2Consts.attributeArticleLink),
                AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.Guid, SortOrders.NONE, 0));
            //Номер группы заменителей
            column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID("cad001c0-306c-11d8-b4e9-00304f19f545"),
                AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0));
            //Номер заменителя в группе
            column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID("cad001c1-306c-11d8-b4e9-00304f19f545"),
                AttributeSourceTypes.Relation, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0));

            DataTable sostavTable = DataHelper.GetChildSostavData(projId, session, relations, recursive, null, column);
            if (sostavTable != null && sostavTable.Rows.Count > 0)
            {
                foreach (DataRow row in sostavTable.Rows)
                {
                    reportDataList.Add(new DataUnitReport(row, attrCountGuid));
                }
            }

            return reportDataList;
        }

        MeasuredValue AllCount(DataUnitReport unitReport, Dictionary<long, List<DataUnitReport>> unitReports,
            out string errorMess)
        {
            errorMess = String.Empty;
            if (unitReport.Count == null)
            {
                return null;
            }

            MeasuredValue unitCount = new MeasuredValue(unitReport.Count.Value, unitReport.Count.MeasureID);
            if (unitReport.ProjID == Intermech.Consts.UnknownObjectId)
            {
                return unitCount;
            }

            MeasuredValue allCount = unitCount;
            List<DataUnitReport> projUnits;
            if (unitReports.TryGetValue(unitReport.ProjID, out projUnits))
            {

                foreach (DataUnitReport projUnit in projUnits)
                {
                    MeasuredValue count = new MeasuredValue(unitCount.Value, unitCount.MeasureID);
                    MeasuredValue countProj = AllCount(projUnit, unitReports, out errorMess);
                    if (countProj == null || errorMess != String.Empty)
                    {
                        return null;
                    }

                    if (count.MeasureID == countProj.MeasureID)
                    {
                        count.Value = count.Value * countProj.Value;
                    }
                    else
                    {
                        try
                        {
                            count.Multiply(countProj);
                        }
                        catch (Exception e)
                        {
                            errorMess = e.Message;
                            return null;
                        }
                    }

                    if (allCount == unitCount)
                    {
                        allCount = new MeasuredValue(count.Value, count.MeasureID);
                    }
                    else
                    {
                        try
                        {
                            allCount.Add(count);
                        }
                        catch (Exception e)
                        {
                            errorMess = e.Message;
                            return null;
                        }
                    }
                }

            }

            return allCount;
        }

        /// <summary>
        /// Формирование отсортированной коллекции элементов
        /// </summary>
        /// <param name="session"></param>
        /// <param name="groupUnit">Группа элементов одного типа</param>
        /// <param name="refInProductDataUnits"></param>
        /// <param name="unitAttrList">Коллекция guid атрибутов, по которым необходимо отсортировать элементы в группе</param>
        /// <returns></returns>
        SortedDictionary<DataUnit, List<DataUnitReport>> SortedDataUnit(IUserSession session,
            IEnumerable<DataUnitReport> groupUnit, Dictionary<long, DataUnit> refInProductDataUnits,
            List<string> unitAttrList)
        {
            SortedDictionary<DataUnit, List<DataUnitReport>> sortedUnitDictionary =
                new SortedDictionary<DataUnit, List<DataUnitReport>>(new DataUnitComparer(unitAttrList));
            if (!groupUnit.Any())
            {
                return sortedUnitDictionary;
            }

            // временный словарь id объекта и коллекции DataUnitReport
            Dictionary<long, List<DataUnitReport>> idUnitDictionary = new Dictionary<long, List<DataUnitReport>>();
            // группируем записи по id объекта
            var groupObjIds = groupUnit.GroupBy(a => a.RefInProduct);

            List<DataUnit> unitsType = new List<DataUnit>();

            foreach (IGrouping<long, DataUnitReport> groupObjId in groupObjIds)
            {
                idUnitDictionary.Add(groupObjId.Key, groupObjId.ToList());
                // соберем id объектов
                unitsType.Add(refInProductDataUnits[groupObjId.Key]);
            }

            List<ColumnDescriptor> column = null;
            if (unitAttrList != null && unitAttrList.Count > 0)
            {
                column = new List<ColumnDescriptor>();
                column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeF_OBJECT_ID),
                    AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.Guid, SortOrders.NONE, 0));
                foreach (var unitAttr in unitAttrList)
                {
                    int attrId = MetaDataHelper.GetAttributeTypeID(unitAttr);
                    if (attrId != 0)
                    {
                        column.Add(new ColumnDescriptor(attrId, AttributeSourceTypes.Object, ColumnContents.String,
                            ColumnNameMapping.Guid, SortOrders.NONE, 0));
                    }
                }
            }

            DataTable unitData = DataHelper.GetObjectData(unitsType.Select(a => a.TypeID).Distinct().ToArray(), session,
                null, column, unitsType.Select(a => a.ObjectID).Distinct().ToArray());
            if (unitData == null || unitData.Rows.Count == 0)
            {
                return sortedUnitDictionary;
            }

            foreach (DataRow dataRow in unitData.Rows)
            {
                long idObjUnit = DataSetProcessor.GetInt64Value(dataRow, SystemGUIDs.attributeF_OBJECT_ID,
                    Intermech.Consts.UnknownObjectId);
                if (idObjUnit != Intermech.Consts.UnknownObjectId)
                {
                    sortedUnitDictionary.Add(new DataUnit(dataRow, unitAttrList), idUnitDictionary[idObjUnit]);
                }
            }

            return sortedUnitDictionary;
        }

        List<DataUnitReport> GetZamenitel(DataUnitReport unitReport, List<DataUnitReport> zamenReportUnits)
        {
            List<DataUnitReport> zamenitelList = new List<DataUnitReport>();
            foreach (DataUnitReport zamenReportUnit in zamenReportUnits)
            {
                if (zamenReportUnit.ProjID == unitReport.ProjID && zamenReportUnit.GroupZamen == unitReport.GroupZamen &&
                    zamenReportUnit.NumberZamen > 0 && /*заменитель должен быть того же типа*/unitReport.GroupForOrder == zamenReportUnit.GroupForOrder)
                {
                    zamenitelList.Add(zamenReportUnit);
                }
            }

            return zamenitelList;
        }

        void WriteWorkTableLine(DataUnit dataUnit, MeasuredValue count, bool zamena)
        {
            DocumentTreeNode nodeWorkTableLine =
                _document.Template.FindNode("MaterialLine").CloneFromTemplate(true, true);

            _workTable.AddChildNode(nodeWorkTableLine, false, false);
            //заполним данные
            ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Count", count.Value.ToString());
            ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "EdIzm",
                MeasureHelper.FindDescriptor(count.MeasureID).ShortName);
            //Наименование материала
            if (!zamena)
            {

                ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Number", (++_numberLine).ToString());
                ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Name", dataUnit.Name);
            }
            else
            {
                ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Zamena", dataUnit.Name);
            }
        }

        void WriteWorkTableErrorLine(string errorMess)
        {
            DocumentTreeNode nodeWorkTableLine =
                _document.Template.FindNode("MaterialLine").CloneFromTemplate(true, true);

            _workTable.AddChildNode(nodeWorkTableLine, false, false);

            ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Number", "#");
            ScenarioFunc.WriteNodeRow(nodeWorkTableLine, "Name", "ОШИБКА: " + errorMess);
        }

        void WriteProductionListData(IDBObject zakazObject)
        {
            //Ведомость №
            IDBAttribute attrNumberPV =
                zakazObject.GetAttributeByGuid(new Guid(MRP2Consts.attributeProductionListNumber), false);
            if (attrNumberPV != null)
            {
                TextData desText = _document.FindFirstNodeFromTemplate_Recursive("ZakazNumber") as TextData;
                desText.AssignText(attrNumberPV.AsString, false, false, false);

                // запишем обозначение объекта документа
                _document.Designation = attrNumberPV.AsString + " " + "ВМ";
                _document.FileName = "Ведомость материалов";
            }

        }

        void ReportedGroup(SortedDictionary<DataUnit, List<DataUnitReport>> objectForReportDictionary,
            Dictionary<long, List<DataUnitReport>> objIdReportDictionary, List<DataUnitReport> zamenReportUnits,
            Dictionary<long, DataUnit> dataUnits)
        {
            if (objIdReportDictionary.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<DataUnit, List<DataUnitReport>> objectForReport in objectForReportDictionary)
            {
                List<DataUnitReport> noGroupZamen = objectForReport.Value.Where(a => a.GroupZamen == 0).ToList();
                if (noGroupZamen.Count > 0)
                {
                    ReportObjectLine(noGroupZamen, objIdReportDictionary, objectForReport.Key);
                }

                List<DataUnitReport> groupZamen =
                    objectForReport.Value.Where(a => a.GroupZamen > 0 && a.NumberZamen == 0).ToList();

                foreach (DataUnitReport unitReport in groupZamen)
                {
                    ReportObjectZamenLine(unitReport, objIdReportDictionary, zamenReportUnits, dataUnits);
                }
            }
        }

        bool ReportObjectLine(List<DataUnitReport> objectUnits,
            Dictionary<long, List<DataUnitReport>> objIdReportDictionary, DataUnit dataUnit)
        {
            MeasuredValue allCount = null;
            foreach (DataUnitReport objectUnit in objectUnits)
            {
                string errorMess = string.Empty;
                MeasuredValue count = AllCount(objectUnit, objIdReportDictionary, out errorMess);
                if (count == null || errorMess != String.Empty)
                {
                    errorMessList.Add(MeasuredError(objectUnit.ObjectID, dataUnit.Name, errorMess));
                    return false;
                }

                if (allCount == null)
                {
                    allCount = new MeasuredValue(count.Value, count.MeasureID);
                }
                else
                {
                    try
                    {
                        allCount.Add(count);
                    }
                    catch (Exception e)
                    {
                        errorMessList.Add(MeasuredError(objectUnit.ObjectID, dataUnit.Name, e.Message));
                        return false;
                    }

                }
            }

            if (allCount != null)
            {
                WriteWorkTableLine(dataUnit, allCount, false);
                return true;
            }

            return false;
        }

        bool ReportObjectZamenLine(DataUnitReport unitReport,
            Dictionary<long, List<DataUnitReport>> objIdReportDictionary, List<DataUnitReport> zamenReportUnits,
            Dictionary<long, DataUnit> dataUnits)
        {
            string errorMess;
            MeasuredValue count = AllCount(unitReport, objIdReportDictionary, out errorMess);
            if (count != null && errorMess == String.Empty)
            {
                WriteWorkTableLine(dataUnits[unitReport.RefInProduct], count, false);
                //Заменители
                List<DataUnitReport> zamenitelUnits = GetZamenitel(unitReport, zamenReportUnits);
                foreach (DataUnitReport zamenitelUnit in zamenitelUnits)
                {
                    MeasuredValue countZamen = AllCount(zamenitelUnit, objIdReportDictionary, out errorMess);
                    if (countZamen != null && errorMess == String.Empty)
                    {
                        /// вывод на бланк
                        WriteWorkTableLine(dataUnits[zamenitelUnit.RefInProduct], countZamen, true);
                    }
                    else
                    {
                        errorMessList.Add(MeasuredError(zamenitelUnit.RefInProduct,
                            dataUnits[zamenitelUnit.RefInProduct].Name, errorMess));
                        return false;
                    }
                }

                return true;
            }
            else
            {
                errorMessList.Add(MeasuredError(unitReport.RefInProduct, dataUnits[unitReport.RefInProduct].Name,
                    errorMess));
                return false;
            }
        }

        string MeasuredError(long idObject, string objName, string error)
        {
            return String.Format("При расчете количества объекта id:{0} {1} возникла ошибка: {2}", idObject, objName,
                error);
        }

        private Dictionary<long, DataUnit> GetDataUnit(IUserSession session, IEnumerable<long> idDataUnits,
            Dictionary<int, List<string>> objTypeSortColumn)
        {
            Dictionary<long, DataUnit> dataUnits = new Dictionary<long, DataUnit>();

            List<ObjInfoItem> infoItems = GetInfoItems(session, idDataUnits);

            var groupItems = infoItems.GroupBy(a => a.ObjTypeID);
            foreach (IGrouping<int, ObjInfoItem> groupItem in groupItems)
            {
                List<ColumnDescriptor> column = new List<ColumnDescriptor>();

                column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeF_OBJECT_ID),
                    AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.Guid, SortOrders.NONE, 0));
                column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeF_OBJECT_TYPE),
                    AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.Guid, SortOrders.NONE, 0));

                //Наименование
                column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeName),
                    AttributeSourceTypes.Object, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE, 0));

                List<string> attrGuidList;

                if (objTypeSortColumn.TryGetValue(groupItem.Key, out attrGuidList))
                {
                    foreach (string attrGuid in attrGuidList)
                    {
                        column.Add(new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(attrGuid),
                            AttributeSourceTypes.Object, ColumnContents.String, ColumnNameMapping.Guid, SortOrders.NONE,
                            0));
                    }
                }

                DataTable objDataTable = DataHelper.GetObjectData(groupItem.Key, session, null, column,
                    groupItem.Select(a => a.ObjectID));
                if (objDataTable == null && objDataTable.Rows.Count == 0)
                {
                    continue;
                }

                foreach (DataRow dataRow in objDataTable.Rows)
                {
                    DataUnit dataUnit = new DataUnit(dataRow, attrGuidList);
                    dataUnits.Add(dataUnit.ObjectID, dataUnit);
                }
            }

            return dataUnits;

        }

        //Получить информацию по типам объектов
        private List<ObjInfoItem> GetInfoItems(IUserSession session, IEnumerable<long> idItems)
        {
            List<ObjInfoItem> infoItems = new List<ObjInfoItem>();

            ColumnDescriptor[] column = new ColumnDescriptor[]
            {
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeF_OBJECT_ID),
                    AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.FieldName, SortOrders.NONE, 0),
                new ColumnDescriptor(MetaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeF_OBJECT_TYPE),
                    AttributeSourceTypes.Object, ColumnContents.ID, ColumnNameMapping.FieldName, SortOrders.NONE, 0)
            };

            ConditionStructure[] condition = new ConditionStructure[]
            {
                new ConditionStructure((int)ObligatoryObjectAttributes.F_OBJECT_ID, RelationalOperators.In,
                    idItems.ToArray(), null, LogicalOperators.NONE, 0, false)
            };

            IDBObjectCollection dbObjectCollection = session.GetObjectCollection(-1);

            dbObjectCollection.LocalTypesMode = true;

            DataTable objTable = dbObjectCollection.Select(new DBRecordSetParams(condition, column));

            if (objTable == null || objTable.Rows.Count == 0)
            {
                return infoItems;
            }

            foreach (DataRow objTableRow in objTable.Rows)
            {
                long objId = DataSetProcessor.GetInt64Value(objTableRow, Intermech.Consts.F_OBJECT_ID,
                    Intermech.Consts.UnknownObjectId);
                int objTypeId = DataSetProcessor.GetInt32Value(objTableRow, Intermech.Consts.F_OBJECT_TYPE,
                    Intermech.Consts.UnknownObjectTypeId);

                if (objTypeId != Intermech.Consts.UnknownObjectId)
                {
                    infoItems.Add(new ObjInfoItem(objId, objTypeId));
                }
            }

            return infoItems;
        }
    }

    /// <summary>
    /// Класс данных элемента состава ПВ
    /// </summary>
    public class DataUnitReport : DataUnitBase
    {
        private long _linkId;
        private long _projID;
        private long _refInProduct;
        private DateTime _objModifyDate;
        private MeasuredValue _count;
        private int _groupZamen;
        private int _numberZamen;

        private GroupForOrderMaterial _groupForOrder;

        public DataUnitReport(DataRow rowUnit, long projID) : base(rowUnit)
        {
            GetObjectData();
            GetProjID(projID);
        }

        public DataUnitReport(DataRow rowUnit, string nameCountColumn, long projID = 0) : this(rowUnit, projID)
        {
            if (!string.IsNullOrEmpty(nameCountColumn))
            {
                if (!GetColumnData(rowUnit, new[] { nameCountColumn }, out _count))
                {
                    _count = null;
                }

            }
        }

        public long LinkID
        {
            get { return _linkId; }
        }

        public long ProjID
        {
            get { return _projID; }
        }

        public long RefInProduct
        {
            get
            {
                if (_refInProduct > 0)
                {
                    return _refInProduct;
                }
                else
                {
                    return _objectId;
                }
            }
        }

        public MeasuredValue Count
        {
            get { return _count; }
        }

        public int GroupZamen
        {
            get { return _groupZamen; }
        }

        public int NumberZamen
        {
            get { return _numberZamen; }
        }

        private void GetObjectData()
        {

            if (!GetColumnData(_rowUnit, new string[] { Intermech.Consts.F_PRJLINK_ID }, out _linkId))
            {
                _linkId = -1;
            }

            //Ссылка на изделие для ПВ
            if (!GetColumnData(_rowUnit, new string[] { MRP2Consts.attributeArticleLink }, out _refInProduct))
            {
                _refInProduct = -1;
            }

            //Номер группы заменителей
            if (!GetColumnData(_rowUnit, new string[] { "cad001c0-306c-11d8-b4e9-00304f19f545" }, out _groupZamen))
            {
                _groupZamen = 0;
            }

            //Номер заменителя в группе
            if (!GetColumnData(_rowUnit, new string[] { "cad001c1-306c-11d8-b4e9-00304f19f545" }, out _numberZamen))
            {
                _numberZamen = 0;
            }
        }

        public void GetProjID(long projID)
        {
            if (projID == 0)
            {
                if (!GetColumnData(_rowUnit,
                        new string[] { Intermech.Consts.F_PROJ_ID, SystemGUIDs.attributeF_PROJ_ID }, out _projID))
                {
                    _projID = Intermech.Consts.UnknownObjectId;
                }
            }
            else
            {
                _projID = projID;
            }
        }

        public GroupForOrderMaterial GroupForOrder { get; set; }
    }

    /// <summary>
    /// Класс данных об объекте для отчета
    /// </summary>
    public class DataUnit : DataUnitBase
    {
        private string _name;
        private readonly Dictionary<string, string> _attrSortDictionary = new Dictionary<string, string>();

        public DataUnit(DataRow rowUnit, List<string> attrTypeSort) : base(rowUnit)
        {
            GetObjectData(attrTypeSort);
        }

        public string Name
        {
            get { return _name; }
        }

        public Dictionary<string, string> AttrSortDictionary
        {
            get { return _attrSortDictionary; }
        }

        private void GetObjectData(List<string> attrTypeSort)
        {
            //Наименование
            if (!GetColumnData(_rowUnit, new string[] { SystemGUIDs.attributeName }, out _name))
            {
                _name = String.Empty;
            }

            _attrSortDictionary.Clear();
            if (attrTypeSort == null || attrTypeSort.Count == 0)
            {
                return;
            }

            foreach (string attrSortGuid in attrTypeSort)
            {
                string attrValue;
                if (!GetColumnData(_rowUnit, new string[] { attrSortGuid }, out attrValue))
                {
                    attrValue = String.Empty;
                }

                _attrSortDictionary.Add(attrSortGuid, attrValue);
            }
        }
    }

    /// <summary>
    /// Базовый класс единицы данных для отчета
    /// </summary>
    public abstract class DataUnitBase
    {
        protected DataRow _rowUnit;
        protected long _objectId;
        protected int _typeID;

        protected DataUnitBase(DataRow rowUnit)
        {
            _rowUnit = rowUnit;
            if (!GetColumnData(_rowUnit,
                    new string[] { Intermech.Consts.F_OBJECT_ID, SystemGUIDs.attributeF_OBJECT_ID }, out _objectId))
            {
                _objectId = Intermech.Consts.UnknownObjectId;
            }

            if (!GetColumnData(_rowUnit,
                    new string[] { Intermech.Consts.F_OBJECT_TYPE, SystemGUIDs.attributeF_OBJECT_TYPE }, out _typeID))
            {
                _typeID = Intermech.Consts.UnknownObjectTypeId;
            }
        }

        public long ObjectID
        {
            get { return _objectId; }
        }

        public int TypeID
        {
            get { return _typeID; }
        }

        public static bool GetColumnData(DataRow row, string[] columns, out long value)
        {
            value = new long();
            foreach (string column in columns)
            {
                if (ColumnInRow(row, column))
                {
                    return Int64.TryParse(row[column].ToString(), out value);
                }
            }

            return false;
        }

        public static bool GetColumnData(DataRow row, string[] columns, out int value)
        {
            value = new int();
            foreach (string column in columns)
            {
                if (ColumnInRow(row, column))
                {
                    return Int32.TryParse(row[column].ToString(), out value);
                }
            }

            return false;
        }

        public static bool GetColumnData(DataRow row, string[] columns, out string value)
        {
            value = String.Empty;
            foreach (string column in columns)
            {
                if (ColumnInRow(row, column))
                {
                    value = row[column].ToString();
                    return true;
                }
            }

            return false;
        }

        public static bool GetColumnData(DataRow row, string[] columns, out MeasuredValue value)
        {
            value = null;
            foreach (string column in columns)
            {
                if (ColumnInRow(row, column))
                {
                    value = MeasureHelper.ConvertToMeasuredValue(row[column].ToString(), false);
                }
            }

            if (value != null)
            {
                return true;
            }

            return false;
        }

        private static bool ColumnInRow(DataRow row, string columnName)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                return true;
            }

            return false;
        }
    }

    public enum GroupForOrderMaterial
    {
        None,
        PVMaterial
    }

    /// <summary>
    /// Класс для сортировки по значениям нескольких атрибутов
    /// </summary>
    public class DataUnitComparer : IComparer<DataUnit>
    {
        private readonly List<string> _attrForSorted;

        public DataUnitComparer(List<string> attrForSorted)
        {
            _attrForSorted = attrForSorted ?? new List<string>();
        }

        public int Compare(DataUnit unit1, DataUnit unit2)
        {
            int compareRezult = 0;
            Dictionary<string, string> sortedAttrUnit1 = unit1.AttrSortDictionary;
            Dictionary<string, string> sortedAttrUnit2 = unit2.AttrSortDictionary;
            foreach (string attr in _attrForSorted)
            {
                // если оба объекта не содержат атрибут
                if (!sortedAttrUnit1.ContainsKey(attr) && !sortedAttrUnit2.ContainsKey(attr))
                {
                    continue;
                }

                // если есть атрибут в обоих объектах
                if (sortedAttrUnit1.ContainsKey(attr) && sortedAttrUnit2.ContainsKey(attr))
                {
                    if (string.IsNullOrEmpty(sortedAttrUnit1[attr]) && string.IsNullOrEmpty(sortedAttrUnit2[attr]))
                    {
                        continue;
                    }

                    if (sortedAttrUnit1[attr] == String.Empty || sortedAttrUnit2[attr] == String.Empty)
                    {
                        compareRezult = sortedAttrUnit2[attr].Length - sortedAttrUnit1[attr].Length;
                        if (compareRezult != 0)
                        {
                            return compareRezult;
                        }
                    }

                    compareRezult = string.Compare(sortedAttrUnit1[attr], sortedAttrUnit2[attr],
                        StringComparison.InvariantCultureIgnoreCase);
                    if (compareRezult == 0)
                    {
                        continue;
                    }
                }

                //если атрибут есть только в первом объекте
                if (!sortedAttrUnit1.ContainsKey(attr) && sortedAttrUnit1[attr].Length > 0)
                {
                    return -1;
                }

                //если атрибут есть только во втором объекте
                if (!sortedAttrUnit2.ContainsKey(attr) && sortedAttrUnit2[attr].Length > 0)
                {
                    return 1;
                }
            }

            compareRezult = string.Compare(unit1.Name, unit2.Name, StringComparison.InvariantCultureIgnoreCase);
            if (compareRezult == 0)
            {
                return -1;
            }

            return compareRezult;
        }
    }
}

