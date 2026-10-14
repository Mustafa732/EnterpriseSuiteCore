using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_Payment_AccountsDetail : BaseClass
    {
        #region Class Methods

        public DataTable GetPaymentAccountsDetail(int companyID, int paymentHeaderID, int periodID,int CurrentFiscalID, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            string Query_GetPaymentAccountsDetail = @" 
SELECT AP_Payment_AccountsDetail.CompanyID, AP_Payment_AccountsDetail.PeriodID, AP_Payment_AccountsDetail.PaymentHeaderID, 
    AP_Payment_AccountsDetail.PaymentDetailAccountID, AP_Payment_AccountsDetail.ChartOfAccountID, AP_Payment_AccountsDetail.CodeCombinationID, 
    GL_CodeCombinationDetail.Description, AP_Payment_AccountsDetail.DepartmentID, SET_Department.DepartmentName, AP_Payment_AccountsDetail.DocumentReference, 
    AP_Payment_AccountsDetail.Narration, AP_Payment_AccountsDetail.CostCenterID, SET_CostCenter.CostCenterName, AP_Payment_AccountsDetail.IncurredOnID, 
    INV_Item.Description as IncurredDescription, AP_Payment_AccountsDetail.Amount, AP_Payment_AccountsDetail.TransactionType, ISNULL(IsBP, 0) as IsBP, 
    '' as TaxPercentage, '' As Action 
FROM AP_Payment_AccountsDetail 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = AP_Payment_AccountsDetail.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = AP_Payment_AccountsDetail.DepartmentID 
    LEFT JOIN INV_Item ON INV_Item.ItemID = AP_Payment_AccountsDetail.IncurredOnID --AND INV_Item.CompanyID = AP_Payment_AccountsDetail.CompanyID 
    LEFT JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.ChartOfAccountID = AP_Payment_AccountsDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = AP_Payment_AccountsDetail.CodeCombinationID 
WHERE AP_Payment_AccountsDetail.PaymentHeaderID = @PaymentHeaderID AND AP_Payment_AccountsDetail.CompanyID = @CompanyID 
    AND AP_Payment_AccountsDetail.PeriodID = @PeriodID and AP_Payment_AccountsDetail.Status = 1 
and AP_Payment_AccountsDetail.FiscalID  = @CurrentFiscalID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPaymentAccountsDetail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetPaymentAccountsDetail, sqlparams);

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

        public DataTable GetPaymentAccountsDetail_RSM(int companyID, int paymentHeaderID, int periodID, int CurrentFiscalID, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dt = null;

            string Query_GetPaymentAccountsDetail = @" 
SELECT AP_Payment_AccountsDetail.CompanyID, AP_Payment_AccountsDetail.PeriodID, AP_Payment_AccountsDetail.PaymentHeaderID, 
    AP_Payment_AccountsDetail.PaymentDetailAccountID, AP_Payment_AccountsDetail.ChartOfAccountID, AP_Payment_AccountsDetail.CodeCombinationID, 
    GL_CodeCombinationDetail.Description, AP_Payment_AccountsDetail.DepartmentID, SET_Department.DepartmentName, AP_Payment_AccountsDetail.DocumentReference, 
    AP_Payment_AccountsDetail.Narration, AP_Payment_AccountsDetail.CostCenterID, SET_CostCenter.CostCenterName, AP_Payment_AccountsDetail.IncurredOnID, 
    INV_Item.Description as IncurredDescription, AP_Payment_AccountsDetail.Amount, AP_Payment_AccountsDetail.TransactionType, ISNULL(IsBP, 0) as IsBP, 
    '' as TaxPercentage, AP_Payment_AccountsDetail.TaxCalculationAmount, AR_SET_Tax.TaxID, AR_SET_Tax.TaxName, '' As Action 
FROM AP_Payment_AccountsDetail 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = AP_Payment_AccountsDetail.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = AP_Payment_AccountsDetail.DepartmentID 
    LEFT JOIN INV_Item ON INV_Item.ItemID = AP_Payment_AccountsDetail.IncurredOnID --AND INV_Item.CompanyID = AP_Payment_AccountsDetail.CompanyID 
    LEFT JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.ChartOfAccountID = AP_Payment_AccountsDetail.ChartOfAccountID 
        AND GL_CodeCombinationDetail.CodeCombinationID = AP_Payment_AccountsDetail.CodeCombinationID 
	LEFT JOIN AR_SET_Tax ON AR_SET_Tax.TaxID = AP_Payment_AccountsDetail.TaxID
	AND AR_SET_Tax.CompanyID = AP_Payment_AccountsDetail.CompanyID AND AR_SET_Tax.PeriodID = AP_Payment_AccountsDetail.PeriodID AND AR_SET_Tax.Status = 1
WHERE AP_Payment_AccountsDetail.PaymentHeaderID = @PaymentHeaderID AND AP_Payment_AccountsDetail.CompanyID = @CompanyID 
    AND AP_Payment_AccountsDetail.PeriodID = @PeriodID and AP_Payment_AccountsDetail.Status = 1 
and AP_Payment_AccountsDetail.FiscalID  = @CurrentFiscalID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPaymentAccountsDetail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetPaymentAccountsDetail, sqlparams);

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

        public static bool DeleteDataByKeyPaymentHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int paymentHeaderID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_Delete_ByKey_PaymentHeaderID = @" Update [AP_Payment_AccountsDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PaymentHeaderID] = @PaymentHeaderID and [PeriodID] = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey_PaymentHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey_PaymentHeaderID, sqlparams) > 0)
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
