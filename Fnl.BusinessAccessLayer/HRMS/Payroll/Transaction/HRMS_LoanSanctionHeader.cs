using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;
using Common.Utils;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.HRMS.Payroll.Setup;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_LoanSanctionHeader
    {

        #region Constant Strings

        const string Query_GetData_byPersonID = @" SELECT [HRMS_LoanSanctionHeader].[LoanSanctionHeaderID] FROM [HRMS_LoanSanctionHeader] WHERE [CompanyID] = @CompanyID and [PersonID] = @PersonID and [PeriodID] = @PeriodID ";

        const string Query_SelectAllSearch_WithPaging = @" 
SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID], 
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionHeader].[Status], 
[HRMS_LoanSanctionHeader].[CreatedBy], 
[HRMS_LoanSanctionHeader].[CreateDate], 
[HRMS_LoanSanctionHeader].[CreatedByValue], 
[HRMS_LoanSanctionHeader].[UpdatedBy], 
[HRMS_LoanSanctionHeader].[UpdateDate], 
[HRMS_LoanSanctionHeader].[UpdatedByValue],
max(HRMS_LoanSanctionDetail.LoanPaidOn) [LoanPaidOn] ,
ROW_NUMBER() Over (Order by [HRMS_LoanSanctionHeader].[CompanyID], [HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], [HRMS_LoanSanctionHeader].[PeriodID]) as 'Row' 

FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1

Inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID = [HRMS_LoanSanctionHeader].[LoanSanctionHeaderID]
AND HRMS_LoanSanctionDetail.CompanyID = [HRMS_LoanSanctionHeader].CompanyID
AND HRMS_LoanSanctionDetail.PeriodID= [HRMS_LoanSanctionHeader].PeriodID and HRMS_LoanSanctionDetail.[Status] = 1

 Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
       And HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID

LEFT JOin HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID

";

        const string Query_SelectAllSearch_WithoutPaging = @" SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID], 
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionHeader].[Status], 
[HRMS_LoanSanctionHeader].[CreatedBy], 
[HRMS_LoanSanctionHeader].[CreateDate], 
[HRMS_LoanSanctionHeader].[CreatedByValue], 
[HRMS_LoanSanctionHeader].[UpdatedBy], 
[HRMS_LoanSanctionHeader].[UpdateDate], 
[HRMS_LoanSanctionHeader].[UpdatedByValue] 

FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1 

  Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
        And HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID

  ";


        const string Query_SelectLoanHeaderAndDetailSearch_WithoutPaging = @" SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID], 
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionDetail].[LoanSanctionDetailID],
[HRMS_LoanSanctionDetail].[PayrollTypeID],
[HRMS_PayrollType].Code as LoanTypeCode,
[HRMS_LoanSanctionDetail].[PayrollTypeCode],
[HRMS_PayrollType].[Description] ,
[HRMS_PayrollType].[Description] as LoanType,
[HRMS_LoanSanctionDetail].[LoanDueDate] as LoanDueDate,
[HRMS_LoanSanctionDetail].[LoanStartDate] as LoanStartDate,
[HRMS_LoanSanctionDetail].[LoanEndDate],
[HRMS_LoanSanctionDetail].[StopDate],
[HRMS_LoanSanctionDetail].[ResumeDate],
[HRMS_LoanSanctionDetail].[LoanAmount] as LoanAmount,
[HRMS_LoanSanctionDetail].[InstallmentAmount],
[HRMS_LoanSanctionDetail].[LoanAmount] / [HRMS_LoanSanctionDetail].[InstallmentAmount] as InstallmentMonths,
[HRMS_LoanSanctionDetail].[LoanInterest],
[HRMS_LoanSanctionDetail].[LoanPaidOn] as LoanSanctionDate,
[HRMS_LoanSanctionDetail].[LoanDeductionStop],
[HRMS_LoanSanctionDetail].[IsDeductionCompleted],
[HRMS_LoanSanctionDetail].[IsActive],
[HRMS_LoanSanctionHeader].[LoanGuarantor],
[LoanGurantor].FullName as LoanerName,
'' as Action 


FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_LoanSanctionDetail ON
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID] = [HRMS_LoanSanctionDetail].[LoanSanctionHeaderID] AND 
[HRMS_LoanSanctionHeader].[CompanyID] = [HRMS_LoanSanctionDetail].[CompanyID] AND 
[HRMS_LoanSanctionHeader].[PeriodID] = [HRMS_LoanSanctionDetail].[PeriodID] AND 
[HRMS_LoanSanctionDetail].[Status]  = 1


INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1 

INNER JOIN HRMS_PayrollType ON
[HRMS_LoanSanctionDetail].[PayrollTypeID] = [HRMS_PayrollType].[PayrollTypeID] AND
[HRMS_PayrollType].[Status] = 1

Left Join HRMS_SET_Employee LoanGurantor on LoanGurantor.PersonID = [HRMS_LoanSanctionHeader].LoanGuarantor
AND 
HRMS_LoanSanctionHeader.CompanyID = LoanGurantor.CompanyID AND
LoanGurantor.Status = 1 ";

        const string Query_SelectAllWithPayRollID = @" Select PayrollTypeID, PayrollTypeCode,Description from HRMS_PayrollType where PayrollTypeCode=5 and Status=1
AND CompanyID=@CompanyID ";

        const string Query_SelectAllLoanSactionHeader = @"  SELECT * 
 FROM [HRMS_LoanSanctionHeader] 
 WHERE CompanyID = @CompanyID 
 and LoanSanctionHeaderID = @LoanSanctionHeaderID  
 and PeriodID = @PeriodID ";

        const string Query_GetDatabyLoanSanctionHeaderIDOnSearch = @"select 
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
		        AND Set_JobDesignation.CompanyID = HRMS_Set_Employee.CompanyID
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
		and HRMS_PayrollType.CompanyID =  HRMS_LoanSanctionDetail.CompanyID
		
		left join HRMS_EmployeeShift on HRMS_EmployeeShift.PersonID = HRMS_SET_Employee.PersonID 
		and HRMS_EmployeeShift.CompanyID =  HRMS_SET_Employee.CompanyID
		and HRMS_EmployeeShift.Status = 1
		
		left join HRMS_SET_Shift on HRMS_SET_Shift.ShiftID = HRMS_EmployeeShift.ShiftID 
		and HRMS_SET_Shift.ShiftCode = HRMS_EmployeeShift.ShiftCode  
		and HRMS_SET_Shift.Status = 1
		
		
		where 
  HRMS_LoanPaymentDetail.CreateDate between @FromDate and @ToDate 
   AND  HRMS_LoanPaymentDetail.CompanyID =  @CompanyID 
		and HRMS_LoanPaymentDetail.PeriodID =  @PeriodID		
		and  HRMS_LoanPaymentDetail.Status =1  
		and HRMS_LoanSanctionHeader.Status =1 
		and HRMS_SET_Employee.Status =1 	 
		and HRMS_PayrollType.Status =1 
        {0}
        order by HRMS_Set_Employee.PersonID asc";

        const string Query_SelectLoanHeaderAndDetailSearch = @"SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionDetail].LoanSanctionDetailID, 
