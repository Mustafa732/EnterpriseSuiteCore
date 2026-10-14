using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Setup
{
    public partial class AR_SET_BankAccountDetail : BaseClass
    {
        public DataTable GetDataByKeyWithOtherTables(int BankAccountID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BankAccountID", BankAccountID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_GetDataByKey = @" 
Select AR_SET_BankAccountDetail.CompanyID, AR_SET_BankAccountDetail.PeriodID, AR_SET_BankAccountDetail.BankAccountDetailID, AR_SET_BankAccountDetail.BankAccountID, 
    Convert(Varchar(10), AR_SET_BankAccountDetail.DateValidForm, 105) AS DateValidForm, AR_SET_BankAccountDetail.FromNo, AR_SET_BankAccountDetail.ToNo, 
    AR_SET_BankAccountDetail.Active, AR_SET_BankAccountDetail.Status, AR_SET_BankAccountDetail.UpdatedBy, AR_SET_BankAccountDetail.UpdateDate, 
    AR_SET_BankAccountDetail.UpdatedByValue, AR_SET_BankAccountDetail.CreatedBy, AR_SET_BankAccountDetail.CreateDate, AR_SET_BankAccountDetail.CreatedByValue, 
    AR_SET_BankAccount.CompanyID, AR_SET_BankAccount.PeriodID, AR_SET_BankAccount.BankAccountID, AR_SET_BankAccount.BankAccountName, 
    AR_SET_BankAccount.BankAccountNameALT, AR_SET_BankAccount.BankAccountNumber, AR_SET_BankAccount.BankBranchID, AR_SET_BankAccount.AccountHolderName, 
    AR_SET_BankAccount.AccountHolderNameALT, AR_SET_BankAccount.BankAccountTypeID, AR_SET_BankAccount.MultiCurrency, AR_SET_BankAccount.CurrencyID, 
    AR_SET_BankAccount.Description, AR_SET_BankAccount.InActiveDate, AR_SET_BankAccount.MinCheckAmount, AR_SET_BankAccount.MaxCheckAmount, 
    AR_SET_BankAccount.ChartofAccountID, AR_SET_BankAccount.BankChargesCCID, AR_SET_BankAccount.BankErrorsCCID, AR_SET_BankAccount.LossCCID, 
    AR_SET_BankAccount.AssetCCID, AR_SET_BankAccount.ClearingCCID, AR_SET_BankAccount.EarnedCCID, AR_SET_BankAccount.FactorCCID, 
    AR_SET_BankAccount.FutureDatedPaymentCCID, AR_SET_BankAccount.GainCCID, AR_SET_BankAccount.OnAccountCCID, AR_SET_BankAccount.ReceiptClearingCCID, 
    AR_SET_BankAccount.RemittanceCCID, AR_SET_BankAccount.ShortTermDepositCCID, AR_SET_BankAccount.UnAppliedCCID, AR_SET_BankAccount.UnEarnedCCID, 
    AR_SET_BankAccount.UnIdentifiedCCID, AR_SET_BankAccount.BankGLAccountCCID, AR_SET_BankAccount.Status, AR_SET_BankAccount.CreatedBy, AR_SET_BankAccount.CreateDate, 
    AR_SET_BankAccount.UpdatedBy, AR_SET_BankAccount.UpdateDate, AR_SET_BankAccount.CreatedByValue, AR_SET_BankAccount.UpdatedByValue, 
    AR_SET_BankAccount.OperatingType, AR_SET_BankAccount.PaymentCCID, '' AS [Action] 
From AR_SET_BankAccountDetail 
    INNER JOIN AR_SET_BankAccount On AR_SET_BankAccount.BankAccountID = AR_SET_BankAccountDetail.BankAccountID 
        AND AR_SET_BankAccount.CompanyID = AR_SET_BankAccountDetail.CompanyID AND AR_SET_BankAccount.PeriodID = AR_SET_BankAccountDetail.PeriodID 
WHERE AR_SET_BankAccountDetail.BankAccountID = @BankAccountID And AR_SET_BankAccountDetail.PeriodID = @PeriodID 
    AND AR_SET_BankAccountDetail.Status = 1 ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

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

    }
}
