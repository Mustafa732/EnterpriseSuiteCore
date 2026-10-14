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
   public partial  class HRMS_EmployeeExperience
   {
       #region Constants

       const string Query_DeleteDataByEmployeeID = @"
delete from HRMS_EmployeeExperience where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID

";
       const string Query_Delete_ByPersonID = @" Update [HRMS_EmployeeExperience] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";

       const string Query_GetNew_MaxId = @" SELECT IsNull(max(EmployeeExperienceID), 0) + 1 as EmployeeExperienceID FROM HRMS_EmployeeExperience WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

       const string Query_GetDatabyEmployeeID = @"
select HRMS_Set_Employee.FullName,HRMS_EmployeeExperience.*,'' as [Action] from HRMS_EmployeeExperience
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeExperience.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeExperience.CompanyID
where HRMS_EmployeeExperience.CompanyID = @CompanyID
and HRMS_EmployeeExperience.PeriodID = @PeriodID
and HRMS_EmployeeExperience.Status = 1
{0}

";

      const string  Query_GetDatabyEmployeeIDOnSearch= @"
       select distinct 
 Set_Country.CountryName,HRMS_Set_Employee.FullName,HRMS_EmployeeExperience.*,'' as [Action],
 HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,HRMS_Set_Employee.MobileNumber,
Set_Designation.DesignationName as Designation,SET_Department.DepartmentName as Department,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.Email,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit,HRMS_Set_Shift.Description as Shift,HRMS_Set_Shift.ShiftID,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID,Set_Designation.DesignationID,set_Location.LocationID,set_Location.Description as Location,Set_Company.CompanyID

  from HRMS_EmployeeExperience
 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeExperience.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeExperience.CompanyID

left join Set_Country on Set_Country.CountryID = HRMS_EmployeeExperience.Country

left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
         left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
        
        left join HRMS_SET_Shift on HRMS_SET_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
        and HRMS_SET_Shift.ShiftCode = HRMS_EmployeeShift.ShiftCode
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

where HRMS_EmployeeExperience.CompanyID = @CompanyID
and HRMS_EmployeeExperience.PeriodID = @PeriodID
and HRMS_EmployeeExperience.Status = 1
and HRMS_Set_Employee.Status = 1
and Set_Country.Status= 1
{0}
       ";
       #endregion

       #region Methods
       public static DataTable DeleteDataByEmployeeID(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }


       public static int GetMyNewEmployeeExperienceID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetNew_MaxId, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetNew_MaxId, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetNew_MaxId, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }


       public static bool DeleteDataByPersonID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int personID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", personID);
               sqlparams[3] = new SqlParameter("@Status", false);
               sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByPersonID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByPersonID, sqlparams) > 0)
                       result = true;
               }
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static DataTable GetDataByEmployeeIDOnSearch(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeExperience.PersonID = " + PersonID);
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyEmployeeIDOnSearch, sb.ToString()), sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }
    

       public static DataTable GetDataByEmployeeID(int CompanyID, int PeriodID, int PersonID, int DesignationID, string CnicNo, int StatusCode, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeExperience.PersonID = " + PersonID);
               if (DesignationID > 0)
                   sb.Append(" AND HRMS_SET_Employee.FK_JobDesignationID =" + DesignationID);
               if (CnicNo != "")
                   sb.Append(" AND HRMS_SET_Employee.CNICNumber like '%" + CnicNo + "%' ");
               if (StatusCode > 0)
                   sb.Append(" and HRMS_EmployeeExperience.StatusCode = " + StatusCode);
               

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyEmployeeID, sb.ToString()), sqlparams);

               if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                   dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dr;
       }


       #endregion
   }
}
