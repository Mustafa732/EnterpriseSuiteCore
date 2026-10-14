using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.FA.Setup
{
   public partial class FA_Period
    {

       const string Query_MySelectAll_WithPaging = @" SELECT [CompanyID], [FAPeriodID], [IntervalID], [IntervalValue], [PeriodTypeID], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue],case PeriodTypeID when 1 then 'Life' when 2 then 'Warranty' end as PeriodType,
 case IntervalID when 1 then 'Days' when 2 then 'Weeks' when 3 then 'Months' when 4 then 'Years' end as IntervalName, ROW_NUMBER() Over (Order by CompanyID, FAPeriodID) as 'Row' FROM [FA_Period] ";

       const string Query_MySelectAll_WithoutPaging = @" SELECT [CompanyID], [FAPeriodID], [IntervalID], [IntervalValue], [PeriodTypeID], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue],case PeriodTypeID when 1 then 'Life' when 2 then 'Warranty' end as PeriodType,
 case IntervalID when 1 then 'Days' when 2 then 'Weeks' when 3 then 'Months' when 4 then 'Years' end as IntervalName FROM [FA_Period] "; 


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
    }
}
