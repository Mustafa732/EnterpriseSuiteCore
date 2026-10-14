using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;



namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Authority
    {

        const string Query_SelectAllForSearch_WithPaging = @" SELECT [CompanyID], [PeriodID], [AuthorityID], [AuthorityName]
, [AuthorityLevel], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], 
[UpdatedByValue], ROW_NUMBER() Over (Order by AuthorityName, CompanyID, PeriodID) as 'Row' FROM [SET_Authority] ";


        const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [CompanyID], [PeriodID], [AuthorityID], 
[AuthorityName], [AuthorityLevel], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], 
[UpdateDate], [UpdatedByValue] FROM [SET_Authority] ";





        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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
