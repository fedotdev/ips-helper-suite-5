using System;
using Intermech.Expert;
using Intermech.Interfaces;
using Intermech.Interfaces.Expert;
using Intermech.Interfaces.Server;
using Intermech.Expert.Server;
using System.Collections.Generic;

public class Script
{
	/* Заполняем дату изменения, размер и контрольную сумму по CRC32 для первого файла у объекта контекста */
	/* Используются только первые два параметра - ti и context, и из контекста берется только первый объект */
	public ICSharpScriptContext ScriptContext {get; private set;}

	public void Execute(ExpertServer.ExpServTask ti, long[] context, Intermech.Interfaces.Expert.HybridTableExp dTable, Int32 parm1, Int32 parm2, List<object> parmList)
	{
		IUserSession ius = ti.GetSession();
		long objID = context[0];
		
		// Идентификаторы заполняемых атрибутов
		string modifyDateGuid	= "cad00702-306c-11d8-b4e9-00304f19f545";
		string sizeGuid 	= "cad001ae-306c-11d8-b4e9-00304f19f545";
		string checkSumGuid 	= "cad014af-306c-11d8-b4e9-00304f19f545";
		
		// Добавить атрибут "Дата исправления", если еще не было
        if (!ti.savedData.Columns.Contains(modifyDateGuid))
            ti.savedData.AddColumn(modifyDateGuid, typeof(DateTime));
		// Добавить атрибут "Количество элементов", если еще не было
        if (!ti.savedData.Columns.Contains(sizeGuid))
            ti.savedData.AddColumn(sizeGuid, typeof(Int64));
		// Добавить атрибут "Контрольная сумма", если еще не было
        if (!ti.savedData.Columns.Contains(checkSumGuid))
            ti.savedData.AddColumn(checkSumGuid, typeof(Int64));
		
		HybridRowExp dr = ti.savedDataByObjId(objID);
		if (dr == null)
			return;
		
		Int32 fileAttrId = MetaDataHelper.GetAttributeTypeID( new Guid("cad0004b-306c-11d8-b4e9-00304f19f545" /*Файл*/) );
		Intermech.Interfaces.BlobStream.BlobReaderStream  stream = new Intermech.Interfaces.BlobStream.BlobReaderStream(objID, Intermech.AttributableElements.Object, fileAttrId, 0 /*tP.FileIndex*/, 0, ius);
		BlobInformation bi = stream.BlobInformation;
		
		dr[sizeGuid] = bi.RealFileSize;
		dr[modifyDateGuid] = bi.ModifyDate;
		
		Intermech.Checksums.Crc32Checksum sum = new Intermech.Checksums.Crc32Checksum();
		Intermech.Checksums.ChecksumClass sumClass = sum.Compute(stream);
		if (sumClass != null)
		{
			dr[checkSumGuid] = Convert.ToInt64(sumClass.Value);
		}
	}
}