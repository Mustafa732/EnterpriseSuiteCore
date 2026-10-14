using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Text;
namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial class HRMS_LoanPaymentDetail
   {

       #region Constants

       const string Query_DeleteData_BySalaryHeaderID = @"
        Delete from HRMS_LoanPaymentDetail
        WHERE CompanyID = @CompanyID and SalaryHeaderID = @SalaryHeaderID 
        and (IsPosted = 0 or IsPosted is null) ";

        const string Query_GetDataFromLookupHeader = @"
        select 
         -1 as [LookupDetailID],
        'Select' as [EnumeratedDescription] 
union 
 
  SELECT [LookupDetail].[LookupDetailID],[LookupDetail].[EnumeratedDescription]
        FROM [LookupHeader]
        inner join [LookupDetail] on [LookupDetail].[LookupHeaderID] = [LookupHeader].[LookupID] and [LookupDetail].[IsActive]=1
        where LookupType='PaymentType'
        ";
        const string Query_GetDataForOtherType = @" 
       
         SELECT -1 as ID,
        'Select' as Value 
        
        UNION 
        select 1 as ID,
        'Allowances' as Value
       
        UNION 
        Select 3 as ID,
        'Contribution' as Value
";

       const string Query_GetDataByPersonID = @"

select '' as LoanType,
         -1 as LoanSanctionHeaderID,
         -1 as LoanSanctionID,
        'Select' as LoanSanctionDetailDetail 

union all
        select distinct  HRMS_PayrollType.[Description] as LoanType,
        HRMS_LoanSanctionHeader.LoanSanctionHeaderID,--HRMS_LoanSanctionDetail.LoanSanctionDetailID
        HRMS_LoanSanctionDetail.LoanSanctionDetailID as LoanSanctionID,
      convert(varchar(50),HRMS_LoanSanctionDetail.LoanSanctionDetailID)+'-'+ HRMS_PayrollType.[Description] as LoanSanctionDetailDetail

        from HRMS_Set_Employee

        inner join HRMS_LoanSanctionHeader on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
        and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_LoanSanctionHeader.status = 1

        inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
        and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
and HRMS_LoanSanctionDetail.status = 1
     --   left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
      --  and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID

        inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
        --and HRMS_PayrollType.CompanyID = HRMS_LoanSanctionDetail.CompanyID

        where HRMS_Set_Employee.PersonID = @PersonID 
        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
        and HRMS_LoanSanctionDetail.IsActive = 1
        and isnull(HRMS_LoanSanctionDetail.IsDeductionCompleted,0) = 0
        group by HRMS_PayrollType.[Description],
        HRMS_LoanSanctionHeader.LoanSanctionHeaderID,
        HRMS_LoanSanctionDetail.LoanSanctionDetailID,
        
        HRMS_LoanSanctionDetail.LoanSanctionDetailID,
        HRMS_LoanSanctionDetail.LoanAmount
        --HRMS_LoanPaymentDetail.LoanDisburstAmount


";

       const string Query_GetDataByPersonID2 = @"
select '' as LoanType,
         -1 as LoanSanctionHeaderID,
        'Select' as LoanSanctionDetailID 

union all
        select distinct  HRMS_PayrollType.[Description] as LoanType,
        HRMS_LoanSanctionHeader.LoanSanctionHeaderID,--HRMS_LoanSanctionDetail.LoanSanctionDetailID
      convert(varchar(50),HRMS_LoanSanctionDetail.LoanSanctionDetailID)+'-'+ HRMS_PayrollType.[Description] as LoanSanctionDetailID

        from HRMS_Set_Employee

        inner join HRMS_LoanSanctionHeader on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
        and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_LoanSanctionHeader.status = 1

        inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
        and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
and HRMS_LoanSanctionDetail.status = 1
        left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
        and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID

        inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
        and HRMS_PayrollType.CompanyID = HRMS_LoanSanctionDetail.CompanyID

        where HRMS_Set_Employee.PersonID = @PersonID 
        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
        and HRMS_LoanSanctionHeader.Status = 1
        group by HRMS_PayrollType.[Description],
        HRMS_LoanSanctionHeader.LoanSanctionHeaderID,
        HRMS_LoanSanctionDetail.LoanSanctionDetailID,
        HRMS_LoanSanctionDetail.LoanAmount,
        HRMS_LoanPaymentDetail.LoanDisburstAmount
        ";
       const string Query_GetDataForEmployeeLeavesAgainstPayrollTypeCode = @"

 Select 
        TAB.Description
        ,TAB.PayrollTypeID
        ,TAB.PersonID
        ,sum(TAB.leaveDays) as LeaveDays
       -- ,sum(TAB.Quantity) as Quantity
        ,cast( isnull(sum(TAB.leavedays),0)  - isnull( sum(TAB.Quantity),0) as int) as Quantity
        from 
        (
              select 
              HRMS_PayrollType.Description,
              HRMS_PayrollType.PayrollTypeID,
              HRMS_EmployeeAttendance.PersonID,
              HRMS_EmployeePayroll.leaveDays,
              sum(HRMS_EmployeeAttendance.Quantity) as Quantity
 
              from HRMS_PayrollType 

              left join HRMS_EmployeePayroll on
              HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
              HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
              HRMS_PayrollType.CompanyID = HRMS_EmployeePayroll.CompanyID and 
              HRMS_EmployeePayroll.PersonID = @PersonID and
              (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and 
              HRMS_EmployeePayroll.[Status] = 1 and HRMS_EmployeePayroll.IsActive = 1
  
              left join HRMS_EmployeeAttendance on
              HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
              HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID and
              HRMS_EmployeeAttendance.[Status] = 1 and
              HRMS_EmployeeAttendance.PersonID = @PersonID 
  
              where HRMS_PayrollType.payrolltypecode = 4 and 
              HRMS_PayrollType.[status] = 1 and HRMS_PayrollType.isActive = 1
              and HRMS_PayrollType.CompanyID = @CompanyID
              and HRMS_PayrollType.code = @PayrollTypeCode
              group by  HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeID,
               HRMS_EmployeeAttendance.PersonID,HRMS_EmployeePayroll.leaveDays
        
        ) as TAB
        Group By TAB.Description, TAB.PayrollTypeID, TAB.PersonID
        Order By TAB.PayrollTypeID
";

       const string Query_GetDataByLoanSanctionID = @"
select
HRMS_LoanSanctionHeader.LoanSanctionHeaderID
,
HRMS_SET_Employee.FullName
,HRMS_SET_Employee.PersonID
,HRMS_PayrollType.Description
,isnull(LoanAmount,0) as LoanAmount
,isnull(LoanAmount,0)-isnull(sum(LoanDisburstAmount),0) as PreviousBalance
,HRMS_PayrollType.[Description] as LoanType
,HRMS_LoanSanctionDetail.LoanSanctionDetailID
,HRMS_LoanPaymentDetail.LoanPaymentDetailID
 from HRMS_LoanSanctionHeader
inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
and  HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
and HRMS_SET_Employee.Status = 1  

inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanSanctionHeader.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanSanctionHeader.PeriodID
and HRMS_LoanSanctionDetail.Status =1 
 
 inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
 and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
 and HRMS_PayrollType.Status =1 
 
-- inner join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
---- and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
and HRMS_LoanPaymentDetail.Status =1 


where HRMS_LoanSanctionDetail.LoanSanctionHeaderID = @LoanSanctionHeaderID
        and HRMS_Set_Employee.PersonID = @PersonID
        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
        and HRMS_LoanSanctionDetail.PeriodID = @PeriodID
        and HRMS_LoanSanctionHeader.Status =1 
and HRMS_LoanSanctionDetail.LoanSanctionDetailID =@LoanSanctionDetailID
        

group by HRMS_SET_Employee.FullName
,HRMS_SET_Employee.PersonID
,HRMS_PayrollType.Description
,LoanAmount
,HRMS_PayrollType.[Description] 
,HRMS_LoanSanctionDetail.LoanSanctionDetailID
,HRMS_LoanPaymentDetail.LoanPaymentDetailID
,HRMS_LoanSanctionHeader.LoanSanctionHeaderID

";


       const string Query_getDatatoCloseLoan = @"
select
HRMS_LoanSanctionHeader.LoanSanctionHeaderID
,HRMS_PayrollType.PayrollTypeID
,HRMS_PayrollType.PayrollTypeCode
,HRMS_SET_Employee.FullName
,HRMS_SET_Employee.PersonID
,HRMS_PayrollType.Description
,isnull(LoanAmount,0) as LoanAmount
,isnull(LoanAmount,0)-isnull(sum(LoanDisburstAmount),0) as PreviousBalance
,HRMS_PayrollType.[Description] as LoanType
,HRMS_LoanSanctionDetail.LoanSanctionDetailID
,HRMS_LoanPaymentDetail.LoanPaymentDetailID
 from HRMS_LoanSanctionHeader
inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
and  HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
and HRMS_SET_Employee.Status = 1  

inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanSanctionHeader.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanSanctionHeader.PeriodID
and HRMS_LoanSanctionDetail.Status =1 
 
 inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
 and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
 and HRMS_PayrollType.Status =1 
 
-- inner join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
---- and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
and HRMS_LoanPaymentDetail.Status =1 


where HRMS_LoanSanctionDetail.LoanSanctionHeaderID = @LoanSanctionHeaderID
        and HRMS_Set_Employee.PersonID = @PersonID
        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
        and HRMS_LoanSanctionDetail.PeriodID = @PeriodID
        and HRMS_LoanSanctionHeader.Status =1 
and HRMS_LoanSanctionDetail.LoanSanctionDetailID =@LoanSanctionDetailID

        

group by HRMS_SET_Employee.FullName
,HRMS_SET_Employee.PersonID
,HRMS_PayrollType.Description
,LoanAmount
,HRMS_PayrollType.[Description] 
,HRMS_LoanSanctionDetail.LoanSanctionDetailID
,HRMS_LoanPaymentDetail.LoanPaymentDetailID
,HRMS_LoanSanctionHeader.LoanSanctionHeaderID
,HRMS_PayrollType.PayrollTypeID
,HRMS_PayrollType.PayrollTypeCode

";
//       const string Query_GetDataByLoanSanctionID = @"
//        select distinct  isnull(LoanAmount,0) as LoanAmount,isnull(LoanAmount,0)-isnull(sum(LoanDisburstAmount),0) as PreviousBalance,
//        HRMS_PayrollType.[Description] as LoanType,HRMS_LoanSanctionDetail.LoanSanctionDetailID,HRMS_Set_Employee.PersonID
//        from HRMS_Set_Employee
//
//        inner join HRMS_LoanSanctionHeader on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
//        and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
//
//        inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
//        and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
//        and HRMS_LoanSanctionHeader.PeriodID = HRMS_LoanSanctionDetail.PeriodID
//
//        left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
//        and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID
//        and HRMS_LoanPaymentDetail.PeriodID = HRMS_LoanSanctionDetail.PeriodID
//
//        inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
//        and HRMS_PayrollType.CompanyID = HRMS_LoanSanctionDetail.CompanyID
//
//        where HRMS_LoanSanctionDetail.LoanSanctionDetailID =@LoanSanctionDetailID
//        and HRMS_Set_Employee.PersonID = @PersonID
//        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
//        and HRMS_LoanSanctionDetail.PeriodID = @PeriodID
//        group by PreviousBalance,PaymentType,HRMS_PayrollType.[Description],
//        HRMS_LoanSanctionDetail.LoanSanctionDetailID
//        ,HRMS_LoanSanctionDetail.LoanAmount,HRMS_Set_Employee.PersonID
//
//        ";
       const string Query_Get_MyMaxId = @" SELECT IsNull(max(LoanPaymentDetailID), 0) + 1 as LoanPaymentDetailID FROM HRMS_LoanPaymentDetail WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

       const string Query_Update_BySanctionHeaderID = @" Update [HRMS_LoanPaymentDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [LoanPaymentDetailID] = @LoanPaymentDetailID and [PeriodID] = @PeriodID ";

       const string Query_DeleteDataByLoanPaymentDetailID = @" delete from HRMS_LoanPaymentDetail where LoanPaymentDetailID = @LoanPaymentDetailID and CompanyID =@CompanyID and PeriodID =@PeriodID";

       const string Query_GetDataForEmployeeAgainstPayrollTypeCode = @"
 select isnull(sum(HRMS_EmployeePayroll.CalculatedAmount),0) as CalculatedAmount,
 isnull(SUm(HRMS_EmployeePayroll.ManualAmount),0) as ManualAmount from  HRMS_Set_Employee
 inner join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PersonID = HRMS_Set_Employee.PersonID
 and HRMS_EmployeePayroll.CompanyID = HRMS_Set_Employee.CompanyID
 inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID
where  HRMS_EmployeePayroll.PayrollTypeCode = 1 and HRMS_PayrollType.PayrollTypeID = @PayrollTypeCode
and HRMS_EmployeePayroll.Status = 1
and HRMS_Set_Employee.Status = 1 
and HRMS_Set_Employee.CompanyID = @CompanyID
and HRMS_Set_Employee.PersonID = @PersonID
and HRMS_PayrollType.Status = 1

";

       const string Query_GetDatabyLoanSanctionHeaderID = @"
       select 
        HRMS_SET_Employee.PersonID,
        HRMS_Set_Employee.FullName,
        max( isnull( HRMS_LoanSanctionDetail.LoanAmount,0)) as LoanAmount,
        Sum(HRMS_LoanPaymentDetail.PreviousBalance) as PreviousBalance,
        sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) as LoanDisburstAmount,
        sum(HRMS_LoanPaymentDetail.CurrentBalance) as CurrentBalance
from HRMS_LoanPaymentDetail

inner join HRMS_LoanSanctionHeader on HRMS_LoanPaymentDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID
and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
and HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID

inner join HRMS_LoanSanctionDetail on  HRMS_LoanSanctionDetail.LoanSanctionDetailID =  HRMS_LoanPaymentDetail.LoanSanctionDetailID
and HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanPaymentDetail.PeriodID

Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
      And HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID

        where HRMS_Set_Employee.CompanyID = @CompanyID
        and HRMS_LoanPaymentDetail.PeriodID = @PeriodID
        and HRMS_LoanPaymentDetail.Status = 1
        and HRMS_LoanSanctionHeader.Status = 1     
        and HRMS_Set_Employee.Status = 1 
        {0}
group by 
HRMS_Set_Employee.FullName
,HRMS_SET_Employee.PersonID

        ";
       const string Query_GetDataForOtherGrid = @"
select HRMS_SalaryDetail.PayRollTypeCode as PayRollTypeID,HRMS_SalaryDetail.PayRollTypeCode,case HRMS_SalaryDetail.PayrollTypeCode when 1 then 'Allowances' when 2 then 'Contribution' end as  PayRollTypeName,HRMS_PayrollType.[Description],
isnull(sum(HRMS_EmployeePayroll.CalculatedAmount),0) as CalculatedAmount,
isnull(sum(HRMS_EmployeePayroll.ManualAmount),0) as ManualAmount,
isnull(sum(HRMS_SalaryDetail.Amount),0) as AdjustmentAmount,HRMS_PayrollType.Code as PayRollTypeCodeText,HRMS_EmployeeOpening.PersonID,'' as Action,
HRMS_SalaryDetail.SalaryDetailID,HRMS_SalaryHeader.SalaryHeaderID,HRMS_SET_Employee.FullName

from  HRMS_EmployeeOpening

inner join HRMS_SalaryHeader on HRMS_SalaryHeader.OpeningID = HRMS_EmployeeOpening.OpeningID and HRMS_SalaryHeader.[Status]= 1

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID and HRMS_SalaryDetail.[Status]=1

inner join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PersonID = HRMS_SalaryHeader.PersonID 
and HRMS_EmployeePayroll.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID and HRMS_EmployeePayroll.[Status] = 1
 
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and HRMS_PayrollType.[Status] = 1

 inner join  HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_EmployeePayroll.PersonID
 and HRMS_SET_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
 and HRMS_SET_Employee.Status = 1

where HRMS_EmployeeOpening.PersonID = @PersonID and HRMS_EmployeeOpening.[Status] = 1
and HRMS_EmployeeOpening.CompanyID = @CompanyID

group by HRMS_SalaryDetail.PayrollTypeID, HRMS_SalaryDetail.PayrollTypeCode,HRMS_PayrollType.[Description],HRMS_PayrollType.Code,HRMS_EmployeeOpening.PersonID
,HRMS_SalaryDetail.SalaryDetailID,HRMS_SalaryHeader.SalaryHeaderID,HRMS_SET_Employee.FullName
";
       const string Query_GetDataForLeaveGrid = @"
 Select 
        TAB.Description 
        ,TAB.PayrollTypeID as LeaveTypeID
        ,TAB.PersonID
        ,sum(TAB.leaveDays) as TotalLeaves
        ,sum(TAB.Quantity) as LeaveAdjustment
        ,cast( isnull(sum(TAB.leavedays),0)  - isnull( sum(TAB.Quantity),0) as int) as LeaveBalance
        ,TAB.Code as LeaveTypeText,'' as Action,TAB.EmployeeAttendanceID,TAB.FullName,TAB.OpeningID
        from 
        (
              select 
              HRMS_PayrollType.Description,
              HRMS_PayrollType.PayrollTypeID,
              HRMS_EmployeeAttendance.PersonID,
              HRMS_EmployeePayroll.leaveDays,HRMS_PayrollType.Code,
              sum(HRMS_EmployeeAttendance.Quantity) as Quantity
              ,HRMS_EmployeeAttendance.EmployeeAttendanceID
              ,HRMS_SET_Employee.FullName,HRMS_EmployeeAttendance.OpeningID
              from HRMS_PayrollType 

              left join HRMS_EmployeePayroll on
              HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
              HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
              HRMS_PayrollType.CompanyID = HRMS_EmployeePayroll.CompanyID and 
              HRMS_EmployeePayroll.PersonID = @PersonID and
              (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and 
              HRMS_EmployeePayroll.[Status] = 1 and HRMS_EmployeePayroll.IsActive = 1
              
              left join HRMS_EmployeeAttendance on
              HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
              HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID and
              HRMS_EmployeeAttendance.[Status] = 1 and
              HRMS_EmployeeAttendance.PersonID = @PersonID     
              
              inner join HRMS_EmployeeOpening on HRMS_EmployeeOpening.OpeningID = HRMS_EmployeeAttendance.OpeningID
              and HRMS_EmployeeOpening.CompanyID = HRMS_EmployeeAttendance.CompanyID

              inner join  HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_EmployeePayroll.PersonID
              and HRMS_SET_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
              and HRMS_SET_Employee.Status = 1

              where HRMS_PayrollType.payrolltypecode = 4 and 
              HRMS_PayrollType.[status] = 1 and HRMS_PayrollType.isActive = 1
              and HRMS_PayrollType.CompanyID = @CompanyID
             
              group by  HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeID,
               HRMS_EmployeeAttendance.PersonID,HRMS_EmployeePayroll.leaveDays,HRMS_PayrollType.Code,
                  HRMS_EmployeeAttendance.EmployeeAttendanceID,HRMS_SET_Employee.FullName,HRMS_EmployeeAttendance.OpeningID
        
        ) as TAB
        
        Group By TAB.Description, TAB.PayrollTypeID, TAB.PersonID,TAB.Code,TAB.EmployeeAttendanceID,TAB.FullName,TAB.OpeningID
";


       const string Query_GetDataForLoanGrid = @"

select 
		isnull(HRMS_LoanSanctionDetail.LoanAmount,0) as LoanAmount
		,HRMS_PayrollType.[Description] as LoanType    
        ,HRMS_LoanPaymentDetail.*
        ,'' as [Action],HRMS_EmployeeOpening.PersonID ,HRMS_SET_Employee.FullName       
        
 from HRMS_LoanPaymentDetail

left join HRMS_LoanSanctionHeader on  HRMS_LoanSanctionHeader.LoanSanctionHeaderID =  HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionHeader.CompanyID =  HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionHeader.PeriodID =  HRMS_LoanPaymentDetail.PeriodID

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
and HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID

left join HRMS_LoanSanctionDetail on  HRMS_LoanSanctionDetail.LoanSanctionDetailID =  HRMS_LoanPaymentDetail.LoanSanctionDetailID
and HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanPaymentDetail.PeriodID	
		
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
and HRMS_PayrollType.CompanyID =  HRMS_LoanSanctionDetail.CompanyID
and HRMS_PayrollType.Status =1 
		
inner join HRMS_EmployeeOpening on HRMS_EmployeeOpening.OpeningID =HRMS_LoanPaymentDetail.OpeningID
and HRMS_EmployeeOpening.CompanyID =HRMS_LoanPaymentDetail.CompanyID
and HRMS_EmployeeOpening.Status = 1
		
		where   HRMS_LoanPaymentDetail.CompanyID =  @CompanyID 
				and HRMS_LoanPaymentDetail.PeriodID =  @PeriodID
				{0}				
				and  HRMS_LoanPaymentDetail.Status =1  	
				and HRMS_SET_Employee.Status =1 

";
       const string Query_GetDatabyLoanSanctionHeaderIDOnSearch = @"select  distinct
        LoanPaymentDetailID,
		isnull(HRMS_LoanSanctionDetail.LoanAmount,0) as LoanAmount
		,HRMS_PayrollType.[Description] as LoanType
        ,HRMS_PayrollType.[Code] as LoanTypeID
        ,Set_Country.CountryName
        ,HRMS_Set_Employee.FullName
        ,HRMS_Set_Employee.PersonID
        ,HRMS_Set_Employee.FatherName
        ,HRMS_Set_Employee.BasicSalary
         ,Set_JobDesignation.JobDesignationID as DesignationID
        ,Set_JobDesignation.JobDesignationName as Designation
        ,SET_Department.DepartmentName as Department
        ,HRMS_Set_Employee.JoiningDate
        ,HRMS_Set_Employee.DateOfBirth
        ,HRMS_Set_Employee.Email
        ,HRMS_Set_Employee.AddressLine1
        ,Set_OperatingUnit.OperatingUnitName as Unit
        ,HRMS_Set_Shift.Description as Shift
        ,HRMS_Set_Shift.ShiftID
        ,Set_OperatingUnit.OperatingUnitID
        ,SET_Department.DepartmentID
        ,Set_JobDesignation.JobDesignationID
        ,set_Location.LocationID
        ,set_Location.Description as Location
        ,Set_Company.CompanyID
        ,HRMS_LoanPaymentDetail.*
        ,'' as [Action]
        
        
 from HRMS_LoanPaymentDetail

inner join HRMS_LoanSanctionHeader on  HRMS_LoanSanctionHeader.LoanSanctionHeaderID =  HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionHeader.CompanyID =  HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionHeader.PeriodID =  HRMS_LoanPaymentDetail.PeriodID

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
and HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID

inner join HRMS_LoanSanctionDetail on  HRMS_LoanSanctionDetail.LoanSanctionDetailID =  HRMS_LoanPaymentDetail.LoanSanctionDetailID
and HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanPaymentDetail.PeriodID

left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		        --AND Set_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
		        and Set_JobDesignation.status =1
		        
		        left join Set_Country on Set_Country.CountryID = HRMS_Set_Employee.FK_CountryID
		        
		        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
                and SET_Department.status =1
        
                left join Set_Company on  Set_Company.CompanyID = HRMs_SET_Employee.CompanyID
                and Set_Company.Status=1
        
		        left join Set_OperatingUnit on  Set_OperatingUnit.OperatingUnitID = Set_Company.OperatingUnitID
		        and Set_OperatingUnit.status = 1
        
         left join set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
                and set_Location.status = 1
		
		
		inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
		and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
		--and HRMS_PayrollType.CompanyID =  HRMS_LoanSanctionDetail.CompanyID
		
		left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_SET_Employee.PersonID 
		and HRMS_EmployeeShift.CompanyID =  HRMS_SET_Employee.CompanyID
		and HRMS_EmployeeShift.Status = 1
		
		left join HRMS_SET_Shift on HRMS_SET_Shift.ShiftID = HRMS_EmployeeShift.ShiftID 
		and HRMS_SET_Shift.ShiftCode = HRMS_EmployeeShift.ShiftCode  
		and HRMS_SET_Shift.Status = 1
		
		
		where HRMS_LoanPaymentDetail.CompanyID =  @CompanyID 
		and HRMS_LoanPaymentDetail.PeriodID =  @PeriodID
		--and HRMS_LoanSanctionHeader.PersonID = @PersonID
		and  HRMS_LoanPaymentDetail.Status =1  
		and HRMS_LoanSanctionHeader.Status =1 
		and HRMS_SET_Employee.Status =1 	 
		and HRMS_PayrollType.Status =1 
        {0}
        order by HRMS_PayrollType.[Code] asc
        ";
       #endregion

       #region Methods

       public static bool DeleteDataBySalaryHeaderID(SqlTransaction objTransaction, int companyID, int SalaryHeaderID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteData_BySalaryHeaderID, sqlparams) > 0)
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

       public static bool UpdateLoanSancton(SqlTransaction objTransaction, int LoanSanctionHeaderID, int LoanSanctionDetailID, int CompanyID)
       {
           bool result = false;
           const string query = @"update HRMS_LoanSanctionDetail set IsDeductionCompleted = 1  where LoanSanctionDetailID = @LoanSanctionDetailID
and LoanSanctionHeaderID = @LoanSanctionHeaderID and CompanyID = @CompanyID";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);
               sqlparams[1] = new SqlParameter("@LoanSanctionDetailID", LoanSanctionDetailID);
               sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, query, sqlparams) > 0)
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

       public static DataTable GetDataByLoanSanctionID(int companyID, int periodID, int LoanSanctionHeaderID, int LoanSanctionDetailID,int PersonID, SqlTransaction sqltrans = null)
       {
           DataTable dt = null;

           #region Query 

           string query = @"
select
		 HRMS_SET_Employee.FullName
		,HRMS_PayrollType.Description
		,HRMS_LoanSanctionDetail.LoanAmount
		,HRMS_LoanSanctionDetail.InstallmentAmount
		,(select sum(LoanDisburstAmount ) 
		from HRMS_LoanPaymentDetail where 
		HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionDetailID
		and HRMS_LoanPaymentDetail.Status =1 
        and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID
		 ) as DisburstAmount
		 , LoanAmount   - isnull((select sum(LoanDisburstAmount ) from HRMS_LoanPaymentDetail where 
		HRMS_LoanPaymentDetail.LoanSanctionHeaderID   = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and HRMS_LoanPaymentDetail.LoanSanctionDetailID   = HRMS_LoanSanctionDetail.LoanSanctionDetailID
		and HRMS_LoanPaymentDetail.Status =1 
        and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID
		 ),0) as PreviousBalance
		,HRMS_LoanSanctionHeader.LoanSanctionHeaderID
		,HRMS_PayrollType.PayrollTypeID
		,HRMS_PayrollType.PayrollTypeCode
		,HRMS_SET_Employee.FullName
		,HRMS_SET_Employee.PersonID
		,HRMS_PayrollType.Description LoanType
		,HRMS_LoanSanctionDetail.LoanSanctionDetailID

from HRMS_LoanSanctionHeader
	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_LoanSanctionHeader.PersonID
		and  HRMS_SET_Employee.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
		and HRMS_SET_Employee.Status = 1  

	inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID =  HRMS_LoanSanctionHeader.LoanSanctionHeaderID
		and HRMS_LoanSanctionDetail.CompanyID =  HRMS_LoanSanctionHeader.CompanyID
		and HRMS_LoanSanctionDetail.PeriodID =  HRMS_LoanSanctionHeader.PeriodID
		and HRMS_LoanSanctionDetail.Status =1 
 
	inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
		and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
		and HRMS_PayrollType.Status =1 

where HRMS_LoanSanctionDetail.LoanSanctionHeaderID = @LoanSanctionHeaderID
        and HRMS_Set_Employee.PersonID = @PersonID
        and HRMS_LoanSanctionDetail.CompanyID = @CompanyID
        and HRMS_LoanSanctionDetail.PeriodID = @PeriodID
        and HRMS_LoanSanctionHeader.Status =1 
and HRMS_LoanSanctionDetail.LoanSanctionDetailID =@LoanSanctionDetailID
 
";

           #endregion

           try
           {

               SqlParameter[] sqlparams = new SqlParameter[5];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);
               sqlparams[3] = new SqlParameter("@PersonID", PersonID);
               sqlparams[4] = new SqlParameter("@LoanSanctionDetailID", LoanSanctionDetailID);
               //LoanSanctionDetailID

               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
           //Query_GetDataByLoanSanctionID
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetDataToCloseLoan(int companyID, int periodID,int LoanSanctionHeaderID, int LoanSanctionDetailID, int PersonID, SqlTransaction sqltrans = null)
       {
           DataTable dt = null;

           try
           {

               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);
               sqlparams[3] = new SqlParameter("@PersonID", PersonID);
               sqlparams[4] = new SqlParameter("@LoanSanctionDetailID", LoanSanctionDetailID);
               //sqlparams[5] = new SqlParameter("@PayrollTypeID", payrolltypeid);
               //sqlparams[6] = new SqlParameter("@PayrollTypeCode", payrolltypecode);
               //LoanSanctionDetailID

               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_getDatatoCloseLoan, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_getDatatoCloseLoan, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }
       public static DataTable GetDataOfLoanDeduction(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, int LocationID = 0, int PayrollTypeID = 0)
       {
           DataTable dt = null;
           StringBuilder sb1 = new StringBuilder();
           const string Query_GetAllDataLoanDeduction = @"select distinct
	    HRMS_Set_Employee.FullName,HRMS_Set_Employee.EmployeeCode,
         isnull(sum(HRMS_LoanPaymentDetail.LoanDisburstAmount),0)
         as GrossAmount,'Recovered' AS TopHeading,20 AS HeaderSorting 
        ,HRMS_PayrollType.Description as LoanTypeName,HRMS_PayrollType.PayrollTypeCode
        FROM HRMS_Set_Employee 
		INNER JOIN HRMS_LoanSanctionHeader ON HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
		AND HRMS_LoanSanctionHeader.CompanyID = HRMS_Set_Employee.CompanyID 
		INNER JOIN HRMS_LoanSanctionDetail ON  HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID 
		AND HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
		AND HRMS_LoanSanctionDetail.PeriodID = HRMS_LoanSanctionHeader.PeriodID 
		AND HRMS_LoanSanctionDetail.status = 1
		left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
        and HRMS_LoanPaymentDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
        and HRMS_LoanPaymentDetail.Status = 1		
        and HRMS_LoanPaymentDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID
		INNER JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
        inner join HRMS_SalaryHeader on HRMS_SalaryHeader.SalaryHeaderID=HRMS_LoanPaymentDetail.SalaryHeaderID
       --inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
        INNER JOIN  set_Location on set_Location.LocationID = HRMS_Set_Employee.FK_LocationID
        and set_Location.status = 1
		--where (cast(HRMS_LoanSanctionDetail.CreateDate as date) <= @ToDate)
        --where (cast(HRMS_SalaryHeader.SalaryDate as date) <= @ToDate)	
        where (cast(HRMS_SalaryHeader.SalaryDate as date)= @ToDate)	
		AND HRMS_LoanSanctionDetail.CompanyID =@CompanyID
		AND HRMS_LoanSanctionDetail.PeriodID =@PeriodID
{0}
		Group by HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName,HRMS_PayrollType.PayrollTypeCode,HRMS_PayrollType.Description
";

           try
           {



               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@FromDate", Date_From);
               sqlparams[3] = new SqlParameter("@ToDate", Date_To);
               if (personIDFrom != null && personIDTo != null)
               {
                   sb1.Append(" and ( HRMS_Set_Employee.PersonID between " + personIDFrom + " and " + personIDTo + " )");
               }
               else if (personIDFrom != null)
               {
                   sb1.Append(" and ( HRMS_Set_Employee.PersonID = " + personIDFrom + " )");
               }
               else if (personIDTo != null)
               {
                   sb1.Append(" and ( HRMS_Set_Employee.PersonID  = " + personIDTo + " )");
               }
               if (EmployeeStatusID > 0)
               {
                   sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
               }
               else
               {
                   sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

               }
               if (EmployeeTypeID > 0)
               {
                   sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
               }
               if (PayrollTypeID > 0)
               {
                   sb1.Append(" and HRMS_PayrollType.PayrollTypeID = " + PayrollTypeID);
               }
               if (LocationID > 0)
               {
                   sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
               }
               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllDataLoanDeduction, sb1.ToString()), sqlparams);
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

       public static DataTable GetDataByPersonID(int companyID, int periodID, int PersonID, SqlTransaction sqltrans = null)
       {
           DataTable dt = null;
           try
           {

               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetDataByPersonID, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByPersonID, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }


       public static DataTable GetDataForOtherType( SqlTransaction sqltrans = null)
       {
           DataTable dt = null;

           try
           {

               SqlParameter[] sqlparams = new SqlParameter[0];
              // sqlparams[0] = new SqlParameter("@CompanyID", companyID);             

               if (sqltrans != null)
                   dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetDataForOtherType, sqlparams).Tables[0];
               else
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForOtherType, sqlparams).Tables[0];
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return dt;
       }

       public static int GetMyNewLoanPaymentDetailID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
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

       public static bool UpdateDataByLoanPaymentDetailID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int LoanPaymentDetailID, int periodID)
       {
           bool result = false;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               sqlparams[1] = new SqlParameter("@LoanPaymentDetailID", LoanPaymentDetailID);
               sqlparams[2] = new SqlParameter("@PeriodID", periodID);
               sqlparams[3] = new SqlParameter("@Status", false);
               sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_BySanctionHeaderID, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_BySanctionHeaderID, sqlparams) > 0)
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

       public static bool DeleteDataBySanctionHeaderID(int LoanPaymentDetailID, int CompanyID, int PeriodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           bool result;
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@LoanPaymentDetailID", LoanPaymentDetailID);
               sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

               //DataSet ds = null;

               //if (objTransaction != null)
               //    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByLoanPaymentDetailID, sqlparams);
               //else if (objConnection != null)
               //    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DeleteDataByLoanPaymentDetailID, sqlparams);
               //else
                   if(SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByLoanPaymentDetailID, sqlparams) > 0)
               {result = true;}
               else{
               result = false;}
             
           }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }

       public static DataTable GetDataByLoanSanctionHeaderID(int CompanyID, int PeriodID, int PersonID, int DesignationID, string CnicNo, string UserID , SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   sb.Append(" and HRMS_Set_Employee.PersonID = " + PersonID);
               if (DesignationID > 0)
                   sb.Append(" AND HRMS_SET_Employee.FK_JobDesignationID =" + DesignationID);
               if (CnicNo != "")
                   sb.Append(" AND HRMS_SET_Employee.CNICNumber like '%" + CnicNo + "%' ");
               if (UserID != null)
               {
                   sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
               }

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderID, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderID, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderID, sb.ToString()), sqlparams);

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


       public static DataTable GetDataofEmployeeAgainstPayrollTypeCode(int CompanyID,int periodid, int PersonID, int PayrollTypeCode, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
          
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodid);            
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);
               sqlparams[3] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);

               DataSet ds = null;

            
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataForEmployeeAgainstPayrollTypeCode, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForEmployeeAgainstPayrollTypeCode, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForEmployeeAgainstPayrollTypeCode, sqlparams);

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


       public static DataTable GetDataofEmployeeLeavesAgainstPayrollTypeCode(int CompanyID, int periodid, int PersonID, int PayrollTypeCode, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodid);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);
               sqlparams[3] = new SqlParameter("@PayrollTypeCode", PayrollTypeCode);

               DataSet ds = null;


               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataForEmployeeLeavesAgainstPayrollTypeCode, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForEmployeeLeavesAgainstPayrollTypeCode, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForEmployeeLeavesAgainstPayrollTypeCode, sqlparams);

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

       public static DataTable GetDataByLoanSanctionHeaderIDOnSearch(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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
                   sb.Append(" and HRMS_LoanSanctionHeader.PersonID = " + PersonID);
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderIDOnSearch, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderIDOnSearch, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderIDOnSearch, sb.ToString()), sqlparams);

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

       public static DataTable GetDocNoforLoanReciept(int CompanyID, int PeriodID, int PersonID = 0,string Query = "", SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dt = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query), sqlparams);

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


       public static DataTable GetDataForLoanPaymentGrid(int CompanyID, int PeriodID, int PersonID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
           
               DataSet ds = null;

               if (PersonID > 0)
                   sb.Append(" and HRMS_EmployeeOpening.PersonID = " + PersonID);
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataForLoanGrid, sb.ToString()), sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataForLoanGrid, sb.ToString()), sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataForLoanGrid, sb.ToString()), sqlparams);

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


       public static DataTable GetDataForLeaveGrid(int CompanyID,  int PersonID , SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

            
               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataForLeaveGrid, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForLeaveGrid, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForLeaveGrid, sqlparams);

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

       public static DataTable GetDataForOtherGrid(int CompanyID, int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;


               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataForOtherGrid, sqlparams);
               else if (objConnection != null)
                   ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForOtherGrid, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForOtherGrid, sqlparams);

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

       public static bool UpdatePaymentDetailIDRecieptGenerated(int CompanyID ,int periodID, int LoanPaymentID, SqlTransaction objTransaction = null)
       {
           string Query = @"update HRMS_LoanPaymentDetail set HRMS_LoanPaymentDetail.IsReceiptGenerated = 1 where 
CompanyID  = @CompanyID 
and PeriodID = @PeriodID   ";
           bool result = false;
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[2];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", periodID);

               StringBuilder sb = new StringBuilder();
               sb.Append(" and LoanPaymentDetailID = "+LoanPaymentID+"");

               if (objTransaction != null)
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query + sb), sqlparams) > 0)
                       result = true;
                   else
                       if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query + sb), sqlparams) > 0)
                           result = true;
                  }
           catch (Exception ex)
           {
               SqlHelper.LogException(ex);
               throw ex;
           }

           return result;
       }


       public static DataTable GetBalanceFromHeaderID(int CompanyID, int PeriodID, int SalaryHeaderID, int PayrollTypeID, string QueryForGetBalance, SqlTransaction objTransaction = null)
       {
        

           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@PayrollTypeID", PayrollTypeID);


               DataSet ds = null;


               if (objTransaction != null)
                   ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryForGetBalance, sqlparams);
               else
                   ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForGetBalance, sqlparams);

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


       public static bool DeleteDataByKeyBySalaryHeaderID(int CompanyID, int PeriodID, int SalaryHeaderID, Guid updatedBy, DateTime updateDate, string updatedByValue, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;

           const string Query_Delete_BySalaryHeader = @" Update [HRMS_LoanPaymentDetail] Set [Status] = @Status, [IsActive] = @IsActive, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
                    WHERE 
                        [CompanyID] = @CompanyID 
                    and [PeriodID] = @PeriodID 
                    and [PaymentType] like '%LoanRecovery(Salary)%'
                    AND HRMS_LoanPaymentDetail.SalaryHeaderID=@SalaryHeaderID
                     ";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[8];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@IsActive", false);
               sqlparams[4] = new SqlParameter("@Status", false);
               sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_BySalaryHeader, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_BySalaryHeader, sqlparams) > 0)
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

       public static bool DeleteDataByKeyBySalaryHeaderID2(int CompanyID, int PeriodID, int SalaryHeaderID, Guid updatedBy, DateTime updateDate, string updatedByValue, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;

           const string Query_Delete_BySalaryHeader = @" Update [HRMS_LoanPaymentDetail] Set [Status] = @Status, [IsActive] = @IsActive, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
                    WHERE 
                        [CompanyID] = @CompanyID 
                    and [PeriodID] = @PeriodID 
                    and [PaymentType] like '%LoanRecovery(Salary)%'
                    and  [SalaryHeaderID] =  @SalaryHeaderID
                     ";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[8];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
               sqlparams[3] = new SqlParameter("@IsActive", false);
               sqlparams[4] = new SqlParameter("@Status", false);
               sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_BySalaryHeader, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_BySalaryHeader, sqlparams) > 0)
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

       public static bool DeleteDataBySalHeaderID(int CompanyID, int PeriodID, int SalaryHeaderID, bool DeleteAll, Guid updatedBy, DateTime updateDate, string updatedByValue, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
       {
           bool result = false;
           StringBuilder sb = new StringBuilder();
           const string Query_Delete_BySalaryHeader_SalDel = @" Update [HRMS_LoanPaymentDetail] Set [Status] = @Status, [IsActive] = @IsActive, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
                    WHERE 
                        [CompanyID] = @CompanyID 
                    AND [PeriodID] = @PeriodID                    
                    AND [PaymentType] like '%LoanRecovery(Salary)%'
                    {0} ";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[7];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
               sqlparams[2] = new SqlParameter("@IsActive", false);
               sqlparams[3] = new SqlParameter("@Status", false);
               sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
               sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
               sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);
               //sqlparams[7] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);

               if (DeleteAll == false)
                   sb.Append(" AND SalaryHeaderID = " + SalaryHeaderID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Delete_BySalaryHeader_SalDel, sb.ToString()), sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_Delete_BySalaryHeader_SalDel, sb.ToString()), sqlparams) > 0)
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


       public static bool UpdateLoanSanctonOnSalaryDelete(SqlTransaction objTransaction, int LoanSanctionHeaderID, int LoanSanctionDetailID, int CompanyID)
       {
           bool result = false;
           const string query = @"update HRMS_LoanSanctionDetail set IsDeductionCompleted = 0  where LoanSanctionDetailID = @LoanSanctionDetailID
and LoanSanctionHeaderID = @LoanSanctionHeaderID and CompanyID = @CompanyID";

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);
               sqlparams[1] = new SqlParameter("@LoanSanctionDetailID", LoanSanctionDetailID);
               sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

               if (objTransaction == null)
               {
                   if (SqlHelper.ExecuteNonQuery(CommandType.Text, query, sqlparams) > 0)
                       result = true;
               }
               else
               {
                   if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, query, sqlparams) > 0)
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

       #endregion
     
      

   }
}
