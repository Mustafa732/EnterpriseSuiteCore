using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PaymentInvoicesDetail : BaseClass
    {
        #region Fields

        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #endregion

        #region Class Methods

        public static DataTable GetPOHeaderNumber(int companyID, int periodID, long DocumentNo)
        {
            DataTable dt = null;

            #region Query

            string Query_GetPOHeaderNumber = @" Select AP_PurchaseOrderHeader.POHeaderID From AR_InvoiceDetail 
    Inner JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
    Inner Join INV_StockTransactionDetail ON AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        AND AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID AND AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND INV_StockTransactionDetail.Status = 1 
    Inner Join AP_PurchaseOrderDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderDetail.Status = 1 
    Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        AND AP_PurchaseOrderDetail.Status = 1 
Where AR_InvoiceHeader.InvoiceNumber = @Doc AND AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID and AR_InvoiceHeader.Status = 1 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", DocumentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPOHeaderNumber, sqlparams);

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

        public static DataTable GetPOHeaderNumberEdit(int companyID, int periodID, long DocumentNo)
        {
            DataTable dt = null;

            #region Query

            string Query_GetPOHeaderNumberEdit = @" Select AP_PurchaseOrderHeader.POHeaderID From AR_InvoiceDetail 
    Inner JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
    Inner Join INV_StockTransactionDetail ON AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        AND AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID AND AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND INV_StockTransactionDetail.Status = 1 
    Inner Join AP_PurchaseOrderDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderDetail.Status = 1 
    Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        AND AP_PurchaseOrderDetail.Status = 1 
Where AR_InvoiceHeader.InvoiceID = @Doc AND AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.Status = 1

";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", DocumentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPOHeaderNumberEdit, sqlparams);

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

        public decimal Get_PaymentAmount_Against_PaymentDirectInvoice(int companyID, int periodID, int purchaseInvoiceHeaderID)
        {
            decimal amount = 0;

            string Query_Get_PaymentAmount_Against_PaymentDirectInvoice = @"
Select ISNull(Sum(AP_PaymentInvoicesDetail.Amount), 0) As Amount 
From AP_PaymentInvoicesDetail 
    Inner Join AP_PaymentHeader ON AP_PaymentInvoicesDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
        And AP_PaymentInvoicesDetail.CompanyID = AP_PaymentHeader.CompanyID And AP_PaymentInvoicesDetail.PeriodID = AP_PaymentHeader.PeriodID 
        And AP_PaymentHeader.Status = 1 
    Inner Join AR_InvoiceHeader On AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID 
        and AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID and AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID 
        and AR_InvoiceHeader.Status = 1 
Where AR_InvoiceHeader.InvoiceID = @InvoiceHeaderID AND AP_PaymentInvoicesDetail.CompanyID = @CompanyID And AP_PaymentInvoicesDetail.PeriodID = @PeriodID 
    And AP_PaymentInvoicesDetail.Status = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", purchaseInvoiceHeaderID);

                amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_PaymentAmount_Against_PaymentDirectInvoice, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amount;
        }

        public DataTable GetDataByKeyFromInvoice(int companyID, int periodID, int paymentHeaderID)
        {
            DataTable dt = null;

            string Query_GetData_byKey_FromInvoice = FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_byKey_FromInvoice;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_FromInvoice, sqlparams);

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

        public DataTable GetDataByKeyFromInvoiceForAlHaj(int companyID, int periodID, int paymentHeaderID)
        {
            DataTable dt = null;

            string Query_GetData_byKey_FromInvoiceforAlHaj = @" 
SELECT ROW_NUMBER() OVER (ORDER BY AR_InvoiceHeader.InvoiceID asc)RowUniqueID, AP_PaymentInvoicesDetail.PaymentInvoicesID, AP_PaymentInvoicesDetail.PaymentHeaderID, AP_PaymentInvoicesDetail.InvoiceHeaderID, 
    AR_InvoiceHeader.InvoiceNumber AS InvoiceNo, AR_InvoiceHeader.InvoiceDate, AR_InvoiceHeader.AmountPaid AS InvoiceAmount, 
    AR_InvoiceHeader.AmountPaid AS BalanceAmount, AP_PaymentInvoicesDetail.Amount, 
(Select SUM(TaxAmount)TaxAmount From AP_PaymentTaxes Where PaymentInvoiceID = PaymentInvoicesID AND PaymentHeaderID = AP_PaymentInvoicesDetail.PaymentHeaderID )TaxAmount,
'' AS Action
FROM AP_PaymentInvoicesDetail
INNER JOIN AR_InvoiceHeader ON AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
	AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
	AND AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND AR_InvoiceHeader.Status = 1
WHERE AP_PaymentInvoicesDetail.CompanyID = @CompanyID AND AP_PaymentInvoicesDetail.PeriodID = @PeriodID AND AP_PaymentInvoicesDetail.Status = 1
	AND AP_PaymentInvoicesDetail.PaymentHeaderID = @PaymentHeaderID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_FromInvoiceforAlHaj, sqlparams);

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
        public DataTable GetDataByKeyFromInvoice_AlImdad(int companyID, int periodID, int paymentHeaderID)
        {
            DataTable dt = null;

            string Query_GetData_byKey_FromInvoice = @" 
SELECT distinct -1 as RowUniqueID, AP_PaymentInvoicesDetail.PaymentInvoicesID, AP_PaymentInvoicesDetail.PaymentHeaderID, AP_PaymentInvoicesDetail.InvoiceHeaderID, 
  CASE WHEN  SET_DocumentType.DocumentType like '%Custom Clearing%'  then IMP_CustomClearing.CustomClearingID when  
   SET_DocumentType.DocumentType like '%Transportation%'  then Set_ContainerTransportationandUnloading.ContainerTransportationID   WHEN  SET_DocumentType.DocumentType like '%Loading%UnLoading%'  then Fabrics_LoadingAndUnloading.LoadingUnloadingID   Else
    AR_InvoiceHeader.InvoiceNumber end AS InvoiceNo, CASE WHEN  SET_DocumentType.DocumentType like '%Custom Clearing%'  then IMP_CustomClearing.CCDate when  
   SET_DocumentType.DocumentType like '%Transportation%'  then Set_ContainerTransportationandUnloading.CreateDate   WHEN  SET_DocumentType.DocumentType like '%Loading%UnLoading%'  then Fabrics_LoadingAndUnloading.CreateDate   Else
    AR_InvoiceHeader.InvoiceDate end AS InvoiceDate,-- AP_PaymentInvoicesDetail.Amount AS InvoiceAmount, 
 --AR_InvoiceHeader.AmountPaid AS InvoiceAmount,
 ISNUll(  CASE WHEN  SET_DocumentType.DocumentType like '%Custom Clearing%' then  (Select  isnull(SUM(IMP_CustomClearing.TotalInvoiceAmount),0) 
from IMP_CustomClearing
		 Where  IMP_CustomClearing.CustomClearingID=AP_PaymentInvoicesDetail.InvoiceHeaderID) WHEN    SET_DocumentType.DocumentType like '%Transportation%' then  (Select  isnull(SUM(Set_ContainerTransportationandUnloading.TotalTransAmount),0) 
from Set_ContainerTransportationandUnloading
		 Where  Set_ContainerTransportationandUnloading.ContainerTransportationID=AP_PaymentInvoicesDetail.InvoiceHeaderID)
	WHEN  SET_DocumentType.DocumentType like '%Loading%UnLoading%' then  (Select  isnull(SUM(Fabrics_LoadingAndUnloading.TotalUnloadAmount),0) 
from Fabrics_LoadingAndUnloading
		 Where  Fabrics_LoadingAndUnloading.LoadingUnloadingID=AP_PaymentInvoicesDetail.InvoiceHeaderID) 	 
		  else AR_InvoiceHeader.AmountPaid end,0) InvoiceAmount,
		   ISNUll(ISNUll(  CASE WHEN  SET_DocumentType.DocumentType like '%Custom Clearing%' then  (Select  isnull(SUM(IMP_CustomClearing.TotalInvoiceAmount),0) 
from IMP_CustomClearing
		 Where  IMP_CustomClearing.CustomClearingID=AP_PaymentInvoicesDetail.InvoiceHeaderID) WHEN    SET_DocumentType.DocumentType like '%Transportation%' then  (Select  isnull(SUM(Set_ContainerTransportationandUnloading.TotalTransAmount),0) 
from Set_ContainerTransportationandUnloading
		 Where  Set_ContainerTransportationandUnloading.ContainerTransportationID=AP_PaymentInvoicesDetail.InvoiceHeaderID)
	WHEN  SET_DocumentType.DocumentType like '%Loading%UnLoading%' then  (Select  isnull(SUM(Fabrics_LoadingAndUnloading.TotalUnloadAmount),0) 
from Fabrics_LoadingAndUnloading
		 Where  Fabrics_LoadingAndUnloading.LoadingUnloadingID=AP_PaymentInvoicesDetail.InvoiceHeaderID) 	 
		  else AR_InvoiceHeader.AmountPaid end,0),0) + ISNULL(FreightAmount,0)  - ISNULL(OtherAmount,0)  
- isnull( (
            select distinct top 1 isnull(GL_TransactionDetail.Amount, 0) from AR_InvoiceHeader AR 
                inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR.invoiceType and set_DocumentType.DocumentType = 'Debit Note' 
                inner join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = AR.InvoiceType and GL_TransactionHeader.CompanyID = ar.CompanyID 
                    and GL_TransactionHeader.PeriodID = ar.PeriodID and GL_TransactionHeader.DocumentNo = AR.InvoiceNumber 
                inner join GL_TransactionDetail on GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID 
                    and GL_TransactionDetail.CompanyID = ar.CompanyID and GL_TransactionDetail.PeriodID = ar.PeriodID 
                    and GL_TransactionDetail.TransactionType = 'Debit' 
            where AR.VendorID = AR_InvoiceHeader.VendorID
        ) , 0) 
---- Already Paid (Partial Case)
        -
        (
        select isnull(sum(PaymentInvoiceDetail.Amount),0) 
		from AP_PaymentInvoicesDetail  PaymentInvoiceDetail
		inner join Ap_paymentHeader inv_hdr on inv_hdr.PaymentID = PaymentInvoiceDetail.PaymentHeaderID
        AND inv_hdr.Status=1
		and inv_hdr.CompanyID = PaymentInvoiceDetail.CompanyID
		and inv_hdr.PeriodID = PaymentInvoiceDetail.PeriodID
		and inv_hdr.ReversalDate is null and inv_hdr.ReversalComments is null
    	--AND MenuID in (Select MenuID from ApplicationMenu where MenuPath in ('/AP/Transaction/Payment.aspx','/AP/Transaction/DirectInvoicePayment.aspx'))
		
where PaymentInvoiceDetail.InvoiceHeaderID = AP_PaymentInvoicesDetail.InvoiceHeaderID
AND PaymentInvoiceDetail.SourceTypeID=AP_PaymentInvoicesDetail.SourceTypeID
    and PaymentInvoiceDetail.CompanyID = AP_PaymentInvoicesDetail.CompanyID
    and PaymentInvoiceDetail.PeriodID = AP_PaymentInvoicesDetail.PeriodID
        )
- (select  isnull(sum(Amount),0) from AR_Invoice_AccountsDetail accDtl 
where accDtl.InvoiceHeaderID = AR_Invoice_AccountsDetail.InvoiceHeaderID 
and accDtl.Status = 1
and accDtl.TransactionType = 'Credit'
) + AP_PaymentInvoicesDetail.Amount
  as BalanceAmount

	,AP_PaymentInvoicesDetail.Amount, 0 as TaxAmount, AP_PaymentInvoicesDetail.SourceTypeID
	 ,'' AS Action
FROM AP_PaymentInvoicesDetail
LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID=ISNULL(AP_PaymentInvoicesDetail.SourceTypeID,0)
AND SET_DocumentType.Status=1
LEFT JOIN AR_InvoiceHeader ON AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
	AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
	AND AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID
		AND (AR_InvoiceHeader.InvoiceType = AP_PaymentInvoicesDetail.SourceTypeID  OR  AP_PaymentInvoicesDetail.SourceTypeID iS null)
	AND AR_InvoiceHeader.Status = 1
	left join AR_Invoice_AccountsDetail on AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
			and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
			and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
	LEFT JOIN IMP_CustomClearing ON IMP_CustomClearing.CustomClearingID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND IMP_CustomClearing.Status=1
LEFT JOIN Set_ContainerTransportationandUnloading ON Set_ContainerTransportationandUnloading.ContainerTransportationID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND Set_ContainerTransportationandUnloading.Status = 1
	LEFT JOIN Fabrics_LoadingAndUnloading ON Fabrics_LoadingAndUnloading.LoadingUnloadingID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND AR_InvoiceHeader.Status = 1


WHERE AP_PaymentInvoicesDetail.CompanyID = @CompanyID AND AP_PaymentInvoicesDetail.PeriodID = @PeriodID 
    AND AP_PaymentInvoicesDetail.PaymentHeaderID = @PaymentHeaderID AND AP_PaymentInvoicesDetail.Status = 1 
--and AR_InvoiceHeader.FiscalID = AP_PaymentInvoicesDetail.FiscalID  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", paymentHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_FromInvoice, sqlparams);

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

        public static DataTable GetDataPaymentAgainstSupplyOfGoodsReport(string companyID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int? TaxPayerID = 0, int PeriodID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                companyID = "(" + companyID + ")";

                DataSet ds = null;

                if (TaxPayerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = " + TaxPayerID.ToString());
                }


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_PaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_PaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_PaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetDataBTLPaymentAgainstSupplyOfGoodsReport(string companyID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int? TaxPayerID = 0, int PeriodID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                companyID = "(" + companyID + ")";

                if (TaxPayerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = " + TaxPayerID.ToString());
                }


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_BTLPaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_BTLPaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetData_BTLPaymentAgainstSupplyOfGoodsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }


        public static DataTable GetAllDataForPaymentsAgainstContracts_Report(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }
                companyID = "(" + companyID + ")";
                //dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_BTLPaymentsAgainstContracts,additionalParameters), sqlparams).Tables[0];
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ForPaymentsAgainstContracts + additionalParameters, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable GetDataForReportonPaymentsagainstInterest(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ForReportonPaymentsagainstInterest + additionalParameters, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable GetDataForReportOnPaymentsAgainstDividend(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ForReportOnPaymentsAgainstDividend + additionalParameters, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable GetDataForReportOnDeductionAgainstSaleReceipts(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ForReportOnDeductionAgainstSaleReceipts + additionalParameters, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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



        public static DataTable ReportOnDeductionAgainstReceiptsOfAuctionSale(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_ForReportOnDeductionAgainstReceiptsOfAuctionSale + additionalParameters, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable GetDataReportOnForeignPaymentsReport(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int? TaxPayerID = 0, int CompanyID = 0, int PeriodID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                if (TaxPayerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = " + TaxPayerID.ToString());
                }


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnForeignPaymentsReport, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnForeignPaymentsReport, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnForeignPaymentsReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetDataReportOnPaymentsAgainstRentReport(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int? TaxPayerID = 0, string CompanyID = null, int PeriodID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                if (TaxPayerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = " + TaxPayerID.ToString());
                }

                CompanyID = "(" + CompanyID + ")";
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstRentReport, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstRentReport, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstRentReport, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetDataReportOnPaymentsAgainstBrokerageCommissionReport(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int? TaxPayerID = 0, string CompanyID = null, int PeriodID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                DataSet ds = null;

                if (TaxPayerID != null)
                {
                    sb.Append("and AR_SET_Customer.CustomerID = " + TaxPayerID.ToString());
                }

                CompanyID = "(" + CompanyID + ")";
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstBrokerageCommission, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstBrokerageCommission, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_GetData_ReportOnPaymentsAgainstBrokerageCommission, sb.ToString()).Replace("@CompanyID", CompanyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
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

                const string Query_Delete_ByKey_PaymentHeaderID = @" Update [AP_PaymentInvoicesDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PaymentHeaderID] = @PaymentHeaderID and [PeriodID] = @PeriodID ";

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
