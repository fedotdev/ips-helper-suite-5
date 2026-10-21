using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Intermech.Expert;
using Intermech.Extensions;
using Intermech.Interfaces;
using Intermech.Interfaces.AutoSelection;
using Intermech.Interfaces.AutoSelection.AutoSelectionLog;
using Intermech.Interfaces.Client;
using Intermech.Interfaces.Compositions;
using Intermech.Interfaces.Compositions.CompositionService;
using Intermech.Interfaces.TechCard;
using Intermech.Interfaces.TechCard.Services.ClassifyObject;
using Intermech.Kernel.Search;
using Intermech.Navigator.ContextMenu;
using Intermech.Navigator.Interfaces;
using Intermech.Search;
using Intermech.Search.UI;
using Intermech.TechCard.Client.Common;
using Intermech.TechCard.Client.ObjectTypeSupport.ProcessRoutingEntry;
using Intermech.TechCard.Client.Services.ClassifyObject;
using Intermech.TechCard.Client.UI.Controls;

namespace Intermech.TechCard.Client.TcObjectsTypes;

public static class ProcRouteHelper
{
	public static object RouteProcDefaultAttrValue
	{
		get
		{
			object obj = DBNull.Value;
			IMSAttributeType attributeType = MetaDataHelper.GetAttributeType(TechCardConsts.AttributeTypes.ProcRouteDefaultAttrID);
			if (attributeType != null)
			{
				obj = attributeType.DefaultValue;
				if ((obj == null || obj == DBNull.Value || obj.ToString() == string.Empty) && attributeType.PossibleValues != null && attributeType.PossibleValues.Count > 0)
				{
					obj = attributeType.PossibleValues[0];
				}
			}
			if (obj == null || obj == DBNull.Value || obj.ToString() == string.Empty)
			{
				obj = "Default";
			}
			return obj;
		}
	}

	public static long GetDefaultProcRouteForArticle(long objArtId, IUserSession session, bool createIfNotFound = true)
	{
		IList<long> createdObjects;
		return GetDefaultProcRouteForArticle(objArtId, session, createIfNotFound, out createdObjects);
	}

	public static long GetDefaultProcRouteForArticle(long objArtId, IUserSession session, bool createIfNotFound, out IList<long> createdObjects)
	{
		long value = 0L;
		if (objArtId == 0L || session == null)
		{
			createdObjects = new List<long>();
			return value;
		}
		if (!GetDefaultProcRouteForArticles(new long[1] { objArtId }, session, createIfNotFound, out createdObjects).TryGetValue(objArtId, out value))
		{
			value = 0L;
		}
		return value;
	}

	public static Dictionary<long, long> GetDefaultProcRouteForArticles(IList<long> objArtList, IUserSession session, bool createIfNotFound = true)
	{
		IList<long> createdObjects;
		return GetDefaultProcRouteForArticles(objArtList, session, createIfNotFound, out createdObjects);
	}

