using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;
using System.Text;


namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial  class HRMS_EmployeeStatus
   {
       #region Constant

       const string Query_EmployeeTravelReport = @"
select HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,
HRMS_EmployeeTravel.FromDate,HRMS_EmployeeTravel.ToDate,
HRMS_EmployeeTravel.Destination,HRMS_EmployeeTravel.Purpose,
HRMS_EmployeeTravel.IsCompanySponsor,
case HRMS_EmployeeTravel.VisaID when 1 then 'International' when 2 then 'Local' when 3 then 'Domestic' end as Visa,
case HRMS_EmployeeTravel.VisaTypeID when 1 then 'Education' when 2 then 'Labour' when 3 then 'Visit' 
when 4 then 'Medical' when 5 then 'Business'
end as VisaType
 from HRMS_EmployeeTravel
inner join HRMS_Set_Employee on HRMS_Set_Employee.personID = HRMS_EmployeeTravel.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeTravel.CompanyID
and HRMS_Set_Employee.Status = 1
where HRMS_EmployeeTravel.Status = 1
{0}
and HRMS_EmployeeTravel.CompanyID = @CompanyID

";
       const string Query_EmployeeSeperationReport = @"

  select
	    HRMS_EmployeeCheckList.Description as CheckListItem
	    ,HRMS_SeperationCheckList.ListID
	    ,HRMS_SET_Employee.PersonID	    
	    , HRMS_SET_Employee.EmployeeCode
		,HRMS_SET_Employee.Fullname
		,HRMS_SET_Employee.FatherName		
		,SET_Department.DepartmentName
		,HRMS_SET_Employee.DateofBirth
		,SET_JobDesignation.JobDesignationName
		,HRMS_SET_Employee.JoiningDate
		,HRMS_SET_Employee.Email
		,HRMS_SET_Employee.BasicSalary
		,HRMS_SET_EmployeeStatus.StatusName
		,HRMS_EmployeeStatus.Comments
	    ,HRMS_EmployeeStatus.StatusDate
	    from HRMS_EmployeeStatus
		
		left join HRMS_SET_EmployeeStatus on HRMS_SET_EmployeeStatus.StatusID = HRMS_EmployeeStatus.StatusID	
		and HRMS_SET_EmployeeStatus.Status = 1
		
		left join HRMS_SeperationCheckList on HRMS_SeperationCheckList.PersonID = HRMS_EmployeeStatus.PersonID 
			
       left  join HRMS_EmployeeCheckList on HRMS_EmployeeCheckList.ListID =  HRMS_SeperationCheckList.ListID
		and HRMS_EmployeeCheckList.Status =1 
		
		left join HRMS_SET_Employee on HRMS_SET_Employee.PersonId =  HRMS_EmployeeStatus.PersonID
		and HRMS_SET_Employee.CompanyID = HRMS_EmployeeStatus.CompanyID
		and HRMS_SET_Employee.Status =1 
		
		left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
		and	SET_Department.status  = 1
		
		left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
			and	SET_JobDesignation.status  = 1
		
     	where HRMS_EmployeeStatus.Status = 1     	
     	and HRMS_EmployeeStatus.CompanyID = @CompanyID
     	{0}

";

       const string Query_EmployeeStatusReport = @"
select 
	HRMS_SET_Employee.EmployeeCode
		,Fullname
		,FatherName
		,BasicSalary
		,DepartmentName
		,DateofBirth
		,JoiningDate
		,Email
		,BasicSalary
		,HRMS_SET_Employee.AddressLine1 +','+ HRMS_SET_Employee.AddressLine2 +','+ HRMS_SET_Employee.AddressLine3 as Address
		,SET_JobDesignation.JobDesignationName
		,LocationName
		,BusinessName +'-'+ OperatingUnitNAme as Company
		,OperatingUnitName   as Unit
		,StatusName
		,HRMS_EmployeeStatus.Comments
		,HRMS_EmployeeStatus.StatusDate
,HRMS_EmployeeStatus.PersonID
	 from  dbo.HRMS_EmployeeStatus
		
		inner join HRMS_SET_EmployeeStatus on HRMS_SET_EmployeeStatus.StatusID =  HRMS_EmployeeStatus.StatusID
		and HRMS_SET_EmployeeStatus.Status =1 
		
	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonId =  HRMS_EmployeeStatus.PersonID
		and HRMS_SET_Employee.CompanyID = HRMS_EmployeeStatus.CompanyID
		and HRMS_SET_Employee.Status =1 
		
		
		
	left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
		and		SET_Department.status  = 1
		
	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		and		SET_JobDesignation.status  = 1
		
	left join SET_Location on SET_Location.LocationID = HRMS_SET_Employee.FK_LocationID
		and SET_Location.Status =1 
		
	inner join SET_Company on SET_Company.CompanyID =  HRMS_SET_Employee.CompanyID
		and SET_Company.Status =1 
		
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID
		and SET_OperatingUnit.Status =1 
		
	inner join SET_Business on SET_Business.BusinessID = SET_Company.BusinessID
		and SET_Business.Status =1
		
	where
	 HRMS_EmployeeStatus.CompanyID  = @CompanyID
	 and
	 HRMS_EmployeeStatus.PeriodID  = @PeriodID

    {0}
";

       const string Query_DeleteDataByEmployeeID = @"
delete from HRMS_EmployeeStatus where PersonID = @PersonID and CompanyID =@CompanyID and PeriodID =@PeriodID

";
       const string Query_Delete_ByPersonID = @" Update [HRMS_EmployeeStatus] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";

       const string Query_GetDatabyEmployeeID = @"
select HRMS_SET_EmployeeStatus.StatusName,HRMS_Set_Employee.FullName,HRMS_EmployeeStatus.*,'' as [Action] from HRMS_EmployeeStatus
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeStatus.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeStatus.CompanyID
inner join HRMS_SET_EmployeeStatus on HRMS_SET_EmployeeStatus.StatusID = HRMS_EmployeeStatus.StatusID
where HRMS_EmployeeStatus.CompanyID = @CompanyID
and HRMS_EmployeeStatus.PeriodID = @PeriodID
and HRMS_EmployeeStatus.Status = 1
{0}

";
       const string Query_GetDatabyEmployeeIDOnSearch = @"


select distinct HRMS_SET_EmployeeStatus.StatusName,
HRMS_Set_Employee.FullName,HRMS_EmployeeStatus.*,'' as [Action],
 HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
HRMS_Set_Employee.BasicSalary,
Set_Designation.DesignationName as Designation,SET_Department.DepartmentName as Department,
HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.Email,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName as Unit,HRMS_Set_Shift.Description as Shift,
HRMS_Set_Shift.ShiftID,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID,Set_Designation.DesignationID,set_Location.LocationID,
set_Location.Description as Location,Set_Company.CompanyID
--,HRMS_EmployeeCheckList.Description,
--HRMS_EmployeeCheckList.ListID
--,HRMS_SeperationCheckList.IsChecked as 'Checked'
  from HRMS_EmployeeStatus

 inner join HRMS_SET_EmployeeStatus on HRMS_SET_EmployeeStatus.StatusID = HRMS_EmployeeStatus.StatusID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeStatus.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_EmployeeStatus.CompanyID

 left join HRMS_SeperationCheckList on HRMS_SeperationCheckList.EmployeeStatusID = HRMS_EmployeeStatus.EmployeeStatusID
and HRMS_SeperationCheckList.CompanyID = HRMS_EmployeeStatus.CompanyID
  
  left join HRMS_EmployeeCheckList on HRMS_EmployeeCheckList.ListID = HRMS_SeperationCheckList.ListID

left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
        and Set_Company.Status=1
        
		left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		and Set_OperatingUnit.status = 1
        
        left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftCode = HRMS_Set_Employee.PersonTypeCode
        
        left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

where HRMS_EmployeeStatus.CompanyID = @CompanyID
and HRMS_EmployeeStatus.PeriodID = @PeriodID
and HRMS_EmployeeStatus.Status = 1
and HRMS_Set_Employee.Status = 1
{0}
";

       #endregion
       //Query_EmployeeStatusReport
       #region Methods

       public static DataTable GET_EmployeeStatusReport(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {

               SqlParameter[] sqlparams = new SqlParameter[3];

               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

               if (PersonID != -1)
               {
                   sb.Append(" and HRMS_EmployeeStatus.PersonID = " + PersonID + " ");
               }
               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_EmployeeStatusReport, sb), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeeStatusReport, sb), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeStatusReport, sb), sqlparams);

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

       public static DataTable GET_EmployeeSeperationReport(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {

               SqlParameter[] sqlparams = new SqlParameter[1];

               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
             
               if (PersonID != -1)
               {
                   sb.Append(" and HRMS_EmployeeStatus.PersonID = " + PersonID + " ");
               }
               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_EmployeeSeperationReport, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeeSeperationReport, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSeperationReport, sb.ToString()), sqlparams);

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

       public static DataTable GET_EmployeeTravelReport(int PersonID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {

               SqlParameter[] sqlparams = new SqlParameter[1];

               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

               if (PersonID != -1)
               {
                   sb.Append(" and HRMS_EmployeeTravel.PersonID = " + PersonID + " ");
               }
               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_EmployeeTravelReport, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeeTravelReport, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeTravelReport, sb.ToString()), sqlparams);

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

       public static bool DeleteDataByPersonID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID,  int periodID, int personID)
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

       public static DataTable GetDataByEmployeeID(int CompanyID, int PeriodID, int PersonID , int DesignationID, string CnicNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   sb.Append(" and HRMS_EmployeeStatus.PersonID = " + PersonID);
               if (DesignationID > 0)
                   sb.Append(" AND HRMS_SET_Employee.FK_JobDesignationID =" + DesignationID);
               if (CnicNo != "")
                   sb.Append(" AND HRMS_SET_Employee.CNICNumber like '%" + CnicNo + "%' ");
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
                   sb.Append(" and HRMS_EmployeeStatus.PersonID = " + PersonID);
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
