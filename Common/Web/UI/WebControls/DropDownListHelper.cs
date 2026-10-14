using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Common.Web.UI.WebControls
{
    public class DropDownListHelper
    {
        /// <summary>
        /// Add "Select" item in the beginning -.NET 8 MVC version
        /// </summary>
        public static void AddSelectItemAtBeginning(List<SelectListItem> ddl)
        {
            ddl.Insert(0, new SelectListItem { Text = "Select", Value = "-1" });
        }

        public static void AddEmptyOption(List<SelectListItem> ddl)
        {
            ddl.Insert(0, new SelectListItem { Text = "Select", Value = "" });
        }

        /// <summary>
        /// Add custom item in the beginning
        /// </summary>
        public static void AddSelectItemAtBeginning(List<SelectListItem> ddl, string strText, string strValue = "-1")
        {
            ddl.Insert(0, new SelectListItem { Text = strText, Value = strValue });
        }

        /// <summary>
        /// OLD method - DataTable me Select add karna - ye waisa ka waisa rahega
        /// </summary>
        public static void AddSelectItemInDataTable(ref DataTable dt, string dataTextField, string valueTextField)
        {
            DataRow drSelect = dt.NewRow();
            drSelect[dataTextField] = "Select";
            drSelect[valueTextField] = "-1";

            dt.Rows.InsertAt(drSelect, 0);
            dt.AcceptChanges();
        }

        // Helper for MVC - DataTable se direct SelectList banane ke liye
        public static List<SelectListItem> ConvertDataTableToSelectList(DataTable dt, string textField, string valueField, bool addSelect = true)
        {
            var list = new List<SelectListItem>();
            foreach (DataRow row in dt.Rows)
            {
                list.Add(new SelectListItem
                {
                    Text = row[textField]?.ToString() ?? "",
                    Value = row[valueField]?.ToString() ?? ""
                });
            }
            if (addSelect)
                AddSelectItemAtBeginning(list);

            return list;
        }
    }
}