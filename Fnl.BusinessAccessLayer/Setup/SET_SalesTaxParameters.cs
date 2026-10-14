using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_SalesTaxParameters
    {
        const string Query_DataByItemID = @" 
select FromDate,ToDate,Item as ItemID,isnull(TUnitPrice,0) as TUnitPrice,isnull(SalesTaxPercent,0) as SalesTaxPercent,isnull(ExciseDuty,0) as ExciseDuty,isnull(FEDAmount,0) as FEDAmount,isnull(Inclusive,0) as Inclusive,isnull(GWithHoldingTax ,0) as GWithHoldingTax,isnull(FixedFEDPerTon,0) as FixedFEDPerTon
from SET_SalesTaxParameters
where Status = 1
--and SET_SalesTaxParameters.CompanyID = @CompanyID 
and SET_SalesTaxParameters.PeriodID = @PeriodID
and SET_SalesTaxParameters.Item = @ItemID
and isnull(SET_SalesTaxParameters.IsRegistered,0) = @IsRegistered
and @Date between FromDate and ToDate";

        const string Query_SelectAllData_WithPaging = @" SELECT [SalesTaxParameterID], [SET_SalesTaxParameters].[CompanyID], [SET_SalesTaxParameters].[PeriodID]
, [SET_SalesTaxParameters].[FromDate], [SET_SalesTaxParameters].[ToDate]
, [SET_SalesTaxParameters].[Item],INV_Item.Description as ItemDescription, [SET_SalesTaxParameters].[TUnitPrice],[SET_SalesTaxParameters].[SalesTaxPercent]
, [SET_SalesTaxParameters].[ExciseDuty],[SET_SalesTaxParameters].[FEDAmount], [SET_SalesTaxParameters].[Inclusive]
, [SET_SalesTaxParameters].[GWithHoldingTax], [SET_SalesTaxParameters].[IsActive], [SET_SalesTaxParameters].[Status],
[SET_SalesTaxParameters].[CreatedBy], [SET_SalesTaxParameters].[CreateDate], [SET_SalesTaxParameters].[UpdatedBy]
, [SET_SalesTaxParameters].[UpdateDate], [SET_SalesTaxParameters].[CreatedByValue], [SET_SalesTaxParameters].[UpdatedByValue], ROW_NUMBER() Over (Order by [SET_SalesTaxParameters].CompanyID, [SET_SalesTaxParameters].PeriodID, [SET_SalesTaxParameters].SalesTaxParameterID) as 'Row' FROM

[SET_SalesTaxParameters]

INNER JOIN INV_Item ON INV_Item.ItemID =[SET_SalesTaxParameters].Item
 "; 



        public static DataTable GetDataByItemID(int companyID, int periodID, int ItemID , DateTime Date, bool IsRegistered =false, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@Date", Date);
                sqlparams[4] = new SqlParameter("@IsRegistered", IsRegistered);
                

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DataByItemID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DataByItemID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DataByItemID, sqlparams);

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


        public static DataTable GetAllDataList(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllData_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithItemName(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                    string Query_WithPaging = @"SELECT INV_Item.Description as ItemDescription, [SalesTaxParameterID], [SET_SalesTaxParameters].[CompanyID], [PeriodID], [FromDate], [ToDate], [Item], [TUnitPrice],
			 [SalesTaxPercent], [ExciseDuty], [FEDAmount], [Inclusive], [GWithHoldingTax], [IsActive], [SET_SalesTaxParameters].[Status],
			  [SET_SalesTaxParameters].[CreatedBy], [SET_SalesTaxParameters].[CreateDate], [SET_SalesTaxParameters].[UpdatedBy],
			   [SET_SalesTaxParameters].[UpdateDate], [SET_SalesTaxParameters].[CreatedByValue], [SET_SalesTaxParameters].[UpdatedByValue],
			   ROW_NUMBER() Over (Order by [SET_SalesTaxParameters].CompanyID, [SET_SalesTaxParameters].PeriodID, SalesTaxParameterID) as 'Row' 
			   FROM [SET_SalesTaxParameters] 
			   left join INV_Item on INV_Item.ItemID = [SET_SalesTaxParameters].Item

";
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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
