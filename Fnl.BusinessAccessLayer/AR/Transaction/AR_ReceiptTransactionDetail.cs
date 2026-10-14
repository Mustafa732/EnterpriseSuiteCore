using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptTransactionDetail : BaseClass
    {
        #region Fields
        
        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #endregion

        #region Methods

        public DataTable GetInvoiceHeaderID(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_GetInvoiceHeaderID = @" Select InvoiceHeaderID, * from AR_ReceiptTransactionDetail 
Where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceHeaderID, sqlparams);

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

        public DataTable GetInvoiceAmountFromSIHeader(int companyID, int periodID, int customerID, int invoiceID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                sqlparams[3] = new SqlParameter("@InvoiceId", invoiceID);

                string GetInvoiceSIHeader = @" 
SELECT InvoiceNumber, CustomerID, Set_DocumentType.DocumentType As InvoiceType, InvoiceDate, InvoiceID, AmountPaid as InvoiceAmount, GrossAmount, DiscountAmountTaken 
FROM SAL_InvoiceHeader 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID 
        And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID AND Set_DocumentType.Status = 1 
WHERE SAL_InvoiceHeader.StatusCode = 1 AND (SAL_InvoiceHeader.CustomerID = @CustomerID OR @CustomerID is null) AND SAL_InvoiceHeader.PeriodID = @PeriodID 
    AND SAL_InvoiceHeader.CompanyID = @CompanyID and SAL_InvoiceHeader.Status = 1 and SAL_InvoiceHeader.PaymentStatusType is null 
    and SAL_InvoiceHeader.InvoiceId = @InvoiceId AND SAL_InvoiceHeader.IsPaymentGenerated = 0 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, GetInvoiceSIHeader, sqlparams);

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

        public DataTable GetInvoiceHeaderIDForReceiptInvoice(int companyID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_GetInvoiceHeaderIDFromReceiptInvoices = @" Select InvoiceHeaderID, * from AR_ReceiptInvoicesDetail 
    inner join AR_ReceiptTransactionheader on AR_ReceiptTransactionheader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID 
        and AR_ReceiptTransactionheader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID and AR_ReceiptTransactionheader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID 
Where AR_ReceiptInvoicesDetail.CompanyID = @CompanyID and AR_ReceiptInvoicesDetail.PeriodID = @PeriodID and AR_ReceiptInvoicesDetail.Status = 1 
    and AR_ReceiptTransactionheader.ReceiptDate is null ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceHeaderIDFromReceiptInvoices, sqlparams);

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

        public DataTable GetDataByKey_FromHeader(int companyID, int periodID, int receiptHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);

                #region Query

                string Query_GetData_byKey_FromHeader = @" 
SELECT AR_ReceiptTransactionDetail.ReceiptDetailID, AR_ReceiptTransactionDetail.ReceiptHeaderID, AR_ReceiptTransactionDetail.CompanyID, 
    AR_ReceiptTransactionDetail.PeriodID, AR_ReceiptTransactionDetail.SerialNo, AR_ReceiptTransactionDetail.ItemCategoryID, 
    INV_SET_Category.CategoryName AS ItemCategoryName, AR_ReceiptTransactionDetail.ItemID, INV_Item.Description AS ItemDescription, SET_UnitofMeasure.UOMName, 
    AR_ReceiptTransactionDetail.ItemUnitID, AR_ReceiptTransactionDetail.ItemUnitQuantity, AR_ReceiptTransactionDetail.ItemQuality, 
    AR_ReceiptTransactionDetail.BaseQuantity, AR_ReceiptTransactionDetail.Quantity, AR_ReceiptTransactionDetail.BaseAmount, AR_ReceiptTransactionDetail.Rate, 
    AR_ReceiptTransactionDetail.GrossAmount, AR_ReceiptTransactionDetail.TaxAmount, AR_ReceiptTransactionDetail.DiscountAmount, AR_ReceiptTransactionDetail.NetAmount, 
    AR_ReceiptTransactionDetail.Status, AR_ReceiptTransactionDetail.InvoiceDetailID, AR_ReceiptTransactionDetail.InvoiceHeaderID, 
    AR_ReceiptTransactionDetail.InvoiceDocumentNo, AR_ReceiptTransactionDetail.CreatedBy, AR_ReceiptTransactionDetail.CreateDate, 
    AR_ReceiptTransactionDetail.CreatedByValue, AR_ReceiptTransactionDetail.UpdatedBy, AR_ReceiptTransactionDetail.UpdateDate, 
    AR_ReceiptTransactionDetail.UpdatedByValue, SAL_InvoiceHeader.InvoiceNumber, SAL_InvoiceDetail.UnitPrice, AR_ReceiptTransactionDetail.Recv_Amount, 
    0 as RowUniqueID, '' AS Action 
FROM AR_ReceiptTransactionDetail 
    INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceDetailID = AR_ReceiptTransactionDetail.InvoiceDetailID 
        AND SAL_InvoiceDetail.InvoiceHeaderID = AR_ReceiptTransactionDetail.InvoiceHeaderID AND SAL_InvoiceDetail.CompanyID = AR_ReceiptTransactionDetail.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = AR_ReceiptTransactionDetail.PeriodID AND SAL_InvoiceDetail.Status = 1 
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID AND SAL_InvoiceHeader.Status = 1 
    INNER JOIN INV_SET_Category ON 
--AR_ReceiptTransactionDetail.CompanyID = INV_SET_Category.CompanyID AND 
AR_ReceiptTransactionDetail.Status = 1 
        and AR_ReceiptTransactionDetail.ItemCategoryID = INV_SET_Category.CategoryID 
    INNER JOIN INV_Item ON AR_ReceiptTransactionDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 
    INNER JOIN SET_UnitofMeasure ON AR_ReceiptTransactionDetail.ItemUnitID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
WHERE AR_ReceiptTransactionDetail.CompanyID = @CompanyID and AR_ReceiptTransactionDetail.PeriodID = @PeriodID 
    and AR_ReceiptTransactionDetail.ReceiptHeaderID = @ReceiptHeaderID AND AR_ReceiptTransactionDetail.Status = 1 ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_FromHeader, sqlparams);

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

        public DataTable GetDataByKey_FromInvoice(int companyID, int periodID, int invoiceHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);

                string Query_GetDtail_Invoice_ByInvoiceHeaderKey = @" 
