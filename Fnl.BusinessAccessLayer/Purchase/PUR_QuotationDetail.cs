using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class PUR_QuotationDetail
    {
        #region Constant Queries

        const string Query_Select_By_QuotationID_CompanyId_PeriodId = @" 

SELECT [PUR_QuotationDetail].[CompanyID], [PUR_QuotationDetail].[PeriodID], [PUR_QuotationDetail].[QuotationHeaderID], [PUR_QuotationDetail].[QuotationDetailID], 
    [PUR_QuotationDetail].[EnquiryHeaderID], [PUR_QuotationDetail].[EnquiryDetailID], [PUR_QuotationDetail].[OfferedQuantity], [PUR_QuotationDetail].[QuotedPrice], 
    [PUR_QuotationDetail].[NegotiatedPrice], [PUR_QuotationDetail].[Approved], [PUR_QuotationDetail].[ApprovedDate], [PUR_QuotationDetail].[ApprovedBy], 
    [PUR_QuotationDetail].OrderQuantity, [PUR_QuotationDetail].OrderDate, [PUR_QuotationDetail].[Status], [PUR_QuotationDetail].[CreatedBy], 
    [PUR_QuotationDetail].[CreateDate], [PUR_QuotationDetail].[CreatedByValue], [PUR_QuotationDetail].[UpdatedBy], [PUR_QuotationDetail].[UpdateDate], 
    [PUR_QuotationDetail].[UpdatedByValue], [PUR_EnquiryDetail].[ItemCategoryID], [PUR_EnquiryDetail].[ItemID], [PUR_EnquiryDetail].[ItemUnitID], 
    [PUR_EnquiryDetail].[Quantity] AS [RequestedQuantity], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], '' [Action] 
,isnull([PUR_QuotationDetail].LastPrice,0) as LastPrice
,isnull([PUR_QuotationDetail].NegotiationPrice1,0) as NegotiationPrice1
,isnull([PUR_QuotationDetail].NegotiationPrice2,0) as NegotiationPrice2
,[PUR_QuotationDetail].Specs

,isnull(
isnull(([PUR_QuotationDetail].[OfferedQuantity] * [PUR_QuotationDetail].[NegotiationPrice2]),
([PUR_QuotationDetail].[OfferedQuantity] * [PUR_QuotationDetail].[NegotiationPrice1]))
,([PUR_QuotationDetail].[OfferedQuantity] * [PUR_QuotationDetail].QuotedPrice)) as Amount

FROM [PUR_QuotationDetail]
    INNER JOIN [PUR_EnquiryDetail] ON [PUR_EnquiryDetail].[EnquiryDetailID] = [PUR_QuotationDetail].[EnquiryDetailID] 
        AND [PUR_EnquiryDetail].[EnquiryHeaderID] = [PUR_QuotationDetail].[EnquiryHeaderID] AND [PUR_EnquiryDetail].[CompanyID] = [PUR_QuotationDetail].[CompanyID] 
        AND [PUR_EnquiryDetail].[PeriodID] = [PUR_QuotationDetail].[PeriodID] AND [PUR_EnquiryDetail].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
        AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE [PUR_QuotationDetail].[QuotationHeaderID] = @QuotationHeaderID AND [PUR_QuotationDetail].[CompanyID] = @CompanyID 
    AND [PUR_QuotationDetail].[PeriodID] = @PeriodID AND [PUR_QuotationDetail].[Status] = 1 
ORDER BY [PUR_QuotationDetail].[EnquiryHeaderID],[PUR_QuotationDetail].[QuotationDetailID] asc  ";

        const string Query_GetRequisitionData = @" 
select Distinct 
	PUR_QuotationDetail.CompanyID, PUR_QuotationDetail.PeriodID, 
	-1 as POHeaderID, 
	-1 as PODetailID, 
	Pur_EnquiryRequisition.StockHeaderID as RequisitionHeaderID, 
	Pur_EnquiryRequisition.StockDetailID as RequisitionDetailID, 
	Pur_EnquiryRequisition.DocumentTypeID,
	Pur_EnquiryRequisition.DocumentNo,
    PUR_EnquiryDetail.ItemID,
    CAST(Row_Number() over (Order By [PUR_QuotationDetail].[QuotationHeaderID]) as int) as RowUniqueID
