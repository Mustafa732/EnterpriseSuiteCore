using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PaymentSupplierDetail : BaseClass
    {
        #region Methods

        public DataTable GetSupplierDetailForPayment(int companyID, int paymentHeaderID, int periodID, int CurrentFiscalID)
        {
            DataSet ds = null;
            DataTable dt = null;

            string Query_SelectSupplierDetailForPayment = @" 
select AP_PaymentSupplierDetail.CompanyID, AP_PaymentSupplierDetail.PeriodID, AP_PaymentSupplierDetail.PaymentHeaderID, AP_PaymentSupplierDetail.PaymentSupplierID, 
    AP_PaymentSupplierDetail.SupplierID, AR_SET_Customer.CustomerName, AP_PaymentSupplierDetail.SupplierAccountCode, AP_PaymentSupplierDetail.BusinessPartnerTypeID, 
    AP_PaymentSupplierDetail.TaxPercentage, AP_PaymentSupplierDetail.Amount As MaximumAmount, AP_PaymentSupplierDetail.Amount, AP_PaymentSupplierDetail.TaxAmount, 
    AP_PaymentSupplierDetail.TransactionType, AP_PaymentSupplierDetail.Narration,AmountDollar, '' as Action, AP_PaymentSupplierDetail.invoiceHeaderID As InvoiceHeaderID 
	,AP_PaymentSupplierDetail.TaxID,AP_PaymentSupplierDetail.TaxName,AP_PaymentSupplierDetail.InvoiceAmount
,GL_CodeCombinationDetail.CodeCombinationID  + '-' + GL_CodeCombinationDetail.Description as SupplierAccountCodeDescription
,AP_PaymentSupplierDetail.InvoiceNumber as InvoiceNo
from AP_PaymentSupplierDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PaymentSupplierDetail.SupplierID --and AR_SET_Customer.CompanyID = AP_PaymentSupplierDetail.CompanyID 
        and AR_SET_Customer.PeriodID = AP_PaymentSupplierDetail.PeriodID 
left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_PaymentSupplierDetail.SupplierAccountCode
where AP_PaymentSupplierDetail.CompanyID = @CompanyID and AP_PaymentSupplierDetail.PeriodID = @PeriodID 
    and AP_PaymentSupplierDetail.PaymentHeaderID = @PaymentHeaderID And AP_PaymentSupplierDetail.Status = 1 
	and AP_PaymentSupplierDetail.FiscalID= @CurrentFiscalID
and isnull(AP_PaymentSupplierDetail.Narration,'') != 'GST'
  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSupplierDetailForPayment, sqlparams);

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

        public static bool DeleteDataByPaymentID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID,
            int paymentHeaderID, int periodID)
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

                string Query_Delete_ByPaymentID = @" delete from [AP_PaymentSupplierDetail] WHERE [CompanyID] = @CompanyID and [PaymentHeaderID] = @PaymentHeaderID and [PeriodID] = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByPaymentID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByPaymentID, sqlparams) > 0)
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
        public DataTable GetSupplierDetailForDirectPayment(int companyID, int paymentHeaderID, int periodID, int CurrentFiscalID)
        {
            DataSet ds = null;
            DataTable dt = null;

            string Query_SelectSupplierDetailForPayment = @" 

select AP_PaymentSupplierDetail.CompanyID, AP_PaymentSupplierDetail.PeriodID, AP_PaymentSupplierDetail.PaymentHeaderID, AP_PaymentSupplierDetail.PaymentSupplierID, 
    AP_PaymentSupplierDetail.SupplierID, AR_SET_Customer.CustomerName, AP_PaymentSupplierDetail.SupplierAccountCode, AP_PaymentSupplierDetail.BusinessPartnerTypeID, 
    AP_PaymentSupplierDetail.TaxPercentage, AP_PaymentSupplierDetail.Amount As MaximumAmount, AP_PaymentSupplierDetail.Amount, AP_PaymentSupplierDetail.TaxAmount, 
    AP_PaymentSupplierDetail.TransactionType, AP_PaymentSupplierDetail.Narration, '' as Action, AP_PaymentSupplierDetail.invoiceHeaderID As InvoiceHeaderID 
,AP_PaymentSupplierDetail.InvoiceSupplierID
,AP_PaymentSupplierDetail.TaxID,AP_PaymentSupplierDetail.TaxName,AP_PaymentSupplierDetail.InvoiceAmount
,GL_CodeCombinationDetail.Description as SupplierAccountCodeDescription
,AP_PaymentSupplierDetail.InvoiceNumber as InvoiceNo
from AP_PaymentSupplierDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PaymentSupplierDetail.SupplierID --and AR_SET_Customer.CompanyID = AP_PaymentSupplierDetail.CompanyID 
        and AR_SET_Customer.PeriodID = AP_PaymentSupplierDetail.PeriodID 
inner join	GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_PaymentSupplierDetail.SupplierAccountCode
where AP_PaymentSupplierDetail.CompanyID = @CompanyID and AP_PaymentSupplierDetail.PeriodID = @PeriodID 
    and AP_PaymentSupplierDetail.PaymentHeaderID = @PaymentHeaderID And AP_PaymentSupplierDetail.Status = 1 
	and AP_PaymentSupplierDetail.FiscalID= @CurrentFiscalID  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSupplierDetailForPayment, sqlparams);

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

                const string Query_Delete_ByKey_PaymentHeaderID = @" Update [AP_PaymentSupplierDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PaymentHeaderID] = @PaymentHeaderID and [PeriodID] = @PeriodID ";

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

        public static bool DeletePermenantDataByKeyPaymentID(SqlTransaction objTransaction, int companyID, int periodID, int PaymentHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", PaymentHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);


                const string Query_Delete_PermenantDataByKey = @" delete from AP_PaymentSupplierDetail where PaymentHeaderID = @PaymentHeaderID and Narration='GST' and CompanyID = @CompanyID";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
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

        public static bool DeletePermenantDataByKeyPaymentIDForCane(SqlTransaction objTransaction, int companyID, int periodID, int PaymentHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", PaymentHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);


                const string Query_Delete_PermenantDataByKey = @" delete from AP_PaymentSupplierDetail where PaymentHeaderID = @PaymentHeaderID and CompanyID = @CompanyID";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_PermenantDataByKey, sqlparams) > 0)
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
