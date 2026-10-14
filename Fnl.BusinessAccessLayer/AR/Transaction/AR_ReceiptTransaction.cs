using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Sales;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptTransactionHeader : BaseClass
    {
        #region Class Methods

        public DataTable INProcessRecieptReport(int companyID, int periodID, Nullable<int> customerID)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                sb.Append(" and AR_ReceiptTransactionHeader.DocumentTypeID =  127 and AR_ReceiptTransactionHeader.StatusCode = 5 ");

                if (customerID != null)
                    sb.Append(" and AR_ReceiptTransactionHeader.CustomerID  = " + customerID + "");

                #region Query

                string Query_INProcessRecieptReport = @" 
select AR_SET_Customer.CustomerName, SAL_OrderHeader.ContractNo as SO_No, CONVERT(varchar(15), AR_ReceiptTransactionHeader.ReceiptDate, 106) as ReceiptDate, 
    CONVERT(varchar(15), AR_ReceiptTransactionHeader.DocumentDate, 106) as DocumentDate, AR_ReceiptTransactionHeader.Amount as Header_Gross_Amount, 
    AR_ReceiptBanksDetail.InstrumentNo, CONVERT(varchar(15), AR_ReceiptBanksDetail.InstrumentDate, 106 ) as InstrumentDate, AR_ReceiptBanksDetail.Amount, 
    AR_SET_BankBranch.BankBranchName 
from AR_ReceiptTransactionHeader 
	inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
        and AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID and AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        and AR_ReceiptBanksDetail.status = 1 
	inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_ReceiptTransactionHeader.CustomerID 
        --and AR_SET_Customer.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
and AR_SET_Customer.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        and AR_SET_Customer.Status = 1 
	inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID = AR_ReceiptBanksDetail.BankBranchID and AR_SET_BankBranch.Status = 1 
	inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = AR_ReceiptTransactionHeader.SourceHeaderID 
        and SAL_OrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID and SAL_OrderHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        and SAL_OrderHeader.Status = 1 and AR_ReceiptTransactionHeader.status = 1 
Where AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID 
    {0} ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_INProcessRecieptReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dt;
        }

        public DataTable GetReceiptForReceiptLookup(int companyID, int periodID)
        {
            DataTable dtPaymentReceipt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_GetReceiptForReceiptLookup = @" 
select AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptTransactionHeader.DocumentNo, SET_DocumentType.DocumentType, 
    AR_ReceiptTransactionHeader.DocumentDate 
from AR_ReceiptTransactionHeader
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
where (IsInvoiceAdjusted = 0 or IsInvoiceAdjusted is null) And AR_ReceiptTransactionHeader.Status = 1 And AR_ReceiptTransactionHeader.CompanyID = @CompanyID 
    And AR_ReceiptTransactionHeader.PeriodID = @PeriodID 
    and (SET_DocumentType.DocumentType = 'Receipt Without Invoice' Or SET_DocumentType.DocumentType like 'Direct Receipt%') 
    AND AR_ReceiptTransactionHeader.StatusCode = 1 
Order By AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.DocumentDate, SET_DocumentType.DocumentType ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetReceiptForReceiptLookup), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtPaymentReceipt;
        }

        public DataSet GetReceiptReport(int companyID, int periodId, int fiscalID, int? rfqNoFrom, int? rfqNoTo, int? documentTypeID, Nullable<DateTime> fromDate,
            Nullable<DateTime> toDate)
        {
            string additionalFilter = string.Empty;
            DataSet ds1 = new DataSet();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);
                #region Filter by Receipt Number

                if (rfqNoFrom.HasValue && rfqNoTo.HasValue)
                    additionalFilter = " AND AR_ReceiptTransactionHeader.DocumentNo BETWEEN " + rfqNoFrom + " AND " + rfqNoTo;
                else if (rfqNoFrom.HasValue && !rfqNoTo.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] >= " + rfqNoFrom;
                else if (rfqNoTo.HasValue && !rfqNoFrom.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] <= " + rfqNoTo;

                #endregion

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }
                else if (fromDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate >= @FromDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                }
                else if (toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate <= @ToDate ";
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetReceiptDataforReport,
                    additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    ds1 = ds;

                ds.Tables[0].TableName = "SaleOrder";
                ds.Tables[1].TableName = "Table1";
                ds.Tables[2].TableName = "GL_Voucher";
                ds.Tables[3].TableName = "CustomerDTL";
                ds.Tables[4].TableName = "CustomerBankDetail";
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataSet GetSugarSaleAdvanceReport(int companyID, int periodId, int fiscalID, int? rfqNoFrom, int? rfqNoTo, int? documentTypeID, Nullable<DateTime> fromDate,
            Nullable<DateTime> toDate)
        {
            string additionalFilter = string.Empty;
            DataSet ds1 = new DataSet();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);
                #region Filter by Receipt Number

                if (rfqNoFrom.HasValue && rfqNoTo.HasValue)
                    additionalFilter = " AND AR_ReceiptTransactionHeader.DocumentNo BETWEEN " + rfqNoFrom + " AND " + rfqNoTo;
                else if (rfqNoFrom.HasValue && !rfqNoTo.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] >= " + rfqNoFrom;
                else if (rfqNoTo.HasValue && !rfqNoFrom.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] <= " + rfqNoTo;

                #endregion

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }
                else if (fromDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate >= @FromDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                }
                else if (toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate <= @ToDate ";
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetReceiptDataforSugarSaleAdvanceReport,
                    additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    ds1 = ds;

                ds.Tables[0].TableName = "SaleOrder";
                ds.Tables[1].TableName = "Table1";
                ds.Tables[2].TableName = "GL_Voucher";
                ds.Tables[3].TableName = "CustomerBankDetail";
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataSet GetDataForReceiptCollectionDetail(int companyID, int periodId, int fiscalID, int? rfqNoFrom, int? rfqNoTo, int? documentTypeID, Nullable<DateTime> fromDate,
    Nullable<DateTime> toDate, Nullable<int> documentType = null)
        {
            string additionalFilter = string.Empty;
            DataSet ds1 = new DataSet();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);
                #region Filter by Receipt Number

                if (rfqNoFrom.HasValue && rfqNoTo.HasValue)
                    additionalFilter = " AND AR_ReceiptTransactionHeader.DocumentNo BETWEEN " + rfqNoFrom + " AND " + rfqNoTo;
                else if (rfqNoFrom.HasValue && !rfqNoTo.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] >= " + rfqNoFrom;
                else if (rfqNoTo.HasValue && !rfqNoFrom.HasValue)
                    additionalFilter = " AND [AR_ReceiptTransactionHeader].[DocumentNo] <= " + rfqNoTo;
                if (documentType.HasValue)
                    additionalFilter += " AND Set_DocumentType.DocumentTypeID = " + documentType.Value.ToString() + " ";

                #endregion

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }
                else if (fromDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate >= @FromDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                }
                else if (toDate.HasValue)
                {
                    additionalFilter += " AND AR_ReceiptTransactionHeader.ReceiptDate <= @ToDate ";
                    sqlparams[5] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetDataForReceiptCollectionDetail,
                    additionalFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    ds1 = ds;

                ds.Tables[0].TableName = "CustomerBankDetail";
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataTable GetDataByKeyWithOtherTables(Nullable<int> invoiceHeaderID, int periodID, int companyID, Nullable<int> supplierID, string strWhere)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                if (supplierID.HasValue)
                    sqlparams[3] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[3] = new SqlParameter("@SupplierID", DBNull.Value);

                #region Query

                string Query_Select_By_PrimaryKey_WithOtherTables = @" select * from 
(
    SELECT distinct InvoiceNumber as DocumentNumber, InvoiceType, InvoiceDate as DocumentDate, InvoiceID, SET_DocumentType.DocumentType, AR_InvoiceHeader.AmountPaid as InvoiceAmount, 
        ISNull(Sum(Payment.Amount), 0) As Amount, AR_InvoiceHeader.Description,AR_InvoiceHeader.CompanyID,AR_InvoiceHeader.PeriodID
,AR_InvoiceHeader.VendorReferenceNumber    
FROM AR_InvoiceHeader 
	    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType AND SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
            AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID AND SET_DocumentType.Status = 1 
	    INNER JOIN ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID AND ApplicationMenu.MenuPath = '/AR/Transaction/Invoice_Transaction_frm.aspx' 
	    left Join 
		(
			Select AP_PaymentInvoicesDetail.InvoiceHeaderID, AP_PaymentInvoicesDetail.CompanyID, AP_PaymentInvoicesDetail.PeriodID, AP_PaymentInvoicesDetail.Amount 
            From AP_PaymentInvoicesDetail 
		        Inner Join AP_PaymentHeader On AP_PaymentHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID 
                    And AP_PaymentHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID And AP_PaymentHeader.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID 
                    And AP_PaymentHeader.Status = 1 And AP_PaymentHeader.ReversalDate is null
                    And AP_PaymentInvoicesDetail.Status = 1 
		) As Payment On AR_InvoiceHeader.InvoiceID = Payment.InvoiceHeaderID and AR_InvoiceHeader.CompanyID = Payment.CompanyID 
            and AR_InvoiceHeader.PeriodID = Payment.PeriodID 
    WHERE 
--(AR_InvoiceHeader.VendorID = @SupplierID OR @SupplierID is null) 
(AR_InvoiceHeader.InvoiceID in (select InvoiceHeaderId from AR_InvoiceSupplierDetail where SupplierID = @SupplierID) OR @SupplierID is null) 

AND AR_InvoiceHeader.CompanyID = @CompanyID AND AR_InvoiceHeader.PeriodID = @PeriodID 
        and AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.IsPaymentCompleted <> 1 AND AR_InvoiceHeader.StatusCode = 1 
    group by InvoiceNumber, InvoiceType, InvoiceDate, InvoiceID, SET_DocumentType.DocumentType, AR_InvoiceHeader.AmountPaid, AR_InvoiceHeader.Description,AR_InvoiceHeader.CompanyID,AR_InvoiceHeader.PeriodID
,AR_InvoiceHeader.VendorReferenceNumber
) FINTAB 
where FINTAB.InvoiceAmount > FINTAB.Amount ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + strWhere, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetTableInvoice(int periodID, int companyID, int SaleInvoiceHeaderId)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@SaleInvoiceHeaderId", SaleInvoiceHeaderId);

                string Query_Select_By_SaleInvoiceHeaderId = @" 
SELECT InvoiceNumber, SalesInvoiceHeaderId, CustomerID, FreightCode, Set_DocumentType.DocumentTypeID AS InvoiceTypeID, Set_DocumentType.DocumentType As InvoiceType, InvoiceDate, InvoiceID, 
    AmountPaid as InvoiceAmount, GrossAmount, DiscountAmountTaken 
FROM SAL_InvoiceHeader 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID 
        And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID AND Set_DocumentType.Status = 1 
WHERE SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.CompanyID = @CompanyID and SAL_InvoiceHeader.Status = 1 
    and SAL_InvoiceHeader.PaymentStatusType is null AND SAL_InvoiceHeader.IsPaymentGenerated = 0 and SAL_InvoiceHeader.InvoiceID = @SaleInvoiceHeaderId ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_SaleInvoiceHeaderId, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDataByKeyTableInvoice(Nullable<int> invoiceHeaderID, int periodID, int companyID, Nullable<int> customerID, Nullable<int> InvoiceFor = 0, int CurrenctFiscalID = 1)
        {
            DataTable dtToReturn = null;

            #region  Query for income TAX Case
            #endregion

            try
            {
                string Query = "";
                SqlParameter[] sqlparams = new SqlParameter[5];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@CustomerID", customerID);
                sqlparams[4] = new SqlParameter("@CurrenctFiscalID", CurrenctFiscalID);




                if (InvoiceFor == 3)
                    Query = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Select_By_PrimaryKeyInvoice_ForIncomeTAX;
                else
                    Query = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_Select_By_PrimaryKeyInvoice_New;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDataByKeyTableInvoiceLubricant(Nullable<int> invoiceHeaderID, int periodID, int companyID, Nullable<int> customerID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@CustomerID", customerID);

                string Query_Select_By_PrimaryKey_Invoice_Lubricant = @" 
SELECT distinct INV_StockTransactionHeader.DocumentNo as InvoiceNumber, Set_DocumentType.DocumentType As InvoiceType, 
    INV_StockTransactionHeader.DocumentDate As InvoiceDate, INV_StockTransactionHeader.StockTransactionID AS InvoiceID 
FROM INV_StockTransactionHeader 
   inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        and SAL_ReverseDetail.ReverseDetailID = INV_StockTransactionDetail.SourceLineID and SAL_ReverseDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        and SAL_ReverseDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        And Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
where Set_DocumentType.DocumentType = 'Dispatch Return' AND SAL_ReverseDetail.CompanyID = @CompanyID AND SAL_ReverseDetail.PeriodID = @PeriodID 
    AND (INV_StockTransactionHeader.BusinessPartnerID = @CustomerID OR @CustomerID is null) ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_Invoice_Lubricant, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDataByKeyTableInvoiceAndVehicleInv(Nullable<int> invoiceHeaderID, int periodID, int companyID, Nullable<int> customerID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@CustomerID", customerID);

                #region Query

                string Query_Select_By_PrimaryKey_InvoiceForVehicle = @" SELECT InvoiceNumber, Set_DocumentType.DocumentType As InvoiceType, InvoiceDate, InvoiceID 
FROM SAL_InvoiceHeader 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID 
        And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID AND Set_DocumentType.Status = 1 
WHERE SAL_InvoiceHeader.StatusCode = 1 AND (CustomerID = @CustomerID OR @CustomerID is null) AND SAL_InvoiceHeader.PeriodID = @PeriodID 
    AND SAL_InvoiceHeader.CompanyID = @CompanyID and SAL_InvoiceHeader.Status = 1 and SAL_InvoiceHeader.PaymentStatusType is null 
    AND SAL_InvoiceHeader.IsPaymentGenerated = 0 

union all 

SELECT ServiceNo as InvoiceNumber, Set_DocumentType.DocumentType As InvoiceType, ServiceDate, ServicePerformanceID as InvoiceID FROM AS_ServicePerformanceHeader 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = AS_ServicePerformanceHeader.DocumentTypeID 
        And Set_DocumentType.CompanyID = AS_ServicePerformanceHeader.CompanyID And Set_DocumentType.PeriodID = AS_ServicePerformanceHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
WHERE AS_ServicePerformanceHeader.status = 1 AND (BPReceivableID = @CustomerID OR @CustomerID is null) AND AS_ServicePerformanceHeader.PeriodID = @PeriodID 
    AND AS_ServicePerformanceHeader.CompanyID = @CompanyID ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_InvoiceForVehicle, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetAllDataWithOtherTables_ReceiptHeader(int companyID, int periodID, int receiptTransHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptTransHeaderID", receiptTransHeaderID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTables, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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

                    #region Query

                    string Query_SelectAll_WithPaging_WithOtherTables = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithPaging_WithOtherTables;

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTables + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesNew(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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

                    #region Query

                    string Query_SelectAll_WithPaging_WithOtherTablesNew = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithPaging_WithOtherTablesNew;

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTablesNew, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTablesNew, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTablesNew + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesForOldDocument(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

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

                    #region Query

                    string Query_SelectAll_WithPaging_WithOtherTables = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithPaging_WithOtherTables_ForOldDocuments;

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere)
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTables + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateReceiptHeaderAndDetailForPartialReciept(List<AR_ReceiptInvoicesDetail> colReceiptDetail, List<AR_ReceiptInvoicesDetail> colReceiptDetailUpdate,
        List<AR_ReceiptInvoicesDetail> colReceiptDetailDelete, GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReceipt");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (AR_ReceiptInvoicesDetail ReceiptAdd in colReceiptDetail)
                {
                    ReceiptAdd.ReceiptInvoicesID = short.Parse(AR_ReceiptInvoicesDetail.GetNewReceiptInvoicesID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans).ToString());
                    ReceiptAdd.Insert(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    if (ReceiptAdd.InvoiceHeaderID.HasValue)
                        CheckQuantity(ReceiptAdd.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                foreach (AR_ReceiptInvoicesDetail ReceiptUpdate in colReceiptDetailUpdate)
                {
                    ReceiptUpdate.UpdateDataByKey(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    if (ReceiptUpdate.InvoiceHeaderID.HasValue)
                        CheckQuantity(ReceiptUpdate.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                foreach (AR_ReceiptInvoicesDetail ReceiptDelete in colReceiptDetailDelete)
                {
                    //AR_ReceiptInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                    //  ReceiptDelete.ReceiptInvoicesID, ReceiptTransactionID);

                    AR_ReceiptInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID, ReceiptTransactionID, ReceiptDelete.ReceiptInvoicesID);
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail



                    decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colAddTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReceipt");
            }

            return result;
        }

        public bool InsertReceiptHeaderAndDetailForPartialReceipt(List<AR_ReceiptInvoicesDetail> colReceiptDetail, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail,
       SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertReceipt");

            try
            {
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                this.ReceiptTransactionID = AR_ReceiptTransactionHeader.GetNewReceiptTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.Insert(sqltrans);

                short receiptDetailID = short.Parse(AR_ReceiptInvoicesDetail.GetNewReceiptInvoicesID(CompanyID, PeriodID, ReceiptTransactionID, sqltrans).ToString());

                foreach (AR_ReceiptInvoicesDetail ReceiptDetail in colReceiptDetail)
                {
                    ReceiptDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                    ReceiptDetail.ReceiptInvoicesID = receiptDetailID++;
                    ReceiptDetail.Insert(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    DataRow dr = SAL_InvoiceHeader.GetDataByKey(CompanyID, Convert.ToInt32(ReceiptDetail.InvoiceHeaderID), PeriodID);

                    decimal Amount = AR_ReceiptInvoicesDetail.Get_RecieptAmount_Against_RecieptDirectInvoiceForFlag(CompanyID, PeriodID,
                        ReceiptDetail.InvoiceHeaderID.Value, sqltrans);

                    if (dr["FreightAmount"].ToString() == "")
                    {
                        Amount = Amount + 0;
                    }
                    else
                    {
                        Amount = Amount + Convert.ToInt32(dr["FreightAmount"]);
                    }
                    int ActualAmount = Convert.ToInt32(dr["AmountPaid"]);

                    if (Amount >= ActualAmount)
                    {
                        SAL_OrderHeader.Update_IsPaymentGenrated(CompanyID, PeriodID, Convert.ToInt32(ReceiptDetail.InvoiceHeaderID));
                    }

                    //if (ReceiptDetail.InvoiceHeaderID.HasValue)
                    //    CheckQuantity(ReceiptDetail.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt,
                    //        f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = f_DocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        short receiptInvoiceID = 1;

                        try { receiptInvoiceID = colReceiptDetail.Find(x => x.InvoiceHeaderID.Value == Convert.ToInt32(td.Narration)).ReceiptInvoicesID; }
                        catch { }

                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.ReceiptTransactionID;
                        td.SourceDetailID = receiptInvoiceID;
                        td.Narration = "Sales Invoice # " + td.Narration;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertReceipt");
            }

            return result;
        }

        public bool InsertReceiptHeaderAndDetail(List<AR_ReceiptTransactionDetail> colReceiptDetail, List<AR_ReceiptInvoicesDetail> colReceiptInvoicesDetail, List<AR_ReceiptAccountsDetail> lst_ReceiptAccounts,
            List<AR_ReceiptBanksDetail> lst_ReceiptBanks, List<AR_ReceiptCustomerDetail> lst_ReceiptCustomer, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, List<AR_ReceiptTaxes> colReceiptTaxes, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertReceipt");

            try
            {
                #region Insert in Receipt Header table

                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                this.ReceiptTransactionID = AR_ReceiptTransactionHeader.GetNewReceiptTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                #endregion

                #region Insert in Receipt Detail table

                short receiptDetailID = short.Parse(AR_ReceiptTransactionDetail.GetNewReceiptDetailID(CompanyID, PeriodID, ReceiptTransactionID, sqltrans).ToString());

                foreach (AR_ReceiptTransactionDetail ReceiptDetail in colReceiptDetail)
                {
                    ReceiptDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                    ReceiptDetail.ReceiptDetailID = receiptDetailID++;
                    ReceiptDetail.FiscalID = CurrentFiscalID;
                    ReceiptDetail.Insert(sqltrans);

                    #region Receipt Taxes

                    if (colReceiptTaxes != null)
                    {
                        var Receipts = from objReceipts in colReceiptTaxes
                                       where objReceipts.RowUniqueID == int.Parse(ReceiptDetail.InvoiceDocumentNo)
                                       select objReceipts;

                        foreach (AR_ReceiptTaxes ReceiptTaxes in Receipts)
                        {
                            ReceiptTaxes.ReceiptHeaderID = this.ReceiptTransactionID;
                            ReceiptTaxes.ReceiptDetailID = (short)ReceiptDetail.ReceiptDetailID;
                            ReceiptTaxes.ReceiptTaxID = AR_ReceiptTaxes.GetNewReceiptTaxID(CompanyID, PeriodID, sqltrans);

                            ReceiptTaxes.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Checking For Invoice Header Complete Status

                    if (ReceiptDetail.InvoiceHeaderID.HasValue)
                        CheckQuantity(ReceiptDetail.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt,
                            f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #endregion

                #region Insert in Receipt Invoices Detail table

                short receiptInvoicesID = short.Parse(AR_ReceiptInvoicesDetail.GetNewReceiptInvoicesID(this.f_CompanyID, this.f_PeriodID, this.ReceiptTransactionID, sqltrans).ToString());

                foreach (AR_ReceiptInvoicesDetail receiptInvoicesDetail in colReceiptInvoicesDetail)
                {
                    receiptInvoicesDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                    receiptInvoicesDetail.ReceiptInvoicesID = receiptInvoicesID++;
                    receiptInvoicesDetail.FiscalID = CurrentFiscalID;
                    receiptInvoicesDetail.Insert(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    if (receiptInvoicesDetail.InvoiceHeaderID.HasValue)
                        CheckQuantity(receiptInvoicesDetail.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans,
                            SOEnumaration.SalesOrder_Stage.Receipt, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #endregion

                #region Insert in Receipt Accounts table

                if (lst_ReceiptAccounts != null)
                {
                    short receiptAccountID = (short)AR_ReceiptAccountsDetail.GetNewReceiptAccountID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptAccountsDetail objReceiptAccount in lst_ReceiptAccounts)
                    {
                        objReceiptAccount.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptAccount.ReceiptAccountID = receiptAccountID++;
                        objReceiptAccount.FiscalID = CurrentFiscalID;
                        objReceiptAccount.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Banks table

                if (lst_ReceiptBanks != null)
                {
                    short receiptBankID = (short)AR_ReceiptBanksDetail.GetNewReceiptBankID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptBanksDetail objReceiptBank in lst_ReceiptBanks)
                    {
                        objReceiptBank.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptBank.ReceiptBankID = receiptBankID++;
                        objReceiptBank.FiscalID = CurrentFiscalID;
                        objReceiptBank.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Customer table

                if (lst_ReceiptCustomer != null)
                {
                    short receiptCustomerID = (short)AR_ReceiptCustomerDetail.GetNewReceiptCustomerID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptCustomerDetail objReceiptCustomer in lst_ReceiptCustomer)
                    {
                        objReceiptCustomer.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptCustomer.ReceiptCustomerID = receiptCustomerID++;
                        objReceiptCustomer.FiscalID = CurrentFiscalID;
                        objReceiptCustomer.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = f_DocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        string naration = td.Narration.ToString();
                        if (naration.Contains("*"))
                        {
                            naration = naration.Substring(0, naration.LastIndexOf('*') - 1);
                        }
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.ReceiptTransactionID;
                        td.Narration = naration;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
                    da.FiscalID = CurrentFiscalID;
                    da.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertReceipt");
            }

            return result;
        }

        public bool UpdateReceiptHeaderAndDetail(List<AR_ReceiptTransactionDetail> colReceiptDetail, List<AR_ReceiptTransactionDetail> colReceiptDetailUpdate,
            List<AR_ReceiptTransactionDetail> colReceiptDetailDelete, List<AR_ReceiptAccountsDetail> lst_ReceiptAccountsAdd,
            List<AR_ReceiptAccountsDetail> lst_ReceiptAccountsUpdate, List<AR_ReceiptAccountsDetail> lst_ReceiptAccountsDelete,
            List<AR_ReceiptBanksDetail> lst_ReceiptBanksAdd, List<AR_ReceiptBanksDetail> lst_ReceiptBanksUpdate, List<AR_ReceiptBanksDetail> lst_ReceiptBanksDelete,
            GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da, List<AR_ReceiptCustomerDetail> lst_ReceiptCustomerAdd,
            List<AR_ReceiptCustomerDetail> lst_ReceiptCustomerUpdate, List<AR_ReceiptCustomerDetail> lst_ReceiptCustomerDelete
            , List<AR_ReceiptInvoicesDetail> colReceiptInvoicesDetailAdd, List<AR_ReceiptInvoicesDetail> colReceiptInvoicesDetailUpdate, List<AR_ReceiptInvoicesDetail> colReceiptInvoicesDetailDelete
            )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReceipt");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region Receipt Transaction Detail

                foreach (AR_ReceiptTransactionDetail ReceiptAdd in colReceiptDetail)
                {
                    ReceiptAdd.ReceiptDetailID = short.Parse(AR_ReceiptTransactionDetail.GetNewReceiptDetailID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans).ToString());
                    ReceiptAdd.Insert(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    if (ReceiptAdd.InvoiceHeaderID.HasValue)
                        CheckQuantity(ReceiptAdd.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                foreach (AR_ReceiptTransactionDetail ReceiptUpdate in colReceiptDetailUpdate)
                {
                    ReceiptUpdate.UpdateDataByKey(sqltrans);

                    #region Checking For Invoice Header Complete Status

                    if (ReceiptUpdate.InvoiceHeaderID.HasValue)
                        CheckQuantity(ReceiptUpdate.InvoiceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Receipt, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                foreach (AR_ReceiptTransactionDetail ReceiptDelete in colReceiptDetailDelete)
                {
                    AR_ReceiptTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        ReceiptDelete.ReceiptDetailID, ReceiptTransactionID);
                }

                #endregion

                #region Receipt Accounts Detail

                foreach (AR_ReceiptAccountsDetail objReceiptAccount in lst_ReceiptAccountsAdd)
                {
                    objReceiptAccount.ReceiptHeaderID = ReceiptTransactionID;
                    objReceiptAccount.ReceiptAccountID = Convert.ToInt16(AR_ReceiptAccountsDetail.GetNewReceiptAccountID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans));
                    objReceiptAccount.Insert(sqltrans);
                }

                foreach (AR_ReceiptAccountsDetail objReceiptAccount in lst_ReceiptAccountsUpdate)
                {
                    objReceiptAccount.ReceiptHeaderID = this.ReceiptTransactionID;
                    objReceiptAccount.UpdateDataByKey(sqltrans);
                }

                foreach (AR_ReceiptAccountsDetail objReceiptAccount in lst_ReceiptAccountsDelete)
                {
                    AR_ReceiptAccountsDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        objReceiptAccount.ReceiptAccountID, ReceiptTransactionID);
                }

                #endregion

                #region Receipt Banks Detail

                foreach (AR_ReceiptBanksDetail objReceiptBank in lst_ReceiptBanksAdd)
                {
                    objReceiptBank.ReceiptHeaderID = ReceiptTransactionID;
                    objReceiptBank.ReceiptBankID = Convert.ToInt16(AR_ReceiptBanksDetail.GetNewReceiptBankID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans));
                    objReceiptBank.Insert(sqltrans);
                }

                foreach (AR_ReceiptBanksDetail objReceiptBank in lst_ReceiptBanksUpdate)
                    objReceiptBank.UpdateDataByKey(sqltrans);

                foreach (AR_ReceiptBanksDetail objReceiptBank in lst_ReceiptBanksDelete)
                {
                    AR_ReceiptBanksDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        objReceiptBank.ReceiptBankID, ReceiptTransactionID);
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colAddTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"


                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
                    da.Insert(sqltrans);
                }

                #endregion


                #region Insert in Receipt Customer table

                if (lst_ReceiptCustomerAdd != null)
                {
                    short receiptCustomerID = (short)AR_ReceiptCustomerDetail.GetNewReceiptCustomerID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptCustomerDetail objReceiptCustomer in lst_ReceiptCustomerAdd)
                    {
                        objReceiptCustomer.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptCustomer.ReceiptCustomerID = receiptCustomerID++;
                        objReceiptCustomer.Insert(sqltrans);
                    }


                }

                if (lst_ReceiptBanksUpdate != null)
                    foreach (AR_ReceiptCustomerDetail objReceiptCusUpdate in lst_ReceiptCustomerUpdate)
                        objReceiptCusUpdate.UpdateDataByKey(sqltrans);

                if (lst_ReceiptBanksDelete != null)
                {
                    foreach (AR_ReceiptCustomerDetail objReceiptCusDel in lst_ReceiptCustomerDelete)
                    {
                        AR_ReceiptCustomerDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                            objReceiptCusDel.ReceiptCustomerID, ReceiptTransactionID);
                    }
                }

                if (colReceiptInvoicesDetailUpdate != null)
                    foreach (AR_ReceiptInvoicesDetail objReceiptinvocieUpdate in colReceiptInvoicesDetailUpdate)
                        objReceiptinvocieUpdate.UpdateDataByKey(sqltrans);


                if (colReceiptInvoicesDetailDelete != null)
                {
                    foreach (AR_ReceiptInvoicesDetail objINvoiceDelete in colReceiptInvoicesDetailDelete)
                    {
                        AR_ReceiptInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                            objINvoiceDelete.ReceiptHeaderID, objINvoiceDelete.ReceiptInvoicesID);
                    }
                }
                if (colReceiptInvoicesDetailAdd != null)
                {
                    short receiptInvoicesID = short.Parse(AR_ReceiptInvoicesDetail.GetNewReceiptInvoicesID(this.f_CompanyID, this.f_PeriodID, this.ReceiptTransactionID, sqltrans).ToString());

                    foreach (AR_ReceiptInvoicesDetail objReceiptinvocieAdd in colReceiptInvoicesDetailAdd)
                    {
                        objReceiptinvocieAdd.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptinvocieAdd.ReceiptInvoicesID = receiptInvoicesID++;
                        objReceiptinvocieAdd.FiscalID = this.FiscalID;
                        objReceiptinvocieAdd.Insert(sqltrans);
                    }
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReceipt");
            }

            return result;
        }

        public bool DeleteReceiptHeaderAndDetail(List<AR_ReceiptTransactionDetail> colDeleteReceiptDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colDeleteTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteReceipt");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);

                foreach (AR_ReceiptTransactionDetail ReceiptDelete in colDeleteReceiptDetail)
                    AR_ReceiptTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        ReceiptDelete.ReceiptDetailID, ReceiptTransactionID);

                AR_ReceiptAccountsDetail.DeleteDataByReceiptTransactionHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);
                AR_ReceiptBanksDetail.DeleteDataByReceiptTransactionHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);
                AR_ReceiptInvoicesDetail.DeleteDataByReceiptTransactionHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);
                AR_ReceiptCustomerDetail.DeleteDataByReceiptTransactionHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteReceipt");
            }
            return result;
        }

        public bool DeleteReceiptHeaderAndDetailForPartialReceipt(List<AR_ReceiptInvoicesDetail> colDeleteReceiptDetail, GL_TransactionHeader th,
          List<GL_TransactionDetail> colDeleteTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteReceipt");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, ReceiptTransactionID);

                foreach (AR_ReceiptInvoicesDetail ReceiptDelete in colDeleteReceiptDetail)
                    AR_ReceiptInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        ReceiptTransactionID, ReceiptDelete.ReceiptInvoicesID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteReceipt");
            }
            return result;
        }

        public bool UpdateReceiptHeadeForApprove(GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove dA)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateRTHeader");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colAddTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                    dA.UpdateDataByKeyStatus(sqltrans);

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);

                #endregion

                #region Insert In SET_DocumentApprove

                dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(f_CompanyID, f_PeriodID, sqltrans);
                dA.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateRTHeader");
            }

            return result;
        }

        public bool CancelOrReverseApprovedReceipt(List<AR_ReceiptTransactionDetail> colReceiptDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove dA, Nullable<DateTime> dt_ReverseDate)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("CancelOrReverseReceipt");

            try
            {
                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans);

                #region Update Receipt

                if (!dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))   //  Cancellation
                    this.Status = false;

                this.ReversalDate = dt_ReverseDate;
                this.ReversalComments = "Reversal document no.: " + newDocumentNo.ToString();
                this.UpdateDataByKey(sqltrans);

                #endregion

                #region Insert In AR_ReceiptTransaction Header And Detail, When ApprovalStatus = "Reversed"

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))
                {
                    #region Insert In AR_ReceiptTransactionHeader

                    this.CreateDate = this.UpdateDate.Value;
                    this.CreatedBy = this.UpdatedBy.Value;
                    this.CreatedByValue = this.UpdatedByValue;
                    this.UpdateDate = null;
                    this.UpdatedBy = null;
                    this.UpdatedByValue = null;
                    this.ReversalDate = null;
                    this.ReversalComments = null;
                    this.Amount = this.Amount * -1;
                    this.SourceHeaderID = this.ReceiptTransactionID;
                    this.DocumentNo = newDocumentNo;
                    this.DocumentDate = dt_ReverseDate.Value;
                    this.ReceiptTransactionID = AR_ReceiptTransactionHeader.GetNewReceiptTransactionID(f_CompanyID, f_PeriodID, sqltrans);

                    this.Insert(sqltrans);

                    #endregion

                    #region Insert In SET_DocumentApprove

                    dA.Status = true;
                    dA.DocumentNo = newDocumentNo.ToString();
                    dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    dA.Insert(sqltrans);

                    #endregion

                    #region Insert In AR_ReceiptTransactionDetail

                    short receiptDetailID = short.Parse(AR_ReceiptTransactionDetail.GetNewReceiptDetailID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans).ToString());

                    foreach (AR_ReceiptTransactionDetail ReceiptDetail in colReceiptDetail)
                    {
                        ReceiptDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                        ReceiptDetail.BaseAmount = ReceiptDetail.BaseAmount * -1;
                        ReceiptDetail.DiscountAmount = ReceiptDetail.DiscountAmount * -1;
                        ReceiptDetail.GrossAmount = ReceiptDetail.GrossAmount * -1;
                        ReceiptDetail.NetAmount = ReceiptDetail.NetAmount * -1;
                        ReceiptDetail.TaxAmount = ReceiptDetail.TaxAmount * -1;
                        ReceiptDetail.ReceiptDetailID = receiptDetailID++;
                        ReceiptDetail.Insert(sqltrans);
                    }

                    #endregion

                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = newDocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.ReceiptTransactionID;
                        td.Insert(sqltrans);
                    }

                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "CancelOrReverseReceipt");
            }

            return result;
        }

        public bool ReverseApprovedReceipt(List<AR_ReceiptTransactionDetail> colReceiptDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove dA, List<AR_ReceiptAccountsDetail> colReceiptAccounts, List<AR_ReceiptBanksDetail> colReceiptBanks, List<AR_ReceiptCustomerDetail> colReceiptCustomer, Nullable<DateTime> dt_ReverseDate, DateTime currentDate, Guid currentUserID, string currentUserName, out int ReversalDocumentNo, string Comments, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("CancelOrReverseReceipt");

            try
            {
                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                ReversalDocumentNo = newDocumentNo;

                #region Update Receipt

                if (!dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))   //  Cancellation
                    this.Status = false;

                this.ReversalDate = dt_ReverseDate;
                this.ReversalComments = Comments + "  (Reversal document no.: " + newDocumentNo.ToString() + ").";
                this.UpdateDataByKey(sqltrans);

                #endregion

                #region Update Sales Invoice (DI-Invoice)

                if (this.SourceHeaderID.HasValue && this.DocumentTypeID_DocumentType.DocumentType.ToLower().StartsWith("receipt against di"))
                {
                    using (SAL_InvoiceHeader obj_SalesInvoice = new SAL_InvoiceHeader())
                    {
                        obj_SalesInvoice.UpdateFlagHeader(this.f_CompanyID, this.f_PeriodID, this.f_SourceHeaderID.Value, false, currentUserID, currentDate,
                            currentUserName, sqltrans);
                    };
                }

                #endregion

                #region Insert In AR_ReceiptTransaction Header And Detail, When ApprovalStatus = "Reversed"

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))
                {
                    #region Insert In AR_ReceiptTransactionHeader

                    this.CreateDate = currentDate; //this.UpdateDate.Value;
                    this.CreatedBy = currentUserID;
                    this.CreatedByValue = currentUserName;
                    this.UpdateDate = null;
                    this.UpdatedBy = null;
                    this.UpdatedByValue = null;
                    this.ReversalDate = null;
                    this.ReversalComments = Comments + "  (This document is generated against the Reversal process).";
                    this.Amount = this.Amount * -1;
                    this.SourceHeaderID = this.ReceiptTransactionID;
                    this.DocumentNo = newDocumentNo;
                    this.DocumentDate = dt_ReverseDate.Value;
                    this.ReceiptTransactionID = AR_ReceiptTransactionHeader.GetNewReceiptTransactionID(f_CompanyID, f_PeriodID, sqltrans);

                    this.Insert(sqltrans);

                    #endregion

                    #region Insert In SET_DocumentApprove

                    dA.Status = true;
                    dA.DocumentNo = newDocumentNo.ToString();
                    dA.FiscalID = f_FiscalID;
                    dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    dA.Insert(sqltrans);

                    #endregion

                    #region Insert In AR_ReceiptTransactionDetail

                    short receiptDetailID = short.Parse(AR_ReceiptTransactionDetail.GetNewReceiptDetailID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans).ToString());

                    foreach (AR_ReceiptTransactionDetail ReceiptDetail in colReceiptDetail)
                    {
                        ReceiptDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                        ReceiptDetail.BaseAmount = ReceiptDetail.BaseAmount * -1;
                        ReceiptDetail.DiscountAmount = ReceiptDetail.DiscountAmount * -1;
                        ReceiptDetail.GrossAmount = ReceiptDetail.GrossAmount * -1;
                        ReceiptDetail.NetAmount = ReceiptDetail.NetAmount * -1;
                        ReceiptDetail.TaxAmount = ReceiptDetail.TaxAmount * -1;
                        ReceiptDetail.ReceiptDetailID = receiptDetailID++;
                        ReceiptDetail.FiscalID = f_FiscalID;
                        ReceiptDetail.Insert(sqltrans);
                    }

                    #endregion

                    #region Insert In GL_TransactionHeader

                    string originalNarration = th.Narration != null ? th.Narration : "";
                    if (th.Narration != null)
                        th.Narration = th.Narration + (th.Narration.Trim().EndsWith(".") ? " " : ". ") + "This document is reversed (Reversed document no.: "
                            + newDocumentNo.ToString() + ").";
                    else
                        th.Narration = "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";

                    th.UpdateDataByKey(sqltrans);

                    th.ReversalID = th.TransactionID;

                    if (th.Narration != null)
                    {
                        th.Narration = Comments + " " + originalNarration + (originalNarration.Trim().EndsWith(".") ? " " : ". ")
                            + "This document is generated against the Reversal process.";
                    }
                    else
                        th.Narration = Comments + "  (This document is generated against the Reversal process).";

                    th.DocumentNo = newDocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = f_FiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.ReceiptTransactionID;
                        td.FiscalID = f_FiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert in Receipt Accounts table

                if (colReceiptAccounts != null)
                {
                    short receiptAccountID = (short)AR_ReceiptAccountsDetail.GetNewReceiptAccountID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptAccountsDetail objReceiptAccount in colReceiptAccounts)
                    {
                        objReceiptAccount.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptAccount.ReceiptAccountID = receiptAccountID++;
                        objReceiptAccount.FiscalID = f_FiscalID;
                        objReceiptAccount.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Banks table

                if (colReceiptBanks != null)
                {
                    short receiptBankID = (short)AR_ReceiptBanksDetail.GetNewReceiptBankID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptBanksDetail objReceiptBank in colReceiptBanks)
                    {
                        objReceiptBank.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptBank.ReceiptBankID = receiptBankID++;
                        objReceiptBank.FiscalID = f_FiscalID;
                        objReceiptBank.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Customer table

                if (colReceiptCustomer != null)
                {
                    short receiptCustomerID = (short)AR_ReceiptCustomerDetail.GetNewReceiptCustomerID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptCustomerDetail objReceiptCustomer in colReceiptCustomer)
                    {
                        objReceiptCustomer.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptCustomer.ReceiptCustomerID = receiptCustomerID++;
                        objReceiptCustomer.FiscalID = f_FiscalID;
                        objReceiptCustomer.Insert(sqltrans);
                    }
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
                    SqlHelper.RollBackTransaction(sqltrans, "CancelOrReverseReceipt");
            }

            return result;
        }

        public bool UpdateDataByKeyStatus(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[4] = new SqlParameter("@Status", false);
                sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);

                #region Query

                string Query_Update_By_Keys_Status = @" UPDATE AR_ReceiptTransactionHeader 
SET Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID and DocumentNo = @DocumentNo 

UPDATE AR_ReceiptTransactionDetail SET Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and ReceiptHeaderID = 
    (
        SELECT ReceiptTransactionID FROM AR_ReceiptTransactionHeader 
        WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and DocumentTypeID = @DocumentTypeID and DocumentNo = @DocumentNo
    ) ";

                #endregion

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
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

        public DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    string query = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithPaging_WithOtherTables_ForApprove.Replace("{filter}", strWhere);

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTables_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesForApproveNew(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            try
            {
                if (PageIndex != null)
                {
                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    string query = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithPaging_WithOtherTables_ForApproveNew.Replace("{filter}", strWhere);

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_WithOtherTables_ForApproveNew + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int rTransactionID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptTransactionID", rTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_SelectAll_ForApproveDetail, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public DataTable GetDataByKeyForApprove(int companyID, int periodID, int rTransactionID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptTransactionID", rTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                #region Query

                string Query_Select_DataByKey_ForApprove = @" 
SELECT DISTINCT AR_ReceiptTransactionHeader.ReceiptTransactionID, AR_ReceiptTransactionHeader.CompanyID, AR_ReceiptTransactionHeader.PeriodID, 
    AR_ReceiptTransactionHeader.Status, SET_UserApprovalAmount.ApprovalOperator, SET_ApprovalAuthority.UserID, SET_Authority.AuthorityLevel, 
    SET_UserApprovalAmount.ApprovalOperator, SET_ApprovalAuthority.ApprovalAuthorityID, SET_UserApprovalAmount.ApprovalAmountID 
FROM AR_ReceiptTransactionHeader 
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
        AND SET_UserApprovalAmount.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                AND CAST(CONVERT(VARCHAR(10), AR_ReceiptTransactionHeader.DocumentDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL AND CAST(CONVERT(VARCHAR(10), AR_ReceiptTransactionHeader.DocumentDate, 110) AS DATETIME) 
                BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END)) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT rth.Amount FROM AR_ReceiptTransactionHeader rth 
                WHERE rth.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND rth.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND rth.ReceiptTransactionID = AR_ReceiptTransactionHeader.ReceiptTransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT rth.Amount FROM AR_ReceiptTransactionHeader rth 
                WHERE rth.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND rth.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
                AND rth.ReceiptTransactionID = AR_ReceiptTransactionHeader.ReceiptTransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
        AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID AND SET_Authority.Status = 1 
    INNER JOIN aspnet_UsersInRoles On aspnet_UsersInRoles.UserID = AR_ReceiptTransactionHeader.CreatedBy 
        And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null) 

        --new Working (Roles aginst Approver)
		inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  AR_ReceiptTransactionHeader.DocumentTypeID
		inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID
		inner join MenusInRoles on MenusInRoles.MenuID = SET_DocumentType.MenuID
		inner join aspnet_Roles on aspnet_Roles.RoleId =  MenusInRoles.RoleId

WHERE AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.CompanyID = @CompanyID 
    AND AR_ReceiptTransactionHeader.ReceiptTransactionID = @ReceiptTransactionID AND AR_ReceiptTransactionHeader.DocumentTypeID = @DocumentTypeID 
    AND AR_ReceiptTransactionHeader.DocumentNo = @DocumentNo
and aspnet_UsersInRoles.RoleId = aspnet_Roles.RoleId 
";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_DataByKey_ForApprove, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        private void CheckQuantity(int invoiceHeaderID, int companyID, int periodID, SqlTransaction sqltrans, SOEnumaration.SalesOrder_Stage closedCode,
            Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_CheckQuantity = @" 
SELECT [SAL_InvoiceDetail].[InvoiceHeaderID], [SAL_InvoiceDetail].[InvoiceDetailID], [SAL_InvoiceDetail].[QuantityInvoiced] AS InvQuantity, 
    Sum([AR_ReceiptTransactionDetail].[Quantity]) AS RecQuantity 
FROM [AR_ReceiptTransactionDetail] 
    INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceDetailID = AR_ReceiptTransactionDetail.InvoiceDetailID 
        AND SAL_InvoiceDetail.InvoiceHeaderID = AR_ReceiptTransactionDetail.InvoiceHeaderID AND SAL_InvoiceDetail.CompanyID = AR_ReceiptTransactionDetail.CompanyID 
        AND [SAL_InvoiceDetail].[PeriodID] = [AR_ReceiptTransactionDetail].[PeriodID] AND [SAL_InvoiceDetail].[Status] = 1 
WHERE AR_ReceiptTransactionDetail.InvoiceHeaderID = @InvoiceHeaderID AND AR_ReceiptTransactionDetail.CompanyID = @CompanyID 
    AND AR_ReceiptTransactionDetail.PeriodID = @PeriodID AND [AR_ReceiptTransactionDetail].[Status] = 1 
Group By [SAL_InvoiceDetail].[InvoiceHeaderID], [SAL_InvoiceDetail].[InvoiceDetailID], [SAL_InvoiceDetail].[QuantityInvoiced] 
Having Sum([AR_ReceiptTransactionDetail].[Quantity]) >= [SAL_InvoiceDetail].[QuantityInvoiced] ";

                DataTable dtInvoice = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_CheckQuantity, sqlparams).Tables[0];
                bool result = true;

                if (dtInvoice != null)
                {
                    SAL_InvoiceHeader invh = new SAL_InvoiceHeader();

                    foreach (DataRow dr in dtInvoice.Rows)
                    {
                        result = invh.UpdateFlagDetail(sqltrans, Convert.ToInt32(dr["InvoiceHeaderID"]), Convert.ToInt32(dr["InvoiceDetailID"]), true, companyID, periodID, updatedBy, updateDate, updatedByValue, closedCode);

                        if (!result)
                            break;
                    }

                    //Checking Invoice Header
                    if (result)
                    {
                        bool paymentReceived = invh.CheckIsDetailCompleted(sqltrans, invoiceHeaderID, false, f_CompanyID, f_PeriodID);
                        DataRow dr = SAL_InvoiceHeader.GetDataByKey(CompanyID, Convert.ToInt32(invoiceHeaderID), PeriodID, null, sqltrans);
                        decimal receivedAmount = AR_ReceiptInvoicesDetail.Get_RecieptAmount_Against_RecieptDirectInvoiceForFlag(f_CompanyID, f_PeriodID,
                            invoiceHeaderID, sqltrans);

                        if (dr["PaymentStatusType"] != null && dr["PaymentStatusType"] != DBNull.Value)
                        {
                            AR_ReceiptTransactionHeader obj_Receipt = new AR_ReceiptTransactionHeader(companyID, periodID, Convert.ToInt32(dr["PaymentStatusType"]));

                            if (obj_Receipt.IsDataLoaded)
                                receivedAmount += Convert.ToDecimal(obj_Receipt.Amount);
                        }

                        if (dr["FreightAmount"] != DBNull.Value && !string.IsNullOrEmpty(dr["FreightAmount"].ToString()))
                            receivedAmount += Convert.ToDecimal(dr["FreightAmount"]);

                        decimal actualAmount = Convert.ToDecimal(dr["AmountPaid"]);

                        if (receivedAmount >= actualAmount)
                            paymentReceived = true;
                        else
                            paymentReceived = false;

                        if (paymentReceived)
                            invh.UpdateFlagHeader(companyID, periodID, invoiceHeaderID, true, updatedBy, updateDate, updatedByValue, sqltrans);
                    }
                    else
                        throw new BusinessLogicException("Error generated: AR_ReceiptTransaction.cs, Line: 367, Function: InvH.UpdateFlagDetail");
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static Double CheckAmountForAdvanceAgainstSO(int companyID, int periodID, int sourceHeaderID, int CustomerID = 0)
        {
            double Amount = 0;
            string Filters = "";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                if (CustomerID != 0 && CustomerID != -1)
                    Filters = " and AR_ReceiptTransactionHeader.CustomerID = " + CustomerID;

                #region Query

                #endregion

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_CheckAmount_AdvanceAgainstSO, Filters), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public Double CheckAmountForAdvanceAgainstSO2(int companyID, int periodID, int sourceHeaderID)
        {
            double Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                string Query_CheckAmount_AdvanceAgainstSO2 = @" SELECT ISNULL(SUM(AR_ReceiptTransactionHeader.Amount), 0) AS Amount FROM AR_ReceiptTransactionHeader 
    INNER JOIN 
    (
SELECT Distinct  [SAL_OrderHeader].[OrderID],[SAL_OrderHeader].[CompanyID] ,[SAL_OrderHeader].[PeriodID] ,[SAL_OrderHeader].[SaleContractTypeID] ,[SAL_OrderHeader].[CustomerID],
[SAL_OrderHeader].[BrokerID],[SAL_OrderHeader].[PaymentTermID]
      ,[SAL_OrderHeader].[Season] ,[SAL_OrderHeader].[ContractNo],[SAL_OrderHeader].[ContractDate] ,[SAL_OrderHeader].[PaymentDate] ,[SAL_OrderHeader].[DispatchDate]
	  ,[SAL_OrderHeader].[DTRENo],[SAL_OrderHeader].[DTREDate] ,[SAL_OrderHeader].[IsApproved]
      ,[SAL_OrderHeader].[ApprovedBy],[SAL_OrderHeader].[ApprovalDate],[SAL_OrderHeader].[Remarks],[SAL_OrderHeader].[IsPaid] ,[SAL_OrderHeader].[PaidAmount]
	  ,[SAL_OrderHeader].[Status],[SAL_OrderHeader].[CreatedBy] ,[SAL_OrderHeader].[CreateDate],[SAL_OrderHeader].[UpdatedBy] ,[SAL_OrderHeader].[UpdateDate]
	  ,[SAL_OrderHeader].[CreatedByValue],[SAL_OrderHeader].[UpdatedByValue],[SAL_OrderHeader].[StatusCode],[SAL_OrderHeader].[ClosedCode] ,[SAL_OrderHeader].[ClosedOn]
	  ,[SAL_OrderHeader].[TotalAmount] ,[SAL_OrderHeader].[Held],[SAL_OrderHeader].[ApprovalRequired],[SAL_OrderHeader].[Cancelled],[SAL_OrderHeader].[CancelledOn] 
	  ,[SAL_OrderHeader].[CancelledBy] ,[SAL_OrderHeader].[Frozen],[SAL_OrderHeader].[referenceno],[SAL_OrderHeader].[DepartmentID],[SAL_OrderHeader].[FreightTermsCode]
	  ,[SAL_OrderHeader].[FreightDetail],[SAL_OrderHeader].[InspectionBy],[SAL_OrderHeader].[PaymentTypeID],[SAL_OrderHeader].[CommissionAmount]
	   ,[SAL_OrderHeader].[CommissionCOA],[SAL_OrderHeader].[InsuranceAmount],[SAL_OrderHeader].[InsuranceCOA] ,[SAL_OrderHeader].[ChartOfAccountID] 
	   ,[SAL_OrderHeader].[CheckListChecked],[SAL_OrderHeader].[AdvanceAmountPercentage],[SAL_OrderHeader].[BilledTo]
	  ,[SAL_OrderHeader].[Commission] ,[SAL_OrderHeader].[TransactionType] ,[SAL_OrderHeader].[Quality] ,[SAL_OrderHeader].[AmountType]
	  ,[SAL_OrderHeader].[ShipTo],[SAL_OrderHeader].[appid],[SAL_OrderHeader].[FiscalID],[SAL_OrderHeader].[SalesPersonID]
FROM SAL_InvoiceDetail 
            INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID 
                AND INV_StockTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID 
                AND INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
                AND INV_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID AND INV_StockTransactionDetail.Status = 1 
		    INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID  = INV_StockTransactionDetail.CompanyID 
                AND SAL_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
                AND SAL_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                AND SAL_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceCode AND SAL_DeliveryOrderDetail.Status = 1 
            INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
                AND SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID AND SAL_OrderHeader.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
                AND SAL_OrderHeader.Status = 1
		WHERE SAL_InvoiceDetail.CompanyID = @CompanyID AND SAL_InvoiceDetail.PeriodID = @PeriodID AND SAL_InvoiceDetail.Status = 1 
            AND SAL_InvoiceDetail.InvoiceHeaderID = @SourceHeaderID
    ) AS tbl ON tbl.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND tbl.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        AND tbl.OrderID = AR_ReceiptTransactionHeader.SourceHeaderID AND tbl.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
        AND SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.Status = 1 AND SET_DocumentType.DocumentType = 'Advance Against SO' 
WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.Status = 1 ";

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmount_AdvanceAgainstSO2, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public DataTable GetCreditNoteAmount(int companyID, int periodID, int sourceHeaderID)
        {
            DataTable dtCreditNoteAmount = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                string Query_GetCreditNoteAmount = @" select * from SAL_InvoiceHeader 
where CompanyId = @CompanyID and periodId = @PeriodID and SalesInvoiceHeaderID = @SourceHeaderID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCreditNoteAmount, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtCreditNoteAmount = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtCreditNoteAmount;
        }

        public DataTable GetDataByDocTypeIDWithOtherTables(Nullable<int> invoiceHeaderID, int periodID, int companyID, int docTypeID, Nullable<int> supplierID,
            string strWhere)
        {
            DataTable dtToReturn = null;
            StringBuilder appendedFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                if (supplierID.HasValue)
                    sqlparams[3] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[3] = new SqlParameter("@SupplierID", DBNull.Value);

                if (docTypeID != 0)
                    appendedFilter.Append(" AND Set_DocumentTypesFilter.DocumentTypeID = " + docTypeID.ToString());


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetDataByDocID + strWhere + appendedFilter, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public Double CheckAmountForAdvanceAgainstSO3(int companyID, int periodID, int sourceHeaderID)
        {
            double Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                string Query_CheckAmount_AdvanceAgainstSO3 = @" SELECT ISNULL(SUM(AR_ReceiptTransactionHeader.Amount), 0) AS Amount FROM AR_ReceiptTransactionHeader 
    LEFT JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND SAL_OrderHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        AND SAL_OrderHeader.OrderID = AR_ReceiptTransactionHeader.SourceHeaderID AND SAL_OrderHeader.Status = 1 
WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.Status = 1 
    AND AR_ReceiptTransactionHeader.SourceHeaderID = @SourceHeaderID
    AND ReversalDate is null";

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmount_AdvanceAgainstSO3, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public Double CheckAmountForAdvanceAgainstSO4(int companyID, int periodID, int sourceHeaderID)
        {
            double Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                string Query_CheckAmount_AdvanceAgainstSO3 = @" SELECT ISNULL(SUM(AR_ReceiptTransactionHeader.Amount), 0) AS Amount FROM AR_ReceiptTransactionHeader 
    LEFT JOIN SAL_ContractHeader ON SAL_ContractHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND SAL_ContractHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        AND SAL_ContractHeader.ContractID = AR_ReceiptTransactionHeader.SourceHeaderID AND SAL_ContractHeader.Status = 1 
WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.Status = 1 
    AND AR_ReceiptTransactionHeader.SourceHeaderID = @SourceHeaderID
 and AR_ReceiptTransactionHeader.DocumentTypeID in (select DocumentTypeID from SET_DocumentType where DocumentType like '%Advance Against SO%' and Status = 1)-- and CompanyID = @CompanyID and PeriodID = @PeriodID)
    AND ReversalDate is null";

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmount_AdvanceAgainstSO3, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }


        public Double CheckAmountForAdvanceAgainstSaleContract(int companyID, int periodID, int sourceHeaderID)
        {
            double Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                string Query_CheckAmount_AdvanceAgainstSO3 = @" SELECT ISNULL(SUM(AR_ReceiptTransactionHeader.Amount), 0) AS Amount FROM AR_ReceiptTransactionHeader 
    LEFT JOIN SAL_ContractHeader ON SAL_ContractHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND SAL_ContractHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        AND SAL_ContractHeader.ContractID = AR_ReceiptTransactionHeader.SourceHeaderID AND SAL_ContractHeader.Status = 1 
WHERE AR_ReceiptTransactionHeader.CompanyID = 1 AND AR_ReceiptTransactionHeader.PeriodID = 1 AND AR_ReceiptTransactionHeader.Status = 1 
    AND AR_ReceiptTransactionHeader.SourceHeaderID = @SourceHeaderID
    AND ReversalDate is null";

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmount_AdvanceAgainstSO3, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public static bool UpdateIsInvoiceAdjusted(int companyID, int periodID, int paymentid, bool status)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PID", paymentid);
                sqlparams[3] = new SqlParameter("@status", status);

                string Query_UpdateIsInvoiceAdjusted = @" Update AR_ReceiptTransactionHeader Set IsInvoiceAdjusted = @status 
Where Status = 1 And CompanyId = @CompanyID And PeriodID = @PeriodID And ReceiptTransactionID = @PID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsInvoiceAdjusted, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }
        public static decimal GetSalesAmountforCreditLmit(int CompanyID, int CustomerID, int PeriodID)
        {
            decimal Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                string Query_SalesAmountforCreditLmit = @" Select  SUM(SalesAmount) -ISNULL(SUM(ReceiptAmount),0) as SalesAmount from (
Select Distinct    SAL_InvoiceHeader.CompanyID,SAL_InvoiceHeader.PeriodID,SAL_InvoiceHeader.InvoiceID, SUM(SAL_InvoiceDetail.NetAmount) as SalesAmount  from SAL_InvoiceHeader 
INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceHeader.InvoiceID=SAL_InvoiceDetail.InvoiceHeaderID
AND  SAL_InvoiceDetail.CompanyID=SAL_InvoiceHeader.CompanyID

INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=SAL_InvoiceDetail.StockHeaderID
AND INV_StockTransactionDetail.TransactionDetailID=SAL_InvoiceDetail.StockDetailID
 AND INV_StockTransactionDetail.CompanyID=SAL_InvoiceDetail.CompanyID
 AND INV_StockTransactionDetail.Status=1
 INNER JOIN INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID=INV_StockTransactionDetail.StockTransactionID
AND INV_StockTransactionHeader.PeriodID=INV_StockTransactionDetail.PeriodID
 AND INV_StockTransactionHeader.CompanyID=INV_StockTransactionDetail.CompanyID
 AND INV_StockTransactionHeader.Status=1
  INNER JOIN SAL_DispatchHeader on SAL_DispatchHeader.DispatchNo=INV_StockTransactionHeader.DocumentNo
  AND SAL_DispatchHeader.DocumentTypeID=INV_StockTransactionHeader.DocumentTypeID
  AND SAL_DispatchHeader.FiscalID=INV_StockTransactionHeader.FiscalID
AND SAL_DispatchHeader.PeriodID=INV_StockTransactionHeader.PeriodID
 AND SAL_DispatchHeader.CompanyID=INV_StockTransactionHeader.CompanyID
 AND SAL_DispatchHeader.Status=1
  INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DocumentTypeID=INV_StockTransactionDetail.SourceDocumentTypeID
 AND SAL_DeliveryOrderHeader.DeliveryOrderNo=INV_StockTransactionDetail.SourceCode
  AND SAL_DeliveryOrderHeader.DeliveryOrderID=INV_StockTransactionDetail.SourceHeaderID
  AND SAL_DeliveryOrderHeader.PeriodID=INV_StockTransactionDetail.PeriodID
 AND SAL_DeliveryOrderHeader.CompanyID=INV_StockTransactionDetail.CompanyID
  AND SAL_DeliveryOrderHeader.Status=1

 INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID=SAL_DeliveryOrderHeader.DeliveryOrderID
  AND SAL_DeliveryOrderDetail.PeriodID=SAL_DeliveryOrderHeader.PeriodID
 AND SAL_DeliveryOrderDetail.CompanyID=SAL_DeliveryOrderHeader.CompanyID
 AND SAL_DeliveryOrderDetail.Status=1
 INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID=SAL_DeliveryOrderDetail.SalesOrderHeaderID
 --AND SAL_OrderHeader.OrderDetailID=SAL_DeliveryOrderDetail.SalesOrderDetailID
 AND SAL_OrderHeader.CompanyID=SAL_DeliveryOrderDetail.CompanyID
  AND SAL_OrderHeader.PeriodID=SAL_DeliveryOrderDetail.PeriodID
  AND SAL_OrderHeader.Status=1


  AND ISNULL(SAL_OrderHeader.IsSpecialDeal,0)<>1
Where SAL_InvoiceHeader.Status=1 AND SAL_InvoiceHeader.StatusCode=1 AND  SAL_InvoiceHeader.CustomerID=@CustomerID  
AND SAL_InvoiceHeader.CompanyID=@CompanyID
AND SAL_InvoiceHeader.PeriodID=@PeriodID
group by  SAL_InvoiceHeader.InvoiceID,SAL_InvoiceHeader.CompanyID,SAL_InvoiceHeader.PeriodID
) as fintab
LEFT JOIN  (
Select    InvoiceHeaderID, AR_ReceiptTransactionHeader.CompanyID,AR_ReceiptTransactionHeader.PeriodID,AR_ReceiptTransactionHeader.Amount as ReceiptAmount from  AR_ReceiptInvoicesDetail
INNER JOIN  AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.ReceiptTransactionID=AR_ReceiptInvoicesDetail.ReceiptHeaderID
AND AR_ReceiptTransactionHeader.CompanyID=AR_ReceiptInvoicesDetail.CompanyID
AND AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptInvoicesDetail.PeriodID
AND AR_ReceiptTransactionHeader.Status=1 AND AR_ReceiptTransactionHeader.StatusCode=1

Where AR_ReceiptInvoicesDetail.Status=1 
AND AR_ReceiptTransactionHeader.CompanyID=@CompanyID
AND AR_ReceiptTransactionHeader.PeriodID=@PeriodID
--group by InvoiceHeaderID, AR_ReceiptTransactionHeader.CompanyID,AR_ReceiptTransactionHeader.PeriodID
) as ReceiptHeader
ON  ReceiptHeader.InvoiceHeaderID= fintab.InvoiceID
AND   ReceiptHeader.CompanyID= fintab.CompanyID
AND  ReceiptHeader.PeriodID= fintab.PeriodID


";

                Amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_SalesAmountforCreditLmit, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;

        }

        public DataTable GetDataByDocTypeIDWithOtherTables_Al_Imdad(Nullable<int> invoiceHeaderID, int periodID, int companyID, int docTypeID, int DocTypefiterID, Nullable<int> supplierID,
          string strWhere)
        {
            DataTable dtToReturn = null;
            StringBuilder appendedFilter = new StringBuilder();
            StringBuilder docTypeIDFilter = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                if (invoiceHeaderID.HasValue)
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", invoiceHeaderID.Value);
                else
                    sqlparams[0] = new SqlParameter("@invoiceHeaderID", DBNull.Value);

                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                if (supplierID.HasValue)
                    sqlparams[3] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[3] = new SqlParameter("@SupplierID", DBNull.Value);

                if (docTypeID != 0)
                {
                    appendedFilter.Append(" AND Set_DocumentTypesFilter.DocumentTypeID = " + docTypeID.ToString());

                }
                if (DocTypefiterID != 0)
                {
                    appendedFilter.Append(" AND SET_DocumentType.DocumentTypeID = " + DocTypefiterID.ToString());
                    docTypeIDFilter.Append(" Where FINTAB.DocumentTypeID = " + DocTypefiterID.ToString());
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_GetDataByDocID_Alimdad, strWhere, appendedFilter, docTypeIDFilter), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        #endregion

        public DataTable GetCollectionDetailBankWiseData(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> fromBrokerID, Nullable<int> bankID, int DocumentType, Nullable<short> documentGroupID = null, int statuscode = 0
            , Nullable<int> DepartmentID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (fromBrokerID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", fromBrokerID);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", DBNull.Value);
                }

                if (fromDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", fromDate);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", DBNull.Value);
                }
                if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", toDate);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", DBNull.Value);
                }

                if (bankID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@BankID", bankID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@BankID", DBNull.Value);
                }


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (DocumentType != -1)
                    sb.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentType + " ");

                if (statuscode > 0)
                {
                    sb.Append(" and AR_ReceiptTransactionHeader.statuscode =  " + statuscode);
                }

                if (DepartmentID.HasValue)
                {
                    sb.Append(" and AR_ReceiptTransactionHeader.CostCentreID =  " + DepartmentID.Value);
                }



                #region Query

                string Query_CollectionRecieptReport = @" select distinct
    fintab.ReceiptTransactionID,
	fintab.DocumentDate,
		fintab.InstrumentNo,
    Sum(fintab.BankAmount) as BankAmount,
    fintab.BankAccountID,
    sum(fintab.Amount) as CustomerAmount,
    fintab.CustomerID,
    --fintab.BusinessPartnerTypeID,
    fintab.CustomerName, 
    fintab.ReceiptHeaderAmount,
	fintab.DocumentNo,
    fintab.DocumentTypeID,
	fintab.DocumentType,
    fintab.BankNameAcc,
	fintab.CompanyName
    from