SELECT SAL_InvoiceDetail.CompanyID, SAL_InvoiceDetail.PeriodID, SAL_InvoiceDetail.SerialNumber AS SerialNo, INV_Item.ItemTypeID AS ItemCategoryID, 
    INV_SET_Category.CategoryName AS ItemCategoryName, SAL_InvoiceDetail.itemid, INV_Item.Description AS ItemDescription, SET_UnitofMeasure.UOMName, 
    SAL_InvoiceDetail.ItemUnitID, SAL_InvoiceDetail.QuantityInvoiced AS ItemUnitQuantity, '' AS ItemQuality, SAL_InvoiceDetail.QuantityInvoiced AS BaseQuantity, 
    SAL_InvoiceDetail.QuantityInvoiced AS Quantity, SAL_InvoiceDetail.BaseAmount, SAL_InvoiceDetail.UnitPrice, 0 AS Rate, SAL_InvoiceDetail.GrossAmount, 
    (ISNULL(SAL_InvoiceDetail.TaxAmount, 0) + ISNULL(SAL_InvoiceDetail.ExciseDutyAmount, 0) + ISNULL(SAL_InvoiceDetail.FEDAmount, 0)) as TaxAmount, 
    ISNULL(SAL_InvoiceDetail.DiscountAmountTaken, 0) AS DiscountAmount, SAL_InvoiceDetail.NetAmount, SAL_InvoiceDetail.InvoiceHeaderID, 
    SAL_InvoiceDetail.InvoiceDetailID, SAL_InvoiceHeader.InvoiceNumber AS DocumentNo, SAL_InvoiceHeader.FreightAmount, GL_TransactionDetail.TransactionType 
