using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
   public partial class HRMS_EmployeeIncrement_Allowancewise
    {

       const string Query_SelectAll_WithPagingForSearch = @" 
Select IncrementID,HRMS_Set_Employee.EmployeeCode,HRMS_EmployeeIncrement_Allowancewise.IncrementDate,HRMS_EmployeeIncrement_Allowancewise.IncrementAmount,FullName
,ROW_NUMBER() Over (Order by IncrementID) as 'Row'
 from HRMS_EmployeeIncrement_Allowancewise

inner join HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_EmployeeIncrement_Allowancewise.PersonID
";

       const string Query_SelectAll_WithOthertable = @" SELECT *,  (case when HRMS_PayrollType.PayrollTypeCode = 1 then 'Allowance'
  when HRMS_PayrollType.PayrollTypeCode = 2 then 'Deduction'
  when HRMS_PayrollType.PayrollTypeCode = 3 then 'Contribution' else '' end) as Type,'Update' as Action FROM [HRMS_EmployeeIncrement_Allowancewise_Detail] 
              JOIN HRMS_PayrollType ON HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeIncrement_Allowancewise_Detail.PayrollTypeID
                AND HRMS_PayrollType.PayrollTypeCode = HRMS_EmployeeIncrement_Allowancewise_Detail.PayrollTypeCode 
                AND HRMS_PayrollType.Status = 1 AND HRMS_PayrollType.IsActive = 1 
				
				inner join HRMS_EmployeeIncrement_Allowancewise on HRMS_EmployeeIncrement_Allowancewise.IncrementID = HRMS_EmployeeIncrement_Allowancewise_Detail.IncrementID
				and HRMS_EmployeeIncrement_Allowancewise.CompanyID = HRMS_EmployeeIncrement_Allowancewise_Detail.CompanyID
				and HRMS_EmployeeIncrement_Allowancewise.Status = 1 

				 JOIN HRMS_EmployeePayroll ON HRMS_EmployeePayroll.PayrollTypeID = HRMS_EmployeeIncrement_Allowancewise_Detail.PayrollTypeID
                AND HRMS_EmployeePayroll.PayrollTypeCode = HRMS_EmployeeIncrement_Allowancewise_Detail.PayrollTypeCode 	
				and 	HRMS_EmployeePayroll.PersonID = 	HRMS_EmployeeIncrement_Allowancewise.PersonID					
                AND HRMS_EmployeePayroll.Status = 1

WHERE HRMS_EmployeeIncrement_Allowancewise_Detail.[Status] = 1 AND (HRMS_EmployeeIncrement_Allowancewise_Detail.[CompanyID] = @CompanyID OR @CompanyID is null) 
AND (HRMS_EmployeeIncrement_Allowancewise.[IncrementID] = @IncrementID OR @IncrementID is null) 
AND (HRMS_EmployeeIncrement_Allowancewise_Detail.[PeriodID] = @PeriodID OR @PeriodID is null) ";


        public bool InsertSOHeaderAndDetail(List<HRMS_EmployeeIncrement_Allowancewise_Detail> colSODetail, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertIncrement");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {


                this.Insert(sqltrans);

                foreach (HRMS_EmployeeIncrement_Allowancewise_Detail sod in colSODetail)
                {


                    sod.IncrementDetailID = int.Parse(HRMS_EmployeeIncrement_Allowancewise_Detail.GetNewIncrementDetailID(CompanyID, IncrementID, PeriodID, sqltrans).ToString());
                    sod.Insert(sqltrans);  

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
                        SqlHelper.RollBackTransaction(sqltrans, "InsertIncrement");
                    }
                    else
                    { }
                }
            }

            return result;
        }


        public bool UpdateSOHeaderAndDetail(List<HRMS_EmployeeIncrement_Allowancewise_Detail> colSODetail_Update, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("UpdateIncrement");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {


                this.UpdateDataByKey(sqltrans);

                foreach (HRMS_EmployeeIncrement_Allowancewise_Detail col in colSODetail_Update)
                {
                    col.UpdateDataByKey(sqltrans);
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
                        SqlHelper.RollBackTransaction(sqltrans, "UpdateIncrement");
                    }
                    else
                    { }
                }
            }

            return result;
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    //if (objTransaction != null)
                    //    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    //else
                    //    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingForSearch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveDataWithOtherTable(Nullable<int> companyID, Nullable<int> incrementID, Nullable<int> periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@IncrementID", incrementID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithOthertable, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_WithOthertable, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithOthertable, sqlparams);

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
