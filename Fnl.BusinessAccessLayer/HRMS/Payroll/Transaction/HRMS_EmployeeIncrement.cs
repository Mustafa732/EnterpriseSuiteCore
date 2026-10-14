using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System.Text;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{

    public partial class HRMS_EmployeeIncrement
    {
        const string Query_SelectAll_WithPagingForSearch = @" 
Select IncrementID,HRMS_EmployeeIncrement.PersonID,FullName,HRMS_EmployeeIncrement.Status
   ,HRMS_Employement_Status.Description , HRMS_Set_Employee.EmployeeCode, HRMS_EmployeeIncrement.IncrementAmount,HRMS_EmployeeIncrement.IncrementDate
, OldGrossPay,TotalAmount
,ROW_NUMBER() Over (Order by IncrementID) as 'Row'
 from HRMS_EmployeeIncrement
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement.PersonID
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
";

        const string Query_SelectAll_WithoutPagingForSearch = @"  Select IncrementID,HRMS_EmployeeIncrement.PersonID,FullName,HRMS_EmployeeIncrement.Status
      ,HRMS_Employement_Status.Description , HRMS_Set_Employee.EmployeeCode, HRMS_EmployeeIncrement.IncrementAmount
 from HRMS_EmployeeIncrement
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement.PersonID
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
";

        const string Query_GetAllDataEmp = @"   

 ";

        const string Query_GetGradewiseIncData = @"   select * from HRMS_SET_GradeWiseIncrement where GradeID = @GradeID and {0} between BasicFrom and BasicTo


 ";
        public static DataTable GetEmpDataForIncrement(Nullable<int> companyID, Nullable<int> DepartmentID, Nullable<int> SectionID, Nullable<int> StatusID, Nullable<int> GradeID, Nullable<int> PersonID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            if (DepartmentID != null && DepartmentID > 0)
            {
                sb.Append(" and FK_DepartmentID =  " + DepartmentID);
            }
            if (SectionID != null && SectionID > 0)
            {
                sb.Append(" AND HRMS_Set_Employee.FK_SectionID =  " + SectionID);
            }
            if (StatusID != null && StatusID > 0)
            {
                sb.Append("and HRMS_Employement_Status.StatusID = " + StatusID);
            }
            if (GradeID != null && GradeID > 0)
            {
                sb.Append("and HRMS_Set_Employee.FK_GradeID = " + GradeID);
            }
            if (PersonID != null && PersonID > 0)
            {
                sb.Append("and HRMS_Set_Employee.PersonID = " + PersonID);
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = null;

               // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllDataEmp, sb.ToString()), sqlparams);
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllDataEmp, sb.ToString()), sqlparams); ;


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

        public static DataTable GetGradeWiseEmpDataForIncrement(int companyID,int GradeID, decimal BasicSalary)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            if (BasicSalary != null && BasicSalary > 0)
            {
                sb.Append(" " + BasicSalary);
            }
            //if (SectionID != null && SectionID > 0)
            //{
            //    sb.Append(" AND HRMS_Set_Employee.FK_SectionID =  " + SectionID);
            //}
            //if (StatusID != null && StatusID > 0)
            //{
            //    sb.Append("and HRMS_Employement_Status.StatusID = " + StatusID);
            //}
            //if (GradeID != null && GradeID > 0)
            //{
            //    sb.Append("and HRMS_Set_Employee.FK_GradeID = " + GradeID);
            //}
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@GradeID", GradeID);


                DataSet ds = null;

                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllDataEmp, sb.ToString()), sqlparams);
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetGradewiseIncData, sb.ToString()), sqlparams); ;


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

        public static DataTable GetDataForAdvanceEmp(DateTime FromDate,DateTime ToDate, Nullable<int> companyID, Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null, int LocationID = 0, int DivisionID = 0, int personTypeID = 0, Nullable<int> DesignationID = null, Nullable<int> DepartmentID = null, Nullable<int> SectionID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", FromDate);
                sqlparams[2] = new SqlParameter("@ToDate", ToDate);


                if (LocationID > 0)
                {
                    sb.Append(" And fintab.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" And fintab.DivisionID = " + DivisionID);
                }

                if (personTypeID > 0)
                {
                    sb.Append(" And fintab.PersonTypeCode = " + personTypeID);
                }
                if (DesignationID != null && DesignationID > 0)
                {
                    sb.Append(" And fintab.FK_JobDesignationID = " + DesignationID);
                }
                if (DepartmentID != null && DepartmentID > 0)
                {
                    sb.Append(" And fintab.FK_DepartmentID = " + DepartmentID);
                }

                if (SectionID != null && SectionID > 0)
                {
                    sb.Append(" And fintab.FK_SectionID = " + SectionID);
                }


                DataSet ds = null;



                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetPresentDaysForEmployees, sb.ToString()), sqlparams); ;


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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDetailData(int HeaderID, int CompanyID)
        {
            DataTable dt = null;
            const string query = @" 
Select *,IncrementID, '' as SectionName,'' as Action ,JobDesignationName as DesignationName , IncrementAmount as LastIncrementAmount,DeductionAmount
from HRMS_EmployeeIncrement

inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement.PersonID
and HRMS_Set_Employee.Status = 1

inner join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = FK_JobDesignationID
LEFT join SET_Section on SET_Section.SectionID = HRMS_Set_Employee.FK_SectionID
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
where HRMS_EmployeeIncrement.Status = 1 and IncrementID = @HeaderID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@HeaderID", HeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDetailDatanew(int EmployeeCode, int CompanyID, DateTime? IncreementDate = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            const string query = @" 
Select *,IncrementID, '' as SectionName,'' as Action ,JobDesignationName as DesignationName , IncrementAmount as LastIncrementAmount,DeductionAmount
,CAST(1 AS bit) AS isSelected, Set_Grade.GradeName
from HRMS_EmployeeIncrement

inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement.PersonID
and HRMS_Set_Employee.Status = 1

inner join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = FK_JobDesignationID
LEFT join SET_Section on SET_Section.SectionID = HRMS_Set_Employee.FK_SectionID
LEFT join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_Employement_Status.CompanyID = HRMS_Set_Employee.CompanyID
Left join Set_Grade on Set_Grade.GradeID = HRMS_SET_Employee.FK_GradeID
        and Set_Grade.status =1  
where HRMS_EmployeeIncrement.Status = 1 and HRMS_Set_Employee.EmployeeCode = @EmployeeCode
{0}
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@EmployeeCode", EmployeeCode);

                if (IncreementDate.HasValue)
                {
                    sb.Append(" AND HRMS_EmployeeIncrement.IncrementDate = '" + IncreementDate.Value.ToString("yyyy-MM-dd") + "'");
                }


                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams);


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


        public static DataTable CheckIncrementAgainstEmployee(int CompanyID, int PersonID, string Month, string Year)
        {
            DataTable dt = null;
            const string query = @" 
select * from HRMS_EmployeeIncrement where Status = 1 and PersonID = @PersonID and Month(EffectiveDate) = @Month and Year(EffectiveDate) = @Year and CompanyID=@CompanyID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", Month);
                sqlparams[3] = new SqlParameter("@Year", Year);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
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