FROM SAL_InvoiceDetail 
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID AND SAL_InvoiceHeader.Status = 1 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_InvoiceDetail.ItemCategoryID 
--AND INV_SET_Category.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_InvoiceDetail.ItemID AND INV_Item.Status = 1 --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID 
    INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_InvoiceDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
    LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
        AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
    AND SAL_InvoiceDetail.Status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDtail_Invoice_ByInvoiceHeaderKey, sqlparams);

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

        public DataTable GetDataByKey_FromInvoiceAfterDudction(int companyID, int periodID, int invoiceHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);

                #region Query

                string Query_GetDtail_Invoice_ByInvoiceHeaderKey_AfterDeduction = @" 
SELECT SAL_InvoiceDetail.CompanyID, SAL_InvoiceDetail.PeriodID, SAL_InvoiceDetail.SerialNumber AS SerialNo, INV_Item.ItemTypeID AS ItemCategoryID, 
    INV_SET_Category.CategoryName AS ItemCategoryName, SAL_InvoiceDetail.itemid, INV_Item.Description AS ItemDescription, SET_UnitofMeasure.UOMName, 
    SAL_InvoiceDetail.ItemUnitID, SAL_InvoiceDetail.QuantityInvoiced - 
    (
        select ISNULL(sum(ReversedQuantity), 0) inv_StockTransactionDetail from inv_StockTransactionDetail 
        where TransactionDetailID = SAL_InvoiceDetail.StockDetailID and StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
            and CompanyID = SAL_InvoiceDetail.CompanyID and PeriodID = SAL_InvoiceDetail.PeriodID
    ) AS ItemUnitQuantity, 
    '' AS ItemQuality, SAL_InvoiceDetail.QuantityInvoiced - 
    (
		select ISNULL(sum(ReversedQuantity), 0) inv_StockTransactionDetail from inv_StockTransactionDetail 
		where TransactionDetailID = SAL_InvoiceDetail.StockDetailID and StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
            and CompanyID = SAL_InvoiceDetail.CompanyID and PeriodID = SAL_InvoiceDetail.PeriodID
    ) AS BaseQuantity, 
    SAL_InvoiceDetail.QuantityInvoiced - 
    (
		select ISNULL(sum(ReversedQuantity), 0) inv_StockTransactionDetail from inv_StockTransactionDetail 
		where TransactionDetailID = SAL_InvoiceDetail.StockDetailID and StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
            and CompanyID = SAL_InvoiceDetail.CompanyID and PeriodID = SAL_InvoiceDetail.PeriodID
    ) AS Quantity, 
    SAL_InvoiceDetail.BaseAmount - 
    (
		(
            select ISNULL(sum(ReversedQuantity), 0) inv_StockTransactionDetail from inv_StockTransactionDetail 
		    where TransactionDetailID = SAL_InvoiceDetail.StockDetailID and StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
                and CompanyID = SAL_InvoiceDetail.CompanyID and PeriodID = SAL_InvoiceDetail.PeriodID
        ) * SAL_InvoiceDetail.UnitPrice
    ) AS BaseAmount, 
    SAL_InvoiceDetail.UnitPrice, 0 AS Rate, SAL_InvoiceDetail.GrossAmount - 
    (
		(
            select ISNULL(sum(ReversedQuantity), 0) inv_StockTransactionDetail from inv_StockTransactionDetail 
		    where TransactionDetailID = SAL_InvoiceDetail.StockDetailID and StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
and CompanyID = SAL_InvoiceDetail.CompanyID
        and PeriodID = SAL_InvoiceDetail.PeriodID) * SAL_InvoiceDetail.UnitPrice
    )
    as GrossAmount, 
    (ISNULL(SAL_InvoiceDetail.TaxAmount, 0) + ISNULL(SAL_InvoiceDetail.ExciseDutyAmount, 0) + ISNULL(SAL_InvoiceDetail.FEDAmount, 0)) as TaxAmount, 
    ISNULL(SAL_InvoiceDetail.DiscountAmountTaken, 0) AS DiscountAmount, 
    
    SAL_InvoiceDetail.NetAmount 
    -
    (
		(select ISNULL(sum(ReversedQuantity),0) inv_StockTransactionDetail 
		from inv_StockTransactionDetail
		where TransactionDetailID = SAL_InvoiceDetail.StockDetailID
        and StockTransactionID = SAL_InvoiceDetail.StockHeaderID
        and CompanyID = SAL_InvoiceDetail.CompanyID
        and PeriodID = SAL_InvoiceDetail.PeriodID) * SAL_InvoiceDetail.UnitPrice
    )
    as NetAmount,
    
     SAL_InvoiceDetail.InvoiceHeaderID, 
    SAL_InvoiceDetail.InvoiceDetailID, SAL_InvoiceHeader.InvoiceNumber AS DocumentNo, SAL_InvoiceHeader.FreightAmount, GL_TransactionDetail.TransactionType 
