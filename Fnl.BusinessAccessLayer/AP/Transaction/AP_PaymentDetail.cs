using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PaymentDetail : BaseClass
    {
        #region Fields

        private bool f_suppliesTax;
        private bool f_additionalTax;

        #endregion

        #region Properties

        public bool SuppliesTax
        {
            get { return f_suppliesTax; }
            set { f_suppliesTax = value; }
        }

        public bool AdditionalTax
        {
            get { return f_additionalTax; }
            set { f_additionalTax = value; }
        }

        #endregion

        #region Class Methods

        public DataTable GetInvoiceHeaderIDFromAP_PayamentDetail(int companyID, int periodID)
        {
            DataTable dt = null;

            string Query_GetInvoiceHeaderIDFromAP_PayamentDetail = @"Select InvoiceHeaderID,* from AP_PaymentDetail
                                                                        Where CompanyID = @CompanyID
                                                                        and PeriodID = @PeriodID
                                                                        and Status = 1";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceHeaderIDFromAP_PayamentDetail, sqlparams);

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

        public DataTable GetInvoiceHeaderIDFromAP_PayamentInvoicesDetail(int companyID, int periodID)
        {
            DataTable dt = null;
            string Query_GetInvoiceHeaderIDFromAP_PayamentInvoicesDetail = @" Select InvoiceHeaderID, * from AP_PaymentInvoicesDetail 
Where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceHeaderIDFromAP_PayamentInvoicesDetail, sqlparams);

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

        public DataTable GetDataByKey_FromHeader(int companyID, int periodID, int paymentHeaderID)
        {
            DataTable dt = null;

            #region Query

            string Query_GetData_byKey_FromHeader = @" 
SELECT AP_PaymentDetail.PaymentDetailID, AR_Set_Customer.SuppliesTaxExempted as SuppliesTax, isnull(INV_Item.AdditionalDeductionTaxFlag, 0) as AdditionalDeduction, 
    AP_PaymentDetail.PaymentHeaderID, AP_PaymentDetail.CompanyID, AP_PaymentDetail.PeriodID, AP_PaymentDetail.SerialNo, AP_PaymentDetail.ItemCategoryID, 
    INV_SET_Category.CategoryName AS ItemCategoryName, AP_PaymentDetail.ItemID, INV_Item.Description AS ItemDescription, SET_UnitofMeasure.UOMName, 
    AP_PaymentDetail.ItemUnitID, AP_PaymentDetail.ItemUnitQuantity, AP_PaymentDetail.ItemQuality, AP_PaymentDetail.BaseQuantity, AP_PaymentDetail.Quantity, 
    AP_PaymentDetail.BaseAmount, AP_PaymentDetail.Rate, AP_PaymentDetail.GrossAmount, AP_PaymentDetail.TaxAmount, AP_PaymentDetail.DiscountAmount, 
    AP_PaymentDetail.NetAmount, AP_PaymentDetail.Status, AP_PaymentDetail.InvoiceDetailID, AP_PaymentDetail.InvoiceHeaderID, AP_PaymentDetail.InvoiceDocumentNo, 
    AP_PaymentDetail.StockDetailID, AP_PaymentDetail.StockHeaderID, AP_PaymentDetail.PurchaseOrderDetailID, AP_PaymentDetail.PurchaseOrderHeaderID, 
    AP_PaymentDetail.CreatedBy, AP_PaymentDetail.CreateDate, AP_PaymentDetail.CreatedByValue, AP_PaymentDetail.UpdatedBy, AP_PaymentDetail.UpdateDate, 
    AP_PaymentDetail.UpdatedByValue, AR_InvoiceDetail.UnitPrice, AR_InvoiceHeader.InvoiceNumber, '' AS Action 
FROM AP_PaymentDetail 
    INNER JOIN INV_SET_Category ON 
--AP_PaymentDetail.CompanyID = INV_SET_Category.CompanyID AND 
AP_PaymentDetail.ItemCategoryID = INV_SET_Category.CategoryID 
        AND AP_PaymentDetail.Status = 1 
    INNER JOIN INV_Item ON AP_PaymentDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 --AP_PaymentDetail.CompanyID = INV_Item.CompanyID AND 
    INNER JOIN SET_UnitofMeasure ON AP_PaymentDetail.ItemUnitID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
    INNER JOIN AR_InvoiceDetail ON AR_InvoiceDetail.CompanyID = AP_PaymentDetail.CompanyID AND AR_InvoiceDetail.PeriodID = AP_PaymentDetail.PeriodID 
        AND AR_InvoiceDetail.InvoiceHeaderID = AP_PaymentDetail.InvoiceHeaderID AND AR_InvoiceDetail.InvoiceDetailID = AP_PaymentDetail.InvoiceDetailID 
        AND AR_InvoiceDetail.Status = 1 
    INNER JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
    Inner Join AR_Set_Customer ON AR_Set_Customer.CustomerID = AR_InvoiceHeader.VendorID --AND AR_Set_Customer.CompanyID = AR_InvoiceHeader.CompanyID 
        AND AR_Set_Customer.PeriodID = AR_InvoiceHeader.PeriodID AND AR_Set_Customer.Status = 1 
WHERE AP_PaymentDetail.CompanyID = @CompanyID AND AP_PaymentDetail.PeriodID = @PeriodID AND AP_PaymentDetail.PaymentHeaderID = @PaymenttHeaderID 
    AND AP_PaymentDetail.Status = 1 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymenttHeaderID", paymentHeaderID);

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

        public DataTable GetDataByKey_FromInvoice(int companyID, int periodID, int InvoiceHeaderID)
        {
            DataTable dt = null;
            string Query_GetDtail_Invoice_ByInvoiceHeaderKey = @" 
SELECT AR_InvoiceDetail.CompanyID, AR_InvoiceDetail.PeriodID, AR_InvoiceDetail.SerialNumber AS SerialNo, INV_Item.ItemTypeID AS ItemCategoryID, 
    INV_SET_Category.CategoryName AS ItemCategoryName, isnull(INV_Item.AdditionalDeductionTaxFlag, 0) as AdditionalDeduction, AR_InvoiceDetail.ItemID, 
    INV_Item.Description AS ItemDescription, AR_InvoiceDetail.ItemUnitOfMeasureID AS ItemUnitID, SET_UnitofMeasure.UOMName, 
    ISNULL(AR_InvoiceDetail.QuantityInvoiced, 0) AS ItemUnitQuantity, 'NA' AS ItemQuality, ISNULL(AR_InvoiceDetail.QuantityInvoiced, 0) AS BaseQuantity, 
    ISNULL(AR_InvoiceDetail.QuantityInvoiced, 0) AS Quantity, AR_InvoiceDetail.BaseAmount, ISNULL(AR_InvoiceDetail.ExchangeRate, 0) AS Rate, 
    AR_InvoiceDetail.GrossAmount, AR_InvoiceDetail.TaxAmount, AR_InvoiceDetail.DiscountAmountTaken AS DiscountAmount, AR_InvoiceDetail.NetAmount, 
    AR_InvoiceDetail.InvoiceHeaderID, AR_InvoiceDetail.InvoiceDetailID, AR_InvoiceDetail.UnitPrice, AR_InvoiceHeader.InvoiceNumber AS DocumentNo, 
    AR_InvoiceHeader.InvoiceNumber 
FROM AR_InvoiceDetail 
    INNER JOIN INV_Item ON --AR_InvoiceDetail.CompanyID = INV_Item.CompanyID AND 
AR_InvoiceDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1 
    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID --INV_Item.CompanyID = INV_SET_Category.CompanyID AND 
        AND INV_SET_Category.Status = 1 
    INNER JOIN SET_UnitofMeasure ON AR_InvoiceDetail.ItemUnitOfMeasureID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
    INNER JOIN AR_InvoiceHeader ON AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
        AND AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID AND AR_InvoiceHeader.Status = 1 
WHERE AR_InvoiceDetail.CompanyID = @CompanyID AND AR_InvoiceDetail.PeriodID = @PeriodID AND AR_InvoiceDetail.Status = 1 
    AND AR_InvoiceHeader.InvoiceID = @InvoiceHeaderID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);

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

        #endregion
    }
}