from PUR_QuotationDetail
inner join PUR_EnquiryDetail on PUR_EnquiryDetail.EnquiryHeaderID = PUR_QuotationDetail.EnquiryHeaderID
	and PUR_EnquiryDetail.EnquiryDetailID = PUR_QuotationDetail.EnquiryDetailID
	and PUR_EnquiryDetail.CompanyID = PUR_QuotationDetail.CompanyID
	and PUR_EnquiryDetail.PeriodID = PUR_QuotationDetail.PeriodID
inner join Pur_EnquiryRequisition on Pur_EnquiryRequisition.EnquiryDetailID = PUR_EnquiryDetail.EnquiryDetailID
    and Pur_EnquiryRequisition.EnquiryHeaderID = PUR_EnquiryDetail.EnquiryHeaderID
	and Pur_EnquiryRequisition.CompanyID = PUR_EnquiryDetail.CompanyID
	and Pur_EnquiryRequisition.PeriodID = PUR_EnquiryDetail.PeriodID
where PUR_QuotationDetail.CompanyID = @CompanyID
	and PUR_QuotationDetail.PeriodID = @PeriodID 
	and PUR_QuotationDetail.QuotationHeaderID = @QuotationHeaderID
order by Pur_EnquiryRequisition.DocumentNo

";

        const string Query_Select_By_Enquiry_Supplier = @" 
SELECT [PUR_QuotationDetail].[CompanyID], [PUR_QuotationDetail].[PeriodID], [PUR_QuotationDetail].[QuotationHeaderID], [PUR_QuotationDetail].[QuotationDetailID], 
    [PUR_QuotationDetail].[EnquiryHeaderID], [PUR_QuotationDetail].[EnquiryDetailID], [PUR_QuotationDetail].[OfferedQuantity], [PUR_QuotationDetail].[QuotedPrice], 
    [PUR_QuotationDetail].[NegotiationPrice1],[PUR_QuotationDetail].[NegotiationPrice2], [PUR_QuotationDetail].[Approved], [PUR_QuotationDetail].[ApprovedDate], [PUR_QuotationDetail].[ApprovedBy], 
    [PUR_QuotationDetail].OrderQuantity, [PUR_QuotationDetail].OrderDate, [PUR_QuotationDetail].[Status], [PUR_QuotationDetail].[CreatedBy], 
    [PUR_QuotationDetail].[CreateDate], [PUR_QuotationDetail].[CreatedByValue], [PUR_QuotationDetail].[UpdatedBy], [PUR_QuotationDetail].[UpdateDate], 
    [PUR_QuotationDetail].[UpdatedByValue], [PUR_EnquiryDetail].[ItemCategoryID], [PUR_EnquiryDetail].[ItemID], [PUR_EnquiryDetail].[ItemUnitID], 
    [PUR_EnquiryDetail].[Quantity] AS [RequestedQuantity], [INV_SET_Category].[CategoryName], [PUR_EnquiryDetail].[ItemDescription] AS Description, 
    [SET_UnitofMeasure].[UOMName] 
FROM [PUR_QuotationDetail] 
    INNER JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = [PUR_QuotationDetail].CompanyID 
        AND PUR_QuotationHeader.PeriodID = [PUR_QuotationDetail].PeriodID AND PUR_QuotationHeader.QuotationID = [PUR_QuotationDetail].QuotationHeaderID 
    INNER JOIN [PUR_EnquiryDetail] ON [PUR_EnquiryDetail].[EnquiryDetailID] = [PUR_QuotationDetail].[EnquiryDetailID] 
        AND [PUR_EnquiryDetail].[EnquiryHeaderID] = [PUR_QuotationDetail].[EnquiryHeaderID] AND [PUR_EnquiryDetail].[CompanyID] = [PUR_QuotationDetail].[CompanyID] 
        AND [PUR_EnquiryDetail].[PeriodID] = [PUR_QuotationDetail].[PeriodID] AND [PUR_EnquiryDetail].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
        AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE [PUR_QuotationDetail].[EnquiryHeaderID] = @EnquiryHeaderID AND [PUR_QuotationDetail].[CompanyID] = @CompanyID 
    AND [PUR_QuotationDetail].[PeriodID] = @PeriodID AND [PUR_QuotationDetail].[Status] = 1 AND (PUR_QuotationHeader.SupplierID = @SupplierID OR @SupplierID IS NULL) 
