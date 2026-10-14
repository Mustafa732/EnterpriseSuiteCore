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
   public partial  class HRMS_EmployeeCashPayment
   {
       #region Constants

       const string Query_GetDatabyEmployeeIDOnSearch = @"

Select 
   	 datename( month, HRMS_SalaryHeader.SalaryDate ) as SalaryMonth
	,datename( year, HRMS_SalaryHeader.SalaryDate ) as SalaryYear
	,HRMS_Set_Employee.FullName as EmployeeName
	,HRMS_EmployeeCashPayment.PaymentAmount
	,HRMS_EmployeeCashPayment.PaymentDate
	,HRMS_EmployeeCashPayment.PaymentID
,case 
		when PaymentType = 2 
	then 'Cash' 
		when PaymentType = 1 
	then 'Cheque' 
		end 
			as PaymentMode 
from HRMS_SalaryHeader 

		inner join HRMS_EmployeeCashPayment on HRMS_EmployeeCashPayment.PaymentID = HRMS_SalaryHeader.PaymentID
		and  HRMS_EmployeeCashPayment.CompanyID = HRMS_SalaryHeader.CompanyID

		inner join HRMS_SET_Employee on HRMS_SalaryHeader.PersonID = HRMS_SET_Employee.PersonID
		and  HRMS_SET_Employee.CompanyID = HRMS_SalaryHeader.CompanyID

	
	where HRMS_SalaryHeader.CompanyID = @CompanyID
   {0}
	and HRMS_EmployeeCashPayment.Status = 1
    and HRMS_SalaryHeader.Status  = 1 
	
";

       #endregion


       #region Methods

       public static DataTable GetDataByEmployeeIDOnSearch(int CompanyID, int PeriodID, int PersonID, int DesignationID, string CnicNo, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
       {
           DataTable dr = null;
           StringBuilder sb = new StringBuilder();
           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
               sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
             //  sqlparams[2] = new SqlParameter("@PersonID", PersonID);

               DataSet ds = null;

               if (PersonID > 0)
                   sb.Append(" and HRMS_SalaryHeader.PersonID = " + PersonID);
               if (DesignationID > 0)
                   sb.Append(" AND HRMS_SET_Employee.FK_JobDesignationID =" + DesignationID);
               if (CnicNo != "")
                   sb.Append(" AND HRMS_SET_Employee.CNICNumber like '%" + CnicNo + "%' ");
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
