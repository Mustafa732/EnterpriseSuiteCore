using System;
using System.Data;
using Microsoft.Data.SqlClient;

using DAL.DataAccessLayer;
using System.Text;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_InvoiceDetail : BaseClass
    {
        #region Class Methods

        public bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int invoiceHeaderID, 
            int periodID)
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

                string Query_Delete_ByKeyInvoiceHeaderID = @" Update AR_InvoiceDetail 
Set [Status] = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
Where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceHeaderID = @InvoiceHeaderID ";

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

        public DataTable Get_StockHeaderForGateEntry(int companyID, int periodID, int StockTransactionID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_StockForGateEntry = @" select * from 
(
    SELECT INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionHeader.DocumentTypeID as SourceDocumentTypeID, 
        INV_StockTransactionHeader.DocumentNo as SourceDocumentNo, INV_StockTransactionDetail.TransactionDetailID as SourceLineID, 
        INV_StockTransactionHeader.StockTransactionID as SourceHeaderID, (ABS(INV_StockTransactionDetail.TransactionQuantity - 
        (
            select isnull(SUM(TransactionQuantity), 0) from Gate_OutwardDetail 
            where SourceHeaderID = @StockTransactionID and SourceLineID = INV_StockTransactionDetail.TransactionDetailID 
                and CompanyID = INV_StockTransactionHeader.CompanyID and PeriodID = INV_StockTransactionHeader.periodID
        )) * INV_StockTransactionDetail.TransactionCost) as TotalAmount, 
        0 as Gate_OutwardDetailID, 0 as Gate_OutwardHeaderID, INV_StockTransactionDetail.ItemID, 
        INV_Item.[Description] + '  (' + INV_Item.InventoryItemStatusCode + ')' as [Description], INV_Item.ListPricePerUnit, 
        INV_StockTransactionDetail.TransactionUOMID, SET_UnitofMeasure.UOMName, ABS(INV_StockTransactionDetail.TransactionQuantity - 
        (
            select isnull(SUM(TransactionQuantity), 0) from Gate_OutwardDetail 
            where SourceHeaderID = @StockTransactionID and SourceLineID = INV_StockTransactionDetail.TransactionDetailID 
                and CompanyID = INV_StockTransactionHeader.CompanyID and PeriodID = INV_StockTransactionHeader.periodID
        )) AS TransactionQuantity, 
        INV_StockTransactionDetail.TransactionCost as Rate, INV_StockTransactionDetail.RackID,' ' as ReferenceNo, '' AS [Action] 
    FROM INV_StockTransactionHeader 
        INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        INNER JOIN INV_Item ON INV_StockTransactionDetail.ItemID = INV_Item.ItemID 
        INNER JOIN SET_UnitofMeasure ON INV_StockTransactionDetail.TransactionUOMID = SET_UnitofMeasure.UOMID 
    WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID 
        and INV_StockTransactionDetail.StockTransactionID = @StockTransactionID
) as TAB 
where TAB.TotalAmount > 0 ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_StockForGateEntry, sqlParams);

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

        public DataTable Get_StockHeaderDetail_ByKey(int companyID, int periodID, int StockTransactionID, short transferType,bool GrnImport = false)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            if (GrnImport == false)
                sb.Append(" where OuterTab.QuantityInvoiced > 0 ");
            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);
                sqlParams[3] = new SqlParameter("@TransferType", transferType);
                
                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_StockHeaderDetail_ByKey,sb.ToString()), sqlParams);

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

        public static DataTable GetDataByKey(int companyID, int periodID, int InvoiceHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);
                sqlParams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);

                string Query_SelectAll_ByKey = @" 

