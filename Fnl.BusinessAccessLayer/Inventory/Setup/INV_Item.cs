using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;
using DAL.DataAccessLayer;



namespace FnI.BusinessAccessLayer.Inventory.Setup
{
    public partial class INV_Item
    {
        #region Constant Queries

        const string ItemMaster_CC_Query = @" 

SELECT 
  
  
  INV_Item.ExpenseAccountCCID
, INV_Item.CostOfSalesAccountCCID
, INV_Item.SalesAccountCCID
, INV_Item.CodeCombinationID
, INV_Item.InventoryAccountCCID
, INV_Item.TAXCCID
, INV_Item.PurchasingTaxCCID
--------------------
, INV_Item.Description
, SET_UnitofMeasure.UOMName
, INV_Item.EncumbranceAccountCCID 
, INV_Item.QtyRcvExceptionCode
, INV_Item.MarketPrice
, INV_Item.InventoryItemStatusCode
, INV_Item.ItemCatalogGroupID
, INV_SET_Category.CategoryID
, SET_ItemCatalog.Description AS CatalogName
, INV_SET_Category.CategoryName AS CategoryName 
, INV_Item.QtyRcvTolerancePercent as Tolerance
, INV_Item.TAXCCID as SaleTaxPayableAccount
, INV_Item.SalesTaxExpenseCCID
, INV_Item.FEDTaxCCID as FurtherTaxPayableAccount
, INV_Item.FEDTaxExpenseCCID as FurtherTaxExpenceAccount
, INV_Item.ItemID
, INV_Item.EquipmentTypeID
, FA_SET_AssetType.AssetTypeName
FROM INV_Item 
	INNER JOIN INV_SET_Category ON 
			--INV_SET_Category.CompanyID = INV_Item.CompanyID AND 
    INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
	LEFT JOIN SET_ItemCatalog ON 
			SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID 
			and SET_ItemCatalog.Status = 1

	inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode

	 left join FA_SET_AssetType on INV_Item.EquipmentTypeID = FA_SET_AssetType.AssetTypeID
WHERE 
--INV_Item.[CompanyID] = @CompanyID and 
INV_Item.Status = 1 
and SET_UnitofMeasure.Status = 1



";

        const string Query_SelectAllForDDLSPL = @" 
SELECT INV_Item.[ItemID], [Description], INV_Item.[InventoryItemStatusCode]
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference]

FROM [INV_Item] 
    Inner Join SPL_SupplierItems ON SPL_SupplierItems.ItemID = INV_Item.ItemID 
--And SPL_SupplierItems.CompanyID = INV_Item.CompanyID 
AND SPL_SupplierItems.Status = 1 
    left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE [INV_Item].[Status] = 1 --AND ([INV_Item].[CompanyID] = @CompanyID OR @CompanyID is null) 
    AND (SPL_SupplierItems.SupplierID = @SupplierID or @SupplierID is null) ";



        const string Query_item_By_CategorySPLSupplier = @" 
SELECT INV_SET_Category.CategoryName, INV_Item.Description, INV_SET_Category.CategoryID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference]
FROM INV_Item 
    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--and INV_Item.CompanyID = INV_SET_Category.CompanyID 
    Inner Join SPL_SupplierItems ON SPL_SupplierItems.ItemID = INV_Item.ItemID 
--And SPL_SupplierItems.CompanyID = INV_Item.CompanyID 
AND SPL_SupplierItems.Status = 1 
    left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE INV_SET_Category.CategoryID = @CategoryID 
--AND INV_Item.CompanyID = @CompanyID 
AND INV_SET_Category.Status = 1 AND INV_Item.Status = 1 
    AND (SPL_SupplierItems.SupplierID = @SupplierID or SPL_SupplierItems.SupplierID is null) ";

        const string Query_DashBoardMaxQuantity = @" 
select item.ItemID, item.Description As ItemName, Cast(CountOfItems.ItemCount as Decimal(18, 2)) as ItemCount, item.MaximumQuantity, 
    Convert(Decimal(10, 2), item.listPricePerUnit) As Price, Round(IsNull(
    (
		Select top 1 UnitPrice from AP_PurchaseOrderDetail 
            Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
                and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
		and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
		And AP_PurchaseOrderHeader.Status = 1
		Where AP_PurchaseOrderDetail.CompanyID = @CompanyID
		and AP_PurchaseOrderDetail.PeriodID = @PeriodID
		and AP_PurchaseOrderDetail.Status = 1
		and AP_PurchaseOrderDetail.ItemID = item.ItemID
		Order by AP_PurchaseOrderHeader.DocumentDate desc
	),0),2) As LastPrice,
ROW_NUMBER() Over (Order by item.Description) as 'Row'
from INV_Item item
	Inner Join
		(
			select INV_StockTransactionHeader.CompanyID, INV_StockTransactionDetail.ItemID, 
				Sum( case SET_DocumentType.TransactionImpact when  'No Impact' then 0
					else INV_StockTransactionDetail.TransactionQuantity End
				) as ItemCount
			From INV_StockTransactionDetail 
				Inner Join Inv_StockTransactionHeader 
					ON Inv_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
					And Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
					And Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
					AND Inv_StockTransactionHeader.Status = 1 
				Inner Join Set_DocumentType 
					On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
					And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
					And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
					AND Set_DocumentType.Status = 1 
			Where INV_StockTransactionDetail.CompanyID = @CompanyID and INV_StockTransactionDetail.PeriodID = @PeriodID  
				and INV_StockTransactionDetail.Status = 1 
			Group By INV_StockTransactionDetail.ItemID, INV_StockTransactionHeader.CompanyID 
		) As CountOfItems On 
--item.CompanyID = CountOfItems.CompanyID And 
item.ItemID = CountOfItems.ItemID 
Where 
--item.CompanyID = @CompanyID And 
item.Status = 1 And CountOfItems.ItemCount > item.MaximumQuantity
				
				
	";

        const string Query_DashBoardOrder = @"
select item.ItemID, item.Description As ItemName, Cast(CountOfItems.ItemCount as Decimal(18, 2)) as ItemCount, item.FixedOrderQuantity, 
	Convert(Decimal(10,2),item.listPricePerUnit) As Price,
		Round(IsNull((
		Select top 1 UnitPrice from AP_PurchaseOrderDetail
		Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
		and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
		and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
		And AP_PurchaseOrderHeader.Status = 1
		Where AP_PurchaseOrderDetail.CompanyID = @CompanyID
		and AP_PurchaseOrderDetail.PeriodID = @PeriodID
		and AP_PurchaseOrderDetail.Status = 1
		and AP_PurchaseOrderDetail.ItemID = item.ItemID
		Order by AP_PurchaseOrderHeader.DocumentDate desc
	),0),2) As LastPrice,
	ROW_NUMBER() Over (Order by item.Description) as 'Row'
from INV_Item item
	Inner Join
		(
			select INV_StockTransactionHeader.CompanyID, INV_StockTransactionDetail.ItemID, 
				Sum( case SET_DocumentType.TransactionImpact when  'No Impact' then 0
					else INV_StockTransactionDetail.TransactionQuantity End
				) as ItemCount
			From INV_StockTransactionDetail 
				Inner Join Inv_StockTransactionHeader 
					ON Inv_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
					And Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
					And Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
					AND Inv_StockTransactionHeader.Status = 1 
				Inner Join Set_DocumentType 
					On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
					And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
					And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
					AND Set_DocumentType.Status = 1 
			Where INV_StockTransactionDetail.CompanyID = @CompanyID and INV_StockTransactionDetail.PeriodID = @PeriodID 
				and INV_StockTransactionDetail.Status = 1 
			Group By INV_StockTransactionDetail.ItemID, INV_StockTransactionHeader.CompanyID 
		) As CountOfItems On 
--item.CompanyID = CountOfItems.CompanyID And 
item.ItemID = CountOfItems.ItemID 
Where item.Status = 1 And CountOfItems.ItemCount <= item.FixedOrderQuantity";


        const string Query_DashBoardMinimumQuantity = @"

select 
item.ItemID, 
item.Description As ItemName, 
Cast(CountOfItems.ItemCount as Decimal(18, 2)) as ItemCount, 
item.MinimumQuantity, 
Convert(Decimal(10,2),item.listPricePerUnit) As Price,
	Round(IsNull((
		Select top 1 UnitPrice from AP_PurchaseOrderDetail
		Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
		and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
		and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
		And AP_PurchaseOrderHeader.Status = 1
		Where AP_PurchaseOrderDetail.CompanyID = 7
		and AP_PurchaseOrderDetail.PeriodID = 9
		and AP_PurchaseOrderDetail.Status = 1
		and AP_PurchaseOrderDetail.ItemID = item.ItemID
		Order by AP_PurchaseOrderHeader.DocumentDate desc
	),0),2) As LastPrice,
ROW_NUMBER() Over (Order by item.Description) as 'Row'
from INV_Item item
	Inner Join
		(
			select INV_StockTransactionHeader.CompanyID, INV_StockTransactionDetail.ItemID, 
				Sum( case SET_DocumentType.TransactionImpact when  'No Impact' then 0
					else INV_StockTransactionDetail.TransactionQuantity End
				) as ItemCount
			From INV_StockTransactionDetail 
				Inner Join Inv_StockTransactionHeader 
					ON Inv_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
					And Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
					And Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
					AND Inv_StockTransactionHeader.Status = 1 
				Inner Join Set_DocumentType 
					On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
					And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
					And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
					AND Set_DocumentType.Status = 1 
			Where INV_StockTransactionDetail.CompanyID = @CompanyID and INV_StockTransactionDetail.PeriodID = @PeriodID 
				and INV_StockTransactionDetail.Status = 1 
			Group By INV_StockTransactionDetail.ItemID, INV_StockTransactionHeader.CompanyID 
		) As CountOfItems On 
--item.CompanyID = CountOfItems.CompanyID And 
item.ItemID = CountOfItems.ItemID 
Where item.Status = 1 And CountOfItems.ItemCount <= item.MinimumQuantity";

        const string Query_SelectAll_Searching_WithPaging = @"  SELECT [INV_Item].[CompanyID], [INV_Item].[ItemID], [INV_Item].[Description], [INV_Item].[InvoicingRuleID], [INV_Item].[ChartofAccountID], 
        [INV_Item].[CodeCombinationID], [INV_Item].[ExpenseAccountCCID], [INV_Item].[EncumbranceAccountCCID], [INV_Item].[CostOfSalesAccountCCID], 
        [INV_Item].[SalesAccountCCID], [INV_Item].[FEDTaxCCID], [INV_Item].[ExciseDutyCCID], [INV_Item].[SalesDiscountCCID], [INV_Item].[PurchaseDiscountCCID], 
        [INV_Item].[ItemCatalogGroupID], [INV_Item].[DefaultSupplierID], [INV_Item].[QtyRcvExceptionCode], [INV_Item].[MarketPrice], [INV_Item].[HazardClassID], 
        [INV_Item].[QtyRcvTolerancePercent], [INV_Item].[ListPricePerUnit], [INV_Item].[UnNumberID], [INV_Item].[PriceTolerancePercent], [INV_Item].[AssetCategoryID], 
        [INV_Item].[RoundingFactor], [INV_Item].[UnitOfIssue], [INV_Item].[AutoLotAlphaPrefix], [INV_Item].[StartAutoLotNumber], [INV_Item].[LotControlCode], 
        [INV_Item].[ShelfLifeCode], [INV_Item].[ShelfLifeDays], [INV_Item].[SerialNumberControlCode], [INV_Item].[StartAutoSerialNumber], 
        [INV_Item].[AutoSerialAlphaPrefix], [INV_Item].[SourceTypeID], [INV_Item].[UnitWeight], [INV_Item].[WeightUOMCode], [INV_Item].[VolumeUOMCode], 
        [INV_Item].[UnitVolume], [INV_Item].[ShrinkageRate], [INV_Item].[LeadTimeLotSize], [INV_Item].[StdLotSize], [INV_Item].[PrimaryUOMCode], 
        [INV_Item].[PrimaryUnitOfMeasure], [INV_Item].[AllowedUnitsLookupCode], [INV_Item].[InventoryItemStatusCode], [INV_Item].[FixedLotMultipler], 
        [INV_Item].[RoundingControlTypeID], [INV_Item].[MinimumQuantity], [INV_Item].[MaximumQuantity], [INV_Item].[MinimumOrderQuantity], 
        [INV_Item].[MaximumOrderQuantity], [INV_Item].[FixedOrderQuantity], [INV_Item].[FixedDaysSupply], [INV_Item].[WarrantyVendorID], 
        [INV_Item].[MaximunWarrantyAmount], [INV_Item].[ResponseTimePeriodCode], [INV_Item].[TAXCCID], [INV_Item].[ItemTypeID], [INV_Item].[PurchasingTaxCCID], 
        [INV_Item].[EquipmentTypeID], [INV_Item].[WebStatus], [INV_Item].[PurchasingItemFlag], [INV_Item].[ShippableItemFlag], [INV_Item].[CustomerOrderFlag], 
        [INV_Item].[InternalOrderFlag], [INV_Item].[ServiceItemFlag], [INV_Item].[InventoryItemFlag], [INV_Item].[EngItemFlag], [INV_Item].[InventoryAssetFlag], 
        [INV_Item].[PurchasingEnabledFlag], [INV_Item].[CustomerOrderEnabledFlag], [INV_Item].[InternalOrderEnabledFlag], [INV_Item].[SoTransactionsFlag], 
        [INV_Item].[MTLTransactionEnabledFlag], [INV_Item].[StockEnabledFlag], [INV_Item].[CatalogStatusFlag], [INV_Item].[ReturnableFlag], [INV_Item].[CollateralFlag], 
        [INV_Item].[TaxableFlag], [INV_Item].[AllowItemDescUpdateFlag], [INV_Item].[InspectionRequiredFlag], [INV_Item].[ReceiptRequiredFlag], 
        [INV_Item].[RFQRequiredFlag], [INV_Item].[AllowSubstituteReceiptsFlag], [INV_Item].[AllowUnorderedReceiptsFlag], [INV_Item].[AllowExpressDeliveryFlag], 
        [INV_Item].[PreventiveMaintenanceFlag], [INV_Item].[InvoiceableItemFlag], [INV_Item].[MustUseApprovedVendorFlag], [INV_Item].[CheckShortagesFlag], 
        [INV_Item].[ElectronicFlag], [INV_Item].[DownloadableFlag], [INV_Item].[VolDiscountExemptFlag], [INV_Item].[CouponExemptFlag], 
        [INV_Item].[CommsActivationReqdFlag], [INV_Item].[OrderableOnWebFlag], [INV_Item].[IndivisibleFlag], [INV_Item].[EnabledFlag], [INV_Item].[SummaryFlag], 
        [INV_Item].[Status], [INV_Item].[CreatedBy], [INV_Item].[CreateDate], [INV_Item].[UpdatedBy], [INV_Item].[UpdateDate], [INV_Item].[CreatedByValue], 
        [INV_Item].[UpdatedByValue], [SET_ItemCatalog].[Description] AS ItemCatalogGroupDescription, [FA_SET_AssetType].AssetTypeName AS EquipmentTypeDescription, 
        ROW_NUMBER() OVER (ORDER BY [SET_ItemCatalog].[Description],Inv_item.description) AS 'Row' 
        FROM [INV_Item]
        LEFT JOIN [SET_ItemCatalog] ON [SET_ItemCatalog].[ItemCatalogID] = [INV_Item].[ItemCatalogGroupID] AND [SET_ItemCatalog].[Status] = 1 
        --LEFT JOIN [SET_EquipmentType] ON [SET_EquipmentType].[EquipmentTypeID] = [INV_Item].[EquipmentTypeID] AND [SET_EquipmentType].[Status] = 1 
        Left JOIN [FA_SET_AssetType] on  [FA_SET_AssetType].AssetTypeID = [INV_Item].[EquipmentTypeID] AND [FA_SET_AssetType].Status = 1
  ";

        const string Query_SelectAll_Searching = @" 
        SELECT [INV_Item].[CompanyID], [INV_Item].[ItemID], [INV_Item].[Description], [INV_Item].[InvoicingRuleID], [INV_Item].[ChartofAccountID], 
        [INV_Item].[CodeCombinationID], [INV_Item].[ExpenseAccountCCID], [INV_Item].[EncumbranceAccountCCID], [INV_Item].[CostOfSalesAccountCCID], 
        [INV_Item].[SalesAccountCCID], [INV_Item].[FEDTaxCCID], [INV_Item].[ExciseDutyCCID], [INV_Item].[SalesDiscountCCID], [INV_Item].[PurchaseDiscountCCID], 
        [INV_Item].[ItemCatalogGroupID], [INV_Item].[DefaultSupplierID], [INV_Item].[QtyRcvExceptionCode], [INV_Item].[MarketPrice], [INV_Item].[HazardClassID], 
        [INV_Item].[QtyRcvTolerancePercent], [INV_Item].[ListPricePerUnit], [INV_Item].[UnNumberID], [INV_Item].[PriceTolerancePercent], [INV_Item].[AssetCategoryID], 
        [INV_Item].[RoundingFactor], [INV_Item].[UnitOfIssue], [INV_Item].[AutoLotAlphaPrefix], [INV_Item].[StartAutoLotNumber], [INV_Item].[LotControlCode], 
        [INV_Item].[ShelfLifeCode], [INV_Item].[ShelfLifeDays], [INV_Item].[SerialNumberControlCode], [INV_Item].[StartAutoSerialNumber], 
        [INV_Item].[AutoSerialAlphaPrefix], [INV_Item].[SourceTypeID], [INV_Item].[UnitWeight], [INV_Item].[WeightUOMCode], [INV_Item].[VolumeUOMCode], 
        [INV_Item].[UnitVolume], [INV_Item].[ShrinkageRate], [INV_Item].[LeadTimeLotSize], [INV_Item].[StdLotSize], [INV_Item].[PrimaryUOMCode], 
        [INV_Item].[PrimaryUnitOfMeasure], [INV_Item].[AllowedUnitsLookupCode], [INV_Item].[InventoryItemStatusCode], [INV_Item].[FixedLotMultipler], 
        [INV_Item].[RoundingControlTypeID], [INV_Item].[MinimumQuantity], [INV_Item].[MaximumQuantity], [INV_Item].[MinimumOrderQuantity], 
        [INV_Item].[MaximumOrderQuantity], [INV_Item].[FixedOrderQuantity], [INV_Item].[FixedDaysSupply], [INV_Item].[WarrantyVendorID], 
        [INV_Item].[MaximunWarrantyAmount], [INV_Item].[ResponseTimePeriodCode], [INV_Item].[TAXCCID], [INV_Item].[ItemTypeID], [INV_Item].[PurchasingTaxCCID], 
        [INV_Item].[EquipmentTypeID], [INV_Item].[WebStatus], [INV_Item].[PurchasingItemFlag], [INV_Item].[ShippableItemFlag], [INV_Item].[CustomerOrderFlag], 
        [INV_Item].[InternalOrderFlag], [INV_Item].[ServiceItemFlag], [INV_Item].[InventoryItemFlag], [INV_Item].[EngItemFlag], [INV_Item].[InventoryAssetFlag], 
        [INV_Item].[PurchasingEnabledFlag], [INV_Item].[CustomerOrderEnabledFlag], [INV_Item].[InternalOrderEnabledFlag], [INV_Item].[SoTransactionsFlag], 
        [INV_Item].[MTLTransactionEnabledFlag], [INV_Item].[StockEnabledFlag], [INV_Item].[CatalogStatusFlag], [INV_Item].[ReturnableFlag], [INV_Item].[CollateralFlag], 
        [INV_Item].[TaxableFlag], [INV_Item].[AllowItemDescUpdateFlag], [INV_Item].[InspectionRequiredFlag], [INV_Item].[ReceiptRequiredFlag], 
        [INV_Item].[RFQRequiredFlag], [INV_Item].[AllowSubstituteReceiptsFlag], [INV_Item].[AllowUnorderedReceiptsFlag], [INV_Item].[AllowExpressDeliveryFlag], 
        [INV_Item].[PreventiveMaintenanceFlag], [INV_Item].[InvoiceableItemFlag], [INV_Item].[MustUseApprovedVendorFlag], [INV_Item].[CheckShortagesFlag], 
        [INV_Item].[ElectronicFlag], [INV_Item].[DownloadableFlag], [INV_Item].[VolDiscountExemptFlag], [INV_Item].[CouponExemptFlag], 
        [INV_Item].[CommsActivationReqdFlag], [INV_Item].[OrderableOnWebFlag], [INV_Item].[IndivisibleFlag], [INV_Item].[EnabledFlag], [INV_Item].[SummaryFlag], 
        [INV_Item].[Status], [INV_Item].[CreatedBy], [INV_Item].[CreateDate], [INV_Item].[UpdatedBy], [INV_Item].[UpdateDate], [INV_Item].[CreatedByValue], 
        [INV_Item].[UpdatedByValue], [SET_ItemCatalog].[Description] AS ItemCatalogGroupDescription, [SET_EquipmentType].[Description] AS EquipmentTypeDescription
        FROM [INV_Item]
        LEFT JOIN [SET_ItemCatalog] ON [SET_ItemCatalog].[ItemCatalogID] = [INV_Item].[ItemCatalogGroupID] AND [SET_ItemCatalog].[Status] = 1 
        LEFT JOIN [SET_EquipmentType] ON [SET_EquipmentType].[EquipmentTypeID] = [INV_Item].[EquipmentTypeID] AND [SET_EquipmentType].[Status] = 1 ";

        #region Query_BelowMinimumLevelStock

        const string Query_BelowMinimumLevelStock = @" 
