using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;
using System.Data;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_EmployeeEducation
   {
       #region Constants
       const string Query_GetDataFromLookupHeader = @"

SELECT [LookupDetail].[LookupDetailID],[LookupDetail].[EnumeratedDescription]
FROM [LookupHeader]
inner join [LookupDetail] on [LookupDetail].[LookupHeaderID] = [LookupHeader].[LookupID] and [LookupDetail].[IsActive]=1
where LookupType='EduGradeTtype'
and [LookupDetail].LookupHeaderID > 1
order by [LookupDetail].[OrderBy]
";

       const string Query_GetDataForPayRollType = @"
select  'Select' as 'Name' ,  '-1' as 'Value'
union
select 'Allowances' as  'Name', '1' as 'Value'
union
select 'Deductions' as  'Name','2' as 'Value'
order by Value

";


       const string Query_Get_MyMaxId = @" SELECT IsNull(max(EmployeeEducationID), 0) + 1 as EmployeeEducationID FROM HRMS_EmployeeEducation WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID";
      /// <summary>
      /// //
      /// </summary>
      /// ssassdaaadaada htht

       const string Query_DeleteDataByEmployeeID = @"
delete from HRMS_EmployeeEducation where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID

";

       const string Query_GetDatabyEmployeeID = @"
select HRMS_Set_Employee.FullName,HRMS_EmployeeEducation.*,'' as Action from HRMS_EmployeeEducation
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeEducation.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeEducation.CompanyID
where HRMS_EmployeeEducation.CompanyID = @CompanyID
and HRMS_EmployeeEducation.PeriodID = @PeriodID
and HRMS_EmployeeEducation.Status = 1
{0}

";

       const string Query_GetDatabyEmployeeIDToBind = @"
select 
HRMS_EmployeeEducation.PersonID 
,HRMS_SET_Employee.FullName
,HRMS_EmployeeEducation.Degree
,HRMS_EmployeeEducation.Country
,HRMS_EmployeeEducation.DateFrom 
,HRMS_EmployeeEducation.DateTo 
,HRMS_EmployeeEducation.Institute
,HRMS_EmployeeEducation.GradingCriteria
,HRMS_EmployeeEducation.KeySubjects
,HRMS_EmployeeEducation.Grade
,HRMS_EmployeeEducation.Comments 
,[LookupDetail].[EnumeratedDescription]  as GradingCriteriaName
,eduCntry.CountryName 
,'' as Action 
from  
 
  HRMS_EmployeeEducation 

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_EmployeeEducation.PersonID
and HRMS_EmployeeEducation.Status =1 

left join SET_Country eduCntry on eduCntry.CountryID = HRMS_EmployeeEducation.Country
and eduCntry.Status =1 

left join [LookupDetail] on [LookupDetail].LookupDetailID  =  HRMS_EmployeeEducation.GradingCriteria 
and [LookupDetail].LookupHeaderID = 18 
where HRMS_EmployeeEducation.Status =1 
and HRMS_EmployeeEducation.CompanyID = @CompanyID
and HRMS_EmployeeEducation.PeriodID = @PeriodID
and HRMS_EmployeeEducation.PersonID =  @PersonID

";

       const string Query_GetDatabyEmployeeIDOnSearch = @"

select distinct 
CASE 
WHEN  GradingCriteria = 1 then 'Percentage' 
when GradingCriteria = 2 then 'GPA' 
when GradingCriteria = 3 then 'Grade'
else '' end as GradingCriteriaName,
 Set_Country.CountryName,HRMS_Set_Employee.FullName,
HRMS_EmployeeEducation.*,
'' as [Action],
 HRMS_Set_Employee.FullName
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,
Set_JobDesignation.JobDesignationName as Designation
,SET_Department.DepartmentName as Department
,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,
HRMS_Set_Employee.Email
,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit
,HRMS_Set_Shift.Description as Shift
,HRMS_Set_Shift.ShiftID
,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID
,Set_JobDesignation.JobDesignationID as DesignationID
,set_Location.LocationID
,set_Location.Description as Location
,Set_Company.CompanyID
  from HRMS_EmployeeEducation
 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeEducation.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeEducation.CompanyID

left join Set_Country on Set_Country.CountryID = HRMS_EmployeeEducation.Country

left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		and Set_JobDesignation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
          left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
        
           left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

where HRMS_EmployeeEducation.CompanyID = @CompanyID
and HRMS_EmployeeEducation.PeriodID = @PeriodID
and HRMS_EmployeeEducation.Status = 1
and HRMS_Set_Employee.Status = 1
and Set_Country.Status= 1
{0}
";
       const string Query_Delete_ByPersonID = @"

 Update [HRMS_EmployeeEducation] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";
       #endregion

       #region Methods

       public static DataTable GetDataFromLookupHeader(int companyID, SqlTransaction sqltrans = null)
       {
           DataTable dt = null;
       
           try
           {

               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
              
               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetDataFromLookupHeader, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataFromLookupHeader, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static DataTable GetDataForPayRollType(int companyID, SqlTransaction sqltrans = null)
       {
           DataTable dt = null;

           try
           {

               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);

               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetDataForPayRollType, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForPayRollType, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static int GetMyNewEmployeeEducationID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
              

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MyMaxId, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MyMaxId, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MyMaxId, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

       public static bool DeleteDataByEmployeeID(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           bool result;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               DataSet ds = null;

               //if (objTransaction != null)
               if (SqlHelper.ExecuteNonQuery( objTransaction, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams) > 0)
               { //objTransaction,
               result = true;
               }
               else {

                  
               result = false;}
               //else if (objConnection != null)
               //    ds = SqlHelper.ExecuteNonQuery(objConnection, CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);
               //else
               //    ds = SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByEmployeeID, sqlparams);

               //if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
               //    dr = ds.Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }


       public static DataTable GetDataByEmployeeIDtoBind(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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

               
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDToBind), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyEmployeeIDToBind), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyEmployeeIDToBind), sqlparams);

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

       public static DataTable GetDataByEmployeeID(int CompanyID, int PeriodID, int StatusCode, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   sb.Append(" and HRMS_EmployeeEducation.PersonID = " + PersonID);

               if (StatusCode > 0)
                   sb.Append(" and HRMS_EmployeeEducation.StatusCode = " + StatusCode);

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
                   sb.Append(" and HRMS_EmployeeEducation.PersonID = " + PersonID);
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
       #endregion
   }
}
