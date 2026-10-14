using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_INVTransactionApplication
    {
        #region Fields
        #endregion

        #region Properties
        #endregion

        #region Constant String
        const string Query_GetInvoiceDetails = @" 
SELECT SAL_DeliveryOrderHeader.DeliveryOrderNo,
[SAL_InvoiceDetail].[InvoiceDetailID], [SAL_InvoiceDetail].[CompanyID], [SAL_InvoiceDetail].[PeriodID], [SAL_InvoiceDetail].[InvoiceHeaderID], 
    Case when INV_StockTransactionDetail.TransactionUOMID = INV_Item.PrimaryUOMCode then 1 
        when INV_StockTransactionDetail.TransactionUOMID = INV_Item.VolumeUOMCode then ISNULL(INV_Item.UnitVolume, 1) 
        when INV_StockTransactionDetail.TransactionUOMID = INV_Item.WeightUOMCode then ISNULL(INV_Item.UnitWeight, 1) end as [UnitQuantity], 
    [SAL_InvoiceDetail].[AccountingDate], [SAL_InvoiceDetail].[ActualPostedFlag], [SAL_InvoiceDetail].[Amount], [SAL_InvoiceDetail].[AmountIncludesTaxFlag], 
    [SAL_InvoiceDetail].[AssetsTrackingFlag], [SAL_InvoiceDetail].[AWHTaxFlag], [SAL_InvoiceDetail].[AWHTaxGrossAmount], [SAL_InvoiceDetail].[AWHTaxGroupID], 
    [SAL_InvoiceDetail].[AWHTaxInvoiceID], [SAL_InvoiceDetail].[AWHTaxInvoicePaymentID], [SAL_InvoiceDetail].[AWHTaxRateID], [SAL_InvoiceDetail].[BaseAmount], 
    [SAL_InvoiceDetail].[BaseInvoicePriceVariance], [SAL_InvoiceDetail].[BaseQuantityVariance], [SAL_InvoiceDetail].[BatchID], [SAL_InvoiceDetail].[CashPostedFlag], 
    [SAL_InvoiceDetail].[CountryOfSupply], [SAL_InvoiceDetail].[CreditCardTransactionID], [SAL_InvoiceDetail].[DiscountAmountTaken], 
    [SAL_InvoiceDetail].[DistributionCodeCombinationID], [SAL_InvoiceDetail].[DistributionLineNumber], [SAL_InvoiceDetail].[DistributionMatchType], 
    [SAL_InvoiceDetail].[EncumberedFlag], [SAL_InvoiceDetail].[ExchangeDate], [SAL_InvoiceDetail].[ExchangeRate], [SAL_InvoiceDetail].[ExchangeRateType], 
    [SAL_InvoiceDetail].[ExchangeRateVariance], [SAL_InvoiceDetail].[FinalMatchFlag], [SAL_InvoiceDetail].[GrossAmount], [SAL_InvoiceDetail].[InvoicePriceVariance], 
    [SAL_InvoiceDetail].[MatchStatusFlag], [SAL_InvoiceDetail].[MatchedUOMType], [SAL_InvoiceDetail].[NetAmount], [SAL_InvoiceDetail].[PurchaseOrderDistributionID], 
    [SAL_InvoiceDetail].[PostedFlag], [SAL_InvoiceDetail].[PrepayAmountRemaining], [SAL_InvoiceDetail].[PriceVarianceCodeCombinationID], 
    [SAL_InvoiceDetail].[QuantityInvoiced], [SAL_InvoiceDetail].[QuantityVariance], [SAL_InvoiceDetail].[RateVarianceCodeCombinationID], 
    [SAL_InvoiceDetail].[ReceiptTransactionID], [SAL_InvoiceDetail].[ReceiptMissingFlag], [SAL_InvoiceDetail].[ReceiptRequiredFlag], 
    [SAL_InvoiceDetail].[ReceiptVerifiedFlag], [SAL_InvoiceDetail].[ReversalFlag], [SAL_InvoiceDetail].[SerialNumber], [SAL_InvoiceDetail].[TaxAmount], 
    [SAL_InvoiceDetail].[IsExciseDutyExempted], [SAL_InvoiceDetail].[ExciseDutyAmount], [SAL_InvoiceDetail].[IsFEDExempted], [SAL_InvoiceDetail].[FEDAmount], 
    [SAL_InvoiceDetail].[TaxAutoCalculatedFlag], [SAL_InvoiceDetail].[UnitPrice], [SAL_InvoiceDetail].[Status], [SAL_InvoiceDetail].[StockDetailID], 
    [SAL_InvoiceDetail].[StockHeaderID], [SAL_InvoiceDetail].[CreatedBy], [SAL_InvoiceDetail].[CreateDate], [SAL_InvoiceDetail].[CreatedByValue], 
    [SAL_InvoiceDetail].[UpdatedBy], [SAL_InvoiceDetail].[UpdateDate], [SAL_InvoiceDetail].[UpdatedByValue], [SAL_InvoiceDetail].[ItemCategoryID], 
    [SAL_InvoiceDetail].[ItemID], (select Case when [SAL_InvoiceDetail].[Inclusive] is NULL then 0 else [SAL_InvoiceDetail].[Inclusive] end) as Inclusive, 
    [SAL_InvoiceDetail].[ItemUnitID] AS [ItemUnitOfMeasureID], [SAL_DispatchHeader].[DispatchNo], [INV_SET_Category].[CategoryName], 
    [INV_Item].[Description] AS [ItemDescription], [INV_Item].[ListPricePerUnit], [SET_UnitofMeasure].[UOMName], 
    Abs([INV_StockTransactionDetail].[TransactionQuantity]) AS [BaseQuantity], [SAL_InvoiceDetail].VehicleID AS DOVehicleInformationID, '' As [Chassis], '' [Action] 
FROM [SAL_InvoiceDetail] 
    INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[TransactionDetailID] = [SAL_InvoiceDetail].[StockDetailID] 
        AND [INV_StockTransactionDetail].[StockTransactionID] = [SAL_InvoiceDetail].[StockHeaderID] 
        AND [INV_StockTransactionDetail].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [SAL_InvoiceDetail].[PeriodID] 
        AND [INV_StockTransactionDetail].[Status] = 1 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = [INV_StockTransactionDetail].[StockTransactionID] 
        AND [INV_StockTransactionHeader].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
        AND [INV_StockTransactionHeader].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1 
    INNER JOIN [SAL_DispatchHeader] ON [SAL_DispatchHeader].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] 
        AND [SAL_DispatchHeader].[DispatchNo] = [INV_StockTransactionHeader].[DocumentNo] 
        AND [SAL_DispatchHeader].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [SAL_DispatchHeader].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] 
        AND [SAL_DispatchHeader].[Status] = 1 
    Left JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID] 
        AND [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID] 
        AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
        AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [SAL_DeliveryOrderDetail].[Status] = 1 
        
        -- new joins
        INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
		AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID     
        
    Left JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_InvoiceDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_InvoiceDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] 
        AND [INV_Item].[Status] = 1
  INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_InvoiceDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
 