Select INV_Item.ItemID, Inv_Set_Category.CategoryName As ItemCategory, INV_Item.Description As ItemDescription, SET_UnitofMeasure.UOMName As UOM, 
    Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity END) As QtyInHand, 
    IsNull(Avg(INV_StockTransactionDetail.TransactionCost), 0) As AvgPrice, LastPurchaseInfo.UnitPrice As SellingPrice, LastPurchaseInfo.PurchaseOrderDate As PODate, 
    LastPurchaseInfo.ListPricePerUnit As POPrice, LastPurchaseInfo.CustomerName As POSupplier, LastPurchaseInfo.Quantity As POQty, 
    LastTransactionInfo.StockDate As StockTransDate, LastTransactionInfo.StockTransPrice, LastTransactionInfo.TransactionQuantity As StockTransQty, 
    LastTransactionInfo.DocumentType, LastTransactionInfo.DocumentTypeID 
From INV_Item 
    Inner Join Inv_Set_Category On INV_Item.ItemTypeID = Inv_Set_Category.CategoryID 
--And INV_Item.CompanyID = Inv_Set_Category.CompanyID 
        And Inv_Set_Category.IsActive = 1 And Inv_Set_Category.Status = 1 
        And (Inv_Set_Category.IsInvisibleUponStockReports IS NULL OR Inv_Set_Category.IsInvisibleUponStockReports = 0) 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID And SET_UnitofMeasure.IsActive = 1 And SET_UnitofMeasure.Status = 1 
    Inner Join INV_StockTransactionDetail On INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--And INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID 
        And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.IsActive = 1 And INV_StockTransactionDetail.Status = 1 
    Inner JOIN Inv_StockTransactionHeader ON Inv_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        And Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID And Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        And Inv_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
        And Inv_StockTransactionHeader.CompanyID = Set_DocumentType.CompanyID And Inv_StockTransactionHeader.PeriodID = Set_DocumentType.PeriodID 
        And Set_DocumentType.Status = 1 

{0}


    Left Join
    (
        Select Distinct PurchaseOrder.CompanyID, PurchaseOrder.ItemID, PurchaseOrder.PurchaseOrderDate, AP_PurchaseOrderDetail.UnitPrice, 
            AP_PurchaseOrderDetail.ListPricePerUnit, AR_SET_Customer.CustomerName, AP_PurchaseOrderDetail.Quantity 
        From AP_PurchaseOrderDetail 
            Inner Join AP_PurchaseOrderHeader On AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
                And AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
              --  And (AP_PurchaseOrderHeader.VendorID = @SupplierID Or @SupplierID Is Null) 
            Inner Join
            (
                Select AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.PeriodID, AP_PurchaseOrderDetail.ItemID, 
                    Max(AP_PurchaseOrderHeader.DocumentDate) As PurchaseOrderDate, Max(AP_PurchaseOrderHeader.CreateDate) As PODate 
                from AP_PurchaseOrderHeader 
                    Inner Join AP_PurchaseOrderDetail On AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
                        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
                        And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID And AP_PurchaseOrderDetail.IsActive = 1 
                        And	AP_PurchaseOrderDetail.Status = 1 
                Where AP_PurchaseOrderHeader.CompanyID = @CompanyID And AP_PurchaseOrderHeader.PeriodID = @PeriodID And AP_PurchaseOrderHeader.IsActive = 1 
                    And AP_PurchaseOrderHeader.Status = 1 
                Group by AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.PeriodID, AP_PurchaseOrderDetail.ItemID
            ) As PurchaseOrder On AP_PurchaseOrderHeader.CompanyID = PurchaseOrder.CompanyID And AP_PurchaseOrderHeader.PeriodID = PurchaseOrder.PeriodID 
                And AP_PurchaseOrderDetail.ItemID = PurchaseOrder.ItemID And AP_PurchaseOrderHeader.DocumentDate = PurchaseOrder.PurchaseOrderDate 
                And AP_PurchaseOrderHeader.CreateDate = PurchaseOrder.PODate 
            Inner Join AR_SET_Customer On 
--AP_PurchaseOrderHeader.CompanyID = AR_SET_Customer.CompanyID And 
AP_PurchaseOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
                And AP_PurchaseOrderHeader.VendorID = AR_SET_Customer.CustomerID
    ) As LastPurchaseInfo On 
--INV_Item.CompanyID = LastPurchaseInfo.CompanyID And 
INV_Item.ItemID = LastPurchaseInfo.ItemID 
        Left Join
        (
        Select Stock.CompanyID, Stock.PeriodID, Stock.ItemID, Stock.StockDate, Stock.StockTransPrice, INV_StockTransactionDetail.TransactionQuantity, 
        SET_DocumentType.DocumentType, INV_StockTransactionHeader.DocumentTypeID
        From INV_StockTransactionDetail
        Inner Join INV_StockTransactionHeader On INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
        And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        And INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
        Inner Join
        (
        Select INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionDetail.ItemID, 
        Max(INV_StockTransactionHeader.DocumentDate) As StockDate, Avg(INV_StockTransactionDetail.TransactionCost) As StockTransPrice, 
        Max(INV_StockTransactionHeader.CreateDate) As StockCreateDate
        from INV_StockTransactionHeader
        Inner Join INV_StockTransactionDetail On INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And INV_StockTransactionDetail.IsActive = 1 
        And	INV_StockTransactionDetail.Status = 1
        Where INV_StockTransactionHeader.CompanyID = @CompanyID And INV_StockTransactionHeader.PeriodID = @PeriodID 
        And INV_StockTransactionHeader.IsActive = 1 And INV_StockTransactionHeader.Status = 1
        Group by INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionDetail.ItemID
        ) As Stock On INV_StockTransactionHeader.CompanyID = Stock.CompanyID And INV_StockTransactionHeader.PeriodID = Stock.PeriodID 
        And INV_StockTransactionDetail.ItemID = Stock.ItemID And INV_StockTransactionHeader.DocumentDate = Stock.StockDate 
        And INV_StockTransactionHeader.CreateDate = Stock.StockCreateDate
        Inner Join SET_DocumentType On INV_StockTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
        And INV_StockTransactionHeader.PeriodID = SET_DocumentType.PeriodID And INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        ) As LastTransactionInfo On 
--INV_Item.CompanyID = LastTransactionInfo.CompanyID And 
INV_Item.ItemID = LastTransactionInfo.ItemID 
        Where INV_Item.Status = 1 --and INV_Item.CompanyID = @CompanyID
         AND INV_StockTransactionHeader.DocumentDate  between @FromDate AND @ToDate
         And (INV_StockTransactionHeader.LocationID = @LocationID Or @LocationID Is Null) 
         {AdditionalParameters}
        Group by INV_Item.ItemID, Inv_Set_Category.CategoryName, INV_Item.Description, SET_UnitofMeasure.UOMName, INV_Item.MinimumQuantity, LastPurchaseInfo.UnitPrice, 
        LastPurchaseInfo.PurchaseOrderDate, LastPurchaseInfo.ListPricePerUnit, LastPurchaseInfo.CustomerName, LastPurchaseInfo.Quantity, LastTransactionInfo.StockDate, 
        LastTransactionInfo.StockTransPrice, LastTransactionInfo.TransactionQuantity, LastTransactionInfo.DocumentType, LastTransactionInfo.DocumentTypeID 
        Having IsNull(Sum(INV_StockTransactionDetail.TransactionQuantity), 0) <= IsNull(INV_Item.MinimumQuantity, 0)
        Order by INV_Item.Description ";

        #endregion

        #region Query_MaximumLevelStock

        const string Query_MaximumLevelStock = @" 
        Select INV_Item.ItemID, Inv_Set_Category.CategoryName As ItemCategory,Inv_Set_Category.IsInvisibleUponStockReports, INV_Item.Description As ItemDescription, SET_UnitofMeasure.UOMName As UOM, 
        Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' 
		then 0
		else INV_StockTransactionDetail.TransactionQuantity END) As QtyInHand, IsNull(Avg(INV_StockTransactionDetail.TransactionCost), 0) As AvgPrice, 
        LastPurchaseInfo.UnitPrice As SellingPrice, LastPurchaseInfo.PurchaseOrderDate As PODate, LastPurchaseInfo.ListPricePerUnit As POPrice, 
        LastPurchaseInfo.CustomerName As POSupplier, LastPurchaseInfo.Quantity As POQty, LastTransactionInfo.StockDate As StockTransDate, 
        LastTransactionInfo.StockTransPrice, LastTransactionInfo.TransactionQuantity As StockTransQty, LastTransactionInfo.DocumentType, 
        LastTransactionInfo.DocumentTypeID,DocumentTypesGroup.GroupName 
        From INV_Item 
        Inner Join Inv_Set_Category On INV_Item.ItemTypeID = Inv_Set_Category.CategoryID 
--And INV_Item.CompanyID = Inv_Set_Category.CompanyID 
        And Inv_Set_Category.IsActive = 1 And Inv_Set_Category.Status = 1
        Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID And SET_UnitofMeasure.IsActive = 1 And SET_UnitofMeasure.Status = 1
        Left Join INV_StockTransactionDetail On INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--And INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID 
        And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.IsActive = 1 And INV_StockTransactionDetail.Status = 1
	    Inner JOIN Inv_StockTransactionHeader ON Inv_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
			And Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
			And Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			And Inv_StockTransactionHeader.Status = 1
		Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID
			And Inv_StockTransactionHeader.CompanyID = Set_DocumentType.CompanyID 
			And Inv_StockTransactionHeader.PeriodID = Set_DocumentType.PeriodID 
			And Set_DocumentType.Status = 1
        Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And  
        Left Join
        (
        Select Distinct PurchaseOrder.CompanyID, PurchaseOrder.ItemID, PurchaseOrder.PurchaseOrderDate, AP_PurchaseOrderDetail.UnitPrice, 
        AP_PurchaseOrderDetail.ListPricePerUnit, AR_SET_Customer.CustomerName, AP_PurchaseOrderDetail.Quantity
        From AP_PurchaseOrderDetail
        Inner Join AP_PurchaseOrderHeader On AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
        Inner Join
        (
        Select AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.PeriodID, AP_PurchaseOrderDetail.ItemID, 
        Max(AP_PurchaseOrderHeader.DocumentDate) As PurchaseOrderDate, Max(AP_PurchaseOrderHeader.CreateDate) As PODate
        from AP_PurchaseOrderHeader
        Inner Join AP_PurchaseOrderDetail On AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        And AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID And AP_PurchaseOrderDetail.IsActive = 1 
        And	AP_PurchaseOrderDetail.Status = 1
        Where AP_PurchaseOrderHeader.CompanyID = @CompanyID And AP_PurchaseOrderHeader.PeriodID = @PeriodID And AP_PurchaseOrderHeader.IsActive = 1 
        And AP_PurchaseOrderHeader.Status = 1
        Group by AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.PeriodID, AP_PurchaseOrderDetail.ItemID
        ) As PurchaseOrder On AP_PurchaseOrderHeader.CompanyID = PurchaseOrder.CompanyID And AP_PurchaseOrderHeader.PeriodID = PurchaseOrder.PeriodID 
        And AP_PurchaseOrderDetail.ItemID = PurchaseOrder.ItemID And AP_PurchaseOrderHeader.DocumentDate = PurchaseOrder.PurchaseOrderDate 
        And AP_PurchaseOrderHeader.CreateDate = PurchaseOrder.PODate
        Inner Join AR_SET_Customer On --AP_PurchaseOrderHeader.CompanyID = AR_SET_Customer.CompanyID And 
        AP_PurchaseOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        And AP_PurchaseOrderHeader.VendorID = AR_SET_Customer.CustomerID 
        ) As LastPurchaseInfo On 
