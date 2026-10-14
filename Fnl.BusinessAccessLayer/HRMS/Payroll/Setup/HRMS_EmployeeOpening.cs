using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;
using FnI.BusinessAccessLayer.HRMS.Payroll.Transaction;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
   public partial class HRMS_EmployeeOpening
   {
       #region Constants
       const string Query_EmployeeOpeningData = @"
select distinct
HRMS_EmployeeShift.PersonID
,HRMS_SET_Employee.FullName
,HRMS_SET_Employee.EmployeeCode
,HRMS_SET_Employee.FatherName
,HRMS_SET_Employee.JoiningDate
,HRMS_SET_Employee.DateOfBirth
,HRMS_SET_Employee.Email
,HRMS_SET_Employee.BasicSalary
,HRMS_SET_Employee.AddressLine1 +','+ HRMS_SET_Employee.AddressLine2 +','+ HRMS_SET_Employee.AddressLine3 as Address
,empdep.DepartmentName
,empJDes.JobDesignationName
,HRMS_SET_Shift.Description as Shift
,Set_location.LocationName
,Set_Business.BusinessName +'-'+ Set_OperatingUnit.OperatingUnitName as CompanyName
,Set_OperatingUnit.OperatingUnitName as Unit
,SET_Period.Description as Period
,HRMS_SalaryHeader.NetAmount as Sal_NetAmount
,HRMS_SalaryHeader.TaxAmount as Sal_TaxAmount
 from HRMS_SET_Employee

left join HRMS_EmployeeOpening on HRMS_SET_Employee.PersonID = HRMS_EmployeeOpening.PersonID
and  HRMS_SET_Employee.companyID = HRMS_EmployeeOpening.companyID 

 left join SET_Department empdep on empdep.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID 
 and empdep.Status =1 
 
 left join SET_JobDesignation empJDes on empJDes.JobDesignationID = HRMS_SET_Employee.FK_JobDesignationID
 and empJDes.Status =1 
 and HRMS_SET_Employee.Status =1 
 
 
 left join Set_Company on Set_Company.CompanyID = HRMS_SET_Employee.CompanyID
 and Set_Company.Status =1 
 
 left join Set_Business on Set_Business.BusinessID  =  Set_Company.BusinessID
 and Set_Business.Status =1 
 

 left join Set_OperatingUnit on Set_OperatingUnit.BusinessID  =  Set_Company.OperatingUnitID
 and Set_OperatingUnit.Status =1 
 
 left join HRMS_EmployeeShift  on HRMS_EmployeeShift.PersonID = HRMS_SET_Employee.PersonID
 and HRMS_EmployeeShift.Status = 1
 
 left join HRMS_SET_Shift on HRMS_SET_Shift.ShiftID  = HRMS_EmployeeShift.ShiftID
 and HRMS_SET_Shift.ShiftCode  = HRMS_EmployeeShift.ShiftCode
 and HRMS_SET_Shift.Status =1 
 
 left join SET_Location on  SET_Location.LocationID = HRMS_SET_Employee.FK_LocationID
 and SET_Location.Status =1 
 
 left join SET_Period on SET_Period.PeriodID =  HRMS_EmployeeOpening.PeriodID 
 and Set_period.Status =1 

left join HRMS_SalaryHeader on HRMS_SalaryHeader.OpeningID = HRMS_EmployeeOpening.OpeningID
and HRMS_SalaryHeader.CompanyID = HRMS_EmployeeShift.CompanyID
and HRMS_SalaryHeader.PeriodID = HRMS_EmployeeShift.PeriodID
and HRMS_SalaryHeader.PersonID = HRMS_EmployeeShift.PersonID
and HRMS_SalaryHeader.Status =1

where HRMS_EmployeeShift.CompanyID  =  @CompanyID
 and HRMS_EmployeeShift.PeriodID =  @PeriodID
  {0}


--------------------------Loan -------------------------

select 
HRMS_LoanSanctionHeader.PersonID as LoanPersonID
,HRMS_PayrollType.Description as LoanType
,HRMS_LoanPaymentDetail.PaymentType
,HRMS_LoanPaymentDetail.LoanPaymentDate
,HRMS_LoanPaymentDetail.LoanDisburstAmount
,HRMS_LoanPaymentDetail.PreviousBalance
,HRMS_LoanPaymentDetail.CurrentBalance
,HRMS_LoanSanctionDetail.LoanAmount
from  HRMS_LoanPaymentDetail

left join HRMS_EmployeeOpening on HRMS_LoanPaymentDetail.OpeningID = HRMS_EmployeeOpening.OpeningID
and HRMS_LoanPaymentDetail.CompanyID = HRMS_EmployeeOpening.CompanyID
and HRMS_LoanPaymentDetail.PeriodID = HRMS_EmployeeOpening.PeriodID
and HRMS_LoanPaymentDetail.Status =1

inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID  = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and  HRMS_LoanSanctionDetail.LoanSanctionDetailID  = HRMS_LoanPaymentDetail.LoanSanctionDetailID
and HRMS_LoanSanctionDetail.Status =1 

left join HRMS_LoanSanctionHeader on HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionHeader.Status = 1
--and HRMS_LoanSanctionHeader.PersonID  = HRMS_EmployeeOpening.PersonID

left join HRMS_PayrollType on  HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
and HRMS_PayrollType.Status = 1

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
and  HRMS_SET_Employee.companyID = HRMS_LoanSanctionHeader.companyID

where HRMS_LoanSanctionHeader.CompanyID  =  @CompanyID 
and HRMS_LoanSanctionHeader.PeriodID =  @PeriodID
   {0}
--------------------------Leaves -------------------------

select 

HRMS_EmployeeOpening.PersonID as LeavePersonID
,HRMS_PayrollType.Description as LeaveType
,HRMS_EmployeePayroll.LeaveDays 
,HRMS_EmployeeAttendance.Quantity as AdjustLeaves
--,HRMS_EmployeePayroll.LeaveDays - HRMS_EmployeeAttendance.Quantity as  BalanceLeaves
 from HRMS_EmployeeAttendance

left join HRMS_EmployeeOpening on HRMS_EmployeeAttendance.OpeningID =  HRMS_EmployeeOpening.OpeningID
and HRMS_EmployeeAttendance.PersonID =  HRMS_EmployeeOpening.PersonID
and HRMS_EmployeeAttendance.Status = 1

left  join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =   HRMS_EmployeeAttendance.LeaveID
and HRMS_PayrollType.PayrollTypeCode  = 4

left join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PersonID =  HRMS_EmployeeAttendance.PersonID
and HRMS_EmployeePayroll.PayrolltypeCode = HRMS_PayrollType.PayrollTypeCode
and HRMS_EmployeePayroll.PayrolltypeID = HRMS_PayrollType.PayrollTypeID

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_EmployeeAttendance.PersonID
and  HRMS_SET_Employee.companyID = HRMS_EmployeeAttendance.companyID 

where HRMS_EmployeeAttendance.CompanyID  =  @CompanyID 
and HRMS_EmployeeAttendance.PeriodID =  @PeriodID
and HRMS_PayrollType.Status =1 
and HRMS_EmployeePayroll.Status = 1
  {0}

--------------------------Others (Allowance / Deductions)-------------------------

select 
		HRMS_SalaryHeader.PersonID as OtherPersonID
		,HRMS_PayrollType.Description as OtherType
		,case  HRMS_PayrollType.PayrollTypeCode
				when 1 then 'Allowance'
				when 2 then 'Deduction'
				when 3 then 'Contributions'
				when 5 then 'Deduction'
		end as Others
		,HRMS_SalaryDetail.Amount OpenAmountAdjust
		,HRMS_EmployeePayroll.CalculatedAmount
		,HRMS_EmployeePayroll.ManualAmount
 from HRMS_SalaryHeader 

	left join HRMS_EmployeeOpening on  HRMS_SalaryHeader.OpeningID =  HRMS_EmployeeOpening.OpeningID
	and  HRMS_SalaryHeader.PersonID =  HRMS_EmployeeOpening.PersonID
	and HRMS_SalaryHeader.Status =1 

	inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
	and HRMS_SalaryDetail.Status =1 

	left join HRMS_EmployeePayroll on  HRMS_EmployeePayroll.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
	--and HRMS_EmployeePayroll.PayrollTypeCode in (1,3) 
	and HRMS_EmployeePayroll.PersonID   = HRMS_SalaryHeader.PersonID
	and HRMS_EmployeePayroll.Status =1 

	left join HRMS_PayrollType on  HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID
	and HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode
	and HRMS_PayrollType.Status =1 

	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_SalaryHeader.PersonID
and  HRMS_SET_Employee.companyID = HRMS_SalaryHeader.companyID 

where HRMS_SalaryHeader.CompanyID  =  @CompanyID 
and HRMS_SalaryHeader.PeriodID =  @PeriodID  
and HRMS_EmployeePayroll.Status = 1 
{0}

";

       const string Query_SelectbyPersonID = @"
select  HRMS_EmployeeOpening.PersonID
	    ,HRMS_SET_Employee.Fullname		
		,SET_Department.DepartmentName
	    ,SET_JobDesignation.JobDesignationName
		,BusinessName +' - '+ OperatingUnitNAme as CompanyName			
		,HRMS_EmployeeOpening.OpeningID
		,SET_Period.PeriodSetName as PeriodName
				
	     from  HRMS_EmployeeOpening
				
	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonId =  HRMS_EmployeeOpening.PersonID
		and HRMS_SET_Employee.CompanyID = HRMS_EmployeeOpening.CompanyID
		and HRMS_SET_Employee.Status =1 				
		
	left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
		and	SET_Department.status  = 1
		
	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		and	SET_JobDesignation.status  = 1	
		
	inner join SET_Company on SET_Company.CompanyID =  HRMS_SET_Employee.CompanyID
		and SET_Company.Status =1 
		
	inner join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID
		and SET_OperatingUnit.Status =1 
		
	inner join SET_Business on SET_Business.BusinessID = SET_Company.BusinessID
		and SET_Business.Status =1
		
	inner join SET_Period on SET_Period.PeriodID = HRMS_EmployeeOpening.PeriodID
	and SET_Period.Status = 1
		
	where
	 HRMS_EmployeeOpening.Status = 1
	 {0}

";

       const string Query_SelectbyPersonIDOnSearchEditing = @"
select 
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
,sum(HRMS_SalaryHeader.DeductionAmount) as PaidTax
,sum(HRMS_SalaryHeader.NetAmount) as PaidNetSalary
,HRMS_EmployeeOpening.CompanyID
,HRMS_EmployeeOpening.periodID
,SET_Business.BusinessID	
,HRMS_SalaryHeader.SalaryHeaderID	
,HRMS_EmployeeOpening.OpeningID		
	     from  HRMS_EmployeeOpening
				
	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonId =  HRMS_EmployeeOpening.PersonID
		and HRMS_SET_Employee.CompanyID = HRMS_EmployeeOpening.CompanyID
		and HRMS_SET_Employee.Status =1 				
		
	left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
		and	SET_Department.status  = 1
		
	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		and	SET_JobDesignation.status  = 1	
		
	left join SET_Company on SET_Company.CompanyID =  HRMS_SET_Employee.CompanyID
		and SET_Company.Status =1 
		
	left join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID
		and SET_OperatingUnit.Status =1 
		
	left join SET_Business on SET_Business.BusinessID = SET_Company.BusinessID
		and SET_Business.Status =1
		
		left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_Set_Employee.PersonID
        
           left join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_EmployeeShift.ShiftID
		  
	left join SET_Period on SET_Period.PeriodID = HRMS_EmployeeOpening.PeriodID
	and SET_Period.Status = 1

		 left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1

left join HRMS_SalaryHeader on HRMS_SalaryHeader.OpeningID = HRMS_EmployeeOpening.OpeningID
and HRMS_SalaryHeader.CompanyID = HRMS_EmployeeOpening.CompanyID
and HRMS_SalaryHeader.Status = 1

	where HRMS_EmployeeOpening.Status = 1

	 {0}

group by
HRMS_Set_Employee.FullName
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.BasicSalary,
Set_JobDesignation.JobDesignationName
,SET_Department.DepartmentName
,HRMS_Set_Employee.JoiningDate,
HRMS_Set_Employee.DateOfBirth,
HRMS_Set_Employee.Email
,HRMS_Set_Employee.AddressLine1,
Set_OperatingUnit.OperatingUnitName
,HRMS_Set_Shift.Description
,HRMS_Set_Shift.ShiftID
,Set_OperatingUnit.OperatingUnitID,
SET_Department.DepartmentID
,Set_JobDesignation.JobDesignationID
,set_Location.LocationID
,set_Location.Description
,Set_Company.CompanyID
,HRMS_EmployeeOpening.CompanyID
,HRMS_EmployeeOpening.periodID
,SET_Business.BusinessID
,HRMS_SalaryHeader.SalaryHeaderID
,HRMS_EmployeeOpening.OpeningID		
";

       const string Query_Get_NewMaxId = @" SELECT IsNull(max(OpeningID), 0) + 1 as OpeningID FROM HRMS_EmployeeOpening WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

       #endregion

       #region Methods

       public static DataSet GetEmployeeOpeningData(int companyid, int period, Nullable<int> PersonID)
       {
           DataSet ds = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];

               sqlparams[0] = new SqlParameter("@CompanyID", companyid);
               sqlparams[1] = new SqlParameter("@PeriodID", period);
               if (PersonID != null)
               {
                   sb.Append(" and HRMS_SET_Employee.PersonID =  " + PersonID + "  ");
               }

               ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeOpeningData, sb), sqlparams);


           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ds;
       }

       public static int GetMyNewOpeningID(int companyID,int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           int ID = 0;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);             

               if (objTransaction != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_NewMaxId, sqlparams));
               else if (objConnection != null)
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_NewMaxId, sqlparams));
               else
                   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_NewMaxId, sqlparams));
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return ID;
       }

       public static DataTable GetDataByPersonID(int PersonID)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeOpening.PersonID = " + PersonID);

               dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_SelectbyPersonID,sb.ToString()), sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetDataByPersonIDOnSearchEditing(int PersonID, int openingid)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[1];
               sqlparams[0] = new SqlParameter("@PersonID", PersonID);

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeOpening.PersonID = " + PersonID + " and HRMS_EmployeeOpening.OpeningID = " + openingid);

               dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectbyPersonIDOnSearchEditing, sb.ToString()), sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       #endregion

       #region update all grids of opening

       public bool UpdateLoanDetail(List<HRMS_LoanPaymentDetail> colAddStockTranDetailAdd,
       List<HRMS_LoanPaymentDetail> colUpdateStockTranDetailUpdate, List<HRMS_LoanPaymentDetail> colDeleteStockTranDetailDelete)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateOpeningLoanDetail");

           try
           {
               foreach (HRMS_LoanPaymentDetail std in colAddStockTranDetailAdd)
               {
                   std.LoanPaymentDetailID = HRMS_LoanPaymentDetail.GetMyNewLoanPaymentDetailID(std.CompanyID, std.PeriodID);
                   std.Insert(sqltrans);
               }
               foreach (HRMS_LoanPaymentDetail std in colUpdateStockTranDetailUpdate)
               {
                 
                   std.UpdateDataByKey(sqltrans);
               }
               foreach (HRMS_LoanPaymentDetail std in colDeleteStockTranDetailDelete)
               {
                   HRMS_LoanPaymentDetail.DeleteDataByKey(sqltrans, Guid.Parse(std.UpdatedBy.ToString()), DateTime.Parse(std.UpdateDate.ToString()), std.UpdatedByValue,
                       std.CompanyID,std.LoanPaymentDetailID,std.LoanSanctionDetailID,std.LoanSanctionHeaderID,std.PeriodID);
               }

               result = true;
           }
           catch (BusinessLogicException ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           finally
           {
               if (result)
                   SqlHelper.CommitTransaction(sqltrans);
               else
                   SqlHelper.RollBackTransaction(sqltrans, "UpdateOpeningLoanDetail");
           }

           return result;
       }

       
       public bool UpdateLeaveDetail(List<HRMS_EmployeeAttendance> colAddStockTranDetailAdd,
      List<HRMS_EmployeeAttendance> colUpdateStockTranDetailUpdate, List<HRMS_EmployeeAttendance> colDeleteStockTranDetailDelete)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateOpeningLeaveDetail");

           try
           {
               foreach (HRMS_EmployeeAttendance std in colAddStockTranDetailAdd)
               {
                   std.EmployeeAttendanceID = HRMS_EmployeeAttendance.GetNewEmployeeAttendanceID(std.CompanyID, std.PeriodID);
                   std.Insert(sqltrans);
               }
               foreach (HRMS_EmployeeAttendance std in colUpdateStockTranDetailUpdate)
               {
                 
                   std.UpdateDataByKey(sqltrans);
               }
               foreach (HRMS_EmployeeAttendance std in colDeleteStockTranDetailDelete)
               {
                   HRMS_EmployeeAttendance.DeleteDataByKey(sqltrans, Guid.Parse(std.UpdatedBy.ToString()), DateTime.Parse(std.UpdateDate.ToString()), std.UpdatedByValue,std.CompanyID,std.EmployeeAttendanceID ,std.PeriodID);
               }

               result = true;
           }
           catch (BusinessLogicException ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           finally
           {
               if (result)
                   SqlHelper.CommitTransaction(sqltrans);
               else
                   SqlHelper.RollBackTransaction(sqltrans, "UpdateOpeningLeaveDetail");
           }

           return result;
       }


       public bool UpdateOtherDetail(List<HRMS_SalaryDetail> colAddStockTranDetailAdd,
 List<HRMS_SalaryDetail> colUpdateStockTranDetailUpdate, List<HRMS_SalaryDetail> colDeleteStockTranDetailDelete)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateOtherDetail");

           try
           {
               foreach (HRMS_SalaryDetail std in colAddStockTranDetailAdd)
               {

                   std.SalaryDetailID = (short)HRMS_SalaryDetail.GetNewSalaryDetailID(std.CompanyID, std.PeriodID, std.SalaryHeaderID);                             
                   std.Insert(sqltrans);
               }
               foreach (HRMS_SalaryDetail std in colUpdateStockTranDetailUpdate)
               {

                   std.UpdateDataByKey(sqltrans);
               }
               foreach (HRMS_SalaryDetail std in colDeleteStockTranDetailDelete)
               {
                   HRMS_SalaryDetail.DeleteDataByKey(sqltrans, Guid.Parse(std.UpdatedBy.ToString()), DateTime.Parse(std.UpdateDate.ToString()), std.UpdatedByValue, std.CompanyID,std.PeriodID,std.SalaryDetailID,std.SalaryHeaderID);
               }

               result = true;
           }
           catch (BusinessLogicException ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }
           finally
           {
               if (result)
                   SqlHelper.CommitTransaction(sqltrans);
               else
                   SqlHelper.RollBackTransaction(sqltrans, "UpdateOtherDetail");
           }

           return result;
       }
       #endregion
   }
}
