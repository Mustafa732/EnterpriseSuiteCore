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

	public partial class HRMS_FinalSettlement
	{
        const string QUERY_GetDataForReport = @" Select FNFID
,HRMS_FInalSettlement.PersonID,NoticePayAmount,PeriodFrom
,PeriodTo,PeriodDaysAmount,PrvMonthSalary,OverTimeAmount,PrvMonthOverTime,FoodAllowance,AttenAllowanceMonth,AttenAllowanceAmount
,ProdIncentiveMonth,ProdIncentiveAmount
,LeaveEncashYear,LeaveEncashAmount,OtherPayable
,OtherPayablemount,NoticeDeductionAmount,Loan,IncomeTax as Tamount,EOBI,OtherDeduction,OtherDeductionAmount,Remarks
,HRMS_Set_Employee.FullName,BasicSalary,EmployeeCode,JoiningDate,JobEndingDate,FK_DepartmentID,DepartmentName
,JobDesignationName,FatherName,
DateName( month , DateAdd( month , AttenAllowanceMonth , -1 ) )AttendanceMonthName
,DateName( month , DateAdd( month , ProdIncentiveMonth , -1 ) )ProductionMonthName,
DateDiff(day,PeriodFrom,PeriodTo)Days
 from HRMS_FInalSettlement
 
 inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_FInalSettlement.PersonID
 inner join Set_Department on Set_Department.DepartmentID = HRMS_Set_Employee.FK_DepartmentID
 inner join Set_JobDesignation on Set_JobDesignation.JobDesignationID = HRMS_Set_Employee.FK_JobDesignationID
 
 
 where HRMS_FInalSettlement.Status = 1
 and HRMS_Set_Employee.Status = 1 
 and HRMS_FInalSettlement.CompanyID = @CompanyID
 and HRMS_FInalSettlement.PersonID = @PersonID
  ";

        const string Query_SelectAllPaging = @" 
SELECT [FNFID], HRMS_FinalSettlement.[CompanyID], [PeriodID], HRMS_FinalSettlement.PersonID,HRMS_Set_Employee.FullName,
 [NoticePayAmount], [PeriodFrom], [PeriodTo], [PeriodDaysAmount], [PrvMonthSalary], [OverTimeAmount], [PrvMonthOverTime],
  [FoodAllowance], [AttenAllowanceMonth], [AttenAllowanceAmount], [ProdIncentiveMonth], [ProdIncentiveAmount], [LeaveEncashYear], 
  [LeaveEncashAmount], [OtherPayable], [OtherPayablemount], [NoticeDeductionAmount], [Loan], [IncomeTax], [EOBI], [OtherDeduction], 
  [OtherDeductionAmount], [Remarks], HRMS_FinalSettlement.[Status], [StatusCode],HRMS_FinalSettlement.CreateDate,
   ROW_NUMBER() Over (Order by HRMS_FinalSettlement.CompanyID, FNFID) as 'Row' 
   FROM [HRMS_FinalSettlement] 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_FinalSettlement.PersonID ";

        const string Query_SelectAllWithoutPagingForSearch = @" 

SELECT [FNFID], HRMS_FinalSettlement.[CompanyID], [PeriodID], HRMS_FinalSettlement.PersonID,HRMS_Set_Employee.FullName,
 [NoticePayAmount], [PeriodFrom], [PeriodTo], [PeriodDaysAmount], [PrvMonthSalary], [OverTimeAmount], [PrvMonthOverTime],
  [FoodAllowance], [AttenAllowanceMonth], [AttenAllowanceAmount], [ProdIncentiveMonth], [ProdIncentiveAmount], [LeaveEncashYear], 
  [LeaveEncashAmount], [OtherPayable], [OtherPayablemount], [NoticeDeductionAmount], [Loan], [IncomeTax], [EOBI], [OtherDeduction], 
  [OtherDeductionAmount], [Remarks], HRMS_FinalSettlement.[Status], [StatusCode],HRMS_FinalSettlement.CreateDate
   
   FROM [HRMS_FinalSettlement] 
inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_FinalSettlement.PersonID ";



        public static DataTable GetAllDataForReport(Nullable<int> companyID, Nullable<int> PersonID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PersonID", PersonID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QUERY_GetDataForReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, QUERY_GetDataForReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, QUERY_GetDataForReport, sqlparams);

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


        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAllPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAllPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAllWithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllWithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
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
