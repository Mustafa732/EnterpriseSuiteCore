using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
  public partial  class SET_Grade
  {
      #region Constants

      const string Query_GetOnlyGrade = @" SELECT [SET_Grade].[GradeID], [SET_Grade].[GradeName], [SET_Grade].[Status] AS GradeStatus FROM [SET_Grade] 
                          WHERE [SET_Grade].Status = 1 AND ([SET_Grade].IsActive = @IsActive OR IsActive = -1) 
                          {0}  
                          ORDER BY [SET_Grade].[GradeName]";

      const string Query_GetOnlyGradeAmountRange = @" SELECT top 1 isnull(MinAmount,0) as MinAmount,isnull(MaxAmount,0) as MaxAmount,GradeID FROM [SET_Grade] 
                          WHERE Status = 1 AND ([SET_Grade].IsActive = @IsActive OR IsActive = -1) 
                          {0}  
                        ";

      //const string Query_SelectAllForSearch_WithPaging = @" SELECT [SET_Grade].[GradeID], [SET_Grade].[GradeName], [SET_Grade].[Status] AS GradeStatus, ROW_NUMBER() Over (Order by GradeName) as 'Row' FROM [SET_Grade] ";
      //const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [SET_Grade].[GradeID], [SET_Grade].[GradeName], [SET_Grade].[Status] AS GradeStatus FROM [SET_Grade] ";


      const string Query_SelectAllForSearch_WithPaging = @"  SELECT Distinct HRMS_SET_GradeWiseIncrement.GradeID, SET_Grade.GradeName, HRMS_SET_GradeWiseIncrement.GradeHeaderID,HRMS_SET_GradeWiseIncrement.BasicFrom,HRMS_SET_GradeWiseIncrement.BasicTo,HRMS_SET_GradeWiseIncrement.IncrementAmount, HRMS_SET_GradeWiseIncrement.[Status] AS Status, '' Action,  ROW_NUMBER() Over (Order by HRMS_SET_GradeWiseIncrement.GradeID) as 'Row'  FROM HRMS_SET_GradeWiseIncrement
 left join SET_Grade on SET_Grade.GradeID = HRMS_SET_GradeWiseIncrement.GradeID
";
      const string Query_SelectAllForSearch_WithoutPaging = @" SELECT [SET_Grade].[GradeID], [SET_Grade].[GradeName], [SET_Grade].[Status] AS GradeStatus FROM [SET_Grade] ";




      #endregion

      #region Methods

      public static DataTable GetOnlyGrade(int GradeID, int? isActive = null)
      {
          DataTable dt = null;
          StringBuilder sb = new StringBuilder();
          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];
              sqlparams[0] = new SqlParameter("@IsActive", isActive);
          
              if (GradeID > 0)
              sb.Append(" AND  [SET_Grade].GradeID = " + GradeID);

              DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_GetOnlyGrade,sb.ToString()), sqlparams);

              if (ds != null && ds.Tables.Count > 0)
                  dt = ds.Tables[0];
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }

      public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                      dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                  else
                      dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
              }
          }
          catch (Exception ex)
          {
              SqlHelper.LogException(ex);
              throw ex;
          }

          return dt;
      }



      public static DataTable GetOnlyGradeAmountRange(int GradeID, int? isActive = null)
      {
          DataTable dt = null;
          StringBuilder sb = new StringBuilder();
          try
          {
              SqlParameter[] sqlparams = new SqlParameter[1];
              sqlparams[0] = new SqlParameter("@IsActive", isActive);

              if (GradeID > 0)
                  sb.Append(" AND GradeID = " + GradeID);

              DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetOnlyGradeAmountRange, sb.ToString()), sqlparams);

              if (ds != null && ds.Tables.Count > 0)
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