SELECT AR_InvoiceDetail.CompanyID, AR_InvoiceDetail.PeriodID, AR_InvoiceDetail.InvoiceDetailID, AR_InvoiceDetail.InvoiceHeaderID, AR_InvoiceDetail.ItemID, 
    AR_InvoiceDetail.ItemDescription, AR_InvoiceDetail.ItemUnitOfMeasureID, SET_UnitofMeasure.UOMName, AR_InvoiceDetail.AssetsAdditionFlag, 
    AR_InvoiceDetail.BaseAmount, AR_InvoiceDetail.QuantityInvoiced, AR_InvoiceDetail.UnitPrice, AR_InvoiceDetail.GrossAmount, AR_InvoiceDetail.AmountIncludesTaxFlag, 
    AR_InvoiceDetail.DiscountAmountTaken, AR_InvoiceDetail.TaxAmount, AR_InvoiceDetail.NetAmount, AR_InvoiceDetail.SupplierDCQuantity, 
    AR_InvoiceDetail.SupplierGrossAmount, AR_InvoiceDetail.DCNetAmount, '' AS Action, AR_InvoiceDetail.StockHeaderID, AR_InvoiceDetail.StockDetailID, 
    AR_InvoiceDetail.PurchaseOrderHeaderID, AR_InvoiceDetail.PurchaseOrderDetailID, AR_InvoiceDetail.Source, SerialNumber ,AR_InvoiceDetail.TaxAmountOnNet as TaxAmountNet,


    
   AP_PURCHASEORDERHEADER.Rate ,
    
      (Case when AP_PurchaseOrderHeader.Rate is not null then 
     isnull(AP_PurchaseOrderDetail.ForexValue,0) 
     else
     0
     end) ForexValue
     
    ,(Case when AP_PurchaseOrderHeader.Rate is not null then
		 Round(isnull(AP_PurchaseOrderDetail.ForexValue,0)/isnull(AP_PurchaseOrderDetail.Quantity,1),2) 
		 else
		 0
		 end) ForexUnitPrice
		 ,Isnull(AR_InvoiceDetail.OtherTaxAmount,0) as OtherTaxAmount,
		 		 ISNULL(AR_InvoiceDetail.OtherAmountIncludesTaxFlag,0 ) as OtherAmountIncludesTaxFlag,
				 		 ISNULL(AR_InvoiceDetail.OtherTaxAmountOnNet,0)as OtherTaxAmountOnNet,
						 		 ISNULL(AR_InvoiceDetail.OtherTaxAutoCalculatedFlag,0) as OtherTaxAutoCalculatedFlag
	,ISNULL(AP_PurchaseOrderDetail.DiscountBeforeGST,0) as DiscountBeforeGST, convert(bit,'true') as IsChecked
,AP_PURCHASEORDERHEADER.DocumentNumber AS PONumber
FROM AR_InvoiceDetail 
    LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AR_InvoiceDetail.ItemUnitOfMeasureID 
LEFT JOIN AP_PURCHASEORDERHEADER ON AP_PURCHASEORDERHEADER.POHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID
And AP_PURCHASEORDERHEADER.CompanyID=AR_InvoiceDetail.CompanyID
And AP_PURCHASEORDERHEADER.PeriodID=AR_InvoiceDetail.PeriodID
And AP_PURCHASEORDERHEADER.Status=1
    LEFT JOIN AP_PurchaseOrderDetail ON AP_PurchaseOrderDetail.POHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID AND AP_PurchaseOrderDetail.PoDetailID =AR_InvoiceDetail.PurchaseOrderDetailID
	And AP_PurchaseOrderDetail.CompanyID=AR_InvoiceDetail.CompanyID
	And AP_PurchaseOrderDetail.PeriodID=AR_InvoiceDetail.PeriodID
	And AP_PurchaseOrderDetail.Status=1