AND [PUR_QuotationHeader].QuotationID = @QuotationID
ORDER BY [PUR_QuotationDetail].[QuotationDetailID] ";


        const string Query_Select_By_EnquiryID = @" 
SELECT [PUR_QuotationDetail].[CompanyID], [PUR_QuotationDetail].[PeriodID], [PUR_QuotationDetail].[QuotationHeaderID], [PUR_QuotationDetail].[QuotationDetailID], 
    [PUR_QuotationDetail].[EnquiryHeaderID], [PUR_QuotationDetail].[EnquiryDetailID], [PUR_QuotationDetail].[OfferedQuantity], [PUR_QuotationDetail].[QuotedPrice], 
    [PUR_QuotationDetail].[NegotiationPrice1],[PUR_QuotationDetail].[NegotiationPrice2], [PUR_QuotationDetail].[Approved], [PUR_QuotationDetail].[ApprovedDate], [PUR_QuotationDetail].[ApprovedBy], 
    [PUR_QuotationDetail].OrderQuantity, [PUR_QuotationDetail].OrderDate, [PUR_QuotationDetail].[Status], [PUR_QuotationDetail].[CreatedBy], 
    [PUR_QuotationDetail].[CreateDate], [PUR_QuotationDetail].[CreatedByValue], [PUR_QuotationDetail].[UpdatedBy], [PUR_QuotationDetail].[UpdateDate], 
    [PUR_QuotationDetail].[UpdatedByValue], [PUR_EnquiryDetail].[ItemCategoryID], [PUR_EnquiryDetail].[ItemID], [PUR_EnquiryDetail].[ItemUnitID], 
    [PUR_EnquiryDetail].[Quantity] AS [RequestedQuantity], [INV_SET_Category].[CategoryName], [PUR_EnquiryDetail].[ItemDescription] AS Description, 
    [SET_UnitofMeasure].[UOMName],AR_SET_Customer.CustomerID ,AR_SET_Customer.CustomerName,PUR_QuotationHeader.DocumentNo
,PUR_QuotationDetail.Remarks,PUR_QuotationDetail.DirectorRemarks  ,PUR_QuotationDetail.Approved
FROM [PUR_QuotationDetail] 
    INNER JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = [PUR_QuotationDetail].CompanyID 
        AND PUR_QuotationHeader.PeriodID = [PUR_QuotationDetail].PeriodID AND PUR_QuotationHeader.QuotationID = [PUR_QuotationDetail].QuotationHeaderID 
    INNER JOIN [PUR_EnquiryDetail] ON [PUR_EnquiryDetail].[EnquiryDetailID] = [PUR_QuotationDetail].[EnquiryDetailID] 
        AND [PUR_EnquiryDetail].[EnquiryHeaderID] = [PUR_QuotationDetail].[EnquiryHeaderID] AND [PUR_EnquiryDetail].[CompanyID] = [PUR_QuotationDetail].[CompanyID] 
        AND [PUR_EnquiryDetail].[PeriodID] = [PUR_QuotationDetail].[PeriodID] AND [PUR_EnquiryDetail].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID]         
AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
        AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
inner join AR_SET_Customer on AR_SET_Customer.customerid= PUR_QuotationHeader.SupplierID 
WHERE [PUR_QuotationDetail].[EnquiryHeaderID] = @EnquiryHeaderID AND [PUR_QuotationDetail].[CompanyID] = @CompanyID 
    AND [PUR_QuotationDetail].[PeriodID] = @PeriodID AND [PUR_QuotationDetail].[Status] = 1 
and PUR_QuotationDetail.IsPurchased = 0 
ORDER BY PUR_QuotationHeader.DocumentNo ";

        const string Query_Select_By_Quotation = @" 
