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
    public partial class HRMS_EmployeeMonthlyDeduction
    {
        #region constants

//        const string Query_GetData_by_Key = @"SELECT [HRMS_EmployeeMonthlyDeduction].*,[HRMS_SET_Employee].[FullName] AS [FromPersonName],[HRMS_SET_Employee].[FullName] AS [ToPersonName],[HRMS_PayrollType].[Description] AS [PayrollTypeName]
//                                            FROM [HRMS_EmployeeMonthlyDeduction] 
//                                            inner join [HRMS_SET_Employee] on [HRMS_SET_Employee].[PersonID] = [HRMS_EmployeeMonthlyDeduction].[FromPersonID]
//                                            inner join [HRMS_PayrollType] on [HRMS_PayrollType].[PayrollTypeID]=[HRMS_EmployeeMonthlyDeduction].[PayrollTypeID]
//                                            where [HRMS_EmployeeMonthlyDeduction].[MonthlyDeductionID]=@MonthlyDeductionID";


//        const string Query_Select_WithPaging = @"SELECT [HRMS_EmployeeMonthlyDeduction].*,[HRMS_SET_Employee].[FullName] AS [FromPersonName]
//                                            , ROW_NUMBER() Over (Order by [HRMS_EmployeeMonthlyDeduction].[CompanyID], MonthlyDeductionID, PeriodID) as 'Row' FROM [HRMS_EmployeeMonthlyDeduction]
//                                            inner join [HRMS_SET_Employee] on [HRMS_SET_Employee].[PersonID] = [HRMS_EmployeeMonthlyDeduction].[FromPersonID]";


//        const string Query_Select_WithoutPaging = @"SELECT [HRMS_EmployeeMonthlyDeduction].*,[HRMS_SET_Employee].[FullName] AS [FromPersonName]
//                                              FROM [HRMS_EmployeeMonthlyDeduction]
//                                            inner join [HRMS_SET_Employee] on [HRMS_SET_Employee].[PersonID] = [HRMS_EmployeeMonthlyDeduction].[FromPersonID]";

        //const string Query_Get_MaxId_MD = @" SELECT IsNull(max(MasterHeaderID), 0) + 1 as MasterHeaderID FROM HRMS_EmployeeMonthlyDeduction WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

//            const string Query_SelectAllPersonID = @" SELECT HRMS_PayrollType.IsTaxable,*   FROM [HRMS_EmployeeMonthlyDeduction] 
//                                inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeMonthlyDeduction.PayRollTypeID
//                                and HRMS_PayrollType.CompanyID = HRMS_EmployeeMonthlyDeduction.CompanyID where [HRMS_EmployeeMonthlyDeduction].[Status] = 1 AND                                     [HRMS_EmployeeMonthlyDeduction].[CompanyID] = @CompanyID AND [HRMS_EmployeeMonthlyDeduction].[PeriodID] = @PeriodID 
//                                AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] >= @PersonID AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] <= @PersonID
                                //";

        #endregion

        #region methods


        public static int GetNewMonthlyDeductionCode( string query = "")
        {
            int ID = 0 ;

            try
            {
                
                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    ID = int.Parse(ds.Tables[0].Rows[0]["MonthlyDeductionCode"].ToString());
               
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }
        public static DataTable getPersonIDforMonthlyDed (int companyID, int fromPerson, int toPerson,string query)
        {
            DataTable dt = null;

            

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromPerson", fromPerson);
                sqlparams[2] = new SqlParameter("@ToPerson", toPerson);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataRow GetEmpData(int companyID, int monthlyDeductionID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@MonthlyDeductionID", monthlyDeductionID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetData_by_Key, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetData_by_Key, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_GetData_by_Key, sqlparams);

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


        public static DataTable GetAllEmpData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Select_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static int GetNewMasterID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Get_MaxId_MD, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Get_MaxId_MD, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_Get_MaxId_MD, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllIncreementPersonID(Nullable<int> companyID, Nullable<int> periodID, int PersonIDForIncreement, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonIDForIncreement);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllPersonID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllPersonID, sqlparams);

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

        public static DataTable GetIncrementOfPersonByMonth(Nullable<int> companyID, Nullable<int> periodID, int PersonIDForIncreement, DateTime? forthemonth,SqlTransaction sqltrans= null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonIDForIncreement);
                sqlparams[3] = new SqlParameter("@forthemonth", forthemonth);

                string query = @"
                        SELECT HRMS_PayrollType.IsTaxable,*   
                        FROM [HRMS_EmployeeMonthlyDeduction] 
                        inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeMonthlyDeduction.PayRollTypeID
                        where [HRMS_EmployeeMonthlyDeduction].[Status] = 1 
                        AND        [HRMS_EmployeeMonthlyDeduction].[CompanyID] = @CompanyID AND [HRMS_EmployeeMonthlyDeduction].[PeriodID] = @PeriodID 
                        AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] >= @PersonID AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] <= @PersonID
                        and IsTaxable = 1
                        and ForTheMonth <= @forthemonth
                ";

                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans,CommandType.Text, query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable GetIncrementOfPersonByMonth_AlNoor(Nullable<int> companyID, Nullable<int> periodID, int PersonIDForIncreement, DateTime? forthemonth, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonIDForIncreement);
               // sqlparams[3] = new SqlParameter("@forthemonth", forthemonth);
                sqlparams[3] = new SqlParameter("@forthemonth", String.Format("{0:MMMM}", forthemonth));
                sqlparams[4] = new SqlParameter("@fortheyear", String.Format("{0:yyyy}", forthemonth));
           
                string query = @"
                        SELECT HRMS_PayrollType.IsTaxable,*   
                        FROM [HRMS_EmployeeMonthlyDeduction] 
                        inner join HRMS_PayrollType on HRMS_PayrollType.PayrollTypeID = HRMS_EmployeeMonthlyDeduction.PayRollTypeID
                        where [HRMS_EmployeeMonthlyDeduction].[Status] = 1 
                        AND        [HRMS_EmployeeMonthlyDeduction].[CompanyID] = @CompanyID AND [HRMS_EmployeeMonthlyDeduction].[PeriodID] = @PeriodID 
                        AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] >= @PersonID AND [HRMS_EmployeeMonthlyDeduction].[FromPersonID] <= @PersonID
                        and IsTaxable = 1
                       --and ForTheMonth <= @forthemonth
                        and DATENAME(month,HRMS_EmployeeMonthlyDeduction.ForTheMonth) = @forthemonth
                       and DATENAME(year,HRMS_EmployeeMonthlyDeduction.ForTheMonth) = @fortheyear
                ";

                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable GetEmployeeIncrementOfPersonByMonth_AlNoor(Nullable<int> companyID, Nullable<int> periodID, int PersonIDForIncreement, DateTime? forthemonth, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PersonID", PersonIDForIncreement);
                sqlparams[3] = new SqlParameter("@forthemonth", String.Format("{0:MMMM}", forthemonth));
                sqlparams[4] = new SqlParameter("@fortheyear", String.Format("{0:yyyy}", forthemonth));

                string query = @"
                        select *,isnull(ArrerAmount,0) as ArrerAmount from HRMS_EmployeeIncrement where Status = 1 and PersonID = @PersonID 
                        and DATENAME(month,HRMS_EmployeeIncrement.IncrementDate) = @forthemonth
                        and DATENAME(year,HRMS_EmployeeIncrement.IncrementDate) = @fortheyear
                        and CompanyID= @CompanyID
                ";

                DataSet ds = null;

                if (objConnection == null)
                {
                    if (sqltrans != null)
                        ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable GetEmployeeEmployeeMonthlyDeuction_AlNoor(Nullable<int> companyID, Nullable<int> periodID, int PayrollTypeCode, int payrolltypeid, Nullable<int> fromPersonID, Nullable<int> toPersonID, DateTime? fromdate, DateTime? ToDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayRollTypeCode", PayrollTypeCode);
                sqlparams[3] = new SqlParameter("@PayrollTypeID", payrolltypeid);
                sqlparams[4] = new SqlParameter("@FromPersonID", fromPersonID);
                sqlparams[5] = new SqlParameter("@ToPersonID", toPersonID);
                sqlparams[6] = new SqlParameter("@FromDate", fromdate);
                sqlparams[7] = new SqlParameter("@ToDate", ToDate);
                //sqlparams[6] = new SqlParameter("@forthemonth", String.Format("{0:MMMM}", forthemonth));
                //sqlparams[7] = new SqlParameter("@fortheyear", String.Format("{0:yyyy}", forthemonth));

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllDataforexistingdata, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllDataforexistingdata, sqlparams);

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

        public static DataTable GetEmployeeEmployeeMonthlyDeuction_ForAll(Nullable<int> companyID, Nullable<int> periodID, int PayrollTypeCode, int payrolltypeid, Nullable<int> fromPersonID, DateTime? fromdate, DateTime? ToDate, SqlTransaction sqltrans = null, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PayRollTypeCode", PayrollTypeCode);
                sqlparams[3] = new SqlParameter("@PayrollTypeID", payrolltypeid);
                sqlparams[4] = new SqlParameter("@FromPersonID", fromPersonID);
                //sqlparams[5] = new SqlParameter("@ToPersonID", toPersonID);
                sqlparams[5] = new SqlParameter("@FromDate", fromdate);
                sqlparams[6] = new SqlParameter("@ToDate", ToDate);
                //sqlparams[6] = new SqlParameter("@forthemonth", String.Format("{0:MMMM}", forthemonth));
                //sqlparams[7] = new SqlParameter("@fortheyear", String.Format("{0:yyyy}", forthemonth));

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllDataforexistingdataNew, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.HRMS.HRMS_EmployeePayRoll.Query_SelectAllDataforexistingdataNew, sqlparams);

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

        #endregion



    }
}
