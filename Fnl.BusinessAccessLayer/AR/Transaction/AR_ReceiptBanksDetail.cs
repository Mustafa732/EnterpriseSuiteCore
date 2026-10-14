using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptBanksDetail
    {
        #region Class Methods

        public DataTable GetReceiptBanksData(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> receiptHeaderID, SqlConnection objConnection = null, 
            SqlTransaction objTransaction = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);

                #region Query

                string Query_GetReceiptBanksData = @" 
SELECT AR_ReceiptBanksDetail.CompanyID, AR_ReceiptBanksDetail.PeriodID, AR_ReceiptBanksDetail.ReceiptHeaderID, AR_ReceiptBanksDetail.ReceiptBankID, 
    AR_ReceiptBanksDetail.BankBranchID, AR_ReceiptBanksDetail.BankAccountID, AR_ReceiptBanksDetail.ReceiptMethodID, AR_ReceiptBanksDetail.InstrumentNo, 
    AR_ReceiptBanksDetail.InstrumentDate, AR_ReceiptBanksDetail.CurrencyID, AR_ReceiptBanksDetail.Amount, AR_ReceiptBanksDetail.Comments, 
    AR_ReceiptBanksDetail.Status, AR_ReceiptBanksDetail.CreatedBy, AR_ReceiptBanksDetail.CreateDate, AR_ReceiptBanksDetail.CreatedByValue, 
    AR_ReceiptBanksDetail.UpdatedBy, AR_ReceiptBanksDetail.UpdateDate, AR_ReceiptBanksDetail.UpdatedByValue, AR_ReceiptBanksDetail.TransactionType, 
    SET_ReceiptMethod.ReceiptMethodName, AR_SET_BankBranch.BankBranchName, AR_SET_BankAccount.BankAccountName, SET_Currency.CurrencyName,
AR_ReceiptBanksDetail.DepositorName,AR_ReceiptBanksDetail.DepositorCNIC,AR_ReceiptBanksDetail.DepositorNTN,AR_ReceiptBanksDetail.DepositorCity,
AR_ReceiptBanksDetail.SheetNo,AR_ReceiptBanksDetail.FAX ,AR_ReceiptBanksDetail.CostCenterID, 
       AR_ReceiptBanksDetail.departmentID ,'' as Action 
, AR_SET_BankAccount.PaymentCCID as BankAccountCode,GL_CodeCombinationDetail.Description as BankAccountCodeDescription

FROM AR_ReceiptBanksDetail 
    Inner join SET_ReceiptMethod on SET_ReceiptMethod.ReceiptMethodID = AR_ReceiptBanksDetail.ReceiptMethodID 
    Inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID = AR_ReceiptBanksDetail.BankBranchID 
    Inner Join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID = AR_ReceiptBanksDetail.BankAccountID 
and AR_SET_BankAccount.BankBranchID = AR_ReceiptBanksDetail.BankBranchID
--        and AR_SET_BankAccount.CompanyID = AR_ReceiptBanksDetail.CompanyID 
and AR_SET_BankAccount.PeriodID = AR_ReceiptBanksDetail.PeriodID 
    Inner join SET_Currency on SET_Currency.CurrencyID = AR_ReceiptBanksDetail.CurrencyID
left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_SET_BankAccount.PaymentCCID 
WHERE AR_ReceiptBanksDetail.Status = 1 AND AR_ReceiptBanksDetail.CompanyID = @CompanyID AND AR_ReceiptBanksDetail.PeriodID = @PeriodID 
    AND AR_ReceiptBanksDetail.ReceiptHeaderID = @ReceiptHeaderID ";

                #endregion

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetReceiptBanksData, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetReceiptBanksData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetReceiptBanksData, sqlparams);

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

        public static DataTable GetBankDetailsForDuplicate(Nullable<int> companyID, Nullable<int> periodID, string BankBranchID, string InstrumentNo)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BankBranchID", BankBranchID);
                sqlparams[3] = new SqlParameter("@InstrumentNo", InstrumentNo);

                DataSet ds = null;

                #region Query

                string Query_GetReceiptBanksData = @" 
Select * from AR_ReceiptBanksDetail where CompanyID = @CompanyID And PeriodID = @PeriodID And Status = 1 And BankBranchID = @BankBranchID and InstrumentNo = @InstrumentNo";

                #endregion


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetReceiptBanksData, sqlparams);

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


        public static short GetNewRecieptBankID()
        {
            short id = -1;
            DataTable dt;
            DataSet ds;

            string Query = @"
select  isnull( max (ReceiptBankID), 0)  + 1 as ID
 from AR_ReceiptBanksDetail 
";
            ds = SqlHelper.ExecuteDataset(CommandType.Text, Query);

            if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
            {
                dt = ds.Tables[0];
                id = short.Parse(dt.Rows[0]["ID"].ToString());
            }
            return id;
        }

        public static bool DeleteDataByReceiptTransactionHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int receiptHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_Delete_ByReceiptTransactionHeaderID = @" Update [AR_ReceiptBanksDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReceiptHeaderID] = @ReceiptHeaderID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByReceiptTransactionHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByReceiptTransactionHeaderID, sqlparams) > 0)
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

        public static bool DeleteDataByReceiptHeaderID(int companyID, int periodID, int transactionID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", transactionID);

                const string Query_Delete_ByHeaderID = @" Delete from AR_ReceiptBanksDetail where CompanyID = @CompanyID and PeriodID = @PeriodID and  ReceiptHeaderID = @ReceiptHeaderID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderID, sqlparams) > 0)
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

        #endregion
    }
}