--INV_Item.CompanyID = LastPurchaseInfo.CompanyID And 
INV_Item.ItemID = LastPurchaseInfo.ItemID 
        Left Join
        (
        Select Stock.CompanyID, Stock.PeriodID, Stock.ItemID, Stock.StockDate, Stock.StockTransPrice, INV_StockTransactionDetail.TransactionQuantity, 
        SET_DocumentType.DocumentType, INV_StockTransactionHeader.DocumentTypeID
        From INV_StockTransactionDetail
        Inner Join INV_StockTransactionHeader On INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
        And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        And INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
        Inner Join
        (
        Select INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionDetail.ItemID, 
        Max(INV_StockTransactionHeader.DocumentDate) As StockDate, Avg(INV_StockTransactionDetail.TransactionCost) As StockTransPrice, 
        Max(INV_StockTransactionHeader.CreateDate) As StockCreateDate
        from INV_StockTransactionHeader
        Inner Join INV_StockTransactionDetail On INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And INV_StockTransactionDetail.IsActive = 1 
        And	INV_StockTransactionDetail.Status = 1
        Where INV_StockTransactionHeader.CompanyID = @CompanyID And INV_StockTransactionHeader.PeriodID = @PeriodID 
        And INV_StockTransactionHeader.IsActive = 1 And INV_StockTransactionHeader.Status = 1
        Group by INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionDetail.ItemID
        ) As Stock On INV_StockTransactionHeader.CompanyID = Stock.CompanyID And INV_StockTransactionHeader.PeriodID = Stock.PeriodID 
        And INV_StockTransactionDetail.ItemID = Stock.ItemID And INV_StockTransactionHeader.DocumentDate = Stock.StockDate 
        And INV_StockTransactionHeader.CreateDate = Stock.StockCreateDate
        Inner Join SET_DocumentType On INV_StockTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
        And INV_StockTransactionHeader.PeriodID = SET_DocumentType.PeriodID And INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        ) As LastTransactionInfo On 
--INV_Item.CompanyID = LastTransactionInfo.CompanyID And 
INV_Item.ItemID = LastTransactionInfo.ItemID 
        Where INV_Item.Status = 1 --and INV_Item.CompanyID = @CompanyID
        and ISNULL(Inv_Set_Category.IsInvisibleUponStockReports,0) = 0
        {AdditionalParameters}
        Group by INV_Item.ItemID, Inv_Set_Category.CategoryName,Inv_Set_Category.IsInvisibleUponStockReports, INV_Item.Description, SET_UnitofMeasure.UOMName, INV_Item.MaximumQuantity, LastPurchaseInfo.UnitPrice, 
        LastPurchaseInfo.PurchaseOrderDate, LastPurchaseInfo.ListPricePerUnit, LastPurchaseInfo.CustomerName, LastPurchaseInfo.Quantity, LastTransactionInfo.StockDate, 
        LastTransactionInfo.StockTransPrice, LastTransactionInfo.TransactionQuantity, LastTransactionInfo.DocumentType, LastTransactionInfo.DocumentTypeID,DocumentTypesGroup.GroupName 
        Having IsNull(Sum(INV_StockTransactionDetail.TransactionQuantity), 0) >= IsNull(INV_Item.MaximumQuantity, 0)
        Order by INV_Item.Description ";

        #endregion

        const string Query_Cane_Items = @"select INV_SET_Category.CategoryID,INV_Item.ItemID,INV_SET_Category.CategoryName,INV_Item.Description as ItemName, INV_Item.PrimaryUOMCode,ListPricePerUnit, case when INV_Item.IndivisibleFlag = 1 then 'Variety' else 'None Variety' end Variety, INV_Item.CreateDate from INV_Item
        inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
      inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
        where SET_ItemCatalog.Description = 'Cane' --and INV_Item.CompanyID = @CompanyID 
and INV_Item.Status = 1 {0}";

        const string Query_item_By_Category = @" 
SELECT INV_SET_Category.CategoryName, INV_Item.Description, INV_SET_Category.CategoryID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference]
FROM INV_Item 

    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--and INV_Item.CompanyID = INV_SET_Category.CompanyID 
   left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE INV_SET_Category.CategoryID = @CategoryID --AND INV_Item.CompanyID = @CompanyID 
AND INV_SET_Category.Status = 1 AND INV_Item.Status = 1 ";

        const string Query_item_By_CatalogID = @" 
SELECT SET_ItemCatalog.Description as CatalogDescriptin, INV_Item.Description, SET_ItemCatalog.ItemCatalogID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference], INV_Item.ItemTypeID
FROM INV_Item 

    INNER JOIN SET_ItemCatalog ON INV_Item.ItemCatalogGroupID = SET_ItemCatalog.ItemCatalogID 
--and INV_Item.CompanyID = INV_SET_Category.CompanyID 
   left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE SET_ItemCatalog.ItemCatalogID = @catalogID 
AND SET_ItemCatalog.Status = 1 AND INV_Item.Status = 1  ";


        const string Query_item_By_CatalogCategory = @"SELECT INV_SET_Category.CategoryName, INV_Item.Description, INV_SET_Category.CategoryID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference]
FROM INV_Item 

    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
	inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
   left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE INV_SET_Category.Status = 1 AND INV_Item.Status = 1
{0}
--AND  INV_SET_Category.CategoryID = @CategoryID 
{1}
--AND INV_Item.ItemCatalogGroupID=@CatalogID
";


        const string Query_item_By_Catalog = @"SELECT INV_Item.Description, INV_Item.ItemID 
FROM INV_Item 

	inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
WHERE SET_ItemCatalog.Status = 1 AND INV_Item.Status = 1
{0}";

        const string Query_item_By_Category_Top1 = @" 
SELECT top 1 *
FROM INV_Item 

    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--and INV_Item.CompanyID = INV_SET_Category.CompanyID 
   left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE INV_SET_Category.CategoryID = @CategoryID --AND INV_Item.CompanyID = @CompanyID 
AND INV_SET_Category.Status = 1
AND INV_Item.Status = 1 ";

        const string Query_item_By_Catalog_Top1 = @" 
SELECT top 1 *
FROM INV_Item 

    INNER JOIN SET_ItemCatalog ON INV_Item.ItemCatalogGroupID = SET_ItemCatalog.ItemCatalogID 
WHERE SET_ItemCatalog.ItemCatalogID = @CatalogID
AND SET_ItemCatalog.Status = 1
AND INV_Item.Status = 1 ";

        const string Query_item_By_FixedAsset = @"
        select Inv_Item.Description,
        Inv_item.ItemID,
        INV_Item.InventoryItemStatusCode
        from INV_Item
        INNER JOIN FA_AssetTypePolicy on INV_Item.ItemTypeID = FA_AssetTypePolicy.ItemCategoryID
        --AND INV_Item.CompanyID = FA_AssetTypePolicy.CompanyID
        Where INV_Item.ItemTypeID = @CategoryID
        --AND INV_Item.CompanyID = @CompanyID
        AND INV_Item.Status = 1
        AND INV_Item.InventoryAssetFlag = 1";

        const string Query_item_By_CategoryWithParameter = @" SELECT 
        INV_SET_Category.CategoryName,
        INV_Item.Description,
        INV_SET_Category.CategoryID, 
        INV_Item.ItemID 
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
        --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)							 
        WHERE INV_SET_Category.CategoryID = @CategoryID 
        --AND INV_Item.CompanyID = @CompanyID 
        AND INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1 {0} ";

        const string Query_SelectAll_InventoryAssetFlagTrue = @" SELECT * FROM [INV_Item] WHERE [Status] = 1 AND InventoryAssetFlag = 1 ";


        const string Query_SelectItemByCategory = @"  

        Select INV_SET_Category.CategoryName , INV_Item.Description , INV_Item.ItemID ,INV_SET_Category.CategoryID 
        From INV_SET_Category
        INNER JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID --AND INV_Item.CompanyID = @CompanyID
        AND INV_Item.Status = 1 AND INV_Item.InventoryAssetFlag = 1
        Where 
--INV_SET_Category.CompanyID = @CompanyID AND 
INV_SET_Category.Status = 1 ";

        const string Query_Top5Customer = @"Select Top 5 ItemID,Description From INV_Item where Status = 1 ";

        const string Query_ItemWithCategoryByCategoryName = @"
        SELECT 
        INV_SET_Category.CategoryName,
        INV_Item.Description,
        INV_SET_Category.CategoryID, 
        INV_Item.ItemID 
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
					                --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)							 
        WHERE INV_SET_Category.CategoryName = @CategoryName
        --AND INV_Item.CompanyID = @CompanyID
        AND INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1";

        const string Query_CheckItemInUsed = @" 
SELECT * FROM
    (
	    SELECT ItemID, CompanyID, Status FROM AP_PaymentDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM AR_InvoiceDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM SAL_InvoiceDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM AR_ReceiptTransactionDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM Gate_OutwardDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM INV_StockTransactionDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM PUR_EnquiryDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM AP_PurchaseOrderDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM SAL_OrderDetail
	    Union
	    SELECT ItemID, CompanyID, Status FROM FA_AssetIdentity
    ) AS tbl_Item 
WHERE tbl_Item.Status = 1 AND tbl_Item.ItemID = @ItemID ";

        const string Query_SelectAll_Item_withRawMaterial = @"Select ItemID,Description From INV_Item
where IsRawMaterial = 1 ";

        const string Query_ItemOldAndPoPrice = @"
Select TOP 1 Inv_Item.ListPricePerUnit As ListPricePerUnit, 
	ISNULL
	((
		Select top 1 UnitPrice 
		From AP_PurchaseOrderDetail 
		INNER JOIN AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
			AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
			AND AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID AND AP_PurchaseOrderHeader.StatusCode = 1 
			AND AP_PurchaseOrderHeader.Status = 1 
		INNER JOIN SET_DocumentType On SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID 
			AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
			AND	SET_DocumentType.TransactionImpact = 'Positive'
		Where AP_PurchaseOrderDetail.CompanyID = @CompanyID AND AP_PurchaseOrderDetail.PeriodID = @PeriodID 
			AND AP_PurchaseOrderDetail.ItemID = @ItemID AND AP_PurchaseOrderDetail.Status = 1 
		ORder By AP_PurchaseOrderDetail.Updatedate desc, AP_PurchaseOrderDetail.CreateDate desc
	), 0) As LastPurchasePrice,
	ISNULL
	((
		Select top 1 PriorCost 
		From INV_StockTransactionDetail 
			Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
				AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
				and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID and INV_StockTransactionHeader.Status = 1 
				and INV_StockTransactionHeader.StatusCode = 1
			INNER JOIN SET_DocumentType On SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
				AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
				AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.TransactionImpact = 'Positive'
		where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
			AND INV_StockTransactionDetail.ItemID = @ItemID AND INV_StockTransactionDetail.Status = 1 
		ORder By INV_StockTransactionDetail.UpdateDate desc, INV_StockTransactionDetail.CreateDate desc
	), 0) As PriorCost,
	ISNULL
	((
		Select top 1 EstimatedPOItemPrice 
		From INV_StockTransactionDetail 
			Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
				AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
				and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID and INV_StockTransactionHeader.Status = 1 
				and INV_StockTransactionHeader.StatusCode = 1 
		where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
			AND INV_StockTransactionDetail.ItemID = @ItemID AND INV_StockTransactionDetail.Status = 1 
		ORder By INV_StockTransactionDetail.UpdateDate desc, INV_StockTransactionDetail.CreateDate desc
	), 0) As EstimatedPOItemPrice, 
	
	ISNULL((Select Sum(TransactionQuantity) From INV_StockTransactionDetail 
        Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
				AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
				and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID and INV_StockTransactionHeader.Status = 1 
				and INV_StockTransactionHeader.StatusCode = 1
			INNER JOIN SET_DocumentType On SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
				AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
				AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.TransactionImpact = 'Negative'

Where INV_StockTransactionDetail.ItemID = @ItemID AND INV_StockTransactionDetail.Status = 1
	 	AND Cast(INV_StockTransactionDetail.CreateDate as Date) Between @datefrom AND @dateto
and INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
	 	 ),0)LastConsumption 
From INV_Item
Where INV_Item.ItemID = @ItemID 
--AND INV_Item.CompanyID = @CompanyID 
And INV_Item.Status = 1 ";

        const string Query_SelectAllForDDL = @" 
            SELECT INV_Item.[ItemID], INV_Item.[Description],INV_Item.[InventoryItemStatusCode]
,SET_SourceType.Description as SourceType ,isnull(INV_Item.[ShelfReference],'') as ShelfReference

FROM [INV_Item]
  left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID

WHERE INV_Item.[Status] = 1 ";

        const string Query_Get_ApproveGroupName = @"
            SELECT ApprovalGroupID,GroupName FROM SET_ApprovalItemGroup WHERE Status = 1 ANd IsActive = 1";

        const string Query_SelectAll_InvoiceableItemFlagTrue = @"SELECT * FROM INV_Item WHERE InvoiceableItemFlag = 1 AND Status = 1";

        const string Query_GetStatus_ItemDescriptionUpdate = @"SELECT [AllowItemDescUpdateFlag]   FROM [INV_Item] where ItemID = @itemid";

        const string Query_GetAllItemByItemID = @"SELECT 
--INV_Item.ItemID,
--INV_Item.Description,
--INV_Item.ListPricePerUnit AS Rate, 
--INV_Item.AllowItemDescUpdateFlag
--FROM INV_Item
--INV_Item.ItemID = @ItemID
--AND Status = 1

INV_Item.ItemID,
INV_Item.Description,
INV_Item.ListPricePerUnit AS Rate, 
INV_Item.AllowItemDescUpdateFlag
FROM INV_Item Where itemid = @ItemID
--INV_Item.ItemID = @ItemID
AND Status = 1
";

        const string Query_GetItemData = @"
select 
	INV_Item.PrimaryUOMCode as UOMID,
	SET_UnitOfMeasure.UOMName,
	ISNULL(INV_Item.ItemCatalogGroupID,0) as ItemCatalogGroupID,
    SET_ItemCatalog.Description as CatalogDescription,
	ISNULL(INV_Item.GSM,0) as GSM,
	ISNULL(INV_Item.Length,0) as [Length],
	ISNULL(INV_Item.Width,0) as Width,
INV_Item.InventoryAccountCCID
from INV_Item
inner join SET_UnitOfMeasure on INV_Item.PrimaryUOMCode = SET_UnitOfMeasure.UOMID
left join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
where INV_Item.ItemID = @ItemID
	--and INV_Item.CompanyID = @CompanyID

";
                const string Query_GetCategoryByItemCatalog = @"
select Distinct
	INV_SET_Category.CategoryID, 
	INV_SET_Category.CategoryName 
from INV_Item 
inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	-- 
where SET_ItemCatalog.ItemCatalogID = @ItemCatalogID
	--and INV_Item.CompanyID = @CompanyID

";

                const string Query_GetInventoryItemStatusCodeByItemCatalog = @"
select Distinct INV_SET_Category.CategoryName,INV_SET_Category.CategoryID
	--,InventoryItemStatusCode,ItemID
from INV_Item 
inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
inner join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	-- 
where SET_ItemCatalog.ItemCatalogID = @ItemCatalogID
	--and INV_Item.CompanyID = @CompanyID

";


        const string Query_UpdateListPriceUnit_By_PrimaryKey = @" UPDATE [INV_Item] SET [ListPricePerUnit] = @ListPricePerUnit, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue
		WHERE [ItemID] = @ItemID ";

        const string Query_item_By_invoiceableItem = @" SELECT 
        INV_SET_Category.CategoryName,
        INV_Item.Description,
        INV_SET_Category.CategoryID, 
        INV_Item.ItemID,
        INV_Item.InventoryItemStatusCode 
        ,SET_SourceType.Description as SourceType
        ,INV_Item.ListPricePerUnit

        ,0 as TemplateQty0
        ,0 as TemplateQty1
        ,0 as TemplateQty2
        ,0 as TemplateQty3
        ,0 as TemplateQty4
        ,0 as TemplateQty5
  
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
        --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)	
        left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
						 
        WHERE 