(
Select distinct
    AR_ReceiptTransactionHeader.ReceiptTransactionID,
	AR_ReceiptTransactionHeader.DocumentDate,
	AR_ReceiptBanksDetail.InstrumentNo,
    --AR_ReceiptBanksDetail.Amount as BankAmount,
    case AR_ReceiptCustomerDetail.TransactionType when 'Debit' then AR_ReceiptBanksDetail.Amount * -1 else AR_ReceiptBanksDetail.Amount end as BankAmount,
    AR_ReceiptBanksDetail.BankAccountID,
     0 as Amount,
	AR_ReceiptCustomerDetail.CustomerID,
    --AR_ReceiptCustomerDetail.BusinessPartnerTypeID,
    AR_SET_Customer.CustomerName, 
    AR_ReceiptTransactionHeader.Amount as ReceiptHeaderAmount,
	AR_ReceiptTransactionHeader.DocumentNo,
    AR_ReceiptTransactionHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    --(AR_SET_Bank.BankName +'-'+    AR_SET_BankBranch.BankBranchName+'-'+ AR_SET_BankAccount.BankAccountName) as BankNameAcc,
    (AR_SET_BankAccount.BankAccountName) as BankNameAcc,
	SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName, AR_ReceiptBanksDetail.ReceiptBankID


from AR_ReceiptTransactionHeader
    inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID= AR_ReceiptTransactionHeader.ReceiptTransactionID
    and AR_ReceiptTransactionHeader.CompanyID= AR_ReceiptBanksDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptBanksDetail.PeriodID
    and AR_ReceiptBanksDetail.Status=1

    inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID= AR_ReceiptBanksDetail.BankAccountID  and AR_SET_BankAccount.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
    
        And AR_SET_BankAccount.Status=1

    inner join AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.receiptHeaderid= AR_ReceiptTransactionHeader.ReceiptTransactionID
     and AR_ReceiptTransactionHeader.CompanyID=AR_ReceiptCustomerDetail.CompanyID
     And AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptCustomerDetail.PeriodID
     And AR_ReceiptTransactionHeader.Status=1
    And AR_ReceiptCustomerDetail.Status=1
    and AR_ReceiptCustomerDetail.BusinessPartnerTypeID != 7

     inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_ReceiptCustomerDetail.CustomerID
    -- And AR_SET_Customer.CompanyID=AR_ReceiptCustomerDetail.CompanyID
    -- And AR_SET_Customer.PeriodID= AR_ReceiptCustomerDetail.PeriodID
     And AR_SET_Customer.Status=1

     inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
     inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID 

	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AR_ReceiptTransactionHeader.DocumentTypeID
	 AND Set_DocumentType.CompanyID=AR_ReceiptTransactionHeader.CompanyID
	 AND Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
	 AND Set_DocumentType.Status=1

	 inner join SET_Company on SET_Company.CompanyId=AR_ReceiptTransactionHeader.CompanyID
	 and SET_Company.Status=1

	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1

	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 where AR_ReceiptTransactionHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	 	 and  AR_ReceiptTransactionHeader.CompanyID=@CompanyID
	 and AR_ReceiptTransactionHeader.PeriodID=@PeriodID
	 And AR_ReceiptTransactionHeader.Status=1
	 And AR_ReceiptTransactionHeader.StatusCode = 1
	 And (AR_ReceiptCustomerDetail.CustomerID = @FromBrokerID or  @FromBrokerID is null)
	 And (AR_SET_Bank.BankID=@BankID or @BankID is null)
    AND (SET_DocumentType.DocumentType LIKE '%collection%' OR SET_DocumentType.DocumentType LIKE '%receipt%')     
    {0}

Union All
Select 
    AR_ReceiptTransactionHeader.ReceiptTransactionID,
	AR_ReceiptTransactionHeader.DocumentDate,
	AR_ReceiptBanksDetail.InstrumentNo,
    AR_ReceiptBanksDetail.Amount as BankAmount,
    AR_ReceiptBanksDetail.BankAccountID,
    AR_ReceiptTransactionHeader.Amount,
	AR_ReceiptTransactionHeader.CustomerID,
    --AR_ReceiptTransactionHeader.BusinessPartnerTypeID,
    AR_SET_Customer.CustomerName, 
    AR_ReceiptTransactionHeader.Amount as ReceiptHeaderAmount,
		AR_ReceiptTransactionHeader.DocumentNo,
    AR_ReceiptTransactionHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    --(AR_SET_Bank.BankName +'-'+    AR_SET_BankBranch.BankBranchName+'-'+ AR_SET_BankAccount.BankAccountName) as BankNameAcc,
    (AR_SET_BankAccount.BankAccountName) as BankNameAcc,
		SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName , AR_ReceiptBanksDetail.ReceiptBankID

	from AR_ReceiptTransactionHeader

			inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID=AR_ReceiptTransactionHeader.ReceiptTransactionID
			    and AR_ReceiptTransactionHeader.CompanyID= AR_ReceiptBanksDetail.CompanyID
				and AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptBanksDetail.PeriodID
				and AR_ReceiptBanksDetail.Status=1

				inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID=AR_ReceiptBanksDetail.BankBranchID
				And AR_SET_BankBranch.Status=1
				inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID
				And AR_SET_Bank.Status=1
				inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID=AR_ReceiptBanksDetail.BankAccountID  and AR_SET_BankAccount.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
				    
					And AR_SET_BankAccount.Status=1
				inner join AR_SET_Customer on AR_SET_Customer.CustomerID= AR_ReceiptTransactionHeader.CustomerID
				And AR_SET_Customer.Status=1
				
	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AR_ReceiptTransactionHeader.DocumentTypeID
	 AND Set_DocumentType.CompanyID=AR_ReceiptTransactionHeader.CompanyID
	 AND Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
	 AND Set_DocumentType.Status=1

	 	 inner join SET_Company on SET_Company.CompanyId=AR_ReceiptTransactionHeader.CompanyID
	 and SET_Company.Status=1
	 	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1
	 	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 where AR_ReceiptTransactionHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	 and  AR_ReceiptTransactionHeader.CompanyID=@CompanyID
	 and AR_ReceiptTransactionHeader.PeriodID=@PeriodID
	 And AR_ReceiptTransactionHeader.Status=1
	 And AR_ReceiptTransactionHeader.StatusCode = 1
	 And (AR_ReceiptTransactionHeader.CustomerID = @FromBrokerID or  @FromBrokerID is null)
	 And (AR_SET_Bank.BankID=@BankID or @BankID is null)
AND (SET_DocumentType.DocumentType LIKE '%collection%' OR SET_DocumentType.DocumentType LIKE '%receipt%')	
and AR_ReceiptTransactionHeader.ReceiptTransactionID not in (select ReceiptHeaderID from AR_ReceiptCustomerDetail where status = 1)
    {0}
			)as fintab 
