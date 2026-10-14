using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Utils;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_ApprovedStrength
    {
        #region Constant Strings

        const string Query_SelectAll_WithOtherTablesWithPaging = @" SELECT 
                    [HRMS_ApprovedStrength].[ApprovedStrengthID]
                    ,[HRMS_ApprovedStrength].[FK_GroupID]
                    ,[HRMS_ApprovedStrength].[FK_BusinessID]
                    ,[HRMS_ApprovedStrength].[FK_OperatingUnitID]
                    ,[SET_OperatingUnit].[OperatingUnitName]
                    ,[HRMS_ApprovedStrength].[FK_DepartmentID]
                    ,[SET_Department].[DepartmentName]
                    ,[HRMS_ApprovedStrength].[FK_SectionID]
                    ,[HRMS_ApprovedStrength].[FK_DesignationID]
                    ,[SET_Designation].[DesignationName]
                    ,[HRMS_ApprovedStrength].[EmploymentTypeID]
                    ,[HRMS_ApprovedStrength].[RequriedFromDate]
                    ,[HRMS_ApprovedStrength].[RequriedToDate]
                    ,[HRMS_ApprovedStrength].[ApprovedStrength]
                    ,[HRMS_ApprovedStrength].[Comments]
                    ,[HRMS_ApprovedStrength].[Attachment]
                    ,[HRMS_ApprovedStrength].[IsActive]
                    ,[HRMS_ApprovedStrength].[Status]
                    ,[HRMS_ApprovedStrength].[CreatedBy]
                    ,[HRMS_ApprovedStrength].[CreateDate]
                    ,[HRMS_ApprovedStrength].[CreatedByValue]
                    ,[HRMS_ApprovedStrength].[UpdatedByValue]
                    ,[HRMS_ApprovedStrength].[UpdatedBy]
                    ,[HRMS_ApprovedStrength].[UpdateDate]
                        
                    , ROW_NUMBER() OVER (ORDER BY [HRMS_ApprovedStrength].[ApprovedStrengthID]) AS 'Row'
                    FROM [HRMS_ApprovedStrength]
                    INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_ApprovedStrength].[FK_DepartmentID]
                    INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_ApprovedStrength].[FK_DesignationID]
                    INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_ApprovedStrength].[FK_OperatingUnitID]";

        const string Query_SelectAll_WithOtherTablesWithOutPaging = @" SELECT 
                    [HRMS_ApprovedStrength].[ApprovedStrengthID]
                    ,[HRMS_ApprovedStrength].[FK_GroupID]
                    ,[HRMS_ApprovedStrength].[FK_BusinessID]
                    ,[HRMS_ApprovedStrength].[FK_OperatingUnitID]
                    ,[SET_OperatingUnit].[OperatingUnitName]
                    ,[HRMS_ApprovedStrength].[FK_DepartmentID]
                    ,[SET_Department].[DepartmentName]
                    ,[HRMS_ApprovedStrength].[FK_SectionID]
                    ,[HRMS_ApprovedStrength].[FK_DesignationID]
                    ,[SET_Designation].[DesignationName]
                    ,[HRMS_ApprovedStrength].[EmploymentTypeID]
                    ,[HRMS_ApprovedStrength].[RequriedFromDate]
                    ,[HRMS_ApprovedStrength].[RequriedToDate]
                    ,[HRMS_ApprovedStrength].[ApprovedStrength]
                    ,[HRMS_ApprovedStrength].[Comments]
                    ,[HRMS_ApprovedStrength].[Attachment]
                    ,[HRMS_ApprovedStrength].[IsActive]
                    ,[HRMS_ApprovedStrength].[Status]
                    ,[HRMS_ApprovedStrength].[CreatedBy]
                    ,[HRMS_ApprovedStrength].[CreateDate]
                    ,[HRMS_ApprovedStrength].[CreatedByValue]
                    ,[HRMS_ApprovedStrength].[UpdatedByValue]
                    ,[HRMS_ApprovedStrength].[UpdatedBy]
                    ,[HRMS_ApprovedStrength].[UpdateDate]
                    FROM [HRMS_ApprovedStrength]
                    INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_ApprovedStrength].[FK_DepartmentID]
                    INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_ApprovedStrength].[FK_DesignationID]
                    INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [HRMS_ApprovedStrength].[FK_OperatingUnitID]";

        #endregion

        #region Class Methods

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
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithOtherTablesWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithOtherTablesWithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                        ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                    }
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOtherTablesWithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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
