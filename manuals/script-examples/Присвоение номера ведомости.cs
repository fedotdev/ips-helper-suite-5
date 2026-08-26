using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Data;
using Intermech;
using Intermech.Interfaces;
using Intermech.Interfaces.Client;
using Intermech.Kernel.Search;

//Скрипт для присвоения атрибута N Ведомости
//Ищет все номера всех ведомостей, находит максимальный, делает + 1 и присваивает его
// если максимальный номер уже превышает настройку, то ищется минимальный "свободный" номер

public class Script
{
    /// <summary>
    /// Свойство ScriptContext для обращения к IPS API.
    /// Значение свойства заполняется автоматически непосредственно перед вызовом метода Execute.
    /// </summary>
    public ICSharpScriptContext ScriptContext { get; private set; }
    // parameters содержит список значений атрибутов объекта, связи,
    // идентификаторы версий объекта и связи, а также пользовательскую сессию.
    public AttributeValidationScriptParameters Execute(AttributeValidationScriptParameters parameters)
    {
        // Получаем список значений атрибутов объекта из переданных параметров
        List<AttributeValues> attrValuesList = parameters.ObjectAttributeValues;
        // Ищем нужный атрибут по гуиду
        AttributeValues attrValues = attrValuesList.Find(x => x.AttributeGuid.ToString() == "cadd9a81-306c-11d8-b4e9-00304f19f545"); // атрибут номер ведомости
        // Изменяем значение атрибута если нашли его
        if (attrValues != null)
        {
            using (var sk = new SessionKeeper())
            {
                var collection = sk.Session.GetObjectCollection(new Guid("cadd9a5c-306c-11d8-b4e9-00304f19f545")); // тип объекта производственные ведомости
                int aID = MetaDataHelper.GetAttributeID(SystemGUIDs.attributeF_LC_STEP); // атрибут шаг ЖЦ
                ConditionStructure[] conds = new ConditionStructure[] {
                        new ConditionStructure(aID, RelationalOperators.NotEqual,
                            MetaDataHelper.GetLCLevelID(SystemGUIDs.levelAnnulment), LogicalOperators.AND, 0, false),
                        new ConditionStructure(aID, RelationalOperators.NotEqual,
                            MetaDataHelper.GetLCLevelID(SystemGUIDs.levelDeleted), LogicalOperators.AND, 0, false),
                        };
                var vID = MetaDataHelper.GetAttributeID("cadd9a81-306c-11d8-b4e9-00304f19f545"); // атрибут номер ведомости
                var paramSet = new DBRecordSetParams(conds, new object[] { vID }, new object[] { vID }, new SortOrders[] { SortOrders.NONE });
                paramSet.RecordCount = QueryConsts.All;
                DataTable table = collection.Select(paramSet);
                if (table.Rows.Count > 0)
                {
                    int r = -1, i = 0;
                    int r1;
                    // читаем максимальный номер из настроек, по умолчанию 5000
                    int MaxNumber = (int)sk.Session.Configurations.ReadInteger("MRP2", "MRP2", "maxPLNumber", 5000, DBConfigMode.GlobalOnly);
                    BitArray ba = new BitArray(MaxNumber + 1);
                    do // ищем максимальный номер среди всех имеющихся и заодно помечаем битики занятыми номерами
                    {
                        if (int.TryParse(table.Rows[i][0].ToString(), out r1))
                        {
                            if (r1 < ba.Length) ba[r1] = true;
                            if (r1 > r) r = r1;
                        }
                        i++;
                    } while (i < table.Rows.Count);

                    if (r == -1)
                        r = 1; //нет ни одного номера
                    else
                    if (r < MaxNumber)  // нашли макс. номер и он меньше 5000, берем его +1
                    {
                        r++;
                    }
                    else
                    {  // макс номер больше или равен 5000 = значит ищем пропуски в номерах начиная с 1го
                        i = 1;
                        while (ba[i] && i < ba.Length) i++;
                        if (i < ba.Length)
                            r = i;
                        else
                            r = -1; // все номера заняты (надо увеличить настройку? или удалить неиспользуемые)
                    }

                    attrValues.Values = new object[] { r.ToString("D4") };
                }
                else
                {
                    attrValues.Values = new object[] { "0001" };
                    return parameters;
                }
            }
        }
        return parameters;
    }
}