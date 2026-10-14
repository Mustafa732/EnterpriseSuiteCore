using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_GradeWisePayroll
    {
        # region "Constant Strings"

        const string Query_SelectAll_WithOtherTable = @"select SET_Grade.GradeName, HRMS_GradeWisePayroll.GradeID, GradeWisePayrollID, PayrollTypeID,
        PayrollTypeCode, AmountFrom, AmountTo, EffectiveFrom, EffectiveTo, HRMS_GradeWisePayroll.[Status], 
        HRMS_GradeWisePayroll.IsActive, HRMS_GradeWisePayroll.CreatedBy, HRMS_GradeWisePayroll.[CreatedByValue], 
        HRMS_GradeWisePayroll.[CreatedDate], HRMS_GradeWisePayroll.[UpdatedBy], 
        HRMS_GradeWisePayroll.[UpdatedByValue], HRMS_GradeWisePayroll.[UpdatedDate],
        ROW_NUMBER() Over (Order by HRMS_GradeWisePayroll.CompanyID, GradeWisePayrollID, 
        HRMS_GradeWisePayroll.PeriodID) as 'Row'
        From HRMS_GradeWisePayroll
        Inner Join SET_Grade ON HRMS_GradeWisePayroll.GradeID = SET_Grade.GradeID";

        #endregion


        # region "Class Methods"
        public static DataTable GetAllData_WithOtherTable(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOtherTable + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        # endregion
    }
}
