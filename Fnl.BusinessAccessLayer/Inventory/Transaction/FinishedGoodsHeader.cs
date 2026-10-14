using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
   public partial class FinishedGoodsHeader
   {
       #region

       const string Query_SelectAll_Searching_WithPaging = @" SELECT [FinishedGoodsHeader].[StockTransactionID],[FinishedGoodsHeader].[CompanyID], [FinishedGoodsHeader].[FinishGoodsHeaderID], [FinishedGoodsHeader].[PeriodID],
[FinishedGoodsHeader].[DocumentTypeID],[FinishedGoodsHeader].[DocumentNo],[FinishedGoodsHeader].[DocumentDate],
 [FinishedGoodsHeader].[WorkOrderNo],[FinishedGoodsHeader].[CustomerID], [FinishedGoodsHeader].[CustomerOrderNo],
 [FinishedGoodsHeader].[CustomerOrderDate],[FinishedGoodsHeader].[CreateDate], [FinishedGoodsHeader].[CreatedByValue],
  [FinishedGoodsHeader].[CreatedBy], [FinishedGoodsHeader].[UpdateDate], [FinishedGoodsHeader].[UpdatedByValue],
  [FinishedGoodsHeader].[UpdatedBy], [FinishedGoodsHeader].[Status],AR_Set_Customer.CustomerName,
   ROW_NUMBER() Over (Order by [FinishedGoodsHeader].CompanyID, FinishGoodsHeaderID) as 'Row'
  FROM [FinishedGoodsHeader] 
inner join AR_Set_Customer on AR_Set_Customer.CustomerID = [FinishedGoodsHeader].CustomerID
and AR_Set_Customer.CompanyID = [FinishedGoodsHeader].CompanyID
and AR_Set_Customer.PeriodID = [FinishedGoodsHeader].PeriodID
and AR_Set_Customer.Status = 1 

";

       const string Query_SelectAll_Searching = @"
           
           SELECT [FinishedGoodsHeader].[StockTransactionID],[FinishedGoodsHeader].[CompanyID], [FinishedGoodsHeader].[FinishGoodsHeaderID], [FinishedGoodsHeader].[PeriodID],
[FinishedGoodsHeader].[DocumentTypeID],[FinishedGoodsHeader].[DocumentNo],[FinishedGoodsHeader].[DocumentDate],
 [FinishedGoodsHeader].[WorkOrderNo],[FinishedGoodsHeader].[CustomerID], [FinishedGoodsHeader].[CustomerOrderNo],
 [FinishedGoodsHeader].[CustomerOrderDate],[FinishedGoodsHeader].[CreateDate], [FinishedGoodsHeader].[CreatedByValue],
  [FinishedGoodsHeader].[CreatedBy], [FinishedGoodsHeader].[UpdateDate], [FinishedGoodsHeader].[UpdatedByValue],
  [FinishedGoodsHeader].[UpdatedBy], [FinishedGoodsHeader].[Status],AR_Set_Customer.CustomerName
    FROM [FinishedGoodsHeader] 
inner join AR_Set_Customer on AR_Set_Customer.CustomerID = [FinishedGoodsHeader].CustomerID
and AR_Set_Customer.CompanyID = [FinishedGoodsHeader].CompanyID
and AR_Set_Customer.PeriodID = [FinishedGoodsHeader].PeriodID
and AR_Set_Customer.Status = 1 
           ";
       const string Query_Get_MaxDocumentNo = @" SELECT IsNull(max(DocumentNo), 0) + 1 as DocumentNo FROM FinishedGoodsHeader WHERE [CompanyID] = @CompanyID ";
       #endregion

       public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

       public static int GetNewDocumentNo(int companyID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxDocumentNo, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxDocumentNo, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxDocumentNo, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

    }
}