select * from(
SELECT [PUR_QuotationDetail].[CompanyID], [PUR_QuotationDetail].[PeriodID], 
    CAST(Row_Number() over (Order By [PUR_QuotationDetail].[QuotationHeaderID]) as int) as RowUniqueID, [PUR_QuotationDetail].[QuotationHeaderID], 
	[PUR_QuotationDetail].[QuotationDetailID], [PUR_QuotationDetail].[EnquiryHeaderID], [PUR_QuotationDetail].[EnquiryDetailID],  '0' as ExciseDuty, '0' as ExciseDutyAmount,
	[PUR_QuotationDetail].[OfferedQuantity], ISNULL([PUR_QuotationDetail].[OrderQuantity], 0) - 
	    (
		    select IsNull(Sum(AP_PurchaseOrderDetail.Quantity), 0) 
		    from AP_PurchaseOrderHeader 
			    inner join AP_PurchaseOrderDetail on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
                    AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
                    AND AP_PurchaseOrderDetail.Status = 1 
		    WHERE AP_PurchaseOrderDetail.CompanyID = [PUR_QuotationDetail].[CompanyID] and AP_PurchaseOrderDetail.[PeriodID] = [PUR_QuotationDetail].[PeriodID] 
                AND [PUR_QuotationDetail].[QuotationHeaderID] = AP_PurchaseOrderDetail.[FromHeaderID] 
                AND [PUR_QuotationDetail].[QuotationDetailID] = AP_PurchaseOrderDetail.[FromDetailID] AND AP_PurchaseOrderHeader.Status = 1 
                AND AP_PurchaseOrderHeader.StatusCode = 1 
                AND AP_PurchaseOrderHeader.TypeCode  in 
		        (Select DocumentTypeID from SET_DocumentType Where  MenuID in (Select MenuID from ApplicationMenu Where  MenuPath in ('/AP/Transaction/PurchaseOrder_frm.aspx')))
	    ) as [Quantity], 
	isnull([PUR_QuotationDetail].[NegotiatedPrice],0) as NegotiatedPrice,isnull([PUR_QuotationDetail].[QuotedPrice],0) as QuotedPrice,isnull([PUR_QuotationDetail].[NegotiationPrice1],0) as NegotiationPrice1,isnull([PUR_QuotationDetail].[NegotiationPrice2],0) as NegotiationPrice2,
    [PUR_QuotationDetail].[Approved], [PUR_QuotationDetail].[ApprovedDate], 
    [PUR_QuotationDetail].[ApprovedBy], [PUR_QuotationDetail].OrderQuantity, [PUR_QuotationDetail].OrderDate, [PUR_QuotationDetail].[Status], 
    [PUR_QuotationDetail].[CreatedBy], [PUR_QuotationDetail].[CreateDate], [PUR_QuotationDetail].[CreatedByValue], [PUR_QuotationDetail].[UpdatedBy], 
    [PUR_QuotationDetail].[UpdateDate], [PUR_QuotationDetail].[UpdatedByValue], [PUR_EnquiryDetail].[ItemCategoryID], [PUR_EnquiryDetail].[ItemID], 
    [PUR_EnquiryDetail].[ItemUnitID], [PUR_EnquiryDetail].[Quantity] AS [RequestedQuantity], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [INV_Item].[ShelfReference],
    [SET_UnitofMeasure].[UOMName], PUR_QuotationHeader.SupplierID,AR_SET_Customer.CustomerName as SupplierName, PUR_EnquiryHeader.DeliveryDate, Pur_EnquiryRequisition.StockHeaderID, 
    Pur_EnquiryRequisition.StockDetailID, Pur_EnquiryRequisition.DocumentTypeID, Pur_EnquiryRequisition.DocumentNo ,
	[INV_StockTransactionDetail].JobID,[INV_StockTransactionDetail].JobCode 
, (case when isnull(PUR_QuotationDetail.NegotiationPrice2,0) > 0 then isnull(PUR_QuotationDetail.NegotiationPrice2,0) when isnull(PUR_QuotationDetail.NegotiationPrice1,0) > 0  then isnull(PUR_QuotationDetail.NegotiationPrice1,0) 
when isnull(PUR_QuotationDetail.QuotedPrice,0) > 0  then isnull(PUR_QuotationDetail.QuotedPrice,0)  else 0 end ) *  
    (ISNULL([PUR_QuotationDetail].[OrderQuantity], 0) - 
	    (
		    select IsNull(Sum(AP_PurchaseOrderDetail.Quantity), 0) 
		    from AP_PurchaseOrderHeader 
			    inner join AP_PurchaseOrderDetail on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
                    AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
                    AND AP_PurchaseOrderDetail.Status = 1 
		    WHERE AP_PurchaseOrderDetail.CompanyID = [PUR_QuotationDetail].[CompanyID] and AP_PurchaseOrderDetail.[PeriodID] = [PUR_QuotationDetail].[PeriodID] 
                AND [PUR_QuotationDetail].[QuotationHeaderID] = AP_PurchaseOrderDetail.[FromHeaderID] 
                AND [PUR_QuotationDetail].[QuotationDetailID] = AP_PurchaseOrderDetail.[FromDetailID] AND AP_PurchaseOrderHeader.Status = 1 
                AND AP_PurchaseOrderHeader.StatusCode = 1 
     
		     AND AP_PurchaseOrderHeader.TypeCode  in 
		        (Select DocumentTypeID from SET_DocumentType Where  MenuID in (Select MenuID from ApplicationMenu Where  MenuPath in ('/AP/Transaction/PurchaseOrder_frm.aspx')))
	    )) as NetAmount,
        0 as ForexValue, 0 as DiscountPer,0 as DiscountAmount,INV_Item.InventoryItemStatusCode,[PUR_QuotationDetail].Specs
FROM [PUR_QuotationDetail] 
	INNER JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = PUR_QuotationDetail.CompanyID AND PUR_QuotationHeader.PeriodID = PUR_QuotationDetail.PeriodID 
        AND PUR_QuotationHeader.QuotationID = [PUR_QuotationDetail].QuotationHeaderID 
	INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryDetailID = PUR_QuotationDetail.EnquiryDetailID 
        AND PUR_EnquiryDetail.EnquiryHeaderID = PUR_QuotationDetail.EnquiryHeaderID AND [PUR_EnquiryDetail].[CompanyID] = [PUR_QuotationDetail].[CompanyID] 
        AND [PUR_EnquiryDetail].[PeriodID] = [PUR_QuotationDetail].[PeriodID] AND [PUR_EnquiryDetail].[Status] = 1 
	LEFT JOIN Pur_EnquiryRequisition ON Pur_EnquiryRequisition.CompanyID = PUR_EnquiryDetail.CompanyID 
        AND Pur_EnquiryRequisition.PeriodID = PUR_EnquiryDetail.PeriodID AND Pur_EnquiryRequisition.EnquiryHeaderID = PUR_EnquiryDetail.EnquiryHeaderID 
        AND Pur_EnquiryRequisition.EnquiryDetailID = PUR_EnquiryDetail.EnquiryDetailID 
				Inner Join [INV_StockTransactionDetail] on [INV_StockTransactionDetail].StockTransactionID=Pur_EnquiryRequisition.StockHeaderID
		And [INV_StockTransactionDetail].TransactionDetailID=Pur_EnquiryRequisition.StockDetailID
		And [INV_StockTransactionDetail].CompanyID=Pur_EnquiryRequisition.CompanyID
		And [INV_StockTransactionDetail].PeriodID=Pur_EnquiryRequisition.PeriodID
		And [INV_StockTransactionDetail].Status=1
	INNER JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = PUR_EnquiryDetail.CompanyID AND PUR_EnquiryHeader.PeriodID = PUR_EnquiryDetail.PeriodID 
        AND PUR_EnquiryHeader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID AND PUR_EnquiryHeader.Status = 1 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
AND [INV_SET_Category].[Status] = 1 
	INNER JOIN INV_Item ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
	INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
	Inner Join AR_SET_Customer on AR_SET_Customer.CustomerID=PUR_QuotationHeader.SupplierID
	And AR_SET_Customer.PeriodID=PUR_QuotationHeader.PeriodID
	And PUR_QuotationHeader.Status=1
WHERE PUR_QuotationDetail.QuotationHeaderID = @QuotationHeaderID AND [PUR_QuotationDetail].[CompanyID] = @CompanyID AND [PUR_QuotationDetail].[PeriodID] = @PeriodID 
    AND [PUR_QuotationDetail].[Status] = 1 
--ORDER BY [PUR_QuotationDetail].[QuotationDetailID] 

) AS TAb
where TAb.Quantity > 0
ORDER BY TAb.StockHeaderID, TAb.StockDetailID Asc