group by
    fintab.ReceiptTransactionID,
	fintab.DocumentDate,
	fintab.InstrumentNo,
    fintab.BankAccountID,
    fintab.CustomerID,
    --fintab.BusinessPartnerTypeID,
    fintab.CustomerName, 
    fintab.ReceiptHeaderAmount,
	fintab.DocumentNo,
    fintab.DocumentTypeID,
		fintab.DocumentType,
    fintab.BankNameAcc,
		fintab.CompanyName  ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_CollectionRecieptReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dt;
        }

        public DataTable GetCollectionDetailBankBrokerWiseData(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> fromBrokerID, Nullable<int> bankID, Nullable<int> DealerID, int DocumentType, Nullable<short> documentGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (fromBrokerID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", fromBrokerID);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", DBNull.Value);
                }

                if (fromDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", fromDate);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", DBNull.Value);
                }
                if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", toDate);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", DBNull.Value);
                }

                if (bankID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@BankID", bankID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@BankID", DBNull.Value);
                }
                if (DealerID.HasValue)
                {
                    sqlparams[6] = new SqlParameter("@BrokerID", DealerID);
                }
                else
                {
                    sqlparams[6] = new SqlParameter("@BrokerID", DBNull.Value);
                }
                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (DocumentType != -1)
                    sb.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentType + " ");



                #region Query

                string Query_CollectionRecieptBankWiseBrokerReport = @"select 
    fintab.ReceiptTransactionID,
	fintab.DocumentDate,
    Sum(fintab.BankAmount) as BankAmount,
    fintab.BankAccountID,
    sum(fintab.Amount) as CustomerAmount,
    fintab.CustomerID,
    fintab.BusinessPartnerTypeID,
    fintab.CustomerName, 
    fintab.ReceiptHeaderAmount,
	fintab.DocumentNo,
    fintab.DocumentTypeID,
	fintab.DocumentType,
    fintab.BankNameAcc,
	fintab.CompanyName,
	fintab.BrokerName
    from
