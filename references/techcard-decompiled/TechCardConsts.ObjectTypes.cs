using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Intermech.Collections;
using Intermech.Expert;
using Intermech.Extensions;
using Intermech.Kernel.Search;
using Intermech.Localization;

namespace Intermech.Interfaces.TechCard;

public static class TechCardConsts
{
	public static class Consts
	{
		public static int SQL_PACKET_SIZE = 400;
	}

	public static class Params
	{
		public static readonly string MdeInheritArchiveRightsFromTechProc = "Techcard.InheritArchiveRights";

		public static readonly string ObjWithCadmechDraft2CopyPluginData = "ObjWithCadmechDraft2CopyPluginData";

		public static readonly string TechProcGroupLinkTaskPluginData = "TechProcGroupLinkTaskPluginData";
	}

	public static class Objects
	{
		public static readonly Guid AssemblingProductionObjectGUID = new Guid("cad0141d-306c-11d8-b4e9-00304f19f545");

		public static readonly Guid TimeMeasureObjectGuid = new Guid("cad002e0-306c-11d8-b4e9-00304f19f545");
	}

	public class ObjectTypes
	{
		private static IReadOnlyList<int> _techBaseUserObjectIds;

		private static IReadOnlyList<int> _techSpecialObjectIds;

		[IsObjectType(true)]
		public static readonly Guid TechBaseObjectGUID;

		public static readonly Guid[] TechBaseUserObjectGuids;

		public static readonly Guid[] TechSpecialObjectGuids;

		[IsObjectType(true)]
		public static readonly Guid DopPriemGUID;

		[IsObjectType(true)]
		public static readonly Guid DopPriemNormGUID;

		[IsObjectType(true)]
		public static readonly Guid EdinicaSostavaGUID;

		[IsObjectType(true)]
		public static readonly Guid KomlEdinicaGUID;

		[IsObjectType(true)]
		public static readonly Guid SobirEdinicaGUID;

		[IsObjectType(true)]
		public static readonly Guid CommentaryGUID;

		[IsObjectType(true)]
		public static readonly Guid AlbumKTDGUID;

		[IsObjectType(true)]
		public static readonly Guid KomlectVedomGUID;

		[IsObjectType(true)]
		public static readonly Guid KomplectTechDocGUID;

		[IsObjectType(true)]
		public static readonly Guid KomplectTPDocGUID;

		[IsObjectType(true)]
		public static readonly Guid ContrParamBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid GrupContrParamGUID;

		[IsObjectType(true)]
		public static readonly Guid ContrParamGUID;

		[IsObjectType(true)]
		public static readonly Guid TechRoutingObjectGUID;

		[IsObjectType(true)]
		public static readonly Guid ProcRoutingGUID;

		[IsObjectType(true)]
		public static readonly Guid ProcRoutingEntryGUID;

		[IsObjectType(true)]
		[Obsolete("Use ProcRoutingGUID instead")]
		public static readonly Guid MarshrObrabGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MaterialBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MarkaGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MaterialGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MaterialCompositionGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		[Obsolete("Use MaterialCompositionGUID instead")]
		public static readonly Guid MaterialSostavnGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MaterialGroupGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid MaterialSetGUID;

		[IsObjectType(true)]
		public static readonly Guid NormirovanieGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid OborudBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid GrupOborudGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid OborudGUID;

		[IsObjectType(true)]
		public static readonly Guid OperaciyaGUID;

		[IsObjectType(true)]
		public static readonly Guid OperationNormGUID;

		[IsObjectType(true)]
		public static readonly Guid OsnastBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid InstrumentGUID;

		[IsObjectType(true)]
		public static readonly Guid InstrumPosGUID;

		[IsObjectType(true)]
		public static readonly Guid OsnastkaGUID;

		[IsObjectType(true)]
		public static readonly Guid PerehodGUID;

		[IsObjectType(true)]
		public static readonly Guid PerehodNormGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid PersonalBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid GrupPersonalGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid PersonalGUID;

		[IsObjectType(true)]
		public static readonly Guid SurfaceBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid SurfaceSlaveGUID;

		[IsObjectType(true)]
		public static readonly Guid SurfaceMasterGUID;

		[IsObjectType(true)]
		public static readonly Guid SurfaceParamGUID;

		[IsObjectType(true)]
		public static readonly Guid RegimGUID;

		[IsObjectType(true)]
		public static readonly Guid TechProcBaseGUID;

		public static readonly Guid TechProcNormGUID;

		[IsObjectType(true)]
		public static readonly Guid TechProcGroupGUID;

		[IsObjectType(true)]
		public static readonly Guid TechProcEdinGUID;

		[IsObjectType(true)]
		public static readonly Guid TechProcTipovGUID;

		[IsObjectType(true)]
		public static readonly Guid TrebovanBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid TechnTrebovanBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid GrTechnTrebovanGUID;

		[IsObjectType(true)]
		public static readonly Guid TechnTrebovanGUID;

		[IsObjectType(true)]
		public static readonly Guid TechnUslovBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid GrTechnUslovGUID;

		[IsObjectType(true)]
		public static readonly Guid TechnUslovGUID;

		[IsObjectType(true)]
		public static readonly Guid TrebovTechnBezopasnBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid GrTrebovTechnBezopasnGUID;

		[IsObjectType(true)]
		public static readonly Guid TrebovTechnBezopasnGUID;

		[IsObjectType(true)]
		public static readonly Guid DraftBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid DraftGroupGUID;

		[IsObjectType(true)]
		public static readonly Guid DraftGUID;

		[IsObjectType(true)]
		public static readonly Guid DraftOLEGUID;

		[IsObjectType(true)]
		public static readonly Guid DraftCadmechGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid TechBaseDocGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid TechDocReportGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid TechDocGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid ComplectDocBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid ComlectTechDocBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid TechProcElemBaseGUID;

		[IsObjectType(true)]
		public static readonly Guid ZagotGUID;

		[IsObjectType(true)]
		public static readonly Guid CehBaseRouteGUID;

		[IsObjectType(true)]
		public static readonly Guid CehRouteGUID;

		[IsObjectType(true)]
		public static readonly Guid MkRouteGUID;

		[IsObjectType(true)]
		public static readonly Guid SbRouteGUID;

		[IsObjectType(true)]
		public static readonly Guid ElemRouteGUID;

		[IsObjectType(true)]
		public static readonly Guid ElemRouteTemplateGuid;

		[IsObjectType(true)]
		public static readonly Guid TemplRouteBaseGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static Guid SpecialToolOrderGuid;

		private static readonly Guid SpecialToolGuid;

		[IsObjectType(true)]
		public static readonly Guid CehZahodObjectGUID;

		[IsObjectType(true)]
		public static readonly Guid ZagotGroupGUID;

		[IsObjectType(true)]
		public static readonly Guid ZagotInTpGUID;

		[IsObjectType(true)]
		[NotInheritedBaseTechObjType(true)]
		public static readonly Guid TechSketchDocuments;

		[IsObjectType(true)]
		public static readonly Guid FolderGroupForTechProcGUID;

		[IsObjectType(false)]
		public static readonly Guid AttrFormGUID;

		[IsObjectType(false)]
		public static readonly Guid NotionObjectGUID;

		[IsObjectType(false)]
		public static readonly Guid NumerationObjectBaseGUID;

		[IsObjectType(false)]
		public static readonly Guid NumerationRuleGUID;

		[IsObjectType(false)]
		public static readonly Guid NumerationObjectGUID;

		[IsObjectType(false)]
		public static readonly Guid ArticleBaseGUID;

		[IsObjectType(false)]
		public static readonly Guid ArticleCopyBaseGUID;

		[IsObjectType(false)]
		public static readonly Guid WorkTypeBaseObjectGUID;

		[IsObjectType(false)]
		public static readonly Guid ProductTypeObjectGUID;

		[IsObjectType(false)]
		public static readonly Guid WorkTypeObjectGUID;

		[IsObjectType(false)]
		public static readonly Guid ZakazObjectGUID;

		public static readonly Guid ExternalCADModelTypeGuid;

		[Obsolete("Use TechBaseUserObjectGuids instead. Will be removed in IPS 8.0", true)]
		public static Guid TechBaseUserObjectGuid => TechBaseUserObjectGuids.FirstOrDefault();

		public static int TechSketchDocumentsID { get; private set; }

		public static int TechBaseObjectID { get; private set; }

		[Obsolete("User TechBaseUserObjectIds instead. Will be removed in IPS 8.0", true)]
		public static int TechBaseUserObjectId
		{
			get
			{
				if (!TechBaseUserObjectIds.Any())
				{
					return -1;
				}
				return TechBaseUserObjectIds.First();
			}
		}

		public static int TechProcBaseID { get; private set; }

		public static int TechProcGroupID { get; private set; }

		public static int TechProcTipovID { get; private set; }

		public static int TechProcEdinID { get; private set; }

		public static int TechProcElemBaseID { get; private set; }

		public static int ProcRoutingID { get; private set; }

		public static int ProcRoutingEntryID { get; private set; }

		[Obsolete("Use ProcRoutingID instead")]
		public static int MarshrObrabID => ProcRoutingID;

		public static int OperaciyaID { get; private set; }

		public static int PerehodID { get; private set; }

		public static int ElemRouteID { get; private set; }

		public static int ElemRouteTemplateId { get; private set; }

		public static int TemplRouteBaseID { get; private set; }

		public static int ArticleBaseID { get; private set; }

		public static int ArticleCopyBaseID { get; private set; }

		public static int CehRouteID { get; private set; }

		public static int DraftBaseID { get; private set; }

		public static int DraftCadmechID { get; private set; }

		public static int DraftOLEID { get; private set; }

		public static int DopPriemID { get; private set; }

		public static int EdinicaSostavaID { get; private set; }

		public static int SobirEdinicaID { get; private set; }

		public static int KomlEdinicaID { get; private set; }

		public static int OborudBaseID { get; private set; }

		public static int MaterialBaseID { get; private set; }

		public static int MaterialGroupID { get; private set; }

		public static int MaterialSetID { get; private set; }

		public static int MaterialCompositionID { get; private set; }

		public static int ZagotID { get; private set; }

		public static int CehBaseRouteID { get; private set; }

		public static int PersonalBaseID { get; private set; }

		public static int GrupPersonalID { get; private set; }

		public static int PersonalID { get; private set; }

		public static int NormirovanieID { get; private set; }

		public static int SurfaceBaseID { get; private set; }

		public static int SurfaceSlaveID { get; private set; }

