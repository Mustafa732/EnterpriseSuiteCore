using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DAL.DataAccessLayer;
using Microsoft.Data.SqlClient;
using Common.Utils;
namespace FnI.BusinessAccessLayer.HRMS.Payroll.Transaction
{
    public partial class HRMS_MonthlyAdvance
    {
        const string Query_Get_MaxHeaderId = @" SELECT IsNull(max(MonthlyAdvanceHeaderID), 0) + 1 as MonthlyAdvanceHeaderID FROM HRMS_MonthlyAdvance WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID ";

        const string Query_Select_By_DipRecipeID = @" SELECT HRMS_MonthlyAdvance.MonthlyAdvanceID as SerialNo,HRMS_MonthlyAdvance.MonthlyAdvanceID,HRMS_MonthlyAdvance.MonthlyAdvanceHeaderID,
 HRMS_MonthlyAdvance.AdvanceDate,HRMS_MonthlyAdvance.AdvanceMonth,HRMS_MonthlyAdvance.AdvanceTypeID,HRMS_MonthlyAdvance.EmployeeID,
 HRMS_Set_Employee.FullName,HRMS_MonthlyAdvance.GrossSalary,HRMS_MonthlyAdvance.Percentage,HRMS_MonthlyAdvance.NetAmount,'' as Action 
 FROM HRMS_MonthlyAdvance
 INNER JOIN HRMS_Set_Employee on HRMS_Set_Employee.PersonID = HRMS_MonthlyAdvance.EmployeeID Where HRMS_MonthlyAdvance.CompanyID = @CompanyID and HRMS_MonthlyAdvance.PeriodID = @PeriodID  and HRMS_MonthlyAdvance.MonthlyAdvanceHeaderID = @MonthlyAdvanceHeaderID";


        const string Query_SelectAll_WithPagings = @" select 
DocumentNO,[Month],AdvanceDate,status,ROW_NUMBER() Over (Order by CompanyID) as Row from (
SELECT distinct MonthlyAdvanceHeaderID as DocumentNO,datename(MONTH,AdvanceMonth) as [Month],AdvanceDate,CompanyID,status
  FROM [HRMS_MonthlyAdvance]
  ) as TAB";

        const string Query_SelectAll_WithoutPagings = @"select 
DocumentNO,[Month],AdvanceDate,status
--ROW_NUMBER() Over (Order by CompanyID) as Row 
from (
SELECT distinct MonthlyAdvanceHeaderID as DocumentNO,datename(MONTH,AdvanceMonth) as [Month],AdvanceDate,CompanyID,status
  FROM [HRMS_MonthlyAdvance]
  ) as TAB";

        const string Pagings = @"WITH ResultSet AS({0})
			SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize;
			SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query)";

        const string Query_GetData_DocmentNo = @" SELECT * FROM [HRMS_MonthlyAdvance] WHERE [CompanyID] = @CompanyID and [MonthlyAdvanceHeaderID] = @MonthlyAdvanceHeaderID and [PeriodID] = @PeriodID ";