(
Select 
    AR_ReceiptTransactionHeader.ReceiptTransactionID,
	AR_ReceiptTransactionHeader.DocumentDate,
    AR_ReceiptBanksDetail.Amount as BankAmount,
    AR_ReceiptBanksDetail.BankAccountID,
    AR_ReceiptCustomerDetail.Amount,
	AR_ReceiptCustomerDetail.CustomerID,
    AR_ReceiptCustomerDetail.BusinessPartnerTypeID,
    AR_SET_Customer.CustomerName, 
    AR_ReceiptTransactionHeader.Amount as ReceiptHeaderAmount,
	AR_ReceiptTransactionHeader.DocumentNo,
    AR_ReceiptTransactionHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    (AR_SET_Bank.BankName +'-'+    AR_SET_BankBranch.BankBranchName+'-'+ AR_SET_BankAccount.BankAccountName) as BankNameAcc,
	SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName
	, Set_Broker.CustomerName as BrokerName

from AR_ReceiptTransactionHeader
    inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID= AR_ReceiptTransactionHeader.ReceiptTransactionID
    and AR_ReceiptTransactionHeader.CompanyID= AR_ReceiptBanksDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptBanksDetail.PeriodID
    and AR_ReceiptBanksDetail.Status=1

    inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID= AR_ReceiptBanksDetail.BankAccountID
    and  AR_SET_BankAccount.CompanyID= AR_ReceiptBanksDetail.CompanyID
        and  AR_SET_BankAccount.PeriodID= AR_ReceiptBanksDetail.PeriodID
        And AR_SET_BankAccount.Status=1

    inner join AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.receiptHeaderid= AR_ReceiptTransactionHeader.ReceiptTransactionID
     and AR_ReceiptTransactionHeader.CompanyID=AR_ReceiptCustomerDetail.CompanyID
     And AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptCustomerDetail.PeriodID
     And AR_ReceiptTransactionHeader.Status=1

     inner join AR_SET_Customer on AR_SET_Customer.CustomerID=AR_ReceiptCustomerDetail.CustomerID
     And AR_SET_Customer.CompanyID=AR_ReceiptCustomerDetail.CompanyID
     And AR_SET_Customer.PeriodID= AR_ReceiptCustomerDetail.PeriodID
     And AR_SET_Customer.Status=1

	 left Join AR_SET_Customer Set_Broker on Set_Broker.CustomerID=AR_SET_Customer.BrokerID
     And Set_Broker.CompanyID=AR_SET_Customer.CompanyID
     And Set_Broker.PeriodID= AR_SET_Customer.PeriodID
     And Set_Broker.Status=1

     inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
     inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID 

	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AR_ReceiptTransactionHeader.DocumentTypeID
    and Set_DocumentType.CompanyID= AR_ReceiptTransactionHeader.CompanyID

	 inner join SET_Company on SET_Company.CompanyId=AR_ReceiptTransactionHeader.CompanyID
	 and SET_Company.Status=1

	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1

	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 where AR_ReceiptTransactionHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	 	 and  AR_ReceiptTransactionHeader.CompanyID=@CompanyID
	 and AR_ReceiptTransactionHeader.PeriodID=@PeriodID
	 And AR_ReceiptTransactionHeader.Status=1
	 And (Set_Broker.CustomerID = @FromBrokerID or  @FromBrokerID is null)
	 And (AR_SET_Bank.BankID=@BankID or @BankID is null)
     And (Set_Broker.CustomerID = @BrokerID or @BrokerID is null)
    and SET_DocumentType.DocumentType like '%collection%' 

Union All
Select distinct
    AR_ReceiptTransactionHeader.ReceiptTransactionID,
	AR_ReceiptTransactionHeader.DocumentDate,
    AR_ReceiptBanksDetail.Amount as BankAmount,
    AR_ReceiptBanksDetail.BankAccountID,
    AR_ReceiptTransactionHeader.Amount,
	AR_ReceiptTransactionHeader.CustomerID,
    AR_ReceiptTransactionHeader.BusinessPartnerTypeID,
    AR_SET_Customer.CustomerName, 
    AR_ReceiptTransactionHeader.Amount as ReceiptHeaderAmount,
		AR_ReceiptTransactionHeader.DocumentNo,
    AR_ReceiptTransactionHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    (AR_SET_Bank.BankName +'-'+    AR_SET_BankBranch.BankBranchName+'-'+ AR_SET_BankAccount.BankAccountName) as BankNameAcc,
		SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName 
, Set_Broker.CustomerName as BrokerName
	from AR_ReceiptTransactionHeader

			inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID=AR_ReceiptTransactionHeader.ReceiptTransactionID
			    and AR_ReceiptTransactionHeader.CompanyID= AR_ReceiptBanksDetail.CompanyID
				and AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptBanksDetail.PeriodID
				and AR_ReceiptBanksDetail.Status=1

				inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID=AR_ReceiptBanksDetail.BankBranchID
				And AR_SET_BankBranch.Status=1
				inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID
				And AR_SET_Bank.Status=1
				inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID=AR_ReceiptBanksDetail.BankAccountID
				    and  AR_SET_BankAccount.CompanyID= AR_ReceiptBanksDetail.CompanyID
					and  AR_SET_BankAccount.PeriodID= AR_ReceiptBanksDetail.PeriodID
					And AR_SET_BankAccount.Status=1
				inner join AR_SET_Customer on AR_SET_Customer.CustomerID= AR_ReceiptTransactionHeader.CustomerID
				And AR_SET_Customer.Status=1

					 left Join AR_SET_Customer Set_Broker on Set_Broker.CustomerID=AR_SET_Customer.BrokerID
     And Set_Broker.CompanyID=AR_SET_Customer.CompanyID
     And Set_Broker.PeriodID= AR_SET_Customer.PeriodID
     And Set_Broker.Status=1
				
	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AR_ReceiptTransactionHeader.DocumentTypeID
and Set_DocumentType.CompanyID= AR_ReceiptTransactionHeader.CompanyID

	 	 inner join SET_Company on SET_Company.CompanyId=AR_ReceiptTransactionHeader.CompanyID
	 and SET_Company.Status=1
	 	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1
	 	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 where AR_ReceiptTransactionHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	 and  AR_ReceiptTransactionHeader.CompanyID=@CompanyID
	 and AR_ReceiptTransactionHeader.PeriodID=@PeriodID
	 And AR_ReceiptTransactionHeader.Status=1
	 And (Set_Broker.CustomerID = @FromBrokerID or  @FromBrokerID is null)
	 And (AR_SET_Bank.BankID=@BankID or @BankID is null)
     And (Set_Broker.CustomerID = @BrokerID or @BrokerID is null)
	 and SET_DocumentType.DocumentType like '%collection%' 
			)as fintab 
