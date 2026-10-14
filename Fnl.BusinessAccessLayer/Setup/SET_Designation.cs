using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_Designation
    {
        #region Constant Strings

        const string Query_SelectAll_WithOtherTables_WithPaging = @" SELECT 
                        [SET_Designation].[DesignationID], 
                        [SET_Designation].[DesignationName], 
                        [SET_Designation].[IsActive], 
                        [SET_Designation].[Status], 
                        [SET_Designation].[CreatedBy],
                        [SET_Designation].[CreateDate],
                        [SET_Designation].[CreatedByValue],
                        [SET_Designation].[UpdatedBy],
                        [SET_Designation].[UpdateDate],
                        [SET_Designation].[UpdatedByValue],
                        [SET_Designation].[FK_GradeID],
                        [SET_Grade].[GradeName],
                        [SET_Designation].[ReportTo],
                        [ReportTo].[DesignationName] AS 'ReportToName',
                        [SET_Designation].[CompanyID],
                        ROW_NUMBER() Over (Order by [SET_Designation].DesignationID) as 'Row' 
                        FROM [SET_Designation] 
                        INNER JOIN [SET_Grade] ON [SET_Grade].[GradeID] = [SET_Designation].[FK_GradeID]
                        INNER JOIN [SET_Designation] AS [ReportTo] ON [ReportTo].[DesignationID] = [SET_Designation].[DesignationID] ";

        const string Query_SelectAll_WithOtherTables_WithOutPaging = @" SELECT 
                        [SET_Designation].[DesignationID], 
                        [SET_Designation].[DesignationName], 
                        [SET_Designation].[IsActive], 
                        [SET_Designation].[Status], 
                        [SET_Designation].[CreatedBy],
                        [SET_Designation].[CreateDate],
                        [SET_Designation].[CreatedByValue],
                        [SET_Designation].[UpdatedBy],
                        [SET_Designation].[UpdateDate],
                        [SET_Designation].[UpdatedByValue],
                        [SET_Designation].[FK_GradeID],
                        [SET_Grade].[GradeName],
                        [SET_Designation].[ReportTo],
                        [ReportTo].[DesignationName] AS 'ReportToName',
                        [SET_Designation].[CompanyID],
                        ROW_NUMBER() Over (Order by [SET_Designation].DesignationID) as 'Row' 
                        FROM [SET_Designation] 
                        INNER JOIN [SET_Grade] ON [SET_Grade].[GradeID] = [SET_Designation].[FK_GradeID]
                        INNER JOIN [SET_Designation] AS [ReportTo] ON [ReportTo].[DesignationID] = [SET_Designation].[ReportTo]";



        const string Query_SelectAllForSearch_WithPaging = @" SELECT [DesignationID], [DesignationName], [IsActive], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], [FK_GradeID], [ReportTo], ROW_NUMBER() Over (Order by DesignationName) as 'Row' FROM [SET_Designation] ";
        const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [DesignationID], [DesignationName], [IsActive], [Status], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], [FK_GradeID], [ReportTo], FROM [SET_Designation] "; 



        #endregion

        #region Class Methods

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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





        #endregion
    }
}
