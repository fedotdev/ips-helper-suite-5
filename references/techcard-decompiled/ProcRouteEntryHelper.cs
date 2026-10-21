using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Intermech.Diagnostics;
using Intermech.Interfaces;
using Intermech.Interfaces.Client;
using Intermech.Interfaces.Compositions;
using Intermech.Interfaces.Compositions.CompositionService;
using Intermech.Interfaces.TechCard;
using Intermech.Kernel.Search;
using Intermech.MRP2;
using Intermech.Navigator.Interfaces;
using Intermech.TechCard.Client.Common;
using Intermech.TechCard.Client.UI.Controls;

namespace Intermech.TechCard.Client.ObjectTypeSupport.ProcessRoutingEntry;

public static class ProcRouteEntryHelper
{
	public static long CreateProcRouteEntry(IUserSession session, ObjInfoItem projItem, bool createIsEmpty, ISelectedItems selectedItems)
	{
		IList<AttributeValues> attributeValueList = null;
		if (selectedItems != null)
		{
			if (!createIsEmpty & !GetCurrentEntryAttributeValues(session, selectedItems, out attributeValueList))
			{
				return 0L;
			}
		}
		else if (!createIsEmpty)
		{
			return 0L;
		}
		return CreateProcRouteEntry(session, projItem, attributeValueList);
	}

	public static long CreateProcRouteEntry([NotNull] IUserSession session, [NotNull] ObjInfoItem projInfoItem, [CanBeNull] IEnumerable<AttributeValues> attributeValueList)
	{
		if (session == null)
		{
			throw new ArgumentNullException("session");
		}
		if (projInfoItem == null)
		{
			throw new ArgumentNullException("projInfoItem");
		}
		IMSApplicability applicability = MetaDataHelper.GetApplicability(projInfoItem.ObjTypeID, TechCardConsts.ObjectTypes.ProcRoutingEntryID, TechCardConsts.RelTypes.TechRelationID);
		if (applicability == null || applicability.ApplicabilityMode == ApplicabilityModes.Disabled)
		{
			return 0L;
		}
		IDBObject @object = session.GetObject(projInfoItem.ObjectID, throwNotFoundException: true);
		IDBObjectCollection objectCollection = session.GetObjectCollection(TechCardConsts.ObjectTypes.ProcRoutingEntryID);
		if (objectCollection == null)
		{
			return 0L;
		}
		List<IDBRelation> list = new List<IDBRelation>();
		IDBObject iDBObject = objectCollection.Create();
		if (attributeValueList != null && attributeValueList.Any())
		{
			iDBObject.SetAttributesValues(attributeValueList.ToArray());
		}
		IDBRelationCollection relationCollection = session.GetRelationCollection(TechCardConsts.RelTypes.TechRelationID);
		TechcardClientUtils.StartCreateRelations(@object.ObjectID, session);
		try
		{
			list.Add(TechcardClientUtils.CreateRelation(relationCollection, @object, iDBObject));
			if (iDBObject.IsCreationMode)
			{
				iDBObject.CommitCreation(deleteOnException: false);
			}
		}
		finally
		{
			TechcardClientUtils.StopCreateRelations(session);
		}
		if (list.Count != 0)
		{
			NotificationQueue notificationQueue = new NotificationQueue();
			notificationQueue.QueueEvent(new DBRelationsEventArgs("RelationsCreated", list.Select((IDBRelation item) => item.RelationID).ToList(), list.Select((IDBRelation item) => item.ProjID).ToList(), null, list.Select((IDBRelation item) => item.RelationType).ToList()));
			notificationQueue.FlushQueue();
		}
		return iDBObject.ObjectID;
	}

