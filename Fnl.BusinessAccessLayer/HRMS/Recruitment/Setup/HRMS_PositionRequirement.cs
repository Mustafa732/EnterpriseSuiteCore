using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Recruitment.Setup
{
    public partial class HRMS_PositionRequirement
    {
        #region Constant Strings

        

        const string Query_JobRequisitionData = @"
Select BusinessName
,OperatingUnitName
,JobDesignationName
, DepartmentName
,CityName
,HRMS_PositionRequirement.RefNo
,HRMS_PositionRequirement.RequestDate
,HRMS_PositionRequirement.Location
,HRMS_PositionRequirement.NoOfPositions
,HRMS_PositionRequirement.RequriedFromDate
,HRMS_PositionRequirement.RequriedToDate
,HRMS_PositionRequirement.MinExperience
,HRMS_PositionRequirement.MaxAgeLimit
,HRMS_PositionRequirement.EmploymentTypeID
,HRMS_PositionRequirement.MinQualificationID
,HRMS_PositionRequirement.Comments
 from HRMS_PositionRequirement
left join SET_Department on SET_Department.DepartmentID =  HRMS_PositionRequirement.FK_DepartmentID
and SET_Department.Status =1 
and HRMS_PositionRequirement.Status =1 
left join SET_City on SET_City.CityiD = HRMS_PositionRequirement.FK_CityID 
and SET_City.Status =1 
left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_PositionRequirement.FK_DesignationID
and Set_JobDesignation.Status =1 
and Set_JobDesignation.CompanyID = HRMS_PositionRequirement.CompanyID
left join SET_Business on  SET_Business.BusinessID =  HRMS_PositionRequirement.FK_BusinessID
and SET_Business.status =1 
left join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID =  HRMS_PositionRequirement.FK_OperatingUnitID
and  HRMS_PositionRequirement.status =1 
where HRMS_PositionRequirement.CompanyID =  @CompanyID
and HRMS_PositionRequirement.PeriodID = @PeriodID
{0}

";

        const string Query_SelectAll_WithOtherTables_WithPaging = @" SELECT 
                        [HRMS_PositionRequirement].[CompanyID], 
                        [HRMS_PositionRequirement].[PeriodID], 
                        [HRMS_PositionRequirement].[PositionRequirementID], 
                        [HRMS_PositionRequirement].[RefNo], 
                        [HRMS_PositionRequirement].[RequestDate], 
                        [HRMS_PositionRequirement].[FK_GroupID], 
                        [HRMS_PositionRequirement].[FK_BusinessID],
                        [SET_Business].[BusinessName], 
                        [HRMS_PositionRequirement].[FK_OperatingUnitID],
                        [SET_OperatingUnit].[OperatingUnitName],
                        [HRMS_PositionRequirement].[Location], 
                        [HRMS_PositionRequirement].[FK_DepartmentID],
                        [SET_Department].[DepartmentName],
                        [HRMS_PositionRequirement].[FK_SectionID],
                        [SET_Section].[SectionName], 
                        [HRMS_PositionRequirement].[FK_DesignationID],
                        [SET_Designation].[DesignationName],
                        [HRMS_PositionRequirement].[NoOfPositions], 
                        [HRMS_PositionRequirement].[EmploymentTypeID], 
                        [HRMS_PositionRequirement].[RequriedFromDate], 
                        [HRMS_PositionRequirement].[RequriedToDate], 
                        [HRMS_PositionRequirement].[MinQualificationID], 
                        [HRMS_PositionRequirement].[FK_CityID],
                        [SET_City].[CityName], 
                        [HRMS_PositionRequirement].[MinExperience], 
                        [HRMS_PositionRequirement].[MaxAgeLimit], 
                        [HRMS_PositionRequirement].[HasApprovedStrength], 
                        [HRMS_PositionRequirement].[Comments], 
                        [HRMS_PositionRequirement].[Attacment], 
                        [HRMS_PositionRequirement].[Status], 
                        [HRMS_PositionRequirement].[IsActive], 
                        [HRMS_PositionRequirement].[CreatedBy], 
                        [HRMS_PositionRequirement].[CreateDate], 
                        [HRMS_PositionRequirement].[CreatedByValue], 
                        [HRMS_PositionRequirement].[UpdatedBy], 
                        [HRMS_PositionRequirement].[UpdateDate],
                        [HRMS_PositionRequirement].[UpdatedByValue],
                        ROW_NUMBER() Over (Order by [HRMS_PositionRequirement].[CompanyID], [HRMS_PositionRequirement].[PeriodID], [HRMS_PositionRequirement].[PositionRequirementID]) as 'Row' 
                        FROM [HRMS_PositionRequirement]
                        INNER JOIN [SET_Business] ON [SET_Business].[BusinessID] = [HRMS_PositionRequirement].[FK_BusinessID]
                        INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitID] = [HRMS_PositionRequirement].[FK_OperatingUnitID] 
                        INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID]
                        LEFT  JOIN [SET_Section] ON [SET_Section].[SectionID] = [HRMS_PositionRequirement].[FK_SectionID]
                        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
                        INNER JOIN [SET_City] ON [SET_City].[CityID] = [HRMS_PositionRequirement].[FK_CityID]";

        const string Query_SelectAll_WithOtherTables_WithOutPaging = @" SELECT 
                        [HRMS_PositionRequirement].[CompanyID], 
                        [HRMS_PositionRequirement].[PeriodID], 
                        [HRMS_PositionRequirement].[PositionRequirementID], 
                        [HRMS_PositionRequirement].[RefNo], 
                        [HRMS_PositionRequirement].[RequestDate], 
                        [HRMS_PositionRequirement].[FK_GroupID], 
                        [HRMS_PositionRequirement].[FK_BusinessID],
                        [SET_Business].[BusinessName], 
                        [HRMS_PositionRequirement].[FK_OperatingUnitID],
                        [SET_OperatingUnit].[OperatingUnitName],
                        [HRMS_PositionRequirement].[Location], 
                        [HRMS_PositionRequirement].[FK_DepartmentID],
                        [SET_Department].[DepartmentName],
                        [HRMS_PositionRequirement].[FK_SectionID],
                        [SET_Section].[SectionName], 
                        [HRMS_PositionRequirement].[FK_DesignationID],
                        [SET_Designation].[DesignationName],
                        [HRMS_PositionRequirement].[NoOfPositions], 
                        [HRMS_PositionRequirement].[EmploymentTypeID], 
                        [HRMS_PositionRequirement].[RequriedFromDate], 
                        [HRMS_PositionRequirement].[RequriedToDate], 
                        [HRMS_PositionRequirement].[MinQualificationID], 
                        [HRMS_PositionRequirement].[FK_CityID],
                        [SET_City].[CityName], 
                        [HRMS_PositionRequirement].[MinExperience], 
                        [HRMS_PositionRequirement].[MaxAgeLimit], 
                        [HRMS_PositionRequirement].[HasApprovedStrength], 
                        [HRMS_PositionRequirement].[Comments], 
                        [HRMS_PositionRequirement].[Attacment], 
                        [HRMS_PositionRequirement].[Status], 
                        [HRMS_PositionRequirement].[IsActive], 
                        [HRMS_PositionRequirement].[CreatedBy], 
                        [HRMS_PositionRequirement].[CreateDate], 
                        [HRMS_PositionRequirement].[CreatedByValue], 
                        [HRMS_PositionRequirement].[UpdatedBy], 
                        [HRMS_PositionRequirement].[UpdateDate],
                        [HRMS_PositionRequirement].[UpdatedByValue]
                        FROM [HRMS_PositionRequirement]
                        INNER JOIN [SET_Business] ON [SET_Business].[BusinessID] = [HRMS_PositionRequirement].[FK_BusinessID]
                        INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitID] = [HRMS_PositionRequirement].[FK_OperatingUnitID] 
                        INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID]
                        LEFT  JOIN [SET_Section] ON [SET_Section].[SectionID] = [HRMS_PositionRequirement].[FK_SectionID]
                        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
                        INNER JOIN [SET_City] ON [SET_City].[CityID] = [HRMS_PositionRequirement].[FK_CityID]";

        const string Query_GetData_WithOtherTable_byPositionRefNo = @" SELECT 
                        [HRMS_PositionRequirement].[CompanyID], 
                        [HRMS_PositionRequirement].[PeriodID], 
                        [HRMS_PositionRequirement].[PositionRequirementID], 
                        [HRMS_PositionRequirement].[RefNo], 
                        [HRMS_PositionRequirement].[RequestDate], 
                        [HRMS_PositionRequirement].[FK_GroupID], 
                        [HRMS_PositionRequirement].[FK_BusinessID],
                        [SET_Business].[BusinessName], 
                        [HRMS_PositionRequirement].[FK_OperatingUnitID],
                        [SET_OperatingUnit].[OperatingUnitName],
                        [HRMS_PositionRequirement].[Location], 
                        [HRMS_PositionRequirement].[FK_DepartmentID],
                        [SET_Department].[DepartmentName],
                        [HRMS_PositionRequirement].[FK_SectionID],
                        [SET_Section].[SectionName], 
                        [HRMS_PositionRequirement].[FK_DesignationID],
                        [SET_Designation].[DesignationName],
                        [SET_Designation].[FK_GradeID],
                        [HRMS_PositionRequirement].[NoOfPositions], 
                        [HRMS_PositionRequirement].[EmploymentTypeID], 
                        [HRMS_PositionRequirement].[RequriedFromDate], 
                        [HRMS_PositionRequirement].[RequriedToDate], 
                        [HRMS_PositionRequirement].[MinQualificationID], 
                        [HRMS_PositionRequirement].[FK_CityID],
                        [SET_City].[CityName], 
                        [HRMS_PositionRequirement].[MinExperience], 
                        [HRMS_PositionRequirement].[MaxAgeLimit], 
                        [HRMS_PositionRequirement].[HasApprovedStrength], 
                        [HRMS_PositionRequirement].[Comments], 
                        [HRMS_PositionRequirement].[Attacment], 
                        [HRMS_PositionRequirement].[Status], 
                        [HRMS_PositionRequirement].[IsActive], 
                        [HRMS_PositionRequirement].[CreatedBy], 
                        [HRMS_PositionRequirement].[CreateDate], 
                        [HRMS_PositionRequirement].[CreatedByValue], 
                        [HRMS_PositionRequirement].[UpdatedBy], 
                        [HRMS_PositionRequirement].[UpdateDate],
                        [HRMS_PositionRequirement].[UpdatedByValue],
						[HRMS_CandidateProcessHeader].StatusCode
                        FROM [HRMS_PositionRequirement]
                        INNER JOIN [SET_Business] ON [SET_Business].[BusinessID] = [HRMS_PositionRequirement].[FK_BusinessID]
                        INNER JOIN [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitID] = [HRMS_PositionRequirement].[FK_OperatingUnitID] 
                        INNER JOIN [SET_Department] ON [SET_Department].[DepartmentID] = [HRMS_PositionRequirement].[FK_DepartmentID]
                        LEFT  JOIN [SET_Section] ON [SET_Section].[SectionID] = [HRMS_PositionRequirement].[FK_SectionID]
                        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID]
                        INNER JOIN [SET_City] ON [SET_City].[CityID] = [HRMS_PositionRequirement].[FK_CityID]
						LEFT JOIN [HRMS_CandidateProcessHeader] ON [HRMS_CandidateProcessHeader].[FK_PositionRequirementID] = [HRMS_PositionRequirement].[PositionRequirementID]
                        WHERE [HRMS_PositionRequirement].[RefNo] =@RefNo AND [HRMS_PositionRequirement].[Status] = 1";

        const string Query_GetRow_byPositionRefNo = @" SELECT * FROM [HRMS_PositionRequirement] WHERE [RefNo] = @RefNo AND [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)";
        
        const string Query_GetData_byPositionRefNo = @" SELECT
                        [HRMS_PositionRequirement].[PositionRequirementID],
                        [SET_Designation].[DesignationName]
                        FROM [HRMS_PositionRequirement]
                        INNER JOIN [SET_Designation] ON [SET_Designation].[DesignationID] = [HRMS_PositionRequirement].[FK_DesignationID] 
                        WHERE [RefNo] = @RefNo";


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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithOtherTables_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithOtherTables_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithOtherTables_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOtherTables_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataWithOtherTablesByRefNo(string refNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RefNo", refNo);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_WithOtherTable_byPositionRefNo, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_WithOtherTable_byPositionRefNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_WithOtherTable_byPositionRefNo, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow GetDataByRefNo(string refNo, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@RefNo", refNo);
                if (isActive.HasValue)
                    sqlparams[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[1] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetRow_byPositionRefNo, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRow_byPositionRefNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRow_byPositionRefNo, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetJobRequisitionData(Nullable<int> department, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int > companyid,Nullable<int> period, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null ;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                DataSet ds;
                StringBuilder sb = new StringBuilder();
                sb.Append("  and HRMS_PositionRequirement.RequestDate between '"+ datefrom +"' and  '"+ dateto +"'  ");
                if (department > 0)
                {

                    sb.Append("  and SET_Department.DepartmentID = "+ department +" ");
                }


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text,string.Format( Query_JobRequisitionData,sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_JobRequisitionData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_JobRequisitionData, sb), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            //Query_JobRequisitionData
        return dt;
        }



        public static DataTable GetDataTableByRefNo(string refNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@RefNo", refNo);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byPositionRefNo, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPositionRefNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPositionRefNo, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetApproveStrength(int comapnyid, int periodid, int businessid, int unitid, int deptid, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            int dt = 0;

            string Query_GetApproveStrength = @"select count(*) from HRMS_PositionRequirement 
                    where CompanyID = @CompanyID AND PeriodID = @PeriodID AND FK_BusinessID= @BusinessID AND FK_OperatingUnitID = @OperatingUnitID
                    AND FK_DepartmentID = @DepartmentID AND  HasApprovedStrength = 1";


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", comapnyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@BusinessID", businessid);
                sqlparams[3] = new SqlParameter("@OperatingUnitID", unitid);
                sqlparams[4] = new SqlParameter("@DepartmentID", deptid);



                if (objTransaction != null)
                    dt = int.Parse(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetApproveStrength, sqlparams).ToString());
                else if (objConnection != null)
                    dt = int.Parse(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetApproveStrength, sqlparams).ToString());
                else
                    dt = int.Parse(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetApproveStrength, sqlparams).ToString());


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