WHERE [SAL_InvoiceDetail].[CompanyID] = @CompanyID  AND [SAL_InvoiceDetail].[PeriodID] = @PeriodID AND [SAL_InvoiceDetail].[Status] = 1
{0}  
ORDER BY [SAL_InvoiceDetail].[SerialNumber]
";

        const string Query_Get_details = @"SELECT  SAL_ITA.[CompanyID],SAL_ITA.[PeriodID],[TransactionApplicationID] ,SAL_ITA.[DiscountFrequencyID],SAL_ITA.[DiscountHeaderID],SAL_ITA.[DiscountDetailID]
,SAL_ITA.[SalesInvoiceID],SAL_InvoiceHEader.InvoiceNumber,SAL_ITA.[ItemCategoryID],INV_SET_Category.CategoryName,SAL_ITA.[ItemID],INV_Item.Description as ItemName,SAL_ITA.[Quantity],SAL_ITA.[Amount],SAL_ITA.[Status]
,SAL_ITA.[CreatedBy],SAL_ITA.[CreateDate],SAL_ITA.[CreatedByValue],SAL_ITA.[UpdatedBy],SAL_ITA.[UpdateDate],SAL_ITA.[UpdatedByValue]
  
  
  FROM [SAL_INVTransactionApplication] SAL_ITA
  
  inner join SAL_DiscountTransactionDetail SAL_DTD on SAL_DTD.DiscountFrequencyID = SAL_ITA.DiscountFrequencyID
  AND SAL_DTD.DiscountTransactionHeaderID = SAL_ITA.DiscountHeaderID
  --And SAL_DTD.DiscountTransactionDetailID = SAL_ITA.DiscountDetailID
AND SAL_DTD.SalesInvoiceID = SAL_ITA.SalesInvoiceID
  AND SAL_DTD.CompanyID =SAL_ITA.CompanyID
  AND SAL_DTD.PeriodID = SAL_ITA.PeriodID
  
  inner join INV_Item on INV_Item.ItemID = SAL_ITA.ItemID
  --And INV_Item.CompanyID = SAL_ITA.CompanyID

inner join SAL_INvoiceHeader on SAL_InvoiceHEader.InvoiceID = SAL_ITA.[SalesInvoiceID]
  And SAL_INvoiceHeader.companyID = SAL_ITA.companyID
  And SAL_INvoiceHeader.periodID = SAL_ITA.PeriodID
  
  inner join INV_SET_Category on INV_SET_Category.CategoryID = SAL_ITA.ItemCategoryID
  --And INV_SET_Category.CompanyID = SAL_ITA.CompanyID

WHERE SAL_ITA.CompanyID = @CompanyID And SAL_ITA.PeriodID = @PeriodID and SAL_ITA.DiscountFrequencyID = @DiscountFrequencyID and SAL_ITA.DiscountHeaderID =@DiscountTransactionHeaderID
--and SAL_ITA.DiscountDetailID =@DiscountTransactionDetailID 
and SAL_ITA.SalesInvoiceID = @SalesInvoiceID";



        #endregion

        #region Constructor
        #endregion

        #region Loading Methods
        #endregion

        #region Class Methods
        public static DataTable Get_Invoice_Details(int companyID, int periodID,string where)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                //sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetInvoiceDetails,where), sqlparams);

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


        public static DataTable GetAll(int InvoiceID,int DisFreqID,int DiscTransHdrID,int DisTransDetID,int companyID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DiscountFrequencyID", DisFreqID);
                sqlparams[3] = new SqlParameter("@DiscountTransactionHeaderID", DiscTransHdrID);
                sqlparams[4] = new SqlParameter("@DiscountTransactionDetailID", DisTransDetID);
                sqlparams[5] = new SqlParameter("@SalesInvoiceID", InvoiceID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Get_details, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Get_details, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_details, sqlparams);

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