FROM SAL_InvoiceDetail 
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID AND SAL_InvoiceHeader.Status = 1 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_InvoiceDetail.ItemCategoryID 
--AND INV_SET_Category.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_InvoiceDetail.ItemID AND INV_Item.Status = 1 --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID 
    INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_InvoiceDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
    LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
        AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
        
WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
    AND SAL_InvoiceDetail.Status = 1 ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDtail_Invoice_ByInvoiceHeaderKey_AfterDeduction, sqlparams);

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

        public DataTable GetDataByKey_FromInvoiceForAlhaj(int companyID, int periodID, int invoiceHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);

                #region Query

                string Query_GetDtail_Invoice_ByInvoiceHeaderKeyForAlhaj = @" 
select distinct * from 
(
    SELECT SAL_InvoiceDetail.CompanyID, SAL_InvoiceDetail.PeriodID, 1 AS SerialNo, 
    (
        select top 1 INV_Item.ItemTypeID from INV_Item 
            INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID 
        WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
            AND SAL_InvoiceDetail.Status = 1
    ) as ItemCategoryID, 
    '' AS ItemCategoryName, 
    (
        select top 1 INV_Item.ItemID from INV_Item 
            INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1  --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID 
        WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
            AND SAL_InvoiceDetail.Status = 1
    ) as itemid,
    (
        select top 1 INV_Item.Description from INV_Item 
            INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 
        WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
            AND SAL_InvoiceDetail.Status = 1
    ) as ItemDescription, 
    '' as UOMName, 
    (
        select top 1 SAL_InvoiceDetail.ItemUnitID from INV_Item 
            INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 
        WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
            AND SAL_InvoiceDetail.Status = 1
    ) as ItemUnitID, 
    sum(SAL_InvoiceDetail.QuantityInvoiced) AS ItemUnitQuantity, '' AS ItemQuality, sum(SAL_InvoiceDetail.QuantityInvoiced) AS BaseQuantity, 
    sum(SAL_InvoiceDetail.QuantityInvoiced) AS Quantity, sum(SAL_InvoiceDetail.BaseAmount) as BaseAmount, sum(SAL_InvoiceDetail.UnitPrice) as UnitPrice, 0 AS Rate, 
    sum(SAL_InvoiceDetail.GrossAmount) as GrossAmount, 
    (ISNULL(sum(SAL_InvoiceDetail.TaxAmount), 0) + ISNULL(sum(SAL_InvoiceDetail.ExciseDutyAmount), 0) + ISNULL(sum(SAL_InvoiceDetail.FEDAmount), 0)) as TaxAmount, 
    ISNULL(sum(SAL_InvoiceDetail.DiscountAmountTaken), 0) AS DiscountAmount, 


        case set_Documenttype.DocumentType 
    when'Invoice Parts with Sales Tax' 
		then (GL_TransactionHeader.ControlDebit - isnull(sum(isnull(AR_ReceiptTransactionDetail.Recv_Amount,0)),0)) 
	when'Invoice Parts with Sales Tax (LCV)' 
		then (GL_TransactionHeader.ControlDebit - isnull(sum(isnull(AR_ReceiptTransactionDetail.Recv_Amount,0)),0)) 
    when 'Invoice Parts without Sales Tax' 
		then (isnull(sum(GL_TransactionHeader.ControlDebit),0) - isnull(sum(sal_DispatchHeader.OtherDiscount),0)) - isnull(sum(AR_ReceiptTransactionDetail.Recv_Amount),0) 
	when 'Invoice Parts without Sales Tax (LCV)' 
		then (isnull(sum(GL_TransactionHeader.ControlDebit),0) - isnull(sum(sal_DispatchHeader.OtherDiscount),0)) - isnull(sum(AR_ReceiptTransactionDetail.Recv_Amount),0) 
    when'Sales Invoice-X-PV' 
		then (GL_TransactionHeader.ControlDebit - isnull(sum(isnull(AR_ReceiptTransactionDetail.Recv_Amount,0)),0)) 
    else
		(GL_TransactionHeader.ControlDebit - isnull(sum(isnull(AR_ReceiptTransactionDetail.Recv_Amount,0)),0)) 

    end as NetAmount,


--(isnull(sum(GL_TransactionHeader.ControlDebit),0) - isnull(sum(sal_DispatchHeader.OtherDiscount),0)) - isnull(sum(AR_ReceiptTransactionDetail.Recv_Amount),0) as NetAmount,


SAL_InvoiceDetail.InvoiceHeaderID, 
    --SAL_InvoiceDetail.InvoiceDetailID, 
    --null as InvoiceDetailID, 
(select top 1 InvoiceDetailID from SAL_InvoiceDetail
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID AND SAL_InvoiceHeader.Status = 1 
        WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID AND SAL_InvoiceDetail.Status = 1) as InvoiceDetailID ,

    SAL_InvoiceHeader.InvoiceNumber AS DocumentNo, sum(SAL_InvoiceHeader.FreightAmount)as FreightAmount, GL_TransactionDetail.TransactionType 
FROM SAL_InvoiceDetail
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID AND SAL_InvoiceHeader.Status = 1 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_InvoiceDetail.ItemCategoryID 
--AND INV_SET_Category.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_InvoiceDetail.ItemID AND INV_Item.Status = 1 
    INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_InvoiceDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
    LEFT JOIN GL_TransactionDetail ON GL_TransactionDetail.CodeCombinationID = SAL_InvoiceHeader.FreightCode 
        AND GL_TransactionDetail.SourceHeaderID = SAL_InvoiceHeader.InvoiceID AND GL_TransactionDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND GL_TransactionDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
    
    inner join set_Documenttype on SAL_InvoiceHeader.InvoiceType = set_Documenttype.DocumentTypeID
    and SAL_InvoiceHeader.CompanyID = set_Documenttype.CompanyID
    and SAL_InvoiceHeader.PeriodID = set_Documenttype.PeriodID

    
        Left join AR_ReceiptTransactionDetail on AR_ReceiptTransactionDetail.InvoiceDetailID = SAL_InvoiceDetail.InvoiceDetailID
        and AR_ReceiptTransactionDetail.InvoiceHeaderID = SAL_InvoiceDetail.InvoiceHeaderID
        and AR_ReceiptTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID
        and AR_ReceiptTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID

        left join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
        and GL_TransactionHeader.DocumentNo = SAL_InvoiceHeader.InvoiceNumber
        and GL_TransactionHeader.CompanyID = SAL_InvoiceHeader.CompanyID
        and GL_TransactionHeader.PeriodID = SAL_InvoiceHeader.PeriodID


        left join inv_StockTransactionDetail on inv_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID
		and inv_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID
		and inv_StockTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID
		and inv_StockTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID
		
		left join inv_StockTransactionHeader on inv_StockTransactionHeader.StockTransactionID = inv_StockTransactionDetail.StockTransactionID 
		and inv_StockTransactionDetail.CompanyID = inv_StockTransactionHeader.CompanyID
		and inv_StockTransactionDetail.PeriodID = inv_StockTransactionHeader.PeriodID
		
		
		left join sal_DispatchHeader on sal_DispatchHeader.DocumentTypeID = inv_StockTransactionHeader.DocumentTypeID
		and sal_DispatchHeader.DispatchNo = inv_StockTransactionHeader.DocumentNo
		and sal_DispatchHeader.CompanyID = inv_StockTransactionHeader.CompanyID
		and sal_DispatchHeader.PeriodID = inv_StockTransactionHeader.PeriodID

WHERE SAL_InvoiceDetail.companyid = @companyid and SAL_InvoiceDetail.periodid = @periodid and SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
    AND SAL_InvoiceDetail.Status = 1

group by

SAL_InvoiceDetail.CompanyID, 
SAL_InvoiceDetail.PeriodID ,
--GL_TransactionHeader.ControlDebit,
SAL_InvoiceDetail.InvoiceHeaderID,
set_Documenttype.DocumentType,
SAL_InvoiceHeader.InvoiceNumber,GL_TransactionDetail.TransactionType ,
GL_TransactionHeader.ControlDebit
    

    
    
    ) as TAB
    where TAB.NetAmount > 0