";



        const string Query_Select_By_QuotationPOEdit = @" 
SELECT [PUR_QuotationDetail].[CompanyID], 
    [PUR_QuotationDetail].[PeriodID], 
    CAST(Row_Number() over (Order By [PUR_QuotationDetail].[QuotationHeaderID]) as int) as RowUniqueID, [PUR_QuotationDetail].[QuotationHeaderID], 
	[PUR_QuotationDetail].[QuotationDetailID], 
[PUR_QuotationDetail].[EnquiryHeaderID], 
[PUR_QuotationDetail].[EnquiryDetailID], 
[PUR_QuotationDetail].[OfferedQuantity], ISNULL([PUR_QuotationDetail].[OrderQuantity], 0) - 
	    (
		    select IsNull(Sum(AP_PurchaseOrderDetail.Quantity), 0) 
		    from AP_PurchaseOrderHeader 
			    inner join AP_PurchaseOrderDetail on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
                    AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
                    AND AP_PurchaseOrderDetail.Status = 1 
		    WHERE AP_PurchaseOrderDetail.CompanyID = [PUR_QuotationDetail].[CompanyID] and AP_PurchaseOrderDetail.[PeriodID] = [PUR_QuotationDetail].[PeriodID] 
                AND [PUR_QuotationDetail].[QuotationHeaderID] = AP_PurchaseOrderDetail.[FromHeaderID] 
                AND [PUR_QuotationDetail].[QuotationDetailID] = AP_PurchaseOrderDetail.[FromDetailID] AND AP_PurchaseOrderHeader.Status = 1 
                AND AP_PurchaseOrderHeader.StatusCode = 1 
            AND AP_PurchaseOrderHeader.TypeCode in 
		        (Select DocumentTypeID from SET_DocumentType Where  MenuID in (Select MenuID from ApplicationMenu Where  MenuPath in ('/AP/Transaction/PurchaseOrder_frm.aspx')))
	    ) as [Quantity], 
	[PUR_QuotationDetail].[QuotedPrice], 
