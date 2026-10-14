using System;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;
using System.Collections.Generic;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
   public partial class FA_ProcessInput
   {
       #region Constants

       const string Query_MySelectAll_WithPaging = @" SELECT [CompanyID], [ProcessInputID], [AssetHeadTypeID], [ProcessTypeID], [PeriodStatusID], [FAUpto], [Status],
 case [PeriodStatusID] when 1 then 'Day' when 2 then 'Month' when 3 then 'Quarter' when 4 then 'Year' end as PeriodStatus,
  case [ProcessTypeID] when 1 then 'Account Process' when 2 then 'Tax Process' end as ProcessType,
[CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], ROW_NUMBER() Over (Order by CompanyID, ProcessInputID) as 'Row' FROM [FA_ProcessInput] ";

       const string Query_MySelectAll_WithoutPaging = @" SELECT [CompanyID], [ProcessInputID], [AssetHeadTypeID], [ProcessTypeID], [PeriodStatusID], 
[FAUpto], [Status],
 case [PeriodStatusID] when 1 then 'Day' when 2 then 'Month' when 3 then 'Quarter' when 4 then 'Year' end as PeriodStatus,
  case [ProcessTypeID] when 1 then 'Account Process' when 2 then 'Tax Process' end as ProcessType,
[CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue] FROM [FA_ProcessInput] ";

       const string Query_GetItemsForProcess = @"

select FA_SET_AssetHeadType.CategoryID,FA_SET_AssetHeadType.AssetHeadTypeID, FA_SET_AssetType.ItemID,FA_AssetTypePolicy.PeriodStatus,FA_AssetTypePolicy.GLCodeCombinationID
    from FA_AssetTypePolicy

Inner Join FA_SET_AssetHeadType on FA_SET_AssetHeadType.AssetHeadTypeID = FA_AssetTypePolicy.AssetHeadTypeID
	and FA_SET_AssetHeadType.CompanyID = FA_AssetTypePolicy.CompanyID
	and FA_SET_AssetHeadType.Status = 1 

Inner Join FA_SET_AssetType on FA_SET_AssetType.AssetHeadTypeID = FA_SET_AssetHeadType.AssetHeadTypeID
	and FA_SET_AssetType.CompanyID = FA_SET_AssetHeadType.CompanyID
    and FA_SET_AssetType.Status = 1

where FA_AssetTypePolicy.Status = 1 
    and FA_AssetTypePolicy.CompanyID = @CompanyID
	and FA_AssetTypePolicy.IsActive = 1
	and AssetClassification = @AssetTypeID

";
       const string Query_GetDatabyAssetHeadTypeID = @"
SELECT [CompanyID], [ProcessInputID], [AssetHeadTypeID], [ProcessTypeID], [PeriodStatusID], 
[FAUpto], [Status],
 case [PeriodStatusID] when 1 then 'Day' when 2 then 'Month' when 3 then 'Quarter' when 4 then 'Year' end as PeriodStatus,
  case [ProcessTypeID] when 1 then 'Account Process' when 2 then 'Tax Process' end as ProcessType,
[CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue] FROM [FA_ProcessInput]
where FA_ProcessInput.Status = 1 and FA_ProcessInput.AssetHeadTypeID= @AssetHeadTypeID
and FA_ProcessInput.CompanyID = @CompanyID
";

       #endregion

       #region Methods

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
       
       public static DataTable GetItemIDForProcess(Nullable<int> companyID, int AssetTypeID, SqlConnection objConnection = null)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);              
               sqlparams[1] = new SqlParameter("@AssetTypeID", AssetTypeID);


               DataSet ds = null;

               if (objConnection == null)
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemsForProcess, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetItemsForProcess, sqlparams);

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

       public static DataTable GetDatabyAssetHeadTypeID(Nullable<int> companyID, int AssetHeadTypeID, SqlConnection objConnection = null)
       {
           DataTable dt = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@AssetHeadTypeID", AssetHeadTypeID);


               DataSet ds = null;

               if (objConnection == null)
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDatabyAssetHeadTypeID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDatabyAssetHeadTypeID, sqlparams);

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


       #endregion
   }
}