--INV_Item.CompanyID = @CompanyID  AND 
INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1";

        const string Query_OrderableItemsOnly = @" SELECT 
        INV_SET_Category.CategoryName,
        INV_Item.Description,
        INV_SET_Category.CategoryID, 
        INV_Item.ItemID,
        INV_Item.InventoryItemStatusCode 
        ,SET_SourceType.Description as SourceType
        ,INV_Item.ListPricePerUnit

        ,0 as TemplateQty0
        ,0 as TemplateQty1
        ,0 as TemplateQty2
        ,0 as TemplateQty3
        ,0 as TemplateQty4
        ,0 as TemplateQty5
  
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
        --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)	
        left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
						 
        WHERE --INV_Item.CompanyID = @CompanyID AND 
        INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1 and INV_Item.IsOrderable =1";

        const string Query_item_By_SecondarySales = @" SELECT 
        INV_Item.ItemID,
        INV_Item.InventoryItemStatusCode as [Item Code],
        INV_Item.Description,
        INV_Item.ListPricePerUnit as [Estimated Price]       
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
        --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)	
        left JOIn SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
        WHERE --INV_Item.CompanyID = @CompanyID AND 
        INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1";

        const string Query_Myitem_By_invoiceableItem = @" SELECT 
        INV_SET_Category.CategoryName,
        INV_Item.Description,
        INV_SET_Category.CategoryID, 
        INV_Item.ItemID,INV_Item.Status,
        INV_Item.InventoryItemStatusCode 
        FROM INV_Item 
        INNER JOIN INV_SET_Category ON (INV_Item.ItemTypeID = INV_SET_Category.CategoryID) 
        --and (INV_Item.CompanyID = INV_SET_Category.CompanyID)							 
        WHERE --INV_Item.CompanyID = @CompanyID AND 
        INV_SET_Category.Status = 1 
        AND INV_Item.Status = 1 and itemid not in(select itemid from FA_SET_AssetType where Status = 1)";

        const string Query_GetDescriptionList = @" 
select [Description] from INV_Item where [Description] like @Description + '%'
";
        
        const string Query_GetDefecteditems = @" 
SELECT INV_SET_Category.CategoryName, INV_Item.Description, INV_SET_Category.CategoryID, INV_Item.ItemID, INV_Item.InventoryItemStatusCode
,SET_SourceType.Description as SourceType ,INV_Item.[ShelfReference]

FROM [INV_Item] 
    
    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--and INV_Item.CompanyID = INV_SET_Category.CompanyID 
  left JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
WHERE 
--INV_SET_Category.CategoryID = @CategoryID AND 
--INV_Item.CompanyID = @CompanyID AND 
INV_SET_Category.Status = 1 AND INV_Item.Status = 1 AND INV_SET_Category.CategoryName like '%Defects%'";

        const string Query_Items_By_User = @"
SELECT INV_Item.Description, INV_Item.ItemID 
FROM INV_Item 
	INNER JOIN UsersInCategories ON 
--INV_Item.CompanyID = UsersInCategories.CompanyID And 
INV_Item.ItemTypeID = UsersInCategories.CategoryID 
WHERE --INV_Item.CompanyID = @CompanyID AND 
INV_Item.Status = 1 AND INV_Item.Status = 1 And INV_Item.InvoiceableItemFlag = 1 And UsersInCategories.UserID = @UserID ";


        const string ItemListPrice_CC_Query = @"SELECT   
INV_Item.ItemID
, INV_Item.Description
, SET_UnitofMeasure.UOMName
, INV_Item.EncumbranceAccountCCID 
, INV_Item.QtyRcvExceptionCode
, INV_Item.MarketPrice
, INV_Item.InventoryItemStatusCode
, INV_Item.ItemCatalogGroupID
, INV_SET_Category.CategoryID
, SET_ItemCatalog.Description AS CatalogName
, INV_SET_Category.CategoryName AS CategoryName
, INV_Item.ListPricePerUnit AS ListPricePerUnit
,SET_SourceType.SourceTypeID
,SET_SourceType.Description as SourceType
,SET_ApprovalItemGroup.GroupName
FROM INV_Item 
	INNER JOIN INV_SET_Category ON 
			--INV_SET_Category.CompanyID = INV_Item.CompanyID AND 
INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
	LEFT JOIN SET_ItemCatalog ON 
			SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID 
			and SET_ItemCatalog.Status = 1

           -- INNER JOIN INV_ItemsPriceHistory ON 
			--INV_ItemsPriceHistory.CompanyID = INV_Item.CompanyID AND INV_ItemsPriceHistory.ItemID = INV_Item.ItemID 		

Left JOIN SET_ApprovalItemGroup ON SET_ApprovalItemGroup.ApprovalGroupID = INV_Item.ApprovalItemGroupID
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode
INNER JOIN SET_SourceType on SET_SourceType.SourceTypeID = INV_Item.SourceTypeID
WHERE 
--INV_Item.[CompanyID] = 1 and 
INV_Item.Status = 1 
and SET_UnitofMeasure.Status = 1";

        const string Query_GetSaftyItemsStock = @"  Select distinct * from (
Select ROW_NUMBER() OVER (ORDER BY Cast(INV_Item.ItemID as int) DESC) AS Row,
INV_SET_Category.CategoryName AS ItemCategory,
INV_Item.Description As ItemName,
INV_Item.InventoryItemStatusCode as PartsNo,
Set_UnitOfMeasure.UOMName AS UOM, 
ISNULL(INV_Item.MinimumOrderQuantity,0) as MinQuantity
,INV_Item.MaximumOrderQuantity as MAxQuantity
,IsNull(HoldingStock.QtyInHand, 0) As QtyInHand
--,INV_Item.MinimumOrderQuantity - HoldingStock.QtyInHand as OrderQty
--,0 as ExcessQty
,case when INV_Item.MaximumOrderQuantity > HoldingStock.QtyInHand then INV_Item.MaximumOrderQuantity - HoldingStock.QtyInHand else 0 end as OrderQty
,case when INV_Item.MaximumOrderQuantity < HoldingStock.QtyInHand then HoldingStock.QtyInHand - INV_Item.MaximumOrderQuantity else 0  end as ExcessQty

--,HoldingStock.LocationID 
,SET_ItemCatalog.Description As ItemCatalog
From INV_Item 
	Inner Join INV_SET_Category ON 
--INV_Item.CompanyID = INV_SET_Category.CompanyID And 
INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
       -- And (INV_SET_Category.CategoryID = @CategoryID OR @CategoryID is null) 
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
	Inner Join Set_UnitOfMeasure ON INV_Item.PrimaryUOMCode = Set_UnitOfMeasure.UOMID 

	Inner Join SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
	And SET_ItemCatalog.Status =1

	Left Join 
	(
		Select ItemID,SUm(QtyInHand)QtyInHand,avg(AveragePrice)AveragePrice,Sum(GrossAmountHolding)GrossAmountHolding FROM 
		(
			Select 
			INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID, 
			 Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) AS QtyInHand, 
			 IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0) AS AveragePrice, 
			 (IsNull(Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End), 0) 
				 * IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0)) AS GrossAmountHolding
      
			from INV_StockTransactionDetail 
				Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
					And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
					And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And INV_StockTransactionHeader.Status = 1 
					And INV_StockTransactionHeader.IsActive = 1 

				Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
					And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
					AND Set_DocumentType.Status = 1 
				Where INV_StockTransactionDetail.PeriodID = 1 And INV_StockTransactionDetail.Status = 1 
				And INV_StockTransactionDetail.IsActive = 1 AND INV_StockTransactionHeader.StatusCode = 1
				{1}
			Group By INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID
		)FinTab
Group BY 
ItemID
    ) As HoldingStock On 
--HoldingStock.CompanyID = INV_Item.CompanyID And 
HoldingStock.ItemID = INV_Item.ItemID 
     Where INV_Item.Status = 1 --And INV_Item.CompanyID = @CompanyID 
       --and INV_Item.MinimumOrderQuantity > HoldingStock.QtyInHand
       and INV_Item.MaximumOrderQuantity <> 0
       --and  HoldingStock.QtyInHand <> 0
       and INV_Item.MinimumOrderQuantity <> 0
     {0}
--       UNION ALL
       
--       Select ROW_NUMBER() OVER (ORDER BY Cast(INV_Item.ItemID as int) DESC) AS Row,
--INV_SET_Category.CategoryName AS ItemCategory,
--INV_Item.Description As ItemName,
--INV_Item.InventoryItemStatusCode as PartsNo,
--Set_UnitOfMeasure.UOMName AS UOM
--,ISNULL(INV_Item.MinimumOrderQuantity,0) as MinQuantity
--,ISNULL(INV_Item.MaximumOrderQuantity,0) as MaxQuantity
--,IsNull(HoldingStock.QtyInHand, 0) As QtyInHand
----, 0 as OrderQty
----,HoldingStock.QtyInHand- INV_Item.MaximumOrderQuantity as ExcessQty
--,case when INV_Item.MaximumOrderQuantity > HoldingStock.QtyInHand then INV_Item.MaximumOrderQuantity - HoldingStock.QtyInHand else 0 end as OrderQty
--,case when INV_Item.MaximumOrderQuantity < HoldingStock.QtyInHand then HoldingStock.QtyInHand - INV_Item.MaximumOrderQuantity else 0  end as ExcessQty

--,HoldingStock.LocationID 
--,SET_ItemCatalog.Description As ItemCatalog
   
--From INV_Item 
--	Inner Join INV_SET_Category ON 
----INV_Item.CompanyID = INV_SET_Category.CompanyID And 
--INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
--       -- And (INV_SET_Category.CategoryID = @CategoryID OR @CategoryID is null) 
--        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
--	Inner Join Set_UnitOfMeasure ON INV_Item.PrimaryUOMCode = Set_UnitOfMeasure.UOMID 

--	Inner Join SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
--	And SET_ItemCatalog.Status =1

--	Left Join 
--	(
--		Select INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID, INV_StockTransactionHeader.LocationID  ,
--            Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) AS QtyInHand, 
--            IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0) AS AveragePrice, 
--            (IsNull(Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End), 0) 
--                * IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0)) AS GrossAmountHolding 
                
--		from INV_StockTransactionDetail 
--			Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
--                And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
--                And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And INV_StockTransactionHeader.Status = 1 
--                And INV_StockTransactionHeader.IsActive = 1 

--            Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
--                And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
--                AND Set_DocumentType.Status = 1 
--			Where INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 And INV_StockTransactionDetail.IsActive = 1 
--            {1}
         
--        Group By INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID,INV_StockTransactionHeader.LocationID 
--    ) As HoldingStock On 
----HoldingStock.CompanyID = INV_Item.CompanyID And 
--HoldingStock.ItemID = INV_Item.ItemID 
--     Where INV_Item.Status = 1 --And INV_Item.CompanyID = @CompanyID 
--        --and HoldingStock.QtyInHand > INV_Item.MaximumOrderQuantity
--        and INV_Item.MaximumOrderQuantity <> 0
--       and  HoldingStock.QtyInHand <>0
--       and INV_Item.MinimumOrderQuantity <> 0
--{0}
) as Tab
";

        const string Query_GetSafteyItemsStock = @"  Select distinct * from (
Select ROW_NUMBER() OVER (ORDER BY Cast(INV_Item.ItemID as int) DESC) AS Row,
INV_SET_Category.CategoryName AS ItemCategory,
INV_Item.Description As ItemName,
INV_Item.InventoryItemStatusCode as PartsNo,
Set_UnitOfMeasure.UOMName AS UOM, 
ISNULL(INV_Item.MinimumOrderQuantity,0) as MinQuantity
,INV_Item.MaximumOrderQuantity as MAxQuantity
,IsNull(HoldingStock.QtyInHand, 0) As QtyInHand
,case when INV_Item.MinimumOrderQuantity - HoldingStock.QtyInHand > 0 then INV_Item.MinimumOrderQuantity - HoldingStock.QtyInHand else 0 end as OrderQty
,case when INV_Item.MaximumOrderQuantity < HoldingStock.QtyInHand then HoldingStock.QtyInHand - INV_Item.MaximumOrderQuantity else 0  end as ExcessQty

,SET_ItemCatalog.Description As ItemCatalog
From INV_Item 
	Inner Join INV_SET_Category ON 
INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
	Inner Join Set_UnitOfMeasure ON INV_Item.PrimaryUOMCode = Set_UnitOfMeasure.UOMID 

	Inner Join SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
	And SET_ItemCatalog.Status =1

	Left Join 
	(
		Select ItemID,SUm(QtyInHand)QtyInHand,avg(AveragePrice)AveragePrice,Sum(GrossAmountHolding)GrossAmountHolding FROM 
		(
			Select 
			INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID, 
			 Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) AS QtyInHand, 
			 IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0) AS AveragePrice, 
			 (IsNull(Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End), 0) 
				 * IsNull(AVG(INV_StockTransactionDetail.TransactionCost), 0)) AS GrossAmountHolding
			from INV_StockTransactionDetail 
				Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
					And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
					And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And INV_StockTransactionHeader.Status = 1 
					And INV_StockTransactionHeader.IsActive = 1 
				Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
					And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
					AND Set_DocumentType.Status = 1 
				Where INV_StockTransactionDetail.PeriodID = 1 And INV_StockTransactionDetail.Status = 1 
				And INV_StockTransactionDetail.IsActive = 1 AND INV_StockTransactionHeader.StatusCode = 1
				{1}
			Group By INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.ItemID
		)FinTab
Group BY 
ItemID
    ) As HoldingStock On 
HoldingStock.ItemID = INV_Item.ItemID 
     Where INV_Item.Status = 1 
       and INV_Item.MaximumOrderQuantity <> 0
       and INV_Item.MinimumOrderQuantity <> 0
     {0}
) as Tab
";



        const string Query_ApprovalGroupWiseItemMaster = @" SELECT 
  
  
  INV_Item.ExpenseAccountCCID
, INV_Item.CostOfSalesAccountCCID
, INV_Item.SalesAccountCCID
, INV_Item.CodeCombinationID
, INV_Item.InventoryAccountCCID
, INV_Item.TAXCCID
, INV_Item.PurchasingTaxCCID
--------------------
, INV_Item.Description
, SET_UnitofMeasure.UOMName
, INV_Item.EncumbranceAccountCCID 
, INV_Item.QtyRcvExceptionCode
, INV_Item.MarketPrice
, INV_Item.InventoryItemStatusCode
, INV_Item.ItemCatalogGroupID
, INV_SET_Category.CategoryID
, SET_ItemCatalog.Description AS CatalogName
, INV_SET_Category.CategoryName AS CategoryName 
,SET_ApprovalItemGroup.GroupName
,INV_Item.ApprovalItemGroupID
FROM INV_Item 
	Left JOIN INV_SET_Category ON 
			--INV_SET_Category.CompanyID = INV_Item.CompanyID AND 
INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
	LEFT JOIN SET_ItemCatalog ON SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID 
			and SET_ItemCatalog.Status = 1
Left JOIN   SET_ApprovalItemGroup ON SET_ApprovalItemGroup.ApprovalGroupID =INV_Item.ApprovalItemGroupID
 AND SET_ApprovalItemGroup.Status = 1 ANd SET_ApprovalItemGroup.IsActive = 1
	inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode
WHERE 
--INV_Item.[CompanyID] = @CompanyID and 
INV_Item.Status = 1 
and SET_UnitofMeasure.Status = 1

";
        const string Query_SemiFinsishedData = @"Select INV_Item.ItemID,InventoryItemStatusCode,INV_Item.Description as ItemDescription, INV_SET_Category.CategoryName
,SET_ItemCatalog.Description as ItemCatalog, SET_SourceType.Description as Sourcetype,Origin,SET_Country.CountryName
,SET_UnitofMeasure.UOMName,INV_ItemDetails.Composition,INV_ItemDetails.KnittingMethod,INV_ItemDetails.Embellishment
,INV_ItemDetails.GoodsType,INV_ItemDetails.PackType,INV_ItemDetails.GSM
 from INV_Item

INNER JOIN INV_ItemDetails ON INV_ItemDetails.ItemID=INV_Item.ItemID
LEft JOIN INV_SET_Category On INV_SET_Category.CategoryID=INV_Item.ItemTypeID
LEft JOIN SET_ItemCatalog On SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
LEft JOIN SET_SourceType On SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
LEft JOIN SET_Country On SET_Country.CountryID=INV_ItemDetails.Origin
LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode 
Where INV_ItemDetails.Status=1 AND INV_Item.Status=1
AND INV_ItemDetails.ProductType=1
--AND INV_Item.CompanyID=@CompanyID";

        const string Query_GetAllProduct_WithPaging = @"SELECT  [INV_Item].[CompanyID],[INV_Item].[ItemID], INV_Item.[Description], [InvoicingRuleID], [INV_Item].[ChartofAccountID], [CodeCombinationID], [ExpenseAccountCCID], [EncumbranceAccountCCID]
