using System;
using System.Data;

namespace FnI.Core.Web
{
    public class ASEnumFnISystem
    {
        public static DataTable BindToEnum(Type enumType)
        {
            DataTable dt = new DataTable();

            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);

            dt.Columns.Add(new DataColumn("key", typeof(string)));
            dt.Columns.Add(new DataColumn("value", typeof(string)));

            for (int i = 0; i < names.Length; i++)
            {
                DataRow row = dt.NewRow();
                row["key"] = Convert.ToInt16(values.GetValue(i)).ToString();
                row["value"] = names[i];
                dt.Rows.Add(row);
            }

            return dt;
        }

        //////
        //  In case of new addition, place the newly item in sorted order (alphabetically) but assign a new number. 
        //  (Warning) Do not change numbering of previous items due to change in sorting order, since these values are saved in the database.
        //////

        public enum InspectionDetailStatus
        {
            Ok = 1,
            Good = 2,
            NotGood = 3, 
            Rejected = 4,
            Approved = 5
        }
    }
}
