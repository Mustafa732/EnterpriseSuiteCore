using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;


namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_GradeWiseEmolumentHeader
    {
        #region Constructor

        
        #endregion


        #region Constant Strings

        const string Query_SelectAll_Search_WithPaging = @" SELECT HRMS_GradeWiseEmolumentHeader.[CompanyID], HRMS_GradeWiseEmolumentHeader.[PeriodID]
, [GradeWiseEmolumentHeaderID],HRMS_GradeWiseEmolumentHeader.[GradeID], CONVERT(VARCHAR(10),[EffectiveFrom],105)EffectiveFrom, HRMS_GradeWiseEmolumentHeader.[Status], HRMS_GradeWiseEmolumentHeader.[IsActive]
, HRMS_GradeWiseEmolumentHeader.[CreatedBy], HRMS_GradeWiseEmolumentHeader.[CreatedByValue]
, HRMS_GradeWiseEmolumentHeader.[CreateDate], HRMS_GradeWiseEmolumentHeader.[UpdatedBy], HRMS_GradeWiseEmolumentHeader.[UpdatedByValue], HRMS_GradeWiseEmolumentHeader.[UpdateDate],SET_Grade.GradeName
, ROW_NUMBER() Over (Order by HRMS_GradeWiseEmolumentHeader.CompanyID, GradeWiseEmolumentHeaderID, PeriodID) as 'Row'
 FROM [HRMS_GradeWiseEmolumentHeader]
 Inner Join SET_Grade on HRMS_GradeWiseEmolumentHeader.GradeID=SET_Grade.GradeID ";

        const string Query_SelectAll_Search_WithoutPaging = @" SELECT HRMS_GradeWiseEmolumentHeader.[CompanyID], HRMS_GradeWiseEmolumentHeader.[PeriodID], HRMS_GradeWiseEmolumentHeader.[GradeWiseEmolumentHeaderID], HRMS_GradeWiseEmolumentHeader.[GradeID], HRMS_GradeWiseEmolumentHeader.[EffectiveFrom], HRMS_GradeWiseEmolumentHeader.[Status], HRMS_GradeWiseEmolumentHeader.[IsActive],
  HRMS_GradeWiseEmolumentHeader.[CreatedBy], HRMS_GradeWiseEmolumentHeader.[CreatedByValue], HRMS_GradeWiseEmolumentHeader.[CreateDate], HRMS_GradeWiseEmolumentHeader.[UpdatedBy],SET_Grade.GradeName
  , HRMS_GradeWiseEmolumentHeader.[UpdatedByValue], HRMS_GradeWiseEmolumentHeader.[UpdateDate] FROM [HRMS_GradeWiseEmolumentHeader]
  Inner Join SET_Grade on HRMS_GradeWiseEmolumentHeader.GradeID=SET_Grade.GradeID
 ";

       

        #endregion.

        #region Class Methods

        public static DataTable GetAllData_Search(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Search_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_Search_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else 
                
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Search_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            
         
            
        }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        
    }
        #endregion
}