[HRMS_LoanSanctionDetail].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID], 
[HRMS_PayrollType].Code ,
[HRMS_EmployeePayroll].EmployeePayrollID,
[HRMS_EmployeePayroll].[PayrollTypeID],
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionDetail].[LoanSanctionDetailID],
[HRMS_LoanSanctionDetail].[PayrollTypeCode],
[HRMS_PayrollType].Code as LoanTypeCode,
[HRMS_PayrollType].[Description] as LoanType,
[HRMS_PayrollType].[Description] ,
[HRMS_PayrollType].[Formula],
[HRMS_LoanSanctionDetail].[LoanDueDate] as LoanDueDate,
[HRMS_LoanSanctionDetail].[LoanStartDate] as LoanStartDate,
[HRMS_LoanSanctionDetail].[LoanEndDate],
[HRMS_LoanSanctionDetail].[StopDate],
[HRMS_LoanSanctionDetail].[ResumeDate],
[HRMS_LoanSanctionDetail].[LoanAmount] as CalculatedAmount,
[HRMS_LoanSanctionDetail].[InstallmentAmount],
[HRMS_LoanSanctionDetail].[LoanInterest],
[HRMS_LoanSanctionDetail].[LoanPaidOn] as LoanSanctionDate,
[HRMS_LoanSanctionDetail].[LoanDeductionStop],
[HRMS_LoanSanctionDetail].[IsDeductionCompleted],
[HRMS_LoanSanctionDetail].[IsActive],
'' as Action,
[HRMS_EmployeePayroll].[ManualAmount],
[HRMS_EmployeePayroll].[LeaveDays],
[HRMS_EmployeePayroll].[Quantity],
[HRMS_EmployeePayroll].[UnitOfMeasure] 


FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_LoanSanctionDetail ON
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID] = [HRMS_LoanSanctionDetail].[LoanSanctionHeaderID] AND 
[HRMS_LoanSanctionHeader].[CompanyID] = [HRMS_LoanSanctionDetail].[CompanyID] AND 
[HRMS_LoanSanctionHeader].[PeriodID] = [HRMS_LoanSanctionDetail].[PeriodID] AND 
[HRMS_LoanSanctionDetail].[Status]  = 1

INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1 

INNER JOIN HRMS_PayrollType ON
[HRMS_LoanSanctionDetail].[PayrollTypeID] = [HRMS_PayrollType].[PayrollTypeID]
AND [HRMS_PayrollType].[Status] = 1  

INNER JOIN [HRMS_EmployeePayroll] 
ON [HRMS_PayrollType].PayrollTypeCode = [HRMS_EmployeePayroll].[PayrollTypeID]";
     const string   Query_GetDLoanSanctionRecoveryData= @"