WHERE AR_InvoiceDetail.CompanyID = @CompanyID AND AR_InvoiceDetail.PeriodID = @PeriodID 
AND AR_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID 
    AND AR_InvoiceDetail.Status = 1
  ";


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_ByKey, sqlParams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectAll_ByKey, sqlParams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ByKey, sqlParams);

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

        public bool UpdateDataByKeyInvoiceHeaderID(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[66];
                sqlparams[0] = new SqlParameter("@InvoiceDetailID", f_InvoiceDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[3] = new SqlParameter("@InvoiceHeaderID", f_InvoiceHeaderID);

                if (f_AccountingDate.HasValue)
                    sqlparams[4] = new SqlParameter("@AccountingDate", f_AccountingDate.Value);
                else
                    sqlparams[4] = new SqlParameter("@AccountingDate", DBNull.Value);

                sqlparams[5] = new SqlParameter("@ActualPostedFlag", f_ActualPostedFlag);
                sqlparams[6] = new SqlParameter("@Amount", f_Amount);
                sqlparams[7] = new SqlParameter("@AmountIncludesTaxFlag", f_AmountIncludesTaxFlag);
                sqlparams[8] = new SqlParameter("@AssetsAdditionFlag", f_AssetsAdditionFlag);
                sqlparams[9] = new SqlParameter("@AssetsTrackingFlag", f_AssetsTrackingFlag);
                sqlparams[10] = new SqlParameter("@AWHTaxFlag", f_AWHTaxFlag);

                if (f_AWHTaxGrossAmount.HasValue)
                    sqlparams[11] = new SqlParameter("@AWHTaxGrossAmount", f_AWHTaxGrossAmount.Value);
                else
                    sqlparams[11] = new SqlParameter("@AWHTaxGrossAmount", DBNull.Value);

                if (f_AWHTaxGroupID.HasValue)
                    sqlparams[12] = new SqlParameter("@AWHTaxGroupID", f_AWHTaxGroupID.Value);
                else
                    sqlparams[12] = new SqlParameter("@AWHTaxGroupID", DBNull.Value);

                if (f_AWHTaxInvoiceID.HasValue)
                    sqlparams[13] = new SqlParameter("@AWHTaxInvoiceID", f_AWHTaxInvoiceID.Value);
                else
                    sqlparams[13] = new SqlParameter("@AWHTaxInvoiceID", DBNull.Value);

                if (f_AWHTaxInvoicePaymentID.HasValue)
                    sqlparams[14] = new SqlParameter("@AWHTaxInvoicePaymentID", f_AWHTaxInvoicePaymentID.Value);
                else
                    sqlparams[14] = new SqlParameter("@AWHTaxInvoicePaymentID", DBNull.Value);

                if (f_AWHTaxRateID.HasValue)
                    sqlparams[15] = new SqlParameter("@AWHTaxRateID", f_AWHTaxRateID.Value);
                else
                    sqlparams[15] = new SqlParameter("@AWHTaxRateID", DBNull.Value);

                if (f_BaseAmount.HasValue)
                    sqlparams[16] = new SqlParameter("@BaseAmount", f_BaseAmount.Value);
                else
                    sqlparams[16] = new SqlParameter("@BaseAmount", DBNull.Value);

                if (f_BaseInvoicePriceVariance.HasValue)
                    sqlparams[17] = new SqlParameter("@BaseInvoicePriceVariance", f_BaseInvoicePriceVariance.Value);
                else
                    sqlparams[17] = new SqlParameter("@BaseInvoicePriceVariance", DBNull.Value);

                if (f_BaseQuantityVariance.HasValue)
                    sqlparams[18] = new SqlParameter("@BaseQuantityVariance", f_BaseQuantityVariance.Value);
                else
                    sqlparams[18] = new SqlParameter("@BaseQuantityVariance", DBNull.Value);

                if (f_BatchID.HasValue)
                    sqlparams[19] = new SqlParameter("@BatchID", f_BatchID.Value);
                else
                    sqlparams[19] = new SqlParameter("@BatchID", DBNull.Value);

                sqlparams[20] = new SqlParameter("@CashPostedFlag", f_CashPostedFlag);

                if (f_CountryOfSupply.HasValue)
                    sqlparams[21] = new SqlParameter("@CountryOfSupply", f_CountryOfSupply.Value);
                else
                    sqlparams[21] = new SqlParameter("@CountryOfSupply", DBNull.Value);

                if (f_CreditCardTransactionID.HasValue)
                    sqlparams[22] = new SqlParameter("@CreditCardTransactionID", f_CreditCardTransactionID.Value);
                else
                    sqlparams[22] = new SqlParameter("@CreditCardTransactionID", DBNull.Value);

                if (f_DiscountAmountTaken.HasValue)
                    sqlparams[23] = new SqlParameter("@DiscountAmountTaken", f_DiscountAmountTaken.Value);
                else
                    sqlparams[23] = new SqlParameter("@DiscountAmountTaken", DBNull.Value);

                if (f_DistributionCodeCombinationID.HasValue)
                    sqlparams[24] = new SqlParameter("@DistributionCodeCombinationID", f_DistributionCodeCombinationID.Value);
                else
                    sqlparams[24] = new SqlParameter("@DistributionCodeCombinationID", DBNull.Value);

                if (f_DistributionLineNumber.HasValue)
                    sqlparams[25] = new SqlParameter("@DistributionLineNumber", f_DistributionLineNumber.Value);
                else
                    sqlparams[25] = new SqlParameter("@DistributionLineNumber", DBNull.Value);

                if (f_DistributionMatchType.HasValue)
                    sqlparams[26] = new SqlParameter("@DistributionMatchType", f_DistributionMatchType.Value);
                else
                    sqlparams[26] = new SqlParameter("@DistributionMatchType", DBNull.Value);

                sqlparams[27] = new SqlParameter("@EncumberedFlag", f_EncumberedFlag);

                if (f_ExchangeDate.HasValue)
                    sqlparams[28] = new SqlParameter("@ExchangeDate", f_ExchangeDate.Value);
                else
                    sqlparams[28] = new SqlParameter("@ExchangeDate", DBNull.Value);

                if (f_ExchangeRate.HasValue)
                    sqlparams[29] = new SqlParameter("@ExchangeRate", f_ExchangeRate.Value);
                else
                    sqlparams[29] = new SqlParameter("@ExchangeRate", DBNull.Value);

                if (f_ExchangeRateType.HasValue)
                    sqlparams[30] = new SqlParameter("@ExchangeRateType", f_ExchangeRateType.Value);
                else
                    sqlparams[30] = new SqlParameter("@ExchangeRateType", DBNull.Value);

                if (f_ExchangeRateVariance.HasValue)
                    sqlparams[31] = new SqlParameter("@ExchangeRateVariance", f_ExchangeRateVariance.Value);
                else
                    sqlparams[31] = new SqlParameter("@ExchangeRateVariance", DBNull.Value);

                if (f_FinalMatchFlag.HasValue)
                    sqlparams[32] = new SqlParameter("@FinalMatchFlag", f_FinalMatchFlag.Value);
                else
                    sqlparams[32] = new SqlParameter("@FinalMatchFlag", DBNull.Value);

                sqlparams[33] = new SqlParameter("@GrossAmount", f_GrossAmount);

                if (f_InvoicePriceVariance.HasValue)
                    sqlparams[34] = new SqlParameter("@InvoicePriceVariance", f_InvoicePriceVariance.Value);
                else
                    sqlparams[34] = new SqlParameter("@InvoicePriceVariance", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ItemDescription))
                    sqlparams[35] = new SqlParameter("@ItemDescription", f_ItemDescription);
                else
                    sqlparams[35] = new SqlParameter("@ItemDescription", DBNull.Value);

                sqlparams[36] = new SqlParameter("@ItemID", f_ItemID);
                sqlparams[37] = new SqlParameter("@ItemUnitOfMeasureID", f_ItemUnitOfMeasureID);
                sqlparams[38] = new SqlParameter("@MatchStatusFlag", f_MatchStatusFlag);

                if (f_MatchedUOMType.HasValue)
                    sqlparams[39] = new SqlParameter("@MatchedUOMType", f_MatchedUOMType.Value);
                else
                    sqlparams[39] = new SqlParameter("@MatchedUOMType", DBNull.Value);

                sqlparams[40] = new SqlParameter("@NetAmount", f_NetAmount);

                if (f_PurchaseOrderDistributionID.HasValue)
                    sqlparams[41] = new SqlParameter("@PurchaseOrderDistributionID", f_PurchaseOrderDistributionID.Value);
                else
                    sqlparams[41] = new SqlParameter("@PurchaseOrderDistributionID", DBNull.Value);

                sqlparams[42] = new SqlParameter("@PostedFlag", f_PostedFlag);

                if (f_PrepayAmountRemaining.HasValue)
                    sqlparams[43] = new SqlParameter("@PrepayAmountRemaining", f_PrepayAmountRemaining.Value);
                else
                    sqlparams[43] = new SqlParameter("@PrepayAmountRemaining", DBNull.Value);

                if (f_PriceVarianceCodeCombinationID.HasValue)
                    sqlparams[44] = new SqlParameter("@PriceVarianceCodeCombinationID", f_PriceVarianceCodeCombinationID.Value);
                else
                    sqlparams[44] = new SqlParameter("@PriceVarianceCodeCombinationID", DBNull.Value);

                sqlparams[45] = new SqlParameter("@QuantityInvoiced", f_QuantityInvoiced);

                if (f_QuantityVariance.HasValue)
                    sqlparams[46] = new SqlParameter("@QuantityVariance", f_QuantityVariance.Value);
                else
                    sqlparams[46] = new SqlParameter("@QuantityVariance", DBNull.Value);

                if (f_RateVarianceCodeCombinationID.HasValue)
                    sqlparams[47] = new SqlParameter("@RateVarianceCodeCombinationID", f_RateVarianceCodeCombinationID.Value);
                else
                    sqlparams[47] = new SqlParameter("@RateVarianceCodeCombinationID", DBNull.Value);

                if (f_ReceiptTransactionID.HasValue)
                    sqlparams[48] = new SqlParameter("@ReceiptTransactionID", f_ReceiptTransactionID.Value);
                else
                    sqlparams[48] = new SqlParameter("@ReceiptTransactionID", DBNull.Value);

                sqlparams[49] = new SqlParameter("@ReceiptMissingFlag", f_ReceiptMissingFlag);
                sqlparams[50] = new SqlParameter("@ReceiptRequiredFlag", f_ReceiptRequiredFlag);
                sqlparams[51] = new SqlParameter("@ReceiptVerifiedFlag", f_ReceiptVerifiedFlag);
                sqlparams[52] = new SqlParameter("@ReversalFlag", f_ReversalFlag);
                sqlparams[53] = new SqlParameter("@SerialNumber", f_SerialNumber);

                if (f_TaxAmount.HasValue)
                    sqlparams[54] = new SqlParameter("@TaxAmount", f_TaxAmount.Value);
                else
                    sqlparams[54] = new SqlParameter("@TaxAmount", DBNull.Value);

                sqlparams[55] = new SqlParameter("@TaxAutoCalculatedFlag", f_TaxAutoCalculatedFlag);
                sqlparams[56] = new SqlParameter("@UnitPrice", f_UnitPrice);

                if (f_UpdatedBy.HasValue)
                    sqlparams[57] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[57] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[58] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[58] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[59] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[59] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                sqlparams[60] = new SqlParameter("@Status", f_Status);

                if (f_StockDetailID.HasValue)
                    sqlparams[61] = new SqlParameter("@StockDetailID", f_StockDetailID.Value);
                else
                    sqlparams[61] = new SqlParameter("@StockDetailID", DBNull.Value);

                if (f_StockHeaderID.HasValue)
                    sqlparams[62] = new SqlParameter("@StockHeaderID", f_StockHeaderID.Value);
                else
                    sqlparams[62] = new SqlParameter("@StockHeaderID", DBNull.Value);

                if (f_PurchaseOrderDetailID.HasValue)
                    sqlparams[63] = new SqlParameter("@PurchaseOrderDetailID", f_PurchaseOrderDetailID.Value);
                else
                    sqlparams[63] = new SqlParameter("@PurchaseOrderDetailID", DBNull.Value);

                if (f_PurchaseOrderHeaderID.HasValue)
                    sqlparams[64] = new SqlParameter("@PurchaseOrderHeaderID", f_PurchaseOrderHeaderID.Value);
                else
                    sqlparams[64] = new SqlParameter("@PurchaseOrderHeaderID", DBNull.Value);

                if (f_Source.HasValue)
                    sqlparams[65] = new SqlParameter("@Source", f_Source.Value);
                else
                    sqlparams[65] = new SqlParameter("@Source", DBNull.Value);

                #region Query

                string Query_Update_By_PrimaryKeyInvoiceHeaderID = @" UPDATE AR_InvoiceDetail 
SET AccountingDate = @AccountingDate, ActualPostedFlag = @ActualPostedFlag, Amount = @Amount, AmountIncludesTaxFlag = @AmountIncludesTaxFlag, 
    AssetsAdditionFlag = @AssetsAdditionFlag, AssetsTrackingFlag = @AssetsTrackingFlag, AWHTaxFlag = @AWHTaxFlag, AWHTaxGrossAmount = @AWHTaxGrossAmount, 
    AWHTaxGroupID = @AWHTaxGroupID, AWHTaxInvoiceID = @AWHTaxInvoiceID, AWHTaxInvoicePaymentID = @AWHTaxInvoicePaymentID, AWHTaxRateID = @AWHTaxRateID, 
    BaseAmount = @BaseAmount, BaseInvoicePriceVariance = @BaseInvoicePriceVariance, BaseQuantityVariance = @BaseQuantityVariance, BatchID = @BatchID, 
    CashPostedFlag = @CashPostedFlag, CountryOfSupply = @CountryOfSupply, CreditCardTransactionID = @CreditCardTransactionID, 
    DiscountAmountTaken = @DiscountAmountTaken, DistributionCodeCombinationID = @DistributionCodeCombinationID, DistributionLineNumber = @DistributionLineNumber, 
    DistributionMatchType = @DistributionMatchType, EncumberedFlag = @EncumberedFlag, ExchangeDate = @ExchangeDate, ExchangeRate = @ExchangeRate, 
    ExchangeRateType = @ExchangeRateType, ExchangeRateVariance = @ExchangeRateVariance, FinalMatchFlag = @FinalMatchFlag, GrossAmount = @GrossAmount, 
    InvoicePriceVariance = @InvoicePriceVariance, ItemDescription = @ItemDescription, ItemID = @ItemID, ItemUnitOfMeasureID = @ItemUnitOfMeasureID, 
    MatchStatusFlag = @MatchStatusFlag, MatchedUOMType = @MatchedUOMType, NetAmount = @NetAmount, PurchaseOrderDistributionID = @PurchaseOrderDistributionID, 
    PostedFlag = @PostedFlag, PrepayAmountRemaining = @PrepayAmountRemaining, PriceVarianceCodeCombinationID = @PriceVarianceCodeCombinationID, 
    QuantityInvoiced = @QuantityInvoiced, QuantityVariance = @QuantityVariance, RateVarianceCodeCombinationID = @RateVarianceCodeCombinationID, 
    ReceiptTransactionID = @ReceiptTransactionID, ReceiptMissingFlag = @ReceiptMissingFlag, ReceiptRequiredFlag = @ReceiptRequiredFlag, 
    ReceiptVerifiedFlag = @ReceiptVerifiedFlag, ReversalFlag = @ReversalFlag, SerialNumber = @SerialNumber, TaxAmount = @TaxAmount, 
    TaxAutoCalculatedFlag = @TaxAutoCalculatedFlag, UnitPrice = @UnitPrice, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue, 
    Status = @Status, StockDetailID = @StockDetailID, StockHeaderID = @StockHeaderID, PurchaseOrderDetailID = @PurchaseOrderDetailID, 
    PurchaseOrderHeaderID = @PurchaseOrderHeaderID, Source = @Source 
WHERE CompanyID = @CompanyID and InvoiceHeaderID = @InvoiceHeaderID and PeriodID = @PeriodID ";

                #endregion

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKeyInvoiceHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKeyInvoiceHeaderID, sqlparams) > 0)
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

        public DataTable GetTaxAmountByKey(int companyID, int periodID, int InvoiceID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);
                sqlParams[2] = new SqlParameter("@InvoiceID", InvoiceID);

                string Query_GetTaxAmountByKey = @" 