[PUR_QuotationDetail].[NegotiatedPrice], 
[PUR_QuotationDetail].[Approved], 
[PUR_QuotationDetail].[ApprovedDate], 
    [PUR_QuotationDetail].[ApprovedBy], 
[PUR_QuotationDetail].OrderQuantity, 
[PUR_QuotationDetail].OrderDate, 
[PUR_QuotationDetail].[Status], 
    [PUR_QuotationDetail].[CreatedBy], 
[PUR_QuotationDetail].[CreateDate], 
[PUR_QuotationDetail].[CreatedByValue], 
[PUR_QuotationDetail].[UpdatedBy], 
    [PUR_QuotationDetail].[UpdateDate], [PUR_QuotationDetail].[UpdatedByValue], [PUR_EnquiryDetail].[ItemCategoryID], [PUR_EnquiryDetail].[ItemID], 
    [PUR_EnquiryDetail].[ItemUnitID], 
[PUR_EnquiryDetail].[Quantity] AS [RequestedQuantity], 
[INV_SET_Category].[CategoryName], [INV_Item].[Description], 
[SET_UnitofMeasure].[UOMName], 
PUR_QuotationHeader.SupplierID, 
PUR_EnquiryHeader.DeliveryDate, 
Pur_EnquiryRequisition.StockHeaderID, 
Pur_EnquiryRequisition.StockDetailID, Pur_EnquiryRequisition.DocumentTypeID, Pur_EnquiryRequisition.DocumentNo 
FROM [PUR_QuotationDetail] 
	INNER JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = PUR_QuotationDetail.CompanyID AND PUR_QuotationHeader.PeriodID = PUR_QuotationDetail.PeriodID 
        AND PUR_QuotationHeader.QuotationID = [PUR_QuotationDetail].QuotationHeaderID 
	INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryDetailID = PUR_QuotationDetail.EnquiryDetailID 
        AND PUR_EnquiryDetail.EnquiryHeaderID = PUR_QuotationDetail.EnquiryHeaderID AND [PUR_EnquiryDetail].[CompanyID] = [PUR_QuotationDetail].[CompanyID] 
        AND [PUR_EnquiryDetail].[PeriodID] = [PUR_QuotationDetail].[PeriodID] AND [PUR_EnquiryDetail].[Status] = 1 
	LEFT JOIN Pur_EnquiryRequisition ON Pur_EnquiryRequisition.CompanyID = PUR_EnquiryDetail.CompanyID 
        AND Pur_EnquiryRequisition.PeriodID = PUR_EnquiryDetail.PeriodID AND Pur_EnquiryRequisition.EnquiryHeaderID = PUR_EnquiryDetail.EnquiryHeaderID 
        AND Pur_EnquiryRequisition.EnquiryDetailID = PUR_EnquiryDetail.EnquiryDetailID 
	INNER JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = PUR_EnquiryDetail.CompanyID AND PUR_EnquiryHeader.PeriodID = PUR_EnquiryDetail.PeriodID 
        AND PUR_EnquiryHeader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID AND PUR_EnquiryHeader.Status = 1 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [PUR_EnquiryDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN INV_Item ON [INV_Item].[ItemID] = [PUR_EnquiryDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [PUR_EnquiryDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
	INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [PUR_EnquiryDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE PUR_QuotationDetail.QuotationHeaderID = @QuotationHeaderID AND [PUR_QuotationDetail].[CompanyID] = @CompanyID AND [PUR_QuotationDetail].[PeriodID] = @PeriodID 
    AND [PUR_QuotationDetail].[Status] = 1 
ORDER BY [PUR_QuotationDetail].[QuotationDetailID] ";

        const string Query_Select_AlreadyOrderedQuantity = @" 
SELECT ISNULL(SUM(PUR_QuotationDetail.OrderQuantity), 0) AS AlreadyOrderedQuantity 
FROM PUR_QuotationDetail
	INNER JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = PUR_QuotationDetail.CompanyID AND PUR_QuotationHeader.PeriodID = PUR_QuotationDetail.PeriodID  
        AND PUR_QuotationHeader.QuotationID = PUR_QuotationDetail.QuotationHeaderID AND PUR_QuotationHeader.Status = 1 
WHERE PUR_QuotationDetail.CompanyID = @CompanyID AND PUR_QuotationDetail.PeriodID = @PeriodID AND PUR_QuotationDetail.QuotationHeaderID = @QuotationHeaderID 
    AND PUR_QuotationDetail.QuotationDetailID = @QuotationDetailID AND PUR_QuotationHeader.SupplierID <> @SupplierID AND PUR_QuotationDetail.Status = 1 ";

        const string Query_IsAllDataDeleted = @" SELECT Count(*) FROM PUR_QuotationDetail 
WHERE PUR_QuotationDetail.CompanyID = @CompanyID AND PUR_QuotationDetail.PeriodID = @PeriodID AND PUR_QuotationDetail.QuotationHeaderID = @QuotationHeaderID 
    AND PUR_QuotationDetail.Status = 1 ";

        #endregion

        #region Class Methods

        public static DataTable GetDataByKeyTable(int quotationHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_QuotationID_CompanyId_PeriodId, sqlparams);

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

        public static DataTable GetRequisitionData(int quotationHeaderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRequisitionData, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRequisitionData, sqlparams);

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

        public static DataTable GetDataBySupplier(int companyID, int periodID, int enquiryHeaderID, Nullable<int> supplierID, SqlConnection objConnection = null,Nullable<int> QuotationID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (supplierID.HasValue)
                    sqlparams[3] = new SqlParameter("@SupplierID", supplierID);
                else
                    sqlparams[3] = new SqlParameter("@SupplierID", DBNull.Value);

                if (QuotationID.HasValue)
                    sqlparams[4] = new SqlParameter("@QuotationID", QuotationID);
                else
                    sqlparams[4] = new SqlParameter("@QuotationID", DBNull.Value);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_Enquiry_Supplier, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_Enquiry_Supplier, sqlparams);

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

        public static DataTable GetDataByQuotation(int companyID, int periodID, int quotationHeaderID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_Quotation, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_Quotation, sqlparams);

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

        public static DataTable GetDataByQuotationForConnect(int companyID, int periodID, int quotationHeaderID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_Quotation, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_Quotation, sqlparams);

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

        public static DataTable GetDataByQuotationPOEdit(int companyID, int periodID, int quotationHeaderID, SqlConnection objConnection = null, int? PODetailID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@PODetailID", PODetailID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_Quotation, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_Quotation, sqlparams);

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

        public static double Get_AlreadyOrderedQuantity(int companyID, int periodID, int quotationHeaderID, short quotationDetailID, int supplierID)
        {
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);
                sqlparams[3] = new SqlParameter("@QuotationDetailID", quotationDetailID);
                sqlparams[4] = new SqlParameter("@SupplierID", supplierID);

                return Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_Select_AlreadyOrderedQuantity, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static bool UpdateQuotationDetails(List<PUR_QuotationDetail> listQuotationDetails)
        {
            bool result = false;
            SqlTransaction objTransaction = SqlHelper.GetTransaction("UpdateQuotations");

            try
            {
                foreach (PUR_QuotationDetail quotationDetail in listQuotationDetails)
                {
                    quotationDetail.UpdateDataByKey(objTransaction);
                }

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
                    SqlHelper.CommitTransaction(objTransaction);
                else
                    SqlHelper.RollBackTransaction(objTransaction, "UpdateQuotations");
            }

            return result;
        }

        public static bool IsAllDataDeleted(int companyID, int periodID, int quotationHeaderID, SqlTransaction objSQLTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);

                if(objSQLTransaction != null)
                    result = Convert.ToInt32(SqlHelper.ExecuteScalar(objSQLTransaction, CommandType.Text, Query_IsAllDataDeleted, sqlparams)) > 0 ? false : true;
                else
                    result = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_IsAllDataDeleted, sqlparams)) > 0 ? false : true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetDataByKeyForFindMode(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> quotationHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@QuotationHeaderID", quotationHeaderID);

                DataSet ds = null;

                const string Query_SelectDataForView = @"
Select PUR_QuotationDetail.OrderQuantity as RequestedQuantity, * from PUR_QuotationHeader
	inner Join PUR_QuotationDetail on PUR_QuotationDetail.QuotationHeaderID= PUR_QuotationHeader.QuotationID
	AND PUR_QuotationDetail.CompanyID = PUR_QuotationHeader.CompanyID
	AND PUR_QuotationDetail.PeriodID = PUR_QuotationHeader.PeriodID
	And PUR_QuotationDetail.Status=1
Inner Join PUR_EnquiryDetail on PUR_EnquiryDetail.EnquiryHeaderID=PUR_QuotationDetail.EnquiryHeaderID
	and PUR_EnquiryDetail.EnquiryDetailID=PUR_QuotationDetail.EnquiryDetailID
	And PUR_EnquiryDetail.CompanyID=PUR_QuotationDetail.CompanyID
	And PUR_EnquiryDetail.PeriodID=PUR_QuotationDetail.PeriodID
	And PUR_EnquiryDetail.Status=1
	inner join PUR_EnquiryHEader on PUR_EnquiryHEader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID
	and PUR_EnquiryHEader.COmpanYID = PUR_EnquiryDetail.COmpanYID
	and PUR_EnquiryHEader.PeriodID= PUR_EnquiryDetail.PeriodID
	and PUR_EnquiryHEader.Status = 1
	inner Join INV_SET_Category on INV_SET_Category.CategoryID=PUR_EnquiryDetail.ItemCategoryID
	inner Join INV_Item on INV_Item.itemID= PUR_EnquiryDetail.ItemID
	left Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID=INV_Item.PrimaryUnitOfMeasure
	Where PUR_QuotationHeader.Status=1 and PUR_QuotationHeader.QuotationID =@QuotationHeaderID 
        And PUR_QuotationHeader.CompanyID=@CompanyID And PUR_QuotationHeader.PeriodID=@PeriodID
";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectDataForView, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SelectDataForView, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataForView, sqlparams);

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


        public static DataTable GetDataByEnquiryHeaderID(int companyID, int periodID, int enquiryHeaderID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_EnquiryID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_EnquiryID, sqlparams);

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
    }
}