Select
         Fintab.*
 from 
 (
 Select HRMS_LoanSanctionHeader.CompanyID,
 HRMS_LoanSanctionHeader.PeriodID
 ,HRMS_Set_Employee.FullName
        ,HRMS_Set_Employee.EmployeeCode
        ,HRMS_Set_Employee.PersonID
       ,HRMS_Set_Employee.BasicSalary
        ,HRMS_Set_Employee.FatherName
        ,HRMS_PayrollType.[Description] as LoanType
        ,HRMS_PayrollType.PayrollTypeID as LoanTypeID
        ,HRMS_LoanSanctionDetail.LoanPaidOn as PaymentDate
        ,'Sanction' as PaymentType
        ,isnull(HRMS_LoanSanctionDetail.LoanAmount,0) as SanctionAmount
        ,0 as Recovered
        ,'Loan' as  Remarks
       
 from HRMS_Set_Employee
 INNER JOIN HRMS_LoanSanctionHeader ON HRMS_Set_Employee.PersonID= HRMS_LoanSanctionHeader.PersonID 
 AND HRMS_LoanSanctionHeader.CompanyID = HRMS_Set_Employee.CompanyID	
		INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID 
		--AND HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 
		inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
		and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
		
		
		Where 
	      HRMS_LoanSanctionDetail.CreateDate between @FromDate and @ToDate 
      AND  HRMS_LoanSanctionDetail.CompanyID =  @CompanyID 
		and HRMS_LoanSanctionDetail.PeriodID =  @PeriodID		
		and HRMS_SET_Employee.Status =1 
       and HRMS_LoanSanctionHeader.Status =1 
               {0}

	
		UNION ALL
		
		Select 
		HRMS_LoanSanctionHeader.CompanyID
		,HRMS_LoanSanctionHeader.PeriodID
        ,HRMS_Set_Employee.FullName
        ,HRMS_Set_Employee.EmployeeCode
        ,HRMS_Set_Employee.PersonID
        ,HRMS_Set_Employee.BasicSalary
        ,HRMS_Set_Employee.FatherName
        ,HRMS_PayrollType.[Description] as LoanType
        ,HRMS_PayrollType.PayrollTypeID as LoanTypeID
        ,HRMS_LoanPaymentDetail.LoanPaymentDate as PaymentDate
        ,HRMS_LoanPaymentDetail.PaymentType
        ,0 as  SanctionAmount
        ,HRMS_LoanPaymentDetail.LoanDisburstAmount as Recovered
        ,'Recovery' as Remarks
       
        From HRMS_Set_Employee
        
          INNER JOIN HRMS_LoanSanctionHeader ON HRMS_Set_Employee.PersonID= HRMS_LoanSanctionHeader.PersonID 
               AND HRMS_LoanSanctionHeader.CompanyID = HRMS_Set_Employee.CompanyID 
               INNER JOIN HRMS_LoanSanctionDetail ON HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanSanctionHeader.LoanSanctionHeaderID 
		AND HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanSanctionHeader.CompanyID 

         INNER JOIN HRMS_LoanPaymentDetail ON HRMS_LoanPaymentDetail.CompanyID=HRMS_Set_Employee.CompanyID
         AND    HRMS_LoanPaymentDetail.LoanSanctionHeaderID =  HRMS_LoanSanctionHeader.LoanSanctionHeaderID 
         AND   HRMS_LoanPaymentDetail.LoanSanctionDetailID = HRMS_LoanSanctionDetail.LoanSanctionDetailID
         inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID =  HRMS_LoanSanctionDetail.PayrollTypeID
		and HRMS_PayrollType.PayrollTypeCode =  HRMS_LoanSanctionDetail.PayrollTypeCode
	

        Where
         HRMS_LoanSanctionDetail.CreateDate between @FromDate and @ToDate 
        AND  HRMS_LoanSanctionDetail.CompanyID =  @CompanyID 
		and HRMS_LoanSanctionDetail.PeriodID =  @PeriodID	
		and  HRMS_LoanPaymentDetail.Status =1  
       and HRMS_LoanSanctionHeader.Status =1  
               {0}
         ) as Fintab
         
  
   GROUP BY Fintab.FullName,Fintab.EmployeeCode,Fintab.PersonID,Fintab.PaymentType,Fintab.CompanyID, Fintab.PeriodID,Fintab.BasicSalary,Fintab.FatherName,Fintab.PaymentDate
   ,Fintab.SanctionAmount,Fintab.Recovered ,Fintab.Remarks,Fintab.LoanType,Fintab.LoanTypeID  
ORDER by Fintab.Remarks, Fintab.PaymentDate";
     const string QuerySelectAll_WithPaging = @"SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID], 
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionHeader].[Status], 
[HRMS_LoanSanctionHeader].[CreatedBy], 
[HRMS_LoanSanctionHeader].[CreateDate], 
[HRMS_LoanSanctionHeader].[CreatedByValue], 
[HRMS_LoanSanctionHeader].[UpdatedBy], 
[HRMS_LoanSanctionHeader].[UpdateDate], 
[HRMS_LoanSanctionHeader].[UpdatedByValue],
ROW_NUMBER() Over (Order by [HRMS_LoanSanctionHeader].[CompanyID], [HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], [HRMS_LoanSanctionHeader].[PeriodID]) as 'Row' 

FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1 

Inner join HRMS_EmployeeInEmployement_Status on HRMS_EmployeeInEmployement_Status.StatusID = HRMS_Set_Employee.StatusID
       And HRMS_EmployeeInEmployement_Status.CompanyID = HRMS_Set_Employee.CompanyID

Inner join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID = [HRMS_LoanSanctionHeader].[LoanSanctionHeaderID]
AND HRMS_LoanSanctionDetail.CompanyID = [HRMS_LoanSanctionHeader].CompanyID
AND HRMS_LoanSanctionDetail.PeriodID= [HRMS_LoanSanctionHeader].PeriodID and HRMS_LoanSanctionDetail.[Status] = 1

LEFT JOin HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID

";
        #endregion

        #region Class Methods

        public static DataRow GetDataByPersonID(int companyID, int periodID, int PersonID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPersonID, sqlparams);

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

        public static DataRow GetObjectByPersonID(int companyID, int periodID, int PersonID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            HRMS_LoanSanctionHeader _object = new HRMS_LoanSanctionHeader();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byPersonID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    //    _object = new HRMS_LoanSanctionHeader(companyID, int.Parse(ds.Tables[0].Rows[0].ToString()), periodID);
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        public static DataTable GetAllDataWithPersonDetail(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), QuerySelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithLoanDetail(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {              
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLoanHeaderAndDetailSearch_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithLoanDetail_Report(Nullable<int> CompanyID, Nullable<int> PeriodID, bool Active, bool Inactive, Nullable<int> EmployeeFrom = null,
              Nullable<int> EmployeeTo = null, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<DateTime> StartDate = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dt = null;

            #region Query
            string Query_SelectLoanHeaderAndDetailSearch = @" SELECT 
[HRMS_LoanSanctionHeader].[CompanyID], 
[HRMS_LoanSanctionHeader].[PeriodID], 
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID], 
[HRMS_LoanSanctionHeader].[PersonID],
[HRMS_Set_Employee].[EmployeeCode],
[HRMS_SET_Employee].[FullName],
[HRMS_LoanSanctionDetail].[LoanSanctionDetailID],
[HRMS_LoanSanctionDetail].[PayrollTypeID],
[HRMS_PayrollType].Code as LoanTypeCode,
[HRMS_LoanSanctionDetail].[PayrollTypeCode],
[HRMS_PayrollType].[Description] ,
[HRMS_PayrollType].[Description] as LoanType,
[HRMS_LoanSanctionDetail].[LoanDueDate] as LoanDueDate,
[HRMS_LoanSanctionDetail].[LoanStartDate] as LoanStartDate,
[HRMS_LoanSanctionDetail].[LoanEndDate],
[HRMS_LoanSanctionDetail].[StopDate],
[HRMS_LoanSanctionDetail].[ResumeDate],
[HRMS_LoanSanctionDetail].[LoanAmount] as LoanAmount,
[HRMS_LoanSanctionDetail].[InstallmentAmount],
[HRMS_LoanSanctionDetail].[LoanAmount] / [HRMS_LoanSanctionDetail].[InstallmentAmount] as InstallmentMonths,
[HRMS_LoanSanctionDetail].[LoanInterest],
[HRMS_LoanSanctionDetail].[LoanPaidOn] as LoanSanctionDate,
[HRMS_LoanSanctionDetail].[LoanDeductionStop],
[HRMS_LoanSanctionDetail].[IsDeductionCompleted],
[HRMS_LoanSanctionDetail].[IsActive],
[HRMS_LoanSanctionHeader].[LoanGuarantor],
[LoanGurantor].FullName as LoanerName,
'' as Action 


FROM [HRMS_LoanSanctionHeader] 

INNER JOIN HRMS_LoanSanctionDetail ON
[HRMS_LoanSanctionHeader].[LoanSanctionHeaderID] = [HRMS_LoanSanctionDetail].[LoanSanctionHeaderID] AND 
[HRMS_LoanSanctionHeader].[CompanyID] = [HRMS_LoanSanctionDetail].[CompanyID] AND 
[HRMS_LoanSanctionHeader].[PeriodID] = [HRMS_LoanSanctionDetail].[PeriodID] AND 
[HRMS_LoanSanctionDetail].[Status]  = 1


INNER JOIN HRMS_SET_Employee ON 
HRMS_LoanSanctionHeader.PersonID = HRMS_SET_Employee.PersonID AND
HRMS_LoanSanctionHeader.CompanyID = HRMS_SET_Employee.CompanyID AND
HRMS_SET_Employee.Status = 1 

INNER JOIN HRMS_PayrollType ON
[HRMS_LoanSanctionDetail].[PayrollTypeID] = [HRMS_PayrollType].[PayrollTypeID] AND
[HRMS_PayrollType].[Status] = 1

Left Join HRMS_SET_Employee LoanGurantor on LoanGurantor.PersonID = [HRMS_LoanSanctionHeader].LoanGuarantor
AND 
HRMS_LoanSanctionHeader.CompanyID = LoanGurantor.CompanyID AND
LoanGurantor.Status = 1 
WHERE HRMS_LoanSanctionHeader.CompanyID = @CompanyID AND HRMS_LoanSanctionHeader.PeriodID = @PeriodID AND HRMS_LoanSanctionHeader.Status = 1
";

            #endregion Query

            SqlParameter[] sqlparams = new SqlParameter[5];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);



            if (FromDate.HasValue)
            {
                sqlparams[2] = new SqlParameter("@DateFrom", String.Format("{0:yyyy-MM-dd}", FromDate));
            }
            
            if (ToDate.HasValue)
            {
                sqlparams[3] = new SqlParameter("@DateTo", String.Format("{0:yyyy-MM-dd}", ToDate));
            }

            if (StartDate.HasValue)
            {
                sqlparams[4] = new SqlParameter("@StartDate", String.Format("{0:yyyy-MM-dd}", StartDate));
            }

            if (FromDate != null && ToDate != null)
            {
                sb.Append(" AND [HRMS_LoanSanctionDetail].[LoanPaidOn] between @DateFrom AND @DateTo ");
            }

            if (StartDate != null)
            {
                sb.Append(" AND [HRMS_LoanSanctionDetail].[LoanStartDate] = @StartDate ");
            }

            if (Active == true && Inactive == false)
            {
                sb.Append(" AND HRMS_LoanSanctionDetail.IsActive = 1 ");
            }


            if (Inactive == true && Active == false)
            {
                sb.Append(" AND HRMS_LoanSanctionDetail.IsActive = 0 ");
            }

            if (Active == true && Inactive == true)
            {
                sb.Append("");
            }

            if (EmployeeFrom.HasValue && EmployeeTo.HasValue)
                sb.Append(" AND HRMS_LoanSanctionHeader.PersonID between " + EmployeeFrom + " AND " + EmployeeTo);

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLoanHeaderAndDetailSearch + sb.ToString(), sqlparams.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetLoanDetailData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
           Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectLoanHeaderAndDetailSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool UpdateLoanSanctionHeaderWithDetails(List<HRMS_LoanSanctionDetail> colLoanDetailAdd, List<HRMS_LoanSanctionDetail> colLoanDetailUpdate, List<HRMS_LoanSanctionDetail> colLoanDetailDel)
        {

            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateLoanSanction");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (HRMS_LoanSanctionDetail colSDetailAdd in colLoanDetailAdd)
                {
                    colSDetailAdd.LoanSanctionDetailID = int.Parse(HRMS_LoanSanctionDetail.GetNewLoanSanctionDetailID(CompanyID, LoanSanctionHeaderID, PeriodID, sqltrans).ToString());
                    colSDetailAdd.Insert(sqltrans);
                }

                foreach (HRMS_LoanSanctionDetail colSDetailEdit in colLoanDetailUpdate)
                {
                    colSDetailEdit.UpdateDataByKey(sqltrans);
                }

                foreach (HRMS_LoanSanctionDetail colSDetailDelete in colLoanDetailDel)
                {
                    HRMS_LoanSanctionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, colSDetailDelete.LoanSanctionDetailID, LoanSanctionHeaderID, f_PeriodID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateLoanSanction");
            }

            return result;

        }

        #endregion

        public static DataTable Get_EmployeeLoanLedger(int companyID, int periodID, DateTime? Date_From, DateTime? Date_To, int? personIDFrom, int? personIDTo, int? PayRollTypeID, int? EmployeeTypeID)
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
                if (PayRollTypeID != null)
                    sb.Append("and  HRMS_PayrollType.PayrollTypeID=" + PayRollTypeID + " ");

                if (EmployeeTypeID!=null)
                    sb.Append("and HRMS_SET_Employee.PersonTypeCode=" + EmployeeTypeID + " ");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDatabyLoanSanctionHeaderIDOnSearch, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllActiveDataWithPayRollID(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithPayRollID, sqlparams);

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

        public static DataTable EmployeeLoanLedger(int companyID, int periodID, DateTime? Date_From, DateTime? Date_To, int? personIDFrom, int? personIDTo, int? PayRollTypeID, int? EmployeeTypeID)
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
                if (PayRollTypeID != null)
                    sb.Append("and  HRMS_PayrollType.PayrollTypeID=" + PayRollTypeID + " ");

                if (EmployeeTypeID != null)
                    sb.Append("and HRMS_SET_Employee.PersonTypeCode=" + EmployeeTypeID + " ");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDLoanSanctionRecoveryData, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetAllActiveLoanSactionHeader(Nullable<int> companyID, Nullable<int> periodID, int LoanSanctionHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllLoanSactionHeader, sqlparams);

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

        public static DataTable GetLoanMonthsPolicy(Nullable<int> companyID, Nullable<int> periodID, int PayrolltypeID)
        {
            DataTable dt = null;

            const string Query_GetData = @" Select * from  HRMS_LoanPolicy

where LoanTypeID =  @PayrolltypeID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayrolltypeID", PayrolltypeID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);

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

        public static DataTable GetRecoveredAmount(Nullable<int> companyID, Nullable<int> periodID, int LoanSanctionheaderid)
        {
            DataTable dt = null;

            const string Query_GetData = @" select sum(LoanDisburstAmount) as LoanDisburstAmount from  
HRMS_LoanPaymentDetail

where LoanSanctionHeaderID = @LoanSanctionHeaderID 
and CompanyID = @CompanyID and PeriodID = @PeriodID and status = 1";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionheaderid);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);

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

        public static DataTable GetRecoveredAmount_Typewise(Nullable<int> companyID, Nullable<int> periodID, int LoanSanctionheaderid, string LoanType = "")
        {
            DataTable dt = null;

            const string Query_GetData = @" select sum(LoanDisburstAmount) as LoanDisburstAmount from  
HRMS_LoanPaymentDetail

left join HRMS_LoanSanctionDetail on HRMS_LoanSanctionDetail.LoanSanctionHeaderID = HRMS_LoanPaymentDetail.LoanSanctionHeaderID
and HRMS_LoanSanctionDetail.LoanSanctionDetailID = HRMS_LoanPaymentDetail.LoanSanctionDetailID
and HRMS_LoanSanctionDetail.CompanyID = HRMS_LoanPaymentDetail.CompanyID
and HRMS_LoanSanctionDetail.PeriodID = HRMS_LoanPaymentDetail.PeriodID
and HRMS_LoanSanctionDetail.Status = 1

inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_LoanSanctionDetail.PayrollTypeID
and HRMS_PayrollType.PayrollTypeCode = HRMS_LoanSanctionDetail.PayrollTypeCode
and HRMS_PayrollType.Status = 1
and HRMS_PayrollType.Description = @LoanType

where HRMS_LoanPaymentDetail.LoanSanctionHeaderID = @LoanSanctionHeaderID 
and HRMS_LoanPaymentDetail.CompanyID = @CompanyID and HRMS_LoanPaymentDetail.PeriodID = @PeriodID and HRMS_LoanPaymentDetail.status = 1";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionheaderid);
                sqlparams[3] = new SqlParameter("@LoanType", LoanType);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);

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

        public static DataTable GetActualLoanAmount(Nullable<int> companyID, Nullable<int> periodID, int LoanSanctionheaderid, int LoanSanctionDetailid)
        {
            DataTable dt = null;

   const string Query_GetData = @" 
   Select isnull(sum(LoanAmount),0) as LoanAmount from HRMS_LoanSanctionDetail where LoanSanctionHeaderID =  @LoanSanctionHeaderID  and LoanSanctionDetailID = @LoanSanctionDetailID 
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionheaderid);
                sqlparams[3] = new SqlParameter("@LoanSanctionDetailID", LoanSanctionDetailid);


                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);

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