Select ISNULL(Sum(AR_InvoiceDetail.TaxAmount), 0) TaxAmount, ISNULL(Sum(AR_InvoiceHeader.GrossAmount), 0) GrossAmount, 
    ISNULL(Sum(AR_InvoiceDetail.DiscountAmountTaken), 0) DiscountAmount 
FROM AR_InvoiceDetail 
    INNER JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
Where AR_InvoiceDetail.Status = 1 AND AR_InvoiceDetail.InvoiceHeaderID = @InvoiceID AND AR_InvoiceDetail.CompanyID = @CompanyID 
    AND AR_InvoiceDetail.PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTaxAmountByKey, sqlParams);

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

        public static DataTable InvoiceLedger_ReportData(Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int companyID, int periodID, int CustomerID, int? TaxAmount = null, int ApprovingGroup = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyId", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                string Query_InvoiceLedgerReport = @" 
 SELECT DISTINCT  AR_InvoiceHeader.InvoiceID, AR_InvoiceHeader.InvoiceNumber,AR_InvoiceHeader.InvoiceDate,AR_InvoiceHeader.InvoiceType,
    SET_DocumentType.DocumentType,AR_InvoiceHeader.Description, AR_SET_Customer.CustomerName,AR_InvoiceHeader.GrossAmount 
        ,Max(AR_InvoiceDetail.TaxAmount) As TaxPercent
        ,AR_InvoiceHeader.GrossAmount * Max(AR_InvoiceDetail.TaxAmount) / 100 as TaxAmount
        ,AR_InvoiceHeader.OtherAmount
        
        FROM AR_InvoiceHeader 
        
        Inner JOIN AR_InvoiceDetail ON AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID 
        AND AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID 
        AND AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
        
        inner join inv_item on inv_item.ItemID = AR_InvoiceDetail.ItemID
        and inv_item.Status = 1
        
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID 
        AND SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType
             
        LEFT JOIN AR_SET_Customer  on AR_InvoiceHeader.VendorID = AR_SET_Customer.CustomerID 
        --and AR_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID 
        and AR_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID 
        
	    LEFT JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = AR_InvoiceHeader.CompanyID 
	    AND SET_DocumentApprove.PeriodID = AR_InvoiceHeader.PeriodID 
        AND SET_DocumentApprove.Status = 1 
        AND SET_DocumentApprove.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
        AND SET_DocumentApprove.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
           
WHERE AR_InvoiceHeader.CompanyID = @CompanyId 
AND AR_InvoiceHeader.PeriodID = @PeriodID 
AND AR_InvoiceHeader.Status = 1  
{0}

Group By AR_InvoiceHeader.InvoiceID, AR_InvoiceHeader.InvoiceNumber,AR_InvoiceHeader.InvoiceDate,AR_InvoiceHeader.InvoiceType,
    SET_DocumentType.DocumentType,AR_InvoiceHeader.Description, AR_SET_Customer.CustomerName,AR_InvoiceHeader.GrossAmount ,AR_InvoiceHeader.OtherAmount

 ";

                DataSet ds = null;


                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and convert(date,AR_InvoiceHeader.InvoiceDate) between @FromDate AND @ToDate ");
                }
                else if (dateFrom != null)
                {
                    sb.Append(" and convert(date,AR_InvoiceHeader.InvoiceDate) = @FromDate ");
                }
                else if (dateTo == null)
                {
                    sb.Append(" and convert(date,AR_InvoiceHeader.InvoiceDate) = @ToDate");
                }

                if (CustomerID != 0)
                    sb.Append(" and AR_InvoiceHeader.VendorID = " + CustomerID + "");

                if(TaxAmount != null)
                sb.Append(" and isnull(AR_InvoiceDetail.TaxAmount,0) = " + TaxAmount + "");


                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_InvoiceLedgerReport, sb), sqlparams);

                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_InvoiceLedgerReport, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_InvoiceLedgerReport, sb), sqlparams);

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


        public static DataTable GetDataVendorAgingSummary_Report_Report(int companyID, int periodID, Guid UserID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
         int CustomerID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            #region Query
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder Customer = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[5] = new SqlParameter("@UserID", UserID);

                DataSet ds = null;
                
                if (CustomerID > 0)
                {
                    Customer.Append(" AND AR_Set_Customer.CustomerID  = @customerid");
                    sb.Append(" AND TABInvoice.CustomerID  = @customerid");

                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAging, Customer.ToString(), sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAging, Customer.ToString(), sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAging, Customer.ToString(), sb.ToString()), sqlparams);

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

        public static DataTable GetDataVendorAgingSummaryBuckets_Report(int companyID, int periodID, Guid UserID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
         int CustomerID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            #region Query
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder Customer = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[5] = new SqlParameter("@UserID", UserID);

                DataSet ds = null;

                if (CustomerID > 0)
                {
                    Customer.Append(" AND AR_Set_Customer.CustomerID  = @customerid");
                    sb.Append(" AND TABInvoice.CustomerID  = @customerid");

                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAgingWithBucket, Customer.ToString(), sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAgingWithBucket, Customer.ToString(), sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VendorAgingWithBucket, Customer.ToString(), sb.ToString()), sqlparams);

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

        public DataTable Get_ImportDutyRates(int companyID, int periodID)
        {
            DataTable dtToReturn = null;
            const string Query_GetRates = @" Select RateID,RateType,[Rate%] as RatePercent,RateAmount,RateNarration,CodeCombinationID
,ROW_NUMBER() Over (Order by CompanyID, PeriodID, RateType) as 'SerialNo' ,'' as Action from 
[Fabric_ImportDutyRates]

where Status = 1
and CompanyID = @CompanyID
and PeriodID = @PeriodID 


order by RateType ";

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRates, sqlParams);

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

        public static DataTable GetDataVendorAgingSummary_Report_New(int companyID, int periodID, Guid UserID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
        int CustomerID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            #region Query
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder Customer = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[5] = new SqlParameter("@UserID", UserID);

                DataSet ds = null;

                if (CustomerID > 0)
                {
                    Customer.Append(" AND AR_Set_Customer.CustomerID  = @customerid");
                    sb.Append(" AND TABInvoice.CustomerID  = @customerid");

                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VenodrAgingWithADvance, Customer.ToString(), sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VenodrAgingWithADvance, Customer.ToString(), sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VenodrAgingWithADvance, Customer.ToString(), sb.ToString()), sqlparams);

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

        public bool DeleteDataByKeyInvoiceHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int invoiceHeaderID,
            int periodID)
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

                string Query_Delete_ByKeyInvoiceHeaderID = @" Update AR_InvoiceDetail 