	public static Dictionary<long, long> GetDefaultProcRouteForArticles(IList<long> objArtList, IUserSession session, bool createIfNotFound, out IList<long> createdObjects)
	{
		createdObjects = new List<long>();
		Dictionary<long, long> dictionary = new Dictionary<long, long>();
		if (session == null)
		{
			return dictionary;
		}
		if (objArtList == null || objArtList.Count == 0)
		{
			return dictionary;
		}
		List<ColumnDescriptor> list = new List<ColumnDescriptor>(RelObjInfoDbScheme<ObjInfoItem>.GetSourceTableColumns())
		{
			new ColumnDescriptor(TechCardConsts.AttributeTypes.ProcRouteDefaultAttrID, AttributeSourceTypes.Object, ColumnContents.Text, ColumnNameMapping.Guid, SortOrders.NONE, 0)
		};
		ConditionStructure[] conditions = new ConditionStructure[2]
		{
			new ConditionStructure(-7, RelationalOperators.In, MetaDataHelper.GetObjectTypeChildrenIDRecursive(TechCardConsts.ObjectTypes.ProcRoutingID).ToArray(), LogicalOperators.AND, 0, caseSensitive: false),
			new ConditionStructure(TechCardConsts.AttributeTypes.ProcRouteDefaultAttrID, RelationalOperators.NotEmpty, null, null, LogicalOperators.AND, 0, caseSensitive: false, AttributeSourceTypes.Object)
		};
		DataTable childSostavData = DataHelper.GetChildSostavData(ObjInfoHelper.GetObjectInfoList(objArtList), session, new int[1] { TechCardConsts.RelTypes.TechRelationID }, recursive: false, new DBRecordSetParams(conditions, list.ToArray(), 0L));
		if (childSostavData != null)
		{
			foreach (DataRow item in from item in childSostavData.AsEnumerable()
				where !string.IsNullOrEmpty(Convert.ToString(item[TechCardConsts.AttributeTypes.ProcRouteDefaultAttrGuid.ToString()]))
				select item)
			{
				dictionary[DataSetProcessor.GetInt64Value(item["F_PROJ_ID"], 0L)] = DataSetProcessor.GetInt64Value(item["F_OBJECT_ID"], 0L);
			}
		}
		if (!createIfNotFound || dictionary.Count == objArtList.Count)
		{
			return dictionary;
		}
		NotificationQueue notificationQueue = new NotificationQueue();
		IDBObjectCollection objectCollection = session.GetObjectCollection(TechCardConsts.ObjectTypes.ProcRoutingID);
		IDBRelationCollection relationCollection = session.GetRelationCollection(TechCardConsts.RelTypes.TechRelationID);
		List<ObjInfoItem> extraContextObjectItems = new List<ObjInfoItem>();
		IList<AttributeValues> entryAttributeValueList = null;
		IInvokeService service = ServiceUtils.GetService<IInvokeService>(ServicesManager.ServiceContainer, throwIfNotFound: true);
		service.InvokeAction(-1, delegate
		{
			ISelectedItems selectedItems = SelectedItemsHelper.GetNavigatorSelection() ?? EmptySelectedItems.Value;
			if (TechcardClientControlsUtils.GetItemsApplicabilityInfo(selectedItems, ApplicationServices.Container, out var relObjInfoItems))
			{
				extraContextObjectItems.AddRange(relObjInfoItems.Select((RelObjInfoItem item) => item.ProjInfo));
			}
			ProcRouteEntryHelper.GetCurrentEntryAttributeValues(session, selectedItems, out entryAttributeValueList);
		});
		IAutoSelectionService autoSelectionService = ServiceUtils.GetService<IAutoSelectionService>(ApplicationServices.Container, throwIfNotFound: false);
		foreach (long objArt in objArtList)
		{
			if (dictionary.ContainsKey(objArt))
			{
				continue;
			}
			QuickObjectInfo objectInfo = session.GetObjectInfo(objArt);
			if (objectInfo.Empty)
			{
				continue;
			}
			IDBObject iDBObject = objectCollection.Create();
			ObjInfoItem objInfoItem = new ObjInfoItem(iDBObject);
			ObjInfoItem objInfoItem2 = new ObjInfoItem(objectInfo.ObjectID, objectInfo.ObjectTypeID);
			IList<AttributeValues> list2 = TechCardClientClassifyObjectHelper.ClassifyObject(session, new TechCardClassifyObjectParams(objInfoItem, objInfoItem2)
			{
				ExtraContextObjInfoItems = extraContextObjectItems
			}) ?? new List<AttributeValues>();
			list2.Add(new AttributeValues(TechCardConsts.AttributeTypes.ProcRouteDefaultAttrID, RouteProcDefaultAttrValue));
			iDBObject.SetAttributesValues(list2.ToArray());
			TechcardClientUtils.StartCreateRelations(objInfoItem2.ObjectID, session);
			try
			{
				IDBRelation iDBRelation = TechcardClientUtils.CreateRelation(relationCollection, objInfoItem2, objInfoItem, NewRelationProperties.Empty);
				if (iDBRelation == null)
				{
					continue;
				}
				notificationQueue.QueueEvent(new DBRelationsEventArgs("RelationsCreated", new long[1] { iDBRelation.RelationID }, new long[1] { iDBRelation.ProjID }, null, new int[1] { iDBRelation.RelationType }));
				if (iDBObject.IsCreationMode)
				{
					iDBObject.CommitCreation(deleteOnException: false, iDBObject.ObjectModifyMode == ObjectModifyModes.Checkout);
					ObjInfoItem projInfoItem = new ObjInfoItem(iDBObject);
					if (entryAttributeValueList.Any())
					{
						ProcRouteEntryHelper.CreateProcRouteEntry(session, projInfoItem, entryAttributeValueList);
					}
				}
				if (autoSelectionService != null)
				{
					AutoSelectionParams autoSelectionParam = new AutoSelectionParams(iDBObject.ObjectID, new long[1] { objInfoItem2.ObjectID }, new long[1] { iDBRelation.RelationID }, AutoSelectionMode.AutoObject);
					service.InvokeAction(-1, delegate
					{
						autoSelectionService.ExecuteSelection(autoSelectionParam);
						IAutoSelectionLog getLastExecuteLog = autoSelectionService.GetLastExecuteLog;
						if (getLastExecuteLog?.ModificationHistory == null)
						{
							return;
						}
						foreach (NotificationEventArgs item2 in NotificationHelper.GetQueue(getLastExecuteLog.ModificationHistory.ToList()).ToEnumerable())
						{
							notificationQueue.QueueEvent(item2);
						}
					});
				}
				goto IL_03b4;
			}
			finally
			{
				TechcardClientUtils.StopCreateRelations(session);
			}
			IL_03b4:
			createdObjects.Add(iDBObject.ObjectID);
			dictionary.Add(objArt, iDBObject.ObjectID);
		}
		notificationQueue.FlushQueue();
		return dictionary;
	}