, [CostOfSalesAccountCCID], [SalesAccountCCID], [FEDTaxCCID], [ExciseDutyCCID], [SalesDiscountCCID], [PurchaseDiscountCCID], [ItemCatalogGroupID], [DefaultSupplierID]
, [QtyRcvExceptionCode], [MarketPrice], [HazardClassID], [QtyRcvTolerancePercent], [ListPricePerUnit], [UNNumberID], [PriceTolerancePercent], 
[AssetCategoryID], [RoundingFactor], [UnitOfIssue], [AutoLotAlphaPrefix], [StartAutoLotNumber], [LotControlCode], [ShelfLifeCode], [ShelfLifeDays], 
[SerialNumberControlCode], [StartAutoSerialNumber], [AutoSerialAlphaPrefix],INV_Item.[SourceTypeID], [UnitWeight], [WeightUOMCode], [VolumeUOMCode],
[UnitVolume], [ShrinkageRate], [LeadTimeLotSize], [StdLotSize], [PrimaryUOMCode], [PrimaryUnitOfMeasure], [AllowedUnitsLookupCode], [InventoryItemStatusCode]
, [FixedLotMultipler], [RoundingControlTypeID], [MinimumQuantity], [MaximumQuantity], [MinimumOrderQuantity], [MaximumOrderQuantity], [FixedOrderQuantity],
[FixedDaysSupply], [WarrantyVendorID], [MaximunWarrantyAmount], [ResponseTimePeriodCode], [TAXCCID], [ItemTypeID], [PurchasingTaxCCID], [EquipmentTypeID],
[WebStatus], [PurchasingItemFlag], [ShippableItemFlag], [CustomerOrderFlag], [InternalOrderFlag], [ServiceItemFlag], [InventoryItemFlag], [EngItemFlag], 
[InventoryAssetFlag], [PurchasingEnabledFlag], [CustomerOrderEnabledFlag], [InternalOrderEnabledFlag], [SOTransactionsFlag], [MTLTransactionEnabledFlag], 
[StockEnabledFlag], [CatalogStatusFlag], [ReturnableFlag], [CollateralFlag], [TaxableFlag], [AllowItemDescUpdateFlag], [InspectionRequiredFlag], [ReceiptRequiredFlag],
[RFQRequiredFlag], [AllowSubstituteReceiptsFlag], [AllowUnorderedReceiptsFlag], [AllowExpressDeliveryFlag], [PreventiveMaintenanceFlag], [InvoiceableItemFlag],
[MustUseApprovedVendorFlag], [CheckShortagesFlag], [ElectronicFlag], [DownloadableFlag], [VolDiscountExemptFlag], [CouponExemptFlag], [CommsActivationReqdFlag], 
[OrderableOnWebFlag], [IndivisibleFlag], [EnabledFlag], [SummaryFlag], [INV_Item].[Status], [INV_Item].[CreatedBy],[INV_Item].[CreateDate], [INV_Item].[UpdatedBy], [INV_Item].[UpdateDate], [INV_Item].[CreatedByValue],
 [INV_Item].[UpdatedByValue], 
[IsRawMaterial], [ExpenseAllowed], [ApprovalItemGroupID], [FrontAxleWeight], [FrontAxleWeightUOMCode], [RearAxleWeightI], [RearAxleWeightIUOMCode], [INV_Item].[RearAxleWeightII], 
[RearAxleWeightIIUOMCode], [TrailerWeight], [TrailerWeightUOMCode], [FrontWheels], [RearWheels], [OtherWheels], [InventoryAccountCCID], [CubicCentimeters], [INV_Item].[GSM],
[Length], [Width], [AdditionalDeductionTaxFlag], [ShelfReference], [SalesTaxExpenseCCID], [FEDTaxExpenseCCID], [SEDTaxExpenseCCID], [Color], [AccumulatedDepreciationCCID], 
[OtherExpenseAccountCCID], [DyeFlag], [ChemicalFlag], [AdjustmentStockAccountCCID], [ProfitlossAccountCCID],[IsOrderable], [NonRecipeItem], [SubstituteCode], [BrandID], [INV_Item].[Composition], [INV_Item].[KnittingMethod],
[AR_SET_Customer].[CustomerName],[INV_SET_Category].[CategoryName],
[INV_Item].[PackType],[INV_Item].[PackingStyle], [INV_Item].[Embellishment], [INV_Item].[GoodsType], [INV_Item].[ProductStyle], [INV_Item].[Obsolete]
,INV_ItemDetails.ProductType
,CASE WHEN INV_ItemDetails.ProductType=1 then 'SemiFinished'  WHEN INV_ItemDetails.ProductType=2 then 'Finished' else null END ProductTypeName
,SET_SourceType.Description as SourceType
, ROW_NUMBER() Over (Order by [INV_Item].CompanyID, [INV_Item].ItemID desc) as 'Row' 
FROM [INV_Item] 
INNER JOIN INV_ItemDetails ON INV_ItemDetails.ItemID=[INV_Item].ItemID AND INV_ItemDetails.Status=1
LEFT JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
LEFT JOIN [AR_SET_Customer] ON [INV_Item].[DefaultSupplierID]=[AR_SET_Customer].[CustomerID]
LEFT JOIN [INV_SET_Category] ON [INV_Item].[ItemTypeID]=[INV_SET_Category].[CategoryID]";

        const string Query_GetAllProduct_WithOutPaging = @"SELECT  [INV_Item].[CompanyID],[INV_Item].[ItemID], INV_Item.[Description], [InvoicingRuleID], [INV_Item].[ChartofAccountID], [CodeCombinationID], [ExpenseAccountCCID], [EncumbranceAccountCCID]
, [CostOfSalesAccountCCID], [SalesAccountCCID], [FEDTaxCCID], [ExciseDutyCCID], [SalesDiscountCCID], [PurchaseDiscountCCID], [ItemCatalogGroupID], [DefaultSupplierID]
, [QtyRcvExceptionCode], [MarketPrice], [HazardClassID], [QtyRcvTolerancePercent], [ListPricePerUnit], [UNNumberID], [PriceTolerancePercent], 
[AssetCategoryID], [RoundingFactor], [UnitOfIssue], [AutoLotAlphaPrefix], [StartAutoLotNumber], [LotControlCode], [ShelfLifeCode], [ShelfLifeDays], 
[SerialNumberControlCode], [StartAutoSerialNumber], [AutoSerialAlphaPrefix], INV_Item.[SourceTypeID], [UnitWeight], [WeightUOMCode], [VolumeUOMCode],
[UnitVolume], [ShrinkageRate], [LeadTimeLotSize], [StdLotSize], [PrimaryUOMCode], [PrimaryUnitOfMeasure], [AllowedUnitsLookupCode], [InventoryItemStatusCode]
, [FixedLotMultipler], [RoundingControlTypeID], [MinimumQuantity], [MaximumQuantity], [MinimumOrderQuantity], [MaximumOrderQuantity], [FixedOrderQuantity],
[FixedDaysSupply], [WarrantyVendorID], [MaximunWarrantyAmount], [ResponseTimePeriodCode], [TAXCCID], [ItemTypeID], [PurchasingTaxCCID], [EquipmentTypeID],
[WebStatus], [PurchasingItemFlag], [ShippableItemFlag], [CustomerOrderFlag], [InternalOrderFlag], [ServiceItemFlag], [InventoryItemFlag], [EngItemFlag], 
[InventoryAssetFlag], [PurchasingEnabledFlag], [CustomerOrderEnabledFlag], [InternalOrderEnabledFlag], [SOTransactionsFlag], [MTLTransactionEnabledFlag], 
[StockEnabledFlag], [CatalogStatusFlag], [ReturnableFlag], [CollateralFlag], [TaxableFlag], [AllowItemDescUpdateFlag], [InspectionRequiredFlag], [ReceiptRequiredFlag],
[RFQRequiredFlag], [AllowSubstituteReceiptsFlag], [AllowUnorderedReceiptsFlag], [AllowExpressDeliveryFlag], [PreventiveMaintenanceFlag], [InvoiceableItemFlag],
[MustUseApprovedVendorFlag], [CheckShortagesFlag], [ElectronicFlag], [DownloadableFlag], [VolDiscountExemptFlag], [CouponExemptFlag], [CommsActivationReqdFlag], 
[OrderableOnWebFlag], [IndivisibleFlag], [EnabledFlag], [SummaryFlag], [INV_Item].[Status], [INV_Item].[CreatedBy],[INV_Item].[CreateDate], [INV_Item].[UpdatedBy], [INV_Item].[UpdateDate], [INV_Item].[CreatedByValue],
[AR_SET_Customer].[CustomerName],[INV_SET_Category].[CategoryName],
 [INV_Item].[UpdatedByValue], 
