using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_InvoiceDetail
    {
        #region Constant Queries
        const string Query_Update_By_PrimaryKeyOnlyReversedQty = @" UPDATE [SAL_InvoiceDetail] SET [AccountingDate] = @AccountingDate, [ActualPostedFlag] = @ActualPostedFlag, [Amount] = @Amount, [AmountIncludesTaxFlag] = @AmountIncludesTaxFlag, [AssetsTrackingFlag] = @AssetsTrackingFlag, [AWHTaxFlag] = @AWHTaxFlag, [AWHTaxGrossAmount] = @AWHTaxGrossAmount, [AWHTaxGroupID] = @AWHTaxGroupID, [AWHTaxInvoiceID] = @AWHTaxInvoiceID, [AWHTaxInvoicePaymentID] = @AWHTaxInvoicePaymentID, [AWHTaxRateID] = @AWHTaxRateID, [BaseAmount] = @BaseAmount, [BaseInvoicePriceVariance] = @BaseInvoicePriceVariance, [BaseQuantityVariance] = @BaseQuantityVariance, [BatchID] = @BatchID, [CashPostedFlag] = @CashPostedFlag, [CountryOfSupply] = @CountryOfSupply, [CreditCardTransactionID] = @CreditCardTransactionID, [DiscountAmountTaken] = @DiscountAmountTaken, [DistributionCodeCombinationID] = @DistributionCodeCombinationID, [DistributionLineNumber] = @DistributionLineNumber, [DistributionMatchType] = @DistributionMatchType, [EncumberedFlag] = @EncumberedFlag, [ExchangeDate] = @ExchangeDate, [ExchangeRate] = @ExchangeRate, [ExchangeRateType] = @ExchangeRateType, [ExchangeRateVariance] = @ExchangeRateVariance, [FinalMatchFlag] = @FinalMatchFlag, [GrossAmount] = @GrossAmount, [InvoicePriceVariance] = @InvoicePriceVariance, [IsPaymentGenerated] = @IsPaymentGenerated, [MatchStatusFlag] = @MatchStatusFlag, [MatchedUOMType] = @MatchedUOMType, [NetAmount] = @NetAmount, [PurchaseOrderDistributionID] = @PurchaseOrderDistributionID, [PostedFlag] = @PostedFlag, [PrepayAmountRemaining] = @PrepayAmountRemaining, [PriceVarianceCodeCombinationID] = @PriceVarianceCodeCombinationID, [QuantityInvoiced] = @QuantityInvoiced, [QuantityVariance] = @QuantityVariance, [RateVarianceCodeCombinationID] = @RateVarianceCodeCombinationID, [ReceiptTransactionID] = @ReceiptTransactionID, [ReceiptMissingFlag] = @ReceiptMissingFlag, [ReceiptRequiredFlag] = @ReceiptRequiredFlag, [ReceiptVerifiedFlag] = @ReceiptVerifiedFlag, [ReversalFlag] = @ReversalFlag, [SerialNumber] = @SerialNumber, [TaxAmount] = @TaxAmount, [IsExciseDutyExempted] = @IsExciseDutyExempted, [ExciseDutyAmount] = @ExciseDutyAmount, [IsFEDExempted] = @IsFEDExempted, [FEDAmount] = @FEDAmount, [TaxAutoCalculatedFlag] = @TaxAutoCalculatedFlag, [ItemCategoryID] = @ItemCategoryID, [ItemID] = @ItemID, [ItemUnitID] = @ItemUnitID, [UnitPrice] = @UnitPrice, [Status] = @Status, [StockDetailID] = @StockDetailID, [StockHeaderID] = @StockHeaderID, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [VehicleID] = @VehicleID, [Inclusive] = @Inclusive, [ReversedQuantity] = @ReversedQuantity, [Description] = @Description, [FiscalID] = @FiscalID, [WithHolding] = @WithHolding, [TaxonGovtLevis] = @TaxonGovtLevis, [FurtherTaxonGovtLevis] = @FurtherTaxonGovtLevis, [FCRate] = @FCRate, [FCAmount] = @FCAmount, [GovtLevisAmount] = @GovtLevisAmount, [IsSalesTaxExempted] = @IsSalesTaxExempted, [SalesTaxAmount] = @SalesTaxAmount, [ExciseDutyActualAmount] = @ExciseDutyActualAmount, [IsWithHoldingExempted] = @IsWithHoldingExempted, [WithHoldingAmount] = @WithHoldingAmount, [LossInTransitQty] = @LossInTransitQty, [SalOrderDetailID] = @SalOrderDetailID, [OrderHeaderID] = @OrderHeaderID, [ContractNo] = @ContractNo, [ContractTypeID] = @ContractTypeID
		WHERE [CompanyID] = @CompanyID and [InvoiceDetailID] = @InvoiceDetailID and [InvoiceHeaderID] = @InvoiceHeaderID and [PeriodID] = @PeriodID ";

        const string Query_GetInvoiceDetailReturned = @" 
Select Sal_InvoiceDetail.* From Sal_InvoiceDetail 
	Inner Join Sal_InvoiceHeader On Sal_InvoiceHeader.CompanyID = Sal_InvoiceDetail.CompanyID 
		And Sal_InvoiceHeader.PeriodID = Sal_InvoiceDetail.PeriodID And Sal_InvoiceHeader.InvoiceID = Sal_InvoiceDetail.InvoiceHeaderID 
		And Sal_InvoiceHeader.SalesInvoiceHeaderID = @InvoiceHeaderID 
Where Sal_InvoiceDetail.CompanyID = @CompanyID And Sal_InvoiceDetail.PeriodID = @PeriodID  ";
  
          const string Query_NewSerialNo_ByCompanyPeriodSIDIDs = @" SELECT ISNULL(MAX(SerialNumber), 0) + 1 AS SerialNumber FROM [SAL_InvoiceDetail] 
            WHERE [InvoiceHeaderID] = @InvoiceHeaderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID; ";

        const string Query_Sales_TaxRegisterReport = @"
        Select 
        [SAL_InvoiceHeader].[InvoiceDate],
        [SAL_InvoiceHeader].[InvoiceNumber],
        [AR_SET_Customer].[CustomerName],
        [AR_SET_Customer].[STRNNumber],
        [SAL_InvoiceDetail].[QuantityInvoiced] As QuantityMTons,
        [SAL_InvoiceDetail].[UnitPrice] As PricePerTon,
        [SAL_InvoiceDetail].[BaseAmount] As ValueExclusiveOfTaxex,
        [SAL_InvoiceDetail].[FEDAmount] As SalesTaxFEDRs,
        [SAL_InvoiceDetail].[ExciseDutyAmount] As SED,
        [SAL_InvoiceDetail].[TaxAmount] As InclusiveOfTaxex,
        ([SAL_InvoiceDetail].[BaseAmount] + [SAL_InvoiceDetail].[FEDAmount] + [SAL_InvoiceDetail].[TaxAmount]) As ValueInclusiveOfTaxes
        From [SAL_InvoiceHeader]
        INNER JOIN [AR_SET_Customer] on [AR_SET_Customer].[CustomerID] = [SAL_InvoiceHeader].[CustomerID] 
        and [AR_SET_Customer].[PeriodID] = [SAL_InvoiceHeader].[PeriodID]
        INNER JOIN [SAL_InvoiceDetail] ON [SAL_InvoiceDetail].[InvoiceHeaderID] = [SAL_InvoiceHeader].[InvoiceID]
        AND [SAL_InvoiceHeader].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] 
        AND [SAL_InvoiceHeader].[PeriodID] = [SAL_InvoiceDetail].[PeriodID] AND [SAL_InvoiceDetail].[Status] = 1
        where SAL_InvoiceDetail.CompanyID = @CompanyID and SAL_InvoiceDetail.PeriodID = @PeriodID {0}";

        const string Query_GetDataForGateEntry = @"

Select

0 as Gate_OutwardDetailID,
0 as Gate_OutwardHeaderID,
[SAL_OrderDetail].[ItemID],
[INV_Item].[Description] ,
[INV_Item].[ListPricePerUnit]
,[SAL_OrderDetail].[ItemUnitID] as TransactionUOMID
,[SET_UnitofMeasure].[UOMName], 
INV_StockTransactionHeader.DocumentTypeID as SourceDocumentTypeID,
INV_StockTransactionHeader.DocumentNo as SourceDocumentNo,
INV_StockTransactionHeader.StockTransactionID as SourceHeaderID ,
INV_StockTransactionDetail.TransactionDetailID as SourceLineID,
Abs([INV_StockTransactionDetail].[TransactionQuantity]) AS ToTalQuantity
,  ISNULL((Abs([INV_StockTransactionDetail].[TransactionQuantity])),0) - ISNULL((SELECT SUM
(Gate_OutwardDetail.TransactionQuantity) FROM Gate_OutwardDetail 
     INNER JOIN Gate_OutwardHeader on Gate_OutwardHeader.Gate_OutwardHeaderID = Gate_OutwardDetail.Gate_OutwardHeaderID 
     AND Gate_OutwardHeader.CompanyID = Gate_OutwardDetail.CompanyID 
     AND Gate_OutwardHeader.PeriodID = Gate_OutwardDetail.PeriodID     
     WHERE Gate_OutwardDetail.SourceLineID = [INV_StockTransactionDetail].TransactionDetailID
     and Gate_OutwardDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
     and Gate_OutwardDetail.SourceDocumentNo = INV_StockTransactionHeader.DocumentNo
     and Gate_OutwardDetail.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
     ),0) AS TransactionQuantity,' ' as ReferenceNo
,'' [Action]
FROM [INV_StockTransactionDetail] 
inner join INV_StockTransactionHeader on [INV_StockTransactionDetail].StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	and [INV_StockTransactionDetail].CompanyID = INV_StockTransactionHeader.CompanyID
	and [INV_StockTransactionDetail].PeriodID = INV_StockTransactionHeader.PeriodID
INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID] 
	AND [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID] 
	AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
	AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] 
	AND [SAL_DeliveryOrderDetail].[Status] = 1
INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
	AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] 
	AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
	AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
	AND [SAL_OrderDetail].[Status] = 1
INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
	--AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
	AND [INV_SET_Category].[Status] = 1
INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
	--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
	AND [INV_Item].[Status] = 1
INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] 
	AND [SET_UnitofMeasure].[Status] = 1
WHERE [INV_StockTransactionDetail].[StockTransactionID] = @StockHeaderID AND [INV_StockTransactionDetail].[CompanyID] = @CompanyID  AND [INV_StockTransactionDetail].[PeriodID] = @PeriodID AND [INV_StockTransactionDetail].[Status] = 1
ORDER BY [INV_StockTransactionDetail].[TransactionDetailID]";

        const string Query_GetDataByKey_StockIDs = @"
            Select distinct 
            tab.DeliveryOrderID,
            tab.DeliveryOrderNo,
            tab.DODate,
            tab.DocumentType,
            tab.StockTransactionID,
            tab.DispatchNo
            From (
		            SELECT Distinct 
		            SAL_DeliveryOrderHeader.DeliveryOrderID,
		            SAL_DeliveryOrderHeader.DeliveryOrderNo,
		            SAL_DeliveryOrderHeader.DODate,
		            StockHeader.[StockTransactionID], 
		            Set_DocumentType.DocumentType,
		            [SAL_DispatchHeader].[DispatchNo], 
		            Sum([INV_StockTransactionDetail].[TransactionQuantity] * -1) as StockQuantity, 
		            SUM(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0)) as InvoicedQuantity
		            ,Cast(((select SUM(TransactionQuantity) from INV_stockTransactionDetail Where INV_stockTransactionDetail.CompanyID = StockHeader.CompanyID And INV_stockTransactionDetail.PeriodID = StockHeader.PeriodID 
		            And INV_stockTransactionDetail.StocktransactionID = StockHeader.StockTransactionID 
		            Group By INV_stockTransactionDetail.CompanyID, INV_stockTransactionDetail.PeriodID) * -1) AS Decimal(18,0))TransactionQty

		            FROM [SAL_DispatchHeader]

		            INNER JOIN [INV_StockTransactionHeader] StockHeader ON StockHeader.[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
			            AND StockHeader.[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
			            AND StockHeader.[CompanyID] = [SAL_DispatchHeader].[CompanyID] 
			            AND StockHeader.[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
			            AND StockHeader.[Status] = 1

		            INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = StockHeader.[StockTransactionID] 
			            AND [INV_StockTransactionDetail].[CompanyID] = StockHeader.[CompanyID] 
			            AND [INV_StockTransactionDetail].[PeriodID] = StockHeader.[PeriodID] 
			            AND [INV_StockTransactionDetail].[Status] = 1

		            LEFT JOIN [SAL_InvoiceDetail] ON [SAL_InvoiceDetail].[StockHeaderID] = [INV_StockTransactionDetail].[StockTransactionID] 
			            AND [SAL_InvoiceDetail].[StockDetailID] = [INV_StockTransactionDetail].[TransactionDetailID]
			            AND [SAL_InvoiceDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
			            AND [SAL_InvoiceDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] 
			            AND [SAL_InvoiceDetail].[Status] = 1

		            LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
			            And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
			            AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
			            AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID

		            LEFT JOIN SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID
			            AND SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderHeader.PeriodID
			            AND SAL_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID

		            INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
			            AND Set_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND Set_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID 

		            WHERE [SAL_DispatchHeader].[StatusCode] = 1
			            AND ([SAL_DeliveryOrderHeader].DeliveryOrderNo = @DeliveryOrderNo OR @DeliveryOrderNo IS NULL) 
			            AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
			            AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
			            AND [SAL_DispatchHeader].[Status] = 1
			            AND [SAL_InvoiceDetail].[StockHeaderID] IS NULL

		            Group By 
			            SAL_DeliveryOrderHeader.DeliveryOrderID,
			            SAL_DeliveryOrderHeader.DeliveryOrderNo,
			            SAL_DeliveryOrderHeader.DODate,
			            SAL_DispatchHeader.DispatchNo, 
			            StockHeader.[StockTransactionID],
			            Set_DocumentType.DocumentType, 
			            StockHeader.CompanyID, StockHeader.PeriodID
			            HAVING Sum([INV_StockTransactionDetail].[TransactionQuantity] * -1) > SUM(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0))
            ) tab
            Where tab.TransactionQty > InvoicedQuantity
            Order By tab.DeliveryOrderNo";
        
        const string Query_GetDataByKey_SaleInvoiceReturn = @" 
SELECT * FROM 
(
    SELECT SAL_ReverseDetail.SourceDetailID,
        0 as DeliveryOrderNo,
        --[INV_StockTransactionDetail].[TransactionDetailID] AS [StockDetailID], 
        SAL_ReverseDetail.SourceDetailID AS [StockDetailID],
        [INV_StockTransactionDetail].[StockTransactionID] AS [StockHeaderID], 
        [INV_StockTransactionDetail].[CompanyID], [INV_StockTransactionDetail].[PeriodID], [INV_StockTransactionHeader].[Comments], 
        StockItem.UnitWeight as  [UnitQuantity],
        [INV_StockTransactionDetail].[TransactionQuantity] AS [Quantity],
        [INV_StockTransactionDetail].[SerialNo], [INV_StockTransactionDetail].[SourceHeaderID], [INV_StockTransactionDetail].[SourceLineID], 
        [INV_StockTransactionDetail].[Status], [INV_StockTransactionDetail].[CreatedBy], [INV_StockTransactionDetail].[CreateDate], 
        [INV_StockTransactionDetail].[CreatedByValue], [INV_StockTransactionDetail].[UpdatedBy], [INV_StockTransactionDetail].[UpdateDate], 
        [INV_StockTransactionDetail].[UpdatedByValue],
        
        StockCat.CategoryID as ItemCategoryID, 
        StockItem.ItemID as ItemID, 
        StockItem.[Description] as [Description], 
        [INV_StockTransactionDetail].[TransactionCost] as [Rate], 
        
        [INV_StockTransactionDetail].[TransactionCost] as [RetailPrice], 
        [INV_StockTransactionDetail].[Amount] as [BaseAmount], 
       
        [INV_StockTransactionDetail].[Amount] as [GrossAmount],
        Abs([INV_StockTransactionDetail].[TransactionQuantity]) AS [BaseQuantity]
      
      , UOM.UOMID as [ItemUnitID]
     
      , StockCat.CategoryName as [CategoryName]
      
      ,StockItem.[ListPricePerUnit] as [ListPricePerUnit]
      
      , UOM.UOMName as [UOMName]
     
     ,[INV_StockTransactionDetail].[Amount] as [NetAmount]
       ,  INV_StockTransactionDetail.Amount as TotalAmount
      ,'False' as [IsSalesTaxExempted]
      , ISNULL(INV_StockTransactionDetail.SalesTax,0) As[SalesTaxAmount]
      ,ISNULL(INV_StockTransactionDetail.DiscountAmount,0) as DiscountAmountTaken
      ,'False' as [IsExciseDutyExempted]
      ,'False' as Inclusive
      ,0 as [ExciseDutyAmount]
      ,'False' as [IsFEDExempted]
      ,0 as [FEDAmount]
      ,'' [Action]
      ,0 as  OtherDiscount
      ,SET_DocumentType.DocumentType
 FROM [INV_StockTransactionDetail]
  
  

  INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = [INV_StockTransactionDetail].[StockTransactionID]
  AND [INV_StockTransactionDetail].[CompanyID] =   [INV_StockTransactionHeader].[CompanyID]
   AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID]
  
  INNER JOIN SAL_ReverseHeader ON 
	  [INV_StockTransactionHeader].[DocumentTypeID] = SAL_ReverseHeader.[DocumentTypeID] 
	  AND [INV_StockTransactionHeader].[DocumentNo] = SAL_ReverseHeader.DocumentNo 
	  AND [INV_StockTransactionHeader].[CompanyID] = SAL_ReverseHeader.[CompanyID] 
	  AND [INV_StockTransactionHeader].[PeriodID] = SAL_ReverseHeader.[PeriodID] 
	  AND [INV_StockTransactionHeader].[Status] = 1

    inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID = SAL_ReverseHeader.ReverseHeaderID
	  AND SAL_ReverseDetail.[CompanyID] = SAL_ReverseHeader.[CompanyID] 
	  AND SAL_ReverseDetail.[PeriodID] = SAL_ReverseHeader.[PeriodID] 
        AND SAL_ReverseDetail.ReverseDetailID = [INV_StockTransactionDetail].SourceLineID
	  
   LEft JOIN [INV_Item] as StockItem ON StockItem.[ItemID] = [INV_StockTransactionDetail].[ItemID] 
--AND StockItem.[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
AND StockItem.[Status] = 1
 
  LEft JOIN [INV_SET_Category] as StockCat ON [StockCat].[CategoryID] = [StockItem].[ItemTypeID] 
--AND [StockCat].[CompanyID] = [StockItem].[CompanyID] 
AND [StockCat].[Status] = 1
 
  LEft JOIN [SET_UnitofMeasure] as UOM ON UOM.[UOMID] = INV_StockTransactionDetail.TransactionUOMID AND UOM.[Status] = 1
  
  inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_ReverseHeader.DocumentTypeID
  and SET_DocumentType.CompanyID = SAL_ReverseHeader.CompanyID
  and SET_DocumentType.PeriodID= SAL_ReverseHeader.PeriodID
  
  WHERE [INV_StockTransactionDetail].[StockTransactionID] = @StockHeaderID AND 
  [INV_StockTransactionDetail].[CompanyID] = @CompanyID  AND [INV_StockTransactionDetail].[PeriodID] = @PeriodID AND [INV_StockTransactionDetail].[Status] = 1
  --ORDER BY [INV_StockTransactionDetail].[TransactionDetailID]
)Tab
  
  Where Tab.Quantity > 0
   ORDER BY Tab.[StockDetailID]";
        
        #endregion

        #region Class Methods

        public static DataTable Get_SalesTaxReport(int invoiceNumberFrom, int invoiceNumberTO, int DocumentTypeID, int companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@invoiceNumberFrom", invoiceNumberFrom);
                sqlparams[1] = new SqlParameter("@InvoiceNumberTO", invoiceNumberTO);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[6] = new SqlParameter("@ToDate", DateTo);
                if (DocumentTypeID > 0)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                if (DateFrom.HasValue && DateTo.HasValue)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                else if (DateFrom.HasValue)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceDate =@FromDate ");
                else if (DateTo.HasValue)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceDate =@ToDate ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_TaxReport, sb.ToString()), sqlparams);

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

        public static DataSet Get_SummarySalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    sb.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%Scrap%'");
                    //sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Scrap%' ");
                }
                 
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Summary_Sales_Scrap_n_Mud_Report, sb.ToString(), sb1.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet Get_MonthlySummarySalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Scrap%' ");
                    sb2.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%Scrap%' ");
                }
                //if (DocumentTypeID > 0)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Monthly_Summary_Sales_Scrap_n_Mud_Report, sb.ToString(), sb1.ToString(), sb2.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet Get_MonthlySummary_Mud_SalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Mud%' " );
                    sb2.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%Mud%' ");
                }
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Monthly_Summary_Sales__Mud_Report, sb.ToString(), sb1.ToString(), sb2.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }
        public static DataSet Get_MonthlySummary_Sugar_SalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    if (DocumentTypeID == 181)
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sales Order%sugar%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%Sugar%Register%'");
                        sb2.Append(" And SET_DocumentType.DocumentType Not like '%Sales Invoice%Sugar%Un-Register%'");
                    }
                    else
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sales Order%sugar%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%Sugar%Un-Register%'");
                    }
                }
                else
                {
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType like '%Sales Invoice%Sugar%Register%'");
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sales Order%sugar%' ");
                    sb2.Append(" And SET_DocumentType.DocumentType like '%Sales Invoice%sugar%' ");
                }
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Monthly_Summary_Sales__Sugar_Report, sb.ToString(), sb1.ToString(), sb2.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }
        public static DataSet Get_MonthlySummary_Molasses_SalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    if (DocumentTypeID == 190)
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Molasses%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Molasses%Register%'");
                        sb2.Append(" And SET_DocumentType.DocumentType Not like '%Sales invoice%Molasses%Un-Register%'");
                    }
                    else
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Molasses%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Molasses%Un-Register%'");
                    }
                }
                else
                {
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType like '%Sales invoice%Molasses%Register%'");
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Molasses%' ");
                    sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Molasses%' ");
                }
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Monthly_Summary_Sales__Molasses_Report, sb.ToString(), sb1.ToString(), sb2.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }
        public static DataSet Get_MonthlySummary_Bagasse_SalesReport(int DocumentTypeID, string companyID, int periodID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);

                if (DocumentTypeID > 0)
                {
                    if (DocumentTypeID == 190)
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Bagasse%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Bagasse%Register%'");
                        sb2.Append(" And SET_DocumentType.DocumentType Not like '%Sales invoice%Bagasse%Un-Register%'");
                    }
                    else
                    {
                        sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                        sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Bagasse%' ");
                        sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Bagasse%Un-Register%'");
                    }
                }
                else
                {
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType like '%Sales invoice%Bagasse%Register%'");
                    sb1.Append(" And SET_DocumentType.DocumentType like '%Sale Order%Bagasse%' ");
                    sb2.Append(" And SET_DocumentType.DocumentType like '%Sales invoice%Bagasse%' ");
                }
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");

                companyID = "(" + companyID + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Monthly_Summary_Sales__Bagasse_Report, sb.ToString(), sb1.ToString(), sb2.ToString()).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }
        
        public static DataSet Get_Summary_Sales_Mud_Report(int DocumentTypeID, int companyID, int periodID,int ItemCategory, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", DateTo);
                sqlparams[5] = new SqlParameter("@ItemCategory", ItemCategory);
                
                if (DocumentTypeID > 0)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceType = " + DocumentTypeID);
                //if (DateFrom.HasValue && DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate  between @FromDate AND @ToDate");
                //else if (DateFrom.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @FromDate ");
                //else if (DateTo.HasValue)
                //    sb.Append(" And SAL_InvoiceHeader.InvoiceDate = @ToDate ");


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Summary_Mud_Sales_Report, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    return ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataTable Get_SalesRegisterReport(int person, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, string sal_from, string sal_to,
            string del_from, string del_to, int companyID, int periodID, int DocumentTypeID, short? documentGroupID=null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (person > 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = "+person+"");
                else 
                    sb.Append("");
                
                if (datefrom != null && dateto != null)
                {
                    sb.Append(" and cast( [SAL_InvoiceHeader].[InvoiceDate] as date) between '" + datefrom.Value.ToString("yyyy/MM/dd") + "' and '" 
                        + dateto.Value.ToString("yyyy/MM/dd") + "'");
                }
                else 
                    sb.Append("");

                if (sal_from != "" && sal_to != "")
                    sb.Append(" and SAL_OrderHeader.ContractNo between "+ int.Parse(sal_from.ToString())+" and "+int.Parse(sal_to.ToString())+"");
                else 
                    sb.Append("");

                if (del_from != "" && del_to != "")
                    sb.Append(" and SAL_DeliveryOrderHeader.DeliveryOrderNo between " + int.Parse(del_from.ToString()) + " and " + int.Parse(del_to.ToString()) + "");
                else 
                    sb.Append("");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_OrderHeader.SaleContractTypeID = " + DocumentTypeID + "");


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesRegisterReport ,sb), sqlparams);

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

        //Query_SalesRegisterReport
        public static DataTable Get_SalesTaxRegisterReport(int companyID, int periodID, int? customerIDFrom = null, int? customerIDTo = null, string salesTaxNoFrom = null, string salesTaxNoTo = null, int? invoiceNoFrom = null, int? invoiceNoTo = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerIDFrom != null && customerIDTo != null)
                    sb.Append(" and [SAL_InvoiceHeader].[CustomerID] between " + customerIDFrom + " and " + customerIDTo);
                else if (customerIDFrom != null)
                    sb.Append(" and [SAL_InvoiceHeader].[CustomerID] >= " + customerIDFrom);
                else if (customerIDTo != null)
                    sb.Append(" and [SAL_InvoiceHeader].[CustomerID] <= " + customerIDTo);

                if (salesTaxNoFrom != null && salesTaxNoTo != null)
                    sb.Append(" and [AR_SET_Customer].[STRNNumber] like '%" + salesTaxNoFrom + "%' or  [AR_SET_Customer].[STRNNumber] like '%" + salesTaxNoTo + "%'");
                else if (salesTaxNoFrom != null)
                    sb.Append(" and [AR_SET_Customer].[STRNNumber] like '%" + salesTaxNoFrom + "%' ");
                else if (salesTaxNoTo != null)
                    sb.Append(" and [AR_SET_Customer].[STRNNumber] like '%" + salesTaxNoTo + "%'");

                if (invoiceNoFrom != null && invoiceNoTo != null)
                    sb.Append(" and [SAL_InvoiceHeader].[InvoiceNumber] between " + invoiceNoFrom + " and " + invoiceNoTo);
                else if (invoiceNoFrom != null)
                    sb.Append(" and [SAL_InvoiceHeader].[InvoiceNumber] >= " + invoiceNoFrom);
                else if (invoiceNoTo != null)
                    sb.Append(" and [SAL_InvoiceHeader].[InvoiceNumber] <= " + invoiceNoTo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Sales_TaxRegisterReport,sb.ToString()), sqlparams);

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

        public static DataTable GetDataForGateEntry_DispatchLookup(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForGateEntry, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_StockIDs(int DeliveryOrderNo, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderNo", DeliveryOrderNo);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey_StockIDs, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_DispatchLookup(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                string Query_ = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_GetDataByKey_DispatchLookup;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_DispatchLookupForEdit(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                string Query_ = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_GetDataByKey_DispatchLookupForEdit;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_ForDOUpdate(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                string Query_ = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetDataByKey_ForDOUpdate;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_SaleInvoiceReturn(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey_SaleInvoiceReturn, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetDataByKey, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID, int invoiceDetailID = 0)
         {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@invoiceDetailID", invoiceDetailID);
                if (invoiceDetailID > 0)
                    sb.Append(" and [SAL_InvoiceDetail].InvoiceDetailID =  @invoiceDetailID");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_GetDataByKeyForReturn, sb), sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTablesWithOutVehicle(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_GetVehical = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_GetDataByKeyWithOutVehicle;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehical, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTablesWithOutVehicleBySO(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_GetVehical = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_GetDataByKeyWithOutVehicleBySO;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehical, sqlparams);

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


        public static DataTable GetDataByKeyWithOtherTablesWithOutVehicleForDIInvoice(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                string Query_GetVehical = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetDataByKeyWithOutVehicleForDIInvoice;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetVehical, sqlparams);

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

        public int GetNewSerialNo(int invoiceHeaderID, int companyID, int periodID)
        {
            int maxSerialNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);

                maxSerialNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodSIDIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public static bool Update_Sal_Invoice_AccountsDetail(int invoiceDetailAccountID, int invoiceHeaderID, int companyID, int periodID)
        {
            try
            {
                
               string Query_Update_Status_0  = @"update Sal_Invoice_AccountsDetail set status = 0 

                                                        where CompanyID = " + companyID + " and PeriodID =  " + periodID + @"  
                                                    and InvoiceDetailAccountID =  " + invoiceDetailAccountID + @"  
                                                    and InvoiceHeaderID =  " + invoiceHeaderID + "";


                SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Update_Status_0),null);

                return true;
            }
            catch (Exception ex)
            {
                
                SqlHelper.LogException(ex);
                throw ex;
            }            
        }

        public static bool Update_Sal_InvoiceCustomerDetail(int invoiceCustomerID, int invoiceHeaderID, int companyID, int periodID)
        {
            try
            {

                string Query_Update_Status_0 = @"update Sal_InvoiceCustomerDetail set status = 0 

                                                        where CompanyID = " + companyID + " and PeriodID =  " + periodID + @"  
                                                    and InvoiceCustomerID =  " + invoiceCustomerID + @"  
                                                    and InvoiceHeaderID =  " + invoiceHeaderID + "";


                SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Update_Status_0), null);

                return true;
            }
            catch (Exception ex)
            {

                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataTable GetAccountDataByInvoiceHeader(int companyID, int invoiceHeaderID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetData_byInvoiceHeader = @" 
SELECT Distinct Sal_Invoice_AccountsDetail.CompanyID, Sal_Invoice_AccountsDetail.PeriodID, Sal_Invoice_AccountsDetail.InvoiceHeaderID, 
    Sal_Invoice_AccountsDetail.InvoiceDetailAccountID, Sal_Invoice_AccountsDetail.ChartOfAccountID, Sal_Invoice_AccountsDetail.CodeCombinationID, 
    Sal_Invoice_AccountsDetail.Amount, Sal_Invoice_AccountsDetail.Status, Sal_Invoice_AccountsDetail.CreatedBy, Sal_Invoice_AccountsDetail.CreateDate, 
    Sal_Invoice_AccountsDetail.CreatedByValue, Sal_Invoice_AccountsDetail.UpdatedBy, Sal_Invoice_AccountsDetail.UpdateDate, Sal_Invoice_AccountsDetail.UpdatedByValue, 
    Sal_Invoice_AccountsDetail.DocumentReference, Sal_Invoice_AccountsDetail.Narration, Sal_Invoice_AccountsDetail.DepartmentID, 
    Sal_Invoice_AccountsDetail.TransactionType, '' as IncurredDescription, -1 as IncurredOnID,
    GL_CodeCombinationDetail.Description, SET_Department.DepartmentName, SET_CostCenter.CostcenterID, SET_CostCenter.CostcenterName, '' as Action 
FROM Sal_Invoice_AccountsDetail 
    INNER JOIN GL_CodeCombinationDetail ON GL_CodeCombinationDetail.CodeCombinationID = Sal_Invoice_AccountsDetail.CodeCombinationID 
        --AND GL_CodeCombinationDetail.CompanyID = Sal_Invoice_AccountsDetail.CompanyID 
    left JOIN SET_Department ON SET_Department.DepartmentID = Sal_Invoice_AccountsDetail.DepartmentID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostcenterID = Sal_Invoice_AccountsDetail.CostCenterID 
WHERE Sal_Invoice_AccountsDetail.CompanyID = @CompanyID and Sal_Invoice_AccountsDetail.InvoiceHeaderID = @InvoiceHeaderID and PeriodID = @PeriodID 
    AND Sal_Invoice_AccountsDetail.Status = 1  ";

                if (objConnection != null)
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byInvoiceHeader, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCustomerDetailForInvoice(int companyID, int invoiceHeaderID, int periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_SelectCustomerDetailForInvoice = @"
select Sal_InvoiceCustomerDetail.CompanyID, Sal_InvoiceCustomerDetail.PeriodID, Sal_InvoiceCustomerDetail.InvoiceHeaderID, Sal_InvoiceCustomerDetail.InvoiceCustomerID, 
    Sal_InvoiceCustomerDetail.CustomerID, AR_SET_Customer.CustomerName, Sal_InvoiceCustomerDetail.CustomerAccountCode, Sal_InvoiceCustomerDetail.BusinessPartnerTypeID, 
    Sal_InvoiceCustomerDetail.Amount, Sal_InvoiceCustomerDetail.TransactionType, Sal_InvoiceCustomerDetail.Narration, Sal_InvoiceCustomerDetail.Status, '' as Action 
from Sal_InvoiceCustomerDetail 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = Sal_InvoiceCustomerDetail.CustomerID 
--and AR_SET_Customer.CompanyID = Sal_InvoiceCustomerDetail.CompanyID 
        and AR_SET_Customer.PeriodID = Sal_InvoiceCustomerDetail.PeriodID 
where Sal_InvoiceCustomerDetail.CompanyID = @CompanyID and Sal_InvoiceCustomerDetail.PeriodID = @PeriodID 
    and Sal_InvoiceCustomerDetail.InvoiceHeaderID = @InvoiceHeaderID and Sal_InvoiceCustomerDetail.Status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectCustomerDetailForInvoice, sqlparams);

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
        public static DataTable GetServiceRegisterReportData(int companyID, int periodID, int DocumentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? CustomerID, int? DocumentGroup, int CurrentFiscalID=0)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", dateFrom); 
                sqlparams[4] = new SqlParameter("@ToDate", dateTo);

                #region Query

                string Query_SelectServiceRegisterdata = @" select Row_Number() Over (Order by t.CustomerID,t.InvoiceID asc) as RowNo, t.* from (SELECT 
   [SAL_InvoiceHeader].CompanyID,[SAL_InvoiceHeader].CustomerID,Set_DocumentType.DocumentType As InvoiceType
   --,[SAL_InvoiceHeader].InvoiceID
   ,( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [SAL_InvoiceHeader].InvoiceID
   else [SAL_InvoiceHeader].ReversalID END ) as InvoiceID
   -- ,( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [SAL_InvoiceHeader].InvoiceDate
 --else (select Sal_Reverse.InvoiceDate from 
 --[SAL_InvoiceHeader] Sal_Reverse
 --where  Sal_Reverse.InvoiceID=[SAL_InvoiceHeader].ReversalID )
 -- END ) as InvoiceDate
 ,[SAL_InvoiceHeader].InvoiceDate
   ,[SAL_InvoiceHeader].Description ,[SAL_InvoiceHeader].InvoiceNumber,
 INV_Item.Description as Item,
   DocumentTypesGroup.Comments as SupplierAddress,SET_OperatingUnit.NTNNumber as SupplierNTN,SET_OperatingUnit.SalesTaxNumber as SupplierSTRN,SET_Business.PhoneNumber as SupplierPhoneNumber,
   [SAL_InvoiceHeader].billedtoid, BILLTo.CustomerName as BIllTo,
   AR_SET_Customer.CustomerName,AR_SET_Customer.AddressLine1 as CustomerAddress
   ,AR_SET_Customer.NTNNumber as CustomerNTN
   ,AR_SET_Customer.PhoneNumber as CustomerPhoneNumber
   ,AR_SET_Customer.STRNNumber as CustomeSTRN
   ,(CASE WHEN [SAL_InvoiceHeader].ReversalID is null then Sal_Invoice_AccountsDetail.DocumentReference
   else 'Reversal' END )  as ItemDescription
  -- ,Sal_Invoice_AccountsDetail.DocumentReference as ItemDescription
  -- ,[Sal_Invoice_AccountsDetail].Narration
   ,( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [Sal_Invoice_AccountsDetail].Narration
   else [SAL_InvoiceHeader].Description END ) as Narration
   --,Abs([INV_StockTransactionDetail].[TransactionQuantity]) AS [BaseQuantity]
 --,[SAL_InvoiceDetail].UnitPrice
   --,(CASE When [Sal_Invoice_AccountsDetail].DocumentReference like '%[%]%'
  --  then replace([Sal_Invoice_AccountsDetail].DocumentReference,'%','') else
   --  '' end ) as
 ,0 as [TaxAmount]
 , (CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [Sal_Invoice_AccountsDetail].Amount
   else [SAL_InvoiceHeader].AmountPaid END ) as Amount
-- ,isnull([Sal_Invoice_AccountsDetail].Amount,[SAL_InvoiceHeader].AmountPaid) as Amount
 ,(isnull([Sal_Invoice_AccountsDetail].Amount,0) * (CASE When [Sal_Invoice_AccountsDetail].DocumentReference like '%[%]%'
    then CAST(replace([Sal_Invoice_AccountsDetail].DocumentReference,'%','')as float ) else
     '' end ) / 100) as TaxPayable
  ,[Sal_Invoice_AccountsDetail].Amount + (isnull([Sal_Invoice_AccountsDetail].[Amount],0) * isnull((CASE When [Sal_Invoice_AccountsDetail].DocumentReference like '%[%]%'
    then CAST(replace([Sal_Invoice_AccountsDetail].DocumentReference,'%','')as float ) else
     '' end ),0)) / 100 as TotalAmount
,SET_OperatingUnit.Logoimage as LogoImage
,[SAL_InvoiceHeader].CustomerAddress as Shipto
,[SAL_InvoiceDetail].TaxAmount as Tax
  ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          when null
          then ' '+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          else  SUBSTRING(GroupName,1,1)+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          end  ) as ReversalNumber
          ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [SAL_InvoiceHeader].InvoiceNumber
   else (select Sal_Reverse.InvoiceNumber from 
 [SAL_InvoiceHeader] Sal_Reverse
 where  Sal_Reverse.InvoiceID=[SAL_InvoiceHeader].ReversalID )END ))
          when null
          then ' '+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [SAL_InvoiceHeader].InvoiceNumber
   else (select Sal_Reverse.InvoiceNumber from 
 [SAL_InvoiceHeader] Sal_Reverse
 where  Sal_Reverse.InvoiceID=[SAL_InvoiceHeader].ReversalID )END ))
          else  SUBSTRING(GroupName,1,1)+'SB' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),( CASE WHEN [SAL_InvoiceHeader].ReversalID is null then [SAL_InvoiceHeader].InvoiceNumber
   else (select Sal_Reverse.InvoiceNumber from 
 [SAL_InvoiceHeader] Sal_Reverse
 where  Sal_Reverse.InvoiceID=[SAL_InvoiceHeader].ReversalID )END ))
          end  ) as  SINumber

 FROM
 [SAL_InvoiceHeader]
 Left JOIN Sal_Invoice_AccountsDetail On Sal_Invoice_AccountsDetail.InvoiceHeaderID =[SAL_InvoiceHeader].InvoiceID
 AND Sal_Invoice_AccountsDetail.CompanyID=[SAL_InvoiceHeader].CompanyID
  AND Sal_Invoice_AccountsDetail.PeriodID=[SAL_InvoiceHeader].PeriodID
  AND Sal_Invoice_AccountsDetail.Status=1
  lefT JOIN Sal_InvoiceCustomerDetail On Sal_InvoiceCustomerDetail.InvoiceHeaderID =[SAL_InvoiceHeader].InvoiceID
   AND Sal_InvoiceCustomerDetail.CompanyID=[SAL_InvoiceHeader].CompanyID
  AND Sal_InvoiceCustomerDetail.PeriodID=[SAL_InvoiceHeader].PeriodID
   AND Sal_InvoiceCustomerDetail.Status=1
  
 left JOIN AR_SET_Customer  ON AR_SET_Customer.CustomerID = Sal_InvoiceCustomerDetail.CustomerID 
  --AND AR_SET_Customer.CompanyID = [SAL_InvoiceHeader].CompanyID
   AND AR_SET_Customer.PeriodID = [SAL_InvoiceHeader].PeriodID 
   left JOIN AR_SET_Customer BILLTo ON BILLTo.CustomerID = [SAL_InvoiceHeader].billedtoid 
  AND BILLTo.CompanyID = [SAL_InvoiceHeader].CompanyID
   AND BILLTo.PeriodID = [SAL_InvoiceHeader].PeriodID 
   
 INNER JOIN SET_Company ON SET_Company.CompanyId=[SAL_InvoiceHeader].CompanyID
 AND SET_Company.Status=1
 INNER JOIN SET_OperatingUnit On SET_OperatingUnit.OperatingUnitId=SET_Company.OperatingUnitId AND SET_OperatingUnit.Status=1
 INNER JOIN SET_Business On SET_Business.BusinessId=SET_Company.BusinessId and SET_Business.IsActive=1
 INNER Join Set_DocumentType On Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
        And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
       AND  Set_DocumentType.DocumentType like '%(CSP)%'
LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
-- Tax Amount 
left JOIN [SAL_InvoiceDetail] On [SAL_InvoiceDetail].InvoiceHeaderID=[SAL_InvoiceHeader].InvoiceID
AND [SAL_InvoiceDetail].Status=1
left JOIN INV_Item ON INV_Item.ItemID=SAL_InvoiceDetail.ItemID
 --AND INV_Item.CompanyID=SAL_InvoiceDetail.CompanyID
 --AND INV_Item.Per=SAL_InvoiceDetail.PeriodID

 INNER JOIN Set_PeriodCustom on [SAL_InvoiceHeader].InvoiceDate  between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
		and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID	
   
 WHERE [SAL_InvoiceHeader].[Status] = 1 AND [SAL_InvoiceHeader].[CompanyID] = @CompanyID
AND [SAL_InvoiceHeader].[PeriodID] = @PeriodID 

 AND SAL_InvoiceHeader.InvoiceDate  between @FromDate and @ToDate 
 {0}
 ) t
 
 group by t.CompanyID,t.InvoiceType
   --,[SAL_InvoiceHeader].InvoiceID
   ,t.InvoiceID
 ,t.InvoiceDate
   ,t.Description ,t.InvoiceNumber,
 t.Item,
   t.SupplierAddress,t.SupplierNTN,t.SupplierSTRN,t.SupplierPhoneNumber,
   t.billedtoid,t.BIllTo
   ,t.CustomerName,t.CustomerAddress
   ,t.CustomerNTN
   ,t.CustomerPhoneNumber
   ,t.CustomeSTRN
   ,t.ItemDescription
   ,t.Narration
   
 ,t.TaxAmount
 , t.Amount
 ,t.TaxPayable
  ,t.TotalAmount
