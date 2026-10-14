using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class BankReconcilation_Header
    {
        const string Query_DeleteDataByBankReconcilationID = @"  Delete from BankReconcilation_Detail where BankReconcilationID = @BankReconcilationID And CompanyID =@CompanyID And PeriodID=@PeriodID";

        const string Query_SelectSaltDetail_By_SubDetail = @"
select BankReconcilationSubDetail.BankReconcilationID as SerialNo,BankReconcilationSubDetail.BankReconcilationID,
BankReconcilationSubDetail.BankReconcilationSubDetailID,BankReconcilationSubDetail.Date
,ReportedByBank,ReportedDifferentAmountDebit,ReportedDifferentAmountCredit,NoteReportedAmountDebit,NoteReportedAmountCredit,BankReconcilationSubDetail.Description
,'' as Action
 from 
BankReconcilationSubDetail
where BankReconcilationSubDetail.CompanyID =@CompanyID and BankReconcilationSubDetail.PeriodID = @PeriodID
and BankReconcilationSubDetail.BankReconcilationID = @BankReconcilationID
and BankReconcilationSubDetail.Status = 1
";

        const string Query_GetDetailDataByHeaderID = @"Select Distinct BankReconcilation_Detail.BankReconcilationID as SerialNo ,BankReconcilation_Detail.BankReconcilationID,BankReconcilation_Detail.BankReconcilationDetailID,--BankReconcilationSubDetail.BankReconcilationSubDetailID,
 BankReconcilation_Detail.DocumentTypeID,BankReconcilation_Detail.DocumentNo,BankReconcilation_Detail.DocumentDate,
 BankReconcilation_Detail.ChequeNo AS PaymentServerOrderNo,BankReconcilation_Detail.Debit AS CurrentBalanceDebit,BankReconcilation_Detail.Credit AS CurrentBalanceCredit,BankReconcilation_Detail.Balance,
 BankReconcilation_Detail.TypeOfBalance,BankReconcilation_Detail.ReportedByBank,BankReconcilation_Detail.ReportedDifferentAmount,
 BankReconcilation_Detail.NoteReportedAmount,BankReconcilation_Header.Date,BankReconcilation_Header.FromDate,BankReconcilation_Header.ToDate
 ,BankReconcilation_Header.BankAccountNameID,BankReconcilation_Header.BankBranchID,BankReconcilation_Header.BankNameID,
-- BankReconcilationSubDetail.ReportedDifferentAmountDebit,BankReconcilationSubDetail.ReportedDifferentAmountCredit,BankReconcilationSubDetail.NoteReportedAmountDebit,BankReconcilationSubDetail.ReportedByBank,BankReconcilationSubDetail.NoteReportedAmountCredit
 BankReconcilation_Detail.ItemName,BankReconcilation_Detail.ReportedByDate,
Set_DocumentType.DocumentType
from BankReconcilation_Detail
Inner join BankReconcilation_Header on BankReconcilation_Header.BankReconcilationID = BankReconcilation_Detail.BankReconcilationID
--INNER JOIN BankReconcilationSubDetail ON BankReconcilationSubDetail.BankReconcilationID = BankReconcilation_Header.BankReconcilationID
Left Join Set_DocumentType On Set_DocumentType.DocumentTypeID = BankReconcilation_Detail.DocumentTypeID
where BankReconcilation_Detail.status =1
And BankReconcilation_Detail.BankReconcilationID = @BankReconcilationID";

        const string Query_SelectAll_WithPagings = @"SELECT BankReconcilation_Header.[BankReconcilationID], BankReconcilation_Header.[CompanyID], BankReconcilation_Header.[PeriodID]
, BankReconcilation_Header.[Date], BankReconcilation_Header.[BankNameID],BankReconcilation_Header.[BankBranchID], BankReconcilation_Header.[BankAccountNameID]
, BankReconcilation_Header.[FromDate], BankReconcilation_Header.[ToDate], BankReconcilation_Header.[Status], 
BankReconcilation_Header.[CreatedBy], BankReconcilation_Header.[CreateDate], BankReconcilation_Header.[UpdatedBy], 
BankReconcilation_Header.[UpdateDate], BankReconcilation_Header.[CreatedByValue], BankReconcilation_Header.[UpdatedByValue],
AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,AR_Set_bankAccount.BankAccountName
, ROW_NUMBER() Over (Order by BankReconcilation_Header.BankReconcilationID, BankReconcilation_Header.CompanyID, BankReconcilation_Header.PeriodID) as 'Row' 
FROM [BankReconcilation_Header] 
INNER JOIN AR_Set_Bank on AR_Set_Bank.BankID = BankReconcilation_Header.BankNameID
INNER JOIN AR_Set_bankBranch on AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID  ";

        const string Query_SelectAll_WithoutPagings = @" SELECT BankReconcilation_Header.[BankReconcilationID], BankReconcilation_Header.[CompanyID], BankReconcilation_Header.[PeriodID]
, BankReconcilation_Header.[Date], BankReconcilation_Header.[BankNameID],BankReconcilation_Header.[BankBranchID], BankReconcilation_Header.[BankAccountNameID]
, BankReconcilation_Header.[FromDate], BankReconcilation_Header.[ToDate], BankReconcilation_Header.[Status], 
BankReconcilation_Header.[CreatedBy], BankReconcilation_Header.[CreateDate], BankReconcilation_Header.[UpdatedBy], 
BankReconcilation_Header.[UpdateDate], BankReconcilation_Header.[CreatedByValue], BankReconcilation_Header.[UpdatedByValue],
AR_Set_Bank.BankName,AR_Set_bankBranch.BankBranchName,AR_Set_bankAccount.BankAccountName
--, ROW_NUMBER() Over (Order by BankReconcilation_Header.BankReconcilationID, BankReconcilation_Header.CompanyID, BankReconcilation_Header.PeriodID) as 'Row' 
FROM [BankReconcilation_Header] 
INNER JOIN AR_Set_Bank on AR_Set_Bank.BankID = BankReconcilation_Header.BankNameID
INNER JOIN AR_Set_bankBranch on AR_Set_bankBranch.BankBranchID = BankReconcilation_Header.BankBranchID
INNER JOIN AR_Set_bankAccount on AR_Set_bankAccount.BankAccountID = BankReconcilation_Header.BankAccountNameID ";

        const string Pagings = @"WITH ResultSet AS({0})
			SELECT * FROM ResultSet WHERE Row between (@PageIndex - 1) * @PageSize + 1 and @PageIndex * @PageSize;
			SET @TotalRows = (SELECT COUNT(*) FROM({1}) as Query)";

    
        public static DataTable GetDataByHeaderID(Nullable<int> companyID, Nullable<int> BankReconcilationID, Nullable<int> periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@BankReconcilationID", BankReconcilationID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDetailDataByHeaderID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDetailDataByHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDetailDataByHeaderID, sqlparams);

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

        public static DataTable GetDetailDataByKeySubDetail_OtherTables(int BankReconcilationID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BankReconcilationID", BankReconcilationID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSaltDetail_By_SubDetail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectSaltDetail_By_SubDetail, sqlparams);

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



        public bool InsertBankReconcilationHeaderWithDetails(List<BankReconcilation_Detail> colBankReconcilationDetailDtl, List<BankReconcilationSubDetail> colBankReconcilationSubDetail, SqlTransaction objTransaction = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertReceipe");
            try
            {

                this.Insert(sqltrans);
                //objRequsition.Insert(sqltrans);
               
                #region BankReconcilationDetail

                short BankReconcilationDetailID = short.Parse(BankReconcilation_Detail.GetNewBankReconcilationDetailID(this.BankReconcilationID, this.CompanyID, this.PeriodID, sqltrans).ToString());

                foreach (BankReconcilation_Detail BRD in colBankReconcilationDetailDtl)
                {
                    BRD.BankReconcilationID = this.BankReconcilationID;
                    BRD.BankReconcilationDetailID = BankReconcilationDetailID++;
                    BRD.Insert(sqltrans);
                }

                #endregion

                #region BankReconcilationSubDetail

                short BankReconcilationSubDetailID = short.Parse(BankReconcilationSubDetail.GetNewBankReconcilationSubDetailID(this.BankReconcilationID, this.CompanyID, this.PeriodID, sqltrans).ToString());
                //short DetailIDForRequsistion = ProcessRecipeDetailID;
                foreach (BankReconcilationSubDetail BRSD in colBankReconcilationSubDetail)
                {
                    BRSD.BankReconcilationID = this.BankReconcilationID;
                    BRSD.BankReconcilationSubDetailID = BankReconcilationSubDetailID++;
                    BRSD.Insert(sqltrans);
                }

                #endregion


           

              

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertReceipe");
            }

            return result;

        }


        public bool UpdateBankReconcilationHeaderWithDetails(List<BankReconcilation_Detail> colBankReconcilationDetailADD, List<BankReconcilationSubDetail> colBankReconcilationSubDetailADD, List<BankReconcilationSubDetail> colBankReconcilationSubDetailUpdate, List<BankReconcilationSubDetail> colBankReconcilationSubDetailDelete, SqlTransaction objTransaction = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("updateBankReconcilation");
            try
            {

                this.UpdateDataByKey(sqltrans);
                //objRequsition.UpdateDataByKey(sqltrans);

                #region //BankReconcilation_Detail

                BankReconcilation_Header.DeleteDataByBankReconcilationID(this.CompanyID, this.BankReconcilationID, this.PeriodID, sqltrans);


                short DetailID = short.Parse(BankReconcilation_Detail.GetNewBankReconcilationDetailID(this.BankReconcilationID, this.CompanyID, this.PeriodID, sqltrans).ToString());

                foreach (BankReconcilation_Detail BRD in colBankReconcilationDetailADD)
                {
                    BRD.BankReconcilationID = this.BankReconcilationID;
                    BRD.BankReconcilationDetailID = DetailID++;
                    BRD.Insert(sqltrans);
                }


                #endregion

                #region // BankReconcilationSub_Detail

                short SubDetailID = short.Parse(BankReconcilationSubDetail.GetNewBankReconcilationSubDetailID(this.BankReconcilationID, this.CompanyID, this.PeriodID, sqltrans).ToString());
                foreach (BankReconcilationSubDetail BRSD in colBankReconcilationSubDetailADD)
                {
                    BRSD.BankReconcilationID = this.BankReconcilationID;
                    BRSD.BankReconcilationSubDetailID = SubDetailID++;
                    BRSD.Insert(sqltrans);
                }


                foreach (BankReconcilationSubDetail BRSDUpdate in colBankReconcilationSubDetailUpdate)
                    BRSDUpdate.UpdateDataByKey(sqltrans);


                foreach (BankReconcilationSubDetail BRSDDelete in colBankReconcilationSubDetailDelete)
                    BankReconcilationSubDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, this.BankReconcilationID, BRSDDelete.BankReconcilationSubDetailID, f_CompanyID, f_PeriodID);

                #endregion

              
               


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
                    SqlHelper.RollBackTransaction(sqltrans, "updateRecipe");
            }

            return result;

        }

        public bool DeleteBankReconcilationHeaderWithDetails(List<BankReconcilation_Detail> colBRDDelete, List<BankReconcilationSubDetail> colBRSDDelete, SqlTransaction objTransaction = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteBRD");
            try
            {

                BankReconcilation_Header.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, this.BankReconcilationID);

                foreach (BankReconcilation_Detail BRDDelete in colBRDDelete)
                    BankReconcilation_Detail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, BRDDelete.BankReconcilationDetailID, this.BankReconcilationID);

                foreach (BankReconcilationSubDetail BRSDDelete in colBRSDDelete)
                    BankReconcilationSubDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, BRSDDelete.BankReconcilationSubDetailID, this.BankReconcilationID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteSSP");
            }

            return result;

        }




        public static DataTable DeleteDataByBankReconcilationID(Nullable<int> companyID, Nullable<int> BankReconcilationID, Nullable<int> periodID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@BankReconcilationID", BankReconcilationID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_DeleteDataByBankReconcilationID, sqlparams);

                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeleteDataByBankReconcilationID, sqlparams);

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

        public static DataTable GetAllDataSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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


    }
}