";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDtail_Invoice_ByInvoiceHeaderKeyForAlhaj, sqlparams);

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

        public DataRow GetRefrenceNoByInvoiceNo(int companyID, int invoiceID, int periodID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InVoiceID", invoiceID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetRefrenceNoByKey = @" Select distinct SAL_InvoiceDetail.InvoiceHeaderID, SAL_OrderHeader.referenceno,SAL_OrderHeader.DTRENo from SAL_InvoiceDetail 
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
        AND SAL_InvoiceDetail.StockHeaderID = StockTransactionID AND SAL_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
    INNER JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID 
        AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID 
    left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = [SAL_DeliveryOrderDetail].SalesOrderHeaderID 
        AND SAL_OrderHeader.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND SAL_OrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND SAL_OrderHeader.[Status] = 1 
WHERE SAL_InvoiceDetail.InvoiceHeaderID = @InVoiceID AND SAL_InvoiceDetail.CompanyID = @CompanyID AND SAL_InvoiceDetail.PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNoByKey, sqlparams);

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

        public DataRow GetRefrenceNoByReceiptNo(int companyID, int receiptID, int periodID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InRecID", receiptID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetRefrenceNOByReceiptID = @" 
Select distinct SAL_InvoiceDetail.InvoiceHeaderID, AR_ReceiptTransactionDetail.ReceiptHeaderID, SAL_OrderHeader.referenceno, Sal_InvoiceHeader.InvoiceNumber 
from SAL_InvoiceDetail 
    INNER JOIN Sal_InvoiceHeader ON Sal_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID AND Sal_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID 
        AND Sal_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID AND Sal_InvoiceHeader.Status = 1 
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
        AND SAL_InvoiceDetail.StockHeaderID = StockTransactionID AND SAL_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
    INNER JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID 
        AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID 
    left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = [SAL_DeliveryOrderDetail].SalesOrderHeaderID 
        AND SAL_OrderHeader.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND SAL_OrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND SAL_OrderHeader.[Status] = 1 
    INNER JOIN AR_ReceiptTransactionDetail on AR_ReceiptTransactionDetail.InvoiceDetailID = SAL_InvoiceDetail.InvoiceDetailID 
        AND SAL_InvoiceDetail.InvoiceHeaderID = AR_ReceiptTransactionDetail.InvoiceHeaderID AND SAL_InvoiceDetail.CompanyID = AR_ReceiptTransactionDetail.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = AR_ReceiptTransactionDetail.PeriodID 
WHERE AR_ReceiptTransactionDetail.ReceiptHeaderID = @InRecID AND SAL_InvoiceDetail.CompanyID = @CompanyID AND SAL_InvoiceDetail.PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNOByReceiptID, sqlparams);

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

        public static DataTable GetMultiReceiptByInvoiceID(int CompanyID, int PeriodID, int InvoiceHeaderID)
        {
            DataTable dt = null;

            #region Query

            string Query_GetMultiReceiptByInvoiceID = @"

SELECT  AR_ReceiptInvoicesDetail.InvoiceHeaderID, SUM(AR_ReceiptInvoicesDetail.Amount)Amount
FROM    dbo.AR_ReceiptInvoicesDetail
WHERE   AR_ReceiptInvoicesDetail.CompanyID = @CompanyID
        AND AR_ReceiptInvoicesDetail.PeriodID = @PeriodID
        AND AR_ReceiptInvoicesDetail.InvoiceHeaderID = @InvoiceHeaderID and status = 1
        GROUP BY AR_ReceiptInvoicesDetail.InvoiceHeaderID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetMultiReceiptByInvoiceID, sqlparams);

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

        #endregion
    }
}
