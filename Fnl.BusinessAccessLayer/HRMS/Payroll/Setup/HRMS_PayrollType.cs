using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Setup
{
    public partial class HRMS_PayrollType:BaseClass
    {
        #region Constant Queries



        const string AllUom = @"
select * from dbo.SET_UnitofMeasure where Status = 1 

";
        const string Query_GetAllByPayrollTypeCode = @"
        SELECT * FROM [HRMS_PayrollType] WHERE [Status] = 1 
        AND [IsActive] = 1 
        AND [PayrollTypeCode] = @PayrollTypeCode
        ";

        const string Query_GetAllByPayrollTypeCodeDetail = @"
        SELECT ROW_NUMBER() OVER (ORDER BY PayrollTypeID) AS RowNumber, PayrollTypeID,Description,'' as bit  FROM [HRMS_PayrollType] WHERE [Status] = 1 
        AND [IsActive] = 1 
        --AND [CompanyID] = @CompanyID 
        AND [PayrollTypeCode] = @PayrollTypeCode
        ";

        const string Query_GetAllByMultiPayrollTypeCode = @"
        SELECT * FROM [HRMS_PayrollType] WHERE [Status] = 1 
        AND [IsActive] = 1 
        {0}

        ";

        const string Query_GetAllByMultiPayrollTypeCodeDetail = @"
        SELECT ROW_NUMBER() OVER (ORDER BY PayrollTypeID) AS RowNumber, PayrollTypeID,Description,'' as bit  FROM [HRMS_PayrollType] WHERE [Status] = 1 
        AND [IsActive] = 1 
        --AND [CompanyID] = @CompanyID 
        {0}

        ";

        const string Query_GetAllByPayRollTypeIDBYID = @"
        SELECT * FROM [HRMS_PayrollType] WHERE [Status] = 1 
        AND [IsActive] = 1 
        AND [CompanyID] = @CompanyID 
        AND [PayrollTypeID] = @PayrollTypeID
        {0}

        ";

        const string Query_GetAllByPayrollTypeCodePersonalIDDescription = @"
		select 
		PayrollTypeID,
		Code,
		Description
		from  HRMS_PayrollType 
		where PayrollTypeCode = @PayrollTypeCode and 
		( Description LIKE '%overtime%' or Description LIKE '%over time%') 
		group by PayrollTypeID, Code, Description";

        const string Query_GetAllByPayrollTypeCodePersonalID = @"
		select 
		HRMS_PayrollType.PayrollTypeID,
		HRMS_PayrollType.Code,
		HRMS_PayrollType.Description,
		sum(HRMS_EmployeePayroll.leaveDays) as leaveDays

		from HRMS_EmployeePayroll
		
		inner join HRMS_PayrollType on
		HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
		HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
		--HRMS_PayrollType.CompanyID = HRMS_EmployeePayroll.CompanyID and 
		HRMS_PayrollType.isActive = 1 and HRMS_PayrollType.[status] = 1 

		where HRMS_EmployeePayroll.CompanyID = @CompanyID and HRMS_EmployeePayroll.PayrollTypeCode = @PayrollTypeCode and 
		HRMS_EmployeePayroll.PersonID = @PersonalID and
		(HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0) and 
		HRMS_EmployeePayroll.[Status] = 1 and HRMS_EmployeePayroll.IsActive = 1
		
		group by HRMS_PayrollType.PayrollTypeID, HRMS_PayrollType.Code, HRMS_PayrollType.Description
        ";

        const string Query_GetAllByPayrollByDescription = @"
		select 
		PayrollTypeID,
		Code,
		Description
		from  HRMS_PayrollType 
		where 
--CompanyID = @CompanyID 

        {0}";
		
	


          const string Query_GetLoanTypeGradeWiseAndEmployeeWiseEntitled = @"

select 
tab.FullName
,tab.GradeName
,tab.PayrollTypeCode
,tab.PayrollTypeID
,tab.Code
,tab.[Description]
,tab.[Abbreviation]
,tab.[DefaultValue]
,tab.[Impact]
,tab.ChartOfAccountID
,tab.[GLCodeCombinationID]
,tab.[Formula]

,sum( tab.AmountFrom) as AmountFrom 

,sum( tab.AmountTo) as AmountTo
,isnull(max(tab.[Installment]),0) as [Installment]
from (
SELECT 
	   [HRMS_SET_Employee].[FullName]
	   ,SET_Grade.GradeName
	   ,[HRMS_GradeWiseLoanDetail].PayrollTypeID
	   ,[HRMS_GradeWiseLoanDetail].PayrollTypeCode
	  -- ,[HRMS_PayrollType].[Description]
	   ,[HRMS_PayrollType].[Code]
       ,[HRMS_PayrollType].[Description]
       ,[HRMS_PayrollType].[Abbreviation]
       ,[HRMS_PayrollType].[DefaultValue]
       ,[HRMS_PayrollType].[Impact]
       ,[HRMS_PayrollType].[ChartOfAccountID]
       ,[HRMS_PayrollType].[GLCodeCombinationID]
       ,[HRMS_PayrollType].[Formula]
      --,[HRMS_GradeWisePayroll].[GradeWisePayrollID]
      --,[HRMS_GradeWisePayroll].[GradeID]
      --,[HRMS_GradeWisePayroll].[PayrollTypeID]
      --,[HRMS_GradeWisePayroll].[PayrollTypeCode]
      ,[HRMS_GradeWiseLoanDetail].[AmountFrom]
      ,[HRMS_GradeWiseLoanDetail].[AmountTo]
      ,[HRMS_GradeWiseLoanDetail].[Installment]
      --,[HRMS_GradeWisePayroll].[EffectiveTo]
      
  FROM [HRMS_SET_Employee]
  
  INNER JOIN SET_Grade on
  [HRMS_SET_Employee].[FK_GradeID] = SET_Grade.[GradeID] and
 -- [HRMS_SET_Employee].[CompanyId] = SET_Grade.[CompanyId] and
  SET_Grade.[Status] = 1 and SET_Grade.[IsActive] = 1

  INNER JOIN [HRMS_GradeWiseLoanHeader] ON
  SET_Grade.GradeID = [HRMS_GradeWiseLoanHeader].[GradeID] AND 
--SET_Grade.CompanyID = [HRMS_GradeWiseLoanHeader].[CompanyID] AND
  [HRMS_GradeWiseLoanHeader].Status = 1 
  
INNER JOIN [HRMS_GradeWiseLoanDetail] ON
  [HRMS_GradeWiseLoanHeader].[GradeWiseLoanHeaderID] = [HRMS_GradeWiseLoanDetail].[GradeWiseLoanHeaderID] AND 
  [HRMS_GradeWiseLoanHeader].CompanyID = [HRMS_GradeWiseLoanDetail].[CompanyID] AND
  [HRMS_GradeWiseLoanHeader].PeriodID = [HRMS_GradeWiseLoanDetail].[PeriodID] AND
  [HRMS_GradeWiseLoanDetail].Status = 1 
  
INNER JOIN [HRMS_PayrollType] ON
  [HRMS_GradeWiseLoanDetail].[PayrollTypeID] = [HRMS_PayrollType].[PayrollTypeId] AND
  [HRMS_GradeWiseLoanDetail].[PayrollTypeCode] = [HRMS_PayrollType].[PayrollTypeCode] AND
--  [HRMS_GradeWiseLoanDetail].[CompanyID] = [HRMS_PayrollType].[CompanyID] AND
  [HRMS_PayrollType].[Status] = 1 AND [HRMS_PayrollType].[IsActive] = 1 
  
  
 WHERE
			[HRMS_SET_Employee].[Status] = 1 
		AND [HRMS_SET_Employee].[IsActive] = 1 
        AND [HRMS_SET_Employee].[CompanyID] =  @CompanyID 
        AND [HRMS_PayrollType].[PayrollTypeCode] = @PayrollTypeCode
		AND [HRMS_SET_Employee].[PersonID] = @PersonID
		

union all

SELECT 
	    [HRMS_Set_Employee].[FullName]
	   ,SET_Grade.GradeName
	   ,[HRMS_EmployeePayroll].PayrollTypeID
	   ,[HRMS_EmployeePayroll].PayrollTypeCode
	  -- ,[HRMS_PayrollType].[Description]
	   ,[HRMS_PayrollType].[Code]
       ,[HRMS_PayrollType].[Description]
       ,[HRMS_PayrollType].[Abbreviation]
       ,[HRMS_PayrollType].[DefaultValue]
       ,[HRMS_PayrollType].[Impact]
       ,[HRMS_PayrollType].[ChartOfAccountID]
       ,[HRMS_PayrollType].[GLCodeCombinationID]
       ,[HRMS_PayrollType].[Formula]
       ,[HRMS_EmployeePayroll].[CalculatedAmount] as AmountFrom
	   ,[HRMS_EmployeePayroll].[CalculatedAmount] as AmountTo
       ,0 as Installment
  FROM [HRMS_PayrollType]
  
  
INNER JOIN [HRMS_EmployeePayroll] ON
[HRMS_PayrollType].PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID] AND
[HRMS_PayrollType].PayrollTypeCode = [HRMS_EmployeePayroll].[PayrollTypeCode] AND
[HRMS_EmployeePayroll].[PersonID] = @PersonID and
[HRMS_PayrollType].Status = 1 and HRMS_EmployeePayroll.IsActive = 1

JOIN [HRMS_Set_Employee] ON
[HRMS_EmployeePayroll].[PersonID]  = [HRMS_Set_Employee].[PersonID] and
[HRMS_EmployeePayroll].[CompanyID]  = [HRMS_Set_Employee].[CompanyID] and
[HRMS_Set_Employee].[Status] = 1 and [HRMS_Set_Employee].IsActive = 1

  left JOIN SET_Grade on
  [HRMS_SET_Employee].[FK_GradeID] = SET_Grade.[GradeID] and
  --[HRMS_SET_Employee].[CompanyId] = SET_Grade.[CompanyId] and
  SET_Grade.[Status] = 1 and SET_Grade.[IsActive] = 1
  
 WHERE
			[HRMS_EmployeePayroll].[Status] = 1 
		AND [HRMS_EmployeePayroll].[IsActive] = 1 
        AND [HRMS_EmployeePayroll].[CompanyID] =  @CompanyID 
        AND [HRMS_PayrollType].[PayrollTypeCode] = @PayrollTypeCode
		AND [HRMS_EmployeePayroll].[PersonID] = @PersonID
		
		
		
		union all
		
		
		select [HRMS_SET_Employee].[FullName]
	   ,SET_Grade.GradeName
	   ,HRMS_PayrollType.PayrollTypeID
	   ,HRMS_PayrollType.PayrollTypeCode
	  -- ,[HRMS_PayrollType].[Description]
	   ,[HRMS_PayrollType].[Code]
       ,[HRMS_PayrollType].[Description]
       ,[HRMS_PayrollType].[Abbreviation]
       ,[HRMS_PayrollType].[DefaultValue]
       ,[HRMS_PayrollType].[Impact]
       ,[HRMS_PayrollType].[ChartOfAccountID]
       ,[HRMS_PayrollType].[GLCodeCombinationID]
       ,[HRMS_PayrollType].[Formula] 
	,  HRMS_LoanSanctionDetail.LoanAmount * -1 as AmountFrom
       ,  HRMS_LoanSanctionDetail.LoanAmount * -1 as AmountTo
       ,0 as InstallmentAmount
       
       from HRMS_LoanSanctionHeader

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
and HRMS_SET_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_SET_Employee.Status = 1


inner join HRMS_LoanSanctionDetail on  HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
and  HRMS_LoanSanctionDetail.PeriodID = HRMS_LoanSanctionHeader.PeriodID

and HRMS_LoanSanctionDetail.Status =1 



inner join HRMS_PayrollType on  HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
and HRMS_PayrollType.Status =1 

left JOIN SET_Grade on
  [HRMS_SET_Employee].[FK_GradeID] = SET_Grade.[GradeID] and
--[HRMS_SET_Employee].[CompanyId] = SET_Grade.[CompanyId] and
  SET_Grade.[Status] = 1 and SET_Grade.[IsActive] = 1
  
  
  where HRMS_SET_Employee.PersonID = @PersonID
  and HRMS_LoanSanctionHeader.CompanyID = @CompanyID 
  --and HRMS_LoanSanctionHeader.PeriodID = @PeriodID
AND [HRMS_PayrollType].[PayrollTypeCode] = @PayrollTypeCode
and HRMS_LoanSanctionHeader.Status =1 
		
) as tab		
		
		group by tab.FullName
,tab.GradeName
,tab.PayrollTypeCode
,tab.PayrollTypeID
,tab.Code
,tab.[Description]
,tab.[Abbreviation]
,tab.[DefaultValue]
,tab.[Impact]
,tab.ChartOfAccountID
,tab.[GLCodeCombinationID]
,tab.[Formula]

";

        const string Query_GetAllByPayrollTypeCodeAndPersonID = @"
SELECT 
	   [HRMS_SET_Employee].[FullName]
	   ,SET_Grade.GradeName
	   ,[HRMS_GradeWiseLoanDetail].PayrollTypeID
	   ,[HRMS_GradeWiseLoanDetail].PayrollTypeCode
	   ,[HRMS_PayrollType].[Description]
	   ,[HRMS_PayrollType].[Code]
       ,[HRMS_PayrollType].[Description]
       ,[HRMS_PayrollType].[Abbreviation]
       ,[HRMS_PayrollType].[DefaultValue]
       ,[HRMS_PayrollType].[Impact]
       ,[HRMS_PayrollType].[ChartOfAccountID]
       ,[HRMS_PayrollType].[GLCodeCombinationID]
       ,[HRMS_PayrollType].[Formula]
      --,[HRMS_GradeWisePayroll].[GradeWisePayrollID]
      --,[HRMS_GradeWisePayroll].[GradeID]
      --,[HRMS_GradeWisePayroll].[PayrollTypeID]
      --,[HRMS_GradeWisePayroll].[PayrollTypeCode]
      --,[HRMS_GradeWisePayroll].[AmountFrom]
      --,[HRMS_GradeWisePayroll].[AmountTo]
      --,[HRMS_GradeWisePayroll].[EffectiveFrom]
      --,[HRMS_GradeWisePayroll].[EffectiveTo]
      
  FROM [HRMS_SET_Employee]
  
  INNER JOIN SET_Grade on
  [HRMS_SET_Employee].[FK_GradeID] = SET_Grade.[GradeID] and
  [HRMS_SET_Employee].[CompanyId] = SET_Grade.[CompanyId] and
  SET_Grade.[Status] = 1 and SET_Grade.[IsActive] = 1

  INNER JOIN [HRMS_GradeWiseLoanHeader] ON
  SET_Grade.GradeID = [HRMS_GradeWiseLoanHeader].[GradeID] AND 
  SET_Grade.CompanyID = [HRMS_GradeWiseLoanHeader].[CompanyID] AND
  [HRMS_GradeWiseLoanHeader].Status = 1 
  
INNER JOIN [HRMS_GradeWiseLoanDetail] ON
  [HRMS_GradeWiseLoanHeader].[GradeWiseLoanHeaderID] = [HRMS_GradeWiseLoanDetail].[GradeWiseLoanHeaderID] AND 
  [HRMS_GradeWiseLoanHeader].CompanyID = [HRMS_GradeWiseLoanDetail].[CompanyID] AND
  [HRMS_GradeWiseLoanHeader].PeriodID = [HRMS_GradeWiseLoanDetail].[PeriodID] AND
  [HRMS_GradeWiseLoanDetail].Status = 1 
  
INNER JOIN [HRMS_PayrollType] ON
  [HRMS_GradeWiseLoanDetail].[PayrollTypeID] = [HRMS_PayrollType].[PayrollTypeId] AND
  [HRMS_GradeWiseLoanDetail].[PayrollTypeCode] = [HRMS_PayrollType].[PayrollTypeCode] AND
  [HRMS_GradeWiseLoanDetail].[CompanyID] = [HRMS_PayrollType].[CompanyID] AND
  [HRMS_PayrollType].[Status] = 1 AND [HRMS_PayrollType].[IsActive] = 1 
  
  
 WHERE
			[HRMS_SET_Employee].[Status] = 1 
		AND [HRMS_SET_Employee].[IsActive] = 1 
        AND [HRMS_SET_Employee].[CompanyID] =  @CompanyID 
        AND [HRMS_PayrollType].[PayrollTypeCode] = @PayrollTypeCode
		AND [HRMS_SET_Employee].[PersonID] = @PersonID
        ";
        

        const string Query_GetAllTypes = @"
       
        Select  '{AL:' + CAST(PayrollTypeID AS VARCHAR(150)) + '}' FormulaCode,Code,Description,Formula,PayRollTypeID,isnull(DefaultValue,0) as Value  from dbo.HRMS_PayrollType Where PayrollTypeCode = @AllowanceCode And HRMS_PayrollType.Status = 1 And HRMS_PayrollType.IsActive = 1
        Union
        Select  '{DE:' + CAST(PayrollTypeID AS VARCHAR(150)) + '}' FormulaCode,Code,Description,Formula,PayRollTypeID,isnull(DefaultValue,0) as Value from dbo.HRMS_PayrollType Where PayrollTypeCode = @DeductionCode And HRMS_PayrollType.Status = 1 And HRMS_PayrollType.IsActive = 1
        Union
           Select  '{CN:' + CAST(PayrollTypeID AS VARCHAR(150)) + '}' FormulaCode,Code,Description,Formula,PayRollTypeID,isnull(DefaultValue,0) as Value from dbo.HRMS_PayrollType Where PayrollTypeCode = @ContributionCode And HRMS_PayrollType.Status = 1 And HRMS_PayrollType.IsActive = 1
        ";

        const string Query_GetPayrollTypeIDByCode = @"
       SELECT PayrollTypeID from HRMS_PayrollType 
       WHERE [CompanyID] = @CompanyID 
        AND [PayrollTypeCode] = @PayrollTypeCode
        AND [IsIncomeTax] = 1
        ";

        const string Query_GetAllowances = @"
       SELECT * from HRMS_PayrollType 
       WHERE [CompanyID] = @CompanyID 
        AND GLImpact = 'Debit'
        and status = 1
        and Description <> 'Arrears'
        ";

        const string Query_IsIncomeTaxExist = @"
       SELECT Count(IsIncomeTax) from HRMS_PayrollType 
       WHERE [CompanyID] = @CompanyID 
       AND [IsIncomeTax] = 1 AND IsActive = 1 AND Status = 1 
        ";

        const string Query_UpdateIncomeTax = @" UPDATE [HRMS_PayrollType] SET [IsIncomeTax] = 0
		WHERE [CompanyID] = @CompanyID and [Status] = 1 and IsActive = 1 ";

        const string Query_GetAllPayrollTypeByCode = @"select 
case 
when HRMS_PayrollType.Impact = 1 then 'Positive'
when HRMS_PayrollType.Impact = 2 then 'Negative'
when HRMS_PayrollType.Impact = 3 then 'No Impact'
end as ImpactType
 ,HRMS_PayrollType.PayrollTypeCode
 ,HRMS_PayrollType.Code
 ,HRMS_PayrollType.Description
,HRMS_PayrollType.Abbreviation
,HRMS_PayrollType.DefaultValue
,HRMS_PayrollType.Impact
,HRMS_PayrollType.ChartOfAccountID
,HRMS_PayrollType.GLCodeCombinationID
,HRMS_PayrollType.Formula
,HRMS_PayrollType.IsIncomeTax
,HRMS_PayrollType.IsTaxable
,HRMS_PayrollType.IsPayable
,HRMS_PayrollType.IsLWP
,HRMS_PayrollType.IsEOBI
,HRMS_PayrollType.IsPF
,HRMS_PayrollType.Factor
 ,case when  HRMS_PayrollType.IsIncomeTax = 1 then 'Is IncomeTax' else 'No' end as IncomeTax
,case when  HRMS_PayrollType.IsTaxable = 1 then 'Is Taxable ' else 'No' end as Taxable
,case when  HRMS_PayrollType.IsPayable = 1 then 'Is Payable' else 'No' end as Payable
,case when  HRMS_PayrollType.IsLWP = 1 then 'Is LWP' else 'No' end as LWP
,case when  HRMS_PayrollType.IsEOBI = 1 then 'Is EOBI' else 'No' end as EOBI
,case when  HRMS_PayrollType.IsPF = 1 then 'Is PF' else 'No' end as PF
,HRMS_PayrollType.IsDailyWages
  from HRMS_PayrollType 
  
left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = HRMS_PayrollType.GLCodeCombinationID
--and GL_CodeCombinationDetail.CompanyID = HRMS_PayrollType.CompanyID 
and GL_CodeCombinationDetail.ChartOfAccountID = HRMS_PayrollType.ChartOfAccountID 
and GL_CodeCombinationDetail.Status =1 
where 
HRMS_PayrollType.Status =1 
and HRMS_PayrollType.IsActive =1 
--and HRMS_PayrollType.CompanyID = @CompanyID
      {0}
        ";

        const string Query_Get_EmployeeAnnualLeaveReport = @"

Select * from (

         Select Tab.CompanyID, Tab.FullName, Tab.PersonID, Tab.EmployeeCode, isnull(Sum(Tab.AnnualLeave),0) as AnnualLeave, 
	       isnull(SUM(Tab.BasicSalary),0) as BasicSalary, isnull(SUM(Tab.Cola),0) as Cola,
	       isnull(SUM(Tab.BasicSalary),0) + isnull(SUM(Tab.Cola),0) as Total, 
		  ( ( isnull(SUM(Tab.BasicSalary),0) + isnull(SUM(Tab.Cola),0) ) / 30 ) * isnull(Sum(Tab.AnnualLeave),0) as LeaveProvisionAmount
	      
  from (

		   ---- For Annual Leave column query 
 Select distinct HRMS_Set_Employee.CompanyID, HRMS_Set_Employee.FullName, HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode
 -- ,isnull((isnull(sum(HRMS_EmployeePayRoll.LeaveDays),0)- isnull (sum(DATEDIFF(dd,HRMS_LeaveRequest.FromDate,HRMS_LeaveRequest.ToDate) + 1),0)),0) as AnnualLeave	
	,(  Select 

	   isnull((isnull((HRMS_PR.LeaveDays),0)- isnull (sum(DATEDIFF(dd,HRMS_LR.FromDate,HRMS_LR.ToDate) + 1),0)),0)
	   from HRMS_EmployeePayRoll as HRMS_PR
	     Inner Join HRMS_LeaveRequest HRMS_LR on HRMS_PR.PersonID = HRMS_LR.Fk_PersonID 
	      And HRMS_PR.PayrollTypeID = HRMS_LR.FK_PayrollTypeID 
	      And HRMS_PR.PayrollTypeCode = HRMS_LR.FK_PayrollTypeCode 
	      And HRMS_PR.CompanyID = HRMS_LR.CompanyID  
	      And (HRMS_PR.IsOneTime is null or HRMS_PR.IsOneTime=0)  
		  And HRMS_PR.Status = 1 and HRMS_PR.IsActive = 1
          And HRMS_LR.IsApproved = 1
	      AND (HRMS_PR.FiscalID=@CurrentFiscalID OR @CurrentFiscalID is null)

   Inner Join Set_PeriodCustom on HRMS_LR.FromDate between Set_PeriodCustom.StartDate 
         And Set_PeriodCustom.EndDate And Set_PeriodCustom.Status = 1  And Set_PeriodCustom.FiscalID = @CurrentFiscalID

	   where HRMS_PR.Status = 1 and HRMS_PR.PersonID = HRMS_Set_Employee.PersonID
		   And HRMS_PR.PayrollTypeID = 33 and HRMS_LR.CompanyID = @CompanyID
		   And HRMS_LR.FK_PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID
		   And HRMS_PR.CompanyID = @CompanyID
	  Group by HRMS_PR.LeaveDays

	) as AnnualLeave

	--- For Basic Salary Column
	,(
		select isnull(sum(HRMS_PR.CalculatedAmount),0) + isnull(sum(HRMS_PR.ManualAmount),0) 
		   from HRMS_EmployeePayRoll HRMS_PR
		    INNER JOIN [HRMS_PayrollType] HRMS_PT on HRMS_PT.PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]
                    AND HRMS_PT.IsPayable=1
		   where HRMS_PR.PersonID  = HRMS_Set_Employee.PersonID
		     And HRMS_PR.CompanyID  = HRMS_Set_Employee.CompanyID
			 And HRMS_EmployeePayRoll.PayrollTypeID = HRMS_PR.PayrollTypeID
		     And HRMS_PR.PayrollTypeID = 2 
			 and HRMS_PR.Status = 1
			 And HRMS_PR.Status = 1 and HRMS_PR.IsActive = 1
			 And HRMS_PR.CompanyID = @CompanyID
    ) as BasicSalary

	--- For Cola Column
	,(
		select isnull(sum(HRMS_PR.CalculatedAmount),0) + isnull(sum(HRMS_PR.ManualAmount),0) 
		   from HRMS_EmployeePayRoll HRMS_PR
		     INNER JOIN [HRMS_PayrollType] HRMS_PT on HRMS_PT.PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]
                    AND HRMS_PT.IsPayable=1
		   where HRMS_PR.PersonID  = HRMS_Set_Employee.PersonID
		     And HRMS_PR.CompanyID  = HRMS_Set_Employee.CompanyID
			 And HRMS_EmployeePayRoll.PayrollTypeID = HRMS_PR.PayrollTypeID
		     And HRMS_PR.PayrollTypeID in (6,7,8,9,10)
			 And HRMS_PR.Status = 1 and HRMS_PR.IsActive = 1
			 And HRMS_PR.CompanyID = @CompanyID

    ) as Cola

    FROM HRMS_Set_Employee 
	Inner JOIN HRMS_EmployeePayRoll ON HRMS_EmployeePayRoll.PersonID = HRMS_Set_Employee.PersonID
		And HRMS_EmployeePayRoll.CompanyID = HRMS_Set_Employee.CompanyID
		And HRMS_EmployeePayRoll.Status = 1	
	Inner JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayRoll.PayrollTypeID	
	    And HRMS_PayrollType.Status = 1	
	left join HRMS_EmployeeAttendance on
	    HRMS_EmployeePayroll.PersonID = HRMS_EmployeeAttendance.PersonID and
	    HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and	 
	    HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID 
	    And HRMS_EmployeePayroll.Status = 1

   where HRMS_Set_Employee.CompanyID = @CompanyID 
		  And HRMS_Set_Employee.Status =1 and HRMS_Set_Employee.IsActive = 1
		 -- and HRMS_LeaveRequest.ToDate <= @ToDate
		  {0}
	group by HRMS_Set_Employee.CompanyID,HRMS_Set_Employee.FullName, HRMS_Set_Employee.PersonID,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeePayRoll.LeaveDays,
             HRMS_EmployeePayRoll.PayRollTypeID--, HRMS_LeaveRequest.FromDate, HRMS_LeaveRequest.ToDate,Set_PeriodCustom.EndDate

	) Tab
	Group by Tab.CompanyID, Tab.PersonID, Tab.EmployeeCode, Tab.FullName

   ) finalTab

	 where finalTab.AnnualLeave > 0

        ";

        #endregion

        #region Class Methods
        //AllUom
        public static DataTable GetAllUOM()
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {



                dt = SqlHelper.ExecuteDataset(CommandType.Text, AllUom).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllPayrollTypeByCode(int companyID, Nullable<int> payrollTypeCode)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
               
                if (payrollTypeCode != null)
                {

                    sb.Append(" AND [PayrollTypeCode] = " + payrollTypeCode + " ");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllPayrollTypeByCode, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllPayrollTypeByCodeOT(int companyID, Nullable<int> payrollTypeCode)
        {
            #region Query

           // const string QueryForPayrollOT = @"Select * from "; 
            #endregion
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (payrollTypeCode != null)
                {

                    sb.Append(" AND [Code] = " + payrollTypeCode + " ");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllPayrollTypeByCode, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetdatabyIDanddescription(string CodeID, string payrolltypename)
        {
            #region Query

            const string Query_GetAllPayrollTypeByCodeanddescription = @"
                     Select Formula,Description from HRMS_PayrollType 
                            where HRMS_PayrollType.Status = 1 
                       {0}
                     "; 
            #endregion
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (CodeID != null)
                {

                    sb.Append(" AND HRMS_PayrollType.Code = " + CodeID + " ");
                }
                if (payrolltypename != null && payrolltypename != "")
                {

                    sb.Append(" And HRMS_PayrollType.Description = '" + payrolltypename + "'");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllPayrollTypeByCodeanddescription, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public DataTable GetAllActivDetaileData(int PayrollTypeID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[2];
                SqlParam[0] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

                if (isActive.HasValue)
                    SqlParam[1] = new SqlParameter("@IsActive", isActive.Value);
                else
                    SqlParam[1] = new SqlParameter("@IsActive", DBNull.Value);

                string Query_SelectAll_By_Type = @" 

select HRMS_PayRollTypeDetail.[CompanyID],HRMS_PayRollTypeDetail.PayrollTypeID,
HRMS_PayRollTypeDetail.PayrollTypeDetailID,HRMS_PayRollTypeDetail.EmployeeStatusCode,
HRMS_PayRollTypeDetail.GLcodecombination,HRMS_PayRollTypeDetail.ExpenseTypeID,EMP_ExpenseType.ExpenseType,
 GL_CodeCombinationDetail.Description ,HRMS_Employement_Status.Description as LocationDescription
 from HRMS_PayRollTypeDetail
inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID=HRMS_PayRollTypeDetail.EmployeeStatusCode
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID=HRMS_PayRollTypeDetail.GLcodecombination
left Join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID=HRMS_PayRollTypeDetail.ExpenseTypeID
 Where (PayrollTypeID = @PayrollTypeID OR @PayrollTypeID is null)
 AND HRMS_PayRollTypeDetail.[Status] = 1;";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_By_Type, SqlParam).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }





        public static DataTable GetAllByPayrollTypeCode(int companyID, short payrollTypeCode,string Filter)
        {
            DataTable dt = null;
          
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);

                if (payrollTypeCode == 1)
                {
                    string Where = " AND [PayrollTypeCode] in (1,6)" + Filter;
                 
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMultiPayrollTypeCode, Where), sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCode + Filter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeCodeAdditionalPayment(int companyID, short payrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);

                if (payrollTypeCode == 1)
                {
                    string Where = " AND [PayrollTypeCode] in (1,6)";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMultiPayrollTypeCode, Where), sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCode , sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllByPayrollTypeCodeDetail(int companyID, short payrollTypeCode, string Filter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);

                if (payrollTypeCode == 1)
                {
                    string Where = " AND [PayrollTypeCode] in (1,6)" + Filter;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMultiPayrollTypeCodeDetail, Where), sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCodeDetail, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeID(int companyID, short payrollID, string Filter ,SqlTransaction sqltrans = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                if (sqltrans == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByMultiPayrollTypeCode , Filter), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetAllByMultiPayrollTypeCode, Filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeIDBYID(int companyID, int PayrollTypeID, string Filter, SqlTransaction sqltrans = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

                if (sqltrans == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByPayRollTypeIDBYID, Filter), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetAllByPayRollTypeIDBYID, Filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeCodePersonalID(int companyID, short payrollTypeCode, int PersonalID,string description)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonalID", PersonalID);
                if (description != null && description.Length>0)
                {
                    sqlparams[3] = new SqlParameter("@Description", description);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCodePersonalIDDescription, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCodePersonalID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllTypes(int companyID, int basicSalaryCode, int allowanceCode, int deductionCode, int contributionCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@BasicSalaryCode", basicSalaryCode);
                sqlparams[2] = new SqlParameter("@AllowanceCode", allowanceCode);
                sqlparams[3] = new SqlParameter("@DeductionCode", deductionCode);
                sqlparams[4] = new SqlParameter("@ContributionCode", contributionCode);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllTypes, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeCodeAndPersonID(int companyID, short payrollTypeCode, int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllByPayrollTypeCodeAndPersonID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetPayrollTypeIDForIncomeTax(int companyID, int payrollTypeCode, string code)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@Code", code);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetPayrollTypeIDByCode, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static bool IsIncomeTaxExist(int CompanyID, int PeriodID)
        {

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_IsIncomeTaxExist, sqlparams)) > 0)
                    return true;
               
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return false;
        }
      
        public bool UpdateIncomeTax(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];
                //sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIncomeTax, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateIncomeTax, sqlparams) > 0)
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

        public static DataTable GetLoanTypeGradeWiseAndEmployeeWiseEntitled(int companyID, short payrollTypeCode, int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLoanTypeGradeWiseAndEmployeeWiseEntitled, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLoanTypeagainstpersonid(int companyID, short payrollTypeCode, int PersonID, string Fillter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetLoanTypeagainstpersonid + Fillter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLoanTypeagainstpersonidandpayrolltypeid(int companyID, short payrollTypeCode, int PersonID , int payrolltypeid)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@PayrollTypeID", payrolltypeid);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetLoanTypeagainstpersonidandpayrolltypeid, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeByDescription(string description)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                if (description != null && description.Length > 0)
                {
                    sb.Append(" Description like '%" + description + "%' AND Status = 1");
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByPayrollByDescription,sb.ToString()), sqlparams).Tables[0];
                }
              
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllByPayrollTypeByDescriptionNew(string description)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            #region query
            const string Query_GetAllByPayrollByDescriptionNew = @"
		select PayrollTypeID, Code, Description from  HRMS_PayrollType 
		where 
        {0}";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                if (description != null && description.Length > 0)
                {
                    sb.Append(" Description like '%" + description + "%'");
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllByPayrollByDescriptionNew, sb.ToString()), sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


       // HRMS_PayrollType Detail
        public static bool InsertPayRollTypeDetail(int companyId, int PayrollTypeID, HRMS_PayrollType objPayrollType, List<HRMS_PayRollTypeDetail> ColAddTypeDet)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertTypeDetails");

            try
            {
                DataRow drType = HRMS_PayrollType.GetDataByKey( PayrollTypeID);

                if (drType != null)
                    objPayrollType.UpdateDataByKey(sqltrans);

                else
                    objPayrollType.Insert(sqltrans);

                HRMS_PayrollType obj_TypeDetail = new HRMS_PayrollType();
                    obj_TypeDetail.DeletePermanentByForeignKey(sqltrans, PayrollTypeID);

                foreach (HRMS_PayRollTypeDetail rtypeDetail in ColAddTypeDet)
                    rtypeDetail.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertTypeDetails");
            }

            return result;
        }

        internal bool DeletePermanentByForeignKey(SqlTransaction objTransaction, int PayrollTypeID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

                string Query_Delete_ByForeignKey = @" DELETE FROM HRMS_PayrollTypeDetail WHERE PayrollTypeID = @PayrollTypeID; ";

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByForeignKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete, Class Type Detail" + ex.Message);
            }

            return result;
        }


        internal static bool DeletePermanentByKey(SqlTransaction objTransaction, int PayrollTypeID)
        {
            bool result = false;

            try
            {
                SqlParameter[] SqlParam = new SqlParameter[1];
                SqlParam[0] = new SqlParameter("@PayrollTypeDetailID", PayrollTypeID);

                string Query_PermanentDelete_ByKey = @" DELETE FROM HRMS_PayRollTypeDetail WHERE PayrollTypeDetailID = @PayrollTypeDetailID; ";

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_PermanentDelete_ByKey, SqlParam) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw new BusinessLogicException("Error In Delete. Function: DeletePermanentByKey. " + ex.Message + ex.StackTrace);
            }
            return result;
        }


        public static bool UpdatePayrollTypeDetail(HRMS_PayrollType objTypeHeader, List<HRMS_PayRollTypeDetail> ColDeleteTypeDt, List<HRMS_PayRollTypeDetail> ColAddDetailDt)
        {
            short i = 0;
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Update_TypeDetail");

            try
            {
                if (!(objTypeHeader.UpdateDataByKey(sqltrans)))
                    throw new BusinessLogicException("Error in update Header: ");

                foreach (HRMS_PayRollTypeDetail CD in ColDeleteTypeDt)
                   
                        if (!(HRMS_PayrollType.DeletePermanentByKey(sqltrans, CD.PayrollTypeDetailID)))
                            throw new BusinessLogicException("Error in delete Detail: ");

//                 foreach (HRMS_PayRollTypeDetail BDt in ColAddDetailDt)
//                 {
//                     //i = short.Parse(HRMS_PayRollTypeDetail.GetNewPayrollTypeDetailID(objTypeHeader.PayrollTypeID).ToString());
//                     if (BDt.PayrollTypeDetailID == -1)
//                     {
//                         BDt.PayrollTypeID = objTypeHeader.PayrollTypeID;
//                         //BDt.PayrollTypeDetailID = short.Parse(HRMS_PayRollTypeDetail.GetNewPayrollTypeDetailID(objTypeHeader.PayrollTypeID).ToString());
//                         if (!(BDt.Insert(sqltrans)))
//                             throw new BusinessLogicException("Error in insert Details: ");
////                         i++;
//                     }
//                 }
                

                foreach (HRMS_PayRollTypeDetail BD in ColAddDetailDt)
                {
                    if (BD.PayrollTypeID == -1 ) 
                    {
                        BD.PayrollTypeID = objTypeHeader.PayrollTypeID;

                        if (!(BD.Insert(sqltrans)))
                            throw new BusinessLogicException("Error in insert Details: ");
                        
                    }
                    else if (!(BD.UpdateDataByKey(sqltrans)))
                        throw new BusinessLogicException("Error in update Details: ");
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
                    SqlHelper.RollBackTransaction(sqltrans, "Update_TypeDetail");
            }

            return result;
        }

        public static DataTable EOBIContributionReportReport(string companies, int CurrentCompanyId, Nullable<DateTime> AsOnDate = null, int? PayRollTypeCode = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyId);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);

                //companies = "(" + companies + ")";

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_EOBIContribution.Replace("@CompanyID", companies) + sb.ToString(), sqlparams);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_EOBIContribution + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable EOBIContributionDetailEmployeeCountReport(string CompanyID, int PeriodID, int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_EOBIContributionDetail_EmployeeCount.ToString().Replace("@CompanyID", CompanyID), sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable EOBIContributionDetailEmployeeAmountReport(string CompanyID, int PeriodID, int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_EOBIContributionDetail_EmployeeAmount.ToString().Replace("@CompanyID", CompanyID), sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable SocialContributionDetailEmployeeCountReport(string CompanyID, int PeriodID, int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SocialContributionDetail_EmployeeCount.ToString().Replace("@CompanyID", CompanyID), sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable MonthWiseEmployeeCostReport(int CompanyID, int PeriodID, int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_MonthWiseEmployeeCost, sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable SocialContributionDetailEmployeeAmountReport(string CompanyID, int PeriodID, int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SocialContributionDetail_EmployeeAmount.ToString().Replace("@CompanyID", CompanyID), sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable SocialSecurityContributionReport(string companies, int CurrentCompanyId, Nullable<DateTime> AsOnDate = null, int? PayRollTypeCode = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyId);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);
               // sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SocialSecurityContribution.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SocialSecurityContribution + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataSet EOBIContributionDetailReport(string companies, int CurrentCompanyId, Nullable<DateTime> AsOnDate = null, int? PayRollTypeCode = null)
        {
            DataSet dsReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyId);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_EOBIContributionDetail.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

               if (ds != null && ds.Tables.Count > 0)
                   dsReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsReturn;
        }

        public static DataSet PESSIContributionDetailReport(string companies, int CurrentCompanyId, Nullable<DateTime> AsOnDate = null, int? PayRollTypeCode = null)
        {
            DataSet dsReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyId);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_PESSIContributionDetail.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dsReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsReturn;
        }


        public static DataTable GetAllAllowances(int companyID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllowances.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetLoanAndAdvanceTypes(int companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                const string Query_GetLoanAndAdvanceTypes = @"select * from HRMS_PayrollType where PayrollTypeCode in (2,5) and (Description like '%loan%' or Description like  '%advance%' )";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLoanAndAdvanceTypes, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetOverTimePayableType(int companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                const string Query_GetOverTimePayableType = @"select * from HRMS_PayrollType where Description like '%OverTime%'";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOverTimePayableType, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllowanceForIncrementEmployee(int companyID = 1)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                const string Query_GetAllowanceForIncrement = @"Select * from HRMS_PayrollType Where Status = 1 and   (Description like '%Gross%Salary%' or  Description like '%Basic%Salary')";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllowanceForIncrement, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetLoanDetailByPersonID(int companyID, short payrollTypeCode, int PersonID, string NoofBonus, bool isEditmode = false, int bonusHeaderID = 0)
        {
            DataTable dt = null;
            StringBuilder Fillter = new StringBuilder();
            StringBuilder sbBalance = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@NoofBonus", NoofBonus);

                if (!isEditmode)
                    sbBalance.Append(" where Balance > 0 ");
                    
                if(isEditmode)
                    Fillter.Append(" and HRMS_BonusLoanDetail.bonusHeaderID = " + bonusHeaderID);
                string BonusLoanDetail = "";
                if (isEditmode)
                {
                    BonusLoanDetail = @"LEFT JOIN HRMS_BonusLoanDetail on HRMS_BonusLoanDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID
  AND HRMS_BonusLoanDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID
  AND HRMS_BonusLoanDetail.PeriodID = HRMS_LoanSanctionHeader.PeriodID
  AND HRMS_BonusLoanDetail.status = 1";
                }

                #region MyRegion
                const string Query_GetLoanDetailByPersonID = @"select PersonID,PayrollTypeID,LoanDescription,
case when (sum(InstallmentAmount) * @NoofBonus) > sum(Balance) then sum(Balance) else (sum(InstallmentAmount) * @NoofBonus) end as DeductionAmount,LoanSanctionHeaderID,LoanDate
,sum(Balance) as ActualLoanBalance
 from (
select HRMS_LoanSanctionDetail.LoanSanctionHeaderID,convert(date,HRMS_LoanSanctionDetail.loanStartDate) as LoanDate,
HRMS_PayrollType.PayrollTypeID,
HRMS_PayrollType.Description as LoanDescription,
	  HRMS_LoanSanctionDetail.InstallmentAmount,
	   (HRMS_LoanSanctionDetail.LoanAmount)  -	
		 (
		 (isnull((select sum(isnull(HRMS_LoanPaymentDetailinner.LoanDisburstAmount,0)) 
from HRMS_LoanPaymentDetail HRMS_LoanPaymentDetailinner
        inner join HRMS_LoanSanctionHeader on HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanPaymentDetailinner.LoanSanctionHeaderID
			and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanPaymentDetailinner.CompanyID
			and HRMS_LoanSanctionHeader.PeriodID = HRMS_LoanPaymentDetailinner.PeriodID
			and HRMS_LoanSanctionHeader.Status = 1
		  inner join HRMS_LoanSanctionDetail HRMS_LoanSanctionDetailinner on
		  HRMS_LoanSanctionDetailinner.LoanSanctionDetailID = HRMS_LoanPaymentDetailinner.LoanSanctionDetailID
		  and HRMS_LoanSanctionDetailinner.LoanSanctionHeaderID = HRMS_LoanPaymentDetailinner.LoanSanctionHeaderID
		  and HRMS_LoanSanctionDetailinner.CompanyID = HRMS_LoanPaymentDetailinner.CompanyID 
		  and HRMS_LoanPaymentDetailinner.CompanyID = @CompanyID
		  and HRMS_LoanSanctionDetailinner.Status = 1
		  and HRMS_LoanPaymentDetailinner.Status = 1
		  where HRMS_LoanSanctionDetailinner.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		  and HRMS_LoanSanctionDetailinner.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
        and HRMS_LoanSanctionHeader.PersonID = HRMS_LoanSanctionHeader.PersonID
		  ),0))
		 +
		 isnull( (
		  select sum(HRMS_BonusLoanDetail.LoanDeduction) 
		  from HRMS_BonusLoanDetail 
		  inner join HRMS_BonusDetail on HRMS_BonusDetail.BonusHeaderID = HRMS_BonusLoanDetail .BonusHeaderID
		  and HRMS_BonusDetail.BonusDetailID = HRMS_BonusLoanDetail .BonusDetailID
		  and HRMS_BonusDetail.status = 1
		  where HRMS_BonusLoanDetail.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
		  and HRMS_BonusLoanDetail.CompanyID = HRMS_LoanSanctionDetail.CompanyID
		  and HRMS_BonusDetail.EmployeeID = HRMS_LoanSanctionHeader.PersonID
and HRMS_BonusLoanDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		  and HRMS_BonusLoanDetail.status = 1
			),0) 

		 ) as Balance
,HRMS_LoanSanctionHeader.PersonID
       from HRMS_LoanSanctionHeader

inner join HRMS_LoanSanctionDetail on  HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
and  HRMS_LoanSanctionDetail.PeriodID = HRMS_LoanSanctionHeader.PeriodID
and HRMS_LoanSanctionDetail.Status =1 
And HRMS_LoanSanctionDetail.isDeductionCompleted = 0

inner join HRMS_PayrollType on  HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
and HRMS_PayrollType.Status =1 
  
{BonusLoanDetail}
  
where HRMS_LoanSanctionHeader.PersonID = @PersonID
and HRMS_LoanSanctionHeader.CompanyID = @CompanyID
AND [HRMS_PayrollType].[PayrollTypeCode] = @PayrollTypeCode
and isnull(HRMS_PayrollType.DeductionfromBonus,0) = 1
and HRMS_LoanSanctionHeader.Status =1
{0}
) as tab
{1}
--where Balance > 0

group by PersonID,PayrollTypeID,LoanDescription,LoanSanctionHeaderID,LoanDate ";
                #endregion


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetLoanDetailByPersonID.Replace("{BonusLoanDetail}", BonusLoanDetail), Fillter, sbBalance), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveDataForalnoorreport(Nullable<bool> isActive = true, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            #region query
            const string Query_SelectAllDatforreport = @" SELECT * FROM [HRMS_PayrollType] WHERE [Status] = 1 AND ([IsActive] = @IsActive OR @IsActive is null)  and PayrollTypeCode = 5";
            #endregion


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllDatforreport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllDatforreport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDatforreport, sqlparams);

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

        public static DataTable Get_EmployeeAnnualLeaveReport(int companyID, int periodID, int FiscalID, Nullable<DateTime> Date_From, Nullable<int> EmpStatusID, int personID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@ToDate", Date_From);

                #region filter parameters

                if (EmpStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (personID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonID = " + personID);
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SET_Employee].FK_DepartmentID = " + DepartmentID);
                }

                #endregion

                 dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_EmployeeAnnualLeaveReport, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllDataByPayrollTypeCode(int payrollTypeCode)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);

                #region Query
                const string Query_GetAllDataByPayrollTypeCode = @"
Select '' as bit, * from HRMS_PayrollType where
	  HRMS_PayrollType.Status = 1 AND [IsActive] = 1
	  and HRMS_PayrollType.PayrollTypeCode = @PayrollTypeCode
";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllDataByPayrollTypeCode, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllForOTReprt()
        {
            DataTable dt = null;

            try
            {
                string Query = @"Select * From HRMS_PayrollType where PayrollTypeID in (13,68,69,83)";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByName(string payrollTypeName, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            #region Query
            const string Query_GetData_byKName = @" SELECT * FROM [HRMS_PayrollType] WHERE [Description] = @Description ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Description", payrollTypeName);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKName, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKName, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKName, sqlparams);

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

        public static DataTable GetCalculatedAmountForOverTimeCalc(int CompanyID, int PeriodID, string Code, int PersonID)
        {
            DataTable dt = null;

            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@PersonID", PersonID);
            sqlparams[3] = new SqlParameter("@Code", Code);

            try
            {
                string Query = @"Select CASE WHEN ISNULL(CalculatedAmount,0) = 0 THEN ManualAmount ELSE CalculatedAmount END AS CalculatedAmount From HRMS_EmployeePayroll 
INNER JOIN  HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID 
AND  HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode 
AND  HRMS_PayrollType.Status = 1 AND  HRMS_PayrollType.IsActive = 1
where HRMS_PayrollType.PayrollTypeCode in (01) AND HRMS_PayrollType.Code = @Code
AND HRMS_EmployeePayroll.CompanyID = @CompanyID
AND HRMS_EmployeePayroll.PeriodID = @PeriodID
AND HRMS_EmployeePayroll.PersonID = @PersonID";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
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