,t.LogoImage
,t.Shipto
,t.Tax
  ,t.ReversalNumber
          ,t.SINumber,
 t.CustomerID
          
          order by t.invoiceid,t.invoicedate  ";
                #endregion
                if(DocumentTypeID > 0)
                    sb.Append(" AND SAL_InvoiceHeader.InvoiceType = "+ DocumentTypeID);
                if (CustomerID.HasValue)
                    sb.Append(" AND Sal_InvoiceCustomerDetail.CustomerID  = " + CustomerID);
                if(DocumentGroup.HasValue)
                    sb.Append(" AND  Set_DocumentType.GroupID = " + DocumentGroup);

                //sb.Append(" ORDER BY SAL_InvoiceHeader.InvoiceDate,SAL_InvoiceHeader.InvoiceID asc");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectServiceRegisterdata,sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {

                        if (dt.Rows[i]["Narration"].ToString().Contains("%") && dt.Rows[i]["InvoiceID"].ToString() == dt.Rows[i-1]["InvoiceID"].ToString())
                        {
                            try {
                                dt.Rows[i - 1]["TaxAmount"] = dt.Rows[i]["Amount"];
                               // dt.Rows[i].;
                            }
                            catch
                            {  }

                        }
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_InvoiceDetailReturned(int companyID, int periodID, int invoiceHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceDetailReturned, sqlparams);

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


        public static DataTable GetDataByDispatchNos_ForInvoice(string DispatchNo,  int DocumenttypeID, int companyID, int periodID, int CurrentFiscalId,bool IsSheikhoo = false,int COntractNo = 0)
        {
            DataTable dtToReturn = null;

            try
            {

                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DispatchNos", DispatchNo);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumenttypeID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalId", CurrentFiscalId);

                if (DispatchNo.ToString() != null || DispatchNo.ToString() != "")
                    sb.Append("AND  SAL_DispatchHeader.DispatchNo in (" + DispatchNo.ToString() + ") AND  SAL_DispatchHeader.CompanyID = " + companyID + " AND  SAL_DispatchHeader.PeriodID = " + periodID + "");
                sb.Append("AND  SAL_DispatchHeader.DocumentTypeID = " + DocumenttypeID + " AND  SAL_DispatchHeader.FiscalID  = " + CurrentFiscalId + "");
                string Query_GetDataByKey_DispatchNos = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetDataByKey_DispatchNosCustomerWise;

                if (IsSheikhoo == true)
                {
                    if (COntractNo > 0)
                        sb.Append(" and SAL_ContractHeader.ContractNo =  '" + COntractNo + "' ");

                    Query_GetDataByKey_DispatchNos = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_GetDispatchDataWithSaleContractDate;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByKey_DispatchNos, sb.ToString()), sqlparams);

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

        public static DataTable GetCalculatedTaxAmount(int invoiceHeaderID, int companyID, int periodID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_getCalculatedAmount, sqlparams);

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

        public bool UpdateDataByKeyOnlyReversedQty(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[88];
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
                sqlparams[8] = new SqlParameter("@AssetsTrackingFlag", f_AssetsTrackingFlag);
                sqlparams[9] = new SqlParameter("@AWHTaxFlag", f_AWHTaxFlag);

                if (f_AWHTaxGrossAmount.HasValue)
                    sqlparams[10] = new SqlParameter("@AWHTaxGrossAmount", f_AWHTaxGrossAmount.Value);
                else
                    sqlparams[10] = new SqlParameter("@AWHTaxGrossAmount", DBNull.Value);

                if (f_AWHTaxGroupID.HasValue)
                    sqlparams[11] = new SqlParameter("@AWHTaxGroupID", f_AWHTaxGroupID.Value);
                else
                    sqlparams[11] = new SqlParameter("@AWHTaxGroupID", DBNull.Value);

                if (f_AWHTaxInvoiceID.HasValue)
                    sqlparams[12] = new SqlParameter("@AWHTaxInvoiceID", f_AWHTaxInvoiceID.Value);
                else
                    sqlparams[12] = new SqlParameter("@AWHTaxInvoiceID", DBNull.Value);

                if (f_AWHTaxInvoicePaymentID.HasValue)
                    sqlparams[13] = new SqlParameter("@AWHTaxInvoicePaymentID", f_AWHTaxInvoicePaymentID.Value);
                else
                    sqlparams[13] = new SqlParameter("@AWHTaxInvoicePaymentID", DBNull.Value);

                if (f_AWHTaxRateID.HasValue)
                    sqlparams[14] = new SqlParameter("@AWHTaxRateID", f_AWHTaxRateID.Value);
                else
                    sqlparams[14] = new SqlParameter("@AWHTaxRateID", DBNull.Value);

                sqlparams[15] = new SqlParameter("@BaseAmount", f_BaseAmount);

                if (f_BaseInvoicePriceVariance.HasValue)
                    sqlparams[16] = new SqlParameter("@BaseInvoicePriceVariance", f_BaseInvoicePriceVariance.Value);
                else
                    sqlparams[16] = new SqlParameter("@BaseInvoicePriceVariance", DBNull.Value);

                if (f_BaseQuantityVariance.HasValue)
                    sqlparams[17] = new SqlParameter("@BaseQuantityVariance", f_BaseQuantityVariance.Value);
                else
                    sqlparams[17] = new SqlParameter("@BaseQuantityVariance", DBNull.Value);

                if (f_BatchID.HasValue)
                    sqlparams[18] = new SqlParameter("@BatchID", f_BatchID.Value);
                else
                    sqlparams[18] = new SqlParameter("@BatchID", DBNull.Value);

                sqlparams[19] = new SqlParameter("@CashPostedFlag", f_CashPostedFlag);

                if (f_CountryOfSupply.HasValue)
                    sqlparams[20] = new SqlParameter("@CountryOfSupply", f_CountryOfSupply.Value);
                else
                    sqlparams[20] = new SqlParameter("@CountryOfSupply", DBNull.Value);

                if (f_CreditCardTransactionID.HasValue)
                    sqlparams[21] = new SqlParameter("@CreditCardTransactionID", f_CreditCardTransactionID.Value);
                else
                    sqlparams[21] = new SqlParameter("@CreditCardTransactionID", DBNull.Value);

                if (f_DiscountAmountTaken.HasValue)
                    sqlparams[22] = new SqlParameter("@DiscountAmountTaken", f_DiscountAmountTaken.Value);
                else
                    sqlparams[22] = new SqlParameter("@DiscountAmountTaken", DBNull.Value);

                if (f_DistributionCodeCombinationID.HasValue)
                    sqlparams[23] = new SqlParameter("@DistributionCodeCombinationID", f_DistributionCodeCombinationID.Value);
                else
                    sqlparams[23] = new SqlParameter("@DistributionCodeCombinationID", DBNull.Value);

                if (f_DistributionLineNumber.HasValue)
                    sqlparams[24] = new SqlParameter("@DistributionLineNumber", f_DistributionLineNumber.Value);
                else
                    sqlparams[24] = new SqlParameter("@DistributionLineNumber", DBNull.Value);

                if (f_DistributionMatchType.HasValue)
                    sqlparams[25] = new SqlParameter("@DistributionMatchType", f_DistributionMatchType.Value);
                else
                    sqlparams[25] = new SqlParameter("@DistributionMatchType", DBNull.Value);

                sqlparams[26] = new SqlParameter("@EncumberedFlag", f_EncumberedFlag);

                if (f_ExchangeDate.HasValue)
                    sqlparams[27] = new SqlParameter("@ExchangeDate", f_ExchangeDate.Value);
                else
                    sqlparams[27] = new SqlParameter("@ExchangeDate", DBNull.Value);

                if (f_ExchangeRate.HasValue)
                    sqlparams[28] = new SqlParameter("@ExchangeRate", f_ExchangeRate.Value);
                else
                    sqlparams[28] = new SqlParameter("@ExchangeRate", DBNull.Value);

                if (f_ExchangeRateType.HasValue)
                    sqlparams[29] = new SqlParameter("@ExchangeRateType", f_ExchangeRateType.Value);
                else
                    sqlparams[29] = new SqlParameter("@ExchangeRateType", DBNull.Value);

                if (f_ExchangeRateVariance.HasValue)
                    sqlparams[30] = new SqlParameter("@ExchangeRateVariance", f_ExchangeRateVariance.Value);
                else
                    sqlparams[30] = new SqlParameter("@ExchangeRateVariance", DBNull.Value);

                if (f_FinalMatchFlag.HasValue)
                    sqlparams[31] = new SqlParameter("@FinalMatchFlag", f_FinalMatchFlag.Value);
                else
                    sqlparams[31] = new SqlParameter("@FinalMatchFlag", DBNull.Value);

                sqlparams[32] = new SqlParameter("@GrossAmount", f_GrossAmount);

                if (f_InvoicePriceVariance.HasValue)
                    sqlparams[33] = new SqlParameter("@InvoicePriceVariance", f_InvoicePriceVariance.Value);
                else
                    sqlparams[33] = new SqlParameter("@InvoicePriceVariance", DBNull.Value);

                sqlparams[34] = new SqlParameter("@IsPaymentGenerated", f_IsPaymentGenerated);
                sqlparams[35] = new SqlParameter("@MatchStatusFlag", f_MatchStatusFlag);

                if (f_MatchedUOMType.HasValue)
                    sqlparams[36] = new SqlParameter("@MatchedUOMType", f_MatchedUOMType.Value);
                else
                    sqlparams[36] = new SqlParameter("@MatchedUOMType", DBNull.Value);

                sqlparams[37] = new SqlParameter("@NetAmount", f_NetAmount);

                if (f_PurchaseOrderDistributionID.HasValue)
                    sqlparams[38] = new SqlParameter("@PurchaseOrderDistributionID", f_PurchaseOrderDistributionID.Value);
                else
                    sqlparams[38] = new SqlParameter("@PurchaseOrderDistributionID", DBNull.Value);

                sqlparams[39] = new SqlParameter("@PostedFlag", f_PostedFlag);

                if (f_PrepayAmountRemaining.HasValue)
                    sqlparams[40] = new SqlParameter("@PrepayAmountRemaining", f_PrepayAmountRemaining.Value);
                else
                    sqlparams[40] = new SqlParameter("@PrepayAmountRemaining", DBNull.Value);

                if (f_PriceVarianceCodeCombinationID.HasValue)
                    sqlparams[41] = new SqlParameter("@PriceVarianceCodeCombinationID", f_PriceVarianceCodeCombinationID.Value);
                else
                    sqlparams[41] = new SqlParameter("@PriceVarianceCodeCombinationID", DBNull.Value);

                sqlparams[42] = new SqlParameter("@QuantityInvoiced", f_QuantityInvoiced);

                if (f_QuantityVariance.HasValue)
                    sqlparams[43] = new SqlParameter("@QuantityVariance", f_QuantityVariance.Value);
                else
                    sqlparams[43] = new SqlParameter("@QuantityVariance", DBNull.Value);

                if (f_RateVarianceCodeCombinationID.HasValue)
                    sqlparams[44] = new SqlParameter("@RateVarianceCodeCombinationID", f_RateVarianceCodeCombinationID.Value);
                else
                    sqlparams[44] = new SqlParameter("@RateVarianceCodeCombinationID", DBNull.Value);

                if (f_ReceiptTransactionID.HasValue)
                    sqlparams[45] = new SqlParameter("@ReceiptTransactionID", f_ReceiptTransactionID.Value);
                else
                    sqlparams[45] = new SqlParameter("@ReceiptTransactionID", DBNull.Value);

                sqlparams[46] = new SqlParameter("@ReceiptMissingFlag", f_ReceiptMissingFlag);
                sqlparams[47] = new SqlParameter("@ReceiptRequiredFlag", f_ReceiptRequiredFlag);
                sqlparams[48] = new SqlParameter("@ReceiptVerifiedFlag", f_ReceiptVerifiedFlag);
                sqlparams[49] = new SqlParameter("@ReversalFlag", f_ReversalFlag);
                sqlparams[50] = new SqlParameter("@SerialNumber", f_SerialNumber);

                if (f_TaxAmount.HasValue)
                    sqlparams[51] = new SqlParameter("@TaxAmount", f_TaxAmount.Value);
                else
                    sqlparams[51] = new SqlParameter("@TaxAmount", DBNull.Value);

                sqlparams[52] = new SqlParameter("@IsExciseDutyExempted", f_IsExciseDutyExempted);

                if (f_ExciseDutyAmount.HasValue)
                    sqlparams[53] = new SqlParameter("@ExciseDutyAmount", f_ExciseDutyAmount.Value);
                else
                    sqlparams[53] = new SqlParameter("@ExciseDutyAmount", DBNull.Value);

                sqlparams[54] = new SqlParameter("@IsFEDExempted", f_IsFEDExempted);

                if (f_FEDAmount.HasValue)
                    sqlparams[55] = new SqlParameter("@FEDAmount", f_FEDAmount.Value);
                else
                    sqlparams[55] = new SqlParameter("@FEDAmount", DBNull.Value);

                sqlparams[56] = new SqlParameter("@TaxAutoCalculatedFlag", f_TaxAutoCalculatedFlag);
                sqlparams[57] = new SqlParameter("@ItemCategoryID", f_ItemCategoryID);
                sqlparams[58] = new SqlParameter("@ItemID", f_ItemID);
                sqlparams[59] = new SqlParameter("@ItemUnitID", f_ItemUnitID);
                sqlparams[60] = new SqlParameter("@UnitPrice", f_UnitPrice);
                sqlparams[61] = new SqlParameter("@Status", f_Status);
                sqlparams[62] = new SqlParameter("@StockDetailID", f_StockDetailID);
                sqlparams[63] = new SqlParameter("@StockHeaderID", f_StockHeaderID);

                if (f_UpdatedBy.HasValue)
                    sqlparams[64] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[64] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[65] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[65] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[66] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[66] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (f_VehicleID.HasValue)
                    sqlparams[67] = new SqlParameter("@VehicleID", f_VehicleID.Value);
                else
                    sqlparams[67] = new SqlParameter("@VehicleID", DBNull.Value);

                if (f_Inclusive.HasValue)
                    sqlparams[68] = new SqlParameter("@Inclusive", f_Inclusive.Value);
                else
                    sqlparams[68] = new SqlParameter("@Inclusive", DBNull.Value);

                if (f_ReversedQuantity.HasValue)
                    sqlparams[69] = new SqlParameter("@ReversedQuantity", f_ReversedQuantity.Value);
                else
                    sqlparams[69] = new SqlParameter("@ReversedQuantity", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Description))
                    sqlparams[70] = new SqlParameter("@Description", f_Description);
                else
                    sqlparams[70] = new SqlParameter("@Description", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[71] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[71] = new SqlParameter("@FiscalID", DBNull.Value);

                if (f_WithHolding.HasValue)
                    sqlparams[72] = new SqlParameter("@WithHolding", f_WithHolding.Value);
                else
                    sqlparams[72] = new SqlParameter("@WithHolding", DBNull.Value);

                if (f_TaxonGovtLevis.HasValue)
                    sqlparams[73] = new SqlParameter("@TaxonGovtLevis", f_TaxonGovtLevis.Value);
                else
                    sqlparams[73] = new SqlParameter("@TaxonGovtLevis", DBNull.Value);

                if (f_FurtherTaxonGovtLevis.HasValue)
                    sqlparams[74] = new SqlParameter("@FurtherTaxonGovtLevis", f_FurtherTaxonGovtLevis.Value);
                else
                    sqlparams[74] = new SqlParameter("@FurtherTaxonGovtLevis", DBNull.Value);

                if (f_FCRate.HasValue)
                    sqlparams[75] = new SqlParameter("@FCRate", f_FCRate.Value);
                else
                    sqlparams[75] = new SqlParameter("@FCRate", DBNull.Value);

                if (f_FCAmount.HasValue)
                    sqlparams[76] = new SqlParameter("@FCAmount", f_FCAmount.Value);
                else
                    sqlparams[76] = new SqlParameter("@FCAmount", DBNull.Value);

                if (f_GovtLevisAmount.HasValue)
                    sqlparams[77] = new SqlParameter("@GovtLevisAmount", f_GovtLevisAmount.Value);
                else
                    sqlparams[77] = new SqlParameter("@GovtLevisAmount", DBNull.Value);

                if (f_IsSalesTaxExempted.HasValue)
                    sqlparams[78] = new SqlParameter("@IsSalesTaxExempted", f_IsSalesTaxExempted.Value);
                else
                    sqlparams[78] = new SqlParameter("@IsSalesTaxExempted", DBNull.Value);

                if (f_SalesTaxAmount.HasValue)
                    sqlparams[79] = new SqlParameter("@SalesTaxAmount", f_SalesTaxAmount.Value);
                else
                    sqlparams[79] = new SqlParameter("@SalesTaxAmount", DBNull.Value);

                if (f_ExciseDutyActualAmount.HasValue)
                    sqlparams[80] = new SqlParameter("@ExciseDutyActualAmount", f_ExciseDutyActualAmount.Value);
                else
                    sqlparams[80] = new SqlParameter("@ExciseDutyActualAmount", DBNull.Value);

                if (f_IsWithHoldingExempted.HasValue)
                    sqlparams[81] = new SqlParameter("@IsWithHoldingExempted", f_IsWithHoldingExempted.Value);
                else
                    sqlparams[81] = new SqlParameter("@IsWithHoldingExempted", DBNull.Value);

                if (f_WithHoldingAmount.HasValue)
                    sqlparams[82] = new SqlParameter("@WithHoldingAmount", f_WithHoldingAmount.Value);
                else
                    sqlparams[82] = new SqlParameter("@WithHoldingAmount", DBNull.Value);

                if (f_LossInTransitQty.HasValue)
                    sqlparams[83] = new SqlParameter("@LossInTransitQty", f_LossInTransitQty.Value);
                else
                    sqlparams[83] = new SqlParameter("@LossInTransitQty", DBNull.Value);

                if (f_SalOrderDetailID.HasValue)
                    sqlparams[84] = new SqlParameter("@SalOrderDetailID", f_SalOrderDetailID.Value);
                else
                    sqlparams[84] = new SqlParameter("@SalOrderDetailID", DBNull.Value);

                if (f_OrderHeaderID.HasValue)
                    sqlparams[85] = new SqlParameter("@OrderHeaderID", f_OrderHeaderID.Value);
                else
                    sqlparams[85] = new SqlParameter("@OrderHeaderID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ContractNo))
                    sqlparams[86] = new SqlParameter("@ContractNo", f_ContractNo);
                else
                    sqlparams[86] = new SqlParameter("@ContractNo", DBNull.Value);

                if (f_ContractTypeID.HasValue)
                    sqlparams[87] = new SqlParameter("@ContractTypeID", f_ContractTypeID.Value);
                else
                    sqlparams[87] = new SqlParameter("@ContractTypeID", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKeyOnlyReversedQty, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKeyOnlyReversedQty, sqlparams) > 0)
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


        public static DataTable GetDataByDispatchNos_ForInvoice_Shiekoo(string DispatchNo, int DocumenttypeID, int companyID, int periodID, int CurrentFiscalId, bool IsSheikhoo = false, int COntractNo = 0)
        {
            DataTable dtToReturn = null;

            try
            {

                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DispatchNos", DispatchNo);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumenttypeID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalId", CurrentFiscalId);

                if (DispatchNo.ToString() != null || DispatchNo.ToString() != "")
                    sb.Append("AND  SAL_DispatchHeader.DispatchNo in (" + DispatchNo.ToString() + ") AND  SAL_DispatchHeader.CompanyID = " + companyID + " AND  SAL_DispatchHeader.PeriodID = " + periodID + "");
                sb.Append("AND  SAL_DispatchHeader.DocumentTypeID = " + DocumenttypeID + " AND  SAL_DispatchHeader.FiscalID  = " + CurrentFiscalId + "");
                string Query_GetDataByKey_DispatchNos = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetDataByKey_DispatchNosCustomerWise;

                if (IsSheikhoo == true)
                {
                    if (COntractNo > 0)
                        sb.Append(" and SAL_ContractHeader.ContractNo =  '" + COntractNo + "' ");

                    Query_GetDataByKey_DispatchNos = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_GetDispatchDataWithSaleContractDateShiekhoo;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDataByKey_DispatchNos, sb.ToString()), sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTables_DispatchLookup_ForFA(int stockHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                string Query_ = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_GetDataByKey_DispatchLookup_ForFA;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_, sqlparams);

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


        public static DataTable GetItemWiseQuantitySale(string companyID,  int periodID ,DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                


                const string Query_GetItemWiseQuantitySale = @" SELECT INV_Item.Description + ' (' +  SET_UnitofMeasure.UOMName + ')' AS Description  ,SUM(QuantityInvoiced) AS value
,'' as ColorName
FROM dbo.SAL_InvoiceDetail 
INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_InvoiceDetail.ItemUnitID
INNER JOIN dbo.SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID
AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID
AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1

INNER JOIN dbo.INV_Item ON INV_Item.ItemID = SAL_InvoiceDetail.ItemID 
WHERE SAL_InvoiceHeader.CompanyID in @CompanyID
AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate and @ToDate
GROUP BY INV_Item.Description,SET_UnitofMeasure.UOMName ";

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


        public static DataTable GetItemWiseAmountSale(string companyID, int periodID, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                const string Query_GetItemWiseQuantitySale = @" SELECT INV_Item.Description ,ROUND(SUM(NetAmount) / 1000000,2) AS value
,'' as ColorName
FROM dbo.SAL_InvoiceDetail 
INNER JOIN dbo.SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID
AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID
AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1

INNER JOIN dbo.INV_Item ON INV_Item.ItemID = SAL_InvoiceDetail.ItemID 
WHERE SAL_InvoiceHeader.CompanyID in @CompanyID
AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate and @ToDate
GROUP BY INV_Item.Description";

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


        public static DataTable GetInvoiceData(int invoiceHeaderID, int companyID, int periodID,bool IsInvoicePostWithBuyerName)
        {
            DataTable dtToReturn = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region MyRegion
                string Query_GetInvoiceData = @"
SELECT 
invoiceType,convert(VARCHAR(10),InvoiceDate,23) as InvoiceDate,sellerBusinessName	,sellerProvince	,sellerAddress	,buyerNTNCNIC	,buyerBusinessName	,buyerProvince	,buyerAddress	,buyerRegistrationType	,scenarioId	,hsCode	,productDescription	,rate	,uoM	,sum(quantity) as quantity	
,(sum(totalValues) - sum(WithHoldingAmount)) as totalValues
,(sum(totalValues)  - sum(WithHoldingAmount) - sum(SaleTaxAmount)) as valueSalesExcludingST,ROUND((sum(totalValues)  - sum(WithHoldingAmount) - sum(SaleTaxAmount)),0) / (SUM(quantity) / 1000) AS fixedNotifiedValueOrRetailPrice
,(sum(totalValues)  - sum(WithHoldingAmount) - sum(SaleTaxAmount)) * taxAmount /100 as salesTaxApplicable

,salesTaxWithheldAtSource,ROUND(SUM(WithHoldingAmount),0) AS extraTax ,sum(ExciseDutyActualAmount) as furtherTax ,sroScheduleNo
,sum(FixFEDAmount)as fedPayable ,discount ,saleType ,sroItemSerialNo ,FiscalID,sellerNTNCNIC
 
from (
SELECT Distinct 
Set_DocumentType.DocumentType as invoiceType ,SAL_invoiceHeader.InvoiceDate,set_business.BusinessName as sellerBusinessName,set_business.NTNNumber as sellerNTNCNIC
,isnull(set_business.Province,'')  as sellerProvince,set_business.AddressLine1 as sellerAddress,isnull(CustomerParty.CNICNumber,CustomerParty.NTNNumber) as buyerNTNCNIC,CustomerParty.CustomerName as  buyerBusinessName
,case CustomerParty.ProvinceID when 1 then 'Balochistan' when 2 then 'KPK'  when 3 then 'Punjab' when 4 then 'Sindh' else 'Other' end as buyerProvince,CustomerParty.AddressLine1 as  buyerAddress  ,case isnull(SAL_DispatchHeader.IsRegister,0) when 1 then 'Registered' else 'Unregistered' end buyerRegistrationType
,isnull(SAL_DispatchHeader.SROID,'') as scenarioId
--Detail Data
,INV_Item.HsCode as hsCode ,INV_Item.Description as  productDescription ,convert(varchar,SAL_InvoiceDetail.taxAmount) + '%' as rate ,'KG' as uoM 
,case 
	when SET_UnitofMeasure.UOMName like '%ton%' 
then
	SAL_InvoiceDetail.QuantityInvoiced * 1000
else
	SAL_InvoiceDetail.QuantityInvoiced 
end
	
	as quantity 
,SAL_InvoiceDetail.NetAmount as totalValues ,0 as fixedNotifiedValueOrRetailPrice 

,(SAL_InvoiceDetail.QuantityInvoiced  * SAL_InvoiceDetail.UnitPrice) * SAL_InvoiceDetail.TaxAmount / 100 as salesTaxApplicable 


,0 as salesTaxWithheldAtSource ,0 as extraTax ,'' as sroScheduleNo ,0 as discount
,isnull(SET_FBRScenario.ScenarioType,'') as saleType,'' as sroItemSerialNo
, ([SAL_InvoiceDetail].QuantityInvoiced * SAL_OrderDetail.T_Rate / 100) * SAL_InvoiceDetail.ExciseDutyAmount as ExciseDutyActualAmount
,SAL_InvoiceDetail.TaxAmount 
* (case when ( 
	SAL_ContractDetail.Rate <= 
	(case when 
(select top 1 isnull(Inclusive,0) as Inclusive
from SET_SalesTaxParameters
where Status = 1
and SET_SalesTaxParameters.PeriodID = SAL_ContractDetail.PeriodID
and SET_SalesTaxParameters.Item = SAL_ContractDetail.ItemID
and SAL_ContractHeader.ContractDate between FromDate and ToDate
AND ISNULL(SET_SalesTaxParameters.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)
order by SET_SalesTaxParameters.SalesTaxParameterID) = 1
	then
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc ) 
	else
		(        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1  AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc )  - (        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 order by CreateDate desc )  * [SAL_InvoiceDetail].TaxAmount / (100 + [SAL_InvoiceDetail].TaxAmount ))) 
	end ) 
	 )

   then 
   
   (case when SAL_ContractDetail.Inclusive = 1 then
		--!st case Sugar 36 No
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1  AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc )  * [SAL_InvoiceDetail].TaxAmount / (100 + [SAL_InvoiceDetail].TaxAmount) * [SAL_InvoiceDetail].QuantityInvoiced
   else 

	   (case when SET_DocumentType.DocumentType like '%billet%' 
	   then 
			(        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1  AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc )  * [SAL_InvoiceDetail].TaxAmount/ 100 ) * [SAL_InvoiceDetail].QuantityInvoiced
		else
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_InvoiceDetail.itemid and SAL_InvoiceHeader.invoicedate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc )  * [SAL_InvoiceDetail].TaxAmount / (100 + [SAL_InvoiceDetail].TaxAmount) 
		end)

   end)
   else
		--(([SAL_InvoiceDetail].[NetAmount] *  [SAL_ContractDetail].[SalesTaxAmount]) / (100+  [SAL_ContractDetail].[SalesTaxAmount] + isnull([SAL_ContractDetail].[ExciseDutyAmount],0) ) )
	--	(([SAL_InvoiceDetail].[NetAmount] *  [SAL_InvoiceDetail].TaxAmount) / (100+  [SAL_InvoiceDetail].TaxAmount + isnull([SAL_ContractDetail].[ExciseDutyAmount],0) ) )
    
	( [SAL_InvoiceDetail].[NetAmount] -
	  (([SAL_InvoiceDetail].QuantityInvoiced *  ( INV_Item.UnitWeight * (SAL_OrderDetail.PricePerKG + 
	(Case When ([SAL_OrderHeader].ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))))
	 * ( [SAL_InvoiceDetail].WithHolding )) / (100 + [SAL_InvoiceDetail].WithHolding) 
	 )
	 
	-- / (100+  [SAL_InvoiceDetail].TaxAmount + isnull([SAL_ContractDetail].[ExciseDutyAmount],0) ) 

 
   end )
   
   / (100 + SAL_InvoiceDetail.TaxAmount + isnull(Set_CustomerCategoryWiseTax.FurtherTax,0))


	  as SaleTaxAmount


, ([SAL_InvoiceDetail].QuantityInvoiced * [SAL_OrderDetail].T_Rate / 100) * SAL_InvoiceDetail.FEDAmount as FEDTaxAmount

, (([SAL_InvoiceDetail].QuantityInvoiced *  ( INV_Item.UnitWeight * (SAL_OrderDetail.PricePerKG + 
	(Case When ([SAL_OrderHeader].ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))))
	 * ( [SAL_InvoiceDetail].WithHolding )) / (100 + [SAL_InvoiceDetail].WithHolding) as WithHoldingAmount
	,
	case when SAL_InvoiceDetail.Inclusive = 1 then
	(([SAL_InvoiceDetail].QuantityInvoiced * (
	INV_Item.UnitWeight *
	 case when COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) > 0  
	 then COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) 
	 else 
	 ( (case when SAL_ContractHeader.ContractNo is null then SAL_OrderDetail.PricePerKG else 
	 
	 (sal_invoicedetail.unitprice / INV_Item.UnitWeight )
	 end )
	  + 
	(Case When ([SAL_OrderHeader].ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))
				end
				)))
				
				else
				
				 (([SAL_InvoiceDetail].QuantityInvoiced * (
	INV_Item.UnitWeight *
	 case when COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) > 0  
	 then COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) 
	 else 
	 ( (case when SAL_ContractHeader.ContractNo is null then SAL_OrderDetail.PricePerKG else 
	 
	 (sal_invoicedetail.unitprice / INV_Item.UnitWeight )
	 end )
	  + 
	(Case When ([SAL_OrderHeader].ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))
				end
				)))
				+ (
				
				(([SAL_InvoiceDetail].QuantityInvoiced * (
	INV_Item.UnitWeight *
	 case when COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) > 0  
	 then COnvert(decimal(18,6),ISNULL(SAL_COntractHeader.FreightDetail,0)) 
	 else 
	 ( (case when SAL_ContractHeader.ContractNo is null then SAL_OrderDetail.PricePerKG else 
	 
	 (sal_invoicedetail.unitprice / INV_Item.UnitWeight )
	 end )
	  + 
	(Case When ([SAL_OrderHeader].ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))
				end
				))) * (isnull([SAL_InvoiceDetail].TaxAmount,0) + isnull([SAL_InvoiceDetail].ExciseDutyAmount,0)) / 100
				)
				 
				  end 
				 As AmountReceivable

