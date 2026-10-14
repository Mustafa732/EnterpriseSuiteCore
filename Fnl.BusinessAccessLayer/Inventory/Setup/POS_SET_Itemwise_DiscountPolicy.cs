using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class POS_SET_Itemwise_DiscountPolicy
    {

        const string Query_SelectAll_WithPagings = @"
SELECT POS_SET_Itemwise_DiscountPolicy.PolicyID,POS_SET_Itemwise_DiscountPolicy.DiscountPer,POS_SET_Itemwise_DiscountPolicy.CreateDate,
POS_INV_Item.Description ,SET_ItemCatalog.Description AS ItemCatalog,POS_BusinessPartnerCategory.CategoryName AS BPCategoryName,POS_SET_CUstomer.CustomerName,INV_SET_Category.CategoryName,
 ROW_NUMBER() Over (Order by POS_SET_Itemwise_DiscountPolicy.CompanyId, POS_SET_Itemwise_DiscountPolicy.PolicyID) as 'Row' 
 FROM POS_SET_Itemwise_DiscountPolicy 
 
 LEFT JOIN POS_INV_Item ON POS_INV_Item.ItemID =  POS_SET_Itemwise_DiscountPolicy.ItemID
 --AND POS_INV_Item.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_INV_Item.Status =1 
 
 LEFT JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCataLogID =  POS_SET_Itemwise_DiscountPolicy.ItemCataLogID
 AND SET_ItemCatalog.Status =1 
 
 LEFT JOIN POS_BusinessPartnerCategory ON POS_BusinessPartnerCategory.BuniessPartnerCategoryID =  POS_SET_Itemwise_DiscountPolicy.BpCatagoryID
 AND POS_BusinessPartnerCategory.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_BusinessPartnerCategory.Status =1
 
  LEFT JOIN POS_SET_CUstomer ON POS_SET_CUstomer.CustomerID =  POS_SET_Itemwise_DiscountPolicy.CustomerID
 AND POS_SET_CUstomer.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_SET_CUstomer.Status =1 
 
  LEFT JOIN INV_SET_Category ON INV_SET_Category.CategoryID =  POS_SET_Itemwise_DiscountPolicy.ItemCategoryID
 --AND POS_INV_Item.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_INV_Item.Status =1";

        const string Query_SelectAll_WithoutPagings = @"
SELECT POS_SET_Itemwise_DiscountPolicy.PolicyID,POS_SET_Itemwise_DiscountPolicy.DiscountPer,POS_SET_Itemwise_DiscountPolicy.CreateDate,
POS_INV_Item.Description ,SET_ItemCatalog.Description AS ItemCatalog,POS_BusinessPartnerCategory.CategoryName AS BPCategoryName,POS_SET_CUstomer.CustomerName,INV_SET_Category.CategoryName
--,ROW_NUMBER() Over (Order by POS_SET_Itemwise_DiscountPolicy.CompanyId, POS_SET_Itemwise_DiscountPolicy.PolicyID) as 'Row' 
 FROM POS_SET_Itemwise_DiscountPolicy 
 
 LEFT JOIN POS_INV_Item ON POS_INV_Item.ItemID =  POS_SET_Itemwise_DiscountPolicy.ItemID
 --AND POS_INV_Item.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_INV_Item.Status =1 
 
 LEFT JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCataLogID =  POS_SET_Itemwise_DiscountPolicy.ItemCataLogID
 AND SET_ItemCatalog.Status =1 
 
 LEFT JOIN POS_BusinessPartnerCategory ON POS_BusinessPartnerCategory.BuniessPartnerCategoryID =  POS_SET_Itemwise_DiscountPolicy.BpCatagoryID
 AND POS_BusinessPartnerCategory.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_BusinessPartnerCategory.Status =1
 
  LEFT JOIN POS_SET_CUstomer ON POS_SET_CUstomer.CustomerID =  POS_SET_Itemwise_DiscountPolicy.CustomerID
 AND POS_SET_CUstomer.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_SET_CUstomer.Status =1 
 
  LEFT JOIN INV_SET_Category ON INV_SET_Category.CategoryID =  POS_SET_Itemwise_DiscountPolicy.ItemCategoryID
 --AND POS_INV_Item.CompanyId =  POS_SET_Itemwise_DiscountPolicy.CompanyId
 AND POS_INV_Item.Status =1
 
";

        const string Pagings = @"WITH ResultSet AS({0})
			SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize;
			SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query)";

        const string Query_Validate_ItemWiseDiscount = @"
SELECT ItemCataLogID,ItemCategoryID,ItemID,BpCatagoryID ,CustomerID
FROM POS_SET_Itemwise_DiscountPolicy
WHERE 
Status = 1
AND CompanyId = @CompanyId
{0}";

        public static DataTable ValidateItemWiseDiscount(int CompanyId, int ItemCataLogID, int ItemCategoryID, int ItemID, int BpCatagoryID, int CustomerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyId", CompanyId);
               
                DataSet ds = null;

              
                if (ItemCataLogID > 0)
                {
                    sb.Append(" and ItemCataLogID=" + ItemCataLogID + "");
                }
                if (ItemCategoryID > 0)
                {
                    sb.Append(" and ItemCategoryID=" + ItemCategoryID + "");
                }

                if (ItemID > 0)
                {
                    sb.Append(" and ItemID =" + ItemID + "");
                }

                if (BpCatagoryID > 0)
                {
                    sb.Append(" and BpCatagoryID =" + BpCatagoryID + "");
                }
                if (CustomerID != 0)
                {
                    sb.Append(" and CustomerID = " + CustomerID + "");
                }





                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text,string.Format(Query_Validate_ItemWiseDiscount,sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Validate_ItemWiseDiscount, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Validate_ItemWiseDiscount, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData_ByOtherTable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Pagings, Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Pagings, Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



    }
}