		public static int SurfaceMasterID { get; private set; }

		public static int SurfaceParamID { get; private set; }

		public static int CehZahodObjectID { get; private set; }

		public static int TechnTrebovanBaseID { get; private set; }

		public static int TechnUslovBaseID { get; private set; }

		public static int OsnastBaseID { get; private set; }

		public static int CommentaryID { get; private set; }

		public static int ContrParamBaseID { get; private set; }

		public static int InstrumPosID { get; private set; }

		public static int RegimID { get; private set; }

		public static int ZagotGroupID { get; private set; }

		public static int ZagotInTpID { get; private set; }

		public static int FolderGroupForTechProcID { get; private set; }

		public static int MarkaID { get; private set; }

		public static int MaterialID { get; private set; }

		public static int WorkTypeBaseObjectID { get; private set; }

		public static int NumerationRuleID { get; private set; }

		public static int NumerationObjectID { get; private set; }

		public static int ZakazObjectID { get; private set; }

		public static int WorkTypeObjectID { get; private set; }

		public static int TechBaseDocID { get; private set; }

		public static int TechDocReportID { get; private set; }

		public static int TechDocID { get; private set; }

		public static int ComplectDocBaseID { get; private set; }

		public static int ComlectTechDocBaseID { get; private set; }

		public static int SpecialToolOrderID { get; private set; }

		public static int SpecialToolID { get; private set; }

		public static int DocumentBaseID { get; private set; }

		public static IReadOnlyList<int> ArticleObjectTypes { get; private set; }

		public static IReadOnlyList<int> TechAllBaseObjTypes { get; private set; }

		public static IReadOnlyList<int> TechNotInheritedBaseObjTypes { get; private set; }

		public static IReadOnlyList<int> TechCompositionGtpNonCloneTypes { get; private set; }

		public static IEnumerable<int> TechBaseUserObjectIds => _techBaseUserObjectIds;

		public static IEnumerable<int> TechSpecialObjectIds => _techSpecialObjectIds;

		[Obsolete("Use TechCompositionGtpNonCloneTypes instead", false)]
		public static int[] TechCompGtpNonClone => TechCompositionGtpNonCloneTypes.ToArray();

		public static IReadOnlyList<int> TechCompositionNonCloneTypes { get; private set; }

		[Obsolete("Use TechCompositionNonCloneTypes instead", false)]
		public static int[] TechCompNotCopy => TechCompositionNonCloneTypes.ToArray();

		public static IReadOnlyList<int> TechArtCompositionTypes { get; private set; }

		[Obsolete("Use TechArtCompositionTypes instead", false)]
		public static int[] TechArtCompTypes => TechArtCompositionTypes.ToArray();

		private static void InitFields(IMetaDataHelper metaDataHelper)
		{
			TechBaseObjectID = metaDataHelper.GetObjectTypeID(TechBaseObjectGUID, throwExceptionIfNotFound: false);
			TechProcBaseID = metaDataHelper.GetObjectTypeID(TechProcBaseGUID, throwExceptionIfNotFound: false);
			TechProcGroupID = metaDataHelper.GetObjectTypeID(TechProcGroupGUID, throwExceptionIfNotFound: false);
			TechProcTipovID = metaDataHelper.GetObjectTypeID(TechProcTipovGUID, throwExceptionIfNotFound: false);
			TechProcEdinID = metaDataHelper.GetObjectTypeID(TechProcEdinGUID, throwExceptionIfNotFound: false);
			TechProcElemBaseID = metaDataHelper.GetObjectTypeID(TechProcElemBaseGUID, throwExceptionIfNotFound: false);
			ProcRoutingID = metaDataHelper.GetObjectTypeID(ProcRoutingGUID, throwExceptionIfNotFound: false);
			ProcRoutingEntryID = metaDataHelper.GetObjectTypeID(ProcRoutingEntryGUID, throwExceptionIfNotFound: false);
			HashSet<int> hashSet = new HashSet<int>();
			Guid[] techBaseUserObjectGuids = TechBaseUserObjectGuids;
			foreach (Guid objTypeGuid in techBaseUserObjectGuids)
			{
				int objectTypeID = metaDataHelper.GetObjectTypeID(objTypeGuid, throwExceptionIfNotFound: false);
				if (objectTypeID != -1)
				{
					hashSet.Add(objectTypeID);
				}
			}
			_techBaseUserObjectIds = hashSet.ToArray();
			HashSet<int> hashSet2 = new HashSet<int>();
			techBaseUserObjectGuids = TechSpecialObjectGuids;
			foreach (Guid objTypeGuid2 in techBaseUserObjectGuids)
			{
				int objectTypeID2 = metaDataHelper.GetObjectTypeID(objTypeGuid2, throwExceptionIfNotFound: false);
				if (objectTypeID2 != -1)
				{
					hashSet2.Add(objectTypeID2);
				}
			}
			_techSpecialObjectIds = hashSet2.ToArray();
			OperaciyaID = metaDataHelper.GetObjectTypeID(OperaciyaGUID, throwExceptionIfNotFound: false);
			PerehodID = metaDataHelper.GetObjectTypeID(PerehodGUID, throwExceptionIfNotFound: false);
			ElemRouteID = metaDataHelper.GetObjectTypeID(ElemRouteGUID, throwExceptionIfNotFound: false);
			CehRouteID = metaDataHelper.GetObjectTypeID(CehRouteGUID, throwExceptionIfNotFound: false);
			TemplRouteBaseID = metaDataHelper.GetObjectTypeID(TemplRouteBaseGUID, throwExceptionIfNotFound: false);
			ElemRouteTemplateId = metaDataHelper.GetObjectTypeID(ElemRouteTemplateGuid, throwExceptionIfNotFound: false);
			DraftBaseID = metaDataHelper.GetObjectTypeID(DraftBaseGUID, throwExceptionIfNotFound: false);
			DraftCadmechID = metaDataHelper.GetObjectTypeID(DraftCadmechGUID, throwExceptionIfNotFound: false);
			DraftOLEID = metaDataHelper.GetObjectTypeID(DraftOLEGUID, throwExceptionIfNotFound: false);
			DopPriemID = metaDataHelper.GetObjectTypeID(DopPriemGUID, throwExceptionIfNotFound: false);
			EdinicaSostavaID = metaDataHelper.GetObjectTypeID(EdinicaSostavaGUID, throwExceptionIfNotFound: false);
			SobirEdinicaID = metaDataHelper.GetObjectTypeID(SobirEdinicaGUID, throwExceptionIfNotFound: false);
			KomlEdinicaID = metaDataHelper.GetObjectTypeID(KomlEdinicaGUID, throwExceptionIfNotFound: false);
			ArticleBaseID = metaDataHelper.GetObjectTypeID(ArticleBaseGUID, throwExceptionIfNotFound: false);
			ArticleCopyBaseID = metaDataHelper.GetObjectTypeID(ArticleCopyBaseGUID, throwExceptionIfNotFound: false);
			CehZahodObjectID = metaDataHelper.GetObjectTypeID(CehZahodObjectGUID, throwExceptionIfNotFound: false);
			WorkTypeBaseObjectID = metaDataHelper.GetObjectTypeID(WorkTypeBaseObjectGUID, throwExceptionIfNotFound: false);
			NumerationRuleID = metaDataHelper.GetObjectTypeID(NumerationRuleGUID, throwExceptionIfNotFound: false);
			NumerationObjectID = metaDataHelper.GetObjectTypeID(NumerationObjectGUID, throwExceptionIfNotFound: false);
			OborudBaseID = metaDataHelper.GetObjectTypeID(OborudBaseGUID, throwExceptionIfNotFound: false);
			NormirovanieID = metaDataHelper.GetObjectTypeID(NormirovanieGUID, throwExceptionIfNotFound: false);
			MaterialBaseID = metaDataHelper.GetObjectTypeID(MaterialBaseGUID, throwExceptionIfNotFound: false);
			MaterialGroupID = metaDataHelper.GetObjectTypeID(MaterialGroupGUID, throwExceptionIfNotFound: false);
			MaterialSetID = metaDataHelper.GetObjectTypeID(MaterialSetGUID, throwExceptionIfNotFound: false);
			MaterialCompositionID = metaDataHelper.GetObjectTypeID(MaterialCompositionGUID, throwExceptionIfNotFound: false);
			MarkaID = metaDataHelper.GetObjectTypeID(MarkaGUID, throwExceptionIfNotFound: false);
			MaterialID = metaDataHelper.GetObjectTypeID(MaterialGUID, throwExceptionIfNotFound: false);
			ZagotID = metaDataHelper.GetObjectTypeID(ZagotGUID, throwExceptionIfNotFound: false);
			ZagotGroupID = metaDataHelper.GetObjectTypeID(ZagotGroupGUID, throwExceptionIfNotFound: false);
			ZagotInTpID = metaDataHelper.GetObjectTypeID(ZagotInTpGUID, throwExceptionIfNotFound: false);
			CehBaseRouteID = metaDataHelper.GetObjectTypeID(CehBaseRouteGUID, throwExceptionIfNotFound: false);
			ZakazObjectID = metaDataHelper.GetObjectTypeID(ZakazObjectGUID, throwExceptionIfNotFound: false);
			WorkTypeObjectID = metaDataHelper.GetObjectTypeID(WorkTypeObjectGUID, throwExceptionIfNotFound: false);
			TechBaseDocID = metaDataHelper.GetObjectTypeID(TechBaseDocGUID, throwExceptionIfNotFound: false);
			TechDocReportID = metaDataHelper.GetObjectTypeID(TechDocReportGUID, throwExceptionIfNotFound: false);
			TechDocID = metaDataHelper.GetObjectTypeID(TechDocGUID, throwExceptionIfNotFound: false);
			ComplectDocBaseID = metaDataHelper.GetObjectTypeID(ComplectDocBaseGUID, throwExceptionIfNotFound: false);
			ComlectTechDocBaseID = metaDataHelper.GetObjectTypeID(ComlectTechDocBaseGUID, throwExceptionIfNotFound: false);
			PersonalBaseID = metaDataHelper.GetObjectTypeID(PersonalBaseGUID, throwExceptionIfNotFound: false);
			GrupPersonalID = metaDataHelper.GetObjectTypeID(GrupPersonalGUID, throwExceptionIfNotFound: false);
			PersonalID = metaDataHelper.GetObjectTypeID(PersonalGUID, throwExceptionIfNotFound: false);
			SurfaceBaseID = metaDataHelper.GetObjectTypeID(SurfaceBaseGUID, throwExceptionIfNotFound: false);
			SurfaceSlaveID = metaDataHelper.GetObjectTypeID(SurfaceSlaveGUID, throwExceptionIfNotFound: false);
			SurfaceMasterID = metaDataHelper.GetObjectTypeID(SurfaceMasterGUID, throwExceptionIfNotFound: false);
			SurfaceParamID = metaDataHelper.GetObjectTypeID(SurfaceParamGUID, throwExceptionIfNotFound: false);
			SpecialToolID = metaDataHelper.GetObjectTypeID(SpecialToolGuid, throwExceptionIfNotFound: false);
			SpecialToolOrderID = metaDataHelper.GetObjectTypeID(SpecialToolOrderGuid, throwExceptionIfNotFound: false);
			DocumentBaseID = metaDataHelper.GetObjectTypeID("cad00070-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			TechnTrebovanBaseID = metaDataHelper.GetObjectTypeID(TechnTrebovanBaseGUID, throwExceptionIfNotFound: false);
			TechnUslovBaseID = metaDataHelper.GetObjectTypeID(TechnUslovBaseGUID, throwExceptionIfNotFound: false);
			OsnastBaseID = metaDataHelper.GetObjectTypeID(OsnastBaseGUID, throwExceptionIfNotFound: false);
			CommentaryID = metaDataHelper.GetObjectTypeID(CommentaryGUID, throwExceptionIfNotFound: false);
			ContrParamBaseID = metaDataHelper.GetObjectTypeID(ContrParamBaseGUID, throwExceptionIfNotFound: false);
			InstrumPosID = metaDataHelper.GetObjectTypeID(InstrumPosGUID, throwExceptionIfNotFound: false);
			RegimID = metaDataHelper.GetObjectTypeID(RegimGUID, throwExceptionIfNotFound: false);
			TechSketchDocumentsID = metaDataHelper.GetObjectTypeID(TechSketchDocuments, throwExceptionIfNotFound: false);
			FolderGroupForTechProcID = metaDataHelper.GetObjectTypeID(FolderGroupForTechProcGUID, throwExceptionIfNotFound: false);
		}

		private static void InitLists(IMetaDataHelper metaDataHelper)
		{
			InitNotInheritedTypes(metaDataHelper);
			TechCompositionGtpNonCloneTypes = new int[1] { DraftCadmechID };
			TechCompositionNonCloneTypes = new int[0];
			List<int> list = new List<int>(new int[3] { ArticleBaseID, MaterialBaseID, ArticleCopyBaseID });
			HashSet<int> proektRelationIds = new int[2]
			{
				metaDataHelper.GetRelationTypeID(RelTypes.ProektRelationGuid, throwExceptionIfNotFound: false),
				metaDataHelper.GetRelationTypeID(RelTypes.CustomProektRelationGuid, throwExceptionIfNotFound: false)
			}.Where((int relationType) => relationType != -1).LocalToHashSet();
			IEnumerable<int> enumerable = metaDataHelper.GetApplicabilities((IMSApplicability applicatibity) => proektRelationIds.Contains(applicatibity.RelationTypeID))?.Select((IMSApplicability applicatibity) => applicatibity.ChildObjectTypeID);
			if (enumerable != null)
			{
				list.AddRange(enumerable);
			}
			TechArtCompositionTypes = list.Distinct().ToArray();
			HashSet<int> hashSet = new HashSet<int> { ArticleBaseID, ArticleCopyBaseID };
			List<IMSApplicability> objectTypeParentApplicabilities = metaDataHelper.GetObjectTypeParentApplicabilities(ProcRoutingID);
			if (objectTypeParentApplicabilities != null)
			{
				HashSet<int> procRoutingIds = metaDataHelper.GetObjectTypeChildrenIDRecursive(ProcRoutingID)?.LocalToHashSet() ?? new HashSet<int>();
				hashSet.AddRange(from item in objectTypeParentApplicabilities
					where item.RelationTypeID == RelTypes.TechRelationID && item.ApplicabilityMode != ApplicabilityModes.Disabled && !procRoutingIds.Contains(item.InObjectType)
					select item.InObjectType);
			}
			ArticleObjectTypes = hashSet.ToArray();
			List<int> list2 = new List<int>();
			list2.Add(TechBaseObjectID);
			list2.AddRange(TechNotInheritedBaseObjTypes);
			list2.AddRange(TechBaseUserObjectIds);
			TechAllBaseObjTypes = list2;
		}

		private static void InitNotInheritedTypes(IMetaDataHelper metaDataHelper)
		{
			HashedList<int> hashedList = new HashedList<int>();
			FieldInfo[] fields = typeof(ObjectTypes).GetFields();
			int objectTypeID = metaDataHelper.GetObjectTypeID(TechBaseObjectGUID, throwExceptionIfNotFound: false);
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				if (fieldInfo.FieldType != typeof(Guid) || !fieldInfo.IsStatic)
				{
					continue;
				}
				object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(IsObjectTypeAttribute), inherit: false);
				IsObjectTypeAttribute isObjectTypeAttribute = ((customAttributes.Length != 0) ? (customAttributes[0] as IsObjectTypeAttribute) : null);
				if (isObjectTypeAttribute != null && isObjectTypeAttribute.IsTechCardType)
				{
					int objectTypeID2 = metaDataHelper.GetObjectTypeID((Guid)fieldInfo.GetValue(null), throwExceptionIfNotFound: false);
					if (objectTypeID2 == -1)
					{
						continue;
					}
					if (!metaDataHelper.IsObjectTypeChildOf(objectTypeID2, objectTypeID) && !hashedList.Contains(objectTypeID2))
					{
						hashedList.Add(objectTypeID2);
					}
				}
				customAttributes = fieldInfo.GetCustomAttributes(typeof(NotInheritedBaseTechObjType), inherit: false);
				NotInheritedBaseTechObjType notInheritedBaseTechObjType = ((customAttributes.Length != 0) ? (customAttributes[0] as NotInheritedBaseTechObjType) : null);
				if (notInheritedBaseTechObjType != null && notInheritedBaseTechObjType.Value)
				{
					int objectTypeID3 = metaDataHelper.GetObjectTypeID((Guid)fieldInfo.GetValue(null), throwExceptionIfNotFound: false);
					if (objectTypeID3 != -1 && !hashedList.Contains(objectTypeID3))
					{
						hashedList.Add(objectTypeID3);
					}
				}
			}
			TechNotInheritedBaseObjTypes = hashedList;
		}