Set [Status] = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
Where PeriodID = @PeriodID and CompanyID = @CompanyID and InvoiceHeaderID = @InvoiceHeaderID ";

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

        public static DataTable ItemWisePurchaseDetailReportData(string companyID, int PeriodID, DateTime FromDate, DateTime ToDate, string CatalogID, int? itemCategoryID, int? itemID, Nullable<int> locationID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            StringBuilder parameters = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                DataSet ds = null;

                if (itemID.HasValue)
                    parameters.Append(" AND INV_Item.ItemID =" + itemID);

                //if (itemCategoryID.HasValue)
                //    parameters.Append(" AND INV_Item.ItemTypeID =" + itemCategoryID);

                if (CatalogID != null)
                    parameters.Append(" AND INV_Item.ItemCatalogGroupID in (" + CatalogID + ")");

                if (locationID != null)
                    parameters.Append(" AND AR_InvoiceHeader.LocationID =" + locationID);

                companyID = "(" + companyID + ")";

                if (objConnection == null) //FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_VenodrAgingWithADvance
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetItemWisePurchaseDetailReportData.ToString().Replace("@CompanyID", companyID), parameters.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetItemWisePurchaseDetailReportData.ToString().Replace("@CompanyID", companyID), parameters.ToString()), sqlparams);

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


        public static DataTable GetItemWiseQuantityPurchase(string companyID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);



                const string Query_GetItemWiseQuantitySale = @" SELECT SET_ItemCatalog.Description ,SUM(QuantityInvoiced) AS value ,'' as ColorName
