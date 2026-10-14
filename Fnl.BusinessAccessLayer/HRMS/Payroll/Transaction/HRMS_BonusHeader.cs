using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Common.Utils;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;
using System.Data;
using FnI.BusinessAccessLayer.AR.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;

//using FnI.BusinessAccessLayer.HRMS.Payroll.Transaction;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_BonusHeader
    {

        #region Query
        const string Query_DataHeaderDetailForBonusPost = @"
SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        OT.NetBonusAmount as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID
,'Net' as TypeStatus
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue
        FROM HRMS_BonusDetail OT
			Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
		AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        INNER JOIN [HRMS_PayrollType] PT on PT.Description='Bonus'
		AND PT.Status=1
		

        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate
--AND HRMS_BonusHeader.BonusID = @BonusID
{0}
        --And (@OverTimeDate between FromDate and ToDate) 
		      And (OT.IsPosted is null OR OT.IsPosted = 0 )
        union all 
        SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLCodeCombination as GLAccountCode,
        PT.GLImpactTwo as GLImpact, 
        OT.BonusAmount as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID  
,'Bonus' as TypeStatus   
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue   
FROM HRMS_BonusDetail OT
					Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
            AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        INNER JOIN [HRMS_PayrollType] PT on PT.Description='Bonus'
		AND PT.Status=1

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=PT.PayrollTypeID
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		--And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1
        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
--AND HRMS_BonusHeader.BonusID = @BonusID
{0}
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate   
     And (OT.IsPosted is null OR OT.IsPosted = 0 )


 union all
	 --incometax
	 SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        OT.InComeTax as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID
,'Income Tax' as TypeStatus  
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue    
        FROM HRMS_BonusDetail OT
			Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
		AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        INNER JOIN [HRMS_PayrollType] PT on PT.Description='Income Tax'
		AND PT.Status=1
		and isnull(PT.GLImpactTwo,0) != '0'
		
		

        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate  
--AND HRMS_BonusHeader.BonusID =  @BonusID
{0}

		      And (OT.IsPosted is null OR OT.IsPosted = 0 )


			    union all
	 --Advacne salary
	 SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        OT.AdvanceSalary as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID
,'Advance Salary' as TypeStatus 
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue     
        FROM HRMS_BonusDetail OT
			Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
		AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        INNER JOIN [HRMS_PayrollType] PT on PT.Description='Advance Salary'
		AND PT.Status=1
		and isnull(PT.GLImpactTwo,0) != '0'
		

        WHERE OT.[Status] = 1 
AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate  
--AND HRMS_BonusHeader.BonusID =  @BonusID
{0}
		      And (OT.IsPosted is null OR OT.IsPosted = 0 )



union all
--Arrear Of Bonus
			   SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLCodeCombination as GLAccountCode,
        PT.GLImpactTwo as GLImpact, 
        OT.ArrearOfBonus as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID  
,'Arrear Of Bonus' as TypeStatus 
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue     
FROM HRMS_BonusDetail OT
					Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
            AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        INNER JOIN [HRMS_PayrollType] PT on PT.Description='Bonus'
		AND PT.Status=1

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=PT.PayrollTypeID
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		--And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1
        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate  
--AND HRMS_BonusHeader.BonusID =  @BonusID
{0} 
     And (OT.IsPosted is null OR OT.IsPosted = 0 )


union all
--Deduction
			  SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        pt.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact as GLImpact, 
        OT.DeductionOfBonus as GLAmount,ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID  
,'Deduction' as TypeStatus 
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue     
FROM HRMS_BonusDetail OT
					Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
            AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1
        inner JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID =  OT.PayrollTypeID 
		AND PT.Status=1

        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate  
--AND HRMS_BonusHeader.BonusID =  @BonusID
{0} 
     And (OT.IsPosted is null OR OT.IsPosted = 0 )

union all


--Loan
			  SELECT OT.CompanyID,OT.PeriodID,HRMS_BonusHeader.BonusID ,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        OT.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        pt.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact as GLImpact, 
        --OT.DeductionOfBonus as GLAmount,
        HRMS_BonusLoanDetail.LoanDeduction as GLAmount,
       ARC.CustomerName,EMP.CostCenterID
,OT.BonusDetailID  
,'Loan' as TypeStatus  
,HRMS_BonusHeader.CreatedBy
,HRMS_BonusHeader.CreatedByValue    
FROM HRMS_BonusDetail OT
					Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = OT.BonusHeaderID
and HRMS_BonusHeader.CompanyID = OT.CompanyID
			AND  HRMS_BonusHeader.Status = 1
        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=OT.EmployeeID
            AND EMP.CompanyID = OT.CompanyID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 
            AND ARC.companyID = EMP.CompanyID
            AND ARC.Status=1

	inner join HRMS_BonusLoanDetail on HRMS_BonusLoanDetail.BonusHeaderID = OT.BonusHeaderID
	and HRMS_BonusLoanDetail.BonusDetailID = OT.BonusDetailID
	and HRMS_BonusLoanDetail.CompanyID = OT.CompanyID
	and HRMS_BonusLoanDetail.PeriodID = OT.PeriodID
	and HRMS_BonusLoanDetail.Status = 1
	
	     inner JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID =  HRMS_BonusLoanDetail.PayrollTypeID 
		AND PT.Status=1

        WHERE OT.[Status] = 1 
		AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate  
--AND HRMS_BonusHeader.BonusID =  @BonusID
{0} 
     And (OT.IsPosted is null OR OT.IsPosted = 0 )

";

        const string Query_SelectAll_Searching_WithPaging = @"SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(tbl_BH.[BonusID] as int) DESC) AS 'Row' 
FROM 
(
    SELECT  HRMS_BonusHeader.*, HRMS_Employement_Status.Description as EmployeeStatus FROM HRMS_BonusHeader   
Inner join  HRMS_BonusDetail on HRMS_BonusDetail.BonusHeaderID = HRMS_BonusHeader.BonusID
	And HRMS_BonusDetail.CompanyID = HRMS_BonusHeader.CompanyID
	And HRMS_BonusDetail.PeriodID = HRMS_BonusHeader.PeriodID
		Left Join HRMS_Employement_Status on  HRMS_Employement_Status.StatusID = HRMS_BonusHeader.EmployStatusID
        {0} 
) AS tbl_BH 
";

        const string Query_SelectAll_Searching_WithPagingSanghar = @"SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(tbl_BH.[BonusID] as int) DESC) AS 'Row' 
FROM 
(
   SELECT  HRMS_BonusHeader.*, HRMS_Employement_Status.Description as EmployeeStatus, FullName AS EmployeeName FROM HRMS_BonusHeader   
Inner join  HRMS_BonusDetail on HRMS_BonusDetail.BonusHeaderID = HRMS_BonusHeader.BonusID
	And HRMS_BonusDetail.CompanyID = HRMS_BonusHeader.CompanyID
	And HRMS_BonusDetail.PeriodID = HRMS_BonusHeader.PeriodID
		Left Join HRMS_Employement_Status on  HRMS_Employement_Status.StatusID = HRMS_BonusHeader.EmployStatusID
        INNER Join HRMS_Set_Employee on HRMS_Set_Employee.CompanyID = HRMS_BonusDetail.CompanyID AND HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
        {0} 
) AS tbl_BH 
";

        const string Query_EmpBonusForPosting = @"
Select HRMS_Set_Employee.PersonID, HRMS_Set_Employee.StatusID,HRMS_Employement_Status.Description as EmployeeStatusName ,HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.BonusAmount,
Round(HRMS_BonusDetail.InComeTax, 0) as InComeTax,
Round(HRMS_BonusDetail.ArrearOfBonus, 0) as ArrearOfBonus,
Round(HRMS_BonusDetail.DeductionOfBonus ,0) as DeductionOfBonus,
Round(HRMS_BonusDetail.AdvanceSalary,0) as AdvanceSalary,
Round(HRMS_BonusDetail.NetBonusAmount,0) as NetBonusAmount,
HRMS_BonusDetail.BonusDetailID,HRMS_BonusDetail.BonusHeaderID, HRMS_BonusHeader.BonusNo 
,HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate
from HRMS_BonusDetail
	inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID= HRMS_BonusDetail.BonusHeaderID
	And HRMS_BonusHeader .Status=1
	inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID=HRMS_BonusDetail.EmployeeID
	inner JOin HRMS_Employement_Status on HRMS_Employement_Status.StatusID=  HRMS_Set_Employee.StatusID 
WHERE HRMS_BonusDetail.[Status] = 1 
And (HRMS_BonusDetail.IsPosted is null OR HRMS_BonusDetail.IsPosted = 0 )
AND  HRMS_BonusHeader.MillStartDate >= @MillStartDate
 and  HRMS_BonusHeader.CurrentManulDate <= @ManualDate
";
        const string Query_EmpBonusForPosting2 = @"SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,
HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        Round(BDetail.NetBonusAmount,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail

		Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 

        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
        And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
        union all 
        SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLcodecombination as GLAccountCode,
        PT.GLImpactTwo, 
                Round(BDetail.NetBonusAmount,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail
        Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'Bonus' and Status=1)

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1

		        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
			  union All 
--------Income Tax-------------
			  SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        Round(BDetail.InComeTax,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail

		Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1  And 
		PayrollTypeCode= 2) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 

        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
        And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
        union all 
        SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLcodecombination as GLAccountCode,
        PT.GLImpactTwo, 
                Round(BDetail.InComeTax,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail
        Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2)

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1

		        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
order By  GLImpact";

        const string Query_EmpBonusForPostingFOR_GL = @"Select Tab.CompanyID, Tab.PeriodID,  
Tab.GLAccountCode, 	
ABS(Tab.GLAmount) as GLAmount, 
Case When (Tab.GLAmount) > 0 then 'Credit' else 'Debit' end as GLImpact
,Tab.BonusHeaderID,  Tab.CostCenterID from( 
Select FintabfINAL.CompanyID, FintabfINAL.PeriodID,  
FintabfINAL.GLAccountCode, 
Sum(FintabfINAL.GLAmount) as GLAmount
,FintabFinal.BonusHeaderID, FintabFinal.CostCenterID
 from (

Select Fintab.CompanyID, Fintab.PeriodID,  
Fintab.GLAccountCode, Fintab.GLImpact, 
cASE  when Fintab.GLImpact = 'Debit' then Sum(Fintab.GLAmount) * (-1) else Sum(Fintab.GLAmount) end as  GLAmount 
, Fintab.BonusHeaderID,  Fintab.CostCenterID
from (
SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,
HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        Round(BDetail.NetBonusAmount,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail

		Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 

        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
        And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
{0}

        union all 
        SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLcodecombination as GLAccountCode,
        PT.GLImpactTwo, 
                Round(BDetail.NetBonusAmount,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail
        Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'Bonus' and Status=1)

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'Bonus' and Status=1) 
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1

		        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
{0}

			  union All 
--------Income Tax-------------
			  SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        PT.GLCodeCombinationID as GLAccountCode,
        PT.GLImpact, 
        Round(BDetail.InComeTax,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail

		Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1  And 
		PayrollTypeCode= 2) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 

        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
        And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
{0}

        union all 
        SELECT BDetail.CompanyID,BDetail.PeriodID,BDetail.BonusDetailID,
BDetail.BonusHeaderID,HRMS_BonusHeader.BonusNO,
HRMS_BonusHeader.MillStartDate,HRMS_BonusHeader.CurrentManulDate,
		PT.PayrollTypeID,
        BDetail.EmployeeID as PersonID,
        PT.PayrollTypeCode,
		PT.ChartOfAccountID as GLAccountID,
        ARC.CustomerAccountCCID as GLEmpAccountCode,
        HRMS_PayRollTypeDetail.GLcodecombination as GLAccountCode,
        PT.GLImpactTwo, 
                Round(BDetail.InComeTax,0) as GLAmount,ARC.CustomerName,EMP.CostCenterID
        FROM HRMS_BonusDetail BDetail
        Inner Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID = BDetail.BonusHeaderID
		AND HRMS_BonusHeader.CompanyID = BDetail.CompanyID
		AND HRMS_BonusHeader.PeriodID = BDetail.PeriodID
		AND HRMS_BonusHeader.Status=1

        INNER JOIN HRMS_Set_Employee EMP on EMP.PersonID=BDetail.EmployeeID
        left JOIN AR_SET_Customer ARC on ARC.CustomerID=EMP.FK_CustomerID 

        INNER JOIN [HRMS_PayrollType] PT on PT.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 
		and PT.PayrollTypeCode=(Select PayrollTypeCode from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2)

		Inner Join HRMS_PayRollTypeDetail on HRMS_PayRollTypeDetail.PayrollTypeID=(Select PayrollTypeID from HRMS_PayrollType Where Description like 'income%tax-Bonus' and Status=1 And PayrollTypeCode= 2) 
		And HRMS_PayRollTypeDetail.EmployeeStatusCode=EMP.StatusID
		And HRMS_PayRollTypeDetail.ExpenseTypeID=EMP.ExpenseTypeID
		And HRMS_PayRollTypeDetail.CompanyID=PT.CompanyID
		And HRMS_PayRollTypeDetail.Status=1

		        WHERE BDetail.[Status] = 1 
		And (HRMS_BonusHeader.MillStartDate >= @MillStartDate)
		And  (HRMS_BonusHeader.CurrentManulDate <= @ManualCurrentDate)
		      And (BDetail.IsPosted is null OR BDetail.IsPosted = 0 )
And (EMP.StatusID = @StatusID OR  @StatusID is null)
And (HRMS_BonusHeader.BonusNo = @BonusNo or  @BonusNo is null)
{0}

) Fintab
Group By 
Fintab.CompanyID, Fintab.PeriodID,  
Fintab.GLAccountCode, Fintab.GLImpact,
Fintab.BonusHeaderID, Fintab.CostCenterID

) FintabFinal

gROUP bY 
FintabFinal.CompanyID, FintabFinal.PeriodID,  
FintabFinal.GLAccountCode,
FintabFinal.BonusHeaderID, FintabFinal.CostCenterID
) TAB	
";
        const string Query_BonusForPosting = @"Select HRMS_Set_Employee.StatusID,HRMS_Set_Employee.EmployeeCode,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.* ,HRMS_PayrollType.Description as BonusType, HRMS_PayrollType.PayrollTypeID,
HRMS_BonusHeader.MillStartDate, HRMS_BonusHeader.CurrentManulDate
,(select top 1 FK_CodeCombinationID from AR_SET_BusinessPartnerTypes
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode
 where AR_SET_BusinessPartnerTypes.BusinessPartnerID = HRMS_Set_Employee.FK_CustomerID 
 and AR_SET_BusinessPartner.BusinessPartnerName = 'Bonus' ) BonusCode
from HRMS_BonusDetail
		inner Join HRMS_Set_Employee on HRMS_Set_Employee.PersonID=HRMS_BonusDetail.EmployeeID
        AND HRMS_Set_Employee.CompanyID = HRMS_BonusDetail.CompanyID
		AND HRMS_Set_Employee.Status = 1
		Inner JOin HRMS_BonusHeader on HRMS_BonusHeader.BonusID = HRMS_BonusDetail.BonusHeaderID
        AND HRMS_BonusHeader.CompanyID = HRMS_BonusDetail.CompanyID
		AND HRMS_BonusHeader.Status = 1
		Inner Join HRMS_PayrollType on HRMS_PayrollType.Description = 'Bonus'
		and HRMS_PayrollType.Status=1
WHERE HRMS_BonusDetail.[Status] = 1 
AND HRMS_BonusHeader.CompanyID =@CompanyID 
AND HRMS_BonusHeader.PeriodID =@PeriodID
And (HRMS_BonusDetail.IsPosted is null OR HRMS_BonusDetail.IsPosted = 0 )
And HRMS_BonusHeader.MillStartDate >= @MillStartDate
And HRMS_BonusHeader.CurrentManulDate <= @CurrentManulDate";
        const string Query_SelectAll_SearchingWithDetail_WithPaging = @" ";
        #endregion

        #region Class Method

        public bool InsertBonusHeaderAndDetail(List<HRMS_BonusDetail> colBODetail, int CurrentFiscalID = 0, List<HRMS_BonusLoanDetail> colLoanDetail = null, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertEmployeeBonus");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {

                this.Insert(sqltrans);

                foreach (HRMS_BonusDetail bod in colBODetail)
                {
                    if (bod.Balance < 0)
                        continue;
                    bod.BonusDetailID = int.Parse(HRMS_BonusDetail.GetNewBonusDetailID(this.BonusID, this.CompanyID, this.PeriodID, sqltrans).ToString());
                    int TempBonusDetailID = bod.BonusDetailID;
                    bod.Insert(sqltrans);
                }

                if (colLoanDetail != null)
                {
                    foreach (HRMS_BonusLoanDetail lon in colLoanDetail)
                    {
                        //lon.BonusLoanDetailID = int.Parse(HRMS_BonusLoanDetail.GetNewBonusLoanDetailID(this.BonusID, this.CompanyID, this.PeriodID, sqltrans).ToString());

                        lon.Insert(sqltrans);
                    }
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
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertEmployeeBonus");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public bool UpdateBonusWithDetails(List<HRMS_BonusDetail> colbonusDetailAdd, List<HRMS_BonusDetail> colBonusDetailUpdate, List<HRMS_BonusDetail> colBonusDetailDel,List<HRMS_BonusLoanDetail> colLoanDetailUpdate)
        {

            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateBonus");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (HRMS_BonusDetail colSDetailAdd in colbonusDetailAdd)
                {
                    if (colSDetailAdd.Balance < 0)
                        continue;
                    colSDetailAdd.BonusDetailID = int.Parse(HRMS_BonusDetail.GetNewBonusDetailID(BonusID,CompanyID,PeriodID, sqltrans).ToString()); //CompanyID, PeriodID
                    colSDetailAdd.Insert(sqltrans);
                }

                foreach (HRMS_BonusDetail colSDetailEdit in colBonusDetailUpdate)
                {
                    if (colSDetailEdit.Balance < 0)
                        continue;
                    colSDetailEdit.UpdateDataByKey(sqltrans);
                }

                foreach (HRMS_BonusDetail colSDetailDelete in colBonusDetailDel)
                {
                    HRMS_BonusDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, colSDetailDelete.BonusDetailID,BonusID,CompanyID,PeriodID);// f_CompanyID, f_PeriodID,
                }

                foreach (HRMS_BonusLoanDetail coloandetailEdit in colLoanDetailUpdate)
                {
                    coloandetailEdit.UpdateDataByKey(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateBonus");
            }

            return result;

        }

        public static DataTable GetAllDataWithOtherTablesWithDetail(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
    bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
    Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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
                    StringBuilder query = new StringBuilder(Query_SelectAll_SearchingWithDetail_WithPaging.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_OrderHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_OrderHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_OrderDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(FnI.QueryResourceLibrary.Sales.SAL_OrderHeader.Query_SelectAll_SearchingWithDetail.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_OrderHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_OrderHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_OrderDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
    bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
    Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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
                    StringBuilder query = new StringBuilder(Query_SelectAll_Searching_WithPaging);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(query.ToString(),strWhere)
                         + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), string.Format(query.ToString(), strWhere)
                         + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(Query_SelectAll);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForSanghar(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
    bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
    Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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
                    StringBuilder query = new StringBuilder(Query_SelectAll_Searching_WithPagingSanghar);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(query.ToString(), strWhere)
                         + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), string.Format(query.ToString(), strWhere)
                         + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(Query_SelectAll);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllDetailDataByHeaderID(Nullable<int> companyID, Nullable<int> periodID, int BonusHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[3] = new SqlParameter("@BonusHeaderID", BonusHeaderID);

                DataSet ds = null;

                #region
                const string Query_DetailDataByHeaderID = @"
Select HRMS_BonusDetail.BonusHeaderID,HRMS_BonusDetail.EmployeeID,HRMS_BonusDetail.BonusDetailID,HRMS_BonusDetail.CompanyID
,HRMS_BonusDetail.PeriodID,GrossSalary,HRMS_BonusDetail.BasicSalary,HRMS_BonusDetail.BonusAmount as Bonus,HRMS_BonusDetail.NoOfDays as Days,HRMS_BonusDetail.BonusAbsent
,HRMS_BonusDetail.CreateDate
,HRMS_BonusDetail.CreatedBy
,HRMS_BonusDetail.UpdateDate
,HRMS_BonusDetail.UpdatedBy
,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.JoiningDate
,HRMS_BonusDetail.DesignationID
,SET_JobDesignation.JobDesignationName as DesignationName
, SET_Department.DepartmentID
,SET_Department.DepartmentName
,HRMS_BonusDetail.Eligibility
,HRMS_BonusDetail.InComeTax,
HRMS_BonusDetail.NetBonusAmount,
HRMS_Employement_Status.Description as EmployeeStatusName
,HRMS_BonusDetail.ArrearOfBonus,
HRMS_BonusDetail.DeductionOfBonus
,HRMS_BonusDetail.AdvanceSalary
,HRMS_BonusDetail.LoanAmount
,HRMS_BonusDetail.Balance
,HRMS_BonusDetail.LoanDeduction
,HRMS_BonusDetail.IsPosted
,CAST( isnull(HRMS_BonusDetail.IsPosted,0) as int) as IsPostedBonus
,HRMS_Set_Employee.EmployeeCode
,[HRMS_PayrollType].Description as LoanType
,[HRMS_PayrollType].PayrollTypeID 
,[HRMS_PayrollType].PayrollTypeID as LoanTypeCode
,isnull(HRMS_BonusDetail.NoOfBonus,1) as NoOfBonus
, 0 as Active
,'Update' Action
,(select case when CalculatedAmount > 0 then CalculatedAmount else ManualAmount end as Value
                                    from HRMS_EmployeePayroll
                                    where PersonID = HRMS_BonusDetail.EmployeeID and CompanyID = HRMS_BonusDetail.CompanyID and PeriodID = HRMS_BonusDetail.PeriodID 
									and Status = 1 and PayrollTypeID = 2) as ActualBonus
FROM HRMS_BonusDetail  
Left Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID =   HRMS_BonusDetail.BonusHeaderID
And HRMS_BonusHeader.CompanyID =   HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID =   HRMS_BonusDetail.PeriodID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
and HRMS_Set_Employee.CompanyID = HRMS_BonusDetail.CompanyID
left join [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = HRMS_BonusDetail.PayrollTypeID
   and [HRMS_PayrollType].Status= 1
Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_BonusDetail.DesignationID
AND SET_JobDesignation.Status=1
Inner join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
AND SET_Department.Status=1
inner Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_Set_Employee.StatusID
			And HRMS_Employement_Status.Status=1
Where HRMS_BonusDetail.Status=1
AND HRMS_BonusDetail.CompanyID=@CompanyID
AND HRMS_BonusDetail.PeriodID=@PeriodID
AND HRMS_BonusDetail.BonusHeaderID=@BonusHeaderID";
                
             #endregion

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DetailDataByHeaderID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DetailDataByHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DetailDataByHeaderID, sqlparams);

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

        public static DataTable GetAllDetailDataByHeaderIDForSanghar(Nullable<int> companyID, Nullable<int> periodID, int BonusHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[3] = new SqlParameter("@BonusHeaderID", BonusHeaderID);

                DataSet ds = null;

                #region
                const string Query_DetailDataByHeaderIDForSanghar = @"
Select HRMS_BonusDetail.BonusHeaderID,HRMS_BonusDetail.EmployeeID,HRMS_BonusDetail.BonusDetailID,HRMS_BonusDetail.CompanyID
,HRMS_BonusDetail.PeriodID,GrossSalary,HRMS_BonusDetail.BasicSalary,HRMS_BonusDetail.BonusAmount as Bonus,HRMS_BonusDetail.NoOfDays as Days,HRMS_BonusDetail.BonusAbsent
,HRMS_BonusDetail.CreateDate
,HRMS_BonusDetail.CreatedBy
,HRMS_BonusDetail.UpdateDate
,HRMS_BonusDetail.UpdatedBy
,HRMS_Set_Employee.FullName as EmployeeName
,HRMS_BonusDetail.JoiningDate
,HRMS_BonusDetail.DesignationID
,SET_JobDesignation.JobDesignationName as DesignationName
, SET_Department.DepartmentID
,SET_Department.DepartmentName
,HRMS_BonusDetail.Eligibility
,HRMS_BonusDetail.InComeTax,
HRMS_BonusDetail.NetBonusAmount,
HRMS_Employement_Status.Description as EmployeeStatusName
,HRMS_BonusDetail.ArrearOfBonus,
HRMS_BonusDetail.DeductionOfBonus
,HRMS_BonusDetail.AdvanceSalary
,HRMS_BonusDetail.LoanAmount
,HRMS_BonusDetail.Balance
,HRMS_BonusDetail.LoanDeduction
,HRMS_BonusDetail.IsPosted
,HRMS_BonusDetail.Rate
,HRMS_BonusHeader.PresentDays
,CAST( isnull(HRMS_BonusDetail.IsPosted,0) as int) as IsPostedBonus
,HRMS_Set_Employee.EmployeeCode
,[HRMS_PayrollType].Description as LoanType
,[HRMS_PayrollType].PayrollTypeID 
,[HRMS_PayrollType].PayrollTypeID as LoanTypeCode
,isnull(HRMS_BonusDetail.NoOfBonus,1) as NoOfBonus
, 0 as Active
,HRMS_BonusDetail.IsIncludeDeductionAmount
, 0 AS DeductionOfBonusTemp
,'Update' Action
,(select case when CalculatedAmount > 0 then CalculatedAmount else ManualAmount end as Value
                                    from HRMS_EmployeePayroll
                                    where PersonID = HRMS_BonusDetail.EmployeeID and CompanyID = HRMS_BonusDetail.CompanyID and PeriodID = HRMS_BonusDetail.PeriodID 
									and Status = 1 and PayrollTypeID = 2) as ActualBonus
FROM HRMS_BonusDetail  
Left Join HRMS_BonusHeader on HRMS_BonusHeader.BonusID =   HRMS_BonusDetail.BonusHeaderID
And HRMS_BonusHeader.CompanyID =   HRMS_BonusDetail.CompanyID
And HRMS_BonusHeader.PeriodID =   HRMS_BonusDetail.PeriodID
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_BonusDetail.EmployeeID
and HRMS_Set_Employee.CompanyID = HRMS_BonusDetail.CompanyID
left join [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = HRMS_BonusDetail.PayrollTypeID
   and [HRMS_PayrollType].Status= 1
Inner join SET_JobDesignation on SET_JobDesignation.JobDesignationID = HRMS_BonusDetail.DesignationID
AND SET_JobDesignation.Status=1
Inner join SET_Department on SET_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
AND SET_Department.Status=1
inner Join HRMS_Employement_Status on HRMS_Employement_Status.StatusID =HRMS_Set_Employee.StatusID
			And HRMS_Employement_Status.Status=1
Where HRMS_BonusDetail.Status=1
AND HRMS_BonusDetail.CompanyID=@CompanyID
AND HRMS_BonusDetail.PeriodID=@PeriodID
AND HRMS_BonusDetail.BonusHeaderID=@BonusHeaderID";

                #endregion

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DetailDataByHeaderIDForSanghar, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DetailDataByHeaderIDForSanghar, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DetailDataByHeaderIDForSanghar, sqlparams);

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

        public static DataTable GetBonusDataFor_Post(DateTime MillStartDate, DateTime ManualDate, Nullable<int> Bonusno, Nullable<int> EmployeeStatusID = null, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[1] = new SqlParameter("@ManualDate", ManualDate);
                if (Bonusno.HasValue)
                {
                    sb.Append(" And HRMS_BonusHeader.BonusNo = " + Bonusno + " ");
                }

                DataSet ds = null;

                if (EmployeeStatusID.HasValue)
                {
                    sb.Append(" And HRMS_Set_Employee.StatusID = " + EmployeeStatusID + " ");
                }


                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_EmpBonusForPosting + sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmpBonusForPosting + sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmpBonusForPosting + sb), sqlparams);

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


        public static DataTable BonusDataFor_Posting(DateTime MillStartDate, DateTime ManualDate, Nullable<int> Bonusno, Nullable<int> EmployeeStatusID = null, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[1] = new SqlParameter("@ManualCurrentDate", ManualDate);


                if (Bonusno.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BonusNo", Bonusno);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@BonusNo", DBNull.Value);
                }
                if (EmployeeStatusID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@StatusID", EmployeeStatusID);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@StatusID", DBNull.Value);
                }
                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_EmpBonusForPosting2), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmpBonusForPosting2), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmpBonusForPosting2), sqlparams);

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

        public static DataTable BonusDataFor_PostingFor_GL(DateTime MillStartDate, DateTime ManualDate, Nullable<int> Bonusno, Nullable<int> EmployeeStatusID = null, string PersonID = null, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[1] = new SqlParameter("@ManualCurrentDate", ManualDate);


                if (Bonusno.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@BonusNo", Bonusno);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@BonusNo", DBNull.Value);
                }
                if (EmployeeStatusID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@StatusID", EmployeeStatusID);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID != string.Empty)
                {
                    sb.Append("And EMP.PersonID in (" + PersonID + ")");
                }
                DataSet ds = null;
                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_EmpBonusForPostingFOR_GL, sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_EmpBonusForPostingFOR_GL, sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_EmpBonusForPostingFOR_GL, sb), sqlparams);

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


        public static DataTable Get_DIInvoiceBonusDocument(int companyID, int periodID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DocumentTypeDirectInvoiceBonus, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertIntoBonusPostingHeaderandDetailWithINvoiceandGL(List<HRMS_BonusDetail> colBonusDetupdate, AR_InvoiceHeader objInvoiceHeader, List<AR_InvoiceSupplierDetail> colEmpInvoiceDetail, GL_TransactionHeader objTranHead, List<GL_TransactionDetail> colTranDetAddMulti, SqlTransaction sqltran)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sqltran == null)
            {
                sqltrans = SqlHelper.GetTransaction("HRMS_EmployeeBonusPosting");
            }
            else
            {
                sqltrans = sqltran;
            }
            try
            {

                //int InvoiceheaderID = AR_InvoiceHeader.GetNewInvoiceID(this.CompanyID, this.PeriodID);

                if (objInvoiceHeader.Insert())
                {
                    short invoiceSupplierID = Convert.ToInt16(AR_InvoiceSupplierDetail.GetNewInvoiceSupplierID(f_CompanyID, objInvoiceHeader.InvoiceID, f_PeriodID, sqltrans));

                    foreach (AR_InvoiceSupplierDetail invoiceSupplier in colEmpInvoiceDetail)
                    {
                        invoiceSupplier.InvoiceSupplierID = invoiceSupplierID++;
                        invoiceSupplier.InvoiceHeaderID = objInvoiceHeader.InvoiceID;
                        invoiceSupplier.FiscalID = this.FiscalID;
                        invoiceSupplier.Insert(sqltrans);
                    }
                    objTranHead.Insert();

                    foreach (GL_TransactionDetail GLD in colTranDetAddMulti)
                    {
                        GLD.TransactionID = objTranHead.TransactionID;
                        GLD.Insert();
                    }

                    foreach (HRMS_BonusDetail objBonusDetail in colBonusDetupdate)
                    {
                        objBonusDetail.UpdateDataByKey(sqltrans);
                    }
                    result = true;
                }
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
                {
                    if (sqltrans == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sqltrans == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "HRMS_EmployeeBonusPosting");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public static DataTable GetDataHeaderForPost(int companyID , int PeriodID, Nullable<DateTime> CurrentManulDate, Nullable<DateTime> MillStartDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null, Nullable<int> BonusTypeID = null, Nullable<int> StatusCode = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[3] = new SqlParameter("@CurrentManulDate", CurrentManulDate);

                DataSet ds = null;

                if (StatusCode.HasValue)
                {
                    sb.Append(" And HRMS_BonusHeader.EmployStatusID = " + StatusCode + " ");
                }

                if (BonusTypeID.HasValue)
                {
                    sb.Append(" AND HRMS_PayrollType.PayrollTypeID = " + BonusTypeID + " ");
                }

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_BonusForPosting + sb), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BonusForPosting + sb), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_BonusForPosting + sb), sqlparams);

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

        public static DataTable Get_DIInvoiceBonusPostingDocument(int companyID, int periodID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_DocumentTypeDirectInvoiceBonusposting, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataHeaderDetailForBonusPost(int companyID, int periodID, string BonusID, Nullable<DateTime> MillCurrentDate, Nullable<DateTime> MillStartDate, string BonusDetailIDs, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@MillStartDate", MillStartDate);
                sqlparams[3] = new SqlParameter("@CurrentManulDate", MillCurrentDate);
                sqlparams[4] = new SqlParameter("@BonusDetailID", BonusDetailIDs);
                sqlparams[5] = new SqlParameter("@BonusID", BonusID);
                DataSet ds = null;

                if (BonusID != "")
                    sb.Append(" AND HRMS_BonusHeader.BonusID in (" + BonusID + ")");


                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_DataHeaderDetailForBonusPost,sb.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DataHeaderDetailForBonusPost, sb.ToString()), sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_DataHeaderDetailForBonusPost, sb.ToString()), sqlparams);


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


        public bool DeleteBonusHederAndDetail(List<HRMS_BonusDetail> colDeleteStockTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteStockTransaction");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, f_BonusID);
                HRMS_BonusHeader.DeleteDataByHeaderKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_BonusID);
                HRMS_BonusDetail.DeleteDataByHeaderKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_BonusID);
                HRMS_BonusDetail.DeleteLoanDetailDataByHeaderKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_BonusID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteStockTransaction");
            }
            return result;
        }

        public static bool DeleteDataByHeaderKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int BonuID)
        {
            bool result = false;

            try
            {

                const string Query_Delete_ByHeaderKey = @" Update [HRMS_BonusHeader] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [BonusID] = @BonuID ";


                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@BonuID", BonuID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderKey, sqlparams) > 0)
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
    
        public static int GetNewDocumentNo(int companyID, int periodID, int fiscalID, int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDocNo = @" Select ISNULL(max(BonusNo),0)+1 DocumentNo from HRMS_BonusHeader 
where CompanyID = @CompanyID and PeriodID= @PeriodID and FormulaID = @DocumentTypeID and FiscalID = @FiscalID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@FiscalID", fiscalID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxDocNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }
        #endregion
    }
}