	public static bool GetCurrentEntryAttributeValues([NotNull] IUserSession session, [NotNull] ISelectedItems selectedItems, out IList<AttributeValues> attributeValueList)
	{
		attributeValueList = new List<AttributeValues>();
		if (!TechcardClientControlsUtils.GetItemsApplicabilityInfo(selectedItems ?? throw new ArgumentNullException("selectedItems"), ApplicationServices.Container, out var relObjInfoItems))
		{
			return false;
		}
		List<RelObjInfoItem> list = relObjInfoItems as List<RelObjInfoItem>;
		if (list == null)
		{
			list = relObjInfoItems.ToList();
		}
		ICollection<int> articleObjectTypes = TechCardConsts.Utils.GetPossibleArticleChildrenObjectTypes();
		RelObjInfoItem relObjInfoItem = list.FirstOrDefault((RelObjInfoItem a) => articleObjectTypes.Contains(a.PartInfo.ObjTypeID));
		if (relObjInfoItem == null)
		{
			return false;
		}
		ObjInfoItem partInfo = relObjInfoItem.PartInfo;
		if (MetaDataHelper.IsObjectTypeChildOf(partInfo.ObjTypeID, MRP2Consts.objtypeIdExitAssembly))
		{
			object objectAttributeValueByGuid = session.GetObjectAttributeValueByGuid(partInfo.ObjectID, TechCardConsts.AttributeTypes.ProductionObjectUIDAttrGuid);
			attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfExitAssemblyAttrID, objectAttributeValueByGuid));
		}
		ObjInfoItem projInfo = relObjInfoItem.ProjInfo;
		if (projInfo != null && !projInfo.HasEmptyInfo)
		{
			if (MetaDataHelper.IsObjectTypeChildOf(projInfo.ObjTypeID, TechCardConsts.ObjectTypes.ArticleBaseID) || MetaDataHelper.IsObjectTypeChildOf(projInfo.ObjTypeID, TechCardConsts.ObjectTypes.ZakazObjectID))
			{
				attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfAssemblyObjectAttrID, GetItemID(projInfo)));
			}
			if (MetaDataHelper.IsObjectTypeChildOf(projInfo.ObjTypeID, MetaDataHelper.GetObjectTypeID("cadd9a5d-306c-11d8-b4e9-00304f19f545")))
			{
				object objectAttributeValueByGuid2 = session.GetObjectAttributeValueByGuid(projInfo.ObjectID, TechCardConsts.AttributeTypes.ProductionObjectUIDAttrGuid);
				attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfAssemblyCopyAttrID, objectAttributeValueByGuid2));
			}
			for (int i = list.IndexOf(relObjInfoItem); i < list.Count; i++)
			{
				ObjInfoItem projInfo2 = list[i].ProjInfo;
				if (projInfo2 == null || projInfo2.HasEmptyInfo)
				{
					break;
				}
				if (MetaDataHelper.IsObjectTypeChildOf(projInfo2.ObjTypeID, MRP2Consts.objtypeIdExitAssembly))
				{
					object objectAttributeValueByGuid3 = session.GetObjectAttributeValueByGuid(projInfo2.ObjectID, TechCardConsts.AttributeTypes.ProductionObjectUIDAttrGuid);
					attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfExitAssemblyAttrID, objectAttributeValueByGuid3));
				}
				if (MetaDataHelper.IsObjectTypeChildOf(projInfo2.ObjTypeID, TechCardConsts.ObjectTypes.ZakazObjectID))
				{
					attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfOrderObjectAttrID, GetItemID(projInfo2)));
				}
				if (MetaDataHelper.IsObjectTypeChildOf(projInfo2.ObjTypeID, MRP2Consts.objtypeIdProductionLists))
				{
					attributeValueList.Add(new AttributeValues(TechCardConsts.AttributeTypes.MemberOfProductionReportObjectAttrID, GetItemID(projInfo2)));
				}
			}
			return attributeValueList.Any();
		}
		return false;
	}

	public static IList<long> GetProcRouteEntryWithValues([NotNull] IUserSession session, IEnumerable<ObjInfoItem> projItems, IEnumerable<int> relTypes, IList<AttributeValues> attributeValueList)
	{
		ICompositionLoadService service = ServiceUtils.GetService<ICompositionLoadService>(session, throwIfNotFound: true);
		List<ConditionStructure> list = new List<ConditionStructure>();
		foreach (AttributeValues attributeValue in attributeValueList)
		{
			IMSAttributeType attributeType = MetaDataHelper.GetAttributeType(attributeValue.AttributeID);
			if (attributeType != null)
			{
				ConditionStructure item = new ConditionStructure(attributeValue.AttributeID, RelationalOperators.Equal, attributeValue.Value, null, LogicalOperators.AND, 0, caseSensitive: false, AttributeSourceTypes.Object);
				FieldTypes fieldType = attributeType.FieldType;
				if (fieldType == FieldTypes.ftObjectLink || fieldType == FieldTypes.ftObjectLinkByID)
				{
					item.Content = ColumnContents.ID;
				}
				list.Add(item);
			}
		}
		list.Add(new ConditionStructure(-7, RelationalOperators.Equal, TechCardConsts.ObjectTypes.ProcRoutingEntryID, null, LogicalOperators.NONE, 0, caseSensitive: false, AttributeSourceTypes.Object));
		ColumnDescriptor[] columns = new ColumnDescriptor[1]
		{
			new ColumnDescriptor(-2)
		};
		CompositionLoadingParams loadingParams = new CompositionLoadingParams(projItems, new int[1] { TechCardConsts.ObjectTypes.ProcRoutingEntryID }, relTypes, columns, list, composition: true, grouping: false)
		{
			LoadLevels = 1
		};
		DataTable dataTable = service.LoadComplexCompositions(session, loadingParams);
		if (dataTable == null)
		{
			return new List<long>();
		}
		return (from row in dataTable.AsEnumerable()
			select DataSetProcessor.GetInt64Value(row, 0, 0L)).ToList();
	}

	private static long GetItemID(ObjInfoItem infoItem)
	{
		if (infoItem is ObjInfoIDItem objInfoIDItem)
		{
			return objInfoIDItem.ID;
		}
		return ApplicationServices.Container.GetService<IObjectsInfoCache>().GetObjectInfo(infoItem.ObjectID).ID;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '8.2.0.7535-95108c96')