	public static List<long> GetArticlesForProcRoute(long procRouteId, IUserSession session)
	{
		List<long> list = new List<long>();
		if (procRouteId == 0L || session == null)
		{
			return list;
		}
		ICollection<int> possibleArticleChildrenObjectTypes = TechCardConsts.Utils.GetPossibleArticleChildrenObjectTypes();
		List<int> objectTypeChildrenIDRecursive = MetaDataHelper.GetObjectTypeChildrenIDRecursive(TechCardConsts.ObjectTypes.ProcRoutingID);
		DataTable applicabilitiesList = session.GetRelationsApplicabilityCollection().GetApplicabilitiesList(TechCardConsts.RelTypes.TechRelationID, TechCardConsts.ObjectTypes.ProcRoutingID, -1);
		if (applicabilitiesList != null)
		{
			int idxFldInObjType = applicabilitiesList.Columns.IndexOf("F_INOBJECT_TYPE");
			possibleArticleChildrenObjectTypes.AddRange(from row in applicabilitiesList.AsEnumerable()
				select Convert.ToInt32(row[idxFldInObjType]));
		}
		ColumnDescriptor[] columns = new ColumnDescriptor[2]
		{
			new ColumnDescriptor(-21, AttributeSourceTypes.Auto, ColumnContents.Text, ColumnNameMapping.FieldName, SortOrders.NONE, 0),
			new ColumnDescriptor(-7, AttributeSourceTypes.Auto, ColumnContents.Text, ColumnNameMapping.FieldName, SortOrders.NONE, 0)
		};
		ICompositionLoadService service = ServiceUtils.GetService<ICompositionLoadService>(session, throwIfNotFound: true);
		CompositionLoadingParams loadingParams = new CompositionLoadingParams(new ObjInfoItem[1]
		{
			new ObjInfoItem(procRouteId)
		}, possibleArticleChildrenObjectTypes, objectTypeChildrenIDRecursive, new int[1] { TechCardConsts.RelTypes.TechRelationID }, columns, null, composition: false, grouping: false, 2, null, DataHelper.Consts.cnt_def_filtrationRule);
		DataTable dataTable = service.LoadComplexCompositions(session.SessionGUID, loadingParams);
		if (dataTable != null && dataTable.Rows.Count > 0)
		{
			foreach (DataRow row in dataTable.Rows)
			{
				long num = Convert.ToInt64(row["F_PROJ_ID"]);
				if (num != 0L)
				{
					list.Add(num);
				}
			}
		}
		return list;
	}

	public static bool GetArticleContextInfo(long artObjId, out long projArtObjId, out long zakazObjId)
	{
		projArtObjId = 0L;
		zakazObjId = 0L;
		if (!TechcardClientControlsUtils.GetItemsApplicabilityInfo(Intermech.Navigator.ContextMenu.ObjectExtensions.GetItems(artObjId), ApplicationServices.Container, out var relObjInfoItems))
		{
			return false;
		}
		RelObjInfoItem relObjInfoItem = relObjInfoItems.FirstOrDefault();
		if (ObjInfoItem.IsEmpty(relObjInfoItem?.ProjInfo))
		{
			return false;
		}
		if (MetaDataHelper.IsObjectTypeChildOf(relObjInfoItem.ProjInfo.ObjTypeID, TechCardConsts.ObjectTypes.ZakazObjectID))
		{
			projArtObjId = relObjInfoItem.ProjInfo.ObjectID;
			zakazObjId = relObjInfoItem.ProjInfo.ObjectID;
			return true;
		}
		ICollection<int> possibleArticleChildrenObjectTypes = TechCardConsts.Utils.GetPossibleArticleChildrenObjectTypes();
		if (!possibleArticleChildrenObjectTypes.Contains(relObjInfoItem.ProjInfo.ObjTypeID))
		{
			return false;
		}
		projArtObjId = relObjInfoItem.ProjInfo.ObjectID;
		foreach (RelObjInfoItem item in relObjInfoItems)
		{
			if (ObjInfoItem.IsEmpty(item?.ProjInfo))
			{
				break;
			}
			if (MetaDataHelper.IsObjectTypeChildOf(item.ProjInfo.ObjTypeID, TechCardConsts.ObjectTypes.ZakazObjectID))
			{
				zakazObjId = item.ProjInfo.ObjectID;
				break;
			}
			if (!possibleArticleChildrenObjectTypes.Contains(item.ProjInfo.ObjTypeID))
			{
				break;
			}
		}
		return true;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '8.2.0.7535-95108c96')