		static ObjectTypes()
		{
			TechBaseObjectGUID = new Guid("cad00163-306c-11d8-b4e9-00304f19f545");
			TechBaseUserObjectGuids = new Guid[17]
			{
				new Guid("cadd997c-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be0-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be1-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be2-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be3-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be4-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be5-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be6-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be7-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be8-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9be9-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bea-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9beb-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bec-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bed-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bee-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bef-306c-11d8-b4e9-00304f19f545")
			};
			TechSpecialObjectGuids = new Guid[13]
			{
				new Guid("cadd9bf3-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf4-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf5-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf6-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf7-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf8-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bf9-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bfa-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bfb-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bfc-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bfd-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bfe-306c-11d8-b4e9-00304f19f545"),
				new Guid("cadd9bff-306c-11d8-b4e9-00304f19f545")
			};
			DopPriemGUID = new Guid("cad00164-306c-11d8-b4e9-00304f19f545");
			DopPriemNormGUID = new Guid("cad005c1-306c-11d8-b4e9-00304f19f545");
			EdinicaSostavaGUID = new Guid("cad00165-306c-11d8-b4e9-00304f19f545");
			KomlEdinicaGUID = new Guid("cad00166-306c-11d8-b4e9-00304f19f545");
			SobirEdinicaGUID = new Guid("cad00167-306c-11d8-b4e9-00304f19f545");
			CommentaryGUID = new Guid("cad00168-306c-11d8-b4e9-00304f19f545");
			AlbumKTDGUID = new Guid("cad001a8-306c-11d8-b4e9-00304f19f545");
			KomlectVedomGUID = new Guid("cad0016a-306c-11d8-b4e9-00304f19f545");
			KomplectTechDocGUID = new Guid("cad0016b-306c-11d8-b4e9-00304f19f545");
			KomplectTPDocGUID = new Guid("cad009ed-306c-11d8-b4e9-00304f19f545");
			ContrParamBaseGUID = new Guid("cad0016c-306c-11d8-b4e9-00304f19f545");
			GrupContrParamGUID = new Guid("cad0016d-306c-11d8-b4e9-00304f19f545");
			ContrParamGUID = new Guid("cad0016e-306c-11d8-b4e9-00304f19f545");
			TechRoutingObjectGUID = new Guid("cadd9bba-306c-11d8-b4e9-00304f19f545");
			ProcRoutingGUID = new Guid("cad0016f-306c-11d8-b4e9-00304f19f545");
			ProcRoutingEntryGUID = new Guid("cadd9bbb-306c-11d8-b4e9-00304f19f545");
			MarshrObrabGUID = ProcRoutingGUID;
			MaterialBaseGUID = new Guid("cad00170-306c-11d8-b4e9-00304f19f545");
			MarkaGUID = new Guid("cad00171-306c-11d8-b4e9-00304f19f545");
			MaterialGUID = new Guid("cad00172-306c-11d8-b4e9-00304f19f545");
			MaterialCompositionGUID = new Guid("cad00173-306c-11d8-b4e9-00304f19f545");
			MaterialSostavnGUID = MaterialCompositionGUID;
			MaterialGroupGUID = new Guid("cadd9ab5-306c-11d8-b4e9-00304f19f545");
			MaterialSetGUID = new Guid("cadd9abc-306c-11d8-b4e9-00304f19f545");
			NormirovanieGUID = new Guid("cad00174-306c-11d8-b4e9-00304f19f545");
			OborudBaseGUID = new Guid("cad00175-306c-11d8-b4e9-00304f19f545");
			GrupOborudGUID = new Guid("cad00176-306c-11d8-b4e9-00304f19f545");
			OborudGUID = new Guid("cad00177-306c-11d8-b4e9-00304f19f545");
			OperaciyaGUID = new Guid("cad00178-306c-11d8-b4e9-00304f19f545");
			OperationNormGUID = new Guid("CAD005C2-306C-11D8-B4E9-00304F19F545");
			OsnastBaseGUID = new Guid("cad00179-306c-11d8-b4e9-00304f19f545");
			InstrumentGUID = new Guid("cad0017a-306c-11d8-b4e9-00304f19f545");
			InstrumPosGUID = new Guid("cad0017b-306c-11d8-b4e9-00304f19f545");
			OsnastkaGUID = new Guid("cad0017c-306c-11d8-b4e9-00304f19f545");
			PerehodGUID = new Guid("cad0017d-306c-11d8-b4e9-00304f19f545");
			PerehodNormGUID = new Guid("CAD005C3-306C-11D8-B4E9-00304F19F545");
			PersonalBaseGUID = new Guid("cad0017e-306c-11d8-b4e9-00304f19f545");
			GrupPersonalGUID = new Guid("cad0017f-306c-11d8-b4e9-00304f19f545");
			PersonalGUID = new Guid("cad00180-306c-11d8-b4e9-00304f19f545");
			SurfaceBaseGUID = new Guid("cad00181-306c-11d8-b4e9-00304f19f545");
			SurfaceSlaveGUID = new Guid("cad00182-306c-11d8-b4e9-00304f19f545");
			SurfaceMasterGUID = new Guid("cad00183-306c-11d8-b4e9-00304f19f545");
			SurfaceParamGUID = new Guid("cadd961a-306c-11d8-b4e9-00304f19f545");
			RegimGUID = new Guid("cad00184-306c-11d8-b4e9-00304f19f545");
			TechProcBaseGUID = new Guid("cad00185-306c-11d8-b4e9-00304f19f545");
			TechProcNormGUID = new Guid("cad005c4-306c-11d8-b4e9-00304f19f545");
			TechProcGroupGUID = new Guid("cad00186-306c-11d8-b4e9-00304f19f545");
			TechProcEdinGUID = new Guid("cad00187-306c-11d8-b4e9-00304f19f545");
			TechProcTipovGUID = new Guid("cad00188-306c-11d8-b4e9-00304f19f545");
			TrebovanBaseGUID = new Guid("cad00189-306c-11d8-b4e9-00304f19f545");
			TechnTrebovanBaseGUID = new Guid("cad0018a-306c-11d8-b4e9-00304f19f545");
			GrTechnTrebovanGUID = new Guid("cad0018b-306c-11d8-b4e9-00304f19f545");
			TechnTrebovanGUID = new Guid("cad0018c-306c-11d8-b4e9-00304f19f545");
			TechnUslovBaseGUID = new Guid("cad0018d-306c-11d8-b4e9-00304f19f545");
			GrTechnUslovGUID = new Guid("cad0018e-306c-11d8-b4e9-00304f19f545");
			TechnUslovGUID = new Guid("cad0018f-306c-11d8-b4e9-00304f19f545");
			TrebovTechnBezopasnBaseGUID = new Guid("cad00190-306c-11d8-b4e9-00304f19f545");
			GrTrebovTechnBezopasnGUID = new Guid("cad00191-306c-11d8-b4e9-00304f19f545");
			TrebovTechnBezopasnGUID = new Guid("cad00192-306c-11d8-b4e9-00304f19f545");
			DraftBaseGUID = new Guid("cad00193-306c-11d8-b4e9-00304f19f545");
			DraftGroupGUID = new Guid("cad00194-306c-11d8-b4e9-00304f19f545");
			DraftGUID = new Guid("cad00195-306c-11d8-b4e9-00304f19f545");
			DraftOLEGUID = new Guid("cad005bc-306c-11d8-b4e9-00304f19f545");
			DraftCadmechGUID = new Guid("cad005bd-306c-11d8-b4e9-00304f19f545");
			TechBaseDocGUID = new Guid("cad009ec-306c-11d8-b4e9-00304f19f545");
			TechDocReportGUID = new Guid("cad00197-306c-11d8-b4e9-00304f19f545");
			TechDocGUID = new Guid("cad00198-306c-11d8-b4e9-00304f19f545");
			ComplectDocBaseGUID = new Guid("cad00199-306c-11d8-b4e9-00304f19f545");
			ComlectTechDocBaseGUID = new Guid("cad00169-306c-11d8-b4e9-00304f19f545");
			TechProcElemBaseGUID = new Guid("cad001a2-306c-11d8-b4e9-00304f19f545");
			ZagotGUID = new Guid("cad001da-306c-11d8-b4e9-00304f19f545");
			CehBaseRouteGUID = new Guid("cad001e4-306c-11d8-b4e9-00304f19f545");
			CehRouteGUID = new Guid("cad001e5-306c-11d8-b4e9-00304f19f545");
			MkRouteGUID = new Guid("cad001e6-306c-11d8-b4e9-00304f19f545");
			SbRouteGUID = new Guid("cad001e7-306c-11d8-b4e9-00304f19f545");
			ElemRouteGUID = new Guid("cad001e8-306c-11d8-b4e9-00304f19f545");
			ElemRouteTemplateGuid = new Guid("cadd9b7f-306c-11d8-b4e9-00304f19f545");
			TemplRouteBaseGUID = new Guid("cad001fd-306c-11d8-b4e9-00304f19f545");
			SpecialToolOrderGuid = new Guid("cadd951e-306c-11d8-b4e9-00304f19f545");
			SpecialToolGuid = new Guid("cadd955a-306c-11d8-b4e9-00304f19f545");
			CehZahodObjectGUID = new Guid("cad001ff-306c-11d8-b4e9-00304f19f545");
			ZagotGroupGUID = new Guid("cadd9c00-306c-11d8-b4e9-00304f19f545");
			ZagotInTpGUID = new Guid("cadd9c72-306c-11d8-b4e9-00304f19f545");
			TechSketchDocuments = new Guid("cadd9bae-306c-11d8-b4e9-00304f19f545");
			FolderGroupForTechProcGUID = new Guid("cadd9ebc-306c-11d8-b4e9-00304f19f545");
			AttrFormGUID = new Guid("cad0011c-306c-11d8-b4e9-00304f19f545");
			NotionObjectGUID = new Guid("cad001bc-306c-11d8-b4e9-00304f19f545");
			NumerationObjectBaseGUID = new Guid("cad001c3-306c-11d8-b4e9-00304f19f545");
			NumerationRuleGUID = new Guid("cad001c4-306c-11d8-b4e9-00304f19f545");
			NumerationObjectGUID = new Guid("cad001c5-306c-11d8-b4e9-00304f19f545");
			ArticleBaseGUID = new Guid("cad00268-306c-11d8-b4e9-00304f19f545");
			ArticleCopyBaseGUID = new Guid("cadd9a5d-306c-11d8-b4e9-00304f19f545");
			WorkTypeBaseObjectGUID = new Guid("cad005ad-306c-11d8-b4e9-00304f19f545");
			ProductTypeObjectGUID = new Guid("cad005ae-306c-11d8-b4e9-00304f19f545");
			WorkTypeObjectGUID = new Guid("cad005af-306c-11d8-b4e9-00304f19f545");
			ZakazObjectGUID = new Guid("cad00580-306c-11d8-b4e9-00304f19f545");
			ExternalCADModelTypeGuid = new Guid("cad0078f-306c-11d8-b4e9-00304f19f545");
			InitData(MetaDataHelper);
		}