FROM AR_InvoiceDetail 
INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AR_InvoiceDetail.ItemUnitOfMeasureID

INNER JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail .InvoiceHeaderID
AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1

INNER JOIN dbo.INV_Item ON INV_Item.ItemID = AR_InvoiceDetail.ItemID 
INNER JOIN dbo.SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemTypeID 
WHERE AR_InvoiceHeader.CompanyID in @CompanyID
AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate and @ToDate
GROUP BY SET_ItemCatalog.Description ";

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemWiseQuantitySale.Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable GetItemWiseAmountPurchase(string companyID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);



                const string Query_GetItemWiseQuantitySale = @" SELECT SET_ItemCatalog.Description ,ROUND(SUM(NetAmount) / 1000000,2)   AS value ,'' as ColorName
FROM AR_InvoiceDetail 
INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AR_InvoiceDetail.ItemUnitOfMeasureID

INNER JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail .InvoiceHeaderID
AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
AND AR_InvoiceHeader.Status = 1 AND AR_InvoiceHeader.StatusCode = 1

INNER JOIN dbo.INV_Item ON INV_Item.ItemID = AR_InvoiceDetail.ItemID 
INNER JOIN dbo.SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemTypeID 
WHERE AR_InvoiceHeader.CompanyID in @CompanyID
AND AR_InvoiceHeader.InvoiceDate BETWEEN @FromDate and @ToDate
GROUP BY SET_ItemCatalog.Description ";

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemWiseQuantitySale.Replace("@CompanyID", companyID), sqlparams);

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

        #endregion
    }
}
