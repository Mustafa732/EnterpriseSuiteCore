using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
	public partial class AR_InvoiceSupplierDetail : BaseClass
	{
		#region Class Methods

        public DataTable GetSupplierDetailForInvoice(int companyID, int invoiceHeaderID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_SelectSupplierDetailForInvoice = @"
select AR_InvoiceSupplierDetail.CompanyID, AR_InvoiceSupplierDetail.PeriodID, AR_InvoiceSupplierDetail.InvoiceHeaderID, AR_InvoiceSupplierDetail.InvoiceSupplierID, 
    AR_InvoiceSupplierDetail.SupplierID, AR_SET_Customer.CustomerName, AR_InvoiceSupplierDetail.SupplierAccountCode, AR_InvoiceSupplierDetail.BusinessPartnerTypeID, 
    AR_InvoiceSupplierDetail.Amount, AR_InvoiceSupplierDetail.TransactionType, AR_InvoiceSupplierDetail.Narration,AR_InvoiceSupplierDetail.Status, '' as Action,
AR_InvoiceSupplierDetail.StockTransactionID
,AR_InvoiceSupplierDetail.TaxID,AR_InvoiceSupplierDetail.TaxName,AR_InvoiceSupplierDetail.InvoiceAmount
,AR_InvoiceSupplierDetail.SupplierAccountCode + ' - ' + GL_CodeCombinationDetail.Description as CodeDescription
from AR_InvoiceSupplierDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceSupplierDetail.SupplierID --and AR_SET_Customer.CompanyID = AR_InvoiceSupplierDetail.CompanyID 
        and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
    inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_InvoiceSupplierDetail.SupplierAccountCode
where AR_InvoiceSupplierDetail.CompanyID = @CompanyID and AR_InvoiceSupplierDetail.PeriodID = @PeriodID 
    and AR_InvoiceSupplierDetail.InvoiceHeaderID = @InvoiceHeaderID and AR_InvoiceSupplierDetail.Status = 1 
and isnull(AR_InvoiceSupplierDetail.Narration,'') != 'GST'
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSupplierDetailForInvoice, sqlparams);

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

        public DataTable GetSupplierDetailForPayment(int companyID, int invoiceHeaderID, int periodID)
        {
            DataTable dt_Invoices = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_SelectSupplierDetailForPayment = @" 
select AR_InvoiceSupplierDetail.CompanyID, AR_InvoiceSupplierDetail.PeriodID, -1 as PaymentHeaderID, -1 as PaymentSupplierID, AR_InvoiceSupplierDetail.SupplierID, 
    AR_SET_Customer.CustomerName, AR_InvoiceSupplierDetail.SupplierAccountCode, AR_InvoiceSupplierDetail.BusinessPartnerTypeID, 0 as TaxPercentage, 
    AR_InvoiceSupplierDetail.Amount As MaximumAmount, AR_InvoiceSupplierDetail.Amount-ISNUll(InvoicedSupplierPayment.Amount,0) as Amount,  0 as TaxAmount, 'Debit' as TransactionType, 
    AR_InvoiceSupplierDetail.InvoiceHeaderID,Cast(AR_InvoiceSupplierDetail.InvoiceSupplierID as int) as InvoiceSupplierID,
    ('Invoice No: ' + CAST(AR_InvoiceHeader.InvoiceNumber as Varchar(10)) + '. ' + ISNULL(AR_InvoiceSupplierDetail.Narration,'')) as Narration
,0 as InvoiceAmount,'' as TaxName,null as TaxID	,'' as Action 
,GL_CodeCombinationDetail.Description as SupplierAccountCodeDescription
	,null as InvoiceNo
from AR_InvoiceSupplierDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceSupplierDetail.SupplierID --and AR_SET_Customer.CompanyID = AR_InvoiceSupplierDetail.CompanyID 
        and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
    inner join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceSupplierDetail.InvoiceHeaderID 
        and AR_InvoiceHeader.CompanyID = AR_InvoiceSupplierDetail.CompanyID and AR_InvoiceHeader.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_InvoiceSupplierDetail.SupplierAccountCode
LEft JOIN (Select InvoiceHeaderID,InvoiceSupplierID,AP_PaymentSupplierDetail.CompanyID,AP_PaymentSupplierDetail.PeriodID	,IsNull(Sum(Case When AP_PaymentSupplierDetail.TransactionType = 'Debit' Then AP_PaymentSupplierDetail.Amount 
        Else AP_PaymentSupplierDetail.Amount * -1 End), 0) As Amount  from AP_PaymentSupplierDetail

        INNER JOIN AP_PaymentHeader ON AP_PaymentHeader.CompanyID = AP_PaymentSupplierDetail.CompanyID AND AP_PaymentHeader.PeriodID = AP_PaymentSupplierDetail.PeriodID
		AND AP_PaymentHeader.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID 

		Where AP_PaymentSupplierDetail.Status=1 AND AP_PaymentHeader.ReversalComments IS NULL AND AP_PaymentHeader.ReversalDate IS NULL

		group by InvoiceHeaderID,InvoiceSupplierID,AP_PaymentSupplierDetail.CompanyID,AP_PaymentSupplierDetail.PeriodID	 ) InvoicedSupplierPayment
		ON InvoicedSupplierPayment.invoiceHeaderID=AR_InvoiceSupplierDetail.InvoiceHeaderID
		AND InvoicedSupplierPayment.InvoiceSupplierID=AR_InvoiceSupplierDetail.InvoiceSupplierID
		AND InvoicedSupplierPayment.CompanyID=AR_InvoiceSupplierDetail.CompanyID
		AND InvoicedSupplierPayment.PeriodID=AR_InvoiceSupplierDetail.PeriodID


where AR_InvoiceSupplierDetail.CompanyID = @CompanyID and AR_InvoiceSupplierDetail.PeriodID = @PeriodID 
and AR_InvoiceSupplierDetail.Status = 1
    and AR_InvoiceSupplierDetail.InvoiceHeaderID = @InvoiceHeaderID and AR_InvoiceSupplierDetail.TransactionType = 'Credit';

Select AP_PaymentSupplierDetail.SupplierID,AP_PaymentSupplierDetail.InvoiceHeaderID,AP_PaymentSupplierDetail.InvoiceSupplierID, AP_PaymentSupplierDetail.SupplierAccountCode, AP_PaymentSupplierDetail.BusinessPartnerTypeID, 
	IsNull(Sum(Case When AP_PaymentSupplierDetail.TransactionType = 'Debit' Then AP_PaymentSupplierDetail.Amount 
        Else AP_PaymentSupplierDetail.Amount * -1 End), 0) As Amount 
		,AP_PaymentSupplierDetail.InvoiceNumber as InvoiceNo
From AP_PaymentHeader 
	Inner Join AP_PaymentSupplierDetail On AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID 
		And AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID And AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
	Inner Join AP_PaymentInvoicesDetail On AP_PaymentInvoicesDetail.CompanyID = AP_PaymentHeader.CompanyID 
		And AP_PaymentInvoicesDetail.PeriodID = AP_PaymentHeader.PeriodID And AP_PaymentInvoicesDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
        And AP_PaymentInvoicesDetail.Status = 1 And AP_PaymentInvoicesDetail.InvoiceHeaderID = @InvoiceHeaderID 
Where AP_PaymentHeader.CompanyID = @CompanyID And AP_PaymentHeader.PeriodID = @PeriodID 
Group By AP_PaymentSupplierDetail.SupplierID, AP_PaymentSupplierDetail.SupplierAccountCode, AP_PaymentSupplierDetail.BusinessPartnerTypeID 
,AP_PaymentSupplierDetail.InvoiceHeaderID,AP_PaymentSupplierDetail.InvoiceSupplierID,AP_PaymentSupplierDetail.InvoiceNumber";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSupplierDetailForPayment, sqlparams);

                #region Deduct amount on FIFO method

                if (ds != null && ds.Tables != null && ds.Tables.Count == 2 && ds.Tables[0].Rows.Count > 0)
                {
                  
                    DataRow[] rowsToDelete = ds.Tables[0].Select("Amount <= 0");

                    foreach (DataRow rowToDelte in rowsToDelete)
                    {
                        ds.Tables[0].Rows.Remove(rowToDelte);
                        rowToDelte.Delete();
                    }
                }

                ds.Tables[0].AcceptChanges();

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt_Invoices = ds.Tables[0];

                #endregion
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt_Invoices;
        }

        public static bool DeletePermenantDataByKeyInvoiceID(SqlTransaction objTransaction, int companyID, int periodID, int InvoiceHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);


                const string Query_Delete_PermenantDataByKey = @" delete from AR_InvoiceSupplierDetail where InvoiceHeaderID = @InvoiceHeaderID and Narration='GST' and CompanyID = @CompanyID";

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

        public static bool DeleteDataByKeyInvoiceHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int invoiceHeaderID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_Delete_ByKeyInvoiceHeaderID = @" Update [AR_InvoiceSupplierDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [InvoiceHeaderID] = @InvoiceHeaderID and [PeriodID] = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKeyInvoiceHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKeyInvoiceHeaderID, sqlparams) > 0)
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

        public DataTable GetSupplierDetailForInvoice_ForOldDoc(int companyID, int invoiceHeaderID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_SelectSupplierDetailForInvoice = @"
select AR_InvoiceSupplierDetail.CompanyID, AR_InvoiceSupplierDetail.PeriodID, AR_InvoiceSupplierDetail.InvoiceHeaderID, AR_InvoiceSupplierDetail.InvoiceSupplierID, 
    AR_InvoiceSupplierDetail.SupplierID, AR_SET_Customer.CustomerName, AR_InvoiceSupplierDetail.SupplierAccountCode, AR_InvoiceSupplierDetail.BusinessPartnerTypeID, 
    AR_InvoiceSupplierDetail.Amount, AR_InvoiceSupplierDetail.TransactionType, AR_InvoiceSupplierDetail.Narration,AR_InvoiceSupplierDetail.Status, 'Add' as Action,
AR_InvoiceSupplierDetail.StockTransactionID
,AR_InvoiceSupplierDetail.TaxID,AR_InvoiceSupplierDetail.TaxName,AR_InvoiceSupplierDetail.InvoiceAmount
,GL_CodeCombinationDetail.Description as CodeDescription
from AR_InvoiceSupplierDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceSupplierDetail.SupplierID --and AR_SET_Customer.CompanyID = AR_InvoiceSupplierDetail.CompanyID 
        and AR_SET_Customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID 
inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_InvoiceSupplierDetail .SupplierAccountCode
where AR_InvoiceSupplierDetail.CompanyID = @CompanyID and AR_InvoiceSupplierDetail.PeriodID = @PeriodID 
    and AR_InvoiceSupplierDetail.InvoiceHeaderID = @InvoiceHeaderID and AR_InvoiceSupplierDetail.Status = 1 
and isnull(AR_InvoiceSupplierDetail.Narration,'') != 'GST'
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectSupplierDetailForInvoice, sqlparams);

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