		internal static void InitData(IMetaDataHelper metaDataHelper)
		{
			InitFields(metaDataHelper);
			InitLists(metaDataHelper);
		}
	}

	public static class AttributeTypes
	{
		[IsAttributeType(true)]
		public static readonly Guid ProductionAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid FormListAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid LifeCycleStepPrevGUID;

		[IsAttributeType(true)]
		public static readonly Guid TechProcGroupRelAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfAssemblyVersionAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfAssemblyObjectAttrGUID;

		[IsAttributeType(true)]
		[Obsolete("Use MemberOfAssemblyVersionAttrGUID instead", true)]
		public static readonly Guid MemberOfSborkaObjectAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfOrderVersionAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfOrderObjectAttrGUID;

		[IsAttributeType(true)]
		[Obsolete("Use MemberOfOrderVersionAttrGUID instead", true)]
		public static readonly Guid MemberOfZakazObjectAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfMainObjectAttrGUID;

		[IsAttributeType(true)]
		public static Guid MemberOfTechProcObjAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfAssemblyCopyAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfExitAssemblyAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid DesignationOfExitAssemblyAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfProductionReportObjectAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid MemberOfProductionReportVersionAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid ProductionObjectUIDAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid DateStartAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid DateFinishAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid RouteTypeAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid RoutePurposeAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid RouteKindAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid RouteStringAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid CehRouteAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid AreaRouteAttrGUID;

		[IsAttributeType(true)]
		public static readonly Guid WorkTypeAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid TP2RouteTypeAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid ArtTypeAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid ZagTypeAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid ObjectGUIDAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid SketchListGUIDAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid SketchNameGuid;

		[IsAttributeType(true)]
		public static readonly Guid Count4CompositionAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid Count4TechProcAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid Count4ArticleAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid CountRemainAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid GtpContextAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid CadmechAttrTypeAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid PerehTextAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid PerehTextExtraAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid NonNumerationFlagAttrGuid;

		public static readonly Guid ElemRouteCaptionAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid ElemRouteTemplateReferenceAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid CommentaryTextAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid DopPriemTextAttrGuid;

		[IsAttributeType(true)]
		public static readonly Guid TechGroupFolderNameAttrGuid;

		public static readonly Guid ImbaseKeyAttrGuid;

		public static readonly Guid ImbaseCodeAttrGuid;

		public static readonly Guid ImbaseObjectAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ObjectRefAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ArticleAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationTypeAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationNumberLengthAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationCharListAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationFirtNumberAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationStepAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationAreaAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationSeparatorAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationTypeVariantAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationUseBaseNumberAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationMethodAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationModeAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid NumerationOnDeleteAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ImageAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid libImageAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ContextAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ElemRouteLinkAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ProcRouteDefaultAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid TechcardAttrGroupGuid;

		[IsAttributeType(false)]
		public static readonly Guid LinkCondAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid OLEObjectAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid ObjectNumAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid SortAttrTypeGuid;

		[IsAttributeType(false)]
		public static readonly Guid CountAttrTypeGuid;

		[IsAttributeType(false)]
		public static readonly Guid EcoAuxObjAttrGuid;

		[IsAttributeType(false)]
		public static readonly Guid DesignationEtpObj4Gtp;

		public static readonly Guid ProductionReportAnalogGuid;

		public static readonly Guid LinkToArticleForProductionReportAttrGuid;

		public static readonly Guid TetheredProductAttrGuid;

		public static int ProductionAttrID { get; private set; }

		public static int TechProcGroupRelAttrID { get; private set; }

		public static int FormListAttrID { get; private set; }

		public static int LifeCycleStepPrevID { get; private set; }

		public static int CehRouteAttrID { get; private set; }

		public static int AreaRouteAttrID { get; private set; }

		public static int MemberOfAssemblyVersionAttrID { get; private set; }

		public static int MemberOfAssemblyObjectAttrID { get; private set; }

		[Obsolete("Use MemberOfAssemblyVersionAttrID instead", true)]
		public static int MemberOfSborkaObjectAttrID => MemberOfAssemblyVersionAttrID;

		public static int MemberOfAssemblyCopyAttrID { get; private set; }

		public static int MemberOfExitAssemblyAttrID { get; private set; }

		public static int DesignationOfExitAssemblyAttrID { get; private set; }

		public static int MemberOfProductionReportObjectAttrID { get; private set; }

		public static int MemberOfProductionReportVersionAttrID { get; private set; }

		public static int ProductionObjectUIDAttrID { get; private set; }

		public static int MemberOfOrderVersionAttrID { get; private set; }

		public static int MemberOfOrderObjectAttrID { get; private set; }

		[Obsolete("Use MemberOfOrderVersionAttrID instead", true)]
		public static int MemberOfZakazObjectAttrID => MemberOfOrderVersionAttrID;

		public static int MemberOfMainObjectAttrID { get; private set; }

		public static int MemberOfTechProcObjAttrId { get; private set; }

		public static int Count4CompositionAttrID { get; private set; }

		public static int Count4TechProcAttrID { get; private set; }

		public static int Count4ArticleAttrID { get; private set; }

		public static int CountRemainAttrID { get; private set; }

		public static int GtpContextAttrID { get; set; }

		public static int RouteStringAttrID { get; private set; }

		public static int NonNumerationFlagAttrID { get; private set; }

		public static int ElemRouteTemplateReferenceAttrID { get; private set; }

		public static int TetheredProductAttrID { get; private set; }

		public static int PerehTextAttrID { get; private set; }

		public static int PerehTextExtraAttrID { get; private set; }

		public static int CommentaryTextAttrID { get; private set; }

		public static int DopPriemTextAttrID { get; private set; }

		public static int TechGroupFolderNameAttrID { get; private set; }

		public static int DesignationAttrTypeID { get; private set; }

		public static int NameAttrTypeID { get; private set; }

		public static int ImbaseObjectAttrID { get; private set; }

		public static int ObjectRefAttrID { get; private set; }

		public static int ContextAttrID { get; private set; }

		public static int ElemRouteLinkAttrID { get; private set; }

		public static int ProcRouteDefaultAttrID { get; private set; }

		public static int SortAttrTypeID { get; private set; }

		public static int CountAttrTypeID { get; private set; }

		public static int ContextVersionID { get; private set; }

		public static int FileAttrTypeID { get; private set; }

		public static int ArchiveAttrID { get; private set; }

		public static int ProductionReportAnalogID { get; private set; }

		public static int LinkToArticleForProductionReportAttrID { get; private set; }

		public static int ObjectPrototypeAttrID { get; private set; }

		public static int DesignationEtpObj4GtpID { get; private set; }

		static AttributeTypes()
		{
			ProductionAttrGUID = new Guid("cad0019c-306c-11d8-b4e9-00304f19f545");
			FormListAttrGUID = new Guid("cad0019d-306c-11d8-b4e9-00304f19f545");
			LifeCycleStepPrevGUID = new Guid("cad001bb-306c-11d8-b4e9-00304f19f545");
			TechProcGroupRelAttrGUID = new Guid("cad009ee-306c-11d8-b4e9-00304f19f545");
			MemberOfAssemblyVersionAttrGUID = new Guid("cad001d5-306c-11d8-b4e9-00304f19f545");
			MemberOfAssemblyObjectAttrGUID = new Guid("cadd9c2a-306c-11d8-b4e9-00304f19f545");
			MemberOfSborkaObjectAttrGUID = MemberOfAssemblyVersionAttrGUID;
			MemberOfOrderVersionAttrGUID = new Guid("cad001d6-306c-11d8-b4e9-00304f19f545");
			MemberOfOrderObjectAttrGUID = new Guid("cadd9c2b-306c-11d8-b4e9-00304f19f545");
			MemberOfZakazObjectAttrGUID = MemberOfOrderVersionAttrGUID;
			MemberOfMainObjectAttrGUID = new Guid("cad009f0-306c-11d8-b4e9-00304f19f545");
			MemberOfTechProcObjAttrGuid = new Guid("cadd9a3a-306c-11d8-b4e9-00304f19f545");
			MemberOfAssemblyCopyAttrGUID = new Guid("cadd9bc9-306c-11d8-b4e9-00304f19f545");
			MemberOfExitAssemblyAttrGUID = new Guid("cadd9bca-306c-11d8-b4e9-00304f19f545");
			DesignationOfExitAssemblyAttrGUID = new Guid("cadd9ccc-306c-11d8-b4e9-00304f19f545");
			MemberOfProductionReportObjectAttrGUID = new Guid("cadd9bda-306c-11d8-b4e9-00304f19f545");
			MemberOfProductionReportVersionAttrGUID = new Guid("cadd9bcb-306c-11d8-b4e9-00304f19f545");
			ProductionObjectUIDAttrGuid = new Guid("cadd9bcc-306c-11d8-b4e9-00304f19f545");
			DateStartAttrGUID = new Guid("cad001e9-306c-11d8-b4e9-00304f19f545");
			DateFinishAttrGUID = new Guid("cad001ea-306c-11d8-b4e9-00304f19f545");
			RouteTypeAttrGUID = new Guid("cad001eb-306c-11d8-b4e9-00304f19f545");
			RoutePurposeAttrGUID = new Guid("cad001ec-306c-11d8-b4e9-00304f19f545");
			RouteKindAttrGUID = new Guid("cad001ed-306c-11d8-b4e9-00304f19f545");
			RouteStringAttrGUID = new Guid("cad001ef-306c-11d8-b4e9-00304f19f545");
			CehRouteAttrGUID = new Guid("cad001fb-306c-11d8-b4e9-00304f19f545");
			AreaRouteAttrGUID = new Guid("cad001fc-306c-11d8-b4e9-00304f19f545");
			WorkTypeAttrGuid = new Guid("cad005b2-306c-11d8-b4e9-00304f19f545");
			TP2RouteTypeAttrGuid = new Guid("cad005b4-306c-11d8-b4e9-00304f19f545");
			ArtTypeAttrGuid = new Guid("cad005d1-306c-11d8-b4e9-00304f19f545");
			ZagTypeAttrGuid = new Guid("cad005d0-306c-11d8-b4e9-00304f19f545");
			ObjectGUIDAttrGuid = new Guid("cad005ba-306c-11d8-b4e9-00304f19f545");
			SketchListGUIDAttrGuid = new Guid("cad009e8-306c-11d8-b4e9-00304f19f545");
			SketchNameGuid = new Guid("cad009e9-306c-11d8-b4e9-00304f19f545");
			Count4CompositionAttrGuid = new Guid("cad009f2-306c-11d8-b4e9-00304f19f545");
			Count4TechProcAttrGuid = new Guid("cad009f3-306c-11d8-b4e9-00304f19f545");
			Count4ArticleAttrGuid = new Guid("cad009f4-306c-11d8-b4e9-00304f19f545");
			CountRemainAttrGuid = new Guid("cad009f5-306c-11d8-b4e9-00304f19f545");
			GtpContextAttrGuid = new Guid("cadd93fb-306c-11d8-b4e9-00304f19f545");
			CadmechAttrTypeAttrGuid = new Guid("cadd9619-306c-11d8-b4e9-00304f19f545");
			PerehTextAttrGuid = new Guid("cad005ce-306c-11d8-b4e9-00304f19f545");
			PerehTextExtraAttrGuid = new Guid("cad005cf-306c-11d8-b4e9-00304f19f545");
			NonNumerationFlagAttrGuid = new Guid("cadd9710-306c-11d8-b4e9-00304f19f545");
			ElemRouteCaptionAttrGuid = new Guid("cad005e9-306c-11d8-b4e9-00304f19f545");
			ElemRouteTemplateReferenceAttrGuid = new Guid("cadd9c24-306c-11d8-b4e9-00304f19f545");
			CommentaryTextAttrGuid = new Guid("cad005d4-306c-11d8-b4e9-00304f19f545");
			DopPriemTextAttrGuid = new Guid("cad005cd-306c-11d8-b4e9-00304f19f545");
			TechGroupFolderNameAttrGuid = new Guid("cadd9c22-306c-11d8-b4e9-00304f19f545");
			ImbaseKeyAttrGuid = new Guid("cad00162-306c-11d8-b4e9-00304f19f545");
			ImbaseCodeAttrGuid = new Guid("cad0020f-306c-11d8-b4e9-00304f19f545");
			ImbaseObjectAttrGuid = new Guid("cad00209-306c-11d8-b4e9-00304f19f545");
			ObjectRefAttrGuid = new Guid("cad001be-306c-11d8-b4e9-00304f19f545");
			ArticleAttrGuid = new Guid("cad001ee-306c-11d8-b4e9-00304f19f545");
			NumerationTypeAttrGuid = new Guid("cad001c7-306c-11d8-b4e9-00304f19f545");
			NumerationNumberLengthAttrGuid = new Guid("cad001c8-306c-11d8-b4e9-00304f19f545");
			NumerationCharListAttrGuid = new Guid("cad001c9-306c-11d8-b4e9-00304f19f545");
			NumerationFirtNumberAttrGuid = new Guid("cad001ca-306c-11d8-b4e9-00304f19f545");
			NumerationStepAttrGuid = new Guid("cad001cb-306c-11d8-b4e9-00304f19f545");
			NumerationAreaAttrGuid = new Guid("cad001cc-306c-11d8-b4e9-00304f19f545");
			NumerationSeparatorAttrGuid = new Guid("cad001cd-306c-11d8-b4e9-00304f19f545");
			NumerationTypeVariantAttrGuid = new Guid("cad001ce-306c-11d8-b4e9-00304f19f545");
			NumerationUseBaseNumberAttrGuid = new Guid("cad001cf-306c-11d8-b4e9-00304f19f545");
			NumerationMethodAttrGuid = new Guid("cad001d1-306c-11d8-b4e9-00304f19f545");
			NumerationModeAttrGuid = new Guid("cadd9669-306c-11d8-b4e9-00304f19f545");
			NumerationOnDeleteAttrGuid = new Guid("cadd99b4-306c-11d8-b4e9-00304f19f545");
			ImageAttrGuid = new Guid("cad0013e-306c-11d8-b4e9-00304f19f545");
			libImageAttrGuid = new Guid("cad0013d-306c-11d8-b4e9-00304f19f545");
			ContextAttrGuid = new Guid("cad001fe-306c-11d8-b4e9-00304f19f545");
			ElemRouteLinkAttrGuid = new Guid("cad005b1-306c-11d8-b4e9-00304f19f545");
			ProcRouteDefaultAttrGuid = new Guid("cad005b9-306c-11d8-b4e9-00304f19f545");
			TechcardAttrGroupGuid = new Guid("cad005b3-306c-11d8-b4e9-00304f19f545");
			LinkCondAttrGuid = new Guid("cad005bb-306c-11d8-b4e9-00304f19f545");
			OLEObjectAttrGuid = new Guid("cad005be-306c-11d8-b4e9-00304f19f545");
			ObjectNumAttrGuid = new Guid("cad009e6-306c-11d8-b4e9-00304f19f545");
			SortAttrTypeGuid = new Guid("cad00202-306c-11d8-b4e9-00304f19f545");
			CountAttrTypeGuid = new Guid("cad00267-306c-11d8-b4e9-00304f19f545");
			EcoAuxObjAttrGuid = new Guid("cadd93b7-306c-11d8-b4e9-00304f19f545");
			DesignationEtpObj4Gtp = new Guid("cadd9736-306c-11d8-b4e9-00304f19f545");
			ProductionReportAnalogGuid = new Guid("cadd9a7f-306c-11d8-b4e9-00304f19f545");
			LinkToArticleForProductionReportAttrGuid = new Guid("cadd9a8c-306c-11d8-b4e9-00304f19f545");
			TetheredProductAttrGuid = new Guid("cadd9cf7-306c-11d8-b4e9-00304f19f545");
			InitData(MetaDataHelper);
		}

		internal static void InitData(IMetaDataHelper metaDataHelper)
		{
			ProductionAttrID = metaDataHelper.GetAttributeTypeID(ProductionAttrGUID, throwExceptionIfNotFound: false);
			TechProcGroupRelAttrID = metaDataHelper.GetAttributeTypeID(TechProcGroupRelAttrGUID, throwExceptionIfNotFound: false);
			FormListAttrID = metaDataHelper.GetAttributeTypeID(FormListAttrGUID, throwExceptionIfNotFound: false);
			LifeCycleStepPrevID = metaDataHelper.GetAttributeTypeID(LifeCycleStepPrevGUID, throwExceptionIfNotFound: false);
			DesignationAttrTypeID = metaDataHelper.GetAttributeTypeID("cad0001f-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			NameAttrTypeID = metaDataHelper.GetAttributeTypeID("cad00020-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			ImbaseObjectAttrID = metaDataHelper.GetAttributeTypeID(ImbaseObjectAttrGuid, throwExceptionIfNotFound: false);
			ObjectRefAttrID = metaDataHelper.GetAttributeTypeID(ObjectRefAttrGuid, throwExceptionIfNotFound: false);
			ContextAttrID = metaDataHelper.GetAttributeTypeID(ContextAttrGuid, throwExceptionIfNotFound: false);
			ElemRouteLinkAttrID = metaDataHelper.GetAttributeTypeID(ElemRouteLinkAttrGuid, throwExceptionIfNotFound: false);
			ProcRouteDefaultAttrID = metaDataHelper.GetAttributeTypeID(ProcRouteDefaultAttrGuid, throwExceptionIfNotFound: false);
			MemberOfTechProcObjAttrId = MetaDataHelper.GetAttributeTypeID(MemberOfTechProcObjAttrGuid, throwExceptionIfNotFound: false);
			CehRouteAttrID = metaDataHelper.GetAttributeTypeID(CehRouteAttrGUID, throwExceptionIfNotFound: false);
			AreaRouteAttrID = metaDataHelper.GetAttributeTypeID(AreaRouteAttrGUID, throwExceptionIfNotFound: false);
			SortAttrTypeID = metaDataHelper.GetAttributeTypeID(SortAttrTypeGuid, throwExceptionIfNotFound: false);
			CountAttrTypeID = metaDataHelper.GetAttributeTypeID(CountAttrTypeGuid, throwExceptionIfNotFound: false);
			MemberOfAssemblyVersionAttrID = metaDataHelper.GetAttributeTypeID(MemberOfAssemblyVersionAttrGUID, throwExceptionIfNotFound: false);
			MemberOfAssemblyObjectAttrID = metaDataHelper.GetAttributeTypeID(MemberOfAssemblyObjectAttrGUID, throwExceptionIfNotFound: false);
			MemberOfOrderVersionAttrID = metaDataHelper.GetAttributeTypeID(MemberOfOrderVersionAttrGUID, throwExceptionIfNotFound: false);
			MemberOfOrderObjectAttrID = metaDataHelper.GetAttributeTypeID(MemberOfOrderObjectAttrGUID, throwExceptionIfNotFound: false);
			MemberOfMainObjectAttrID = metaDataHelper.GetAttributeTypeID(MemberOfMainObjectAttrGUID, throwExceptionIfNotFound: false);
			MemberOfAssemblyCopyAttrID = metaDataHelper.GetAttributeTypeID(MemberOfAssemblyCopyAttrGUID, throwExceptionIfNotFound: false);
			MemberOfExitAssemblyAttrID = metaDataHelper.GetAttributeTypeID(MemberOfExitAssemblyAttrGUID, throwExceptionIfNotFound: false);
			DesignationOfExitAssemblyAttrID = metaDataHelper.GetAttributeTypeID(DesignationOfExitAssemblyAttrGUID, throwExceptionIfNotFound: false);
			MemberOfProductionReportObjectAttrID = metaDataHelper.GetAttributeTypeID(MemberOfProductionReportObjectAttrGUID, throwExceptionIfNotFound: false);
			MemberOfProductionReportVersionAttrID = metaDataHelper.GetAttributeTypeID(MemberOfProductionReportVersionAttrGUID, throwExceptionIfNotFound: false);
			ProductionObjectUIDAttrID = metaDataHelper.GetAttributeTypeID(ProductionObjectUIDAttrGuid, throwExceptionIfNotFound: false);
			ProductionReportAnalogID = metaDataHelper.GetAttributeTypeID(ProductionReportAnalogGuid, throwExceptionIfNotFound: false);
			LinkToArticleForProductionReportAttrID = metaDataHelper.GetAttributeTypeID(LinkToArticleForProductionReportAttrGuid, throwExceptionIfNotFound: false);
			ObjectPrototypeAttrID = metaDataHelper.GetAttributeID("cadd9668-306c-11d8-b4e9-00304f19f545");
			DesignationEtpObj4GtpID = metaDataHelper.GetAttributeID(DesignationEtpObj4Gtp);
			Count4CompositionAttrID = metaDataHelper.GetAttributeTypeID(Count4CompositionAttrGuid, throwExceptionIfNotFound: false);
			Count4TechProcAttrID = metaDataHelper.GetAttributeTypeID(Count4TechProcAttrGuid, throwExceptionIfNotFound: false);
			Count4ArticleAttrID = metaDataHelper.GetAttributeTypeID(Count4ArticleAttrGuid, throwExceptionIfNotFound: false);
			CountRemainAttrID = metaDataHelper.GetAttributeTypeID(CountRemainAttrGuid, throwExceptionIfNotFound: false);
			GtpContextAttrID = metaDataHelper.GetAttributeTypeID(GtpContextAttrGuid, throwExceptionIfNotFound: false);
			RouteStringAttrID = metaDataHelper.GetAttributeTypeID(RouteStringAttrGUID, throwExceptionIfNotFound: false);
			ContextVersionID = metaDataHelper.GetAttributeTypeID("cad001c2-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			FileAttrTypeID = metaDataHelper.GetAttributeTypeID("cad0004b-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			ArchiveAttrID = metaDataHelper.GetAttributeTypeID(SystemGUIDs.attributeArchive, throwExceptionIfNotFound: false);
			NameAttrTypeID = metaDataHelper.GetAttributeTypeID("cad00020-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			DesignationAttrTypeID = metaDataHelper.GetAttributeTypeID("cad0001f-306c-11d8-b4e9-00304f19f545", throwExceptionIfNotFound: false);
			NonNumerationFlagAttrID = metaDataHelper.GetAttributeTypeID(NonNumerationFlagAttrGuid, throwExceptionIfNotFound: false);
			ElemRouteTemplateReferenceAttrID = metaDataHelper.GetAttributeTypeID(ElemRouteTemplateReferenceAttrGuid, throwExceptionIfNotFound: false);
			TetheredProductAttrID = metaDataHelper.GetAttributeTypeID(TetheredProductAttrGuid, throwExceptionIfNotFound: false);
			PerehTextAttrID = metaDataHelper.GetAttributeTypeID(PerehTextAttrGuid, throwExceptionIfNotFound: false);
			PerehTextExtraAttrID = metaDataHelper.GetAttributeTypeID(PerehTextExtraAttrGuid, throwExceptionIfNotFound: false);
			CommentaryTextAttrID = metaDataHelper.GetAttributeTypeID(CommentaryTextAttrGuid, throwExceptionIfNotFound: false);
			DopPriemTextAttrID = metaDataHelper.GetAttributeTypeID(DopPriemTextAttrGuid, throwExceptionIfNotFound: false);
			TechGroupFolderNameAttrID = metaDataHelper.GetAttributeTypeID(TechGroupFolderNameAttrGuid, throwExceptionIfNotFound: false);
		}
	}

	public static class LcLevel
	{
		public static readonly Guid LifeCycleLevelProcElemBase = new Guid("cad001ba-306c-11d8-b4e9-00304f19f545");

		[Obsolete("Use LifeCycleLevelProcElemBase instead. Will be remove in IPS 9", true)]
		public static readonly Guid ListCycleLevelProcElemBase = LifeCycleLevelProcElemBase;

		public static readonly Guid LifeCycleLevelStoring = new Guid("cad009de-306c-11d8-b4e9-00304f19f545");

		public static readonly Guid LifeCycleLevelAnnulled = new Guid("cad00012-306c-11d8-b4e9-00304f19f545");
	}

	public static class LcSteps
	{
		public static readonly Guid LifeCycleStepProcElemBase = new Guid("cadd9df5-306c-11d8-b4e9-00304f19f545");
	}

	public static class RelTypes
	{
		private static IReadOnlyList<int> _techAllRelationTypes;

		private static IReadOnlyList<int> _techCompositionGtpRelations;

		private static IReadOnlyList<int> _artsCompositionRelations;

		[IsRelationType(true)]
		public static readonly Guid TechRelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechCombinedRelationGuid;

		[Obsolete("Use TechCombinedRelationGuid instead")]
		public static readonly Guid TechSobirRelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechThroughMORelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechThroughtTPRelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechRouteRelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechLinkGTPObjRelationGuid;

		[IsRelationType(true)]
		public static readonly Guid TechDraftRelationGuid;

		[IsRelationType(false)]
		public static readonly Guid CustomTechRelationGuid;

		[IsRelationType(false)]
		public static readonly Guid NotionRelationGuid;

		[IsRelationType(false)]
		public static readonly Guid ProektRelationGuid;

		[IsRelationType(false)]
		public static readonly Guid ProductReportRelationGuid;

		[IsRelationType(false)]
		public static readonly Guid MbomBindingRelationTypeGuid;

		[IsRelationType(false)]
		public static readonly Guid MbomCompositionRelationTypeGuid;

		[IsRelationType(false)]
		public static readonly Guid CustomProektRelationGuid;

		public static int TechRelationID { get; private set; }

		public static int TechCombinedRelationID { get; private set; }

		[Obsolete("Use TechCombinedRelationID instead")]
		public static int TechSobirRelationID { get; private set; }

		public static int TechThroughMORelationID { get; private set; }

		public static int TechRouteRelationID { get; private set; }

		public static int TechLinkGTPObjRelationID { get; private set; }

		public static int TechDraftRelationID { get; private set; }

		public static int TechThroughtTPRelationID { get; set; }

		public static int CustomTechRelationID { get; private set; }

		public static int SimpleRelationID { get; private set; }

		public static int ProektRelationID { get; private set; }

		public static int ProductReportRelationID { get; private set; }

		public static int SortedRelationID { get; private set; }

		public static int MbomBindingRelationTypeId { get; private set; }

		public static int MbomCompositionRelationTypeId { get; private set; }

		public static int CustomProektRelationId { get; set; }

		public static IReadOnlyList<int> TechAllRelationTypes => _techAllRelationTypes;

		[Obsolete("TechCompositionGtpRelations", false)]
		public static int[] TechCompGtpRelations => _techCompositionGtpRelations.ToArray();

		public static IReadOnlyList<int> TechCompositionGtpRelations => _techCompositionGtpRelations;

		public static IReadOnlyList<int> ArtsCompositionRelations => _artsCompositionRelations;

		private static void InitLists(IMetaDataHelper metaDataHelper)
		{
			_techCompositionGtpRelations = new int[2] { TechRelationID, TechDraftRelationID };
			_artsCompositionRelations = new int[7]
			{
				ProektRelationID,
				ProductReportRelationID,
				MbomBindingRelationTypeId,
				MbomCompositionRelationTypeId,
				MetaDataHelper.GetRelationTypeID(PDMPluginGuids.linkZagotRelationGuid, throwExceptionIfNotFound: false),
				CustomProektRelationId,
				MetaDataHelper.GetRelationTypeID(SystemGUIDs.relationTypeTuningPart, throwExceptionIfNotFound: false)
			}.Where((int relationType) => relationType != -1).ToArray();
			List<int> list = new List<int>();
			list.Add(TechRelationID);
			if (CustomTechRelationID != -1)
			{
				list.Add(CustomTechRelationID);
			}
			_techAllRelationTypes = list.ToArray();
		}

		static RelTypes()
		{
			TechRelationGuid = new Guid("cad0019f-306c-11d8-b4e9-00304f19f545");
			TechCombinedRelationGuid = new Guid("cad0019e-306c-11d8-b4e9-00304f19f545");
			TechSobirRelationGuid = TechCombinedRelationGuid;
			TechThroughMORelationGuid = new Guid("cad001e1-306c-11d8-b4e9-00304f19f545");
			TechThroughtTPRelationGuid = new Guid("cadd9403-306c-11d8-b4e9-00304f19f545");
			TechRouteRelationGuid = new Guid("cad005b0-306c-11d8-b4e9-00304f19f545");
			TechLinkGTPObjRelationGuid = new Guid("cad005b8-306c-11d8-b4e9-00304f19f545");
			TechDraftRelationGuid = new Guid("cad009e7-306c-11d8-b4e9-00304f19f545");
			CustomTechRelationGuid = new Guid("cadd9ac5-306c-11d8-b4e9-00304f19f545");
			NotionRelationGuid = new Guid("cad001bd-306c-11d8-b4e9-00304f19f545");
			ProektRelationGuid = new Guid("cad00023-306c-11d8-b4e9-00304f19f545");
			ProductReportRelationGuid = new Guid("cadd9a57-306c-11d8-b4e9-00304f19f545");
			MbomBindingRelationTypeGuid = new Guid("cadd98ab-306c-11d8-b4e9-00304f19f545");
			MbomCompositionRelationTypeGuid = new Guid("cadd98ac-306c-11d8-b4e9-00304f19f545");
			CustomProektRelationGuid = new Guid("cadd9f1e-306c-11d8-b4e9-00304f19f545");
			InitData(MetaDataHelper);
		}

		internal static void InitData(IMetaDataHelper metaDataHelper)
		{
			TechThroughMORelationID = metaDataHelper.GetRelationTypeID(TechThroughMORelationGuid, throwExceptionIfNotFound: false);
			TechRelationID = metaDataHelper.GetRelationTypeID(TechRelationGuid, throwExceptionIfNotFound: false);
			TechSobirRelationID = (TechCombinedRelationID = metaDataHelper.GetRelationTypeID(TechCombinedRelationGuid, throwExceptionIfNotFound: false));
			TechRouteRelationID = metaDataHelper.GetRelationTypeID(TechRouteRelationGuid, throwExceptionIfNotFound: false);
			TechLinkGTPObjRelationID = metaDataHelper.GetRelationTypeID(TechLinkGTPObjRelationGuid, throwExceptionIfNotFound: false);
			TechDraftRelationID = metaDataHelper.GetRelationTypeID(TechDraftRelationGuid, throwExceptionIfNotFound: false);
			TechThroughtTPRelationID = metaDataHelper.GetRelationTypeID(TechThroughtTPRelationGuid, throwExceptionIfNotFound: false);
			CustomTechRelationID = metaDataHelper.GetRelationTypeID(CustomTechRelationGuid, throwExceptionIfNotFound: false);
			SimpleRelationID = metaDataHelper.GetRelationTypeID(new Guid("cad00022-306c-11d8-b4e9-00304f19f545"), throwExceptionIfNotFound: false);
			ProektRelationID = metaDataHelper.GetRelationTypeID(ProektRelationGuid, throwExceptionIfNotFound: false);
			SortedRelationID = metaDataHelper.GetRelationTypeID(new Guid("cad00151-306c-11d8-b4e9-00304f19f545"), throwExceptionIfNotFound: false);
			ProductReportRelationID = metaDataHelper.GetRelationTypeID(ProductReportRelationGuid, throwExceptionIfNotFound: false);
			MbomBindingRelationTypeId = metaDataHelper.GetRelationTypeID(MbomBindingRelationTypeGuid, throwExceptionIfNotFound: false);
			MbomCompositionRelationTypeId = metaDataHelper.GetRelationTypeID(MbomCompositionRelationTypeGuid, throwExceptionIfNotFound: false);
			CustomProektRelationId = metaDataHelper.GetRelationTypeID(CustomProektRelationGuid, throwExceptionIfNotFound: false);
			InitLists(metaDataHelper);
		}
	}

	public static class Utils
	{
		private static readonly IDictionary<int, bool> ObjTypeCache;

		private static readonly IDictionary<int, bool> TechNotInheritObjTypeCache;

		private static void InitCaches()
		{
			if (ObjectTypes.TechNotInheritedBaseObjTypes == null)
			{
				return;
			}
			foreach (int techNotInheritedBaseObjType in ObjectTypes.TechNotInheritedBaseObjTypes)
			{
				TechNotInheritObjTypeCache[techNotInheritedBaseObjType] = true;
			}
		}

		static Utils()
		{
			ObjTypeCache = new ConcurrentDictionary<int, bool>();
			TechNotInheritObjTypeCache = new ConcurrentDictionary<int, bool>();
			InitCaches();
		}

		public static bool GetCommonObjTypeAttrs(int sourceTypeId, int destinationTypeId, bool checkAnyAttrMode, out List<int> attrIds)
		{
			attrIds = new List<int>();
			if (sourceTypeId == -1 || destinationTypeId == -1)
			{
				return false;
			}
			IMSObjectType objectType = MetaDataHelper.GetObjectType(destinationTypeId, throwExceptionIfNotFound: false);
			if (objectType == null)
			{
				return false;
			}
			List<IMSAttribute4ObjectType> attribute4ObjectTypeList = MetaDataHelper.GetAttribute4ObjectTypeList(sourceTypeId);
			if (attribute4ObjectTypeList == null)
			{
				return false;
			}
			foreach (IMSAttribute4ObjectType item in attribute4ObjectTypeList)
			{
				if (item != null)
				{
					int attributeID = item.AttributeID;
					if ((objectType.AnyAttributes && checkAnyAttrMode) || MetaDataHelper.GetAttribute4ObjectType(destinationTypeId, attributeID) != null)
					{
						attrIds.Add(attributeID);
					}
				}
			}
			return true;
		}

		public static bool GetCommonRelTypeAttrs(int sourceTypeId, int destinationTypeId, bool checkAnyAttrMode, out List<int> attrIds)
		{
			attrIds = new List<int>();
			if (sourceTypeId == -1 || destinationTypeId == -1)
			{
				return false;
			}
			IMSRelationType relationType = MetaDataHelper.GetRelationType(destinationTypeId, throwExceptionIfNotFound: false);
			if (relationType == null)
			{
				return false;
			}
			List<IMSAttribute4RelationType> attribute4RelationTypeList = MetaDataHelper.GetAttribute4RelationTypeList(sourceTypeId);
			if (attribute4RelationTypeList == null)
			{
				return false;
			}
			foreach (IMSAttribute4RelationType item in attribute4RelationTypeList)
			{
				if (item != null)
				{
					int attributeID = item.AttributeID;
					if ((relationType.AnyAttributes && checkAnyAttrMode) || MetaDataHelper.GetAttribute4RelationType(destinationTypeId, attributeID) != null)
					{
						attrIds.Add(attributeID);
					}
				}
			}
			return true;
		}

		public static int ObjectTypeByGuid(Guid objTypeGuid, IUserSession userSession)
		{
			return MetaDataHelper.GetObjectTypeID(objTypeGuid, throwExceptionIfNotFound: false);
		}

		public static int AttributeTypeByGuid(Guid attrTypeGuid, IUserSession userSession)
		{
			return MetaDataHelper.GetAttributeTypeID(attrTypeGuid, throwExceptionIfNotFound: false);
		}

		public static int RelationTypeByGuid(Guid relationTypeGuid, IUserSession userSession)
		{
			return MetaDataHelper.GetRelationTypeID(relationTypeGuid, throwExceptionIfNotFound: false);
		}

		[Obsolete("Use IsTechcardObjectType method", false)]
		public static bool isTechcardObjectType(object objectType)
		{
			return IsTechcardObjectType(objectType);
		}

		[Obsolete("Use IsTechcardNotInheridObjType method", false)]
		public static bool isTechcardNotInheridObjType(object objectType)
		{
			return IsTechcardNotInheridObjType(objectType);
		}

		public static bool IsTechcardObjectType(object objectType)
		{
			int objectType2 = GetObjectType(objectType);
			if (objectType2.Equals(-1))
			{
				return false;
			}
			if (ObjTypeCache.TryGetValue(objectType2, out var value))
			{
				return value;
			}
			try
			{
				foreach (int techAllBaseObjType in ObjectTypes.TechAllBaseObjTypes)
				{
					if (MetaDataHelper.IsObjectTypeChildOf(objectType2, techAllBaseObjType))
					{
						value = true;
						return true;
					}
				}
			}
			finally
			{
				ObjTypeCache[objectType2] = value;
			}
			return false;
		}

		public static bool IsTechcardNotInheridObjType(object objectType)
		{
			int objectType2 = GetObjectType(objectType);
			if (!IsTechcardObjectType(objectType2))
			{
				return false;
			}
			if (TechNotInheritObjTypeCache.TryGetValue(objectType2, out var value))
			{
				return value;
			}
			try
			{
				foreach (int techNotInheritedBaseObjType in ObjectTypes.TechNotInheritedBaseObjTypes)
				{
					if (MetaDataHelper.IsObjectTypeChildOf(objectType2, techNotInheritedBaseObjType))
					{
						value = true;
						return true;
					}
				}
			}
			finally
			{
				TechNotInheritObjTypeCache[objectType2] = value;
			}
			return false;
		}

		public static int GetObjectType(object objectType)
		{
			int result = -1;
			if (objectType == null)
			{
				return result;
			}
			if (objectType is int num)
			{
				result = num;
			}
			else if (objectType is Guid objTypeGuid)
			{
				result = MetaDataHelper.GetObjectTypeID(objTypeGuid, throwExceptionIfNotFound: false);
			}
			else if (objectType is IDBObjectType iDBObjectType)
			{
				result = iDBObjectType.ObjectType;
			}
			return result;
		}

		public static int GetRelationType(object relationType)
		{
			int result = -1;
			if (relationType == null)
			{
				return result;
			}
			if (relationType is int num)
			{
				result = num;
			}
			else if (relationType is Guid relTypeGuid)
			{
				result = MetaDataHelper.GetRelationTypeID(relTypeGuid, throwExceptionIfNotFound: false);
			}
			else if (relationType is IDBRelationType iDBRelationType)
			{
				result = iDBRelationType.RelationType;
			}
			return result;
		}

		public static string GetObjectString(long objectId, IUserSession session)
		{
			return GetObjectString(objectId, session, includeVerInfo: false);
		}

		public static string GetObjectString(long objectId, IUserSession session, bool includeVerInfo)
		{
			string text = string.Empty;
			if (objectId != 0L && objectId != -1)
			{
				if (includeVerInfo)
				{
					return GetObjectString(session.GetObject(objectId, throwNotFoundException: false), includeVerInfo: true);
				}
				QuickObjectInfo objectInfo = session.GetObjectInfo(objectId);
				if (!objectInfo.Empty)
				{
					text = objectInfo.Caption;
					if (text.Equals(string.Empty))
					{
						text = string.Format(Intermech.Localization.LocalizationHolder.rm.GetString("Interfaces.TechCard_10"), objectInfo.ObjectID);
					}
				}
				else
				{
					text = string.Empty;
				}
			}
			return text;
		}

		public static string GetObjectString(IDBObject dbObject, bool includeVerInfo)
		{
			string empty = string.Empty;
			if (dbObject == null)
			{
				return empty;
			}
			empty = dbObject.Caption;
			if (includeVerInfo)
			{
				empty = ((!empty.Equals(string.Empty)) ? (empty + string.Format(Intermech.Localization.LocalizationHolder.rm.GetString("Interfaces.TechCard_21"), dbObject.VersionID)) : string.Format(Intermech.Localization.LocalizationHolder.rm.GetString("Interfaces.TechCard_10"), dbObject.ObjectID));
			}
			else if (empty.Equals(string.Empty))
			{
				empty = string.Format(Intermech.Localization.LocalizationHolder.rm.GetString("Interfaces.TechCard_10"), dbObject.ObjectID);
			}
			return empty;
		}

		public static List<string> GetObjectStrings(List<IDBObject> dbObjects, bool includeVerInfo)
		{
			List<string> list = new List<string>();
			if (dbObjects == null || dbObjects.Count == 0)
			{
				return list;
			}
			list.Capacity = dbObjects.Count;
			foreach (IDBObject dbObject in dbObjects)
			{
				string objectString = GetObjectString(dbObject, includeVerInfo);
				if (!(objectString == string.Empty))
				{
					list.Add(objectString);
				}
			}
			return list;
		}

		public static bool GetObjectString(ICollection<long> objIDs, int objTypeIds, IUserSession session, out Dictionary<long, string> objCaptions)
		{
			objCaptions = new Dictionary<long, string>();
			if (objIDs == null || objIDs.Count == 0)
			{
				return false;
			}
			List<long> list = new List<long>(2 * objIDs.Count);
			list.AddRange(objIDs);
			list.AddRange(from item in objIDs
				where item > 0
				select -item);
			foreach (long objID in objIDs)
			{
				if (objID >= 0)
				{
					list.Add(-objID);
				}
			}
			List<ColumnDescriptor> list2 = new List<ColumnDescriptor>();
			list2.Add(new ColumnDescriptor(-2, ColumnContents.Text, ColumnNameMapping.FieldName, SortOrders.NONE, 0));
			list2.Add(new ColumnDescriptor(-50, ColumnContents.Text, ColumnNameMapping.FieldName, SortOrders.NONE, 0));
			DataTable objectData = DataHelper.GetObjectData(objTypeIds, session, null, list2, list);
			if (objectData == null || objectData.Rows.Count == 0)
			{
				return false;
			}
			foreach (DataRow row in objectData.Rows)
			{
				long num = DataSetProcessor.GetInt64Value(row, 0, 0L);
				string text = row[1].ToString();
				if (text.Equals(string.Empty))
				{
					text = string.Format(Intermech.Localization.LocalizationHolder.rm.GetString("Interfaces.TechCard_10"), num);
				}
				if (!objIDs.Contains(num))
				{
					num = -num;
				}
				if (!objCaptions.ContainsKey(num))
				{
					objCaptions.Add(num, text);
				}
			}
			return true;
		}

		public static List<int> GetLinkedObjectTypes()
		{
			List<int> list = new List<int>();
			int objectTypeID = MetaDataHelper.GetObjectTypeID(ObjectTypes.OsnastBaseGUID, throwExceptionIfNotFound: false);
			list.Add(objectTypeID);
			list.AddRange(MetaDataHelper.GetObjectTypeChildrenIDRecursive(objectTypeID));
			objectTypeID = MetaDataHelper.GetObjectTypeID(ObjectTypes.OborudBaseGUID, throwExceptionIfNotFound: false);
			list.Add(objectTypeID);
			list.AddRange(MetaDataHelper.GetObjectTypeChildrenIDRecursive(objectTypeID));
			objectTypeID = MetaDataHelper.GetObjectTypeID(ObjectTypes.PersonalBaseGUID, throwExceptionIfNotFound: false);
			list.Add(objectTypeID);
			list.AddRange(MetaDataHelper.GetObjectTypeChildrenIDRecursive(objectTypeID));
			return list;
		}

		public static List<int> GetGroupTpObjectTypes()
		{
			List<int> list = new List<int>();
			int objectTypeID = MetaDataHelper.GetObjectTypeID(ObjectTypes.TechProcGroupGUID, throwExceptionIfNotFound: false);
			list.Add(objectTypeID);
			list.AddRange(MetaDataHelper.GetObjectTypeChildrenIDRecursive(objectTypeID));
			objectTypeID = MetaDataHelper.GetObjectTypeID(ObjectTypes.TechProcTipovGUID, throwExceptionIfNotFound: false);
			list.Add(objectTypeID);
			list.AddRange(MetaDataHelper.GetObjectTypeChildrenIDRecursive(objectTypeID));
			return list;
		}

		public static ICollection<int> GetPossibleArticleChildrenObjectTypes()
		{
			List<int> objectTypeChildrenIDRecursive = MetaDataHelper.GetObjectTypeChildrenIDRecursive(ObjectTypes.ArticleObjectTypes);
			HashSet<int> procRoutingIds = MetaDataHelper.GetObjectTypeChildrenIDRecursive(ObjectTypes.ProcRoutingID).LocalToHashSet();
			objectTypeChildrenIDRecursive.RemoveAll((int item) => procRoutingIds.Contains(item));
			return objectTypeChildrenIDRecursive.LocalToHashSet();
		}
	}

	internal static IMetaDataHelper MetaDataHelper { get; private set; }

	static TechCardConsts()
	{
		MetaDataHelper = MetaDataHelperService.Instance;
	}

	public static void Init(IMetaDataHelper metaDataHelper)
	{
		MetaDataHelper = metaDataHelper ?? throw new ArgumentNullException("metaDataHelper");
		ObjectTypes.InitData(metaDataHelper);
		AttributeTypes.InitData(metaDataHelper);
		RelTypes.InitData(metaDataHelper);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.1.0.9782' (yours is '8.2.0.7535-95108c96')