        public bool InsertAdvanceMonthHeaderAndDetail(List<HRMS_MonthlyAdvance> colrecipeDtl, List<HRMS_LoanSanctionHeader> colLSHDetail_dt, List<HRMS_LoanSanctionDetail> colLSHDetail_Detail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertMonthlyAdvance");
            try
            {

                //this.Insert(sqltrans);
                //short MonthlyAdvanceID = short.Parse(HRMS_MonthlyAdvance.GetNewMonthlyAdvanceID(this.CompanyID, this.PeriodID, sqltrans).ToString());
                //short MonthlyAdvanceHeaderID = short.Parse(HRMS_MonthlyAdvance.GetNewMonthlyHeaderID(this.CompanyID, this.PeriodID, sqltrans).ToString());
               
               int ID = HRMS_MonthlyAdvance.GetNewMonthlyAdvanceID(this.CompanyID, this.PeriodID,sqltrans);
               

                foreach (HRMS_MonthlyAdvance Ldr in colrecipeDtl)
                {
                    Ldr.MonthlyAdvanceHeaderID = this.MonthlyAdvanceHeaderID;
                    Ldr.MonthlyAdvanceID = ID++;
                    Ldr.Insert(sqltrans);
                }
              

                int LSH_ID = HRMS_LoanSanctionHeader.GetNewLoanSanctionHeaderID(this.CompanyID, this.PeriodID, sqltrans);
                int LoanHdrDetailID = LSH_ID;

                foreach (HRMS_LoanSanctionHeader LSH in colLSHDetail_dt)
                {
                    LSH.LoanSanctionHeaderID = LSH_ID++;
                    //LSH. = ID++;
                    LSH.Insert(sqltrans);
                }
               
                //short LSDDetailID = short.Parse(HRMS_LoanSanctionDetail.GetNewLoanSanctionDetailID(this.CompanyID, LSH_ID, this.PeriodID, sqltrans).ToString());
                foreach (HRMS_LoanSanctionDetail LSD in colLSHDetail_Detail)
                {
                    LSD.LoanSanctionHeaderID = LoanHdrDetailID++;
                    LSD.LoanSanctionDetailID = HRMS_LoanSanctionDetail.GetNewLoanSanctionDetailID(this.CompanyID, LoanHdrDetailID, this.PeriodID, sqltrans);
                    LSD.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertMonthlyAdvance");
            }

            return result;

        }

        public bool UpdateAdvanceMonthHeaderAndDetail(List<HRMS_MonthlyAdvance> colMonthlyAdvanceDtl, List<HRMS_MonthlyAdvance> colMonthlyAdvanceUpdate, List<HRMS_MonthlyAdvance> colMonthlyAdvanceDelete, List<HRMS_LoanSanctionHeader> colLoanSactionDtl, List<HRMS_LoanSanctionHeader> colLoanSactionUpdate, List<HRMS_LoanSanctionHeader> colLoanSactionDelete, List<HRMS_LoanSanctionDetail> colLoanSanctionDetailAdd, List<HRMS_LoanSanctionDetail> colLoanSanctionDetailEdit, List<HRMS_LoanSanctionDetail> colLoanSanctionDetailDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("updateMonthlyAdvance");
            try
            {

                //this.UpdateDataByKey(sqltrans);
                //short RecipeDetailID = short.Parse(HRMS_MonthlyAdvance.GetNewDipRecipeDetailID(this.CompanyID, this.DipRecipeID, this.PeriodID, sqltrans).ToString());

                int A = HRMS_MonthlyAdvance.GetNewMonthlyAdvanceID(this.CompanyID, this.PeriodID, sqltrans);
                foreach (HRMS_MonthlyAdvance Ldr in colMonthlyAdvanceDtl)
                {
                    Ldr.MonthlyAdvanceHeaderID = this.MonthlyAdvanceHeaderID;
                    Ldr.MonthlyAdvanceID = A;
                     Ldr.Insert(sqltrans);
                }


                foreach (HRMS_MonthlyAdvance LdrUpdate in colMonthlyAdvanceUpdate)
                    LdrUpdate.UpdateDataByKey(sqltrans);


                foreach (HRMS_MonthlyAdvance LdrDelete in colMonthlyAdvanceDelete)
                    HRMS_MonthlyAdvance.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, LdrDelete.f_MonthlyAdvanceID, f_PeriodID);


                DataTable dt = HRMS_LoanSanctionHeader.GetAllActiveLoanSactionHeader(this.CompanyID, this.PeriodID,this.MonthlyAdvanceID);
                int LSH_ID = HRMS_LoanSanctionHeader.GetNewLoanSanctionHeaderID(this.CompanyID, this.PeriodID, sqltrans);
                int LoanHdrDetailID = LSH_ID;

                foreach (HRMS_LoanSanctionHeader LSH in colLoanSactionDtl)
                {
                    LSH.LoanSanctionHeaderID = LSH_ID++;
                    //LSH. = ID++;
                    LSH.Insert(sqltrans);
                }


                foreach (HRMS_LoanSanctionHeader LSHUpdate in colLoanSactionUpdate)
                    LSHUpdate.UpdateDataByKey(sqltrans);


                foreach (HRMS_LoanSanctionHeader LSHDelete in colLoanSactionDelete)
                    HRMS_LoanSanctionHeader.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, LSHDelete.LoanSanctionHeaderID, f_PeriodID);


               
                foreach (HRMS_LoanSanctionDetail LSD in colLoanSanctionDetailAdd)
                {
                    LSD.LoanSanctionHeaderID = LoanHdrDetailID++;
                    LSD.LoanSanctionDetailID = HRMS_LoanSanctionDetail.GetNewLoanSanctionDetailID(this.CompanyID, LSD.LoanSanctionHeaderID, this.PeriodID, sqltrans); 
                    LSD.Insert(sqltrans);
                }


                foreach (HRMS_LoanSanctionDetail LSDUpdate in colLoanSanctionDetailEdit)
                    LSDUpdate.UpdateDataByKey(sqltrans);


                foreach (HRMS_LoanSanctionDetail LSDDelete in colLoanSanctionDetailDelete)
                    HRMS_LoanSanctionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, LSDDelete.LoanSanctionDetailID, LSDDelete.LoanSanctionHeaderID, f_PeriodID);




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
                    SqlHelper.RollBackTransaction(sqltrans, "updateMonthlyAdvance");
            }

            return result;

        }

        public bool DeleteMonthlyAdvanceHeaderAndDetail(List<HRMS_MonthlyAdvance> colMonthlyAdvanceDelete, List<HRMS_LoanSanctionHeader> colLoanSanctionHeaderDelete, List<HRMS_LoanSanctionDetail> colLoanSanctionDetailDelete)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteMonthlyAdvance");
            try
            {

                //HRMS_MonthlyAdvance.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,f_MonthlyAdvanceID ,f_PeriodID);

                foreach (HRMS_MonthlyAdvance MADelete in colMonthlyAdvanceDelete)
                    HRMS_MonthlyAdvance.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,MADelete.MonthlyAdvanceID, f_PeriodID);

                foreach (HRMS_LoanSanctionHeader LSHDelete in colLoanSanctionHeaderDelete)
                    HRMS_LoanSanctionHeader.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, LSHDelete.LoanSanctionHeaderID, f_PeriodID);

                foreach (HRMS_LoanSanctionDetail LSDDelete in colLoanSanctionDetailDelete)
                    HRMS_LoanSanctionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,LSDDelete.LoanSanctionDetailID,LSDDelete.LoanSanctionHeaderID, f_PeriodID);



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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteMonthlyAdvance");
            }

            return result;

        }



        public static int GetNewMonthlyHeaderID(int companyID, int periodID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxHeaderId, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByKey_Detail(int MonthlyAdvanceHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@MonthlyAdvanceHeaderID", MonthlyAdvanceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DipRecipeID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_DipRecipeID, sqlparams);

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


        public static DataTable GetAllDataByEmployee(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Pagings, Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Pagings, Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataRow GetDataByAllDocumentNo(int companyID, int MonthlyAdvanceHeaderID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@MonthlyAdvanceHeaderID", MonthlyAdvanceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_DocmentNo, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_DocmentNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_DocmentNo, sqlparams);

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





    }
}