[IsRawMaterial], [ExpenseAllowed], [ApprovalItemGroupID], [FrontAxleWeight], [FrontAxleWeightUOMCode], [RearAxleWeightI], [RearAxleWeightIUOMCode], [INV_Item].[RearAxleWeightII], 
[RearAxleWeightIIUOMCode], [TrailerWeight], [TrailerWeightUOMCode], [FrontWheels], [RearWheels], [OtherWheels], [InventoryAccountCCID], [CubicCentimeters], [INV_Item].[GSM],
[Length], [Width], [AdditionalDeductionTaxFlag], [ShelfReference], [SalesTaxExpenseCCID], [FEDTaxExpenseCCID], [SEDTaxExpenseCCID], [Color], [AccumulatedDepreciationCCID], 
[OtherExpenseAccountCCID], [DyeFlag], [ChemicalFlag], [AdjustmentStockAccountCCID], [ProfitlossAccountCCID],[IsOrderable], [NonRecipeItem], [SubstituteCode], [BrandID], [INV_Item].[Composition], [INV_Item].[KnittingMethod], 
[INV_Item].[PackType],[INV_Item].[PackingStyle], [INV_Item].[Embellishment], [INV_Item].[GoodsType], [INV_Item].[ProductStyle] ,[INV_Item].[Obsolete]
,INV_ItemDetails.ProductType
,CASE WHEN INV_ItemDetails.ProductType=1 then 'SemiFinished'  WHEN INV_ItemDetails.ProductType=2 then 'Finished' else null END ProductTypeName
,SET_SourceType.Description as SourceType
FROM [INV_Item] 
INNER JOIN INV_ItemDetails ON INV_ItemDetails.ItemID=[INV_Item].ItemID AND INV_ItemDetails.Status=1
LEFT JOIN SET_SourceType ON SET_SourceType.SourceTypeID=INV_Item.SourceTypeID 
LEFT JOIN [AR_SET_Customer] ON [INV_Item].[DefaultSupplierID]=[AR_SET_Customer].[CustomerID] 
LEFT JOIN [INV_SET_Category] ON [INV_Item].[ItemTypeID]=[INV_SET_Category].[CategoryID]";



        const string Query_Get_MaxIdWithOutCompanyId = @"SELECT IsNull(max(ItemID), 0) + 1 as ItemID FROM INV_Item";

        const string Query_GetData_byKey_UOM = @" SELECT UOMID,UOMName FROM [INV_Item]
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_Item.PrimaryUnitOfMeasure
WHERE [ItemID] = @ItemID ";
        
        #endregion

        #region Class Methods

        public static DataTable GetItemMasterData(Nullable<int> companyID,Nullable<int> catalogID, Nullable<int> categoryID, Nullable<int> itemID,bool sortbyInventorycode, bool sortbyDescription
            , Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);


                if (fromDate != null && toDate != null)
                {
                    sb.Append(" and convert(date,INV_Item.CreateDate) between @FromDate AND @ToDate ");
                }
                else if (fromDate != null)
                {
                    sb.Append(" and convert(date,INV_Item.CreateDate) = @FromDate ");
                }
                else if (fromDate == null)
                {
                    sb.Append(" and convert(date,INV_Item.CreateDate) = @ToDate");
                }

                if (catalogID.HasValue)
                    whereClause.Append(" AND SET_ItemCatalog.ItemCatalogID = " + catalogID.ToString());

                if (categoryID.HasValue)
                    whereClause.Append(" AND INV_SET_Category.CategoryID = " + categoryID.ToString());

                if (itemID.HasValue)
                    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());

                if (sortbyDescription == true)
                {
                    sb.Append(" order by Description ");
                }
                else
                {
                    sb.Append(" order by INV_Item.InventoryItemStatusCode asc");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, ItemMaster_CC_Query + whereClause.ToString() + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetItemOldAndPoPrice(int companyId, int PeriodID, int ItemID, string DateTo, string DateFrom)
        {
            DataTable dt = null;

            try
            {

                try
                {

                    string year = DateFrom.Substring(0, 4);
                    string month = DateFrom.Substring(5, 2);
                    string date = DateFrom.Substring(8, 2);

                    if (date == "29" && month == "02" && !DateTime.IsLeapYear(int.Parse(year)))
                        DateFrom = year + "-" + month + "-" + 28;

                }
                catch { }

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@dateto", DateTo);
                sqlparams[4] = new SqlParameter("@datefrom", DateFrom);

                

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ItemOldAndPoPrice, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }

            return dt;
        }

        public static DataTable GetItemsWithCategoryByCategoryName(int companyId, string categoryName, string filter = "", SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@CategoryName", categoryName);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ItemWithCategoryByCategoryName + filter, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ItemWithCategoryByCategoryName + filter, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }

            return dt;
        }

        public static DataTable GetTop5Customer(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Top5Customer, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataRow GetItemIDbyName(string ItemName, string CategoryName)
        {
            DataRow dr = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ItemName", ItemName);
                sqlparams[1] = new SqlParameter("@CategoryName", CategoryName);
                string Query = @"select distinct INV_Item.ItemID,INV_Item.PrimaryUOMCode as UOMID from INV_Item 
left join INV_SET_Category on INV_SET_Category.CategoryID=INV_Item.ItemTypeID
where INV_Item.Description like @ItemName and INV_SET_Category.CategoryName like @CategoryName and INV_Item.Status=1";

              

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

                if (ds != null && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }


        public static DataTable Get_ItemByCategoryAssetFlagValidation(Nullable<int> companyID, Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (isActive.HasValue)
                    sqlparams[2] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[2] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectItemByCategory, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveData_InvoiceableItemFlagTrue(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_InvoiceableItemFlagTrue, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveData_InventoryAssetFlagTrue(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_InventoryAssetFlagTrue, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable getCaneItem(int companyID, string strWhere = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Cane_Items, strWhere), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_CatalogCategory(int companyID, int categoryID, int catalogid, string where = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (categoryID != -1)
                {
                    sb1.Append("AND  INV_SET_Category.CategoryID =" + categoryID);
                }

                if (catalogid !=-1)
                {
                    sb2.Append("AND INV_Item.ItemCatalogGroupID =" + catalogid);
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_item_By_CatalogCategory + where, sb1.ToString(), sb2.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_Catalog(int companyID, int catalogid)
        {
            DataTable dtToReturn = null;
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                
                if (catalogid != -1)
                {
                    sb2.Append("AND INV_Item.ItemCatalogGroupID =" + catalogid);
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_item_By_Catalog, sb1.ToString(), sb2.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_Category( int companyID, int categoryID,string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_Category + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_CatalogID(int companyID, int catalogID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@catalogID", catalogID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_CatalogID + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_Category_Top1(int companyID, int categoryID, string where = "", string orderby ="")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_Category_Top1 + where +orderby, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_Catalog_Top1(int companyID, int CatalogID, string where = "", string orderby = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@CatalogID", CatalogID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_Catalog_Top1 + where + orderby, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        
        public static DataTable getItem_By_AssetTypeID(int companyID, int categoryID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_FixedAsset + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_CategorySPLSupplier(int companyID, Nullable<int> categoryID, int SupplierID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);
                sqlparams[2] = new SqlParameter("@SupplierID", SupplierID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_CategorySPLSupplier, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_Category(int companyID, Nullable<int> categoryID, string ParameterFlagColumnName)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@categoryID", categoryID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_item_By_CategoryWithParameter, ParameterFlagColumnName), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetBelowMinimumLevelStockData(int companyID, int periodID, int CostCenterID, int DepartmentID, int LocationID,Nullable<int> itemCategoryID = null,
            Nullable<int> ItemID = null, Nullable<DateTime> FromDate = null,
            Nullable<DateTime> ToDate = null, short? documentGroupID=null, int? ApprovingGroupID =null)
        {

            StringBuilder Sb = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                //   sqlparams[4] = new SqlParameter("@Type", type);
                if (itemCategoryID.HasValue)
                    sqlparams[5] = new SqlParameter("@CategoryID", itemCategoryID);


                if (ItemID.HasValue)
                    sqlparams[6] = new SqlParameter("@ItemID", ItemID);
                else
                    sqlparams[6] = new SqlParameter("@ItemID", DBNull.Value);

                if (LocationID > 0)
                    sqlparams[7] = new SqlParameter("@LocationID", LocationID);
                else
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);

                if (itemCategoryID > 0)
                    Sb.Append(" AND INV_SET_Category.CategoryID = @CategoryID");

                if (ItemID > 0)
                    Sb.Append(" AND INV_StockTransactionDetail.ItemID = @ItemID");


                if (LocationID > 0)
                    Sb.Append(" AND INV_StockTransactionHeader.LocationID =@LocationID");

                if (CostCenterID > 0)
                    Sb.Append(" AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID);

                if (DepartmentID > 0)
                    Sb.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    Sb.Append(" AND INV_Item.ApprovalItemGroupID  =" + ApprovingGroupID);
              
                string additionalFilter = "";
               
                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
              



                string filter = !string.IsNullOrEmpty(Sb.ToString()) ? Sb.ToString() : string.Empty;
                string query = Query_BelowMinimumLevelStock.Replace("{AdditionalParameters}", filter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, additionalFilter), sqlparams);

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

        public static DataTable GetDashBoard_MaxQuantity(int companyID, int periodID, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, SqlConnection sqlcon = null)
        {
            DataTable dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();

                OrderBy = "";

                sb.Append(" INV_StockTransactionDetail.CompanyID = " + companyID + " and INV_StockTransactionDetail.PeriodID =  " + periodID + " and INV_StockTransactionDetail.Status <> 1");


                List<SqlParameter> sqlparamsList = new List<SqlParameter>();

                sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoardMaxQuantity) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_DashBoardMaxQuantity) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoardMaxQuantity) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_DashBoardMaxQuantity) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetDashBoard_MinimumQuantity(int companyID, int periodID, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, SqlConnection sqlcon = null)
        {
            DataTable dt = null;

            try
            {
                OrderBy = "";
                
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_DashBoardMinimumQuantity + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_DashBoardMinimumQuantity + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, string.Format(Paging, Query_DashBoardMinimumQuantity + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_DashBoardMinimumQuantity + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
    
                ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDashBoard_OrderQuantity(int companyID, int periodID, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, SqlConnection sqlcon = null)
        {
            DataTable dt = null;
            try
            {
                StringBuilder sb = new StringBuilder();

                OrderBy = "";

                sb.Append(" INV_StockTransactionDetail.CompanyID = " + companyID + " and INV_StockTransactionDetail.PeriodID =  " + periodID + " and INV_StockTransactionDetail.Status <> 1");


                List<SqlParameter> sqlparamsList = new List<SqlParameter>();

                sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoardOrder) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_DashBoardOrder) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoardOrder) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_DashBoardOrder) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dt;
        }

        public static DataTable GetMaximumLevelStockData(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string itemID = "", int itemCategory = -1,
            int documentGroupID=-1, int LocationID =-1 ,int  ApprovingGroup =0)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", FromDate));
                sqlParams.Add(new SqlParameter("@ToDate", ToDate));

                additionalQueryParameters.Append(" and LastTransactionInfo.StockDate Between @FromDate And @ToDate");

                if (itemCategory != -1)
                {
                    additionalQueryParameters.Append(" AND [INV_Item].[ItemTypeID] = @ItemCategory");
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategory));
                }

                if (!string.IsNullOrEmpty(itemID))
                {
                    additionalQueryParameters.Append(" AND [INV_Item].[ItemID] =" + itemID); 
                }

                if (documentGroupID != -1)
                    additionalQueryParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.ToString() + " ");
              
                if(LocationID != -1)
                    additionalQueryParameters.Append(" and INV_StockTransactionHeader.LocationID =" + LocationID  );

                if (ApprovingGroup > 0)
                    additionalQueryParameters.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                string filter = !string.IsNullOrEmpty(additionalQueryParameters.ToString()) ? additionalQueryParameters.ToString() : string.Empty;
                string query = Query_MaximumLevelStock.Replace("{AdditionalParameters}", filter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlParams.ToArray());

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

        public static bool IsItemInUsed(int companyID, int itemID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckItemInUsed, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0] != null && ds.Tables[0].Rows.Count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetAllItemDataWithRawMaterial(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Item_withRawMaterial, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveData(Nullable<int> companyID, string strWhere)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (strWhere != null && strWhere != "")
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForDDL + strWhere, sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForDDL, sqlparams).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
       

        public static DataTable GetItemData(int companyID, int itemID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemData, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetCategoryByItemCatalog(int companyID, int itemCatalogID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemCatalogID", itemCatalogID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCategoryByItemCatalog, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetInventoryStatusCodeByItemCatalog(int companyID, int itemCatalogID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemCatalogID", itemCatalogID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInventoryItemStatusCodeByItemCatalog, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllActiveDataSPLSupplier(Nullable<int> companyID,int SupplierID, string strWhere)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SupplierID", SupplierID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllForDDLSPL + strWhere, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetApproveGroupItem()
        {
            DataTable dt = null;

            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@Status", status);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_ApproveGroupName).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllActiveData_AllowItemDescUpdate(int companyID, Nullable<int> itemID, out bool ItemDescStatus)
        {
            DataTable dtToReturn = null;            
            ItemDescStatus = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllItemByItemID, sqlparams).Tables[0];

                ItemDescStatus = Convert.ToBoolean(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetStatus_ItemDescriptionUpdate, sqlparams));
            }
            catch (Exception ex) 
            { 
                SqlHelper.LogException(ex); 
            }

            return dtToReturn;
        }

        public static DataTable GetAllActiveData_AllowItemDescUpdateForConnect(int companyID, Nullable<int> itemID, out bool ItemDescStatus)
        {
            DataTable dtToReturn = null;
            ItemDescStatus = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (itemID.HasValue)
                    sqlparams[1] = new SqlParameter("@ItemID", itemID.Value);
                else
                    sqlparams[1] = new SqlParameter("@ItemID", DBNull.Value);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllItemByItemID, sqlparams).Tables[0];

                ItemDescStatus = Convert.ToBoolean(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetStatus_ItemDescriptionUpdate, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return dtToReturn;
        }

        public static DataTable GetItemDesctiptionList(string description)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Description", description);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDescriptionList, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateListUnitPrice(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@ItemID", f_ItemID);

                sqlparams[2] = new SqlParameter("@ListPricePerUnit", f_ListPricePerUnit);

                if (f_UpdatedBy.HasValue)
                    sqlparams[3] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[3] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[4] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[4] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[5] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedByValue", DBNull.Value);



                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateListPriceUnit_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateListPriceUnit_By_PrimaryKey, sqlparams) > 0)
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

        public static DataTable getItem_By_Category(int companyID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_invoiceableItem + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable GetOrderableItem(int companyID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_OrderableItemsOnly + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItem_By_CategoryAuto(int companyID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_item_By_SecondarySales + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }


        public static DataTable getMyItem_By_Category(int companyID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Myitem_By_invoiceableItem + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }


        public static DataTable GetItems_By_User(int companyID, Guid userID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@UserID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Items_By_User, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }

        public static DataTable getItemDefectedParts(int companyID, string where = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                //sqlparams[1] = new SqlParameter("@categoryID", categoryID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDefecteditems + where, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex) { SqlHelper.LogException(ex); }

            return dtToReturn;
        }
        public static DataRow GetDataByKeyTransaction(int companyID, int itemID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;
                const string Query_GetData = @" SELECT * FROM [INV_Item] WHERE [ItemID] = @ItemID ";
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData, sqlparams);

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



        public static DataTable GetItemListPriceData(Nullable<int> companyID, Nullable<int> catalogID, Nullable<int> categoryID, Nullable<int> itemID, bool sortbyInventorycode, bool sortbyDescription, int ApprovingGroup)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (catalogID.HasValue)
                    whereClause.Append(" AND SET_ItemCatalog.ItemCatalogID = " + catalogID.ToString());

                if (categoryID.HasValue)
                    whereClause.Append(" AND INV_SET_Category.CategoryID = " + categoryID.ToString());

                if (itemID.HasValue)
                    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());

                if (ApprovingGroup > 0)

                    whereClause.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());


                if (sortbyDescription == true)
                {
                    sb.Append(" order by Description ");
                }
                if (sortbyInventorycode)
                {
                    sb.Append(" order by InventoryItemStatusCode ");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, ItemListPrice_CC_Query + whereClause.ToString() + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable SaftyItemsStocksReport(int CompanyID, int PeriodID, int? CategoryID, DateTime? fromDate, DateTime? toDate, int? ItemID, int? LocationID , int ApprovingGroup =0)
        {
            DataTable dt = null;

            try
            {
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder additionalParameters1 = new StringBuilder();
                SqlParameter[] Sqlparams = new SqlParameter[4];
                Sqlparams[0] = new SqlParameter("@ComPanyID", CompanyID);
                Sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                Sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                Sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                if (CategoryID.HasValue)
                {
                    additionalParameters.Append(" AND INV_SET_Category.CategoryID= " + CategoryID + "");
                }
                if (ItemID.HasValue)
                {
                    additionalParameters.Append(" AND INV_Item.ItemID=" + ItemID + "");
                }
                if (LocationID.HasValue)
                {
                    //additionalParameters.Append(" AND HoldingStock.LocationID= " + LocationID + "");
                    additionalParameters1.Append(" AND (INV_StockTransactionHeader.LocationID = " + LocationID + " OR INV_StockTransactionDetail.LocationID = " + LocationID + ")");
                }
                #region Filter by Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalParameters1.Append(" And INV_StockTransactionHeader.DocumentDate <= @FromDate --AND @ToDate ");
                }
               

                #endregion
               
                if (ApprovingGroup > 0)
                    additionalParameters1.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());


                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetSaftyItemsStock, additionalParameters, additionalParameters1), Sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable SafteyItemsStocksReport(int CompanyID, int PeriodID, int? CategoryID, DateTime? fromDate, DateTime? toDate, int? ItemID, int? LocationID, int ApprovingGroup = 0)
        {
            DataTable dt = null;

            try
            {
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder additionalParameters1 = new StringBuilder();
                SqlParameter[] Sqlparams = new SqlParameter[4];
                Sqlparams[0] = new SqlParameter("@ComPanyID", CompanyID);
                Sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                Sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                Sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                if (CategoryID.HasValue)
                {
                    additionalParameters.Append(" AND INV_SET_Category.CategoryID= " + CategoryID + "");
                }
                if (ItemID.HasValue)
                {
                    additionalParameters.Append(" AND INV_Item.ItemID=" + ItemID + "");
                }
                if (LocationID.HasValue)
                {
                    //additionalParameters.Append(" AND HoldingStock.LocationID= " + LocationID + "");
                    additionalParameters1.Append(" AND (INV_StockTransactionHeader.LocationID = " + LocationID + " OR INV_StockTransactionDetail.LocationID = " + LocationID + ")");
                }
                #region Filter by Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalParameters1.Append(" And INV_StockTransactionHeader.DocumentDate <= @FromDate --AND @ToDate ");
                }


                #endregion

                if (ApprovingGroup > 0)
                    additionalParameters1.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());


                DataSet ds = null;
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetSafteyItemsStock, additionalParameters, additionalParameters1), Sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetApprovalGroupWiseItemMasterData(Nullable<int> companyID, Nullable<int> ApprovalGroupID, Nullable<int> itemID, bool sortbyInventorycode, bool sortbyDescription)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (ApprovalGroupID.HasValue)
                    whereClause.Append(" AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.ToString());

                if (itemID.HasValue)
                    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());

                if (sortbyDescription == true)
                {
                    sb.Append(" order by Description ");
                }
                else
                {
                    sb.Append(" order by INV_Item.InventoryItemStatusCode asc");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ApprovalGroupWiseItemMaster + whereClause.ToString() + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataRow GetitemByInventoryItemStatusCode(int companyID, string InventoryItemStatusCode)
        {
            DataRow dr = null;
            DataSet ds = null;

            const string Query_GetInventoryItemStatusCodeBy = @"
select * from inv_item where InventoryItemStatusCode = @InventoryItemStatusCode and Status = 1 and CompanyID = @CompanyID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InventoryItemStatusCode", InventoryItemStatusCode);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInventoryItemStatusCodeBy, sqlparams);

                if (ds != null && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }
        public bool UpdateBrandIDByKey(int CompanyID, int ItemID, int BrandID, SqlTransaction objTransaction = null)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ItemID", ItemID);
                sqlparams[2] = new SqlParameter("@BrandID", BrandID);
                #region
                string Query_UpdateBrandID_By_PrimaryKey = @"UPDATE INV_Item SET BrandID=@BrandID WHERE  CompanyID=@CompanyID and ItemID=@ItemID ";
                #endregion
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateBrandID_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateBrandID_By_PrimaryKey, sqlparams) > 0)
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
        public static DataTable GetDistinctLocationID(SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {

                string query = "select distinct LocationID from inv_stocktransactionHeader where status=1";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query);

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

        public static DataTable GetItemDetailsList(string description,string columnname)
        {
            DataTable dt = null;

            string Query_GetDetailsList = @" Select distinct " + columnname + " as Description from INV_ITemdetails where " + columnname + " like '%" + description + "%' ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Description", description);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDetailsList, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllItemsForAlImdad(Nullable<int> companyID,string where = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            

            const string Query_GetAllItems = @" Select Description,INV_Item.ItemID from INV_Item 
inner join INV_ItemDetails on INV_ItemDetails.ItemID = INV_Item.ItemID

where INV_Item.Status = 1
and INV_ItemDetails.Status = 1
--and INV_Item.CompanyID = @CompanyID    


 ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllItems  + where, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllData_SemiFinishedData(int CompanyID, int PeriodID, int? Catagoery = null, int? CatalogID=null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", PeriodID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);


                //if (catalogID.HasValue)
                //    whereClause.Append(" AND SET_ItemCatalog.ItemCatalogID = " + catalogID.ToString());

                if (Catagoery.HasValue)
                    whereClause.Append(" AND INV_Item.ItemTypeID = " + Catagoery.ToString());

                if (CatalogID.HasValue)
                    whereClause.Append(" AND INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());
                //if (itemID.HasValue)
                //    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SemiFinsishedData + whereClause.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllData_FinishedProductData(int CompanyID, int PeriodID, int? Catagoery = null, int? CatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", PeriodID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                #region Queries
                const string Query_FinishedProductData = @"Select INV_Item.ItemID,InventoryItemStatusCode,INV_Item.Description as ItemDescription, INV_SET_Category.CategoryName
,SET_ItemCatalog.Description as ItemCatalog, SET_SourceType.Description as Sourcetype,Origin,SET_Country.CountryName
,SET_UnitofMeasure.UOMName,INV_ItemDetails.Composition,INV_ItemDetails.KnittingMethod,INV_ItemDetails.Embellishment,INV_ItemDetails.Layer
,INV_ItemDetails.GoodsType,INV_ItemDetails.PackType,INV_ItemDetails.GSM,INV_ItemDetails.PackingStyle,INV_Item.UnitWeight as HeaderUnitWeight,PiecesinCarton
,Inv_ItemSubDetail.SubDetailID
,Fabrics_SET_SubProduct.SubProductName
,Inv_ItemSubDetail.UnitWeight
,SET_Width.Width
,SET_Length.Length
,SUBProuductUOM.UOMName as SubUOM
,SET_SubWidth.Width as SubWidth
,SET_SubLength.Length as SubLength
,SET_SubEmbellishment.Embellishment as SubEmbellishment
,SET_SubComposition.CompositionName as SubCompositionName
,SET_SubLayer.LayerName as SubLayerName
,Fabrics_SET_Style.StyleName as SubStyleName
,SET_Color.ColorName
,SET_KnittingMethod.KnittingMethod as SubKnittingMethod
,ROW_NUMBER() OVER(PARTITION BY INV_ItemDetails.ItemID ORDER BY Inv_ItemSubDetail.SubDetailID) as SerialNo
 from INV_Item

INNER JOIN INV_ItemDetails ON INV_ItemDetails.ItemID=INV_Item.ItemID
AND INV_ItemDetails.Status=1
LEFT JOIN Inv_ItemSubDetail On Inv_ItemSubDetail.ItemID=INV_ItemDetails.ItemID
AND Inv_ItemSubDetail.CompanyID=INV_ItemDetails.CompanyID
AND Inv_ItemSubDetail.Status=1
LEft JOIN INV_SET_Category On INV_SET_Category.CategoryID=INV_Item.ItemTypeID
LEft JOIN SET_ItemCatalog On SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
LEft JOIN SET_SourceType On SET_SourceType.SourceTypeID=INV_Item.SourceTypeID
LEft JOIN SET_Country On SET_Country.CountryID=INV_ItemDetails.Origin
LEFT JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_Item.PrimaryUOMCode 
LEFT JOIN SET_Length   ON SET_Length.LengthID = INV_Item.Length 
LEFT JOIN SET_Width   ON SET_Width.WidthID = INV_Item.Width 
--------------Sub Product------
LEFT JOIN Fabrics_SET_SubProduct ON Fabrics_SET_SubProduct.SubProductID = Inv_ItemSubDetail.SubProductID 
LEFT JOIN SET_UnitofMeasure  SUBProuductUOM ON SUBProuductUOM.UOMID = Inv_ItemSubDetail.PrimaryUOMCode 
LEFT JOIN SET_Length SET_SubLength  ON SET_SubLength.LengthID = Inv_ItemSubDetail.Length 
LEFT JOIN SET_Width SET_SubWidth  ON SET_SubWidth.WidthID = Inv_ItemSubDetail.Width 
LEFT JOIN SET_Embellishment  SET_SubEmbellishment  ON SET_SubEmbellishment.EmbellishmentID = Inv_ItemSubDetail.Embellishment 
LEFT JOIN SET_Composition SET_SubComposition   ON SET_SubComposition.CompositionID = Inv_ItemSubDetail.Composition 
LEFT JOIN SET_Layer SET_SubLayer   ON SET_SubLayer.LayerID = Inv_ItemSubDetail.Layer 
LEFT JOIN Fabrics_SET_Style   ON Fabrics_SET_Style.StyleID = Inv_ItemSubDetail.StyleID 
LEFT JOIN SET_Color ON SET_Color.ColorID=Inv_ItemSubDetail.ColorID
LEFT JOIN SET_KnittingMethod   ON SET_KnittingMethod.KnittingMethodID = Inv_ItemSubDetail.KnittingMethod
Where INV_ItemDetails.Status=1 AND INV_Item.Status=1
AND INV_ItemDetails.ProductType=2
--AND INV_Item.CompanyID=@CompanyID";
                #endregion

                if (Catagoery.HasValue)
                    whereClause.Append(" AND INV_Item.ItemTypeID = " + Catagoery.ToString());

                if (CatalogID.HasValue)
                    whereClause.Append(" AND INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());
                //if (itemID.HasValue)
                //    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_FinishedProductData + whereClause.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetAllLocalAndImportItemID(int CompanyID,DateTime ToDate)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@ToDate", ToDate);

                #region Queries
                const string Query_FinishedProductData = @"
--Select distinct ISNUll(INV_Item.ItemID,0) as ItemID ,ISNULL(importItem.ItemID,0) as ImportItemID,INV_Item.InventoryItemStatusCode from INV_Item 

--left join INV_Item importItem on importItem.SubstituteCode = INV_Item.InventoryItemStatusCode
--where INV_Item.ItemID in
--(Select distinct ItemID from INV_StockTransactionDetail
--inner join INV_StockTransactioNHeader on INV_StockTransactioNHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
--where Convert(Date,DocumentDate) <= Convert(date,@ToDate)
--and  DocumentTypeID in
--(Select DocumentTypeID from SET_DocumentType where (SET_DocumentType.DocumentType like '%GRN%' and (DocumentType like '%Import%' or DocumentType like '%local%') or SET_DocumentType.DocumentType like '%opening%Import')))
--order by ItemID

Select distinct INV_StockTransactionDetail.ItemID,importItem.ItemID as ImportItemID--,INV_Item.Description,INV_Item.InventoryItemStatusCode,INV_Item.SubstituteCode,INV_StockTransactioNHeader.DocumentTypeID
--,importItem.SubstituteCode,importItem.InventoryItemStatusCode
 from INV_StockTransactionDetail
inner join INV_StockTransactioNHeader on INV_StockTransactioNHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID
left join INV_Item importItem on importItem.SubstituteCode = INV_Item.InventoryItemStatusCode
--left join INV_Item importItem on importItem.SubstituteCode = INV_Item.InventoryItemStatusCode
--where Convert(Date,DocumentDate) <= Convert(date,@ToDate)
where  DocumentTypeID in
(Select DocumentTypeID from SET_DocumentType where (SET_DocumentType.DocumentType like '%GRN%' and (DocumentType like '%Import%') 
or SET_DocumentType.DocumentType like '%opening%Import'))
 and importItem.SubstituteCode is not null
 and INV_StockTransactionDetail.ItemID = 4303

";
                #endregion
                //if (itemID.HasValue)
                //    whereClause.Append(" AND inv_item.ItemID = " + itemID.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_FinishedProductData + whereClause.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetAllProductRegistration(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
       Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_GetAllProduct_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAllProduct_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAllProduct_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAllProduct_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllProduct_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool ValidationforProductRegistrationAdd(string description)
        {
            DataTable dtToReturn = null;
            bool IsTrue = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@description", description);


                const string Query_ValidationforProductRegistration = @" select * from [INV_Item] where [Status] = 1 
                AND [INV_Item].[Description] = @description";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidationforProductRegistration, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    dtToReturn = ds.Tables[0];


                    if (dtToReturn.Rows.Count > 0)
                    {
                        IsTrue = true;
                    }
                    else
                        IsTrue  = false;

                }
            }
            catch (Exception ex) 
                    { SqlHelper.LogException(ex); }
            return 
                IsTrue;
        }
        public static decimal GetWeightAgainstProductID(int ItemID)
        {
            decimal Weight = 0;
            StringBuilder sb = new StringBuilder();



            const string Query_GetWeight = @" Select ISNULL(UnitWeight,0) from INV_Item Where Status=1 and ItemID=@ItemID ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@ItemID", ItemID);

                Weight =Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetWeight, sqlparams));
            }
            catch (Exception ex)
            {
                 SqlHelper.LogException(ex);
                throw ex;
            }

            return Weight;
        }


        public static int GetItemID(SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxIdWithOutCompanyId));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_Get_MaxIdWithOutCompanyId));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxIdWithOutCompanyId));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }


        #endregion



        public static bool Check_ValidationUpdate(int ItemId, string Description)
        {

            DataTable dtToReturn = null;
            bool IsTrue = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ItemId", ItemId);
                sqlparams[1] = new SqlParameter("@description", Description);


                const string Query_ValidationforProductRegistration = @" select * from [INV_Item] where [Status] = 1 
                AND [INV_Item].[Description] = @description AND NOT [INV_Item].[ItemID]=@ItemId";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ValidationforProductRegistration, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    dtToReturn = ds.Tables[0];


                    if (dtToReturn.Rows.Count > 0)
                    {
                        IsTrue = true;
                    }
                    else
                        IsTrue = false;

                }
            }
            catch (Exception ex)
            { SqlHelper.LogException(ex); }
            return
                IsTrue;
        }


        public INV_Item(int itemID, SqlConnection objConnection = null)
        {
            Load(itemID, objConnection);
        }

        private void Load(int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByItemID(itemID, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_ItemID = Convert.ToInt32(drRecord["ItemID"]);
                this.f_Description = Convert.ToString(drRecord["Description"]);

                if (drRecord["InvoicingRuleID"] != DBNull.Value)
                    this.f_InvoicingRuleID = Convert.ToInt32(drRecord["InvoicingRuleID"]);


                if (drRecord["ChartofAccountID"] != DBNull.Value)
                    this.f_ChartofAccountID = Convert.ToInt32(drRecord["ChartofAccountID"]);


                if (drRecord["CodeCombinationID"] != DBNull.Value)
                    this.f_CodeCombinationID = Convert.ToString(drRecord["CodeCombinationID"]);


                if (drRecord["ExpenseAccountCCID"] != DBNull.Value)
                    this.f_ExpenseAccountCCID = Convert.ToString(drRecord["ExpenseAccountCCID"]);


                if (drRecord["EncumbranceAccountCCID"] != DBNull.Value)
                    this.f_EncumbranceAccountCCID = Convert.ToString(drRecord["EncumbranceAccountCCID"]);


                if (drRecord["CostOfSalesAccountCCID"] != DBNull.Value)
                    this.f_CostOfSalesAccountCCID = Convert.ToString(drRecord["CostOfSalesAccountCCID"]);


                if (drRecord["SalesAccountCCID"] != DBNull.Value)
                    this.f_SalesAccountCCID = Convert.ToString(drRecord["SalesAccountCCID"]);


                if (drRecord["FEDTaxCCID"] != DBNull.Value)
                    this.f_FEDTaxCCID = Convert.ToString(drRecord["FEDTaxCCID"]);


                if (drRecord["ExciseDutyCCID"] != DBNull.Value)
                    this.f_ExciseDutyCCID = Convert.ToString(drRecord["ExciseDutyCCID"]);


                if (drRecord["SalesDiscountCCID"] != DBNull.Value)
                    this.f_SalesDiscountCCID = Convert.ToString(drRecord["SalesDiscountCCID"]);


                if (drRecord["PurchaseDiscountCCID"] != DBNull.Value)
                    this.f_PurchaseDiscountCCID = Convert.ToString(drRecord["PurchaseDiscountCCID"]);


                if (drRecord["ItemCatalogGroupID"] != DBNull.Value)
                    this.f_ItemCatalogGroupID = Convert.ToInt32(drRecord["ItemCatalogGroupID"]);


                if (drRecord["DefaultSupplierID"] != DBNull.Value)
                    this.f_DefaultSupplierID = Convert.ToInt32(drRecord["DefaultSupplierID"]);


                if (drRecord["QtyRcvExceptionCode"] != DBNull.Value)
                    this.f_QtyRcvExceptionCode = Convert.ToString(drRecord["QtyRcvExceptionCode"]);


                if (drRecord["MarketPrice"] != DBNull.Value)
                    this.f_MarketPrice = Convert.ToDouble(drRecord["MarketPrice"]);


                if (drRecord["HazardClassID"] != DBNull.Value)
                    this.f_HazardClassID = Convert.ToInt32(drRecord["HazardClassID"]);


                if (drRecord["QtyRcvTolerancePercent"] != DBNull.Value)
                    this.f_QtyRcvTolerancePercent = Convert.ToDouble(drRecord["QtyRcvTolerancePercent"]);

                this.f_ListPricePerUnit = Convert.ToDouble(drRecord["ListPricePerUnit"]);

                if (drRecord["UNNumberID"] != DBNull.Value)
                    this.f_UNNumberID = Convert.ToString(drRecord["UNNumberID"]);


                if (drRecord["PriceTolerancePercent"] != DBNull.Value)
                    this.f_PriceTolerancePercent = Convert.ToDouble(drRecord["PriceTolerancePercent"]);


                if (drRecord["AssetCategoryID"] != DBNull.Value)
                    this.f_AssetCategoryID = Convert.ToInt32(drRecord["AssetCategoryID"]);


                if (drRecord["RoundingFactor"] != DBNull.Value)
                    this.f_RoundingFactor = Convert.ToInt32(drRecord["RoundingFactor"]);


                if (drRecord["UnitOfIssue"] != DBNull.Value)
                    this.f_UnitOfIssue = Convert.ToString(drRecord["UnitOfIssue"]);


                if (drRecord["AutoLotAlphaPrefix"] != DBNull.Value)
                    this.f_AutoLotAlphaPrefix = Convert.ToString(drRecord["AutoLotAlphaPrefix"]);


                if (drRecord["StartAutoLotNumber"] != DBNull.Value)
                    this.f_StartAutoLotNumber = Convert.ToString(drRecord["StartAutoLotNumber"]);


                if (drRecord["LotControlCode"] != DBNull.Value)
                    this.f_LotControlCode = Convert.ToInt32(drRecord["LotControlCode"]);


                if (drRecord["ShelfLifeCode"] != DBNull.Value)
                    this.f_ShelfLifeCode = Convert.ToInt32(drRecord["ShelfLifeCode"]);


                if (drRecord["ShelfLifeDays"] != DBNull.Value)
                    this.f_ShelfLifeDays = Convert.ToInt32(drRecord["ShelfLifeDays"]);


                if (drRecord["SerialNumberControlCode"] != DBNull.Value)
                    this.f_SerialNumberControlCode = Convert.ToInt32(drRecord["SerialNumberControlCode"]);


                if (drRecord["StartAutoSerialNumber"] != DBNull.Value)
                    this.f_StartAutoSerialNumber = Convert.ToString(drRecord["StartAutoSerialNumber"]);


                if (drRecord["AutoSerialAlphaPrefix"] != DBNull.Value)
                    this.f_AutoSerialAlphaPrefix = Convert.ToString(drRecord["AutoSerialAlphaPrefix"]);


                if (drRecord["SourceTypeID"] != DBNull.Value)
                    this.f_SourceTypeID = Convert.ToInt32(drRecord["SourceTypeID"]);


                if (drRecord["UnitWeight"] != DBNull.Value)
                    this.f_UnitWeight = Convert.ToDouble(drRecord["UnitWeight"]);


                if (drRecord["WeightUOMCode"] != DBNull.Value)
                    this.f_WeightUOMCode = Convert.ToInt32(drRecord["WeightUOMCode"]);


                if (drRecord["VolumeUOMCode"] != DBNull.Value)
                    this.f_VolumeUOMCode = Convert.ToInt32(drRecord["VolumeUOMCode"]);


                if (drRecord["UnitVolume"] != DBNull.Value)
                    this.f_UnitVolume = Convert.ToDouble(drRecord["UnitVolume"]);


                if (drRecord["ShrinkageRate"] != DBNull.Value)
                    this.f_ShrinkageRate = Convert.ToDouble(drRecord["ShrinkageRate"]);


                if (drRecord["LeadTimeLotSize"] != DBNull.Value)
                    this.f_LeadTimeLotSize = Convert.ToDouble(drRecord["LeadTimeLotSize"]);


                if (drRecord["StdLotSize"] != DBNull.Value)
                    this.f_StdLotSize = Convert.ToDouble(drRecord["StdLotSize"]);

                this.f_PrimaryUOMCode = Convert.ToInt32(drRecord["PrimaryUOMCode"]);

                if (drRecord["PrimaryUnitOfMeasure"] != DBNull.Value)
                    this.f_PrimaryUnitOfMeasure = Convert.ToString(drRecord["PrimaryUnitOfMeasure"]);


                if (drRecord["AllowedUnitsLookupCode"] != DBNull.Value)
                    this.f_AllowedUnitsLookupCode = Convert.ToInt32(drRecord["AllowedUnitsLookupCode"]);


                if (drRecord["InventoryItemStatusCode"] != DBNull.Value)
                    this.f_InventoryItemStatusCode = Convert.ToString(drRecord["InventoryItemStatusCode"]);


                if (drRecord["FixedLotMultipler"] != DBNull.Value)
                    this.f_FixedLotMultipler = Convert.ToInt32(drRecord["FixedLotMultipler"]);


                if (drRecord["RoundingControlTypeID"] != DBNull.Value)
                    this.f_RoundingControlTypeID = Convert.ToInt32(drRecord["RoundingControlTypeID"]);


                if (drRecord["MinimumQuantity"] != DBNull.Value)
                    this.f_MinimumQuantity = Convert.ToDouble(drRecord["MinimumQuantity"]);


                if (drRecord["MaximumQuantity"] != DBNull.Value)
                    this.f_MaximumQuantity = Convert.ToDouble(drRecord["MaximumQuantity"]);


                if (drRecord["MinimumOrderQuantity"] != DBNull.Value)
                    this.f_MinimumOrderQuantity = Convert.ToDouble(drRecord["MinimumOrderQuantity"]);


                if (drRecord["MaximumOrderQuantity"] != DBNull.Value)
                    this.f_MaximumOrderQuantity = Convert.ToDouble(drRecord["MaximumOrderQuantity"]);


                if (drRecord["FixedOrderQuantity"] != DBNull.Value)
                    this.f_FixedOrderQuantity = Convert.ToDouble(drRecord["FixedOrderQuantity"]);


                if (drRecord["FixedDaysSupply"] != DBNull.Value)
                    this.f_FixedDaysSupply = Convert.ToDouble(drRecord["FixedDaysSupply"]);


                if (drRecord["WarrantyVendorID"] != DBNull.Value)
                    this.f_WarrantyVendorID = Convert.ToInt32(drRecord["WarrantyVendorID"]);


                if (drRecord["MaximunWarrantyAmount"] != DBNull.Value)
                    this.f_MaximunWarrantyAmount = Convert.ToDouble(drRecord["MaximunWarrantyAmount"]);


                if (drRecord["ResponseTimePeriodCode"] != DBNull.Value)
                    this.f_ResponseTimePeriodCode = Convert.ToString(drRecord["ResponseTimePeriodCode"]);


                if (drRecord["TAXCCID"] != DBNull.Value)
                    this.f_TAXCCID = Convert.ToString(drRecord["TAXCCID"]);

                this.f_ItemTypeID = Convert.ToInt32(drRecord["ItemTypeID"]);

                if (drRecord["PurchasingTaxCCID"] != DBNull.Value)
                    this.f_PurchasingTaxCCID = Convert.ToString(drRecord["PurchasingTaxCCID"]);


                if (drRecord["EquipmentTypeID"] != DBNull.Value)
                    this.f_EquipmentTypeID = Convert.ToInt32(drRecord["EquipmentTypeID"]);


                if (drRecord["WebStatus"] != DBNull.Value)
                    this.f_WebStatus = Convert.ToString(drRecord["WebStatus"]);

                this.f_PurchasingItemFlag = Convert.ToBoolean(drRecord["PurchasingItemFlag"]);
                this.f_ShippableItemFlag = Convert.ToBoolean(drRecord["ShippableItemFlag"]);
                this.f_CustomerOrderFlag = Convert.ToBoolean(drRecord["CustomerOrderFlag"]);
                this.f_InternalOrderFlag = Convert.ToBoolean(drRecord["InternalOrderFlag"]);
                this.f_ServiceItemFlag = Convert.ToBoolean(drRecord["ServiceItemFlag"]);
                this.f_InventoryItemFlag = Convert.ToBoolean(drRecord["InventoryItemFlag"]);
                this.f_EngItemFlag = Convert.ToBoolean(drRecord["EngItemFlag"]);
                this.f_InventoryAssetFlag = Convert.ToBoolean(drRecord["InventoryAssetFlag"]);
                this.f_PurchasingEnabledFlag = Convert.ToBoolean(drRecord["PurchasingEnabledFlag"]);
                this.f_CustomerOrderEnabledFlag = Convert.ToBoolean(drRecord["CustomerOrderEnabledFlag"]);
                this.f_InternalOrderEnabledFlag = Convert.ToBoolean(drRecord["InternalOrderEnabledFlag"]);
                this.f_SOTransactionsFlag = Convert.ToBoolean(drRecord["SOTransactionsFlag"]);
                this.f_MTLTransactionEnabledFlag = Convert.ToBoolean(drRecord["MTLTransactionEnabledFlag"]);
                this.f_StockEnabledFlag = Convert.ToBoolean(drRecord["StockEnabledFlag"]);
                this.f_CatalogStatusFlag = Convert.ToBoolean(drRecord["CatalogStatusFlag"]);
                this.f_ReturnableFlag = Convert.ToBoolean(drRecord["ReturnableFlag"]);
                this.f_CollateralFlag = Convert.ToBoolean(drRecord["CollateralFlag"]);
                this.f_TaxableFlag = Convert.ToBoolean(drRecord["TaxableFlag"]);
                this.f_AllowItemDescUpdateFlag = Convert.ToBoolean(drRecord["AllowItemDescUpdateFlag"]);
                this.f_InspectionRequiredFlag = Convert.ToBoolean(drRecord["InspectionRequiredFlag"]);
                this.f_ReceiptRequiredFlag = Convert.ToBoolean(drRecord["ReceiptRequiredFlag"]);
                this.f_RFQRequiredFlag = Convert.ToBoolean(drRecord["RFQRequiredFlag"]);
                this.f_AllowSubstituteReceiptsFlag = Convert.ToBoolean(drRecord["AllowSubstituteReceiptsFlag"]);
                this.f_AllowUnorderedReceiptsFlag = Convert.ToBoolean(drRecord["AllowUnorderedReceiptsFlag"]);
                this.f_AllowExpressDeliveryFlag = Convert.ToBoolean(drRecord["AllowExpressDeliveryFlag"]);
                this.f_PreventiveMaintenanceFlag = Convert.ToBoolean(drRecord["PreventiveMaintenanceFlag"]);
                this.f_InvoiceableItemFlag = Convert.ToBoolean(drRecord["InvoiceableItemFlag"]);
                this.f_MustUseApprovedVendorFlag = Convert.ToBoolean(drRecord["MustUseApprovedVendorFlag"]);
                this.f_CheckShortagesFlag = Convert.ToBoolean(drRecord["CheckShortagesFlag"]);
                this.f_ElectronicFlag = Convert.ToBoolean(drRecord["ElectronicFlag"]);
                this.f_DownloadableFlag = Convert.ToBoolean(drRecord["DownloadableFlag"]);
                this.f_VolDiscountExemptFlag = Convert.ToBoolean(drRecord["VolDiscountExemptFlag"]);
                this.f_CouponExemptFlag = Convert.ToBoolean(drRecord["CouponExemptFlag"]);
                this.f_CommsActivationReqdFlag = Convert.ToBoolean(drRecord["CommsActivationReqdFlag"]);
                this.f_OrderableOnWebFlag = Convert.ToBoolean(drRecord["OrderableOnWebFlag"]);
                this.f_IndivisibleFlag = Convert.ToBoolean(drRecord["IndivisibleFlag"]);
                this.f_EnabledFlag = Convert.ToBoolean(drRecord["EnabledFlag"]);
                this.f_SummaryFlag = Convert.ToBoolean(drRecord["SummaryFlag"]);
                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);


                if (drRecord["IsRawMaterial"] != DBNull.Value)
                    this.f_IsRawMaterial = Convert.ToBoolean(drRecord["IsRawMaterial"]);

                this.f_ExpenseAllowed = Convert.ToBoolean(drRecord["ExpenseAllowed"]);

                if (drRecord["ApprovalItemGroupID"] != DBNull.Value)
                    this.f_ApprovalItemGroupID = Convert.ToInt64(drRecord["ApprovalItemGroupID"]);


                if (drRecord["FrontAxleWeight"] != DBNull.Value)
                    this.f_FrontAxleWeight = Convert.ToString(drRecord["FrontAxleWeight"]);


                if (drRecord["FrontAxleWeightUOMCode"] != DBNull.Value)
                    this.f_FrontAxleWeightUOMCode = Convert.ToInt32(drRecord["FrontAxleWeightUOMCode"]);


                if (drRecord["RearAxleWeightI"] != DBNull.Value)
                    this.f_RearAxleWeightI = Convert.ToString(drRecord["RearAxleWeightI"]);


                if (drRecord["RearAxleWeightIUOMCode"] != DBNull.Value)
                    this.f_RearAxleWeightIUOMCode = Convert.ToInt32(drRecord["RearAxleWeightIUOMCode"]);


                if (drRecord["RearAxleWeightII"] != DBNull.Value)
                    this.f_RearAxleWeightII = Convert.ToString(drRecord["RearAxleWeightII"]);


                if (drRecord["RearAxleWeightIIUOMCode"] != DBNull.Value)
                    this.f_RearAxleWeightIIUOMCode = Convert.ToInt32(drRecord["RearAxleWeightIIUOMCode"]);


                if (drRecord["TrailerWeight"] != DBNull.Value)
                    this.f_TrailerWeight = Convert.ToString(drRecord["TrailerWeight"]);


                if (drRecord["TrailerWeightUOMCode"] != DBNull.Value)
                    this.f_TrailerWeightUOMCode = Convert.ToInt32(drRecord["TrailerWeightUOMCode"]);


                if (drRecord["FrontWheels"] != DBNull.Value)
                    this.f_FrontWheels = Convert.ToString(drRecord["FrontWheels"]);


                if (drRecord["RearWheels"] != DBNull.Value)
                    this.f_RearWheels = Convert.ToString(drRecord["RearWheels"]);


                if (drRecord["OtherWheels"] != DBNull.Value)
                    this.f_OtherWheels = Convert.ToString(drRecord["OtherWheels"]);


                if (drRecord["InventoryAccountCCID"] != DBNull.Value)
                    this.f_InventoryAccountCCID = Convert.ToString(drRecord["InventoryAccountCCID"]);


                if (drRecord["CubicCentimeters"] != DBNull.Value)
                    this.f_CubicCentimeters = Convert.ToString(drRecord["CubicCentimeters"]);


                if (drRecord["GSM"] != DBNull.Value)
                    this.f_GSM = Convert.ToDecimal(drRecord["GSM"]);


                if (drRecord["Length"] != DBNull.Value)
                    this.f_Length = Convert.ToDecimal(drRecord["Length"]);


                if (drRecord["Width"] != DBNull.Value)
                    this.f_Width = Convert.ToDecimal(drRecord["Width"]);


                if (drRecord["AdditionalDeductionTaxFlag"] != DBNull.Value)
                    this.f_AdditionalDeductionTaxFlag = Convert.ToBoolean(drRecord["AdditionalDeductionTaxFlag"]);


                if (drRecord["ShelfReference"] != DBNull.Value)
                    this.f_ShelfReference = Convert.ToString(drRecord["ShelfReference"]);


                if (drRecord["SalesTaxExpenseCCID"] != DBNull.Value)
                    this.f_SalesTaxExpenseCCID = Convert.ToString(drRecord["SalesTaxExpenseCCID"]);


                if (drRecord["FEDTaxExpenseCCID"] != DBNull.Value)
                    this.f_FEDTaxExpenseCCID = Convert.ToString(drRecord["FEDTaxExpenseCCID"]);


                if (drRecord["SEDTaxExpenseCCID"] != DBNull.Value)
                    this.f_SEDTaxExpenseCCID = Convert.ToString(drRecord["SEDTaxExpenseCCID"]);


                if (drRecord["Color"] != DBNull.Value)
                    this.f_Color = Convert.ToString(drRecord["Color"]);


                if (drRecord["AccumulatedDepreciationCCID"] != DBNull.Value)
                    this.f_AccumulatedDepreciationCCID = Convert.ToString(drRecord["AccumulatedDepreciationCCID"]);


                if (drRecord["OtherExpenseAccountCCID"] != DBNull.Value)
                    this.f_OtherExpenseAccountCCID = Convert.ToString(drRecord["OtherExpenseAccountCCID"]);


                if (drRecord["DyeFlag"] != DBNull.Value)
                    this.f_DyeFlag = Convert.ToBoolean(drRecord["DyeFlag"]);


                if (drRecord["ChemicalFlag"] != DBNull.Value)
                    this.f_ChemicalFlag = Convert.ToBoolean(drRecord["ChemicalFlag"]);


                if (drRecord["AdjustmentStockAccountCCID"] != DBNull.Value)
                    this.f_AdjustmentStockAccountCCID = Convert.ToString(drRecord["AdjustmentStockAccountCCID"]);

                if (drRecord["ProfitlossAccountCCID"] != DBNull.Value)
                    this.f_ProfitlossAccountCCID = Convert.ToString(drRecord["ProfitlossAccountCCID"]);

                if (drRecord["IsOrderable"] != DBNull.Value)
                    this.f_IsOrderable = Convert.ToBoolean(drRecord["IsOrderable"]);


                if (drRecord["NonRecipeItem"] != DBNull.Value)
                    this.f_NonRecipeItem = Convert.ToBoolean(drRecord["NonRecipeItem"]);


                if (drRecord["SubstituteCode"] != DBNull.Value)
                    this.f_SubstituteCode = Convert.ToString(drRecord["SubstituteCode"]);


                if (drRecord["BrandID"] != DBNull.Value)
                    this.f_BrandID = Convert.ToInt32(drRecord["BrandID"]);

                if (drRecord["Composition"] != DBNull.Value)
                    this.f_Composition = Convert.ToString(drRecord["Composition"]);

                if (drRecord["KnittingMethod"] != DBNull.Value)
                    this.f_KnittingMethod = Convert.ToString(drRecord["KnittingMethod"]);

                if (drRecord["PackType"] != DBNull.Value)
                    this.f_PackType = Convert.ToString(drRecord["PackType"]);

                if (drRecord["PackingStyle"] != DBNull.Value)
                    this.f_PackingStyle = Convert.ToString(drRecord["PackingStyle"]);

                if (drRecord["Embellishment"] != DBNull.Value)
                    this.f_Embellishment = Convert.ToString(drRecord["Embellishment"]);

                if (drRecord["GoodsType"] != DBNull.Value)
                    this.f_GoodsType = Convert.ToString(drRecord["GoodsType"]);

                if (drRecord["ProductType"] != DBNull.Value)
                    this.f_ProductType = Convert.ToString(drRecord["ProductType"]);

                if (drRecord["ProductStyle"] != DBNull.Value)
                    this.f_ProductStyle = Convert.ToString(drRecord["ProductStyle"]);

                if (drRecord["Obsolete"] != DBNull.Value)
                    this.f_Obsolete = Convert.ToBoolean(drRecord["Obsolete"]);

                if (drRecord["HsCode"] != DBNull.Value)
                    this.f_HsCode = Convert.ToString(drRecord["HsCode"]);

                if (drRecord["ImagePath"] != DBNull.Value)
                    this.f_ImagePath = Convert.ToString(drRecord["ImagePath"]);
            }
        }


        public static DataRow GetDataByItemID(int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[0] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_ItemID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ItemID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ItemID, sqlparams);

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

        public static DataRow GetParametersDataByItemID(int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];                
                sqlparams[0] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;

                string Query = @" select top 1 SET_SalesTaxParameters.TUnitPrice,SET_SalesTaxParameters.SalesTaxPercent,INV_Item.UnitWeight from SET_SalesTaxParameters
 inner join INV_Item on INV_Item.ItemID = SET_SalesTaxParameters.Item
  where SET_SalesTaxParameters.Item = @ItemID and INV_Item.Status = 1 and SET_SalesTaxParameters.Status = 1
   order by SET_SalesTaxParameters.CreateDate desc";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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


        const string Query_GetData_ItemID = @" SELECT * FROM [INV_Item] WHERE [ItemID] = @ItemID ";
        const string Query_Get_DocumentTypeGroup = @"Select * from DocumentTypesGroup";
        public static DataTable GetDocumentTypeGroup()
        {
            DataTable dt = null;

            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@Status", status);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DocumentTypeGroup).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool GetDataByCategoryID(int CategoryID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            bool result =false;
            DataRow dr = null;
            const string Query_GetData_byCategoryID = @"Select * from INV_Item where Status=1 and  ItemTypeID=@CategoryID ";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CategoryID", CategoryID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byCategoryID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byCategoryID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byCategoryID, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                { 
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


        public static bool CheckItemStatusCode_Unique(int companyid, string whr, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            bool duplicate = false;
            #region

            const string QueryForItemStatusCode = @" Select * from [INV_Item] where Status=1 ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", companyid);


                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, QueryForItemStatusCode + whr, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, QueryForItemStatusCode + whr, sqlparams).Tables[0];



                if (dt != null && dt.Rows.Count > 0)
                {
                    duplicate = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return duplicate;
        }

        public static DataTable GetDataByKeyForUOM(int companyID, int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_UOM, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_UOM, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_UOM, sqlparams);

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



        public static bool CheckItemTransactionexists(int ItemID, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            bool Transactionexists = false;
            #region

            const string Query_CheckItemTransactionexists = @" select * from (
select distinct INV_StockTransactionDetail.ItemID 
from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1
where INV_StockTransactionHeader.Status = 1
and INV_StockTransactionDetail.ItemID = @ItemID
union all
select distinct AR_InvoiceDetail.ItemID 
from AR_InvoiceHeader
inner join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceDetail.Status = 1
where AR_InvoiceHeader.Status = 1
and AR_InvoiceDetail.ItemID = @ItemID

union all
select distinct SAL_InvoiceDetail.ItemID 
from SAL_InvoiceHeader
inner join SAL_InvoiceDetail on SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
and SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID
and SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID
and SAL_InvoiceDetail.Status = 1
where SAL_InvoiceHeader.Status = 1
and SAL_InvoiceDetail.ItemID = @ItemID

union all
select distinct PUR_EnquiryDetail.ItemID 
from PUR_EnquiryHeader
inner join PUR_EnquiryDetail on PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID
and PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID
and PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID
and PUR_EnquiryDetail.Status = 1
where PUR_EnquiryHeader.Status = 1
and PUR_EnquiryDetail.ItemID = @ItemID
union all

select distinct AP_PurchaseOrderDetail.ItemID 
from AP_PurchaseOrderHeader
inner join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PurchaseOrderDetail.Status = 1
where AP_PurchaseOrderHeader.Status = 1
and AP_PurchaseOrderDetail.ItemID = @ItemID

union all

select distinct SAL_OrderDetail.ItemID 
from SAL_OrderHeader
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
and SAL_OrderDetail.Status = 1
where SAL_OrderHeader.Status = 1
and SAL_OrderDetail.ItemID = @ItemID


) as tab";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@ItemID", ItemID);


                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckItemTransactionexists , sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_CheckItemTransactionexists , sqlparams).Tables[0];



                if (dt != null && dt.Rows.Count > 0)
                {
                    Transactionexists = true;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Transactionexists;
        }


        public static DataTable GetAllDatabyID(Nullable<int> companyID, int itemID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            #region Query
            const string Query_AllDatabyID = @" SELECT ItemID,  CodeCombinationID as PurchaseCode, ExpenseAccountCCID as ExpenseCode
           FROM [INV_Item] WHERE [Status] = 1 
           AND INV_Item.ItemID = @ItemID
           AND ([CompanyID] = @CompanyID OR @CompanyID is null) ";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ItemID", itemID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_AllDatabyID, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_AllDatabyID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_AllDatabyID, sqlparams);

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

        public static DataTable GetShelfReferencefromItem(int companyID)
        {
            DataTable dt = null;

            #region Query
            const string Query_Get_shelfReference = @"
            Select ItemID as ShelfReferenceID , ShelfReference from INV_Item  where INV_Item.CompanyID = @CompanyID and ShelfReference is not null";
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_shelfReference, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetVarietyItemList(Nullable<int> companyID,Nullable<int> categoryID)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                whereClause.Append(" and  IndivisibleFlag = 1 ");

                if (categoryID.HasValue)
                    whereClause.Append(" AND INV_SET_Category.CategoryID = " + categoryID.ToString());

               
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, ItemListPrice_CC_Query + whereClause.ToString() + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
    }
}
