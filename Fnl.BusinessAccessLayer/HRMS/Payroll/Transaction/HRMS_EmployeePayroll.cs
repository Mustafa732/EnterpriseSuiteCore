using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_EmployeePayroll
    {
        #region Constant Strings

        const string Query_Update_By_PrimaryKey_ForIncrement = @" UPDATE [HRMS_EmployeePayroll] SET [PersonID] = @PersonID, [PayrollTypeID] = @PayrollTypeID, [PayrollTypeCode] = @PayrollTypeCode, [CalculatedAmount] = @CalculatedAmount,
 [ManualAmount] = @ManualAmount, [Formula] = @Formula, [Status] = @Status,
  [IsActive] = @IsActive, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
		WHERE [CompanyID] = @CompanyID and [EmployeePayrollID] = @EmployeePayrollID and [PeriodID] = @PeriodID ";

        const string Query_GetManualTaxAmount = @"     SELECT * FROM [HRMS_EmployeePayroll]
	inner JOIN HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID
	AND HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode 
	AND HRMS_PayrollType.Status = 1 AND HRMS_PayrollType.IsActive = 1  
	WHERE [PersonID] = @PersonID AND [CompanyID] = @CompanyID 
    AND HRMS_PayrollType.IsIncomeTax = 1 
And HRMS_PayrollType.Description Like '%income%tax%'
   AND HRMS_EmployeePayroll.[IsActive] = 1 AND HRMS_EmployeePayroll.[Status] = 1
";
        const string Query_GetManualTaxAmountTemp = @" SELECT * FROM [HRMS_EmployeePayroll] WHERE [PersonID] = @PersonID AND [CompanyID] = @CompanyID AND [PayrollTypeID] = 5 AND [IsActive] = 1 AND [Status] = 1";

        const string Query_InsertAutoID = @" INSERT INTO [HRMS_EmployeePayroll] ([CompanyID], [PeriodID], [EmployeePayrollID], [PersonID], [PayrollTypeID], [PayrollTypeCode], [CalculatedAmount], [ManualAmount], [Remarks], [Formula], [LeaveDays], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [IsOneTime], [IsPaid], [Quantity], [UnitOfMeasure])
		VALUES(@CompanyID, @PeriodID, @EmployeePayrollID, @PersonID, @PayrollTypeID, @PayrollTypeCode, @CalculatedAmount, @ManualAmount, @Remarks, @Formula, @LeaveDays, @Status, @IsActive, @CreatedBy, @CreateDate, @CreatedByValue, @IsOneTime, @IsPaid, @Quantity, @UnitOfMeasure)  ";


        const string Query_EmployePayrollSeach_WithPaging = @"
        Select ResultSet.PersonID,ResultSet.FullName, ResultSet.CNICNumber, ResultSet.Fk_JobDesignationID,ResultSet.[Status],Row_number() over(Order by ResultSet.PersonID) as 'Row' from (
        Select Distinct HRMS_EmployeePayroll.PersonID, HRMS_Set_Employee.FullName, HRMS_Set_Employee.CNICNumber,
        HRMS_Set_Employee.Fk_JobDesignationID, [HRMS_EmployeePayroll].[Status]
        FROM [HRMS_EmployeePayroll]  
        JOIN HRMS_Set_Employee On HRMS_Set_Employee.PersonID = HRMS_EmployeePayroll.PersonID
	        And HRMS_Set_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
        Where [HRMS_EmployeePayroll].[Status] = 1 and [HRMS_EmployeePayroll].[IsOneTime] is not null
            --AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
            --AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
            --AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
         ) as ResultSet
             where ResultSet.[Status]=1
        ";

        const string Query_EmployePayrollSeach_WithOutPaging = @"
        Select Distinct HRMS_EmployeePayroll.PersonID, HRMS_Set_Employee.FullName
        FROM [HRMS_EmployeePayroll]  
        JOIN HRMS_Set_Employee On HRMS_Set_Employee.PersonID = HRMS_EmployeePayroll.PersonID
	        And HRMS_Set_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
        Where [HRMS_EmployeePayroll].[Status] = 1 and [HRMS_EmployeePayroll].[IsOneTime] is not null
           -- AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
           -- AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
           -- AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
        ";


        const string Query_EmployePayrollSearch_WithPaging = @"
       Select ResultSet.PersonID,ResultSet.FullName,ResultSet.CNICNumber,ResultSet.Fk_JobDesignationID,ResultSet.[Status],Row_number() over(Order by ResultSet.PersonID) as 'Row' from (
        Select Distinct HRMS_EmployeePayroll.PersonID, HRMS_Set_Employee.FullName,HRMS_Set_Employee.CNICNumber,
        HRMS_Set_Employee.Fk_JobDesignationID,[HRMS_EmployeePayroll].[Status],HRMS_EmployeePayroll.CompanyID,HRMS_EmployeeInEmployement_Status.UserID
        FROM [HRMS_EmployeePayroll]  
        JOIN HRMS_Set_Employee On HRMS_Set_Employee.PersonID = HRMS_EmployeePayroll.PersonID
	        And HRMS_Set_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
        Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
        Where [HRMS_EmployeePayroll].[Status] = 1 and [HRMS_EmployeePayroll].[IsOneTime] is null
            --AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
            --AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
            --AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
         ) as ResultSet
        where ResultSet.[Status]=1
        ";

        const string Query_EmployePayrollSearch_WithOutPaging = @"
        Select Distinct HRMS_EmployeePayroll.PersonID, HRMS_Set_Employee.FullName,HRMS_EmployeeInEmployement_Status.UserID
        FROM [HRMS_EmployeePayroll]  
        JOIN HRMS_Set_Employee On HRMS_Set_Employee.PersonID = HRMS_EmployeePayroll.PersonID
	        And HRMS_Set_Employee.CompanyID = HRMS_EmployeePayroll.CompanyID
        Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
           and HRMS_EmployeeInEmployement_Status.[CompanyID] = HRMS_Set_Employee.[CompanyID]
        Where [HRMS_EmployeePayroll].[Status] = 1 and [HRMS_EmployeePayroll].[IsOneTime] is null
           -- AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
           -- AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
           -- AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
        ";

        const string Query_GetPayrollOnetimeByPersonID = @"
        Select [HRMS_PayrollType].Description,[HRMS_PayrollType].Code Code,[HRMS_EmployeePayroll].*,'' as Action from [HRMS_EmployeePayroll]
        JOIN [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]
        Where [HRMS_EmployeePayroll].[Status] = 1
            AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
            AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
            AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
            AND [HRMS_EmployeePayroll].[PersonID] = @PersonID
 AND [HRMS_EmployeePayroll].IsOneTime is not null
        ";
        

        const string Query_GetPayrollByPersonID = @"
        Select [HRMS_PayrollType].Description,[HRMS_PayrollType].Code Code,Round([HRMS_EmployeePayroll].CalculatedAmount,2)  as CalculatedAmount ,[HRMS_EmployeePayroll].*,'' as Action from [HRMS_EmployeePayroll]
        JOIN [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]
        Where [HRMS_EmployeePayroll].[Status] = 1
            AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
            AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
            AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
            AND [HRMS_EmployeePayroll].[PersonID] = @PersonID
             AND [HRMS_EmployeePayroll].IsOneTime is null
        ";

        const string Query_GetData_byPersonID = @" SELECT * FROM [HRMS_EmployeePayroll] WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID and [PeriodID] = @PeriodID ";


        const string Query_GetPayrollByPersonID_ForIncrement = @"
      Select -1 as IncrementID, [HRMS_PayrollType].Description,[HRMS_PayrollType].Code Code,Round([HRMS_EmployeePayroll].CalculatedAmount,2) 
  as PreviousSalaryCalculated,Round([HRMS_EmployeePayroll].ManualAmount,2) as PreviousSalaryManual,Round([HRMS_EmployeePayroll].CalculatedAmount,2) as NewSalaryCalculated, Round([HRMS_EmployeePayroll].ManualAmount,2) as NewSalaryManual,
  0 as Increment,0 as Arrear, [HRMS_EmployeePayroll].*,'Add' as Action,
  (case when HRMS_PayrollType.PayrollTypeCode = 1 then 'Allowance'
  when HRMS_PayrollType.PayrollTypeCode = 2 then 'Deduction'
  when HRMS_PayrollType.PayrollTypeCode = 3 then 'Contribution' else '' end) as Type,HRMS_EmployeePayroll.Formula as PreviousFormula from [HRMS_EmployeePayroll]

       -- JOIN [HRMS_PayrollType] on [HRMS_PayrollType].PayrollTypeID = [HRMS_EmployeePayroll].[PayrollTypeID]

		
        JOIN HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID
                AND HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode 
                AND HRMS_PayrollType.Status = 1 AND HRMS_PayrollType.IsActive = 1  

        Where [HRMS_EmployeePayroll].[Status] = 1
            AND ([HRMS_EmployeePayroll].[IsActive] = @IsActive OR @IsActive is null)
            AND ([HRMS_EmployeePayroll].[CompanyID] = @CompanyID OR @CompanyID is null) 
            AND ([HRMS_EmployeePayroll].[PeriodID] = @PeriodID OR @PeriodID is null) 
            AND [HRMS_EmployeePayroll].[PersonID] = @PersonID
             AND [HRMS_EmployeePayroll].IsOneTime is null
            and [HRMS_EmployeePayroll].payrolltypecode in (1,2,3)

        ";


        const string Query_UpdateAnnualLeavesByPersonID = @"

update HRMS_EmployeePayroll set LeaveDays = @LeaveDays
where HRMS_EmployeePayroll.PayrollTypeCode = @PayrollTypeCode
and HRMS_EmployeePayroll.PayrollTypeID = @PayrollTypeID
and HRMS_EmployeePayroll.PersonID = @PersonID
and HRMS_EmployeePayroll.CompanyID = @CompanyID
and HRMS_EmployeePayroll.[Status] = 1
and HRMS_EmployeePayroll.FiscalID = @CurrentFiscalID
";


        const string Query_GetEmployeeGradeID = @"

 select FK_GradeID from  HRMS_SET_Employee
    where HRMS_SET_Employee.Status = 1
    and PersonID = @PersonID
";

        const string Query_GetdataByGradeID = @"

 select -1 as EmployeePayrollID,0 as Quantity,0 as LeaveDays,0 as UnitOfMeasure,'Add' as Action,HRMS_PayrollType.Description,HRMS_PayrollType.Code,HRMS_GradeWiseEmolumentDetail.PayrollTypeID,
 HRMS_GradeWiseEmolumentDetail.PayrollTypeCode,
 Amount as CalculatedAmount,0 as ManualAmount,isnull(HRMS_GradeWiseEmolumentDetail.Formula,0) as Formula
  from HRMS_GradeWiseEmolumentHeader
  inner join HRMS_GradeWiseEmolumentDetail on
  HRMS_GradeWiseEmolumentDetail.GradeWiseEmolumentHeaderID=HRMS_GradeWiseEmolumentHeader.GradeWiseEmolumentHeaderID
  and HRMS_GradeWiseEmolumentDetail.Status = 1
  inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_GradeWiseEmolumentDetail.PayrollTypeID
  and HRMS_PayrollType.PayrollTypeCode = HRMS_GradeWiseEmolumentDetail.PayrollTypeCode
  and HRMS_PayrollType.Status = 1 
  where HRMS_GradeWiseEmolumentHeader.GradeID = @GradeID
  and HRMS_GradeWiseEmolumentHeader.CompanyID = @CompanyID
  and HRMS_GradeWiseEmolumentHeader.PeriodID = @PeriodID
  and HRMS_GradeWiseEmolumentHeader.Status = 1
";

        const string Query_GetAllActiveDataWithImpact = @"    
        ";

        const string Query_GetAllLeavesObtained = @"
       
        ";
        const string Query_UpdateCalculatedAmountByPersonIDUsingFormula = @"

update HRMS_EmployeePayroll set CalculatedAmount = @CalculatedAmount
where HRMS_EmployeePayroll.PayrollTypeCode = @PayrollTypeCode
and HRMS_EmployeePayroll.PayrollTypeID = @PayrollTypeID
and HRMS_EmployeePayroll.PersonID = @PersonID
and HRMS_EmployeePayroll.CompanyID = @CompanyID
and HRMS_EmployeePayroll.[Status] = 1

";

//        const string Query_GetNotEntitledLeaveByPersonID = @"
//SELECT distinct
//HRMS_PayrollType.Description,
//HRMS_EmployeePayroll.PayrollTypeID,
//HRMS_EmployeePayroll.LeaveDays,
//HRMS_EmployeePayroll.PersonID
//
//FROM HRMS_PayrollType 
//
//left join HRMS_EmployeePayroll ON  
//HRMS_PayrollType.PayrollTypeID  =  HRMS_EmployeePayroll.PayrollTypeID and
//HRMS_PayrollType.CompanyID  =  HRMS_EmployeePayroll.CompanyID and
//HRMS_EmployeePayroll.CompanyID = @CompanyID and
//HRMS_EmployeePayroll.PeriodID = @PeriodID and
//HRMS_EmployeePayroll.PersonID = @PersonID
//
//where 
//HRMS_PayrollType.PayrollTypeCode = 4 AND
//HRMS_PayrollType.Status = 1 AND
//HRMS_PayrollType.IsActive = 1 ";
        const string Query_DeleteByPersonID = @"
update HRMS_EmployeePayroll 
set Status = 0 
 , UpdatedBy = @UpdatedBy
 
, UpdateDate = @UpdateDate
  , UpdatedByValue = @UpdatedByValue
where PersonID = @PersonID
and CompanyID = @CompanyID
and PeriodID = @PeriodID

";

        const string Query_DeleteDataByPersonID = @"
Delete from HRMS_EmployeePayroll WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [EmployeePayrollID] = @PersonID 
";

        const string Query_Update_By_PersonIDForPayroll = @" UPDATE [HRMS_EmployeePayroll] SET  [ManualAmount] = @ManualAmount
WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID  and EmployeePayrollID = @EmployeePayrollID;
";

        const string Query_GetNotEntitledLeaveByPersonID = @"
        Select 
        TAB.Description
        ,TAB.PayrollTypeID
        ,TAB.PersonID
        ,sum(TAB.leaveDays) as leaveDays
        ,sum(TAB.Quantity) as Quantity
        --,cast( isnull(sum(TAB.leavedays),0)  - isnull( sum(TAB.Quantity),0) as int) as Balance
     ,cast( (isnull(sum(TAB.leavedays),0)  - isnull( sum(TAB.Quantity),0)) + isnull(sum(TAB.Quantity),0) as int) as Balance
        from 
        (
              select 
              HRMS_PayrollType.Description,
              HRMS_PayrollType.PayrollTypeID,
              HRMS_EmployeeAttendance.PersonID,
              HRMS_EmployeePayroll.leaveDays,
              HRMS_EmployeeAttendance.Quantity as Quantity
 
              from HRMS_PayrollType 

              left join HRMS_EmployeePayroll on
              HRMS_PayrollType.PayrollTypeID = HRMS_EmployeePayroll.PayrollTypeID and
              HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeePayroll.PayrollTypeCode and
              HRMS_EmployeePayroll.PersonID = @PersonID and
              (HRMS_EmployeePayroll.IsOneTime is null or HRMS_EmployeePayroll.IsOneTime=0)  and 
              HRMS_EmployeePayroll.[Status] = 1 and HRMS_EmployeePayroll.IsActive = 1
  
              left join HRMS_EmployeeAttendance on
              HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeAttendance.LeaveID and
              HRMS_EmployeePayroll.CompanyID = HRMS_EmployeeAttendance.CompanyID and
              HRMS_EmployeeAttendance.[Status] = 1 and
              HRMS_EmployeeAttendance.PersonID = @PersonID 
  
              where HRMS_PayrollType.payrolltypecode = 4 and 
              HRMS_PayrollType.[status] = 1 and HRMS_PayrollType.isActive = 1--@IsActive
                AND HRMS_EmployeePayroll.Companyid = @CompanyID
              

              group by  HRMS_PayrollType.Description,HRMS_PayrollType.PayrollTypeID, HRMS_EmployeeAttendance.PersonID,HRMS_EmployeePayroll.leaveDays,HRMS_EmployeeAttendance.Quantity 
        
        ) as TAB
        Group By TAB.Description, TAB.PayrollTypeID, TAB.PersonID
        Order By TAB.PayrollTypeID
        ";

        const string Query_Insertpayroll = @" INSERT INTO [HRMS_EmployeePayroll] ([CompanyID], [PeriodID], [EmployeePayrollID], [PersonID], [PayrollTypeID], [PayrollTypeCode], [CalculatedAmount], [ManualAmount], [Remarks], [Formula], [LeaveDays], [Status], [IsActive], [CreatedBy], [CreateDate], [CreatedByValue], [IsOneTime], [IsPaid], [Quantity], [UnitOfMeasure])
		VALUES(@CompanyID, @PeriodID, @EmployeePayrollID, @PersonID, @PayrollTypeID, @PayrollTypeCode, @CalculatedAmount, @ManualAmount, @Remarks, @Formula, @LeaveDays, @Status, @IsActive, @CreatedBy, @CreateDate, @CreatedByValue, @IsOneTime, @IsPaid, @Quantity, @UnitOfMeasure)  ";

        const string Query_UpdatePayrollStatus = @"
Update HRMS_EmployeePayroll Set Status = 0 WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [PayrollTypeID] = @PayrollTypeID 
{0}
";
        const string Query_UpdatePayrollStatusForPersonID = @"
SELECT HRMS_EmployeePayroll.CompanyID, HRMS_EmployeePayroll.PeriodID,HRMS_EmployeePayroll.EmployeePayrollID
, HRMS_EmployeePayroll.PersonID, HRMS_EmployeePayroll.PayrollTypeID, HRMS_EmployeePayroll.PayrollTypeCode
, HRMS_EmployeePayroll.CalculatedAmount, HRMS_EmployeePayroll.ManualAmount, HRMS_EmployeePayroll.Remarks, HRMS_EmployeePayroll.Formula
, HRMS_EmployeePayroll.LeaveDays, HRMS_EmployeePayroll.Status, HRMS_EmployeePayroll.IsActive, HRMS_EmployeePayroll.CreatedBy,HRMS_EmployeePayroll.CreatedByValue, 
                       HRMS_EmployeePayroll.IsOneTime, HRMS_EmployeePayroll.IsPaid, HRMS_EmployeePayroll.Quantity, HRMS_EmployeePayroll.UnitOfMeasure, HRMS_EmployeePayroll.FiscalID
FROM            HRMS_EmployeePayroll

INNER JOIN HRMS_Set_Employee ON  HRMS_Set_Employee.PersonID=HRMS_EmployeePayroll.PersonID
AND HRMS_Set_Employee.Status=1
Where HRMS_EmployeePayroll.Status=0 AND HRMS_Set_Employee.PersonID=@PersonID AND HRMS_EmployeePayroll.PayrollTypeID=@PayRollTypeID
AND HRMS_EmployeePayroll.PayrollTypeCode=@PayrollTypeCode
";

        const string Query_GetPayrollByGradeandPayRollTypeID = @"SELECT HRMS_EmployeePayroll.CompanyID, HRMS_EmployeePayroll.PeriodID,HRMS_EmployeePayroll.EmployeePayrollID
, HRMS_EmployeePayroll.PersonID, HRMS_EmployeePayroll.PayrollTypeID, HRMS_EmployeePayroll.PayrollTypeCode
, HRMS_EmployeePayroll.CalculatedAmount, HRMS_EmployeePayroll.ManualAmount, HRMS_EmployeePayroll.Remarks, HRMS_EmployeePayroll.Formula
, HRMS_EmployeePayroll.LeaveDays, HRMS_EmployeePayroll.Status, HRMS_EmployeePayroll.IsActive, HRMS_EmployeePayroll.CreatedBy,HRMS_EmployeePayroll.CreatedByValue, 
                       HRMS_EmployeePayroll.IsOneTime, HRMS_EmployeePayroll.IsPaid, HRMS_EmployeePayroll.Quantity, HRMS_EmployeePayroll.UnitOfMeasure, HRMS_EmployeePayroll.FiscalID
FROM            HRMS_EmployeePayroll

INNER JOIN HRMS_Set_Employee ON  HRMS_Set_Employee.PersonID=HRMS_EmployeePayroll.PersonID
and HRMS_Set_Employee.CompanyID=HRMS_EmployeePayroll.CompanyID
AND HRMS_Set_Employee.Status=1
Where HRMS_EmployeePayroll.Status=1 AND HRMS_Set_Employee.PersonID=@PersonID AND HRMS_EmployeePayroll.PayrollTypeID=@PayRollTypeID
AND HRMS_EmployeePayroll.PayrollTypeCode=@PayrollTypeCode
and HRMS_EmployeePayroll.CompanyID = @CompanyID

        ";
        const string Query_GetPayrollByGradeandPayRollTypeIDForUpdate = @"SELECT HRMS_EmployeePayroll.CompanyID, HRMS_EmployeePayroll.PeriodID,HRMS_EmployeePayroll.EmployeePayrollID
, HRMS_EmployeePayroll.PersonID, HRMS_EmployeePayroll.PayrollTypeID, HRMS_EmployeePayroll.PayrollTypeCode
, HRMS_EmployeePayroll.CalculatedAmount, HRMS_EmployeePayroll.ManualAmount, HRMS_EmployeePayroll.Remarks, HRMS_EmployeePayroll.Formula
, HRMS_EmployeePayroll.LeaveDays, HRMS_EmployeePayroll.Status, HRMS_EmployeePayroll.IsActive, HRMS_EmployeePayroll.CreatedBy,HRMS_EmployeePayroll.CreatedByValue, 
                       HRMS_EmployeePayroll.IsOneTime, HRMS_EmployeePayroll.IsPaid, HRMS_EmployeePayroll.Quantity, HRMS_EmployeePayroll.UnitOfMeasure, HRMS_EmployeePayroll.FiscalID
FROM            HRMS_EmployeePayroll

INNER JOIN HRMS_Set_Employee ON  HRMS_Set_Employee.PersonID=HRMS_EmployeePayroll.PersonID
AND HRMS_Set_Employee.Status=1
Where HRMS_EmployeePayroll.Status=0 AND HRMS_Set_Employee.PersonID=@PersonID AND HRMS_EmployeePayroll.PayrollTypeID=@PayRollTypeID
AND HRMS_EmployeePayroll.PayrollTypeCode=@PayrollTypeCode

        ";
        #endregion

        #region Class Methods

        public static DataTable GetEmployePayrollSeach(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_EmployePayrollSeach_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_EmployePayrollSeach_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployePayrollSeach_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployePayrollSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
       Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_EmployePayrollSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_EmployePayrollSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_EmployePayrollSearch_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetByPersonID(int companyID, int periodID,int personID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollByPersonID, sqlparams).Tables[0];
               
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataByPersonIDForIncrement(int companyID, int periodID, int personID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollByPersonID_ForIncrement, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static Decimal GetByPersonIDForHabib(int companyID, int periodID, int personID,string bonusFormula, Nullable<bool> isActive = true)
        {

            decimal amount = 0;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[4] = new SqlParameter("@Formula", bonusFormula);

                //amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetPayrollByPersonIDForHabib, sqlparams));
                amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_SalaryHeader.Query_GetPayrollByPersonIDForHabib), sqlparams));

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amount;
        }
        public static DataTable GetByPersonIDandType(int companyID, int periodID, int personID, int PayRollTypeID, string Query_GetPayrollByPersonID, bool isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@PayRollTypeID", PayRollTypeID);
                sqlparams[4] = new SqlParameter("@IsActive", isActive);
                
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollByPersonID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetByOnetimePayrollPersonID(int companyID, int periodID, int personID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);

                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollOnetimeByPersonID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetManualTaxAmount(Nullable<long> companyID, Nullable<long> periodID, Nullable<long> personID, Nullable<bool> isActive = true,SqlTransaction sqltrans = null , SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);


                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetManualTaxAmount, sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetManualTaxAmount, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetManualTaxAmount, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetManualTaxAmount, sqlparams);

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


        public static DataTable GetManualTaxAmountTemp(Nullable<long> companyID, Nullable<long> periodID, Nullable<long> personID, Nullable<bool> isActive = true, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);


                if (isActive.HasValue)
                    sqlparams[3] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsActive", DBNull.Value);

                DataSet ds = null;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetManualTaxAmountTemp, sqlparams);
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_GetManualTaxAmountTemp, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetManualTaxAmountTemp, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetManualTaxAmountTemp, sqlparams);

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

        public static DataTable GetdataByGradeID(int companyID, int periodID, int GradeID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@GradeID", GradeID);              

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataByGradeID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetEmployeeGradeID(int companyID, int periodID, int PersonID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetEmployeeGradeID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool UpdateAnnualLeavesByPersonID(int CompanyID, int PersonID, int PayRollTypeCode, int PayRollTypeID, int LeaveDays, SqlTransaction objTransaction = null, int CurrentFiscalID=0)
        {
            bool result = false;

            try
            {
                             
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[3] = new SqlParameter("@PayRollTypeID", PayRollTypeID);
                sqlparams[4] = new SqlParameter("@LeaveDays", LeaveDays);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);        
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateAnnualLeavesByPersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateAnnualLeavesByPersonID, sqlparams) > 0)
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

        public static bool UpdateCalculatedAmountByPersonIDUsingFormula(int CompanyID, int PersonID, int PayRollTypeCode, int PayRollTypeID, decimal CalculatedAmount, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[3] = new SqlParameter("@PayRollTypeID", PayRollTypeID);
                sqlparams[4] = new SqlParameter("@CalculatedAmount", CalculatedAmount);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateCalculatedAmountByPersonIDUsingFormula, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateCalculatedAmountByPersonIDUsingFormula, sqlparams) > 0)
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
    

        public static DataTable GetAllActiveDataWithImpact(bool EmpStatus,Nullable<int> companyID, Nullable<int> periodID,Nullable<DateTime> SalaryDate, Nullable<bool> isActive = true, SqlTransaction sqltrans=null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[3] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[4] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                sqlparams[5] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());

                string Query_GetAllActiveData = string.Empty;

                if (EmpStatus == true)
                   Query_GetAllActiveData = FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataForDailyWagesWithImpact; 
                else
                    Query_GetAllActiveData = FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataWithImpact;

                if(sqltrans!=null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetAllActiveData, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllActiveData, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveDataWithImpact_AlNoor(bool EmpStatus, Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate, Nullable<bool> isActive = true, string PersonIDs = "", SqlTransaction sqltrans = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[3] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[4] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                sqlparams[5] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());

                if (PersonIDs != "")
                    sb.Append(" and HRMS_Set_Employee.personID in (" + PersonIDs + ")");

                string Query_GetAllActiveData = string.Empty;

                if (EmpStatus == true)
                    Query_GetAllActiveData = FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataForDailyWagesWithImpact;
                else
                    Query_GetAllActiveData = FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataWithImpact_AlNoor;

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(Query_GetAllActiveData,sb), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllActiveData,sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool InsertWithAutoID(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[20];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@EmployeePayrollID", f_EmployeePayrollID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[4] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
                sqlparams[5] = new SqlParameter("@PayrollTypeCode", f_PayrollTypeCode);

                if (f_CalculatedAmount.HasValue)
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", f_CalculatedAmount.Value);
                else
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", DBNull.Value);

                if (f_ManualAmount.HasValue)
                    sqlparams[7] = new SqlParameter("@ManualAmount", f_ManualAmount.Value);
                else
                    sqlparams[7] = new SqlParameter("@ManualAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Remarks))
                    sqlparams[8] = new SqlParameter("@Remarks", f_Remarks);
                else
                    sqlparams[8] = new SqlParameter("@Remarks", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Formula))
                    sqlparams[9] = new SqlParameter("@Formula", f_Formula);
                else
                    sqlparams[9] = new SqlParameter("@Formula", DBNull.Value);

                if (f_LeaveDays.HasValue)
                    sqlparams[10] = new SqlParameter("@LeaveDays", f_LeaveDays.Value);
                else
                    sqlparams[10] = new SqlParameter("@LeaveDays", DBNull.Value);

                sqlparams[11] = new SqlParameter("@Status", f_Status);
                sqlparams[12] = new SqlParameter("@IsActive", f_IsActive);
                sqlparams[13] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[14] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[15] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_IsOneTime.HasValue)
                    sqlparams[16] = new SqlParameter("@IsOneTime", f_IsOneTime.Value);
                else
                    sqlparams[16] = new SqlParameter("@IsOneTime", DBNull.Value);

                if (f_IsPaid.HasValue)
                    sqlparams[17] = new SqlParameter("@IsPaid", f_IsPaid.Value);
                else
                    sqlparams[17] = new SqlParameter("@IsPaid", DBNull.Value);

                if (f_Quantity.HasValue)
                    sqlparams[18] = new SqlParameter("@Quantity", f_Quantity.Value);
                else
                    sqlparams[18] = new SqlParameter("@Quantity", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UnitOfMeasure))
                    sqlparams[19] = new SqlParameter("@UnitOfMeasure", f_UnitOfMeasure);
                else
                    sqlparams[19] = new SqlParameter("@UnitOfMeasure", DBNull.Value);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_InsertAutoID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_InsertAutoID, sqlparams) > 0)
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

        public bool Insertpayroll(SqlTransaction objTransaction = null)
        {
            bool result = false;

            int EPID = HRMS_EmployeePayroll.GetNewEmployeePayrollID(CompanyID, PeriodID);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[20];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@EmployeePayrollID", EPID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[4] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
                sqlparams[5] = new SqlParameter("@PayrollTypeCode", f_PayrollTypeCode);

                if (f_CalculatedAmount.HasValue)
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", f_CalculatedAmount.Value);
                else
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", DBNull.Value);

                if (f_ManualAmount.HasValue)
                    sqlparams[7] = new SqlParameter("@ManualAmount", f_ManualAmount.Value);
                else
                    sqlparams[7] = new SqlParameter("@ManualAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Remarks))
                    sqlparams[8] = new SqlParameter("@Remarks", f_Remarks);
                else
                    sqlparams[8] = new SqlParameter("@Remarks", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Formula))
                    sqlparams[9] = new SqlParameter("@Formula", f_Formula);
                else
                    sqlparams[9] = new SqlParameter("@Formula", DBNull.Value);

                if (f_LeaveDays.HasValue)
                    sqlparams[10] = new SqlParameter("@LeaveDays", f_LeaveDays.Value);
                else
                    sqlparams[10] = new SqlParameter("@LeaveDays", DBNull.Value);

                sqlparams[11] = new SqlParameter("@Status", f_Status);
                sqlparams[12] = new SqlParameter("@IsActive", f_IsActive);
                sqlparams[13] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[14] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[15] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_IsOneTime.HasValue)
                    sqlparams[16] = new SqlParameter("@IsOneTime", f_IsOneTime.Value);
                else
                    sqlparams[16] = new SqlParameter("@IsOneTime", DBNull.Value);

                if (f_IsPaid.HasValue)
                    sqlparams[17] = new SqlParameter("@IsPaid", f_IsPaid.Value);
                else
                    sqlparams[17] = new SqlParameter("@IsPaid", DBNull.Value);
                if (f_Quantity.HasValue)
                    sqlparams[18] = new SqlParameter("@Quantity", f_Quantity.Value);
                else
                    sqlparams[18] = new SqlParameter("@Quantity", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UnitOfMeasure))
                    sqlparams[19] = new SqlParameter("@UnitOfMeasure", f_UnitOfMeasure);
                else
                    sqlparams[19] = new SqlParameter("@UnitOfMeasure", DBNull.Value);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insertpayroll, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, sqlparams) > 0)
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

        public static DataTable GetAllLeavesObtained(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personID, string month, string year, SqlTransaction sqltrans = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (personID.HasValue)
                    sqlparams[2] = new SqlParameter("@PersonID", personID.Value);
                else
                    sqlparams[2] = new SqlParameter("@PersonID", DBNull.Value);
                sqlparams[3] = new SqlParameter("@Month", month);
                sqlparams[4] = new SqlParameter("@Year", year);

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans,CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllLeavesObtained, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllLeavesObtained, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetNotEntitledLeaveByPersonID(Nullable<int> companyID, Nullable<int> periodID, int  personId )
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personId);
               

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetNotEntitledLeaveByPersonID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteBykey(SqlTransaction objTransaction, Nullable<int> companyID, Nullable<int> periodID,int personID,Guid updatedby , string updatedbyvalue , DateTime updateddate )
        {//, int personId
            bool result = false;

            try
            {
               
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personID);
                sqlparams[3] = new SqlParameter("@UpdatedBy", updatedby);
                sqlparams[4] = new SqlParameter("@UpdateDate", updateddate);
                sqlparams[5] = new SqlParameter("@UpdatedByValue", updatedbyvalue);


                //  result = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByPersonID, sqlparams).Tables[0];

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteByPersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteByPersonID, sqlparams) > 0)
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


        public  bool DeleteDataBykey(SqlTransaction objTransaction, Nullable<int> companyID, Nullable<int> periodID)
        {//, int personId
          bool result = false;

            try
            {
                
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", f_EmployeePayrollID);


              //  result = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByPersonID, sqlparams).Tables[0];
                
                if(objTransaction == null)
				{
					if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByPersonID, sqlparams) > 0)
						result = true;
				}
				else
				{
					if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataByPersonID, sqlparams) > 0)
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

        public static bool DeleteDataByPersonID(SqlTransaction objTransaction, Nullable<int> companyID, Nullable<int> periodID, int personId)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", personId);


                //  result = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByPersonID, sqlparams).Tables[0];

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByPersonID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataByPersonID, sqlparams) > 0)
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

        public static DataTable GetDataByPersonID(int companyID, int PersonID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

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

        public bool UpdateDataByPersonIDForPayroll(int CompanyID, int PersonID,SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PersonID", f_PersonID);

                if (f_ManualAmount.HasValue)
                    sqlparams[2] = new SqlParameter("@ManualAmount", f_ManualAmount.Value);
                else
                    sqlparams[2] = new SqlParameter("@ManualAmount", DBNull.Value);
                sqlparams[3] = new SqlParameter("@EmployeePayrollID", f_EmployeePayrollID);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PersonIDForPayroll, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PersonIDForPayroll, sqlparams) > 0)
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


        public static bool UpdateStatusForPayrollPersonID(SqlTransaction objTransaction, Nullable<int> companyID, Nullable<int> periodID, Nullable<int> personId, int PayrollTypeID)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@PersonID", personId);
                sqlparams[2] = new SqlParameter("@PayrollTypeID", PayrollTypeID);


                if (personId > 0)
                    sb.Append(" and PersonID =  " + personId);


                //  result = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByPersonID, sqlparams).Tables[0];

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_UpdatePayrollStatus,sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_UpdatePayrollStatus, sb.ToString()), sqlparams) > 0)
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


        public static DataTable GetAllActiveDataWithImpactForAlabbas(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> SalaryDate,Nullable<int> PayrollGroupID, Nullable<bool> isActive = true, SqlTransaction sqltrans = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                sqlparams[3] = new SqlParameter("@SalaryDate", SalaryDate);
                sqlparams[4] = new SqlParameter("@Month", String.Format("{0:MMMM}", SalaryDate));
                sqlparams[5] = new SqlParameter("@Year", SalaryDate.Value.Year.ToString());

                if (PayrollGroupID != null && PayrollGroupID > 0)
                {
                    sb.Append(" where HRMS_PayRoll_Groups.Status = 1  and HRMS_PayRoll_Groups.GroupID =  " + PayrollGroupID);
                }

                if (sqltrans != null)
                    dt = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataWithImpactAlAbbas,sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataWithImpactAlAbbas, sb.ToString()), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetEmployeePayRollsByPersonIDIDandPayRollTypeID(int PayRollTypeCode, int PersonID, int PayRollTypeID,int CompanyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayRollTypeID", PayRollTypeID);
                sqlparams[3] = new SqlParameter("@CompanyID", CompanyID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollByGradeandPayRollTypeID, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetEmployeePayRollsByPersonIDandPayRollTypeIDForUpdate(int PayRollTypeCode, int PersonID, int PayRollTypeID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PayRollTypeCode", PayRollTypeCode);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);
                sqlparams[2] = new SqlParameter("@PayRollTypeID", PayRollTypeID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPayrollByGradeandPayRollTypeIDForUpdate, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataForOneTimeDeductions(DateTime? month, int? DepartmentID)
        {
            DataTable dt = null;

            #region query

            const string query = @"
            select
                [HRMS_SET_Employee].EmployeeCode, [HRMS_SET_Employee].[FullName] AS [FromPersonName],  HRMS_PayrollType.Description , EMD.Amount, EMD.ForTheMonth , [HRMS_SET_Employee].FK_DepartmentID
                FROM [HRMS_EmployeeMonthlyDeduction] EMD
                inner join [HRMS_SET_Employee] on [HRMS_SET_Employee].[PersonID] = EMD.[FromPersonID] and [HRMS_Set_Employee].CompanyID = EMD.CompanyID
		   
                inner join HRMS_PayrollType ON 
                --HRMS_PayrollType.CompanyID = EMD.CompanyID AND
                  HRMS_PayrollType.PayrollTypeID = EMD.PayRollTypeID

                  where FORMAT(ForTheMonth, 'yyyy-MM') = @MonthYear
                    AND EMD.PayRollTypeCode = 2
                    AND EMD.Status = 1
                ";

            #endregion

            try
            {
                string DepartmentFilter = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@MonthYear", month.Value.ToString("yyyy-MM"));

                if (DepartmentID.HasValue)
                {
                    DepartmentFilter = " AND FK_DepartmentID = " + DepartmentID;
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, query + DepartmentFilter, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeletePayRollbyPersonID(int PersonID,int CompanyID, SqlTransaction objTransaction = null)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();

            const string Query_DeleteDataByPersonPayrolltypeCodeID = @" Delete from HRMS_EmployeePayroll WHERE [PersonID] = @PersonID and CompanyID = @CompanyID ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PersonID", PersonID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);


                //  result = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByPersonID, sqlparams).Tables[0];

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_DeleteDataByPersonPayrolltypeCodeID, sb.ToString()), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_DeleteDataByPersonPayrolltypeCodeID, sb.ToString()), sqlparams) > 0)
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

        public static DataRow GetBasicSalaryByPersonID(int companyID, int personID, string PayrollType, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetData_byKeyWithOutPeriodId = @"  SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
 WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
{0}";
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);

                if (PayrollType != string.Empty)
                {
                    sb.Append(" And HRMS_PayrollType.Description Like '" + PayrollType.ToString() + "'");
                }
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetData_byKeyWithOutPeriodId, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetData_byKeyWithOutPeriodId, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetData_byKeyWithOutPeriodId, sb.ToString()), sqlparams);

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

        public static DataTable GetBasicSalaryForAlNoorByPersonID(int companyID, int personID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            const string Query_GetData_byKeyWithOutPeriodId = @"  
SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
     And HRMS_PayrollType.Description Like 'Basic%Salary'

    UNION ALL

 SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
     And HRMS_PayrollType.Description like '%Daily Wages Basic%'

	UNION ALL

   SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
 And HRMS_PayrollType.Description Like 'cola%1'

	UNION ALL
 
   SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
 And HRMS_PayrollType.Description Like 'cola%2'

	UNION ALL

   SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
 And HRMS_PayrollType.Description Like 'cola%7'

    UNION ALL
   
   SELECT * FROM [HRMS_EmployeePayroll] 
		Inner Join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID=[HRMS_EmployeePayroll].PayrollTypeID
		And HRMS_PayrollType.Status=1
  WHERE HRMS_EmployeePayroll.Status=1 and HRMS_EmployeePayroll.[CompanyID] = @CompanyID and HRMS_EmployeePayroll.PersonID = @PersonID 
 And HRMS_PayrollType.Description Like '%Atta%Allow'
";
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", personID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKeyWithOutPeriodId, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKeyWithOutPeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeyWithOutPeriodId, sqlparams);

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


        public static DataRow GetDataByPersonIDANdPayrollTypeID(int companyID, int periodID, int personID, int PayrollTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query = @"  select case when CalculatedAmount > 0 then CalculatedAmount else ManualAmount end as Value
                                    from HRMS_EmployeePayroll
                                    where PersonID = @personID and CompanyID = @companyID and PeriodID = @periodID and Status = 1 and PayrollTypeID = @PayrollTypeID";
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@periodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrollTypeID", PayrollTypeID);
                sqlparams[3] = new SqlParameter("@personID", personID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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


        public static DataSet GetDataDetailOFExecutiveSalary(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, int LocationID = 0, Nullable<int> DepartmentID = null, Nullable<int> EmployeeStatus = null,Nullable<Guid> UserID = null, SqlConnection objConnection = null) //Nullable<int> StatusID, Nullable<int> PersonID, 
        {
            DataSet dsToReturn = null;
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
                    sb.Append(" and HRMS_SET_Employee.FK_LocationID = " + LocationID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND HRMS_SET_Employee.FK_DepartmentID = " + DepartmentID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and HRMS_SET_Employee.StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

            // SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetAllActiveDataWithImpactAlAbbas, sb.ToString()), sqlparams).Tables[0];


                

                // Get the last day of the month
                DateTime lastDayOfMonth = new DateTime(DateTime.Parse(Date_From.ToString()).Year, DateTime.Parse(Date_From.ToString()).Month, DateTime.DaysInMonth(DateTime.Parse(Date_From.ToString()).Year, DateTime.Parse(Date_From.ToString()).Month));

                sb1.Append(" And (HRMS_Set_Employee.JobEndingDate IS NULL OR HRMS_Set_Employee.JobEndingDate > '" + lastDayOfMonth.ToString("yyyy-MM-dd") + "'" + ")");


                if (objConnection == null)
                //    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_DetailOFExecutiveSalary, sqlparams);
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_DetailOFExecutiveSalary, sb,sb1), sqlparams);
                else
                   // dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DetailOFExecutiveSalary, sqlparams);
                    dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_DetailOFExecutiveSalary, sb,sb1), sqlparams);

                   // dsToReturn = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DetailOFExecutiveSalary, sqlparams);

                //if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                //    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        #endregion


        public bool UpdateDataByKey_ForIncrement(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[21];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@EmployeePayrollID", f_EmployeePayrollID);
                sqlparams[3] = new SqlParameter("@PersonID", f_PersonID);
                sqlparams[4] = new SqlParameter("@PayrollTypeID", f_PayrollTypeID);
                sqlparams[5] = new SqlParameter("@PayrollTypeCode", f_PayrollTypeCode);

                if (f_CalculatedAmount.HasValue)
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", f_CalculatedAmount.Value);
                else
                    sqlparams[6] = new SqlParameter("@CalculatedAmount", DBNull.Value);

                if (f_ManualAmount.HasValue)
                    sqlparams[7] = new SqlParameter("@ManualAmount", f_ManualAmount.Value);
                else
                    sqlparams[7] = new SqlParameter("@ManualAmount", DBNull.Value);

               
                if (!string.IsNullOrEmpty(f_Formula))
                    sqlparams[9] = new SqlParameter("@Formula", f_Formula);
                else
                    sqlparams[9] = new SqlParameter("@Formula", DBNull.Value);

              

                sqlparams[11] = new SqlParameter("@Status", f_Status);
                sqlparams[12] = new SqlParameter("@IsActive", f_IsActive);

                if (f_UpdatedBy.HasValue)
                    sqlparams[13] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[13] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[14] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[14] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[15] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[15] = new SqlParameter("@UpdatedByValue", DBNull.Value);

  

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_ForIncrement, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_ForIncrement, sqlparams) > 0)
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

        public static DataTable AttendanceAllowanceReport(Nullable<int> companyID, Nullable<int> periodID, Nullable<DateTime> Date_From, SqlConnection objConnection = null, int LocationID = 0, int DivisionID = 0, int EmployeeTypeID = 0, Nullable<int> DepartmentID = null, int PersonID = 0, Nullable<int> EmployeeStatus = null, Nullable<Guid> UserID = null)
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
                    sb.Append(" and [HRMS_SET_Employee].FK_LocationID = " + LocationID);
                }

                if (DivisionID > 0)
                {
                    sb.Append(" and [HRMS_SET_Employee].DivisionID = " + DivisionID);
                }

                if (EmployeeTypeID > 0)
                {
                    sb.Append(" and [HRMS_SET_Employee].PersonTypeCode = " + EmployeeTypeID);
                }
                if (EmployeeStatus != null && EmployeeStatus > 0)
                {
                    sb.Append(" and [HRMS_SET_Employee].StatusID = " + EmployeeStatus);
                }
                else
                {
                    sb.Append(" and [HRMS_SET_Employee].StatusID in (select StatusID from HRMS_EmployeeInEmployement_Status where UserID = '" + UserID + "'" + ")");
                }

                if (PersonID > 0)
                {
                    sb.Append(" AND [HRMS_SET_Employee].PersonID = " + PersonID);
                }
                if (DepartmentID != null)
                {
                    sb.Append(" AND [HRMS_SET_Employee].FK_DepartmentID = " + DepartmentID);
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_AttendanceAllowanceReport, sb), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_AttendanceAllowanceReport, sb), sqlparams);
                }
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
        
    }
}