group by
    fintab.ReceiptTransactionID,
	fintab.DocumentDate,
    fintab.BankAccountID,
    fintab.CustomerID,
    fintab.BusinessPartnerTypeID,
    fintab.CustomerName, 
    fintab.ReceiptHeaderAmount,
	fintab.DocumentNo,
    fintab.DocumentTypeID,
		fintab.DocumentType,
    fintab.BankNameAcc,
		fintab.CompanyName ,
			fintab.BrokerName    ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_CollectionRecieptBankWiseBrokerReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dt;
        }

        public bool ReverseApprovedReceipt(List<AR_ReceiptTransactionDetail> colReceiptDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove dA, List<AR_ReceiptAccountsDetail> colReceiptAccounts, List<AR_ReceiptBanksDetail> colReceiptBanks, List<AR_ReceiptCustomerDetail> colReceiptCustomer, List<AR_ReceiptInvoicesDetail> colReceiptInvoiceDetail, Nullable<DateTime> dt_ReverseDate, DateTime currentDate, Guid currentUserID, string currentUserName, out int ReversalDocumentNo, string Comments, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("CancelOrReverseReceipt");

            try
            {
                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                ReversalDocumentNo = newDocumentNo;

                #region Update Receipt

                if (!dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))   //  Cancellation
                    this.Status = false;

                //this.DocumentDate = Convert.ToDateTime(dt_ReverseDate.ToString());
                this.ReceiptDate = Convert.ToDateTime(dt_ReverseDate.ToString());
                this.ReversalDate = dt_ReverseDate;
                this.ReversalComments = Comments + "  (Reversal document no.: " + newDocumentNo.ToString() + ").";
                this.UpdateDataByKey(sqltrans);

                #endregion

                #region Update Sales Invoice (DI-Invoice)

                if (this.SourceHeaderID.HasValue && this.DocumentTypeID_DocumentType.DocumentType.ToLower().StartsWith("receipt against di"))
                {
                    using (SAL_InvoiceHeader obj_SalesInvoice = new SAL_InvoiceHeader())
                    {
                        obj_SalesInvoice.UpdateFlagHeader(this.f_CompanyID, this.f_PeriodID, this.f_SourceHeaderID.Value, false, currentUserID, currentDate,
                            currentUserName, sqltrans);
                    };
                }

                #endregion

                #region Insert In AR_ReceiptTransaction Header And Detail, When ApprovalStatus = "Reversed"

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))
                {
                    #region Insert In AR_ReceiptTransactionHeader

                    this.CreateDate = currentDate; //this.UpdateDate.Value;
                    this.CreatedBy = currentUserID;
                    this.CreatedByValue = currentUserName;
                    this.UpdateDate = null;
                    this.UpdatedBy = null;
                    this.UpdatedByValue = null;
                    this.ReversalDate = null;
                    this.ReversalComments = Comments + "  (This document is generated against the Reversal process).";
                    this.Amount = this.Amount * -1;
                    this.SourceHeaderID = this.ReceiptTransactionID;
                    this.DocumentNo = newDocumentNo;
                    this.DocumentDate = dt_ReverseDate.Value;
                    this.ReceiptTransactionID = AR_ReceiptTransactionHeader.GetNewReceiptTransactionID(f_CompanyID, f_PeriodID, sqltrans);

                    this.Insert(sqltrans);

                    #endregion

                    #region Insert In SET_DocumentApprove

                    dA.Status = true;
                    dA.DocumentNo = newDocumentNo.ToString();
                    dA.FiscalID = f_FiscalID;
                    dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    dA.Insert(sqltrans);

                    #endregion

                    #region Insert In AR_ReceiptTransactionDetail

                    short receiptDetailID = short.Parse(AR_ReceiptTransactionDetail.GetNewReceiptDetailID(CompanyID, PeriodID, ReceiptTransactionID,
                        sqltrans).ToString());

                    foreach (AR_ReceiptTransactionDetail ReceiptDetail in colReceiptDetail)
                    {
                        ReceiptDetail.ReceiptHeaderID = this.ReceiptTransactionID;
                        ReceiptDetail.BaseAmount = ReceiptDetail.BaseAmount * -1;
                        ReceiptDetail.DiscountAmount = ReceiptDetail.DiscountAmount * -1;
                        ReceiptDetail.GrossAmount = ReceiptDetail.GrossAmount * -1;
                        ReceiptDetail.NetAmount = ReceiptDetail.NetAmount * -1;
                        ReceiptDetail.TaxAmount = ReceiptDetail.TaxAmount * -1;
                        ReceiptDetail.ReceiptDetailID = receiptDetailID++;
                        ReceiptDetail.FiscalID = f_FiscalID;
                        ReceiptDetail.Insert(sqltrans);
                    }

                    #endregion

                    #region Insert In GL_TransactionHeader

                    string originalNarration = th.Narration != null ? th.Narration : "";
                    if (th.Narration != null)
                        th.Narration = th.Narration + (th.Narration.Trim().EndsWith(".") ? " " : ". ") + "This document is reversed (Reversed document no.: "
                            + newDocumentNo.ToString() + ").";
                    else
                        th.Narration = "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";

                    th.UpdateDataByKey(sqltrans);

                    th.ReversalID = th.TransactionID;

                    if (th.Narration != null)
                    {
                        th.Narration = Comments + " " + originalNarration + (originalNarration.Trim().EndsWith(".") ? " " : ". ")
                            + "This document is generated against the Reversal process. Original Document No.: " + th.DocumentNo.ToString();
                    }
                    else
                        th.Narration = Comments + "  (This document is generated against the Reversal process. Original Document No.: " + th.DocumentNo.ToString() + ")";

                    th.DocumentNo = newDocumentNo;
                    th.DocumentDate = dt_ReverseDate.Value;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = f_FiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.ReceiptTransactionID;
                        td.FiscalID = f_FiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert in Receipt Accounts table

                if (colReceiptAccounts != null)
                {
                    short receiptAccountID = (short)AR_ReceiptAccountsDetail.GetNewReceiptAccountID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptAccountsDetail objReceiptAccount in colReceiptAccounts)
                    {
                        objReceiptAccount.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptAccount.ReceiptAccountID = receiptAccountID++;
                        objReceiptAccount.FiscalID = f_FiscalID;
                        objReceiptAccount.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Banks table

                if (colReceiptBanks != null)
                {
                    short receiptBankID = (short)AR_ReceiptBanksDetail.GetNewReceiptBankID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptBanksDetail objReceiptBank in colReceiptBanks)
                    {
                        objReceiptBank.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptBank.ReceiptBankID = receiptBankID++;
                        objReceiptBank.FiscalID = f_FiscalID;
                        objReceiptBank.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt Customer table

                if (colReceiptCustomer != null)
                {
                    short receiptCustomerID = (short)AR_ReceiptCustomerDetail.GetNewReceiptCustomerID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptCustomerDetail objReceiptCustomer in colReceiptCustomer)
                    {
                        objReceiptCustomer.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptCustomer.ReceiptCustomerID = receiptCustomerID++;
                        objReceiptCustomer.FiscalID = f_FiscalID;
                        objReceiptCustomer.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Receipt InvoiceDetail table

                if (colReceiptInvoiceDetail != null)
                {
                    short ReceiptInvoicesID = (short)AR_ReceiptInvoicesDetail.GetNewReceiptInvoicesID(f_CompanyID, f_PeriodID, this.ReceiptTransactionID, sqltrans);

                    foreach (AR_ReceiptInvoicesDetail objReceiptCustomer in colReceiptInvoiceDetail)
                    {
                        objReceiptCustomer.ReceiptHeaderID = this.ReceiptTransactionID;
                        objReceiptCustomer.ReceiptInvoicesID = ReceiptInvoicesID++;
                        objReceiptCustomer.FiscalID = f_FiscalID;
                        objReceiptCustomer.Insert(sqltrans);
                    }
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
                    SqlHelper.RollBackTransaction(sqltrans, "CancelOrReverseReceipt");
            }

            return result;
        }

        public bool InProcessDocument(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessReceipt");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);
                GL_TransactionDetail.DeletePermenantDataByKeyTranasctionID(sqltrans, CompanyID, PeriodID, th.TransactionID);
                GL_TransactionHeader.Delete_By_PrimaryKey(CompanyID, PeriodID, th.TransactionID, sqltrans);
                SET_DocumentApprove.DeleteApprovalHistroy(CompanyID, PeriodID, this.DocumentNo, this.DocumentTypeID, sqltrans, int.Parse(this.FiscalID.ToString()));
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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessReceipt");
            }

            return result;
        }

        public static DataTable GET_CustomerToCustomerReceipt(int companyID, int periodID, Nullable<int> customer, Nullable<DateTime> datefrom, Nullable<DateTime> dateto,int FiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (customer != null)
                {
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + customer);
                }
                
                if (datefrom != null && dateto != null)
                {
                    sb.Append(" AND AR_ReceiptTransactionHeader.DocumentDate Between @FromDate AND @ToDate ");
                }
                sb.Append(" and AR_ReceiptTransactionHeader.FiscalID = " + FiscalID + " ");

                sb.Append(" Order By AR_ReceiptTransactionHeader.DocumentNo asc");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.CustomerToCustomerReceipt_Query, sb), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public DataTable BankChargesReportData(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> fromBrokerID, Nullable<int> bankID, Nullable<int> bankBranchID, Nullable<int> bankAccountNo, int DocumentType, Nullable<short> documentGroupID = null, int statuscode = 0, string TypeFilter = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (fromBrokerID.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", fromBrokerID);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@FromBrokerID", DBNull.Value);
                }

                if (fromDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", fromDate);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@FromDocumentDate ", DBNull.Value);
                }
                if (fromDate.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", toDate);
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@ToDocumentDate ", DBNull.Value);
                }

                if (bankID.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@BankID", bankID);
                }
                else
                {
                    sqlparams[5] = new SqlParameter("@BankID", DBNull.Value);
                }

                if(bankBranchID != null)
                    sb.Append(" And AR_SET_BankBranch.BankBranchID = " + bankBranchID);

                if (bankAccountNo != null)
                    sb.Append(" AND AR_SET_BankAccount.BankAccountID = " + bankAccountNo + " ");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (DocumentType != -1)
                    sb.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentType + " ");

                if (statuscode > 0)
                {
                    sb.Append(" and AR_ReceiptTransactionHeader.statuscode =  " + statuscode);
                }

                if (TypeFilter != "")
                {
                    if (TypeFilter == "expense")
                        sb.Append(" and GL_CodeCombinationDetail.BankChargesAccount = 1");
                    else if (TypeFilter == "profit")
                        sb.Append(" and GL_CodeCombinationDetail.BankProfitAccount = 1");
                }


                #region Query

                string Query_CollectionRecieptReport = @"select BankName,GroupName,CodeCombinationID as BankAccountCode,CompanyName,BankAmount,Description,BankAccountNumber as AccountNo  from (

