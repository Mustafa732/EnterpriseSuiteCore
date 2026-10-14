using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using System.Data;
using Common.Utils;

namespace FnI.BusinessAccessLayer.FA.Setup
{
   public partial class FA_ItemLocation
   {
       #region Constants

       const string Query_Delete_ByKey = @" Delete from [FA_ItemLocation] WHERE [ItemLocationID] = @ItemLocationID ";

       const string Query_MySelectAll_WithPaging = @" SELECT FA_ItemLocation.[CompanyID],Set_department.DepartmentName, FA_ItemLocation.[ItemLocationID], FA_ItemLocation.[ItemID], FA_ItemLocation.[LocationID], FA_ItemLocation.[DepartmentID],Inv_Item.[Description] as Item,Set_Location.LocationName, ROW_NUMBER() Over (Order by FA_ItemLocation.CompanyID, ItemLocationID) as 'Row' FROM [FA_ItemLocation]
inner join Inv_Item on Inv_Item.ItemID = FA_ItemLocation.ItemID
and INV_Item.Status = 1
inner join Set_Location on Set_Location.LocationID = FA_ItemLocation.LocationID
and Set_Location.Status = 1
left join Set_department on Set_department.DepartmentID = FA_ItemLocation.DepartmentID
and Set_department.Status = 1
";

       const string Query_MySelectAll_WithoutPaging = @" SELECT FA_ItemLocation.[CompanyID],Set_department.DepartmentName, FA_ItemLocation.[ItemLocationID], FA_ItemLocation.[ItemID], FA_ItemLocation.[LocationID], FA_ItemLocation.[DepartmentID],Inv_Item.[Description] as Item,Set_Location.LocationName FROM [FA_ItemLocation] 
inner join Inv_Item on Inv_Item.ItemID = FA_ItemLocation.ItemID
and INV_Item.Status = 1
inner join Set_Location on Set_Location.LocationID = FA_ItemLocation.LocationID
and Set_Location.Status = 1
left join Set_department on Set_department.DepartmentID = FA_ItemLocation.DepartmentID
and Set_department.Status = 1
"; 

       #endregion

       #region Methods

       public static bool DeleteDataByKey(SqlTransaction objTransaction, int ItemLocationID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@ItemLocationID", ItemLocationID);
              

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static DataTable MyGetAllData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
       {
           List<SqlParameter> sqlparamsList = new List<SqlParameter>();
           OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
           DataTable dt = null;

           try
           {
               if (PageIndex != null)
               {
                   sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                   sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                   sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                   SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                   sqlparamTotalRows.Direction = ParameterDirection.Output;

                   SqlParameter[] sqlparams = sqlparamsList.ToArray();

                   if (objTransaction != null)
                       dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                   else
                       dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_MySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                   ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
               }
               else
               {
                   if (objTransaction != null)
                       dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_MySelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                   else
                       dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_MySelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       #endregion

   }
}
