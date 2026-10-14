using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AP.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Sales;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_StockTransactionHeader
    {
        #region Constant Queries


        const string Query_UpdateIsRequestedCompleted = @" Update INV_stockTransactionheader Set IsRequisitionCompleted = @Status 
Where CompanyID = @CompanyID AND PeriodID = @PeriodID And StockTransactionID = @StockTransactionID ";

        const string Query_GetGINQuantityAgainstRequisition = @" SELECT ISNULL(ABS(SUM(INV_StockTransactionDetail.TransactionQuantity)), 0) AS GINQuantity 
FROM INV_StockTransactionDetail 
    INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 AND INV_StockTransactionHeader.StatusCode = 1 
    INNER JOIN Set_DocumentType ON Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
    INNER JOIN ApplicationMenu ON ApplicationMenu.MenuID = Set_DocumentType.MenuID AND ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
WHERE INV_StockTransactionDetail.SourceHeaderID = @RequisitionHeaderID AND INV_StockTransactionDetail.CompanyID = @CompanyID 
    AND INV_StockTransactionDetail.PeriodID = @PeriodID AND INV_StockTransactionDetail.SourceLineID = @RequisitionDetailID 
    AND INV_StockTransactionDetail.SourceDocumentTypeID = @RequisitionDocumentTypeID AND INV_StockTransactionDetail.SourceCode = @RequisitionNo ";

        const string Query_Get_StockSingleRowOnDocNo_DocType = @" Select * From INV_StockTransactionHeader 
Where CompanyID = @CompanyID And PeriodID = @PeriodID And DocumentNo = @DocNo and DocumentTypeID = @DTID {0} ";

        const string Query_GetSumAmountAndSumTransactionQuantity = @" 
Select Isnull(Sum(Amount), 0) As Amount, Isnull(Sum(TransactionQuantity), 0) As TransactionQuantity From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType on Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
        And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Stock_frm.aspx'
Where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 
    And INV_StockTransactionDetail.ItemID = @ItemID ";

        const string Query_LastTransactionCostAgainstItem = @" Select Top 1 TransactionCost From INV_StockTransactionDetail 
Where ItemID = @ItemID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 Order By CreateDate Desc ";

        const string Query_UpdatePriorCost = @" Update INV_StockTransactionDetail Set PriorCost = @PC 
Where ItemID = @ItemID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 ";



        const string Query_GetMaxDocumentNumber = @" Select Max(DocumentNo) + 1 DocumentNo from INV_StockTransactionHeader ";

        const string Query_SelectAll_Searching_WithPagingForRequisition = @" 
SELECT INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionHeader.StockTransactionID, 
    INV_StockTransactionHeader.DocumentTypeID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, INV_StockTransactionHeader.Comments, 
    INV_StockTransactionHeader.Status, INV_StockTransactionHeader.CreatedBy, INV_StockTransactionHeader.CreateDate, INV_StockTransactionHeader.UpdatedBy, 
    INV_StockTransactionHeader.UpdateDate, INV_StockTransactionHeader.CreatedByValue, INV_StockTransactionHeader.UpdatedByValue, 
    INV_StockTransactionHeader.StatusCode, INV_StockTransactionHeader.IsActive, SET_DocumentType.DocumentType, Set_PeriodCustom.PeriodSetName
    ROW_NUMBER() OVER (ORDER BY INV_StockTransactionHeader.StockTransactionID desc) AS 'Row' 
FROM INV_StockTransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.Status = 1 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
";

        const string Query_SelectAll_Searching_WithPaging = @" 
Select 0 as WOC_HeaderID, 0 as CustomerName,0 as CustomerOrderNo, *, ROW_NUMBER() OVER (ORDER BY StockHeader.DocumentNo desc) AS 'Row' from 
(
    SELECT Distinct --INV_StockTransactionDetail.GrainSizeID, INV_StockTransactionDetail.PackTypeID,
        INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionHeader.StockTransactionID, 
        INV_StockTransactionHeader.DocumentTypeID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
        INV_StockTransactionHeader.Comments, INV_StockTransactionHeader.Status, INV_StockTransactionHeader.CreatedBy, INV_StockTransactionHeader.CreateDate, 
        INV_StockTransactionHeader.UpdatedBy, INV_StockTransactionHeader.UpdateDate, INV_StockTransactionHeader.CreatedByValue, 
        INV_StockTransactionHeader.UpdatedByValue, INV_StockTransactionHeader.StatusCode, INV_StockTransactionHeader.IsActive, SET_DocumentType.DocumentType 
        ,AR_SET_Customer.CustomerName as SupplierName
,isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
    and Set_DocumentApprove.DocumentNo = INV_StockTransactionHeader.DocumentNo
and Set_DocumentApprove.FiscalID = INV_StockTransactionHeader.FiscalID
    order by DocumentApproveID desc
        ),INV_StockTransactionHeader.CreatedByValue) as lastApproval,Set_PeriodCustom.PeriodSetName
        ,INV_StockTransactionHeader.DocumentTypeID as SaleContractTypeID , INV_StockTransactionHeader.DocumentNo as ContractNo 
		,INV_StockTransactionHeader.DocumentDate as ContractDate , '' as Broker , '' as Customer ,0 as Quantity,INV_StockTransactionHeader.StockTransactionID as OrderID
        ,AP_PurchaseOrderRequisition.DocumentNo as RequisitionNo
    FROM INV_StockTransactionHeader 
        INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.Status = 1 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        {MenuPath}
        INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
            AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        INNER JOIN INV_Item on INV_StockTransactionDetail.ItemID = INV_Item.ItemID 
--AND INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID 
        inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and Set_PeriodCustom.fiscalid = @currentfiscalid 
		
        left join INV_StockTransactionHeader TranferOut on TranferOut.StockTransactionID = INV_StockTransactionDetail.SourceHeaderID
		and TranferOut.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID
		and TranferOut.DocumentNo = INV_StockTransactionDetail.SourceCode

        Left Join AR_SET_Customer On AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID
		And AR_SET_Customer.PeriodID = INV_StockTransactionHeader.PeriodID
		And AR_SET_Customer.Status = 1

        LEFT JOIN AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID
		and AP_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID
		and AP_PurchaseOrderHeader.DocumentNumber = INV_StockTransactionDetail.SourceCode

		LEFT JOIN AP_PurchaseOrderRequisition on AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
		AND AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderHeader.CompanyID
		AND AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderHeader.PeriodID

{grnfilter}


    {wherefilter}
) as StockHeader";

        const string Query_SelectAll_Searching_WithPagingAssignment = @" 
Select 0 as WOC_HeaderID, 0 as CustomerName,0 as CustomerOrderNo, *, ROW_NUMBER() OVER (ORDER BY StockHeader.DocumentNo desc) AS 'Row' from 
(
    SELECT Distinct INV_StockTransactionHeader.CompanyID, INV_StockTransactionHeader.PeriodID, INV_StockTransactionHeader.StockTransactionID,  INV_StockTransactionDetail.TransactionDetailID, 
        INV_StockTransactionHeader.DocumentTypeID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
        INV_StockTransactionHeader.Comments, INV_StockTransactionHeader.Status, INV_StockTransactionHeader.CreatedBy, INV_StockTransactionHeader.CreateDate, 
        INV_StockTransactionHeader.UpdatedBy, INV_StockTransactionHeader.UpdateDate, INV_StockTransactionHeader.CreatedByValue, 
        INV_StockTransactionHeader.UpdatedByValue, INV_StockTransactionHeader.StatusCode, INV_StockTransactionHeader.IsActive, SET_DocumentType.DocumentType 
,isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
    and Set_DocumentApprove.DocumentNo = INV_StockTransactionHeader.DocumentNo
and Set_DocumentApprove.FiscalID = INV_StockTransactionHeader.FiscalID
    order by DocumentApproveID desc
        ),INV_StockTransactionHeader.CreatedByValue) as lastApproval,Set_PeriodCustom.PeriodSetName
        ,INV_StockTransactionHeader.DocumentTypeID as SaleContractTypeID , INV_StockTransactionHeader.DocumentNo as ContractNo 
		,INV_StockTransactionHeader.DocumentDate as ContractDate , '' as Broker , '' as Customer ,0 as Quantity,INV_StockTransactionHeader.StockTransactionID as OrderID

        ,INV_Item .ItemID
		,INV_Item .Description as item
        ,INV_StockTransactionDetail .TransactionQuantity
    FROM INV_StockTransactionHeader 
        INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.Status = 1 
            AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
            AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 

        left join Pur_RequisitionAssigmentDetail on Pur_RequisitionAssigmentDetail.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
			and Pur_RequisitionAssigmentDetail.TransactionDetailID = INV_StockTransactionDetail.TransactionDetailID
			and Pur_RequisitionAssigmentDetail.ItemID = INV_StockTransactionDetail.ItemID
            and Pur_RequisitionAssigmentDetail.Status = 1
        INNER JOIN INV_Item on INV_StockTransactionDetail.ItemID = INV_Item.ItemID 
        inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and Set_PeriodCustom.fiscalid = @currentfiscalid 
        left join INV_StockTransactionHeader TranferOut on TranferOut.StockTransactionID = INV_StockTransactionDetail.SourceHeaderID
		and TranferOut.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID
		and TranferOut.DocumentNo = INV_StockTransactionDetail.SourceCode
{grnfilter}
{wherefilter}
--and SET_DocumentType.DocumentType like '%HO%'
and (Pur_RequisitionAssigmentDetail.ItemID <> INV_StockTransactionDetail.ItemID or Pur_RequisitionAssigmentDetail.ItemID is null)
) as StockHeader";

        const string Query_SelectAll_Searching_ForRequisition = @"SELECT [INV_StockTransactionHeader].[CompanyID]
      ,[INV_StockTransactionHeader].[PeriodID]      ,[INV_StockTransactionHeader].[StockTransactionID]      ,[INV_StockTransactionHeader].[DocumentTypeID]      ,[INV_StockTransactionHeader].[DocumentNo]
      ,[INV_StockTransactionHeader].[DocumentDate]      ,[INV_StockTransactionHeader].[Comments]      ,[INV_StockTransactionHeader].[Status]
      ,[INV_StockTransactionHeader].[CreatedBy]      ,[INV_StockTransactionHeader].[CreateDate]      ,[INV_StockTransactionHeader].[UpdatedBy]      ,[INV_StockTransactionHeader].[UpdateDate]
      ,[INV_StockTransactionHeader].[CreatedByValue]      ,[INV_StockTransactionHeader].[UpdatedByValue]      ,[INV_StockTransactionHeader].StatusCode      ,[INV_StockTransactionHeader].[IsActive]
      ,[SET_DocumentType].[DocumentType] ,Set_PeriodCustom.PeriodSetName 
FROM [INV_StockTransactionHeader]
INNER JOIN [SET_DocumentType] ON [SET_DocumentType].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] AND [SET_DocumentType].[Status] = 1
AND [SET_DocumentType].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [SET_DocumentType].[CompanyID] = [INV_StockTransactionHeader].[CompanyID]
inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
";

        const string Query_SelectAll_Searching = @" 
SELECT Distinct INV_StockTransactionDetail.GrainSizeID, INV_StockTransactionDetail.PackTypeID,
    [INV_StockTransactionHeader].[CompanyID], [INV_StockTransactionHeader].[PeriodID], [INV_StockTransactionHeader].[StockTransactionID], 
    [INV_StockTransactionHeader].[DocumentTypeID], [INV_StockTransactionHeader].[DocumentNo], [INV_StockTransactionHeader].[DocumentDate], 
    [INV_StockTransactionHeader].[Comments], [INV_StockTransactionHeader].[Status], [INV_StockTransactionHeader].[CreatedBy], 
    [INV_StockTransactionHeader].[CreateDate], [INV_StockTransactionHeader].[UpdatedBy], [INV_StockTransactionHeader].[UpdateDate], 
    [INV_StockTransactionHeader].[CreatedByValue], [INV_StockTransactionHeader].[UpdatedByValue], [INV_StockTransactionHeader].StatusCode, 
    [INV_StockTransactionHeader].[IsActive], [SET_DocumentType].[DocumentType] ,isnull((
        select top 1 CreatedByValue from SET_DocumentApprove     where Set_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
    and Set_DocumentApprove.DocumentNo = INV_StockTransactionHeader.DocumentNo
and Set_DocumentApprove.FiscalID = INV_StockTransactionHeader.FiscalID
order by DocumentApproveID desc
        ),INV_StockTransactionHeader.CreatedByValue) as lastApproval ,Set_PeriodCustom.PeriodSetName
FROM [INV_StockTransactionHeader] 
    INNER JOIN [SET_DocumentType] ON [SET_DocumentType].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] AND [SET_DocumentType].[Status] = 1 
        AND [SET_DocumentType].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [SET_DocumentType].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] 
    INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
inner join Set_PeriodCustom on INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and Set_PeriodCustom.FiscalId  = @CurrentFiscalID

{grnfilter}
";

        const string Query_Item_ID_Desc_FromPO = @"SELECT  INV_Item.[InventoryItemStatusCode],   INV_Item.ItemID,  (case when (APPurOD.ItemDescription  is null) or (APPurOD.ItemDescription  = '') then INV_Item.Description else
APPurOD.ItemDescription end) AS Description, (case when (APPurOD.ItemDescription  is null) or (APPurOD.ItemDescription  = '') then INV_Item.Description else
APPurOD.ItemDescription end) AS SourceType , INV_Item.ShelfReference  FROM  AP_PurchaseOrderDetail APPurOD  INNER JOIN
INV_Item  ON  APPurOD.ItemID = INV_Item.ItemID --APPurOD.CompanyID = INV_Item.CompanyID AND
and APPurOD.CompanyID = @CompanyID and APPurOD.PeriodID =@PeriodID and APPurOD.Status =1
--and INV_Item.CompanyID = @CompanyID 
and INV_Item.Status = 1 and APPurOD.POHeaderID = @POheaderID ";

        private const string Query_getQtyPOHeader_Stock = @"select isnull(sum(TransactionQuantity),0) as TransactionQuantity from
(SELECT [TransactionQuantity] as TransactionQuantity
 FROM  [INV_StockTransactionDetail]  where  [SourceHeaderID] = @POheaderID  
  and    [CompanyID] =  @CompanyID   
   and [PeriodID] =  @PeriodID and [Status] = 1
union all
SELECT  [Quantity]  as  TransactionQuantity   FROM [AP_PurchaseOrderDetail]  where
  POHeaderID = @POheaderID   and [CompanyID] = @CompanyID    
   and [PeriodID] = @PeriodID and [Status] = 1)tbl_POQtyStockQty";

        private const string Query_getQtyPO_Stock = @"
SELECT isnull(
(
    SELECT isnull(Quantity, 0) FROM AP_PurchaseOrderDetail 
    where POHeaderID = @POheaderID and PODetailID = @POdetailID and ItemID = @itemID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1
) -  isnull(sum(Abs([TransactionQuantity])), 0), 0) 
FROM INV_StockTransactionDetail 
where SourceHeaderID = @POheaderID and SourceLineID = @POdetailID And (SourceDocumentTypeID = @PODocumentTypeID OR SourceDocumentTypeID IS NULL) 
    And (SourceCode = @POSourceCode OR SourceCode IS NULL) and ItemID = @itemID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 ";



        private const string Query_Update_PODetail_CloseCode = @" 
UPDATE [AP_PurchaseOrderDetail] SET [ClosedCode] = @closeCode, [ClosedBy] = @closeBy, [ClosedDate] = @closeDate, [ClosedFlag] = @closeFlag, 
    [ClosedReason] = @closeReason 
WHERE [POHeaderID] = @POheaderID and [PODetailID] = @POdetailID and ItemID = @itemID and [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [Status] = 1 
    and [IsActive] = 1 ";

        private const string Query_Update_POHeader_CloseCode = @" 
UPDATE [AP_PurchaseOrderHeader] SET [ClosedCode] = @closeCode, [ClosedDate] = @closeDate 
WHERE [POHeaderID] = @POheaderID and [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [Status] = 1 and [IsActive] = 1 ";

        private const string Query_GetPODetailCount_ByPOHeaderID = @" 
SELECT COUNT(PODETAILID) FROM [AP_PurchaseOrderDetail] 
WHERE POHeaderID = @POheaderID AND (ClosedCode is null Or ClosedCode = 13) and [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [Status] = 1 
    and [IsActive] = 1 ";

        const string Query_Update_TransactionQTY_ByCaneSW = @" 
update INV_StockTransactionDetail set TransactionQuantity = @TransactionQTY 
where CompanyID = @CompanyID and PeriodID = @PeriodID and status = 1 and IsActive = 1 and TransactionDetailID = @TransactionDetailID ";

        const string Query_Select_StockRequisition_Lookup = @" {1}
Select DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate,
 Set_DocumentType.DocumentType,--INV_StockTransactionHeader.DocumentTypeID,
SET_CostCenter.CostCenterName,'' as Item ,INV_StockTransactionHeader.Comments
from INV_StockTransactionHeader 
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
		AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
	Inner JOin ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID 
		inner join INV_StockTransactionDetail  on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
		and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		and INV_StockTransactionDetail.Status = 1
       -- And INV_StockTransactionDetail.IsDone=1

		Left JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
where INV_StockTransactionHeader.StatusCode = 1 AND INV_StockTransactionHeader.Status = 1 AND INV_StockTransactionHeader.IsActive = 1 
	--AND INV_StockTransactionHeader.CompanyID = 1 AND INV_StockTransactionHeader.PeriodID = 1
		AND INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
{DocumentGroup}
and INV_StockTransactionHeader.StockTransactionID In (Select fintab.StockTransactionID from (
Select Cast(Abs(STD.TransactionQuantity) - Abs(isnull(
	(
		Select Sum(INV_StockTransactionDetail.TransactionQuantity) from INV_StockTransactionDetail 
		Where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 
            AND INV_StockTransactionDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
            And INV_StockTransactionDetail.SourceCode = INV_StockTransactionHeader.DocumentNo And INV_StockTransactionDetail.SourceHeaderID = STD.StockTransactionID 
            And INV_StockTransactionDetail.SourceLineID = STD.TransactionDetailID 
	), 0)) AS decimal(18, 5)) as TransactionQuantity2,INV_StockTransactionHeader.StockTransactionID from INV_StockTransactionHeader
	inner join INV_StockTransactionDetail STD on STD.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        and STD.CompanyID = INV_StockTransactionHeader.CompanyID and STD.PeriodID = INV_StockTransactionHeader.PeriodID 
		)fintab where fintab.TransactionQuantity2 >0 )
	AND INV_StockTransactionHeader.StockTransactionID not in (Select Fintab.StockTransactionID from (select * from (
SELECT Convert(smallint, '1') as ChkSelect, Convert(smallint, STD.SerialNo) as SerialNo, -1 AS EnquiryHeaderID, -1 AS EnquiryDetailID, INV_SET_Category.CategoryID as ItemCategoryID, 
    INV_SET_Category.CategoryName, STD.ItemID, INV_Item.Description, INV_Item.Description as ItemDescription, STD.TransactionUOMID as ItemUnitID, 
    SET_UnitofMeasure.UOMName, 
    (
	    Select Abs(INV_StockTransactionDetail.TransactionQuantity) From INV_StockTransactionDetail 
        Where INV_StockTransactionDetail.StockTransactionID = STD.StockTransactionID and INV_StockTransactionDetail.TransactionDetailID = STD.TransactionDetailID 
            AND INV_StockTransactionDetail.CompanyID = STD.CompanyID AND INV_StockTransactionDetail.PeriodID = STD.PeriodID AND INV_StockTransactionDetail.Status = 1 
    ) 
    -
    (
        Select ISNull(SUM(PUR_EnquiryDetail.Quantity), 0) As Quantity From PUR_EnquiryDetail 
            Inner Join PUR_EnquiryRequisition ON PUR_EnquiryRequisition.CompanyID = PUR_EnquiryDetail.CompanyID 
                and PUR_EnquiryRequisition.PeriodID = PUR_EnquiryDetail.PeriodID AND PUR_EnquiryRequisition.EnquiryHeaderID = PUR_EnquiryDetail.EnquiryHeaderID 
                and PUR_EnquiryRequisition.EnquiryDetailID = PUR_EnquiryDetail.EnquiryDetailID 
            INNER JOIN Pur_EnquiryHeader ON Pur_EnquiryHeader.CompanyID = PUR_EnquiryDetail.CompanyID AND Pur_EnquiryHeader.PeriodID = PUR_EnquiryDetail.PeriodID 
                AND Pur_EnquiryHeader.EnquiryID = PUR_EnquiryDetail.EnquiryHeaderID AND Pur_EnquiryHeader.StatusCode = 1 
        Where PUR_EnquiryRequisition.CompanyID = @CompanyID 
			ANd PUR_EnquiryRequisition.PeriodID = @PeriodID 
			and PUR_EnquiryRequisition.StockDetailID = STD.TransactionDetailID 
            AND PUR_EnquiryRequisition.DocumentTypeID = Req.DocumentTypeID 
			and PUR_EnquiryRequisition.DocumentNo = Req.DocumentNo 
			ANd PUR_EnquiryDetail.Status = 1 
    )  AS Quantity, 
    INV_Item.ListPricePerUnit, ISNULL(ISNULL(INV_Item.ListPricePerUnit, 0) * (STD.[TransactionQuantity]), 0) AS TotalAmount, Req.DocumentNo as ReqNo, 
    Req.DocumentTypeID as ReqTypeID, STD.StockTransactionID, STD.TransactionDetailID, 'Add' AS Action, Convert(smallint, STD.SerialNo) as RowUniqueID 
FROM [INV_StockTransactionDetail] STD 
    INNER JOIN INV_StockTransactionHeader AS Req ON Req.StockTransactionID = STD.StockTransactionID AND Req.CompanyID = STD.CompanyID AND Req.PeriodID = STD.PeriodID 
    INNER JOIN INV_Item ON INV_Item.ItemID = STD.ItemID 
    INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID 
    INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = STD.TransactionUOMID 
    LEFT JOIN SET_Currency ON SET_Currency.CurrencyID = STD.CurrencyID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = STD.CostCenterID 
    LEFT JOIN SET_Project ON SET_Project.ProjectID = STD.ProjectID 
	Inner join Pur_RequisitionAssigmentDetail on Pur_RequisitionAssigmentDetail.StockTransactionID = STD.StockTransactionID
			and Pur_RequisitionAssigmentDetail.TransactionDetailID = STD.TransactionDetailID
			and Pur_RequisitionAssigmentDetail.ItemID = STD.ItemID
			and Pur_RequisitionAssigmentDetail.Status = 1
where STD.CompanyID = @CompanyID 
and STD.PeriodID = @PeriodID
 and STD.Status = 1 
) as TAB
where TAB.Quantity > 0
) Fintab)

union

SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, ISnull(SET_CostCenter.CostCenterName,'') as CostCenterName  , '' as Item ,INV_StockTransactionHeader.Comments
FROM INV_StockTransactionHeader 
inner join INV_StockTransactionDetail STD on STD.StockTransactionID = INV_StockTransactionHeader.StockTransactionID AND STD.CompanyID = INV_StockTransactionHeader.CompanyID AND STD.PeriodID = INV_StockTransactionHeader.PeriodID 
AND STD.Status = 1 
{2}
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockRequistion_frm.aspx' Inner JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
{0} 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1 
AND INV_StockTransactionHeader.StatusCode = 1 
    {filter} 
{DocumentGroup}
order by INV_StockTransactionHeader.DocumentDate desc
";

        const string Query_Select_StockRequisition_Lookup_ForPO = @"{1}
Select DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate,
 Set_DocumentType.DocumentType,--INV_StockTransactionHeader.DocumentTypeID,
SET_CostCenter.CostCenterName,'' as Item , INV_StockTransactionHeader.Comments
from INV_StockTransactionHeader 
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
		AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
	Inner JOin ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID 
		inner join INV_StockTransactionDetail  on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
		and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		and INV_StockTransactionDetail.Status = 1
		LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
where INV_StockTransactionHeader.StatusCode = 1 AND INV_StockTransactionHeader.Status = 1 AND INV_StockTransactionHeader.IsActive = 1 
	--AND INV_StockTransactionHeader.CompanyID = 1 AND INV_StockTransactionHeader.PeriodID = 1
{DocumentGroup}
		AND INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
		and INV_StockTransactionHeader.StockTransactionID in 
(Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=0
and eh.FK_Inv_StockTransactionID not IN ( 
Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=1
 ))

union

SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, ISnull(SET_CostCenter.CostCenterName,'') as CostCenterName  , '' as Item , INV_StockTransactionHeader.Comments
FROM INV_StockTransactionHeader inner join INV_StockTransactionDetail STD on STD.StockTransactionID = INV_StockTransactionHeader.StockTransactionID AND STD.CompanyID = INV_StockTransactionHeader.CompanyID AND STD.PeriodID = INV_StockTransactionHeader.PeriodID 
AND STD.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockRequistion_frm.aspx' LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
{0} 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1 
AND INV_StockTransactionHeader.StatusCode = 1 
    {filter} 
 {DocumentGroup}
order by INV_StockTransactionHeader.DocumentDate desc
";


        const string Query_Select_StockRequisition_Lookup_RFQ = @"{1}
Select DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate,
 Set_DocumentType.DocumentType,INV_StockTransactionHeader.Comments ,--INV_StockTransactionHeader.DocumentTypeID,
SET_CostCenter.CostCenterName,'' as Item 
from INV_StockTransactionHeader 
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
		AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
	Inner JOin ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID 
		inner join INV_StockTransactionDetail  on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
		and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		and INV_StockTransactionDetail.Status = 1
		LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
where INV_StockTransactionHeader.StatusCode = 1 AND INV_StockTransactionHeader.Status = 1 AND INV_StockTransactionHeader.IsActive = 1 
	--AND INV_StockTransactionHeader.CompanyID = 1 AND INV_StockTransactionHeader.PeriodID = 1
		AND INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
		and INV_StockTransactionHeader.StockTransactionID in 
(Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=0
and eh.FK_Inv_StockTransactionID not IN ( 
Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=1
 ))

union

SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, ISnull(SET_CostCenter.CostCenterName,'') as CostCenterName  , '' as Item ,INV_StockTransactionHeader.Comments
FROM INV_StockTransactionHeader inner join INV_StockTransactionDetail STD on STD.StockTransactionID = INV_StockTransactionHeader.StockTransactionID AND STD.CompanyID = INV_StockTransactionHeader.CompanyID AND STD.PeriodID = INV_StockTransactionHeader.PeriodID 
AND STD.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockRequistion_frm.aspx' LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
{0} 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1 
AND INV_StockTransactionHeader.StatusCode = 1 
    {filter} 
order by INV_StockTransactionHeader.DocumentDate desc";

        const string Query_Select_StockRequisition_Lookup_RFQ_RSM = @"{1}
Select DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate,
 Set_DocumentType.DocumentType,INV_StockTransactionHeader.Comments ,--INV_StockTransactionHeader.DocumentTypeID,
SET_CostCenter.CostCenterName,'' as Item 
from INV_StockTransactionHeader 
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
		AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
		AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
	Inner JOin ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID 
		inner join INV_StockTransactionDetail  on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
		and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		and INV_StockTransactionDetail.Status = 1
		LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
where INV_StockTransactionHeader.StatusCode = 1 AND INV_StockTransactionHeader.Status = 1 AND INV_StockTransactionHeader.IsActive = 1 
	--AND INV_StockTransactionHeader.CompanyID = 1 AND INV_StockTransactionHeader.PeriodID = 1
		AND INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
		and INV_StockTransactionHeader.StockTransactionID in 
(Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=0
and eh.FK_Inv_StockTransactionID not IN ( 
Select eh.FK_Inv_StockTransactionID from PUR_EnquiryHeader as eh where eh.Status=1
 ))

union

SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, ISnull(SET_CostCenter.CostCenterName,'') as CostCenterName  , '' as Item ,INV_StockTransactionHeader.Comments
FROM INV_StockTransactionHeader inner join INV_StockTransactionDetail STD on STD.StockTransactionID = INV_StockTransactionHeader.StockTransactionID AND STD.CompanyID = INV_StockTransactionHeader.CompanyID AND STD.PeriodID = INV_StockTransactionHeader.PeriodID 
AND STD.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockRequistion_frm.aspx' LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
{0} 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1 
AND INV_StockTransactionHeader.StatusCode = 1 
    {filter} 
{DocumentGroup}
order by INV_StockTransactionHeader.DocumentDate desc";

        const string Query_Select_StockTransferOut_Lookup = @" 

select * from (
Select INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, Set_DocumentType.DocumentType ,(
select isnull(abs(sum(TransactionQuantity)),0)
from  INV_StockTransactionDetail 
where INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID= INV_StockTransactionHeader .CompanyID
and INV_StockTransactionDetail.PeriodID= INV_StockTransactionHeader .PeriodID
and INV_StockTransactionDetail.status = 1)
as OUTQty
,(select isnull(abs(sum(TransactionQuantity)),0)
from  INV_StockTransactionDetail 
where INV_StockTransactionDetail.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.SourceCode = INV_StockTransactionHeader.DocumentNo
and INV_StockTransactionDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and INV_StockTransactionDetail.CompanyID= INV_StockTransactionHeader.CompanyID and INV_StockTransactionDetail.PeriodID= INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1
)  as INQty 

from INV_StockTransactionHeader 


    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
And (Set_DocumentType.DocumentType Like '%Goods Outward%' or Set_DocumentType.DocumentType Like '%Transfer Out%' or Set_DocumentType.DocumentType Like '%Transfer-Out%')
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID And INV_StockTransactionHeader.Status = 1 
    And INV_StockTransactionHeader.StatusCode = 1 
	--AND (INV_StockTransactionHeader.IsTransferred IS NULL OR INV_StockTransactionHeader.IsTransferred = 0)
	AND LOWER(Set_DocumentType.DocumentType) NOT LIKE '%(fa)%' 
{0}
)as  tab
where tab.OUTQty > tab.INQty";

        const string Query_Select_FromLook_GateEntry = @" 
Select INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate from INV_StockTransactionHeader 
where INV_StockTransactionHeader.BusinessPartnerID = @BpartnerID and CompanyID = @CompanyID and PeriodID = @PeriodID ";



        const string Query_Select_CheckPOQuantity = @" 
SELECT DISTINCT INV_StockTransactionDetail.* 
FROM INV_StockTransactionDetail 
    INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 ";

        const string Query_UpdateStockHeaderForBottling = @"
	    Update INV_StockTransActionHeader
	    Set Status = 0
	    where StockTransactionID in (Select Distinct StockTransactionID from INV_StockTransActionDetail where SourceHeaderID = @RecipeID)
        ";

        const string Query_GetStrockReqSupplierForRFQ = @" 
select INV_StockTransactionDetail.ItemID, Ar_Set_Customer.CustomerID, Ar_Set_Customer.CustomerName
from INV_StockTransactionDetail
INNER JOIN Ar_Set_Customer on Ar_Set_Customer.CustomerID = INV_StockTransactionDetail.SupplierID
	--and AR_SET_Customer.CompanyID = INV_StockTransactionDetail.CompanyID
Where INV_StockTransactionDetail.[StockTransactionID] = @StockTransactionID
AND INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID
";

        const string Query_GetStockRequisitionNo = @"
SELECT INV_StockTransactionHeader.DocumentNo,INV_StockTransactionHeader.DemandType, INV_StockTransactionHeader.SeasonType, INV_StockTransactionHeader.DocumentTypeID
FROM INV_StockTransactionHeader 
    INNER JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND PUR_EnquiryHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND PUR_EnquiryHeader.FK_INV_StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID 
AND INV_StockTransactionHeader.Status = 1 
    AND PUR_EnquiryHeader.FK_INV_StockTransactionID = @FK_INV_StockTransactionID ";

        const string Query_GetRecordForTransferOut = @"
Select INV_stockTransactionDetail.SerialNo,
 INV_stockTransactionDetail.TransactionDetailID,
 INV_SET_Category.CategoryID,
 INV_SET_Category.CategoryName,
 INV_stockTransactionDetail.ItemID,
 INV_Item.[Description],
 [SET_UnitofMeasure].[UOMID],
 [SET_UnitofMeasure].[UOMName],
    (
        select IsNull(Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' then 0 else std.TransactionQuantity END), 0) 
                As SumOfQuantityInStock 
        from INV_StockTransactionDetail std
			Inner Join INV_StockTransactionHeader sth On sth.StockTransactionID = std.StockTransactionID 
                AND sth.CompanyID = INV_StockTransactionDetail.CompanyID 
                And std.PeriodID = sth.PeriodID
			Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = sth.DocumentTypeID 
                And SET_DocumentType.CompanyID = sth.CompanyID And Set_DocumentType.PeriodID = sth.PeriodID 
            where std.ItemID =  INV_stockTransactionDetail.ItemID And std.CompanyID = @CompanyID 
                And std.PeriodID = @PeriodID And std.Status = 1 And SET_DocumentType.TransactionImpact <> 'No Impact' 
    ) As CurrentStock,
 INV_stockTransactionDetail.TransactionQuantity,
INV_stockTransactionDetail.TransactionCost,
(INV_stockTransactionDetail.TransactionCost * INV_stockTransactionDetail.TransactionQuantity)Amount,
INV_StockTransactionDetail.StockTransactionID as SourceHeaderID,
INV_StockTransactionDetail.TransactionDetailID as SourceLineID, 
INV_StockTransactionHeader.DocumentNO AS SourceCode,
INV_StockTransactionHeader.DocumentTypeID, 
INV_stockTransactionHeader.LocationID,
'' as Action
From INV_stockTransactionHeader

INNER JOIN INV_stockTransactionDetail ON INV_stockTransactionDetail.StockTransactionID = INV_stockTransactionHeader.StockTransactionID
		   AND INV_stockTransactionHeader.CompanyID = INV_stockTransactionDetail.CompanyID
		   AND INV_stockTransactionHeader.PeriodID = INV_stockTransactionDetail.PeriodID
INNER JOIN INV_Item ON INV_Item.ItemID = INV_stockTransactionDetail.ItemID
		   --AND INV_stockTransactionDetail.CompanyID = INV_Item.CompanyID
Inner Join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
-- 

LEFT JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = INV_stockTransactionDetail.[TransactionUOMID] 		   

WHERE  INV_stockTransactionHeader.CompanyID = @PeriodID
AND INV_stockTransactionHeader.PeriodID = @CompanyID
AND INV_stockTransactionHeader.StockTransactionID = @StockTransactionID";

        const string Query_Select_StockRequisition_LookupForTransferOut = @"
Select * From 
(
    SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
        Set_DocumentType.DocumentType, SET_CostCenter.CostCenterName, INV_StockTransactionHeader.Comments, ISNULL(
        (
            Select Distinct std.SourceHeaderID From INV_StockTransactionHeader sth 
                INNER JOIN Set_DocumentType dt ON dt.DocumentTypeID = sth.DocumentTypeID AND dt.CompanyID = sth.CompanyID ANd dt.PeriodID = sth.PeriodID 
                inner join INV_StockTransactionDetail std on std.StockTransactionID = sth. StockTransactionID AND std.CompanyID = sth.CompanyID 
                    AND std.PeriodID = sth.PeriodID 
            WHere std.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID AND dt.DocumentType = 'Transfer Out'
        ), 0)ToSourceHeaderID 
    FROM INV_StockTransactionHeader 
        inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID 
            AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
            AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        INNER JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
    WHERE Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND INV_StockTransactionHeader.CompanyID = @CompanyID 
        AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND Set_DocumentType.Status = 1 
        AND Set_DocumentType.DocumentType like 'Parts Requisition%' AND INV_StockTransactionHeader.StatusCode = 1
) Tab 
--Where Tab.StockTransactionID <> Tab.ToSourceHeaderID ";

        const string Query_GoodsIssueNoteReport = @"
SELECT SET_CostCenter.CostCenterName, INV_StockTransactionHeader.Comments, INV_StockTransactionHeader.DocumentDate, INV_StockTransactionHeader.DocumentNo, 
    SET_Location.LocationName, INV_Item.Description, INV_Item.InventoryItemStatusCode, SET_UnitofMeasure.UOMName, INV_StockTransactionDetail.PrimaryQuantity, 
    [INV_StockTransactionDetail].[TransactionQuantity], IsNull(INV_StockTransactionDetail.SourceCode, '') as ReqNo 
	,
 isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = [INV_StockTransactionHeader].DocumentTypeID 
    and Set_DocumentApprove.DocumentNo = [INV_StockTransactionHeader].DocumentNo
    and Set_DocumentApprove.FiscalID = [INV_StockTransactionHeader].FiscalID
    order by DocumentApproveID desc
        ),[INV_StockTransactionHeader].CreatedByValue)  as ApprovedBy
		,(case when [INV_StockTransactionHeader].UpdatedByValue is null then [INV_StockTransactionHeader].CreatedByValue else [INV_StockTransactionHeader].UpdatedByValue end) as PreparedBy
        ,INV_StockTransactionDetail.JobCode,SET_Rack.RackName,SET_Department.DepartmentName,ItemIncurred.Description AS IncurredOn
		,AR_SET_Customer.CustomerName AS EmployeeName
,(
		select max(ReqHdr.DocumentDate) 
		from INV_StockTransactionDetail ReqDtl
		inner join INV_StockTransactionHeader ReqHdr on ReqHdr.StockTransactionID = ReqDtl.StockTransactionID
		and ReqHdr.CompanyID = ReqDtl.CompanyID
		and ReqHdr.PeriodID = ReqDtl.PeriodID
		where ReqDtl.StockTransactionID = INV_StockTransactionDetail.SourceHeaderID
		and ReqHdr.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID
		and ReqHdr.CompanyID = INV_StockTransactionDetail.CompanyID
		and ReqHdr.PeriodID = INV_StockTransactionDetail.PeriodID
		) as ReqDate
FROM INV_StockTransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.Status = 1 
	inner Join ApplicationMenu On ApplicationMenu.MenuID = [SET_DocumentType].MenuID And ApplicationMenu.MenuPath  in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/Average/StockGIN_frm.aspx' )
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_Item.Status = 1 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
        AND INV_SET_Category.Status = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [INV_StockTransactionDetail].[TransactionUOMID] AND [SET_UnitofMeasure].[Status] = 1 
    left JOIN SET_CostCenter ON INV_StockTransactionHeader.CostCenterID = SET_CostCenter.CostCenterID AND SET_CostCenter.Status = 1 
    Inner JOIN SET_Location ON INV_StockTransactionDetail.LocationID = SET_Location.LocationID AND SET_Location.Status = 1

Left join SET_Rack ON SET_Rack.RackID  = INV_StockTransactionDetail.RackID
	AND SET_Rack.Status = 1
	LEFT join SET_Department ON SET_Department.DepartmentID = INV_StockTransactionHeader.DepartmentID
	And SET_Department.Status = 1
	LEFT JOIN INV_Item ItemIncurred ON ItemIncurred.ItemID = INV_StockTransactionDetail.IncurredOnID
	AND ItemIncurred.Status = 1
	LEFT JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID
	AND AR_SET_Customer.Status = 1
 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1
AND INV_StockTransactionHeader.FiscalID = @FiscalID 
";


        const string Query_GoodsReturnNoteReport = @"
SELECT SET_CostCenter.CostCenterName, INV_StockTransactionHeader.Comments, INV_StockTransactionHeader.DocumentDate, INV_StockTransactionHeader.DocumentNo, 
    SET_Location.LocationName, INV_Item.Description, INV_Item.InventoryItemStatusCode, SET_UnitofMeasure.UOMName, INV_StockTransactionDetail.PrimaryQuantity, 
    [INV_StockTransactionDetail].[TransactionQuantity], IsNull(INV_StockTransactionDetail.SourceCode, '') as ReqNo 
FROM INV_StockTransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.Status = 1 
	inner Join ApplicationMenu On ApplicationMenu.MenuID = [SET_DocumentType].MenuID 
    And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockReturn.aspx' 
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID AND INV_Item.Status = 1
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID  
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
        AND INV_SET_Category.Status = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [INV_StockTransactionDetail].[TransactionUOMID] AND [SET_UnitofMeasure].[Status] = 1 
    left JOIN SET_CostCenter ON INV_StockTransactionHeader.CostCenterID = SET_CostCenter.CostCenterID AND SET_CostCenter.Status = 1 
    INNER JOIN SET_Location ON INV_StockTransactionHeader.LocationID = SET_Location.LocationID AND SET_Location.Status = 1 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND INV_StockTransactionHeader.Status = 1 ";



        const string Query_DirectInvoiceReport = @"

select AR_InvoiceHeader.CreateDate,
	AR_InvoiceHeader.InvoiceNumber, 
	SET_Department.DepartmentName,
	SET_CostCenter.CostCenterName,
	AR_Invoice_AccountsDetail.Amount,
	Incurred.Description as IncurredOn, 	
	GL_TransactionDetail.CodeCombinationID,
	GL_TransactionDetail.TransactionType,
	SET_DocumentType.DocumentType,
	AR_Invoice_AccountsDetail.narration,
	GL_TransactionHeader.ControlCredit,
	GL_TransactionHeader.ControlDebit

from AR_InvoiceHeader 

inner join AR_Invoice_AccountsDetail on AR_Invoice_AccountsDetail.companyId = AR_InvoiceHeader.CompanyId
and AR_Invoice_AccountsDetail.PeriodId =AR_InvoiceHeader.Periodid
and AR_Invoice_AccountsDetail.InvoiceHeaderId = AR_InvoiceHeader.InvoiceID

left join SET_Department on SET_Department.DepartmentID = AR_InvoiceHeader.DepartmentID
left JOIN SET_CostCenter ON AR_InvoiceHeader.CostCenterID = SET_CostCenter.CostCenterID

left join INV_Item as Incurred ON Incurred.ItemId = AR_InvoiceHeader.IncurredOnID
AND Incurred.CompanyID = AR_InvoiceHeader.CompanyID 	
AND Incurred.Status = 1 

INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType 
AND  SET_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID 
AND SET_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID
AND  SET_DocumentType.Status = 1 and  (AR_InvoiceHeader.StatusCode = 1)


Inner join GL_TransactionDetail on GL_TransactionDetail.SourceHeaderID = AR_InvoiceHeader.InvoiceID
and GL_TransactionDetail.CompanyID = AR_InvoiceHeader.CompanyID
and GL_TransactionDetail.PeriodID = AR_InvoiceHeader.PeriodId

inner join GL_TransactionHeader on GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID
AND GL_TransactionHeader.CompanyID= GL_TransactionDetail.CompanyID
	AND GL_TransactionHeader.PeriodID= GL_TransactionDetail.PeriodID


and  GL_TransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
	AND GL_TransactionHeader.CompanyID= SET_DocumentType.CompanyID
	AND GL_TransactionHeader.PeriodID= SET_DocumentType.PeriodID






WHERE     (AR_InvoiceHeader.CompanyID = @CompanyID)
        And (SET_Department.DepartmentId = @DepartmentID Or @DepartmentID is null) 
        AND ( AR_InvoiceHeader.CostCenterId = @CostCenterId or @CostCenterId is null) 
        And (AR_InvoiceHeader.IncurredOnID =@IncurredOnID or @IncurredOnID is null)
AND (AR_InvoiceHeader.PeriodID = @PeriodID) AND (AR_InvoiceHeader.Status = 1)

and SET_DocumentType.DocumentType like 'DI%'

";


        const string Query_InspectionReport = @"Select INV_StockTransactionHeader.DocumentNo as RRNo,INV_StockTransactionHeader.DocumentDate as Date,
INV_Item.Description as ItemName, INV_Item.InventoryItemStatusCode as ItemStatusCode, AR_SET_Customer.CustomerName as Supplier,SET_UnitofMeasure.UOMName  as unit,'' as Value, 
INV_StockTransactionDetail.POQuantity As Quantity,INV_StockTransactionDetail.TransactionCost as Rate,
INV_SET_Category.CategoryID,INV_SET_Category.CategoryName
From INV_StockTransactionHeader 

inner Join INV_StockTransactionDetail ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
And INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
inner Join INV_Item On INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--And INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
inner Join AR_SET_Customer ON  
AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID 
--And AR_SET_Customer.CompanyID = INV_StockTransactionHeader.CompanyID 
And AR_SET_Customer.PeriodID = INV_StockTransactionHeader.PeriodID
inner join SET_UnitofMeasure ON SET_UnitofMeasure.UOMID=INV_StockTransactionDetail.TransactionUOMID

inner Join INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--And INV_StockTransactionDetail.CompanyID = INV_SET_Category.CompanyID

inner join SET_DocumentType ON INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
And SET_DocumentType.CompanyID=INV_StockTransactionHeader.CompanyID
And SET_DocumentType.PeriodID=INV_StockTransactionHeader.PeriodID
Where INV_StockTransactionHeader.Status = 1
and ISNULL(INV_StockTransactionDetail.ISInspected,0) = 0
and Set_DocumentType.DocumentType Like '%GRN%'
AND INV_StockTransactionHeader.CompanyID = @CompanyID 
AND INV_StockTransactionHeader.PeriodID = @PeriodID";



        const string Query_Select_OldStockRequisition = @" 
Select Set_DocumentType.DocumentType, Set_DocumentType.DocumentTypeID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, INV_StockTransactionHeader.StockTransactionID
From INV_StockTransactionHeader
INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
Where INV_StockTransactionHeader.StatusCode = 1 AND INV_StockTransactionHeader.Status = 1
AND Set_DocumentType.DocumentType = @DocumentType
AND INV_StockTransactionHeader.CompanyID = @CompanyID 
AND INV_StockTransactionHeader.PeriodID = @PeriodID";

        const string Query_GetKeyBySourceHeaderID = @"
select * from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
where SourceHeaderID = @SourceHeaderID
	and DocumentTypeID = @DocumentTypeID
	and DocumentNo = @DocumentNo
    and INV_StockTransactionHeader.CompanyID = @CompanyID
	and INV_StockTransactionHeader.PeriodID = @PeriodID
        ";

        const string Query_GetProductionReport = @"
Select SET_Quality.QualityName As ProductionQuality, 
    SET_DocumentType.DocumentType, 
    INV_StockTransactionHeader.BatchID,
	INV_Item.Description As ProductName, 
    INV_StockTransactionDetail.TransactionQuantity AS ProductionQuantity,
    INV_StockTransactionDetail.ItemID, 
    INV_StockTransactionHeader.DocumentDate, 
    'On Date' As ProductionType 
From INV_StockTransactionHeader 
	inner join INV_StockTransactionDetail on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
		And INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
		And INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
	left join SET_Quality on SET_Quality.QualityID = INV_StockTransactionDetail.QualityID 
	inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
		and SET_DocumentType .CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType .PeriodID = INV_StockTransactionHeader.PeriodID 
	inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--and INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
	inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/FinishedGoods.aspx'
Where INV_StockTransactionHeader.CompanyID=@CompanyID
    and INV_StockTransactionHeader.PeriodID=@PeriodID
{0}

";


        const string Query_GetGINRecordsForStockReturnReport = @"
Select SET_DocumentType.DocumentType, SET_CostCenter.CostCenterName, INV_StockTransactionHeader.Comments,
       INV_StockTransactionHeader.BusinessPartnerID , INV_stockTransactionDetail.SerialNo,INV_StockTransactionDetail.TransactionQuantity,
	   INV_StockTransactionHeader.DocumentNo,SET_Location.LocationName, INV_Item.Description ,INV_Item.InventoryItemStatusCode,
	   SET_UnitofMeasure.UOMName,INV_StockTransactionHeader.DocumentDate,INV_StockTransactionHeader.DocumentTypeID,
	   (select top 1 case when TransactionCost = 0 then (INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.AverageRate) ELSE (INV_StockTransactionDetail.TransactionQuantity*INV_StockTransactionDetail.TransactionCost) END) AS ReturnVale,
	(select top 1 INV_StockTransactionHeader.DocumentNo from INV_StockTransactionDetail 
inner join INV_StockTransactionHeader INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
where INV_StockTransactionDetail.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.SourceCode = INV_StockTransactionHeader.DocumentNo
and INV_StockTransactionDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1
) as GINNO,
 isnull((
        select top 1 CreatedByValue from SET_DocumentApprove 
        where Set_DocumentApprove.DocumentTypeID = [INV_StockTransactionHeader].DocumentTypeID 
    and Set_DocumentApprove.DocumentNo = [INV_StockTransactionHeader].DocumentNo
    and Set_DocumentApprove.FiscalID = [INV_StockTransactionHeader].FiscalID
    order by DocumentApproveID desc
        ),[INV_StockTransactionHeader].CreatedByValue)  as ApprovedBy
	,(case when [INV_StockTransactionHeader].UpdatedByValue is null then [INV_StockTransactionHeader].CreatedByValue else [INV_StockTransactionHeader].UpdatedByValue end) as PreparedBy

 From INV_StockTransactionHeader


 INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
		AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		AND SET_DocumentType.DocumentType like  '%Stock Return%' 
        AND SET_DocumentType.Status = 1
	
  left JOIN SET_CostCenter ON INV_StockTransactionHeader.CostCenterID = SET_CostCenter.CostCenterID 
	    AND SET_CostCenter.Status = 1 

  INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
		AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 

INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
        AND INV_Item.Status = 1 

INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
        AND INV_SET_Category.Status = 1

INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [INV_StockTransactionDetail].[TransactionUOMID] 
	    AND [SET_UnitofMeasure].[Status] = 1 

INNER JOIN SET_Location ON INV_StockTransactionHeader.LocationID = SET_Location.LocationID 
        AND SET_Location.Status = 1 
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID 
     AND INV_StockTransactionHeader.PeriodID = @PeriodID 
     AND INV_StockTransactionHeader.FiscalID = @CurrentFiscalID 
     AND INV_StockTransactionHeader.Status = 1  ";

        const string Query_GetConsumptionReport = @" 

Select * from
(
Select INV_SET_Category.CategoryName,ISNULL(SET_CostCenter.CostCenterID,returncostcenter.CostCenterID)CostCenterID, sth.IncurredOnID, 
ISNULL(SET_CostCenter.CostCenterName,returncostcenter.CostCenterName)CostCenterName
, ISNULL(SET_Department.DepartmentName,returndept.DepartmentName)DepartmentName,
ISNULL(SET_Department.DepartmentID,returndept.DepartmentID)DepartmentID,
INV_Item.Description, INV_Item.ItemID,Inv_Item.InventoryItemStatusCode as ItemCode, 
    SET_UnitofMeasure.UOMName,
	 std.TransactionQuantity * (-1) AS TransactionQuantity, 
	std.TransactionCost,
	--GL_TransactionDetail.Amount / (std.TransactionQuantity * (-1)) as TransactionCost,

	 INV_Item.ListPricePerUnit, 
    ((std.TransactionQuantity * (-1)) * std.TransactionCost) Amount,
		 --GL_TransactionDetail.Amount as Amount,
	 sth.DocumentDate,
SET_DocumentType.DocumentType, 
case when std.JobID > 0 then Set_Job.AccountCode else  Inv_Item.ExpenseAccountCCID end as ExpenseAccountCCID ,
GL_CodeCombinationDetail.Description as CodeDescription,
sth.DocumentNo,std.JobCode, std.JobID

From INV_StockTransactionHeader sth 
    INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = sth.StockTransactionID
	 AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID 
AND std.FiscalID = sth.FiscalID 
        AND std.Status = 1 
    Inner join [SET_DocumentType]  on [SET_DocumentType].DocumentTypeID = sth.DocumentTypeID
		and [SET_DocumentType].CompanyID = sth.CompanyID
		and [SET_DocumentType].PeriodID = sth.PeriodID
		and [SET_DocumentType].menuID in (select MenuID from [ApplicationMenu] 
		where MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))
        and [SET_DocumentType].DocumentType not like '%CPR%'
		and [SET_DocumentType].Status = 1
		
left join INV_StockTransactionHeader GIN on GIN.StockTransactionID = std.SourceHeaderID
  and GIN.DocumentTypeID = std.SourceDocumentTypeID
and GIN.CompanyID = std.CompanyID
		and GIN.PeriodID = std.PeriodID
		and GIN.FiscalID= std.FiscalID

      Left Join Set_Job on Set_Job.JobID =std.JobID
and Set_Job.CompanyID = std.CompanyID

   INNER JOIN INV_ITem On INV_ITem.ItemID = std.ITemID 
--AND INV_ITem.CompanyID = std.CompanyID 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = sth.CostCenterID 
	LEFT JOIN SET_CostCenter returncostcenter ON returncostcenter.CostCenterID = GIN.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = sth.DepartmentID 
	LEFT JOIN SET_Department returndept ON returndept.DepartmentID = GIN.DepartmentID 
  INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
  LEFT JOIN GL_CodeCombinationDetail ON Inv_Item.ExpenseAccountCCID = GL_CodeCombinationDetail.CodeCombinationID AND GL_CodeCombinationDetail.Status = 1
  --  
WHERE sth.CompanyID = @CompanyID AND sth.PeriodID = @PeriodID AND sth.Status = 1 AND sth.StatusCode = 1  
{0}
) as tab
where tab.ItemID is not null
{1}

";


        const string Query_GetSourceHeaderID = @"
SELECT ISNULL(StockTransactionID, 0) AS StockTransactionID
FROM INV_StockTransactionHeader 
    
WHERE 
	CompanyID = @CompanyID
	and PeriodID = @PeriodID
	and DocumentTypeID = @DocumentTypeID
	and DocumentNo = @DocumentNo
        ";


        const string Query_Update_TransactionStatusForPO = @"
                      update [INV_StockTransactionHeader]
                      set StatusCode = 0
                      where StockTransactionID = @StockHeaderID
                      And [CompanyID] = @CompanyID
                      And [PeriodID] = @PeriodID
";

        const string Query_Update_TransactionStatusForGRN = @"
                      update INV_StockTransactionDetail
	                set IsDone = 1
	                where CompanyID = @CompanyID
	                and PeriodID = @PeriodID
	                and StockTransactionID = @StockHeaderID
	                and TransactionDetailID = @StockDetailID
";


        const string Query_TotalQuantityOfInvoice = @"
	select isnull(sum(TransactionQuantity),0) as TotalQuantity
	from INV_StockTransactionDetail 
	where StockTransactionID = 	@StockHeaderID
	and INV_StockTransactionDetail.CompanyID = @CompanyID
	and INV_StockTransactionDetail.PeriodID = @PeriodID
	AND (IsDone <> 1 OR IsDone IS NULL) ";

        const string Query_BindDocumentTypeDDL = @"select DISTINCT SET_DocumentType.DocumentTypeID, SET_DocumentType.DocumentType from [SET_DocumentType]
	Inner JOin ApplicationMenu ON ApplicationMenu.MenuID = SET_DocumentType.MenuID 
where ApplicationMenu.MenuPath = @MenuPath And Set_DocumentType.CompanyID = @CompanyID 
	And Set_DocumentType.PeriodID = @PeriodID And Set_DocumentType.Status = 1 And Set_DocumentType.IsActive = 1
";
        const string Query_Update_By_DDLValues = @"
UPDATE INV_StockTransactionHeader
SET StatusCOde = @StatusCode,IsReversed=1,ReversalDate=GETDATE()
where INV_StockTransactionHeader.DocumentTypeID=@DocumentTypeID AND INV_StockTransactionHeader.StockTransactionID=@StockTransactionID
AND INV_StockTransactionHeader.CompanyID = @CompanyID  AND INV_StockTransactionHeader.PeriodID = @PeriodID";

        const string Query_Update_By_StockTransactionID = @"
UPDATE INV_StockTransactionHeader
SET IsReversed=1,ReversalDate=GETDATE(), UpdateDate = @UpdateDate, UpdatedBy = @UpdateBy, UpdatedByValue = @UpdateByValue
where INV_StockTransactionHeader.StockTransactionID=@StockTransactionID
AND INV_StockTransactionHeader.CompanyID = @CompanyID  AND INV_StockTransactionHeader.PeriodID = @PeriodID";

        const string Query_RequisitionClose = @"
UPDATE INV_StockTransactionHeader
SET IsRequisitionCompleted = 1 , Comments =  @remarks
where INV_StockTransactionHeader.DocumentTypeID=@DocumentTypeID AND INV_StockTransactionHeader.StockTransactionID=@StockTransactionID
AND INV_StockTransactionHeader.CompanyID = @CompanyID  AND INV_StockTransactionHeader.PeriodID = @PeriodID";

        const string Query_POClose = @"
UPDATE AP_PurchaseOrderHeader
SET ClosedCode = 1 , ClosedDate = GETDATE() , Comments =  @remarks
where AP_PurchaseOrderHeader.TypeCode=@DocumentTypeID AND AP_PurchaseOrderHeader.POHeaderID=@StockTransactionID
AND AP_PurchaseOrderHeader.CompanyID = @CompanyID  AND AP_PurchaseOrderHeader.PeriodID = @PeriodID

UPDATE AP_PurchaseOrderDetail
SET ClosedCode = 1 , ClosedDate = GETDATE() ,ClosedReason = @remarks
where AP_PurchaseOrderDetail.POHeaderID=@StockTransactionID
AND AP_PurchaseOrderDetail.CompanyID = @CompanyID  AND AP_PurchaseOrderDetail.PeriodID = @PeriodID";

        const string Query_GetDataByDocumentType = @"select * from INV_StockTransactionHeader where CompanyID=@CompanyID AND PeriodID=@PeriodID AND DocumentTypeID=@DocumentTypeID AND DocumentNo=@DocumentNo and FiscalID = @FiscalID";

        const string Query_Select_StockRequisition_LookupPR = @"
SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, SET_CostCenter.CostCenterName , '' as Item , INV_StockTransactionHeader.Comments --,INV_StockTransactionDetail.Isdone
    
FROM INV_StockTransactionHeader
    inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1  
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID 
    And ApplicationMenu.MenuPath = '/Inventory/Transaction/StockRequistion_frm.aspx'
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 
   left join (Select ISNUll(SourceHeaderID,0) as SourceHeaderID,Isdone  from INV_StockTransactionDetail
    where ISdone = 1 and (TransactionQuantity * -1) <> PrimaryQuantity 
    ) Gt on gt.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
     
WHERE INV_StockTransactionHeader.CompanyID = 1 AND INV_StockTransactionHeader.PeriodID = 1 AND INV_StockTransactionHeader.Status = 1 
    AND INV_StockTransactionHeader.StatusCode = 1 
    AND INV_StockTransactionHeader.StockTransactionID NOT IN 
    (
    Select ISNUll(SourceHeaderID,0)--,Set_DocumentType.Documenttype
      from INV_StockTransactionDetail
     inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail. StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1  
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
     WHERE Set_DocumentType.DocumentType like '%PR')
     
      {filter}
    
";
        const string AllDocumentsForApproval = @"SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(FinTab.DocumentNo as int) desc) AS 'Row' 
FROM 
(
---------------Sales Module -------------
----------Sale Order-------------
	Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	SOH.OrderID as OrderID,	SOH.ContractNo as DocumentNo,
	SOH.CreateDate as DocumentDate,(case when SOH.StatusCode =12 then 'ReleaseForApprove' when SOH.StatusCode =13 then 'PartiallyApproved'
when SOH.StatusCode =14 then 'Discuss' when SOH.StatusCode =15 then 'Cancelled'
when SOH.StatusCode =16 then 'Reversed' when SOH.StatusCode =17 then 'NoOrder'
when SOH.StatusCode =5 then 'InProcess' when SOH.StatusCode =1 then 'Approved'
 end) as StatusCode, SOH.CreatedByValue, SOH.Status, SOH.FiscalID, SOH.CompanyID, SOH.PeriodID, SOH.SaleContractTypeID as DocumentTypeID,
 SOH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
from SAL_OrderHeader SOH
	    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SOH.CompanyID AND SET_DocumentApprove.PeriodID = SOH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = SOH.SaleContractTypeID 
            AND SET_DocumentApprove.DocumentNo = SOH.ContractNo 
	        AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = SOH.CompanyID AND PeriodID = SOH.PeriodID AND DocumentTypeID = SOH.SaleContractTypeID 
                    AND DocumentNo = SOH.ContractNo
            )
            inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
            
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  SOH.SaleContractTypeID
            and SET_Documenttype.CompanyID =  SOH.CompanyID
            and SET_Documenttype.periodId =  SOH.periodId

        	inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID

        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SOH.CompanyID AND SET_UserApprovalAmount.PeriodID = SOH.PeriodID 
            AND SET_UserApprovalAmount.Status = 1 
            AND (SET_UserApprovalAmount.DocumentTypeID = SOH.SaleContractTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
            AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
                        WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
                            AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND 
                                (
                                    SELECT soh.TotalAmount FROM SAL_OrderHeader soh 
                                    WHERE soh.CompanyID = SOH.CompanyID AND soh.PeriodID = SOH.PeriodID AND soh.OrderID = SOH.OrderID
                                ) >= SET_UserApprovalAmount.AmountFrom THEN 1 
                            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND 
                                (
                                    SELECT soh.TotalAmount FROM SAL_OrderHeader soh 
                                    WHERE soh.CompanyID = SOH.CompanyID AND soh.PeriodID = SOH.PeriodID AND soh.OrderID = SOH.OrderID
                                ) BETWEEN SET_UserApprovalAmount.AmountFrom AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 

            left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID
			
			inner join Set_PeriodCustom on  SOH.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
			

group by SOH.OrderID, SOH.CompanyID, SOH.PeriodID, SOH.SaleContractTypeID, SOH.CustomerID, SOH.BrokerID, 
        SOH.PaymentTermID, SOH.Season, SOH.ContractNo, SOH.ContractDate, SOH.PaymentDate, SOH.DispatchDate, SOH.DTRENo, 
        SOH.DTREDate, SOH.IsApproved, SOH.ApprovedBy, SOH.ApprovalDate, SOH.Remarks, SOH.IsPaid, SOH.PaidAmount, 
        SOH.StatusCode, SOH.Status, SOH.CreatedBy, SOH.CreateDate, SOH.UpdatedBy, SOH.UpdateDate, SOH.CreatedByValue, 
        SOH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, 
        Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName
        ,Set_documentPendingApprove.ApprovalUserID,SOH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
		----------Delivery Order-------------
		Union All
			Select SET_PeriodCustom.PeriodSetName,
	SET_DocumentType.DocumentType ,
	DOH.DeliveryOrderID as OrderID,
	DOH.DeliveryOrderNo as DocumentNo,
	DOH.CreateDate as DocumentDate,
(case when DOH.StatusCode =12 then 'ReleaseForApprove' when DOH.StatusCode =13 then 'PartiallyApproved'
when DOH.StatusCode =14 then 'Discuss' when DOH.StatusCode =15 then 'Cancelled'
when DOH.StatusCode =16 then 'Reversed' when DOH.StatusCode =17 then 'NoOrder'
when DOH.StatusCode =5 then 'InProcess' when DOH.StatusCode =1 then 'Approved'
 end) as StatusCode, DOH.CreatedByValue, DOH.Status, DOH.FiscalID, DOH.CompanyID, DOH.PeriodID,
 DOH.DocumentTypeID as DocumentTypeID,
 DOH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from SAL_DeliveryOrderHeader DOH
	    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = DOH.CompanyID AND SET_DocumentApprove.PeriodID = DOH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = DOH.DocumentTypeID 
            AND SET_DocumentApprove.DocumentNo = DOH.DeliveryOrderNo 
	        AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = DOH.CompanyID AND PeriodID = DOH.PeriodID AND DocumentTypeID = DOH.DocumentTypeID 
                    AND DocumentNo = DOH.DeliveryOrderNo
            )
            inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
            
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  DOH.DocumentTypeID
            and SET_Documenttype.CompanyID =  DOH.CompanyID
            and SET_Documenttype.periodId =  DOH.periodId
        
		inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID

        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = DOH.CompanyID AND SET_UserApprovalAmount.PeriodID = DOH.PeriodID 
            AND SET_UserApprovalAmount.Status = 1 
            AND (SET_UserApprovalAmount.DocumentTypeID = DOH.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
            AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
                        WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
                            AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND 
                                (
                                    SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
                                    WHERE doh.CompanyID = DOH.CompanyID AND doh.PeriodID = DOH.PeriodID AND doh.DeliveryOrderID = DOH.DeliveryOrderID
                                ) >= SET_UserApprovalAmount.AmountFrom THEN 1 
                            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND 
                                (
                                    SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh
                                    WHERE doh.CompanyID = DOH.CompanyID AND doh.PeriodID = DOH.PeriodID AND doh.DeliveryOrderID= DOH.DeliveryOrderID
                                ) BETWEEN SET_UserApprovalAmount.AmountFrom AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 

            left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID
			
			inner join Set_PeriodCustom on  DOH.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
			

group by DOH.DeliveryOrderID, DOH.CompanyID, DOH.PeriodID, DOH.DocumentTypeID, DOH.CustomerID, DOH.DeliveryOrderNo, 
		DOH.DODate, DOH.StatusCode, DOH.Status, DOH.CreatedBy, DOH.CreateDate, DOH.UpdatedBy, DOH.UpdateDate, DOH.CreatedByValue, 
        DOH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, 
        Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName
        ,Set_documentPendingApprove.ApprovalUserID,DOH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName

		-------------Dispatch order------------------

				Union All
			Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	DISH.DispatchID as OrderID,	DISH.DispatchNo as DocumentNo,
	DISH.CreateDate as DocumentDate,(case when DISH.StatusCode =12 then 'ReleaseForApprove' when DISH.StatusCode =13 then 'PartiallyApproved'
when DISH.StatusCode =14 then 'Discuss' when DISH.StatusCode =15 then 'Cancelled'when DISH.StatusCode =16 then 'Reversed' when DISH.StatusCode =17 then 'NoOrder'
when DISH.StatusCode =5 then 'InProcess' when DISH.StatusCode =1 then 'Approved' end) as StatusCode, DISH.CreatedByValue, 
DISH.Status, DISH.FiscalID, DISH.CompanyID, DISH.PeriodID, DISH.DocumentTypeID as DocumentTypeID, 
DISH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from SAL_DispatchHeader DISH
	    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = DISH.CompanyID 
		AND SET_DocumentApprove.PeriodID = DISH.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = DISH.DocumentTypeID 
            AND SET_DocumentApprove.DocumentNo = DISH.DispatchNo 
	        AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = DISH.CompanyID AND PeriodID = DISH.PeriodID AND DocumentTypeID = DISH.DocumentTypeID 
                    AND DocumentNo = DISH.DispatchNo
            )
            inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
            
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  DISH.DocumentTypeID
            and SET_Documenttype.CompanyID =  DISH.CompanyID
            and SET_Documenttype.periodId =  DISH.periodId
        		inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = DISH.CompanyID AND SET_UserApprovalAmount.PeriodID = DISH.PeriodID 
            AND SET_UserApprovalAmount.Status = 1 
            AND (SET_UserApprovalAmount.DocumentTypeID = DISH.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
            AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
                        WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
                            AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND 
                                (
                                    SELECT DH.TotalAmount FROM SAL_DispatchHeader DH 
                                    WHERE DH.CompanyID = DISH.CompanyID AND DH.PeriodID = DISH.PeriodID
									 AND DH.DispatchID = DISH.DispatchID
                                ) >= SET_UserApprovalAmount.AmountFrom THEN 1 
                            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND 
                                (
                                    SELECT DH.TotalAmount FROM SAL_DispatchHeader DH
                                    WHERE DH.CompanyID = DISH.CompanyID 
									AND DH.PeriodID = DISH.PeriodID
									 AND DH.DispatchID= DISH.DispatchID
                                ) BETWEEN SET_UserApprovalAmount.AmountFrom AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 

            left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID
			
			inner join Set_PeriodCustom on  DISH.DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
			

group by DISH.DispatchID, DISH.CompanyID, DISH.PeriodID, DISH.DocumentTypeID, DISH.CustomerID, DISH.DispatchNo, 
		DISH.DispatchDate, DISH.StatusCode, DISH.Status, DISH.CreatedBy, DISH.CreateDate, DISH.UpdatedBy, DISH.UpdateDate,
		 DISH.CreatedByValue, DISH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,
		DISH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName

				-------------Receipt------------------

				Union All
			Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	ReceiptHeader.ReceiptTransactionID as OrderID,	
			ReceiptHeader.ReceiptNo as DocumentNo,
	ReceiptHeader.CreateDate as DocumentDate,(case when ReceiptHeader.StatusCode =12 then 'ReleaseForApprove' when ReceiptHeader.StatusCode =13 then 'PartiallyApproved'
when ReceiptHeader.StatusCode =14 then 'Discuss' when ReceiptHeader.StatusCode =15 then 'Cancelled'when ReceiptHeader.StatusCode =16 then 'Reversed' when ReceiptHeader.StatusCode =17 then 'NoOrder'
when ReceiptHeader.StatusCode =5 then 'InProcess' when ReceiptHeader.StatusCode =1 then 'Approved' end) as StatusCode, ReceiptHeader.CreatedByValue, 
ReceiptHeader.Status, ReceiptHeader.FiscalID, ReceiptHeader.CompanyID, ReceiptHeader.PeriodID,
 ReceiptHeader.DocumentTypeID as DocumentTypeID, ReceiptHeader.CreatedBy,
  Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from AR_ReceiptTransactionHeader ReceiptHeader
	    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ReceiptHeader.CompanyID 
		AND SET_DocumentApprove.PeriodID = ReceiptHeader.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = ReceiptHeader.DocumentTypeID 
            AND SET_DocumentApprove.DocumentNo = ReceiptHeader.ReceiptNo
	        AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = ReceiptHeader.CompanyID AND PeriodID = ReceiptHeader.PeriodID AND DocumentTypeID = ReceiptHeader.DocumentTypeID 
                    AND DocumentNo = ReceiptHeader.ReceiptNo
            )
            inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
            
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  ReceiptHeader.DocumentTypeID
            and SET_Documenttype.CompanyID =  ReceiptHeader.CompanyID
            and SET_Documenttype.periodId =  ReceiptHeader.periodId
        		inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = ReceiptHeader.CompanyID 
		AND SET_UserApprovalAmount.PeriodID = ReceiptHeader.PeriodID 
            AND SET_UserApprovalAmount.Status = 1 
            AND (SET_UserApprovalAmount.DocumentTypeID = ReceiptHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
            AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
                        WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                            AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
                            AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND 
                                (
                                    SELECT Receipt.Amount FROM AR_ReceiptTransactionHeader Receipt
                                    WHERE Receipt.CompanyID = ReceiptHeader.CompanyID 
									AND Receipt.PeriodID = ReceiptHeader.PeriodID
									 AND Receipt.ReceiptTransactionID = ReceiptHeader.ReceiptTransactionID
                                ) >= SET_UserApprovalAmount.AmountFrom THEN 1 
                            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND 
                                (
                                    SELECT Receipt.Amount FROM AR_ReceiptTransactionHeader Receipt
                                    WHERE ReceiptHeader.CompanyID = Receipt.CompanyID 
									AND ReceiptHeader.PeriodID = Receipt.PeriodID
									 AND ReceiptHeader.ReceiptTransactionID= Receipt.ReceiptTransactionID
                                ) BETWEEN SET_UserApprovalAmount.AmountFrom AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 

            left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID
			
			inner join Set_PeriodCustom on  ReceiptHeader.ReceiptDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
			

group by ReceiptHeader.ReceiptTransactionID, ReceiptHeader.CompanyID, ReceiptHeader.PeriodID, ReceiptHeader.DocumentTypeID, ReceiptHeader.CustomerID, ReceiptHeader.ReceiptNo, 
		ReceiptHeader.ReceiptDate, ReceiptHeader.StatusCode, ReceiptHeader.Status, ReceiptHeader.CreatedBy, ReceiptHeader.CreateDate, ReceiptHeader.UpdatedBy, ReceiptHeader.UpdateDate,
		 ReceiptHeader.CreatedByValue, ReceiptHeader.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,
		ReceiptHeader.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName

-------------Sale InvoiceHeader------------------

				Union All
			Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	SIH.SalesInvoiceHeaderID as OrderID,	
			SIH.InvoiceNumber as DocumentNo,
	SIH.CreateDate as DocumentDate,(case when SIH.StatusCode =12 then 'ReleaseForApprove' when SIH.StatusCode =13 then 'PartiallyApproved'
when SIH.StatusCode =14 then 'Discuss' when SIH.StatusCode =15 then 'Cancelled'when SIH.StatusCode =16 then 'Reversed' when SIH.StatusCode =17 then 'NoOrder'
when SIH.StatusCode =5 then 'InProcess' when SIH.StatusCode =1 then 'Approved' end) as StatusCode, SIH.CreatedByValue, 
SIH.Status, SIH.FiscalID, SIH.CompanyID, SIH.PeriodID, SIH.InvoiceType as DocumentTypeID,
 SIH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID
 ,SET_Documenttype.MenuID,ApplicationMenu.MenuName from SAL_InvoiceHeader SIH
		INNER JOIN AR_SET_Customer ON SIH.CustomerID  = AR_SET_Customer.CustomerID 
			AND SIH.CompanyID = AR_SET_Customer.CompanyID AND SIH.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1
		INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
			AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SIH.CompanyID
			AND SET_DocumentApprove.PeriodID = SIH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SIH.InvoiceType
			AND SET_DocumentApprove.DocumentNo = SIH.InvoiceNumber 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SIH.CompanyID
				AND PeriodID = SIH.PeriodID AND DocumentTypeID = SIH.InvoiceType 
				AND DocumentNo = SIH.InvoiceNumber)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  SIH.InvoiceType
            and SET_Documenttype.CompanyID =  SIH.CompanyID
            and SET_Documenttype.periodId =  SIH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SIH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = SIH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = SIH.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
													WHERE ih.CompanyID = SIH.CompanyID 
														AND ih.PeriodID = SIH.PeriodID 
														AND ih.InvoiceID = SIH.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
															WHERE ih.CompanyID = SIH.CompanyID 
																AND ih.PeriodID = SIH.PeriodID 
																AND ih.InvoiceID = SIH.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 SIH.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by SIH.InvoiceID, SIH.CompanyID, SIH.PeriodID, SIH.InvoiceType, SIH.CustomerID, SIH.InvoiceNumber, SIH.SalesInvoiceHeaderID,
		SIH.InvoiceDate, SIH.StatusCode, SIH.Status, SIH.CreatedBy, SIH.CreateDate, SIH.UpdatedBy, SIH.UpdateDate,
		 SIH.CreatedByValue, SIH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,
		SIH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName

-------------Purchase InvoiceHeader------------------

				Union All
			Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	ARIH.InvoiceID as OrderID,	
			ARIH.InvoiceNumber as DocumentNo,
	ARIH.CreateDate as DocumentDate,(case when ARIH.StatusCode =12 then 'ReleaseForApprove' when ARIH.StatusCode =13 then 'PartiallyApproved'
when ARIH.StatusCode =14 then 'Discuss' when ARIH.StatusCode =15 then 'Cancelled'when ARIH.StatusCode =16 then 'Reversed' when ARIH.StatusCode =17 then 'NoOrder'
when ARIH.StatusCode =5 then 'InProcess' when ARIH.StatusCode =1 then 'Approved' end) as StatusCode, ARIH.CreatedByValue, 
ARIH.Status, ARIH.FiscalID, ARIH.CompanyID, ARIH.PeriodID, ARIH.InvoiceType as DocumentTypeID,
 ARIH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from AR_InvoiceHeader ARIH
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ARIH.CompanyID
			AND SET_DocumentApprove.PeriodID = ARIH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = ARIH.InvoiceType
			AND SET_DocumentApprove.DocumentNo = ARIH.InvoiceNumber 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = ARIH.CompanyID
				AND PeriodID = ARIH.PeriodID AND DocumentTypeID = ARIH.InvoiceType 
				AND DocumentNo = ARIH.InvoiceNumber)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  ARIH.InvoiceType
            and SET_Documenttype.CompanyID =  ARIH.CompanyID
            and SET_Documenttype.periodId =  ARIH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = ARIH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = ARIH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = ARIH.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih 
													WHERE ih.CompanyID = ARIH.CompanyID 
														AND ih.PeriodID = ARIH.PeriodID 
														AND ih.InvoiceID = ARIH.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih 
															WHERE ih.CompanyID = ARIH.CompanyID 
																AND ih.PeriodID = ARIH.PeriodID 
																AND ih.InvoiceID = ARIH.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 ARIH.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by ARIH.InvoiceID, ARIH.CompanyID, ARIH.PeriodID, ARIH.InvoiceType, ARIH.InvoiceNumber, 
		ARIH.InvoiceDate, ARIH.StatusCode, ARIH.Status, ARIH.CreatedBy, ARIH.CreateDate, ARIH.UpdatedBy, ARIH.UpdateDate,
		 ARIH.CreatedByValue, ARIH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,
		Set_documentPendingApprove.ApprovalUserID,ARIH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
-------------Payment Header------------------
Union All
Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	APPH.PaymentID as OrderID,	
			APPH.DocumentNo as DocumentNo,
	APPH.CreateDate as DocumentDate,(case when APPH.StatusCode =12 then 'ReleaseForApprove' when APPH.StatusCode =13 then 'PartiallyApproved'
when APPH.StatusCode =14 then 'Discuss' when APPH.StatusCode =15 then 'Cancelled'when APPH.StatusCode =16 then 'Reversed' when APPH.StatusCode =17 then 'NoOrder'
when APPH.StatusCode =5 then 'InProcess' when APPH.StatusCode =1 then 'Approved' end) as StatusCode, APPH.CreatedByValue, 
APPH.Status, APPH.FiscalID, APPH.CompanyID, APPH.PeriodID, APPH.DocumentTypeID as DocumentTypeID, 
APPH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from AP_PaymentHeader APPH
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = APPH.CompanyID
			AND SET_DocumentApprove.PeriodID = APPH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = APPH.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = APPH.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = APPH.CompanyID
				AND PeriodID = APPH.PeriodID AND DocumentTypeID = APPH.DocumentTypeID 
				AND DocumentNo = APPH.DocumentNo)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  APPH.DocumentTypeID
            and SET_Documenttype.CompanyID =  APPH.CompanyID
            and SET_Documenttype.periodId =  APPH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = APPH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = APPH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = APPH.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.Amount FROM AP_PaymentHeader ih 
													WHERE ih.CompanyID = APPH.CompanyID 
														AND ih.PeriodID = APPH.PeriodID 
														AND ih.PaymentID = APPH.PaymentID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.Amount FROM AP_PaymentHeader ih 
															WHERE ih.CompanyID = APPH.CompanyID 
																AND ih.PeriodID = APPH.PeriodID 
																AND ih.PaymentID = APPH.PaymentID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 APPH.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by APPH.PaymentID, APPH.CompanyID, APPH.PeriodID, APPH.DocumentTypeID, APPH.DocumentNo, 
		APPH.DocumentDate, APPH.StatusCode, APPH.Status, APPH.CreatedBy, APPH.CreateDate, APPH.UpdatedBy, APPH.UpdateDate,
		 APPH.CreatedByValue, APPH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,
		APPH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
-------------INV_StockTransactionHeader----------------
Union All
Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	INVSTH.StockTransactionID as OrderID,	
			INVSTH.DocumentNo as DocumentNo,
	INVSTH.CreateDate as DocumentDate,(case when INVSTH.StatusCode =12 then 'ReleaseForApprove' when INVSTH.StatusCode =13 then 'PartiallyApproved'
when INVSTH.StatusCode =14 then 'Discuss' when INVSTH.StatusCode =15 then 'Cancelled'when INVSTH.StatusCode =16 then 'Reversed' when INVSTH.StatusCode =17 then 'NoOrder'
when INVSTH.StatusCode =5 then 'InProcess' when INVSTH.StatusCode =1 then 'Approved' end) as StatusCode, INVSTH.CreatedByValue, 
INVSTH.Status, INVSTH.FiscalID, INVSTH.CompanyID, INVSTH.PeriodID, INVSTH.DocumentTypeID as DocumentTypeID,
 INVSTH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID
,SET_Documenttype.MenuID,ApplicationMenu.MenuName  from INV_StockTransactionHeader INVSTH
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = INVSTH.CompanyID
			AND SET_DocumentApprove.PeriodID = INVSTH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = INVSTH.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = INVSTH.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = INVSTH.CompanyID
				AND PeriodID = INVSTH.PeriodID AND DocumentTypeID = INVSTH.DocumentTypeID 
				AND DocumentNo = INVSTH.DocumentNo)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  INVSTH.DocumentTypeID
            and SET_Documenttype.CompanyID =  INVSTH.CompanyID
            and SET_Documenttype.periodId =  INVSTH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = INVSTH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = INVSTH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = INVSTH.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.TotalAmount FROM INV_StockTransactionHeader ih 
													WHERE ih.CompanyID = INVSTH.CompanyID 
														AND ih.PeriodID = INVSTH.PeriodID 
														AND ih.StockTransactionID = INVSTH.StockTransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.TotalAmount FROM INV_StockTransactionHeader ih 
															WHERE ih.CompanyID = INVSTH.CompanyID 
																AND ih.PeriodID = INVSTH.PeriodID 
																AND ih.StockTransactionID = INVSTH.StockTransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 INVSTH.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by INVSTH.StockTransactionID, INVSTH.CompanyID, INVSTH.PeriodID, INVSTH.DocumentTypeID, INVSTH.DocumentNo, 
		INVSTH.DocumentDate, INVSTH.StatusCode, INVSTH.Status, INVSTH.CreatedBy, INVSTH.CreateDate, INVSTH.UpdatedBy, INVSTH.UpdateDate,
		 INVSTH.CreatedByValue, INVSTH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID
		,INVSTH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
---------------Purchase order header-------------
Union All
Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	APPOH.POHeaderID as OrderID,	
			APPOH.DocumentNumber as DocumentNo,
	APPOH.CreateDate as DocumentDate,(case when APPOH.StatusCode =12 then 'ReleaseForApprove' when APPOH.StatusCode =13 then 'PartiallyApproved'
when APPOH.StatusCode =14 then 'Discuss' when APPOH.StatusCode =15 then 'Cancelled'when APPOH.StatusCode =16 then 'Reversed' when APPOH.StatusCode =17 then 'NoOrder'
when APPOH.StatusCode =5 then 'InProcess' when APPOH.StatusCode =1 then 'Approved' end) as StatusCode, APPOH.CreatedByValue, 
APPOH.Status, APPOH.FiscalID, APPOH.CompanyID, APPOH.PeriodID, APPOH.TypeCode as DocumentTypeID, APPOH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,
SET_Documenttype.MenuID,ApplicationMenu.MenuName
  from AP_PurchaseOrderHeader APPOH
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = APPOH.CompanyID
			AND SET_DocumentApprove.PeriodID = APPOH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = APPOH.TypeCode
			AND SET_DocumentApprove.DocumentNo = APPOH.DocumentNumber 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = APPOH.CompanyID
				AND PeriodID = APPOH.PeriodID AND DocumentTypeID = APPOH.TypeCode 
				AND DocumentNo = APPOH.DocumentNumber)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  APPOH.TypeCode
            and SET_Documenttype.CompanyID =  APPOH.CompanyID
            and SET_Documenttype.periodId =  APPOH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = APPOH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = APPOH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = APPOH.TypeCode OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.TotalAmount FROM AP_PurchaseOrderHeader ih 
													WHERE ih.CompanyID = APPOH.CompanyID 
														AND ih.PeriodID = APPOH.PeriodID 
														AND ih.POHeaderID = APPOH.POHeaderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.TotalAmount FROM AP_PurchaseOrderHeader ih 
															WHERE ih.CompanyID = APPOH.CompanyID 
																AND ih.PeriodID = APPOH.PeriodID 
																AND ih.POHeaderID = APPOH.POHeaderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 APPOH.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by APPOH.POHeaderID, APPOH.CompanyID, APPOH.PeriodID, APPOH.TypeCode, APPOH.DocumentNumber, 
		APPOH.DocumentDate, APPOH.StatusCode, APPOH.Status, APPOH.CreatedBy, APPOH.CreateDate, APPOH.UpdatedBy, APPOH.UpdateDate,
		 APPOH.CreatedByValue, APPOH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,APPOH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
---------------RFQ-------------
Union All
Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	RFQ.EnquiryID as OrderID,	
			RFQ.DocumentNo as DocumentNo,
	RFQ.CreateDate as DocumentDate,(case when RFQ.StatusCode =12 then 'ReleaseForApprove' when RFQ.StatusCode =13 then 'PartiallyApproved'
when RFQ.StatusCode =14 then 'Discuss' when RFQ.StatusCode =15 then 'Cancelled'when RFQ.StatusCode =16 then 'Reversed' when RFQ.StatusCode =17 then 'NoOrder'
when RFQ.StatusCode =5 then 'InProcess' when RFQ.StatusCode =1 then 'Approved' end) as StatusCode, RFQ.CreatedByValue, 
RFQ.Status, RFQ.FiscalID, RFQ.CompanyID, RFQ.PeriodID, RFQ.DocumentTypeID as DocumentTypeID, RFQ.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID
,ApplicationMenu.MenuName
  from PUR_EnquiryHeader RFQ
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = RFQ.CompanyID
			AND SET_DocumentApprove.PeriodID = RFQ.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = RFQ.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = RFQ.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = RFQ.CompanyID
				AND PeriodID = RFQ.PeriodID AND DocumentTypeID = RFQ.DocumentTypeID 
				AND DocumentNo = RFQ.DocumentNo)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  RFQ.DocumentTypeID
            and SET_Documenttype.CompanyID =  RFQ.CompanyID
            and SET_Documenttype.periodId =  RFQ.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = RFQ.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = RFQ.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = RFQ.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.TotalAmount FROM PUR_EnquiryHeader ih 
													WHERE ih.CompanyID = RFQ.CompanyID 
														AND ih.PeriodID = RFQ.PeriodID 
														AND ih.EnquiryID = RFQ.EnquiryID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.TotalAmount FROM PUR_EnquiryHeader ih 
															WHERE ih.CompanyID = RFQ.CompanyID 
																AND ih.PeriodID = RFQ.PeriodID 
																AND ih.EnquiryID = RFQ.EnquiryID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 RFQ.EnquiryDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by RFQ.EnquiryID, RFQ.CompanyID, RFQ.PeriodID, RFQ.DocumentTypeID, RFQ.DocumentNo, 
		RFQ.EnquiryDate, RFQ.StatusCode, RFQ.Status, RFQ.CreatedBy, RFQ.CreateDate, RFQ.UpdatedBy, RFQ.UpdateDate,
		 RFQ.CreatedByValue, RFQ.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,RFQ.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName
---------------GL_TransactionHeader-------------
Union All
Select SET_PeriodCustom.PeriodSetName,	SET_DocumentType.DocumentType ,	GLH.TransactionID as OrderID,	
			GLH.DocumentNo as DocumentNo,
	GLH.CreateDate as DocumentDate,(case when GLH.StatusCode =12 then 'ReleaseForApprove' when GLH.StatusCode =13 then 'PartiallyApproved'
when GLH.StatusCode =14 then 'Discuss' when GLH.StatusCode =15 then 'Cancelled'when GLH.StatusCode =16 then 'Reversed' when GLH.StatusCode =17 then 'NoOrder'
when GLH.StatusCode =5 then 'InProcess' when GLH.StatusCode =1 then 'Approved' end) as StatusCode, GLH.CreatedByValue, 
GLH.Status, GLH.FiscalID, GLH.CompanyID, GLH.PeriodID, GLH.DocumentTypeID as DocumentTypeID, GLH.CreatedBy, Set_documentPendingApprove.ApprovalUserID as UserID,SET_Documenttype.MenuID
,ApplicationMenu.MenuName
  from GL_TransactionHeader GLH
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = GLH.CompanyID
			AND SET_DocumentApprove.PeriodID = GLH.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = GLH.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = GLH.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = GLH.CompanyID
				AND PeriodID = GLH.PeriodID AND DocumentTypeID = GLH.DocumentTypeID 
				AND DocumentNo = GLH.DocumentNo)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  GLH.DocumentTypeID
            and SET_Documenttype.CompanyID =  GLH.CompanyID
            and SET_Documenttype.periodId =  GLH.periodId
					inner Join ApplicationMenu on ApplicationMenu.MenuID=SET_Documenttype.MenuID
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = GLH.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = GLH.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = GLH.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.ControlDebit FROM GL_TransactionHeader ih 
													WHERE ih.CompanyID = GLH.CompanyID 
														AND ih.PeriodID = GLH.PeriodID 
														AND ih.TransactionID = GLH.TransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.ControlDebit FROM GL_TransactionHeader ih 
															WHERE ih.CompanyID = GLH.CompanyID 
																AND ih.PeriodID = GLH.PeriodID 
																AND ih.TransactionID = GLH.TransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 GLH.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by GLH.TransactionID, GLH.CompanyID, GLH.PeriodID, GLH.DocumentTypeID, GLH.DocumentNo, 
		GLH.DocumentDate, GLH.StatusCode, GLH.Status, GLH.CreatedBy, GLH.CreateDate, GLH.UpdatedBy, GLH.UpdateDate,
		 GLH.CreatedByValue, GLH.UpdatedByValue, SET_DocumentApprove.ApprovalStatus, Set_documentPendingApprove.ApprovalOperator
        ,SET_Documenttype.DocumentType,SET_PeriodCustom.PeriodSetName,Set_documentPendingApprove.ApprovalUserID,
		GLH.FiscalID,SET_Documenttype.MenuID,ApplicationMenu.MenuName


	)as FinTab

		 WHERE FinTab.[Status] = 1
		 AND FinTab.[StatusCode] not in ('Approved','inProcess')
";

        const string Query_SelectItemWiseGRM = @" 
SELECT        INV_Item.ApprovalItemGroupID,SET_ApprovalItemGroup.GroupName,INV_StockTransactionHeader.StockTransactionID,dbo.INV_Item.ItemID, dbo.INV_Item.Description, dbo.INV_StockTransactionHeader.DocumentDate,
                        dbo.INV_StockTransactionHeader.DocumentTypeID,SET_DocumentType.DocumentType,  dbo.INV_StockTransactionHeader.DocumentNo, dbo.INV_StockTransactionHeader.FiscalID, (dbo.INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity,
                        (dbo.INV_StockTransactionDetail.TransactionQuantity * dbo.INV_StockTransactionDetail.TransactionCost) AS TransactionAmount,INV_StockTransactionDetail.TransactionCost,
                         (
select top 1  case when AP_PurchaseOrderDetail.taxAmount > 0 then (AP_PurchaseOrderDetail.taxAmount / (AP_PurchaseOrderDetail.NetAmount - isnull(AP_PurchaseOrderDetail.taxAmount,0))) * 100 else 0 end  from AP_PurchaseOrderDetail inner join AP_PurchaseOrderHeader
                         on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
                         where AP_PurchaseOrderHeader.DocumentNumber = INV_StockTransactionDetail.SourceCode
                         and AP_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID
                         and AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID
                         and AP_PurchaseOrderDetail.ItemID = INV_StockTransactionDetail.ItemID
                         ) As TaxPer
						 ,INV_StockTransactionHeader.BusinessPartnerID
						 ,ar_set_customer.CustomerName
						 ,INV_Item.InventoryItemStatusCode as ItemCode
						 , SET_ItemCatalog.Description as CatalogName,
						 INV_SET_Category.CategoryName
FROM            dbo.INV_StockTransactionDetail 
INNER JOIN dbo.INV_Item ON dbo.INV_Item.ItemID = dbo.INV_StockTransactionDetail.ItemID 
INNER JOIN dbo.SET_ApprovalItemGroup ON dbo.SET_ApprovalItemGroup.ApprovalGroupId = dbo.INV_Item.ApprovalItemGroupID 
INNER JOIN dbo.INV_StockTransactionHeader ON dbo.INV_StockTransactionHeader.StockTransactionID = dbo.INV_StockTransactionDetail.StockTransactionID
and dbo.INV_StockTransactionHeader.CompanyID = dbo.INV_StockTransactionDetail.CompanyID
and dbo.INV_StockTransactionHeader.PeriodID = dbo.INV_StockTransactionDetail.PeriodID


inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
And SET_DocumentType.DocumentType like '%GRN%'

inner join ar_set_customer on ar_set_customer.CustomeriD = INV_StockTransactionHeader.BusinessPartnerID

Inner Join INV_SET_Category on INV_SET_Category.CategoryID=INV_Item.ItemTypeID

Inner Join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID

WHERE 
--(dbo.INV_StockTransactionHeader.DocumentTypeID IN (337,36))and 
INV_StockTransactionDetail.Status = 1
and INV_StockTransactionHeader.Status = 1
and INV_StockTransactionHeader.StatusCode = 1
and INV_StockTransactionHeader.CompanyID = @CompanyID
and INV_StockTransactionHeader.PeriodID = @PeriodID
{0}
Order by  INV_StockTransactionHeader.DocumentTypeID, INV_StockTransactionHeader.DocumentNo
";

        const string Query_DailyIssueReport = @"Select Distinct
 INV_StockTransactionHeader.StockTransactionID,
 INV_StockTransactionHeader.DocumentNo,
 INV_StockTransactionHeader.DocumentDate,
 INV_StockTransactionHeader.CostCenterID,
 INV_StockTransactionHeader.Demandtype,
 SET_CostCenter.CostCenterName,
 INV_StockTransactionHeader.DepartmentID,
 SET_Department.DepartmentName,
 INV_StockTransactionHeader.DocumentTypeID,
 SET_DocumentType.DocumentType,
 INV_StockTransactionDetail.ItemID,
 INV_Item.Description as ItemName,
 INV_Item.InventoryItemStatusCode as ItemCode,
 INV_StockTransactionDetail.TransactionQuantity ,
 SET_UnitofMeasure.UOMName,
INV_StockTransactionHeader.BusinessPartnerID,
 AR_SET_Customer.CustomerName as EmployeeName,
 INV_StockTransactionDetail.IncurredOnID,
 INV_StockTransactionDetail.JobID,
 INV_StockTransactionDetail.JobCode,
 Set_Job.JobDescription,
 IncurredOn.Description as IncurredOn
 from INV_StockTransactionHeader 
		inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
		AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		AND INV_StockTransactionDetail.FiscalID = INV_StockTransactionHeader.FiscalID
		AND INV_StockTransactionDetail.Status=1 
		left Join SET_CostCenter on SET_CostCenter.CostCenterID = INV_StockTransactionDetail.CostCenterID
		AND SET_CostCenter.Status=1
		left Join SET_Department on SET_Department.DepartmentID= INV_StockTransactionHeader.DepartmentID
		And SET_Department.Status=1
		inner Join SET_DocumentType on SET_DocumentType.DocumentTypeID= INV_StockTransactionHeader.DocumentTypeID
        and Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		and Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
		inner Join INV_Item on INV_Item.itemID =  INV_StockTransactionDetail.ItemID
		AND INV_Item .Status=1
		Inner Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID= INV_Item.PrimaryUOMCode
		And SET_UnitofMeasure.Status=1

		Left Join Set_Job on Set_Job.JobID = INV_StockTransactionDetail.JobID
		AND Set_Job.CompanyID = INV_StockTransactionDetail.CompanyID
		AND Set_Job.PeriodID = INV_StockTransactionDetail.PeriodID

		Left JOin INV_Item as IncurredOn on IncurredOn.ItemID=INV_StockTransactionDetail.IncurredOnID
		AND IncurredOn .Status=1

		Left Join AR_SET_Customer on AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID
Where 
--INV_StockTransactionHeader.DocumentTypeID = 28 and
INV_StockTransactionHeader.Status = 1
and [INV_StockTransactionHeader].CompanyID = @CompanyID
and [INV_StockTransactionHeader].PeriodID = @PeriodID
and SET_DocumentType.MenuID in (SELECT MenuID FROM ApplicationMenu WHERE MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))


  ";

        const string Query_SelectGRNWiseRejectItems = @"
       
        Select Distinct TransactionDetailID,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentNo As GRNNo,
        INV_StockTransactionDetail.RejectedQuantity,
		INV_StockTransactionHeader.DocumentDate, INV_StockTransactionDetail.ItemID,
		INV_Item.Description as ItemName,INV_Item.InventoryItemStatusCode as ItemCode,
		SET_UnitofMeasure.UomName,INV_StockTransactionDetail.TransactionQuantity,
		INV_StockTransactionDetail.TransactionCost as Rate,
		AP_PurchaseOrderDetail.ExciseDuty,AP_PurchaseOrderDetail.ExciseDutyAmount,AP_PurchaseOrderDetail.TaxID,
case when INV_StockTransactionDetail.RejectedQuantity > 0 then INV_StockTransactionDetail.RejectedQuantity * isNull(INV_StockTransactionDetail.TransactionCost,0) * isnull(AR_SET_Tax.TaxRate,0) / 100   else 0 end As taxAmount,
		 isnull(INV_StockTransactionDetail.RejectedQuantity * INV_StockTransactionDetail.TransactionCost,0) as NetAmount, 
		isnull(INV_StockTransactionDetail.RejectedQuantity * INV_StockTransactionDetail.TransactionCost,0) + Isnull((case when INV_StockTransactionDetail.RejectedQuantity > 0 then (INV_StockTransactionDetail.RejectedQuantity * INV_StockTransactionDetail.TransactionCost * AR_SET_Tax.TaxRate / 100  )else 0 end),0) as ForexValue,
        AR_SET_Tax.TaxRate  ,Ap_PurchaseOrderHeader.VendorID, AR_SET_Customer.CustomerName
		,DocumentTypesGroup.GroupName
		 from INV_StockTransactionHeader

		inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
		AND INV_StockTransactionDetail.CompanyID=INV_StockTransactionHeader.CompanyID
		AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHeader.PeriodID
		AND INV_StockTransactionDetail.FiscalID=INV_StockTransactionHeader.FiscalID
		AND INV_StockTransactionDetail.status=1

		Inner Join SET_DocumentType on SET_DocumentType.DocumentTypeID= INV_StockTransactionHeader.DocumentTypeID
		AND SET_DocumentType.Status=1 --And SET_DocumentType.DocumentType like '%GRN%'
       and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

		inner Join DocumentTypesGroup on DocumentTypesGroup.GroupID= SET_DocumentType.GroupID
 

		inner Join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemiD
		AND INV_Item.Status=1 
		Inner JOin INV_SET_Category on INV_SET_Category.CategoryID= INV_Item.ItemTypeID
		AND INV_SET_Category.Status=1

		Inner Join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
		and SET_ItemCatalog.Status=1
		 
		Inner Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID=INV_Item.PrimaryUOMCode
		 
		  Left JOin AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID=INV_StockTransactionDetail.SourceHeaderID
		 And AP_PurchaseOrderDetail.PODetailID=INV_StockTransactionDetail.SourceLineID
		 AND AP_PurchaseOrderDetail.Status=1
        and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
		 and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID

		 Left join Ap_PurchaseOrderHeader  on Ap_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
		 And Ap_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID
		 AND Ap_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
		 AND Ap_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
		 AND Ap_PurchaseOrderHeader.Status=1
		 	
		 left Join AR_SET_Tax  on AR_SET_Tax.TaxID=AP_PurchaseOrderDetail.TaxID
		 AND AR_SET_Tax.Status=1

		 Left Join AR_SET_Customer on AR_SET_Customer.CustomerID = Ap_PurchaseOrderHeader.VendorID
		 And AR_SET_Customer.Status=1

		 Where INV_StockTransactionHeader.status=1
AND INV_StockTransactionHeader.StatusCode = 1
		 AND INV_StockTransactionHeader.CompanyID=@CompanyID
		 		 AND INV_StockTransactionHeader.PeriodID=@PeriodID
				 And INV_StockTransactionDetail.RejectedQuantity > 0
				 {0}
		 Order By GRNNo, DocumentDate
	

 ";

        const string Query_SelectGRNWiseSKPurchase = @"
Select Distinct TransactionDetailID,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentNo As GRNNo,
		INV_StockTransactionHeader.DocumentDate, INV_StockTransactionDetail.ItemID,
		INV_Item.Description as ItemName,INV_Item.InventoryItemStatusCode as ItemCode,
		SET_UnitofMeasure.UomName,INV_StockTransactionDetail.TransactionQuantity,
		INV_StockTransactionDetail.TransactionCost as Rate,
		AP_PurchaseOrderDetail.ExciseDuty,AP_PurchaseOrderDetail.ExciseDutyAmount,AP_PurchaseOrderDetail.TaxID,
case when INV_StockTransactionDetail.TransactionQuantity > 0 then INV_StockTransactionDetail.TransactionQuantity * isNull(INV_StockTransactionDetail.TransactionCost,0) * isnull(AR_SET_Tax.TaxRate,0) / 100   else 0 end As taxAmount,
		 isnull(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost,0) as NetAmount, 
		isnull(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost,0) + Isnull((case when INV_StockTransactionDetail.TransactionQuantity > 0 then (INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost * AR_SET_Tax.TaxRate / 100  )else 0 end),0) as ForexValue,
        AR_SET_Tax.TaxRate  ,Ap_PurchaseOrderHeader.VendorID, AR_SET_Customer.CustomerName
		,DocumentTypesGroup.GroupName, AP_PurchaseOrderHeader.DocumentNumber as PONo
--	,isnull((
--        select top 1 CreateDate from SET_DocumentApprove 
--        where Set_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
--    and Set_DocumentApprove.DocumentNo = INV_StockTransactionHeader.DocumentNo
--and Set_DocumentApprove.FiscalID = INV_StockTransactionHeader.FiscalID
--    order by DocumentApproveID desc )INV_StockTransactionHeader.CreateDate) ApprovalDate

,isnull(INV_StockTransactionHeader.updateDate,INV_StockTransactionHeader.DocumentDate) ApprovalDate
		 from INV_StockTransactionHeader

		inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
		AND INV_StockTransactionDetail.CompanyID=INV_StockTransactionHeader.CompanyID
		AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHeader.PeriodID
		AND INV_StockTransactionDetail.FiscalID=INV_StockTransactionHeader.FiscalID
		AND INV_StockTransactionDetail.status=1

		Inner Join SET_DocumentType on SET_DocumentType.DocumentTypeID= INV_StockTransactionHeader.DocumentTypeID
		AND SET_DocumentType.Status=1 And SET_DocumentType.DocumentType like '%GRN%'
       and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

		inner Join DocumentTypesGroup on DocumentTypesGroup.GroupID= SET_DocumentType.GroupID
 

		inner Join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemiD
		AND INV_Item.Status=1 
		Inner JOin INV_SET_Category on INV_SET_Category.CategoryID= INV_Item.ItemTypeID
		AND INV_SET_Category.Status=1

		Inner Join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
		and SET_ItemCatalog.Status=1
		 
		Inner Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID=INV_Item.PrimaryUOMCode
		 
		  Left JOin AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID=INV_StockTransactionDetail.SourceHeaderID
		 And AP_PurchaseOrderDetail.PODetailID=INV_StockTransactionDetail.SourceLineID
		 AND AP_PurchaseOrderDetail.Status=1
        and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
		 and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID

		 Left join Ap_PurchaseOrderHeader  on Ap_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
		 And Ap_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID
		 AND Ap_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
		 AND Ap_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
		 AND Ap_PurchaseOrderHeader.Status=1
		 	
		 left Join AR_SET_Tax  on AR_SET_Tax.TaxID=AP_PurchaseOrderDetail.TaxID
		 AND AR_SET_Tax.Status=1

		 Left Join AR_SET_Customer on AR_SET_Customer.CustomerID = Ap_PurchaseOrderHeader.VendorID
		 And AR_SET_Customer.Status=1

		 Where INV_StockTransactionHeader.status=1
               AND INV_StockTransactionHeader.StatusCode = 1
		        AND INV_StockTransactionHeader.CompanyID=@CompanyID
		 	    AND INV_StockTransactionHeader.PeriodID=@PeriodID
				 {0}
		 Order By GRNNo, DocumentDate
";


        const string Query_SelectGRNWisePurchase_HSM = @"
Select Distinct TransactionDetailID,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentNo As GRNNo,
		INV_StockTransactionHeader.DocumentDate, INV_StockTransactionDetail.ItemID,
		INV_Item.Description as ItemName,INV_Item.InventoryItemStatusCode as ItemCode,
		SET_UnitofMeasure.UomName,--INV_StockTransactionDetail.TransactionQuantity,
        case when TransactionImpact='Positive' and SET_DocumentType.DocumentType not like '%Stock%Return%' 
		  then abs(INV_StockTransactionDetail.TransactionQuantity * (-1)) else 0 end AS TransactionQuantity,
		INV_StockTransactionDetail.POQuantity,INV_StockTransactionDetail.SupplierDCQuantity,
		INV_StockTransactionDetail.ActualReceivedQuantity,INV_StockTransactionDetail.ShortQuantity,
		INV_StockTransactionDetail.RejectedQuantity,
		INV_StockTransactionDetail.TransactionCost as Rate,
		AP_PurchaseOrderDetail.ExciseDuty,AP_PurchaseOrderDetail.ExciseDutyAmount,AP_PurchaseOrderDetail.TaxID,
case when INV_StockTransactionDetail.TransactionQuantity > 0 then INV_StockTransactionDetail.TransactionQuantity * isNull(INV_StockTransactionDetail.TransactionCost,0) * isnull(AR_SET_Tax.TaxRate,0) / 100   else 0 end As taxAmount,
		 isnull(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost,0) as NetAmount, 
		isnull(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost,0) + Isnull((case when INV_StockTransactionDetail.TransactionQuantity > 0 then (INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost * AR_SET_Tax.TaxRate / 100  )else 0 end),0) as ForexValue,
        AR_SET_Tax.TaxRate  ,Ap_PurchaseOrderHeader.VendorID, AR_SET_Customer.CustomerName, AR_SET_Customer.AddressLine1
		,DocumentTypesGroup.GroupName, AP_PurchaseOrderHeader.DocumentNumber as PONo,
		INV_Item.InventoryAccountCCID as GLCode, GL_CodeCombinationDetail.Description as GLDescription
		 from INV_StockTransactionHeader

		inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
		AND INV_StockTransactionDetail.CompanyID=INV_StockTransactionHeader.CompanyID
		AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHeader.PeriodID
		AND INV_StockTransactionDetail.FiscalID=INV_StockTransactionHeader.FiscalID
		AND INV_StockTransactionDetail.status=1

		Inner Join SET_DocumentType on SET_DocumentType.DocumentTypeID= INV_StockTransactionHeader.DocumentTypeID
		AND SET_DocumentType.Status=1 And SET_DocumentType.DocumentType like '%GRN%'
       and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

		inner Join DocumentTypesGroup on DocumentTypesGroup.GroupID= SET_DocumentType.GroupID
 

		inner Join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemiD
		AND INV_Item.Status=1 

		inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = INV_Item.InventoryAccountCCID 
		And GL_CodeCombinationDetail.CompanyID = INV_Item.CompanyID

		Inner JOin INV_SET_Category on INV_SET_Category.CategoryID= INV_Item.ItemTypeID
		AND INV_SET_Category.Status=1

		Inner Join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
		and SET_ItemCatalog.Status=1
		 
		Inner Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID=INV_Item.PrimaryUOMCode
		 
		  Left JOin AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID=INV_StockTransactionDetail.SourceHeaderID
		 And AP_PurchaseOrderDetail.PODetailID=INV_StockTransactionDetail.SourceLineID
		 AND AP_PurchaseOrderDetail.Status=1
        and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID
		 and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID

		 Left join Ap_PurchaseOrderHeader  on Ap_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
		 And Ap_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID
		 AND Ap_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
		 AND Ap_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
		 AND Ap_PurchaseOrderHeader.Status=1
		 	
		 left Join AR_SET_Tax  on AR_SET_Tax.TaxID=AP_PurchaseOrderDetail.TaxID
		 AND AR_SET_Tax.Status=1

		 Left Join AR_SET_Customer on AR_SET_Customer.CustomerID = Ap_PurchaseOrderHeader.VendorID
		 And AR_SET_Customer.Status=1

		 Where INV_StockTransactionHeader.status=1
		 AND INV_StockTransactionHeader.CompanyID=@CompanyID
		 		 AND INV_StockTransactionHeader.PeriodID=@PeriodID
				 {0}
		 Order By GRNNo, DocumentDate
";


        const string Query_SelectGINWiseTransaction = @"Select SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentNo As GINNo,
		INV_StockTransactionHeader.DocumentDate, INV_StockTransactionDetail.ItemID,
		INV_Item.Description as ItemName,INV_Item.InventoryItemStatusCode as ItemCode,
		SET_UnitofMeasure.UomName,
		ABS(INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity,
		
		INV_StockTransactionDetail.TransactionCost as Rate,
		--GL_TransactionDetail.Amount / ABS(INV_StockTransactionDetail.TransactionQuantity) as Rate,

		(INV_StockTransactionDetail.TransactionCost * ABS(INV_StockTransactionDetail.TransactionQuantity)) as  Amount,
		--GL_TransactionDetail.Amount as Amount,

		DocumentTypesGroup.GroupName, INV_StockTransactionDetail.CostCenterID,Set_Costcenter.CostCenterName
		,Case when INV_StockTransactionDetail.JobID > 0 then Set_Job.AccountCode else INV_Item.ExpenseAccountCCID end as ExpenseAccountCCID,
		Case when INV_StockTransactionDetail.JobID > 0 then gl_CCD_JobCode.Description else GL_CodeCombinationDetail.Description end as CodeConsumption
		,isnull(SET_Location.LocationName,SL.LocationName) as LocationName, ItemIncurred.Description AS IncurredOn
		 from INV_StockTransactionHeader

		inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
		AND INV_StockTransactionDetail.CompanyID=INV_StockTransactionHeader.CompanyID
		AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHeader.PeriodID
		AND INV_StockTransactionDetail.FiscalID=INV_StockTransactionHeader.FiscalID
		AND INV_StockTransactionDetail.status=1

		left Join Set_Job on Set_Job.JobID=INV_StockTransactionDetail.JobID
				
		Left Join Set_Costcenter on Set_Costcenter.CostCenterID=INV_StockTransactionDetail.CostCenterID
		AND Set_Costcenter.Status=1

		Inner Join SET_DocumentType on SET_DocumentType.DocumentTypeID= INV_StockTransactionHeader.DocumentTypeID
		AND SET_DocumentType.Status=1 And SET_DocumentType.DocumentType like '%GIN%'
	and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
		and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID

		inner Join DocumentTypesGroup on DocumentTypesGroup.GroupID= SET_DocumentType.GroupID
 

		inner Join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemiD
		AND INV_Item.Status=1 
		Inner JOin INV_SET_Category on INV_SET_Category.CategoryID= INV_Item.ItemTypeID
		AND INV_SET_Category.Status=1

		Inner Join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID=INV_Item.ItemCatalogGroupID
		and SET_ItemCatalog.Status=1
		 
		inner Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID=INV_Item.PrimaryUOMCode
		 
		 Inner Join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationId=INV_Item.ExpenseAccountCCID
		 AND GL_CodeCombinationDetail.Status=1

		 Left Join GL_CodeCombinationDetail gl_CCD_JobCode on gl_CCD_JobCode.CodeCombinationID= Set_Job.AccountCode

		    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
    LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 

LEFT JOIN INV_Item ItemIncurred ON ItemIncurred.ItemID = INV_StockTransactionDetail.IncurredOnID
	AND ItemIncurred.Status = 1

		 Where INV_StockTransactionHeader.status=1
        AND INV_StockTransactionHeader.StatusCode = 1
		 AND INV_StockTransactionHeader.CompanyID=@CompanyID
		 		 AND INV_StockTransactionHeader.PeriodID=@PeriodID
				  AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)
				 {0}
		 Order By GINNo, DocumentDate";

        #endregion


        public DataRow Get_GIN_By_DocumentType(int companyID, int periodID, int documentTypeID, int documentNo)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Get_GIN_By_DocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return drToReturn;
        }

        public static bool UpdateFIFOByTransactionIDAlImdad(int TransactionDetailID, decimal? issuedQuantity, int CompanyID, int PeriodID, bool? status)
        {
            bool result = false;
            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                if (status.HasValue)
                    sqlparams[3] = new SqlParameter("@IsIssued", status.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsIssued", DBNull.Value);

                if (issuedQuantity.HasValue)
                    sqlparams[4] = new SqlParameter("@issuedQuantity", issuedQuantity.Value);
                else
                    sqlparams[4] = new SqlParameter("@issuedQuantity", DBNull.Value);



                string Query = @"update inv_stocktransactiondetail set isissued=@isissued , issuedquantity = @issuedQuantity
                                    where TransactionDetailID = @TransactionDetailID 
                                            and CompanyID =@CompanyID 
                                            and PeriodID  = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public static bool UpdateFIFOByTransactionIDAlImdadParentID(int TransactionDetailID, int ParentFIFOTransactionDetailID, int CompanyID, int PeriodID)
        {
            bool result = false;
            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@ParentFIFOTransactionDetailID", ParentFIFOTransactionDetailID);



                string Query = @"update inv_stocktransactiondetail set FIFOTransactionDetailID=@ParentFIFOTransactionDetailID
                                    where TransactionDetailID = @TransactionDetailID 
                                            and CompanyID =@CompanyID 
                                            and PeriodID  = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }



        public bool ReverseDocument(List<INV_StockTransactionDetail> lst_INVStockTransDetail, DateTime dt_ReverseDate, Guid reversedBy, GL_TransactionHeader objGLHeader,
           List<GL_TransactionDetail> lst_GLDetails, SET_DocumentApprove obj_Approval, out int ReversalDocumentNo, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("ReverseDirectInvoices");
            SqlConnection objConnection = ConnectionHelper.GetOpenSqlConnection();

            try
            {

                //DataTable dtInvoiceTransactionDetail = INV_StockTransactionHeader.GetDataByKeyForGINReversal(Convert.ToInt32(this.CompanyID), Convert.ToInt32(this.PeriodID), Convert.ToInt32(this.StockTransactionID), objConnection);

                this.UpdateDataByKey(objSQLTransaction);

                objGLHeader.UpdateDataByKey(objSQLTransaction); //  Update old General Ledger Header

                #region Insert new Purchase Invoice Header

                this.IsReversed = null;
                this.ReversalDate = DateTime.Now;
                this.ReversalID = this.StockTransactionID;
                this.Comments += "This document is generated against the Reversal process (Doc. No.: " + this.DocumentNo + ")";
                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, objSQLTransaction);
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(this.DocumentTypeID, this.CompanyID, this.PeriodID, objSQLTransaction, CurrentFiscalID);
                this.TotalAmount = this.TotalAmount * -1;
                //this.GrossAmount = this.GrossAmount * -1;

                ReversalDocumentNo = Convert.ToInt32(this.DocumentNo);

                // this.Insert(objSQLTransaction);

                #endregion

                #region Insert in Purchase Invoice Detail table

                //if (lst_INVStockTransDetail != null)
                //{

                //    //short INVStockTransDetailID = (short)INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, objSQLTransaction);
                //    long INVStockTransDetailID = (long)INV_StockTransactionDetail.GetNewTransactionDetailID();
                //    foreach (INV_StockTransactionDetail obj_INVStockTransDetail in lst_INVStockTransDetail)
                //    {
                //        obj_INVStockTransDetail.StockTransactionID = this.StockTransactionID;
                //        obj_INVStockTransDetail.TransactionDetailID = INVStockTransDetailID++;
                //        obj_INVStockTransDetail.Insert(objSQLTransaction);
                //    }
                //}

                #endregion

                #region Insert new General Ledger Header

                objGLHeader.IsReversed = null;
                objGLHeader.Narration += (objGLHeader.Narration == null ? "" : objGLHeader.Narration.Trim().EndsWith(".") ? " " : ". ")
                    + "This document is generated against the Reversal process.";

                objGLHeader.ReversalDate = null;
                objGLHeader.ReversedBy = null;
                objGLHeader.ReversalID = objGLHeader.TransactionID;
                objGLHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                objGLHeader.DocumentNo = Convert.ToInt32(this.DocumentNo);

                objGLHeader.Insert(objSQLTransaction);

                #endregion

                #region Insert new General Ledger Detail


                decimal AmountDebit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                decimal AmountCredit = Math.Round(lst_GLDetails.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                if (AmountDebit != AmountCredit)
                    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                foreach (GL_TransactionDetail objGLDetail in lst_GLDetails)
                {
                    objGLDetail.TransactionID = objGLHeader.TransactionID;
                    objGLDetail.SourceHeaderID = objGLHeader.ReversalID;
                    objGLDetail.Insert(objSQLTransaction);
                }

                #endregion

                #region Insert new Document Approve

                obj_Approval.Status = true;
                obj_Approval.DocumentNo = objGLHeader.DocumentNo.ToString();
                obj_Approval.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                obj_Approval.Insert(objSQLTransaction);


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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "ReverseDirectInvoices");
            }

            return result;
        }

        public static bool UpdateIsRequestedCompleted(int companyID, int periodID, int StockTransactionID, bool Status)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[3] = new SqlParameter("@Status", Status);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsRequestedCompleted, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool UpdateIsRequestedCompleted(int companyID, int periodID, int StockTransactionID, bool Status, SqlTransaction objTransaction = null)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[3] = new SqlParameter("@Status", Status);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsRequestedCompleted, sqlparams) > 0)
                        result = true;
                }
                else
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateIsRequestedCompleted, sqlparams) > 0) ;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static decimal GetGINQuantityAgainstRequisition(int companyID, int periodID, int requisitionHeaderID, long requisitionDetailID, int requisitionDocumentTypeID, long requisitionNo, SqlTransaction objTransaction = null)
        {
            decimal quantityToReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RequisitionHeaderID", requisitionHeaderID);
                sqlparams[3] = new SqlParameter("@RequisitionDetailID", requisitionDetailID);
                sqlparams[4] = new SqlParameter("@RequisitionDocumentTypeID", requisitionDocumentTypeID);
                sqlparams[5] = new SqlParameter("@RequisitionNo", requisitionNo);

                if (objTransaction != null)
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetGINQuantityAgainstRequisition, sqlparams));
                else
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetGINQuantityAgainstRequisition, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return quantityToReturn;
        }

        public static DataTable Get_StockSingleRowOnDocNo_DocType(int companyID, int periodID, int documentTypeID, long documentNo, SqlConnection objConnection = null,
            SqlTransaction objTransaction = null, int stocktransactionid = 0)
        {
            DataTable dt = null;
            string appendQuery = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@DTID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocNo", documentNo);
                //sqlparams[4] = new SqlParameter("@stocktransactionid", stocktransactionid);

                DataSet ds = null;
                if (stocktransactionid > 0)
                    appendQuery = "and stocktransactionid = " + stocktransactionid;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_Get_StockSingleRowOnDocNo_DocType, appendQuery), sqlparams);
                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Get_StockSingleRowOnDocNo_DocType, appendQuery), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_StockSingleRowOnDocNo_DocType, appendQuery), sqlparams);

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

        public static bool Get_UpdatePriorCost(int companyID, int periodID, long ItemID, decimal PC, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@PC", PC);

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePriorCost, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdatePriorCost, sqlparams);

                result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable CheckStockForTransferOut(int companyID, int periodID, SqlTransaction objTransaction = null, string filters = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CheckStockForTransferOut + filters;

                if (objTransaction == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Get_FinishedGoodsSearchwithChasis(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
           Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscal = 0)
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
                    string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_FinishedGoodsSearchwithChasis.Replace("{wherefilter}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_FinishedGoodsSearchwithChasis + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        //    public static DataTable Get_FinishedGoodsSearchwithChasis( int companyID, int periodID, SqlTransaction objTransaction = null,string filters = "")
        //{
        //    DataTable dt;
        //    try
        //    {
        //        SqlParameter[] sqlparams = new SqlParameter[2];
        //        sqlparams[0] = new SqlParameter("@PeriodID", periodID);
        //        sqlparams[1] = new SqlParameter("@CompanyID", companyID);

        //        string query = Query_FinishedGoodsSearchwithChasis + filters;

        //        if (objTransaction == null)
        //        {
        //            dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
        //        }
        //        else
        //            dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams).Tables[0];
        //    }
        //    catch (Exception ex)
        //    {
        //        SqlHelper.LogException(ex);
        //        throw ex;
        //    }

        //    return dt;
        //}

        public static DataTable Get_LastTransactionCostAgainstItem(long ItemID, int companyID, int periodID, SqlTransaction objTransaction = null)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", ItemID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                if (objTransaction == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_LastTransactionCostAgainstItem, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_LastTransactionCostAgainstItem, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_GetSumAmountAndSumTransactionQuantity(long ItemID, int companyID, int periodID, SqlTransaction objTransaction = null)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", ItemID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                if (objTransaction == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSumAmountAndSumTransactionQuantity, sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetSumAmountAndSumTransactionQuantity, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0)
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
                    sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll_Searching_WithPaging_ForApprove_Copy3.Replace("{filter}", strWhere);

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll_Searching_ForApprove + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool Update_TransactionQTY_InDetail_ByCaneSW(int companyID, int periodID, int StockDetailTransID, decimal TransactionQTY,
            SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionDetailID", StockDetailTransID);
                sqlparams[3] = new SqlParameter("@TransactionQTY", TransactionQTY);

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_TransactionQTY_ByCaneSW, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_TransactionQTY_ByCaneSW, sqlparams);

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

            return result;
        }

        public static DataTable item_ID_Desc_FromPO(int? POheaderID, int companyID, int periodID)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POheaderID", POheaderID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Item_ID_Desc_FromPO, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockRequisition_Lookup(int companyID, int periodID, string filter = "", string documentnofilter = "", string RequistionAssignmentFilter = "", string filterDocumentGroup = "")
        {
            DataTable dt;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            //string sb = string.Empty;
            #region for GIN
            if (filter == "GIN")
            {
                sb.Append(" Inner join cte on cte.SourceHeaderID = STD.StockTransactionID and cte.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and cte.SourceCode = INV_StockTransactionHeader.DocumentNo and cte.SourceLineID = STD.TransactionDetailID ");


                string ctefilter = @" with cte  as (
                                    Select INV_StockTransactionDetail.SourceHeaderID , INV_StockTransactionDetail.SourceDocumentTypeID,INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID,
                                    sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))transactionquantity
                                    from INV_StockTransactionHeader 
                                    Inner join INV_StockTransactionDetail 
                                    on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
                                    AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
                                    AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
                                    AND INV_StockTransactionDetail.Status = 1 
                                    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                                    AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID 
                                    = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 
                                    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID 
                                    And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
                                    and sourceheaderid is not null
                                     group by INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceDocumentTypeID, INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID
                                      having sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))>0
                                     )";

                sb1.Append(ctefilter);

                //                filter = @" and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%' 
                //                            and INV_StockTransactionDetail.TransactionQuantity > isnull(GIn.qty,0) ";


                //sb.Append("left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID");


                filter = @"     and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%'
                                {DocumentGroup}
                                GROUP BY INV_StockTransactionHeader.StockTransactionID,INV_StockTransactionHeader.DocumentTypeID,
                                INV_StockTransactionHeader.DocumentNo,std.StockTransactionID,STD.TransactionDetailID,INV_StockTransactionHeader.DocumentDate,
                                Set_DocumentType.DocumentType,SET_CostCenter.CostCenterName,cte.transactionquantity,std.TransactionQuantity
                                having round(sum(std.TransactionQuantity),4) > round(sum(isnull(cte.transactionquantity,0)),4)  ";
                //sb.Append(" left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty ,serialNo from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ,serialNo) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID AND gin.serialNo = INV_StockTransactionDetail.serialNo ");

                const string AddtionalJoins = @" left join INV_StockTransactionDetail GIN on GIN.SourceHEaderId = INV_StockTransactionDetail .StockTransactionID 
and GIN.SourceLineID = INV_StockTransactionDetail.TransactionDetailID and GIN.Status = 1
and GIN.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
left join INV_StockTransactionHeader GINHdr on GINHdr.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and GINHdr.status = 1
left JOIN Set_DocumentType GinDoc ON GinDoc.DocumentTypeID = GINHdr.DocumentTypeID 
AND GinDoc.CompanyID = GINHdr.CompanyID AND GinDoc.PeriodID = GINHdr.PeriodID 
AND GinDoc.Status = 1 
left Join ApplicationMenu GinMenu On GinMenu.MenuID = Set_DocumentType.MenuID 
And GinMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
and GINHdr.DocumentTypeID = GinDoc.DocumentTypeID";
                // sb = AddtionalJoins;
                //ABNORMALLY ACTING SHOWING MULTI THATS WHY COMMENTED 

            }
            // filter += documentnofilter;
            //filter= filter+RequistionAssignmentFilter;
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                string query = "";
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                query = Query_Select_StockRequisition_Lookup.Replace("{filter}", filter);
                query = query.Replace("{DocumentGroup}", filterDocumentGroup);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), RequistionAssignmentFilter.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockRequisition_Lookup_PO(int companyID, int periodID, string filter = "", string documentnofilter = "", string RequistionAssignmentFilter = "", string filterDocumentGroup = "")
        {
            DataTable dt;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            bool IsPO = true;
            //string sb = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                string query = "";
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                query = Query_Select_StockRequisition_Lookup_ForPO.Replace("{filter}", filter);
                query = query.Replace("{DocumentGroup}", filterDocumentGroup);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), RequistionAssignmentFilter.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockRequisition_Lookup_RFQ(int companyID, int periodID, string filter = "", string documentnofilter = "", string RequistionAssignmentFilter = "", string filterDocumentGroup = "")
        {
            DataTable dt;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            //string sb = string.Empty;
            #region for GIN
            if (filter == "GIN")
            {
                sb.Append(" Inner join cte on cte.SourceHeaderID = STD.StockTransactionID and cte.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and cte.SourceCode = INV_StockTransactionHeader.DocumentNo and cte.SourceLineID = STD.TransactionDetailID ");


                string ctefilter = @" with cte  as (
                                    Select INV_StockTransactionDetail.SourceHeaderID , INV_StockTransactionDetail.SourceDocumentTypeID,INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID,
                                    sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))transactionquantity
                                    from INV_StockTransactionHeader 
                                    Inner join INV_StockTransactionDetail 
                                    on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
                                    AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
                                    AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
                                    AND INV_StockTransactionDetail.Status = 1 
                                    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                                    AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID 
                                    = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 
                                    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID 
                                    And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
                                    and sourceheaderid is not null
                                     group by INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceDocumentTypeID, INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID
                                      having sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))>0
                                     )";

                sb1.Append(ctefilter);

                //                filter = @" and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%' 
                //                            and INV_StockTransactionDetail.TransactionQuantity > isnull(GIn.qty,0) ";


                //sb.Append("left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID");


                filter = @"     and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%'
                                {DocumentGroup}
                                GROUP BY INV_StockTransactionHeader.StockTransactionID,INV_StockTransactionHeader.DocumentTypeID,
                                INV_StockTransactionHeader.DocumentNo,std.StockTransactionID,STD.TransactionDetailID,INV_StockTransactionHeader.DocumentDate,
                                Set_DocumentType.DocumentType,SET_CostCenter.CostCenterName,cte.transactionquantity,std.TransactionQuantity
                                having round(sum(std.TransactionQuantity),4) > round(sum(isnull(cte.transactionquantity,0)),4)  ";
                //sb.Append(" left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty ,serialNo from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ,serialNo) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID AND gin.serialNo = INV_StockTransactionDetail.serialNo ");

                const string AddtionalJoins = @" left join INV_StockTransactionDetail GIN on GIN.SourceHEaderId = INV_StockTransactionDetail .StockTransactionID 
and GIN.SourceLineID = INV_StockTransactionDetail.TransactionDetailID and GIN.Status = 1
and GIN.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
left join INV_StockTransactionHeader GINHdr on GINHdr.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and GINHdr.status = 1
left JOIN Set_DocumentType GinDoc ON GinDoc.DocumentTypeID = GINHdr.DocumentTypeID 
AND GinDoc.CompanyID = GINHdr.CompanyID AND GinDoc.PeriodID = GINHdr.PeriodID 
AND GinDoc.Status = 1 
left Join ApplicationMenu GinMenu On GinMenu.MenuID = Set_DocumentType.MenuID 
And GinMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
and GINHdr.DocumentTypeID = GinDoc.DocumentTypeID";
                // sb = AddtionalJoins;
                //ABNORMALLY ACTING SHOWING MULTI THATS WHY COMMENTED 

            }
            // filter += documentnofilter;
            //filter= filter+RequistionAssignmentFilter;
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                string query = "";
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                query = Query_Select_StockRequisition_Lookup_RFQ.Replace("{filter}", filter);
                query = query.Replace("{DocumentGroup}", filterDocumentGroup);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), RequistionAssignmentFilter.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockRequisition_Lookup_RFQ_RSM(int companyID, int periodID, string filter = "", string documentnofilter = "", string RequistionAssignmentFilter = "", string filterDocumentGroup = "")
        {
            DataTable dt;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            //string sb = string.Empty;
            #region for GIN
            if (filter == "GIN")
            {
                sb.Append(" Inner join cte on cte.SourceHeaderID = STD.StockTransactionID and cte.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and cte.SourceCode = INV_StockTransactionHeader.DocumentNo and cte.SourceLineID = STD.TransactionDetailID ");


                string ctefilter = @" with cte  as (
                                    Select INV_StockTransactionDetail.SourceHeaderID , INV_StockTransactionDetail.SourceDocumentTypeID,INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID,
                                    sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))transactionquantity
                                    from INV_StockTransactionHeader 
                                    Inner join INV_StockTransactionDetail 
                                    on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
                                    AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
                                    AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
                                    AND INV_StockTransactionDetail.Status = 1 
                                    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                                    AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID 
                                    = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 
                                    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID 
                                    And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
                                    and sourceheaderid is not null
                                     group by INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceDocumentTypeID, INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID
                                      having sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))>0
                                     )";

                sb1.Append(ctefilter);

                //                filter = @" and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%' 
                //                            and INV_StockTransactionDetail.TransactionQuantity > isnull(GIn.qty,0) ";


                //sb.Append("left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID");


                filter = @"     and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%'
                                {DocumentGroup}
                                GROUP BY INV_StockTransactionHeader.StockTransactionID,INV_StockTransactionHeader.DocumentTypeID,
                                INV_StockTransactionHeader.DocumentNo,std.StockTransactionID,STD.TransactionDetailID,INV_StockTransactionHeader.DocumentDate,
                                Set_DocumentType.DocumentType,SET_CostCenter.CostCenterName,cte.transactionquantity,std.TransactionQuantity
                                having round(sum(std.TransactionQuantity),4) > round(sum(isnull(cte.transactionquantity,0)),4)  ";
                //sb.Append(" left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty ,serialNo from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ,serialNo) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID AND gin.serialNo = INV_StockTransactionDetail.serialNo ");

                const string AddtionalJoins = @" left join INV_StockTransactionDetail GIN on GIN.SourceHEaderId = INV_StockTransactionDetail .StockTransactionID 
and GIN.SourceLineID = INV_StockTransactionDetail.TransactionDetailID and GIN.Status = 1
and GIN.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
left join INV_StockTransactionHeader GINHdr on GINHdr.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and GINHdr.status = 1
left JOIN Set_DocumentType GinDoc ON GinDoc.DocumentTypeID = GINHdr.DocumentTypeID 
AND GinDoc.CompanyID = GINHdr.CompanyID AND GinDoc.PeriodID = GINHdr.PeriodID 
AND GinDoc.Status = 1 
left Join ApplicationMenu GinMenu On GinMenu.MenuID = Set_DocumentType.MenuID 
And GinMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
and GINHdr.DocumentTypeID = GinDoc.DocumentTypeID";
                // sb = AddtionalJoins;
                //ABNORMALLY ACTING SHOWING MULTI THATS WHY COMMENTED 

            }
            // filter += documentnofilter;
            //filter= filter+RequistionAssignmentFilter;
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                string query = "";
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                query = Query_Select_StockRequisition_Lookup_RFQ_RSM.Replace("{filter}", filter);
                query = query.Replace("{DocumentGroup}", filterDocumentGroup);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), RequistionAssignmentFilter.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockRequisition_Lookup_RFQforSSM(int companyID, int periodID, string filter = "", string documentnofilter = "", string filterDocumentGroup = "")
        {
            DataTable dt;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            //string sb = string.Empty;
            #region for GIN
            if (filter == "GIN")
            {
                sb.Append(" Inner join cte on cte.SourceHeaderID = STD.StockTransactionID and cte.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and cte.SourceCode = INV_StockTransactionHeader.DocumentNo and cte.SourceLineID = STD.TransactionDetailID ");


                string ctefilter = @" with cte  as (
                                    Select INV_StockTransactionDetail.SourceHeaderID , INV_StockTransactionDetail.SourceDocumentTypeID,INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID,
                                    sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))transactionquantity
                                    from INV_StockTransactionHeader 
                                    Inner join INV_StockTransactionDetail 
                                    on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
                                    AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
                                    AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
                                    AND INV_StockTransactionDetail.Status = 1 
                                    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                                    AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID 
                                    = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 
                                    Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID 
                                    And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
                                    and sourceheaderid is not null
                                     group by INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceDocumentTypeID, INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceLineID
                                      having sum(abs(isnull(INV_StockTransactionDetail.transactionquantity,0)))>0
                                     )";

                sb1.Append(ctefilter);

                //                filter = @" and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%' 
                //                            and INV_StockTransactionDetail.TransactionQuantity > isnull(GIn.qty,0) ";


                //sb.Append("left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID");


                filter = @"     and Set_DocumentType.DocumentType not like '%PR%' and Set_DocumentType.DocumentType not like '%Store%'
                                {DocumentGroup}
                                GROUP BY INV_StockTransactionHeader.StockTransactionID,INV_StockTransactionHeader.DocumentTypeID,
                                INV_StockTransactionHeader.DocumentNo,std.StockTransactionID,STD.TransactionDetailID,INV_StockTransactionHeader.DocumentDate,
                                Set_DocumentType.DocumentType,SET_CostCenter.CostCenterName,cte.transactionquantity,std.TransactionQuantity
                                having round(sum(std.TransactionQuantity),4) > round(sum(isnull(cte.transactionquantity,0)),4)  ";
                //sb.Append(" left join (Select INV_StockTransactionDetail.SourceHeaderID,SUM(abs(INV_StockTransactionDetail.TransactionQuantity)) as qty ,serialNo from INV_StockTransactionHeader Inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND Set_DocumentType.Status = 1 Inner Join ApplicationMenu On ApplicationMenu.MenuID = Set_DocumentType.MenuID And ApplicationMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' group by INV_StockTransactionDetail.SourceHeaderID ,serialNo) GIN on GIN.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID AND gin.serialNo = INV_StockTransactionDetail.serialNo ");

                const string AddtionalJoins = @" left join INV_StockTransactionDetail GIN on GIN.SourceHEaderId = INV_StockTransactionDetail .StockTransactionID 
and GIN.SourceLineID = INV_StockTransactionDetail.TransactionDetailID and GIN.Status = 1
and GIN.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
left join INV_StockTransactionHeader GINHdr on GINHdr.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and GINHdr.status = 1
left JOIN Set_DocumentType GinDoc ON GinDoc.DocumentTypeID = GINHdr.DocumentTypeID 
AND GinDoc.CompanyID = GINHdr.CompanyID AND GinDoc.PeriodID = GINHdr.PeriodID 
AND GinDoc.Status = 1 
left Join ApplicationMenu GinMenu On GinMenu.MenuID = Set_DocumentType.MenuID 
And GinMenu.MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx' 
and GINHdr.DocumentTypeID = GinDoc.DocumentTypeID";
                // sb = AddtionalJoins;
                //ABNORMALLY ACTING SHOWING MULTI THATS WHY COMMENTED 

            }
            // filter += documentnofilter;
            //filter= filter+RequistionAssignmentFilter;
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                string query = "";
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                query = Query_Select_StockRequisition_Lookup_RFQ.Replace("{filter}", filter);
                query = query.Replace("{DocumentGroup}", filterDocumentGroup);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), filterDocumentGroup.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable Select_StockRequisitionReturn_Lookup(int companyID, int periodID, string Query, string filter = "", string filter2 = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                string query = Query.Replace("{filter}", filter);
                query = query.Replace("{filter2}", filter2);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        // For AutoMobile
        public static DataTable Select_StockRequisition_Lookup_AutoMobile(int companyID, int periodID, string filter = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Select_StockRequisition_Lookup_AutoMobile.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockGIN_Lookup(int companyID, int periodID, string filter = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Select_StockGIN_Lookup.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Select_StockTranferOut_Lookup(int companyID, int periodID, string filter = "")
        {
            DataTable dt;
            string query = "";
            try
            {
                if (filter == "documentout")
                {
                    query = @" Select INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, Set_DocumentType.DocumentType 
from INV_StockTransactionHeader 
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
And (Set_DocumentType.DocumentType Like '%issuance%')
WHERE INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID And INV_StockTransactionHeader.Status = 1 
    And INV_StockTransactionHeader.StatusCode = 1 AND (INV_StockTransactionHeader.IsTransferred IS NULL OR INV_StockTransactionHeader.IsTransferred = 0) ";
                }
                else
                    query = string.Format(Query_Select_StockTransferOut_Lookup, filter);
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable CheckPOQuantity(string strWhere)
        {
            DataTable dt;
            try
            {
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_CheckPOQuantity + strWhere).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable CheckRequisitionQuantity(int CompanyID, int PeriodID, int RequisitionID)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@RequisitionID", RequisitionID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CheckRequisitionQuantity, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public void check_PODetail_ByPOHeaderID(int? POheaderID, int companyID, int periodID, int? closedCode, Nullable<DateTime> closeDate, int? partiallyReceivedCode = null,
            SqlTransaction objTransaction = null)
        {
            int Var_PODetailCount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POheaderID", POheaderID);
                Var_PODetailCount = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetPODetailCount_ByPOHeaderID, sqlparams));


                if (Var_PODetailCount == 0)
                    update_POHeader_OnBehalfof_PODetail(POheaderID, companyID, periodID, closedCode, closeDate, objTransaction);
                else if (Var_PODetailCount > 0)
                    update_POHeader_OnBehalfof_PODetail(POheaderID, companyID, periodID, partiallyReceivedCode, null, objTransaction);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public bool update_POHeader_OnBehalfof_PODetail(int? POheaderID, int companyID, int periodID, int? closedCode, Nullable<DateTime> closeDate,
            SqlTransaction objTransaction = null)
        {
            bool returnResult = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POheaderID", POheaderID);


                sqlparams[3] = new SqlParameter("@closeCode", closedCode);


                if (closeDate.HasValue)
                    sqlparams[4] = new SqlParameter("@closeDate", closeDate.Value);
                else
                    sqlparams[4] = new SqlParameter("@closeDate", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_POHeader_CloseCode, sqlparams) > 0)
                        returnResult = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_POHeader_CloseCode, sqlparams) > 0)
                        returnResult = true;
                }

                return returnResult;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;

            }
        }

        public void Get_POStockQty(int? POheaderID, int? POdetailID, int? itemID, int companyID, int periodID, float transactionQty, int? closedCode,
            Nullable<Guid> closeBy, Nullable<DateTime> closeDate, bool? closeFlag, string closeReason, int? documentTypeID, int? documentNo,
            int? partiallyReceivedCode = null, SqlTransaction objTransaction = null)
        {
            double Var_sumQty = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POheaderID", POheaderID);
                sqlparams[3] = new SqlParameter("@POdetailID", POdetailID);
                sqlparams[4] = new SqlParameter("@itemID", itemID.Value);
                sqlparams[5] = new SqlParameter("@PODocumentTypeID", documentTypeID);
                sqlparams[6] = new SqlParameter("@POSourceCode", documentNo);

                Var_sumQty = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_getQtyPO_Stock, sqlparams));

                if (Var_sumQty > 0)
                    update_POStockQty(POheaderID, POdetailID, itemID, companyID, periodID, partiallyReceivedCode, null, null, false, null, null);
                else
                    update_POStockQty(POheaderID, POdetailID, itemID, companyID, periodID, closedCode, closeBy, closeDate, closeFlag, closeReason, null);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public bool update_POStockQty(int? POheaderID, int? POdetailID, int? itemID, int companyID, int periodID, int? closedCode, Nullable<Guid> closeBy,
            Nullable<DateTime> closeDate, bool? closeFlag, string closeReason, SqlTransaction objTransaction = null)
        {
            bool returnResult = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POheaderID", POheaderID);
                sqlparams[3] = new SqlParameter("@POdetailID", POdetailID);
                sqlparams[4] = new SqlParameter("@itemID", itemID);
                sqlparams[5] = new SqlParameter("@closeCode", closedCode);

                if (closeBy.HasValue)
                    sqlparams[6] = new SqlParameter("@closeBy", closeBy);
                else
                    sqlparams[6] = new SqlParameter("@closeBy", DBNull.Value);

                if (closeDate.HasValue)
                    sqlparams[7] = new SqlParameter("@closeDate", closeDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@closeDate", DBNull.Value);

                sqlparams[8] = new SqlParameter("@closeFlag", closeFlag);

                if (!string.IsNullOrEmpty(closeReason))
                    sqlparams[9] = new SqlParameter("@closeReason", closeReason);
                else
                    sqlparams[9] = new SqlParameter("@closeReason", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_PODetail_CloseCode, sqlparams) > 0)
                        returnResult = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_PODetail_CloseCode, sqlparams) > 0)
                        returnResult = true;
                }

                return returnResult;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataRow GetDataByKeyWithOtherTables(int stockTransactionID, int periodID, int companyID)
        {
            DataRow drToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Select_By_PrimaryKey_WithOtherTables + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataRow GetDataByKeyWithOtherForStock_Form(int stockTransactionID, int periodID, int companyID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Select_By_PrimaryKey_WithOtherForStock_Form, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }



        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0, string AlImdadFilter = "", string MenuPath = "")
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
                    sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    //{grnfilter}

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_SelectAll_Searching_WithPaging.Replace("{wherefilter}", strWhere).Replace("{MenuPath}",MenuPath);
                    query = query.Replace("{grnfilter}", AlImdadFilter);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    string query = Query_SelectAll_Searching.Replace("{grnfilter}", AlImdadFilter);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }




            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForAssignment(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0, string AlImdadFilter = "")
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
                    sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    //{grnfilter}

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_SelectAll_Searching_WithPagingAssignment.Replace("{wherefilter}", strWhere);
                    query = query.Replace("{grnfilter}", AlImdadFilter);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    string query = Query_SelectAll_Searching.Replace("{grnfilter}", AlImdadFilter);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }




            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForRequisition(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
          Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPagingForRequisition + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPagingForRequisition + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                    //    + (string.Empty), Query_SelectAll_Searching_WithPaging + strWhere
                    //    + (string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching_ForRequisition + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int stockTransactionID, int documentTypeID,
            string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int stockTransactionID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.query_Select_DataByKey_ForApprove, sqlparams);

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

        public static DataTable GetAllDataWithOtherTables_lookUp(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, string Where2 = "")
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                //Query_SelectAllGRNCase
                string Query = string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll__looKUp, Where2.ToString(), strWhere.ToString());
                if (Where2 == "Import")
                    Query = string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetGrnImportLookup, "", strWhere.ToString());


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables_lookUpGRN(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
           Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, string Where2 = "")
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                //Query_SelectAllGRNCase
                string Query = string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAllGRNCase, Where2.ToString(), strWhere.ToString());


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertStockTransactionHederAndDetailPTIS(List<INV_StockTransactionDetail> colStockTranDetail, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 1, SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans = sql;
            try
            {

                // this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                //this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);


                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {

                    //std.StockTransactionID = this.StockTransactionID;

                    //  std.FiscalID = CurrentFiscalID;


                    //  std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    //  std.Comments = this.Comments;
                    //   std.TransactionDetailID = detailID;
                    std.Insert(sqltrans);




                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                {
                    #region Insert In GL_TransactionHeader

                    //  th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    //th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);


                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        //  td.TransactionID = th.TransactionID;
                        //td.SourceHeaderID = this.StockTransactionID;

                        //td.FiscalID = CurrentFiscalID;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertWO_Detail");
            }

            return result;
        }

        public bool InsertStockTransactionInspectionHederAndDetail(List<INV_StockTransactionDetail> colStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransactionInspection");
            try
            {
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {
                    std.StockTransactionID = this.StockTransactionID;
                    std.FiscalID = CurrentFiscalID;
                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);
                }

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value && th.ControlCredit > 0)
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
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (da != null)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    {
                        da.DocumentNo = this.f_DocumentNo.ToString();
                        da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransactionInspection");
            }

            return result;
        }

        public bool InsertStockTransactionHederAndDetail(List<INV_StockTransactionDetail> colStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, GL_TransactionHeader thBlendFormulation = null,
            INV_StockTransactionHeader sthBlendFomulation = null, List<SAL_DispatchVehicleInformation> ColVehicleStatus = null, Nullable<bool> FG = false,

            int CurrentFiscalID = 0
            , string IssuanceFlow = ""
            , INV_StockTransactionHeader sth_GIN = null, List<INV_StockTransactionDetail> colStockTranDetail_GIN = null
            , GL_TransactionHeader th_GIN = null, List<GL_TransactionDetail> colTranDetail_GIN = null
            )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");



            try
            {
                int sthBlendID = 0;
                long sthBlendDocumentNo = 0;

                if (!FG.Value)
                    this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                sthBlendID = this.StockTransactionID + 1;
                sthBlendDocumentNo = this.DocumentNo + 1;

                if (sthBlendFomulation != null)
                {
                    sthBlendFomulation.StockTransactionID = sthBlendID;
                    sthBlendFomulation.DocumentNo = sthBlendDocumentNo;
                    sthBlendFomulation.FiscalID = CurrentFiscalID;
                    sthBlendFomulation.Insert(sqltrans);
                }

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {

                    if (IssuanceFlow == "FIFO")
                    {
                        #region Check Stock Against Item
                        string filter = "";
                        filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";

                        DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(f_CompanyID, f_PeriodID,
                    std.ItemID, int.Parse(this.LocationID.ToString()), filter, null, null, sqltrans);

                        if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                        {
                            decimal _qtybalanceFrac = Convert.ToDecimal(dt_StockAgainstItem.Rows[0]["Qty"]);

                            if (Math.Abs(decimal.Parse(std.TransactionQuantity.ToString())) > _qtybalanceFrac)
                            {
                                INV_Item objItem = new INV_Item(f_CompanyID, std.ItemID, null, sqltrans);
                                throw new BusinessLogicException(" Stock not available Against item# " + objItem.Description + " ");
                            }

                        }
                        #endregion
                    }

                    if (dr_documentType != null)
                    {
                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        #region Prior Cost

                        DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                            std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        #endregion

                        #region New Cost

                        DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        {
                            decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                            decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                            if (transactionqty != 0)
                                std.NewCost = Convert.ToDouble(amount / transactionqty);
                        }

                        #endregion

                        #region Estimated PO Item Price

                        DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                            std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        #endregion

                        #endregion
                    }

                    std.StockTransactionID = this.StockTransactionID;

                    std.FiscalID = CurrentFiscalID;

                    if (sthBlendFomulation != null)
                        if (std.TransactionQuantity.ToString().Contains("-"))
                            std.StockTransactionID = sthBlendFomulation.StockTransactionID;

                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);

                    #region D.O. Vehicle Information

                    if (ColVehicleStatus != null)
                    {
                        var Records = from objDOVehicle in ColVehicleStatus
                                      where objDOVehicle.RowUniqueID == std.RowUniqueID
                                      select objDOVehicle;

                        foreach (SAL_DispatchVehicleInformation dovi in Records)
                        {
                            DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, dovi.Chassis, "", sqltrans);

                            if (drEngine != null)
                                throw new BusinessLogicException(" Engine #: " + drEngine["Engine"] + " already exists!");

                            DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, dovi.Chassis, " ", sqltrans);

                            if (drChassis != null)
                                throw new BusinessLogicException(" Chassis #: " + drChassis["Chassis"] + " already exists!");

                            dovi.StockHeaderID = std.StockTransactionID;
                            dovi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                            dovi.StockDetailID = std.TransactionDetailID;
                            dovi.DocumentNo = int.Parse(this.f_DocumentNo.ToString());

                            dovi.Insert(sqltrans);
                        }
                    }

                    #endregion
                }

                // Update STD for comments and Isdone

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value && th.ControlCredit > 0
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
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
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;
                        //if (td.TransactionType.ToString().Contains("Credit") && thBlendFormulation != null)
                        //{
                        //    td.TransactionID = thBlendFormulation.TransactionID;
                        //    td.SourceHeaderID = sthBlendFomulation.StockTransactionID;
                        //}
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.f_DocumentNo.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.FiscalID = CurrentFiscalID;
                    da.Insert(sqltrans);
                }

                #endregion


                #region GIN
                if (this.StatusCode == 1 && sth_GIN != null )
                {
                    sth_GIN.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                    //sth_GIN.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_GIN.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans);
                    sth_GIN.DocumentNo = this.DocumentNo;
                    sth_GIN.Insert(sqltrans);


                    foreach (INV_StockTransactionDetail std in colStockTranDetail_GIN)
                    {


                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        #region Prior Cost

                        DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                            std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        #endregion

                        #region New Cost

                        DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        {
                            decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                            decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                            if (transactionqty != 0)
                                std.NewCost = Convert.ToDouble(amount / transactionqty);
                        }

                        #endregion

                        #region Estimated PO Item Price

                        DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                            std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        #endregion

                        #endregion


                        std.StockTransactionID = sth_GIN.StockTransactionID;

                        std.FiscalID = CurrentFiscalID;

                        if (sthBlendFomulation != null)
                            if (std.TransactionQuantity.ToString().Contains("-"))
                                std.StockTransactionID = sthBlendFomulation.StockTransactionID;

                        std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        std.Comments = this.Comments;


                        std.SourceHeaderID = this.StockTransactionID;
                        std.SourceCode = this.DocumentNo.ToString();
                        std.SourceDocumentTypeID = this.DocumentTypeID;
                        std.SourceLineID = null;

                        std.Insert(sqltrans);


                    }

                    // Update STD for comments and Isdone

                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"


                    #region Insert In GL_TransactionHeader

                    th_GIN.DocumentNo = this.DocumentNo;
                    th_GIN.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th_GIN.FiscalID = CurrentFiscalID;
                    th_GIN.Insert(sqltrans);


                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td in colTranDetail_GIN)
                    {
                        td.TransactionID = th_GIN.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion


                    #endregion
                }
                #endregion


                /*
                #region Subsitute tems in/out

                #region For Adjustment IN
                if (sth_Subsitute != null)
                {
                    // Stock Header
                    sth_Subsitute.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_Subsitute.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute.Insert(sqltrans);

                    //Stock Details
                    foreach (INV_StockTransactionDetail std_Sub in colStockTranDetail_Subsitute)
                    {
                        if (dr_documentType != null)
                        {
                            #region Update Prior Cost , New Cost And Estimated PO Item Price

                            #region Prior Cost

                            DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                                std_Sub.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                            #endregion

                            #region New Cost

                            DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                            {
                                decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std_Sub.Amount;
                                decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std_Sub.TransactionQuantity);

                                if (transactionqty != 0)
                                    std_Sub.NewCost = Convert.ToDouble(amount / transactionqty);
                            }

                            #endregion

                            #region Estimated PO Item Price

                            DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                                std_Sub.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                            #endregion

                            #endregion
                        }

                        std_Sub.StockTransactionID = sth_Subsitute.StockTransactionID;
                        std_Sub.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        std_Sub.Comments = "by GRN (Substitute item)";
                        std_Sub.SourceHeaderID = this.StockTransactionID;
                        std_Sub.SourceDocumentTypeID = this.DocumentTypeID;
                        std_Sub.SourceCode = this.DocumentNo.ToString();
                        std_Sub.SourceLineID = null;
                        std_Sub.Insert(sqltrans);


                    }

                    // GL Detail
                    #region Insert In GL_Transaction Header Subsitute


                    #region Insert In GL_TransactionHeader

                    th_Subsitute.DocumentNo = sth_Subsitute.DocumentNo;
                    th_Subsitute.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th_Subsitute.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td_Sub in colTranDetail_Subsitute)
                    {
                        td_Sub.TransactionID = th_Subsitute.TransactionID;
                        td_Sub.SourceHeaderID = sth_Subsitute.StockTransactionID;
                        td_Sub.Insert(sqltrans);
                    }

                    #endregion


                    #endregion


                }

                #endregion


                #region For Adjustment OUT
                if (sth_Subsitute_Out != null)
                {
                    // Stock Header
                    sth_Subsitute_Out.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute_Out.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_Subsitute_Out.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute_Out.Insert(sqltrans);

                    //Stock Details
                    foreach (INV_StockTransactionDetail std_Sub in colStockTranDetail_Subsitute_Out)
                    {
                        if (dr_documentType != null)
                        {
                            #region Update Prior Cost , New Cost And Estimated PO Item Price

                            #region Prior Cost

                            DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                                std_Sub.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                            #endregion

                            #region New Cost

                            DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                            {
                                decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std_Sub.Amount;
                                decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std_Sub.TransactionQuantity);

                                if (transactionqty != 0)
                                    std_Sub.NewCost = Convert.ToDouble(amount / transactionqty);
                            }

                            #endregion

                            #region Estimated PO Item Price

                            DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                                std_Sub.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                            #endregion

                            #endregion
                        }

                        std_Sub.StockTransactionID = sth_Subsitute_Out.StockTransactionID;
                        std_Sub.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        std_Sub.Comments = "by GRN (Substitute item)";
                        std_Sub.SourceHeaderID = this.StockTransactionID;
                        std_Sub.SourceDocumentTypeID = this.DocumentTypeID;
                        std_Sub.SourceCode = this.DocumentNo.ToString();
                        std_Sub.SourceLineID = null;
                        std_Sub.Insert(sqltrans);


                    }

                    // GL Detail
                    #region Insert In GL_Transaction Header Subsitute


                    #region Insert In GL_TransactionHeader

                    th_Subsitute_Out.DocumentNo = sth_Subsitute_Out.DocumentNo;
                    th_Subsitute_Out.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th_Subsitute_Out.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td_Sub in colTranDetail_Subsitute_Out)
                    {
                        td_Sub.TransactionID = th_Subsitute_Out.TransactionID;
                        td_Sub.SourceHeaderID = sth_Subsitute_Out.StockTransactionID;
                        td_Sub.Insert(sqltrans);
                    }

                    #endregion


                    #endregion


                }

                #endregion

                #endregion
                */
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }


        public bool InsertOnlyStockTransactionHederAndDetail(List<INV_StockTransactionDetail> colStockTranDetail, SET_DocumentApprove da, GL_TransactionHeader thBlendFormulation = null,
    INV_StockTransactionHeader sthBlendFomulation = null, List<SAL_DispatchVehicleInformation> ColVehicleStatus = null, Nullable<bool> FG = false,
    int CurrentFiscalID = 0, string IssuanceFlow = "")
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");



            try
            {
                int sthBlendID = 0;
                long sthBlendDocumentNo = 0;

                if (!FG.Value)
                    this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                sthBlendID = this.StockTransactionID + 1;
                sthBlendDocumentNo = this.DocumentNo + 1;

                if (sthBlendFomulation != null)
                {
                    sthBlendFomulation.StockTransactionID = sthBlendID;
                    sthBlendFomulation.DocumentNo = sthBlendDocumentNo;
                    sthBlendFomulation.FiscalID = CurrentFiscalID;
                    sthBlendFomulation.Insert(sqltrans);
                }

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {

                    if (IssuanceFlow == "FIFO")
                    {
                        #region Check Stock Against Item
                        string filter = "";
                        filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";

                        DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(f_CompanyID, f_PeriodID,
                    std.ItemID, int.Parse(this.LocationID.ToString()), filter, null, null, sqltrans);

                        if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                        {
                            decimal _qtybalanceFrac = Convert.ToDecimal(dt_StockAgainstItem.Rows[0]["Qty"]);

                            if (Math.Abs(decimal.Parse(std.TransactionQuantity.ToString())) > _qtybalanceFrac)
                            {
                                INV_Item objItem = new INV_Item(f_CompanyID, std.ItemID, null, sqltrans);
                                throw new BusinessLogicException(" Stock not available Against item# " + objItem.Description + " ");
                            }

                        }
                        #endregion
                    }

                    if (dr_documentType != null)
                    {
                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        #region Prior Cost

                        DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                            std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        #endregion

                        #region New Cost

                        DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        {
                            decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                            decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                            if (transactionqty != 0)
                                std.NewCost = Convert.ToDouble(amount / transactionqty);
                        }

                        #endregion

                        #region Estimated PO Item Price

                        DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                            std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        #endregion

                        #endregion
                    }

                    std.StockTransactionID = this.StockTransactionID;

                    std.FiscalID = CurrentFiscalID;

                    if (sthBlendFomulation != null)
                        if (std.TransactionQuantity.ToString().Contains("-"))
                            std.StockTransactionID = sthBlendFomulation.StockTransactionID;

                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);

                    #region D.O. Vehicle Information

                    if (ColVehicleStatus != null)
                    {
                        var Records = from objDOVehicle in ColVehicleStatus
                                      where objDOVehicle.RowUniqueID == std.RowUniqueID
                                      select objDOVehicle;

                        foreach (SAL_DispatchVehicleInformation dovi in Records)
                        {
                            DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, dovi.Chassis, "", sqltrans);

                            if (drEngine != null)
                                throw new BusinessLogicException(" Engine #: " + drEngine["Engine"] + " already exists!");

                            DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, dovi.Chassis, " ", sqltrans);

                            if (drChassis != null)
                                throw new BusinessLogicException(" Chassis #: " + drChassis["Chassis"] + " already exists!");

                            dovi.StockHeaderID = std.StockTransactionID;
                            dovi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                            dovi.StockDetailID = std.TransactionDetailID;
                            dovi.DocumentNo = int.Parse(this.f_DocumentNo.ToString());

                            dovi.Insert(sqltrans);
                        }
                    }

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.f_DocumentNo.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }


        public bool InsertStockTransactionHederAndDetailConsumption(List<INV_StockTransactionDetail> colStockTranDetail,
            GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 0, SqlTransaction sql = null
           )
        {
            bool result = false;
            SqlTransaction sqltrans = sql;

            try
            {

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {
                    std.StockTransactionID = this.StockTransactionID;
                    std.FiscalID = CurrentFiscalID;
                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    )
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
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;
                        td.FiscalID = CurrentFiscalID;
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
                    SqlHelper.RollBackTransaction(sqltrans, "Consumption");
            }

            return result;
        }


        public bool InsertStockTransactionDetail(List<INV_StockTransactionDetail> colStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, int CurrentFiscalID, string IssuanceFlow = "")
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");

            try
            {
                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);


                foreach (var colItem in colStockTranDetail)
                {
                    this.f_StockTransactionID = colItem.StockTransactionID;
                }



                INV_StockTransactionDetail.DeleteDataByHeaderKeyFIFO(sqltrans, f_StockTransactionID);


                int ItemId = 0;
                decimal ItemStock = 0;
                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {
                    if (IssuanceFlow == "FIFO")
                    {
                        #region Check Stock Against Item
                        string filter = "";
                        DataTable dt_StockAgainstItem;

                        if (ItemId == 0 || ItemId != std.ItemID)
                        {
                            ItemId = std.ItemID;
                            filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
                            dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(f_CompanyID, f_PeriodID,
                        std.ItemID, int.Parse(this.LocationID.ToString()), filter, null, null, sqltrans);

                            if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                            {
                                ItemStock = Convert.ToDecimal(dt_StockAgainstItem.Rows[0]["Qty"]);
                            }
                        }

                        if (Math.Abs(decimal.Parse(std.TransactionQuantity.ToString())) > ItemStock)
                        {
                            if (std.Status)
                            {
                                INV_Item objItem = new INV_Item(f_CompanyID, std.ItemID, null, sqltrans);
                                throw new BusinessLogicException(" Stock not available Against item# " + objItem.Description + " ");
                            }
                        }
                        ItemStock = ItemStock - Math.Abs(decimal.Parse(std.TransactionQuantity.ToString()));
                        ItemId = std.ItemID;

                        #endregion
                    }

                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = f_DocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    //if (thBlendFormulation != null)
                    //{
                    //    thBlendFormulation.DocumentNo = sthBlendDocumentNo;
                    //    thBlendFormulation.TransactionID = th.TransactionID + 1;
                    //    thBlendFormulation.Insert(sqltrans);
                    //}


                    #endregion

                    #region Insert In GL_TransactionDetail


                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;
                        //if (td.TransactionType.ToString().Contains("Credit") && thBlendFormulation != null)
                        //{
                        //    td.TransactionID = thBlendFormulation.TransactionID;
                        //    td.SourceHeaderID = sthBlendFomulation.StockTransactionID;
                        //}
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.f_DocumentNo.ToString();
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }


        public bool InsertStockTransactionHeaderDetail(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colStockTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockRecord");

            try
            {
                //this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans);
                //sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.Insert(sqltrans);

                int transactionDetailId = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);


                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {

                    #region Update Prior Cost , New Cost And Estimated PO Item Price

                    #region Prior Cost

                    DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                    if (dt_LastTransactionCost != null)
                    {
                        if (dt_LastTransactionCost.Rows.Count > 0)
                        {
                            std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());
                        }
                    }

                    #endregion

                    #region New Cost

                    DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID);

                    if (dr_documentType != null)
                    {
                        if (dr_documentType["TransactionImpact"].ToString() == "Positive")
                        {
                            DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);
                            if (dt_GetAmountTransactionQty != null)
                            {
                                if (dt_GetAmountTransactionQty.Rows.Count > 0)
                                {
                                    decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                                    decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);
                                    std.NewCost = Convert.ToDouble(amount / transactionqty);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Estimated PO Item Price

                    DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);
                    if (dt_estimatedpoitemprice != null)
                    {
                        std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());
                    }

                    #endregion

                    #endregion

                    //std.StockTransactionID = this.StockTransactionID;
                    std.TransactionDetailID = transactionDetailId++;
                    std.Insert(sqltrans);
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockRecord");
            }

            return result;
        }

        public bool UpdateStockTransactionHeaderDetail(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colStockTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockRecord");

            try
            {
                //this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans);
                //sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.UpdateDataByKey(sqltrans);

                //int transactionDetailId = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);


                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {

                    #region Update Prior Cost , New Cost And Estimated PO Item Price

                    #region Prior Cost

                    DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                    if (dt_LastTransactionCost != null)
                    {
                        if (dt_LastTransactionCost.Rows.Count > 0)
                        {
                            std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());
                        }
                    }

                    #endregion

                    #region New Cost

                    DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID);

                    if (dr_documentType != null)
                    {
                        if (dr_documentType["TransactionImpact"].ToString() == "Positive")
                        {
                            DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);
                            if (dt_GetAmountTransactionQty != null)
                            {
                                if (dt_GetAmountTransactionQty.Rows.Count > 0)
                                {
                                    decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                                    decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);
                                    std.NewCost = Convert.ToDouble(amount / transactionqty);
                                }
                            }
                        }
                    }

                    #endregion

                    #region Estimated PO Item Price

                    DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);
                    if (dt_estimatedpoitemprice != null)
                    {
                        std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());
                    }

                    #endregion

                    #endregion

                    //std.StockTransactionID = this.StockTransactionID;
                    //std.TransactionDetailID = transactionDetailId++;
                    std.UpdateDataByKey(sqltrans);
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockRecord");
            }

            return result;
        }

        public bool UpdateStockTransactionInspectionHederAndDetail(List<INV_StockTransactionDetail> colAddStockTranDetail,
           List<INV_StockTransactionDetail> colUpdateStockTranDetail, List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
           List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransactionInspection");

            try
            {
                this.UpdateDataByKey(sqltrans);

                if (colAddStockTranDetail != null)
                {
                    foreach (INV_StockTransactionDetail stdAdd in colAddStockTranDetail)
                    {
                        stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        stdAdd.FiscalID = CurrentFiscalID;
                        stdAdd.Insert(sqltrans);
                    }
                }

                if (colUpdateStockTranDetail != null)
                {
                    foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                    {
                        stdUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                if (colDeleteStockTranDetail != null)
                {
                    foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                    {
                        INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);
                    }
                }

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value && th.ControlCredit > 0)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && th.ControlCredit > 0)
                    {
                        #region Insert In GL_TransactionHeader

                        th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th.FiscalID = CurrentFiscalID;
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
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (da != null)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    {
                        da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                        da.DocumentNo = this.DocumentNo.ToString();
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransactionInspection");
            }

            return result;
        }

        public bool UpdateStockTransactionHederAndDetail(List<INV_StockTransactionDetail> colAddStockTranDetail,
            List<INV_StockTransactionDetail> colUpdateStockTranDetail, List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colAddTranDetail, bool isAccountGIN, SET_DocumentApprove da, List<SAL_DispatchVehicleInformation> colVehicleStatus = null,
            int CurrentFiscalID = 0
            , string IssuanceFlow = ""
            , INV_StockTransactionHeader sth_GIN = null, List<INV_StockTransactionDetail> colStockTranDetail_GIN = null
            , GL_TransactionHeader th_GIN = null, List<GL_TransactionDetail> colTranDetail_GIN = null

)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                if (isAccountGIN)
                {
                    #region Update "AccountGINCode" In GL_TransactionDetail For Account GIN Form

                    foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                        stdUpdate.UpdateDataByKeyAccountGINCode(sqltrans);



                    if (th != null)
                    {
                        #region Insert In GL_TransactionHeader

                        if (th.TransactionID > 0)
                            th.UpdateDataByKey(sqltrans);
                        else
                        {
                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(th.CompanyID, th.PeriodID, sqltrans);
                            th.FiscalID = CurrentFiscalID;
                            th.Insert(sqltrans);
                        }

                        #endregion

                        #region Delete from GL_TransactionDetail, if already exists

                        if (th.TransactionID > 0)
                            GL_TransactionDetail.DeleteDetailDataByKey(sqltrans, th.CompanyID, th.PeriodID, th.TransactionID, th.DefaultChartofAccountID.Value);

                        #endregion

                        #region Insert In GL_TransactionDetail

                        decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                        foreach (GL_TransactionDetail td in colAddTranDetail)
                        {
                            td.TransactionID = th.TransactionID;
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }

                    #endregion
                }
                else
                {
                    //this.UpdateDataByKey(sqltrans);

                    if (colAddStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdAdd in colAddStockTranDetail)
                        {
                            if (IssuanceFlow == "FIFO")
                            {
                                #region Check Stock Against Item
                                string filter = "";
                                filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";

                                DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(f_CompanyID, f_PeriodID,
                            stdAdd.ItemID, int.Parse(this.LocationID.ToString()), filter, null, null, sqltrans);

                                if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                                {
                                    decimal _qtybalanceFrac = Convert.ToDecimal(dt_StockAgainstItem.Rows[0]["Qty"]);

                                    if (Math.Abs(decimal.Parse(stdAdd.TransactionQuantity.ToString())) > _qtybalanceFrac)
                                    {
                                        INV_Item objItem = new INV_Item(f_CompanyID, stdAdd.ItemID, null, sqltrans);
                                        throw new BusinessLogicException(" Stock not available Against item# " + objItem.Description + " ");
                                    }

                                }
                                #endregion
                            }



                            stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                            stdAdd.FiscalID = CurrentFiscalID;
                            stdAdd.Insert(sqltrans);

                            #region Dispatch Vehicle Information
                            if (colVehicleStatus != null)
                            {
                                var Records = from objDOVehicle in colVehicleStatus
                                              where objDOVehicle.RowUniqueID == stdAdd.RowUniqueID
                                              select objDOVehicle;
                                foreach (SAL_DispatchVehicleInformation sdvi in Records)
                                {
                                    DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                    if (drEngine != null)
                                    {
                                        throw new BusinessLogicException(" this Engine # '" + drEngine["Engine"] + "' Already Exits !");
                                    }
                                    DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                    if (drChassis != null)
                                    {
                                        throw new BusinessLogicException(" this Chassis # '" + drChassis["Chassis"] + "' Already Exits !");
                                    }
                                    sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                    sdvi.StockHeaderID = this.StockTransactionID;
                                    sdvi.StockDetailID = stdAdd.TransactionDetailID;

                                    sdvi.Insert(sqltrans);
                                }
                            }
                            #endregion

                        }
                    }

                    if (colUpdateStockTranDetail != null)
                    {
                        int ItemId = 0;
                        decimal ItemStock = 0;
                        foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                        {


                            if (IssuanceFlow == "FIFO")
                            {
                                #region Check Stock Against Item
                                string filter = "";
                                DataTable dt_StockAgainstItem;

                                if (ItemId == 0 || ItemId != stdUpdate.ItemID)
                                {
                                    ItemId = stdUpdate.ItemID;
                                    filter = "Order By INV_StockTransactionHeader.DocumentDate Asc";
                                    dt_StockAgainstItem = INV_StockTransactionDetail.Get_QTYFIFOByLocation(f_CompanyID, f_PeriodID,
                                stdUpdate.ItemID, int.Parse(this.LocationID.ToString()), filter, null, null, sqltrans);

                                    if (dt_StockAgainstItem != null && dt_StockAgainstItem.Rows.Count > 0)
                                    {
                                        ItemStock = Convert.ToDecimal(dt_StockAgainstItem.Rows[0]["Qty"]);
                                    }
                                }

                                if (Math.Abs(decimal.Parse(stdUpdate.TransactionQuantity.ToString())) > ItemStock)
                                {
                                    if (stdUpdate.Status)
                                    {
                                        INV_Item objItem = new INV_Item(f_CompanyID, stdUpdate.ItemID, null, sqltrans);
                                        throw new BusinessLogicException(" Stock not available Against item# " + objItem.Description + " ");
                                    }
                                }
                                ItemStock = ItemStock - Math.Abs(decimal.Parse(stdUpdate.TransactionQuantity.ToString()));
                                ItemId = stdUpdate.ItemID;

                                #endregion
                            }

                            stdUpdate.UpdateDataByKey(sqltrans);

                            #region Dispatch Vehicle Information Delete By StockDetail ID
                            if (colVehicleStatus != null)
                            {
                                if (colVehicleStatus.Count > 0 && colUpdateStockTranDetail.Count > 0)
                                {
                                    SAL_DispatchVehicleInformation.DeleteDataByStockDetailID(sqltrans, CompanyID, PeriodID, stdUpdate.TransactionDetailID);
                                }
                            }
                            #endregion


                            #region Dispatch Vehicle Information
                            if (colVehicleStatus != null)
                            {
                                var Records = from objDOVehicle in colVehicleStatus
                                              where objDOVehicle.RowUniqueID == stdUpdate.RowUniqueID
                                              select objDOVehicle;
                                foreach (SAL_DispatchVehicleInformation sdvi in Records)
                                {
                                    DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                    if (drChassis != null)
                                    {
                                        throw new BusinessLogicException(" this Chassis # '" + drChassis["Chassis"] + "' Already Exits !");
                                    }

                                    DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, sdvi.Engine, "", sqltrans);
                                    if (drChassis != null)
                                    {
                                        throw new BusinessLogicException(" this Engine # '" + drEngine["Engine"] + "' Already Exits !");
                                    }

                                    sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                    sdvi.StockHeaderID = this.StockTransactionID;
                                    sdvi.StockDetailID = stdUpdate.TransactionDetailID;
                                    sdvi.Insert(sqltrans);
                                }
                            }
                            #endregion


                        }
                    }
                    this.UpdateDataByKey(sqltrans);
                    if (colDeleteStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                        {
                            INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

                            #region Dispatch Vehicle Information
                            if (colVehicleStatus != null)
                            {
                                SAL_DispatchVehicleInformation.DeleteDataByStockDetailID(sqltrans, CompanyID, PeriodID, stdDelete.TransactionDetailID);
                            }
                            #endregion
                        }
                    }
                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                    if (th != null)
                    {
                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && th.ControlCredit > 0)
                        {
                            #region Insert In GL_TransactionHeader

                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th.FiscalID = CurrentFiscalID;
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
                                td.FiscalID = CurrentFiscalID;
                                td.Insert(sqltrans);
                            }

                            #endregion
                        }
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



                    #region GIN
                    if (this.StatusCode == 1 && sth_GIN != null)
                    {
                        sth_GIN.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                        //sth_GIN.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_GIN.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans);
                        sth_GIN.DocumentNo = this.DocumentNo;
                        sth_GIN.Insert(sqltrans);


                        foreach (INV_StockTransactionDetail std in colStockTranDetail_GIN)
                        {


                            #region Update Prior Cost , New Cost And Estimated PO Item Price

                            #region Prior Cost

                            DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                                std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                            #endregion

                            #region New Cost

                            DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                            {
                                decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                                decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                                if (transactionqty != 0)
                                    std.NewCost = Convert.ToDouble(amount / transactionqty);
                            }

                            #endregion

                            #region Estimated PO Item Price

                            DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                            if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                                std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                            #endregion

                            #endregion


                            std.StockTransactionID = sth_GIN.StockTransactionID;

                            std.FiscalID = CurrentFiscalID;

                            std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                            std.Comments = this.Comments;


                            std.SourceHeaderID = this.StockTransactionID;
                            std.SourceCode = this.DocumentNo.ToString();
                            std.SourceDocumentTypeID = this.DocumentTypeID;
                            std.SourceLineID = null;

                            std.Insert(sqltrans);


                        }

                        // Update STD for comments and Isdone

                        #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"


                        #region Insert In GL_TransactionHeader

                        th_GIN.DocumentNo = this.DocumentNo;
                        th_GIN.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th_GIN.FiscalID = CurrentFiscalID;
                        th_GIN.Insert(sqltrans);


                        #endregion

                        #region Insert In GL_TransactionDetail

                        foreach (GL_TransactionDetail td in colTranDetail_GIN)
                        {
                            td.TransactionID = th_GIN.TransactionID;
                            td.SourceHeaderID = this.StockTransactionID;
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion


                        #endregion
                    }
                    #endregion
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }



        public bool UpdateStockTransactionHederAndDetailIsVehicle(List<INV_StockTransactionDetail> colAddStockTranDetail,
            List<INV_StockTransactionDetail> colUpdateStockTranDetail, List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colAddTranDetail, bool isAccountGIN, SET_DocumentApprove da, List<SAL_DispatchVehicleInformation> colVehicleStatus = null,
            int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                if (isAccountGIN)
                {
                    #region Update "AccountGINCode" In GL_TransactionDetail For Account GIN Form

                    foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                        stdUpdate.UpdateDataByKeyTransactionQuantity(sqltrans);


                    if (th != null)
                    {
                        #region Update In GL_TransactionHeader

                        if (th.TransactionID > 0)
                            th.UpdateDataByKey(sqltrans);
                        //else
                        //{
                        //    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(th.CompanyID, th.PeriodID, sqltrans);
                        //    th.Insert(sqltrans);
                        //}

                        // GL Transaction


                        decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                        foreach (GL_TransactionDetail GlUpdate in colAddTranDetail)
                            GlUpdate.UpdateAmountByTransactionID(sqltrans);

                        #endregion

                        //#region Delete from GL_TransactionDetail, if already exists

                        //if (th.TransactionID > 0)
                        //    GL_TransactionDetail.DeleteDetailDataByKey(sqltrans, th.CompanyID, th.PeriodID, th.TransactionID, th.DefaultChartofAccountID.Value);

                        //#endregion

                        //#region Insert In GL_TransactionDetail

                        //foreach (GL_TransactionDetail td in colAddTranDetail)
                        //{
                        //    td.TransactionID = th.TransactionID;
                        //    td.Insert(sqltrans);
                        //}

                        //#endregion
                    }

                    #endregion
                }
                else
                {
                    this.UpdateDataByKey(sqltrans);

                    foreach (INV_StockTransactionDetail stdAdd in colAddStockTranDetail)
                    {
                        stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        stdAdd.FiscalID = CurrentFiscalID;
                        stdAdd.Insert(sqltrans);

                        #region Dispatch Vehicle Information
                        if (colVehicleStatus != null)
                        {
                            var Records = from objDOVehicle in colVehicleStatus
                                          where objDOVehicle.RowUniqueID == stdAdd.RowUniqueID
                                          select objDOVehicle;
                            foreach (SAL_DispatchVehicleInformation sdvi in Records)
                            {
                                DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                if (drEngine != null)
                                {
                                    throw new BusinessLogicException(" this Engine # '" + drEngine["Engine"] + "' Already Exits !");
                                }
                                DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                if (drChassis != null)
                                {
                                    throw new BusinessLogicException(" this Chassis # '" + drChassis["Chassis"] + "' Already Exits !");
                                }
                                sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                sdvi.StockHeaderID = this.StockTransactionID;
                                sdvi.StockDetailID = stdAdd.TransactionDetailID;
                                sdvi.Insert(sqltrans);
                            }
                        }
                        #endregion

                    }


                    foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                    {
                        stdUpdate.UpdateDataByKey(sqltrans);

                        #region Dispatch Vehicle Information Delete By StockDetail ID
                        if (colVehicleStatus != null)
                        {
                            if (colVehicleStatus.Count > 0 && colUpdateStockTranDetail.Count > 0)
                            {
                                SAL_DispatchVehicleInformation.DeleteDataByStockDetailID(sqltrans, CompanyID, PeriodID, stdUpdate.TransactionDetailID);
                            }
                        }
                        #endregion


                        #region Dispatch Vehicle Information
                        if (colVehicleStatus != null)
                        {
                            var Records = from objDOVehicle in colVehicleStatus
                                          where objDOVehicle.RowUniqueID == stdUpdate.RowUniqueID
                                          select objDOVehicle;
                            foreach (SAL_DispatchVehicleInformation sdvi in Records)
                            {
                                DataRow drChassis = SAL_DispatchVehicleInformation.GetDataByChassisNo(CompanyID, PeriodID, sdvi.Chassis, "", sqltrans);
                                if (drChassis != null)
                                {
                                    throw new BusinessLogicException(" this Chassis # '" + drChassis["Chassis"] + "' Already Exits !");
                                }

                                DataRow drEngine = SAL_DispatchVehicleInformation.GetDataByEngineNo(CompanyID, PeriodID, sdvi.Engine, "", sqltrans);
                                if (drChassis != null)
                                {
                                    throw new BusinessLogicException(" this Engine # '" + drEngine["Engine"] + "' Already Exits !");
                                }

                                sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                sdvi.StockHeaderID = this.StockTransactionID;
                                sdvi.StockDetailID = stdUpdate.TransactionDetailID;
                                sdvi.Insert(sqltrans);
                            }
                        }
                        #endregion


                    }


                    foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                    {
                        INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

                        #region Dispatch Vehicle Information
                        if (colVehicleStatus != null)
                        {
                            SAL_DispatchVehicleInformation.DeleteDataByStockDetailID(sqltrans, CompanyID, PeriodID, stdDelete.TransactionDetailID);
                        }
                        #endregion
                        //DeleteDataByStockDetailID
                    }
                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                    if (th != null)
                    {
                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            #region Insert In GL_TransactionHeader

                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th.FiscalID = CurrentFiscalID;
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
                                td.FiscalID = CurrentFiscalID;
                                td.Insert(sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion

                    #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                    {
                        da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
                    }

                    #endregion
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public bool DeleteStockTransactionHederAndDetail(List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colDeleteTranDetail, bool IsVehicle = false)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteStockTransaction");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, f_StockTransactionID);

                INV_StockTransactionDetail.DeleteDataByHeaderKeyForStockRequisition(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_StockTransactionID, f_CompanyID);

                if (IsVehicle == true)
                    SAL_DispatchVehicleInformation.DeleteDataByStockHeaderID(sqltrans, CompanyID, PeriodID, f_StockTransactionID);
                //foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                //    INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteStockTransaction");
            }
            return result;
        }

        public bool DeleteStockTransactionDetail(List<INV_StockTransactionDetail> colDeleteStockTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteStockTransaction");

            try
            {
                //DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, f_StockTransactionID);

                foreach (var colItem in colDeleteStockTranDetail)
                {
                    this.f_StockTransactionID = colItem.StockTransactionID;
                }



                INV_StockTransactionDetail.DeleteDataByHeaderKeyFIFO(sqltrans, f_StockTransactionID);

                //if (IsVehicle == true)
                //    SAL_DispatchVehicleInformation.DeleteDataByStockHeaderID(sqltrans, CompanyID, PeriodID, f_StockTransactionID);
                //foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                //    INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteStockTransaction");
            }
            return result;
        }

        public bool UpdateStockHeaderForApprove(GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove dA, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && colAddTranDetail != null)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        #region Insert In GL_TransactionHeader

                        th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th.FiscalID = CurrentFiscalID;
                        th.StatusCode = this.StatusCode;
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
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }
                }

                #endregion

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                {
                    dA.FiscalID = CurrentFiscalID;
                    dA.UpdateDataByKeyStatus(sqltrans);
                }

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                {
                    dA.FiscalID = CurrentFiscalID;
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);
                }

                #endregion

                #region Insert In SET_DocumentApprove

                // delete after disscus
                if (dA.ApprovalStatus == "Discuss")
                    SET_DocumentApprove.DeleteDataByDocNoanddocTypeid(f_CompanyID, f_PeriodID, int.Parse(dA.DocumentNo), dA.DocumentTypeID, sqltrans, CurrentFiscalID);

                dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(f_CompanyID, f_PeriodID, sqltrans);
                dA.FiscalID = CurrentFiscalID;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public static DataRow GetDataByBusinessPartnerID(Nullable<int> BpartnerID, int periodID, int companyID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@BpartnerID", BpartnerID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_FromLook_GateEntry, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public static DataTable GetRequistionForRfq(int companyId, int peridId)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ForRFQLookup, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetMaxDocumentNumber(int companyId, int peridId, SqlTransaction sTrans = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);
                if (sTrans == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetMaxDocumentNumber, sqlparams).Tables[0];
                else

                    dt = SqlHelper.ExecuteDataset(sTrans, CommandType.Text, Query_GetMaxDocumentNumber, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool UpdateStockHeaderForBottling(int companyId, int peridId, int recipeID, string docTypeId)
        {
            bool result = false;
            StringBuilder sb = new StringBuilder();
            SqlParameter[] sqlparams = new SqlParameter[1];
            sqlparams[0] = new SqlParameter("@RecipeID", recipeID);
            sb.Append(" And CompanyId  = " + companyId.ToString() + " And PeriodId  = " + peridId.ToString() + " And DocumentTypeID in " + docTypeId);

            if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateStockHeaderForBottling + sb.ToString(), sqlparams) > 0)
                result = true;
            return result;

        }

        public static DataTable GetSuppllierFromSRForRfq(int companyId, int peridId, int stockTransactionID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStrockReqSupplierForRFQ, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetStockRequisitionNoValue(int companyId, int periodId, int fk_INV_StockTransactionID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@FK_INV_StockTransactionID", fk_INV_StockTransactionID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetStockRequisitionNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetStockRequisitionNo, sqlparams);

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

        public static DataTable GetRecordForTransferOut(int companyId, int peridId, int stockTransactionID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRecordForTransferOut, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateHeaderForApprove(SET_DocumentApprove obj_DocumentApprove, GL_TransactionHeader obj_GL_Header = null,
            List<GL_TransactionDetail> lst_GL_Details = null)
        {
            bool result = true;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStock");

            try
            {
                if (!this.UpdateDataByKey(sqltrans))
                    result = false;

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (obj_GL_Header != null)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        #region Insert In GL_TransactionHeader

                        obj_GL_Header.DocumentNo = this.f_DocumentNo;
                        obj_GL_Header.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        obj_GL_Header.Insert(sqltrans);

                        #endregion

                        #region Insert In GL_TransactionDetail


                        decimal AmountDebit = Math.Round(lst_GL_Details.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(lst_GL_Details.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                        foreach (GL_TransactionDetail obj_GL_Detail in lst_GL_Details)
                        {
                            obj_GL_Detail.SourceHeaderID = this.f_StockTransactionID;
                            obj_GL_Detail.TransactionID = obj_GL_Header.TransactionID;
                            obj_GL_Detail.Insert(sqltrans);
                        }

                        #endregion
                    }
                }

                #endregion

                if (!obj_DocumentApprove.Insert(sqltrans))
                    result = false;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStock");
            }

            return result;
        }

        public static DataTable Select_StockRequisition_LookupForTransferOut(int companyID, int periodID, string filter = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                string query = Query_Select_StockRequisition_LookupForTransferOut.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetData_StoreIssueReport(int companyID, int periodID, int? GINNumberFrom, int? GINNumberTo, int? DepartmentId, int CosterCenterId, int incurredOnId, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[6];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (DepartmentId == -1)
                {

                    sqlParms[3] = new SqlParameter("@DepartmentID", DBNull.Value);
                }
                else
                {
                    sqlParms[3] = new SqlParameter("@DepartmentID", DepartmentId);
                }

                if (CosterCenterId == -1)
                {

                    sqlParms[4] = new SqlParameter("@CostCenterId", DBNull.Value);
                }
                else
                {
                    sqlParms[4] = new SqlParameter("@CostCenterId", CosterCenterId);
                }


                if (incurredOnId == -1)
                {

                    sqlParms[5] = new SqlParameter("@IncurredOnID", DBNull.Value);
                }
                else
                {
                    sqlParms[5] = new SqlParameter("@IncurredOnID", incurredOnId);
                }




                //if (DepartmentId!=-1)               
                //    additionalparameter = additionalparameter + " AND [SET_Department].[DepartmentId] = '" + DepartmentId + "'" ;
                //else
                //    additionalparameter = additionalparameter + " AND [SET_Department].[DepartmentId] = '" + DBNull.Value + "'";

                if (GINNumberFrom.HasValue && GINNumberTo.HasValue)

                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] BETWEEN '" + GINNumberFrom + "' AND '" + GINNumberTo + "'";
                else if (GINNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberFrom + "'";
                else if (GINNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberTo + "'";




                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                additionalparameter = additionalparameter + " order by INV_StockTransactionHeader.DocumentNo ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_StoreIssueReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetData_DirectInvoiceReport(int companyID, int periodID, int? ginNumberFrom, int? ginNumberTo, int? departmentID, int costerCenterID,
            int incurredOnID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (departmentID == -1)
                    sqlParms[2] = new SqlParameter("@DepartmentID", DBNull.Value);
                else
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID);

                if (costerCenterID == -1)
                    sqlParms[3] = new SqlParameter("@CostCenterId", DBNull.Value);
                else
                    sqlParms[3] = new SqlParameter("@CostCenterId", costerCenterID);

                if (incurredOnID == -1)
                    sqlParms[4] = new SqlParameter("@IncurredOnID", DBNull.Value);
                else
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID);

                #region Filter by GIN number

                if (ginNumberFrom.HasValue && ginNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DocumentNo BETWEEN " + ginNumberFrom + " AND " + ginNumberTo;
                else if (ginNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DocumentNo = " + ginNumberFrom;
                else if (ginNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DocumentNo = " + ginNumberTo;

                #endregion

                #region Filter by Date Range

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalparameter += " AND AR_InvoiceHeader.InvoiceDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ";
                }
                else if (fromDate.HasValue)
                    additionalparameter += " AND AR_InvoiceHeader.InvoiceDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ";
                else if (toDate.HasValue != null)
                    additionalparameter += " AND AR_InvoiceHeader.InvoiceDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DirectInvoiceReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetData_StoreRecevingNote(int companyID, int periodID, int? GINNumberFrom, int? GINNumberTo, int? DepartmentId, int CosterCenterId, int incurredOnId, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[6];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (DepartmentId == -1)
                {

                    sqlParms[3] = new SqlParameter("@DepartmentID", DBNull.Value);
                }
                else
                {
                    sqlParms[3] = new SqlParameter("@DepartmentID", DepartmentId);
                }


                if (incurredOnId == -1)
                {

                    sqlParms[4] = new SqlParameter("@IncurredOnID", DBNull.Value);
                }
                else
                {
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnId);
                }


                if (CosterCenterId == -1)
                {

                    sqlParms[5] = new SqlParameter("@CostCenterId", DBNull.Value);
                }
                else
                {
                    sqlParms[5] = new SqlParameter("@CostCenterId", CosterCenterId);
                }






                //if (DepartmentId!=-1)               
                //    additionalparameter = additionalparameter + " AND [SET_Department].[DepartmentId] = '" + DepartmentId + "'" ;
                //else
                //    additionalparameter = additionalparameter + " AND [SET_Department].[DepartmentId] = '" + DBNull.Value + "'";




                if (GINNumberFrom.HasValue && GINNumberTo.HasValue)

                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] BETWEEN '" + GINNumberFrom + "' AND '" + GINNumberTo + "'";
                else if (GINNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberFrom + "'";
                else if (GINNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberTo + "'";




                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_StoreRecevingNote + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetData_StoreRecevingReport(int companyID, int periodID, int? GINNumberFrom, int? GINNumberTo, int? DepartmentId, int? CustomerId, int CosterCenterId, int incurredOnId, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[6];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (DepartmentId == -1)
                {

                    sqlParms[2] = new SqlParameter("@DepartmentID", DBNull.Value);
                }
                else
                {
                    sqlParms[2] = new SqlParameter("@DepartmentID", DepartmentId);
                }

                if (CustomerId == -1)
                {

                    sqlParms[3] = new SqlParameter("@Supplierid", DBNull.Value);
                }
                else
                {
                    sqlParms[3] = new SqlParameter("@Supplierid", CustomerId);
                }


                if (incurredOnId == -1)
                {

                    sqlParms[4] = new SqlParameter("@IncurredOnID", DBNull.Value);
                }
                else
                {
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnId);
                }


                if (CosterCenterId == -1)
                {

                    sqlParms[5] = new SqlParameter("@CostCenterId", DBNull.Value);
                }
                else
                {
                    sqlParms[5] = new SqlParameter("@CostCenterId", CosterCenterId);
                }



                if (GINNumberFrom.HasValue && GINNumberTo.HasValue)

                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] BETWEEN '" + GINNumberFrom + "' AND '" + GINNumberTo + "'";
                else if (GINNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberFrom + "'";
                else if (GINNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberTo + "'";


                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_StoreRecevingReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetData_GoodsIssueNoteReport(int companyID, int periodID, int CurrentFiscalID, int? itemCategoryID, int? GINNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null
          , Nullable<int> DepartmentID = null, Nullable<int> CostCenterID = null, short? documentGroupID = null, Nullable<int> ApprovalGroupID = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];
                StringBuilder sb = new StringBuilder();

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FiscalID", CurrentFiscalID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;

                if (GINNumber.HasValue)
                {
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] =" + GINNumber;
                }

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;
                if (DepartmentID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID;
                if (CostCenterID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID;

                if (documentGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovalGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_Item.ApprovalItemGroupID  = " + ApprovalGroupID;

                additionalparameter = additionalparameter + "    ORDER BY INV_Item.InventoryItemStatusCode";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GoodsIssueNoteReport + additionalparameter), sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetData_DailyIssueReport(int companyID, int periodID, int? itemCategoryID, int? employeeID, int? GINNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null
          , Nullable<int> DepartmentID = null, Nullable<int> CostCenterID = null, short? documentGroupID = null, Nullable<int> ApprovalGroupID = null,bool Consumption = false)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];
                StringBuilder sb = new StringBuilder();

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;
                if (employeeID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + employeeID;

                if (GINNumber.HasValue)
                {
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] =" + GINNumber;
                }

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;
                if (DepartmentID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID;
                if (CostCenterID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID;

                if (documentGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovalGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_Item.ApprovalItemGroupID  = " + ApprovalGroupID;

                if (Consumption)
                {
                    additionalparameter = additionalparameter + " and  AR_SET_Customer.CustomerName is not null ";
                }
                additionalparameter = additionalparameter + "    Order by  INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate";



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DailyIssueReport + additionalparameter), sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetData_DailyIssueReports(int companyID, int periodID, int? itemCategoryID, int? employeeID, int? GINNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null
         , Nullable<int> DepartmentID = null, Nullable<int> CostCenterID = null, short? documentGroupID = null, Nullable<int> ApprovalGroupID = null, Nullable<int> DemandType = null, Nullable<int> SeasonType = null, bool Consumption = false, Nullable<int> itemid = null, string DocumentStatus = "-1", Nullable<int> jobID = null)
        {
            DataSet dtresult = null;
            //DataTable dtresult = null;
            string additionalparameter = string.Empty;
            string filter1 = string.Empty;
            string filter2 = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];
                StringBuilder sb = new StringBuilder();

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = (new SqlParameter("@FromDate", DateFrom));


                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;
                if (itemid.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemID] = " +  itemid;

                if (jobID.HasValue)
                {
                    additionalparameter = additionalparameter + " AND isnull(Set_Job.JobID,0) = " + jobID;
                }

                if (DocumentStatus != "-1")
                additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].StatusCode = " + DocumentStatus;

                if (employeeID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + employeeID;

                if (GINNumber.HasValue)
                {
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] =" + GINNumber;
                }

                if (DateFrom != null && DateTo != null)
                    filter1 = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    filter1 = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    filter1 = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                if (DateFrom != null)
                {
                    filter2 = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] < '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                }
                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;
                if (DepartmentID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID;
                if (CostCenterID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID;

                if (documentGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovalGroupID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_Item.ApprovalItemGroupID  = " + ApprovalGroupID;

                if (DemandType > 0)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DemandType = " + DemandType;
                    //sb.Append(" AND INV_StockTransactionHeader.DemandType = " + DemandType);

                if (SeasonType > 0)
                    additionalparameter = additionalparameter + "AND INV_StockTransactionHeader.SeasonType = " + SeasonType;
                    //sb.Append(" AND INV_StockTransactionHeader.SeasonType = " + SeasonType);

                if (Consumption)
                {
                    additionalparameter = additionalparameter + " and  AR_SET_Customer.CustomerName is not null ";
                }
                //additionalparameter = additionalparameter + "    Order by  INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate";


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyIssueReportsSSML, additionalparameter, filter1, filter2), sqlParms);
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyIssueReports, additionalparameter), sqlParms);
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DailyIssueReports + additionalparameter), sqlParms);

                //if (ds.Tables.Count > 0)
                //    return ds.Tables[0];

                if (ds != null && ds.Tables.Count > 1)
                    dtresult = ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetData_GoodsReturnNoteReport(int companyID, int periodID, int? itemCategoryID, int? GINNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;

                if (GINNumber.HasValue)
                {
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] =" + GINNumber;
                }

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GoodsReturnNoteReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetData_InspectionReport(int companyID, int periodID, int? BatchID, int? ReportNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (BatchID.HasValue)
                    sqlParms[2] = new SqlParameter("@BatchID", BatchID);
                else
                    sqlParms[2] = new SqlParameter("@BatchID", DBNull.Value);

                if (ReportNumber.HasValue)
                {
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] =" + ReportNumber;
                }

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND Cast([INV_StockTransactionHeader].[DocumentDate] as Date) BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND Cast([INV_StockTransactionHeader].[DocumentDate] as Date) >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND Cast([INV_StockTransactionHeader].[DocumentDate] as Date) <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_InspectionReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetProcurementTracking(int companyID, int periodID, int? requisitionNumber, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
            short? documentGroupID = null)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[3];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@documentGroupID", documentGroupID);

                if (requisitionNumber.HasValue)
                    additionalFilter += " AND RequisitionHeader.DocumentNo = " + requisitionNumber.Value.ToString();

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalFilter += " AND RequisitionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'";
                }
                else if (fromDate.HasValue)
                    additionalFilter += " AND RequisitionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00'";
                else if (toDate.HasValue)
                    additionalFilter += " AND RequisitionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'";

                //if (documentGroupID.HasValue)
                //    additionalFilter += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ProcurementTracking + additionalFilter, sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable Select_OldStockRequisition(int companyID, int periodID, string DocumentType, string filter = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@DocumentType", DocumentType);
                string query = Query_Select_OldStockRequisition + filter;
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetKeyBySourceHeaderID(int companyId, int periodID, int sourceHeaderID, int documentNo, int documentTypeID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", documentTypeID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetKeyBySourceHeaderID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetGoDownWiseReport(int companyID, int periodID, int? itemID, Nullable<int> batchId, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null)
        {
            DataTable dt = null;
            string AdditionalParameters1 = "";
            string AdditionalParameters2 = "";
            string AdditionalParameters3 = "";
            string AdditionalParameters4 = "";
            string AdditionalParameters5 = "";
            string AdditionalParameters6 = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (Date_From != null && Date_To != null)
                {
                    AdditionalParameters1 = " AND ProductionHeader.DocumentDate between'" + Date_From.Value.ToString("yyyy-MM-dd") + " 00:00:00 '  and '" + Date_To.Value.ToString("yyyy-MM-dd") + " 23:59:59'";
                    AdditionalParameters2 = " AND SupplyHeader.DocumentDate between'" + Date_From.Value.ToString("yyyy-MM-dd") + " 00:00:00 '  and '" + Date_To.Value.ToString("yyyy-MM-dd") + " 23:59:59'";
                }

                else if (Date_From != null)
                {
                    AdditionalParameters1 = " AND ProductionHeader.DocumentDate >= '" + Date_From.Value.ToString("yyyy-MM-dd") + "'";
                    AdditionalParameters2 = " AND SupplyHeader.DocumentDate >= '" + Date_From.Value.ToString("yyyy-MM-dd") + "'";
                }

                else if (Date_To != null)
                {
                    AdditionalParameters1 = " AND ProductionHeader.DocumentDate <= '" + Date_To.Value.ToString("yyyy-MM-dd") + "'";
                    AdditionalParameters2 = " AND SupplyHeader.DocumentDate <= '" + Date_To.Value.ToString("yyyy-MM-dd") + "'";
                }

                if (itemID.HasValue)
                {
                    AdditionalParameters3 = " AND ProductionItem.ItemID = " + itemID.Value;
                    AdditionalParameters4 = " AND SupplyItem.ItemID = " + itemID.Value;
                }

                if (batchId.HasValue)
                {
                    AdditionalParameters5 = "AND ProductionHeader.BatchID =" + batchId.Value;
                    AdditionalParameters6 = "AND SupplyHeader.BatchID =" + batchId.Value;
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetGoDownWiseReport, AdditionalParameters1, AdditionalParameters2, AdditionalParameters3, AdditionalParameters4, AdditionalParameters5, AdditionalParameters6), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetProductionReport(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> batchID)
        {
            DataSet ds = null;

            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            // sb.Append(Query_GetProductionReport);
            string whereClause = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (batchID.HasValue)
                    sqlparams[2] = new SqlParameter("@BatchID", batchID);
                else
                    sqlparams[2] = new SqlParameter("@BatchID", DBNull.Value);


                #region Where Filter

                if (fromDate.HasValue && toDate.HasValue)
                    whereClause = " AND INV_StockTransactionHeader.DocumentDate between '" + fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00' And '" + toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59'";
                else if (fromDate.HasValue)
                    whereClause = " AND INV_StockTransactionHeader.DocumentDate >= '" + fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00'";
                else if (toDate.HasValue)
                    whereClause = " AND INV_StockTransactionHeader.DocumentDate <= '" + toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59'";

                #endregion


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetProductionReport, whereClause), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dtToReturn = ds.Tables[0];
                    AddToDateRecords(ref dtToReturn);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        private static void AddToDateRecords(ref DataTable dtToReturn)
        {
            DataTable dtProduction = dtToReturn.Clone();

            #region Get Distinct records

            var distinctRecords = (from dr in dtToReturn.AsEnumerable()
                                   select new
                                   {
                                       ProductionQuality = dr["ProductionQuality"],
                                       itemID = Convert.ToInt32(dr["ItemID"]),
                                       batchID = 0, //dr["BatchID"],
                                       //batchDescription = string.Empty, //dr["BatchDescription"],
                                       documentDate = Convert.ToDateTime(dr["DocumentDate"])
                                   }).Distinct();

            var distinctDates = (from dr in dtToReturn.AsEnumerable()
                                 select new
                                 {
                                     documentDate = Convert.ToDateTime(dr["DocumentDate"])
                                 }).Distinct();

            var distinctItems = (from dr in dtToReturn.AsEnumerable()
                                 select new
                                 {
                                     itemID = Convert.ToInt32(dr["ItemID"]),
                                     ProductionQuality = dr["ProductionQuality"]
                                 }).Distinct();

            #endregion

            #region On Date

            foreach (var drDistinct in distinctRecords)
            {
                DataRow drNew = dtProduction.NewRow();

                drNew["ProductionQuality"] = drDistinct.ProductionQuality;
                drNew["ItemID"] = drDistinct.itemID;

                decimal transactionQuantity = (from dr in dtToReturn.AsEnumerable()
                                               where dr.Field<DateTime>("DocumentDate") == drDistinct.documentDate
                                                 && dr.Field<int>("ItemID") == drDistinct.itemID
                                               select dr.Field<decimal>("ProductionQuantity")).Sum();

                // decimal transactionQuantityMT = (from dr in dtToReturn.AsEnumerable()
                //   where dr.Field<DateTime>("DocumentDate") == drDistinct.documentDate
                //      && dr.Field<int>("ItemID") == drDistinct.itemID
                //select dr.Field<decimal>("ProductionQuantityMT")).Sum();

                drNew["ProductionQuantity"] = transactionQuantity;
                // drNew["ProductionQuantityMT"] = transactionQuantityMT;
                drNew["BatchID"] = drDistinct.batchID;

                drNew["DocumentDate"] = drDistinct.documentDate;
                drNew["ProductionType"] = "OnDate";

                dtProduction.Rows.Add(drNew);
            }

            #endregion

            #region On Date - Total

            foreach (var drDistinctDate in distinctDates)
            {
                DataRow drNew = dtProduction.NewRow();

                drNew["ProductionQuality"] = "Total";
                drNew["ItemID"] = -1;

                decimal transactionQuantity = (from dr in dtToReturn.AsEnumerable()
                                               where dr.Field<DateTime>("DocumentDate") == drDistinctDate.documentDate
                                               select dr.Field<decimal>("ProductionQuantity")).Sum();

                //  decimal transactionQuantityMT = (from dr in dtToReturn.AsEnumerable()
                //where dr.Field<DateTime>("ProductionDate") == drDistinctDate.documentDate
                //select dr.Field<decimal>("ProductionQuantityMT")).Sum();

                drNew["ProductionQuantity"] = transactionQuantity;
                //  drNew["ProductionQuantityMT"] = transactionQuantityMT;
                drNew["DocumentDate"] = drDistinctDate.documentDate;
                drNew["ProductionType"] = "OnDate";

                dtProduction.Rows.Add(drNew);
            }

            #endregion

            #region To Date

            foreach (var drDistinct in distinctRecords)
            {
                DataRow drNew = dtProduction.NewRow();

                drNew["ProductionQuality"] = drDistinct.ProductionQuality;
                drNew["ItemID"] = drDistinct.itemID;

                decimal transactionQuantity = (from dr in dtToReturn.AsEnumerable()
                                               where dr.Field<DateTime>("DocumentDate") <= drDistinct.documentDate
                                                && dr.Field<int>("ItemID") == drDistinct.itemID
                                               select dr.Field<decimal>("ProductionQuantity")).Sum();

                //   decimal transactionQuantityMT = (from dr in dtToReturn.AsEnumerable()
                //where dr.Field<DateTime>("ProductionDate") <= drDistinct.documentDate
                //   && dr.Field<int>("ItemID") == drDistinct.itemID
                //select dr.Field<decimal>("ProductionQuantityMT")).Sum();

                drNew["ProductionQuantity"] = transactionQuantity;
                // drNew["ProductionQuantityMT"] = transactionQuantityMT;
                drNew["DocumentDate"] = drDistinct.documentDate;
                drNew["ProductionType"] = "ToDate";

                dtProduction.Rows.Add(drNew);
            }

            #endregion

            #region Add To Date records that are equals to zero

            foreach (var objItem in distinctItems)
            {
                foreach (var objDate in distinctDates)
                {
                    int recordFound = (from dr in dtProduction.AsEnumerable()
                                       where dr.Field<DateTime>("DocumentDate") == objDate.documentDate && dr.Field<int>("ItemID") == objItem.itemID
                                       select dr).Count<DataRow>();

                    if (recordFound == 0)
                    {
                        decimal transactionQuantity = (from dr in dtToReturn.AsEnumerable()
                                                       where dr.Field<DateTime>("DocumentDate") <= objDate.documentDate
                                                        && dr.Field<int>("ItemID") == objItem.itemID
                                                       select dr.Field<decimal>("ProductionQuantity")).Sum();

                        //    decimal transactionQuantityMT = (from dr in dtToReturn.AsEnumerable()
                        //where dr.Field<DateTime>("ProductionDate") <= objDate.documentDate
                        //   && dr.Field<int>("ItemID") == objItem.itemID
                        //select dr.Field<decimal>("ProductionQuantityMT")).Sum();

                        DataRow drNew = dtProduction.NewRow();

                        drNew["ProductionQuality"] = objItem.ProductionQuality;
                        drNew["ItemID"] = objItem.itemID;
                        drNew["ProductionQuantity"] = transactionQuantity;
                        // drNew["ProductionQuantityMT"] = transactionQuantityMT;
                        drNew["DocumentDate"] = objDate.documentDate;
                        drNew["ProductionType"] = "ToDate";

                        dtProduction.Rows.Add(drNew);
                    }
                }
            }

            #endregion

            #region To Date - Total

            foreach (var drDistinctDate in distinctDates)
            {
                DataRow drNew = dtProduction.NewRow();

                drNew["ProductionQuality"] = "Total";
                drNew["ItemID"] = -1;

                decimal transactionQuantity = (from dr in dtToReturn.AsEnumerable()
                                               where dr.Field<DateTime>("DocumentDate") <= drDistinctDate.documentDate
                                               select dr.Field<decimal>("ProductionQuantity")).Sum();

                //decimal transactionQuantityMT = (from dr in dtToReturn.AsEnumerable()
                //                                 where dr.Field<DateTime>("ProductionDate") <= drDistinctDate.documentDate
                //                                 select dr.Field<decimal>("ProductionQuantityMT")).Sum();

                drNew["ProductionQuantity"] = transactionQuantity;
                //drNew["ProductionQuantityMT"] = transactionQuantityMT;
                drNew["DocumentDate"] = drDistinctDate.documentDate;
                drNew["ProductionType"] = "ToDate";

                dtProduction.Rows.Add(drNew);
            }

            #endregion

            dtToReturn.Clear();

            foreach (DataRow drToDate in dtProduction.Rows)
                dtToReturn.ImportRow(drToDate);
        }

        public static DataTable GetConsumptionReport(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID,
            Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, short? documentGroupID, Nullable<int> ItemID = null,
            Nullable<int> CategoryID = null, Nullable<int> CatalogId = null, Nullable<int> ConsumptionReportType = null, Nullable<int> JobID = null, bool IsStockReturn = false)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                #region Department

                if (departmentID.HasValue)
                {
                    sb1.Append(" AND tab.DepartmentID = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb1.Append(" AND tab.CostCenterID = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                #region Job Code

                if (JobID.HasValue)
                {
                    sb1.Append(" AND tab.JobID = " + JobID);
                    //sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)

                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_Set_Category.CategoryID = " + CategoryID.ToString());

                if (CatalogId.HasValue)
                    sb.Append("And INV_Item.ItemCatalogGroupID = " + CatalogId.ToString());
                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 )");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%'");
                    }
                }

                if (IsStockReturn == true)
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionReport, sb.ToString(), sb1.ToString()), sqlParms);

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

        public static DataTable GetConsumptionSummaryReport(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID,
           Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
            short? documentGroupID, Nullable<int> ItemID = null, Nullable<int> CategoryID = null, Nullable<int> CatalogID = null
            , Nullable<int> ConsumptionReportType = null, bool IsStockReturn = false)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                const string Query_GetConsumptionSummaryReport = @"Select FINTAB.CategoryName,FINTAB.Description,FINTAB.ItemID
,SUM(FINTAB.TransactionQuantity) as TransactionQuantity 
,Avg(FINTAB.TransactionCost) as ListPricePerUnit,SUM(FINTAB.Amount) as Amount
from (
Select INV_SET_Category.CategoryName,ISNULL(SET_CostCenter.CostCenterID,returncostcenter.CostCenterID)CostCenterID, sth.IncurredOnID, 
ISNULL(SET_CostCenter.CostCenterName,returncostcenter.CostCenterName)CostCenterName
, ISNULL(SET_Department.DepartmentName,returndept.DepartmentName) as DepartmentName,
ISNULL(SET_Department.DepartmentID,returndept.DepartmentID) as DepartmentID, 
 INV_Item.Description, INV_Item.ItemID, 
    SET_UnitofMeasure.UOMName, (std.TransactionQuantity * (-1)) AS TransactionQuantity, 
	std.TransactionCost,
		 --GL_TransactionDetail.Amount / (std.TransactionQuantity * (-1)) as TransactionCost,
	  INV_Item.ListPricePerUnit, 
    ((std.TransactionQuantity * (-1)) * std.TransactionCost) as  Amount, 
	--GL_TransactionDetail.Amount as Amount,
	sth.DocumentDate, SET_DocumentType.DocumentType, Inv_Item.ExpenseAccountCCID 
From INV_StockTransactionHeader sth 
    INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = sth.StockTransactionID
	 AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID 
        AND std.Status = 1 

    Inner join [SET_DocumentType]  on [SET_DocumentType].DocumentTypeID = sth.DocumentTypeID
		and [SET_DocumentType].CompanyID = sth.CompanyID
		and [SET_DocumentType].PeriodID = sth.PeriodID
		and [SET_DocumentType].menuID in (select MenuID from [ApplicationMenu] where MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))
        and [SET_DocumentType].DocumentType not like '%CPR%'
		and [SET_DocumentType].Status = 1
   left join INV_StockTransactionHeader GIn on GIN.StockTransactionID = std.SourceHeaderID
        and GIN.DocumentTypeID = std.SourceDocumentTypeID
and GIN.CompanyID = std.CompanyID
		and GIN.PeriodID = std.PeriodID
		and GIN.FiscalID= std.FiscalID
    INNER JOIN INV_ITem On INV_ITem.ItemID = std.ITemID --AND INV_ITem.CompanyID = std.CompanyID 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = sth.CostCenterID 
	LEFT JOIN SET_CostCenter returncostcenter ON returncostcenter.CostCenterID = GIN.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = sth.DepartmentID 
	LEFT JOIN SET_Department returndept ON returndept.DepartmentID = GIN.DepartmentID 
  INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
Left Join Set_Job on Set_Job.JobID =std.JobID
and Set_Job.CompanyID = std.CompanyID
WHERE sth.CompanyID = @CompanyID AND sth.PeriodID = @PeriodID AND sth.Status = 1 AND sth.StatusCode = 1
{0}

) FINTAB
GROUP by FINTAB.CategoryName,FINTAB.Description,FINTAB.ItemID,FINTAB.ListPricePerUnit,FINTAB.ExpenseAccountCCID 
having SUM(CONVERT(DECIMAL(10,2),FINTAB.TransactionQuantity))<>0
order by FINTAB.Description
";


                #region Department

                if (departmentID.HasValue)
                {
                    sb.Append(" AND ISNULL(SET_Department.DepartmentID,returndept.DepartmentID) = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb.Append(" AND ISNULL(SET_CostCenter.CostCenterID,returncostcenter.CostCenterID) = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)

                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_SET_Category.CategoryID = " + CategoryID.ToString());

                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 ) AND and [SET_DocumentType].DocumentType not like '%CPR%'");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append("AND DocumentType Not like '%Stock%Return%'");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%' and [SET_DocumentType].DocumentType not like '%CPR%'");
                    }
                }
                if (IsStockReturn == true)
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionSummaryReport, sb.ToString()), sqlParms);

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

        public static DataTable GetStorePurchaseRequisitionReport(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, string DepartmentIDs, string FromDocument, string ToDocument)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[2];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                StringBuilder sb = new StringBuilder();

                sb.Append(" WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID and INV_StockTransactionHeader.Status = 1 ");

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                if (DepartmentIDs != string.Empty)
                {
                    sb.Append(" AND SET_Department.DepartmentID in (" + DepartmentIDs + ") ");
                }

                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetStorePurchaseRequisitionReport + sb.ToString(), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetConsumptionSummaryReport_New(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID,
           Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
            short? documentGroupID, Nullable<int> ItemID = null, Nullable<int> CategoryID = null, Nullable<int> CatalogID = null,
             Nullable<int> ConsumptionReportType = null, Nullable<int> DemandType = null, string IsStockReturn = "All")
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                const string Query_GetConsumptionSummaryReport = @"Select FINTAB.CategoryName,FINTAB.Description,FINTAB.ItemID
,SUM(FINTAB.TransactionQuantity) as TransactionQuantity 
,Avg(FINTAB.TransactionCost) as ListPricePerUnit,SUM(FINTAB.Amount) as Amount
from (
Select INV_SET_Category.CategoryName,ISNULL(SET_CostCenter.CostCenterID,returncostcenter.CostCenterID)CostCenterID, sth.IncurredOnID, 
ISNULL(SET_CostCenter.CostCenterName,returncostcenter.CostCenterName)CostCenterName
, ISNULL(SET_Department.DepartmentName,returndept.DepartmentName) as DepartmentName,
ISNULL(SET_Department.DepartmentID,returndept.DepartmentID) as DepartmentID, 
 INV_Item.Description, INV_Item.ItemID, 
    SET_UnitofMeasure.UOMName, (std.TransactionQuantity * (-1)) AS TransactionQuantity, 
	std.TransactionCost,
		 --GL_TransactionDetail.Amount / (std.TransactionQuantity * (-1)) as TransactionCost,
	  INV_Item.ListPricePerUnit, 
    ((std.TransactionQuantity * (-1)) * std.TransactionCost) as  Amount, 
	--GL_TransactionDetail.Amount as Amount,
	sth.DocumentDate, SET_DocumentType.DocumentType, Inv_Item.ExpenseAccountCCID 
From INV_StockTransactionHeader sth 
    INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = sth.StockTransactionID
	 AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID 
        AND std.Status = 1 

    Inner join [SET_DocumentType]  on [SET_DocumentType].DocumentTypeID = sth.DocumentTypeID
		and [SET_DocumentType].CompanyID = sth.CompanyID
		and [SET_DocumentType].PeriodID = sth.PeriodID
		and [SET_DocumentType].menuID in (select MenuID from [ApplicationMenu] where MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))
        and [SET_DocumentType].DocumentType not like '%CPR%'
		and [SET_DocumentType].Status = 1
   left join INV_StockTransactionHeader GIn on GIN.StockTransactionID = std.SourceHeaderID
        and GIN.DocumentTypeID = std.SourceDocumentTypeID
and GIN.CompanyID = std.CompanyID
		and GIN.PeriodID = std.PeriodID
		and GIN.FiscalID= std.FiscalID
    INNER JOIN INV_ITem On INV_ITem.ItemID = std.ITemID --AND INV_ITem.CompanyID = std.CompanyID 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = sth.CostCenterID 
	LEFT JOIN SET_CostCenter returncostcenter ON returncostcenter.CostCenterID = GIN.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = sth.DepartmentID 
	LEFT JOIN SET_Department returndept ON returndept.DepartmentID = GIN.DepartmentID 
  INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
Left Join Set_Job on Set_Job.JobID =std.JobID
and Set_Job.CompanyID = std.CompanyID
WHERE sth.CompanyID = @CompanyID AND sth.PeriodID = @PeriodID AND sth.Status = 1 AND sth.StatusCode = 1
{0}

) FINTAB
GROUP by FINTAB.CategoryName,FINTAB.Description,FINTAB.ItemID,FINTAB.ListPricePerUnit,FINTAB.ExpenseAccountCCID 
having SUM(CONVERT(DECIMAL(10,2),FINTAB.TransactionQuantity))<>0
order by FINTAB.Description
";


                #region Department

                if (departmentID.HasValue)
                {
                    sb.Append(" AND ISNULL(SET_Department.DepartmentID,returndept.DepartmentID) = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb.Append(" AND ISNULL(SET_CostCenter.CostCenterID,returncostcenter.CostCenterID) = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    //sb.Append(" AND Inv_Item.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");

                    sb.Append(" AND  CASE  WHEN std.JobID > 0 THEN Set_Job.AccountCode ELSE Inv_Item.ExpenseAccountCCID END BETWEEN @FromAccountCode AND @ToAccountCode ");

                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)

                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_SET_Category.CategoryID = " + CategoryID.ToString());

                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                if (DemandType > 0)
                    sb.Append(" AND sth.DemandType = " + DemandType);

                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 ) AND [SET_DocumentType].DocumentType not like '%CPR%'");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append("AND DocumentType Not like '%Stock%Return%'");
                    }
                    else if (ConsumptionReportType == 4) //Without Job Code
                    {
                        sb.Append("And (std.JobID is null Or std.JobID = 0 )");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%' and [SET_DocumentType].DocumentType not like '%CPR%'");
                    }
                }
                if (IsStockReturn == "Only Stock Return")
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }
                if (IsStockReturn == "Only GIN")
                {
                    sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionSummaryReport, sb.ToString()), sqlParms);

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

        public static DataTable GetConsumptionconsolidatedReport_New(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID,
                   Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
                    short? documentGroupID, Nullable<int> ItemID = null, Nullable<int> CategoryID = null, Nullable<int> CatalogID = null
                    , Nullable<int> ConsumptionReportType = null, Nullable<int> DemandType = null, string IsStockReturn = "All"
                   )
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;


            #region Query

            const string Query_GetConsumptionconsolidated = @"  Select CategoryName,CostCenterID, CostCenterName,DepartmentName,Description,ItemID, UOMName,SUM(TransactionQuantity)  as TransactionQuantity 
,AVG(TransactionCost) as  TransactionCost,Avg(ListPricePerUnit) as  ListPricePerUnit,SUM(Amount) as Amount 
from (
Select INV_SET_Category.CategoryName,sth.CostCenterID, SET_CostCenter.CostCenterName, 
SET_Department.DepartmentName, INV_Item.Description, INV_Item.ItemID, 
    SET_UnitofMeasure.UOMName, 
	(std.TransactionQuantity * (-1)) AS TransactionQuantity,
	 std.TransactionCost, 
	 --GL_TransactionDetail.Amount / (std.TransactionQuantity * (-1)) as TransactionCost,
	 INV_Item.ListPricePerUnit, 
    ((std.TransactionQuantity * (-1)) * std.TransactionCost) Amount,
	--GL_TransactionDetail.Amount as Amount,
	 sth.DocumentDate, 
	SET_DocumentType.DocumentType, 
case when std.JobID > 0 then Set_Job.AccountCode else  Inv_Item.ExpenseAccountCCID end as ExpenseAccountCCID 
From INV_StockTransactionHeader sth 
    INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = sth.StockTransactionID 
	AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID 
        AND std.Status = 1 
 
  Inner join [SET_DocumentType]  on [SET_DocumentType].DocumentTypeID = sth.DocumentTypeID
		and [SET_DocumentType].CompanyID = sth.CompanyID
		and [SET_DocumentType].PeriodID = sth.PeriodID
		and [SET_DocumentType].menuID in (select MenuID from [ApplicationMenu] where MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))
        and [SET_DocumentType].DocumentType not like '%CPR%'
		and [SET_DocumentType].Status = 1
   left join INV_StockTransactionHeader GIn on GIN.StockTransactionID = std.SourceHeaderID
        and GIN.DocumentTypeID = std.SourceDocumentTypeID
and GIN.CompanyID = std.CompanyID
		and GIN.PeriodID = std.PeriodID
		and GIN.FiscalID= std.FiscalID
    INNER JOIN INV_ITem On INV_ITem.ItemID = std.ITemID --AND INV_ITem.CompanyID = std.CompanyID 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = sth.CostCenterID 
	LEFT JOIN SET_CostCenter returncostcenter ON returncostcenter.CostCenterID = GIN.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = sth.DepartmentID 
	LEFT JOIN SET_Department returndept ON returndept.DepartmentID = GIN.DepartmentID 
  INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 

Left Join Set_Job on Set_Job.JobID =std.JobID
and Set_Job.CompanyID = std.CompanyID

WHERE sth.CompanyID = @CompanyID AND sth.PeriodID = @PeriodID AND sth.Status = 1 AND sth.StatusCode = 1
 {0}
) as  fintab
GROUP BY CategoryName,CostCenterID, CostCenterName,DepartmentName,Description,ItemID, UOMName";


            #endregion
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                #region Department

                if (departmentID.HasValue)
                {
                    sb.Append(" AND SET_Department.DepartmentID = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb.Append(" AND sth.CostCenterID = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)
                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_Set_Category.categoryID = " + CategoryID.ToString());

                if (CatalogID.HasValue)
                    sb.Append("And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());
                if (DemandType > 0)
                    sb.Append(" AND sth.DemandType = " + DemandType);


                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 )");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                    }
                    else if (ConsumptionReportType == 4) //Without Job Code
                    {
                        sb.Append("And (std.JobID is null Or std.JobID = 0 )");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%'");
                    }
                }
                if (IsStockReturn == "Only Stock Return")
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }
                if (IsStockReturn == "Only GIN")
                {
                    sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionconsolidated, sb.ToString()), sqlParms);

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

        public static DataTable GetConsumptionReport_New(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID, int? CustomerID,
                  Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, short? documentGroupID, Nullable<int> ItemID = null,
                  Nullable<int> CategoryID = null, Nullable<int> CatalogId = null, Nullable<int> ConsumptionReportType = null, Nullable<int> JobID = null, Nullable<int> DemandType = null, string IsStockReturn = "All")
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                #region Department

                if (departmentID.HasValue)
                {
                    sb1.Append(" AND tab.DepartmentID = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb1.Append(" AND tab.CostCenterID = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    sb1.Append(" AND tab.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb1.Append(" AND tab.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb1.Append(" AND tab.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                #region Job Code

                if (JobID.HasValue)
                {
                    sb1.Append(" AND tab.JobID = " + JobID);
                    //sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)

                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_Set_Category.CategoryID = " + CategoryID.ToString());

                if (CatalogId.HasValue)
                    sb.Append("And INV_Item.ItemCatalogGroupID = " + CatalogId.ToString());

                if (DemandType > 0)
                    sb.Append(" AND sth.DemandType = " + DemandType);

                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 )");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                    }
                    else if (ConsumptionReportType == 4) //Without Job Code
                    {
                        sb.Append("And (std.JobID is null Or std.JobID = 0 )");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%'");
                    }
                }

                if (IsStockReturn == "Only Stock Return")
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }
                if (IsStockReturn == "Only GIN")
                {
                    sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                }

                if (CustomerID > 0)
                {
                    sb.Append(" AND sth.BusinessPartnerID = " + CustomerID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionReport, sb.ToString(), sb1.ToString()), sqlParms);

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

        public static DataTable GetPendingStorePurchaseRequisitionReport(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, string DepartmentIDs, string FromDocument, string ToDocument, string query, int? doctypeid = null,
            short? documentGroupID = null, Nullable<int> ApprovalGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[3];
                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                StringBuilder sb = new StringBuilder();

                sb.Append(@" WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID and INV_StockTransactionHeader.Status = 1  
                and INV_StockTransactionHeader.IsRequisitionCompleted = 0 ");

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                if (DepartmentIDs != string.Empty)
                {
                    sb.Append(" AND SET_Department.DepartmentID in (" + DepartmentIDs + ") ");
                }

                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }
                if (doctypeid != null)
                    if (doctypeid > 0)
                    {

                        sb.Append(" and SET_DocumentType.DocumentTypeID = " + doctypeid + " ");
                    }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovalGroupID.HasValue)
                    sb.Append("  AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString());

                sb.Append(" and isnull(INV_StockTransactionHeader.IsRequisitionCompleted,0) = 0 ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetPendingStorePurchaseRequisitionReportNew(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> departmentfrom, Nullable<int> departmentto, string FromDocument, string ToDocument, string query, int? doctypeid = null,
            short? documentGroupID = null, Nullable<int> ApprovalGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[3];
                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                StringBuilder sb = new StringBuilder();

                sb.Append(@" WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID and INV_StockTransactionHeader.Status = 1  
                and INV_StockTransactionHeader.IsRequisitionCompleted = 0 AND INV_StockTransactionHeader.StatusCode IN (1) ");

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                //if (DepartmentIDs != string.Empty)
                //{
                //    sb.Append(" AND SET_Department.DepartmentID in (" + DepartmentIDs + ") ");
                //}
                if (departmentfrom != null && departmentto != null)
                {

                    sb.Append("and Set_Department.DepartmentID between " + departmentfrom + " and " + departmentto + "");
                }

                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }
                if (doctypeid != null)
                    if (doctypeid > 0)
                    {

                        sb.Append(" and SET_DocumentType.DocumentTypeID = " + doctypeid + " ");
                    }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovalGroupID.HasValue)
                    sb.Append("  AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString());

                sb.Append(" and isnull(INV_StockTransactionHeader.IsRequisitionCompleted,0) = 0 ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetGINRecordsForStockReturn(int companyID, int periodID, int stockTransactionID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetGINRecordsForStockReturn, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable StockReturnReprotData(int companyId, int peridId, int? GINNumberFrom, int? GINNumberTo, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> documentTypeID = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            string additionalparameter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                //sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);


                if (GINNumberFrom.HasValue && GINNumberTo.HasValue)

                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] BETWEEN '" + GINNumberFrom + "' AND '" + GINNumberTo + "'";
                else if (GINNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberFrom + "'";
                else if (GINNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberTo + "'";

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                if (documentTypeID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DocumentTypeID = " + documentTypeID.Value;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetGINRecordsForStockReturnReport + additionalparameter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable StockReturnReprotData_New(int companyId, int peridId, int? GINNumberFrom, int? GINNumberTo, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> documentTypeID = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            string additionalparameter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", peridId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                //sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);


                if (GINNumberFrom.HasValue && GINNumberTo.HasValue)

                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] BETWEEN '" + GINNumberFrom + "' AND '" + GINNumberTo + "'";
                else if (GINNumberFrom.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberFrom + "'";
                else if (GINNumberTo.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentNo] = '" + GINNumberTo + "'";

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [INV_StockTransactionHeader].[DocumentDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                if (documentTypeID.HasValue)
                    additionalparameter = additionalparameter + " AND INV_StockTransactionHeader.DocumentTypeID = " + documentTypeID.Value;

                string Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetGINRecordsForStockReturnReport.ToString();

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query + additionalparameter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static int GetStockTransactionID(int companyID, int periodID, int DocumentTypeID, int DocumentNo, SqlTransaction objTransaction = null)
        {
            int quantityToReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                if (objTransaction != null)
                    quantityToReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetSourceHeaderID, sqlparams));
                else
                    quantityToReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetSourceHeaderID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return quantityToReturn;
        }

        public static DataSet ControlSheet(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate, short? documentGroupID = null)
        {
            DataSet ds;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                //ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ControlSheet, sqlparams).Tables[0];
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ControlSheet, additionalFilter), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataSet ImportDocumentReport(int CompanyID, int PeriodID, int? DocumentNo, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int FromLoc, int ToLoc, int Catagoery, int ItemID,
            short? documentGroupID = null, int CurrentFiscalID = 0, bool AlImdad = false)
        {
            DataSet ds;
            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentType", DocumentType);
                sqlparams[5] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                StringBuilder sb = new StringBuilder();

                if (FromLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.LocationID = " + FromLoc);
                if (ToLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.ToLocationID = " + ToLoc);

                if (Catagoery > 0)
                    Filters.Append(" and inv_Set_Category.CategoryID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);


                if (documentGroupID.HasValue)
                    Filters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (DocumentNo != null)
                //    sb.Append("AND INV_StockTransactionHeader.DocumentNo= @DocumentNo");

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetImportDocumentReportDataNew + Filters, sb.ToString()), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetImportDocument_ImportReport(int companyID, int periodID, Nullable<int> salesOrderNoFrom, Nullable<int> salesOrderNoTo,
            Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> customerName = null, Nullable<int> statusCode = null,
            Nullable<int> documentTypeID = null, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append("and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(AP_PurchaseOrderHeader.DocumentDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (salesOrderNoFrom >= 0 && salesOrderNoTo > 0)
                    sb.Append(" And AP_PurchaseOrderHeader.DocumentNumber Between " + salesOrderNoFrom + " And " + salesOrderNoTo + "");
                else if (salesOrderNoFrom > 0 && salesOrderNoTo == 0)
                    sb.Append("And AP_PurchaseOrderHeader.DocumentNumber = " + salesOrderNoFrom);

                if (customerName > 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = " + customerName);

                if (statusCode > 0)
                    sb.Append(" and AP_PurchaseOrderHeader.StatusCode = " + statusCode + "");

                if (documentTypeID.HasValue)
                    sb.Append(" AND SET_DocumentType.DocumentTypeID = " + documentTypeID.Value);

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ImportDocument_ImportReport, sb.ToString()), sqlparams);

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

        public static DataSet ItemTransferRreport(int CompanyID, int PeriodID, int? DocumentNo, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int FromLoc, int ToLoc, int Catagoery, int ItemID,
            short? documentGroupID = null, int CurrentFiscalID = 0, bool AlImdad = false)
        {
            DataSet ds;
            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentType", DocumentType);
                sqlparams[5] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                StringBuilder sb = new StringBuilder();

                if (FromLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.LocationID = " + FromLoc);
                if (ToLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.ToLocationID = " + ToLoc);

                if (Catagoery > 0)
                    Filters.Append(" and inv_Set_Category.CategoryID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);


                if (documentGroupID.HasValue)
                    Filters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DocumentNo != null)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo= @DocumentNo");

                if (!AlImdad)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ItemTransferRreport + Filters, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetTransferInreportData + Filters, sb.ToString()), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static bool Update_TransactionStatusForPO(int companyID, int periodID, long StockHeaderID,
            SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", StockHeaderID);

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_TransactionStatusForPO, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_TransactionStatusForPO, sqlparams);

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

            return result;
        }

        public static bool Update_TransactionStatusForGRN(int companyID, int periodID, long StockHeaderID, long StockDetailID,
            SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", StockHeaderID);
                sqlparams[3] = new SqlParameter("@StockDetailID", StockDetailID);

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_TransactionStatusForGRN, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_TransactionStatusForGRN, sqlparams);

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

            return result;
        }

        public static decimal GetTotalQuantityOfInvoice(int companyID, int periodID, long StockHeaderID, SqlTransaction objTransaction = null)
        {
            decimal ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@StockHeaderID", StockHeaderID);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_TotalQuantityOfInvoice, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_TotalQuantityOfInvoice, sqlparams));

                //   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_QuantityOfVendorInvoice, sqlparams));

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable DocumnetTypeDDL(int companyID, int periodID, string MenuPath)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@MenuPath", MenuPath);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_BindDocumentTypeDDL, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable DocumnetNoDDL(int companyID, int periodID)
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_BindDocumentNODDL, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool UpdateDataByDDLValues(int Companyid, int perioid, int DocmntTypeID, int stocktrnID, int StatusCode)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", stocktrnID);
                sqlparams[4] = new SqlParameter("@StatusCode", StatusCode);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_DDLValues, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_DDLValues, sqlparams) > 0)
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

        public bool UpdateDataByStockTransactionID(int Companyid, int perioid, int stocktrnID, Guid userID, string Username)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                //sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stocktrnID);
                //sqlparams[3] = new SqlParameter("@StatusCode", StatusCode);
                sqlparams[3] = new SqlParameter("@UpdateDate", DateTime.Now);
                sqlparams[4] = new SqlParameter("@UpdateBy", userID);
                sqlparams[5] = new SqlParameter("UpdateByValue", Username);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_StockTransactionID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_StockTransactionID, sqlparams) > 0)
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

        public bool RequisitionClose(int Companyid, int perioid, int DocmntTypeID, int stocktrnID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", stocktrnID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_RequisitionClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_RequisitionClose, sqlparams) > 0)
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

        public bool POClose(int Companyid, int perioid, int DocmntTypeID, int stocktrnID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", stocktrnID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_POClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_POClose, sqlparams) > 0)
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


        public static DataTable Get_AllGINDocumentNo(int companyID, int periodID, int documentTypeID, string Query, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public static DataRow GetDataByDocumentType(int companyID, int periodID, int documentTypeID, int documentNo, int FiscalID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDocumentType, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public bool InsertInProStatus(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[95];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", f_StockTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[5] = new SqlParameter("@DocumentDate", f_DocumentDate);

                if (!string.IsNullOrEmpty(f_Comments))
                    sqlparams[6] = new SqlParameter("@Comments", f_Comments);
                else
                    sqlparams[6] = new SqlParameter("@Comments", DBNull.Value);

                if (f_BusinessPartnerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BusinessPartnerID", f_BusinessPartnerID.Value);
                else
                    sqlparams[7] = new SqlParameter("@BusinessPartnerID", DBNull.Value);

                if (f_CostCenterID.HasValue)
                    sqlparams[8] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                else
                    sqlparams[8] = new SqlParameter("@CostCenterID", DBNull.Value);

                if (f_LocationID.HasValue)
                    sqlparams[9] = new SqlParameter("@LocationID", f_LocationID.Value);
                else
                    sqlparams[9] = new SqlParameter("@LocationID", DBNull.Value);

                if (f_ToLocationID.HasValue)
                    sqlparams[10] = new SqlParameter("@ToLocationID", f_ToLocationID.Value);
                else
                    sqlparams[10] = new SqlParameter("@ToLocationID", DBNull.Value);

                sqlparams[11] = new SqlParameter("@Status", f_Status);
                sqlparams[12] = new SqlParameter("@IsActive", f_IsActive);
                sqlparams[13] = new SqlParameter("@CreatedBy", f_CreatedBy);
                sqlparams[14] = new SqlParameter("@CreateDate", f_CreateDate);
                sqlparams[15] = new SqlParameter("@CreatedByValue", f_CreatedByValue);

                if (f_StatusCode.HasValue)
                    sqlparams[16] = new SqlParameter("@StatusCode", f_StatusCode.Value);
                else
                    sqlparams[16] = new SqlParameter("@StatusCode", DBNull.Value);

                if (f_TotalAmount.HasValue)
                    sqlparams[17] = new SqlParameter("@TotalAmount", f_TotalAmount.Value);
                else
                    sqlparams[17] = new SqlParameter("@TotalAmount", DBNull.Value);

                if (f_DepartmentID.HasValue)
                    sqlparams[18] = new SqlParameter("@DepartmentID", f_DepartmentID.Value);
                else
                    sqlparams[18] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (f_IncurredOnID.HasValue)
                    sqlparams[19] = new SqlParameter("@IncurredOnID", f_IncurredOnID.Value);
                else
                    sqlparams[19] = new SqlParameter("@IncurredOnID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_IssuanceType))
                    sqlparams[20] = new SqlParameter("@IssuanceType", f_IssuanceType);
                else
                    sqlparams[20] = new SqlParameter("@IssuanceType", DBNull.Value);

                sqlparams[21] = new SqlParameter("@IsRequisitionCompleted", f_IsRequisitionCompleted);

                if (f_BatchID.HasValue)
                    sqlparams[22] = new SqlParameter("@BatchID", f_BatchID.Value);
                else
                    sqlparams[22] = new SqlParameter("@BatchID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ChallanNo))
                    sqlparams[23] = new SqlParameter("@ChallanNo", f_ChallanNo);
                else
                    sqlparams[23] = new SqlParameter("@ChallanNo", DBNull.Value);

                if (f_ChallanDate.HasValue)
                    sqlparams[24] = new SqlParameter("@ChallanDate", f_ChallanDate.Value);
                else
                    sqlparams[24] = new SqlParameter("@ChallanDate", DBNull.Value);

                if (f_JobCardRefNo.HasValue)
                    sqlparams[25] = new SqlParameter("@JobCardRefNo", f_JobCardRefNo.Value);
                else
                    sqlparams[25] = new SqlParameter("@JobCardRefNo", DBNull.Value);

                if (f_IsTransferred.HasValue)
                    sqlparams[26] = new SqlParameter("@IsTransferred", f_IsTransferred.Value);
                else
                    sqlparams[26] = new SqlParameter("@IsTransferred", DBNull.Value);

                if (f_ProductRecipeID.HasValue)
                    sqlparams[27] = new SqlParameter("@ProductRecipeID", f_ProductRecipeID.Value);
                else
                    sqlparams[27] = new SqlParameter("@ProductRecipeID", DBNull.Value);

                if (f_ProductQuantity.HasValue)
                    sqlparams[28] = new SqlParameter("@ProductQuantity", f_ProductQuantity.Value);
                else
                    sqlparams[28] = new SqlParameter("@ProductQuantity", DBNull.Value);

                if (f_DueDate.HasValue)
                    sqlparams[29] = new SqlParameter("@DueDate", f_DueDate.Value);
                else
                    sqlparams[29] = new SqlParameter("@DueDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RearTyreLeft))
                    sqlparams[30] = new SqlParameter("@RearTyreLeft", f_RearTyreLeft);
                else
                    sqlparams[30] = new SqlParameter("@RearTyreLeft", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RearTyreRight))
                    sqlparams[31] = new SqlParameter("@RearTyreRight", f_RearTyreRight);
                else
                    sqlparams[31] = new SqlParameter("@RearTyreRight", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FrontTyreRight))
                    sqlparams[32] = new SqlParameter("@FrontTyreRight", f_FrontTyreRight);
                else
                    sqlparams[32] = new SqlParameter("@FrontTyreRight", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FrontTyreleft))
                    sqlparams[33] = new SqlParameter("@FrontTyreleft", f_FrontTyreleft);
                else
                    sqlparams[33] = new SqlParameter("@FrontTyreleft", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BatteryNo))
                    sqlparams[34] = new SqlParameter("@BatteryNo", f_BatteryNo);
                else
                    sqlparams[34] = new SqlParameter("@BatteryNo", DBNull.Value);

                if (f_IsReversed.HasValue)
                    sqlparams[35] = new SqlParameter("@IsReversed", f_IsReversed.Value);
                else
                    sqlparams[35] = new SqlParameter("@IsReversed", DBNull.Value);

                if (f_ReversalDate.HasValue)
                    sqlparams[36] = new SqlParameter("@ReversalDate", f_ReversalDate.Value);
                else
                    sqlparams[36] = new SqlParameter("@ReversalDate", DBNull.Value);

                if (f_ReversalID.HasValue)
                    sqlparams[37] = new SqlParameter("@ReversalID", f_ReversalID.Value);
                else
                    sqlparams[37] = new SqlParameter("@ReversalID", DBNull.Value);

                if (f_CostCenterIDTo.HasValue)
                    sqlparams[38] = new SqlParameter("@CostCenterIDTo", f_CostCenterIDTo.Value);
                else
                    sqlparams[38] = new SqlParameter("@CostCenterIDTo", DBNull.Value);

                if (f_Loan_StockAmount.HasValue)
                    sqlparams[39] = new SqlParameter("@Loan_StockAmount", f_Loan_StockAmount.Value);
                else
                    sqlparams[39] = new SqlParameter("@Loan_StockAmount", DBNull.Value);

                if (f_Loan_COGSAmount.HasValue)
                    sqlparams[40] = new SqlParameter("@Loan_COGSAmount", f_Loan_COGSAmount.Value);
                else
                    sqlparams[40] = new SqlParameter("@Loan_COGSAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Loan_TransactionType))
                    sqlparams[41] = new SqlParameter("@Loan_TransactionType", f_Loan_TransactionType);
                else
                    sqlparams[41] = new SqlParameter("@Loan_TransactionType", DBNull.Value);

                if (f_FreightFC.HasValue)
                    sqlparams[42] = new SqlParameter("@FreightFC", f_FreightFC.Value);
                else
                    sqlparams[42] = new SqlParameter("@FreightFC", DBNull.Value);

                if (f_ExchangeRate.HasValue)
                    sqlparams[43] = new SqlParameter("@ExchangeRate", f_ExchangeRate.Value);
                else
                    sqlparams[43] = new SqlParameter("@ExchangeRate", DBNull.Value);

                if (f_FreightFCXExchangeRate.HasValue)
                    sqlparams[44] = new SqlParameter("@FreightFCXExchangeRate", f_FreightFCXExchangeRate.Value);
                else
                    sqlparams[44] = new SqlParameter("@FreightFCXExchangeRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_InwardGatePass))
                    sqlparams[45] = new SqlParameter("@InwardGatePass", f_InwardGatePass);
                else
                    sqlparams[45] = new SqlParameter("@InwardGatePass", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[46] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[46] = new SqlParameter("@FiscalID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BLNo))
                    sqlparams[47] = new SqlParameter("@BLNo", f_BLNo);
                else
                    sqlparams[47] = new SqlParameter("@BLNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_GDNumber))
                    sqlparams[48] = new SqlParameter("@GDNumber", f_GDNumber);
                else
                    sqlparams[48] = new SqlParameter("@GDNumber", DBNull.Value);

                if (f_ShipViaCode.HasValue)
                    sqlparams[49] = new SqlParameter("@ShipViaCode", f_ShipViaCode.Value);
                else
                    sqlparams[49] = new SqlParameter("@ShipViaCode", DBNull.Value);

                if (f_ClearingCharges.HasValue)
                    sqlparams[50] = new SqlParameter("@ClearingCharges", f_ClearingCharges.Value);
                else
                    sqlparams[50] = new SqlParameter("@ClearingCharges", DBNull.Value);

                if (f_LogisticSupplierID.HasValue)
                    sqlparams[51] = new SqlParameter("@LogisticSupplierID", f_LogisticSupplierID.Value);
                else
                    sqlparams[51] = new SqlParameter("@LogisticSupplierID", DBNull.Value);

                if (f_NoofContainers.HasValue)
                    sqlparams[52] = new SqlParameter("@NoofContainers", f_NoofContainers.Value);
                else
                    sqlparams[52] = new SqlParameter("@NoofContainers", DBNull.Value);

                if (f_ClearingAgentID.HasValue)
                    sqlparams[53] = new SqlParameter("@ClearingAgentID", f_ClearingAgentID.Value);
                else
                    sqlparams[53] = new SqlParameter("@ClearingAgentID", DBNull.Value);

                if (f_TransporterID.HasValue)
                    sqlparams[54] = new SqlParameter("@TransporterID", f_TransporterID.Value);
                else
                    sqlparams[54] = new SqlParameter("@TransporterID", DBNull.Value);

                if (f_LoadingCharges.HasValue)
                    sqlparams[55] = new SqlParameter("@LoadingCharges", f_LoadingCharges.Value);
                else
                    sqlparams[55] = new SqlParameter("@LoadingCharges", DBNull.Value);

                if (f_ContractorID.HasValue)
                    sqlparams[56] = new SqlParameter("@ContractorID", f_ContractorID.Value);
                else
                    sqlparams[56] = new SqlParameter("@ContractorID", DBNull.Value);

                if (f_ImporterID.HasValue)
                    sqlparams[57] = new SqlParameter("@ImporterID", f_ImporterID.Value);
                else
                    sqlparams[57] = new SqlParameter("@ImporterID", DBNull.Value);

                if (f_TransportationCharges.HasValue)
                    sqlparams[58] = new SqlParameter("@TransportationCharges", f_TransportationCharges.Value);
                else
                    sqlparams[58] = new SqlParameter("@TransportationCharges", DBNull.Value);

                if (f_IsDirectStock.HasValue)
                    sqlparams[59] = new SqlParameter("@IsDirectStock", f_IsDirectStock.Value);
                else
                    sqlparams[59] = new SqlParameter("@IsDirectStock", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BiltyNo))
                    sqlparams[60] = new SqlParameter("@BiltyNo", f_BiltyNo);
                else
                    sqlparams[60] = new SqlParameter("@BiltyNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_VehicleNo))
                    sqlparams[61] = new SqlParameter("@VehicleNo", f_VehicleNo);
                else
                    sqlparams[61] = new SqlParameter("@VehicleNo", DBNull.Value);

                if (f_BiltyDate.HasValue)
                    sqlparams[62] = new SqlParameter("@BiltyDate", f_BiltyDate.Value);
                else
                    sqlparams[62] = new SqlParameter("@BiltyDate", DBNull.Value);

                if (f_ShiftID.HasValue)
                    sqlparams[63] = new SqlParameter("@ShiftID", f_ShiftID.Value);
                else
                    sqlparams[63] = new SqlParameter("@ShiftID", DBNull.Value);

                if (f_GrossWeightID.HasValue)
                    sqlparams[64] = new SqlParameter("@GrossWeightID", f_GrossWeightID.Value);
                else
                    sqlparams[64] = new SqlParameter("@GrossWeightID", DBNull.Value);

                if (f_LoadingRate.HasValue)
                    sqlparams[65] = new SqlParameter("@LoadingRate", f_LoadingRate.Value);
                else
                    sqlparams[65] = new SqlParameter("@LoadingRate", DBNull.Value);

                if (f_NoofBilletPieces.HasValue)
                    sqlparams[66] = new SqlParameter("@NoofBilletPieces", f_NoofBilletPieces.Value);
                else
                    sqlparams[66] = new SqlParameter("@NoofBilletPieces", DBNull.Value);

                if (f_ApproxWeightofBillet.HasValue)
                    sqlparams[67] = new SqlParameter("@ApproxWeightofBillet", f_ApproxWeightofBillet.Value);
                else
                    sqlparams[67] = new SqlParameter("@ApproxWeightofBillet", DBNull.Value);

                if (f_HeadingCostCenterID.HasValue)
                    sqlparams[68] = new SqlParameter("@HeadingCostCenterID", f_HeadingCostCenterID.Value);
                else
                    sqlparams[68] = new SqlParameter("@HeadingCostCenterID", DBNull.Value);


                if (!string.IsNullOrEmpty(f_CrucibleNo))
                    sqlparams[69] = new SqlParameter("@CrucibleNo", f_CrucibleNo);
                else
                    sqlparams[69] = new SqlParameter("@CrucibleNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CrucibleLife))
                    sqlparams[70] = new SqlParameter("@CrucibleLife", f_CrucibleLife);
                else
                    sqlparams[70] = new SqlParameter("@CrucibleLife", DBNull.Value);

                if (!string.IsNullOrEmpty(f_EnergyConsumption))
                    sqlparams[71] = new SqlParameter("@EnergyConsumption", f_EnergyConsumption);
                else
                    sqlparams[71] = new SqlParameter("@EnergyConsumption", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TappingTemperature))
                    sqlparams[72] = new SqlParameter("@TappingTemperature", f_TappingTemperature);
                else
                    sqlparams[72] = new SqlParameter("@TappingTemperature", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Frequency))
                    sqlparams[73] = new SqlParameter("@Frequency", f_Frequency);
                else
                    sqlparams[73] = new SqlParameter("@Frequency", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LadleNo))
                    sqlparams[74] = new SqlParameter("@LadleNo", f_LadleNo);
                else
                    sqlparams[74] = new SqlParameter("@LadleNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_LadleLife))
                    sqlparams[75] = new SqlParameter("@LadleLife", f_LadleLife);
                else
                    sqlparams[75] = new SqlParameter("@LadleLife", DBNull.Value);

                if (!string.IsNullOrEmpty(f_SlideGateSetlife))
                    sqlparams[76] = new SqlParameter("@SlideGateSetlife", f_SlideGateSetlife);
                else
                    sqlparams[76] = new SqlParameter("@SlideGateSetlife", DBNull.Value);

                if (!string.IsNullOrEmpty(f_PorousPlugLife))
                    sqlparams[77] = new SqlParameter("@PorousPlugLife", f_PorousPlugLife);
                else
                    sqlparams[77] = new SqlParameter("@PorousPlugLife", DBNull.Value);

                if (!string.IsNullOrEmpty(f_CastingSpeed))
                    sqlparams[78] = new SqlParameter("@CastingSpeed", f_CastingSpeed);
                else
                    sqlparams[78] = new SqlParameter("@CastingSpeed", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MoldLife))
                    sqlparams[79] = new SqlParameter("@MoldLife", f_MoldLife);
                else
                    sqlparams[79] = new SqlParameter("@MoldLife", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TandishTemperature))
                    sqlparams[80] = new SqlParameter("@TandishTemperature", f_TandishTemperature);
                else
                    sqlparams[80] = new SqlParameter("@TandishTemperature", DBNull.Value);

                if (!string.IsNullOrEmpty(f_SequenceLength))
                    sqlparams[81] = new SqlParameter("@SequenceLength", f_SequenceLength);
                else
                    sqlparams[81] = new SqlParameter("@SequenceLength", DBNull.Value);

                if (!string.IsNullOrEmpty(f_InletWaterTerm))
                    sqlparams[82] = new SqlParameter("@InletWaterTerm", f_InletWaterTerm);
                else
                    sqlparams[82] = new SqlParameter("@InletWaterTerm", DBNull.Value);

                if (!string.IsNullOrEmpty(f_MetalLoss))
                    sqlparams[83] = new SqlParameter("@MetalLoss", f_MetalLoss);
                else
                    sqlparams[83] = new SqlParameter("@MetalLoss", DBNull.Value);

                if (f_StartTimeCCM.HasValue)
                    sqlparams[84] = new SqlParameter("@StartTimeCCM", f_StartTimeCCM.Value);
                else
                    sqlparams[84] = new SqlParameter("@StartTimeCCM", DBNull.Value);

                if (f_EndTimeCCM.HasValue)
                    sqlparams[85] = new SqlParameter("@EndTimeCCM", f_EndTimeCCM.Value);
                else
                    sqlparams[85] = new SqlParameter("@EndTimeCCM", DBNull.Value);

                if (f_NetTimeCCM.HasValue)
                    sqlparams[86] = new SqlParameter("@NetTimeCCM", f_NetTimeCCM.Value);
                else
                    sqlparams[86] = new SqlParameter("@NetTimeCCM", DBNull.Value);

                if (f_FurnaceStartTime.HasValue)
                    sqlparams[87] = new SqlParameter("@FurnaceStartTime", f_FurnaceStartTime.Value);
                else
                    sqlparams[87] = new SqlParameter("@FurnaceStartTime", DBNull.Value);

                if (f_FurnaceEndTime.HasValue)
                    sqlparams[88] = new SqlParameter("@FurnaceEndTime", f_FurnaceEndTime.Value);
                else
                    sqlparams[88] = new SqlParameter("@FurnaceEndTime", DBNull.Value);

                if (f_FurnaceNetTime.HasValue)
                    sqlparams[89] = new SqlParameter("@FurnaceNetTime", f_FurnaceNetTime.Value);
                else
                    sqlparams[89] = new SqlParameter("@FurnaceNetTime", DBNull.Value);

                if (f_BusinessPartnerTypeID.HasValue)
                    sqlparams[90] = new SqlParameter("@BusinessPartnerTypeID", f_BusinessPartnerTypeID.Value);
                else
                    sqlparams[90] = new SqlParameter("@BusinessPartnerTypeID", DBNull.Value);

                if (f_InvoiceHeaderID.HasValue)
                    sqlparams[91] = new SqlParameter("@InvoiceHeaderID", f_InvoiceHeaderID.Value);
                else
                    sqlparams[91] = new SqlParameter("@InvoiceHeaderID", DBNull.Value);

                if (f_AssignTo.HasValue)
                    sqlparams[92] = new SqlParameter("@AssignTo", f_AssignTo.Value);
                else
                    sqlparams[92] = new SqlParameter("@AssignTo", DBNull.Value);

                if (f_DemandType.HasValue)
                    sqlparams[93] = new SqlParameter("@DemandType", f_DemandType.Value);
                else
                    sqlparams[93] = new SqlParameter("@DemandType", DBNull.Value);

                if (f_SeasonType.HasValue)
                    sqlparams[94] = new SqlParameter("@SeasonType", f_SeasonType.Value);
                else
                    sqlparams[94] = new SqlParameter("@SeasonType", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Insert, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Insert, sqlparams) > 0)
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

        public bool UpdateDataByKeyOwn(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[54];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", f_StockTransactionID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[5] = new SqlParameter("@DocumentDate", f_DocumentDate);

                if (!string.IsNullOrEmpty(f_Comments))
                    sqlparams[6] = new SqlParameter("@Comments", f_Comments);
                else
                    sqlparams[6] = new SqlParameter("@Comments", DBNull.Value);

                if (f_BusinessPartnerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BusinessPartnerID", f_BusinessPartnerID.Value);
                else
                    sqlparams[7] = new SqlParameter("@BusinessPartnerID", DBNull.Value);

                if (f_CostCenterID.HasValue)
                    sqlparams[8] = new SqlParameter("@CostCenterID", f_CostCenterID.Value);
                else
                    sqlparams[8] = new SqlParameter("@CostCenterID", DBNull.Value);

                if (f_LocationID.HasValue)
                    sqlparams[9] = new SqlParameter("@LocationID", f_LocationID.Value);
                else
                    sqlparams[9] = new SqlParameter("@LocationID", DBNull.Value);

                if (f_ToLocationID.HasValue)
                    sqlparams[10] = new SqlParameter("@ToLocationID", f_ToLocationID.Value);
                else
                    sqlparams[10] = new SqlParameter("@ToLocationID", DBNull.Value);

                sqlparams[11] = new SqlParameter("@Status", f_Status);
                sqlparams[12] = new SqlParameter("@IsActive", f_IsActive);

                if (f_UpdatedBy.HasValue)
                    sqlparams[13] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[13] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[14] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[14] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[15] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[15] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (f_StatusCode.HasValue)
                    sqlparams[16] = new SqlParameter("@StatusCode", f_StatusCode.Value);
                else
                    sqlparams[16] = new SqlParameter("@StatusCode", DBNull.Value);

                if (f_TotalAmount.HasValue)
                    sqlparams[17] = new SqlParameter("@TotalAmount", f_TotalAmount.Value);
                else
                    sqlparams[17] = new SqlParameter("@TotalAmount", DBNull.Value);

                if (f_DepartmentID.HasValue)
                    sqlparams[18] = new SqlParameter("@DepartmentID", f_DepartmentID.Value);
                else
                    sqlparams[18] = new SqlParameter("@DepartmentID", DBNull.Value);

                if (f_IncurredOnID.HasValue)
                    sqlparams[19] = new SqlParameter("@IncurredOnID", f_IncurredOnID.Value);
                else
                    sqlparams[19] = new SqlParameter("@IncurredOnID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_IssuanceType))
                    sqlparams[20] = new SqlParameter("@IssuanceType", f_IssuanceType);
                else
                    sqlparams[20] = new SqlParameter("@IssuanceType", DBNull.Value);

                sqlparams[21] = new SqlParameter("@IsRequisitionCompleted", f_IsRequisitionCompleted);

                if (f_BatchID.HasValue)
                    sqlparams[22] = new SqlParameter("@BatchID", f_BatchID.Value);
                else
                    sqlparams[22] = new SqlParameter("@BatchID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_ChallanNo))
                    sqlparams[23] = new SqlParameter("@ChallanNo", f_ChallanNo);
                else
                    sqlparams[23] = new SqlParameter("@ChallanNo", DBNull.Value);

                if (f_ChallanDate.HasValue)
                    sqlparams[24] = new SqlParameter("@ChallanDate", f_ChallanDate.Value);
                else
                    sqlparams[24] = new SqlParameter("@ChallanDate", DBNull.Value);

                if (f_JobCardRefNo.HasValue)
                    sqlparams[25] = new SqlParameter("@JobCardRefNo", f_JobCardRefNo.Value);
                else
                    sqlparams[25] = new SqlParameter("@JobCardRefNo", DBNull.Value);

                if (f_IsTransferred.HasValue)
                    sqlparams[26] = new SqlParameter("@IsTransferred", f_IsTransferred.Value);
                else
                    sqlparams[26] = new SqlParameter("@IsTransferred", DBNull.Value);

                if (f_ProductRecipeID.HasValue)
                    sqlparams[27] = new SqlParameter("@ProductRecipeID", f_ProductRecipeID.Value);
                else
                    sqlparams[27] = new SqlParameter("@ProductRecipeID", DBNull.Value);

                if (f_ProductQuantity.HasValue)
                    sqlparams[28] = new SqlParameter("@ProductQuantity", f_ProductQuantity.Value);
                else
                    sqlparams[28] = new SqlParameter("@ProductQuantity", DBNull.Value);

                if (f_DueDate.HasValue)
                    sqlparams[29] = new SqlParameter("@DueDate", f_DueDate.Value);
                else
                    sqlparams[29] = new SqlParameter("@DueDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RearTyreLeft))
                    sqlparams[30] = new SqlParameter("@RearTyreLeft", f_RearTyreLeft);
                else
                    sqlparams[30] = new SqlParameter("@RearTyreLeft", DBNull.Value);

                if (!string.IsNullOrEmpty(f_RearTyreRight))
                    sqlparams[31] = new SqlParameter("@RearTyreRight", f_RearTyreRight);
                else
                    sqlparams[31] = new SqlParameter("@RearTyreRight", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FrontTyreRight))
                    sqlparams[32] = new SqlParameter("@FrontTyreRight", f_FrontTyreRight);
                else
                    sqlparams[32] = new SqlParameter("@FrontTyreRight", DBNull.Value);

                if (!string.IsNullOrEmpty(f_FrontTyreleft))
                    sqlparams[33] = new SqlParameter("@FrontTyreleft", f_FrontTyreleft);
                else
                    sqlparams[33] = new SqlParameter("@FrontTyreleft", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BatteryNo))
                    sqlparams[34] = new SqlParameter("@BatteryNo", f_BatteryNo);
                else
                    sqlparams[34] = new SqlParameter("@BatteryNo", DBNull.Value);

                if (f_IsReversed.HasValue)
                    sqlparams[35] = new SqlParameter("@IsReversed", f_IsReversed.Value);
                else
                    sqlparams[35] = new SqlParameter("@IsReversed", DBNull.Value);

                if (f_ReversalDate.HasValue)
                    sqlparams[36] = new SqlParameter("@ReversalDate", f_ReversalDate.Value);
                else
                    sqlparams[36] = new SqlParameter("@ReversalDate", DBNull.Value);

                if (f_ReversalID.HasValue)
                    sqlparams[37] = new SqlParameter("@ReversalID", f_ReversalID.Value);
                else
                    sqlparams[37] = new SqlParameter("@ReversalID", DBNull.Value);

                if (f_CostCenterIDTo.HasValue)
                    sqlparams[38] = new SqlParameter("@CostCenterIDTo", f_CostCenterIDTo.Value);
                else
                    sqlparams[38] = new SqlParameter("@CostCenterIDTo", DBNull.Value);

                if (f_Loan_StockAmount.HasValue)
                    sqlparams[39] = new SqlParameter("@Loan_StockAmount", f_Loan_StockAmount.Value);
                else
                    sqlparams[39] = new SqlParameter("@Loan_StockAmount", DBNull.Value);

                if (f_Loan_COGSAmount.HasValue)
                    sqlparams[40] = new SqlParameter("@Loan_COGSAmount", f_Loan_COGSAmount.Value);
                else
                    sqlparams[40] = new SqlParameter("@Loan_COGSAmount", DBNull.Value);

                if (!string.IsNullOrEmpty(f_Loan_TransactionType))
                    sqlparams[41] = new SqlParameter("@Loan_TransactionType", f_Loan_TransactionType);
                else
                    sqlparams[41] = new SqlParameter("@Loan_TransactionType", DBNull.Value);

                if (f_FreightFC.HasValue)
                    sqlparams[42] = new SqlParameter("@FreightFC", f_FreightFC.Value);
                else
                    sqlparams[42] = new SqlParameter("@FreightFC", DBNull.Value);

                if (f_ExchangeRate.HasValue)
                    sqlparams[43] = new SqlParameter("@ExchangeRate", f_ExchangeRate.Value);
                else
                    sqlparams[43] = new SqlParameter("@ExchangeRate", DBNull.Value);

                if (f_FreightFCXExchangeRate.HasValue)
                    sqlparams[44] = new SqlParameter("@FreightFCXExchangeRate", f_FreightFCXExchangeRate.Value);
                else
                    sqlparams[44] = new SqlParameter("@FreightFCXExchangeRate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_InwardGatePass))
                    sqlparams[45] = new SqlParameter("@InwardGatePass", f_InwardGatePass);
                else
                    sqlparams[45] = new SqlParameter("@InwardGatePass", DBNull.Value);

                if (f_FiscalID.HasValue)
                    sqlparams[46] = new SqlParameter("@FiscalID", f_FiscalID.Value);
                else
                    sqlparams[46] = new SqlParameter("@FiscalID", DBNull.Value);

                if (!string.IsNullOrEmpty(f_BLNo))
                    sqlparams[47] = new SqlParameter("@BLNo", f_BLNo);
                else
                    sqlparams[47] = new SqlParameter("@BLNo", DBNull.Value);

                if (!string.IsNullOrEmpty(f_GDNumber))
                    sqlparams[48] = new SqlParameter("@GDNumber", f_GDNumber);
                else
                    sqlparams[48] = new SqlParameter("@GDNumber", DBNull.Value);

                if (f_ShipViaCode.HasValue)
                    sqlparams[49] = new SqlParameter("@ShipViaCode", f_ShipViaCode.Value);
                else
                    sqlparams[49] = new SqlParameter("@ShipViaCode", DBNull.Value);

                if (f_NoofContainers.HasValue)
                    sqlparams[50] = new SqlParameter("@NoofContainers", f_NoofContainers.Value);
                else
                    sqlparams[50] = new SqlParameter("@NoofContainers", DBNull.Value);

                if (f_ClearingCharges.HasValue)
                    sqlparams[51] = new SqlParameter("@ClearingCharges", f_ClearingCharges.Value);
                else
                    sqlparams[51] = new SqlParameter("@ClearingCharges", DBNull.Value);

                if (f_LogisticSupplierID.HasValue)
                    sqlparams[52] = new SqlParameter("@LogisticSupplierID", f_LogisticSupplierID.Value);
                else
                    sqlparams[52] = new SqlParameter("@LogisticSupplierID", DBNull.Value);

                if (f_ClearingAgentID.HasValue)
                    sqlparams[53] = new SqlParameter("@ClearingAgentID", f_ClearingAgentID.Value);
                else
                    sqlparams[53] = new SqlParameter("@ClearingAgentID", DBNull.Value);
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey, sqlparams) > 0)
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

        public bool UpdateDataByKeyForDOUpdateForm(int companyid, int periodid, int stocktransactionid, DateTime dispatchdate, int? businesspartnerid)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stocktransactionid);
                sqlparams[3] = new SqlParameter("@DocumentDate", dispatchdate);
                sqlparams[4] = new SqlParameter("@BusinessPartnerID", businesspartnerid);

                string Query = @"update inv_stocktransactionheader set DocumentDate=@DocumentDate, BusinessPartnerID = @BusinessPartnerID
                                    where StockTransactionID = @StockTransactionID 
                                            and CompanyID =@CompanyID 
                                            and PeriodID  = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataTable Get_StockHeaderForFixedAsset(int companyID, int periodID, string where)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[2];
                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                string qry_GetStockForFixedAsset = @" 
SELECT DISTINCT INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, INV_StockTransactionHeader.StockTransactionID, 
	SET_DocumentType.DocumentType 
FROM INV_StockTransactionHeader 
	INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
		AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
		AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID And IsNull(IsIncurred, 0) = 0 
	Inner Join AP_PurchaseOrderHeader On AP_PurchaseOrderHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
		And AP_PurchaseOrderHeader.PeriodID = INV_StockTransactionDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID 
		And AP_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID 
		And AP_PurchaseOrderHeader.DocumentNumber = INV_StockTransactionDetail.SourceCode And AP_PurchaseOrderHeader.Status = 1 
		And AP_PurchaseOrderHeader.StatusCode = 1 
	Inner Join AP_PurchaseOrderDetail On AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
		And AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
		And AP_PurchaseOrderDetail.PODetailID = INV_StockTransactionDetail.SourceLineID And AP_PurchaseOrderDetail.Status = 1 
		And IsNull(AP_PurchaseOrderDetail.CapitalExpenseFlag, 0) = 1 
	INNER JOIN SET_DocumentType ON INV_StockTransactionHeader.CompanyID = SET_DocumentType.CompanyID AND SET_DocumentType.Status = 1 
		AND INV_StockTransactionHeader.PeriodID = SET_DocumentType.PeriodID AND INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
Where INV_StockTransactionHeader.CompanyID = @CompanyID And INV_StockTransactionHeader.PeriodID = @PeriodID And INV_StockTransactionHeader.Status = 1 
	And INV_StockTransactionHeader.StatusCode = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_GetStockForFixedAsset + where, sqlParms);

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


        public static DataSet GetStockRequisitionReport_RSM(int CompanyID, int PeriodID, int FiscalID, DateTime? fromDate, DateTime? toDate, Nullable<int> DocumentTypeID, string FromDocument, string ToDocument)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[3];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[2] = new SqlParameter("@FiscalID", FiscalID);

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");


                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }

                if (DocumentTypeID.HasValue)
                    sb.Append(" And Set_DocumentType.DocumentTypeID = " + DocumentTypeID.ToString() + " ");

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetStorePurchaseRequisitionReport_RSM, sb.ToString()), sqlParms);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetStockRequisitionStatusReport(int CompanyID, int PeriodID, int IsShow, DateTime? fromDate, DateTime? toDate, Nullable<int> DepartmentID, string HeadingCostCenter, Nullable<int> DocumentTypeID, Nullable<int> ItemID, string CostCenterIDs, string FromDocument, string ToDocument, string Query,
            short? documentGroupID = null, Nullable<int> ApprovalGroupID = null, Nullable<int> DocStatus = null, Nullable<int> DemandType = null,Nullable<int> SeasonType = null)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[2];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                StringBuilder sb = new StringBuilder();

                sb.Append(@" WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID and INV_StockTransactionHeader.Status = 1  ");

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                if (CostCenterIDs != string.Empty)
                {
                    sb.Append(" AND SET_CostCenter.CostCenterID in (" + CostCenterIDs + ") ");
                }


                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }
                if (DepartmentID.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DepartmentID = " + DepartmentID.Value.ToString() + " ");

                if (ItemID.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.ItemID = " + ItemID.Value.ToString() + " ");

                if (HeadingCostCenter != string.Empty)
                    sb.Append(" AND SET_CostCenter.HeadingCostCenterName like '" + HeadingCostCenter.ToString() + "' ");


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (DocumentTypeID.HasValue)
                    sb.Append(" And Set_DocumentType.DocumentTypeID = " + DocumentTypeID.ToString() + " ");

                if (ApprovalGroupID.HasValue)
                    sb.Append("  AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString());

                if (DocStatus.Value > -1)
                {
                    sb.Append("  AND INV_StockTransactionHeader.StatusCode = " + DocStatus);
                }

                
                if (DemandType > 0)
                    sb.Append(" AND INV_StockTransactionHeader.DemandType = " + DemandType);

                if (SeasonType > 0)
                    sb.Append(" AND INV_StockTransactionHeader.SeasonType = " + SeasonType);

                if (IsShow == 1)
                {
                    sb.Append("and COALESCE(AP_PurchaseOrderDetail.Quantity,0) = 0");
                }
                else
                {

                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetStockRequisitionStatusReportGIN(int CompanyID, int PeriodID, int IsShow, DateTime? fromDate, DateTime? toDate, Nullable<int> DepartmentID, string HeadingCostCenter, Nullable<int> DocumentTypeID, Nullable<int> ItemID, string CostCenterIDs, string FromDocument, string ToDocument, string Query,
            short? documentGroupID = null, Nullable<int> ApprovalGroupID = null, Nullable<int> DocStatus = null)
        {
            DataTable dtresult = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlParms = new SqlParameter[2];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                sb.Append(@" WHERE INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID and INV_StockTransactionHeader.Status = 1  ");

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                if (CostCenterIDs != string.Empty)
                {
                    sb.Append(" AND SET_CostCenter.CostCenterID in (" + CostCenterIDs + ") ");
                }


                if (FromDocument != string.Empty && ToDocument != string.Empty)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo between " + FromDocument + " and " + ToDocument + " ");
                }
                if (DepartmentID.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DepartmentID = " + DepartmentID.Value.ToString() + " ");

                if (ItemID.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.ItemID = " + ItemID.Value.ToString() + " ");

                if (HeadingCostCenter != string.Empty)
                    sb.Append(" AND SET_CostCenter.HeadingCostCenterName like '" + HeadingCostCenter.ToString() + "' ");


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (DocumentTypeID.HasValue)
                    sb.Append(" And Set_DocumentType.DocumentTypeID = " + DocumentTypeID.ToString() + " ");

                if (ApprovalGroupID.HasValue)
                    sb.Append("  AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString());

                if (DocStatus.Value > -1)
                {
                    sb.Append("  AND INV_StockTransactionHeader.StatusCode = " + DocStatus);
                }

                if (IsShow == 1)
                {
                    sb1.Append(" where COALESCE(Tab.IssuedQuantity,0) = 0");
                }
                else
                {

                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString(), sb1.ToString()), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable StockTransferNote(int CompanyID, int PeriodID, string transferType, DateTime? fromDate, DateTime? DateTo, int Location, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtresult = null;

            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[2] = new SqlParameter("@FromDate", fromDate);
                sqlParms[3] = new SqlParameter("@DateTo", DateTo);
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (Location > 0)
                    Filters.Append(" and INV_StockTransactionHeader.LocationID = " + Location);

                if (documentGroupID.HasValue)
                    Filters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (!string.IsNullOrEmpty(transferType))
                    Filters.Append(" " + transferType.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_StockTransferNoteReport, Filters), sqlParms);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public bool DeleteGINTransaction(int TransactionHeaderID, Guid CurrentUserID, string UserName)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("GINStatusChange");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);

                DataTable Dt_Stocktransactiondetail = INV_StockTransactionDetail.GetDataByTransactionHeaderID(this.f_StockTransactionID, this.CompanyID, this.PeriodID, true, sqltrans);
                if (Dt_Stocktransactiondetail != null && Dt_Stocktransactiondetail.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt_Stocktransactiondetail.Rows.Count; i++)
                    {
                        INV_StockTransactionDetail obj_StockDetail = new INV_StockTransactionDetail();
                        obj_StockDetail.TransactionDetailID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["TransactionDetailID"].ToString());
                        obj_StockDetail.StockTransactionID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["StockTransactionID"].ToString());
                        obj_StockDetail.POQuantity = 0;
                        obj_StockDetail.CompanyID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["CompanyID"].ToString());
                        obj_StockDetail.PeriodID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["PeriodID"].ToString());

                        INV_StockTransactionDetail.UpdateStockDetailPOQtyByKey(obj_StockDetail.CompanyID, obj_StockDetail.PeriodID, long.Parse(obj_StockDetail.TransactionDetailID.ToString()), int.Parse(obj_StockDetail.StockTransactionID.ToString()), int.Parse(obj_StockDetail.POQuantity.ToString()), sqltrans);
                    }
                }

                DataTable Dt_GLDetail = GL_TransactionHeader.GetAccountGINData(this.CompanyID, this.PeriodID, this.DocumentTypeID, this.DocumentNo, int.Parse(this.FiscalID.ToString()), null, sqltrans);

                if (Dt_GLDetail != null && Dt_GLDetail.Rows.Count > 0)
                {
                    GL_TransactionHeader obj_GlHeader = new GL_TransactionHeader(this.CompanyID, this.PeriodID, Convert.ToInt32(Dt_GLDetail.Rows[0]["TransactionID"]),
                        null, sqltrans);

                    for (int i = 0; i < Dt_GLDetail.Rows.Count; i++)
                    {
                        GL_TransactionDetail.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID,
                            int.Parse(Dt_GLDetail.Rows[i]["SerialNo"].ToString()), obj_GlHeader.TransactionID);
                    }

                    GL_TransactionHeader.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID, obj_GlHeader.TransactionID);

                    SET_DocumentApprove.DeleteApprovalHistroy(this.CompanyID, this.PeriodID, int.Parse(this.DocumentNo.ToString()), this.DocumentTypeID, null, int.Parse(this.FiscalID.ToString()));

                    SET_DocumentApprove SET_DocumentApprv = new SET_DocumentApprove();
                    SET_DocumentApprv.CompanyID = this.CompanyID;
                    SET_DocumentApprv.PeriodID = this.PeriodID;
                    SET_DocumentApprv.Status = true;
                    SET_DocumentApprv.Comments = string.Empty;
                    SET_DocumentApprv.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.CompanyID, this.PeriodID);
                    SET_DocumentApprv.DocumentNo = this.DocumentNo.ToString();
                    SET_DocumentApprv.DocumentTypeID = this.DocumentTypeID;
                    SET_DocumentApprv.CreateDate = DateTime.Now;
                    SET_DocumentApprv.CreatedBy = CurrentUserID;
                    SET_DocumentApprv.CreatedByValue = UserName;
                    SET_DocumentApprv.ApprovalStatus = Enumaration.CurrentStatus.InProcess.ToString();
                    SET_DocumentApprv.Insert();
                }
                //else
                //    throw new BusinessLogicException("Voucher Detail Not Found Agaionst Document no : " + this.DocumentNo + "");

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
                    SqlHelper.RollBackTransaction(sqltrans, "GINStatusChange");
            }

            return result;
        }

        public bool DeleteGRNTransaction(int TransactionHeaderID, int DocumentNo, Guid CurrentUserID, string UserName)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("GRNStatusChange");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);

                DataTable Dt_Stocktransactiondetail = INV_StockTransactionDetail.GetDataByTransactionHeaderID(this.f_StockTransactionID, this.CompanyID, this.PeriodID, true, sqltrans);
                if (Dt_Stocktransactiondetail != null && Dt_Stocktransactiondetail.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt_Stocktransactiondetail.Rows.Count; i++)
                    {
                        INV_StockTransactionDetail obj_StockDetail = new INV_StockTransactionDetail();
                        obj_StockDetail.TransactionDetailID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["TransactionDetailID"].ToString());
                        obj_StockDetail.StockTransactionID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["StockTransactionID"].ToString());
                        obj_StockDetail.POQuantity = 0;
                        obj_StockDetail.CompanyID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["CompanyID"].ToString());
                        obj_StockDetail.PeriodID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["PeriodID"].ToString());

                        INV_StockTransactionDetail.UpdateStockDetailPOQtyByKey(obj_StockDetail.CompanyID, obj_StockDetail.PeriodID, long.Parse(obj_StockDetail.TransactionDetailID.ToString()), int.Parse(obj_StockDetail.StockTransactionID.ToString()), int.Parse(obj_StockDetail.POQuantity.ToString()), sqltrans);
                    }
                }

                DataTable Dt_GLDetail = GL_TransactionHeader.GetAccountGRNData(this.CompanyID, this.PeriodID, this.DocumentTypeID, this.DocumentNo, null, sqltrans, this.FiscalID);

                if (Dt_GLDetail != null && Dt_GLDetail.Rows.Count > 0)
                {
                    GL_TransactionHeader obj_GlHeader = new GL_TransactionHeader(this.CompanyID, this.PeriodID, Convert.ToInt32(Dt_GLDetail.Rows[0]["TransactionID"]),
                        null, sqltrans);

                    for (int i = 0; i < Dt_GLDetail.Rows.Count; i++)
                    {
                        GL_TransactionDetail.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID,
                            int.Parse(Dt_GLDetail.Rows[i]["SerialNo"].ToString()), obj_GlHeader.TransactionID);
                    }

                    GL_TransactionHeader.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID, obj_GlHeader.TransactionID);

                    SET_DocumentApprove.DeleteApprovalHistroy(this.CompanyID, this.PeriodID, int.Parse(this.DocumentNo.ToString()), this.DocumentTypeID, null, int.Parse(this.FiscalID.ToString()));
                    
                    SET_DocumentApprove SET_DocumentAppr = new SET_DocumentApprove();
                    SET_DocumentAppr.CompanyID = this.CompanyID;
                    SET_DocumentAppr.PeriodID = this.PeriodID;
                    SET_DocumentAppr.Status = true;
                    SET_DocumentAppr.Comments = string.Empty;
                    SET_DocumentAppr.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.CompanyID, this.PeriodID);
                    SET_DocumentAppr.DocumentNo = this.DocumentNo.ToString();
                    SET_DocumentAppr.DocumentTypeID = this.DocumentTypeID;
                    SET_DocumentAppr.CreateDate = DateTime.Now;
                    SET_DocumentAppr.CreatedBy = CurrentUserID;
                    SET_DocumentAppr.CreatedByValue = UserName;
                    SET_DocumentAppr.ApprovalStatus = Enumaration.CurrentStatus.InProcess.ToString();
                    SET_DocumentAppr.FiscalID = this.FiscalID;
                    SET_DocumentAppr.Insert();
                }
                //else
                //    throw new BusinessLogicException("Voucher Detail Not Found Agaionst Document no : " + this.DocumentNo + "");

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
                    SqlHelper.RollBackTransaction(sqltrans, "GRNStatusChange");
            }

            return result;
        }

        public bool DeleteRETURNTransaction(int TransactionHeaderID, int DocumentNo, Guid CurrentUserID, string UserName)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("RETURNStatusChange");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);

                DataTable Dt_Stocktransactiondetail = INV_StockTransactionDetail.GetDataByTransactionHeaderID(this.f_StockTransactionID, this.CompanyID, this.PeriodID, true, sqltrans);
                if (Dt_Stocktransactiondetail != null && Dt_Stocktransactiondetail.Rows.Count > 0)
                {
                    for (int i = 0; i < Dt_Stocktransactiondetail.Rows.Count; i++)
                    {
                        INV_StockTransactionDetail obj_StockDetail = new INV_StockTransactionDetail();
                        obj_StockDetail.TransactionDetailID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["TransactionDetailID"].ToString());
                        obj_StockDetail.StockTransactionID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["StockTransactionID"].ToString());
                        obj_StockDetail.POQuantity = 0;
                        obj_StockDetail.CompanyID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["CompanyID"].ToString());
                        obj_StockDetail.PeriodID = int.Parse(Dt_Stocktransactiondetail.Rows[i]["PeriodID"].ToString());

                        INV_StockTransactionDetail.UpdateStockDetailPOQtyByKey(obj_StockDetail.CompanyID, obj_StockDetail.PeriodID, long.Parse(obj_StockDetail.TransactionDetailID.ToString()), int.Parse(obj_StockDetail.StockTransactionID.ToString()), int.Parse(obj_StockDetail.POQuantity.ToString()), sqltrans);
                    }
                }

                DataTable Dt_GLDetail = GL_TransactionHeader.GetAccountRETURNData(this.CompanyID, this.PeriodID, this.DocumentTypeID, this.DocumentNo, null, sqltrans, this.FiscalID);

                if (Dt_GLDetail != null && Dt_GLDetail.Rows.Count > 0)
                {
                    GL_TransactionHeader obj_GlHeader = new GL_TransactionHeader(this.CompanyID, this.PeriodID, Convert.ToInt32(Dt_GLDetail.Rows[0]["TransactionID"]),
                        null, sqltrans);

                    for (int i = 0; i < Dt_GLDetail.Rows.Count; i++)
                    {
                        GL_TransactionDetail.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID,
                            int.Parse(Dt_GLDetail.Rows[i]["SerialNo"].ToString()), obj_GlHeader.TransactionID);
                    }

                    GL_TransactionHeader.DeleteDataByKey(sqltrans, CurrentUserID, DateTime.Now, UserName, this.CompanyID, this.PeriodID, obj_GlHeader.TransactionID);

                    SET_DocumentApprove SET_DocumentApprove = new SET_DocumentApprove();
                    SET_DocumentApprove.CompanyID = this.CompanyID;
                    SET_DocumentApprove.PeriodID = this.PeriodID;
                    SET_DocumentApprove.Status = true;
                    SET_DocumentApprove.Comments = string.Empty;
                    SET_DocumentApprove.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.CompanyID, this.PeriodID);
                    SET_DocumentApprove.DocumentNo = this.DocumentNo.ToString();
                    SET_DocumentApprove.DocumentTypeID = this.DocumentTypeID;
                    SET_DocumentApprove.CreateDate = DateTime.Now;
                    SET_DocumentApprove.CreatedBy = CurrentUserID;
                    SET_DocumentApprove.CreatedByValue = UserName;
                    SET_DocumentApprove.ApprovalStatus = Enumaration.CurrentStatus.InProcess.ToString();
                    SET_DocumentApprove.Insert();
                }
                //else
                //    throw new BusinessLogicException("Voucher Detail Not Found Agaionst Document no : " + this.DocumentNo + "");

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
                    SqlHelper.RollBackTransaction(sqltrans, "RETURNStatusChange");
            }

            return result;
        }

        public static bool IsInvoiceMade(int companyID, int periodID, int stockHeaderID)
        {
            bool isInvoiceMade = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

                string qry_IsInvoiceMade = @" Select * From AR_InvoiceDetail Where   StockHeaderID = @StockHeaderID and CompanyID =@CompanyID  and Status = 1 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_IsInvoiceMade, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    isInvoiceMade = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return isInvoiceMade;
        }

        public static DataTable Get_GRNCWIP(int companyID, int periodID)
        {
            DataTable dtresult = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CWIP_TransferOut, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable Get_GRNCWIPHeader(int companyID, int periodID)
        {
            DataTable dtresult = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CWIP_TransferOutHeader, sqlparams);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CWIP_TransferOutHeaderA, sqlparams);


                if (ds != null && ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];



                DataTable dt_detail = Get_GRNCWIP(companyID, periodID);


                if (dt_detail != null && dt_detail.Rows.Count > 0)
                {

                    foreach (DataRow dr in dtresult.Rows)
                    {

                    }

                }



            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtresult;
        }

        public static DataTable GetLandCostDetail(int CompanyID, int PeriodID, int? DocumentGroupID, DateTime? fromDate, DateTime? toDate, string Chargetype)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            string Query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                #region Filter by Invoice Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate >= @FromDate ";
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate <= @ToDate ";
                    sqlparams[2] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                if (DocumentGroupID.HasValue)
                {
                    additionalParameters = " AND SET_DocumentType.GroupID =" + DocumentGroupID.Value;
                }




                #region Filter by payment

                switch (Chargetype)
                {
                    case "0":
                        Query = @"SELECT 

 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,'Custom Duty' as  Type
,SET_DocumentType.DocumentType,INV_Item.Description as ItemName,INV_StockTransactionDetail.TransactionQuantity,INV_StockTransactionHeader.DocumentDate
 ,CustomDutyBP.CustomerName as BusinessPartner,isnull(INV_StockTransactionDetail.CustomDuty,0) as Cost
 
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
Left JOIN INV_Item ON  INV_Item.ItemID=INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID=INV_StockTransactionDetail.CompanyID
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer CustomDutyBP ON CustomDutyBP.CustomerID = INV_StockTransactionDetail.CustomDutyID
 AND CustomDutyBP.CompanyID = INV_StockTransactionDetail.CompanyID
 Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
 {0}";
                        break;

                    case "1":
                        Query = @" SELECT 
 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,'Clearing Charges' as  Type
,SET_DocumentType.DocumentType,INV_Item.Description as ItemName,INV_StockTransactionDetail.TransactionQuantity,INV_StockTransactionHeader.DocumentDate
 ,ClearingChargesBP.CustomerName as BusinessPartner,isnull(INV_StockTransactionDetail.ClearingCharges,0) as Cost
 
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
Left JOIN INV_Item ON  INV_Item.ItemID=INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID=INV_StockTransactionDetail.CompanyID
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer ClearingChargesBP ON ClearingChargesBP.CustomerID = INV_StockTransactionDetail.ClearingChargesID
  AND ClearingChargesBP.CompanyID = INV_StockTransactionDetail.CompanyID
   Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
  {0}
   ";
                        break;
                    case "2":
                        Query = @" SELECT 

 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,
 'Other Charges' as  Type,
 SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate,INV_Item.Description as ItemName
 ,INV_StockTransactionDetail.TransactionQuantity
 ,OtherChargesBP.CustomerName as BusinessPartner,isnull(INV_StockTransactionDetail.OtherCHarges,0) as Cost
 
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
Left JOIN INV_Item ON  INV_Item.ItemID=INV_StockTransactionDetail.ItemID
--AND INV_Item.CompanyID=INV_StockTransactionDetail.CompanyID
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer OtherChargesBP ON OtherChargesBP.CustomerID = INV_StockTransactionDetail.ClearingChargesID
  AND OtherChargesBP.CompanyID = INV_StockTransactionDetail.CompanyID
   Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
   {0}";
                        break;
                }

                #endregion

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text,
                    string.Format(Query, additionalParameters), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetLandCostSummary(int CompanyID, int PeriodID, int? DocumentGroupID, DateTime? fromDate, DateTime? toDate, string Chargetype)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            string Query = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                #region Filter by Invoice Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate >= @FromDate ";
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    additionalParameters = " AND INV_StockTransactionHeader.DocumentDate <= @ToDate ";
                    sqlparams[2] = new SqlParameter("@ToDate", toDate.Value);
                }

                if (DocumentGroupID.HasValue)
                {
                    additionalParameters = " AND SET_DocumentType.GroupID =" + DocumentGroupID.Value;
                }
                #endregion

                #region Filter by payment

                switch (Chargetype)
                {
                    case "0":
                        Query = @"SELECT 

 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,'Custom Duty' as  Type,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate
 ,CustomDutyBP.CustomerName as BusinessPartner,isnull(Sum(INV_StockTransactionDetail.CustomDuty),0) as Cost
 --isnull(INV_StockTransactionDetail.CustomDuty,0) as Cost
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer CustomDutyBP ON CustomDutyBP.CustomerID = INV_StockTransactionDetail.CustomDutyID
 AND CustomDutyBP.CompanyID = INV_StockTransactionDetail.CompanyID
 Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
{0}
 Group By INV_StockTransactionHeader.DocumentNo,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate,CustomDutyBP.CustomerName
 ";
                        break;

                    case "1":
                        Query = @" SELECT 
 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,'Clearing Charges' as  Type,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate
 ,ClearingChargesBP.CustomerName as BusinessPartner,isnull(Sum(INV_StockTransactionDetail.ClearingCharges),0) as Cost
 
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer ClearingChargesBP ON ClearingChargesBP.CustomerID = INV_StockTransactionDetail.ClearingChargesID
  AND ClearingChargesBP.CompanyID = INV_StockTransactionDetail.CompanyID
  Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
  {0}
  Group By INV_StockTransactionHeader.DocumentNo,SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate,ClearingChargesBP.CustomerName
  
   ";
                        break;
                    case "2":
                        Query = @" SELECT 
 INV_StockTransactionHeader.DocumentNo,row_number() over (ORDER By INV_StockTransactionHeader.DocumentNo) as SerialNo,
 'Other Charges' as  Type,
 SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate
 ,OtherChargesBP.CustomerName as BusinessPartner,isnull(Sum(INV_StockTransactionDetail.OtherCHarges),0) as Cost
 
  FROM 
INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyiD =INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID =INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status=1
INNER JOIN SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and SET_DocumentType.PeriodID =INV_StockTransactionHeader.PeriodID
 INNER JOIN AR_SET_Customer OtherChargesBP ON OtherChargesBP.CustomerID = INV_StockTransactionDetail.ClearingChargesID
  AND OtherChargesBP.CompanyID = INV_StockTransactionDetail.CompanyID
   Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID =@PeriodID
 {0}
  GROUP BY INV_StockTransactionHeader.DocumentNo,
 SET_DocumentType.DocumentType,INV_StockTransactionHeader.DocumentDate
 ,OtherChargesBP.CustomerName ";
                        break;
                }

                #endregion

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text,
                    string.Format(Query, additionalParameters), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }




        public static DataSet InventryInOutForReport(int CompanyID, int PeriodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int Location, int Catagoery, int ItemID, string FromDocument, string ToDocument, string QueryForInventoryInOut, int CurrentFiscalID)
        {
            DataSet ds;


            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentType", DocumentType);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (Location > 0)
                    Filters.Append(" and INV_StockTransactionDetail.LocationID = " + Location);

                if (Catagoery > 0)
                    Filters.Append(" and INV_Item.ItemTypeID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);

                if (FromDocument != "" && ToDocument != "")
                {
                    if (int.Parse(FromDocument) > 0 && int.Parse(ToDocument) > 0)
                        Filters.Append(" AND INV_StockTransactionHeader.DocumentNo between " + int.Parse(FromDocument) + " and " + int.Parse(ToDocument) + " ");
                }




                ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForInventoryInOut + Filters, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet InventryInOutStoreForReport(int CompanyID, int PeriodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int Location, int Catagoery, int ItemID, string FromDocument, string ToDocument, string QueryForInventoryInOutStore, int CurrentFiscalID)
        {
            DataSet ds;


            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentType", DocumentType);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (Location > 0)
                    Filters.Append(" and INV_StockTransactionDetail.LocationID = " + Location);

                if (Catagoery > 0)
                    Filters.Append(" and INV_Item.ItemTypeID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);

                if (FromDocument != "" && ToDocument != "")
                {
                    if (int.Parse(FromDocument) > 0 && int.Parse(ToDocument) > 0)
                        Filters.Append(" AND INV_StockTransactionHeader.DocumentNo between " + int.Parse(FromDocument) + " and " + int.Parse(ToDocument) + " ");
                }




                ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForInventoryInOutStore + Filters, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataTable Get_DataCWIPReport(int CompanyID, int PeriodID, int DocumentType, int LocationID, int Catagoery, int ItemID)
        {
            DataTable dt = null;
            StringBuilder AddFilters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);


                if (DocumentType > 0)
                    AddFilters.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentType);

                if (LocationID > 0)
                    AddFilters.Append(" and INV_StockTransactionHeader.LocationID = " + LocationID);

                if (Catagoery > 0)
                    AddFilters.Append(" and INV_Item.ItemTypeID = " + Catagoery);

                if (ItemID > 0)
                    AddFilters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_CWIPReport, AddFilters), sqlparams);
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

        public static DataTable StoreMovementReceivableDetailReport(string companies, int PeriodID, int FiscalID, int? CataLogID, Nullable<DateTime> AsOnDate)
        {
            DataTable dt = null;
            StringBuilder additionalParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companies);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@AsOnDate", AsOnDate);

                #region Query


                string query = @"

select SET_ItemCatalog.GroupID, SET_ItemCatalog.Description,isnull(Abs(SUM(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost )),0) as Amount
,'Receivable' as MainHeading
,AR_SET_BusinessPartner.BusinessPartnerName

from INV_StockTransactionHeader

left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = INV_StockTransactionHeader.Status
and INV_StockTransactionDetail.FiscalID = INV_StockTransactionHeader.FiscalID

inner join Set_Job on Set_Job.JobID = INV_StockTransactionDetail.JobID
and Set_Job.JobCategoryID = 2
inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID

inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
and SET_ItemCatalog.Status = INV_Item.Status

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID
and AR_SET_Customer.Status = INV_StockTransactionHeader.Status

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 

inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where INV_StockTransactionHeader.Status = 1 and INV_StockTransactionHeader.StatusCode = 1 and INV_StockTransactionHeader.CompanyID in @CompanyID 
and INV_StockTransactionHeader.PeriodID = @PeriodID 
and INV_StockTransactionHeader.DocumentDate <= @AsOnDate

and DocumentTypeID in (
select SET_DocumentType.DocumentTypeID from SET_DocumentType where MenuID in (
select MenuID from ApplicationMenu where MenuPath like '%/Inventory/Transaction/Average/StockGIN_frm.aspx%')
and Status = 1 and IsActive = 1 and CompanyID = 1
)

and INV_StockTransactionHeader.BusinessPartnerID is not null
and AR_SET_BusinessPartner.BusinessPartnerName not like 'RELATED PARTIES'

and ( AR_SET_BusinessPartner.BusinessPartnerName like '%Employee%' OR AR_SET_BusinessPartner.BusinessPartnerName like '%Contractors%' OR AR_SET_BusinessPartner.BusinessPartnerName like '%Service_Providers%'
OR AR_SET_BusinessPartner.BusinessPartnerName like '%Inter Unit%'
 )

 {0}

group by SET_ItemCatalog.GroupID,SET_ItemCatalog.Description ,AR_SET_BusinessPartner.BusinessPartnerName


union all

select SET_ItemCatalog.GroupID, SET_ItemCatalog.Description,isnull(Abs(SUM(INV_StockTransactionDetail.TransactionQuantity * INV_StockTransactionDetail.TransactionCost )),0) as Amount
,'RELATED PARTIES' as MainHeading
,AR_SET_Customer.CustomerName BusinessPartnerName

from INV_StockTransactionHeader

left join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = INV_StockTransactionHeader.Status
and INV_StockTransactionDetail.FiscalID = INV_StockTransactionHeader.FiscalID

inner join Set_Job on Set_Job.JobID = INV_StockTransactionDetail.JobID
and Set_Job.JobCategoryID = 2
inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID

inner join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
and SET_ItemCatalog.Status = INV_Item.Status

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID
and AR_SET_Customer.Status = INV_StockTransactionHeader.Status

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 

inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where INV_StockTransactionHeader.Status = 1 and INV_StockTransactionHeader.StatusCode = 1 and INV_StockTransactionHeader.CompanyID in @CompanyID 
and INV_StockTransactionHeader.PeriodID = @PeriodID 
and INV_StockTransactionHeader.DocumentDate <= @AsOnDate

and DocumentTypeID in (
select SET_DocumentType.DocumentTypeID from SET_DocumentType where MenuID in (
select MenuID from ApplicationMenu where MenuPath like '%/Inventory/Transaction/Average/StockGIN_frm.aspx%')
and Status = 1 and IsActive = 1 and CompanyID = 1
)

and INV_StockTransactionHeader.BusinessPartnerID is not null

and  AR_SET_BusinessPartner.BusinessPartnerName like '%RELATED PARTIES%' 
 

 {0}
group by SET_ItemCatalog.GroupID,SET_ItemCatalog.Description ,AR_SET_Customer.CustomerName
";

                #endregion

                if (CataLogID.HasValue)
                    additionalParameters.Append(" AND SET_ItemCatalog.ItemCatalogID = " + CataLogID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query.Replace("@CompanyID", "(" + companies + ")"), additionalParameters), sqlparams);

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


        public static DataTable GetDataForGoodsReceiveNote_Imported(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
           Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
           Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, int? DocumentStatus = null, short? documentGroupID = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (reqfrom != null && reqto != null)
                {
                    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                }
                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus;
                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GoodsReceiveNote_Imported + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GoodsReceiveNote_Imported + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable Select_StockRequisition_LookupPR(int companyID, int periodID, string filter = "")
        {
            DataTable dt;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                string query = Query_Select_StockRequisition_LookupPR.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetConsumptionconsolidatedReport(int companyID, int periodID, int ApprovingGroup, string coaFrom, string coaTo, Nullable<int> departmentID,
           Nullable<int> costerCenterID, Nullable<int> incurredOnID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
            short? documentGroupID, Nullable<int> ItemID = null, Nullable<int> CategoryID = null, Nullable<int> CatalogID = null
            , Nullable<int> ConsumptionReportType = null, bool IsStockReturn = false
           )
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;


            #region Query

            const string Query_GetConsumptionconsolidated = @"  Select CategoryName,CostCenterID, CostCenterName,DepartmentName,Description,ItemID, UOMName,SUM(TransactionQuantity)  as TransactionQuantity 
,AVG(TransactionCost) as  TransactionCost,Avg(ListPricePerUnit) as  ListPricePerUnit,SUM(Amount) as Amount 
from (
Select INV_SET_Category.CategoryName,sth.CostCenterID, SET_CostCenter.CostCenterName, 
SET_Department.DepartmentName, INV_Item.Description, INV_Item.ItemID, 
    SET_UnitofMeasure.UOMName, 
	(std.TransactionQuantity * (-1)) AS TransactionQuantity,
	 std.TransactionCost, 
	 --GL_TransactionDetail.Amount / (std.TransactionQuantity * (-1)) as TransactionCost,
	 INV_Item.ListPricePerUnit, 
    ((std.TransactionQuantity * (-1)) * std.TransactionCost) Amount,
	--GL_TransactionDetail.Amount as Amount,
	 sth.DocumentDate, 
	SET_DocumentType.DocumentType, 
case when std.JobID > 0 then Set_Job.AccountCode else  Inv_Item.ExpenseAccountCCID end as ExpenseAccountCCID 
From INV_StockTransactionHeader sth 
    INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = sth.StockTransactionID 
	AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID 
        AND std.Status = 1 
 
  Inner join [SET_DocumentType]  on [SET_DocumentType].DocumentTypeID = sth.DocumentTypeID
		and [SET_DocumentType].CompanyID = sth.CompanyID
		and [SET_DocumentType].PeriodID = sth.PeriodID
		and [SET_DocumentType].menuID in (select MenuID from [ApplicationMenu] where MenuPath in ('/Inventory/Transaction/Average/StockGIN_frm.aspx','/Inventory/Transaction/StockReturn.aspx'))
        and [SET_DocumentType].DocumentType not like '%CPR%'
		and [SET_DocumentType].Status = 1
   left join INV_StockTransactionHeader GIn on GIN.StockTransactionID = std.SourceHeaderID
        and GIN.DocumentTypeID = std.SourceDocumentTypeID
and GIN.CompanyID = std.CompanyID
		and GIN.PeriodID = std.PeriodID
		and GIN.FiscalID= std.FiscalID
    INNER JOIN INV_ITem On INV_ITem.ItemID = std.ITemID --AND INV_ITem.CompanyID = std.CompanyID 
    Inner Join SET_UnitofMeasure On INV_Item.PrimaryUOMCode = SET_UnitofMeasure.UOMID 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = sth.CostCenterID 
	LEFT JOIN SET_CostCenter returncostcenter ON returncostcenter.CostCenterID = GIN.CostCenterID 
    LEFT JOIN SET_Department ON SET_Department.DepartmentID = sth.DepartmentID 
	LEFT JOIN SET_Department returndept ON returndept.DepartmentID = GIN.DepartmentID 
  INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 

Left Join Set_Job on Set_Job.JobID =std.JobID
and Set_Job.CompanyID = std.CompanyID

WHERE sth.CompanyID = @CompanyID AND sth.PeriodID = @PeriodID AND sth.Status = 1 AND sth.StatusCode = 1
 {0}
) as  fintab
GROUP BY CategoryName,CostCenterID, CostCenterName,DepartmentName,Description,ItemID, UOMName";


            #endregion
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[9];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                #region Department

                if (departmentID.HasValue)
                {
                    sb.Append(" AND SET_Department.DepartmentID = @DepartmentID ");
                    sqlParms[2] = new SqlParameter("@DepartmentID", departmentID.Value);
                }

                #endregion

                #region Cost Center

                if (costerCenterID.HasValue)
                {
                    sb.Append(" AND sth.CostCenterID = @CostCenterID ");
                    sqlParms[3] = new SqlParameter("@CostCenterID", costerCenterID.Value);
                }

                #endregion

                #region Incurred On

                if (incurredOnID.HasValue)
                {
                    sb.Append(" AND std.IncurredOnID = @IncurredOnID ");
                    sqlParms[4] = new SqlParameter("@IncurredOnID", incurredOnID.Value);
                }

                #endregion

                #region Item Expense Account Codes From & To

                if (!string.IsNullOrEmpty(coaFrom) && !string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID BETWEEN @FromAccountCode AND @ToAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }
                else if (!string.IsNullOrEmpty(coaFrom))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @FromAccountCode ");
                    sqlParms[5] = new SqlParameter("@FromAccountCode", coaFrom);
                }
                else if (!string.IsNullOrEmpty(coaTo))
                {
                    sb.Append(" AND Inv_Item.ExpenseAccountCCID >= @ToAccountCode ");
                    sqlParms[6] = new SqlParameter("@ToAccountCode", coaTo);
                }

                #endregion

                #region From & To Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate BETWEEN @FromDate AND @ToDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }
                else if (fromDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate >= @FromDate ");
                    sqlParms[7] = new SqlParameter("@FromDate", fromDate.Value);
                }
                else if (toDate.HasValue)
                {
                    sb.Append(" AND sth.DocumentDate <= @ToDate ");
                    sqlParms[8] = new SqlParameter("@ToDate", toDate.Value);
                }

                #endregion

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ApprovingGroup > 0)
                    sb.Append("And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (ItemID.HasValue)
                    sb.Append("And INV_Item.ItemID = " + ItemID.ToString());

                if (CategoryID.HasValue)
                    sb.Append("And INV_Set_Category.categoryID = " + CategoryID.ToString());

                if (CatalogID.HasValue)
                    sb.Append("And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                if (ConsumptionReportType.HasValue)
                {
                    if (ConsumptionReportType == 1)
                    {
                        sb.Append("  AND (std.JobID > 0 )");
                    }
                    else if (ConsumptionReportType == 3)
                    {
                        sb.Append(" AND DocumentType Not like '%Stock%Return%'");
                    }
                    else
                    {
                        sb.Append(" And (std.JobID is null Or std.JobID = 0 ) AND DocumentType not like '%Stock%Return%'");
                    }
                }
                if (IsStockReturn == true)
                {
                    sb.Append(" AND DocumentType like '%Stock%Return%'");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetConsumptionconsolidated, sb.ToString()), sqlParms);

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

        public int Get_TransactionHeaderID(int companyID, int periodID, int documentNo, int documentTypeID)
        {
            int gl_HeaderID;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[2] = new SqlParameter("@companyid", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetGL_TransHeaderID = @" SELECT StockTransactionID FROM INV_StockTransactionHeader 
Where PeriodID = @PeriodID and CompanyID = @CompanyID and DocumentTypeID = @DocumentTypeID and DocumentNo = @DocumentNo and status = 1 ";

                gl_HeaderID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetGL_TransHeaderID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return gl_HeaderID;
        }

        public bool UpdateTransactionHeaderAndDetailReturn(List<INV_StockTransactionDetail> colTranDetAdd, List<INV_StockTransactionDetail> colTranDetEdit, List<INV_StockTransactionDetail> colTranDetDelete, GL_TransactionHeader th, List<GL_TransactionDetail> colGLTranDetEdit, SET_DocumentApprove da, DataTable newdt)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransactionReturn");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (INV_StockTransactionDetail stdAdd in colTranDetAdd)
                {
                    stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    stdAdd.Insert(sqltrans);

                }


                foreach (INV_StockTransactionDetail stdUpdate in colTranDetEdit)
                {
                    stdUpdate.UpdateDataByKey(sqltrans);

                }


                foreach (INV_StockTransactionDetail stdDelete in colTranDetDelete)
                {
                    INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);
                }
                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null)
                {
                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        #region Insert In GL_TransactionHeader

                        th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th.Insert(sqltrans);

                        #endregion

                        #region Insert In GL_TransactionDetail


                        decimal AmountDebit = Math.Round(colGLTranDetEdit.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colGLTranDetEdit.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                        foreach (GL_TransactionDetail td in colGLTranDetEdit)
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
                        da.Insert(sqltrans);
                    }

                    #endregion
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransactionRetun");
            }

            return result;
        }


        public static bool UpdateFIFOBySETToNull(int TransactionDetailID, int CompanyID, int PeriodID, bool? status)
        {
            bool result = false;
            try
            {


                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                if (status.HasValue)
                    sqlparams[3] = new SqlParameter("@IsIssued", status.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsIssued", DBNull.Value);


                string Query = @"update inv_stocktransactiondetail set isissued=@isissued , IssuedQuantity = NULL
                                    where TransactionDetailID = @TransactionDetailID 
                                            and CompanyID =@CompanyID 
                                            and PeriodID  = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public static bool UpdateFIFOByTransactionID(int TransactionDetailID, decimal? issuedQuantity, int CompanyID, int PeriodID, bool? status)
        {
            bool result = false;
            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);

                if (status.HasValue)
                    sqlparams[3] = new SqlParameter("@IsIssued", status.Value);
                else
                    sqlparams[3] = new SqlParameter("@IsIssued", DBNull.Value);

                if (issuedQuantity.HasValue)
                    sqlparams[4] = new SqlParameter("@issuedQuantity", issuedQuantity.Value);
                else
                    sqlparams[4] = new SqlParameter("@issuedQuantity", DBNull.Value);



                string Query = @"update inv_stocktransactiondetail set isissued=@isissued , issuedquantity = @issuedQuantity
                                    where TransactionDetailID = @TransactionDetailID 
                                            and CompanyID =@CompanyID 
                                            and PeriodID  = @PeriodID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }

        public static bool UpdateStockAverageRate(int TransactionDetailID, decimal? AverageRate)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[1] = new SqlParameter("@AverageRate", AverageRate);



                string Query = @"update inv_stocktransactiondetail set AverageRate=@AverageRate
                                    where TransactionDetailID = @TransactionDetailID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }


        public bool InsertSubsituteitemsDetails(INV_StockTransactionHeader sth_Subsitute = null, List<INV_StockTransactionDetail> colStockTranDetail_Subsitute = null
            , GL_TransactionHeader th_Subsitute = null, List<GL_TransactionDetail> colTranDetail_Subsitute = null
            , INV_StockTransactionHeader sth_Subsitute_Out = null, List<INV_StockTransactionDetail> colStockTranDetail_Subsitute_Out = null
            , GL_TransactionHeader th_Subsitute_Out = null, List<GL_TransactionDetail> colTranDetail_Subsitute_Out = null, int CurrentFiscalID = 0
            )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");

            try
            {

                #region Subsitute tems in/out

                #region For Adjustment IN
                if (sth_Subsitute != null)
                {
                    // Stock Header
                    sth_Subsitute.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_Subsitute.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans, CurrentFiscalID);
                    sth_Subsitute.FiscalID = CurrentFiscalID;
                    sth_Subsitute.Insert(sqltrans);

                    //Stock Details
                    foreach (INV_StockTransactionDetail std_Sub in colStockTranDetail_Subsitute)
                    {

                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        #region Prior Cost

                        DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                            std_Sub.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        #endregion

                        #region New Cost

                        DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        {
                            decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std_Sub.Amount;
                            decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std_Sub.TransactionQuantity);

                            if (transactionqty != 0)
                                std_Sub.NewCost = Convert.ToDouble(amount / transactionqty);
                        }

                        #endregion

                        #region Estimated PO Item Price

                        DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                            std_Sub.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        #endregion

                        #endregion


                        std_Sub.StockTransactionID = sth_Subsitute.StockTransactionID;
                        std_Sub.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        std_Sub.Comments = "by GRN (Substitute item)";
                        std_Sub.SourceHeaderID = this.StockTransactionID;
                        std_Sub.SourceDocumentTypeID = this.DocumentTypeID;
                        std_Sub.SourceCode = this.DocumentNo.ToString();
                        std_Sub.SourceLineID = null;
                        std_Sub.FiscalID = CurrentFiscalID;
                        std_Sub.Insert(sqltrans);


                    }

                    // GL Detail
                    #region Insert In GL_Transaction Header Subsitute


                    #region Insert In GL_TransactionHeader

                    th_Subsitute.DocumentNo = sth_Subsitute.DocumentNo;

                    th_Subsitute.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th_Subsitute.FiscalID = CurrentFiscalID;
                    th_Subsitute.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail


                    decimal AmountDebit = Math.Round(colTranDetail_Subsitute.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail_Subsitute.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td_Sub in colTranDetail_Subsitute)
                    {
                        td_Sub.TransactionID = th_Subsitute.TransactionID;
                        td_Sub.SourceHeaderID = sth_Subsitute.StockTransactionID;
                        td_Sub.FiscalID = CurrentFiscalID;
                        td_Sub.Insert(sqltrans);
                    }

                    #endregion


                    #endregion


                }

                #endregion


                #region For Adjustment OUT
                if (sth_Subsitute_Out != null)
                {
                    // Stock Header
                    sth_Subsitute_Out.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(this.CompanyID, this.PeriodID, sqltrans);
                    sth_Subsitute_Out.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth_Subsitute_Out.DocumentTypeID, this.CompanyID, this.PeriodID, sqltrans, CurrentFiscalID);
                    sth_Subsitute_Out.FiscalID = CurrentFiscalID;
                    sth_Subsitute_Out.Insert(sqltrans);

                    //Stock Details
                    foreach (INV_StockTransactionDetail std_Sub in colStockTranDetail_Subsitute_Out)
                    {

                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        #region Prior Cost

                        DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                            std_Sub.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        #endregion

                        #region New Cost

                        DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        {
                            decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std_Sub.Amount;
                            decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std_Sub.TransactionQuantity);

                            if (transactionqty != 0)
                                std_Sub.NewCost = Convert.ToDouble(amount / transactionqty);
                        }

                        #endregion

                        #region Estimated PO Item Price

                        DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std_Sub.ItemID), CompanyID, PeriodID, sqltrans);

                        if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                            std_Sub.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        #endregion

                        #endregion


                        std_Sub.StockTransactionID = sth_Subsitute_Out.StockTransactionID;
                        std_Sub.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                        std_Sub.Comments = "by GRN (Substitute item)";
                        std_Sub.SourceHeaderID = this.StockTransactionID;
                        std_Sub.SourceDocumentTypeID = this.DocumentTypeID;
                        std_Sub.SourceCode = this.DocumentNo.ToString();
                        std_Sub.SourceLineID = null;
                        std_Sub.FiscalID = CurrentFiscalID;
                        std_Sub.Insert(sqltrans);


                    }

                    // GL Detail
                    #region Insert In GL_Transaction Header Subsitute


                    #region Insert In GL_TransactionHeader

                    th_Subsitute_Out.DocumentNo = sth_Subsitute_Out.DocumentNo;
                    th_Subsitute_Out.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th_Subsitute_Out.FiscalID = CurrentFiscalID;
                    th_Subsitute_Out.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail_Subsitute_Out.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail_Subsitute_Out.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td_Sub in colTranDetail_Subsitute_Out)
                    {
                        td_Sub.TransactionID = th_Subsitute_Out.TransactionID;
                        td_Sub.SourceHeaderID = sth_Subsitute_Out.StockTransactionID;
                        td_Sub.FiscalID = CurrentFiscalID;
                        td_Sub.Insert(sqltrans);
                    }

                    #endregion


                    #endregion


                }

                #endregion

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }

        public static DataRow GetDataByKeyWithOtherTables_AlImdad(int stockTransactionID, int periodID, int companyID)
        {
            DataRow drToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Select_By_PrimaryKey_WithOtherTables_ALImdad + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }

        public bool InsertStockTransactionHederAndDetail_AlImdad(List<INV_StockTransactionDetail> colStockTranDetail, GL_TransactionHeader th,
           List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, GL_TransactionHeader thBlendFormulation = null,
           INV_StockTransactionHeader sthBlendFomulation = null, List<SAL_DispatchVehicleInformation> ColVehicleStatus = null, Nullable<bool> FG = false,

           int CurrentFiscalID = 0,
            INV_StockTransactionHeader sth_RecipeHeader = null, List<INV_StockTransactionDetail> colRecipeTranDetail = null
            //, GL_TransactionHeader th_Subsitute = null, List<GL_TransactionDetail> colTranDetail_Subsitute = null
            //, INV_StockTransactionHeader sth_Subsitute_Out = null, List<INV_StockTransactionDetail> colStockTranDetail_Subsitute_Out = null
            //, GL_TransactionHeader th_Subsitute_Out = null, List<GL_TransactionDetail> colTranDetail_Subsitute_Out = null
           )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");



            try
            {
                int sthBlendID = 0;
                int sthBlendDocumentNo = 0;

                if (!FG.Value)
                    this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {
                    if (dr_documentType != null)
                    {
                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        //#region Prior Cost

                        //DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                        //    std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        //#endregion

                        //#region New Cost

                        //DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        //{
                        //    decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                        //    decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                        //    if (transactionqty != 0)
                        //        std.NewCost = Convert.ToDouble(amount / transactionqty);
                        //}

                        //#endregion

                        //#region Estimated PO Item Price

                        //DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                        //    std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        //#endregion

                        #endregion
                    }

                    std.StockTransactionID = this.StockTransactionID;

                    std.FiscalID = CurrentFiscalID;

                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);


                }

                if (sth_RecipeHeader != null)
                {

                    sth_RecipeHeader.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    sth_RecipeHeader.Insert(sqltrans);

                    if (colRecipeTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail std in colRecipeTranDetail)
                        {
                            std.StockTransactionID = sth_RecipeHeader.StockTransactionID;

                            std.FiscalID = CurrentFiscalID;

                            std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                            std.SourceHeaderID = this.StockTransactionID;
                            std.Comments = this.Comments;
                            std.Insert(sqltrans);
                        }

                    }
                }

                // Update STD for comments and Isdone

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = f_DocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    //if (thBlendFormulation != null)
                    //{
                    //    thBlendFormulation.DocumentNo = sthBlendDocumentNo;
                    //    thBlendFormulation.TransactionID = th.TransactionID + 1;
                    //    thBlendFormulation.Insert(sqltrans);
                    //}


                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;

                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.f_DocumentNo.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }

        public bool InsertStockTransactionHederAndDetail_Union(List<INV_StockTransactionDetail> colStockTranDetail, List<StopagesDetail> colStopagesDetail, List<EmployeeDetail> colShiftDetail, GL_TransactionHeader th,
          List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, GL_TransactionHeader thBlendFormulation = null,
          INV_StockTransactionHeader sthBlendFomulation = null, List<SAL_DispatchVehicleInformation> ColVehicleStatus = null, Nullable<bool> FG = false,

          int CurrentFiscalID = 0,
           INV_StockTransactionHeader sth_RecipeHeader = null, List<INV_StockTransactionDetail> colRecipeTranDetail = null
            //, GL_TransactionHeader th_Subsitute = null, List<GL_TransactionDetail> colTranDetail_Subsitute = null
            //, INV_StockTransactionHeader sth_Subsitute_Out = null, List<INV_StockTransactionDetail> colStockTranDetail_Subsitute_Out = null
            //, GL_TransactionHeader th_Subsitute_Out = null, List<GL_TransactionDetail> colTranDetail_Subsitute_Out = null
          )
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertStockTransaction");



            try
            {
                int sthBlendID = 0;
                int sthBlendDocumentNo = 0;

                if (!FG.Value)
                    this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);

                this.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.DocumentTypeID, PeriodID, null, sqltrans);

                foreach (INV_StockTransactionDetail std in colStockTranDetail)
                {
                    if (dr_documentType != null)
                    {
                        #region Update Prior Cost , New Cost And Estimated PO Item Price

                        //#region Prior Cost

                        //DataTable dt_LastTransactionCost = INV_StockTransactionHeader.Get_LastTransactionCostAgainstItem(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_LastTransactionCost != null && dt_LastTransactionCost.Rows.Count > 0)
                        //    std.PriorCost = Convert.ToDouble(dt_LastTransactionCost.Rows[0]["TransactionCost"].ToString());

                        //#endregion

                        //#region New Cost

                        //DataTable dt_GetAmountTransactionQty = INV_StockTransactionHeader.Get_GetSumAmountAndSumTransactionQuantity(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_GetAmountTransactionQty != null && dt_GetAmountTransactionQty.Rows.Count > 0)
                        //{
                        //    decimal amount = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["Amount"].ToString()) + std.Amount;
                        //    decimal transactionqty = Convert.ToDecimal(dt_GetAmountTransactionQty.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(std.TransactionQuantity);

                        //    if (transactionqty != 0)
                        //        std.NewCost = Convert.ToDouble(amount / transactionqty);
                        //}

                        //#endregion

                        //#region Estimated PO Item Price

                        //DataTable dt_estimatedpoitemprice = AP_PurchaseOrderDetail.Get_LastUnitPrice(Convert.ToInt64(std.ItemID), CompanyID, PeriodID, sqltrans);

                        //if (dt_estimatedpoitemprice != null && dt_estimatedpoitemprice.Rows.Count > 0)
                        //    std.EstimatedPOItemPrice = Convert.ToDouble(dt_estimatedpoitemprice.Rows[0]["UnitPrice"].ToString());

                        //#endregion

                        #endregion
                    }

                    std.StockTransactionID = this.StockTransactionID;

                    std.FiscalID = CurrentFiscalID;

                    std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                    std.Comments = this.Comments;
                    std.Insert(sqltrans);


                }

                foreach (StopagesDetail stod in colStopagesDetail)
                {

                    stod.StockTransactionID = this.StockTransactionID;
                    stod.FiscalID = CurrentFiscalID;
                    stod.StopagesDetailID = StopagesDetail.GetNewStopagesDetailID(sqltrans);
                    stod.Insert(sqltrans);


                }

                foreach (EmployeeDetail stod in colShiftDetail)
                {

                    stod.StockTransactionID = this.StockTransactionID;
                    stod.FiscalID = CurrentFiscalID;
                    stod.EmployeeDetailID = EmployeeDetail.GetNewEmployeeDetailID(sqltrans);
                    stod.Insert(sqltrans);


                }


                if (sth_RecipeHeader != null)
                {

                    sth_RecipeHeader.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);

                    sth_RecipeHeader.Insert(sqltrans);

                    if (colRecipeTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail std in colRecipeTranDetail)
                        {
                            std.StockTransactionID = sth_RecipeHeader.StockTransactionID;

                            std.FiscalID = CurrentFiscalID;

                            std.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);

                            std.SourceHeaderID = this.StockTransactionID;
                            std.Comments = this.Comments;
                            std.Insert(sqltrans);
                        }

                    }
                }

                // Update STD for comments and Isdone

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = f_DocumentNo;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    //if (thBlendFormulation != null)
                    //{
                    //    thBlendFormulation.DocumentNo = sthBlendDocumentNo;
                    //    thBlendFormulation.TransactionID = th.TransactionID + 1;
                    //    thBlendFormulation.Insert(sqltrans);
                    //}


                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.StockTransactionID;

                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.f_DocumentNo.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertStockTransaction");
            }

            return result;
        }



        public static DataSet ItemTransferRreport_AlImdad(int CompanyID, int PeriodID, int? DocumentNo, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int FromLoc, int ToLoc, int Catagoery, int ItemID,
            short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataSet ds;
            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentType", DocumentType);
                sqlparams[5] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                StringBuilder sb = new StringBuilder();

                if (FromLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.LocationID = " + FromLoc);
                if (ToLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.ToLocationID = " + ToLoc);

                if (Catagoery > 0)
                    Filters.Append(" and inv_Set_Category.CategoryID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);


                if (documentGroupID.HasValue)
                    Filters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DocumentNo != null)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentNo= @DocumentNo");

                if (DocumentType > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = @DocumentType ");


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_TransferINReport + Filters, sb.ToString()), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetDataForProductionOnEdit(int stockTransactionID, int periodID, int companyID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetProductionOnEdit + sb.ToString(), sqlparams);

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


        public bool UpdateStockTransactionHederAndDetail_AlImdad(List<INV_StockTransactionDetail> colAddStockTranDetail,
           List<INV_StockTransactionDetail> colUpdateStockTranDetail, List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
           List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da,
           int CurrentFiscalID = 0, INV_StockTransactionHeader RecipeHEader = null, List<INV_StockTransactionDetail> colRecipeTranAddDetail = null, List<INV_StockTransactionDetail> colRecipeTranUpdateDetail = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                {
                    this.UpdateDataByKey(sqltrans);

                    if (colAddStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdAdd in colAddStockTranDetail)
                        {
                            stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                            stdAdd.FiscalID = CurrentFiscalID;
                            stdAdd.Insert(sqltrans);

                        }
                    }

                    if (colUpdateStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                        {
                            stdUpdate.UpdateDataByKey(sqltrans);
                        }
                    }

                    if (colDeleteStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                        {
                            INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

                        }
                    }
                    if (RecipeHEader != null)
                    {
                        RecipeHEader.UpdateDataByKey(sqltrans);

                        if (colRecipeTranAddDetail != null)
                        {
                            foreach (INV_StockTransactionDetail stdUpdate in colRecipeTranAddDetail)
                            {
                                stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;

                                stdUpdate.FiscalID = CurrentFiscalID;

                                stdUpdate.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                                stdUpdate.SourceHeaderID = this.StockTransactionID;
                                stdUpdate.Comments = this.Comments;
                                stdUpdate.Insert(sqltrans);
                            }
                        }

                        if (colRecipeTranUpdateDetail != null)
                        {
                            foreach (INV_StockTransactionDetail stdUpdate in colRecipeTranUpdateDetail)
                            {
                                stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;
                                stdUpdate.UpdateDataByKey(sqltrans);
                            }
                        }
                    }
                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                    if (th != null)
                    {
                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            #region Insert In GL_TransactionHeader

                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th.FiscalID = CurrentFiscalID;
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
                                td.FiscalID = CurrentFiscalID;
                                td.Insert(sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion

                    #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    {
                        da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
                    }

                    #endregion
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public bool UpdateStockTransactionHederAndDetail_Union(List<INV_StockTransactionDetail> colAddStockTranDetail,
          List<INV_StockTransactionDetail> colUpdateStockTranDetail, List<INV_StockTransactionDetail> colDeleteStockTranDetail, GL_TransactionHeader th,
          List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da,
          int CurrentFiscalID = 0, INV_StockTransactionHeader RecipeHEader = null, List<INV_StockTransactionDetail> colRecipeTranAddDetail = null, List<INV_StockTransactionDetail> colRecipeTranUpdateDetail = null
            , List<StopagesDetail> colStopagesAddDetail = null, List<StopagesDetail> colStopagesUpdateDetail = null, List<EmployeeDetail> colEmployeeAddDetail = null, List<EmployeeDetail> colEmployeeUpdateDetail = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                {
                    this.UpdateDataByKey(sqltrans);

                    if (colAddStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdAdd in colAddStockTranDetail)
                        {
                            stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                            stdAdd.FiscalID = CurrentFiscalID;
                            stdAdd.Insert(sqltrans);

                        }
                    }

                    if (colUpdateStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                        {
                            stdUpdate.UpdateDataByKey(sqltrans);
                        }
                    }

                    if (colDeleteStockTranDetail != null)
                    {
                        foreach (INV_StockTransactionDetail stdDelete in colDeleteStockTranDetail)
                        {
                            INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID);

                        }
                    }
                    if (RecipeHEader != null)
                    {
                        RecipeHEader.UpdateDataByKey(sqltrans);

                        if (colRecipeTranAddDetail != null)
                        {
                            foreach (INV_StockTransactionDetail stdUpdate in colRecipeTranAddDetail)
                            {
                                stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;

                                stdUpdate.FiscalID = CurrentFiscalID;

                                stdUpdate.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(sqltrans);
                                stdUpdate.SourceHeaderID = this.StockTransactionID;
                                stdUpdate.Comments = this.Comments;
                                stdUpdate.Insert(sqltrans);
                            }
                        }

                        if (colRecipeTranUpdateDetail != null)
                        {
                            foreach (INV_StockTransactionDetail stdUpdate in colRecipeTranUpdateDetail)
                            {
                                stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;
                                stdUpdate.SourceHeaderID = this.StockTransactionID;
                                stdUpdate.UpdateDataByKey(sqltrans);
                            }
                        }


                        if (colStopagesAddDetail != null)
                        {
                            foreach (StopagesDetail stoAdd in colStopagesAddDetail)
                            {

                                //stoAdd.StockTransactionID = this.StockTransactionID;
                                //stoAdd.FiscalID = CurrentFiscalID;
                                //stoAdd.StopagesDetailID = StopagesDetail.GetNewStopagesDetailID(sqltrans);
                                //stoAdd.Insert(sqltrans);

                            }
                        }

                        if (colStopagesUpdateDetail != null)
                        {
                            //foreach (StopagesDetail stoUpdate in colStopagesUpdateDetail)
                            //{
                            //    stoUpdate.StockTransactionID = this.StockTransactionID;
                            //    stoUpdate.UpdateDataByKey(sqltrans);
                            //}
                        }


                        if (colEmployeeAddDetail != null)
                        {
                            foreach (EmployeeDetail empAdd in colEmployeeAddDetail)
                            {

                                empAdd.StockTransactionID = this.StockTransactionID;
                                empAdd.FiscalID = CurrentFiscalID;
                                empAdd.EmployeeDetailID = EmployeeDetail.GetNewEmployeeDetailID(sqltrans);
                                empAdd.Insert(sqltrans);

                            }
                        }

                        if (colEmployeeUpdateDetail != null)
                        {
                            foreach (EmployeeDetail empUpdate in colEmployeeUpdateDetail)
                            {
                                empUpdate.StockTransactionID = this.StockTransactionID;
                                empUpdate.UpdateDataByKey(sqltrans);
                            }
                        }

                        //if (colStopagesUpdateDetail != null)
                        //{
                        //    foreach (StopagesDetail stdUpdate in colStopagesUpdateDetail)
                        //    {
                        //        stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;
                        //       // stdUpdate.SourceHeaderID = this.StockTransactionID;
                        //        stdUpdate.UpdateDataByKey(sqltrans);
                        //    }
                        //}

                        //if (colEmployeeUpdateDetail != null)
                        //{
                        //    foreach (EmployeeDetail stdUpdate in colEmployeeUpdateDetail)
                        //    {
                        //        stdUpdate.StockTransactionID = RecipeHEader.StockTransactionID;
                        //        // stdUpdate.SourceHeaderID = this.StockTransactionID;
                        //        stdUpdate.UpdateDataByKey(sqltrans);
                        //    }
                        //}
                    }
                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                    if (th != null)
                    {
                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            #region Insert In GL_TransactionHeader

                            th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th.FiscalID = CurrentFiscalID;
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
                                td.FiscalID = CurrentFiscalID;
                                td.Insert(sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion

                    #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    {
                        da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
                    }

                    #endregion
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public static bool IsTransactionMadeAfterDoc(int companyID, int periodID, int stockHeaderID)
        {
            bool isTransactionMade = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

                string qry_IsInvoiceMade = @" Select     *  from INV_StockTransactionHeader
INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID=INV_StockTransactionHeader.DocumentTypeID

AND SET_DocumentType.TransactionImpact<>'No Impact'

 Where INV_StockTransactionHeader.Status=1 AND INV_StockTransactionHeader.CompanyID = @CompanyID AND
 INV_StockTransactionHeader.DocumentDate > ( Select DocumentDate From INV_StockTransactionHeader currentDocument Where StockTransactionID = @StockHeaderID and CompanyID = @CompanyID and Status = 1) ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_IsInvoiceMade, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    isTransactionMade = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return isTransactionMade;
        }



        public static DataTable IssuanceReport(int CompanyID, int PeriodID, int? DocumentNo, DateTime? DateFrom, DateTime? DateTo, int DocumentTypeID, Nullable<int> FromLocation = null, Nullable<int> ToLocation = null, int CurrentFiscalID = 0, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (DateFrom != null && DateTo != null)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].DocumentDate between @FromDate AND @ToDate";


                AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].DocumentType like  '%Issuance Document%' ";
                if (DocumentNo.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[DocumentNo] = " + DocumentNo;
                if (FromLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[LocationID] = " + FromLocation;
                if (ToLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[ToLocationID] = " + ToLocation;
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_IssuanceReport + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_IssuanceReport + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }
        public static DataTable DispatchDocumentReport(int CompanyID, int PeriodID, int? DocumentNo, DateTime? DateFrom, DateTime? DateTo, int DocumentTypeID, Nullable<int> FromLocation = null, Nullable<int> ToLocation = null, int CurrentFiscalID = 0, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (DateFrom != null && DateTo != null)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].DocumentDate between @FromDate AND @ToDate";

                if (DocumentTypeID > 0)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentType]  like '%Dispatch Document%' ";
                if (DocumentNo.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[DocumentNo] = " + DocumentNo;
                if (FromLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[LocationID] = " + FromLocation;
                if (ToLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[ToLocationID] = " + ToLocation;
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DispatchDocument + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DispatchDocument + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }
        public static DataTable SalesDocumentReport(int CompanyID, int PeriodID, int? DocumentNo, DateTime? DateFrom, DateTime? DateTo, int DocumentTypeID, Nullable<int> FromLocation = null, Nullable<int> ToLocation = null, int CurrentFiscalID = 0, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlParms[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (DateFrom != null && DateTo != null)
                    AddtionalParameters = AddtionalParameters + " AND [SAL_OrderHeader].ContractDate between @FromDate AND @ToDate";

                if (DocumentTypeID > 0)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentType]  like '%Sale Document%'";
                if (DocumentNo.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [SAL_OrderHeader].[ContractNo] = " + DocumentNo;
                if (FromLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[LocationID] = " + FromLocation;
                if (ToLocation.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [INV_StockTransactionHeader].[ToLocationID] = " + ToLocation;
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SalesDocumentReport + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SalesDocumentReport + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }
        public static DataTable GRNDocumentReport(int CompanyID, int PeriodID, int? DocumentNo, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int FromLoc, int ToLoc, int Catagoery, int ItemID,
          short? documentGroupID = null, int CurrentFiscalID = 0, bool AlImdad = false)
        {
            DataTable dt = null;
            DataSet ds;
            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentType);
                sqlparams[5] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                StringBuilder sb = new StringBuilder();

                if (FromLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.LocationID = " + FromLoc);
                if (ToLoc > 0)
                    Filters.Append(" and INV_StockTransactionHeader.ToLocationID = " + ToLoc);

                if (Catagoery > 0)
                    Filters.Append(" and inv_Set_Category.CategoryID = " + Catagoery);

                if (ItemID > 0)
                    Filters.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);


                if (documentGroupID.HasValue)
                    Filters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (DocumentNo != null)
                //    sb.Append("AND INV_StockTransactionHeader.DocumentNo= @DocumentNo");

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_GetGRNDocumentData + Filters, sb.ToString()), sqlparams);
                if (ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataSet FinishGoodsDocumentReport(int CompanyID, int PeriodID, int? DocumentNo, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, int DocumentType, int FromLoc, int ToLoc, int Catagoery, int ItemID,
      short? documentGroupID = null, int CurrentFiscalID = 0, bool AlImdad = false, int ShiftID = 0, int RackID = 0, int SerialNo = 0, Nullable<int> BatchID = null, string grainsize = "", string paking = "")
        {
            DataTable dt = null;
            DataSet ds;
            StringBuilder Filters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentType);
                sqlparams[5] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                StringBuilder sb = new StringBuilder();

                if (FromDate.HasValue && ToDate.HasValue)
                {
                    sb.Append(" And cast((FORMAT (INV_StockTransactionHeader.DocumentDate, 'yyyy-MM-dd') + ' ' + FORMAT (INV_StockTransactionHeader.CreateDate, 'hh:mm:ss')) as datetime) between '" + DateUtil.ConvertToString(FromDate.Value, true) + " " + "00:00:00" + " ' and '"
                        + DateUtil.ConvertToString(ToDate.Value, true) + " " + "23:59:59" + " ' ");
                }


                if (FromLoc > 0)
                    sb.Append(" and INV_StockTransactionDetail.LocationID = " + FromLoc);

                if (Catagoery > 0)
                    sb.Append(" and inv_Set_Category.CategoryID = " + Catagoery);

                if (ItemID > 0)
                    sb.Append(" and INV_StockTransactionDetail.ItemID = " + ItemID);


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ShiftID > 0)
                    sb.Append(" and INV_StockTransactionDetail.ShiftID = " + ShiftID);

                if (RackID > 0)
                    sb.Append(" and INV_StockTransactionDetail.RackID = " + RackID);

                if (SerialNo > 0)
                    sb.Append(" and " + SerialNo + " between INV_StockTransactionDetail.SerialNoFrom and INV_StockTransactionDetail.SerialNoTO ");

                if (BatchID.HasValue)
                    sb.Append(" and INV_StockTransactionHeader.BatchID =  " + BatchID);
                if (grainsize != "")
                    sb.Append(" and INV_StockTransactionDetail.GrainSizeID =  " + grainsize);
                if (paking != "")
                    sb.Append(" and INV_StockTransactionDetail.PackTypeID =  " + paking);

                //if (DocumentNo != null)
                //    sb.Append("AND INV_StockTransactionHeader.DocumentNo= @DocumentNo");

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_FinishGoodsDocumentReport, sb.ToString()), sqlparams);
                //if (ds.Tables.Count > 0)
                //    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetAllGrnByDate(Nullable<int> companyID, Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo, string where = "", SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                const string Query_GetAllGRNByDate = @" Select INV_StockTransactionHeader.StockTransactionID,INV_StockTransactionHeader.DocumentTypeID,DocumentDate,DocumentNo,INV_StockTransactionDetail.ItemID,ISNull(TransactionQuantity,0)TransactionQuantity
,InventoryItemStatusCode,SubstituteCode,'' as IsIssued,Convert(decimal,'0') as TotalBalance

 from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and SET_DocumentType.TransactionImpact = 'Positive'
--and SET_DocumentType.DocumentType like '%GRN%' and (DocumentType like '%Import%' or DocumentType like '%local%')
and (SET_DocumentType.DocumentType like '%GRN%' and (DocumentType like '%Import%' or DocumentType like '%local%') or SET_DocumentType.DocumentType like '%opening%Import')
inner join INV_ITem on INV_ITem.ItemID = INV_StockTransactionDetail.ITemID

--where INV_StockTransactionDetail.ItemID in (4245,5103)
where INV_StockTransactionDetail.Status = 1
and INV_StockTransactionHeader.Status = 1
and INV_StockTransactionHeader.StatusCode  = 1
and convert(Date,INV_StockTransactionHeader.DocumentDate) <= Convert(date,@ToDate)
and INV_StockTransactionDetail.TransactionQuantity > 0
--and INV_StockTransactionDetail.ItemID in (4469,5081)
{0}


order by INV_StockTransactionHeader.DocumentDate desc";

                string AddtionalParameters = where;

                SqlParameter[] sqlParms = new SqlParameter[3];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAllGRNByDate, AddtionalParameters.ToString()), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetAllGRNByDate, AddtionalParameters.ToString()), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static decimal GetClosingBalance(Nullable<int> companyID, Nullable<DateTime> DateFrom, Nullable<DateTime> DateTo, int ItemID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            decimal Balance = 0;
            try
            {
                DataSet ds = null;

                const string Query_GetClosingBalance = @" Select ISNULL((SUM(Positve) - SUM(Negative)),0) TotalBalance from
(

Select case when SET_DocumentType.TransactionImpact = 'Positive' then (abs(TransactionQuantity)) else 0 end as Positve,DocumentDate,

 case when SET_DocumentType.TransactionImpact = 'Negative' then (abs(TransactionQuantity)) else 0 end as Negative from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
--and SET_DocumentType.TransactionImpact = 'Positive'

where ItemID = @ItemID
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionHeader.Status = 1
and INV_StockTransactionHeader.StatusCode  = 1
and Convert(Date,INV_StockTransactionHeader.DocumentDate) <= Convert(date,@ToDate)
) as tab
";

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[2] = new SqlParameter("@ToDate", DateTo);
                sqlParms[3] = new SqlParameter("@ItemID", ItemID);

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetClosingBalance + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetClosingBalance + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                {
                    dtresult = ds.Tables[0];
                    if (dtresult.Rows.Count > 0)
                        Balance = decimal.Parse(dtresult.Rows[0]["TotalBalance"].ToString());
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return Balance;
        }

        public static DataTable GetAlLCostingData(Nullable<int> companyID, string StockID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                const string Query_GetAlLCostingData = @" 
";

                string AddtionalParameters = string.Empty;

                if (StockID != "")
                    AddtionalParameters = " and INV_StockTransactionHeader.StocktransactionID in (" + StockID + ")";

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetAlLCostingData + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetAlLCostingData + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    dtresult = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }
        public static DataTable Select_StockTranferOutAl_imdad_Lookup(int companyID, int periodID, string filter = "", string Where = "")
        {
            DataTable dt;
            string query = "";
            try
            {
                if (filter == "documentout")
                {
                    query = @"  Select INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, Set_DocumentType.DocumentType 
from INV_StockTransactionHeader 
  INNER JOIN INV_stockTransactionDetail ON INV_stockTransactionDetail.StockTransactionID = INV_stockTransactionHeader.StockTransactionID 
        AND INV_stockTransactionHeader.CompanyID = INV_stockTransactionDetail.CompanyID AND INV_stockTransactionHeader.PeriodID = INV_stockTransactionDetail.PeriodID 
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
And (Set_DocumentType.DocumentType Like '%issuance%')

	LEFT JOIN (  Select Sum(abs(TransactionQuantity)) as  TransactionQuantity,INV_StockTransactionDetail.SourceCode,INV_StockTransactionDetail.SourceDocumentTypeID,INV_StockTransactionDetail.SourceHeaderID,INV_StockTransactionDetail.SourceLineID From INV_StockTransactionDetail 
	Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
	and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID and INV_StockTransactionHeader.Status = 1 
	Where INV_StockTransactionDetail.Status=1
	group by SourceCode,SourceDocumentTypeID,SourceHeaderID,SourceLineID) TRansferIN  ON TRansferIN.SourceHeaderID=INV_stockTransactionDetail.StockTransactionID 
		AND TRansferIN.SourceLineID=INV_stockTransactionDetail.TransactionDetailID
	AND TRansferIN.SourceCode=INV_stockTransactionHeader.DocumentNo
	AND TRansferIN.SourceDocumentTypeID=INV_stockTransactionHeader.DocumentTypeID
	Where INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID And INV_StockTransactionHeader.Status = 1 
    And INV_StockTransactionHeader.StatusCode = 1 AND (INV_StockTransactionHeader.IsTransferred IS NULL OR INV_StockTransactionHeader.IsTransferred = 0)
 AND (abs(INV_stockTransactionDetail.TransactionQuantity)- ISNULL(TRansferIN.TransactionQuantity,0))  > 0
 ";
                }
                else
                    //  query = Query_Select_StockTransferOut_Lookup + filter;
                    query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_StockTransferOut_Lookup_AlImdad + filter + Where;
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllDocumentsForApproval(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
    int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0)
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
                    sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    string query = AllDocumentsForApproval + strWhere.ToString();

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, AllDocumentsForApproval + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetRequisitionForTracking(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> DateTo = null, int? RequisitionNoFrom = null, int? RequisitionNoTo = null, string UserName = null, string status = null, int DetailID = 0, Nullable<int> FinalStatus = null, SqlTransaction objTransaction = null)
        {
            DataTable dt;
            try
            {
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sb.Append("Where 1=1 ");
                if (fromDate.HasValue && DateTo.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlparams[3] = new SqlParameter("@TODate", DateTo.Value);
                    sb.Append("AND (FinalFintab.DocumentDate Between @FromDate AND @TODate) ");
                }
                if (fromDate.HasValue && (!DateTo.HasValue))
                {
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    sb.Append("AND (FinalFintab.DocumentDate >= @FromDate ) ");
                }
                if (DateTo.HasValue && (!fromDate.HasValue))
                {
                    sqlparams[3] = new SqlParameter("@TODate", DateTo.Value);
                    sb.Append("AND (FinalFintab.DocumentDate <= @TODate) ");
                }

                if (RequisitionNoFrom.HasValue && RequisitionNoTo.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@DocumentNoFrom", RequisitionNoFrom.Value);
                    sqlparams[5] = new SqlParameter("@DocumentNoTo", RequisitionNoTo.Value);
                    sb.Append("AND (FinalFintab.RequisitionNumber Between @DocumentNoFrom AND @DocumentNoTo) ");
                }
                if (RequisitionNoFrom.HasValue && (!RequisitionNoTo.HasValue))
                {
                    sqlparams[4] = new SqlParameter("@DocumentNoFrom", RequisitionNoFrom.Value);
                    sb.Append("AND (FinalFintab.RequisitionNumber >= @DocumentNoFrom ) ");
                }
                if ((!RequisitionNoFrom.HasValue) && RequisitionNoTo.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@DocumentNoTo", RequisitionNoTo.Value);
                    sb.Append("AND (FinalFintab.RequisitionNumber <= @DocumentNoTo) ");
                }

                if (UserName.ToString() != string.Empty)
                    sqlparams[6] = new SqlParameter("@user", UserName);
                else
                    sqlparams[6] = new SqlParameter("@user", DBNull.Value);
                if (status.ToString() != string.Empty && status.ToString() != null)
                {
                    sb.Append("AND (FinalFintab.Status like '%" + status.ToString() + "%')");
                }
                if (FinalStatus.HasValue)
                {
                    if (FinalStatus == 1)
                    {
                        sb.Append("AND (FinalFintab.finalstatus = 'Open')");
                    }
                    else { sb.Append("AND (FinalFintab.finalstatus = 'Close')"); }
                }
                string query = string.Empty;
                if (DetailID == 1)
                {
                    query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetAllRequisitionStatusDetail_Tracking;
                }
                else
                {
                    query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetAllRequisitionStatus_Tracking;
                }
                if (objTransaction == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams).Tables[0];
                }
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetItemWiseGRN(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
    int ApproveGroupID = 0, int DocumentTypeID = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (CustomerID > 0)
                    sb.Append(" and INV_StockTransactionHeader.BusinessPartnerID = " + CustomerID + "");

                if (ApproveGroupID > 0)
                    sb.Append(" and INV_Item.ApprovalItemGroupID = " + ApproveGroupID + "");

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");



                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectItemWiseGRM, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectItemWiseGRM, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectItemWiseGRM, sb), sqlparams);

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

        public static DataTable GetGRNWiseRejectItems(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
int ApproveGroupID = 0, int DocumentTypeID = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (CustomerID > 0)
                    sb.Append(" and Ap_PurchaseOrderHeader.VendorID = " + CustomerID + "");

                if (ApproveGroupID > 0)
                    sb.Append(" and INV_Item.ApprovalItemGroupID = " + ApproveGroupID + "");

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");



                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectGRNWiseRejectItems, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectGRNWiseRejectItems, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectGRNWiseRejectItems, sb), sqlparams);

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

        public static DataTable GetGRNWisePurchase(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
int ApproveGroupID = 0, int DocumentTypeID = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> PoNo = 0, Nullable<int> GrnNo = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (CustomerID > 0)
                    sb.Append(" and Ap_PurchaseOrderHeader.VendorID = " + CustomerID + "");

                if (ApproveGroupID > 0)
                    sb.Append(" and INV_Item.ApprovalItemGroupID = " + ApproveGroupID + "");

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (PoNo > 0)
                    sb.Append(" and AP_PurchaseOrderHeader.DocumentNumber = " + PoNo);

                if (GrnNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + GrnNo);


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectGRNWisePurchase.ToString(), sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectGRNWisePurchase.ToString(), sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectGRNWisePurchase.ToString(), sb), sqlparams);

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

        public static DataTable GetGRNWisePurchaseSK(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
int ApproveGroupID = 0, int DocumentTypeID = 0, int? DocumentStatus = null, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> PoNo = 0, Nullable<int> GrnNo = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and isnull(INV_StockTransactionHeader.updateDate,INV_StockTransactionHeader.DocumentDate) between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and isnull(INV_StockTransactionHeader.updateDate,INV_StockTransactionHeader.DocumentDate) >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (CustomerID > 0)
                    sb.Append(" and Ap_PurchaseOrderHeader.VendorID = " + CustomerID + "");

                if (ApproveGroupID > 0)
                    sb.Append(" and INV_Item.ApprovalItemGroupID = " + ApproveGroupID + "");

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (PoNo > 0)
                    sb.Append(" and AP_PurchaseOrderHeader.DocumentNumber = " + PoNo);

                if (GrnNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + GrnNo);

                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    sb.Append("AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus);
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectGRNWiseSKPurchase, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectGRNWiseSKPurchase, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectGRNWiseSKPurchase, sb), sqlparams);

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

        public static DataTable GetGRNWisePurchase_HSM(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
int ApproveGroupID = 0, int DocumentTypeID = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> PoNo = 0, Nullable<int> GrnNo = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null, string DocumentStatus = "-1")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (CustomerID > 0)
                    sb.Append(" and Ap_PurchaseOrderHeader.VendorID = " + CustomerID + "");

                if (ApproveGroupID > 0)
                    sb.Append(" and INV_Item.ApprovalItemGroupID = " + ApproveGroupID + "");

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (PoNo > 0)
                    sb.Append(" and AP_PurchaseOrderHeader.DocumentNumber = " + PoNo);

                if (GrnNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + GrnNo);

                if (DocumentStatus != "-1")
                    sb.Append(" and INV_StockTransactionHeader.StatusCode = " + DocumentStatus);
                    

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectGRNWisePurchase_HSM, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectGRNWisePurchase_HSM, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectGRNWisePurchase_HSM, sb), sqlparams);

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

        public static DataTable GetGINWiseTransaction(int companyID, int periodID, int CustomerID = 0, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
int ApproveGroupID = 0, int DocumentTypeID = 0, Nullable<int> locationID = null, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                if (locationID.HasValue)
                    sqlparams[4] = new SqlParameter("@LocationID", locationID.Value);
                else
                    sqlparams[4] = new SqlParameter("@LocationID", DBNull.Value);

                DataSet ds = null;

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentDate between @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate >= @FromDate ");

                }
                else if (toDate.HasValue)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate <= @ToDate");
                }

                if (DocumentTypeID > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");




                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_SelectGINWiseTransaction, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SelectGINWiseTransaction, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectGINWiseTransaction, sb), sqlparams);

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

        public static DataTable GetConsumptionAnalysisReport(string companyID, int periodID, DateTime AsOnDate, string ItemCatalogId = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemCatalogId", ItemCatalogId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@AsOnDate", AsOnDate);

                if (ItemCatalogId != "")
                    sb.Append(" AND INV_Item.ItemCatalogGroupID in " + ItemCatalogId + "");

                companyID = "(" + companyID + ")";


                string Query = string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ConsumptionAnaylsis, sb.ToString());
                Query = Query.ToString().Replace("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public DataTable GetBreakDownAnalysisSheet(int periodID, int companyID, int ShiftID, DateTime datefrom, DateTime dateTo)
        {
            DataTable dtReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                // sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ShiftID", ShiftID);
                sqlparams[3] = new SqlParameter("@datefrom", datefrom);
                sqlparams[4] = new SqlParameter("@dateTo", dateTo);

                if (ShiftID != -1)
                {
                    sb.Append(" " + "and HRMS_Set_Shift.ShiftID = @ShiftID");
                }
                if (datefrom != null && dateTo != null)
                {
                    sb.Append(" " + "and Convert(Date,INV_StockTransactionHeader.DocumentDate) between @datefrom and @dateTo");
                }

                else if (datefrom != null)
                {
                    sb.Append(" " + "and Convert(Date,INV_StockTransactionHeader.DocumentDate) = @datefrom");
                }
                else if (dateTo != null)
                {
                    sb.Append(" " + "and Convert(Date,INV_StockTransactionHeader.DocumentDate) = @dateTo");
                }



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.QueryBreakDownAnalysisSheet + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    //  drToReturn = ds.Tables[0].Rows[0];

                    dtReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtReturn;
        }

        public static DataTable GetFinishGoodsSummaryData(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
            Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> BatchID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                //if (fromDate.HasValue && toDate.HasValue)
                //{
                //    sb.Append(" and StockHeader.DocumentDate between @FromDate AND @ToDate ");
                //}
                //else if (fromDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate >= @FromDate ");

                //}
                //else if (toDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate <= @ToDate");
                //}

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_Item.ItemTypeID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (BatchID.HasValue)
                    sb.Append(" and StockHeader.BatchID = " + BatchID);


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectFinishGoodsData, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectFinishGoodsData, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectFinishGoodsData, sb), sqlparams);

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

        public static DataTable StroeITemsSumary(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
            int ObsoleteCheck = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, string DocumentTypes = "",
            SqlConnection objConnection = null, SqlTransaction objTransaction = null, bool FilterZeroQuantity = false, int? RackID = null, int? LocationID = null, bool isFromDate = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (Catalog.HasValue)
                    sb.Append(" and INV_ITem.ItemCatalogGroupID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (RackID.HasValue)
                {
                    sb.Append(" and INV_Item.ItemID in (select ItemID from Set_RackItems inner join Set_Rack on Set_Rack.RackID = Set_RackItems.RackID where Set_Rack .Status = 1 and Set_Rack.RackID = " + RackID + " )  ");
                }

                if (LocationID.HasValue)
                {
                    sb.Append(" and SET_Location.LocationID = " + LocationID + "");
                }

                if (FilterZeroQuantity)
                {
                    sb2.Append(" having ( (Abs(sum(OpeningQty)) + Abs(sum(TransferINQty)) + Abs(sum(StockReturnTransferQty) )) - Abs(sum(TransferQty)) ) != 0 ");
                }

                
                string Query = string.Empty;

                if (!isFromDate)
                {
                    Query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoreItemsSummary_AsOnDate;
                }
                else
                {
                    Query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoreItemsSummary;
                }

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);

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

        public static DataTable StroeITemsSumaryWithShelf(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
            int ObsoleteCheck = 0, Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> shelfRef = null, string DocumentTypes = "",
            SqlConnection objConnection = null, SqlTransaction objTransaction = null, bool FilterZeroQuantity = false, int? RackID = null, int? LocationID = null, int? FromShelfReferenceID = null, int? ToShelfReferenceID = null, bool isFromDate = false)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                if (Catalog.HasValue)
                    sb.Append(" and INV_ITem.ItemCatalogGroupID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                if (shelfRef.HasValue)
                    sb.Append(" and INV_ITem.ItemID = " + shelfRef + "");

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (RackID.HasValue)
                {
                    sb.Append(" and INV_Item.ItemID in (select ItemID from Set_RackItems inner join Set_Rack on Set_Rack.RackID = Set_RackItems.RackID where Set_Rack .Status = 1 and Set_Rack.RackID = " + RackID + " )  ");
                }


                if (LocationID.HasValue)
                {
                    sb.Append(" and SET_Location.LocationID = " + LocationID + "");
                }


                //if (FromShelfReferenceID != int.MinValue && ToShelfReferenceID != int.MaxValue)
                //{
                //    sb.Append(" AND INV_ITem.ItemID between " + FromShelfReferenceID + " and " + ToShelfReferenceID + " ");
                //}


                if (FromShelfReferenceID.HasValue && ToShelfReferenceID.HasValue)
                {
                    sb.Append(" AND INV_ITem.ItemID BETWEEN " + FromShelfReferenceID + " AND " + ToShelfReferenceID + " ");
                }



                if (FilterZeroQuantity)
                {
                    sb2.Append(" having ( (Abs(sum(OpeningQty)) + Abs(sum(TransferINQty)) + Abs(sum(StockReturnTransferQty) )) - Abs(sum(TransferQty)) ) != 0 ");
                }

                string Query = string.Empty;

                if (!isFromDate)
                {
                    Query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoreItemsSummaryShelf_AsOnDate;
                }
                else
                {
                    Query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoreItemsSummaryShelf;
                }


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb, sb1, sb2), sqlparams);

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

        public static DataTable GetFinishGoodsSummaryDataLotWise(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
            Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> BatchID = null, Nullable<int> LocationID = null, Nullable<int> RackID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                //if (fromDate.HasValue && toDate.HasValue)
                //{
                //    sb.Append(" and StockHeader.DocumentDate between @FromDate AND @ToDate ");
                //}
                //else if (fromDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate >= @FromDate ");

                //}
                //else if (toDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate <= @ToDate");
                //}
                if (LocationID.HasValue)
                    sqlparams[4] = new SqlParameter("@LocationID", LocationID.Value);
                else
                    sqlparams[4] = new SqlParameter("@LocationID", DBNull.Value);

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (BatchID.HasValue)
                    sb.Append(" and StockHeader.BatchID = " + BatchID);

                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                if (RackID.HasValue)
                    sb.Append(" and SET_Rack.RackID = " + RackID);

                if (Catalog.HasValue)
                    sb2.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb2.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb2.Append(" and INV_Item.ItemID = " + item + "");

                if (BatchID.HasValue)
                    sb2.Append(" and StockHeader.BatchID = " + BatchID);

                if (LocationID.HasValue)
                {
                    //sb2.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    sb2.Append(" and (ISNULL(SET_Location.LocationID, SL.LocationID) = @LocationID OR @LocationID is null)");
                    
                    
                }

                if (RackID.HasValue)
                    sb2.Append(" and SET_Rack.RackID = " + RackID);


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_FInishGoodsSummaryLotWise, sb, sb2), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_FInishGoodsSummaryLotWise, sb, sb2), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_FInishGoodsSummaryLotWise, sb, sb2), sqlparams);

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


        public static DataTable GetAllDataDetailOfMajorPlantMachinery(Nullable<int> periodID, string companyID, int ItemID, Nullable<DateTime> Date_From, int? StatusID, int? PersonID, SqlConnection objConnection = null)//string ddl_Item,
        {
            DataTable dt = null;
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                if (StatusID.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@StatusID", DBNull.Value);
                }
                if (PersonID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@PersonID", PersonID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@PersonID", DBNull.Value);
                }
                //if (ddl_Item != null)
                //{
                //    sqlparams[5] = new SqlParameter("@ddl_Item", ddl_Item.ToString());
                //}
                //else
                //{
                //    sqlparams[5] = new SqlParameter("@ddl_Item", DBNull.Value);
                //}
                if (ItemID > 0)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DetailOfMajorPlantMachinery, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DetailOfMajorPlantMachinery, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);
                if (ds != null && ds.Tables != null && ds.Tables.Count > 1)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds.Tables[0];
        }


        public static DataSet GetAllDataSugarHandlingStorageExpenses(Nullable<int> periodID, string companyID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SugarHandlingStorageExpenses).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SugarHandlingStorageExpenses).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    ds.Tables[3].TableName = "Table4";
                    ds.Tables[2].TableName = "Table3";
                    ds.Tables[1].TableName = "Table2";
                    ds.Tables[0].TableName = "Table1";
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataSet GetAllDataLimeSlakerReport(Nullable<int> periodID, string companyID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_LimeSlakerReport).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_LimeSlakerReport).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    ds.Tables[3].TableName = "Table4";
                    ds.Tables[2].TableName = "Table3";
                    ds.Tables[1].TableName = "Table2";
                    ds.Tables[0].TableName = "Table1";
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataSet GetAllDataCaneUnloadingExpensesReport(Nullable<int> periodID, string companyID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloadingExpensesReport).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloadingExpensesReport).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    ds.Tables[3].TableName = "Table4";
                    ds.Tables[2].TableName = "Table3";
                    ds.Tables[1].TableName = "Table2";
                    ds.Tables[0].TableName = "Table1";
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }


        public static DataTable GetAllDataSugarHandlingStorageExpensesSummary(Nullable<int> periodID, Nullable<int> companyID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SugarHandlingStorageExpensesMonthly, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SugarHandlingStorageExpensesMonthly, sqlparams);

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

        public static DataTable GetDataLiftingLocationWiseDetailReport(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,
            Nullable<int> item = null, Nullable<int> Catagory = null, Nullable<int> Catalog = null, Nullable<int> BatchID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
                DataSet ds = null;

                //if (fromDate.HasValue && toDate.HasValue)
                //{
                //    sb.Append(" and StockHeader.DocumentDate between @FromDate AND @ToDate ");
                //}
                //else if (fromDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate >= @FromDate ");

                //}
                //else if (toDate.HasValue)
                //{
                //    sb.Append("and StockHeader.DocumentDate <= @ToDate");
                //}

                if (Catalog.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + Catalog + "");

                if (Catagory.HasValue)
                    sb.Append(" and INV_SET_Category.CategoryID = " + Catagory + "");

                if (item.HasValue)
                    sb.Append(" and INV_Item.ItemID = " + item + "");

                if (BatchID.HasValue)
                    sb.Append(" and StockHeader.BatchID = " + BatchID);


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetDataLiftingLocationWiseDetailReport, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetDataLiftingLocationWiseDetailReport, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetDataLiftingLocationWiseDetailReport, sb), sqlparams);

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



        public static DataTable Get_DataForAPI(DateTime Date)
        {
            DataTable dt = null;
            StringBuilder AddFilters = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                sqlparams[0] = new SqlParameter("@Date", Date);

                const string Query_Get_DataForAPI = @"
select CompanyCode,ShiftName,sum(QuantityOnDate) as QuantityOnDate,sum(QuantityTodate) as QuantityTodate,

sum(onDateSalesContract) as onDateSalesContract ,sum(toDateSalesContract) as toDateSalesContract 

,FORMAT (getdate(), 'yyyy-MM-dd HH:mm:ss') as ServerDate from (

select  '005' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'A' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate,0 as onDateSalesContract,0 as toDateSalesContract


union all
select  '005' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'B' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate ,0 as onDateSalesContract,0 as toDateSalesContract
union all
select  '005' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'C' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate,0 as onDateSalesContract,0 as toDateSalesContract
union all
select  '006' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'A' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate,0 as onDateSalesContract,0 as toDateSalesContract
union all
select  '006' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'B' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate,0 as onDateSalesContract,0 as toDateSalesContract
union all
select  '006' as CompanyCode,'Finish Goods - Sugar' as DocumentType,'C' as ShiftName,0 as QuantityOnDate,0 as QuantityTodate,0 as onDateSalesContract,0 as toDateSalesContract

union all

select case INV_StockTransactionHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode ,

 Set_Documenttype .DocumentType,case INV_StockTransactionDetail.ShiftID when 1 then 'A' when 2 then 'B' when 3 then 'C'  else '' end as ShiftName
,sum(INV_StockTransactionDetail.TransactionQuantity) as QuantityOnDate
,0 as QuantityTodate

,0 as onDateSalesContract,0 as toDateSalesContract
from INV_StockTransactionHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and Set_Documenttype.CompanyID = INV_StockTransactionHeader.CompanyID

inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1 and INV_StockTransactionDetail.ShiftID is not null
 
where INV_StockTransactionHeader.StatusCode = 1 and INV_StockTransactionHeader.Status = 1 
and Set_Documenttype.Documenttype = 'Finish Goods - Sugar'
and  convert(date,INV_StockTransactionHeader .DocumentDate ) = convert(date,@Date)
group by INV_StockTransactionHeader.CompanyID, Set_Documenttype .DocumentType,INV_StockTransactionDetail.ShiftID 

union all

select case INV_StockTransactionHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode ,

 Set_Documenttype .DocumentType,case INV_StockTransactionDetail.ShiftID when 1 then 'A' when 2 then 'B' when 3 then 'C'  else '' end as ShiftName
,0 as QuantityOnDate
,sum(INV_StockTransactionDetail.TransactionQuantity) as QuantityTodate

,0 as onDateSalesContract,0 as toDateSalesContract
from INV_StockTransactionHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and Set_Documenttype.CompanyID = INV_StockTransactionHeader.CompanyID

inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1 and INV_StockTransactionDetail.ShiftID is not null
 
where INV_StockTransactionHeader.StatusCode = 1 and INV_StockTransactionHeader.Status = 1 
and Set_Documenttype.Documenttype = 'Finish Goods - Sugar'
and  convert(date,INV_StockTransactionHeader .DocumentDate ) <=  convert(date,@Date)
and INV_StockTransactionHeader.FiscalID in (
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 1 order by FiscalId desc
union all
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 2 order by FiscalId desc
)
group by INV_StockTransactionHeader.CompanyID, Set_Documenttype .DocumentType,INV_StockTransactionDetail.ShiftID 


union all

--Sales order on Date
select * from (
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'A' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,sum(SAL_OrderDetail.Quantity) as onDateSalesContract
,0 as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) = convert(date,@Date)

group by SAL_OrderHeader.CompanyID 

union all
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'B' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,sum(SAL_OrderDetail.Quantity) as onDateSalesContract
,0 as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) = convert(date,@Date)
group by SAL_OrderHeader.CompanyID 

union all
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'C' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,sum(SAL_OrderDetail.Quantity) as onDateSalesContract
,0 as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) = convert(date,@Date)
group by SAL_OrderHeader.CompanyID 
) as tabContrct

union all

--Sales order To Date
select * from (
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'A' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,0 as onDateSalesContract
,sum(SAL_OrderDetail.Quantity) as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) <= convert(date,@Date)
and SAL_OrderHeader.FiscalID in (
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 1 order by FiscalId desc
union all
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 2 order by FiscalId desc
)

group by SAL_OrderHeader.CompanyID 

union all
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'B' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,0 as onDateSalesContract
,sum(SAL_OrderDetail.Quantity) as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) <= convert(date,@Date)
and SAL_OrderHeader.FiscalID in (
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 1 order by FiscalId desc
union all
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 2 order by FiscalId desc
)
group by SAL_OrderHeader.CompanyID 

union all
select case SAL_OrderHeader.CompanyID when 1 then '006' when 2 then '005' else '' end as CompanyCode , 'SaleContract' as DocumentType,
'C' as ShiftName,
0 as QuantityOnDate
,0 as QuantityTodate
,0 as onDateSalesContract
,sum(SAL_OrderDetail.Quantity) as toDateSalesContract
from SAL_OrderHeader
inner join Set_Documenttype  on Set_Documenttype .DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and Set_Documenttype.CompanyID = SAL_OrderHeader.CompanyID
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
where SAL_OrderHeader.Status = 1 and SAL_OrderHeader.StatusCode = 1 and SAL_OrderDetail.Status = 1
and Set_Documenttype.Documenttype like '%Sugar%'
and  convert(date,SAL_OrderHeader.ContractDate) <= convert(date,@Date)
and SAL_OrderHeader.FiscalID in (
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 1 order by FiscalId desc
union all
select top 1 FiscalId from SET_PeriodCustom where CompanyID = 2 order by FiscalId desc
)
group by SAL_OrderHeader.CompanyID 
) as tabContrct


)as tab
group by CompanyCode,ShiftName
order by CompanyCode,ShiftName



";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DataForAPI, sqlparams);
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

        public static DataSet GetSugarProductionDispatchReportData(int companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate = null, Nullable<DateTime> EndDateofPreviousFiscal = null, Nullable<int> LocationID = null, bool isHabib = false, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            //DataTable dt = null;
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@EndDateofPreviousFiscal", EndDateofPreviousFiscal);

                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                string ReportQuery = "";

                if (isHabib)
                    ReportQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SugarProductionDispatchReport_HSM;
                else
                    ReportQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SugarProductionDispatchReport;



                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(ReportQuery, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet GetSugarProductionDispatchReportDataforDKSM(int companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate = null, Nullable<DateTime> EndDateofPreviousFiscal = null, Nullable<int> LocationID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            //DataTable dt = null;
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@EndDateofPreviousFiscal", EndDateofPreviousFiscal);

                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                string ReportQuery = "";

                    ReportQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SugarProductionDispatchReportForDKSM;



                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(ReportQuery, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet GetSugarProductionDispatchReportDatafor_Pattoki(int companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate = null, Nullable<DateTime> EndDateofPreviousFiscal = null, Nullable<int> LocationID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            //DataTable dt = null;
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@EndDateofPreviousFiscal", EndDateofPreviousFiscal);

                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                string ReportQuery = "";

                ReportQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SugarProductionDispatchReportForPTK;



                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(ReportQuery, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(ReportQuery, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public static DataSet DailyProductionSaleStockPositionReportData(int companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate = null, Nullable<int> LocationID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            //DataTable dt = null;
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPosition, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPosition, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPosition, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet DailyProductionSaleStockPositionReportDataCo2(int companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate = null, Nullable<int> LocationID = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            //DataTable dt = null;
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (LocationID.HasValue)
                    sb.Append(" and Set_Location.LocationID = " + LocationID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPositionCO2, sb), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPositionCO2, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_DailyProductionSaleStockPositionCO2, sb), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable Get_GRNReturnData(int CompanyID, int PeriodID, int SourceHeaderID, int DocumentTypeID)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", SourceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                DataSet ds = null;
 
                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GRNReturnData, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {

            }

            return dt;
        }

        public static DataTable Get_ElectricityProductionData(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_Get_ElectricityProductionData, sqlparams);

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

        public static DataTable Get_ProductionCostSheetReport(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate, bool IsSummary)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = null;

                if (IsSummary)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ProductionCostSheetSummaryReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_ProductionCostSheetReport, sqlparams);

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

        public DataTable GetDashBoard_PendingGRN(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null,
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = " ";


            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_PendingGRN = @" 
select DepartmentName,DocumentNo,DocumentDate,DocumentType,CustomerName,ROW_NUMBER() Over (Order by DocumentDate DESC) as 'Row'   from 
(
SELECT DISTINCT 
                      INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
                      SET_DocumentType.DocumentType
                      ,AR_SET_Customer.CustomerName,SET_Department.DepartmentName
                        ,IsNull(PurchaseInvoice.InvoicedQuantity, 0)as InvoiceDtlQuantity
                        ,sum(INV_StockTransactionDetail.TransactionQuantity) as GRNQty
FROM         INV_StockTransactionHeader 
	INNER JOIN
INV_StockTransactionDetail 
ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
and INV_StockTransactionDetail.status = 1
	INNER JOIN
SET_DocumentType 
ON dbo.INV_StockTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
AND   [SET_DocumentType].[Status] = 1 
AND INV_StockTransactionHeader.PeriodID = SET_DocumentType.PeriodID
AND INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID

inner JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID 
        AND AR_SET_Customer.PeriodID = INV_StockTransactionHeader.PeriodID 
LEFT JOIN SET_Department ON SET_Department.DepartmentID = INV_StockTransactionHeader.DepartmentID
Outer Apply 
	(
		select Sum(IsNull(AR_InvoiceDetail.QuantityInvoiced, 0)) As InvoicedQuantity from AR_InvoiceDetail 
		inner join AR_InvoiceHeader on AR_InvoiceHeader.invoiceID = AR_InvoiceDetail.InvoiceHeaderID
		and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
		and AR_InvoiceHeader.periodID = AR_InvoiceDetail.periodID
		where AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID and AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
			
			And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
			and isnull(AR_InvoiceHeader.IsReversed,0) = 0and isnull(AR_InvoiceHeader.ReversalID,0) = 0
			and AR_InvoiceDetail.Status = 1 
		
	) As PurchaseInvoice  

 Where [INV_StockTransactionHeader].[CompanyID] = 1 AND [INV_StockTransactionHeader].[PeriodID] = 1 AND [INV_StockTransactionHeader].[Status] = 1 
 AND [INV_StockTransactionHeader].StatusCode = 1 AND ISNULL([INV_StockTransactionHeader].IsReversed, 0) != 1 

 AND SET_DocumentType.DocumentType like '%GRN%' and  SET_DocumentType.DocumentType like '%GRN%' 
group by INV_StockTransactionHeader.DocumentNo,INV_StockTransactionHeader.DocumentDate,
INV_StockTransactionHeader.DocumentTypeID,SET_DocumentType.DocumentType,PurchaseInvoice.InvoicedQuantity
,AR_SET_Customer.CustomerName,DepartmentName
) as TAB
where TAB.GRNQty <> TAB.InvoiceDtlQuantity";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingGRN) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingGRN) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingGRN) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingGRN) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDashBoard_PendingGRNCount(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Query

                const string Query_GetDashBoard_PendingGRNCount = @"

SELECT DepartmentName,COUNT(DocumentNo) AS Count FROM (
SELECT DISTINCT 
                      INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
                      SET_DocumentType.DocumentType
                      ,AR_SET_Customer.CustomerName,SET_Department.DepartmentName
                        ,IsNull(PurchaseInvoice.InvoicedQuantity, 0)as InvoiceDtlQuantity
                        ,sum(INV_StockTransactionDetail.TransactionQuantity) as GRNQty
FROM         INV_StockTransactionHeader 
	INNER JOIN
INV_StockTransactionDetail 
ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
and INV_StockTransactionDetail.status = 1
	INNER JOIN
SET_DocumentType 
ON dbo.INV_StockTransactionHeader.CompanyID = SET_DocumentType.CompanyID 
AND   [SET_DocumentType].[Status] = 1 
AND INV_StockTransactionHeader.PeriodID = SET_DocumentType.PeriodID
AND INV_StockTransactionHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID

inner JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = INV_StockTransactionHeader.BusinessPartnerID 
        AND AR_SET_Customer.PeriodID = INV_StockTransactionHeader.PeriodID 
LEFT JOIN SET_Department ON SET_Department.DepartmentID = INV_StockTransactionHeader.DepartmentID
Outer Apply 
	(
		select Sum(IsNull(AR_InvoiceDetail.QuantityInvoiced, 0)) As InvoicedQuantity from AR_InvoiceDetail 
		inner join AR_InvoiceHeader on AR_InvoiceHeader.invoiceID = AR_InvoiceDetail.InvoiceHeaderID
		and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
		and AR_InvoiceHeader.periodID = AR_InvoiceDetail.periodID
		where AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID and AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
			
			And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
			and isnull(AR_InvoiceHeader.IsReversed,0) = 0and isnull(AR_InvoiceHeader.ReversalID,0) = 0
			and AR_InvoiceDetail.Status = 1 
		
	) As PurchaseInvoice  

 Where [INV_StockTransactionHeader].[CompanyID] = 1 AND [INV_StockTransactionHeader].[PeriodID] = 1 AND [INV_StockTransactionHeader].[Status] = 1 
 AND [INV_StockTransactionHeader].StatusCode = 1 AND ISNULL([INV_StockTransactionHeader].IsReversed, 0) != 1 

 AND SET_DocumentType.DocumentType like '%GRN%' and  SET_DocumentType.DocumentType like '%GRN%' 
group by INV_StockTransactionHeader.DocumentNo,INV_StockTransactionHeader.DocumentDate,
INV_StockTransactionHeader.DocumentTypeID,SET_DocumentType.DocumentType,PurchaseInvoice.InvoicedQuantity
,AR_SET_Customer.CustomerName,DepartmentName
) as TAB
where TAB.GRNQty <> TAB.InvoiceDtlQuantity
GROUP BY DepartmentName

";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDashBoard_PendingGRNCount, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
    }
}