Select 
    AP_PaymentHeader.PaymentID,
	AP_PaymentHeader.DocumentDate,
	AP_PaymentBanksDetail.InstrumentNo,
    
    
    case when AP_Payment_AccountsDetail.TransactionType = 'Credit' then  AP_Payment_AccountsDetail.Amount  *-1 else AP_Payment_AccountsDetail.Amount  end as BankAmount,
    AP_PaymentBanksDetail.BankAccountID,
    
    AP_PaymentHeader.Amount as ReceiptHeaderAmount,
	AP_PaymentHeader.DocumentNo,
    AP_PaymentHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    (AR_SET_BankAccount.BankAccountName) as BankNameAcc,
	SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName
		,AR_SET_Bank.BankName,AP_Payment_AccountsDetail.CodeCombinationID,DocumentTypesGroup.GroupName
        ,GL_CodeCombinationDetail.Description,AR_SET_BankAccount.BankAccountNumber
from AP_PaymentHeader
    inner join AP_PaymentBanksDetail on AP_PaymentBanksDetail.PaymentHeaderID= AP_PaymentHeader.PaymentID
    and AP_PaymentHeader.CompanyID= AP_PaymentBanksDetail.CompanyID
    and AP_PaymentHeader.PeriodID=AP_PaymentBanksDetail.PeriodID
    and AP_PaymentBanksDetail.Status=1
	and AP_PaymentBanksDetail.PaymentBankID = (select top 1 PaymentBankID from AP_PaymentBanksDetail dtl where dtl.PaymentHeaderID =  AP_PaymentBanksDetail.PaymentHeaderID and dtl.CompanyID =  AP_PaymentBanksDetail.CompanyID and dtl.status=1 )
	

    inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID= AP_PaymentBanksDetail.BankAccountID  and AR_SET_BankAccount.BankBranchID= AP_PaymentBanksDetail.BankBranchID
    
        And AR_SET_BankAccount.Status=1


     inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID= AP_PaymentBanksDetail.BankBranchID
     inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID 

	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AP_PaymentHeader.DocumentTypeID
	 AND Set_DocumentType.CompanyID=AP_PaymentHeader.CompanyID
	 AND Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
	 AND Set_DocumentType.Status=1

	 inner join DocumentTypesGroup on DocumentTypesGroup .GroupID = Set_DocumentType.GroupID

	 inner join SET_Company on SET_Company.CompanyId=AP_PaymentHeader.CompanyID
	 and SET_Company.Status=1

	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1

	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 

	 inner join AP_Payment_AccountsDetail on AP_Payment_AccountsDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
	 and AP_Payment_AccountsDetail.CompanyID = AP_PaymentHeader.CompanyID
	 and AP_Payment_AccountsDetail.PeriodID = AP_PaymentHeader.PeriodID
	 and AP_Payment_AccountsDetail.Status = 1


	 inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_Payment_AccountsDetail.CodeCombinationID

	where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID
	and AP_PaymentHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	And (AR_SET_Bank.BankID=@BankID or @BankID is null)
	 And AP_PaymentHeader.Status=1
	 {0}

	 union all

	 Select 
    AR_ReceiptTransactionHeader.ReceiptTransactionID,
	AR_ReceiptTransactionHeader.DocumentDate,
	AR_ReceiptBanksDetail.InstrumentNo,
    
    case when AR_ReceiptAccountsDetail.TransactionType = 'Credit' then  AR_ReceiptAccountsDetail.Amount  *-1 else AR_ReceiptAccountsDetail.Amount  end as BankAmount,
    AR_ReceiptBanksDetail.BankAccountID,
    
    AR_ReceiptTransactionHeader.Amount as ReceiptHeaderAmount,
	AR_ReceiptTransactionHeader.DocumentNo,
    AR_ReceiptTransactionHeader.DocumentTypeID,
	Set_DocumentType.DocumentType,
    (AR_SET_BankAccount.BankAccountName) as BankNameAcc,
	SET_Company.BusinessId,
	SET_Company.OperatingUnitId,
		(SET_Business.BusinessName+'('+	SET_OperatingUnit.OperatingUnitName+')') as CompanyName
		,AR_SET_Bank.BankName,AR_ReceiptAccountsDetail.CodeCombinationID,DocumentTypesGroup.GroupName
        ,GL_CodeCombinationDetail.Description,AR_SET_BankAccount.BankAccountNumber