,isnull([SAL_InvoiceDetail].FEDFixRate,0) * [SAL_InvoiceDetail].QuantityInvoiced as FixFEDAmount
,SAL_InvoiceHeader.FiscalID
,isnull(SAL_InvoiceDetail.TaxAmount,0) as TaxAmount
 FROM [SAL_InvoiceHeader]
  INNER JOIN [SAL_InvoiceDetail] On [SAL_InvoiceDetail].InvoiceHeaderID =[SAL_InvoiceHeader].InvoiceID
 AND [SAL_InvoiceDetail].CompanyID=[SAL_InvoiceHeader].CompanyID  AND [SAL_InvoiceDetail].PeriodID=[SAL_InvoiceHeader].PeriodID
 and [SAL_InvoiceDetail].status = 1

 INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[TransactionDetailID] = [SAL_InvoiceDetail].[StockDetailID] 
    AND [INV_StockTransactionDetail].[StockTransactionID] = [SAL_InvoiceDetail].[StockHeaderID] 
    AND [INV_StockTransactionDetail].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [SAL_InvoiceDetail].[PeriodID] 
    AND [INV_StockTransactionDetail].[Status] = 1 INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = [INV_StockTransactionDetail].[StockTransactionID] 
        AND [INV_StockTransactionHeader].[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
        AND [INV_StockTransactionHeader].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1 
    INNER JOIN [SAL_DispatchHeader] ON [SAL_DispatchHeader].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] 
        AND [SAL_DispatchHeader].[DispatchNo] = [INV_StockTransactionHeader].[DocumentNo] 
        AND [SAL_DispatchHeader].FiscalID = [INV_StockTransactionHeader].FiscalID
        AND [SAL_DispatchHeader].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [SAL_DispatchHeader].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] 
        AND [SAL_DispatchHeader].[Status] = 1 LEFT JOIN INV_Item ON INV_Item.ItemID=[INV_StockTransactionDetail].ItemID 
 Left JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID 
