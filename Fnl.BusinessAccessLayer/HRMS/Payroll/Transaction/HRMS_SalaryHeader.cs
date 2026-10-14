using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;
using Common.Utils;
using System.Collections.Generic;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.AR.Transaction;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_SalaryHeader : BaseClass
    {
        #region Constant Strings

        const string Query_Get_EmployeeDetailWithDeductionReport = @"
        ";
        const string Query_RoasterNotAssing = @"
Select * 
  from (
    select Distinct
         EP.PersonID, EP.EmployeeCode, EP.FullName,
         convert(date,(SET_DateTime.thedate)) as [Date] ,
         format(HRMS_AssignShiftsDetail.FromDate,'dd-MM-yyyy') as ShiftDate

from SET_DateTime

    Inner join HRMS_Set_Employee EP on (EP.CompanyID = @CompanyID OR @CompanyID is null)
          and EP.Status=1

    left join HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.PersonID = EP.PersonID

    left join HRMS_AssignShiftsDetail on --HRMS_AssignShiftsDetail.HeaderID = HRMS_AssignShiftsHeader.HeaderID and
             convert(date,HRMS_AssignShiftsDetail.FromDate) =  CONVERT(date,SET_DateTime.thedate)
           --or  convert(date,HRMS_AssignShiftsDetail.FromDate) = CONVERT(date,SET_DateTime.thedate)

   Inner join SET_Department on SET_Department.DepartmentID = EP.FK_DepartmentID	
   Inner join SET_Location  on SET_Location.LocationID =  EP.FK_LocationID

  where  EP.CompanyID = @CompanyID
	   And SET_DateTime.thedate between @FromDate and @ToDate    
	   And HRMS_AssignShiftsDetail.FromDate is null
	   {0}

	 )FinalTab

	 Order by  FinalTab.EmployeeCode asc
";
        const string Query_SelectAll_ForLoanAdjustmentPayment = @" SELECT * FROM [HRMS_SalaryHeader] WHERE [Status] = 1 AND ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null) AND [PersonID] = @PersonID";

        const string Query_DocumentTypeDirectInvoiceForALHAJ = @"
        SELECT DocumentTypeID,DocumentType
        FROM [SET_DocumentType] 
        where [DocumentType] = 'DI-Invoice (HR)' and [CompanyID]=@CompanyID and [PeriodID]=@PeriodID and 
        [MenuID] = (SELECT [MenuID] FROM [ApplicationMenu] where [MenuPath]='/AR/AR_Vehicle/Invoice_Transaction_frm.aspx' and [IsEnabled]=1)
        order by [DocumentType]
        ";

        const string Query_Post_By_PrimaryKey = @" update HRMS_SalaryHeader set PostID = @PostID ,IsApproved = 1 ,PostBy = @PostBy ,PostDate = @PostDate ,PostByValue = @PostByValue
        where SalaryHeaderID = @SalaryHeaderID and PersonID = @PersonID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_Approve_By_PrimaryKey = @" update HRMS_SalaryHeader set IsApproved = 1 
        where SalaryHeaderID = @SalaryHeaderID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_PrePost_By_PrimaryKey = @" update HRMS_SalaryHeader set PostID = @PostID ,IsApproved = 0 ,PostBy = @PostBy ,PostDate = @PostDate ,PostByValue = @PostByValue
        where SalaryHeaderID = @SalaryHeaderID and PersonID = @PersonID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

        const string Query_Get_MaxPostId = @"
        select isnull(max(PostID),0)+1 from  HRMS_SalaryHeader where CompanyID = @CompanyID and PeriodID = @PeriodID
        ";

        const string Query_SelectAll_WithoutPaging_Ckbx = @" SELECT convert(bit,'False') as IsChecked,[CompanyID], [PeriodID], [SalaryHeaderID], [PersonID], [SalaryDate], [SalaryType], [GrossAmount], [DeductionAmount], [NetAmount], [GrossAmountTaxable], [Status], [IsApproved], [CreatedBy], [CreateDate], [CreatedByValue], [UpdatedBy], [UpdateDate], [UpdatedByValue], [DaysWorking], [DaysAbsent], [DaysLWP], [DaysLate], [DaysEarly], [DaysTotal], [TaxAmount], [ContributionEmployer], [OpeningID], [PostID], [PostBy], [PostDate], [PostByValue], [PaymentID], [InvoiceID], [MasterSalary] FROM [HRMS_SalaryHeader] ";

        const string Query_GetData_byPersonID = @" SELECT * FROM [HRMS_SalaryHeader] WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PersonID] = @PersonID ";


        const string MonthRecord_Query = @"select CAST(SalaryDate AS DATE) AS SalaryDate,DATENAME(MM,SalaryDate)+' '+DATENAME(YY,SalaryDate)  as SalaryMonth,DATENAME(MM,SalaryDate) as MonthName,DATENAME(YY,SalaryDate)  as SalaryYear,DATEPART(MM,SalaryDate) as SalaryMonthDigit
          from HRMS_SalaryHeader SH
          where SH.PersonID=@PersonID AND SH.CompanyID=@CompanyID AND SH.PeriodID =@PeriodID AND SH.Status = 1
        group by SH.SalaryDate";

        const string Query_DataHeaderForPost = @"
        SELECT DISTINCT SH.CompanyID,SH.PeriodID,
        SH.SalaryHeaderID,SH.SalaryDate,
        EMP.EmployeeCode,EMP.FullName as EmployeeName,
        0 as GLAccountID, BPT.FK_CodeCombinationID as GLAccountCode, 'Debit' as GLImpact, 
        (Round(SH.GrossAmount,0)-round(SH.DeductionAmount,0)) as GLAmount, SH.DaysWorking
        --Round(SH.NetAmount,0) as GLAmount

        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        and EMP.CompanyID = SH.CompanyID
        INNER JOIN AR_SET_Customer ARC on ARC.CustomerID = EMP.FK_CustomerID  
        INNER JOIN AR_SET_BusinessPartnerTypes BPT on BPT.PeriodID = SH.PeriodID and BPT.TypeCode = 7
        and BPT.BusinessPartnerID = ARC.CustomerID
        --and BPT.CompanyID = SH.CompanyID 
        
        WHERE SH.[Status] = 1 AND (SH.[IsApproved] = 0 OR SH.[IsApproved] is null)  AND SH.[SalaryDate] = @SalaryDate
		--AND (EMP.IsActive = 1)  
        --and SH.NetAmount <> '0.0000'
        And (EMP.CompanyID = @CompanyID or @CompanyID is null)
		{0}        
        ";

        const string Query_DataHeaderForPrePost = @"
              
        ";

        const string Query_DataHeaderDetailForPost = @"
      SELECT SH.CompanyID,SH.PeriodID,SH.SalaryHeaderID,0 as SalaryDetailID,SH.SalaryDate,SH.SalaryType,
       EMP.PersonID,
        0 as PayrollTypeCode, 0 as PayrollTypeID,'True' as IsEmployeePart,
        0 as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        null as GLAccountCode,'Debit' as GLImpact, SH.[GrossAmount] as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        WHERE SH.[Status] = 1 AND (SH.[IsApproved] = 0 OR SH.[IsApproved] is null) AND SH.[SalaryDate] = @SalaryDate

UNION ALL    -- when header is null


		        SELECT distinct SH.CompanyID,SH.PeriodID,SH.SalaryHeaderID,SD.SalaryDetailID,SH.SalaryDate,SH.SalaryType,
        EMP.PersonID,
        PT.PayrollTypeCode,PT.PayrollTypeID,PT.IsEmployeePart,
        PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
		 --PT.GLCodeTwo as GLAccountCode,
         HRMS_PayRollTypeDetail.GLcodecombination as GLAccountCode,


        PT.GLImpact, SD.Amount as GLAmount,ARC.CustomerName,EMP.CostCenterID
		
        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_SalaryDetail] SD on SD.SalaryHeaderID = SH.SalaryHeaderID
        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=SD.PayrollTypeID and PT.PayrollTypeCode=SD.PayrollTypeCode
		and PT.glcodecombinationid is  null 
			inner join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID =pt.PayrollTypeID and emp.StatusID=HRMS_PayRollTypeDetail.EmployeeStatusCode	
        and 	HRMS_PayRollTypeDetail.Status = 1	
        WHERE SH.[Status] = 1 AND (SH.[IsApproved] = 0 OR SH.[IsApproved] is null) 
		AND SH.[SalaryDate] = @SalaryDate
       
	       UNION ALL    -- when header is not null

        SELECT SH.CompanyID,SH.PeriodID,SH.SalaryHeaderID,SD.SalaryDetailID,SH.SalaryDate,SH.SalaryType,
        EMP.PersonID,
        PT.PayrollTypeCode,PT.PayrollTypeID,PT.IsEmployeePart,
        PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
		 PT.glcodecombinationid as GLAccountCode,

        PT.GLImpact, SD.Amount as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_SalaryDetail] SD on SD.SalaryHeaderID = SH.SalaryHeaderID
        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=SD.PayrollTypeID and PT.PayrollTypeCode=SD.PayrollTypeCode
		and PT.glcodecombinationid is not  null 
        and pt.IsEmployeePart = 1
        WHERE SH.[Status] = 1 AND (SH.[IsApproved] = 0 OR SH.[IsApproved] is null) 
		AND SH.[SalaryDate] = @SalaryDate
        ";

        const string Query_DataHeaderDetailForPost_AlAbbas = @"SELECT SH.CompanyID,SH.PeriodID,SH.SalaryHeaderID,SD.SalaryDetailID,SH.SalaryDate,SH.SalaryType,
        EMP.PersonID,
        PT.PayrollTypeCode,PT.PayrollTypeID,PT.IsEmployeePart,
        PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        SD.Amount as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_SalaryDetail] SD on SD.SalaryHeaderID = SH.SalaryHeaderID
        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=SD.PayrollTypeID and PT.PayrollTypeCode=SD.PayrollTypeCode

        WHERE SH.[Status] = 1  AND SH.[SalaryDate] = @SalaryDate  
        {0}
        
        
        union all 
        SELECT SH.CompanyID,SH.PeriodID,SH.SalaryHeaderID,SD.SalaryDetailID,SH.SalaryDate,SH.SalaryType,
        EMP.PersonID,
        PT.PayrollTypeCode,PT.PayrollTypeID,PT.IsEmployeePart,
        PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeTwo,PT.GLImpactTwo,
        SD.Amount as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM [HRMS_SalaryHeader] SH

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=SH.PersonID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_SalaryDetail] SD on SD.SalaryHeaderID = SH.SalaryHeaderID
        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=SD.PayrollTypeID and PT.PayrollTypeCode=SD.PayrollTypeCode

        WHERE SH.[Status] = 1  AND SH.[SalaryDate] = @SalaryDate 
        {0}
        
        
        
        
        order by PayrollTypeCode ";


        const string Query_EmployeeYearlyLedger = @"

select 
	 Set_JobDesignation.JobDesignationName
	,SET_Section.SectionName
	,SET_Department.DepartmentName
	,HRMS_SET_Employee.FullName
	,HRMS_SET_Employee.FatherName
	,HRMS_SET_Employee.EmployeeCode
	,case 
			HRMS_SET_Employee.PersonTypeCode
			when 1 then 'Permanent'
			when 2 then 'Contract'
			when 3 then 'DailyWages'
			when 4 then 'Seasonal Contract'
			when 5 then 'Seasonal DailyWages'
			end 
			as 
			PersonType
	 ,HRMS_SalaryHeader.NetAmount 
	 , convert(varchar(3), datename(month, HRMS_SalaryHeader.SalaryDate))+'-'+ convert(varchar(10), datename(year, HRMS_SalaryHeader.SalaryDate) ) as MonthName
	 , datename(year, HRMS_SalaryHeader.SalaryDate) as Year
	 ,HRMS_SalaryHeader.SalaryDate
from HRMS_SalaryHeader
	inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and   HRMS_SET_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_SET_Employee.Status = 1

	left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID 
	and SET_Department.Status =1 
	left join SET_Section on SET_Section.SectionID =  HRMS_SET_Employee.FK_SectionID 
	and SET_Section.Status =1 
	left join Set_JobDesignation on Set_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
	and Set_JobDesignation.Status =1
	
	where 
	HRMS_SalaryHeader.CompanyID = @CompanyID 
	and 
	HRMS_SalaryHeader.PeriodID = @PeriodID 
	and datename(year, HRMS_SalaryHeader.SalaryDate) = @Year
";

        const string Query_PreiousIncomeTax = @"
 select isnull(sum(Amount),0) as TaxAmount  from HRMS_SalaryHeader
 inner join HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID =  HRMS_SalaryDetail.SalaryHeaderID
 and HRMS_SalaryHeader.CompanyID =  HRMS_SalaryDetail.CompanyID
 and HRMS_SalaryHeader.PeriodID =  HRMS_SalaryDetail.PeriodID
 and HRMS_SalaryHeader.Status = 1
 inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
 and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
 and HRMS_PayrollType.Status = 1
 and HRMS_PayrollType.Description like '%income%tax%'

   where HRMS_SalaryHeader.Status = 1
   and HRMS_SalaryHeader.CompanyID = @CompanyID
  and HRMS_SalaryHeader.PeriodID = @PeriodID
   and HRMS_SalaryHeader.PersonID = @PersonID
   and HRMS_SalaryHeader.SalaryDate BETWEEN @dtFrm and @dtTo

";

        const string Query_DepartmentWiseYearlyReport = @"
select 
		 max(datename(Year ,HRMS_SalaryHeader.SalaryDate) )as SalaryYear
		,max(datename(year, HRMS_Overtime.FromDate) ) as OverTimeYearFrom
		,max(datename(year, HRMS_Overtime.ToDate) ) as OverTimeYearTo
		,isnull( sum(HRMS_SalaryHeader.NetAmount),0) + isnull( sum(HRMS_Overtime.Salary),0) as TotalSalary
		,isnull( sum(HRMS_SalaryHeader.NetAmount),0) as TotalSalaryAmount
		,isnull(sum(HRMS_Overtime.Salary),0) as TotalOverTime
		,Max(HRMS_SET_Employee.FK_DepartmentID ) as DepartmentID 
		,SET_Department.DepartmentName
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 1 then isnull( sum(HRMS_SalaryHeader.NetAmount) ,0)
		else 0  end as Permanent
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 2 then isnull( sum(HRMS_SalaryHeader.NetAmount) ,0)
		else 0  end as Contract
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 3 then isnull( sum(HRMS_SalaryHeader.NetAmount) ,0)
		else 0  end as DailyWages
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 4 then isnull(sum(HRMS_SalaryHeader.NetAmount) ,0)
		else 0  end as SeasonalContract
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 5 then isnull( sum(HRMS_SalaryHeader.NetAmount) ,0)
		else 0  end as SeasonalDailyWages
				
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 1 then isnull( sum(HRMS_Overtime.OverTimeAmount) ,0)
		else 0  end as OvertimePermanent
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 2 then isnull( sum(HRMS_Overtime.OverTimeAmount) ,0)
		else 0  end as OvertimeContract
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 3 then isnull( sum(HRMS_Overtime.OverTimeAmount) ,0)
		else 0  end as OvertimeDailyWages
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 4 then isnull( sum(HRMS_Overtime.OverTimeAmount) ,0)
		else 0  end as OvertimeSeasonalContract
	,case
		HRMS_SET_Employee.PersonTypeCode 
		when 5 then isnull( sum(HRMS_Overtime.OverTimeAmount) ,0)
		else 0  end as OvertimeSeasonalDailyWages
	
		
		
		
 from HRMS_SalaryHeader
 
 inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_SalaryHeader.PersonID
		 and HRMS_SET_Employee.status =1
		 and HRMS_SET_Employee.companyid = HRMS_SalaryHeader.CompanyID
 inner join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID 
		 and SET_Department.Status =1 
		 
		 left join HRMS_Overtime on HRMS_Overtime.PersonID = HRMS_SET_Employee.PersonID

where 
		HRMS_SalaryHeader.CompanyID = @CompanyID
		and HRMS_SalaryHeader.PeriodID =  @PeriodID
		
		and (datename(year, HRMS_SalaryHeader.SalaryDate) = @Year
		or datename(year, HRMS_SalaryHeader.SalaryDate) is null)
		
		and (datename(year, HRMS_Overtime.FromDate) = @Year 
		or datename(year, HRMS_Overtime.FromDate) is null )
		
		and (datename(year, HRMS_Overtime.ToDate) = @Year 
		or datename(year, HRMS_Overtime.ToDate) is null )
		
 group by 
		HRMS_SET_Employee.PersonTypeCode 
		 ,SET_Department.DepartmentName
		

";
        const string Query_RestrictionOnSalaryDate = @"

select
 datepart( month , SalaryDate) as Month 
,datepart( year , SalaryDate) as Year
 from [HRMS_SalaryHeader]
 where  datepart( month , SalaryDate)   = @Month
 and  datepart( year , SalaryDate) = @Year
and CompanyID = @CompanyID
 and PeriodID = @PeriodID
";

        const string Query_EmployeePFReport = @"

        
";

        const string Query_EmployeeSESIReport = @"
Select Distinct  HRMS_Set_Employee.FullName,
        HRMS_Set_Employee.FatherName,
        HRMS_Set_Employee.EmployeeCode,
        HRMS_Set_Employee.PersonID,
        Set_Grade.GradeName,
        isnull((select (sum(isnull(HRMS_SalaryDetailinenr.Amount,0))) from  HRMS_SalaryDetail HRMS_SalaryDetailinenr
where 
 HRMS_SalaryDetailinenr.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetailinenr.Status = 1
 and HRMS_SalaryDetailinenr.PayrollTypeCode = 1),0)
		 as GrossAmount,
        HRMS_SalaryHeader.NetAmount as NetAmount,
        Set_Designation.DesignationName,
        HRMS_PayrollType.Code,
        HRMS_PayrollType.Description as DedType,
        HRMS_Set_Employee.SocialSecNo,
        MAX(HRMS_SalaryDetail.CreateDate),
        isnull(HRMS_PayrollType.DefaultValue,0) as DefaultValue,
        Round(HRMS_SalaryDetail.Amount,0) as ManualAmount,HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365 AS Years,
        SET_Department.DepartmentName AS Department

FROM HRMS_Set_Employee 

		inner join HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
		AND HRMS_SalaryHeader.Status=1
		
		inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
		and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
		and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
		And HRMS_SalaryDetail.PayrollTypeID in (select PayrollTypeID  from HRMS_PayrollType where  Description like '%Social%Security%' and status = 1)

		inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
		--and HRMS_PayrollType.CompanyID = HRMS_SalaryDetail.CompanyID		
		
        inner join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PayrollTypeID = HRMS_PayrollType.PayrollTypeID
		--and HRMS_PayrollType.CompanyID=HRMS_EmployeePayroll.CompanyID 
		And HRMS_EmployeePayroll.Status=1

		left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		--AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        Left join Set_Grade on Set_Grade.GradeID = HRMS_SET_Employee.FK_GradeID
        --and Set_Grade.CompanyID = HRMS_SET_Employee.CompanyID
        and Set_Grade.status =1        
        
        where 
		HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
        and HRMS_SalaryDetail.CompanyID =@CompanyID
        and HRMS_SalaryDetail.PeriodID = @PeriodID
        and HRMS_PayrollType.Description like '%Social%Security%'
		And HRMS_SalaryDetail.Amount > 0
        and isnull(HRMS_Set_Employee.IsSSMember,0) = 1
        {0}
        group by HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.PersonID,
        HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
        Set_Grade.GradeName,Set_Designation.DesignationName,HRMS_PayrollType.Description,
        HRMS_PayrollType.Code,HRMS_Set_Employee.SocialSecNo,HRMS_PayrollType.DefaultValue,
		HRMS_Set_Employee.BasicSalary,HRMS_SalaryHeader.NetAmount,HRMS_SalaryDetail.Amount,HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365,SET_Department.DepartmentName,HRMS_SalaryHeader.SalaryHeaderID";

        const string Query_EmployeeSESIReportforDKSM = @"
Select Distinct  HRMS_Set_Employee.FullName,
        HRMS_Set_Employee.FatherName,
        HRMS_Set_Employee.EmployeeCode,
        HRMS_Set_Employee.PersonID,
        Set_Grade.GradeName,
        (((isnull(HRMS_EmployeePayroll.ManualAmount,0) * 100)/6) / (DATEDIFF(day, @FromDate, @ToDate) + 1) * HRMS_SalaryHeader.DaysWorking)  as GrossAmount,
        HRMS_SalaryHeader.NetAmount as NetAmount,
        Set_Designation.DesignationName,
        HRMS_PayrollType.Code,
        HRMS_PayrollType.Description as DedType,
        HRMS_Set_Employee.SocialSecNo,
        MAX(HRMS_SalaryDetail.CreateDate),
        isnull(HRMS_PayrollType.DefaultValue,0) as DefaultValue,
        --Round(HRMS_SalaryDetail.Amount,0) as ManualAmount,
        (((isnull(HRMS_SalaryDetail.Amount,0))) / (DATEDIFF(day, @FromDate, @ToDate) + 1) * HRMS_SalaryHeader.DaysWorking) as ManualAmount,
        HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365 AS Years,
        SET_Department.DepartmentName AS Department

FROM HRMS_Set_Employee 

		inner join HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
		AND HRMS_SalaryHeader.Status=1
		
		inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
		and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
		and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
		And HRMS_SalaryDetail.PayrollTypeID in (select PayrollTypeID  from HRMS_PayrollType where  Description like '%Social%Security%' and status = 1)

		inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
		--and HRMS_PayrollType.CompanyID = HRMS_SalaryDetail.CompanyID		
		
        inner join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PayrollTypeID = HRMS_PayrollType.PayrollTypeID
		--and HRMS_PayrollType.CompanyID=HRMS_EmployeePayroll.CompanyID 
		And HRMS_EmployeePayroll.Status=1

		left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		--AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and Set_Designation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        Left join Set_Grade on Set_Grade.GradeID = HRMS_SET_Employee.FK_GradeID
        --and Set_Grade.CompanyID = HRMS_SET_Employee.CompanyID
        and Set_Grade.status =1        
        
        where 
		HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
        and HRMS_SalaryDetail.CompanyID =@CompanyID
        and HRMS_SalaryDetail.PeriodID = @PeriodID
        and HRMS_PayrollType.Description like '%Social%Security%'
		And HRMS_SalaryDetail.Amount > 0
        and isnull(HRMS_Set_Employee.IsSSMember,0) = 1
        and HRMS_EmployeePayroll.PersonID = HRMS_SalaryHeader.PersonID
        {0}
         group by HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.PersonID,HRMS_EmployeePayroll.ManualAmount,
        HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
        Set_Grade.GradeName,Set_Designation.DesignationName,HRMS_PayrollType.Description,
        HRMS_PayrollType.Code,HRMS_Set_Employee.SocialSecNo,HRMS_PayrollType.DefaultValue,
		HRMS_Set_Employee.BasicSalary,HRMS_SalaryHeader.NetAmount,HRMS_SalaryDetail.Amount,HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365,SET_Department.DepartmentName,HRMS_SalaryHeader.SalaryHeaderID";

        const string Query_ESSIContributionReportforDKSM = @"
Select Distinct  HRMS_Set_Employee.FullName,
        HRMS_Set_Employee.FatherName,
        HRMS_Set_Employee.EmployeeCode,
        HRMS_Set_Employee.PersonID,
        Set_Grade.GradeName,
        HRMS_Set_Employee.JoiningDate,
        case when len(CNICNumber) = 13 then SUBSTRING(CNICNumber, 1, 5)+'-'+SUBSTRING(CNICNumber, 6, 7)+'-'+SUBSTRING(CNICNumber, 11, 1)  else CNICNumber 
		   end as CNICNumber,
        (((isnull(HRMS_EmployeePayroll.ManualAmount,0) * 100)/6) / (DATEDIFF(day, @FromDate, @ToDate) + 1) * HRMS_SalaryHeader.DaysWorking)  as GrossAmount,
        HRMS_SalaryHeader.NetAmount as NetAmount,
        SET_JobDesignation.JobDesignationName,
        HRMS_PayrollType.Code,
        HRMS_PayrollType.Description as DedType,
        HRMS_Set_Employee.SocialSecNo,
        MAX(HRMS_SalaryDetail.CreateDate),
        isnull(HRMS_PayrollType.DefaultValue,0) as DefaultValue,
        Round(HRMS_SalaryDetail.Amount,0) as ManualAmount,HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365 AS Years,
        SET_Department.DepartmentName AS Department

FROM HRMS_Set_Employee 

		inner join HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
		AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
		AND HRMS_SalaryHeader.Status=1
		
		inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
		and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
		and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID
		And HRMS_SalaryDetail.PayrollTypeID in (select PayrollTypeID  from HRMS_PayrollType where  Description like '%Social%Security%' and status = 1)

		inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
		--and HRMS_PayrollType.CompanyID = HRMS_SalaryDetail.CompanyID		
		
        inner join HRMS_EmployeePayroll on HRMS_EmployeePayroll.PayrollTypeID = HRMS_PayrollType.PayrollTypeID
		--and HRMS_PayrollType.CompanyID=HRMS_EmployeePayroll.CompanyID 
		And HRMS_EmployeePayroll.Status=1

	    left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
		--AND Set_Designation.CompanyID = HRMS_Set_Employee.CompanyID
		and SET_JobDesignation.status =1
		
        left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
        and SET_Department.status =1
        
        Left join Set_Grade on Set_Grade.GradeID = HRMS_SET_Employee.FK_GradeID
        --and Set_Grade.CompanyID = HRMS_SET_Employee.CompanyID
        and Set_Grade.status =1        
        
        where 
		HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
        and HRMS_SalaryDetail.CompanyID =@CompanyID
        and HRMS_SalaryDetail.PeriodID = @PeriodID
        and HRMS_PayrollType.Description like '%Social%Security%'
		And HRMS_SalaryDetail.Amount > 0
        and isnull(HRMS_Set_Employee.IsSSMember,0) = 1
        and HRMS_EmployeePayroll.PersonID = HRMS_SalaryHeader.PersonID
        {0}
        group by HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.PersonID,HRMS_EmployeePayroll.ManualAmount,
        HRMS_Set_Employee.FullName,HRMS_Set_Employee.FatherName,
        Set_Grade.GradeName,SET_JobDesignation.JobDesignationName,HRMS_PayrollType.Description,
        HRMS_PayrollType.Code,HRMS_Set_Employee.SocialSecNo,HRMS_PayrollType.DefaultValue,
		HRMS_Set_Employee.BasicSalary,HRMS_SalaryHeader.NetAmount,HRMS_SalaryDetail.Amount,HRMS_SalaryHeader.DaysWorking,
        HRMS_Set_Employee.DateOfBirth,HRMS_Set_Employee.CNICNumber,HRMS_Set_Employee.JoiningDate,
		DATEDIFF(DAY, HRMS_Set_Employee.DateOfBirth, GETDATE()) / 365,SET_Department.DepartmentName,HRMS_SalaryHeader.SalaryHeaderID";

        const string Query_OneTimeAllowanceAndDeductionReport = @" SELECT 
TABSb.PersonID,
TABSb.PersonTypeCode,
TABSb.EmployeeCode,
TABSb.FullName,
TABSb.JobDesignationName,
TABSb.DepartmentName,
TABSb.AccountHolderName,
TABSb.BankAccountNumber,
TABSb.BankBranchName,
TABSb.BankName,
TABSb.BasicSalary,
TABSb.GrossAmount,
TABSb.DeductionAmount,
TABSb.NetAmount,
TABSb.PayrollTypeCode,
(case TABSb.Impact 
	when '1' then TABSb.Description 
	else null end)as Allowances ,
(case TABSb.Impact 
	when '2' then TABSb.Description 
	else null end)as Deductions ,
(case TABSb.Impact 
	when '1' then TABSb.Amount 
	else null end)as AllowancesAmount ,
(case TABSb.Impact 
	when '2' then TABSb.Amount 
	else null end) as DeductionsAmount ,
(case TABSb.Impact 
	when '1' then TABSb.Standard 
	else null end)as AllowancesStandardAmount ,
(case TABSb.Impact 
	when '2' then TABSb.Standard 
	else null end) as DeductionsStandardAmount ,
TABSb.Impact,
TABSb.LeaveWithouPay

from

(
SELECT 

HRMS_SET_Employee.PersonID,
HRMS_SET_Employee.PersonTypeCode,
HRMS_SET_Employee.EmployeeCode,
HRMS_SET_Employee.FullName,
HRMS_SET_Employee.BasicSalary,
HRMS_PayrollType.DefaultValue as Standard,
SET_JobDesignation.JobDesignationName,
SET_Department.DepartmentName,
AR_SET_BankAccount.AccountHolderName,
AR_SET_BankAccount.BankAccountNumber,
AR_SET_BankBranch.BankBranchName,
AR_SET_Bank.BankName,
HRMS_SalaryHeader.GrossAmount,
HRMS_SalaryHeader.DeductionAmount,
HRMS_SalaryHeader.NetAmount,
HRMS_SalaryDetail.PayrollTypeCode,
HRMS_PayrollType.Description,
HRMS_SalaryDetail.Amount,
HRMS_PayrollType.Impact,
'' as LeaveWithouPay

FROM

HRMS_SalaryHeader

INNER JOIN HRMS_SalaryDetail on
HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID  and
HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_SET_Employee on
HRMS_SalaryHeader.PersonID = HRMS_SET_Employee.PersonID AND 
HRMS_SalaryHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1

left JOIN SET_JobDesignation ON
HRMS_SET_Employee.FK_JobDesignationID = SET_JobDesignation.JobDesignationID AND 
SET_JobDesignation.Status = 1

left JOIN SET_Department ON 
HRMS_SET_Employee.FK_DepartmentID = SET_Department.DepartmentID AND
SET_Department.Status = 1

left JOIN AR_SET_BankAccount on
HRMS_SET_Employee.FK_BankAccountID = AR_SET_BankAccount.BankAccountID AND
AR_SET_BankAccount.Status = 1

left JOIN AR_SET_BankBranch on
HRMS_SET_Employee.FK_BankBranchID = AR_SET_BankBranch.BankBranchID and
AR_SET_BankBranch.Status = 1

left JOIN AR_SET_Bank on
AR_SET_BankBranch.BankID = AR_SET_Bank.BankID and 
AR_SET_Bank.Status = 1

left join HRMS_EmployeePayroll on
HRMS_SalaryDetail.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
HRMS_SalaryDetail.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
HRMS_EmployeePayroll.IsOneTime = 1

INNER JOIN HRMS_PayrollType on
HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode AND
HRMS_PayrollType.Status = 1 and HRMS_PayrollType.PayrollTypeCode <> 4


WHERE 

DATENAME(month, [HRMS_SalaryHeader].[SalaryDate]) = @Month AND
DATEPART(Year, [HRMS_SalaryHeader].[SalaryDate]) = @Year AND
[HRMS_SalaryHeader].CompanyID = @CompanyID AND 
[HRMS_SalaryHeader].PeriodID = @PeriodID AND 
([HRMS_SalaryHeader].PersonID = @PersonID OR @PersonID is null) AND
[HRMS_SalaryHeader].Status = 1
 ) TABSb ";



        const string Query_GetDataByPayrollTypeCode = @"

";
        const string Query_GetdataForAlreadySalaryGenerate = @"
select * from hrms_salaryHEader
where DATENAME(month,hrms_salaryHEader.SalaryDate) = @Month
and DATENAME(YEAR,hrms_salaryHEader.SalaryDate) = @Year
and Status = 1 and CompanyID = @CompanyID
";

        const string Query_GetdataForSalaryHeaderPF = @"
select * from HRMS_SalaryHeader where SalaryHeaderID = @SalaryHeaderID
 and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1
";

        const string Query_GetdataForAlreadySalaryGenerate_ForInc = @"
select * from hrms_salaryHEader
where DATENAME(month,hrms_salaryHEader.SalaryDate) = @Month
and DATENAME(YEAR,hrms_salaryHEader.SalaryDate) = @Year
and Status = 1 and CompanyID = @CompanyID
";


        const string Query_GetdataForAlreadyIncrement = @"
select * from HRMS_EmployeeIncrement_Allowancewise
where DATENAME(month,HRMS_EmployeeIncrement_Allowancewise.PayrollDate) = @Month
and DATENAME(YEAR,HRMS_EmployeeIncrement_Allowancewise.PayrollDate) = @Year
and Status = 1 and CompanyID = @CompanyID
";

        const string Query_SelectAllDataWithTrans = @"SELECT 
        HRMS_Set_Employee.PersonID
        ,(select DATEDIFF(MONTH, HRMS_SET_TaxYear.FromDate, @SalaryDate) as NumberOfMonths from HRMS_SET_TaxYear where PeriodID = @PeriodID and @SalaryDate <= HRMS_SET_TaxYear.ToDate and @SalaryDate >= HRMS_SET_TaxYear.FromDate ) as NumberOfMonths
        ,isnull(Sum(HRMS_SalaryHeader.GrossAmountTaxable),0) as GrossAmount,isnull(Sum(HRMS_SalaryHeader.TaxAmount),0) as TaxAmount ,HRMS_SalaryHeader.IsApproved
        FROM HRMS_Set_Employee left JOIN HRMS_SalaryHeader on HRMS_Set_Employee.CompanyID =  HRMS_SalaryHeader.CompanyID
        and HRMS_Set_Employee.PersonID =  HRMS_SalaryHeader.PersonID And (HRMS_SalaryHeader.SalaryDate <= @SalaryDate AND HRMS_SalaryHeader.SalaryDate <= DATEADD(d, -1, DATEADD(m, DATEDIFF(m, 0, @SalaryDate) + 1, 0)) )
        WHERE HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.IsActive = 1 
group by HRMS_Set_Employee.PersonID,HRMS_SalaryHeader.IsApproved";

        const string Query_SelectAllDataWithTransStatusID = @"SELECT 
        HRMS_Set_Employee.PersonID
        ,Isnull((select Isnull(DATEDIFF(MONTH, HRMS_SET_TaxYear.FromDate, @SalaryDate),0) as NumberOfMonths from HRMS_SET_TaxYear 
		where PeriodID = @PeriodID and @SalaryDate <= HRMS_SET_TaxYear.ToDate 
		and @SalaryDate >= HRMS_SET_TaxYear.FromDate ),0) as NumberOfMonths
        ,isnull(Sum(HRMS_SalaryHeader.GrossAmountTaxable),0) as GrossAmount,isnull(Sum(HRMS_SalaryHeader.TaxAmount),0) as TaxAmount ,HRMS_SalaryHeader.IsApproved
        FROM HRMS_Set_Employee left JOIN HRMS_SalaryHeader on HRMS_Set_Employee.CompanyID =  HRMS_SalaryHeader.CompanyID
        and HRMS_Set_Employee.PersonID =  HRMS_SalaryHeader.PersonID
		 And (HRMS_SalaryHeader.SalaryDate <= @SalaryDate AND HRMS_SalaryHeader.SalaryDate <= DATEADD(d, -1, DATEADD(m, DATEDIFF(m, 0, @SalaryDate) + 1, 0)) )
        WHERE HRMS_Set_Employee.Status = 1 and HRMS_Set_Employee.IsActive = 1 --And HRMS_SalaryHeader.Status=1
        And (HRMS_SalaryHeader.Status is null OR  HRMS_SalaryHeader.Status = 1)
{0}
group by HRMS_Set_Employee.PersonID,HRMS_SalaryHeader.IsApproved";
        const string Query_GetDataByMonthAndYear = @"select Max(SalaryHeaderID) as SalaryHeaderID,Max(PostID) as PostID from HRMS_SalaryHeader 

WHERE
DATENAME(month, HRMS_SalaryHeader.SalaryDate) =  @Month
AND DATENAME(year, HRMS_SalaryHeader.SalaryDate) =  @Year
AND HRMS_SalaryHeader.PeriodID = @PeriodID
AND HRMS_SalaryHeader.CompanyID = @CompanyID
AND HRMS_SalaryHeader.Status = 1
AND (HRMS_SalaryHeader.PersonID = @PersonID OR @PersonID IS NULL)
{0}
 ";

        const string Query_GetSalaryByMonthAndYear = @"select isnull(Max(NetAmount),0) as NetAmount from HRMS_SalaryHeader 

WHERE
DATENAME(month, HRMS_SalaryHeader.SalaryDate) =  @Month
AND DATENAME(year, HRMS_SalaryHeader.SalaryDate) =  @Year
AND HRMS_SalaryHeader.CompanyID = @CompanyID
AND HRMS_SalaryHeader.Status = 1
AND (HRMS_SalaryHeader.PersonID = @PersonID)";


        const string Query_dataforCashPayment = @"
select
		SalaryHeaderID
		, NetAmount
		,PersonID 
		,datename(month ,  SalaryDate) as SalaryMonth 
		,datename(year ,  SalaryDate) as SalaryYear 
from HRMS_SalaryHeader
	 where Status = 1
			and PersonID = @PersonID
			and CompanyID = @CompanyID
			and PeriodID  = @PeriodID
            and PaymentID is null 
";

        const string UpdatePaymentID = @"
	 update HRMS_SalaryHeader set PaymentID =  @PaymentID  where SalaryHeaderID = @SalaryHeaderID
and CompanyID = @CompanyID and PeriodID = @PeriodID
";

        const string Query_GetTaxableGrossAmount = @"SELECT 
 SET_Period.StartDate
,SET_Period.EndDate
,Sum(HRMS_SalaryHeader.GrossAmountTaxable) as GrossAmount
,Count(HRMS_SalaryHeader.GrossAmountTaxable) as NumberOfMonths
from SET_Period

JOIN HRMS_SalaryHeader on
HRMS_SalaryHeader.PeriodID = SET_Period.PeriodID and
HRMS_SalaryHeader.Status = 1 

where

HRMS_SalaryHeader.SalaryDate >= SET_Period.StartDate and
HRMS_SalaryHeader.PeriodID = @PeriodID and 
HRMS_SalaryHeader.PersonID = @PersonID

group by SET_Period.StartDate,SET_Period.EndDate";

        const string Query_IncomeTaxDedMonthWise = @"

";

        const string Query_EmployeeSalarySheet_ReportDptWiseData_old = @"
select
case when TAB.PayrollTypeCode = 6 then (1+TAB.PayrollTypeId) when TAB.PayrollTypeCode = 1 then (150+ TAB.PayrollTypeId)
when TAB.PayrollTypeCode = 2 then (210+PayrollTypeId) when TAB.PayrollTypeCode = 3 then (250 + PayrollTypeId)
when TAB.PayrollTypeCode = 5 then (258+PayrollTypeId)
end as SortCategory,
 --tab.DeductionType,
tab.FullName, CONVERT(varchar, tab.EmployeeCode) as EmployeeCode,
--,tab.DeductionAmount,
tab.DepartmentID,tab.DepartmentName,tab.PayrollTypeID,tab.GrossAmount,
tab.NetAmount
,tab.Description, round(tab.DesAmount,0) as Amount
 from (

Select distinct 
--HRMS_PayrollType.Description as DeductionType,
HRMS_SalaryDetail.PayrollTypeCode,
HRMS_SE.FullName,HRMS_SE.EmployeeCode,
--HRMS_SalaryDetail.Amount as DeductionAmount,
HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,
HRMS_PayrollType.PayrollTypeID,max(round(HRMS_SalaryHeader.GrossAmount,0)) as GrossAmount,
max(round(HRMS_SalaryHeader.NetAmount,0)) as NetAmount,
HRMS_PayrollType.Description,round(HRMS_SalaryDetail.Amount,0) as DesAmount


from HRMS_SalaryHeader

INNER JOIN HRMS_SalaryDetail on
HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID  and
HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_SET_Employee HRMS_SE on
HRMS_SalaryHeader.PersonID = HRMS_SE.PersonID AND 
HRMS_SalaryHeader.CompanyID = HRMS_SE.CompanyID AND
HRMS_SE.Status = 1

left JOIN SET_JobDesignation ON
HRMS_SE.FK_JobDesignationID = SET_JobDesignation.JobDesignationID AND 
HRMS_SE.CompanyID = SET_JobDesignation.CompanyID and
SET_JobDesignation.Status = 1

left JOIN SET_Department HRMS_DP ON 
HRMS_SE.FK_DepartmentID = HRMS_DP.DepartmentID AND
HRMS_DP.Status = 1

left JOIN AR_SET_BankAccount on
HRMS_SE.FK_BankAccountID = AR_SET_BankAccount.BankAccountID AND

AR_SET_BankAccount.Status = 1

inner join AR_SET_BankBranch on 
AR_SET_BankBranch.BankBranchID =  AR_SET_BankAccount.BankBranchID
and AR_SET_BankBranch.status =1 

left JOIN AR_SET_Bank on
AR_SET_BankBranch.BankID = AR_SET_Bank.BankID and 
AR_SET_Bank.Status = 1

Left  join HRMS_EmployeePayroll on
HRMS_SalaryDetail.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
HRMS_SalaryDetail.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode
 and (HRMS_EmployeePayroll.PersonID  = [HRMS_SalaryHeader].PersonID)

INNER JOIN HRMS_PayrollType on
HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_SalaryDetail.CompanyID = HRMS_PayrollType.CompanyID AND
HRMS_PayrollType.Status = 1 and HRMS_PayrollType.PayrollTypeCode <> 4
and HRMS_PayrollType.Isvisiable = 1

WHERE 
DATENAME(month, HRMS_SalaryHeader.SalaryDate) = @Month AND
DATEPART(Year,HRMS_SalaryHeader.SalaryDate) = @Year AND
HRMS_SalaryHeader.CompanyID = @CompanyID AND 
HRMS_SalaryHeader.PeriodID = @PeriodID AND 
HRMS_SalaryHeader.Status = 1
{0}

group by 
--HRMS_PayrollType.Description,
HRMS_SE.FullName
,HRMS_SalaryHeader.PersonID,
HRMS_SE.EmployeeCode
,HRMS_DP.DepartmentName
,HRMS_DP.DepartmentID
,HRMS_PayrollType.PayrollTypeID
,HRMS_SalaryDetail.PayrollTypeCode
,HRMS_SalaryDetail.Amount
,HRMS_PayrollType.Description

)tab

group by --tab.DeductionType,
tab.FullName,tab.EmployeeCode,--tab.DeductionAmount,
tab.DepartmentID,tab.DepartmentName,tab.PayrollTypeID,tab.PayrollTypeCode,tab.GrossAmount,tab.NetAmount
,tab.Description,tab.DesAmount
--ORDER BY tab.EmployeeCode


---------------------
Union All
Select
210  as SortCategor,
--tab.DeductionType,
tab.FullName,  CONVERT(varchar, tab.EmployeeCode) as EmployeeCode,--tab.DeductionAmount,
tab.DepartmentID,tab.DepartmentName,
tab.PayrollTypeID,tab.GrossAmount,tab.NetAmount, tab.Description, round(tab.DesAmount,0) as Amount
 from (

select 
DISTINCT HRMS_SH.MasterSalary,HRMS_SH.GrossAmount as GrossSalary, HRMS_SD.PeriodID,HRMS_SD.CompanyID,0 as SalaryDetailID,CAST(HRMS_SE.EmployeeCode as int) as EmployeeCode,HRMS_SE.FullName as FullName,
HRMS_JD.JobDesignationID as JobDesignationID,HRMS_JD.JobDesignationName as JobDesignationName,HRMS_DP.DepartmentID as DepartmentID,HRMS_DP.DepartmentName as DepartmentName,0 as PayrollTypeID,0 as PayrollTypeCode,
--'Gross Amount' as DeductionType,
0 as Amount,HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking,
(case when MAX(HRMS_SH.GrossAmount)%10 = 9 then MAX(HRMS_SH.GrossAmount) else MAX(HRMS_SH.GrossAmount) end) GrossAmount,
--(case when MAX(HRMS_SH.GrossAmount)%10 = 9 then MAX(HRMS_SH.GrossAmount) else MAX(HRMS_SH.GrossAmount) end) as DeductionAmount,
0 as NetAmount
,HRMS_PT.Description, round(HRMS_SD.Amount,0) as DesAmount
from HRMS_SalaryDetail HRMS_SD 

Inner join HRMS_SalaryHeader HRMS_SH 
	on HRMS_SD.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SD.SalaryHeaderID=HRMS_SH.SalaryHeaderID

Inner join HRMS_SET_Employee HRMS_SE
	ON HRMS_SE.PersonID=HRMS_SH.PersonID
	AND HRMS_SE.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SE.Status=1 AND HRMS_SE.IsActive=1

Left Join SET_JobDesignation HRMS_JD
	ON HRMS_JD.CompanyID=HRMS_SE.CompanyID
	AND HRMS_JD.JobDesignationID=HRMS_SE.FK_JobDesignationID
	AND HRMS_JD.Status=1
	
Inner JOin SET_Department HRMS_DP
	ON HRMS_DP.DepartmentID=HRMS_SE.FK_DepartmentID
	AND HRMS_DP.Status=1
	
left Join HRMS_EmployeePayroll HRMS_EP
	ON HRMS_EP.PayrollTypeCode=HRMS_SD.PayrollTypeCode
	AND HRMS_EP.PayrollTypeID=HRMS_SD.PayrollTypeID
	AND HRMS_EP.PersonID=HRMS_SH.PersonID
	AND HRMS_EP.Status=1
	
INNER JOIN HRMS_PayrollType HRMS_PT on
HRMS_SD.PayrollTypeID = HRMS_PT.PayrollTypeID AND
HRMS_SD.PayrollTypeCode = HRMS_PT.PayrollTypeCode 
AND HRMS_SD.CompanyID = HRMS_PT.CompanyID AND
HRMS_PT.Status = 1 and HRMS_PT.PayrollTypeCode <> 4 AND HRMS_PT.IsVisiable=1
WHERE	
DATENAME(month, HRMS_SH.SalaryDate) = @Month AND
DATEPART(Year,HRMS_SH.SalaryDate) = @Year AND
HRMS_SH.CompanyID = @CompanyID AND 
HRMS_SH.PeriodID = @PeriodID AND 
HRMS_SH.Status = 1
{0}
group by HRMS_EP.PersonID,HRMS_SE.BasicSalary,
SalaryDetailID,HRMS_PT.Description ,HRMS_SD.PayrollTypeID,HRMS_SD.PayrollTypeCode,Amount,HRMS_SH.MasterSalary,
HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SE.FullName,HRMS_SH.GrossAmount,--HRMS_SH.DeductionAmount,
HRMS_SD.PeriodID,HRMS_SD.CompanyID,
HRMS_SH.NetAmount,HRMS_JD.JobDesignationID,HRMS_JD.JobDesignationName,HRMS_SE.EmployeeCode,HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking
,HRMS_PT.Description, HRMS_SD.Amount
)TAB
GROUP BY
 TAB.PayrollTypeCode,tab.GrossSalary,tab.PeriodID,tab.CompanyID,tab.PersonID,tab.FullName,tab.JobDesignationID,tab.JobDesignationName,tab.DepartmentID,tab.DepartmentName,
tab.PayrollTypeID, 
--tab.DeductionType,
tab.Amount,tab.EmployeeCode,--tab.DeductionAmount,
tab.PayrollTypeID,tab.PayrollTypeCode,tab.GrossAmount,tab.NetAmount,tab.DaysTotal,tab.DaysWorking,tab.MasterSalary
,tab.Description,tab.DesAmount
--order by tab.EmployeeCode
";

        const string Query_EmployeeMonthlyDeduction_ReportData = @"
select HRMS_PayrollType.description as DeductionType,HRMS_SET_Employee.FullName as FromEmployeeName,HRMS_SET_Employee.PersonID as FromPersonID,
HRMS_EmployeeMonthlyDeduction.Amount,EP.FullName as ToEmployeeName,HRMS_EmployeeMonthlyDeduction.FromGrossSalary,
HRMS_EmployeeMonthlyDeduction.ToGrossSalary,FrmLoc.LocationName as  fromLocation,ToLoc.LocationName as ToLocation,
SET_Business.BusinessName +'-'+ SET_OperatingUnit.OperatingUnitName as FromCompany,
BU.BusinessName +'-'+ OP.OperatingUnitName as ToCompany,
HRMS_EmployeeMonthlyDeduction.FromDate,HRMS_EmployeeMonthlyDeduction.ToDate,HRMS_EmployeeMonthlyDeduction.ForTheMonth,

Case HRMS_EmployeeMonthlyDeduction.PayRollTypeCode when 1 then 'Allowances' when 2 then 'Deduction' end as DedType,
case HRMS_EmployeeMonthlyDeduction.FromPersonTypeCode 
when 1 then 'Permanent'
when 2 then 'Contract'
when 3 then 'DailyWages'
when 4 then 'Seasonal Contract'
when 5 then 'Seasonal DailyWages'
when 6 then 'Trainee'
end 
as PersonTypeFrom,
case HRMS_EmployeeMonthlyDeduction.ToPersonTypeCode 
when 1 then 'Permanent'
when 2 then 'Contract'
when 3 then 'DailyWages'
when 4 then 'Seasonal Contract'
when 5 then 'Seasonal DailyWages'
when 6 then 'Trainee'
end 
as PersonTypeTo,
case HRMS_EmployeeMonthlyDeduction.FromStatusID 
when 1 then 'Regular'
when 2 then 'part Time'
when 3 then 'Full Time'
when 4 then 'Contract'
when 5 then 'Temporary'
when 6 then 'Seasonal Contract'
end 
as PersonStatusFrom,
case HRMS_EmployeeMonthlyDeduction.ToStatusID
when 1 then 'Regular'
when 2 then 'part Time'
when 3 then 'Full Time'
when 4 then 'Contract'
when 5 then 'Temporary'
when 6 then 'Seasonal Contract'
end 
as PersonStatusTo,
case HRMS_EmployeeMonthlyDeduction.FromLocationTypeID
when 1 then 'HO'
when 2 then 'CO'
when 3 then 'Plant'
end
as FromLocationType,
case HRMS_EmployeeMonthlyDeduction.ToLocationTypeID
when 1 then 'HO'
when 2 then 'CO'
when 3 then 'Plant'
end
as ToLocationType

from HRMS_EmployeeMonthlyDeduction
 
inner join HRMS_SET_Employee on HRMS_SET_Employee.personID = HRMS_EmployeeMonthlyDeduction.fromPersonID
and HRMS_SET_Employee.CompanyID = HRMS_EmployeeMonthlyDeduction.CompanyID
and HRMS_SET_Employee.Status = 1

inner join HRMS_SET_Employee EP on EP.personID = HRMS_EmployeeMonthlyDeduction.ToPersonID
and EP.CompanyID = HRMS_EmployeeMonthlyDeduction.CompanyID

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeMonthlyDeduction.PayrollTypeID

left join Set_Department on Set_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
and Set_Department.Status = 1

left join Set_Location FrmLoc on FrmLoc.LocationID = HRMS_EmployeeMonthlyDeduction.FromLocationID
and FrmLoc.Status = 1

left join Set_Location ToLoc on ToLoc.LocationID = HRMS_EmployeeMonthlyDeduction.ToLocationID
and ToLoc.Status = 1

left join Set_Company fromCompany on fromCompany.CompanyID = HRMS_EmployeeMonthlyDeduction.FromCompanyID
and fromCompany.Status =1	
		
left join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitID = fromCompany.OperatingUnitId
and SET_OperatingUnit.Status =1 
		
left join SET_Business on SET_Business.BusinessID = fromCompany.BusinessID
and SET_Business.Status =1
	
left join Set_Company ToCompany on ToCompany.CompanyID = HRMS_EmployeeMonthlyDeduction.ToCompanyID
and ToCompany.Status =1	
		
left join SET_OperatingUnit OP on OP.OperatingUnitID = ToCompany.OperatingUnitID
and OP.Status =1 
		
left join SET_Business BU on BU.BusinessID = ToCompany.BusinessID
and BU.Status =1

where 
DATENAME(month, HRMS_EmployeeMonthlyDeduction.ForTheMonth) = @Month AND
DATEPART(Year, HRMS_EmployeeMonthlyDeduction.ForTheMonth) = @Year AND
HRMS_EmployeeMonthlyDeduction.CompanyID = @CompanyID AND 
HRMS_EmployeeMonthlyDeduction.PeriodID = @PeriodID AND 
HRMS_EmployeeMonthlyDeduction.Status = 1
{0}
";

        const string Query_IncomeTaxComputation_ReportData = @" SELECT 

FIN2TAB.PersonID,
FIN2TAB.EmployeeCode,
FIN2TAB.FullName,
FIN2TAB.BasicSalary,
FIN2TAB.JobDesignationName,
FIN2TAB.DepartmentName,
FIN2TAB.PeriodStartDate,
FIN2TAB.PeriodEndDate,
FIN2TAB.SalaryMonths,
FIN2TAB.SalaryMonthsInPeriod,
FIN2TAB.LastSalaryDate,
FIN2TAB.FirstSalaryDate,
FIN2TAB.AllowancesAmount,
FIN2TAB.GrossAmount,
FIN2TAB.NetAmount,
FIN2TAB.IncomeTaxAmount
,FIN2TAB.PreviousTaxableGrossAmount,
HRMS_IncomeTaxParameters.FromAmount,
HRMS_IncomeTaxParameters.ToAmount,
HRMS_IncomeTaxParameters.Percentage,
HRMS_IncomeTaxParameters.ExceedAmount,
HRMS_IncomeTaxParameters.FixedTaxAmount,
isnull(tblCurrent.Allowances,0) CurrentAllowances,
tblCurrent.SalaryDate AS CurrentSalaryDate,
isnull(tblCurrent.GAmount,0) AS CurrentGrossAmount,
isnull(tblCurrent.netAmount,0) AS CurrentNetAmount,
isnull(tblCurrent.GrossAmountTaxable,0) AS CurrentTaxableGross,
isnull(tblCurrent.Dedudction,0) AS CurrentDedudction 
,isnull(tblCurrent.IncomeTaxAmount,0) as CurrentIncomeTaxAmount

FROM (

SELECT 

PeriodID,
PersonID,
EmployeeCode,
FullName,
BasicSalary,
JobDesignationName,
DepartmentName,
MIN(StartDate) AS PeriodStartDate,
MAX(EndDate) AS PeriodEndDate,
SUM(Amount) AS AllowancesAmount,
max(GAmount) AS GrossAmount,
SUM(netAmount) AS NetAmount
,SUM(GrossAmountTaxable)-max(GAmount) AS PreviousTaxableGrossAmount,
MAX(Months) AS SalaryMonthsInPeriod,
count(Months) as SalaryMonths,
MAX(LastSalaryMonths) AS LastSalaryDate,
MIN(StartSalaryMonths) AS FirstSalaryDate
,sum(IncomeTaxAmount) as IncomeTaxAmount

from

(SELECT 
SET_Period.PeriodID,
HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.EmployeeCode,
HRMS_Set_Employee.FullName,
HRMS_Set_Employee.BasicSalary,
[SET_JobDesignation].[JobDesignationName],
SET_Department.DepartmentName,
SET_Period.StartDate,
SET_Period.EndDate,
max(sd1.Amount)AS Amount,
sh1.SalaryDate AS salaryDate,
max(sh1.GrossAmount)AS GAmount,
max(sh1.NetAmount) AS netAmount,
max(sh1.GrossAmountTaxable) AS GrossAmountTaxable,
(
case when DATEDIFF(Month,max(SET_Period.StartDate),max(sh1.SalaryDate))+1 > 12 then 12 else DATEDIFF(Month,max(SET_Period.StartDate),max(sh1.SalaryDate))+1 end) AS Months,
max(sh1.SalaryDate) AS LastSalaryMonths,
min(sh1.SalaryDate) AS StartSalaryMonths
,SDT.Amount as IncomeTaxAmount

FROM HRMS_SalaryHeader sh1

JOIN HRMS_Set_Employee  ON
HRMS_Set_Employee.PersonID = sh1.PersonID AND
HRMS_Set_Employee.CompanyID = sh1.CompanyID AND
sh1.Status = 1 
left JOIN HRMS_SalaryDetail sd1 ON 
sh1.SalaryHeaderID = sd1.SalaryHeaderID AND
sh1.CompanyID = sd1.CompanyID AND
sh1.PeriodID = sd1.PeriodID AND
sd1.Status = 1 

left JOIN HRMS_PayrollType ON
sd1.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
sd1.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode AND
----sd1.CompanyID = HRMS_PayrollType.CompanyID AND 
HRMS_PayrollType.PayrollTypeCode = 1 AND HRMS_PayrollType.IsTaxable = 1 AND HRMS_PayrollType.IsPayable = 1


JOIN SET_Period ON
sh1.PeriodID = SET_Period.PeriodID AND
SET_Period.Status = 1 AND SET_Period.IsActive = 1

Left JOIN HRMS_SalaryDetail SDT
ON SDT.SalaryHeaderID=sh1.SalaryHeaderID
 AND SDT.Status = 1 
 and SDT.PayrollTypeID=5 and SDT.PayrollTypeCode=2
AND sh1.SalaryDate <= @SalaryDate

left JOIN SET_JobDesignation ON
[HRMS_Set_Employee].[FK_JobDesignationID]=  [SET_JobDesignation].[JobDesignationID] AND
----[HRMS_Set_Employee].[CompanyID]=  [SET_JobDesignation].[CompanyID] AND
[SET_JobDesignation].[Status] = 1 AND [SET_JobDesignation].[IsActive] = 1

LEFT JOIN SET_Department ON
[HRMS_Set_Employee].FK_DepartmentID = SET_Department.DepartmentID AND
[HRMS_Set_Employee].Status = 1 AND  SET_Department.IsActive = 1

WHERE sh1.SalaryDate <= @SalaryDate
{0}

GROUP BY
SET_Period.PeriodID,
HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.EmployeeCode,
HRMS_Set_Employee.FullName,
[SET_JobDesignation].[JobDesignationName],
SET_Department.DepartmentName,
SET_Period.StartDate,
SET_Period.EndDate,
sh1.SalaryDate,
SDT.Amount
,HRMS_Set_Employee.BasicSalary
) as FINTAB


group by 
PeriodID,
PersonID,
EmployeeCode,
FullName,
BasicSalary,
JobDesignationName,
DepartmentName
--,IncomeTaxAmount
) AS FIN2TAB

 LEFT JOIN HRMS_IncomeTaxParameters on
 HRMS_IncomeTaxParameters.PeriodID = FIN2TAB.PeriodID AND
 HRMS_IncomeTaxParameters.Status = 1 and HRMS_IncomeTaxParameters.IsActive = 1
 and (((12-SalaryMonthsInPeriod)* GrossAmount)+ PreviousTaxableGrossAmount )  between FromAmount and ToAmount

LEFT JOIN 

(SELECT 
SET_Period.PeriodID,
HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.EmployeeCode,
sum(sd1.Amount) Allowances,
sh1.SalaryDate AS salaryDate,
max(sh1.GrossAmount)AS GAmount,
max(sh1.NetAmount) AS netAmount,
max(sh1.GrossAmountTaxable) AS GrossAmountTaxable,
max(sh1.DeductionAmount) AS Dedudction
,SDT.Amount as IncomeTaxAmount

FROM HRMS_SalaryHeader sh1--

JOIN HRMS_Set_Employee  ON
HRMS_Set_Employee.PersonID = sh1.PersonID AND
HRMS_Set_Employee.CompanyID = sh1.CompanyID AND
sh1.Status = 1 

INNER JOIN HRMS_SalaryDetail sd1 ON 
sh1.SalaryHeaderID = sd1.SalaryHeaderID AND
sh1.CompanyID = sd1.CompanyID AND
sh1.PeriodID = sd1.PeriodID AND
sd1.Status = 1 


INNER JOIN HRMS_PayrollType ON
sd1.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
sd1.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode AND
----sd1.CompanyID = HRMS_PayrollType.CompanyID AND 
HRMS_PayrollType.PayrollTypeCode = 1 AND HRMS_PayrollType.IsTaxable = 1 AND HRMS_PayrollType.Code <> 1

--left JOIN HRMS_PayrollType ON
--sd1.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
--sd1.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode AND
--sd1.CompanyID = HRMS_PayrollType.CompanyID AND 
--HRMS_PayrollType.PayrollTypeCode = 1 AND HRMS_PayrollType.IsTaxable = 1 AND HRMS_PayrollType.IsPayable = 1


JOIN SET_Period ON
sh1.PeriodID = SET_Period.PeriodID AND
SET_Period.Status = 1 AND SET_Period.IsActive = 1

left JOIN SET_JobDesignation ON
[HRMS_Set_Employee].[FK_JobDesignationID]=  [SET_JobDesignation].[JobDesignationID] AND
----[HRMS_Set_Employee].[CompanyID]=  [SET_JobDesignation].[CompanyID] AND
[SET_JobDesignation].[Status] = 1 AND [SET_JobDesignation].[IsActive] = 1

Left JOIN HRMS_SalaryDetail SDT
ON SDT.SalaryHeaderID=sh1.SalaryHeaderID
 AND SDT.Status = 1 
 and SDT.PayrollTypeID=5 and SDT.PayrollTypeCode=2
 AND datename(month,sh1.SalaryDate) = @Month and  datename(year,sh1.SalaryDate) = @Year

LEFT JOIN SET_Department ON
[HRMS_Set_Employee].FK_DepartmentID = SET_Department.DepartmentID AND
[HRMS_Set_Employee].Status = 1 AND  SET_Department.IsActive = 1

WHERE datename(month,sh1.SalaryDate) = @Month and  datename(year,sh1.SalaryDate) = @Year
{0}

GROUP BY
SET_Period.PeriodID,
HRMS_Set_Employee.PersonID,
HRMS_Set_Employee.EmployeeCode,
[SET_JobDesignation].[JobDesignationName],
SET_Department.DepartmentName,
SET_Period.StartDate,
SET_Period.EndDate,
sh1.SalaryDate,
SDT.Amount
,HRMS_Set_Employee.BasicSalary ) AS tblCurrent on

tblCurrent.PersonID = FIN2TAB.PersonID
";

        const string Query_EmployeeCashAdvice = @"

select convert( varchar(20), HRMS_SalaryHeader.SalaryHeaderID  )+'-'+ convert( varchar(20), HRMS_Set_Employee.EmployeeCode )as PaySlipNo
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID
,HRMS_Set_Employee.EmployeeCode
,HRMS_Set_Employee.FullName
--,(HRMS_SalaryHeader.GrossAmount) - (HRMS_SalaryHeader.DeductionAmount) as NetAmount 
,isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode = 1),0)
  
  - isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode in (2,5)),0)
   as NetAmount 
,AR_SET_Bank.BankName
,HRMS_Set_Employee.EmpAccountNo
, AR_SET_BankBranch.BankBranchName
,HRMS_SalaryHeader.SalaryDate
,HRMS_Set_Employee.IBanNo
,SET_Department.DepartmentName
,SET_Section.SectionName
,HRMS_SalaryHeader.DaysTotal
,HRMS_SalaryHeader.GrossAmount
,isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode in (2,5)),0) AS Deductions
,ISNULL((SELECT SUM(OverTimeHours) FROM HRMS_OverTime WHERE CompanyID = HRMS_SalaryHeader.CompanyID
AND PeriodID = HRMS_SalaryHeader.PeriodID AND PersonID = HRMS_SalaryHeader.PersonID
AND (FromDate <= @FromDate AND ToDate >= @ToDate))
,0) AS OvertimeHours
FROM HRMS_Set_Employee 
  
    INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
    AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
   And HRMS_SalaryHeader.Status=1

INNER JOIN SET_Department ON SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
   AND SET_Department.Status = 1 AND SET_Department.IsActive = 1

   INNER JOIN SET_Section ON SET_Section.SectionID = HRMS_Set_Employee.FK_SectionID
   AND SET_Section.Status = 1 AND SET_Section.IsActive = 1

inner join AR_SET_BankBranch on 
AR_SET_BankBranch.BankBranchID =  HRMS_Set_Employee.FK_BankBranchID
and AR_SET_BankBranch.status =1 

left JOIN AR_SET_Bank on
HRMS_SET_Employee.FK_BankAccountID = AR_SET_Bank.BankID and 
AR_SET_Bank.Status = 1
   
    where  HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
    and HRMS_SalaryHeader.CompanyID =@CompanyID and HRMS_SalaryHeader.PeriodID =@PeriodID
    and  HRMS_Set_Employee.IsActive = 1
    AND AR_SET_Bank.BankName like '%CASH%'

 {0}

Order by Cast(HRMS_Set_Employee.EmployeeCode as int) asc
";


        const string Query_EmployeeCashAdvice_RSM = @"

select convert( varchar(20), HRMS_SalaryHeader.SalaryHeaderID  )+'-'+ convert( varchar(20), HRMS_Set_Employee.EmployeeCode )as PaySlipNo
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID
,HRMS_Set_Employee.EmployeeCode
,HRMS_Set_Employee.FullName
--,(HRMS_SalaryHeader.GrossAmount) - (HRMS_SalaryHeader.DeductionAmount) as NetAmount 
,isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode = 1),0)
  
  - isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode in (2,5)),0)
   as NetAmount 
,AR_SET_Bank.BankName
,HRMS_Set_Employee.EmpAccountNo
, AR_SET_BankBranch.BankBranchName
,HRMS_SalaryHeader.SalaryDate
,HRMS_Set_Employee.IBanNo
,SET_Department.DepartmentName
,SET_Section.SectionName
,HRMS_SalaryHeader.DaysTotal
,HRMS_SalaryHeader.DaysWorking
,HRMS_JD.JobDesignationName
,HRMS_SalaryHeader.GrossAmount
,isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.PayrollTypeCode in (2,5)),0) AS Deductions
,ISNULL((SELECT SUM(OverTimeHours) FROM HRMS_OverTime WHERE CompanyID = HRMS_SalaryHeader.CompanyID
AND PeriodID = HRMS_SalaryHeader.PeriodID AND PersonID = HRMS_SalaryHeader.PersonID
AND (FromDate <= @FromDate AND ToDate >= @ToDate))
,0) AS OvertimeHours
FROM HRMS_Set_Employee 
  
    INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
    AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID 
   And HRMS_SalaryHeader.Status=1

INNER JOIN SET_Department ON SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
   AND SET_Department.Status = 1 AND SET_Department.IsActive = 1

   INNER JOIN SET_Section ON SET_Section.SectionID = HRMS_Set_Employee.FK_SectionID
   AND SET_Section.Status = 1 AND SET_Section.IsActive = 1

inner join AR_SET_BankBranch on 
AR_SET_BankBranch.BankBranchID =  HRMS_Set_Employee.FK_BankBranchID
and AR_SET_BankBranch.status =1 

left JOIN AR_SET_Bank on
HRMS_SET_Employee.FK_BankAccountID = AR_SET_Bank.BankID and 
AR_SET_Bank.Status = 1

Left Join SET_JobDesignation HRMS_JD
	ON  HRMS_JD.JobDesignationID=HRMS_Set_Employee.FK_JobDesignationID
	AND HRMS_JD.Status=1
   
    where  HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
    and HRMS_SalaryHeader.CompanyID =@CompanyID and HRMS_SalaryHeader.PeriodID =@PeriodID
    and  HRMS_Set_Employee.IsActive = 1
    AND AR_SET_Bank.BankName like '%CASH%'

 {0}

Order by Cast(HRMS_Set_Employee.EmployeeCode as int) asc
";


        const string Query_EmployeeLoanRegisterSummary = @"
  select HRMS_Set_Employee.PersonID
  ,HRMS_Set_Employee.FullName
  ,HRMS_Set_Employee.EmployeeCode
  ,isnull(sum(HRMS_LoanPaymentDetail.LoanDisburstAmount),0) as InstallmentAmount
  ,isnull(HRMS_LoanSanctionDetail.LoanAmount,0) as LoanAmount
  , isnull(HRMS_LoanSanctionDetail.LoanAmount,0) - SUM(isnull(HRMS_LoanPaymentDetail.LoanDisburstAmount,0)) as Balance
  from HRMS_Set_Employee 
  inner join HRMS_LoanSanctionHeader on HRMS_LoanSanctionHeader.PersonID =  HRMS_Set_Employee.PersonID
  and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
  and HRMS_LoanSanctionHeader.Status =1
  and HRMS_Set_Employee.Status =1 
  
  inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID
  and HRMS_LoanSanctionDetail.PeriodID = HRMS_LoanSanctionHeader.PeriodID 
  and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
  --and HRMS_LoanSanctionDetail.Status = 1
  	INNER JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
  left join HRMS_LoanPaymentDetail on HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
  and HRMS_LoanPaymentDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
  and HRMS_LoanPaymentDetail.Status = 1
        
  WHERE HRMS_LoanSanctionDetail.LoanStartDate between @FromDate and @ToDate 
  and HRMS_LoanSanctionHeader.CompanyID =  @CompanyID
  and HRMS_LoanSanctionHeader.PeriodID = @PeriodID  
  {0}
  Group by HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName,HRMS_Set_Employee.PersonID,HRMS_LoanSanctionDetail.LoanAmount
,HRMS_PayrollType.Description
";
        const string Query_EmployeeEOBIReport = @"
select 
SET_grade.GradeName,
SET_Department.DepartmentName,
Set_Designation.DesignationName,
 [HRMS_SalaryDetail].CreateDate,
 [HRMS_SalaryDetail].Amount,
 HRMS_SET_Employee.FullName,
 HRMS_SET_Employee.EmployeeCode,
 HRMS_SET_Employee.PersonID
 
  from HRMS_SET_Employee 
inner join [HRMS_SalaryHeader] on [HRMS_SalaryHeader].personid =   HRMS_SET_Employee.personid
inner join [HRMS_SalaryDetail] on [HRMS_SalaryDetail].SalaryHeaderID = [HRMS_SalaryHeader].SalaryHeaderID
left join Set_Designation on Set_Designation.DesignationID =  HRMS_SET_Employee.FK_JobDesignationID
and Set_Designation.CompanyID= HRMS_SET_Employee.CompanyID
and Set_Designation.Status =1 
and Set_Designation.IsActive = 1
left join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
and SET_Department.Status = 1
and SET_Department.Isactive = 1
inner join Set_Grade on Set_Grade.GradeID = HRMS_SET_Employee.FK_GradeID
        and Set_Grade.CompanyID = HRMS_SET_Employee.CompanyID
        and Set_Grade.status =1
        inner join HRMS_Payrolltype on HRMS_Payrolltype.PayrollTypeID = HRMS_Payrolltype.PayrollTypeID
        and  HRMS_Payrolltype.CompanyID = HRMS_SET_Employee.CompanyID
        and   HRMS_Payrolltype.Description  like  '%EOBI%' 
        
WHERE HRMS_SET_Employee.IsEOBIMember= 1
and HRMS_SET_Employee.CompanyID = @CompanyID
and [HRMS_SalaryDetail].CreateDate between @FromDate and @ToDate  
{0}
";
        const string Query_StrenghtandSalary = @"Declare @IsActive int=1;
select 

fintab.PersonID,
 fintab.FullName
,fintab.FatherName
,fintab.EmployeeCode
,fintab.EmployGrade
, fintab.GrossSalary ,
sum(fintab.BasicSalary)  BasicSalary ,
sum(fintab.HomeAllowance) HomeAllowance,
sum(fintab.ConveyanceAllowance) ConvAllowance,
sum(fintab.MedicalAllowance) mediAllowance,
SUM(fintab.SAA) SAAAllowance,
SUM(fintab.Special) SpecialAllowance,
SUM(fintab.Utility) UtilityAllowance
,fintab.DepartmentName 
,fintab.JobDesignationName 
,EmployeeStatus
 from

( 
Select 
TAB.PersonID,HRMS_Set_Employee.FullName
,HRMS_Set_Employee.FatherName
,HRMS_SET_Employee.EmployeeCode
,HRMS_Set_Employee.BasicSalary as GrossSalary
,SET_Grade.GradeName as EmployGrade


,case TAB.[PayrollTypeID] when 2 then TAB.CalculatedAmount 
else 0 end as BasicSalary

,case TAB.[PayrollTypeID] when 3 then TAB.CalculatedAmount 
else 0 end as HomeAllowance

,case TAB.[PayrollTypeID] when 4 then TAB.CalculatedAmount 
else 0 end as ConveyanceAllowance
--new
,case TAB.[PayrollTypeID] when 5 then TAB.CalculatedAmount 
else 0 end as MedicalAllowance
,case TAB.[PayrollTypeID] when 7 then TAB.CalculatedAmount 
else 0 end as SAA
,case TAB.[PayrollTypeID] when 8 then TAB.CalculatedAmount 
else 0 end as Special
,case TAB.[PayrollTypeID] when 15 then TAB.CalculatedAmount 
else 0 end as Utility

,SET_Department.DepartmentName
,SET_JobDesignation.JobDesignationName
,HRMS_Employement_Status.Description as EmployeeStatus
from (

SELECT 

		[HRMS_EmployeePayroll].[CompanyID], [HRMS_EmployeePayroll].[PeriodID] ,[EmployeePayrollID] ,[PersonID] ,
	    [HRMS_EmployeePayroll].[PayrollTypeID],[HRMS_EmployeePayroll].[PayrollTypeCode] ,
       [HRMS_EmployeePayroll].[Formula],[HRMS_EmployeePayroll].[IsOneTime],[HRMS_EmployeePayroll].[IsPaid],[LeaveDays]
		,IsNull(CalculatedAmount,0) CalculatedAmount ,IsNull(ManualAmount,0) ManualAmount 
		,Case Impact 
		When 1 Then 'Positive'
		When 2 Then 'Negative'
		When 3 Then 'No Impact'
		END 'Impact'
		,'False' IsIncomeTax
		,HRMS_PayrollType.IsTaxable
		,HRMS_PayrollType.IsPayable
		, IsNull([HRMS_EmployeePayroll].CalculatedAmount,0) + IsNull([HRMS_EmployeePayroll].ManualAmount,0) as SBAmount 
	FROM [HRMS_EmployeePayroll] 
		JOIN HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = [HRMS_EmployeePayroll].PayrollTypeID
		AND HRMS_PayrollType.PayrollTypeCode = [HRMS_EmployeePayroll].PayrollTypeCode
		--AND HRMS_PayrollType.CompanyID = [HRMS_EmployeePayroll].CompanyID
		AND HRMS_PayrollType.Status = 1 AND [HRMS_EmployeePayroll].PayrollTypeCode <> 5
	WHERE [HRMS_EmployeePayroll].[Status] = 1 AND 
		([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null) AND 
		([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) AND 
		([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) AND
		([HRMS_EmployeePayroll].[IsOneTime] is null or [HRMS_EmployeePayroll].[IsOneTime] = '1')

UNION ALL
																											
	SELECT
		[HRMS_GradeWiseEmolumentHeader].[CompanyID] 
		,[HRMS_GradeWiseEmolumentHeader].[PeriodID] 
		,[HRMS_GradeWiseEmolumentHeader].[GradeWiseEmolumentHeaderID] as EmployeePayrollID
		,HRMS_SET_Employee.[PersonID] 
		,HRMS_PayrollType.[PayrollTypeID] 
		,HRMS_PayrollType.[PayrollTypeCode] 
		,[HRMS_GradeWiseEmolumentDetail].[Formula]
		,null as [IsOneTime]
		,null as [IsPaid]
		,null as [LeaveDays] 
		,IsNull([HRMS_GradeWiseEmolumentDetail].[Amount],0) CalculatedAmount 
		,0 ManualAmount 
		,Case [HRMS_PayrollType].[Impact] 
		When 1 Then 'Positive'
		When 2 Then 'Negative'
		When 3 Then 'No Impact'
		END 'Impact'
		,'False' IsIncomeTax
		,HRMS_PayrollType.IsTaxable
		,HRMS_PayrollType.IsPayable
		, [HRMS_GradeWiseEmolumentDetail].[Amount] as SBAmount 
	FROM
	[HRMS_GradeWiseEmolumentHeader]

		inner JOIN [HRMS_GradeWiseEmolumentDetail] ON
		[HRMS_GradeWiseEmolumentHeader].[GradeWiseEmolumentHeaderID] = [HRMS_GradeWiseEmolumentDetail].[GradeWiseEmolumentHeaderID] AND
		[HRMS_GradeWiseEmolumentHeader].[CompanyID] = [HRMS_GradeWiseEmolumentDetail].[CompanyID] AND
		[HRMS_GradeWiseEmolumentHeader].[PeriodID] = [HRMS_GradeWiseEmolumentDetail].[PeriodID] AND
		[HRMS_GradeWiseEmolumentDetail].[Status] = 1

		JOIN HRMS_PayrollType ON 
		[HRMS_GradeWiseEmolumentDetail].PayrollTypeID = HRMS_PayrollType.PayrollTypeID
		--AND [HRMS_GradeWiseEmolumentDetail].CompanyID = HRMS_PayrollType.CompanyID
		AND HRMS_PayrollType.Status = 1 

		JOIN HRMS_SET_Employee on
		[HRMS_GradeWiseEmolumentHeader].[GradeID]=[HRMS_SET_Employee].[FK_GradeID] AND
		[HRMS_GradeWiseEmolumentHeader].[CompanyID]=[HRMS_SET_Employee].[CompanyID] AND
		[HRMS_SET_Employee].[Status] = 1

	where 
		[HRMS_GradeWiseEmolumentHeader].[Status] = 1 AND 
		([HRMS_GradeWiseEmolumentHeader].[IsActive] = @IsActive OR @IsActive is null) AND 
		([HRMS_GradeWiseEmolumentHeader].[CompanyID] = @CompanyID OR @CompanyID is null) AND 
		([HRMS_GradeWiseEmolumentHeader].[PeriodID] = @PeriodID OR @PeriodID is null)  
           
) TAB

	inner join HRMS_Set_Employee
	on HRMS_Set_Employee.[PersonID] = TAB.[PersonID] AND
	   HRMS_Set_Employee.[Status] = 1 AND (HRMS_Set_Employee.[IsActive] = @IsActive OR @IsActive is null) AND 
	  (HRMS_Set_Employee.[CompanyID] = @CompanyID OR @CompanyID is null) 

	  inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
	  
	left join SET_Department on SET_Department.DepartmentID =  HRMS_SET_Employee.FK_DepartmentID
		and SET_Department.Status =1 
	left join SET_JobDesignation on SET_JobDesignation.JobDesignationID =  HRMS_SET_Employee.FK_JobDesignationID
	inner join SET_Grade on SET_Grade.GradeID =HRMS_SET_Employee.FK_GradeID
		and SET_JobDesignation.Status =1 

   where TAB.[PayrollTypeCode]=1   

 {0}
       
 ) fintab
 
 
 group by
 fintab.PersonID,
 fintab.FullName
,fintab.FatherName
,fintab.EmployeeCode
, fintab.GrossSalary,fintab.DepartmentName
,fintab.JobDesignationName
,fintab.EmployGrade
,EmployeeStatus 
 order by fintab.PersonID ";

        const string Query_GrandSummaryReport = @"
	select 
tab.Allowances
	,tab.Deductions
	, tab.Contributions
	,tab.Leaves
	,tab.Loan
,tab.Description
	,tab.PayrollTypeCode
	,sum(tab.Amount) as Amount
	 ,sum(tab.NetAmount) as NetAmount
,(
	 select sum(Amount) from HRMS_SalaryDetail
	inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID
		and HRMS_PayrollType.CompanyID =  HRMS_SalaryDetail.CompanyID
		and HRMS_PayrollType.Status =1 
		and HRMS_SalaryDetail.PayrollTypeCode =  tab.PayrollTypeCode
		group by HRMS_SalaryDetail.PayrollTypeCode
	 ) as TotalforPayrollType	  

from
	 (
	select 

		(case HRMS_SalaryDetail.PayrollTypeCode 
		when 1 then HRMS_PayrollType.Description
		else null end) as Allowances

		,(case HRMS_SalaryDetail.PayrollTypeCode 
		when 2 then HRMS_PayrollType.Description
		else null end) as Deductions

		,(case HRMS_SalaryDetail.PayrollTypeCode 
		when 3 then HRMS_PayrollType.Description
		else null end) as Contributions

		,(case HRMS_SalaryDetail.PayrollTypeCode 
		when 4 then HRMS_PayrollType.Description
		else null end) as Leaves

		,(case HRMS_SalaryDetail.PayrollTypeCode 
		when 5 then HRMS_PayrollType.Description
		else null end) as Loan

		,HRMS_PayrollType.Description
		,HRMS_SalaryDetail.PayrollTypeCode, sum(Amount) as Amount 
	,HRMS_Salaryheader.NetAmount
	,HRMS_Salaryheader.SalaryDate
	,HRMS_SalaryDetail.CompanyID
	,HRMS_SalaryDetail.PeriodID
	from HRMS_SalaryDetail
	inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID
		and HRMS_PayrollType.CompanyID =  HRMS_SalaryDetail.CompanyID
		and HRMS_PayrollType.Status =1 

	inner join HRMS_Salaryheader on HRMS_Salaryheader.SalaryHeaderID =  HRMS_SalaryDetail.SalaryHeaderID
		and HRMS_Salaryheader.CompanyID = HRMS_Salarydetail.CompanyID
		and HRMS_Salaryheader.PeriodID = HRMS_Salarydetail.PeriodID
		and HRMS_Salaryheader.Status =1 

where HRMS_SalaryDetail.CompanyID = @CompanyID
	 and HRMS_SalaryDetail.PeriodID =  @PeriodID

	group by  HRMS_PayrollType.Description,HRMS_SalaryDetail.PayrollTypeCode ,HRMS_Salaryheader.NetAmount,HRMS_Salaryheader.SalaryDate
,HRMS_SalaryDetail.CompanyID
	,HRMS_SalaryDetail.PeriodID
,HRMS_PayrollType.Description


) tab
where tab.SalaryDate between @FromDate and @ToDate
group by 
tab.Allowances
	,tab.Deductions
	, tab.Contributions
	,tab.Leaves
	,tab.Loan
	,tab.PayrollTypeCode
    ,tab.Description

order by tab.PayrollTypeCode

";
        const string Query_GetEmployeeBonusReport = @"
Select HRMS_BonusDetail.BonusHeaderID,HRMS_BonusDetail.EmployeeID,HRMS_BonusDetail.BonusDetailID,HRMS_BonusDetail.CompanyID
,HRMS_BonusDetail.PeriodID,GrossSalary,HRMS_BonusDetail.BasicSalary,HRMS_BonusDetail.BonusAmount as Bonus,HRMS_BonusHeader.NoOfDays as NoOfDays,HRMS_BonusDetail.BonusAbsent
,HRMS_BonusDetail.CreateDate
,HRMS_BonusDetail.CreatedBy
,HRMS_BonusDetail.UpdateDate
,HRMS_BonusDetail.UpdatedBy
,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.JoiningDate
,HRMS_BonusDetail.DesignationID
,SET_Designation.DesignationName
, SET_Department.DepartmentID
,SET_Department.DepartmentName
,HRMS_BonusDetail.Eligibility
,HRMS_BonusDetail.InComeTax,
HRMS_BonusDetail.NetBonusAmount,
HRMS_Employement_Status.Description as EmployeeStatusName
,HRMS_Set_Employee.EmployeeCode
,HRMS_Set_Employee.FullName
,HRMS_BonusDetail.NoOfDays as PresentDays
,HRMS_BonusDetail.ArrearOfBonus,
HRMS_BonusDetail.DeductionOfBonus
,HRMS_BonusDetail.AdvanceSalary
,SET_Business.BusinessName +'-'+ SET_OperatingUnit.OperatingUnitName as CompanyName

FROM HRMS_BonusDetail  
Left Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID =   HRMS_BonusDetail.BonusHeaderID
And HRMS_BonusHeader.CompanyID =   HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID =   HRMS_BonusDetail.PeriodID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
and HRMS_Set_Employee.CompanyID = HRMS_BonusHeader.CompanyID
LEFT join SET_Designation on SET_Designation.DesignationID = HRMS_BonusDetail.DesignationID
LEFT join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
Left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_BonusHeader.EmployStatusID
			And HRMS_Employement_Status.Status=1
			Inner Join Set_Company on Set_Company.CompanyId=HRMS_BonusDetail.CompanyID
			--Inner Join SET_Business on SET_Business.BusinessId=  Set_Company.CompanyID
left Join SET_Business on SET_Business.IsActive =  1
			inner Join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId= Set_Company.OperatingUnitId
Where HRMS_BonusDetail.Status=1 and HRMS_BonusHeader.MillStartDate = @DateFrom and HRMS_BonusHeader.CurrentManulDate = @DateTo
  and HRMS_BonusDetail.CompanyID  = @CompanyID
  and HRMS_BonusDetail.PeriodID  = @PeriodID
  and HRMS_BonusHeader.FiscalID  = @FiscalID
{0}

";
        const string Query_GetEmployeeBonusReport1 = @"

Select HRMS_BonusDetail.BonusHeaderID,HRMS_BonusDetail.EmployeeID,HRMS_BonusDetail.BonusDetailID,HRMS_BonusDetail.CompanyID
,HRMS_BonusDetail.PeriodID,GrossSalary,HRMS_BonusDetail.BasicSalary,HRMS_BonusDetail.BonusAmount as Bonus,HRMS_BonusHeader.NoOfDays as NoOfDays,HRMS_BonusDetail.BonusAbsent
,HRMS_BonusDetail.CreateDate
,HRMS_BonusDetail.CreatedBy
,HRMS_BonusDetail.UpdateDate
,HRMS_BonusDetail.UpdatedBy
,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.JoiningDate
,HRMS_BonusDetail.DesignationID
,SET_Designation.DesignationName
, SET_Department.DepartmentID
,SET_Department.DepartmentName
,HRMS_BonusDetail.Eligibility
,HRMS_BonusDetail.InComeTax,
HRMS_BonusDetail.NetBonusAmount,
HRMS_Employement_Status.Description as EmployeeStatusName
,HRMS_Set_Employee.EmployeeCode
,HRMS_Set_Employee.FullName
,HRMS_BonusDetail.NoOfDays as PresentDays
,HRMS_BonusDetail.ArrearOfBonus,
HRMS_BonusDetail.DeductionOfBonus
,HRMS_BonusDetail.AdvanceSalary
,SET_Business.BusinessName +'-'+ SET_OperatingUnit.OperatingUnitName as CompanyName
,HRMS_BonusDetail.CreatedByValue as UserName
FROM HRMS_BonusDetail  
Left Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID =   HRMS_BonusDetail.BonusHeaderID
And HRMS_BonusHeader.CompanyID =   HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID =   HRMS_BonusDetail.PeriodID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
and HRMS_Set_Employee.CompanyID = HRMS_BonusHeader.CompanyID
LEFT join SET_Designation on SET_Designation.DesignationID = HRMS_BonusDetail.DesignationID
LEFT join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
Left Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_BonusHeader.EmployStatusID
			And HRMS_Employement_Status.Status=1
			Inner Join Set_Company on Set_Company.CompanyId=HRMS_BonusDetail.CompanyID
			--Inner Join SET_Business on SET_Business.BusinessId=  Set_Company.CompanyID
left Join SET_Business on SET_Business.IsActive =  1
			inner Join SET_OperatingUnit on SET_OperatingUnit.OperatingUnitId= Set_Company.OperatingUnitId
Where HRMS_BonusDetail.Status=1 and HRMS_BonusHeader.MillStartDate = @DateFrom and HRMS_BonusHeader.CurrentManulDate = @DateTo
  and HRMS_BonusDetail.CompanyID  = @CompanyID
  and HRMS_BonusDetail.PeriodID  = @PeriodID
  and HRMS_BonusHeader.FiscalID  = @FiscalID
{0}

";

        const string Query_GetEmployeeEobi = @"

select 

            HRMS_SalaryHeader.PersonID
            ,HRMS_SET_Employee.FullName
            ,HRMS_SET_Employee.FatherName
            ,HRMS_SET_Employee.EmployeeCode
            ,HRMS_SalaryHeader.SalaryDate
            ,DATENAME(month ,  HRMS_SalaryHeader.SalaryDate) as Month
            ,DATENAME(year ,  HRMS_SalaryHeader.SalaryDate) as Year
            ,HRMS_SalaryHeader.SalaryDate
            ,HRMS_PayrollType.Description as Contribution
            ,HRMS_SalaryDetail.Amount
            ,SET_JobDesignation.JobDesignationName as JobDesignation

 from HRMS_SalaryHeader

inner join  HRMS_SalaryDetail on  HRMS_SalaryDetail.SalaryHeaderID  = HRMS_SalaryHeader.SalaryHeaderID
        and HRMS_SalaryDetail.CompanyID  = HRMS_SalaryHeader.CompanyID
        and HRMS_SalaryDetail.PeriodID  = HRMS_SalaryHeader.PeriodID
        --and HRMS_SalaryDetail.Status =1 

inner join HRMS_PayrollType on  HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
        and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
       -- and HRMS_PayrollType.CompanyID = HRMS_SalaryDetail.CompanyID
        and HRMS_PayrollType.Status =1 

inner join HRMS_SET_Employee on HRMS_SET_Employee.PersonID =  HRMS_SalaryHeader.PersonID
        and HRMS_SET_Employee.CompanyID  = HRMS_SalaryHeader.CompanyID
        and HRMS_SET_Employee.Status =1 

left join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_SET_Employee.FK_JobDesignationID
        and  SET_JobDesignation.Status =1 

where  HRMS_SalaryDetail.PayrollTypeCode = 3
         and HRMS_SalaryHeader.Status = 1
        and isnull(HRMS_Set_Employee.IsEOBIMember,0) = 1
       and  HRMS_SalaryHeader.CompanyID  = @CompanyID
        and HRMS_SalaryHeader.PeriodID  = @PeriodID
		and hrms_salaryheader.salarydate >= @FromDate
	  and hrms_salaryheader.salarydate <= @ToDate
  {0}

";
        const string QueryFOrSearching_Detail = @"SELECT convert(bit,'False') as IsChecked,HRMS_SalaryHeader.[CompanyID], HRMS_SalaryHeader.[PeriodID],EmployeeCode,[SalaryHeaderID],HRMS_Set_Employee.PersonID,HRMS_Set_Employee.FullName,[SalaryDate], [SalaryType], [GrossAmount]
 ,[DeductionAmount], [NetAmount],[GrossAmountTaxable],HRMS_SalaryHeader. [Status],[DaysWorking], [DaysAbsent], [DaysLWP], [DaysLate], [DaysEarly], [DaysTotal]
 ,[TaxAmount], [ContributionEmployer], [OpeningID], [PostID]
 ,[PostBy], [PostDate], [PostByValue], [PaymentID], ROW_NUMBER() Over (Order by HRMS_SalaryHeader.CompanyID, HRMS_SalaryHeader.PeriodID, HRMS_SalaryHeader.PersonID) as 'Row'  
 FROM [HRMS_SalaryHeader] 
 inner join HRMS_Set_Employee 
 ON HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
 AND HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonId
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
";

        const string QueryFOrSearching_Detail_WithoutPaging = @"SELECT convert(bit,'False') as IsChecked,HRMS_SalaryHeader.[CompanyID], HRMS_SalaryHeader.[PeriodID],EmployeeCode,[SalaryHeaderID],HRMS_Set_Employee.PersonID,HRMS_Set_Employee.FullName,[SalaryDate], [SalaryType], [GrossAmount]
 ,[DeductionAmount], [NetAmount],[GrossAmountTaxable],HRMS_SalaryHeader. [Status],[DaysWorking], [DaysAbsent], [DaysLWP], [DaysLate], [DaysEarly], [DaysTotal]
 ,[TaxAmount], [ContributionEmployer], [OpeningID], [PostID]
 ,[PostBy], [PostDate], [PostByValue], [PaymentID] 
 FROM [HRMS_SalaryHeader] 
 inner join HRMS_Set_Employee 
 ON HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
 AND HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonId
Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]

";


        const string Query_GetEmployeeBonusReportSummary = @"

Select Fintab.CompanyID,
Fintab.PeriodID,
Sum(Fintab.BasicSalary) as BasicSalary,
Sum(Fintab.Bonus)as Bonus,
Sum(Fintab.BonusAbsent) as BonusAbsent,
Fintab.DepartmentID,
Fintab.DepartmentName,
Sum(Fintab.InComeTax) as InComeTax,
Sum(Fintab.ArrearOfBonus) as ArrearOfBonus,
Sum(Fintab.DeductionOfBonus) as DeductionOfBonus,
Sum(Fintab.AdvanceSalary) as AdvanceSalary,
Sum(Fintab.NetBonusAmount) as NetBonusAmount 
,Fintab.CostCenterName 
,Fintab.CostCenterID
from ( 
Select HRMS_BonusDetail.BonusHeaderID,
HRMS_BonusDetail.EmployeeID,
HRMS_BonusDetail.CompanyID
,HRMS_BonusDetail.PeriodID,
HRMS_BonusDetail.BasicSalary,
HRMS_BonusDetail.BonusAmount as Bonus,
HRMS_BonusDetail.BonusAbsent
, SET_Department.DepartmentID
,SET_Department.DepartmentName
,HRMS_BonusDetail.InComeTax,
HRMS_BonusDetail.NetBonusAmount
,HRMS_BonusDetail.ArrearOfBonus,
HRMS_BonusDetail.DeductionOfBonus
,HRMS_BonusDetail.AdvanceSalary,
SET_CostCenter.CostCenterID
,SET_CostCenter.CostCenterName
,HRMS_BonusDetail.CreatedByValue as UserName
FROM HRMS_BonusDetail  
Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID =   HRMS_BonusDetail.BonusHeaderID
 And HRMS_BonusHeader.CompanyID =   HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID =   HRMS_BonusDetail.PeriodID
And HRMS_BonusHeader.Status=1
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
And HRMS_Set_Employee.CompanyID=HRMS_BonusDetail.CompanyID
And HRMS_Set_Employee.Status = 1
Inner join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
AND SET_Department.Status=1
inner join SET_CostCenter on  SET_CostCenter.CostCenterID=HRMS_Set_Employee.CostCenterID
Where HRMS_BonusDetail.Status=1
AND HRMS_BonusDetail.CompanyID=@CompanyID
AND HRMS_BonusDetail.PeriodID=@PeriodID
AND (SET_Department.DepartmentID=@DepartmentID or @DepartmentID is null)
AND (SET_CostCenter.CostCenterID=@CostCenterID or @CostCenterID is null)
AND HRMS_BonusHeader.MillStartDate =  @MillStartDate 
And HRMS_BonusHeader.CurrentManulDate = @CurrentManulDate
AND HRMS_BonusHeader.FiscalID = @CurrentFiscalID
{0}
)Fintab
Group By
Fintab.CompanyID
,Fintab.PeriodID,
 Fintab.DepartmentID
,Fintab.DepartmentName
,Fintab.CostCenterName,Fintab.CostCenterID
Order By  Fintab.DepartmentID
";


        const string Query_EmployeeSalarySheet_ReportDptWiseData = @"



";

        const string Query_EmployeeSalarySheet_ReportDptWiseDataNew = @"
Select tempEmpCount.NoOFEmployees,* From (
select
case 
when TAB.PayrollTypeCode = 2 then (210+PayrollTypeId) when TAB.PayrollTypeCode = 3 then (350 + PayrollTypeId)
when TAB.PayrollTypeCode = 5 then (258+PayrollTypeId) 
when TAB.DeductionType = 'Supply Days' and TAB.PayrollTypeCode = 1 then (202)
when TAB.PayrollTypeCode = 1 then (150+ TAB.PayrollTypeId)
end as SortCategory,Round(tab.GrossSalary,0) as GrossSalary,tab.MasterSalary,tab.PeriodID,tab.CompanyID,tab.PersonID, tab.EmployeeCode,tab.PayrollTypeID,tab.PayrollTypeCode, tab.FullName,tab.JobDesignationID,tab.JobDesignationName,tab.DepartmentID,tab.DepartmentName,tab.LocationID,tab.LocationName,
 tab.DeductionType as Description, '' as Description2, Round(tab.Amount,0) as Allowances,0 as Deduction, tab.GrossAmount,tab.DeductionAmount,tab.NetAmount,tab.DaysTotal,tab.DaysWorking,DENSE_RANK() OVER( ORDER BY EmployeeCode) AS SeriaLNo
,isnull(tab.paidbybankForAllowances,0) as paidbybankForAllowances,isnull(tab.paidbybankForDeductions,0) as paidbybankForDeductions,
 isnull(tab.paidbycashForAllowances,0) as paidbycashForAllowances,isnull(tab.paidbycashForDeductions,0) as paidbycashForDeductions

 from (

select 
DISTINCT HRMS_SH.MasterSalary,HRMS_SH.GrossAmount as GrossSalary,HRMS_SD.PeriodID,HRMS_SD.CompanyID,HRMS_SD.SalaryDetailID,CAST(HRMS_SE.EmployeeCode as varchar) as EmployeeCode,HRMS_SE.FullName,
HRMS_JD.JobDesignationID,HRMS_JD.JobDesignationName,HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,HRMS_Loc.LocationID,HRMS_Loc.LocationName, HRMS_SD.PayrollTypeID,HRMS_SD.PayrollTypeCode,
HRMS_PT.Description as DeductionType,Round(HRMS_SD.Amount,0) as Amount,HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking,
0 as GrossAmount ,0 as NetAmount,0 as DeductionAmount
,(select sum(Amount) from HRMS_SalaryDetail HRMS_SalaryDetailinner
inner join HRMS_SalaryHeader HRMS_SalaryHeaderinner on HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SalaryDetailinner.SalaryHeaderID
and HRMS_SalaryHeaderinner.CompanyID = HRMS_SalaryDetailinner.CompanyID
and HRMS_SalaryHeaderinner.PeriodID = HRMS_SalaryDetailinner.PeriodID
and HRMS_SalaryHeaderinner.Status = 1
inner join HRMS_Set_Employee HRMS_Set_Employeeinner on HRMS_Set_Employeeinner.PersonID = HRMS_SalaryHeaderinner.PersonID
and HRMS_Set_Employeeinner.CompanyID = HRMS_SalaryHeaderinner.CompanyID
and HRMS_Set_Employeeinner.Status =1 
Inner Join AR_Set_BankBranch AR_Set_BankBranchinner  on AR_Set_BankBranchinner.BankBranchID=HRMS_Set_Employeeinner.FK_BankBranchID
		Inner Join AR_Set_Bank AR_Set_Bankinner  on AR_Set_Bankinner.BankID=AR_Set_BankBranchinner.BankID
		and AR_Set_Bankinner.BankName not like '%cash%'
where HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SH.SalaryHeaderID
and HRMS_SalaryDetailinner.SalaryDetailID = HRMS_SD.SalaryDetailID) paidbybankForAllowances
 ,0 as paidbybankForDeductions
 ,(select sum(Amount) from HRMS_SalaryDetail HRMS_SalaryDetailinner
inner join HRMS_SalaryHeader HRMS_SalaryHeaderinner on HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SalaryDetailinner.SalaryHeaderID
and HRMS_SalaryHeaderinner.CompanyID = HRMS_SalaryDetailinner.CompanyID
and HRMS_SalaryHeaderinner.PeriodID = HRMS_SalaryDetailinner.PeriodID
and HRMS_SalaryHeaderinner.Status = 1
inner join HRMS_Set_Employee HRMS_Set_Employeeinner on HRMS_Set_Employeeinner.PersonID = HRMS_SalaryHeaderinner.PersonID
and HRMS_Set_Employeeinner.CompanyID = HRMS_SalaryHeaderinner.CompanyID
and HRMS_Set_Employeeinner.Status =1 
Inner Join AR_Set_BankBranch AR_Set_BankBranchinner  on AR_Set_BankBranchinner.BankBranchID=HRMS_Set_Employeeinner.FK_BankBranchID
		Inner Join AR_Set_Bank AR_Set_Bankinner  on AR_Set_Bankinner.BankID=AR_Set_BankBranchinner.BankID
		and AR_Set_Bankinner.BankName  like '%cash%'
where HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SH.SalaryHeaderID
and HRMS_SalaryDetailinner.SalaryDetailID = HRMS_SD.SalaryDetailID) paidbycashForAllowances
 ,0 as paidbycashForDeductions
from HRMS_SalaryDetail HRMS_SD 

Inner join HRMS_SalaryHeader HRMS_SH 
	on HRMS_SD.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SD.SalaryHeaderID=HRMS_SH.SalaryHeaderID
	And HRMS_SH.Status=1
Inner join HRMS_SET_Employee HRMS_SE
	ON HRMS_SE.PersonID=HRMS_SH.PersonID
	AND HRMS_SE.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SE.Status=1 AND HRMS_SE.IsActive=1

Left Join SET_JobDesignation HRMS_JD
	ON HRMS_JD.JobDesignationID=HRMS_SE.FK_JobDesignationID
	AND HRMS_JD.Status=1
	
Inner JOin SET_Department HRMS_DP
	ON HRMS_DP.DepartmentID=HRMS_SE.FK_DepartmentID
	AND HRMS_DP.Status=1

	Inner JOin SET_Location HRMS_Loc
	ON HRMS_Loc.LocationID=HRMS_SE.FK_LocationID
	AND HRMS_Loc.Status=1
	
left Join HRMS_EmployeePayroll HRMS_EP
	ON HRMS_EP.PayrollTypeCode=HRMS_SD.PayrollTypeCode
	AND HRMS_EP.PayrollTypeID=HRMS_SD.PayrollTypeID
	AND HRMS_EP.PersonID=HRMS_SH.PersonID
	AND HRMS_EP.Status=1
	
INNER JOIN HRMS_PayrollType HRMS_PT on
HRMS_SD.PayrollTypeID = HRMS_PT.PayrollTypeID AND
HRMS_SD.PayrollTypeCode = HRMS_PT.PayrollTypeCode 
AND HRMS_PT.Status = 1 and HRMS_PT.PayrollTypeCode <> 4 AND HRMS_PT.IsVisiable=1

WHERE	
	DATENAME(month, HRMS_SH.SalaryDate) = @Month AND
DATEPART(Year,HRMS_SH.SalaryDate) = @Year AND
HRMS_SH.CompanyID = @CompanyID AND 
HRMS_SH.PeriodID = @PeriodID AND 
HRMS_SH.Status = 1
 AND (HRMS_DP.DepartmentID = @DepartmentID OR @DepartmentID is null)
 AND (HRMS_SE.StatusID  = @StatusID OR  @StatusID is null)
 and HRMS_PT.PayrollTypeCode = 1
group by  
SalaryDetailID,HRMS_SE.BasicSalary,HRMS_PT.Description ,HRMS_SD.PayrollTypeID,HRMS_SD.PayrollTypeCode,Amount,HRMS_SH.GrossAmount,
HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SE.FullName,HRMS_SH.GrossAmount,HRMS_SH.DeductionAmount,HRMS_SD.PeriodID,HRMS_SD.CompanyID,
HRMS_SH.NetAmount,HRMS_JD.JobDesignationID,HRMS_JD.JobDesignationName,HRMS_SE.EmployeeCode,HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,HRMS_Loc.LocationID,HRMS_Loc.LocationName
,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking,HRMS_SH.MasterSalary
)TAB
GROUP BY
 TAB.PayrollTypeCode,tab.GrossSalary,tab.PeriodID,tab.CompanyID,tab.PersonID,tab.FullName,tab.PayrollTypeCode,tab.JobDesignationID
 ,tab.JobDesignationName,tab.DepartmentID,tab.DepartmentName,tab.LocationID,tab.LocationName,tab.PayrollTypeID, tab.DeductionType,tab.Amount,tab.EmployeeCode
 ,tab.DeductionAmount,tab.PayrollTypeID,tab.GrossAmount,tab.NetAmount,tab.DaysTotal,tab.DaysWorking,tab.MasterSalary,tab.SalaryDetailID,paidbybankForAllowances,paidbybankForDeductions,
 paidbycashForAllowances,paidbycashForDeductions


 union all

 select
case 
when TAB.PayrollTypeCode = 2 then (210+PayrollTypeId) when TAB.PayrollTypeCode = 3 then (350 + PayrollTypeId)
when TAB.PayrollTypeCode = 5 then (258+PayrollTypeId) 
when TAB.DeductionType = 'Supply Days' and TAB.PayrollTypeCode = 1 then (202)
when TAB.PayrollTypeCode = 1 then (150+ TAB.PayrollTypeId)
end as SortCategory,Round(tab.GrossSalary,0) as GrossSalary,tab.MasterSalary,tab.PeriodID,tab.CompanyID,tab.PersonID, tab.EmployeeCode,tab.PayrollTypeID,tab.PayrollTypeCode, tab.FullName,tab.JobDesignationID,tab.JobDesignationName,tab.DepartmentID,tab.DepartmentName,tab.LocationID,tab.LocationName,
'' as Description,  tab.DeductionType as Description2,0 as Allowances, Round(tab.Amount,0) as Deduction,tab.GrossAmount,tab.DeductionAmount,tab.NetAmount,tab.DaysTotal,tab.DaysWorking,DENSE_RANK() OVER( ORDER BY EmployeeCode) AS SeriaLNo
,isnull(tab.paidbybankForAllowances,0) as paidbybankForAllowances,isnull(tab.paidbybankForDeductions,0) as paidbybankForDeductions,
 isnull(tab.paidbycashForAllowances,0) as paidbycashForAllowances,isnull(tab.paidbycashForDeductions,0) as paidbycashForDeductions
 from (

select 
DISTINCT HRMS_SH.MasterSalary,HRMS_SH.GrossAmount as GrossSalary,HRMS_SD.PeriodID,HRMS_SD.CompanyID,HRMS_SD.SalaryDetailID,CAST(HRMS_SE.EmployeeCode as varchar) as EmployeeCode,HRMS_SE.FullName,
HRMS_JD.JobDesignationID,HRMS_JD.JobDesignationName,HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,HRMS_Loc.LocationID,HRMS_Loc.LocationName, HRMS_SD.PayrollTypeID,HRMS_SD.PayrollTypeCode,
HRMS_PT.Description as DeductionType,Round(HRMS_SD.Amount,0) as Amount,HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking,
0 as GrossAmount ,0 as NetAmount,0 as DeductionAmount
,0 as paidbybankForAllowances
,(select sum(Amount) from HRMS_SalaryDetail HRMS_SalaryDetailinner
inner join HRMS_SalaryHeader HRMS_SalaryHeaderinner on HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SalaryDetailinner.SalaryHeaderID
and HRMS_SalaryHeaderinner.CompanyID = HRMS_SalaryDetailinner.CompanyID
and HRMS_SalaryHeaderinner.PeriodID = HRMS_SalaryDetailinner.PeriodID
and HRMS_SalaryHeaderinner.Status = 1
inner join HRMS_Set_Employee HRMS_Set_Employeeinner on HRMS_Set_Employeeinner.PersonID = HRMS_SalaryHeaderinner.PersonID
and HRMS_Set_Employeeinner.CompanyID = HRMS_SalaryHeaderinner.CompanyID
and HRMS_Set_Employeeinner.Status =1 
Inner Join AR_Set_BankBranch AR_Set_BankBranchinner  on AR_Set_BankBranchinner.BankBranchID=HRMS_Set_Employeeinner.FK_BankBranchID
		Inner Join AR_Set_Bank AR_Set_Bankinner  on AR_Set_Bankinner.BankID=AR_Set_BankBranchinner.BankID
		and AR_Set_Bankinner.BankName not like '%cash%'
where HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SH.SalaryHeaderID
and HRMS_SalaryDetailinner.SalaryDetailID = HRMS_SD.SalaryDetailID) paidbybankForDeductions
 ,0 as paidbycashForAllowances
,(select sum(Amount) from HRMS_SalaryDetail HRMS_SalaryDetailinner
inner join HRMS_SalaryHeader HRMS_SalaryHeaderinner on HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SalaryDetailinner.SalaryHeaderID
and HRMS_SalaryHeaderinner.CompanyID = HRMS_SalaryDetailinner.CompanyID
and HRMS_SalaryHeaderinner.PeriodID = HRMS_SalaryDetailinner.PeriodID
and HRMS_SalaryHeaderinner.Status = 1
inner join HRMS_Set_Employee HRMS_Set_Employeeinner on HRMS_Set_Employeeinner.PersonID = HRMS_SalaryHeaderinner.PersonID
and HRMS_Set_Employeeinner.CompanyID = HRMS_SalaryHeaderinner.CompanyID
and HRMS_Set_Employeeinner.Status =1 
Inner Join AR_Set_BankBranch AR_Set_BankBranchinner  on AR_Set_BankBranchinner.BankBranchID=HRMS_Set_Employeeinner.FK_BankBranchID
		Inner Join AR_Set_Bank AR_Set_Bankinner  on AR_Set_Bankinner.BankID=AR_Set_BankBranchinner.BankID
		and AR_Set_Bankinner.BankName  like '%cash%'
where HRMS_SalaryHeaderinner.SalaryHeaderID = HRMS_SH.SalaryHeaderID
and HRMS_SalaryDetailinner.SalaryDetailID = HRMS_SD.SalaryDetailID) paidbycashForDeductions

from HRMS_SalaryDetail HRMS_SD 

Inner join HRMS_SalaryHeader HRMS_SH 
	on HRMS_SD.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SD.SalaryHeaderID=HRMS_SH.SalaryHeaderID
	And HRMS_SH.Status=1
Inner join HRMS_SET_Employee HRMS_SE
	ON HRMS_SE.PersonID=HRMS_SH.PersonID
	AND HRMS_SE.CompanyID=HRMS_SH.CompanyID
	AND HRMS_SE.Status=1 AND HRMS_SE.IsActive=1

Left Join SET_JobDesignation HRMS_JD
	ON HRMS_JD.JobDesignationID=HRMS_SE.FK_JobDesignationID
	AND HRMS_JD.Status=1
	
Inner JOin SET_Department HRMS_DP
	ON HRMS_DP.DepartmentID=HRMS_SE.FK_DepartmentID
	AND HRMS_DP.Status=1

	Inner JOin SET_Location HRMS_Loc
	ON HRMS_Loc.LocationID=HRMS_SE.FK_LocationID
	AND HRMS_Loc.Status=1
	
left Join HRMS_EmployeePayroll HRMS_EP
	ON HRMS_EP.PayrollTypeCode=HRMS_SD.PayrollTypeCode
	AND HRMS_EP.PayrollTypeID=HRMS_SD.PayrollTypeID
	AND HRMS_EP.PersonID=HRMS_SH.PersonID
	AND HRMS_EP.Status=1
	
INNER JOIN HRMS_PayrollType HRMS_PT on
HRMS_SD.PayrollTypeID = HRMS_PT.PayrollTypeID AND
HRMS_SD.PayrollTypeCode = HRMS_PT.PayrollTypeCode 
AND HRMS_PT.Status = 1 and HRMS_PT.PayrollTypeCode <> 4 AND HRMS_PT.IsVisiable=1

WHERE	
	DATENAME(month, HRMS_SH.SalaryDate) = @Month AND
DATEPART(Year,HRMS_SH.SalaryDate) = @Year AND
HRMS_SH.CompanyID = @CompanyID AND 
HRMS_SH.PeriodID = @PeriodID AND 
HRMS_SH.Status = 1
 AND (HRMS_DP.DepartmentID = @DepartmentID OR @DepartmentID is null)
 AND (HRMS_SE.StatusID  = @StatusID OR  @StatusID is null)
 and HRMS_PT.PayrollTypeCode in (2,5)
group by  
SalaryDetailID,HRMS_SE.BasicSalary,HRMS_PT.Description ,HRMS_SD.PayrollTypeID,HRMS_SD.PayrollTypeCode,Amount,HRMS_SH.GrossAmount,
HRMS_SH.SalaryHeaderID,HRMS_SH.PersonID,HRMS_SE.FullName,HRMS_SH.GrossAmount,HRMS_SH.DeductionAmount,HRMS_SD.PeriodID,HRMS_SD.CompanyID,
HRMS_SH.NetAmount,HRMS_JD.JobDesignationID,HRMS_JD.JobDesignationName,HRMS_SE.EmployeeCode,HRMS_DP.DepartmentID,HRMS_DP.DepartmentName,HRMS_Loc.LocationID,HRMS_Loc.LocationName
,HRMS_SH.DaysTotal,HRMS_SH.DaysWorking,HRMS_SH.MasterSalary
)TAB
GROUP BY
 TAB.PayrollTypeCode,tab.GrossSalary,tab.PeriodID,tab.CompanyID,tab.PersonID,tab.FullName,tab.PayrollTypeCode,tab.JobDesignationID
 ,tab.JobDesignationName,tab.DepartmentID,tab.DepartmentName,tab.LocationID,tab.LocationName,tab.PayrollTypeID, tab.DeductionType,tab.Amount,tab.EmployeeCode
 ,tab.DeductionAmount,tab.PayrollTypeID,tab.GrossAmount,tab.NetAmount,tab.DaysTotal,tab.DaysWorking,tab.MasterSalary,tab.SalaryDetailID,
 paidbybankForAllowances,paidbybankForDeductions,
 paidbycashForAllowances,paidbycashForDeductions

)FintabFinal
		Left Join (Select DepartmentID,Count(HRMS_SalaryHeader.PersonID) as NoOFEmployees from HRMS_SET_Employee 
	Inner Join SET_Department on SET_Department.DepartmentID = HRMS_SET_Employee.FK_DepartmentID
	AND HRMS_SET_Employee.Status=1 
	Inner JOin HRMS_SalaryHeader on HRMS_SalaryHeader.CompanyID=HRMS_SET_Employee.CompanyID
	AND HRMS_SalaryHeader.PersonID = HRMS_SET_Employee.PersonID
		AND HRMS_SalaryHeader.Status=1 AND HRMS_SET_Employee.IsActive=1
		AND	DATENAME(month, HRMS_SalaryHeader.SalaryDate) = @Month AND
DATEPART(Year,HRMS_SalaryHeader.SalaryDate) = @Year
 AND (HRMS_SET_Employee.StatusID  = @StatusID OR  @StatusID is null)
AND (SET_Department.DepartmentID = @DepartmentID OR @DepartmentID is null)
and HRMS_SET_Employee.CompanyID = @CompanyID
	Group By DepartmentID)tempEmpCount on  tempEmpCount.DepartmentID=FintabFinal.DepartmentID
	Order by FintabFinal.PersonID";

        const string Query_InsertPF = @" INSERT INTO {0}.dbo.[HRMS_SalaryHeader] ([CompanyID], [PeriodID], [SalaryHeaderID], [PersonID], [SalaryDate], [SalaryType], [GrossAmount], [DeductionAmount], [NetAmount], [GrossAmountTaxable], [Status], [IsApproved], [CreatedBy], [CreateDate], [CreatedByValue], [DaysWorking], [DaysAbsent], [DaysLWP], [DaysLate], [DaysEarly], [DaysTotal], [TaxAmount], [ContributionEmployer], [OpeningID], [PostID], [PostBy], [PostDate], [PostByValue], [PaymentID], [InvoiceID], [MasterSalary], [PayrollGroupID], [JobDesignationID], [Leaves], [TotalHours], [OTHours], [RatePerDay], [DeductionHours], [RatePerHour], [OTAmount], [DedAmount], [TotalShiftHours], [UpdatedBy], [UpdateDate], [UpdatedByValue])
		VALUES(@CompanyID, @PeriodID, @SalaryHeaderID, @PersonID, @SalaryDate, @SalaryType, @GrossAmount, @DeductionAmount, @NetAmount, @GrossAmountTaxable, @Status, @IsApproved, @CreatedBy, @CreateDate, @CreatedByValue, @DaysWorking, @DaysAbsent, @DaysLWP, @DaysLate, @DaysEarly, @DaysTotal, @TaxAmount, @ContributionEmployer, @OpeningID, @PostID, @PostBy, @PostDate, @PostByValue, @PaymentID, @InvoiceID, @MasterSalary, @PayrollGroupID, @JobDesignationID, @Leaves, @TotalHours, @OTHours, @RatePerDay, @DeductionHours, @RatePerHour, @OTAmount, @DedAmount, @TotalShiftHours, @UpdatedBy, @UpdateDate, @UpdatedByValue) ";

        const string Query_EmployeeBankAdvice = @"
select convert( varchar(20), HRMS_SalaryHeader.SalaryHeaderID  )+'-'+ convert( varchar(20), HRMS_Set_Employee.EmployeeCode )as PaySlipNo
,HRMS_Set_Employee.FatherName
,HRMS_Set_Employee.PersonID
,HRMS_Set_Employee.EmployeeCode
,HRMS_Set_Employee.FullName
,HRMS_Set_Employee.IBanNo
,HRMS_Set_Employee.CNICNumber
--,HRMS_EmployeePayment.ChequeNo
,(select max(ChequeNo)  from HRMS_EmployeePayment HRMS_EmployeePaymentinner
inner join HRMS_Set_Employee HRMS_Set_Employeeinner on  HRMS_Set_Employeeinner.PersonID = HRMS_EmployeePaymentinner.PaymentPersonID
 AND HRMS_Set_Employeeinner.CompanyID = HRMS_EmployeePaymentinner.CompanyID
	AND HRMS_Set_Employeeinner.Status = 1 and HRMS_EmployeePaymentinner.PaymentType = 1
   AND HRMS_EmployeePaymentinner.PaymentMonth BETWEEN  @FromDate  and  @ToDate
 --AND HRMS_EmployeePaymentinner.PaymentDate BETWEEN DATEADD(month, 1, @FromDate)  and DATEADD(month, 1,@ToDate)
 where HRMS_Set_Employee.PersonID  = HRMS_Set_Employeeinner.PersonID
 and HRMS_Set_Employeeinner.PersonID = HRMS_SalaryHeader.PersonID
 ) as ChequeNo
--,case when HRMS_SalaryHeader.NetAmount%10 = 9 then  ROUND(HRMS_SalaryHeader.NetAmount,-1) else HRMS_SalaryHeader.NetAmount end as NetAmount
--,round(HRMS_SalaryHeader.GrossAmount,0) - round(HRMS_SalaryHeader.DeductionAmount,0)as NetAmount
,isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
 and HRMS_SalaryDetail.PayrollTypeCode = 1),0)
  - isnull((select (sum(isnull(HRMS_SalaryDetail.Amount,0)))
 from HRMS_SalaryDetail
where
 HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
 and HRMS_SalaryDetail.Status = 1
 and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
 and HRMS_SalaryDetail.PayrollTypeCode in (2,5)),0)
   as NetAmount
,AR_SET_Bank.BankName
,HRMS_Set_Employee.EmpAccountNo
, AR_SET_BankBranch.BankBranchName
,HRMS_SalaryHeader.SalaryDate
,HRMS_Set_Employee.CreatedByValue as UserName
FROM HRMS_Set_Employee
    INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
    AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
   And HRMS_SalaryHeader.Status=1
right join AR_SET_BankBranch on
AR_SET_BankBranch.BankBranchID =  HRMS_Set_Employee.FK_BankBranchID
and AR_SET_BankBranch.status =1
left JOIN AR_SET_Bank on
HRMS_SET_Employee.FK_BankAccountID = AR_SET_Bank.BankID and
AR_SET_Bank.Status = 1
INNER JOIN SET_EmployeeType ON SET_EmployeeType.TypeID = HRMS_Set_Employee.PersonTypeCode
   AND SET_EmployeeType.Status = 1
--left JOIN HRMS_EmployeePayment ON HRMS_Set_Employee.PersonID = HRMS_EmployeePayment.PaymentPersonID
  --  AND HRMS_Set_Employee.CompanyID = HRMS_EmployeePayment.CompanyID
--	AND HRMS_EmployeePayment.Status = 1 and HRMS_EmployeePayment.PaymentType = 1
  --  AND HRMS_EmployeePayment.PaymentDate BETWEEN @FromDate and @ToDate
    where  HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
    and HRMS_SalaryHeader.CompanyID =@CompanyID and HRMS_SalaryHeader.PeriodID =@PeriodID
    and  HRMS_Set_Employee.IsActive = 1
  and  HRMS_Set_Employee.EmpAccountNo is not null
	and len(trim(HRMS_Set_Employee.EmpAccountNo)) > 1
	and HRMS_SalaryHeader.NetAmount > 0
 {0}
";


        private static string objConnection;

        #endregion

        #region Class Methods

        public static DataTable Get_DirectInvoiceDocument(int companyID, int periodID,Guid CurrentUserID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentUserID", CurrentUserID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DocumentTypeDirectInvoice, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_DirectInvoiceDocumentForAlHaj(int companyID, int periodID)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DocumentTypeDirectInvoiceForALHAJ, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable Get_GrandSummaryReport(int companyID, int periodID, Nullable<DateTime> datefrom, Nullable<DateTime> dateto)
        {
            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GrandSummaryReport, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public DataTable Get_dataforScrollPayment(int companyID, int periodID, Nullable<int> month, Nullable<int> year, Nullable<int> bank, Nullable<int> bankbranch, Nullable<int> EmpStatusID, string Query_getScrollPaymentRecord)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@BankID", bank);
                if (bank.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BankID", bank);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@BankID", DBNull.Value);
                }
                if (bankbranch.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@BankBranchID", bankbranch);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@BankBranchID", DBNull.Value);
                }
                sqlparams[4] = new SqlParameter("@Month", month);
                sqlparams[5] = new SqlParameter("@Year", year);

                if (EmpStatusID.HasValue)
                { sqlparams[6] = new SqlParameter("@StatusID", EmpStatusID); }
                else
                { sqlparams[6] = new SqlParameter("@StatusID", DBNull.Value); }
                //  dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_getScrollPaymentRecord, sqlparams).Tables[0];
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_getScrollPaymentRecord, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_EOBIReport(int companyID, int periodID, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> EMpStatusID = null, Nullable<Guid> UserID = null)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);               

                if (EMpStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMpStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmployeeEobi, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable Get_EmployeeBonusReport(int companyID, int periodID, int FiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmpStatusID)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            if (EmpStatusID > 0)
                sb.Append(" and HRMS_Set_Employee.StatusID =  " + EmpStatusID);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DateFrom", Date_From);
                sqlparams[1] = new SqlParameter("@DateTo", Date_To);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@FiscalId", FiscalID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeBonusReport, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable Get_EmployeeBonusReport1(int companyID, int periodID, int FiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmpStatusID, Nullable<int> FormulaID, int LocationID = 0, int EmployeeTypeID = 0, string ReportFile = "", Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            if (EmpStatusID > 0)
            {
                sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
            }
            else
            {
                sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
            }
            if (LocationID > 0)
            {
                sb.Append("and HRMS_Set_Employee.FK_LocationID = " + LocationID);
            }
            if (EmployeeTypeID > 0)
            {
                sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
            }
            if (FormulaID != null)
            {

                sb.Append(" AND HRMS_BonusHeader.FormulaID = " + FormulaID + " ");
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DateFrom", Date_From);
                sqlparams[1] = new SqlParameter("@DateTo", Date_To);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@FiscalId", FiscalID);

                if (ReportFile == "EmployeeBonusReport")
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetEmployeeBonusReport1, sb.ToString()), sqlparams).Tables[0];
                }
                else if (ReportFile == "EmployeeBonusReport_HSM")
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetEmployeeBonusReport_HSM.ToString(), sb.ToString()), sqlparams).Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable Get_QueryForSalaryPostingReport(int companyid, int periodid, Nullable<DateTime> SalaryDate = null, Nullable<DateTime> SalaryDateTo = null, string Posting = "0")
        {
            DataTable dt;
            StringBuilder filter = new StringBuilder();
            StringBuilder filterPosting = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);

                #region Query
                const string Query_ForSalaryPostingReport = @"
Select ROW_NUMBER() OVER(Order BY Tab.CompanyID asc) AS SNo, * from (
Select Distinct CompanyID,PeriodID, SalaryDate, PostDate  from HRMS_SalaryHeader
  where HRMS_SalaryHeader.Status = 1 And HRMS_SalaryHeader.CompanyID  = @CompanyID
       And HRMS_SalaryHeader.PeriodID = @PeriodID
	   {0}
	   {1}
)Tab
 order by SalaryDate
";
                #endregion


                if (SalaryDate.HasValue && SalaryDateTo.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_SalaryHeader.Salarydate) between '" + ((DateTime)SalaryDate.Value).ToString("yyyy-MM-dd") + "' and + '" + ((DateTime)SalaryDateTo.Value).ToString("yyyy-MM-dd") + "' ");

                }
                else if (SalaryDate.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_SalaryHeader.Salarydate) = '" + ((DateTime)SalaryDate.Value).ToString("yyyy-MM-dd") + "' ");

                }
                else if (SalaryDateTo.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_SalaryHeader.Salarydate) = '" + ((DateTime)SalaryDateTo.Value).ToString("yyyy-MM-dd") + "' ");

                }

                #region Filter by Posting

                switch (Posting)
                {
                    case "1":
                        filterPosting.Append(" and HRMS_SalaryHeader.PostDate is not null ");
                        break;

                    case "2":
                        filterPosting.Append(" and HRMS_SalaryHeader.PostDate is null ");
                        break;

                    default:
                        break;
                }

                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForSalaryPostingReport, filter.ToString(), filterPosting.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_QueryForBonusPostingReport(int companyid, int periodid, Nullable<DateTime> BonusDate = null, Nullable<DateTime> BonusDateTo = null, string Posting = "0")
        {
            DataTable dt;
            StringBuilder filter = new StringBuilder();
            StringBuilder filterPosting = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);

                #region Query
                const string Query_ForBonusPostingReport = @"
  Select ROW_NUMBER() OVER(Order BY Tab.CompanyID asc) AS SNo, * from (
  Select Distinct CompanyID,PeriodID, BonusDate, PostingDate from HRMS_BonusPostingHeader 
    Where HRMS_BonusPostingHeader.Status = 1 And HRMS_BonusPostingHeader.CompanyID  = @CompanyID
	   And HRMS_BonusPostingHeader.PeriodID = @PeriodID
	   {0}
	   {1}
  
  )Tab
  order by BonusDate
";
                #endregion


                if (BonusDate.HasValue && BonusDateTo.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_BonusPostingHeader.BonusDate) between '" + ((DateTime)BonusDate.Value).ToString("yyyy-MM-dd") + "' and + '" + ((DateTime)BonusDateTo.Value).ToString("yyyy-MM-dd") + "' ");

                }
                else if (BonusDate.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_BonusPostingHeader.BonusDate) = '" + ((DateTime)BonusDate.Value).ToString("yyyy-MM-dd") + "' ");

                }
                else if (BonusDateTo.HasValue)
                {
                    filter.Append(" AND convert(date,HRMS_BonusPostingHeader.BonusDate) = '" + ((DateTime)BonusDateTo.Value).ToString("yyyy-MM-dd") + "' ");

                }

                #region Filter by Posting

                switch (Posting)
                {
                    case "1":
                        filterPosting.Append(" and HRMS_BonusPostingHeader.PostingDate is not null ");
                        break;

                    case "2":
                        filterPosting.Append(" and HRMS_BonusPostingHeader.PostingDate is null ");
                        break;

                    default:
                        break;
                }

                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ForBonusPostingReport, filter.ToString(), filterPosting.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_EmployeeBonusReportSummary(DateTime Date_From, DateTime Date_To, int CompanyID, int PeriodID, Nullable<int> DepartmentID, Nullable<int> CostCenterID, Nullable<int> EmpStatusID, int FiscalID, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@MillStartDate", Date_From);
                sqlparams[3] = new SqlParameter("@CurrentManulDate", Date_To);
                if (DepartmentID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DepartmentID", DepartmentID);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@DepartmentID", DBNull.Value);
                }
                if (CostCenterID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@CostCenterID", CostCenterID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@CostCenterID", DBNull.Value);
                }

                sqlparams[6] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (EmpStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_GetEmployeeBonusReportSummary,sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GET_StrenghtandSalary(int companyID, int periodID, Nullable<int> department)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //   sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (department != null)
                {
                    sb.Append("  and SET_Department.DepartmentID  = " + department + "  ");

                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_StrenghtandSalary, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }
        //Query_DepartmentWiseYearlyReport

        public static DataTable DepartmentYearlyReport(int companyID, int periodID, int year)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Year", year);
                //   sqlparams[2] = new SqlParameter("@PersonID", personID);

                // sb.Append(" and datename(year, HRMS_SalaryHeader.SalaryDate) = " + year + " ");


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DepartmentWiseYearlyReport), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable IncomeTaxPrevious(int companyID, int PeriodID, string year, string Month, int PersonID, DateTime dtFrm, DateTime dtTo, SqlTransaction sqltrans = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@Year", year);
                sqlparams[3] = new SqlParameter("@Month", Month);
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);
                sqlparams[5] = new SqlParameter("@dtFrm", dtFrm);
                sqlparams[6] = new SqlParameter("@dtTo", dtTo);

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_PreiousIncomeTax, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_PreiousIncomeTax, sqlparams).Tables[0];

                //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PreiousIncomeTax), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable IncomeTaxForAllMonths(int companyID, int PeriodID, int PersonID, DateTime dtFrm, DateTime dtTo, SqlTransaction sqltrans = null)
        {

            #region Query

            const string QueryForAllMonthtax = @" 
Select * from HRMS_SalaryHeader 
   where HRMS_SalaryHeader.Status = 1
   and HRMS_SalaryHeader.CompanyID = @CompanyID
   and HRMS_SalaryHeader.PeriodID = @PeriodID
   and HRMS_SalaryHeader.PersonID = @PersonID
   and HRMS_SalaryHeader.SalaryDate BETWEEN @dtFrm and @dtTo 
";
            #endregion

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@dtFrm", dtFrm);
                sqlparams[4] = new SqlParameter("@dtTo", dtTo);

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForAllMonthtax, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForAllMonthtax, sqlparams).Tables[0];

                //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PreiousIncomeTax), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        //public static DataTable IncomeTaxPrevious(int companyID, string year, string Month, int PersonID, DateTime dtFrm, DateTime dtTo, SqlTransaction sqltrans = null)
        //{

        //    DataTable dt = null;
        //    StringBuilder sb = new StringBuilder();
        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[6];
        //        sqlparams[0] = new SqlParameter("@CompanyID", companyID);               
        //        sqlparams[1] = new SqlParameter("@Year", year);
        //        sqlparams[2] = new SqlParameter("@Month", Month);
        //        sqlparams[3] = new SqlParameter("@PersonID", PersonID);
        //        sqlparams[4] = new SqlParameter("@dtFrm", dtFrm);
        //        sqlparams[5] = new SqlParameter("@dtTo", dtTo);

        //        if (sqltrans != null)
        //            dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_PreiousIncomeTax, sqlparams).Tables[0];
        //        else
        //            dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_PreiousIncomeTax, sqlparams).Tables[0];

        //        //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PreiousIncomeTax), sqlparams).Tables[0];

        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }
        //    return dt;
        //}


        public static DataTable EmployeeYearlyLedger(int companyID, int periodID, int year)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Year", year);



                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeYearlyLedger), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetDataByPersonID(int companyID, int periodID, int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPersonID, sqlparams);

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

        public static DataTable GET_SalaryBreakUp(int companyID, int periodID, Nullable<int> department, Nullable<int> designation, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int employeestatus=0)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (department != null)
                {
                    sb.Append("  and SET_Department.DepartmentID  = " + department + "  ");

                }

                if (designation != null)
                {
                    sb.Append(" and SET_JobDesignation.JobDesignationID = " + designation + " ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (employeestatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatus);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_StrenghtandSalary, sb, sb1), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }


        public static DataTable GETAll_SalaryBreakUp(int companyID, int periodID, Nullable<int> department, Nullable<int> designation, int emplyeeid, int Toemplyeeid, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int employeestatus = 0, int GradeID = 0, Nullable<Guid> UserID = null)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeid > 0 && Toemplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID between " + emplyeeid + " and " + Toemplyeeid + " )");
                }
                else if (emplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID = " + emplyeeid + " )");
                }
                else if (Toemplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID  = " + Toemplyeeid + " )");
                }

                if (department != null)
                {
                    sb.Append("  and SET_Department.DepartmentID  = " + department + "  ");

                }

                if (designation != null)
                {
                    sb.Append(" and SET_JobDesignation.JobDesignationID = " + designation + " ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (employeestatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatus);
                }
                else
                { 
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                 
                }

                if (GradeID > 0)
                    sb.Append(" AND SET_Grade.GradeID = " + GradeID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalaryBreakUp, sb, sb1, sb2), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GETAll_SalaryBreakUp_New(int companyID, int periodID, Nullable<int> department, Nullable<int> designation, int emplyeeid, int Toemplyeeid, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int employeestatus = 0, int GradeID = 0, Nullable<Guid> UserID = null, bool Active = false, bool inActive = false, bool IsSanghar = false)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            StringBuilder sb4 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeid > 0 && Toemplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID between " + emplyeeid + " and " + Toemplyeeid + " )");
                }
                else if (emplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID = " + emplyeeid + " )");
                }
                else if (Toemplyeeid > 0)
                {
                    sb.Append(" and ( HRMS_Set_Employee.PersonID  = " + Toemplyeeid + " )");
                }

                if (Active == false && inActive == true)
                {
                    sb3.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && inActive == false)
                {
                    sb3.Append(" and HRMS_Set_Employee.isActive = 1");
                }

                if (department != null)
                {
                    sb.Append("  and SET_Department.DepartmentID  = " + department + "  ");

                }

                if (designation != null)
                {
                    sb.Append(" and SET_JobDesignation.JobDesignationID = " + designation + " ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (employeestatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatus);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (IsSanghar)
                {
                    sb4.Append(" AND PType.Description NOT IN ('Advance for Solar', 'Advance Salary', 'Mess', 'Shop', 'Canteen', 'Commette', 'Convey/Electric', 'EOBI Company') ");
                }

                if (GradeID > 0)
                    sb.Append(" AND SET_Grade.GradeID = " + GradeID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalaryBreakUp_New, sb, sb1, sb2, sb3, sb4), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GETAll_SalaryBreakUp_AlNoor(int companyID, int periodID, Nullable<int> department, Nullable<int> designation, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int employeestatus = 0, int GradeID = 0, Nullable<Guid> UserID = null)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (department != null)
                {
                    sb.Append("  and SET_Department.DepartmentID  = " + department + "  ");

                }

                if (designation != null)
                {
                    sb.Append(" and SET_JobDesignation.JobDesignationID = " + designation + " ");
                }

                if (LocationID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (employeestatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatus);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (GradeID > 0)
                    sb.Append(" AND SET_Grade.GradeID = " + GradeID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalaryBreakUp_AlNoor, sb, sb1), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable RestrictionOnSalaryDate(int companyID, int periodID, string month, string year)
        {

            //Query_RestrictionOnSalaryDate
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //   sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@Month", month);
                sqlparams[3] = new SqlParameter("@Year", year);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_RestrictionOnSalaryDate, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;



        }

        public static DataTable Get_EmployeeEOBIReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> PersonFrom, Nullable<int> PersonTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                //person
                if (PersonTo != null && PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid between " + PersonFrom + " and " + PersonTo + " ");
                }
                else if (PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid = " + PersonFrom + " ");
                }
                else if (PersonTo != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid  = " + PersonTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //   sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeEOBIReport, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeeBankAdvice(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> bank, Nullable<int> branch, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> employeestatusID, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (employeestatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (bank == -1)
                {
                }
                else
                {
                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (branch == -1)
                {
                }
                else
                {
                    sb.Append("and AR_SET_BankBranch.BankBranchID = " + branch + " ");
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }
                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }
                if (EmployeeTypeID > 0)
                {
                    //sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb.Append(" and SET_EmployeeType.TypeID = " + EmployeeTypeID);
                }
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeBankAdvice, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable Get_EmployeeBankAdviceWithScroll(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> bank, Nullable<int> branch, Nullable<int> bankaccountno, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> employeestatusID, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, string ChequeNo = "", bool chequeNoWise = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (employeestatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (bank == -1)
                {

                }
                else
                {
                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }

                if (branch == -1)
                {

                }
                else
                {
                    sb.Append("and AR_SET_BankBranch.BankBranchID = " + branch + " ");
                }

                if (bankaccountno == -1)
                {

                }
                else
                {
                    sb.Append(" And AR_SET_BankAccount.BankAccountID = " + bankaccountno + " ");
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    //sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb.Append(" and SET_EmployeeType.TypeID = " + EmployeeTypeID);
                }

                if (!string.IsNullOrEmpty(ChequeNo))
                    sb.Append(" AND HRMS_EmployeePayment.ChequeNo = '" + ChequeNo + "' ");

                string query = "";

                if (chequeNoWise)
                    query = FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_EmployeeBankAdvice;
                else
                    query = FnI.QueryResourceLibrary.AR.AR_SET_Bank.Query_EmployeeBankAdviceBeforeScroll;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_TaxableAndExemptSalaryPayments(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];                
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                string Query_TaxableAndExemptSalaryPayments = @"--Declare @CompanyID int = 1
select distinct Row_Number() Over (Order by HRMS_SalaryHeader.SalaryHeaderID asc) as RowNumber,
HRMS_Set_Employee.EmployeeCode,
HRMS_Set_Employee.CNICNumber, HRMS_Set_Employee.FullName, null as FBRCode ,HRMS_EmployeePayment.ChequeNo, HRMS_EmployeePayment.PaymentDate
,round(HRMS_SalaryHeader.GrossAmount,0) as GrossAmount, round(HRMS_SalaryHeader.GrossAmount,0) as TaxableAmount
,0 as ExemptAmount, 0 as AvgRate, round(HRMS_SalaryDetail.Amount,0) as TaxChargable, round(HRMS_SalaryDetail.Amount,0) as TaxDeducted
,(round(HRMS_SalaryHeader.GrossAmount,0) - round(HRMS_SalaryDetail.Amount,0)) as NetPayment, null as CPRNo

,(select distinct SUM(round(HRMS_SalaryHeader.GrossAmount,0)) as BTLSalaries FROM HRMS_SalaryHeader 

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_SalaryHeader.Status=1 and HRMS_SalaryHeader.SalaryHeaderID not in (

select distinct HRMS_SalaryHeader.SalaryHeaderID FROM HRMS_SalaryHeader 

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1 and HRMS_PayrollType.Description like 'Income Tax'

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_SalaryHeader.Status=1
)) as BTLSalaries

FROM HRMS_Set_Employee 
  
INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID And HRMS_SalaryHeader.Status=1

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1

INNER JOIN SET_EmployeeType ON SET_EmployeeType.TypeID = HRMS_Set_Employee.PersonTypeCode AND SET_EmployeeType.Status = 1 
 
inner JOIN HRMS_EmployeePayment ON HRMS_EmployeePayment.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
AND HRMS_EmployeePayment.CompanyID = HRMS_SalaryHeader.CompanyID 
AND HRMS_EmployeePayment.PeriodID = HRMS_SalaryHeader.PeriodID 
and HRMS_EmployeePayment.PaymentPersonID = HRMS_SalaryHeader.PersonID
AND HRMS_EmployeePayment.Status = 1

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_Set_Employee.IsActive = 1 and HRMS_PayrollType.Description like 'Income Tax'

";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_TaxableAndExemptSalaryPayments, sb).Replace("@CompanyID", companyID), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_BTLSalaryPayment(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                string Query_TaxableAndExemptSalaryPayments = @"

select distinct Row_Number() Over (Order by HRMS_SalaryHeader.SalaryHeaderID asc) as RowNumber,
HRMS_Set_Employee.EmployeeCode,
HRMS_Set_Employee.CNICNumber, HRMS_Set_Employee.FullName, null as FBRCode ,HRMS_EmployeePayment.ChequeNo, HRMS_EmployeePayment.PaymentDate
,round(HRMS_EmployeePayment.NetAmount,0) as GrossAmount, round(HRMS_SalaryHeader.GrossAmount,0) as TaxableAmount
,0 as ExemptAmount, 0 as AvgRate, round(HRMS_SalaryDetail.Amount,0) as TaxChargable, round(HRMS_SalaryDetail.Amount,0) as TaxDeducted
,(round(HRMS_SalaryHeader.GrossAmount,0) - round(HRMS_SalaryDetail.Amount,0)) as NetPayment, null as CPRNo

,(select distinct SUM(round(HRMS_SalaryHeader.GrossAmount,0)) as BTLSalaries FROM HRMS_SalaryHeader 

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_SalaryHeader.Status=1 and HRMS_SalaryHeader.SalaryHeaderID not in (

select distinct HRMS_SalaryHeader.SalaryHeaderID FROM HRMS_SalaryHeader 

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1 and HRMS_PayrollType.Description like 'Income Tax'

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_SalaryHeader.Status=1
)) as BTLSalaries

FROM HRMS_Set_Employee 
  
INNER JOIN HRMS_SalaryHeader ON HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
AND HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID And HRMS_SalaryHeader.Status=1

inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID and HRMS_SalaryDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.Status = 1

INNER JOIN SET_EmployeeType ON SET_EmployeeType.TypeID = HRMS_Set_Employee.PersonTypeCode AND SET_EmployeeType.Status = 1 
 
inner JOIN HRMS_EmployeePayment ON HRMS_EmployeePayment.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
AND HRMS_EmployeePayment.CompanyID = HRMS_SalaryHeader.CompanyID 
AND HRMS_EmployeePayment.PeriodID = HRMS_SalaryHeader.PeriodID 
and HRMS_EmployeePayment.PaymentPersonID = HRMS_SalaryHeader.PersonID
AND HRMS_EmployeePayment.Status = 1

where HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate and HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
and HRMS_Set_Employee.IsActive = 1 and HRMS_PayrollType.Description like 'Income Tax'

";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_TaxableAndExemptSalaryPayments, sb).Replace("@CompanyID", companyID), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable Get_BonusBankAdvice(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> FiscalID, Nullable<int> bank, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> employeestatusID, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (bank != null)
                {

                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    //sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb.Append(" and SET_EmployeeType.TypeID = " + EmployeeTypeID);
                }
                if (employeestatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + employeestatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusBankAdvice, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeeIncrementReport(Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null, Nullable<int> DepartmentFrom = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                if (Date_From.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@DateFrom", String.Format("{0:yyyy-MM-dd}", Date_From));
                }
                if (Date_To.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@DateTo", String.Format("{0:yyyy-MM-dd}", Date_To));
                }
                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND IncrementDate between @DateFrom AND @DateTo ");
                }
                
                if (DepartmentFrom.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_DepartmentID between " + DepartmentFrom + " AND " + DepartmentFrom);
                }

                #region query
                const string query = @"
                Select *,IncrementID, '' as SectionName,'' as Action ,JobDesignationName as DesignationName , IncrementAmount as LastIncrementAmount,DeductionAmount
                , Set_Grade.GradeName
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
                where HRMS_EmployeeIncrement.Status = 1
                {0}
                Order by IncrementDate DESC , SET_Department.DepartmentName ASC
                ";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable EmployeeDetailWithDeductionReport(bool Active, bool InActive, Nullable<int> companyID, Nullable<int> bank, int LocationID = 0, string DivisionID = "", int EmployeeTypeID = 0, Nullable<int> EmployeeFrom = null,
              Nullable<int> EmployeeTo = null, Nullable<int> DepartmentFrom = null, Nullable<int> DepartmentTo = null, Nullable<int> EmpStatusTypeID = null, Nullable<int> SectionID = null, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null,
               Nullable<DateTime> JDate_From = null, Nullable<DateTime> JDate_To = null, Nullable<DateTime> DOBDate_From = null, Nullable<DateTime> DOBDate_To = null, Nullable<DateTime> CreateDate_From = null, Nullable<DateTime> CreateDate_To = null, Nullable<Guid> UserID = null, string companyIDs = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);



                if (Date_From.HasValue)
                {
                    sqlparams[0] = new SqlParameter("@DateFrom", String.Format("{0:yyyy-MM-dd}", Date_From));
                }

                //sqlparams[1] = new SqlParameter("@DateFrom", Date_From);
                //sqlparams[2] = new SqlParameter("@DateTo", Date_To);
                if (Date_To.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@DateTo", String.Format("{0:yyyy-MM-dd}", Date_To));
                }
                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND HRMS_Set_Employee.JoiningDate between @DateFrom AND @DateTo ");
                }


                // sqlparams[3] = new SqlParameter("@JDateFrom", JDate_From);
                // sqlparams[4] = new SqlParameter("@JDateTo", JDate_To);
                if (JDate_From.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@JDateFrom", String.Format("{0:yyyy-MM-dd}", JDate_From));
                }

                if (JDate_To.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@JDateTo", String.Format("{0:yyyy-MM-dd}", JDate_To));
                }
                if (JDate_From != null && JDate_To != null)
                {
                    sb.Append(" AND  HRMS_Set_Employee.AppointedOn between @JDateFrom AND @JDateTo");
                }

                if (DOBDate_From != null && DOBDate_To != null)
                {
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth between '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");
                }
                else if (DOBDate_From != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth >= '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "'");
                else if (DOBDate_To != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth <= '" + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");

                if (CreateDate_From != null && CreateDate_To != null)
                {
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) between '" + ((DateTime)CreateDate_From).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)CreateDate_To).ToString("yyyy-MM-dd") + "'");
                }
                else if (CreateDate_From != null)
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) >= '" + ((DateTime)CreateDate_From).ToString("yyyy-MM-dd") + "'");
                else if (CreateDate_To != null)
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) <= '" + ((DateTime)CreateDate_To).ToString("yyyy-MM-dd") + "'");

                if (Active == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 1 ");
                }


                if (InActive == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is not null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 0 ");
                }

                if (bank != null)
                {

                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (EmployeeFrom.HasValue && EmployeeTo.HasValue)
                    sb.Append(" and HRMS_Set_Employee.PersonID between " + EmployeeFrom + " AND " + EmployeeTo);

                if (DepartmentFrom.HasValue && DepartmentTo.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_DepartmentID between " + DepartmentFrom + " AND " + DepartmentTo);
                }

                if (Date_From != null && Date_To != null)
                {
                    //sb.Append(" AND ( HRMS_Set_Employee.JoiningDate between @DateFrom AND @DateTo  OR HRMS_Set_Employee.JobEndingDate between @DateFrom AND @DateTo )");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusTypeID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (!string.IsNullOrEmpty(DivisionID))
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID IN (" + DivisionID + ")");
                }


                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (SectionID != null)
                {
                    sb.Append(" AND [SET_Section].SectionID = " + SectionID);
                }

                string query = FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_EmployeeDetailWithDeductionReport;

                if (companyIDs != null && !string.IsNullOrEmpty(companyIDs))
                {
                    query = query.Replace("@CompanyID", companyIDs);
                }
                else
                {
                    query = query.Replace("@CompanyID", companyID.ToString());
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb), sqlparams).Tables[0];
                //  dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_EmployeeDetailWithDeductionReport, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable Get_EmployeeDetailWithDeductionReport(bool Active, bool InActive, Nullable<int> companyID, Nullable<int> bank, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<int> EmployeeFrom = null,
              Nullable<int> EmployeeTo = null, Nullable<int> DepartmentFrom = null, Nullable<int> DepartmentTo = null, Nullable<int> EmpStatusTypeID = null, Nullable<int> SectionID = null, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null,
               Nullable<DateTime> JDate_From = null, Nullable<DateTime> JDate_To = null, Nullable<DateTime> DOBDate_From = null, Nullable<DateTime> DOBDate_To = null, Nullable<DateTime> CreateDate_From = null, Nullable<DateTime> CreateDate_To = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

              

                if (Date_From.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@DateFrom", String.Format("{0:yyyy-MM-dd}", Date_From));
                }

                //sqlparams[1] = new SqlParameter("@DateFrom", Date_From);
                //sqlparams[2] = new SqlParameter("@DateTo", Date_To);
                if (Date_To.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@DateTo", String.Format("{0:yyyy-MM-dd}", Date_To));
                }
                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND HRMS_Set_Employee.JoiningDate between @DateFrom AND @DateTo ");
                }
                

               // sqlparams[3] = new SqlParameter("@JDateFrom", JDate_From);
               // sqlparams[4] = new SqlParameter("@JDateTo", JDate_To);
                if (JDate_From.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@JDateFrom", String.Format("{0:yyyy-MM-dd}", JDate_From));
                }

                if (JDate_To.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@JDateTo", String.Format("{0:yyyy-MM-dd}", JDate_To));
                }
                if (JDate_From != null && JDate_To != null)
                {
                    sb.Append(" AND  HRMS_Set_Employee.AppointedOn between @JDateFrom AND @JDateTo");
                }

                if (DOBDate_From != null && DOBDate_To != null)
                {
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth between '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");
                }
                else if (DOBDate_From != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth >= '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "'");
                else if (DOBDate_To != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth <= '" + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");

                if (CreateDate_From != null && CreateDate_To != null)
                {
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) between '" + ((DateTime)CreateDate_From).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)CreateDate_To).ToString("yyyy-MM-dd") + "'");
                }
                else if (CreateDate_From != null)
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) >= '" + ((DateTime)CreateDate_From).ToString("yyyy-MM-dd") + "'");
                else if (CreateDate_To != null)
                    sb.Append(" and cast(HRMS_Set_Employee.CreateDate as date) <= '" + ((DateTime)CreateDate_To).ToString("yyyy-MM-dd") + "'");
                
                if (Active == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 1 ");
                }


                if (InActive == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is not null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 0 ");
                }

                if (bank != null)
                {

                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (EmployeeFrom.HasValue && EmployeeTo.HasValue)
                    sb.Append(" and HRMS_Set_Employee.PersonID between " + EmployeeFrom + " AND " + EmployeeTo);

                if (DepartmentFrom.HasValue && DepartmentTo.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_DepartmentID between " + DepartmentFrom + " AND " + DepartmentTo);
                }

                if (Date_From != null && Date_To != null)
                {
                    //sb.Append(" AND ( HRMS_Set_Employee.JoiningDate between @DateFrom AND @DateTo  OR HRMS_Set_Employee.JobEndingDate between @DateFrom AND @DateTo )");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusTypeID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }


                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (SectionID != null)
                {
                    sb.Append(" AND [SET_Section].SectionID = " + SectionID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_EmployeeDetailWithDeductionReport, sb), sqlparams).Tables[0];
              //  dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_EmployeeDetailWithDeductionReport, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeeAllowancesReport(bool Active, bool InActive, Nullable<int> companyID, Nullable<int> bank, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<int> EmployeeFrom = null,
             Nullable<int> EmployeeTo = null, Nullable<int> DepartmentFrom = null, Nullable<int> DepartmentTo = null, Nullable<int> EmpStatusTypeID = null, Nullable<int> SectionID = null, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null,
              Nullable<DateTime> JDate_From = null, Nullable<DateTime> JDate_To = null, Nullable<DateTime> DOBDate_From = null, Nullable<DateTime> DOBDate_To = null, Nullable<Guid> UserID = null,bool salaryfilter = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);



                if (Date_From.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@DateFrom", String.Format("{0:yyyy-MM}", Date_From));
                }

                //sqlparams[1] = new SqlParameter("@DateFrom", Date_From);
                //sqlparams[2] = new SqlParameter("@DateTo", Date_To);
                if (Date_To.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@DateTo", String.Format("{0:yyyy-MM}", Date_To));
                }
                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND convert(varchar(7), SalaryDate, 126) between @DateFrom AND @DateTo ");
                }

                if (salaryfilter)
                    sb.Append(" and HRMS_EmployeePayroll.CalculatedAmount >= 100000 ");
                //else
                //    sb.Append(" and HRMS_EmployeePayroll.CalculatedAmount < 100000 ");

                // sqlparams[3] = new SqlParameter("@JDateFrom", JDate_From);
                // sqlparams[4] = new SqlParameter("@JDateTo", JDate_To);
                if (JDate_From.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@JDateFrom", String.Format("{0:yyyy-MM-dd}", JDate_From));
                }

                if (JDate_To.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@JDateTo", String.Format("{0:yyyy-MM-dd}", JDate_To));
                }
                if (JDate_From != null && JDate_To != null)
                {
                    sb.Append(" AND  HRMS_Set_Employee.AppointedOn between @JDateFrom AND @JDateTo");
                }

                if (DOBDate_From != null && DOBDate_To != null)
                {
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth between '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");
                }
                else if (DOBDate_From != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth >= '" + ((DateTime)DOBDate_From).ToString("yyyy-MM-dd") + "'");
                else if (DOBDate_To != null)
                    sb.Append(" and HRMS_Set_Employee.DateOfBirth <= '" + ((DateTime)DOBDate_To).ToString("yyyy-MM-dd") + "'");


                if (Active == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 1 ");
                }


                if (InActive == true)
                {
                    sb.Append(" and HRMS_Set_Employee.JobEndingDate is not null ");
                    sb.Append(" and HRMS_Set_Employee.isactive = 0 ");
                }

                if (bank != null)
                {

                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (EmployeeFrom.HasValue && EmployeeTo.HasValue)
                    //sb.Append(" and HRMS_Set_Employee.PersonID between " + EmployeeFrom + " AND " + EmployeeTo);
                    sb.Append(" and CAST(HRMS_Set_Employee.EmployeeCode as int) between " + EmployeeFrom + " AND " + EmployeeTo);

                if (DepartmentFrom.HasValue && DepartmentTo.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_DepartmentID between " + DepartmentFrom + " AND " + DepartmentTo);
                }

                if (Date_From != null && Date_To != null)
                {
                    //sb.Append(" AND ( HRMS_Set_Employee.JoiningDate between @DateFrom AND @DateTo  OR HRMS_Set_Employee.JobEndingDate between @DateFrom AND @DateTo )");
                }

                if (EmpStatusTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusTypeID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }


                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (SectionID != null)
                {
                    sb.Append(" AND [SET_Section].SectionID = " + SectionID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_EmployeeAllowancesReport, sb), sqlparams).Tables[0];
                //  dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_EmployeeDetailWithDeductionReport, sb), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable Get_EmployeeLoanSummary(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> personIDFrom, Nullable<int> personIDTo, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool Active = false, bool InActive = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //   sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (personIDFrom != null && personIDTo != null)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonID between " + personIDFrom + " and  " + personIDTo + " ");
                }

                else if
                    (personIDFrom != null)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonID =  " + personIDFrom + " ");
                }
                else if (personIDTo != null)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonID  =  " + personIDTo + " ");

                }
                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (Active == false && InActive == true)
                {
                    sb.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb.Append(" and HRMS_Set_Employee.isActive = 1");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeLoanRegisterSummary, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeeSESIReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> PersonFrom, Nullable<int> PersonTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> StatusID, int LocationID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                //person
                if (PersonTo != null && PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid between " + PersonFrom + " and " + PersonTo + " ");
                }
                else if (PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid = " + PersonFrom + " ");
                }
                else if (PersonTo != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid  = " + PersonTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (LocationID > 0)
                {
                    sb.Append("and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }

                if (StatusID != null)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + StatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSESIReport, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeeSESIReportforDKSM(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> PersonFrom, Nullable<int> PersonTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> StatusID, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                //person
                if (PersonTo != null && PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid between " + PersonFrom + " and " + PersonTo + " ");
                }
                else if (PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid = " + PersonFrom + " ");
                }
                else if (PersonTo != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid  = " + PersonTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (StatusID != null)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + StatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSESIReportforDKSM, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_ESSIContributionReportforDKSM(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> PersonFrom, Nullable<int> PersonTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> StatusID, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                //person
                if (PersonTo != null && PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid between " + PersonFrom + " and " + PersonTo + " ");
                }
                else if (PersonFrom != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid = " + PersonFrom + " ");
                }
                else if (PersonTo != null)
                {
                    sb.Append(" and HRMS_SET_Employee.personid  = " + PersonTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (StatusID != null)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + StatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ESSIContributionReportforDKSM, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable Get_EmployeePFReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmplFrom, Nullable<int> EmplTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, int LocationID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                sb.Append("and HRMS_PayrollType.PayrollTypeCode in (2,3)");
                //person
                if (EmplTo != null && EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) between " + EmplFrom + " and " + EmplTo + " ");
                }
                else if (EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) = " + EmplFrom + " ");
                }
                else if (EmplTo != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int)  = " + EmplTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (EmployeeStatusID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (LocationID > 0)
                {
                    sb.Append("and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFReport, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable EmployeePFReportMonthWise(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmplFrom, Nullable<int> EmplTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, int LocationID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                sb.Append("and HRMS_PayrollType.PayrollTypeCode in (2,3)");
                //person
                if (EmplTo != null && EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) between " + EmplFrom + " and " + EmplTo + " ");
                    sb1.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) between " + EmplFrom + " and " + EmplTo + " ");
                }
                else if (EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) = " + EmplFrom + " ");
                }
                else if (EmplTo != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int)  = " + EmplTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (EmployeeStatusID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (LocationID > 0)
                {
                    sb.Append("and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFReportMOnthWise, sb, sb1), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_EmployeePFReportDateWise(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmplFrom, Nullable<int> EmplTo, Nullable<int> DepartmentFrom, Nullable<int> DepartmentTo, Nullable<int> GradeFrom, Nullable<int> GradeTo, Nullable<int> EmployeeStatusID, int EmployeeTypeID = 0, int LocationID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[4];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@FromDate", Date_From);
            sqlparams[3] = new SqlParameter("@ToDate", Date_To);
            try
            {
                sb.Append("and HRMS_PayrollType.PayrollTypeCode in (2,3)");
                //person
                if (EmplTo != null && EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) between " + EmplFrom + " and " + EmplTo + " ");
                }
                else if (EmplFrom != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int) = " + EmplFrom + " ");
                }
                else if (EmplTo != null)
                {
                    sb.Append(" and  CAST(isnull(HRMS_Set_Employee.EmployeeCode,0) as int)  = " + EmplTo + " ");
                }
                //department 
                if (DepartmentFrom != null && DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID between " + DepartmentFrom + " and " + DepartmentTo + " ");
                }
                else if (DepartmentFrom != null)
                {
                    sb.Append(" and SET_Department.DepartmentID = " + DepartmentFrom + "  ");
                }
                else if (DepartmentTo != null)
                {
                    sb.Append(" and SET_Department.DepartmentID =  " + DepartmentTo + " ");
                }
                // grade
                if (GradeFrom != null && GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID between " + GradeFrom + " and " + GradeTo + " ");
                }
                else if (GradeFrom != null)
                {
                    sb.Append(" and Set_Grade.GradeID = " + GradeFrom + "  ");
                }
                else if (GradeTo != null)
                {
                    sb.Append(" and Set_Grade.GradeID  =  " + GradeTo + " ");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (EmployeeStatusID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (LocationID > 0)
                {
                    sb.Append("and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFReportDateWise, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public DataSet EmployeeSalary_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
            SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0, Nullable<Guid> UserID = null)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[11];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();                

                if (EmpStatusID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                    sb3.Append(" and SE.StatusID = " + EmpStatusID);                    
                }
                else
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb3.Append(" and SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1, sb2, sb3), sqlparams);
                }


                //if (objConnection == null)
                //    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //else
                //{
                //    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //}
                #region Query 2
                string query_EmpSalaryHSM = @"

select case when (t.YTDDedComplete - CMDed) = 0 then CMDed
when (t.YTDDedComplete + CMDed) >= t.LoanAmount then (t.YTDDedComplete)  else (t.YTDDedComplete + CMDed)
 end as PMDed, t.* from (
Select Distinct Description,PayrollTypeCode,PersonID , CMDed ,ISNULL ((Select 
    SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) CMDed  
    from HRMS_LoanPaymentDetail
	INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionDetailID=HRMS_LoanPaymentDetail.LoanSanctionDetailID
	INNER JOIN HRMS_LoanSanctionHeader ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.CompanyID=HRMS_LoanSanctionHeader.CompanyID
INNER JOIN HRMS_PayrollType ON HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_Set_Employee.status = 1
					
						
			
    where
    HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
and HRMS_LoanPaymentDetail.LoanPaymentDate between DATEADD(month, DATEDIFF(month, 0, @fullDate), 0) and @fullDate 
AND HRMS_LoanPaymentDetail.SalaryHeaderID is null
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	AND tab.PayrollTypeCode=HRMS_PayrollType.PayrollTypeCode
	AND tab.Description=HRMS_PayrollType.Description
	AND tab.PersonID=HRMS_LoanSanctionHeader.PersonID   
						
						

group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode
               , HRMS_LoanSanctionHeader.PersonID           
),0) as CMManualDed
-- , PMDed+ISNULL ((Select 
--                        SUM(LPD.LoanDisburstAmount) CMDed  
--                        from HRMS_LoanPaymentDetail LPD
--						INNER JOIN HRMS_LoanSanctionDetail LSD ON LSD.LoanSanctionHeaderID=LPD.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionDetailID=LPD.LoanSanctionDetailID
--						INNER JOIN HRMS_LoanSanctionHeader LSH ON LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--					 AND  LSD.CompanyID=LSH.CompanyID
--                   	INNER JOIN HRMS_PayrollType HPT ON LSD.PayrollTypeID=HPT.PayrollTypeID
--						inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = LSH.PersonID
--					and HRMS_Set_Employee.CompanyID = LSH.CompanyID
--					and HRMS_Set_Employee.status = 1
			
--                        where
--                       HPT.PayrollTypeCode in (5)
--                        and HPT.IsEmployeePart = 1
--                        and HPT.IsVisiable = 1
--                    and LPD.LoanPaymentDate <= @fullDate 
--						--  And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
--					{0}
--					AND LPD.SalaryHeaderID is null
--                        and LSD.LoanSanctionHeaderID = LPD.LoanSanctionHeaderID
--                      and LPD.Status = 1
--					  AND LSD.PayrollTypeCode=tab.PayrollTypeCode
--					  AND HPT.Description=tab.Description
--					  AND LSH.PersonID=tab.PersonID  


--group by HPT.Description,HPT.PayrollTypeCode
--               ,LPD.Currentbalance,LPD.PreviousBalance, LSH.PersonID           
--),0) as PMDed
--,LoanAmount
,(case when LoanAmount != 0 then LoanAmount else AdavanceAmount end) as LoanAmount
,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) --- tab.CMDed 
end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDed


,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
        and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDedComplete




from (

Select distinct HRMS_LoanSanctionDetail.LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    SUM(HRMS_SalaryDetail.Amount) CMDed  ,
    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                                                                   
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
    --and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--						and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


--						and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
--						and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
--						and HRMS_Set_Employee.Status = 1
--						And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)

--						)

--,0) LoanAmount

,
        ISNULL(( select  sum(loanamount) 
			from 
        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail as InnerHRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType

            where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and HRMS_LoanSanctionHeader.CompanyID = InnerHRMS_LoanSanctionDetail.CompanyID
        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
        and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID						
		and  InnerHRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
		and HRMS_LoanSanctionHeader.Status = 1
		and InnerHRMS_LoanSanctionDetail.Status = 1
						
		and InnerHRMS_LoanSanctionDetail.LoanStartDate = HRMS_LoanSanctionDetail.LoanStartDate
		and InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and InnerHRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID

		and InnerHRMS_LoanSanctionDetail.LoanStartDate <= @fullDate

		and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
		and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
		and HRMS_Set_Employee.Status = 1
		--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
		{0}
		and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
		and HRMS_PayrollType.Description not like '%Advance%'
		)

,0) LoanAmount


,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID

	and HRMS_LoanSanctionDetail.LoanAmount=HRMS_SalaryDetail.Amount

	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
	and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


	and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description like '%Advance%'
	)

,0) AdavanceAmount

--,SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) as LoanDisburstAmount ,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance
--,HRMS_LoanPaymentDetail.LoanDisburstAmount,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance

    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail,HRMS_Set_Employee
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
                     
    and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 
		and HRMS_LoanPaymentDetail.LoanDisburstAmount = HRMS_SalaryDetail.Amount

	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
						

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

--and HRMS_LoanPaymentDetail.Currentbalance != 0


group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,HRMS_SalaryHeader.PersonID
                        ,HRMS_SalaryDetail.Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode
                        ,HRMS_LoanPaymentDetail.Currentbalance
						,HRMS_LoanSanctionDetail.LoanStartDate
						,HRMS_LoanSanctionDetail.LoanSanctionHeaderID
						,HRMS_LoanSanctionDetail.LoanSanctionDetailID
						,HRMS_LoanSanctionDetail.CompanyID
                        
union all

Select distinct NULL as LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    HRMS_SalaryDetail.Amount CMDed  ,
    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                       
                       
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
	and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--                        and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate
--						)

--,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
					and HRMS_LoanSanctionDetail.LoanStartDate < DATEADD(month, DATEDIFF(month, 0, @fullDate), 0)

		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)

,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
			and Month(HRMS_LoanSanctionDetail.LoanStartDate) = Month(@fullDate)
	and Year(HRMS_LoanSanctionDetail.LoanStartDate) = Year(@fullDate)
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)


,0) AdvanceAmount


    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType, HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3)
	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
                        
	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
    {0}
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

	and HRMS_PayrollType.Description not like 'EOBI Company'
						
) as tab
--where tab.PersonID = 552

)t";

                #endregion

                if (objConnection == null)                  
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_EmpSalaryHSM, sb2), sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_EmpSalaryHSM, sb2), sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public DataSet EmployeeSalary_ReportDataForHSM(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> LoanNEwDate, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
            SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0, Nullable<Guid> UserID = null, int departmentID = 0 , int bankID = 0)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;
            DataTable dt3 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                sqlparams[11] = new SqlParameter("@LoanNewDate", LoanNEwDate);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();
                StringBuilder LoanAndRecoveryFilter = new StringBuilder();

                if (EmpStatusID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                    sb3.Append(" and SE.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb3.Append(" and SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" and SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    sb1.Append(" and SE.DivisionID = " + DivisionID);
                    
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                   
                }

                if (LocationID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                //if (DepartmentID != null)
                //{
                //    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                //}
                if (emplyeeID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                //if (payrolltypeID != null)
                //{
                //    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                //}
                if (EmpStatusID != null && EmpStatusID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (departmentID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.FK_DepartmentID = " + departmentID);
                    sb3.Append(" and SE.FK_DepartmentID = " + departmentID);
                }

                if (bankID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.FK_BankAccountID = " + bankID);
                    sb3.Append(" and SE.FK_BankAccountID = " + bankID);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1, sb2, sb3), sqlparams);
                }


                //if (objConnection == null)
                //    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //else
                //{
                //    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //}
                #region Query 2
                string query_EmpSalaryHSM = @"

select case when (t.YTDDedComplete - CMDed) = 0 then CMDed
when (t.YTDDedComplete + CMDed) >= t.LoanAmount then (t.YTDDedComplete)  else (t.YTDDedComplete + CMDed)
 end as PMDed, t.* from (
Select Distinct Description,PayrollTypeCode,PersonID , CMDed ,ISNULL ((Select 
    SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) CMDed  
    from HRMS_LoanPaymentDetail
	INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionDetailID=HRMS_LoanPaymentDetail.LoanSanctionDetailID
	INNER JOIN HRMS_LoanSanctionHeader ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.CompanyID=HRMS_LoanSanctionHeader.CompanyID
INNER JOIN HRMS_PayrollType ON HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_Set_Employee.status = 1
					
						
			
    where
    HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
and HRMS_LoanPaymentDetail.LoanPaymentDate between DATEADD(month, DATEDIFF(month, 0, @fullDate), 0) and @fullDate 
AND HRMS_LoanPaymentDetail.SalaryHeaderID is null
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	AND tab.PayrollTypeCode=HRMS_PayrollType.PayrollTypeCode
	AND tab.Description=HRMS_PayrollType.Description
	AND tab.PersonID=HRMS_LoanSanctionHeader.PersonID   
						
						

group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode
               , HRMS_LoanSanctionHeader.PersonID           
),0) as CMManualDed
-- , PMDed+ISNULL ((Select 
--                        SUM(LPD.LoanDisburstAmount) CMDed  
--                        from HRMS_LoanPaymentDetail LPD
--						INNER JOIN HRMS_LoanSanctionDetail LSD ON LSD.LoanSanctionHeaderID=LPD.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionDetailID=LPD.LoanSanctionDetailID
--						INNER JOIN HRMS_LoanSanctionHeader LSH ON LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--					 AND  LSD.CompanyID=LSH.CompanyID
--                   	INNER JOIN HRMS_PayrollType HPT ON LSD.PayrollTypeID=HPT.PayrollTypeID
--						inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = LSH.PersonID
--					and HRMS_Set_Employee.CompanyID = LSH.CompanyID
--					and HRMS_Set_Employee.status = 1
			
--                        where
--                       HPT.PayrollTypeCode in (5)
--                        and HPT.IsEmployeePart = 1
--                        and HPT.IsVisiable = 1
--                    and LPD.LoanPaymentDate <= @fullDate 
--						--  And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
--					{0}
--					AND LPD.SalaryHeaderID is null
--                        and LSD.LoanSanctionHeaderID = LPD.LoanSanctionHeaderID
--                      and LPD.Status = 1
--					  AND LSD.PayrollTypeCode=tab.PayrollTypeCode
--					  AND HPT.Description=tab.Description
--					  AND LSH.PersonID=tab.PersonID  


--group by HPT.Description,HPT.PayrollTypeCode
--               ,LPD.Currentbalance,LPD.PreviousBalance, LSH.PersonID           
--),0) as PMDed
--,LoanAmount
,(case when LoanAmount != 0 then LoanAmount else AdavanceAmount end) as LoanAmount
,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) --- tab.CMDed 
end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDed


,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
        and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDedComplete




from (

Select distinct HRMS_LoanSanctionDetail.LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    SUM(HRMS_SalaryDetail.Amount) CMDed  ,
    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                                                                   
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
    --and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--						and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


--						and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
--						and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
--						and HRMS_Set_Employee.Status = 1
--						And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)

--						)

--,0) LoanAmount

,
        ISNULL(( select  sum(loanamount) 
			from 
        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail as InnerHRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType

            where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and HRMS_LoanSanctionHeader.CompanyID = InnerHRMS_LoanSanctionDetail.CompanyID
        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
        and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID						
		and  InnerHRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
		and HRMS_LoanSanctionHeader.Status = 1
		and InnerHRMS_LoanSanctionDetail.Status = 1
						
		and InnerHRMS_LoanSanctionDetail.LoanStartDate = HRMS_LoanSanctionDetail.LoanStartDate
		and InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and InnerHRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID

		and InnerHRMS_LoanSanctionDetail.LoanStartDate <= @fullDate

		and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
		and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
		and HRMS_Set_Employee.Status = 1
		--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
		{0}
		and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
		and HRMS_PayrollType.Description not like '%Advance%'
		)

,0) LoanAmount


,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID

	and HRMS_LoanSanctionDetail.LoanAmount=HRMS_SalaryDetail.Amount

	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
	and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


	and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description like '%Advance%'
	)

,0) AdavanceAmount

--,SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) as LoanDisburstAmount ,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance
--,HRMS_LoanPaymentDetail.LoanDisburstAmount,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance

    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail,HRMS_Set_Employee
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
                     
    and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 
		and HRMS_LoanPaymentDetail.LoanDisburstAmount = HRMS_SalaryDetail.Amount

	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
						

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

--and HRMS_LoanPaymentDetail.Currentbalance != 0


group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,HRMS_SalaryHeader.PersonID
                        ,HRMS_SalaryDetail.Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode
                        ,HRMS_LoanPaymentDetail.Currentbalance
						,HRMS_LoanSanctionDetail.LoanStartDate
						,HRMS_LoanSanctionDetail.LoanSanctionHeaderID
						,HRMS_LoanSanctionDetail.LoanSanctionDetailID
						,HRMS_LoanSanctionDetail.CompanyID,HRMS_SalaryDetail.SalaryDetailID
                        
union all

Select distinct NULL as LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    --HRMS_SalaryDetail.Amount CMDed  ,
ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH                   
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate = @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
    and SD.CompanyID = HRMS_SalaryDetail.CompanyID
    ),0)CMDed,

    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH                   
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
	and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--                        and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate
--						)

--,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
					and HRMS_LoanSanctionDetail.LoanStartDate < DATEADD(month, DATEDIFF(month, 0, @fullDate), 0)

		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)

,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
			and Month(HRMS_LoanSanctionDetail.LoanStartDate) = Month(@fullDate)
	and Year(HRMS_LoanSanctionDetail.LoanStartDate) = Year(@fullDate)
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)


,0) AdvanceAmount


    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType, HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3)
	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
                        
	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
    {0}
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

	and HRMS_PayrollType.Description not like 'EOBI Company'
						
) as tab
--where tab.PersonID = 552

)t";

                #endregion

                if (objConnection == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_EmpSalaryHSM, sb2), sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_EmpSalaryHSM, sb2), sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }

                if (objConnection == null)
                    dt3 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalaryForHSM, LoanAndRecoveryFilter), sqlparams).Tables[0];
                else
                    dt3 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalaryForHSM, LoanAndRecoveryFilter), sqlparams).Tables[0];

                try
                {

                    dt3.TableName = "tblLoanAndRecoveryReport";
                    DataTable dt4 = dt3.Copy();

                    ds.Tables.Add(dt4);
                }
                catch { }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public DataSet EmployeeSalary_ReportData_Sanghar(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> LoanNEwDate, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
            SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0, Nullable<Guid> UserID = null, int departmentID = 0, int bankID = 0)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;
            DataTable dt3 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                sqlparams[11] = new SqlParameter("@LoanNewDate", LoanNEwDate);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();
                StringBuilder LoanAndRecoveryFilter = new StringBuilder();

                if (EmpStatusID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                    sb3.Append(" and SE.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb3.Append(" and SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" and SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    sb1.Append(" and SE.DivisionID = " + DivisionID);

                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);

                }

                if (LocationID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                
                if (emplyeeID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                
                if (EmpStatusID != null && EmpStatusID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (departmentID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.FK_DepartmentID = " + departmentID);
                    sb3.Append(" and SE.FK_DepartmentID = " + departmentID);
                }

                if (bankID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.FK_BankAccountID = " + bankID);
                    sb3.Append(" and SE.FK_BankAccountID = " + bankID);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_Sanghar, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_Sanghar, sb, sb1, sb2, sb3), sqlparams);
                }
                
                #region Query 2
                string query_EmpSalarySanghar = @"

select case when (t.YTDDedComplete - CMDed) = 0 then CMDed
when (t.YTDDedComplete + CMDed) >= t.LoanAmount then (t.YTDDedComplete)  else (t.YTDDedComplete + CMDed)
 end as PMDed, t.* from (
Select Distinct Description,PayrollTypeCode,PersonID , CMDed ,ISNULL ((Select 
    SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) CMDed  
    from HRMS_LoanPaymentDetail
	INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionDetailID=HRMS_LoanPaymentDetail.LoanSanctionDetailID
	INNER JOIN HRMS_LoanSanctionHeader ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
	AND  HRMS_LoanSanctionDetail.CompanyID=HRMS_LoanSanctionHeader.CompanyID
INNER JOIN HRMS_PayrollType ON HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
and HRMS_Set_Employee.status = 1
					
						
			
    where
    HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
and HRMS_LoanPaymentDetail.LoanPaymentDate between DATEADD(month, DATEDIFF(month, 0, @fullDate), 0) and @fullDate 
AND HRMS_LoanPaymentDetail.SalaryHeaderID is null
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	AND tab.PayrollTypeCode=HRMS_PayrollType.PayrollTypeCode
	AND tab.Description=HRMS_PayrollType.Description
	AND tab.PersonID=HRMS_LoanSanctionHeader.PersonID   
						
						

group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode
               , HRMS_LoanSanctionHeader.PersonID           
),0) as CMManualDed
-- , PMDed+ISNULL ((Select 
--                        SUM(LPD.LoanDisburstAmount) CMDed  
--                        from HRMS_LoanPaymentDetail LPD
--						INNER JOIN HRMS_LoanSanctionDetail LSD ON LSD.LoanSanctionHeaderID=LPD.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionDetailID=LPD.LoanSanctionDetailID
--						INNER JOIN HRMS_LoanSanctionHeader LSH ON LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--						AND  LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
--					 AND  LSD.CompanyID=LSH.CompanyID
--                   	INNER JOIN HRMS_PayrollType HPT ON LSD.PayrollTypeID=HPT.PayrollTypeID
--						inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = LSH.PersonID
--					and HRMS_Set_Employee.CompanyID = LSH.CompanyID
--					and HRMS_Set_Employee.status = 1
			
--                        where
--                       HPT.PayrollTypeCode in (5)
--                        and HPT.IsEmployeePart = 1
--                        and HPT.IsVisiable = 1
--                    and LPD.LoanPaymentDate <= @fullDate 
--						--  And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
--					{0}
--					AND LPD.SalaryHeaderID is null
--                        and LSD.LoanSanctionHeaderID = LPD.LoanSanctionHeaderID
--                      and LPD.Status = 1
--					  AND LSD.PayrollTypeCode=tab.PayrollTypeCode
--					  AND HPT.Description=tab.Description
--					  AND LSH.PersonID=tab.PersonID  


--group by HPT.Description,HPT.PayrollTypeCode
--               ,LPD.Currentbalance,LPD.PreviousBalance, LSH.PersonID           
--),0) as PMDed
--,LoanAmount
,(case when LoanAmount != 0 then LoanAmount else AdavanceAmount end) as LoanAmount
,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) --- tab.CMDed 
end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    -- and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
		and (HRMS_SalaryHeader.SalaryDate between (select Hrms_Set_TaxYear.FromDate from Hrms_Set_TaxYear where @fullDate between FromDate and ToDate)
	and @fullDate)
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDed


,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description not like '%loan%'
							
	group by HRMS_SalaryDetail.PayrollTypeID


UNION ALL

Select case when (sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed) = 0
then sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) else 
sum(HRMS_LoanPaymentDetail.LoanDisburstAmount) - tab.CMDed end as YTDDed  
    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_Set_Employee,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
        and HRMS_SalaryHeader.SalaryDate <= @fullDate
    and HRMS_SalaryDetail.Status = 1
	AND HRMS_SalaryHeader.PersonID=tab.PersonID  
	AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
	AND HRMS_PayrollType.Description=tab.Description

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID
	AND HRMS_PayrollType.Description like '%loan%'
							
	and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 						
	and HRMS_LoanSanctionDetail.LoanStartDate = tab.LoanStartDate

	group by HRMS_SalaryDetail.PayrollTypeID
	) as YTDDedComplete




from (

Select distinct HRMS_LoanSanctionDetail.LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    SUM(HRMS_SalaryDetail.Amount) CMDed  ,
    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                                                                   
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
    --and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--						and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


--						and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
--						and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
--						and HRMS_Set_Employee.Status = 1
--						And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)

--						)

--,0) LoanAmount

,
        ISNULL(( select  sum(loanamount) 
			from 
        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail as InnerHRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType

            where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and HRMS_LoanSanctionHeader.CompanyID = InnerHRMS_LoanSanctionDetail.CompanyID
        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
        and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID						
		and  InnerHRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
		and HRMS_LoanSanctionHeader.Status = 1
		and InnerHRMS_LoanSanctionDetail.Status = 1
						
		and InnerHRMS_LoanSanctionDetail.LoanStartDate = HRMS_LoanSanctionDetail.LoanStartDate
		and InnerHRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
		and InnerHRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID

		and InnerHRMS_LoanSanctionDetail.LoanStartDate <= @fullDate

		and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
		and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
		and HRMS_Set_Employee.Status = 1
		--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
		{0}
		and InnerHRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
		and HRMS_PayrollType.Description not like '%Advance%'
		)

,0) LoanAmount


,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_Set_Employee,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionHeader.CompanyID = HRMS_LoanSanctionDetail.CompanyID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID

	and HRMS_LoanSanctionDetail.LoanAmount=HRMS_SalaryDetail.Amount

	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
	and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate


	and HRMS_Set_Employee.PersonID = HRMS_LoanSanctionHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_LoanSanctionHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description like '%Advance%'
	)

,0) AdavanceAmount

--,SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) as LoanDisburstAmount ,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance
--,HRMS_LoanPaymentDetail.LoanDisburstAmount,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance

    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail,HRMS_Set_Employee
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (5)
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
                     
    and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
	and HRMS_LoanPaymentDetail.CompanyID = HRMS_SalaryHeader.CompanyID 
    and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
	and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
    and HRMS_LoanPaymentDetail.Status = 1 and HRMS_LoanSanctionDetail.Status = 1
	and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						
	and HRMS_LoanSanctionDetail.PayrollTypeID =  HRMS_SalaryDetail.PayrollTypeID 
		and HRMS_LoanPaymentDetail.LoanDisburstAmount = HRMS_SalaryDetail.Amount

	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
						

	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
	{0}
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

--and HRMS_LoanPaymentDetail.Currentbalance != 0


group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,HRMS_SalaryHeader.PersonID
                        ,HRMS_SalaryDetail.Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode
                        ,HRMS_LoanPaymentDetail.Currentbalance
						,HRMS_LoanSanctionDetail.LoanStartDate
						,HRMS_LoanSanctionDetail.LoanSanctionHeaderID
						,HRMS_LoanSanctionDetail.LoanSanctionDetailID
						,HRMS_LoanSanctionDetail.CompanyID
                        
union all

Select distinct NULL as LoanStartDate, HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

    HRMS_SalaryHeader.PersonID ,
    HRMS_SalaryDetail.Amount CMDed  ,
    ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                       
                       
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate <= @fullDate
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
	and SD.SalaryDetailID = HRMS_SalaryDetail.SalaryDetailID
    ),0)PMDed,
                        
    HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
    where 
    SH.SalaryHeaderID = SD.SalaryHeaderID 
    and SH.SalaryDate < @fullDate
                        
    and SH.PersonID = HRMS_SalaryHeader.PersonID 
    AND SD.Status =1
    AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
    and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
    ),0) TMDed
--						,
--                       ISNULL(( select sum(loanamount) from 
--                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail
--                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
--                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
--                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
--						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
--                        and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
--						and HRMS_LoanSanctionHeader.Status = 1
--						and HRMS_LoanSanctionDetail.Status = 1
--						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
--						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate
--						)

--,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
					and HRMS_LoanSanctionDetail.LoanStartDate < DATEADD(month, DATEDIFF(month, 0, @fullDate), 0)

		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)

,0) LoanAmount

,
    ISNULL(( select sum(loanamount) from 
    HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail,HRMS_PayrollType
    where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
    and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
    and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
	and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
    and  HRMS_LoanSanctionDetail.CompanyID=HRMS_SalaryDetail.CompanyID
	and HRMS_LoanSanctionHeader.Status = 1
	and HRMS_LoanSanctionDetail.Status = 1
	--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
			and Month(HRMS_LoanSanctionDetail.LoanStartDate) = Month(@fullDate)
	and Year(HRMS_LoanSanctionDetail.LoanStartDate) = Year(@fullDate)
		and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
	and HRMS_PayrollType.Description not like '%Advance%'
	)


,0) AdvanceAmount


    from 
    HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType, HRMS_Set_Employee
                        
    where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
    and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
    and HRMS_PayrollType.PayrollTypeCode in (2,3)
	and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
                        
	and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
	and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
	and HRMS_Set_Employee.Status = 1
	--And (HRMS_Set_Employee.StatusID=@StatusID or @StatusID is null)
    {0}
    and HRMS_PayrollType.IsEmployeePart = 1
    and HRMS_PayrollType.IsVisiable = 1
    and HRMS_SalaryHeader.SalaryDate = @fullDate
    and HRMS_SalaryDetail.Status = 1
	and HRMS_SalaryHeader.CompanyID = @CompanyID
	and HRMS_SalaryHeader.PeriodID = @PeriodID

	and HRMS_PayrollType.Description not like 'EOBI Company'
						
) as tab
--where tab.PersonID = 552

)t";

                #endregion

                if (objConnection == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_EmpSalarySanghar, sb2), sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query_EmpSalarySanghar, sb2), sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }

                if (objConnection == null)
                    dt3 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalary_Sanghar, LoanAndRecoveryFilter), sqlparams).Tables[0];
                else
                    dt3 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalary_Sanghar, LoanAndRecoveryFilter), sqlparams).Tables[0];

                try
                {

                    dt3.TableName = "tblLoanAndRecoveryReport";
                    DataTable dt4 = dt3.Copy();

                    ds.Tables.Add(dt4);
                }
                catch { }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet Salaryjournalvoucher_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, Nullable<int> LocationID, Nullable<int> DepartmentID, Nullable<int> EmployeeStatus,
            Nullable<int> designation, string employeestatusidies, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            //DataTable ds1 = null;

            //DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
               // sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                //if (EmployeeTypeID > 0)
                //    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                //else
                //    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();

                if (DepartmentID != null)
                    sqlparams[9] = new SqlParameter("@DepartmentID", DepartmentID);
                else
                    sqlparams[9] = new SqlParameter("@DepartmentID", DBNull.Value);
                //if (EmployeeStatus != null)
                //    sqlparams[5] = new SqlParameter("@StatusID", EmployeeStatus);
                //else
                //    sqlparams[5] = new SqlParameter("@StatusID", DBNull.Value);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (designation != null)
                {

                    sb.Append(" and HRMS_Set_Employee.FK_JobDesignationID =  " + designation + " ");
                }
                //if (EmployeeStatus > 0)
                //{
                //    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                //}
                if (employeestatusidies != "")
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID in " + employeestatusidies.ToString() + " ");
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_salaryvoucherreport, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_salaryvoucherreport, sb, sb1, sb2, sb3), sqlparams);
                }

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet SalaryvoucherIncrement_ReportData(Nullable<int> companyID, Nullable<int> periodID, int totalNoOfDays, Nullable<DateTime> Date_From,Nullable<DateTime> EffectiveDate_From, Nullable<int> emplyeeID, Nullable<int> LocationID, Nullable<int> DepartmentID, Nullable<int> EmployeeStatus,
           Nullable<int> designation, string employeestatusidies, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            //DataTable ds1 = null;

            //DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[13];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                // sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                //if (EmployeeTypeID > 0)
                //    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                //else
                //    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();

                if (DepartmentID != null)
                    sqlparams[9] = new SqlParameter("@DepartmentID", DepartmentID);
                else
                    sqlparams[9] = new SqlParameter("@DepartmentID", DBNull.Value);

                sqlparams[10] = new SqlParameter("@EffectiveMonth", String.Format("{0:MMMM}", EffectiveDate_From));
                sqlparams[11] = new SqlParameter("@EffectiveYear", EffectiveDate_From.Value.Year.ToString());
                sqlparams[12] = new SqlParameter("@TotalNoOfDays", totalNoOfDays);
                //if (EmployeeStatus != null)
                //    sqlparams[5] = new SqlParameter("@StatusID", EmployeeStatus);
                //else
                //    sqlparams[5] = new SqlParameter("@StatusID", DBNull.Value);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (designation != null)
                {

                    sb.Append(" and HRMS_Set_Employee.FK_JobDesignationID =  " + designation + " ");
                }
                //if (EmployeeStatus > 0)
                //{
                //    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                //}
                if (employeestatusidies != "")
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID in " + employeestatusidies.ToString() + " ");
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_salaryvoucherIncrementreport, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_salaryvoucherIncrementreport, sb, sb1, sb2, sb3), sqlparams);
                }

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public DataSet  EmployeeSalary_ReportData_AlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> LoanNEwDate, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
    SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0, Nullable<Guid> UserID = null)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;
            DataTable dt3 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID_", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID_", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID_", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID_", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month_", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months_", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year_", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date_", startDate);

                sqlparams[7] = new SqlParameter("@fullDate_", startDate);

                sqlparams[8] = new SqlParameter("@loanDate_", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID_", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode_", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode_", DBNull.Value);

                sqlparams[11] = new SqlParameter("@LoanNewDate_", LoanNEwDate);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                StringBuilder sb3 = new StringBuilder();
                StringBuilder LoanAndRecoveryFilter = new StringBuilder();

                if (EmpStatusID > 0)
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                    sb3.Append(" and SE.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb2.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb3.Append(" and SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }


                if (LocationID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                //if (DepartmentID != null)
                //{
                //    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                //}
                if (emplyeeID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                //if (payrolltypeID != null)
                //{
                //    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                //}
                if (EmpStatusID != null && EmpStatusID > 0)
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    LoanAndRecoveryFilter.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_AlNoor, sb, sb1, sb2, sb3), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_AlNoor, sb, sb1, sb2, sb3), sqlparams);
                }


                //if (objConnection == null)
                //    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //else
                //{
                //    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary, sqlparams).Tables[0];
                //}

                if (objConnection == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary_Alnoor, sb2), sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalary_Alnoor, sb2), sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }


                if (objConnection == null)
                    dt3 = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalary, LoanAndRecoveryFilter), sqlparams).Tables[0];
                else
                    dt3 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportDataEmployeeSalary, LoanAndRecoveryFilter), sqlparams).Tables[0];


                try
                {

                    dt3.TableName = "tblLoanAndRecoveryReport";
                    DataTable dt4 = dt3.Copy();

                    ds.Tables.Add(dt4);
                }
                catch { }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public DataSet EmployeeSalary_ReportDataAR(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
        SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                if (EmpStatusID > 0)
                    sqlparams[11] = new SqlParameter("@StatusID", EmpStatusID);
                else
                    sqlparams[11] = new SqlParameter("@StatusID", DBNull.Value);



                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_AR, sb, sb1), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData_AR, sb, sb1), sqlparams);
                }

                if (objConnection == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalaryHSM_AR, sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpSalaryHSM_AR, sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public DataTable SummaryStaffAdvances_ReportData(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", Date_From);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SummaryStaffAdvances).Replace("@CompanyID", companyID), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SummaryStaffAdvances).Replace("@CompanyID", companyID), sqlparams);
                }

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

        public DataTable StaffAdvances_ReportData(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", Date_From);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_StaffAdvancesHO2).Replace("@CompanyID", companyID), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_StaffAdvancesHO2).Replace("@CompanyID", companyID), sqlparams);
                }

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

        public DataTable StaffAdvances_ReportDataSite(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", Date_From);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_StaffAdvancesSite2).Replace("@CompanyID", companyID), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_StaffAdvancesSite2).Replace("@CompanyID", companyID), sqlparams);
                }

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


        public DataSet EmployeeSalaryForEmail_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
    SqlConnection objConnection = null, SqlTransaction objTransaction = null, Nullable<int> LocationID = 0, Nullable<int> DivisionID = 0, Nullable<int> EmployeeTypeID = 0, Nullable<int> CurrentFiscalID = null)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                if (EmpStatusID.HasValue)
                    sqlparams[11] = new SqlParameter("@StatusID", EmpStatusID);
                else
                    sqlparams[11] = new SqlParameter("@StatusID", DBNull.Value);



                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }

                string query = @"
Select Distinct Description,PayrollTypeCode,PersonID , CMDed ,ISNULL ((Select 
                        SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) CMDed  
                        from HRMS_LoanPaymentDetail
						INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanPaymentDetail.LoanSanctionHeaderID
						AND  HRMS_LoanSanctionDetail.LoanSanctionDetailID=HRMS_LoanPaymentDetail.LoanSanctionDetailID
						INNER JOIN HRMS_LoanSanctionHeader ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
						AND  HRMS_LoanSanctionDetail.LoanSanctionHeaderID=HRMS_LoanSanctionHeader.LoanSanctionHeaderID
					 AND  HRMS_LoanSanctionDetail.CompanyID=HRMS_LoanSanctionHeader.CompanyID
                   	INNER JOIN HRMS_PayrollType ON HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_PayrollType.PayrollTypeID
						
			
                        where
                       HRMS_PayrollType.PayrollTypeCode in (5)
                        and HRMS_PayrollType.IsEmployeePart = 1
                        and HRMS_PayrollType.IsVisiable = 1
                    and HRMS_LoanPaymentDetail.LoanPaymentDate between DATEADD(month, DATEDIFF(month, 0, @fullDate), 0) and @fullDate 
					AND HRMS_LoanPaymentDetail.SalaryHeaderID is null
                        and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
                      and HRMS_LoanPaymentDetail.Status = 1
					  AND tab.PayrollTypeCode=HRMS_PayrollType.PayrollTypeCode
					  AND tab.Description=HRMS_PayrollType.Description
					    AND tab.PersonID=HRMS_LoanSanctionHeader.PersonID    

group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode, HRMS_LoanSanctionHeader.PersonID           
),0) as CMManualDed , PMDed+ISNULL ((Select 
                        SUM(LPD.LoanDisburstAmount) CMDed  
                        from HRMS_LoanPaymentDetail LPD
						INNER JOIN HRMS_LoanSanctionDetail LSD ON LSD.LoanSanctionHeaderID=LPD.LoanSanctionHeaderID
						AND  LSD.LoanSanctionDetailID=LPD.LoanSanctionDetailID
						INNER JOIN HRMS_LoanSanctionHeader LSH ON LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
						AND  LSD.LoanSanctionHeaderID=LSH.LoanSanctionHeaderID
					 AND  LSD.CompanyID=LSH.CompanyID
                   	INNER JOIN HRMS_PayrollType HPT ON LSD.PayrollTypeID=HPT.PayrollTypeID
						
			
                        where
                       HPT.PayrollTypeCode in (5)
                        and HPT.IsEmployeePart = 1
                        and HPT.IsVisiable = 1
                    and LPD.LoanPaymentDate <= @fullDate 
					AND LPD.SalaryHeaderID is null
                        and LSD.LoanSanctionHeaderID = LPD.LoanSanctionHeaderID
                      and LPD.Status = 1
					  AND LSD.PayrollTypeCode=tab.PayrollTypeCode
					  AND HPT.Description=tab.Description
					  AND LSH.PersonID=tab.PersonID  


group by HPT.Description,HPT.PayrollTypeCode, LSH.PersonID           
),0) as PMDed,LoanAmount
,(Select sum(HRMS_SalaryDetail.Amount) as  YTDDed  
                        from 
                        HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType
                        
                        where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
                        and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
                        and HRMS_PayrollType.PayrollTypeCode in (2,3,5)
                        
                        and HRMS_PayrollType.IsEmployeePart = 1
                        and HRMS_PayrollType.IsVisiable = 1
                        and year(HRMS_SalaryHeader.SalaryDate) = year(@fullDate)
                        and HRMS_SalaryDetail.Status = 1
						AND HRMS_SalaryHeader.PersonID=tab.PersonID  
						AND HRMS_PayrollType.PayrollTypeCode = tab.PayrollTypeCode
						AND HRMS_PayrollType.Description=tab.Description
						) as YTDDed


from (

Select distinct HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

                        HRMS_SalaryHeader.PersonID ,
                        SUM(HRMS_SalaryDetail.Amount) CMDed  ,
                       ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                       
                       
                        where 
                        SH.SalaryHeaderID = SD.SalaryHeaderID 
                        and SH.SalaryDate <= @fullDate
                        and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
                        AND SD.Status =1
                        AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
                        and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                       
                        ),0)PMDed,
                        
                       HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
                        where 
                        SH.SalaryHeaderID = SD.SalaryHeaderID 
                        and SH.SalaryDate < @fullDate
                        
                        and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        AND SD.Status =1
                        AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
                        and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
                        ),0) TMDed,
                       ISNULL(( select sum(loanamount) from 
                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail
                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
						and HRMS_LoanSanctionHeader.Status = 1
						and HRMS_LoanSanctionDetail.Status = 1
						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate
						)

,0) LoanAmount
--,SUM(HRMS_LoanPaymentDetail.LoanDisburstAmount) as LoanDisburstAmount ,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance
--,HRMS_LoanPaymentDetail.LoanDisburstAmount,HRMS_LoanPaymentDetail.PreviousBalance,HRMS_LoanPaymentDetail.Currentbalance

                        from 
                        HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType,HRMS_LoanSanctionDetail,HRMS_LoanPaymentDetail,HRMS_Set_Employee
                        
                        where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
                        and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
                        and HRMS_PayrollType.PayrollTypeCode in (5)
                        and HRMS_PayrollType.IsEmployeePart = 1
                        and HRMS_PayrollType.IsVisiable = 1
                        and HRMS_SalaryHeader.SalaryDate = @fullDate
                        and HRMS_SalaryDetail.Status = 1
                        and HRMS_LoanPaymentDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID 
                        and HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
                        and HRMS_LoanPaymentDetail.Status = 1

						and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
						and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
						

						and HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
						and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
						and HRMS_Set_Employee.Status = 1
--and HRMS_LoanPaymentDetail.Currentbalance != 0


group by HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,HRMS_SalaryHeader.PersonID
                        ,HRMS_SalaryDetail.Amount,HRMS_SalaryDetail.PayrollTypeID,HRMS_SalaryDetail.PayrollTypeCode
                        ,HRMS_LoanPaymentDetail.Currentbalance,HRMS_LoanPaymentDetail.PreviousBalance
                        
                        
union all

Select distinct HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeCode,

                        HRMS_SalaryHeader.PersonID ,
                        HRMS_SalaryDetail.Amount CMDed  ,
                       ISnull((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                       
                       
                        where 
                        SH.SalaryHeaderID = SD.SalaryHeaderID 
                        and SH.SalaryDate <= @fullDate
                        and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        
                        AND SD.Status =1
                        AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
                        and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                       
                        ),0)PMDed,
                        
                       HRMS_SalaryDetail.Amount + ISNULL((select sum(SD.Amount) from  HRMS_SalaryDetail SD,HRMS_SalaryHeader SH
                       
                      
                        where 
                        SH.SalaryHeaderID = SD.SalaryHeaderID 
                        and SH.SalaryDate < @fullDate
                        
                        and SH.PersonID = HRMS_SalaryHeader.PersonID 
                        AND SD.Status =1
                        AND SD.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
                        and SD.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
                        
                        ),0) TMDed,
                       ISNULL(( select sum(loanamount) from 
                        HRMS_LoanSanctionHeader,HRMS_LoanSanctionDetail
                        where HRMS_LoanSanctionHeader.LoanSanctionHeaderID = HRMS_LoanSanctionDetail.LoanSanctionHeaderID
                        and HRMS_LoanSanctionHeader.PersonID = HRMS_SalaryHeader.PersonID
                        and HRMS_LoanSanctionDetail.PayrollTypeID=HRMS_SalaryDetail.PayrollTypeID
						and  HRMS_LoanSanctionDetail.PayrollTypeCode=HRMS_SalaryDetail.PayrollTypeCode
						and HRMS_LoanSanctionHeader.Status = 1
						and HRMS_LoanSanctionDetail.Status = 1
						--and HRMS_LoanSanctionDetail.IsDeductionCompleted = 0
						and HRMS_LoanSanctionDetail.LoanStartDate <= @fullDate
						)

,0) LoanAmount

                        from 
                        HRMS_SalaryHeader,HRMS_SalaryDetail,HRMS_PayrollType
                        
                        where HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID 
                        and HRMS_SalaryDetail.PayrollTypeID =HRMS_PayrollType.PayrollTypeID
                        and HRMS_PayrollType.PayrollTypeCode in (2,3)
                        
                        
                        and HRMS_PayrollType.IsEmployeePart = 1
                        and HRMS_PayrollType.IsVisiable = 1
                        and HRMS_SalaryHeader.SalaryDate = @fullDate
                        and HRMS_SalaryDetail.Status = 1
                        and HRMS_SalaryDetail.CompanyID = @CompanyID
						and HRMS_SalaryHeader.CompanyID = @CompanyID

) as tab

                    ";



                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1, "", ""), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportData, sb, sb1,"",""), sqlparams);
                }


                if (objTransaction == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams).Tables[0];
                }

                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public DataSet EmployeeSalary_ReportDataTwo(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID,
           SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0)
        {
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportDataTwo, sb, sb1), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalary_ReportDataTwo, sb, sb1), sqlparams);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable EmployeeSalarySheet_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int orderbyID, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, bool IsDKSM = false, bool showNegative = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder OrderBy = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (orderbyID == 1)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.SectionID,0) asc  ");
                }
                else if (orderbyID == 2)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.DepartmentID,0) asc ");
                }
                else
                {
                    OrderBy.Append(" )FinalTab Order bY cast (FinalTab.SortCategory as int)");
                }

                // const string OrderBy = @")FinalTab Order bY cast (FinalTab.SortCategory as int)"; //EmployeeCode 
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";
                
                
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SE.FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }

                if (showNegative == true)
                {
                    sb2.Append("INNER");
                }
                else
                { 
                    sb2.Append("LEFT");
                }

                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        if (salaryconnectparameter == true)
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi + OrderByConnect, sb, sb1, sb2), sqlparams);
                        }
                        else
                        {
                            if (IsDKSM)
                                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForDKSM + queryforBandhi + OrderBy, sb, sb1), sqlparams);
                            else
                                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi + OrderBy, sb, sb1, sb2), sqlparams);
                        }
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi, sb, "", ""), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                if (showNegative == true)
                {
                    var deductionTypes = new[] { "Gross Payable", "Deduction Amount", "Net Payable" };

                    dt = dt.AsEnumerable()
                      .Where(row => Array.Exists(deductionTypes, type => type == row.Field<string>("DeductionType")))
                      .CopyToDataTable();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable EmployeeSalarySheet_ReportDataTSM(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int orderbyID, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, bool IsDKSM = false, bool showNegative = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder OrderBy = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (orderbyID == 1)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.SectionID,0) asc  ");
                }
                else if (orderbyID == 2)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.DepartmentID,0) asc ");
                }
                else
                {
                    OrderBy.Append(" )FinalTab Order bY cast (FinalTab.SortCategory as int)");
                }

                // const string OrderBy = @")FinalTab Order bY cast (FinalTab.SortCategory as int)"; //EmployeeCode 
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SE.FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }

                if (showNegative == true)
                {
                    sb2.Append("INNER");
                }
                else
                {
                    sb2.Append("LEFT");
                }

                if (objConnection == null)

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForTSM_ReportData + queryforBandhi + OrderBy, sb, sb1, sb2), sqlparams);
                   
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                if (showNegative == true)
                {
                    var deductionTypes = new[] { "Gross Payable", "Deduction Amount", "Net Payable" };

                    dt = dt.AsEnumerable()
                      .Where(row => Array.Exists(deductionTypes, type => type == row.Field<string>("DeductionType")))
                      .CopyToDataTable();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable EmployeeSalarySheet_ReportDataNew(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int orderbyID, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, string JobCategoryID = "", Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> PayrollGroupID = null, string EmployeeStatus = "", string AppendSerialNo = null, Nullable<Guid> UserID = null, bool showNegative = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder OrderBy = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (orderbyID == 1)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.SectionID,0) asc  ");
                }
                else if (orderbyID == 2)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.DepartmentID,0) asc ");
                }
                else
                {
                    OrderBy.Append(" )FinalTab Order bY cast (FinalTab.SortCategory as int)");
                }
                
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != "")
                {
                    sb.Append(" and HRMS_SE.StatusID IN (" + EmployeeStatus + ") ");
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID != "")
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID IN (" + JobCategoryID + ") ");
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SE.FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }

                if (showNegative == true)
                {
                    sb2.Append("INNER");
                }
                else
                {
                    sb2.Append("LEFT");
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi + OrderBy, sb, sb1, sb2), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi, sb, "", ""), sqlparams);
                    
    
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                if (showNegative == true)
                {
                    var deductionTypes = new[] { "Gross Payable", "Deduction Amount", "Net Payable" };

                    dt = dt.AsEnumerable()
                      .Where(row => Array.Exists(deductionTypes, type => type == row.Field<string>("DeductionType")))
                      .CopyToDataTable();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable EmployeeSalarySheet_ReportData_Sanghar(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int orderbyID, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, bool IsDKSM = false, bool showNegative = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder OrderBy = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (orderbyID == 1)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.SectionID,0) asc  ");
                }
                else if (orderbyID == 2)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.DepartmentID,0) asc ");
                }
                else
                {
                    OrderBy.Append(" )FinalTab Order bY cast (FinalTab.SortCategory as int)");
                }

                // const string OrderBy = @")FinalTab Order bY cast (FinalTab.SortCategory as int)"; //EmployeeCode 
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                StringBuilder sb2 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SE.FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }

                if (showNegative == true)
                {
                    sb2.Append("INNER");
                }
                else
                {
                    sb2.Append("LEFT");
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_Sanghar + queryforBandhi + OrderBy, sb, sb1, sb2), sqlparams);
                }

                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi, sb, "", ""), sqlparams);
                }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                if (showNegative == true)
                {
                    var deductionTypes = new[] { "Gross Payable", "Deduction Amount", "Net Payable" };

                    //dt = dt.AsEnumerable()
                    //  .Where(row => Array.Exists(deductionTypes, type => type == row.Field<string>("DeductionType")))
                    //  .CopyToDataTable();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable EmployeeSalarySheet_ReportDataDKSMNew(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int orderbyID, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, bool IsDKSM = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder OrderBy = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (orderbyID == 1)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.SectionID,0) asc  ");
                }
                else if (orderbyID == 2)
                {
                    OrderBy.Append(" )FinalTab Order bY ISNULL(FinalTab.DepartmentID,0) asc ");
                }
                else
                {
                    OrderBy.Append(" )FinalTab Order bY cast (FinalTab.SortCategory as int)");
                }

                // const string OrderBy = @")FinalTab Order bY cast (FinalTab.SortCategory as int)"; //EmployeeCode 
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SE.FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }
                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForDKSMNew_ReportData + queryforBandhi + OrderBy, sb, sb1), sqlparams);
                        }
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeSalarySheet_ReportDataDKSM(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforDKSM = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, bool IsDKSM = false)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }
                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (IsDKSM == true)
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForDKSM + queryforDKSM + OrderBy, sb, sb1), sqlparams);

                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeSalarySheet_ReportDataWithStatus(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null, Nullable<int> DepartmentID = null,string ReportName = "")
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                //const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";

                string OrderBy = "";
                
                if (ReportName.Contains("DesignationWise"))
                {
                    OrderBy = ")FinalTab Order bY FinalTab.JobDesignationName ,cast (FinalTab.EmployeeCode as int)";
                    AppendSerialNo = "DENSE_RANK() OVER(PARTITION BY JobDesignationID ORDER BY FinalTab.JobDesignationName,cast (FinalTab.EmployeeCode as int) ) AS SeriaLNo";
                }
                else
                {
                    OrderBy = ")FinalTab Order bY FinalTab.EmployementStatus ,cast (FinalTab.EmployeeCode as int)";
                    AppendSerialNo = "DENSE_RANK() OVER(PARTITION BY EmployementStatus ORDER BY FinalTab.EmployementStatus,cast (FinalTab.EmployeeCode as int) ) AS SeriaLNo";
                }
                


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }

                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SE].FK_DepartmentID = " + DepartmentID);
                }

                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }
                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        if (salaryconnectparameter == true)
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi + OrderByConnect, sb, sb1), sqlparams);
                        }
                        else
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportDataWithStatus + queryforBandhi + OrderBy, sb, sb1), sqlparams);
                        }
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeSalarySheet_ReportData_AlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0,Nullable<int> DepartmentID = null,int CostCenterID = 0,int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SE].FK_DepartmentID = " + DepartmentID);
                }
                if (CostCenterID > 0)
                {
                    sb.Append(" AND [HRMS_SE].CostCenterID = " + CostCenterID);
                }
                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }
                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        if (salaryconnectparameter == true)
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForAlNoor_ReportData + queryforBandhi + OrderByConnect, sb, sb1), sqlparams);
                        }

                        else
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForAlNoor_ReportData + queryforBandhi + OrderBy, sb, sb1), sqlparams);
                        }
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForAlNoor_ReportData + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable SalaryArrearReport_AlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> DepartmentID = null, int CostCenterID = 0, int PersonID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SE].FK_DepartmentID = " + DepartmentID);
                }
                if (CostCenterID > 0)
                {
                    sb.Append(" AND [HRMS_SE].CostCenterID = " + CostCenterID);
                }
                if (PersonID > 0)
                {
                    sb.Append(" AND HRMS_SE.PersonID = " + PersonID);
                }
                if (AppendSerialNo != null)
                {
                    sb1.Append(AppendSerialNo);
                }
                if (objConnection == null)

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_SalaryArrearForAlNoor_ReportData + queryforBandhi + OrderBy, sb, sb1), sqlparams);
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForAlNoor_ReportData + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeArrears_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromMonth", String.Format("{0:MM}", Date_From));
                sqlparams[3] = new SqlParameter("@FromYear", Date_From.Value.Year.ToString());
                const string OrderBy = @")FinalTab Order bY FinalTab.PersonID,FinalTab.StartingMonth";
                StringBuilder sb = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }

                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" AND HRMS_SE.StatusID  = " + EmployeeStatus);
                }
                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeArrears_ReportData + queryforBandhi + OrderBy, sb), sqlparams);
                    }
                    else
                    {
                        // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeArrears_ReportData + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }
        public static DataTable SectionWiseSalarySheet_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Section_Wise_Salary_Sheet_Report, sqlparams);
                else
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Section_Wise_Salary_Sheet_Report, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }


        public static DataTable PayRollVarienceReport_ReportData(Guid UserID, Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, int? Department, int? Status, bool AllDepartment = false, int LocationID = 0, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month1", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year1", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@Month2", String.Format("{0:MMMM}", Date_To));
                sqlparams[5] = new SqlParameter("@Year2", Date_To.Value.Year.ToString());
                string filter = "";
                if (PersonID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[6] = new SqlParameter("@PersonID", 0);
                }

                if (Department != null && Department > 0)
                {
                    sqlparams[7] = new SqlParameter("@DepartmentID", Department);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DepartmentID", 0);
                }

                sqlparams[8] = new SqlParameter("@UserID", UserID);

                if (Status != null && Status > 0)
                {
                    sb2.Append(" and HRMS_EmployeeInEmployement_Status.StatusID = " + Status);
                }
                else
                {
                    sb2.Append(" and HRMS_EmployeeInEmployement_Status.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (AllDepartment == true)
                {

                    sb1.Append("Order by FintabFinal.PersonID asc");
                }


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PayRollVarienceReportData + filter, sb.ToString(), sb1.ToString(), sb2.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PayRollVarienceReportData + filter, sb.ToString(), sb1.ToString(), sb2.ToString()), sqlparams);


                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }


        public static DataSet PayRollVarienceReport_ReportData_DepartmentandCategory(Guid UserID, Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, int? Department, int? Status, bool AllDepartment = false, int LocationID = 0, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month1", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year1", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@Month2", String.Format("{0:MMMM}", Date_To));
                sqlparams[5] = new SqlParameter("@Year2", Date_To.Value.Year.ToString());
                string filter = "";
                if (PersonID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[6] = new SqlParameter("@PersonID", 0);
                }

                if (Department != null && Department > 0)
                {
                    sqlparams[7] = new SqlParameter("@DepartmentID", Department);
                }
                else
                {
                    sqlparams[7] = new SqlParameter("@DepartmentID", 0);
                }

                sqlparams[8] = new SqlParameter("@UserID", UserID);

                if (Status != null && Status > 0)
                {
                    sb2.Append(" and HRMS_EmployeeInEmployement_Status.StatusID = " + Status);
                }
                else
                {
                    sb2.Append(" and HRMS_EmployeeInEmployement_Status.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (AllDepartment == true)
                {

                    sb1.Append("Order by FintabFinal.PersonID asc");
                }


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PayRollVarienceReportData_DepartandCategory + filter, sb.ToString(), sb1.ToString(), sb2.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PayRollVarienceReportData_DepartandCategory + filter, sb.ToString(), sb1.ToString(), sb2.ToString()), sqlparams);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }



        public static DataTable AllowancesPayRollVarienceReport_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month1", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year1", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@Month2", String.Format("{0:MMMM}", Date_To));
                sqlparams[5] = new SqlParameter("@Year2", Date_To.Value.Year.ToString());

                if (PersonID > 0)
                {
                    sb.Append(" and HRMS_SalaryHeader.PersonID = " + PersonID);
                }
                if (PersonID > 0)
                {
                    sb1.Append(" and sh.PersonID = " + PersonID);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_AllowancesPayRollVarienceReportData, sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_AllowancesPayRollVarienceReportData, sb.ToString(), sb1.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable LoanAndRecoveryRegister_ReportData(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, bool? IsDetail = false, Nullable<Guid> UserID = null, SqlConnection objConnection = null, bool Active = false, bool InActive = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
               

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@Month1", String.Format("{0:MMMM}", Date_From));
                //sqlparams[2] = new SqlParameter("Month1", Date_From);
                //sqlparams[3] = new SqlParameter("@Year1", Date_From.Value.Year.ToString());
                //sqlparams[4] = new SqlParameter("@Month2", String.Format("{0:MMMM}", Date_To));
                //sqlparams[5] = new SqlParameter("@Year2", Date_To.Value.Year.ToString());
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                }
                if (emplyeeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                if (payrolltypeID != null)
                {
                    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }


                if (Active == false && InActive == true)
                {
                    sb.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && InActive == false)
                {
                    sb.Append(" and HRMS_Set_Employee.isActive = 1");
                }

                companyID = "(" + companyID + ")";

                if (IsDetail == true)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportData_Detail.Replace("@CompanyID",companyID), sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportData_Detail.Replace("@CompanyID", companyID), sb), sqlparams);
                }

                else
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportData.Replace("@CompanyID", companyID), sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterReportData.Replace("@CompanyID", companyID), sb), sqlparams);
                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable LoanMovementandLoanMovementExecutiveBoth_ReportData(string companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, bool rbLoanMovementExecutiveSalary = false, Nullable<Guid> UserID = null, SqlConnection objConnection = null, bool Active = false, bool inActive = false)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                }
                if (emplyeeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                if (rbLoanMovementExecutiveSalary)
                    sb.Append(" and HRMS_EmployeePayroll.CalculatedAmount + HRMS_EmployeePayroll.ManualAmount >= 100000 ");
                else
                    sb.Append(" and HRMS_EmployeePayroll.CalculatedAmount + HRMS_EmployeePayroll.ManualAmount < 100000 ");
                if (payrolltypeID != null)
                {
                    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (Active == false && inActive == true)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 0");
                }
                else if (Active == true && inActive == false)
                {
                    sb1.Append(" and HRMS_Set_Employee.isActive = 1");
                }


                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanMovementandLoanMovementExecutiveBothReportData.Replace("@CompanyID",companyID), sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanMovementandLoanMovementExecutiveBothReportData.Replace("@CompanyID", companyID), sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable DepartmentWiseSalaryMonthWise_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SH.FK_DepartmentID = " + DepartmentID);
                }
                if (emplyeeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonID = " + emplyeeID);
                }

                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DepartmentWiseSalaryMonthWise_ReportData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DepartmentWiseSalaryMonthWise_ReportData, sb), sqlparams);


                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable Get_DataForGrossSalaryMonthWise_Report(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                //if (DepartmentID != null)
                //{
                //    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                //}
                //if (emplyeeID > 0)
                //{
                //    sb.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                //}
                //if (payrolltypeID != null)
                //{
                //    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                //}
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GrossSalaryMonthWiseReportData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GrossSalaryMonthWiseReportData, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable Get_DataForNoTimeOutReportDaily_Report(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and EP.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" and (EP.FK_DepartmentID =  " + DepartmentID + " ) ");

                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and EP.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and EP.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_ForNoOverTimeReportData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_ForNoOverTimeReportData, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }


        public static DataTable LoanAndRecoveryRegisterDetail_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@Month1", String.Format("{0:MMMM}", Date_From));
                //sqlparams[2] = new SqlParameter("Month1", Date_From);
                //sqlparams[3] = new SqlParameter("@Year1", Date_From.Value.Year.ToString());
                //sqlparams[4] = new SqlParameter("@Month2", String.Format("{0:MMMM}", Date_To));
                //sqlparams[5] = new SqlParameter("@Year2", Date_To.Value.Year.ToString());
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                }
                if (emplyeeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonID = " + emplyeeID);
                }
                if (payrolltypeID != null)
                {
                    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + payrolltypeID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterDetail_ReportData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LoanAndRecoveryRegisterDetail_ReportData, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }


        public static DataSet EmployeeSalarySheet_DepartmentWiseReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentID, Nullable<int> DepartmentTo, Nullable<int> EmployeeStatus, SqlConnection objConnection = null, Nullable<Guid> UserID = null, int locationid = 0, int EmployeeTypeID = 0, int? ExpenseType = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);


                if (DepartmentID != null && DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                }
                else if (DepartmentID != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID >= " + DepartmentID);
                    sb3.Append(" AND SET_Department.DepartmentID >= " + DepartmentID);
                }
                else if (DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID <= " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID <= " + DepartmentTo);
                }
                    
                //if (EmployeeStatus != null)
                //    sqlparams[5] = new SqlParameter("@StatusID", EmployeeStatus);
                //else
                //    sqlparams[5] = new SqlParameter("@StatusID", DBNull.Value);

                if (locationid > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + locationid);
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + locationid);
                }

                if (EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (ExpenseType.HasValue)
                {
                    sb.Append(" and HRMS_SE.ExpenseTypeID = " + ExpenseType.Value);
                    sb1.Append(" and HRMS_Set_Employee.ExpenseTypeID = " + ExpenseType.Value);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);
                //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet EmployeeSalarySheet_DepartmentWiseReportDatav2(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentID, Nullable<int> DepartmentTo, string EmployeeStatus = "", SqlConnection objConnection = null, Nullable<Guid> UserID = null, int locationid = 0, int EmployeeTypeID = 0, int? ExpenseType = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);


                if (DepartmentID != null && DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                }
                else if (DepartmentID != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID >= " + DepartmentID);
                    sb3.Append(" AND SET_Department.DepartmentID >= " + DepartmentID);
                }
                else if (DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID <= " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID <= " + DepartmentTo);
                }

                if (locationid > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + locationid);
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + locationid);
                }

                if (EmployeeStatus != "")
                {
                    sb.Append(" and HRMS_SE.StatusID IN (" + EmployeeStatus + ") ");
                    sb1.Append(" and HRMS_Set_Employee.StatusID IN (" + EmployeeStatus + ") ");
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (ExpenseType.HasValue)
                {
                    sb.Append(" and HRMS_SE.ExpenseTypeID = " + ExpenseType.Value);
                    sb1.Append(" and HRMS_Set_Employee.ExpenseTypeID = " + ExpenseType.Value);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseData, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet EmployeeSalarySheet_DepartmentWiseReportDataSanghar(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> DepartmentID, Nullable<int> DepartmentTo, Nullable<int> EmployeeStatus, SqlConnection objConnection = null, Nullable<Guid> UserID = null, int locationid = 0, int EmployeeTypeID = 0, int? ExpenseType = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sb3 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);


                if (DepartmentID != null && DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID BETWEEN " + DepartmentID + " And " + DepartmentTo);
                }
                else if (DepartmentID != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID >= " + DepartmentID);
                    sb3.Append(" AND SET_Department.DepartmentID >= " + DepartmentID);
                }
                else if (DepartmentTo != null)
                {
                    sb2.Append(" AND HRMS_DP.DepartmentID <= " + DepartmentTo);
                    sb3.Append(" AND SET_Department.DepartmentID <= " + DepartmentTo);
                }
                
                if (locationid > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + locationid);
                    sb1.Append(" and HRMS_Set_Employee.FK_LocationID = " + locationid);
                }

                if (EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                    sb1.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                    sb1.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                    sb1.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (ExpenseType.HasValue)
                {
                    sb.Append(" and HRMS_SE.ExpenseTypeID = " + ExpenseType.Value);
                    sb1.Append(" and HRMS_Set_Employee.ExpenseTypeID = " + ExpenseType.Value);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseDataSanghar, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportDptWiseDataSanghar, sb.ToString(), sb1.ToString(), sb2.ToString(), sb3.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable EmployeeSalarySheet_DepartmentWiseReportDataNew(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> DepartmentID, Nullable<int> EmployeeStatus, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                if (DepartmentID != null)
                    sqlparams[4] = new SqlParameter("@DepartmentID", DepartmentID);
                else
                    sqlparams[4] = new SqlParameter("@DepartmentID", DBNull.Value);
                if (EmployeeStatus != null)
                    sqlparams[5] = new SqlParameter("@StatusID", EmployeeStatus);
                else
                    sqlparams[5] = new SqlParameter("@StatusID", DBNull.Value);



                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeSalarySheet_ReportDptWiseDataNew, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeeSalarySheet_ReportDptWiseDataNew, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeSalarySheet_SectionWiseReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> DepartmentID, Nullable<int> SectionID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                if (DepartmentID != null)
                {
                    sb.Append(" AND [SET_Department].DepartmentID = " + DepartmentID);
                }
                if (SectionID != null)
                {
                    sb.Append(" AND [SET_Section].SectionID = " + SectionID);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportSectionWiseData, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeSalarySheet_ReportSectionWiseData, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable IncomeTaxDeductionMonthWise(Nullable<int> companyID, int periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> StatusID, Nullable<int> PersonID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_IncomeTaxDedMonthWise, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_IncomeTaxDedMonthWise, sb.ToString()), sqlparams);


                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable IncomeTaxComputation_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? EmployeeID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder filter = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@SalaryDate", Date_From.Value.Year.ToString() + "-" + Date_From.Value.Month.ToString() + "-28 00:00:00");
                if (EmployeeID.HasValue)
                {
                    filter.Append(" AND HRMS_Set_Employee.PersonID =" + EmployeeID);
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_IncomeTaxComputation_ReportData, filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_IncomeTaxComputation_ReportData, filter), sqlparams);

                if (ds != null)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable EmployeeMonthlyDeduction_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null,
            int EmployeeTypeID = 0,
            int DepartmentID = 0,
            int DeductionTypeID = 0
            )
        {
            DataTable dt = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                if (EmployeeTypeID > 0)
                    sb.Append("And HRMS_SET_Employee.StatusID = " + EmployeeTypeID);

                if (DepartmentID > 0)
                    sb.Append("And HRMS_SET_Employee.FK_DepartmentID  = " + DepartmentID);

                if (DeductionTypeID > 0)
                    sb.Append("And HRMS_PayrollType.PayrollTypeID = " + DeductionTypeID);


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeMonthlyDeduction_ReportData,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,string.Format(Query_EmployeeMonthlyDeduction_ReportData,sb.ToString()), sqlparams);

                if (ds != null)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        #endregion

        public static DataTable EmployeeEarningCard_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> dateTo, Nullable<DateTime> dateFrom, SqlConnection objConnection = null, int? EmployeeIDFrom = 0, int? EmployeeIDTo = 0, int LocationID = 0)
        {


            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DateFrom", dateFrom);
                sqlparams[3] = new SqlParameter("@DateTo", dateTo);
                //sqlparams[4] = new SqlParameter("@empcodefrom", EmployeeIDFrom);
                //sqlparams[5] = new SqlParameter("@empcodeto", EmployeeIDTo);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();


                if (LocationID > 0)
                {
                    sb.Append("and total.LocationID = " + LocationID);
                }


                if (EmployeeIDFrom > 0 && EmployeeIDTo > 0)
                {
                    sb.Append("and total.PersonID between " + EmployeeIDFrom + " and " + EmployeeIDTo + " ");
                }
                if (EmployeeIDTo > 0 && EmployeeIDFrom == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDTo);
                }
                if (EmployeeIDFrom > 0 && EmployeeIDTo == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDFrom);
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_Report, sb), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_Report, sb.ToString()), sqlparams);

                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable EmployeeEarningCardDKSM_ReportData(string CNICNumber, Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> dateTo, Nullable<DateTime> dateFrom, SqlConnection objConnection = null, int? EmployeeIDFrom = 0, int? EmployeeIDTo = 0, int LocationID = 0)
        {


            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DateFrom", dateFrom);
                sqlparams[3] = new SqlParameter("@DateTo", dateTo);
                //sqlparams[4] = new SqlParameter("@empcodefrom", EmployeeIDFrom);
                //sqlparams[5] = new SqlParameter("@empcodeto", EmployeeIDTo);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();


                if (LocationID > 0)
                {
                    sb.Append("and total.LocationID = " + LocationID);
                }

                if (EmployeeIDFrom > 0 && EmployeeIDTo > 0)
                {
                    sb.Append("and total.PersonID between " + EmployeeIDFrom + " and " + EmployeeIDTo + " ");
                }
                if (EmployeeIDTo > 0 && EmployeeIDFrom == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDTo);
                }
                if (EmployeeIDFrom > 0 && EmployeeIDTo == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDFrom);
                }
                if (CNICNumber != "")
                {
                    sb1.Append(" and HRMS_SE.CNICNumber = " + CNICNumber);
                }
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_ReportDKSM, sb, sb1), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_ReportDKSM, sb.ToString(), sb1.ToString()), sqlparams);

                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataSet EmployeeEarningCardHSM_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> dateTo, Nullable<DateTime> dateFrom, SqlConnection objConnection = null, int? EmployeeIDFrom = 0, int? EmployeeIDTo = 0, int LocationID = 0)
        {

            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DateFrom", dateFrom);
                sqlparams[3] = new SqlParameter("@DateTo", dateTo);
                //sqlparams[4] = new SqlParameter("@empcodefrom", EmployeeIDFrom);
                //sqlparams[5] = new SqlParameter("@empcodeto", EmployeeIDTo);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();


                if (LocationID > 0)
                {
                    sb.Append("and total.LocationID = " + LocationID);
                    sb1.Append("and SET_Location.LocationID = " + LocationID);
                }


                if (EmployeeIDFrom > 0 && EmployeeIDTo > 0)
                {
                    sb.Append("and total.PersonID between " + EmployeeIDFrom + " and " + EmployeeIDTo + " ");
                    sb1.Append("and HRMS_Set_Employee.PersonID between " + EmployeeIDFrom + " and " + EmployeeIDTo + " ");
                }
                if (EmployeeIDTo > 0 && EmployeeIDFrom == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDTo);
                    sb1.Append(" and HRMS_Set_Employee.PersonID = " + EmployeeIDTo);
                }
                if (EmployeeIDFrom > 0 && EmployeeIDTo == null)
                {
                    sb.Append(" and total.PersonID = " + EmployeeIDFrom);
                    sb1.Append(" and HRMS_Set_Employee.PersonID = " + EmployeeIDFrom);
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_ReportHSM, sb, sb1), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeeEarningcard_ReportHSM, sb.ToString(), sb1.ToString()), sqlparams);

                }

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }




        #region Class Methods

        public bool PostDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[28];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", f_SalaryHeaderID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);

                if (f_IsApproved.HasValue)
                    sqlparams[4] = new SqlParameter("@IsApproved", f_IsApproved.Value);
                else
                    sqlparams[4] = new SqlParameter("@IsApproved", DBNull.Value);

                if (f_PostID.HasValue)
                    sqlparams[5] = new SqlParameter("@PostID", f_PostID.Value);
                else
                    sqlparams[5] = new SqlParameter("@PostID", DBNull.Value);

                if (f_PostBy.HasValue)
                    sqlparams[6] = new SqlParameter("@PostBy", f_PostBy.Value);
                else
                    sqlparams[6] = new SqlParameter("@PostBy", DBNull.Value);

                if (f_PostDate.HasValue)
                    sqlparams[7] = new SqlParameter("@PostDate", f_PostDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@PostDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PostByValue))
                    sqlparams[8] = new SqlParameter("@PostByValue", f_PostByValue);
                else
                    sqlparams[8] = new SqlParameter("@PostByValue", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Post_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Post_By_PrimaryKey, sqlparams) > 0)
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

        public bool ApprovedDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", f_SalaryHeaderID);
                

                if (f_IsApproved.HasValue)
                    sqlparams[3] = new SqlParameter("@IsApproved", f_IsApproved.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsApproved", DBNull.Value);

                

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Approve_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Approve_By_PrimaryKey, sqlparams) > 0)
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

        public bool PrePostDataByKey(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[28];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", f_SalaryHeaderID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);

                if (f_IsApproved.HasValue)
                    sqlparams[4] = new SqlParameter("@IsApproved", f_IsApproved.Value);
                else
                    sqlparams[4] = new SqlParameter("@IsApproved", DBNull.Value);

                if (f_PostID.HasValue)
                    sqlparams[5] = new SqlParameter("@PostID", f_PostID.Value);
                else
                    sqlparams[5] = new SqlParameter("@PostID", DBNull.Value);

                if (f_PostBy.HasValue)
                    sqlparams[6] = new SqlParameter("@PostBy", f_PostBy.Value);
                else
                    sqlparams[6] = new SqlParameter("@PostBy", DBNull.Value);

                if (f_PostDate.HasValue)
                    sqlparams[7] = new SqlParameter("@PostDate", f_PostDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@PostDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PostByValue))
                    sqlparams[8] = new SqlParameter("@PostByValue", f_PostByValue);
                else
                    sqlparams[8] = new SqlParameter("@PostByValue", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_PrePost_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_PrePost_By_PrimaryKey, sqlparams) > 0)
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

        public static bool UpdateDataByPaymentID(int SalaryHeaderID, int paymentid, Nullable<int> company, Nullable<int> period, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[28];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
                sqlparams[3] = new SqlParameter("@PaymentID", paymentid);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, UpdatePaymentID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, UpdatePaymentID, sqlparams) > 0)
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


        //UpdatePaymentID
        public static int GetNewPostID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxPostId, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxPostId, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxPostId, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetMonthRecord(int companyID, int periodID, int personID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, MonthRecord_Query, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, MonthRecord_Query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataHeaderForPost(int CompanyID, Nullable<DateTime> SalaryDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> payrollgrpid = 0, Nullable<int> StatusCode = -1)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                if (StatusCode != -1)
                {
                    sb.Append(" and EMP.StatusID = " + StatusCode + " ");
                }


                if (payrollgrpid != null && payrollgrpid > 0)
                {
                    sb.Append(" and SH.PayrollGroupID = " + payrollgrpid + " ");
                }

                sb.Append("  order by EmployeeCode ");
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DataHeaderForPost, sb), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_DataHeaderForPost, sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DataHeaderForPost, sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_DataHeaderForPost, sb), sqlparams);

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

        public static DataTable GetDataHeaderForPrePost(int CompanyID, Nullable<DateTime> SalaryDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> payrollgrpid = 0, Nullable<int> StatusCode = -1)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                if (StatusCode != -1)
                {
                    sb.Append(" and EMP.StatusID = " + StatusCode + " ");
                }


                if (payrollgrpid != null && payrollgrpid > 0)
                {
                    sb.Append(" and SH.PayrollGroupID = " + payrollgrpid + " ");
                }

                sb.Append("  order by EmployeeCode ");
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderForPrePost, sb), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderForPrePost, sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderForPrePost, sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderForPrePost, sb), sqlparams);

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

        public static DataTable GetDataHeaderDetailForPost(int CompanyID, Nullable<DateTime> SalaryDate, Nullable<int> EmpStatusID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                if (EmpStatusID.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", EmpStatusID);
                }
                else
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", DBNull.Value);
                }

                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                DataSet ds = null;

                if (objConnection == null)
                {

                    if (sqltrans != null)
                        //ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPost2, sqlparams);
                    else
                        // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPost2, sqlparams);

                }
                else
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPost2, sqlparams);


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


        public static DataTable GetDataHeaderDetailForPrePost(int CompanyID, Nullable<DateTime> SalaryDate, Nullable<int> EmpStatusID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                if (EmpStatusID.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", EmpStatusID);
                }
                else
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", DBNull.Value);
                }

                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                DataSet ds = null;

                if (objConnection == null)
                {

                    if (sqltrans != null)
                        //ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPrePost2, sqlparams);
                    else
                        // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPrePost2, sqlparams);

                }
                else
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DataHeaderDetailForPost, sqlparams);
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DataHeaderDetailForPrePost2, sqlparams);


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
        


        public static DataTable GetAllDataForTheSameMonthSalary(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, int PersonIDForCheck, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            string Filter = "";
            #region Query

            const string QueryForCheCkSameMonth = @" Select * from HRMS_SalaryHeader where CompanyID = @CompanyID AND PeriodID = @PeriodID AND SalaryDate = @SalaryDate AND Status = 1  ";


            if (PersonIDForCheck > 0)
                Filter = " AND PersonID = @PersonID  ";
            #endregion



            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@PersonID", PersonIDForCheck);


                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForCheCkSameMonth + Filter, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForCheCkSameMonth + Filter, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QueryForCheCkSameMonth + Filter, sqlparams);

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


        public static DataTable GetAllActiveDataWithTrans(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);

                DataSet ds = null;

                if (objConnection == null)
                {
                    // ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(Query_SelectAllDataWithTrans), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_SelectAllDataWithTrans), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllDataWithTrans), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllDataWithTrans), sqlparams);

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

        public static DataTable GetAllActiveDataWithTrans_StatusID(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, Nullable<int> EMP_StatusID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);

                if (EMP_StatusID.HasValue)
                {
                    sb.Append("AND HRMS_Set_Employee.StatusID=" + EMP_StatusID.ToString());
                }
                DataSet ds = null;

                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusID, sb), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusID, sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusID, sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusID, sb), sqlparams);

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


        public static DataTable GetdataForAlreadySalaryGenerate(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, string EmplyeeIds
            , SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> PayrollGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                sqlparams[4] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());


                sb.Append(" and personID in (" + EmplyeeIds + ")");

                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" and PayrollGroupID = " + PayrollGroupID);
                }
                DataSet ds = null;

                if (objConnection == null)
                {
                    // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataWithTrans, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);

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

        public static DataTable GetdataForSalaryHeaderPF(int companyID, int periodID, int SalaryHeaderID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", SalaryHeaderID);
                //sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                //sqlparams[4] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());


                //sb.Append(" and personID in (" + EmplyeeIds + ")");

                //if (PayrollGroupID != null && PayrollGroupID > 0)
                //{
                //    sb.Append(" and PayrollGroupID = " + PayrollGroupID);
                //}
                DataSet ds = null;

                if (objConnection == null)
                {
                    // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetdataForSalaryHeaderPF + sb.ToString(), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForSalaryHeaderPF, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForSalaryHeaderPF + sb.ToString(), sqlparams);

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

        public bool InsertPF(string DatabaseName, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[45];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryHeaderID", f_SalaryHeaderID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[4] = new SqlParameter("@SalaryDate", f_SalaryDate);

                if (f_SalaryType.HasValue)
                    sqlparams[5] = new SqlParameter("@SalaryType", f_SalaryType.Value);
                else
                    sqlparams[5] = new SqlParameter("@SalaryType", DBNull.Value);

                if (f_GrossAmount.HasValue)
                    sqlparams[6] = new SqlParameter("@GrossAmount", f_GrossAmount.Value);
                else
                    sqlparams[6] = new SqlParameter("@GrossAmount", DBNull.Value);

                if (f_DeductionAmount.HasValue)
                    sqlparams[7] = new SqlParameter("@DeductionAmount", f_DeductionAmount.Value);
                else
                    sqlparams[7] = new SqlParameter("@DeductionAmount", DBNull.Value);

                if (f_NetAmount.HasValue)
                    sqlparams[8] = new SqlParameter("@NetAmount", f_NetAmount.Value);
                else
                    sqlparams[8] = new SqlParameter("@NetAmount", DBNull.Value);

                if (f_GrossAmountTaxable.HasValue)
                    sqlparams[9] = new SqlParameter("@GrossAmountTaxable", f_GrossAmountTaxable.Value);
                else
                    sqlparams[9] = new SqlParameter("@GrossAmountTaxable", DBNull.Value);

                sqlparams[10] = new SqlParameter("@Status", f_Status);

                if (f_IsApproved.HasValue)
                    sqlparams[11] = new SqlParameter("@IsApproved", f_IsApproved.Value);
                else
                    sqlparams[11] = new SqlParameter("@IsApproved", DBNull.Value);

                sqlparams[12] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[13] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[14] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_DaysWorking.HasValue)
                    sqlparams[15] = new SqlParameter("@DaysWorking", f_DaysWorking.Value);
                else
                    sqlparams[15] = new SqlParameter("@DaysWorking", DBNull.Value);

                if (f_DaysAbsent.HasValue)
                    sqlparams[16] = new SqlParameter("@DaysAbsent", f_DaysAbsent.Value);
                else
                    sqlparams[16] = new SqlParameter("@DaysAbsent", DBNull.Value);

                if (f_DaysLWP.HasValue)
                    sqlparams[17] = new SqlParameter("@DaysLWP", f_DaysLWP.Value);
                else
                    sqlparams[17] = new SqlParameter("@DaysLWP", DBNull.Value);

                if (f_DaysLate.HasValue)
                    sqlparams[18] = new SqlParameter("@DaysLate", f_DaysLate.Value);
                else
                    sqlparams[18] = new SqlParameter("@DaysLate", DBNull.Value);

                if (f_DaysEarly.HasValue)
                    sqlparams[19] = new SqlParameter("@DaysEarly", f_DaysEarly.Value);
                else
                    sqlparams[19] = new SqlParameter("@DaysEarly", DBNull.Value);

                if (f_DaysTotal.HasValue)
                    sqlparams[20] = new SqlParameter("@DaysTotal", f_DaysTotal.Value);
                else
                    sqlparams[20] = new SqlParameter("@DaysTotal", DBNull.Value);

                if (f_TaxAmount.HasValue)
                    sqlparams[21] = new SqlParameter("@TaxAmount", f_TaxAmount.Value);
                else
                    sqlparams[21] = new SqlParameter("@TaxAmount", DBNull.Value);

                if (f_ContributionEmployer.HasValue)
                    sqlparams[22] = new SqlParameter("@ContributionEmployer", f_ContributionEmployer.Value);
                else
                    sqlparams[22] = new SqlParameter("@ContributionEmployer", DBNull.Value);

                if (f_OpeningID.HasValue)
                    sqlparams[23] = new SqlParameter("@OpeningID", f_OpeningID.Value);
                else
                    sqlparams[23] = new SqlParameter("@OpeningID", DBNull.Value);

                if (f_PostID.HasValue)
                    sqlparams[24] = new SqlParameter("@PostID", f_PostID.Value);
                else
                    sqlparams[24] = new SqlParameter("@PostID", DBNull.Value);

                if (f_PostBy.HasValue)
                    sqlparams[25] = new SqlParameter("@PostBy", f_PostBy.Value);
                else
                    sqlparams[25] = new SqlParameter("@PostBy", DBNull.Value);

                if (f_PostDate.HasValue)
                    sqlparams[26] = new SqlParameter("@PostDate", f_PostDate.Value);
                else
                    sqlparams[26] = new SqlParameter("@PostDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PostByValue))
                    sqlparams[27] = new SqlParameter("@PostByValue", f_PostByValue);
                else
                    sqlparams[27] = new SqlParameter("@PostByValue", DBNull.Value);

                if (f_PaymentID.HasValue)
                    sqlparams[28] = new SqlParameter("@PaymentID", f_PaymentID.Value);
                else
                    sqlparams[28] = new SqlParameter("@PaymentID", DBNull.Value);

                if (f_InvoiceID.HasValue)
                    sqlparams[29] = new SqlParameter("@InvoiceID", f_InvoiceID.Value);
                else
                    sqlparams[29] = new SqlParameter("@InvoiceID", DBNull.Value);

                if (f_MasterSalary.HasValue)
                    sqlparams[30] = new SqlParameter("@MasterSalary", f_MasterSalary.Value);
                else
                    sqlparams[30] = new SqlParameter("@MasterSalary", DBNull.Value);

                if (f_PayrollGroupID.HasValue)
                    sqlparams[31] = new SqlParameter("@PayrollGroupID", f_PayrollGroupID.Value);
                else
                    sqlparams[31] = new SqlParameter("@PayrollGroupID", DBNull.Value);

                if (f_JobDesignationID.HasValue)
                    sqlparams[32] = new SqlParameter("@JobDesignationID", f_JobDesignationID.Value);
                else
                    sqlparams[32] = new SqlParameter("@JobDesignationID", DBNull.Value);

                if (f_Leaves.HasValue)
                    sqlparams[33] = new SqlParameter("@Leaves", f_Leaves.Value);
                else
                    sqlparams[33] = new SqlParameter("@Leaves", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TotalHours))
                    sqlparams[34] = new SqlParameter("@TotalHours", f_TotalHours);
                else
                    sqlparams[34] = new SqlParameter("@TotalHours", DBNull.Value);

                if (!string.IsNullOrEmpty(f_OTHours))
                    sqlparams[35] = new SqlParameter("@OTHours", f_OTHours);
                else
                    sqlparams[35] = new SqlParameter("@OTHours", DBNull.Value);

                if (f_RatePerDay.HasValue)
                    sqlparams[36] = new SqlParameter("@RatePerDay", f_RatePerDay.Value);
                else
                    sqlparams[36] = new SqlParameter("@RatePerDay", DBNull.Value);

                if (!string.IsNullOrEmpty(f_DeductionHours))
                    sqlparams[37] = new SqlParameter("@DeductionHours", f_DeductionHours);
                else
                    sqlparams[37] = new SqlParameter("@DeductionHours", DBNull.Value);

                if (f_RatePerHour.HasValue)
                    sqlparams[38] = new SqlParameter("@RatePerHour", f_RatePerHour.Value);
                else
                    sqlparams[38] = new SqlParameter("@RatePerHour", DBNull.Value);

                if (f_OTAmount.HasValue)
                    sqlparams[39] = new SqlParameter("@OTAmount", f_OTAmount.Value);
                else
                    sqlparams[39] = new SqlParameter("@OTAmount", DBNull.Value);

                if (f_DedAmount.HasValue)
                    sqlparams[40] = new SqlParameter("@DedAmount", f_DedAmount.Value);
                else
                    sqlparams[40] = new SqlParameter("@DedAmount", DBNull.Value);

                if (f_TotalShiftHours.HasValue)
                    sqlparams[41] = new SqlParameter("@TotalShiftHours", f_TotalShiftHours.Value);
                else
                    sqlparams[41] = new SqlParameter("@TotalShiftHours", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[42] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[42] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[43] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[43] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[44] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[44] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                //sqlparams[45] = new SqlParameter("@DatabaseName", DatabaseName);
                sb.Append(DatabaseName);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_InsertPF, sb.ToString()), sqlparams) > 0)
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


        public static DataTable GetdataForAlreadySalary_ForIncrement(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, int EmplyeeIds
         , SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> PayrollGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                sqlparams[4] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());


                sb.Append(" and personID in (" + EmplyeeIds + ")");

                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" and PayrollGroupID = " + PayrollGroupID);
                }
                DataSet ds = null;

                if (objConnection == null)
                {
                    // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetdataForAlreadySalaryGenerate_ForInc + sb.ToString(), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate_ForInc + sb.ToString(), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForAlreadySalaryGenerate_ForInc + sb.ToString(), sqlparams);

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


        public static DataTable GetdataForAlreadyIncrement(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date, int EmplyeeId
            , SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> PayrollGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", Date);
                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date));
                sqlparams[4] = new SqlParameter("@Year", Date.Value.Year.ToString());


                sb.Append(" and personID in (" + EmplyeeId + ")");

  
                DataSet ds = null;

                if (objConnection == null)
                {
                  
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetdataForAlreadyIncrement + sb.ToString(), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadyIncrement + sb.ToString(), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForAlreadyIncrement + sb.ToString(), sqlparams);

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

        public static DataTable GetFromandTodate(Nullable<int> CompanyID, Nullable<int> PeriodID, DateTime date, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query



            const string Query = @" select * from HRMS_SET_TaxYear where @date between FromDate and ToDate and Status = 1

";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@date", date);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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
        public static DataTable GetTaxForActiveMonth(Nullable<int> CompanyID, Nullable<int> PeriodID, Nullable<int> PersonID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query



            const string QueryForActiveMonthTax = @" select 
sum(GrossAmountTaxable) as GrossAmountTaxable,
 sum(TaxAmount) AS TaxAmount

 
 from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID 

--group by GrossAmountTaxable,TaxAmount

 --order by HRMS_SalaryHeader.SalaryHeaderID desc

";


            //            const string QueryForActiveMonthTax = @" select top 1
            //
            //sum(GrossAmountTaxable) as GrossAmountTaxable,
            // TaxAmount AS TaxAmount
            //
            // 
            // from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID
            //
            //group by GrossAmountTaxable,TaxAmount
            //
            // --order by HRMS_SalaryHeader.SalaryHeaderID desc
            //
            //";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
               

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForActiveMonthTax, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForActiveMonthTax, sqlparams);

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

        public static DataTable GetTaxForActiveMonthNew(Nullable<int> CompanyID, Nullable<int> PeriodID, Nullable<int> PersonID, DateTime fromdate, DateTime todate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query



            const string QueryForActiveMonthTax = @" select 
sum(GrossAmountTaxable) as GrossAmountTaxable,
 sum(TaxAmount) AS TaxAmount

 
 from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID and SalaryDate between @FromDate and @ToDate

--group by GrossAmountTaxable,TaxAmount

 --order by HRMS_SalaryHeader.SalaryHeaderID desc

";


            //            const string QueryForActiveMonthTax = @" select top 1
            //
            //sum(GrossAmountTaxable) as GrossAmountTaxable,
            // TaxAmount AS TaxAmount
            //
            // 
            // from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID
            //
            //group by GrossAmountTaxable,TaxAmount
            //
            // --order by HRMS_SalaryHeader.SalaryHeaderID desc
            //
            //";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@FromDate", fromdate);
                sqlparams[4] = new SqlParameter("@ToDate", todate);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForActiveMonthTax, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForActiveMonthTax, sqlparams);

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
        public static DataTable GetTaxForActiveMonthNew_AlNoor(Nullable<int> CompanyID, Nullable<int> PeriodID, Nullable<int> PersonID, DateTime fromdate, DateTime todate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query



            const string QueryForActiveMonthTax = @"  select sum(GrossAmountTaxable) as GrossAmountTaxable ,sum(TaxAmount) as TaxAmount from (
 select sum(Amount) as GrossAmountTaxable , 0 as TaxAmount from  hrms_salarydetail where salaryheaderid in (
select SalaryHeaderID from HRMS_SalaryHeader
 where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID and SalaryDate between @FromDate and @ToDate
 ) and  PayrollTypeCode in (1) and Status = 1

 union all

 select 
0 as GrossAmountTaxable,
sum(TaxAmount) AS TaxAmount
 from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID and SalaryDate between @FromDate and @ToDate
 )tab
";


            //            const string QueryForActiveMonthTax = @" select top 1
            //
            //sum(GrossAmountTaxable) as GrossAmountTaxable,
            // TaxAmount AS TaxAmount
            //
            // 
            // from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID
            //
            //group by GrossAmountTaxable,TaxAmount
            //
            // --order by HRMS_SalaryHeader.SalaryHeaderID desc
            //
            //";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@FromDate", fromdate);
                sqlparams[4] = new SqlParameter("@ToDate", todate);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForActiveMonthTax, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForActiveMonthTax, sqlparams);

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
        public static DataTable GetLastMonthSalary(Nullable<int> CompanyID, Nullable<int> PeriodID, Nullable<int> PersonID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query

            const string QueryLastMonthSalary = @" 
select top 1

GrossAmountTaxable

 
 from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID

 order by SalaryHeaderID desc


";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryLastMonthSalary, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryLastMonthSalary, sqlparams);

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

        public static DataTable GetLastMonthSalary_AlNoor(Nullable<int> CompanyID, Nullable<int> PeriodID, Nullable<int> PersonID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            #region Query

            const string QueryLastMonthSalary = @" 
--select top 1

--GrossAmountTaxable

 
 --from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID

 --order by SalaryHeaderID desc

select isnull(sum(isnull(Amount,0)),0) as GrossAmountTaxable from HRMS_SalaryDetail where SalaryHeaderID in (
select top 1
SalaryHeaderID
from HRMS_SalaryHeader where status = 1  and CompanyID =  @CompanyID AND PeriodID = @PeriodID    AND PersonID = @PersonID
order by SalaryHeaderID desc) and PayrollTypeCode in (1) and Status =1 

";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryLastMonthSalary, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryLastMonthSalary, sqlparams);

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

        public static DataTable GetTaxableGrossAmount(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTaxableGrossAmount, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataforCashPayment(Nullable<int> companyID, Nullable<int> periodID, int PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_dataforCashPayment, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetSalaryHeaderIDByMonthAndYear(Nullable<int> periodID, Nullable<int> COmpanyID, Nullable<DateTime> Date_From, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<int> PayrollGrpID = null)
        {

            DataTable dt = null;

            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@CompanyID", COmpanyID);

                if (PayrollGrpID != null && PayrollGrpID > 0)
                    sb.Append(" AND PayrollGroupID = " + PayrollGrpID);

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataByMonthAndYear, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataByMonthAndYear, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByMonthAndYear, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetSalaryByMonthAndYear(Nullable<int> CompanyID, Nullable<DateTime> Date_From, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetSalaryByMonthAndYear, sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetSalaryByMonthAndYear, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalaryByMonthAndYear, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPayrollTypeCode(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPayrollTypeCodeDK(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeDK, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeDK, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeDK, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPayrollTypeCodeHS(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHS, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHS, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHS, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByPayrollTypeCodeHSNew(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHSNew, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHSNew, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCodeHSNew, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByPayrollTypeCode_AlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, int CostCenterID = 0, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (CostCenterID > 0)
                {
                    sb.Append(" AND [HRMS_Set_Employee].CostCenterID = " + CostCenterID);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_Alnoor, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_Alnoor, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_Alnoor, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByPayrollTypeCode_AlNoorV2(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, Nullable<int> EMPStatusID, Nullable<int> EmployeeTypeID, Nullable<int> LocationID, int CostCenterID = 0, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
                sqlparams[3] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                sqlparams[4] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));

                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }

                if (EmployeeTypeID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", EmployeeTypeID);
                }

                else
                {
                    sqlparams[6] = new SqlParameter("@EmployeeTypeID", DBNull.Value);
                }

                if (LocationID.HasValue)
                {
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                }

                else
                {
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);
                }

                if (CostCenterID > 0)
                {
                    sb.Append(" AND [HRMS_Set_Employee].CostCenterID = " + CostCenterID);
                }

                if (EMPStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EMPStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_AlnoorV2, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_AlnoorV2, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByPayrollTypeCode_AlnoorV2, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataEmployeePFStatementReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, int EmployeeTypeID = 0, Nullable<int> EmployeeStatusID = null, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
           
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                
                if (PersonID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@PersonID", 0);
                }

                sqlparams[3] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);
               
                if (Date_From.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", DBNull.Value));
                }
                if (Date_To.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));
                }
                else { sqlparams[5] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", DBNull.Value)); }


                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (objTransaction != null)
                    //dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport , sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                   // dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                      dt = SqlHelper.ExecuteDataset(objTransaction,CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport , sb.ToString()), sqlparams).Tables[0];
                else
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                   dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];

                if (ds != null)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataEmployeePFStatementReport_AlNoor(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int? PersonID, string payrollTypeCode, int EmployeeTypeID = 0, Nullable<int> EmployeeStatusID = null, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (PersonID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@PersonID", 0);
                }

                sqlparams[3] = new SqlParameter("@PayrollTypeCode", payrollTypeCode);

                if (Date_From.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", Date_From));
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@FromDate", String.Format("{0:yyyy-MM-dd}", DBNull.Value));
                }
                if (Date_To.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", Date_To));
                }
                else { sqlparams[5] = new SqlParameter("@ToDate", String.Format("{0:yyyy-MM-dd}", DBNull.Value)); }


                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }
                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }

                if (objTransaction != null)
                    //dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport_AlNoor, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    // dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport_AlNoor, sb.ToString()), sqlparams).Tables[0];
                else
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeePFStatementReport, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_EmployeePFStatementReport_AlNoor, sb.ToString()), sqlparams).Tables[0];

                if (ds != null)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetDataOneTimeAllowanceAndDeductionReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", 0);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_OneTimeAllowanceAndDeductionReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_OneTimeAllowanceAndDeductionReport, sqlparams);

                if (ds != null)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static string GetFormulaResult(string Formula, SqlParameter[] sqlparams, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            string _result = null;

            try
            {
                string strQuery = "Select " + Formula;


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, strQuery, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, strQuery, sqlparams);

                if (ds != null)
                    _result = ds.Tables[0].Rows[0][0].ToString();
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _result;
        }

        public static DataTable GetAllDataFORSearch(Nullable<int> LocationID, Nullable<int> DivID, Nullable<int> PersnType, string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            sb.Append(strWhere);
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
                    if (LocationID > 0)
                    {
                        sb.Append(" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    }

                    if (DivID > 0)
                    {
                        sb.Append(" AND HRMS_Set_Employee.DivisionID = " + DivID);
                    }

                    if (PersnType > 0)
                    {
                        sb.Append(" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                    }

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, QueryFOrSearching_Detail + sb + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), QueryFOrSearching_Detail + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, QueryFOrSearching_Detail + sb + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), QueryFOrSearching_Detail + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (LocationID > 0)
                    {
                        strWhere = strWhere + (" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                    }

                    if (DivID > 0)
                    {
                        strWhere = strWhere + (" AND HRMS_Set_Employee.DivisionID = " + DivID);
                    }
                    if (PersnType > 0)
                    {
                        strWhere = strWhere + (" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                    }

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryFOrSearching_Detail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryFOrSearching_Detail_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataForLoanAdjustmentpayment(Nullable<int> companyID, Nullable<int> periodID, int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);


                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_ForLoanAdjustmentPayment, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_ForLoanAdjustmentPayment, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForLoanAdjustmentPayment, sqlparams);

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


        public static DataTable GetSearchAllDatatForDelete(Nullable<int> LocationID, Nullable<int> DivID, Nullable<int> PersnType, string strWhere, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {

            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            sb.Append(strWhere);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[0] = new SqlParameter("@CompanyID", periodID);
                //sqlparams[1] = new SqlParameter("@PeriodID", PersonID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                if (LocationID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivID > 0)
                {
                    sb.Append(" AND HRMS_Set_Employee.DivisionID = " + DivID);
                }

                if (PersnType > 0)
                {
                    sb.Append(" And HRMS_Set_Employee.PersonTypeCode = " + PersnType);
                }
                if (objTransaction != null)
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(QueryFOrSearching_Detail + strWhere), sqlparams).Tables[0];
                else if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(QueryFOrSearching_Detail + strWhere), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(QueryFOrSearching_Detail + strWhere), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataHeaderDetailForPost_AlAbbas(Nullable<DateTime> SalaryDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> Payrollgrpid = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);

                if (Payrollgrpid > 0)
                {
                    sb.Append(" and SH.PayrollGroupID = " + Payrollgrpid);
                }

                DataSet ds = null;

                if (objConnection == null)
                {

                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_DataHeaderDetailForPost_AlAbbas, sb.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DataHeaderDetailForPost_AlAbbas, sb.ToString()), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_DataHeaderDetailForPost_AlAbbas, sb.ToString()), sqlparams);

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

        public static DataTable GetPrvMonthPayrollData(Nullable<DateTime> SalaryDate, int CompanyID, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> PersonID = 0, string desc = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            if (desc == "food")
            {
                sb.Append(" where description like '%food%' ");

            }

            else if (desc == "overtime")
            {
                sb.Append(" where description like '%over time%' or description like '%Extra%' ");
            }


            else
            {
                sb.Append(" where description like '%" + desc + "%' ");
            }

            const string QUERY_PRVMONTHOT = @" Select HRMS_SALARYdetail.Amount,MasterSalary from HRMS_SALARYdetail 
inner join HRMS_SalaryHEader on HRMS_SalaryHEader.Salaryheaderid = HRMS_SALARYdetail.SalaryHEaderID
where HRMS_SalaryHEader.Status = 1
and HRMS_SALARYdetail.Status = 1
and HRMS_SAlaryHeader.CompanyID = @CompanyID
and HRMS_SalaryHEader.PErsonID = @PersonID
and Convert(date,HRMS_SalaryHEader.SalaryDate) = @SalaryDate
and PayRolltypeID in (Select PayRolltypeID from HRMS_Payrolltype {0} )
and HRMS_SalaryHEader.PostID is not null
and HRMS_SalaryHEader.SalaryHeaderID not in 
(Select SalaryHeaderID from HRMS_EmployeePayment where Status  = 1 and IsActive = 1) ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);



                DataSet ds = null;

                if (objConnection == null)
                {

                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(QUERY_PRVMONTHOT, sb.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(QUERY_PRVMONTHOT, sb.ToString()), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(QUERY_PRVMONTHOT, sb.ToString()), sqlparams);

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

        public static DataTable EmployeeSalarySheet_ReportData_AlAbbas(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> PayrollGroupID = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());

                StringBuilder sb = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (objConnection == null)
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.QUery_EmployeeSalarySheet_AlAbbas + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.QUery_EmployeeSalarySheet_AlAbbas + queryforBandhi, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable GetAllActiveDataWithTransForPreviousGroups(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, Nullable<int> personID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            string query_getprvdata = @"
Select ISNULL(SUM(GrossAmount),0) as PrvGrossAmount,ISNULL(SUM(DeductionAmount),0) as PrvDeductionAmount from HRMS_salaryHEader

where Status = 1
and PersonID = @PersonID
and SalaryDate = @SalaryDate
and CompanyID = @CompanyID
and PeriodID = @PeriodID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@PersonID", personID);

                DataSet ds = null;

                if (sqltrans == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query_getprvdata, sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query_getprvdata, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, query_getprvdata, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query_getprvdata, sqlparams);

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


        public static DataTable LeaveEncashmentReport(Nullable<int> companyID, Nullable<int> periodID, SqlConnection objConnection = null, int? EmployeeIDFrom = 0)
        {


            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", EmployeeIDFrom);

                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();



                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LeaveEncashmentReport, sb), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_LeaveEncashmentReport, sb.ToString()), sqlparams);

                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataRow GetLoansBalance(int EmployeeIDFrom, int PayrollTypeID, Nullable<DateTime> fromDate, Nullable<DateTime> ToDate, Nullable<int> companyID, Nullable<int> periodID, SqlTransaction sqltrans = null)
        {


            DataRow dr = null;
            DataSet ds = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", EmployeeIDFrom);
                sqlparams[3] = new SqlParameter("@PayrollTypeID", PayrollTypeID);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                sqlparams[5] = new SqlParameter("@fromDate", fromDate);
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();


                if (sqltrans == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetBalncesofLoan, sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetBalncesofLoan, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0].Rows[0];
                if (dr != null && dr["Balance"].ToString() == "")
                    dr = null;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable Get_EmployeeCashAdvice(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> bank, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmpStatusID, int LocationID = 0, Nullable<int> DepartmentID = null, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<Guid> UserID = null, bool IsRanipurSugar = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (bank != null)
                {

                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                }
                if (EmpStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_Set_Employee].FK_DepartmentID = " + DepartmentID);
                }

                if (IsRanipurSugar)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeCashAdvice_RSM, sb), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmployeeCashAdvice, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }

        public static DataTable Get_BonusCashAdvice(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> bank, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, Nullable<int> EmpStatusID, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (bank != null)
                {
                    sb.Append(" and AR_SET_Bank.BankID = " + bank + " ");
                }
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    //sb.Append(" and HRMS_Set_Employee.PersonTypeCode = " + EmployeeTypeID);
                    sb.Append(" and SET_EmployeeType.TypeID = " + EmployeeTypeID);
                }
                if (EmpStatusID.HasValue)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BonusCashAdvice, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;

        }


        public static DataTable Get_EmployeeBonusReportSlip(Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,int emplyeeid, Nullable<int> EmpStatusID, Nullable<int> CompanyID, Nullable<Guid> UserID = null)
        {

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            if (emplyeeid > 0 && emplyeeid != null)
            {
                sb.Append(" and HRMS_Set_Employee.PersonID = " + emplyeeid);
            }

            if (EmpStatusID > 0)
            {
                sb.Append(" and HRMS_Set_Employee.StatusID = " + EmpStatusID);
            }
            else
            {
                sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
            }
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DateFrom", Date_From);
                sqlparams[1] = new SqlParameter("@DateTo", Date_To);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetEmployeeBonusReport_Slip, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetIncomeTaxEmployees(int companyID, int periodID, int month, string year, int LocationID = 0, int EmployeeStatusID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@month", month);
                sqlparams[3] = new SqlParameter("@year", year);

                DataSet ds = null;

                string Query_GetIncomeTaxEmployees = @"select 1 as SingleNO,
                ROW_NUMBER() OVER (ORDER BY HRMS_Set_Employee.PersonID asc) AS 'Row' ,HRMS_Set_Employee.EmployeeCode,
                NTNNo,CNICNumber,fullname,SET_City.CityName, HRMS_Set_Employee.AddressLine1, 'Individual' as status, HRMS_SalaryHeader.MasterSalary
                ,CAST(HRMS_SalaryDetail.Amount as decimal(18,0)) as Amount
                --,HRMS_SalaryHeader.GrossAmount
				,
				CAST((		
				select sum(Amount) from 
				HRMS_SalaryDetail dtl
				inner join hrms_payrolltype on hrms_payrolltype.PayrollTypeID = dtl.PayrollTypeID
				and hrms_payrolltype.PayrollTypeCode = dtl.PayrollTypeCode
				and hrms_payrolltype.IsTaxable  =1  and hrms_payrolltype.PayrollTypeCode = 1
				where dtl.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
				and dtl.CompanyID = HRMS_SalaryHeader.CompanyID
				and dtl.Status = 1
				 ) as decimal(18,0)) as GrossAmount
                from 
                HRMS_SalaryHeader inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID 
                and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
                and HRMS_SalaryHeader.Status = 1
                inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID =  HRMS_SalaryHeader.SalaryHeaderID 
                      and HRMS_SalaryDetail.CompanyID =  HRMS_SalaryHeader.CompanyID
                     and HRMS_SalaryDetail.Status = 1
                left join SET_City on SET_City.CityID = HRMS_Set_Employee.FK_CityID and SET_City.Status = 1
                inner join hrms_payrolltype on hrms_payrolltype.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID and hrms_payrolltype.Status =1
                Left Join SET_Location On SET_Location.LocationID = HRMS_Set_Employee.FK_LocationID
				And SET_Location.Status = 1
				Left Join HRMS_Employement_Status On HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID
				--And HRMS_Employement_Status.CompanyID = HRMS_SET_Employee.CompanyID
				And HRMS_Employement_Status.Status = 1
                where  month(HRMS_SalaryHeader.SalaryDate) = @month
                and year(HRMS_SalaryHeader.SalaryDate) = @year
                and hrms_payrolltype.Description like 'Income Tax'
                and HRMS_SalaryHeader.CompanyID = @companyID
                and HRMS_SalaryHeader.PeriodID = @periodID
                {0}
                --and HRMS_SalaryHeader.PersonID = 829
                AND HRMS_SalaryDetail.Amount <> 0
                order by HRMS_Set_Employee.PersonID asc
                ";

                if (LocationID > 0)
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);

                if (EmployeeStatusID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatusID);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");

                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetIncomeTaxEmployees, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetIncomeTaxEmployees, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetIncomeTaxEmployees, sb), sqlparams);

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

        public static DataTable GetIncomeTaxEmployeesCertificateCane(int companyID, int periodID, DateTime FromDate, DateTime ToDate, Nullable<int> EmployeeStatus, Nullable<int> PersonID,Nullable<Guid> UserID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                if (PersonID > 0)
                    sb.Append(" and HRMS_SalaryHeader.PersonID =  " + PersonID);
                if (EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                DataSet ds = null;

                string Query_GetIncomeTaxEmployeesCertificate = @"
select 
            CAST(Isnull(( Select SUM(ISNULL(tbl_BonusDetail.InComeTax,0)) from HRMS_BonusDetail as tbl_BonusDetail
               inner join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = tbl_BonusDetail.BonusHeaderID
                and HRMS_BonusHeader.CompanyID = tbl_BonusDetail.CompanyID
                and HRMS_BonusHeader.PeriodID = tbl_BonusDetail.PeriodID
                and HRMS_BonusHeader.Status = 1
          where tbl_BonusDetail.CompanyID = @CompanyID 
		        And tbl_BonusDetail.PeriodID = @PeriodID	
				And tbl_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID	
				And tbl_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID	
				And HRMS_BonusHeader.CurrentManulDate between @FromDate and @ToDate
        ),0) as decimal(18,2)) as IncomeTaxAmount ,

		CAST(Isnull((  Select SUM(ISNULL(tbl_BonusDetail.BonusAmount,0)) from HRMS_BonusDetail as tbl_BonusDetail
               inner join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = tbl_BonusDetail.BonusHeaderID
                and HRMS_BonusHeader.CompanyID = tbl_BonusDetail.CompanyID
                and HRMS_BonusHeader.PeriodID = tbl_BonusDetail.PeriodID
                and HRMS_BonusHeader.Status = 1
          where tbl_BonusDetail.CompanyID = @CompanyID 
		        And tbl_BonusDetail.PeriodID = @PeriodID	
				And tbl_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID	
				And tbl_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID	
				And HRMS_BonusHeader.CurrentManulDate between @FromDate and @ToDate
        ),0) as decimal(18,2)) as BonusAmount ,
                ROW_NUMBER() OVER (ORDER BY HRMS_Set_Employee.fullname asc) AS 'Row' ,
				--HRMS_SalaryHeader.SalaryDate,
                NTNNo,
                case when len(CNICNumber) = 13 then SUBSTRING(CNICNumber, 1, 5)+'-'+SUBSTRING(CNICNumber, 6, 7)+'-'+SUBSTRING(CNICNumber, 11, 1)  else CNICNumber end as CNICNumber
                ,fullname,SET_City.CityName, HRMS_Set_Employee.AddressLine1, 'Individual' as status
				--, SUM(isnull(HRMS_SalaryHeader.GrossAmount,0)) as MasterSalary,

				,CAST(Isnull((Select SUM(ISNULL(HRMS_SalaryHeaderinner.GrossAmount,0)) from HRMS_SalaryHeader as HRMS_SalaryHeaderinner
				where HRMS_SalaryHeaderinner.PersonID = HRMS_Set_Employee.PersonID
				And HRMS_SalaryHeaderinner.PeriodID = HRMS_Set_Employee.CompanyID
				and HRMS_SalaryHeaderinner.Status = 1
				And HRMS_SalaryHeaderinner.SalaryDate between @FromDate and @ToDate),0) as decimal(18,2))
				as MasterSalary,

                Sum(isnull(HRMS_SalaryDetail.Amount,0)) as Amount
                ,SET_Business.BusinessName,SET_Business.AddressLine1 CompanyAddress,SET_Business.NTNNumber
				,@FromDate as FromDate
				,@ToDate as ToDate
				from 
                HRMS_SalaryHeader inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID and HRMS_SalaryHeader.Status = 1
                inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID =  HRMS_SalaryHeader.SalaryHeaderID and HRMS_SalaryDetail.Status = 1
                left join SET_City on SET_City.CityID = HRMS_Set_Employee.FK_CityID and SET_City.Status = 1
                inner join hrms_payrolltype on hrms_payrolltype.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID and hrms_payrolltype.Status =1
				left join SET_Business on SET_Business.Status = HRMS_SalaryHeader.Status and SET_Business.Status = 1
                where  
				HRMS_SalaryHeader.Status = 1 and
				HRMS_SalaryHeader.SalaryDate between @FromDate and @ToDate
                and hrms_payrolltype.Description like 'Income Tax'
                and HRMS_SalaryHeader.CompanyID = @companyID
				and HRMS_SalaryHeader.PeriodID = @periodID
                --and HRMS_SalaryHeader.PersonID = @PersonID
                {0}
				group by fullname,NTNNo,CNICNumber,CityName, HRMS_Set_Employee.AddressLine1,SET_Business.BusinessName,SET_Business.AddressLine1,SET_Business.NTNNumber, HRMS_Set_Employee.PersonID , HRMS_Set_Employee.CompanyID
                order by HRMS_Set_Employee.fullname asc
                ";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);

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


        public static DataSet GetIncomeTaxEmployeesCertificate(int companyID, int periodID, DateTime FromDate, DateTime ToDate, DateTime Date, Nullable<int> EmployeeStatus, Nullable<int> PersonID, int LocationID = 0, Nullable<Guid> UserID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@Date", Date);

                if (PersonID > 0)
                    sb.Append(" and HRMS_SalaryHeader.PersonID =  " + PersonID);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }

                if (EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_Set_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                // DataSet ds = null;


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetIncomeTaxEmployeesCertificate, sb), sqlparams);

                //if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                //    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable SalariesAndWagesReport(string companyID, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                companyID = "(" + companyID + ")";

                #region MyRegion
                const string SalariesAndWagesReport = @" 
--Declare @CompanyID int = 1
--Declare @PeriodID int = 1
--Declare @FromDate datetime = '2020-12-01 00:00:00:000'
--Declare @ToDate datetime = '2020-12-31 00:00:00:000'

select 'Salaries' as Heading, 'Taxable' as TaxHeading,  HRMS_Employement_Status.Description as EmployeeStatus,EMP_ExpenseType .ExpenseType, sum(Round(HRMS_SalaryHeader.GrossAmount,0)) as Amount 

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1
	INNER JOIN HRMS_SalaryDetail on
	HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
	HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on
	HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
	HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
	AND HRMS_PayrollType.Status = 1 and HRMS_PayrollType.PayrollTypeCode <> 4

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID


inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
	where HRMS_SalaryHeader.CompanyID in @CompanyID
        and HRMS_SalaryHeader.PeriodID = @PeriodID
		And HRMS_SalaryDetail.PayrollTypeCode=2
        and HRMS_PayrollType.Description like 'Income%Tax'
        and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate
		group by HRMS_Employement_Status.Description ,EMP_ExpenseType .ExpenseType

union all

select 'Salaries' as Heading, 'Below Taxable' as TaxHeading,  t.EmployeeStatus,t.ExpenseType
,sum(Round(t.Amount,0)) as Amount 
  from (
select distinct 'Salaries' as Heading, 'Below Taxable' as TaxHeading,  HRMS_Employement_Status.Description as EmployeeStatus,EMP_ExpenseType .ExpenseType
,HRMS_SalaryHeader.GrossAmount as Amount 
,HRMS_SalaryHeader.PersonID

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1
	INNER JOIN HRMS_SalaryDetail on
	HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
	HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
	where HRMS_SalaryHeader.CompanyID in @CompanyID
        and HRMS_SalaryHeader.PeriodID = @PeriodID
        and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate
     	and HRMS_SalaryHeader.PersonID

		not in
		
		(
		
select HRMS_Set_Employee.PersonID
from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1
	INNER JOIN HRMS_SalaryDetail on
	HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
	HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
    and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on
	HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
	HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
	AND HRMS_PayrollType.Status = 1 and HRMS_PayrollType.PayrollTypeCode <> 4

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID


inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
	where HRMS_SalaryHeader.CompanyID in @CompanyID
        and HRMS_SalaryHeader.PeriodID = @PeriodID
		And HRMS_SalaryDetail.PayrollTypeCode=2
        and HRMS_PayrollType.Description like 'Income%Tax'
        and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate
		group by HRMS_Set_Employee.PersonID
        
	
		)
		--group by HRMS_Employement_Status.Description ,EMP_ExpenseType .ExpenseType
)t
group by t.EmployeeStatus, t.ExpenseType

union all


select 'Bonuses' as Heading, 'Taxable' as TaxHeading, HRMS_Employement_Status.Description as EmployeeStatus, EMP_ExpenseType .ExpenseType
,sum(Round(HRMS_BonusDetail.NetBonusAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_BonusDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_BonusHeader On HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID And HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID And HRMS_BonusHeader.Status = 1

where HRMS_BonusDetail.CompanyID in @CompanyID and HRMS_BonusDetail.PeriodID = @PeriodID and HRMS_BonusHeader.CurrentManulDate Between @FromDate and @ToDate
	  and HRMS_BonusDetail.InComeTax != 0
		
group by HRMS_Employement_Status.Description ,EMP_ExpenseType .ExpenseType


UNION ALL

select 'Bonuses' as Heading, 'Below Taxable' as TaxHeading, HRMS_Employement_Status.Description as EmployeeStatus, EMP_ExpenseType .ExpenseType
,sum(Round(HRMS_BonusDetail.NetBonusAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_BonusDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_BonusHeader On HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID And HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID And HRMS_BonusHeader.Status = 1

where HRMS_BonusDetail.CompanyID in @CompanyID and HRMS_BonusDetail.PeriodID = @PeriodID and HRMS_BonusHeader.CurrentManulDate Between @FromDate and @ToDate
	  and HRMS_BonusDetail.InComeTax = 0
		
group by HRMS_Employement_Status.Description ,EMP_ExpenseType .ExpenseType


union all

select 'Overtime' as Heading, '' as TaxHeading, HRMS_Employement_Status.Description as EmployeeStatus, EMP_ExpenseType .ExpenseType
,sum(Round(HRMS_OverTimePostingDetail.OverTimeAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_OverTimePostingDetail on HRMS_OverTimePostingDetail.PersonID = HRMS_Set_Employee.PersonID 
and HRMS_OverTimePostingDetail.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_OverTimePostingDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_OverTimePostingHeader On HRMS_OverTimePostingHeader.OTPHeaderID = HRMS_OverTimePostingDetail.OTPHeaderID
And HRMS_OverTimePostingHeader.CompanyID = HRMS_OverTimePostingDetail.CompanyID And HRMS_OverTimePostingHeader.PeriodID = HRMS_OverTimePostingDetail.PeriodID
And HRMS_OverTimePostingHeader.Status = 1

where HRMS_OverTimePostingDetail.CompanyID in @CompanyID and HRMS_OverTimePostingDetail.PeriodID = @PeriodID and HRMS_Set_Employee.Status = 1
	  and HRMS_OverTimePostingHeader.OverTimeDate Between @FromDate And @ToDate 

group by HRMS_Employement_Status.Description ,EMP_ExpenseType .ExpenseType

";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(SalariesAndWagesReport, additionalFilter).Replace("@CompanyID", companyID), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataSet SalariesAndWagesPermanentStaffReport(string companyID, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataSet ds = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                companyID = "(" + companyID + ")";

                #region 
                const string SalariesAndWagesReport = @" 
--Declare @PeriodID int = 1
--Declare @FromDate datetime = '2020-12-01 00:00:00:000'
--Declare @ToDate datetime = '2020-12-31 00:00:00:000'

select 'Permanent' as EmployeeStatus
, EMP_ExpenseType.ExpenseType, sum(Round(HRMS_SalaryDetail.Amount,0)) as Amount 
,HRMS_PayrollType.Description as PayrollType

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1

INNER JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1 

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID

where HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID 

--and HRMS_PayrollType.Description like 'Income%Tax'
and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate 
and HRMS_Employement_Status.Description like '%Permanent%'

group by HRMS_Employement_Status.Description ,EMP_ExpenseType.ExpenseType, HRMS_PayrollType.Description

------------------

Select t.Heading,t.ExpenseType,SUM(t.Amount) as Amount from (
--Bonus
select 'Other Benefits Total' as Heading, EMP_ExpenseType .ExpenseType ,sum(Round(HRMS_BonusDetail.NetBonusAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_BonusDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_BonusHeader On HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID And HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID And HRMS_BonusHeader.Status = 1

where HRMS_BonusDetail.CompanyID in @CompanyID and HRMS_BonusDetail.PeriodID = @PeriodID and HRMS_BonusHeader.CurrentManulDate Between @FromDate and @ToDate
	--  and HRMS_BonusDetail.InComeTax != 0
and HRMS_Employement_Status.Description like '%Permanent%'
		
group by EMP_ExpenseType .ExpenseType

UNION ALL
--Overtime
select 'Other Benefits Total' as Heading, EMP_ExpenseType.ExpenseType ,sum(Round(HRMS_OverTimePostingDetail.OverTimeAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_OverTimePostingDetail on HRMS_OverTimePostingDetail.PersonID = HRMS_Set_Employee.PersonID 
and HRMS_OverTimePostingDetail.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_OverTimePostingDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_OverTimePostingHeader On HRMS_OverTimePostingHeader.OTPHeaderID = HRMS_OverTimePostingDetail.OTPHeaderID
And HRMS_OverTimePostingHeader.CompanyID = HRMS_OverTimePostingDetail.CompanyID And HRMS_OverTimePostingHeader.PeriodID = HRMS_OverTimePostingDetail.PeriodID
And HRMS_OverTimePostingHeader.Status = 1

where HRMS_OverTimePostingDetail.CompanyID in @CompanyID and HRMS_OverTimePostingDetail.PeriodID = @PeriodID and HRMS_Set_Employee.Status = 1
	  and HRMS_OverTimePostingHeader.OverTimeDate Between @FromDate And @ToDate 
      and HRMS_Employement_Status.Description like '%Permanent%'
group by EMP_ExpenseType .ExpenseType

)t group by t.Heading,t.ExpenseType

UNION ALL

Select 'Allocation to Other Companies' as Heading, 'Admin' as ExpenseType, 0 as Amount

UNION ALL

Select 'Allocation to Other Companies' as Heading, 'COGS' as ExpenseType, 0 as Amount

UNION ALL

Select 'Allocation to Other Companies' as Heading, 'Selling' as ExpenseType, 0 as Amount

----------------------

Select final.Heading , SUM(final.Amount) as Amount from (

select 'Taxable' as Heading, sum(Round(t.Amount,0)) as Amount 

from (

select distinct HRMS_Employement_Status.Description as EmployeeStatus
, EMP_ExpenseType .ExpenseType, HRMS_SalaryDetail.Amount as Amount, HRMS_SalaryHeader.PersonID
, HRMS_PayrollType.Description as PayrollType

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1

INNER JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1 

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID

where HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID

and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate 

and HRMS_Employement_Status.Description like '%Permanent%'

and HRMS_SalaryHeader.PersonID in (
		
select HRMS_Set_Employee.PersonID

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1

INNER JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1 

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID

where HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID 

and HRMS_PayrollType.Description like 'Income%Tax'
and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate 
and HRMS_Employement_Status.Description like '%Permanent%'

group by HRMS_Set_Employee.PersonID )

)t

UNION ALL

select 'Below Taxable' as Heading, sum(Round(t.Amount,0)) as Amount 

from (
select distinct HRMS_Employement_Status.Description as EmployeeStatus
, EMP_ExpenseType .ExpenseType, HRMS_SalaryDetail.Amount as Amount, HRMS_SalaryHeader.PersonID
, HRMS_PayrollType.Description as PayrollType

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_SalaryHeader.Status = 1

INNER JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID 
and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1 

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID

where HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID

and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate 

and HRMS_Employement_Status.Description like '%Permanent%'

and HRMS_SalaryHeader.PersonID not in (
		
select distinct HRMS_Set_Employee.PersonID

from HRMS_Set_Employee
inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_SalaryHeader.Status = 1

INNER JOIN HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID and
HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID and HRMS_SalaryDetail.Status = 1

INNER JOIN HRMS_PayrollType on HRMS_SalaryDetail.PayrollTypeID = HRMS_PayrollType.PayrollTypeID AND
HRMS_SalaryDetail.PayrollTypeCode = HRMS_PayrollType.PayrollTypeCode 
AND HRMS_PayrollType.Status = 1 

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID = HRMS_Set_Employee.StatusID

where HRMS_SalaryHeader.CompanyID in @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID 

and HRMS_PayrollType.Description like 'Income%Tax'
and HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate 
and HRMS_Employement_Status.Description like '%Permanent%'

group by HRMS_Set_Employee.PersonID )

)t

UNION ALL

select 'Taxable' as Heading, sum(Round(HRMS_BonusDetail.NetBonusAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_BonusDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_BonusHeader On HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID And HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID And HRMS_BonusHeader.Status = 1

where HRMS_BonusDetail.CompanyID in @CompanyID and HRMS_BonusDetail.PeriodID = @PeriodID and HRMS_BonusHeader.CurrentManulDate Between @FromDate and @ToDate
	  and HRMS_BonusDetail.InComeTax != 0
and HRMS_Employement_Status.Description like '%Permanent%'
		
group by EMP_ExpenseType .ExpenseType

UNION ALL

select 'Below Taxable' as Heading, sum(Round(HRMS_BonusDetail.NetBonusAmount,0)) as Amount 

from HRMS_Set_Employee

inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
and HRMS_BonusDetail.Status = 1

inner join EMP_ExpenseType on EMP_ExpenseType.ExpenseTypeID = HRMS_Set_Employee.ExpenseTypeID and EMP_ExpenseType.Status = 1

inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID and HRMS_Employement_Status.Status = 1

Inner Join HRMS_BonusHeader On HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID And HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID And HRMS_BonusHeader.Status = 1

where HRMS_BonusDetail.CompanyID in @CompanyID and HRMS_BonusDetail.PeriodID = @PeriodID and HRMS_BonusHeader.CurrentManulDate Between @FromDate and @ToDate
	  and HRMS_BonusDetail.InComeTax = 0
and HRMS_Employement_Status.Description like '%Permanent%'
		
group by EMP_ExpenseType .ExpenseType

)final group by final.Heading

";
                #endregion

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(SalariesAndWagesReport, additionalFilter).Replace("@CompanyID", companyID), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataTable PermanentStaffReport(string companyID, int PeriodID, DateTime SalaryDate)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);

                #region MyRegion

                const string Quey_PermanentStaffReport = @"             
                select Row_Number() Over (Order by Year(HRMS_SalaryHeader.SalaryDate),month(HRMS_SalaryHeader.SalaryDate) asc) as RowNumber, 'Salary' as Heading
                ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus
                ,sum(HRMS_SalaryHeader.GrossAmount) as Amount
                ,(Convert(Varchar(50),{0}) + '-' +  Convert(Varchar(10),YEAR(HRMS_SalaryHeader.SalaryDate))) as SalaryMonth
                from HRMS_Set_Employee
                inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
                and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_SalaryHeader.Status = 1 

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Permanent%Head%' or HRMS_Employement_Status.Description like '%Permanent%Site%')
                and HRMS_Employement_Status.Status = 1
                where HRMS_SalaryHeader.CompanyID in @CompanyID
                and HRMS_SalaryHeader.PeriodID = @PeriodID
                and convert(date,HRMS_SalaryHeader.SalaryDate) <= @SalaryDate
                GROUP BY {0}, MONTH(HRMS_SalaryHeader.SalaryDate), YEAR(HRMS_SalaryHeader.SalaryDate) , HRMS_Employement_Status.Description
                --order by Year(SalaryDate),month(SalaryDate)

                union all

                select Row_Number() Over (Order by HRMS_BonusHeader.BonusNo asc) as RowNumber, 'Bonus' as Heading ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,SUM(HRMS_BonusDetail.BonusAmount) as Amount
                ,(CAST(HRMS_BonusHeader.BonusNo AS VARCHAR(10))
                        +
                        CASE
                            WHEN HRMS_BonusHeader.BonusNo  % 100 IN (11,12,13) THEN 'th'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 1 THEN 'st'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 2 THEN 'nd'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 3 THEN 'rd'
                            ELSE 'th'
                        END) as SalaryMonth

                from HRMS_Set_Employee
                inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID
                and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_BonusDetail.Status = 1 

                inner join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID
                and HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
                and HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID
                and HRMS_BonusHeader.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Permanent%Head%' or HRMS_Employement_Status.Description like '%Permanent%Site%')
                where HRMS_BonusDetail.CompanyID in @CompanyID
                      and HRMS_BonusDetail.PeriodID = @PeriodID
	                  and HRMS_BonusDetail.Status = 1
                      and convert(date,HRMS_BonusHeader.CreateDate) <= @SalaryDate
                group by  HRMS_BonusHeader.BonusNo,HRMS_Employement_Status.Description
              
                Union ALL

                select Row_Number() Over (Order by HRMS_Employement_Status.Description asc) as RowNumber, 'Overtime' as Heading, count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,sum(Round(HRMS_OverTimePostingDetail.OverTimeAmount,0)) as Amount, 'OverTime' as SalaryMonth 

                from HRMS_Set_Employee

                inner join HRMS_OverTimePostingDetail on HRMS_OverTimePostingDetail.PersonID = HRMS_Set_Employee.PersonID 
                and HRMS_OverTimePostingDetail.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_OverTimePostingDetail.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Permanent%Head%' or HRMS_Employement_Status.Description like '%Permanent%Site%')

                Inner Join HRMS_OverTimePostingHeader On HRMS_OverTimePostingHeader.OTPHeaderID = HRMS_OverTimePostingDetail.OTPHeaderID
                And HRMS_OverTimePostingHeader.CompanyID = HRMS_OverTimePostingDetail.CompanyID And HRMS_OverTimePostingHeader.PeriodID = HRMS_OverTimePostingDetail.PeriodID
                And HRMS_OverTimePostingHeader.Status = 1

                where HRMS_OverTimePostingDetail.CompanyID in @CompanyID and HRMS_OverTimePostingDetail.PeriodID = @PeriodID and HRMS_Set_Employee.Status = 1
	                  and HRMS_OverTimePostingHeader.OverTimeDate <= @SalaryDate
                Group by HRMS_Employement_Status.Description
                ";

                sb.Append("{ fn MONTHNAME(HRMS_SalaryHeader.SalaryDate) }");
                #endregion

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Quey_PermanentStaffReport, sb).Replace("@CompanyID", companyID), sqlparams).Tables[0];              
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable SeasonalStaffReport(string companyID, int PeriodID, DateTime SalaryDate)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);

                #region MyRegion

                const string Query = @"             
                select Row_Number() Over (Order by Year(HRMS_SalaryHeader.SalaryDate),month(HRMS_SalaryHeader.SalaryDate) asc) as RowNumber, 'Salary' as Heading
                ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus
                ,sum(HRMS_SalaryHeader.GrossAmount) as Amount
                ,(Convert(Varchar(50),{0}) + '-' +  Convert(Varchar(10),YEAR(HRMS_SalaryHeader.SalaryDate))) as SalaryMonth
                from HRMS_Set_Employee
                inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
                and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_SalaryHeader.Status = 1 

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Seasonal%Head%' or HRMS_Employement_Status.Description like '%Seasonal%Site%')
                and HRMS_Employement_Status.Status = 1
                where HRMS_SalaryHeader.CompanyID in @CompanyID
                and HRMS_SalaryHeader.PeriodID = @PeriodID
                and convert(date,HRMS_SalaryHeader.SalaryDate) <= @SalaryDate
                GROUP BY {0}, MONTH(HRMS_SalaryHeader.SalaryDate), YEAR(HRMS_SalaryHeader.SalaryDate) , HRMS_Employement_Status.Description
                --order by Year(SalaryDate),month(SalaryDate)

                union all

                select Row_Number() Over (Order by HRMS_BonusHeader.BonusNo asc) as RowNumber, 'Bonus' as Heading ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,SUM(HRMS_BonusDetail.BonusAmount) as Amount
                ,(CAST(HRMS_BonusHeader.BonusNo AS VARCHAR(10))
                        +
                        CASE
                            WHEN HRMS_BonusHeader.BonusNo  % 100 IN (11,12,13) THEN 'th'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 1 THEN 'st'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 2 THEN 'nd'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 3 THEN 'rd'
                            ELSE 'th'
                        END) as SalaryMonth

                from HRMS_Set_Employee
                inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID
                and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_BonusDetail.Status = 1 

                inner join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID
                and HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
                and HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID
                and HRMS_BonusHeader.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Seasonal%Head%' or HRMS_Employement_Status.Description like '%Seasonal%Site%')
                where HRMS_BonusDetail.CompanyID in @CompanyID
                      and HRMS_BonusDetail.PeriodID = @PeriodID
	                  and HRMS_BonusDetail.Status = 1
                      and convert(date,HRMS_BonusHeader.CreateDate) <= @SalaryDate
                group by  HRMS_BonusHeader.BonusNo,HRMS_Employement_Status.Description
              
                Union ALL

                select Row_Number() Over (Order by HRMS_Employement_Status.Description asc) as RowNumber, 'Overtime' as Heading, count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,sum(Round(HRMS_OverTimePostingDetail.OverTimeAmount,0)) as Amount, 'OverTime' as SalaryMonth 

                from HRMS_Set_Employee

                inner join HRMS_OverTimePostingDetail on HRMS_OverTimePostingDetail.PersonID = HRMS_Set_Employee.PersonID 
                and HRMS_OverTimePostingDetail.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_OverTimePostingDetail.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Seasonal%Head%' or HRMS_Employement_Status.Description like '%Seasonal%Site%')

                Inner Join HRMS_OverTimePostingHeader On HRMS_OverTimePostingHeader.OTPHeaderID = HRMS_OverTimePostingDetail.OTPHeaderID
                And HRMS_OverTimePostingHeader.CompanyID = HRMS_OverTimePostingDetail.CompanyID And HRMS_OverTimePostingHeader.PeriodID = HRMS_OverTimePostingDetail.PeriodID
                And HRMS_OverTimePostingHeader.Status = 1

                where HRMS_OverTimePostingDetail.CompanyID in @CompanyID and HRMS_OverTimePostingDetail.PeriodID = @PeriodID and HRMS_Set_Employee.Status = 1
	                  and HRMS_OverTimePostingHeader.OverTimeDate <= @SalaryDate
                Group by HRMS_Employement_Status.Description

                ";

                sb.Append("{ fn MONTHNAME(HRMS_SalaryHeader.SalaryDate) }");
                #endregion

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb).Replace("@CompanyID", companyID), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable DailyWagesReport(string companyID, int PeriodID, DateTime SalaryDate)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);

                #region MyRegion

                const string Query = @"             
                select Row_Number() Over (Order by Year(HRMS_SalaryHeader.SalaryDate),month(HRMS_SalaryHeader.SalaryDate) asc) as RowNumber, 'Salary' as Heading
                ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus
                ,sum(HRMS_SalaryHeader.GrossAmount) as Amount
                ,(Convert(Varchar(50),{0}) + '-' +  Convert(Varchar(10),YEAR(HRMS_SalaryHeader.SalaryDate))) as SalaryMonth
                from HRMS_Set_Employee
                inner join HRMS_SalaryHeader on HRMS_SalaryHeader.PersonID = HRMS_Set_Employee.PersonID
                and HRMS_SalaryHeader.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_SalaryHeader.Status = 1 

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Daily Wages%Head%' or HRMS_Employement_Status.Description like '%Daily Wages%Site%')
                and HRMS_Employement_Status.Status = 1
                where HRMS_SalaryHeader.CompanyID in @CompanyID
                and HRMS_SalaryHeader.PeriodID = @PeriodID
                and convert(date,HRMS_SalaryHeader.SalaryDate) <= @SalaryDate
                GROUP BY {0}, MONTH(HRMS_SalaryHeader.SalaryDate), YEAR(HRMS_SalaryHeader.SalaryDate) , HRMS_Employement_Status.Description
                --order by Year(SalaryDate),month(SalaryDate)

                union all

                select Row_Number() Over (Order by HRMS_BonusHeader.BonusNo asc) as RowNumber, 'Bonus' as Heading ,count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,SUM(HRMS_BonusDetail.BonusAmount) as Amount
                ,(CAST(HRMS_BonusHeader.BonusNo AS VARCHAR(10))
                        +
                        CASE
                            WHEN HRMS_BonusHeader.BonusNo  % 100 IN (11,12,13) THEN 'th'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 1 THEN 'st'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 2 THEN 'nd'
                            WHEN HRMS_BonusHeader.BonusNo  % 10 = 3 THEN 'rd'
                            ELSE 'th'
                        END) as SalaryMonth

                from HRMS_Set_Employee
                inner join HRMS_BonusDetail on HRMS_BonusDetail.EmployeeID = HRMS_Set_Employee.PersonID
                and HRMS_BonusDetail.CompanyID = HRMS_Set_Employee.CompanyID
                and HRMS_BonusDetail.Status = 1 

                inner join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID
                and HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
                and HRMS_BonusHeader.PeriodID = HRMS_BonusDetail.PeriodID
                and HRMS_BonusHeader.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Daily Wages%Head%' or HRMS_Employement_Status.Description like '%Daily Wages%Site%')
                where HRMS_BonusDetail.CompanyID in @CompanyID
                      and HRMS_BonusDetail.PeriodID = @PeriodID
	                  and HRMS_BonusDetail.Status = 1
                      and convert(date,HRMS_BonusHeader.CreateDate) <= @SalaryDate
                group by  HRMS_BonusHeader.BonusNo,HRMS_Employement_Status.Description
              
                Union ALL

                select Row_Number() Over (Order by HRMS_Employement_Status.Description asc) as RowNumber, 'Overtime' as Heading, count(HRMS_Set_Employee.personID) as NoOfEmp
                ,HRMS_Employement_Status.Description as EmpStatus ,sum(Round(HRMS_OverTimePostingDetail.OverTimeAmount,0)) as Amount, 'OverTime' as SalaryMonth 

                from HRMS_Set_Employee

                inner join HRMS_OverTimePostingDetail on HRMS_OverTimePostingDetail.PersonID = HRMS_Set_Employee.PersonID 
                and HRMS_OverTimePostingDetail.CompanyID = HRMS_Set_Employee.CompanyID and HRMS_OverTimePostingDetail.Status = 1

                inner join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =  HRMS_Set_Employee.StatusID
                and (HRMS_Employement_Status.Description like '%Daily Wages%Head%' or HRMS_Employement_Status.Description like '%Daily Wages%Site%')

                Inner Join HRMS_OverTimePostingHeader On HRMS_OverTimePostingHeader.OTPHeaderID = HRMS_OverTimePostingDetail.OTPHeaderID
                And HRMS_OverTimePostingHeader.CompanyID = HRMS_OverTimePostingDetail.CompanyID And HRMS_OverTimePostingHeader.PeriodID = HRMS_OverTimePostingDetail.PeriodID
                And HRMS_OverTimePostingHeader.Status = 1

                where HRMS_OverTimePostingDetail.CompanyID in @CompanyID and HRMS_OverTimePostingDetail.PeriodID = @PeriodID and HRMS_Set_Employee.Status = 1
	                  and HRMS_OverTimePostingHeader.OverTimeDate <= @SalaryDate
                Group by HRMS_Employement_Status.Description


                ";

                sb.Append("{ fn MONTHNAME(HRMS_SalaryHeader.SalaryDate) }");
                #endregion

                companyID = "(" + companyID + ")";

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb).Replace("@CompanyID", companyID), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        #endregion

        public static object GetAllDataSalariesWagesTaxableLimit(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalariesWagesBelowTaxableLimit, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalariesWagesBelowTaxableLimit, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataSummarySalariesWagesTaxableLimit(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SummarySalaryWagesTaxableLimit, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SummarySalaryWagesTaxableLimit, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataSalariesWagesBelowTaxableLimit(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalariesWagesBelowTaxableLimit, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalariesWagesBelowTaxableLimit, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataDetailOfTax(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DetailOfTaxReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DetailOfTaxReport, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataReportOnTaxableExemptSalaryPayment(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, int? StatusID, Nullable<DateTime> Date_To, int? PersonID)
        {
            DataTable dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_ReportOnTaxableExemptSalaryPayment, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_ReportOnTaxableExemptSalaryPayment, sqlparams);
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataBTLSalaryPayment(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BTLSalaryPayment, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_BTLSalaryPayment, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataPODetailLocal(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailLocal, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailLocal, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public static object GetAllDataPODetailImported(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailImported, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PODetailImported, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        public DataSet EmployeeBonusSlipForEmail(int companyID, int periodID, int PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {

            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            if (PersonID > 0)
                sb.Append(" and HRMS_BonusDetail.EmployeeID =  " + PersonID);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                if (objTransaction == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetEmployeeBonusReport_SlipForEmail, sb.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.PayRoll.Transaction.HRMS_Appraisal.Query_GetEmployeeBonusReport_SlipForEmail, sb.ToString()), sqlparams);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataTable EmployeeSalarySheet_ForAsadRice_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                //if (AppendSerialNo != null)
                //{
                //    sb1.Append(AppendSerialNo);
                //}
                if (objConnection == null)

                    if (bandhiparameter == true)
                    {
                        if (salaryconnectparameter == true)
                        {

                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData_AsadRice + null + OrderByConnect, sb, sb1), sqlparams);
                        }
                        else
                        {
                            ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData_AsadRice + null + OrderBy, sb, sb1), sqlparams);
                        }
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                else
                    if (bandhiparameter == true)
                    {
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheetForBandhi_ReportData_AsadRice + null, sb), sqlparams);
                    }
                    else
                    {
                        //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_EmployeeSalarySheet_ReportData, sqlparams);
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_EmployeeSalarySheet_ReportData + query, sb), sqlparams);
                    }
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static DataTable AttendanceSheetCountWise_ReportData(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, string query = null, string queryforBandhi = null, bool bandhiparameter = false, bool salaryconnectparameter = false, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, string AppendSerialNo = null, Nullable<Guid> UserID = null)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {

                //SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                //sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());


                //const string OrderBy = @")FinalTab Order bY cast (FinalTab.EmployeeCode as int)";
                //const string OrderByConnect = @")FinalTab Order bY cast (FinalTab.EmployeeCode as varchar)";


                //StringBuilder sb = new StringBuilder();
                //StringBuilder sb1 = new StringBuilder();

                //if (LocationID > 0)
                //{
                //    sb.Append(" and HRMS_SE.FK_LocationID = " + LocationID);
                //}

                //if (DivisionID > 0)
                //{
                //    sb.Append(" and HRMS_SE.DivisionID = " + DivisionID);
                //}

                //if (EmployeeTypeID > 0)
                //{
                //    sb.Append(" and HRMS_SE.PersonTypeCode = " + EmployeeTypeID);
                //}
                //if (JobCategoryID > 0)
                //{
                //    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                //}
                //if (PayrollGroupID != null && PayrollGroupID > 0)
                //{
                //    sb.Append(" AND HRMS_SH.PayrollGroupID  = " + PayrollGroupID);
                //}
                //if (EmployeeStatus != null && EmployeeStatus > 0)
                //{
                //    sb.Append(" and HRMS_SE.StatusID = " + EmployeeStatus);
                //}
                //else
                //{
                //    sb.Append(" and HRMS_SE.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                //}


                //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query2_AttendanceSheetCountWise_ReportData + query, sb), sqlparams);
                    
               
                //if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                //    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }

        public static int GetLeavesFromPayroll(int companyID, int PeriodID, int PersonID, int fiscalid, string Year, SqlTransaction sqltrans = null)
        {

            #region Query

            const string Query = @" 
select (sum(tab.ActualLeaves) - sum(tab.Leaves)) as RemainingLeaves from (
select sum(isnull(LeaveDays,0)) as ActualLeaves,0 as Leaves from HRMS_EmployeePayroll

 where HRMS_EmployeePayroll.Status = 1
   and HRMS_EmployeePayroll.CompanyID = @CompanyID
   and HRMS_EmployeePayroll.PeriodID = @PeriodID
   and HRMS_EmployeePayroll.PersonID = @PersonID
  -- and HRMS_EmployeePayroll.FiscalID = @FiscalID

union all

Select 0 as ActualLeaves,sum(isnull(Leaves,0)) as Leaves from HRMS_SalaryHeader 
   where HRMS_SalaryHeader.Status = 1
   and HRMS_SalaryHeader.CompanyID = @CompanyID
   and HRMS_SalaryHeader.PeriodID = @PeriodID
   and HRMS_SalaryHeader.PersonID = @PersonID
   and year(HRMS_SalaryHeader.SalaryDate) = @Year
  --and HRMS_SalaryHeader.fisca = @FiscalID
  )
  tab
";
            #endregion

            int Leaves = 0;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@Year", Year);

                Leaves = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return Leaves;
        }

        public static int GetShiftHours(int companyID, int PeriodID, int PersonID, int fiscalid, SqlTransaction sqltrans = null)
        {

            #region Query

            const string Query = @" 
  select distinct convert(int,HRMS_Set_Shift.HoursPerDay) as HoursPerDay from HRMS_AssignShiftsDetail
  inner join  HRMS_AssignShiftsHeader on HRMS_AssignShiftsHeader.HeaderID = HRMS_AssignShiftsDetail.HeaderID
  and HRMS_AssignShiftsDetail.Status = 1
inner join HRMS_Set_Shift on HRMS_Set_Shift.ShiftID = HRMS_AssignShiftsDetail.ShiftID
and HRMS_Set_Shift.HoursPerDay > 0 

where HRMS_AssignShiftsHeader.PersonID = @PersonID
and HRMS_AssignShiftsHeader.CompanyID = @CompanyID
and HRMS_AssignShiftsHeader.PeriodID = @PeriodID
and HRMS_AssignShiftsHeader.Status = 1
and HRMS_Set_Shift.IsActive = 1 and HRMS_Set_Shift.status = 1
";
            #endregion

            int shifthours = 0;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                shifthours = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return shifthours;
        }

        public static DataTable GetLeavesDetailReportData_AlAsad(Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo, int companyID, int? personID = 0, int FiscalID = 0, int PeriodID = 0, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[5] = new SqlParameter("@ToDate", DateTo);

                #region Query

                string Query_GetLeavesDetailReport_AlAsad = @"
Select 
tab.EmployeeCode as EmployeeCode,
tab.EmployeeName as EmployeeName,
tab.FatherName as FatherName,
tab.LeaveDays as LeaveDays,
SUM(tab.Oct) as Oct,
SUM(tab.Nov) as Nov,
SUM(tab.Dec) as Dec,
SUM(tab.Jan) as Jan,
SUM(tab.Feb) as Feb,
SUM(tab.Mar) as Mar,
SUM(tab.Apr) as Apr,
SUM(tab.May) as May,
SUM(tab.Jun) as Jun,
SUM(tab.Jul) as Jul,
SUM(tab.Aug) as Aug,
SUM(tab.Sep) as Sep
from
(
Select HRMS_Set_Employee.EmployeeCode, HRMS_Set_Employee.FirstName As EmployeeName, HRMS_Set_Employee.FatherName,HRMS_EmployeePayroll.LeaveDays
,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 0, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 0, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 10
),0)As Oct

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 1, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 1, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 11
),0)As Nov

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 2, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 2, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 12
),0)As Dec

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 3, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 3, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 1
),0)As Jan

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 4, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 4, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 2
),0)As Feb

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 5, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 5, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 3
),0)As Mar

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 6, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 6, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 4
),0)As Apr

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 7, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 7, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 5
),0)As May

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 8, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 8, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 6
),0)As Jun

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 9, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 9, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 7
),0)As Jul

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 10, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 10, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 8
),0)As Aug

,ISNULL(
(Select HRMS_SalaryHeader.Leaves
--Where HRMS_SalaryHeader.SalaryDate Between Dateadd(Month, 11, SET_PeriodCustom.StartDate) And EOMONTH(Dateadd(Month, 11, SET_PeriodCustom.Enddate))
where year(HRMS_SalaryHeader.SalaryDate) Between year(@FromDate) And year(@ToDate) and month(HRMS_SalaryHeader.SalaryDate) = 9
),0)As Sep

from HRMS_SalaryHeader

Inner Join HRMS_Set_Employee On HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
And HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
And HRMS_Set_Employee.Status = 1

Inner Join HRMS_EmployeePayroll On HRMS_EmployeePayroll.PersonID = HRMS_SalaryHeader.PersonID
And HRMS_EmployeePayroll.CompanyID = HRMS_SalaryHeader.CompanyID
And HRMS_EmployeePayroll.PeriodID = HRMS_SalaryHeader.PeriodID
And HRMS_EmployeePayroll.PayrollTypeCode = 4
And HRMS_EmployeePayroll.PayrollTypeID = 18
And HRMS_EmployeePayroll.Status = 1

Inner Join SET_PeriodCustom On SET_PeriodCustom.CompanyID = HRMS_SalaryHeader.CompanyID
And SET_PeriodCustom.CompanyID = HRMS_SalaryHeader.CompanyID
And SET_PeriodCustom.PeriodID = HRMS_SalaryHeader.PeriodID
And SET_PeriodCustom.FiscalId = @FiscalID

Where HRMS_SalaryHeader.CompanyID = @CompanyID And HRMS_SalaryHeader.PeriodID = @PeriodID And HRMS_SalaryHeader.Status = 1
And HRMS_SalaryHeader.SalaryDate Between @FromDate And @ToDate
{0}
)tab
Group By tab.EmployeeCode, tab.EmployeeName, tab.FatherName,tab.LeaveDays
Order By cast(tab.EmployeeCode as int) asc
";

                #endregion

                if (personID > 0)
                {
                    sb.Append(" and HRMS_SalaryHeader.PersonID =  " + personID);
                }
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetLeavesDetailReport_AlAsad, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetLeavesDetailReport_AlAsad, sb.ToString()), sqlparams);

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

        public DataSet GetData_PaySlipReportForAsadRice(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<int> emplyeeID, Nullable<int> EmpStatusID,
        SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, int CurrentFiscalID = 0)
        {
            DataSet ds = null;
            DataSet ds1 = null;

            DataTable dt1 = null;

            try
            {
                DateTime startDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddMonths(1).AddDays(-1);
                DateTime loanDate = new DateTime(Date_From.Value.Year, Date_From.Value.Month, 1).AddDays(-1);
                SqlParameter[] sqlparams = new SqlParameter[12];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (emplyeeID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", emplyeeID);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", Date_From));
                sqlparams[4] = new SqlParameter("@Months", Date_From.Value.Month.ToString());
                sqlparams[5] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[6] = new SqlParameter("@Date", startDate);

                sqlparams[7] = new SqlParameter("@fullDate", startDate);

                sqlparams[8] = new SqlParameter("@loanDate", loanDate);
                sqlparams[9] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (EmployeeTypeID > 0)
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", EmployeeTypeID);
                else
                    sqlparams[10] = new SqlParameter("@PersonTypeCode", DBNull.Value);

                if (EmpStatusID > 0)
                    sqlparams[11] = new SqlParameter("@StatusID", EmpStatusID);
                else
                    sqlparams[11] = new SqlParameter("@StatusID", DBNull.Value);



                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                    sb1.Append(" where SE.FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.DivisionID = " + DivisionID);
                    }
                    else
                    {
                        sb1.Append(" where SE.DivisionID = " + DivisionID);
                    }
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                    if (sb1.Length > 0)
                    {
                        sb1.Append(" and SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                    else
                    {
                        sb1.Append(" where SE.PersonTypeCode = " + EmployeeTypeID);
                    }
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PaySlipReportForAsadRice, sb, sb1), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_PaySlipReportForAsadRice, sb, sb1), sqlparams);
                }

                if (objConnection == null)
                    dt1 = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpDeductionForAsadRice, sqlparams).Tables[0];
                else
                {
                    dt1 = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.query_EmpDeductionForAsadRice, sqlparams).Tables[0];
                }


                try
                {
                    dt1.TableName = "tblDeduction";
                    DataTable dt2 = dt1.Copy();

                    ds.Tables.Add(dt2);

                }
                catch { }


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataSet Get_SalarySummaryWithVariance(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int PersonID, int LocationID = 0,
            int DivisionID = 0, int EmployeeTypeID = 0, int JobCategoryID = 0,Nullable<int> DepartmentID = null,int CostCenterID = 0, Nullable<int> PayrollGroupID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null
            ,SqlConnection objConnection = null)
        {
            
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentMonth", String.Format("{0:MMMM}", Date_From));
                sqlparams[3] = new SqlParameter("@Year", Date_From.Value.Year.ToString());
                sqlparams[4] = new SqlParameter("@PrvMonth", String.Format("{0:MMMM}", Date_To));

                if (PersonID > 0)
                    sqlparams[5] = new SqlParameter("@PersonID", PersonID);
                else
                    sqlparams[5] = new SqlParameter("@PersonID", DBNull.Value);

                if (LocationID > 0)
                {
                    sb.Append(" and HRMS_Set_Employee.FK_LocationID = " + LocationID);
                }


                if (DivisionID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.PersonTypeCode = " + EmployeeTypeID);
                }
                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" AND HRMS_SalaryHeader.PayrollGroupID  = " + PayrollGroupID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (JobCategoryID > 0)
                {
                    sb.Append(" AND  [SET_JobCategory].JobCategoryID = " + JobCategoryID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SET_Employee].FK_DepartmentID = " + DepartmentID);
                }
                if (CostCenterID > 0)
                {
                    sb.Append(" AND [HRMS_SET_Employee].CostCenterID = " + CostCenterID);
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalarySummaryWithVariance, sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SalarySummaryWithVariance, sb.ToString(), sb1.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                {
                    ds.Tables[0].TableName = "Table";
                    ds.Tables[1].TableName = "Table1";
                    ds.Tables[2].TableName = "Table2";
                    ds.Tables[3].TableName = "Table3";
                    //ds.Tables[3].TableName = "Table3";

                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetSalayDataForEmail(int CompanyID, Nullable<DateTime> SalaryDate, Nullable<int> EmpStatusID, Nullable<int> EmployeeID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@SalaryDate", SalaryDate);
                if (EmpStatusID.HasValue)
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", EmpStatusID);
                }
                else
                {
                    sqlparams[1] = new SqlParameter("@EmpStatusID", DBNull.Value);
                }

                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

                if (EmployeeID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@EmployeeID", EmployeeID);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@EmployeeID", DBNull.Value);
                }
                DataSet ds = null;

                const string Query_GetSalayDataForEmail = @"
select distinct HRMS_SalaryHeader.CompanyID,HRMS_SalaryHeader.PeriodID, HRMS_SalaryHeader .PersonID ,HRMS_SalaryHeader.SalaryDate,HRMS_SalaryHeader .SalaryHeaderID
from HRMS_SalaryHeader 
INNER JOIN HRMS_SalaryDetail ON HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader.SalaryHeaderID
					and HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader.CompanyID
					and HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader.PeriodID

INNER JOIN HRMS_Set_Employee ON HRMS_Set_Employee.PersonID = HRMS_SalaryHeader.PersonID
					and HRMS_Set_Employee.CompanyID = HRMS_SalaryHeader.CompanyID
WHERE     HRMS_SalaryHeader .[Status] = 1
                    AND HRMS_SalaryHeader .[SalaryDate] = @SalaryDate
					and HRMS_SalaryHeader .CompanyID = @CompanyID
                    AND HRMS_SalaryDetail.Amount <> 0
                    AND ( HRMS_Set_Employee.StatusID = @EmpStatusID OR @EmpStatusID IS NULL )
                    AND ( HRMS_Set_Employee.PersonID = @EmployeeID OR @EmployeeID IS NULL )
        ";


                if (objConnection == null)
                {

                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetSalayDataForEmail, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalayDataForEmail, sqlparams);

                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetSalayDataForEmail, sqlparams);


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


        public static DataTable GetPreviousMonthSalary(int CompanyID, int PersonID, string Month, string Year, SqlTransaction sqltrans = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            const string query = @" 
select HRMS_SalaryDetail.Amount 
from HRMS_SalaryHeader
inner join HRMS_SalaryDetail on HRMS_SalaryDetail.SalaryHeaderID = hrms_SalaryHeader.SalaryHeaderID
and HRMS_SalaryDetail.CompanyID = hrms_SalaryHeader.CompanyID
and HRMS_SalaryDetail.PeriodID = hrms_SalaryHeader.PeriodID
and HRMS_SalaryDetail.Status = 1
and HRMS_SalaryDetail.PayrollTypeID in (select PayrollTypeID from HRMS_PayrollType where Description like '%Basic Salary%' )
where HRMS_SalaryHeader.Status = 1
and HRMS_SalaryHeader.CompanyID = @CompanyID
and HRMS_SalaryHeader.PersonID = @PersonID
and month(HRMS_SalaryHeader.SalaryDate) = @Month
and year(HRMS_SalaryHeader.SalaryDate) = @Year
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@Month", Month);
                sqlparams[3] = new SqlParameter("@Year", Year);
                
                //ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (sqltrans != null)
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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
        public static DataTable GetEmployeeTaxAndSalaryHistory(int CompanyID, int PeriodID, int FiscalID, DateTime SalaryDate, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", FiscalID);
                //sqlparams[3] = new SqlParameter("@PersonID", PersonID);
                sqlparams[3] = new SqlParameter("@SalaryDate", SalaryDate);

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetEmployeeTaxAndSalaryHistory), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetEmployeeTaxAndSalaryHistory), sqlparams);
                }

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }

        #region Roaster Not Assing Report
        public static DataTable Get_RoasterNotAssingReport(Nullable<int> companyID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int emplyeeID = 0, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> payrolltypeID = null, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);//Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                if (LocationID > 0)
                {
                    sb.Append(" and EP.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" and (EP.FK_DepartmentID =  " + DepartmentID + " ) ");

                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and EP.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and EP.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_RoasterNotAssing, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_RoasterNotAssing, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds.Tables[0];
        }
        #endregion



        public static DataTable GetGrossAmountForTaxCalculation(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, int PersonID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);


                const string Query_GetGrossAmountForTaxCalculation = @"
select sum(Amount) as Amount,count(distinct HRMS_SalaryHeader.SalaryHeaderID) as RowsCount
from HRMS_SalaryHeader 
inner join HRMS_SalaryDetail on HRMS_SalaryHeader.SalaryHeaderID = HRMS_SalaryDetail.SalaryHeaderID
and HRMS_SalaryHeader.CompanyID = HRMS_SalaryDetail.CompanyID
and HRMS_SalaryHeader.Status = 1 and PayrollTypeCode = 1
inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode 
and HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
and HRMS_PayrollType.istaxable = 1
where HRMS_SalaryHeader .Status = 1 and HRMS_SalaryHeader .CompanyID = @CompanyID and PersonID = @PersonID
and SalaryDate between (select top 1 FromDate from HRMS_SET_TaxYear where Status = 1 and HRMS_SET_TaxYear.CompanyID = HRMS_SalaryHeader.CompanyID order by FromDate  desc) and @SalaryDate
";


                DataSet ds = null;
                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetGrossAmountForTaxCalculation), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetGrossAmountForTaxCalculation), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetGrossAmountForTaxCalculation), sqlparams);

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



        public static DataTable GetPreviousIncomeTax(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, int PersonID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[3] = new SqlParameter("@PersonID", PersonID);


                const string Query_GetPreviousIncomeTax = @"
 select HRMS_SalaryHeader.SalaryDate, HRMS_SalaryDetail .Amount 
 from HRMS_SalaryHeader 
 inner join HRMS_SalaryDetail  on HRMS_SalaryDetail.SalaryHeaderID = HRMS_SalaryHeader .SalaryHeaderID
 and  HRMS_SalaryDetail.CompanyID = HRMS_SalaryHeader .CompanyID
 and  HRMS_SalaryDetail.PeriodID = HRMS_SalaryHeader .PeriodID
 and  HRMS_SalaryDetail.status=1
 inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_SalaryDetail.PayrollTypeID
 and HRMS_PayrollType.PayrollTypeCode = HRMS_SalaryDetail.PayrollTypeCode
 
 inner join HRMS_SET_TaxYear on HRMS_SET_TaxYear.FromDate < @SalaryDate and HRMS_SET_TaxYear.ToDate >= @SalaryDate

 where HRMS_SalaryHeader.Status = 1 and HRMS_SalaryHeader.CompanyID = @CompanyID and HRMS_SalaryHeader.PeriodID = @PeriodID
 and PersonID = @PersonID
 and HRMS_PayrollType.Description like '%Income Tax%'
 and HRMS_SalaryHeader.SalaryDate between HRMS_SET_TaxYear.FromDate and HRMS_SET_TaxYear.ToDate 
";


                DataSet ds = null;
                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetPreviousIncomeTax), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPreviousIncomeTax), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetPreviousIncomeTax), sqlparams);

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

        public static DataRow Get_PayrollTypeSalaryDetailByPersonID(int companyID, int periodID, DateTime SalaryDate, int PersonID, int PayrollTypeID, bool IsFromPayroll, SqlTransaction sqltrans = null, SqlConnection objConnection = null, bool IsGrossWithAllTypes = false)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryMonth", SalaryDate.Month);
                sqlparams[3] = new SqlParameter("@SalaryYear", SalaryDate.Year);
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);
                sqlparams[5] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

                string Query = null;

                if (IsFromPayroll)
                    Query = FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_PayrollCardByPersonIDAndPayrollTypeID;
                else
                    Query = FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_PayrollTypeSalaryDetailByPersonID;

                if (IsGrossWithAllTypes)
                {
                    Query = Query.Replace("{filterPayrollType}", "");
                }
                else
                {
                    Query = Query.Replace("{filterPayrollType}", "AND HRMS_PayrollType.PayrollTypeID NOT IN (13, 51, 85, 46)");
                }

                DataSet ds = null;
                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0] : null;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataRow Get_PayrollTypeSalaryDetailByPersonID_Deduct(int companyID, int periodID, DateTime SalaryDate, int PersonID, int PayrollTypeID, bool IsFromGross, SqlTransaction sqltrans = null, SqlConnection objConnection = null, bool IsGrossWithAllTypes = false)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalaryMonth", SalaryDate.Month);
                sqlparams[3] = new SqlParameter("@SalaryYear", SalaryDate.Year);
                sqlparams[4] = new SqlParameter("@PersonID", PersonID);
                sqlparams[5] = new SqlParameter("@PayrollTypeID", PayrollTypeID);

                string Query = null;

                if (IsFromGross)
                    Query = FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_PayrollCardByPersonIDAndPayrollTypeID_New;
                else
                    Query = FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_Get_PayrollTypeSalaryDetailByPersonID;

                if (IsGrossWithAllTypes)
                {
                    Query = Query.Replace("{filterPayrollType}", "");
                }
                else
                {
                    Query = Query.Replace("{filterPayrollType}", "AND HRMS_PayrollType.PayrollTypeID NOT IN (13, 51, 85, 46)");
                }

                DataSet ds = null;
                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dr = ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0 ? ds.Tables[0].Rows[0] : null;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static void UpdateSalaryApprovePosting(List<HRMS_SalaryHeader> lstSalary, List<AR_InvoiceHeader> lstInvoice, List<GL_TransactionHeader> lstGL, SqlTransaction sqltrans = null)
        {
            try
            {
                foreach (HRMS_SalaryHeader updateSalary in lstSalary)
                {
                    updateSalary.UpdateDataByKey(sqltrans);
                }

                foreach (AR_InvoiceHeader updateInvoice in lstInvoice)
                {
                    updateInvoice.UpdateDataByKey(sqltrans);
                }

                foreach (GL_TransactionHeader updateGl in lstGL)
                {
                    updateGl.UpdateDataByKey(sqltrans);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static DataTable GetAllDataForTheSameMonthSalaryDailyWages(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int PersonIDForCheck, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            string Filter = "";
            #region Query

            const string QueryForCheCkSameMonthDailyWages = @" Select * from HRMS_SalaryHeader where CompanyID = @CompanyID AND PeriodID = @PeriodID AND FromDate = @FromDate And ToDate = @ToDate AND Status = 1  ";


            if (PersonIDForCheck > 0)
                Filter = " AND PersonID = @PersonID  ";
            #endregion



            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@PersonID", PersonIDForCheck);


                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, QueryForCheCkSameMonthDailyWages + Filter, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForCheCkSameMonthDailyWages + Filter, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QueryForCheCkSameMonthDailyWages + Filter, sqlparams);

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

        public static DataTable GetAllActiveDataWithTransDailyWages(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        //ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_SelectAllDataWithTransDailyWages, sqlparams);
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransDailyWages), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransDailyWages), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataWithTransDailyWages, sqlparams);
                }
                else
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAllDataWithTransDailyWages, sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransDailyWages), sqlparams);

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

        public static DataTable GetAllActiveDataWithTrans_StatusIDDailyWages(int companyID, int periodID, DateTime fromDate, DateTime toDate, int? empStatusID, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                // Prepare parameters
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                // Add optional employee status filter
                if (empStatusID.HasValue)
                {
                    sb.Append(" AND HRMS_Set_Employee.StatusID = " + empStatusID.Value.ToString());
                }

                DataSet ds = null;

                // Execute the query with or without transaction/connection
                if (objConnection == null)
                {
                    if (sqltrans != null)
                    {
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                        //ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                    }
                    else
                    {
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                        //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                    }
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectAllDataWithTransStatusIDDailyWages, sb), sqlparams);
                }

                // Return the result table
                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    dt = ds.Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }

            return dt;
        }

        public static DataTable GetdataForAlreadySalaryGenerateDailyWages(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, string EmplyeeIds
            , SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> PayrollGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                //sqlparams[3] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                //sqlparams[4] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());


                sb.Append(" and personID in (" + EmplyeeIds + ")");

                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" and PayrollGroupID = " + PayrollGroupID);
                }
                DataSet ds = null;

                if (objConnection == null)
                {
                    // ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetdataForAlreadySalaryGenerateDailyWages, sb.ToString()), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetdataForAlreadySalaryGenerateDailyWages), sqlparams);
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDataWithTrans, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetdataForAlreadySalaryGenerateDailyWages, sb.ToString()), sqlparams);
                //ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForAlreadySalaryGenerate + sb.ToString(), sqlparams);

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

        public static DataTable GetSalaryHeaderIDByDate(Nullable<int> periodID, Nullable<int> COmpanyID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int PersonID, SqlTransaction objTransaction = null, SqlConnection objConnection = null, Nullable<int> PayrollGrpID = null)
        {

            DataTable dt = null;

            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CompanyID", COmpanyID);

                if (PayrollGrpID != null && PayrollGrpID > 0)
                    sb.Append(" AND PayrollGroupID = " + PayrollGrpID);

                if (objTransaction != null)
                    //dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDataByDate, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByDate, sb.ToString()), sqlparams).Tables[0];
                else if (objConnection != null)
                    //dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDataByMonthAndYear, sb.ToString()), sqlparams).Tables[0];
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByDate, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetDataByDate, sb.ToString()), sqlparams).Tables[0];
                //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByMonthAndYear, sb.ToString()), sqlparams).Tables[0];
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
    