from AR_ReceiptTransactionHeader
    inner join AR_ReceiptBanksDetail on AR_ReceiptBanksDetail.ReceiptHeaderID= AR_ReceiptTransactionHeader.ReceiptTransactionID
    and AR_ReceiptTransactionHeader.CompanyID= AR_ReceiptBanksDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID=AR_ReceiptBanksDetail.PeriodID
    and AR_ReceiptBanksDetail.Status=1
	and AR_ReceiptBanksDetail.ReceiptBankID = (select top 1 ReceiptBankID from AR_ReceiptBanksDetail dtl where dtl.ReceiptHeaderID =  AR_ReceiptBanksDetail.ReceiptHeaderID and dtl.CompanyID =  AR_ReceiptBanksDetail.CompanyID and dtl.status=1 )
	

    inner join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID= AR_ReceiptBanksDetail.BankAccountID  and AR_SET_BankAccount.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
    
        And AR_SET_BankAccount.Status=1


     inner join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID= AR_ReceiptBanksDetail.BankBranchID
     inner join AR_SET_Bank on AR_SET_Bank.BankID=AR_SET_BankBranch.BankID 

	 inner join Set_DocumentType on Set_DocumentType.DocumentTypeID= AR_ReceiptTransactionHeader.DocumentTypeID
	 AND Set_DocumentType.CompanyID=AR_ReceiptTransactionHeader.CompanyID
	 AND Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
	 AND Set_DocumentType.Status=1

	 inner join DocumentTypesGroup on DocumentTypesGroup .GroupID = Set_DocumentType.GroupID

	 inner join SET_Company on SET_Company.CompanyId=AR_ReceiptTransactionHeader.CompanyID
	 and SET_Company.Status=1

	 left join SET_Business on SET_Business.BusinessId= SET_Company.BusinessId
	 and SET_Business.Status=1

	 left join 	SET_OperatingUnit on 	SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
	 and SET_OperatingUnit.Status=1

	 

	 inner join AR_ReceiptAccountsDetail on AR_ReceiptAccountsDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
	 and AR_ReceiptAccountsDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
	 and AR_ReceiptAccountsDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
	 and AR_ReceiptAccountsDetail.Status = 1


	 inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AR_ReceiptAccountsDetail.CodeCombinationID

	 where AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
	 and AR_ReceiptTransactionHeader.DocumentDate Between @FromDocumentDate And @ToDocumentDate
	 And (AR_SET_Bank.BankID=@BankID or @BankID is null)
	  And AR_ReceiptTransactionHeader.Status=1
	 {0}


	 ) as tab ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_CollectionRecieptReport, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dt;
        }


        public static DataTable GetAdvanceAmountforDashboard(int CompanyID, int periodID,int FiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);


                #region Query

                string Query_DashBoard_GetAdvanceAmount = @"select  sum(AR_ReceiptTransactionHeader.Amount) as AdvanceAmount