AND Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID 
        AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID 
		left join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID
and SAL_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID
and SAL_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID
    left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = [SAL_DeliveryOrderDetail].SalesOrderHeaderID 
        AND SAL_OrderHeader.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND SAL_OrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND SAL_OrderHeader.[Status] = 1 
				left join [SAL_OrderDetail] on [SAL_OrderDetail].OrderID =SAL_OrderHeader.OrderID 
		        AND SAL_OrderHeader.[CompanyID] = [SAL_OrderDetail].[CompanyID]
				 AND SAL_OrderHeader.[PeriodID] = [SAL_OrderDetail].[PeriodID] 
                AND [SAL_OrderDetail].[Status] = 1 And SAL_OrderDetail.OrderDetailID= SAL_DeliveryOrderDetail.SalesOrderDetailID


				left join SAL_ContractDetail on SAL_ContractDetail.ContractDetailID = SAL_OrderDetail.ContractDetailID
		and  SAL_ContractDetail.ContractID = SAL_OrderDetail.ContractHeaderID
		and SAL_ContractDetail.CompanyID =SAL_OrderDetail.CompanyID
		and SAL_ContractDetail.PeriodID =SAL_OrderDetail.PeriodID
		and SAL_ContractDetail.ItemID =SAL_OrderDetail.ItemID
		and SAL_ContractDetail.Status = 1

		left join SAL_ContractHeader on SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID
		and SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID
		and SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID

    left JOIN INV_SET_Category On INV_SET_Category.CategoryID =INV_Item.ItemTypeID
    
 
 left JOIN AR_SET_Customer  ON AR_SET_Customer.CustomerID = [SAL_InvoiceHeader].CustomerID 
   AND AR_SET_Customer.PeriodID = [SAL_InvoiceHeader].PeriodID 

   left join ar_set_Customer CustomerParty on CustomerParty.CustomerID = SAL_DispatchHeader.CustomerPartyID 
        and CustomerParty.PeriodID = SAL_DispatchHeader.PeriodID 

   left JOIN AR_SET_Customer BC ON BC.CustomerID = [SAL_InvoiceHeader].billedtoid 
   AND AR_SET_Customer.PeriodID = [SAL_InvoiceHeader].PeriodID 
         left JOIN AR_SET_Customer Brokername ON Brokername.CustomerID = SAL_OrderHeader.BrokerID 
  
   AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
  Inner Join Set_CustomerCategoryWiseTax on Set_CustomerCategoryWiseTax.CustomerCategoryWiseTaxID = AR_SET_Customer.CustomerCategoryID
  and Set_CustomerCategoryWiseTax.Status = 1
 INNER JOIN SET_Company ON SET_Company.CompanyId=[SAL_InvoiceHeader].CompanyID
 AND SET_Company.Status=1
 INNER JOIN SET_OperatingUnit On SET_OperatingUnit.OperatingUnitId=SET_Company.OperatingUnitId AND SET_OperatingUnit.Status=1
 INNER JOIN SET_Business On SET_Business.BusinessId=SET_Company.BusinessId and SET_Business.IsActive=1
 INNER Join Set_DocumentType On Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
        And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
 		 left JOin SET_UnitofMeasure on SET_UnitofMeasure.uomId=[SAL_InvoiceDetail].ItemUnitID
		 And SET_UnitofMeasure.status=1
		 left join Set_Batch on Set_Batch.BatchID = SAL_ContractHeader.Season
left join SET_FBRScenario on SET_FBRScenario.ScenarioID = SAL_DispatchHeader.SROID
 WHERE [SAL_InvoiceHeader].[Status] = 1 AND SAL_InvoiceHeader.CompanyID= @CompanyID
 AND [SAL_InvoiceHeader].[PeriodID] = 1 
 AND [SAL_InvoiceHeader].invoiceID = @InvoiceHeaderID
 )as tab
 group by invoiceType	,InvoiceDate	,sellerBusinessName	,sellerProvince	,sellerAddress	,buyerNTNCNIC	,buyerBusinessName	,buyerProvince	,buyerAddress	
 ,buyerRegistrationType	,scenarioId	,hsCode	,productDescription	,rate	,uoM	,fixedNotifiedValueOrRetailPrice,salesTaxApplicable
 ,salesTaxWithheldAtSource,extraTax,sroScheduleNo,discount,saleType,sroItemSerialNo ,FiscalID,sellerNTNCNIC,TaxAmount";               
                #endregion

                
                string FinalQuery = Query_GetInvoiceData;

                if (IsInvoicePostWithBuyerName)
                    FinalQuery = FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetInvoiceDataInvoicePostWithBuyerName;



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FinalQuery, sqlparams);

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