from AR_ReceiptTransactionHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
where SET_DocumentType.DocumentType like '%Advance Against%'
and AR_ReceiptTransactionHeader.CompanyID = @CompanyID
and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.StatusCode = 1

 ";

                #endregion
                if (FiscalID > 0)
                    sb.Append(" and AR_ReceiptTransactionHeader.FiscalID = @FiscalID");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DashBoard_GetAdvanceAmount + sb.ToString(), sqlparams);

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


        public static Double CheckAmountForAdvanceAgainstSOCollection(int companyID, int periodID, int sourceHeaderID, int CustomerID = 0)
        {
            double Amount = 0;
            string Filters = "";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                if (CustomerID != 0 && CustomerID != -1)
                    Filters = " and AR_ReceiptTransactionHeader.CustomerID = " + CustomerID;

                #region Query

                #endregion

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_CheckAmount_AdvanceAgainstSOCollection, Filters), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }


        public static Decimal CheckAmountForAdvanceAlreadyKnock(int companyID, int periodID, int ContractID)
        {
            decimal Amount = 0;
            string Filters = "";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractID", ContractID);

                
                #region Query

                #endregion

                Amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_CheckAmount_AlreadyKnock, Filters), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public static DataTable GetAllDataWithOtherTablesBankReconciliation(int companyID, int periodID, int CurrentFiscalID, string Where, Nullable<int> DocumentTypeID = null, Nullable<int> bankID = null, Nullable<int> branchID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                #region Query

                string GetAllDataWithOtherTablesBankReconciliation = FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_BankReconciliation;

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(GetAllDataWithOtherTablesBankReconciliation, Where), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }

        public static DataTable GetAllDataWithOtherTablesBankReconciliationForSearch(int companyID, int periodID, int CurrentFiscalID, string Where, Nullable<int> DocumentTypeID = null, Nullable<int> bankID = null, Nullable<int> branchID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@BankID", bankID);
                sqlparams[4] = new SqlParameter("@BranchID", branchID);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                #region Query

                string GetAllDataWithOtherTablesBankReconciliation = @" 
Select Distinct tab2.DocumentTypeID, tab2.DocumentNo, tab2.DocumentDate,tab2.DocumentType
--tab2.DocumentTypeID,tab2.ActivityDate, tab2.ReceiptID, tab2.PaymentID, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, SUM(tab2.ReceiptAmount) ReceiptAmount, 
--SUM(tab2.PaymentAmount) PaymentAmount, tab2.DocumentType, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber,tab2.IssueDate,Set_PeriodCustom.PeriodSetName,Narration  
From 
(
	Select AR_ReceiptTransactionHeader.DocumentTypeID,AR_ReceiptTransactionHeader.ActivityDate, AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID, 0 as PaymentID, AR_Set_bankAccount.BankAccountID, 
        AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, AR_ReceiptTransactionHeader.Amount ReceiptAmount, 0 as PaymentAmount, 
        Set_DocumentType.DocumentType, AR_ReceiptTransactionHeader.PaymentServerOrderNo As InstrumentNo, 
        IsNull(AR_ReceiptTransactionHeader.Comments, '') + IsNull(AR_ReceiptTransactionHeader.ReversalComments, '') As Comments, AR_Set_bankAccount.BankAccountNumber 
        ,AR_ReceiptTransactionHeader.Comments AS Narration
        ,AR_ReceiptTransactionHeader.IssueDate
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.CompanyId = AR_ReceiptTransactionHeader.CompanyId 
            And AR_Set_bankAccount.PeriodID = AR_ReceiptTransactionHeader.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptTransactionHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 AND AR_ReceiptTransactionHeader.BankAccountID is not null AND  ( AR_Set_BankBranch.BankID = @BankID OR @BankID is null)
        AND (AR_Set_BankBranch.BankBranchID = @BranchID OR @BranchID is null) --AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate 
	    AND (AR_ReceiptTransactionHeader.DocumentTypeID=@DocumentTypeID  OR @DocumentTypeID is null)
	UNION ALL 
	
	Select AP_PaymentHeader.DocumentTypeID,AP_PaymentHeader.ActivityDate, 0 as ReceiptID, AP_PaymentHeader.PaymentID, AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, 
        AP_PaymentHeader.DocumentNo, 0 as ReceiptAmount, AP_PaymentHeader.Amount as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentHeader.PaymentServerOrderNo As InstrumentNo, IsNull(AP_PaymentHeader.Comments, '') + IsNull(AP_PaymentHeader.ReversalComments, '') As Comments, 
        AR_Set_bankAccount.BankAccountNumber
        ,AP_PaymentHeader.Comments AS Narration
        ,AP_PaymentHeader.IssueDate 
	From AP_PaymentHeader 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.CompanyId = AP_PaymentHeader.CompanyId And AR_Set_bankAccount.PeriodID = AP_PaymentHeader.PeriodID 
            And AR_Set_bankAccount.BankAccountID = AP_PaymentHeader.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
	WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
        AND AP_PaymentHeader.BankAccountID is not null 
        AND  ( AR_Set_BankBranch.BankID = @BankID OR @BankID is null)
        AND (AR_Set_BankBranch.BankBranchID = @BranchID OR @BranchID is null) 
       --  AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AP_PaymentHeader.DocumentDate <= @FromDate  
          AND (AP_PaymentHeader.DocumentTypeID=@DocumentTypeID  OR @DocumentTypeID is null)
    UNION ALL 
    
    Select  AR_ReceiptTransactionHeader.DocumentTypeID,AR_ReceiptTransactionHeader.ActivityDate,AR_ReceiptTransactionHeader.ReceiptTransactionID as ReceiptID,0 as PaymentID,AR_Set_bankAccount.BankAccountID, AR_ReceiptTransactionHeader.DocumentDate, AR_ReceiptTransactionHeader.DocumentNo, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Debit' AND AR_ReceiptBanksDetail.Status = 1 then AR_ReceiptBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AR_ReceiptBanksDetail.TransactionType = 'Credit' AND AR_ReceiptBanksDetail.Status = 1 then AR_ReceiptBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AR_ReceiptBanksDetail.InstrumentNo, AR_ReceiptBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
        ,AR_ReceiptBanksDetail.Comments as Narration 
        ,AR_ReceiptTransactionHeader.IssueDate
	From AR_ReceiptTransactionHeader 
		INNER JOIN AR_ReceiptBanksDetail ON AR_ReceiptBanksDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
			AND AR_ReceiptBanksDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			AND AR_ReceiptBanksDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
		INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.CompanyId = AR_ReceiptBanksDetail.CompanyId 
            And AR_Set_bankAccount.PeriodID = AR_ReceiptBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AR_ReceiptBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
		Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
            And Set_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID And AR_ReceiptTransactionHeader.Status = 1 
        AND AR_ReceiptTransactionHeader.StatusCode = 1 
       --  AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID  AND AR_ReceiptTransactionHeader.DocumentDate <= @FromDate  
          AND  ( AR_Set_BankBranch.BankID = @BankID OR @BankID is null)
         AND (AR_Set_BankBranch.BankBranchID = @BranchID OR @BranchID is null) 
	     AND (AR_ReceiptTransactionHeader.DocumentTypeID=@DocumentTypeID  OR @DocumentTypeID is null)

	UNION ALL 
	
	Select AP_PaymentHeader.DocumentTypeID,AP_PaymentHeader.ActivityDate,0 as ReceiptID,AP_PaymentHeader.PaymentID as PaymentID,AR_Set_bankAccount.BankAccountID, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.DocumentNo, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Debit' AND AP_PaymentBanksDetail.Status = 1 then AP_PaymentBanksDetail.Amount else 0 end) as ReceiptAmount, 
        (Case when AP_PaymentBanksDetail.TransactionType = 'Credit' AND AP_PaymentBanksDetail.Status = 1 then AP_PaymentBanksDetail.Amount else 0 end) as PaymentAmount, Set_DocumentType.DocumentType, 
        AP_PaymentBanksDetail.InstrumentNo, AP_PaymentBanksDetail.Comments, AR_Set_bankAccount.BankAccountNumber 
        , AP_PaymentBanksDetail.Comments AS Narration
        ,AP_PaymentHeader.IssueDate
    From AP_PaymentHeader 
	    INNER JOIN AP_PaymentBanksDetail ON AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID 
            AND AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID AND AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID 
	    INNER JOIN AR_Set_bankAccount On AR_Set_bankAccount.CompanyId = AP_PaymentBanksDetail.CompanyId 
            And AR_Set_bankAccount.PeriodID = AP_PaymentBanksDetail.PeriodID And AR_Set_bankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID 
        INNER JOIN AR_Set_BankBranch On AR_Set_BankBranch.BankBranchID = AR_Set_BankAccount.BankBranchID 
	    Inner Join Set_DocumentType On Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID And Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID 
            And Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
    WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.StatusCode = 1 
            AND (AP_PaymentHeader.DocumentTypeID=@DocumentTypeID  OR @DocumentTypeID is null)
      ---   AND AR_Set_BankBranch.BankID = @BankID  AND AR_Set_BankBranch.BankBranchID = @BranchID -- AND AP_PaymentHeader.DocumentDate <= @FromDate  
   AND  ( AR_Set_BankBranch.BankID = @BankID OR @BankID is null)   AND (AR_Set_BankBranch.BankBranchID = @BranchID OR @BranchID is null) 
         
)tab2 
inner join Set_PeriodCustom on tab2.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
        and Set_PeriodCustom.Status = 1
        AND Set_PeriodCustom.FiscalID= @CurrentFiscalID
where (Tab2.ActivityDate is not null OR Tab2.IssueDate is not null)
{0}
Group By tab2.DocumentType, tab2.BankAccountID, tab2.DocumentNo, tab2.DocumentDate, tab2.InstrumentNo, tab2.Comments, tab2.BankAccountNumber
,tab2.ReceiptID,tab2.PaymentID,tab2.ActivityDate,tab2.IssueDate,Set_PeriodCustom.PeriodSetName ,tab2.DocumentTypeID,Narration

order by tab2.DocumentDate

";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(GetAllDataWithOtherTablesBankReconciliation, Where), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }
    }
}
