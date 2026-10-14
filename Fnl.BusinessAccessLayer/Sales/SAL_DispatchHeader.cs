using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Inventory.Setup;
using System.Linq;


namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DispatchHeader
    {
        #region Non-Table based Parameters

        public static bool IsDirectDispatchEnabled
        {
            get;
            set;
        }

        #endregion

        #region Constant Queries

        const string Query_GetTableCloumnsName = @"
select
   syscolumns.name as [Column],
   syscolumns.xusertype as [Type],
   sysobjects.xtype as [Objtype]
from 
   sysobjects, syscolumns 
where sysobjects.id = syscolumns.id
and   sysobjects.xtype = 'u'
and   sysobjects.name = @TableName
order by syscolumns.name
";


        const string Query_Get_MaxGatePassNo = @" SELECT IsNull(max(CAST(GatePassNo AS INT)), 0) + 1 as GatePassNo FROM SAL_DispatchHeader 
            WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and FiscalID = @FiscalID ";

        const string Query_Get_MaxGatePassNoByTokenNo = @" SELECT IsNull(max(CAST(GatePassNo AS INT)), 0) + 1 as GatePassNo FROM SAL_DispatchHeader 
            WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and FiscalID = @FiscalID and DocumentTypeID = @DocumentTypeID ";

        const string Query_Get_MaxGatePassNoVehicle = @" SELECT IsNull(count(CAST(DispatchID AS INT)), 0) + 1 as GatePassNo FROM SAL_DispatchHeader 
            WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID AND DocumentTypeID = @documentType ";




        const string Query_Select_By_CustomerID_ForLubricantReturn = @" Select * From (
SELECT Distinct StockHeader.[StockTransactionID], [SAL_DispatchHeader].[DispatchNo], 
	[SAL_DispatchHeader].[DispatchDate], 
    SAL_OrderHeader.referenceno,
    --'' as referenceno,
    Set_DocumentType.DocumentType,
	Sum([INV_StockTransactionDetail].[TransactionQuantity] * -1) as StockQuantity, 
	SUM(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0)) as InvoicedQuantity
	,Cast(((select SUM(TransactionQuantity) from INV_stockTransactionDetail Where INV_stockTransactionDetail.CompanyID = StockHeader.CompanyID And INV_stockTransactionDetail.PeriodID = StockHeader.PeriodID 
				And INV_stockTransactionDetail.StocktransactionID = StockHeader.StockTransactionID 
			Group By INV_stockTransactionDetail.CompanyID, INV_stockTransactionDetail.PeriodID) * -1) AS Decimal(18,0))TransactionQty
	,isnull([INV_StockTransactionDetail].ReversedQuantity,0) as ReversedQuantity
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

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID

left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderId
	 AND SAL_OrderHeader.[CompanyID] = SAL_OrderDetail.[CompanyID]
	 AND SAL_OrderHeader.[PeriodID] = SAL_OrderDetail.[PeriodID]
	 AND SAL_OrderHeader.[Status] = 1
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = [SAL_DispatchHeader].DocumentTypeID
	  AND Set_DocumentType.CompanyID = [SAL_DispatchHeader].CompanyID AND Set_DocumentType.PeriodID = [SAL_DispatchHeader].PeriodID 
	 
WHERE [SAL_DispatchHeader].[StatusCode] = 1 AND ([SAL_DispatchHeader].[CustomerID] = @CustomerID OR @CustomerID IS NULL) 
	  AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
	  AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
	  AND [SAL_DispatchHeader].[Status] = 1
      {0}
	  
Group By SAL_DispatchHeader.DocumentTypeID, SAL_DispatchHeader.DispatchNo, SAL_DispatchHeader.DispatchDate, 
	StockHeader.[StockTransactionID], SAL_OrderHeader.referenceno, Set_DocumentType.DocumentType, StockHeader.CompanyID, StockHeader.PeriodID
    ,[INV_StockTransactionDetail].ReversedQuantity
	  --HAVING Sum([INV_StockTransactionDetail].[TransactionQuantity] * -1) > SUM(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0))
	  )tab
	  --Where tab.TransactionQty > InvoicedQuantity
    Where tab.TransactionQty > tab.ReversedQuantity
Order By tab.[DispatchDate]";

        const string Query_Select_By_CustomerIDDispatch = @" Select * From (
SELECT Distinct StockHeader.[StockTransactionID], [SAL_DispatchHeader].[DispatchNo], 
	[SAL_DispatchHeader].[DispatchDate], 
    '' as referenceno,
    Set_DocumentType.DocumentType,
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

left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID

left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderId
	 AND SAL_OrderHeader.[CompanyID] = SAL_OrderDetail.[CompanyID]
	 AND SAL_OrderHeader.[PeriodID] = SAL_OrderDetail.[PeriodID]
	 AND SAL_OrderHeader.[Status] = 1
INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = [SAL_DispatchHeader].DocumentTypeID
	  AND Set_DocumentType.CompanyID = [SAL_DispatchHeader].CompanyID AND Set_DocumentType.PeriodID = [SAL_DispatchHeader].PeriodID 
	 
WHERE [SAL_DispatchHeader].[StatusCode] = 1 AND ([SAL_DispatchHeader].[CustomerID] = @CustomerID OR @CustomerID IS NULL) 
	  AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
	  AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
	  AND [SAL_DispatchHeader].[Status] = 1
      {0}
	  
Group By SAL_DispatchHeader.DocumentTypeID, SAL_DispatchHeader.DispatchNo, SAL_DispatchHeader.DispatchDate, 
	StockHeader.[StockTransactionID], SAL_OrderHeader.referenceno, Set_DocumentType.DocumentType, StockHeader.CompanyID, StockHeader.PeriodID
	  HAVING Sum([INV_StockTransactionDetail].[TransactionQuantity] * -1) > SUM(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0))
	  )tab
	  Where tab.TransactionQty > InvoicedQuantity
      {1}
Order By tab.[DispatchDate]";

        const string Query_CheckQuantity = @" SELECT [SAL_DeliveryOrderDetail].[DOHeaderID], [SAL_DeliveryOrderDetail].[DODetailID], [SAL_DeliveryOrderDetail].[Quantity] AS DOQuantity, Sum([INV_StockTransactionDetail].[TransactionQuantity]) AS DQuantity
FROM [INV_StockTransactionDetail]
	INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID] 
		AND [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID] 
		AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
		AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [SAL_DeliveryOrderDetail].[Status] = 1 
WHERE [INV_StockTransactionDetail].[SourceHeaderID] = @DeliveryOrderHeaderID AND [INV_StockTransactionDetail].[CompanyID] = @CompanyID AND [INV_StockTransactionDetail].[PeriodID] = @PeriodID 
	AND [INV_StockTransactionDetail].[Status] = 1
Group By [SAL_DeliveryOrderDetail].[DOHeaderID], [SAL_DeliveryOrderDetail].[DODetailID], [SAL_DeliveryOrderDetail].[Quantity]
Having Sum([INV_StockTransactionDetail].[TransactionQuantity]) >= [SAL_DeliveryOrderDetail].[Quantity] ";

        const string Query_Update_Flag_Detail = @" UPDATE [INV_StockTransactionDetail] SET [InvoiceFlag] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [StockTransactionID] = @StockTransactionID AND [TransactionDetailID] = @TransactionDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_Get_DOHID = @" SELECT [SourceHeaderID] FROM [INV_StockTransactionDetail] WHERE [StockTransactionID] = @StockHeaderID AND [TransactionDetailID] = @StockDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";

        const string Query_CheckIsDetailCompleted = @" SELECT COUNT([INV_StockTransactionDetail].[InvoiceFlag]) AS IsInvoiceGeneratedCount FROM [INV_StockTransactionDetail] WHERE [INV_StockTransactionDetail].[StockTransactionID] = @StockTransactionID AND [INV_StockTransactionDetail].[CompanyID] = @CompanyID AND [INV_StockTransactionDetail].[PeriodID] = @PeriodID AND [INV_StockTransactionDetail].[Status] = 1 AND [INV_StockTransactionDetail].[InvoiceFlag] = @IsGenerated ";

        const string Query_Update_Flag_Header = @" UPDATE [SAL_DispatchHeader] SET [IsInvoiceGenerated] = @IsGenerated, 
[UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = 
@UpdatedByValue WHERE [DispatchID] = @DispatchID AND [CompanyID] =
@CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";




        const string Query_GetGoodsIssueDetail = @" 
select '(' + cast(AR_SET_Customer.CustomerID as varchar(30)) + ') - ' + AR_SET_Customer.CustomerName as CustomerName, 
    '(' + INV_Item.InventoryItemStatusCode + ')-' + INV_Item.Description as item, SET_Unitofmeasure.UOMName, 
        AR_SET_Customer.CustomerId,
        SAL_OrderDetail.Rate, 
      ROUND( (SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0))
        *
        (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
		when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end),0) AS GrossAmount,
    cast(SAL_OrderHeader.ContractNo as int) as Sal_OrderNo, 
    cast(SAL_OrderHeader.ContractNo as int) as ContractNo,
    SAL_OrderHeader.ContractDate, 
    SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)as Sal_OrderQuantity, 
    SAL_DeliveryOrderHeader.DeliveryOrderNo, SAL_DeliveryOrderHeader.DODate, 
    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as Del_Quantity,
    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as DOQuantity, 
    isnull(SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0) as Balance, 
    ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) DispatchQty, INV_StockTransactionHeader.DocumentNo as DispatchNo, Sal_DispatchHeader.VehicleNo, 
    Sal_DispatchHeader.BiltyNo, Sal_DispatchHeader.DriverName ,
INV_StockTransactionDetail.TransactionQuantity, DocumentTypesGroup.GroupName,
SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)  as SOQuantity
from SAL_OrderHeader 
    inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
        and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_OrderDetail.Status = 1 and SAL_OrderHeader.Status = 1 
    left join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID --and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
and INV_Item.Status = 1 
    left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_OrderDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
--and AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        and AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID and AR_SET_Customer.Status = 1 
    left join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderHeader.OrderID 
        and SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID and SAL_DeliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
        and SAL_DeliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_DeliveryOrderDetail.Status = 1 
    left join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
        and SAL_DeliveryOrderHeader.CompanyID = SAL_OrderHeader.CompanyID and SAL_DeliveryOrderHeader.PeriodID = SAL_OrderHeader.PeriodID 
        and SAL_DeliveryOrderHeader.Status = 1 
    left join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID 
        and INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
        and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
        and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
        and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
    left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
    left Join Sal_DispatchHeader on SAL_DispatchHeader.CompanyID = Inv_stockTransactionHeader.CompanyID 
        and Inv_stockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID and Inv_stockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
        and Inv_stockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo and Sal_DispatchHeader.Status = 1 
    inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
    LEFT Join DocumentTypesGroup On  DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
where SAL_OrderHeader.CompanyID = @CompanyID and SAL_OrderHeader.PeriodID = @PeriodID and SAL_OrderHeader.Status = 1 
    {0} 
group by INV_Item.Description, INV_Item.InventoryItemStatusCode, SET_Unitofmeasure.UOMName, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, 
    AR_SET_Customer.CustomerName, AR_SET_Customer.CustomerID, SAL_OrderHeader.ContractNo, SAL_OrderHeader.ContractDate, SAL_OrderDetail.Quantity, 
    INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo, Sal_DispatchHeader.VehicleNo, Sal_DispatchHeader.BiltyNo, 
    Sal_DispatchHeader.DriverName, SAL_OrderHeader.ContractDate, SAL_DeliveryOrderHeader.DeliveryOrderNo, SAL_DeliveryOrderHeader.DODate, 
    SAL_DeliveryOrderDetail.Quantity, 
    DocumentTypesGroup.GroupName
    ,SAL_OrderDetail.ReversedQuantity,
    Sal_OrderDetail.BaseAmount,
    SAL_OrderDetail.Inclusive,
    SAL_DeliveryOrderDetail.ReversedQty

--order by cast(SAL_OrderHeader.ContractNo as int) asc 

";




        const string query_Select_DataByKey_ForApprove = @" 
SELECT DISTINCT SAL_DispatchHeader.DispatchID
	,SAL_DispatchHeader.CompanyID
	,SAL_DispatchHeader.PeriodID
	,SAL_DispatchHeader.Status
    ,SET_Authority.AuthorityLevel
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_UserApprovalAmount.ApprovalAmountID 
    ,SET_UserApprovalAmount.ApprovalAuthorityID
FROM SAL_DispatchHeader 
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DispatchHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_DispatchHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT dh.TotalAmount FROM SAL_DispatchHeader dh 
											WHERE dh.CompanyID = SAL_DispatchHeader.CompanyID 
												AND dh.PeriodID = SAL_DispatchHeader.PeriodID 
												AND dh.DispatchID = SAL_DispatchHeader.DispatchID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT dh.TotalAmount FROM SAL_DispatchHeader dh 
													WHERE dh.CompanyID = SAL_DispatchHeader.CompanyID 
														AND dh.PeriodID = SAL_DispatchHeader.PeriodID 
														AND dh.DispatchID = SAL_DispatchHeader.DispatchID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1

INNER JOIN aspnet_UsersInRoles On aspnet_UsersInRoles.UserID = SAL_DispatchHeader.CreatedBy 
And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null)

WHERE --SET_UserApprovalAmount.ApprovalOperator = 'AND' AND 
SAL_DispatchHeader.Status = 1 AND SAL_DispatchHeader.PeriodID = @PeriodID 
    AND SAL_DispatchHeader.CompanyID = @CompanyID AND SAL_DispatchHeader.DispatchID = @DispatchID AND SAL_DispatchHeader.DocumentTypeID = @DocumentTypeID 
    AND SAL_DispatchHeader.DispatchNo = @DocumentNo ";


        const string GetDataByCustomerIDForGateEntryQuery = @"
SELECT 
DISTINCT [INV_StockTransactionHeader].[StockTransactionID], 
[SAL_DispatchHeader].[DispatchNo], 
[SAL_DispatchHeader].[DispatchDate],'' as referenceno
FROM [SAL_DispatchHeader]
INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1
INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = [INV_StockTransactionHeader].[StockTransactionID] AND [INV_StockTransactionDetail].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1
left JOIN Gate_OutwardHeader on Gate_OutwardHeader.SourceHeaderID = [INV_StockTransactionHeader].[StockTransactionID] AND Gate_OutwardHeader.CompanyID = INV_StockTransactionHeader.CompanyID and Gate_OutwardHeader.PeriodID = INV_StockTransactionHeader.PeriodID
left JOIN Gate_OutwardDetail on Gate_OutwardDetail.Gate_OutwardHeaderID = Gate_OutwardHeader.Gate_OutwardHeaderID AND Gate_OutwardDetail.CompanyID = Gate_OutwardHeader.CompanyID and Gate_OutwardDetail.PeriodID = Gate_OutwardHeader.PeriodID and Gate_OutwardDetail.ItemID = [INV_StockTransactionDetail].[ITemID]
	  WHERE
	  ([SAL_DispatchHeader].[CustomerID] = @CustomerID OR @CustomerID IS NULL)
	  AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
	  AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
	  AND [SAL_DispatchHeader].[Status] = 1
	  AND isnull((([INV_StockTransactionDetail].TransactionQuantity) - Gate_OutwardDetail.TransactionQuantity),-1) <> 0
GROUP BY [INV_StockTransactionHeader].[StockTransactionID], [SAL_DispatchHeader].[DispatchNo], [SAL_DispatchHeader].[DispatchDate],[Gate_OutwardDetail].TransactionQuantity,([INV_StockTransactionDetail].TransactionQuantity - Gate_OutwardDetail.TransactionQuantity),[INV_StockTransactionDetail].TransactionQuantity
HAVING SUM(ISNULL([INV_StockTransactionDetail].[TransactionQuantity],-0) * -1) > SUM(ISNULL([Gate_OutwardDetail].TransactionQuantity, -0)*-1)
Order By [SAL_DispatchHeader].[DispatchDate]
";

        const string Query_GetRefrenceNoByDONo = @"
            Select SAL_OrderHeader.referenceno, Set_Batch.Description as Batch 
            from SAL_DeliveryOrderHeader
    
                INNER JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
                    AND Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
    
                INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
                    AND SAL_OrderHeader.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND SAL_OrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
                    AND SAL_OrderHeader.[Status] = 1 
    
                LEFT JOIN Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
                    And Set_Batch.BatchID = SAL_OrderHeader.Season 
        
            WHERE SAL_DeliveryOrderHeader.[StatusCode] = 1 AND SAL_DeliveryOrderHeader.DeliveryOrderNo = @DeliveryOrderNo AND SAL_DeliveryOrderHeader.[CompanyID] = @CompanyID 
                AND SAL_DeliveryOrderHeader.[PeriodID] = @PeriodID AND SAL_DeliveryOrderHeader.[Status] = 1";

        const string Query_GetRefrenceNoByKey = @"
Select SAL_OrderHeader.referenceno, Set_Batch.Description as Batch, SAL_OrderHeader.DTRENo
from SAL_DispatchHeader 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
    inner join Set_PeriodCustom on  
 INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

    INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = [INV_StockTransactionHeader].[StockTransactionID] 
        AND [INV_StockTransactionDetail].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] 
        AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1 
    INNER JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID 
        AND Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID 
        AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID 
    Inner Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = [SAL_DeliveryOrderDetail].SalesOrderHeaderID 
        AND SAL_OrderHeader.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND SAL_OrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND SAL_OrderHeader.[Status] = 1 
    Left Join Set_Batch On Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And Set_Batch.PeriodID = SAL_OrderHeader.PeriodID 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
WHERE [SAL_DispatchHeader].[StatusCode] = 1 AND [SAL_DispatchHeader].DispatchNo = @DispatchNo AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
    AND [SAL_DispatchHeader].[PeriodID] = @PeriodID AND [SAL_DispatchHeader].[Status] = 1 ";


        const string Query_GetData_byDispatchNo = @" SELECT * FROM [SAL_DispatchHeader] WHERE [CompanyID] = @CompanyID and [DispatchNo] = @DispatchNo and [PeriodID] = @PeriodID ";

        const string Query_GetDataByInvoiceID = @"
SELECT
DISTINCT
SAL_InvoiceHeader.InvoiceID,
SAL_InvoiceHeader.CustomerID,
AR_SET_Customer.CustomerName,
SAL_DispatchHeader.DispatchNo,
INV_StockTransactionHeader.StockTransactionID
FROM SAL_DispatchHeader
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
AND INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
AND INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID

INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID
AND SAL_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID
AND SAL_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID
AND SAL_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID

INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID
AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
AND SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID

INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID
--AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID
AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID

WHERE SAL_DispatchHeader.CompanyID = @CompanyID
AND SAL_DispatchHeader.PeriodID = @PeriodID
AND SAL_InvoiceHeader.InvoiceID = @InvoiceID
AND SAL_InvoiceHeader.CustomerID = @CustomerID";

        const string Query_DailySaleReport = @"SELECT SAL_DispatchHeader.DispatchNo AS ChallanNo, SAL_DispatchHeader.DispatchDate, AR_SET_Customer.CustomerName AS PartyName, 
	INV_Item.Description AS ItemName, INV_StockTransactionDetail.TransactionQuantity AS Quantity, 
	INV_StockTransactionDetail.TransactionCost AS Rate, INV_StockTransactionDetail.Amount, SAL_DispatchHeader.PaymentType, 
	INV_StockTransactionDetail.DiscountAmount AS Discount, INV_StockTransactionDetail.SalesTax,
    Round((INV_StockTransactionDetail.Amount  * INV_StockTransactionDetail.SalesTax) / 100,2) as SalexTaxAmount
	,(Case SAL_DispatchHeader.PaymentType when 1 then (INV_StockTransactionDetail.Amount  * INV_StockTransactionDetail.SalesTax) / 100 else 0 end) as SaleTaxAmountCash,
	(Case SAL_DispatchHeader.PaymentType when 2 then (INV_StockTransactionDetail.Amount  * INV_StockTransactionDetail.SalesTax) / 100 else 0 end) as SaleTaxAmountCredit 
    FROM SAL_DispatchHeader 
	INNER JOIN INV_StockTransactionHeader ON SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID 
		and SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
		and SAL_DispatchHeader.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
		and SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo 
	INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
		and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
		And INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
	INNER JOIN AR_SET_Customer ON 
--AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID and 
AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID And AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID 
	INNER JOIN INV_Item ON INV_StockTransactionDetail.ItemID = INV_Item.ItemID --INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID And  
    INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
		AND Set_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID 
		AND Set_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID
    WHERE [SAL_DispatchHeader].[CompanyID] = @CompanyID AND [SAL_DispatchHeader].[PeriodID] = @PeriodID AND [SAL_DispatchHeader].[Status] = 1
    AND [SAL_DispatchHeader].[IsDirectDispatch] = 'true'
";


        const string Query_DeliveryChallanVise_Report = @"select distinct SAL_DispatchHeader.CompanyID,SAL_DispatchHeader.PeriodID,SAL_DispatchHeader.DispatchNo,SAL_DispatchHeader.GatePassNo,
	   SAL_DispatchHeader.VehicleNo,SAL_DispatchHeader.TransporterID,AR_SET_Customer.CustomerName TransporterName,SAL_DispatchHeader.DispatchDate,
	   SAL_DispatchHeader.CustomerID,CustName.CustomerName,INV_StockTransactionDetail.ItemID,INV_Item.Description +'  (  '+ INV_Item.InventoryItemStatusCode+'  )' as Description,SAL_DeliveryOrderHeader.DeliveryOrderNo,SAL_OrderHeader.ContractNo as SaleOrederNo,INV_StockTransactionDetail.PrimaryQuantity,
	   ABS(INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity,SAL_OrderDetail.ItemCategoryID,CategoryName,SAL_DeliveryOrderDetail.DOHeaderID
	   from SAL_DispatchHeader
inner join INV_StockTransactionHEader on INV_StockTransactionHEader.CompanyID=SAL_DispatchHeader.CompanyID
AND INV_StockTransactionHEader.PeriodID=SAL_DispatchHeader.PeriodID
AND INV_StockTransactionHEader.DocumentTypeID=SAL_DispatchHeader.DocumentTypeID
AND INV_StockTransactionHEader.DocumentNo=SAL_DispatchHeader.DispatchNo

Inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.CompanyID=INV_StockTransactionHEader.CompanyID
AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHEader.PeriodID
AND INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHEader.StockTransactionID

Inner Join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.CompanyID=INV_StockTransactionDetail.CompanyID
AND SAL_DeliveryOrderHeader.PeriodID=INV_StockTransactionDetail.PeriodID
AND SAL_DeliveryOrderHeader.DeliveryOrderID=INV_StockTransactionDetail.SourceHeaderID
AND SAL_DeliveryOrderHeader.DeliveryOrderNo=INV_StockTransactionDetail.SourceCode

Inner Join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.CompanyID=SAL_DeliveryOrderHeader.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID=SAL_DeliveryOrderHeader.PeriodID
AND SAL_DeliveryOrderDetail.DOHeaderID=SAL_DeliveryOrderHeader.DeliveryOrderID

inner Join SAL_OrderHeader on SAL_OrderHeader.CompanyID=SAL_DeliveryOrderDetail.CompanyID
AND SAL_OrderHeader.PeriodID=SAL_DeliveryOrderDetail.PeriodID
AND SAL_OrderHeader.OrderID=SAL_DeliveryOrderDetail.SalesOrderHeaderID
inner Join SAL_OrderDetail on SAL_OrderDetail.CompanyID=SAL_OrderHeader.CompanyID
AND SAL_OrderDetail.PeriodID=SAL_OrderHeader.PeriodID
AND SAL_OrderDetail.OrderID=SAL_OrderHeader.OrderID
inner join INV_Item on INV_Item.ItemID=INV_StockTransactionDetail.ItemID --INV_Item.CompanyID=INV_StockTransactionDetail.CompanyID AND 
inner  join AR_SET_Customer CustName on CustName.CompanyID=SAL_DispatchHeader.CompanyID
AND CustName.PeriodID=SAL_DispatchHeader.PeriodID
AND CustName.CustomerID=SAL_DispatchHeader.CustomerID
left  join AR_SET_Customer on 
--AR_SET_Customer.CompanyID=SAL_DispatchHeader.CompanyID AND 
AR_SET_Customer.PeriodID=SAL_DispatchHeader.PeriodID
AND AR_SET_Customer.CustomerID=SAL_DispatchHeader.TransporterID
inner Join INV_SET_Category on 
--INV_SET_Category.CompanyID=SAL_OrderDetail.CompanyID
--AND 
INV_SET_Category.CategoryID=SAL_OrderDetail.ItemCategoryID

where [SAL_DispatchHeader].[Status] = 1 
";
        const string Query_CustomerViseMonthly_Report = @"select tab.DispatchNo,tab.DocumentTypeID,tab.SaleOrderNo,tab.CustomerName,tab.Description,tab.Rate,tab.Value,tab.TransactionQuantity as Dispatch_TransactionQuantity,
tab.Quantity,tab.TransactionQuantity,tab.DeliveryOrderNo,tab.DOHeaderID
 from 
(
select distinct SAL_DispatchHeader.CompanyID,SAL_DispatchHeader.PeriodID,SAL_DispatchHeader.DispatchNo,SAL_DispatchHeader.GatePassNo,
	   SAL_DispatchHeader.VehicleNo,SAL_DispatchHeader.TransporterID,AR_SET_Customer.CustomerName TransporterName,SAL_DispatchHeader.DocumentTypeID,
	   SAL_DispatchHeader.DispatchDate,SAL_DispatchHeader.CustomerID,CustName.CustomerName,INV_StockTransactionDetail.ItemID,
	   INV_Item.Description +'  (  '+ INV_Item.InventoryItemStatusCode+'  )' as Description,SAL_DeliveryOrderHeader.DeliveryOrderNo,
	   SAL_OrderHeader.ContractNo as SaleOrderNo,SAL_OrderDetail.Quantity,INV_StockTransactionDetail.PrimaryQuantity,
	   ABS(INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity,SAL_OrderDetail.ItemCategoryID,CategoryName,
	   SAL_DeliveryOrderDetail.DOHeaderID,SAL_OrderDetail.Rate,(SAL_OrderDetail.Quantity * SAL_OrderDetail.Rate) as Value	
	   from SAL_DispatchHeader
inner join INV_StockTransactionHEader on INV_StockTransactionHEader.CompanyID=SAL_DispatchHeader.CompanyID
AND INV_StockTransactionHEader.PeriodID=SAL_DispatchHeader.PeriodID
AND INV_StockTransactionHEader.DocumentTypeID=SAL_DispatchHeader.DocumentTypeID
AND INV_StockTransactionHEader.DocumentNo=SAL_DispatchHeader.DispatchNo

Inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.CompanyID=INV_StockTransactionHEader.CompanyID
AND INV_StockTransactionDetail.PeriodID=INV_StockTransactionHEader.PeriodID
AND INV_StockTransactionDetail.StockTransactionID=INV_StockTransactionHEader.StockTransactionID

Inner Join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.CompanyID=INV_StockTransactionDetail.CompanyID
AND SAL_DeliveryOrderHeader.PeriodID=INV_StockTransactionDetail.PeriodID
AND SAL_DeliveryOrderHeader.DeliveryOrderID=INV_StockTransactionDetail.SourceHeaderID
AND SAL_DeliveryOrderHeader.DeliveryOrderNo=INV_StockTransactionDetail.SourceCode

Inner Join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.CompanyID=SAL_DeliveryOrderHeader.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID=SAL_DeliveryOrderHeader.PeriodID
AND SAL_DeliveryOrderDetail.DOHeaderID=SAL_DeliveryOrderHeader.DeliveryOrderID

inner Join SAL_OrderHeader on SAL_OrderHeader.CompanyID=SAL_DeliveryOrderDetail.CompanyID
AND SAL_OrderHeader.PeriodID=SAL_DeliveryOrderDetail.PeriodID
AND SAL_OrderHeader.OrderID=SAL_DeliveryOrderDetail.SalesOrderHeaderID
inner Join SAL_OrderDetail on SAL_OrderDetail.CompanyID=SAL_OrderHeader.CompanyID
AND SAL_OrderDetail.PeriodID=SAL_OrderHeader.PeriodID
AND SAL_OrderDetail.OrderID=SAL_OrderHeader.OrderID
inner join INV_Item on INV_Item.ItemID=INV_StockTransactionDetail.ItemID --INV_Item.CompanyID=INV_StockTransactionDetail.CompanyID AND 
inner  join AR_SET_Customer CustName on CustName.CompanyID=SAL_DispatchHeader.CompanyID
AND CustName.PeriodID=SAL_DispatchHeader.PeriodID
AND CustName.CustomerID=SAL_DispatchHeader.CustomerID
left  join AR_SET_Customer on --AR_SET_Customer.CompanyID=SAL_DispatchHeader.CompanyID AND 
AR_SET_Customer.PeriodID=SAL_DispatchHeader.PeriodID
AND AR_SET_Customer.CustomerID=SAL_DispatchHeader.TransporterID
inner Join INV_SET_Category on --INV_SET_Category.CompanyID=SAL_OrderDetail.CompanyID
--AND 
INV_SET_Category.CategoryID=SAL_OrderDetail.ItemCategoryID

where [SAL_DispatchHeader].[Status] = 1 
) tab 
";

        const string Query_DispatchDetailReport = @" 
select SAL_DispatchHeader.DispatchNo, AR_SET_Customer.CustomerName, SAL_DispatchHeader.DispatchDate, AR_SET_Customer.CustomerID, SAL_DispatchHeader.TotalAmount 
from SAL_DispatchHeader 
    inner join AR_SET_Customer on --AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID And 
AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID 
        And AR_SET_Customer.CustomerID= SAL_DispatchHeader.CustomerID 
where SAL_DispatchHeader.CompanyID = @CompanyID And SAL_DispatchHeader.PeriodID = @PeriodID 
    {0} ";

        const string QueryUpdateForDispatch = @"UPDATE SAL_DispatchHeader
SET StatusCOde = @StatusCode,IsReversed=1,ReversalDate=GETDATE()
where SAL_DispatchHeader.DocumentTypeID=@DocumentTypeID AND SAL_DispatchHeader.DispatchNo=@DispatchNo
AND SAL_DispatchHeader.CompanyID = @CompanyID  AND SAL_DispatchHeader.PeriodID = @PeriodID";

        const string QueryUpdateForDispatchDOUpdateForm = @"UPDATE SAL_DispatchHeader
SET DispatchDate = @DispatchDate,TransporterID = @TransporterID , BiltyNo = @BiltyNo , BiltyAddress = @BiltyAddress , DriverName = @DriverName
,DriverCNIC = @DriverCNIC , CustomerID = @CustomerID
where  SAL_DispatchHeader.CompanyID = @CompanyID  
AND SAL_DispatchHeader.PeriodID = @PeriodID 
and SAL_DispatchHeader.DispatchID = @DispatchID";

        const string Query_DispatchDetailReportNew = @"Select Convert(int, SAL_DispatchHeader.DispatchNo) As DispatchNo, SAL_DispatchHeader.DocumentTypeID, SET_DocumentType.DocumentType, AR_SET_Customer.CustomerName, 

    SAL_DispatchHeader.DispatchDate, AR_SET_Customer.CustomerID, 
	SAL_DispatchHeader.VehicleNo, SAL_DispatchHeader.BiltyNo, SAL_DispatchHeader.BiltyAddress,
	(Case When SAL_DispatchHeader.ReturnID is not null then 
	        -INV_StockTransactionDetail.amount
	else
		SAL_DispatchHeader.TotalAmount
			end
		) as TotalAmount,

    (Case When SET_DocumentType.DocumentType like '%return%' then 
	   abs(INV_StockTransactionDetail.TransactionQuantity) * -1  
     else
     ABS(INV_StockTransactionDetail.TransactionQuantity)    
	end
    
    ) as Dispatch_TransactionQuantity,
    (Case When SAL_DispatchHeader.ReturnID is not null then
        -(INV_StockTransactionDetail.amount * isnull(SAL_OrderDetail.FEDAmount, 0)) / (100 + isnull(SAL_OrderDetail.FEDAmount, 0)) 
    else
        (SAL_DispatchHeader.TotalAmount * isnull(SAL_OrderDetail.FEDAmount, 0)) / (100 + isnull(SAL_OrderDetail.FEDAmount, 0)) 
    end
    ) as SAL_FEDAmount,
    (Case When SAL_DispatchHeader.ReturnID is not null then
		-(SAL_OrderDetail.GrossAmount)
	else
		SAL_OrderDetail.GrossAmount 
	end) as SalGrosssAmount ,
    (Case When SAL_DispatchHeader.ReturnID is not null then
    -SAL_OrderDetail.NetAmount 
    else
    SAL_OrderDetail.NetAmount 
    end)
    as SalNetAmount, 
    (Case When SAL_DispatchHeader.ReturnID is not null then
		-isnull(SAL_OrderDetail.FEDAmount, 0) 
		else
		isnull(SAL_OrderDetail.FEDAmount, 0)
		end )
    as SAL_FedRate ,
    INV_StockTransactionDetail.SourceCode, 
     DocumentTypesGroup.GroupName,
	 Agent.CustomerName as AgentName,
	 SAL_DispatchHeader.AgentDealerID,
	 SAL_DispatchHeader.GrossWeight,
	 SAL_DispatchHeader.TareWeight,
	 SAL_DispatchHeader.NetWeight,
	 SAL_DispatchHeader.GatePassNo,
	 SAL_DispatchHeader.TransporterID,
	 Transaporter.CustomerName as TransporterName,
	 SAL_DispatchHeader.CustomerCNICII as ManualCNIC,
	 SAL_DispatchHeader.CustomerNameII as ManualName,
     SAL_DispatchHeader.CustomerCityII as DriverCity,
     SAL_DispatchHeader.DriverContactNumber,
    Case Sal_DispatchHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
    When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus ,SAL_OrderHeader.TotalAmount as GrandTotal
,AR_SET_Customer.CityID,set_city.CityName, Set_Batch.Description as Batch
,SAL_OrderHeader.ContractNo as SaleOrderNo
    from SAL_DispatchHeader 
    
    inner join AR_SET_Customer on --AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID And 
AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID 
        And AR_SET_Customer.CustomerID= SAL_DispatchHeader.CustomerID and AR_SET_Customer.Status = 1 

    INNER JOIN INV_StockTransactionHeader StockHeader ON StockHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
        AND StockHeader.DocumentNo = SAL_DispatchHeader.DispatchNo AND StockHeader.CompanyID = SAL_DispatchHeader.CompanyID 
        AND StockHeader.PeriodID = SAL_DispatchHeader.PeriodID
		AND StockHeader.FiscalID = SAL_DispatchHeader.FiscalID
		 AND StockHeader.Status = 1 

    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = StockHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = StockHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = StockHeader.PeriodID 
		AND INV_StockTransactionDetail.FiscalID = StockHeader.FiscalID
        AND INV_StockTransactionDetail.Status = 1 

    Left JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        And Sal_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
		AND Sal_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
		--AND Sal_DeliveryOrderDetail.FiscalID = INV_StockTransactionDetail.FiscalID

    left join SAL_OrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID
		 And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
		 --And SAL_DeliveryOrderDetail.FiscalID = SAL_OrderDetail.FiscalID

    left Join SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderId AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
        AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID AND SAL_OrderHeader.Status = 1 
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID 
        And Set_Batch.BatchID = SAL_OrderHeader.Season
        --And Set_Batch.CompanyID = SAL_OrderHeader.CompanyID
    inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID and SET_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID 
        and SET_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID and SET_DocumentType.status = 1 
   LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
inner join Set_PeriodCustom on SAL_DispatchHeader.DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

    Left join AR_SET_Customer Transaporter on --AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID And 
Transaporter.PeriodID = SAL_DispatchHeader.PeriodID 
        And Transaporter.CustomerID= SAL_DispatchHeader.TransporterID and Transaporter.Status = 1 

		    Left join AR_SET_Customer Agent on --AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID And 
Agent.PeriodID = SAL_DispatchHeader.PeriodID 
        And Agent.CustomerID= SAL_DispatchHeader.AgentDealerID and Transaporter.Status = 1 

left join set_city on set_city.CityID = AR_SET_Customer.CityID

where SAL_DispatchHeader.CompanyID = @CompanyID And	SAL_DispatchHeader.PeriodID = @PeriodID 

    {0}
        ";

        const string PendingDispatchAsOf = @"
 select tab.CustomerName, tab.CustomerID,tab.DispatchNo,tab.DispatchDate,
tab.CategoryName,tab.Description,tab.InventoryItemStatusCode, Dispatch_Qty
, SUM(tab.InvoicedQty) as InvoicedQty
,Dispatch_Qty-SUM(InvoicedQty) as Balance,DispatchStatus

  from 
(
Select AR_SET_Customer.CustomerName, AR_SET_Customer.CustomerID,SAL_DispatchHeader.DispatchNo,SAL_DispatchHeader.DispatchDate,
INV_SET_Category.CategoryName,INV_Item.Description,INV_Item.InventoryItemStatusCode,Abs(INV_StockTransactionDetail.TransactionQuantity) AS Dispatch_Qty,
ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0) as InvoicedQty,isnull(Abs(INV_StockTransactionDetail.TransactionQuantity) - ABS(ISNULL([SAL_InvoiceDetail].[QuantityInvoiced], 0)), 0) as Balance, DocumentTypesGroup.GroupName
,case SAL_DispatchHeader.statusCode when 1 then 'Approved' when 5 then 'Invoice' else '' end as DispatchStatus
From SAL_DispatchHeader

INNER JOIN AR_SET_Customer on AR_SET_Customer.CustomerID=SAL_DispatchHeader.customerID --and AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID 
and AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID and AR_SET_Customer.Status = 1

INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1

INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = [INV_StockTransactionHeader].[StockTransactionID] AND  [INV_StockTransactionDetail].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1

LEFT JOIN [SAL_InvoiceDetail] ON [SAL_InvoiceDetail].[StockHeaderID] = [INV_StockTransactionDetail].[StockTransactionID] 
	  AND [SAL_InvoiceDetail].[StockDetailID] = [INV_StockTransactionDetail].[TransactionDetailID]
	  AND [SAL_InvoiceDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
	  AND [SAL_InvoiceDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] 
	  AND [SAL_InvoiceDetail].[Status] = 1
LEft JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.DOHeaderID = [INV_StockTransactionDetail].SourceHeaderID
		And Sal_DeliveryOrderDetail.DODetailID = [INV_StockTransactionDetail].SourceLineID
		AND Sal_DeliveryOrderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID 
		AND Sal_DeliveryOrderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID

INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1

INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_SET_Category].[Status] = 1

INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1

INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
 inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID and SET_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID 
        and SET_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID and SET_DocumentType.status = 1 
   LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
  --DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
   where SAL_DispatchHeader.CompanyID = @CompanyID and SAL_DispatchHeader.PeriodID = @PeriodID
  {0}  
 
)tab

  group by tab.CustomerName, tab.CustomerID,tab.DispatchNo,tab.DispatchDate,
tab.CategoryName,tab.Description,tab.InventoryItemStatusCode,DispatchStatus, Dispatch_Qty
";

        #endregion

        #region Class Methods

        //Query_VehicleStockStatus

        public static DataTable GetVehicleStockStatus(Nullable<int> company, Nullable<int> period, Nullable<DateTime> dateto)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@ToDate", dateto);




                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_VehicleStockStatus, sqlparams);

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

        public static DataTable GetTableCloumnsName(string TableName, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@TableName", TableName);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetTableCloumnsName, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetTableCloumnsName, sqlparams);

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

        public static DataTable GetDispatchDetailReportData(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                if (dateFrom.HasValue && dateTo.HasValue)
                    whereFilter = " And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";
                else if (dateFrom.HasValue)
                    whereFilter = " And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'";
                else if (dateTo.HasValue)
                    whereFilter = " And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";

                string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderHeader.Query_GetDataForDispatchDetailReport + whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable DispatchDetail(int companyID, int periodID, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentTypeID = null, Nullable<short> statusCode = null, Nullable<int> ContractID = null, Nullable<int> SourceCode = null, SqlTransaction objTransaction = null,
            short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhoo = false, bool IsHabibSugar = false, Nullable<int> BatchID = null, int GatePassNo = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (IsHabibSugar == true)
                    {
                        //whereFilter.Append(" And cast((FORMAT (SAL_DispatchHeader.DispatchDate, 'yyyy-MM-dd') + ' ' + FORMAT (SAL_DispatchHeader.CreateDate, 'hh:mm:ss')) as datetime) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " " + dateFrom.Value.ToString("hh:mm:ss") + " ' and '"
                        //+ DateUtil.ConvertToString(dateTo.Value, true) + " " + dateTo.Value.ToString("hh:mm:ss") + " ' ");
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }
                    else
                    {
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }

                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (customerID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.CustomerID = " + customerID);

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode.Value);

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ContractID > 0)
                {
                    if (IsSheikhoo == true)
                    {
                        whereFilter.Append(" AND SAL_ContractHeader.ContractNo = " + ContractID.Value);
                    }
                    else
                    {
                        whereFilter.Append(" AND SAL_OrderHeader.ContractNo = " + ContractID.Value);
                    }
                }
                if (SourceCode > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.SourceCode = " + SourceCode.Value);
                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);
                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);

                if (GatePassNo > 0)
                {
                    whereFilter.Append(" and SAL_DispatchHeader.GatePassNO = " + GatePassNo + "");
                }

                string query = string.Empty;
                if (IsSheikhoo == true)
                {
                    query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew_Shaikhu, whereFilter);
                }
                else
                {
                    query = string.Format(Query_DispatchDetailReportNew, whereFilter);
                }
                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable DispatchDetailRR(int companyID, int periodID, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null,
        Nullable<DateTime> dateTo = null, Nullable<int> documentTypeID = null, Nullable<short> statusCode = null, Nullable<int> ContractID = null, Nullable<int> SourceCode = null, SqlTransaction objTransaction = null,
        short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhoo = false, bool IsHabibSugar = false, Nullable<int> BatchID = null, Nullable<int> CostCenterID = null, Nullable<int> BrokerID = null, int GatePassNo = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (IsHabibSugar == true)
                    {
                        //whereFilter.Append(" And cast((FORMAT (SAL_DispatchHeader.DispatchDate, 'yyyy-MM-dd') + ' ' + FORMAT (SAL_DispatchHeader.CreateDate, 'hh:mm:ss')) as datetime) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " " + dateFrom.Value.ToString("hh:mm:ss") + " ' and '"
                        //+ DateUtil.ConvertToString(dateTo.Value, true) + " " + dateTo.Value.ToString("hh:mm:ss") + " ' ");
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }
                    else
                    {
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }

                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (customerID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.CustomerID = " + customerID);

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode.Value);

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ContractID > 0)
                {
                    if (IsSheikhoo == true)
                    {
                        whereFilter.Append(" AND SAL_ContractHeader.ContractNo = " + ContractID.Value);
                    }
                    else
                    {
                        whereFilter.Append(" AND SAL_OrderHeader.ContractNo = " + ContractID.Value);
                    }
                }
                if (SourceCode > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.SourceCode = " + SourceCode.Value);
                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);
                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);

                if (CostCenterID.HasValue)
                    whereFilter.Append(" and SAL_DispatchHeader.CostCentreID =  " + CostCenterID.Value);
                if (BrokerID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.BrokerID  = " + BrokerID);

                if (GatePassNo > 0)
                {
                    whereFilter.Append(" and SAL_DispatchHeader.GatePassNO = " + GatePassNo + "");
                }

                //if (DCNumber > 0)
                //{
                //    whereFilter.Append(" and SAL_DispatchHeader.DispatchNo = " + DCNumber + "");

                //}

                string query = string.Empty;
                if (IsSheikhoo == true)
                {
                    query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew_ShaikhuRR, whereFilter);
                }
                else
                {
                    query = string.Format(Query_DispatchDetailReportNew, whereFilter);
                }
                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable DispatchDetailforDispatch(int companyID, int periodID, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentTypeID = null, Nullable<short> statusCode = null, Nullable<int> ContractID = null, Nullable<int> SourceCode = null, SqlTransaction objTransaction = null,
            short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhoo = false, bool IsHabibSugar = false, Nullable<int> BatchID = null, int GatePassNo = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (IsHabibSugar == true)
                    {
                        //whereFilter.Append(" And cast((FORMAT (SAL_DispatchHeader.DispatchDate, 'yyyy-MM-dd') + ' ' + FORMAT (SAL_DispatchHeader.CreateDate, 'hh:mm:ss')) as datetime) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " " + dateFrom.Value.ToString("hh:mm:ss") + " ' and '"
                        //+ DateUtil.ConvertToString(dateTo.Value, true) + " " + dateTo.Value.ToString("hh:mm:ss") + " ' ");
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }
                    else
                    {
                        whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                            + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                    }

                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (customerID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.CustomerID = " + customerID);

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode.Value);

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ContractID > 0)
                {
                    if (IsSheikhoo == true)
                    {
                        whereFilter.Append(" AND SAL_ContractHeader.ContractNo = " + ContractID.Value);
                    }
                    else
                    {
                        whereFilter.Append(" AND SAL_OrderHeader.ContractNo = " + ContractID.Value);
                    }
                }
                if (SourceCode > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.SourceCode = " + SourceCode.Value);
                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);
                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);

                if (GatePassNo > 0)
                {
                    whereFilter.Append(" and Fabrics_GatePassForOutDoor.GatePassID = " + GatePassNo + "");
                }

                string query = string.Empty;
                if (IsSheikhoo == true)
                {
                    query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew_forDispatch, whereFilter);
                }
                else
                {
                    query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew_forDispatch, whereFilter);
                }
                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public DataTable DispatchReportCustomerWise(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            Nullable<int> customerID = null, Nullable<short> statusCode = null, Nullable<int> batchID = null, Nullable<int> itemID = null,
            Nullable<int> documentTypeID = null, string Query_DispatchReport_CustomerWise = "", short? documentGroupID = null, int CurrentFiscalID = 0, bool IsHabibSugar = false,
            string ColorByHazelScale = "")
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                sb.Append(" WHERE SAL_DispatchHeader.CompanyID = " + companyID + " and SAL_DispatchHeader.PeriodID =  " + periodID
                    + " And SAL_DispatchHeader.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerID.HasValue)
                    sb.Append(" And ASCT.CustomerID = " + customerID.Value);

                if (statusCode.HasValue)
                    sb.Append(" And SAL_DispatchHeader.StatusCode = " + statusCode.Value + "");
                else
                    sb.Append(" And SAL_DispatchHeader.StatusCode = " + 1 + "");

                if (batchID.HasValue)
                    sb.Append(" And SAL_OrderHeader.Season = '" + batchID.Value + "'");

                if (itemID.HasValue)
                    sb.Append(" And INV_Item.ItemID = " + itemID.Value);

                if (!string.IsNullOrEmpty(ColorByHazelScale))
                    sb.Append(" And SAL_DispatchHeader.ColorByHazelScale Like '%" + ColorByHazelScale + "%'");

                //if (documentTypeID.HasValue)
                //    sb.Append(" And SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";


                    if (IsHabibSugar)
                    {
                        sb.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + ")");
                    }
                    else
                    {
                        dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                        if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                        {
                            ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                        }
                        sb.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                    }
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DispatchReport_CustomerWise + sb.ToString()), sqlparams);

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

        public DataTable DispatchReportCustomerWise_Steel(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            Nullable<int> customerID = null, Nullable<short> statusCode = null, Nullable<int> batchID = null, Nullable<int> itemID = null,
            Nullable<int> documentTypeID = null, string Query_DispatchReport_CustomerWise_Steel = "", short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> CostCenterID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                sb.Append(" WHERE SAL_DispatchHeader.CompanyID = " + companyID + " and SAL_DispatchHeader.PeriodID =  " + periodID
                    + " And SAL_DispatchHeader.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerID.HasValue)
                    sb.Append(" And ASCT.CustomerID = " + customerID.Value);

                if (statusCode.HasValue)
                    sb.Append(" And SAL_DispatchHeader.StatusCode = " + statusCode.Value + "");

                if (batchID.HasValue)
                    sb.Append(" And SAL_OrderHeader.Season = '" + batchID.Value + "'");

                if (itemID.HasValue)
                    sb.Append(" And INV_Item.ItemID = " + itemID.Value);

                //if (documentTypeID.HasValue)
                //    sb.Append(" And SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (CostCenterID.HasValue)
                    sb.Append(" and SAL_DispatchHeader.CostCentreID =  " + CostCenterID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DispatchReport_CustomerWise_Steel + sb.ToString()), sqlparams);

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

        public DataTable DispatchReportCustomerWise_Ethanol(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
    Nullable<int> customerID = null, Nullable<short> statusCode = null, Nullable<int> batchID = null, Nullable<int> itemID = null,
    Nullable<int> documentTypeID = null, string Query_DispatchReport_CustomerWise = "", short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                sb.Append(" WHERE SAL_DispatchHeader.CompanyID = " + companyID + " and SAL_DispatchHeader.PeriodID =  " + periodID
                    + " And SAL_DispatchHeader.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '"
                        + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerID.HasValue)
                    sb.Append(" And ASCT.CustomerID = " + customerID.Value);

                if (statusCode.HasValue)
                    sb.Append(" And SAL_DispatchHeader.StatusCode = " + statusCode.Value + "");

                if (batchID.HasValue)
                    sb.Append(" And SAL_OrderHeader.Season = '" + batchID.Value + "'");

                if (itemID.HasValue)
                    sb.Append(" And INV_Item.ItemID = " + itemID.Value);

                //if (documentTypeID.HasValue)
                //    sb.Append(" And SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DispatchReport_CustomerWise + sb.ToString()), sqlparams);

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

        public static DataTable GetGoodsIssuesWithSalesTax(int companyID, int periodID, Nullable<DateTime> datefrom, Nullable<DateTime> dateto)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@FromDate", datefrom);
                sqlparams[1] = new SqlParameter("@ToDate", dateto);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GoodsIssuesWithSalesTax, sqlparams);

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

        public static DataTable GetSalesOrderFromDispatch(int companyID, int periodID, string dispatchNo, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds;
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@DispatchNo", dispatchNo);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetSalesOrderFromDispatch, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetSalesOrderFromDispatch, sqlparams);

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

        public static DataSet Get_DispatchVehicle(string dispatchNoFrom, string dispatchNoTo, int companyID, int periodID, int? itemCategoryID,
            Nullable<DateTime> date_From = null, Nullable<DateTime> date_To = null, Nullable<int> DocStatus = null)
        {
            string filter = "";
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                if (!string.IsNullOrEmpty(dispatchNoFrom) && !string.IsNullOrEmpty(dispatchNoTo))
                    filter = " AND Sal_Dispatchheader.DispatchNo between " + dispatchNoFrom + " and " + dispatchNoTo + " ";
                else if (!string.IsNullOrEmpty(dispatchNoFrom))
                    filter = " AND Sal_Dispatchheader.DispatchNo >= " + dispatchNoFrom + "";
                else
                    filter = "";

                if (date_From != null && date_To != null)
                {
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate between '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' and '"
                        + ((DateTime)date_To).ToString("MM-dd-yyyy") + "' ";
                }
                else if (date_From != null && date_To == null)
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate = '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' ";

                if (itemCategoryID.HasValue)
                    filter = filter + " And Sal_DOVehicleInformation.CategoryID = " + itemCategoryID;
                //filter = filter + " And INV_Item.ItemTypeID = " + itemCategoryID;


                if (DocStatus.HasValue)
                {
                    filter = filter + " AND Sal_Dispatchheader.StatusCode = " + DocStatus;
                }
                string finalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Get_DispatchVehicle.Replace("{filter}", filter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static int GetNewGatePassNo(int companyID, int periodID, int FiscalID, SqlTransaction objTransaction = null)
        {
            int GatePassNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);


                if (objTransaction == null)
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxGatePassNo, sqlparams));
                else
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxGatePassNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return GatePassNo;
        }

        public static int GetNewGatePassNoByTokenNo(int companyID, int periodID, int FiscalID, string DocumentTypeID, string TokenNo, SqlTransaction objTransaction = null)
        {
            int GatePassNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@TokenNo", TokenNo);


                if (objTransaction == null)
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxGatePassNoByTokenNo, sqlparams));
                else
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxGatePassNoByTokenNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return GatePassNo;
        }


        public static int GetGatePassNoByTokenNo(int companyID, int periodID, int FiscalID, string DocumentTypeID, string TokenNo, SqlTransaction objTransaction = null)
        {
            int GatePassNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@TokenNo", TokenNo);

                const string Query_GetGatePassNoByTokenNo = @" SELECT GatePassNo FROM SAL_DispatchHeader 
            WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and FiscalID = @FiscalID and DocumentTypeID = @DocumentTypeID and TokenNo = @TokenNo ";


                if (objTransaction == null)
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetGatePassNoByTokenNo, sqlparams));
                else
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetGatePassNoByTokenNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return GatePassNo;
        }

        public static int GetNewGatePassNoVehicle(int companyID, int periodID, int DocumentType, SqlTransaction objTransaction = null)
        {
            int GatePassNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@documentType", DocumentType);


                if (objTransaction == null)
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxGatePassNoVehicle, sqlparams));
                else
                    GatePassNo = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxGatePassNoVehicle, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return GatePassNo;
        }

        public static DataSet Get_DispatchVehicleOmni(string dispatchNoFrom, string dispatchNoTo, int companyID, int periodID, int? itemCategoryID,
          Nullable<DateTime> date_From = null, Nullable<DateTime> date_To = null, Nullable<int> DocStatus = null)
        {
            string filter = "";
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                if (!string.IsNullOrEmpty(dispatchNoFrom) && !string.IsNullOrEmpty(dispatchNoTo))
                    filter = " AND Sal_Dispatchheader.DispatchNo between " + dispatchNoFrom + " and " + dispatchNoTo + " ";
                else if (!string.IsNullOrEmpty(dispatchNoFrom))
                    filter = " AND Sal_Dispatchheader.DispatchNo >= " + dispatchNoFrom + "";
                else
                    filter = "";

                if (date_From != null && date_To != null)
                {
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate between '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' and '"
                        + ((DateTime)date_To).ToString("MM-dd-yyyy") + "' ";
                }
                else if (date_From != null && date_To == null)
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate = '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' ";

                if (itemCategoryID.HasValue)
                    filter = filter + " And [INV_SET_Category].CategoryID = " + itemCategoryID;
                //filter = filter + " And INV_Item.ItemTypeID = " + itemCategoryID;


                if (DocStatus.HasValue)
                {
                    filter = filter + " AND Sal_Dispatchheader.StatusCode = " + DocStatus;
                }
                string finalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Get_DispatchVehicleOmni.Replace("{filter}", filter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }



        public static DataTable GetDataByKeyWithOtherTables(int companyID, int periodID, int dispatchID, SqlConnection objConnection = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_PrimaryKey_WithOtherTables, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_PrimaryKey_WithOtherTables, sqlparams);

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

        public static DataTable GetDataByCustomerID(int companyID, int periodID, Nullable<int> customerID, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_CustomerID, PaymentType), sqlparams);


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

        public static DataTable GetDataByCustomerIDForReversal(int companyID, int periodID, Nullable<int> customerID, string strWhere, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_ToGet_DispatchForReversal, strWhere, PaymentType), sqlparams);


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

        public static DataTable GetDataForTareWeight(int companyID, int periodID, Nullable<int> customerID, string strWhere, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_ForTareWeight, strWhere, PaymentType), sqlparams);


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

        public static DataTable GetDataByCustomerIDFor_FIFO(int companyID, int periodID, Nullable<int> customerID, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_Select_By_CustomerID_ForLubricantReturn, PaymentType), sqlparams);


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

        public static DataTable GetDataForWholeMonth(int companyID, int periodID, int stockHeaderID, string paymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(paymentType))
                    paymentType = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockHeaderID", stockHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_WholeMonth_By_StockID, paymentType), sqlparams);

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

        public static DataTable GetDataByCustomerIDWithDO(int companyID, int periodID, Nullable<int> customerID, string strWhere = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_CustomerIDWithDO, strWhere), sqlparams);


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

        public static DataTable GetDataByCustomerIDForDO(int companyID, int periodID, Nullable<int> customerID, Nullable<int> DOID, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                if (DOID.HasValue)
                    sqlparams[3] = new SqlParameter("@DOID", DOID);
                else
                    sqlparams[3] = new SqlParameter("@DOID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_CustomerIDForDO, PaymentType), sqlparams);


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

        // Al-Haj
        public static DataTable GetDataByCustomerIDDispatch(int companyID, int periodID, Nullable<int> customerID, string PaymentType = "", bool isDCInvoice_Filter = false, string DocumentType = "")
        {
            DataTable dtToReturn = null;
            StringBuilder Sb_DCParts = new StringBuilder();
            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }


                if (isDCInvoice_Filter)
                {
                    Sb_DCParts.Append(" and tab.DocumentType like '%Parts%' ");

                    if (DocumentType.Contains("LCV"))
                        Sb_DCParts.Append(" and tab.DocumentType like '%LCV%'");
                    else if (DocumentType.Contains("HCV"))
                        Sb_DCParts.Append(" and tab.DocumentType like '%HCV%'");
                }
                else
                {
                    Sb_DCParts.Append(" and tab.DocumentType not like '%Parts%' ");
                    if (DocumentType.Contains("LCV"))
                        Sb_DCParts.Append(" and tab.DocumentType like '%LCV%'");
                    else if (DocumentType.Contains("HCV"))
                        Sb_DCParts.Append(" and tab.DocumentType like '%HCV%'");
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_Select_By_CustomerIDDispatch, PaymentType, Sb_DCParts.ToString()), sqlparams);


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

        public static DataTable GetDataByCustomerIDForGateEntry(int companyID, int periodID, Nullable<int> customerID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, GetDataByCustomerIDForGateEntryQuery, sqlparams);

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

        public bool InsertDHeaderAndDetail(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, List<SAL_DispatchVehicleInformation> ColVehicleStatus, SET_DocumentApprove da, bool IsAutoGenrated = false,
            string issuanceFlow = "", DataTable dt_StockAgainstItem = null, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertDispatch");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {
                this.DispatchNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.DispatchID = SAL_DispatchHeader.GetNewDispatchID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;

                this.Insert(sqltrans);

                #region For Stock Transaction Header & Detail

                sth.DocumentNo = int.Parse(this.DispatchNo);
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.FiscalID = CurrentFiscalID;
                sth.Insert(sqltrans);

                long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    std.StockTransactionID = sth.StockTransactionID;
                    std.TransactionDetailID = transactionDetailID++;
                    std.FiscalID = CurrentFiscalID;

                    std.Insert(sqltrans);
                    try
                    {
                        if (ColVehicleStatus != null)
                        {
                            var Records = from objDOVehicle in ColVehicleStatus
                                          where objDOVehicle.StockDetailID == std.SerialNo
                                          select objDOVehicle;

                            #region Vehicle Information
                            foreach (SAL_DispatchVehicleInformation sdvi in Records)
                            {
                                sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                sdvi.StockDetailID = std.TransactionDetailID++;
                                sdvi.StockHeaderID = sth.StockTransactionID;

                                sdvi.Insert(sqltrans);
                            }
                        }
                    }
                    catch { }
                            #endregion


                    #region Checking For Delivery Order Complete Status

                    if (std.SourceHeaderID.HasValue)
                        CheckQuantity(std.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #endregion

                if (issuanceFlow.Equals("FIFO"))
                    Update_GRN_For_FIFO_From_Dispatch(this.f_CompanyID, this.f_PeriodID, dt_StockAgainstItem, sqltrans);

                if (IsAutoGenrated == true)
                {
                    #region Auto Sales Invoice Generation

                    //AutoSalesInvoiceGeneration(sth, colSTDetail, sqltrans);

                    #endregion
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"
                /*
                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = int.Parse(f_DispatchNo);
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
                        td.SourceHeaderID = this.DispatchID;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }
                 */

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DispatchNo;
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
                //if (result)
                //    SqlHelper.CommitTransaction(sqltrans);
                //else
                //    SqlHelper.RollBackTransaction(sqltrans, "InsertDispatch");

                if (result)
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertDispatch");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public bool InsertDHeaderAndDetailWithSaleInvoiceHeder(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, List<SAL_DispatchVehicleInformation> ColVehicleStatus, SET_DocumentApprove da,
     SAL_InvoiceHeader ih, List<SAL_InvoiceDetail> colSInvDetail, List<Sal_Invoice_AccountsDetail> lst_Accounts,
            List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomers, GL_TransactionHeader th_Invoice, List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da_invoice,
         bool IsAutoGenrated = false, string issuanceFlow = "", DataTable dt_StockAgainstItem = null, bool IsCreditNoteForLubricant = false, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertDispatch");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {
                this.DispatchNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.DispatchID = SAL_DispatchHeader.GetNewDispatchID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;

                this.Insert(sqltrans);

                #region For Stock Transaction Header & Detail

                sth.DocumentNo = int.Parse(this.DispatchNo);
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.FiscalID = CurrentFiscalID;
                sth.Insert(sqltrans);

                long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);
                long RefrenceDetailIdForsaleDetail = transactionDetailID;

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    std.StockTransactionID = sth.StockTransactionID;
                    std.TransactionDetailID = transactionDetailID++;
                    std.FiscalID = CurrentFiscalID;

                    std.Insert(sqltrans);
                    try
                    {
                        if (ColVehicleStatus != null)
                        {
                            var Records = from objDOVehicle in ColVehicleStatus
                                          where objDOVehicle.StockDetailID == std.SerialNo
                                          select objDOVehicle;

                            #region Vehicle Information
                            foreach (SAL_DispatchVehicleInformation sdvi in Records)
                            {
                                sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                                sdvi.StockDetailID = std.TransactionDetailID++;
                                sdvi.StockHeaderID = sth.StockTransactionID;

                                sdvi.Insert(sqltrans);
                            }
                        }
                    }
                    catch { }
                            #endregion


                    #region Checking For Delivery Order Complete Status

                    if (std.SourceHeaderID.HasValue)
                        CheckQuantity(std.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #endregion

                if (issuanceFlow.Equals("FIFO"))
                    Update_GRN_For_FIFO_From_Dispatch(this.f_CompanyID, this.f_PeriodID, dt_StockAgainstItem, sqltrans);

                if (IsAutoGenrated == true)
                {
                    #region Auto Sales Invoice Generation

                    AutoSalesInvoiceGeneration(sth, colSTDetail, sqltrans);

                    #endregion
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In SaleInvoice and  GL_TransactionHeader
                    try
                    {
                        #region InvoiceHeader And Detail
                        //ih.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(ih.InvoiceType, ih.CompanyID, ih.PeriodID, sqltrans, CurrentFiscalID).ToString();
                        //ih.InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);
                        //ih.FiscalID = CurrentFiscalID;
                        //ih.Insert(sqltrans);

                        //short invoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(CompanyID, ih.InvoiceID, PeriodID, sqltrans));

                        //foreach (SAL_InvoiceDetail SId in colSInvDetail)
                        //{
                        //    SId.InvoiceHeaderID = ih.InvoiceID;
                        //    SId.InvoiceDetailID = invoiceDetailID++;
                        //    SId.StockHeaderID = sth.StockTransactionID;
                        //    SId.StockDetailID = RefrenceDetailIdForsaleDetail++;
                        //    SId.FiscalID = CurrentFiscalID;
                        //    SId.Insert(sqltrans);

                        //    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                        //        || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        //    {
                        //        INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(SId.StockDetailID.ToString()), null, sqltrans);
                        //        decimal ReversedQuantity = decimal.Parse(stdUpdate.ReversedQuantity == null ? "0" : stdUpdate.ReversedQuantity.ToString());

                        //        if (IsCreditNoteForLubricant)
                        //            stdUpdate.ReversedQuantity = ReversedQuantity + SId.QuantityInvoiced;

                        //        stdUpdate.UpdateDataByKey(sqltrans);
                        //    }

                        //    #region Checking For Dispatch Complete Status

                        //    CheckQuantity(SId.StockHeaderID, ih.CompanyID, ih.PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Invoice, ih.CreatedBy, ih.CreateDate, ih.CreatedByValue);

                        //    #endregion
                        //}

                        //#region Insert into Sales Invoice Accounts Detail
                        //if (lst_Accounts != null)
                        //{
                        //    foreach (Sal_Invoice_AccountsDetail obj_Account in lst_Accounts)
                        //    {
                        //        obj_Account.InvoiceHeaderID = ih.InvoiceID;
                        //        obj_Account.InvoiceDetailAccountID = (short)Sal_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(ih.CompanyID, ih.InvoiceID, ih.PeriodID,
                        //            sqltrans);

                        //        obj_Account.FiscalID = CurrentFiscalID;
                        //        obj_Account.Insert(sqltrans);
                        //    }
                        //}

                        //#endregion

                        //#region Insert into Sales Invoice Customers Detail
                        //if (lst_InvoiceCustomers != null)
                        //{
                        //    foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomer in lst_InvoiceCustomers)
                        //    {
                        //        obj_InvoiceCustomer.InvoiceHeaderID = ih.InvoiceID;
                        //        obj_InvoiceCustomer.InvoiceCustomerID = (short)Sal_InvoiceCustomerDetail.GetNewInvoiceCustomerID(ih.CompanyID, ih.InvoiceID, ih.PeriodID,
                        //            sqltrans);
                        //        obj_InvoiceCustomer.FiscalID = CurrentFiscalID;
                        //        obj_InvoiceCustomer.Insert(sqltrans);
                        //    }
                        //}
                        //#endregion
                        #endregion

                        #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            #region Insert In GL_TransactionHeader

                            th_Invoice.DocumentNo = int.Parse(ih.InvoiceNumber);
                            th_Invoice.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th_Invoice.FiscalID = CurrentFiscalID;
                            th_Invoice.Insert(sqltrans);

                            #endregion

                            #region Insert In GL_TransactionDetail

                            foreach (GL_TransactionDetail td in colTranDetail)
                            {

                                td.TransactionID = th_Invoice.TransactionID;
                                td.SourceHeaderID = ih.InvoiceID;
                                td.FiscalID = CurrentFiscalID;
                                td.Insert(sqltrans);
                            }

                            #endregion
                        }

                        #endregion

                        #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || ih.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                        {
                            da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(ih.CompanyID, ih.PeriodID, sqltrans);
                            da.DocumentNo = ih.InvoiceNumber;
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
                    #endregion
                }
                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DispatchNo;
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
                //if (result)
                //    SqlHelper.CommitTransaction(sqltrans);
                //else
                //    SqlHelper.RollBackTransaction(sqltrans, "InsertDispatch");

                if (result)
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertDispatch");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public SqlTransaction InsertDHeaderAndDetailPTIS(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, GL_TransactionHeader th,
          List<GL_TransactionDetail> colTranDetail, bool IsAutoGenrated = false,
          string issuanceFlow = "", int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans = sql;//SqlHelper.GetTransaction("InsertDispatch");

            try
            {
                //this.DispatchNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.DispatchID = SAL_DispatchHeader.GetNewDispatchID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;

                this.Insert(sqltrans);

                #region For Stock Transaction Header & Detail

                // sth.DocumentNo = int.Parse(this.DispatchNo);
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.FiscalID = CurrentFiscalID;
                sth.Insert(sqltrans);

                // long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    //  std.StockTransactionID = sth.StockTransactionID;
                    // std.TransactionDetailID = transactionDetailID++;
                    // std.FiscalID = CurrentFiscalID;

                    std.Insert(sqltrans);




                    #region Checking For Delivery Order Complete Status

                    if (std.SourceHeaderID.HasValue)
                        CheckQuantity(std.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #endregion

                // if (issuanceFlow.Equals("FIFO"))
                //    Update_GRN_For_FIFO_From_Dispatch(this.f_CompanyID, this.f_PeriodID, dt_StockAgainstItem, sqltrans);

                if (IsAutoGenrated == true)
                {
                    #region Auto Sales Invoice Generation

                    AutoSalesInvoiceGeneration(sth, colSTDetail, sqltrans);

                    #endregion
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    // th.DocumentNo = int.Parse(f_DispatchNo);
                    //th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    // th.FiscalID = CurrentFiscalID;
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
                        td.SourceHeaderID = this.DispatchID;
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
                {
                    //SqlHelper.CommitTransaction(sqltrans);
                }
                else
                {
                    sqltrans = null;
                    SqlHelper.RollBackTransaction(sqltrans, "InsertWO_Detail");
                }
            }

            return sqltrans;
        }



        public bool UpdateDHeaderAndDetail(SAL_DispatchHeader dh, INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colAddSTDetail,
            List<INV_StockTransactionDetail> colUpdateSTDetail, List<INV_StockTransactionDetail> colDeleteSTDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colAddTranDetail, List<SAL_DispatchVehicleInformation> colVehicleStatus, SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDispatch");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region For Stock Transaction Header & Detail

                sth.UpdateDataByKey(sqltrans);

                // for Insert
                foreach (INV_StockTransactionDetail stdAdd in colAddSTDetail)
                {
                    if (stdAdd.TransactionDetailID == -1)
                    {
                        stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);
                        stdAdd.Insert(sqltrans);

                        #region Checking For Delivery Order Complete Status

                        if (stdAdd.SourceHeaderID.HasValue)
                            CheckQuantity(stdAdd.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);
                    }
                        #endregion
                }

                #region Dispatch Vehicle Information Delete By Dispatch ID
                if (colVehicleStatus.Count > 0 && colUpdateSTDetail.Count > 0)
                {
                    SAL_DispatchVehicleInformation.DeleteDataByDispatchID(sqltrans, CompanyID, PeriodID, dh.DispatchID);
                }
                #endregion

                foreach (INV_StockTransactionDetail stdUpdate in colUpdateSTDetail)
                {
                    stdUpdate.UpdateDataByKey(sqltrans);

                    #region Dispatch Vehicle Information

                    var Records = from objDOVehicle in colVehicleStatus
                                  where objDOVehicle.StockDetailID == stdUpdate.SerialNo
                                  select objDOVehicle;
                    foreach (SAL_DispatchVehicleInformation sdvi in Records)
                    {
                        sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                        sdvi.StockHeaderID = sth.StockTransactionID;
                        sdvi.StockDetailID = stdUpdate.TransactionDetailID++;
                        sdvi.Insert(sqltrans);
                    }
                    #endregion


                    #region Checking For Delivery Order Complete Status

                    if (stdUpdate.SourceHeaderID.HasValue)
                        CheckQuantity(stdUpdate.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }



                foreach (INV_StockTransactionDetail ddDelete in colDeleteSTDetail)
                {
                    // INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ddDelete.TransactionDetailID, f_CompanyID, f_PeriodID);
                    ddDelete.UpdateDataByKey(sqltrans);
                }
                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    //th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    //th.Insert(sqltrans);

                    //#endregion

                    //#region Insert In GL_TransactionDetail


                    //decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    //decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    //if (AmountDebit != AmountCredit)
                    //    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    //foreach (GL_TransactionDetail td in colAddTranDetail)
                    //{
                    //    td.TransactionID = th.TransactionID;
                    //    td.Insert(sqltrans);
                    //}

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DispatchNo;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDispatch");
            }

            return result;
        }


        public bool UpdateDOHeaderAndDetail(SAL_DispatchHeader dh, INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colAddSTDetail,
           List<INV_StockTransactionDetail> colUpdateSTDetail, List<INV_LoadingPointDetail> colUpdateLoadingDetail, List<INV_StockTransactionDetail> colDeleteSTDetail, GL_TransactionHeader th,
           List<GL_TransactionDetail> colAddTranDetail, List<SAL_DispatchVehicleInformation> colVehicleStatus, SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDispatch");

            try
            {
                this.UpdateDataForDOUpdateForm(CompanyID, PeriodID, DispatchID, dh.DispatchDate, dh.DriverName, dh.DriverCNIC, dh.BiltyNo, dh.BiltyAddress, dh.CustomerID, (int)dh.TransporterID);

                #region For Stock Transaction Header & Detail

                sth.UpdateDataByKeyForDOUpdateForm(CompanyID, PeriodID, sth.StockTransactionID, sth.DocumentDate, sth.BusinessPartnerID);


                foreach (INV_StockTransactionDetail stdUpdate in colUpdateSTDetail)
                {
                    stdUpdate.UpdateDataForDOUpdate(sqltrans);

                }

                foreach (INV_LoadingPointDetail loadingddUpdate in colUpdateLoadingDetail)
                {

                    loadingddUpdate.UpdateDataForDOUpdate(CompanyID, PeriodID, (int)loadingddUpdate.DOHeaderID, (int)loadingddUpdate.SerialNo, loadingddUpdate.LoadingPointHeaderID, loadingddUpdate.ItemID, sqltrans);

                }

                foreach (INV_StockTransactionDetail ddDelete in colDeleteSTDetail)
                {
                    // INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ddDelete.TransactionDetailID, f_CompanyID, f_PeriodID);
                    ddDelete.UpdateDataByKey(sqltrans);
                }
                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    //th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    //th.Insert(sqltrans);

                    //#endregion

                    //#region Insert In GL_TransactionDetail


                    //decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    //decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    //if (AmountDebit != AmountCredit)
                    //    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    //foreach (GL_TransactionDetail td in colAddTranDetail)
                    //{
                    //    td.TransactionID = th.TransactionID;
                    //    td.Insert(sqltrans);
                    //}

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DispatchNo;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDispatch");
            }

            return result;
        }

        public bool UpdateDHeaderAndDetailWithSaleInvoice(SAL_DispatchHeader dh, INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colAddSTDetail,
    List<INV_StockTransactionDetail> colUpdateSTDetail, List<INV_StockTransactionDetail> colDeleteSTDetail, List<SAL_DispatchVehicleInformation> colVehicleStatus,
            SET_DocumentApprove da, SAL_InvoiceHeader Sal_ih, List<SAL_InvoiceDetail> colSInvDetail, List<Sal_Invoice_AccountsDetail> lst_ExpenseAccounts,
List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomers, GL_TransactionHeader th_SalInvoice, List<GL_TransactionDetail> colTranDetAdd_Invocie, SET_DocumentApprove da_salInvoice,
bool IsCreditNoteForLubricant = false, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDispatch");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region For Stock Transaction Header & Detail

                sth.UpdateDataByKey(sqltrans);

                // for Insert
                foreach (INV_StockTransactionDetail stdAdd in colAddSTDetail)
                {
                    if (stdAdd.TransactionDetailID == -1)
                    {
                        stdAdd.TransactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);
                        stdAdd.Insert(sqltrans);

                        #region Checking For Delivery Order Complete Status

                        if (stdAdd.SourceHeaderID.HasValue)
                            CheckQuantity(stdAdd.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);
                    }
                        #endregion
                }

                #region Dispatch Vehicle Information Delete By Dispatch ID
                if (colVehicleStatus.Count > 0 && colUpdateSTDetail.Count > 0)
                {
                    SAL_DispatchVehicleInformation.DeleteDataByDispatchID(sqltrans, CompanyID, PeriodID, dh.DispatchID);
                }
                #endregion

                foreach (INV_StockTransactionDetail stdUpdate in colUpdateSTDetail)
                {
                    stdUpdate.UpdateDataByKey(sqltrans);

                    #region Dispatch Vehicle Information

                    var Records = from objDOVehicle in colVehicleStatus
                                  where objDOVehicle.StockDetailID == stdUpdate.SerialNo
                                  select objDOVehicle;
                    foreach (SAL_DispatchVehicleInformation sdvi in Records)
                    {
                        sdvi.VehicleID = SAL_DispatchVehicleInformation.GetNewVehicleID(CompanyID, PeriodID, sqltrans);
                        sdvi.StockHeaderID = sth.StockTransactionID;
                        sdvi.StockDetailID = stdUpdate.TransactionDetailID++;
                        sdvi.Insert(sqltrans);
                    }
                    #endregion


                    #region Checking For Delivery Order Complete Status

                    if (stdUpdate.SourceHeaderID.HasValue)
                        CheckQuantity(stdUpdate.SourceHeaderID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Dispatch, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }



                foreach (INV_StockTransactionDetail ddDelete in colDeleteSTDetail)
                {
                    // INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, ddDelete.TransactionDetailID, f_CompanyID, f_PeriodID);
                    ddDelete.UpdateDataByKey(sqltrans);
                }
                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In Invoice Header and GL _TransactionHeader
                    //Sal_ih.InsertSInvHeaderAndDetail(colSInvDetail, lst_ExpenseAccounts, lst_InvoiceCustomers, th_SalInvoice, colTranDetAdd_Invocie, da_salInvoice, false, CurrentFiscalID);
                    try
                    {
                        #region Invoice Header And Detail
                        //Sal_ih.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(Sal_ih.InvoiceType, Sal_ih.CompanyID, Sal_ih.PeriodID, sqltrans, CurrentFiscalID).ToString();
                        //Sal_ih.InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);
                        //Sal_ih.FiscalID = CurrentFiscalID;
                        //Sal_ih.Insert(sqltrans);

                        //short invoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(Sal_ih.CompanyID, Sal_ih.InvoiceID, Sal_ih.PeriodID, sqltrans));

                        //foreach (SAL_InvoiceDetail SId in colSInvDetail)
                        //{
                        //    SId.InvoiceHeaderID = Sal_ih.InvoiceID;
                        //    SId.InvoiceDetailID = invoiceDetailID++;
                        //    SId.FiscalID = CurrentFiscalID;
                        //    SId.Insert(sqltrans);

                        //    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                        //        || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        //    {
                        //        INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(SId.StockDetailID.ToString()), null, sqltrans);
                        //        decimal ReversedQuantity = decimal.Parse(stdUpdate.ReversedQuantity == null ? "0" : stdUpdate.ReversedQuantity.ToString());

                        //        if (IsCreditNoteForLubricant)
                        //            stdUpdate.ReversedQuantity = ReversedQuantity + SId.QuantityInvoiced;

                        //        stdUpdate.UpdateDataByKey(sqltrans);
                        //    }

                        //    #region Checking For Dispatch Complete Status

                        //    CheckQuantity(SId.StockHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Invoice, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                        //    #endregion
                        //}

                        //#region Insert into Sales Invoice Accounts Detail
                        //if (lst_ExpenseAccounts != null)
                        //{
                        //    foreach (Sal_Invoice_AccountsDetail obj_Account in lst_ExpenseAccounts)
                        //    {
                        //        obj_Account.InvoiceHeaderID = Sal_ih.InvoiceID;
                        //        obj_Account.InvoiceDetailAccountID = (short)Sal_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(Sal_ih.CompanyID, Sal_ih.InvoiceID, Sal_ih.PeriodID,
                        //            sqltrans);

                        //        obj_Account.FiscalID = CurrentFiscalID;
                        //        obj_Account.Insert(sqltrans);
                        //    }
                        //}

                        //#endregion

                        //#region Insert into Sales Invoice Customers Detail
                        //if (lst_InvoiceCustomers != null)
                        //{
                        //    foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomer in lst_InvoiceCustomers)
                        //    {
                        //        obj_InvoiceCustomer.InvoiceHeaderID = Sal_ih.InvoiceID;
                        //        obj_InvoiceCustomer.InvoiceCustomerID = (short)Sal_InvoiceCustomerDetail.GetNewInvoiceCustomerID(this.CompanyID, Sal_ih.InvoiceID, this.PeriodID,
                        //            sqltrans);
                        //        obj_InvoiceCustomer.FiscalID = CurrentFiscalID;
                        //        obj_InvoiceCustomer.Insert(sqltrans);
                        //    }
                        //}
                        //#endregion

                        #endregion

                        #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            #region Insert In GL_TransactionHeader

                            th_SalInvoice.DocumentNo = int.Parse(this.DispatchNo);
                            th_SalInvoice.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                            th_SalInvoice.FiscalID = CurrentFiscalID;
                            th_SalInvoice.Insert(sqltrans);

                            #endregion

                            #region Insert In GL_TransactionDetail

                            foreach (GL_TransactionDetail td in colTranDetAdd_Invocie)
                            {

                                td.TransactionID = th_SalInvoice.TransactionID;
                                td.SourceHeaderID = Sal_ih.InvoiceID;
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
                            da.DocumentNo = Sal_ih.InvoiceNumber;
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

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DispatchNo;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDispatch");
            }

            return result;
        }

        public bool DeleteDHeaderAndDetail(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colDeleteSTDetail, GL_TransactionHeader th,
            List<GL_TransactionDetail> colDeleteTranDetail, bool IsVehicle = false)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteDispatch");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, DispatchID, f_PeriodID);
                if (IsVehicle == true)
                {
                    SAL_DispatchVehicleInformation.DeleteDataByDispatchID(sqltrans, CompanyID, PeriodID, f_DispatchID);
                }

                #region For Stock Transaction Header & Detail

                INV_StockTransactionHeader.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, sth.StockTransactionID);

                foreach (INV_StockTransactionDetail stdDelete in colDeleteSTDetail)
                    INV_StockTransactionDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, stdDelete.TransactionDetailID, f_CompanyID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteDispatch");
            }
            return result;
        }

        public bool UpdateDHeaderForApprove(INV_StockTransactionHeader stockHeader, GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail,
            SET_DocumentApprove dA, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDHeader");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);

                #region Update INV_StockTransactionHeader Set StatusCode

                stockHeader.FiscalID = CurrentFiscalID;
                stockHeader.UpdateDataByKey(sqltrans);

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    //th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    //th.FiscalID = CurrentFiscalID;
                    //th.Insert(sqltrans);

                    //#endregion

                    //#region Insert In GL_TransactionDetail


                    //decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    //decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    //if (AmountDebit != AmountCredit)
                    //    throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    //foreach (GL_TransactionDetail td in colAddTranDetail)
                    //{
                    //    td.TransactionID = th.TransactionID;
                    //    td.FiscalID = CurrentFiscalID;
                    //    td.Insert(sqltrans);
                    //}

                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDHeader");
            }

            return result;
        }

        public bool UpdateDHeaderAndSal_IhForApprove(INV_StockTransactionHeader stockHeader, SET_DocumentApprove dA, SAL_InvoiceHeader iheader, List<SAL_InvoiceDetail> colSInvDetail, List<Sal_Invoice_AccountsDetail> lst_Accounts,
            List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomers, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da,
             int CurrentFiscalID, bool IsCreditNoteForLubricant = false)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDHeader");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);

                #region Update INV_StockTransactionHeader Set StatusCode

                stockHeader.FiscalID = CurrentFiscalID;
                stockHeader.UpdateDataByKey(sqltrans);

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader
                    //iheader.InsertSInvHeaderAndDetail(colSInvDetail, lst_Accounts, lst_InvoiceCustomers, th_SalInvoice, colTranDetAdd_Invocie, da_salInvoice, false, CurrentFiscalID);
                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDHeader");
            }

            return result;
        }

        private void CheckQuantity(long deliveryOrderHeaderID, int companyID, int periodID, SqlTransaction sqltrans, SOEnumaration.SalesOrder_Stage closedCode,
            Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderHeaderID", deliveryOrderHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataTable dtDeliveryOrder = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_CheckQuantity, sqlparams).Tables[0];

                if (dtDeliveryOrder != null && dtDeliveryOrder.Rows.Count > 0)
                {
                    bool result = false;
                    SAL_DeliveryOrderHeader doh = new SAL_DeliveryOrderHeader();

                    foreach (DataRow dr in dtDeliveryOrder.Rows)
                    {
                        result = doh.UpdateFlagDetail(sqltrans, Convert.ToInt32(dr["DOHeaderID"]), Convert.ToInt32(dr["DODetailID"]), true, companyID, periodID, updatedBy, updateDate, updatedByValue, closedCode);

                        if (!result)
                            break;
                    }

                    if (result)
                    {
                        if (doh.CheckIsDetailCompleted(sqltrans, deliveryOrderHeaderID, true, companyID, periodID))
                            doh.UpdateFlagHeader(sqltrans, deliveryOrderHeaderID, true, companyID, periodID, updatedBy, updateDate, updatedByValue);
                    }
                    else
                        throw new BusinessLogicException("Error generated: SAL_DispatchHeader.cs, Line: 352, Function: DOH.UpdateFlagDetail");
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        private int GetBaseCurrency(SqlConnection objConnection = null)
        {
            int ID = 0;
            SET_Company objCompany;

            if (objConnection != null)
                objCompany = new SET_Company(this.CompanyID, objConnection);
            else
                objCompany = new SET_Company(this.CompanyID);

            if (objCompany.IsDataLoaded)
            {
                try
                {
                    ID = objCompany.OperatingUnitId_OperatingUnit.BaseCurrencyID;
                }
                catch { }
            }

            return ID;
        }

        private void AutoSalesInvoiceGeneration(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, SqlTransaction sqltrans = null)
        {
            DataTable dt = GetSalesOrderFromDispatch(this.CompanyID, this.PeriodID, this.DispatchNo, sqltrans);

            try
            {
                #region Setting Sales Invoice Header Instance

                SAL_InvoiceHeader ih = new SAL_InvoiceHeader()
                {
                    InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(this.CompanyID, this.PeriodID),
                    PeriodID = this.PeriodID,
                    CompanyID = this.CompanyID,
                    InvoiceType = 101, // Hard Code Filah Because ( Type Should Be Of Sales Invoice )
                    InvoiceNumber = this.f_DispatchNo,
                    InvoiceDate = DateTime.Parse(this.DispatchDate.ToString()),
                    AmountPaid = null,
                    InvoiceCurrencyID = GetBaseCurrency(),
                    GrossAmount = (this.TotalAmount),
                    Description = (!string.IsNullOrEmpty(sth.Comments)) ? sth.Comments : string.Empty,
                    CustomerID = this.CustomerID,
                    billedtoid = null,
                    TermsID = null,
                    VATCode = string.Empty,
                    FreightAmount = null,
                    AuthorizedBy = Guid.Empty,
                    PaymentStatusType = null,
                    StatusCode = 5, // In Process (HardCode) Will be approved or Reviewd From Sales Invoice Form
                    FreightCode = null,
                    Status = true,
                    CreatedBy = this.CreatedBy,
                    CreateDate = DateTime.Now,
                    CreatedByValue = this.CreatedByValue,
                };

                ih.Insert(sqltrans);

                #endregion

                #region Setting Sales Invoice Detail Instance
                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    DataRow dr = INV_Item.GetDataByKey(this.CompanyID, std.ItemID);
                    decimal Tax = 0;
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            if (!string.IsNullOrEmpty(dt.Rows[0]["SalesTaxAmount"].ToString()))
                            {
                                Tax += (decimal.Parse(dt.Rows[0]["SalesTaxAmount"].ToString()) * std.Amount) / 100;
                            }
                            if (!string.IsNullOrEmpty(dt.Rows[0]["ExciseDutyamount"].ToString()))
                            {
                                Tax += (decimal.Parse(dt.Rows[0]["ExciseDutyamount"].ToString()) * std.Amount) / 100;
                            }
                            if (!string.IsNullOrEmpty(dt.Rows[0]["FEDAmount"].ToString()))
                            {
                                Tax += (decimal.Parse(dt.Rows[0]["FEDAmount"].ToString()) * std.Amount) / 100;
                            }
                        }
                    }
                    SAL_InvoiceDetail inv = new SAL_InvoiceDetail()
                    {
                        PeriodID = this.PeriodID,
                        CompanyID = this.CompanyID,
                        InvoiceHeaderID = ih.InvoiceID,
                        InvoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(this.CompanyID, ih.InvoiceID, this.PeriodID, sqltrans)),
                        StockHeaderID = std.StockTransactionID,
                        StockDetailID = std.TransactionDetailID,
                        SerialNumber = std.SerialNo,
                        BaseAmount = std.Amount,
                        VehicleID = null,
                        QuantityInvoiced = Math.Abs((decimal)std.TransactionQuantity),
                        UnitPrice = (decimal)std.TransactionCost,
                        GrossAmount = std.Amount,
                        DiscountAmountTaken = null,
                        AmountIncludesTaxFlag = (dt.Rows.Count > 0) ? bool.Parse(dt.Rows[0]["IsSalesTaxExempted"].ToString()) : false,
                        TaxAmount = (dt.Rows.Count > 0) ? (!string.IsNullOrEmpty(dt.Rows[0]["SalesTaxAmount"].ToString()) ? (decimal?)decimal.Parse(dt.Rows[0]["SalesTaxAmount"].ToString()) : null) : null,
                        IsExciseDutyExempted = (dt.Rows.Count > 0) ? bool.Parse(dt.Rows[0]["IsExciseDutyExempted"].ToString()) : false,
                        ExciseDutyAmount = (dt.Rows.Count > 0) ? (!string.IsNullOrEmpty(dt.Rows[0]["ExciseDutyamount"].ToString()) ?
                        (decimal?)decimal.Parse(dt.Rows[0]["ExciseDutyamount"].ToString()) : null) : null,
                        IsFEDExempted = (dt.Rows.Count > 0) ? bool.Parse(dt.Rows[0]["IsFEDExempted"].ToString()) : false,
                        FEDAmount = (dt.Rows.Count > 0) ? (!string.IsNullOrEmpty(dt.Rows[0]["FEDAmount"].ToString()) ? (decimal?)decimal.Parse(dt.Rows[0]["FEDAmount"].ToString()) : null) : null,
                        NetAmount = std.Amount + Tax,
                        ItemCategoryID = int.Parse(dr["ItemTypeID"].ToString()),
                        ItemID = std.ItemID,
                        ItemUnitID = std.TransactionUOMID,
                        Inclusive = (dt.Rows.Count > 0) ? bool.Parse(dt.Rows[0]["Inclusive"].ToString()) : false,
                        Status = true,
                        CreatedBy = this.CreatedBy,
                        CreateDate = DateTime.Now,
                        CreatedByValue = this.CreatedByValue
                    };
                    inv.Insert(sqltrans);
                }
                #endregion
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

        }

        public bool UpdateFlagDetail(SqlTransaction sqltrans, int stockTransactionID, int transactionDetailID, bool status, int companyID, int periodID,
            Guid updatedBy, DateTime updateDate, string updatedByValue, SOEnumaration.SalesOrder_Stage closedCode, bool isDirectDispatch = false)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@TransactionDetailID", transactionDetailID);
                sqlparams[2] = new SqlParameter("@Status", status);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                sqlparams[4] = new SqlParameter("@PeriodID", periodID);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, Query_Update_Flag_Detail, sqlparams) > 0)
                {
                    result = true;

                    #region Checking For Delivery Order Complete Status
                    if (isDirectDispatch == false)
                    {
                        CheckQuantity(GetDeliveryOrderHeaderID(stockTransactionID, transactionDetailID, companyID, periodID, sqltrans), companyID, periodID, sqltrans, closedCode, updatedBy, updateDate, updatedByValue);
                    }
                    #endregion

                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        private static int GetDeliveryOrderHeaderID(int stockHeaderID, int stockDetailID, int companyID, int periodID, SqlTransaction sqltrans)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@StockDetailID", stockDetailID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans, CommandType.Text, Query_Get_DOHID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool CheckIsDetailCompleted(SqlTransaction objTransaction, int stockTransactionID, bool isGenerated, int companyID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@IsGenerated", isGenerated);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_CheckIsDetailCompleted, sqlparams)) == 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool UpdateFlagHeader(SqlTransaction objTransaction, int dispatchID, bool isGenerated, int companyID, int periodID, Guid updatedBy, DateTime updateDate,
            string updatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@IsGenerated", isGenerated);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag_Header, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable Dispatch_Note_Report(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Dispatch_Note + sb.ToString()), sqlparams);

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

        public static DataTable Dispatch_Note_ReportFor_SKU(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Dispatch_NoteFor_SKU + sb.ToString()), sqlparams);

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

        public static DataTable Dispatch_Note_ReportFor_RaniPur(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Dispatch_NoteFor_RaniPur + sb.ToString()), sqlparams);

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

        public static DataTable Dispatch_Note_HSM_Report(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Dispatch_Note_HSM + sb.ToString()), sqlparams);

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


        public static DataTable Dispatch_Note_Report_Polymer(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID)
        {
            DataTable dtToReturn = null;

            #region Query

            string query = @"
SELECT 
    Cast( SAL_DispatchHeader.DispatchDate as Date) DispatchDate, 
    SET_DocumentType.DocumentType, 
    SAL_DispatchHeader.DispatchNo, 
SET_Location.LocationName, 
AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1, 
SET_City.CityName, 
AR_SET_Customer.ZipCode, 
AR_SET_Customer.PhoneNumber, 
SAL_OrderHeader.ContractNo, 
(Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))) AS DONumber, 
INV_Item.Description AS ItemName, 
SET_UnitOfMeasure.UOMName, 
SAL_DeliveryOrderDetail.Quantity AS DOQuantity, 
INV_StockTransactionDetail.TransactionQuantity AS DispatchQty, 
ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DispatchHeader.StatusCode 
    and LookupHeaderID = 5),'') as DocumentStatus , 
[SET_OperatingUnit].Logoimage as LogoImage, Set_Batch.Description as Batch ,

isnull( (
        SELECT  DOD.Quantity - sum(abs(isnull(STD.TransactionQuantity,0)))
        FROM SAL_DispatchHeader DH
	        INNER JOIN INV_StockTransactionHeader STH ON STH.CompanyID = DH.CompanyID 
                AND STH.PeriodID = DH.PeriodID AND STH.DocumentTypeID = DH.DocumentTypeID 
                AND STH.DocumentNo = DH.DispatchNo AND STH.[Status] = 1 
	        INNER JOIN INV_StockTransactionDetail STD ON STD.CompanyID = STH.CompanyID 
                AND STD.PeriodID = STH.PeriodID 
                AND STD.StockTransactionID = STH.StockTransactionID AND STD.[Status] = 1 
	        LEFT JOIN SAL_DeliveryOrderDetail DOD ON DOD.CompanyID = STD.CompanyID 
                AND DOD.PeriodID = STD.PeriodID AND DOD.DOHeaderID = STD.SourceHeaderID 
                AND DOD.DODetailID = STD.SourceLineID AND DOD.[Status] = 1 
        WHERE DH.CompanyID = @CompanyID AND DH.PeriodID = @PeriodID AND DH.[Status] = 1 
        AND STD.SourceCode = (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))) AND DH.DispatchNo<SAL_DispatchHeader.DispatchNo
        group by DOD.Quantity
        )- abs(INV_StockTransactionDetail.TransactionQuantity),
     SAL_DeliveryOrderDetail.Quantity - abs(INV_StockTransactionDetail.TransactionQuantity)) AS DOBalance

,SAL_DispatchHeader.GrossWeight
,SAL_DispatchHeader.TareWeight
,SAL_DispatchHeader.NetWeight
,SAL_DispatchHeader.BiltyNo
,SAL_DispatchHeader.VehicleNo
,set_VehicleType.TypeName as VehicleName
,INV_StockTransactionHeader.Comments
,INV_Item.Length,INV_Item.Width,INV_Item.GSM,INV_Item.Color,SAL_OrderHeader.FreightDetail as PONo,
SAL_OrderHeader.Remarks as ContactPerson,INV_StockTransactionDetail.RackID as 'PackingBales',
SAL_DispatchHeader.BiltyAddress as DriverName,(abs(INV_StockTransactionDetail.TransactionQuantity)/INV_StockTransactionDetail.RackID) as 'NoOfBales',
Transpoter.CustomerName as TransporterName, 
SAL_OrderDetail.NoteToVendor,
SAl_DeliveryOrderHeader.ShipTo,
SAL_DispatchHeader.GatePassNo
FROM SAL_DispatchHeader 
	INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID AND INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
        AND INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo AND INV_StockTransactionHeader.Status = 1 
	INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID AND INV_StockTransactionDetail.Status = 1 
	left JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND SAL_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        AND SAL_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID AND SAL_DeliveryOrderDetail.Status = 1 
    Inner Join [SET_Company] ON [SET_Company].[CompanyId] = [SAL_DispatchHeader].[CompanyID]
    Inner Join [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [SET_Company].[OperatingUnitId]        
	left JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID AND SAL_OrderHeader.Status = 1 
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
	INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
	INNER JOIN INV_Item ON  INV_Item.ItemID = INV_StockTransactionDetail.ItemID --INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID AND
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DispatchHeader.LocationID 
	INNER JOIN AR_SET_Customer ON 
--AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID AND 
AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID 
        AND AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID 
	LEFT JOIN SET_City ON AR_SET_Customer.CityID = SET_City.CityID
    left join set_VehicleType on set_VehicleType.VehicleTypeID = SAL_DispatchHeader.VehicleTypeID

    left join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID AND SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID
								
	left join AR_SET_Customer Transpoter on Transpoter.CustomerID = SAL_dispatchheader.TransporterID
	and SAL_dispatchheader.CompanyID = Transpoter.CompanyID AND Transpoter.PeriodID = SAL_DispatchHeader.PeriodID
	
	left JOIN SAL_OrderDetail ON SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = sal_orderdetail.OrderID 
        AND SAL_OrderHeader.Status = 1 
        AND sal_orderheader.CustomerID = SAL_DispatchHeader.CustomerID 
WHERE SAL_DispatchHeader.CompanyID = @CompanyID AND SAL_DispatchHeader.PeriodID = @PeriodID AND SAL_DispatchHeader.Status = 1
";
            #endregion

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@DispatchNoFrom", dispatchNoFrom);
                //sqlparams[3] = new SqlParameter("@DispatchNoTo", dispatchNoTo);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query + sb.ToString()), sqlparams);

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

        public static DataTable GetGatePassDetailReport(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassDetail, sb.ToString()), sqlparams);

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


        public static DataTable GetGatePassMBSMReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM, sb.ToString()), sqlparams);

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


        public static DataTable GetGatePassMBSM_HSMReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
           Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                {
                    if (DispatchID > 0)
                        sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                }

                string FinalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_HSM;

                FinalQuery = FinalQuery.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());
                FinalQuery = FinalQuery.Replace("@CompanyID", companyID.ToString());
                FinalQuery = FinalQuery.Replace("@PeriodID", periodID.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FinalQuery, sb.ToString()), sqlparams);


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

        public static DataTable GetGatePassMBSM_NewReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
           Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                {
                    if (DispatchID > 0)
                        sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                }

                string FinalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_New;

                FinalQuery = FinalQuery.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());
                FinalQuery = FinalQuery.Replace("@CompanyID", companyID.ToString());
                FinalQuery = FinalQuery.Replace("@PeriodID", periodID.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FinalQuery, sb.ToString()), sqlparams);


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

        public static DataTable GatePassMBSM_TSMReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
           Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                {
                    if (DispatchID > 0)
                        sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                }

                string FinalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_TSM;

                FinalQuery = FinalQuery.Replace("@CurrentFiscalID", CurrentFiscalID.ToString());
                FinalQuery = FinalQuery.Replace("@CompanyID", companyID.ToString());
                FinalQuery = FinalQuery.Replace("@PeriodID", periodID.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FinalQuery, sb.ToString()), sqlparams);


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

        public static DataTable GetGatePassMBSM_RSMReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
           Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");
                else
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID IN (104,216)");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                {
                    if (DispatchID > 0)
                        sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_HSM, sb.ToString()), sqlparams);


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

        public static DataTable GetGatePassMBSM_RSM_MolassesReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
           Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");
                else
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID NOT IN (104,216)"); // Excluding Dispatch - Sugar and Dispatch - Sugar Export if document type not selected in filter

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                {
                    if (DispatchID > 0)
                        sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_HSM, sb.ToString()), sqlparams);


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

        public static DataTable GetGoodsIssueDetailReport(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> documentTypeID, Nullable<short> documentStatus, string orderBy,
            short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Number



                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom.Value + " AND " + dispatchNoTo.Value);
                else if (dispatchNoFrom.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo >= " + dispatchNoFrom.Value);
                else if (dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo <= " + dispatchNoTo.Value);

                #endregion

                #region Dispatch Date

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");
                }
                else if (dateFrom.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' ");
                else if (dateTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59' ");


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                #endregion

                #region Order By Clause

                string orderClause = string.Empty;

                switch (orderBy)
                {
                    case "SONumber":
                        orderClause = " Order By Cast(SAL_OrderHeader.ContractNo As int) ";
                        break;

                    case "DONumber":
                        orderClause = " Order By Cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "CustomerName":
                        orderClause = " Order By AR_SET_Customer.CustomerName ";
                        break;

                    case "DispatchDate":
                        orderClause = " Order By SAL_DispatchHeader.DispatchDate ";
                        break;
                }

                #endregion



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetGoodsIssueDetail, sb.ToString()) + orderClause, sqlparams);

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

        public static DataTable GetAllDataForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                    if (!IsDirectDispatchEnabled)
                    {
                        string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithPaging_ForApprove.Replace("{filter}", strWhere);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }
                    else
                    {
                        string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithPaging_ForApproveLeftJoin.Replace("{filter}", strWhere);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (!IsDirectDispatchEnabled)
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithoutPaging_ForApprove + strWhere
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }
                    else
                    {
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithoutPaging_ForApproveLeftJoin + strWhere
                            + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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

        public static DataTable GetAllDataForApproveDetail(int companyID, int periodID, int dHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DispatchID", dHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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


        public static DataTable GetAllDataDispatch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
       Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            const string Query_SelectAll_WithPagingDispatch = @" SELECT [SAL_DispatchHeader].*,Set_PeriodCustom.PeriodSetName, 
            ROW_NUMBER() Over (Order by [SAL_DispatchHeader].CompanyID, DispatchID, 
            [SAL_DispatchHeader].PeriodID) as 'Row' 
            FROM [SAL_DispatchHeader]
            inner join Set_PeriodCustom on [SAL_DispatchHeader].DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
            and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID ";

            const string Query_SelectAll_WithoutPagingDispatch = @"
                SELECT [SAL_DispatchHeader].*,Set_PeriodCustom.PeriodSetName FROM [SAL_DispatchHeader] 
                inner join Set_PeriodCustom on [SAL_DispatchHeader].DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
                and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID
            ";

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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingDispatch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingDispatch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingDispatch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingDispatch + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int dHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DispatchID", dHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_Select_DataByKey_ForApprove, sqlparams);

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

        public static DataRow GetRefrenceNoByDispatchNo(int companyID, int DispatchNo, int periodID, SqlConnection objConnection = null, int DocumentTypeID = 0, int CurrentFiscalID = 0)
        {
            DataRow dr = null;
            string sb = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", DispatchNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                if (DocumentTypeID > 0)
                    sb = " AND [SAL_DispatchHeader].DocumentTypeID=@DocumentTypeID";
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNoByKey + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRefrenceNoByKey + sb.ToString(), sqlparams);

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

        public static DataRow GetCostCenterByDispatchStockID(int companyID, int StockTransactionID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string query = @"SELECT SET_COSTCENTER.CostCenterName , SET_COSTCENTER.CostCenterID  from 

                    SAL_DispatchHeader 
                        INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
                            AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
                            AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] 
                            AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
                            AND [INV_StockTransactionHeader].[Status] = 1 
                            AND [INV_StockTransactionHeader].StockTransactionID = @StockTransactionID
                         LEFT JOIN SET_COSTCENTER ON SET_COSTCENTER.COSTCENTERID = SAL_DispatchHeader.CostCentreID
                        WHERE SAL_DispatchHeader.CompanyID = @CompanyID and SAL_DispatchHeader.PeriodID = @PeriodID
                ";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataRow GetRefrenceNoByDONo(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DeliveryOrderNo", orderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNoByDONo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRefrenceNoByDONo, sqlparams);

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

        public static DataRow GetDataByDispatchNO(int companyID, int dispatchID, int periodID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", dispatchID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byDispatchNo, sqlparams);

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

        public static DataTable GetDataByInvoiceID(int companyID, int periodID, int invoiceID, int customerID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@CustomerID", customerID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByInvoiceID, sqlparams);

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

        public static DataTable GetDataForDispatchOrderReportCustomerWise(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> customerName = null, Nullable<int> statusCode = null, Nullable<int> batchID = null, Nullable<int> itemID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                sb.Append(" WHERE SAL_OrderDetail.CompanyID = " + companyID + " and SAL_OrderDetail.PeriodID =  " + periodID + " And SAL_OrderDetail.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion
                if (customerName > 0)
                    sb.Append(" And ASCT.CustomerID = " + customerName);

                if (statusCode > 0)
                    sb.Append(" And SAL_OrderHeader.StatusCode = " + statusCode + "");

                if (batchID > 0)
                    sb.Append(" And SAL_OrderHeader.Season = '" + batchID + "'");

                if (itemID > 0)
                    sb.Append(" And INV_Item.ItemID = " + itemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDispatchOrderDetail_CustomerWiseReport + sb.ToString()));

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

        public static DataTable GetAllDataWithSalesOrder(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();

                    string strGroupBy = "";

                    DataTable dtcols = GetTableCloumnsName("SAL_DispatchHeader");
                    if (dtcols != null)
                    {
                        if (dtcols.Rows.Count > 0)
                        {
                            foreach (DataRow dr in dtcols.Rows)
                            {
                                strGroupBy += " [SAL_DispatchHeader]." + dr["Column"].ToString() + ",";
                            }
                        }
                    }

                    if (!IsDirectDispatchEnabled)
                    {
                        string query = "";
                        query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithPagingInner.Replace("{filter}", strWhere);
                        query = query.Replace("{GroupBy}", strGroupBy);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }
                    else
                    {
                        string query = "";
                        query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithPagingLeft.Replace("{filter}", strWhere);
                        query = query.Replace("{GroupBy}", strGroupBy);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    }

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (!IsDirectDispatchEnabled)
                    {
                        string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithoutPagingInner.Replace("{filter}", strWhere);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    }
                    else
                    {
                        string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAll_WithoutPagingLeft.Replace("{filter}", strWhere);

                        dt = SqlHelper.ExecuteDataset(CommandType.Text, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
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

        public static DataTable Get_DailySaleReport(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> categoryID = null, Nullable<int> doctypeID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (categoryID.HasValue)
                    additionalFilter = additionalFilter + " And INV_Item.ItemTypeID = " + categoryID;

                if (doctypeID.HasValue)
                    additionalFilter = additionalFilter + " And Set_DocumentType.DocumentTypeID = " + doctypeID;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DailySaleReport + additionalFilter, sqlparams);

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

        public static DataTable Get_OmniDispatchOrderReport(int companyID, int periodID, Nullable<int> CustomerID = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> itemCategoryID = null, string DocType = "", string whereId = "", bool IsCancelDispatch = false)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (CustomerID.HasValue)
                {
                    additionalFilter = additionalFilter + " AND [AR_SET_Customer].[CustomerID] =" + CustomerID;
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (itemCategoryID.HasValue)
                    additionalFilter = additionalFilter + " And INV_SET_Category.CategoryID = " + itemCategoryID;

                if (!string.IsNullOrEmpty(DocType))
                    additionalFilter = additionalFilter + " And Set_DocumentType.DocumentType LIke '%" + DocType + "%' ";


                string Query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_FAWDispatchOrderReport + additionalFilter + whereId;


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

        public static DataTable GetDataByKeyWithOtherTablesForFind(int dispatchID, int companyID, int periodID, SqlConnection objConnection = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportForFind, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportForFind, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTablesForFindFor_SKH(int dispatchID, int companyID, int periodID, SqlConnection objConnection = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportForFindFor_SKH, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportForFindFor_SKH, sqlparams);

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

        public static DataTable GetDataforDateWiseLifting(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, string menupath, int batchId, int FromCustomer, int ToCustomer, int DocumentTypeID, Nullable<int> BatchID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (fromDate != null && toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                else if (fromDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "'";
                else if (toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                if (FromCustomer > -1 && ToCustomer > -1)
                    additionalFilter = additionalFilter + " AND AR_SET_Customer.CustomerID between " + FromCustomer + " AND " + ToCustomer;



                if (DocumentTypeID > 0)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, DocumentTypeID, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";

                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }


                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID in (" + DocumentTypeID + "," + ReturnTypeID + ")";
                }

                //if (DocumentTypeID > 0)
                //    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID = " + DocumentTypeID;




                if (BatchID.HasValue)
                    sb.Append(" and [SET_Batch].BatchID =  " + BatchID);


                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter , sb.ToString()), sqlparams);

                string finalQuery = String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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

        public static DataTable GetDataforAgentWiseLifting(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, string menupath, int batchId, int FromAgent, int ToAgent, int DocumentTypeID, Nullable<int> BatchID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (fromDate != null && toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                else if (fromDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "'";
                else if (toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                if (FromAgent > -1 && ToAgent > -1)
                    additionalFilter = additionalFilter + " AND AR_SET_Customer.BrokerID between " + FromAgent + " AND " + ToAgent;



                if (DocumentTypeID > 0)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, DocumentTypeID, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";

                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }

                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID in (" + DocumentTypeID + "," + ReturnTypeID + ")";
                }

                //if (DocumentTypeID > 0)
                //    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID = " + DocumentTypeID;

                if (BatchID.HasValue)
                    sb.Append(" and [SET_Batch].BatchID =  " + BatchID);

                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter , sb.ToString()), sqlparams);

                string finalQuery = String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetAgentWiseLiftingReport + additionalFilter + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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


        public static DataTable GetDataforDateWiseLiftingTransporterWise(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, string menupath, int batchId, int FromCustomer, int ToCustomer, int DocumentTypeID, Nullable<int> BatchID = null, Nullable<int> TransporteID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (fromDate != null && toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                else if (fromDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "'";
                else if (toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                if (FromCustomer > -1 && ToCustomer > -1)
                    additionalFilter = additionalFilter + " AND AR_SET_Customer.CustomerID between " + FromCustomer + " AND " + ToCustomer;



                if (DocumentTypeID > 0)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, DocumentTypeID, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";

                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }


                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID in (" + DocumentTypeID + "," + ReturnTypeID + ")";
                }


                if (BatchID.HasValue)
                    sb.Append(" and [SET_Batch].BatchID =  " + BatchID);



                if (TransporteID.HasValue)
                    sb.Append(" and [SAL_DispatchHeader].TransporterID =  " + TransporteID);

                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter , sb.ToString()), sqlparams);

                string finalQuery = String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingTransporterReport + additionalFilter + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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




        public static DataTable GetDataforDateWiseLifting_PS(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, string menupath, int batchId, int FromCustomer, int ToCustomer, int DocumentTypeID, Nullable<int> BatchID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (fromDate != null && toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                else if (fromDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "'";
                else if (toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                if (FromCustomer > -1 && ToCustomer > -1)
                    additionalFilter = additionalFilter + " AND AR_SET_Customer.CustomerID between " + FromCustomer + " AND " + ToCustomer;



                if (DocumentTypeID > 0)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, DocumentTypeID, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";

                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }


                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID in (" + DocumentTypeID + "," + ReturnTypeID + ")";
                }

                //if (DocumentTypeID > 0)
                //    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID = " + DocumentTypeID;




                if (BatchID.HasValue)
                    sb.Append(" and [SET_Batch].BatchID =  " + BatchID);

                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter , sb.ToString()), sqlparams);

                string finalQuery = String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport_PS + additionalFilter + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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

        public static DataTable DeliveryChallanVise_Report(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, Nullable<int> customerName = null, Nullable<int> itemID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Approval Date filter

                if (fromDate != null && toDate != null)
                    sb.Append(" and [SAL_DispatchHeader].[DispatchDate] Between '" + ((DateTime)fromDate).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)toDate).ToString("yyyy-MM-dd") + "'");
                else if (fromDate != null)
                    sb.Append(" and Cast([SAL_DispatchHeader].[DispatchDate]) >= '" + ((DateTime)fromDate).ToString("yyyy-MM-dd") + "'");
                else if (toDate != null)
                    sb.Append(" and Cast([SAL_DispatchHeader].[DispatchDate]) <= '" + ((DateTime)toDate).ToString("yyyy-MM-dd") + "'");

                #endregion
                if (customerName > 0)
                    sb.Append(" AND SAL_DispatchHeader.CustomerID = " + customerName);

                if (itemID > 0)
                    sb.Append(" AND INV_StockTransactionDetail.ItemID = " + itemID);


                string finalQuery = String.Format(Query_DeliveryChallanVise_Report + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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

        public static DataTable CustomerViseMonthly_Report(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, Nullable<int> customerName = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Approval Date filter

                if (fromDate != null && toDate != null)
                    sb.Append(" where tab.[DispatchDate] Between '" + ((DateTime)fromDate).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)toDate).ToString("yyyy-MM-dd") + "'");
                else if (fromDate != null)
                    sb.Append(" where tab.[DispatchDate]) >= '" + ((DateTime)fromDate).ToString("yyyy-MM-dd") + "'");
                else if (toDate != null)
                    sb.Append(" where tab.[DispatchDate]) <= '" + ((DateTime)toDate).ToString("yyyy-MM-dd") + "'");

                #endregion
                if (customerName > 0)
                    sb.Append(" AND tab.CustomerID = " + customerName);


                string finalQuery = String.Format(Query_CustomerViseMonthly_Report + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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

        public static DataTable VehicleGatePass(int companyID, int periodID, Nullable<int> categoryID, string gatePassNoFrom, string gatePassNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> Pk_ID = null)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                if (categoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));



                sqlParams.Add(new SqlParameter("@GatePassFrom", gatePassNoFrom));
                sqlParams.Add(new SqlParameter("@GatePassTo", gatePassNoTo));

                if ((!string.IsNullOrEmpty(gatePassNoFrom)) && (!string.IsNullOrEmpty(gatePassNoTo)))
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.GatePassNo BETWEEN @GatePassFrom AND @GatePassTo ");

                #region Date range filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate BETWEEN @DateFrom AND @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }
                else if (dateFrom.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate >= @DateFrom ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                }
                else if (dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate <= @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }

                #endregion


                if (Pk_ID.HasValue)
                    additionalQueryParameters.Append(" AND SAL_DispatchHeader.DispatchID = " + Pk_ID + "  ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_VehicleGatePassReport, additionalQueryParameters.ToString()), sqlParams.ToArray());

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

        public static DataTable GetDataForInvoiceAndDispatchReport(int companyID, int periodID, int? categoryID, Nullable<DateTime> Date_From = null, Nullable<DateTime> Date_To = null)
        {
            string filter = string.Empty;
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", ((DateTime)Date_From).ToString("MM-dd-yyyy"));
                sqlparams[3] = new SqlParameter("@ToDate", ((DateTime)Date_To).ToString("MM-dd-yyyy"));
                //sqlparams[4] = new SqlParameter("@CategoryID", categoryID);
                //sb.Append("AND [INV_SET_Category].CategoryID = " + categoryID);

                if (categoryID != null)
                    filter = filter + " And [INV_SET_Category].CategoryID = " + categoryID;


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_INvoiceDispatch, filter), sqlparams);

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

        public static DataSet VehicleGatePassAlhaj(int companyID, int periodID, Nullable<int> categoryID, string gatePassNoFrom, string gatePassNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> Pk_ID = null, Nullable<int> doctypeid = null)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            DataTable dtToReturn = null;
            DataSet dsret = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                if (categoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));



                sqlParams.Add(new SqlParameter("@GatePassFrom", gatePassNoFrom));
                sqlParams.Add(new SqlParameter("@GatePassTo", gatePassNoTo));



                additionalQueryParameters.Append(" AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid);


                if ((!string.IsNullOrEmpty(gatePassNoFrom)) && (!string.IsNullOrEmpty(gatePassNoTo)))
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.GatePassNo BETWEEN @GatePassFrom AND @GatePassTo ");

                #region Date range filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate BETWEEN @DateFrom AND @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }
                else if (dateFrom.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate >= @DateFrom ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                }
                else if (dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate <= @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }

                #endregion


                if (Pk_ID.HasValue)
                    additionalQueryParameters.Append(" AND SAL_DispatchHeader.DispatchID = " + Pk_ID + "  ");

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_VehicleGatePassReport, additionalQueryParameters.ToString()), sqlParams.ToArray());
                dsret = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_VehicleGatePassReport, additionalQueryParameters.ToString()), sqlParams.ToArray());

                //if (ds != null && ds.Tables.Count > 0)
                //   dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsret;
            //return dtToReturn;
        }

        public static DataSet VehicleGatePassOmni(int companyID, int periodID, Nullable<int> categoryID, string gatePassNoFrom, string gatePassNoTo,
          Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> Pk_ID = null)
        {
            StringBuilder additionalQueryParameters = new StringBuilder();
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            DataTable dtToReturn = null;
            DataSet dsret = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                if (categoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));



                sqlParams.Add(new SqlParameter("@GatePassFrom", gatePassNoFrom));
                sqlParams.Add(new SqlParameter("@GatePassTo", gatePassNoTo));



                // additionalQueryParameters.Append(" AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid);


                if ((!string.IsNullOrEmpty(gatePassNoFrom)) && (!string.IsNullOrEmpty(gatePassNoTo)))
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.GatePassNo BETWEEN @GatePassFrom AND @GatePassTo ");

                #region Date range filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate BETWEEN @DateFrom AND @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }
                else if (dateFrom.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate >= @DateFrom ");
                    sqlParams.Add(new SqlParameter("@DateFrom", DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00"));
                }
                else if (dateTo.HasValue)
                {
                    additionalQueryParameters.Append(" AND Sal_DispatchHeader.DispatchDate <= @DateTo ");
                    sqlParams.Add(new SqlParameter("@DateTo", DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59"));
                }

                #endregion


                if (Pk_ID.HasValue)
                    additionalQueryParameters.Append(" AND SAL_DispatchHeader.DispatchID = " + Pk_ID + "  ");

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_VehicleGatePassReport, additionalQueryParameters.ToString()), sqlParams.ToArray());
                dsret = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_OmniGatePass, additionalQueryParameters.ToString()), sqlParams.ToArray());

                //if (ds != null && ds.Tables.Count > 0)
                //   dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsret;
            //return dtToReturn;
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
        #endregion

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
                sqlparams[3] = new SqlParameter("@DispatchNo", stocktrnID);
                sqlparams[4] = new SqlParameter("@StatusCode", StatusCode);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, QueryUpdateForDispatch, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, QueryUpdateForDispatch, sqlparams) > 0)
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

        public bool UpdateDataForDOUpdateForm(int Companyid, int perioid, int Dispatchid, DateTime dispatchdate, string drivername, string drivercnic, string biltyno, string biltyadd, int customerid, int transporterid)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DispatchID", Dispatchid);
                sqlparams[3] = new SqlParameter("@DispatchDate", dispatchdate);
                sqlparams[4] = new SqlParameter("@TransporterID", transporterid);
                sqlparams[5] = new SqlParameter("@BiltyNo", biltyno);
                sqlparams[6] = new SqlParameter("@BiltyAddress", biltyadd);
                sqlparams[7] = new SqlParameter("@DriverName", drivername);
                sqlparams[8] = new SqlParameter("@DriverCNIC", drivercnic);
                sqlparams[9] = new SqlParameter("@CustomerID", customerid);



                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, QueryUpdateForDispatchDOUpdateForm, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, QueryUpdateForDispatchDOUpdateForm, sqlparams) > 0)
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

        public static DataSet Get_DispatchVehicle1(string dispatchNoFrom, string dispatchNoTo, int companyID, int periodID, int? itemCategoryID,
            Nullable<DateTime> date_From = null, Nullable<DateTime> date_To = null, Nullable<int> DocStatus = null)
        {
            string filter = "";
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                if (!string.IsNullOrEmpty(dispatchNoFrom) && !string.IsNullOrEmpty(dispatchNoTo))
                    filter = " AND Sal_Dispatchheader.DispatchNo between " + dispatchNoFrom + " and " + dispatchNoTo + " ";
                else if (!string.IsNullOrEmpty(dispatchNoFrom))
                    filter = " AND Sal_Dispatchheader.DispatchNo >= " + dispatchNoFrom + "";
                else
                    filter = "";

                if (date_From != null && date_To != null)
                {
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate between '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' and '"
                        + ((DateTime)date_To).ToString("MM-dd-yyyy") + "' ";
                }
                else if (date_From != null && date_To == null)
                    filter = filter + " AND Sal_Dispatchheader.DispatchDate = '" + ((DateTime)date_From).ToString("MM-dd-yyyy") + "' ";

                if (itemCategoryID.HasValue)
                    filter = filter + " And INV_SET_Category.CategoryID = " + itemCategoryID;
                //filter = filter + " And INV_Item.ItemTypeID = " + itemCategoryID;


                if (DocStatus.HasValue)
                {
                    filter = filter + " AND Sal_Dispatchheader.StatusCode = " + DocStatus;
                }
                string finalQuery = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Get_DispatchVehicle1.Replace("{filter}", filter);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        #region Helper Methods

        public static void Update_GRN_For_FIFO_From_Dispatch(int companyID, int periodID, DataTable dt_StockAgainstItem, SqlTransaction objTransaction = null)
        {
            foreach (DataRow drUpdatedStock in dt_StockAgainstItem.Rows)
            {
                decimal issuedQuantity = Convert.ToDecimal((drUpdatedStock["IssuedQuantity"] != null && drUpdatedStock["IssuedQuantity"] != DBNull.Value
                    && drUpdatedStock["IssuedQuantity"] != string.Empty) ? drUpdatedStock["IssuedQuantity"].ToString() : "0");

                bool isIssued = (drUpdatedStock["IsIssued"] != null && drUpdatedStock["IsIssued"] != DBNull.Value && drUpdatedStock["IsIssued"] != string.Empty)
                    ? Convert.ToBoolean(drUpdatedStock["IsIssued"]) : false;

                INV_StockTransactionDetail.UpdateIssuedQuantity(companyID, periodID, int.Parse(drUpdatedStock["StockTransactionID"].ToString()),
                    int.Parse(drUpdatedStock["TransactionDetailID"].ToString()), issuedQuantity);

                if (isIssued)
                {
                    INV_StockTransactionDetail.UpdateIsIssued(companyID, periodID, int.Parse(drUpdatedStock["StockTransactionID"].ToString()),
                        int.Parse(drUpdatedStock["TransactionDetailID"].ToString()), true);
                }
            }
        }

        #endregion

        public static DataTable GetPendingDispatchAsOf_Report(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, Nullable<int> customerIDFrom, Nullable<int> customerIDTo,
            Nullable<int> documentGroupID = null, Nullable<int> documentType = null, bool? IsPending = false)
        {
            DataTable dt_ToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            StringBuilder AddParameters = new StringBuilder();

            #region Order By Clause

            string orderByClause = string.Empty;

            ////switch (orderBy)
            ////{

            ////    case "CustomerName":
            ////        orderByClause = " Order By tab.CustomerName ";
            ////        break;
            ////}

            #endregion


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Filter by Dispatch  Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                    additionalFilter.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN @FromDate AND @ToDate ");
                }
                else if (fromDate.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value);
                    additionalFilter.Append(" AND SAL_DispatchHeader.DispatchDate  >= @FromDate ");
                }
                else if (toDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@ToDate", toDate.Value);
                    additionalFilter.Append(" AND SAL_DispatchHeader.DispatchDate  <= @ToDate ");
                }

                #endregion

                #region Filter by Customers

                if (customerIDFrom.HasValue && customerIDTo.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@CustomerIDFrom", customerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@CustomerIDTo", customerIDTo.Value);
                    additionalFilter.Append(" AND AR_SET_Customer.CustomerID BETWEEN @CustomerIDFrom AND @CustomerIDTo ");
                }
                else if (customerIDFrom.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@CustomerIDFrom", customerIDFrom.Value);
                    additionalFilter.Append(" AND AR_SET_Customer.CustomerID >= @CustomerIDFrom ");
                }
                else if (customerIDTo.HasValue)
                {
                    sqlparams[5] = new SqlParameter("@CustomerIDTo", customerIDTo.Value);
                    additionalFilter.Append(" AND AR_SET_Customer.CustomerID <= @CustomerIDTo ");

                }
                if (documentGroupID.HasValue)
                    additionalFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID);

                if (documentType.HasValue)
                    additionalFilter.Append(" And Set_DocumentType.DocumentTypeID =" + documentType);

                #endregion


                if (IsPending == true)
                    AddParameters.Append(" having tab.Dispatch_Qty > sum(tab.InvoicedQty)");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(PendingDispatchAsOf + AddParameters.ToString(), additionalFilter.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt_ToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt_ToReturn;
        }
        public static DataTable DispatchReport_MBIOne(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@DispatchNoFrom", dispatchNoFrom);
                //sqlparams[3] = new SqlParameter("@DispatchNoTo", dispatchNoTo);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDispatchReportOneData + sb.ToString()), sqlparams);

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
        public static DataTable DispatchReport_MBITwo(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@DispatchNoFrom", dispatchNoFrom);
                //sqlparams[3] = new SqlParameter("@DispatchNoTo", dispatchNoTo);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDispatchMBITwoData + sb.ToString()), sqlparams);

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
        public static DataTable DispatchReport_MBIThree(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo, Nullable<int> dONoFrom, Nullable<int> dONoTo, Nullable<int> statusCode, Nullable<int> batchID, Nullable<int> CustomerID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> dispatchID, int DocumentTypeID = 0, short? documentGroupID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@DispatchNoFrom", dispatchNoFrom);
                //sqlparams[3] = new SqlParameter("@DispatchNoTo", dispatchNoTo);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");

                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);

                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dONoFrom.HasValue && dONoTo.HasValue)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode BETWEEN " + dONoFrom + " AND " + dONoTo + "");

                else if (dONoFrom.HasValue && dONoTo == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoFrom);

                else if (dONoTo.HasValue && dONoFrom == null)
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + dONoTo);

                if (statusCode > 0)
                    sb.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode);

                if (batchID.HasValue)
                    sb.Append(" AND SAL_OrderHeader.Season = " + batchID.Value);
                if (CustomerID.HasValue && CustomerID > 0)
                    sb.Append(" AND AR_SET_Customer.CustomerID = " + CustomerID);

                if (dateFrom != null && dateTo != null)
                    //sb.Append(" AND SAL_DispatchHeader.DispatchDate BETWEEN " + dateFrom + " AND " + dateTo + "");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate >= '" + dateFrom + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    // sb.Append(" AND SAL_DispatchHeader.DispatchDate <= '" + dateTo + "'");
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                if (dispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + dispatchID);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                //sb.Append(" order by (Convert(int,isnull(INV_StockTransactionDetail.SourceCode,0))),SAL_DispatchHeader.DispatchNo ");
                sb.Append(" order by  SAL_DispatchHeader.DispatchDate asc ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDispatchMBIThreeData + sb.ToString()), sqlparams);

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

        public static DataRow GetDispatchDocType(string dispatchNo, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@dispatchNo", dispatchNo);


                string query = @"select top 1 SET_DocumentType.DocumentType,SAL_DispatchHeader.DispatchNo
 from SAL_DispatchHeader
left join SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_DispatchHeader.DocumentTypeID
where  SAL_DispatchHeader.DispatchNo =@dispatchNo and SET_DocumentType.DocumentType='Dispatch - Auto'
                ";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataRow GetDispatchCurrency(string dispatchNo, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@dispatchNo", dispatchNo);


                string query = @"select top 1 SET_Currency.CurrencyID,SET_Currency.CurrencyName,Operations_Currency.Rate,Operations_Currency.Date
from SAL_DispatchHeader
left join SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_DispatchHeader.DocumentTypeID
left join Operations_WorkOrderMaster on Operations_WorkOrderMaster.DocumentNo=SAL_DispatchHeader.DispatchNo 
left join Operations_ContractMaster on Operations_ContractMaster.ContractID=Operations_WorkOrderMaster.ContractID 
left join Operations_Currency on Operations_Currency.CurrencyID=Operations_ContractMaster.CurrencyID
left join SET_Currency on SET_Currency.CurrencyID=Operations_ContractMaster.CurrencyID
where  SAL_DispatchHeader.DispatchNo=@dispatchNo and SET_DocumentType.DocumentType='Dispatch - Auto'
and Operations_Currency.Date <= isnull(Operations_WorkOrderMaster.UpdateDate,Operations_WorkOrderMaster.CreateDate)
order by Operations_Currency.Date desc
                ";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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


        public static bool DeleteDispatchAndItsInformation(int DispatchID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@Dispatch_ID", DispatchID);

                const string Query_DeleteDispatchAndItsInformation = @" declare @DispatchID  int = @Dispatch_ID
delete  from INV_StockTransactionDetail where StockTransactionID = (select StockTransactionID  from INV_StockTransactionHeader where DocumentNo =  (select DispatchNo from SAL_DispatchHeader where DispatchID = @DispatchID  ) and DocumentTypeID =  (select DocumentTypeID from SAL_DispatchHeader where DispatchID = @DispatchID  ))
delete from INV_StockTransactionHeader where DocumentNo =  (select DispatchNo from SAL_DispatchHeader where DispatchID = @DispatchID  ) and DocumentTypeID =  (select DocumentTypeID from SAL_DispatchHeader where DispatchID = @DispatchID  )
delete from GL_TransactionDetail where TransactionID = (select TransactionID  from GL_TransactionHeader where DocumentNo =  (select DispatchNo from SAL_DispatchHeader where DispatchID = @DispatchID  ) and DocumentTypeID =  (select DocumentTypeID from SAL_DispatchHeader where DispatchID = @DispatchID  ))
delete from GL_TransactionHeader where DocumentNo =  (select DispatchNo from SAL_DispatchHeader where DispatchID = @DispatchID  ) and DocumentTypeID =  (select DocumentTypeID from SAL_DispatchHeader where DispatchID = @DispatchID  )
delete  from SAL_DispatchHeader where DispatchID = @DispatchID   ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDispatchAndItsInformation, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDispatchAndItsInformation, sqlparams) > 0)
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


        public static DataTable GetAllDispatchesForCustomer(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;

            try
            {

                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SelectAllDispatchers;
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatchesForCustomerRegisteredChecked(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, int isRegister = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            StringBuilder sb = new StringBuilder();
            if (isRegister > 0)
            {
                if (isRegister == 1)
                {
                    sb.Append("Left join AR_SET_Customer on AR_SET_Customer.CustomerID = FInalTab.CustomerID Where AR_SET_Customer.IsRegistered = 1");
                }
                else
                {
                    sb.Append("Left join AR_SET_Customer on AR_SET_Customer.CustomerID = FInalTab.CustomerID Where AR_SET_Customer.IsRegistered = 0");
                }
            }

            DataTable dt = null;

            try
            {

                string query = string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SelectAllDispatchers, sb.ToString());
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetSeperateDispatches(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;

            try
            {

                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SelectSeperateDispatch;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatches(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;

            try
            {

                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SelectAllDispatchersAfterJune;
                //string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAllDispatchersAfterJune;

                if (Issheikhoo == true)
                {
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_GetAllDispatchForSheikhoo;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatchesRegisteredChecked(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, int isRegister = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            StringBuilder sb = new StringBuilder();
            if (isRegister > 0)
            {
                if (isRegister == 1)
                {
                    sb.Append("Where TABFinal.IsRegistered = 1");
                }
                else
                {
                    sb.Append("Where TABFinal.IsRegistered = 0");
                }
            }

            DataTable dt = null;

            try
            {

                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SelectAllDispatchersAfterJune;
                //string query = FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SelectAllDispatchersAfterJune;

                if (Issheikhoo == true)
                {
                    query = string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_GetAllDispatchForSheikhoo, sb.ToString());
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatchesWithoutCNIC(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;

            try
            {
                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetAllDispatchWithoutCNICCheck;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatchesWithoutCNIC_New(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, int CustomerID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                if (CustomerID > 0)
                {
                    sb.Append("AND SAL_DispatchHeader.CustomerID = " + CustomerID);
                }
                string query = string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetAllDispatchWithoutCNICCheck_New, sb);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDispatchesWithoutClubCNIC_New(int CompanyID, int PeriodID, int DocumentTypeID, int CurrentFiscalID = 0, int CustomerID = 0, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, bool Issheikhoo = false, bool isScrap = false)
        {
            SqlParameter[] sqlparams = new SqlParameter[6];
            sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
            sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
            sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);
            sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
            sqlparams[4] = new SqlParameter("@DateTo", DateTo);
            sqlparams[5] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                if (CustomerID > 0)
                {
                    sb.Append("AND SAL_DispatchHeader.CustomerID = " + CustomerID);
                }
                string query = string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetAllDispatchWithoutClubCNICCheck_New, sb);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds.Tables[0].TableName = "Table";
                    dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataForDispatchReturnReport(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
             Nullable<int> documentTypeID = null, SqlTransaction objTransaction = null, int CurrentFiscalID = 0, int LocationID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter.Append(" And SAL_ReverseHeader.CreateDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");
                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_ReverseHeader.CreateDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_ReverseHeader.CreateDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (documentTypeID.HasValue)
                    whereFilter.Append(" AND SAL_ReverseHeader.DocumentTypeID = " + documentTypeID.Value);

                if (LocationID > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.LocationID = " + LocationID);
                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);

                string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchReturnReport, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable DispatchDetailLotWise(int companyID, int periodID, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentTypeID = null, Nullable<short> statusCode = null, SqlTransaction objTransaction = null,
            short? documentGroupID = null, int CurrentFiscalID = 0, int LocationID = 0, int RackID = 0, Nullable<int> BatchID = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");
                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (customerID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.CustomerID = " + customerID);

                if (documentTypeID.HasValue)
                {
                    string DispatchType = "";
                    int ReturnTypeID = 0;
                    DataTable dtReturnType;

                    SET_DocumentType objdoctype = new SET_DocumentType(companyID, documentTypeID.Value, periodID);

                    if (objdoctype.DocumentType.ToLower().Contains("sugar"))
                        DispatchType = "sugar";
                    else if (objdoctype.DocumentType.ToLower().Contains("co2"))
                        DispatchType = "co2";
                    else if (objdoctype.DocumentType.ToLower().Contains("molasses"))
                        DispatchType = "molasses";
                    else if (objdoctype.DocumentType.ToLower().Contains("mud"))
                        DispatchType = "mud";
                    else if (objdoctype.DocumentType.ToLower().Contains("scrap"))
                        DispatchType = "scrap";
                    else if (objdoctype.DocumentType.ToLower().Contains("bagasse"))
                        DispatchType = "bagasse";



                    dtReturnType = SET_DocumentType.GetDataByDocTypeName(companyID, periodID, "and DocumentType like '%Dispatch%Return%" + DispatchType + "%' ", 0);
                    if (dtReturnType != null && dtReturnType.Rows.Count > 0)
                    {
                        ReturnTypeID = dtReturnType.Rows[0]["DocumentTypeID"].ToString().ToIntOrZero();
                    }



                    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID in (" + documentTypeID.Value + "," + ReturnTypeID + ")");
                }

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.StatusCode = " + statusCode.Value);

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (LocationID > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.LocationID = " + LocationID);

                if (RackID > 0)
                    whereFilter.Append(" AND INV_StockTransactionDetail.RackID = " + RackID);

                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);
                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);

                string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDispatchLotWiseReport, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable GetWeightVarianceReportData(int companyID, int periodID, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> documentTypeID = null, SqlTransaction objTransaction = null, Nullable<int> batchID = null,
            Nullable<int> ItemID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");
                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (customerID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.CustomerID = " + customerID);

                if (documentTypeID.HasValue)
                    whereFilter.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + documentTypeID.Value);

                if (batchID > 0)
                    whereFilter.Append(" And SAL_OrderHeader.Season = '" + batchID + "'");

                if (ItemID > 0)
                    whereFilter.Append(" AND INV_Item.ItemID = '" + ItemID + "'");

                //string query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DispatchDetailReportNew, whereFilter);

                string query = string.Empty;

                query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_WeightVarianceReport, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable GetVehicleWiseWeighment_Report(int companyID, int periodID, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int DocumentTypeID = 0, Nullable<int> BatchID = null, int CurrentFiscalID = 0, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");
                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (DocumentTypeID > 0)
                    whereFilter.Append(" And [SET_DocumentType].DocumentTypeID= " + DocumentTypeID);

                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);

                string query = string.Empty;

                query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_VehicleWiseWeighment_Report, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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

        public static DataTable GetVehicleWiseWeighment_ReportForSSML(int companyID, int periodID, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int DocumentTypeID = 0, Nullable<int> BatchID = null, int CurrentFiscalID = 0, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Filter by Dispatch Date
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");
                }
                else if (dateFrom.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'");
                else if (dateTo.HasValue)
                    whereFilter.Append(" And SAL_DispatchHeader.DispatchDate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'");

                #endregion

                if (DocumentTypeID > 0)
                    whereFilter.Append(" And [SAL_DispatchHeader].DocumentTypeID= " + DocumentTypeID);

                if (BatchID.HasValue)
                    whereFilter.Append(" and Set_Batch.BatchID =  " + BatchID);

                string query = string.Empty;

                query = string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_VehicleWiseWeighment_ReportforSSML, whereFilter);

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, query, sqlparams);

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


        public static DataTable GetDeliveryChallanForSteel(int CurrentCompanyID, int? DispatchNo, DateTime? Date_From, DateTime? Date_To, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", DispatchNo);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                //if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                //else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                //else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(Date_From.Value, true) + "' and '"
                        + DateUtil.ConvertToString(Date_To.Value, true) + "'");
                }
                else if (Date_From != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "'");
                else if (Date_To != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");

                if (DispatchNo > 0)
                    sb.Append(" And [SAL_DispatchHeader].DispatchNo= " + DispatchNo);

                //if (DispatchID.HasValue)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForSteel, sb.ToString()), sqlparams);

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

        public static DataTable GetDeliveryChallanForSteelFor_SKU(int CurrentCompanyID, int? DispatchNo, DateTime? Date_From, DateTime? Date_To, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", DispatchNo);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(Date_From.Value, true) + "' and '"
                        + DateUtil.ConvertToString(Date_To.Value, true) + "'");
                }
                else if (Date_From != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "'");
                else if (Date_To != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");

                if (DispatchNo > 0)
                    sb.Append(" And [SAL_DispatchHeader].DispatchNo= " + DispatchNo);

                //if (DispatchID.HasValue)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForSteel_SKU, sb.ToString()), sqlparams);

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

        public static DataTable GetDeliveryChallanForSteel_DealerCopy(int CurrentCompanyID, int? DispatchNo, DateTime? Date_From, DateTime? Date_To, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", DispatchNo);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                //if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                //else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                //else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(Date_From.Value, true) + "' and '"
                        + DateUtil.ConvertToString(Date_To.Value, true) + "'");
                }
                else if (Date_From != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "'");
                else if (Date_To != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");

                if (DispatchNo > 0)
                    sb.Append(" And [SAL_DispatchHeader].DispatchNo= " + DispatchNo);

                //if (DispatchID.HasValue)
                //    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForSteel_DealerCopy, sb.ToString()), sqlparams);

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

        public static DataTable GetDeliveryChallanForSteel_DealerCopyFor_SKU(int CurrentCompanyID, int? DispatchNo, DateTime? Date_From, DateTime? Date_To, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@DispatchNo", DispatchNo);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(Date_From.Value, true) + "' and '"
                        + DateUtil.ConvertToString(Date_To.Value, true) + "'");
                }
                else if (Date_From != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)Date_From).ToString("yyyy-MM-dd") + "'");
                else if (Date_To != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)Date_To).ToString("yyyy-MM-dd") + "'");

                if (DispatchNo > 0)
                    sb.Append(" And [SAL_DispatchHeader].DispatchNo= " + DispatchNo);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForSteel_DealerCopy_SKU, sb.ToString()), sqlparams);

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

        public static DataTable GetDeliveryChallanForDispatch(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0
            , Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForDispatch, sb.ToString()), sqlparams);

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


        public static DataTable GetDeliveryChallanForDispatch_HSM(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0
            , Nullable<int> DispatchID = null, string ChallanNoFrom = "", string ChallanNoTo = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                if (ChallanNoFrom != "" && ChallanNoTo != "")

                    sb.Append(" AND SAL_DispatchHeader.GatePassNo between " + ChallanNoFrom + "  and " + ChallanNoTo + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForDispatch_HSM, sb.ToString()), sqlparams);

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


        public static DataTable GetDeliveryChallanForDispatch_New(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0
            , Nullable<int> DispatchID = null, string ChallanNoFrom = "", string ChallanNoTo = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                if (ChallanNoFrom != "" && ChallanNoTo != "")

                    sb.Append(" AND SAL_DispatchHeader.GatePassNo between " + ChallanNoFrom + "  and " + ChallanNoTo + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForDispatch_New, sb.ToString()), sqlparams);

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


        public static DataTable GetDeliveryChallanForDispatch_RSM(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0
            , Nullable<int> DispatchID = null, string ChallanNoFrom = "", string ChallanNoTo = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");
                else
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID IN (104,216)");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                if (ChallanNoFrom != "" && ChallanNoTo != "")

                    sb.Append(" AND SAL_DispatchHeader.GatePassNo between " + ChallanNoFrom + "  and " + ChallanNoTo + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDeliveryChallanForDispatch_RSM, sb.ToString()), sqlparams);

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

        public static DataTable GetDeliveryChallanFor_Molasses(int companyID, int periodID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0
            , Nullable<int> DispatchID = null, string ChallanNoFrom = "", string ChallanNoTo = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");
                else
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID NOT IN (104,216)");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");

                if (ChallanNoFrom != "" && ChallanNoTo != "")

                    sb.Append(" AND SAL_DispatchHeader.GatePassNo between " + ChallanNoFrom + "  and " + ChallanNoTo + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_DeliveryChallanFor_Molasses, sb.ToString()), sqlparams);

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

        public DataTable GetDataDispatchNoteJk_Report(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int CompanyID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                #region Query

                string Query_GetData_DispatchNoteJkReport = @"SELECT SAL_DispatchNoteHeader.POHeaderID, SAL_DispatchNoteHeader.Transporter, SAL_DispatchNoteHeader.BiltyNo,
                SAL_DispatchNoteHeader.DispatchNoteNo, SAL_DispatchNoteHeader.DispatchNoteDate, SAL_DispatchNoteHeader.NoteToReceiver, 
                SAL_DispatchNoteHeader.BiltyDate, SAL_DispatchNoteHeader.FreightAmount, SAL_DispatchNoteDetail.SerialNo, 
                SAL_DispatchNoteDetail.PODate, SAL_DispatchNoteDetail.Description, 
                SAL_DispatchNoteDetail.Unit, SAL_DispatchNoteDetail.Quantity, SAL_DispatchNoteDetail.Supplier as SupplierName, 
                SAL_DispatchNoteDetail.RequisitionNo,SAL_DispatchNoteDetail.RequisitionDate,AP_PurchaseOrderHeader.DocumentNumber AS PONumber,
                SAL_DispatchNoteDetail.Comments, UsersDetail.FullName AS Username, SET_Designation.DesignationName
                FROM SAL_DispatchNoteHeader
                INNER JOIN SAL_DispatchNoteDetail ON SAL_DispatchNoteDetail.DispatchNoteID = SAL_DispatchNoteHeader.DispatchNoteID
                AND SAL_DispatchNoteDetail.CompanyID = SAL_DispatchNoteHeader.CompanyID
                AND SAL_DispatchNoteDetail.PeriodID = SAL_DispatchNoteHeader.PeriodID
                INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = SAL_DispatchNoteDetail.POHeaderID
                AND AP_PurchaseOrderHeader.CompanyID = SAL_DispatchNoteDetail.CompanyID
                AND AP_PurchaseOrderHeader.PeriodID = SAL_DispatchNoteDetail.PeriodID
				inner join aspnet_Users on aspnet_Users.UserId = SAL_DispatchNoteHeader.CreatedBy
				left join UsersDetail on UsersDetail.UserID = aspnet_Users.UserId
				left join SET_Designation on SET_Designation.DesignationID = UsersDetail.DesignationID
                WHERE SAL_DispatchNoteDetail.CompanyID = @CompanyID AND SAL_DispatchNoteDetail.PODate BETWEEN @FromDate AND @ToDate";

                #endregion

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_DispatchNoteJkReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_DispatchNoteJkReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_DispatchNoteJkReport, sqlparams);

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

        public DataTable GetDataDispatchAdviceCO2_Report(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int CompanyID = 0, int PeriodID = 0, int DispatchNo = 0, int DocumentTypeID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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

                if (DispatchNo > 0)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo = " + DispatchNo);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceCO2Report, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceCO2Report, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceCO2Report, sb.ToString()), sqlparams);

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

        public DataTable GetDataDispatchAdviceEthanol_Report(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int CompanyID = 0, int PeriodID = 0, int DispatchNo = 0, int DocumentTypeID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
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

                if (DispatchNo > 0)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo = " + DispatchNo);

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceEthanolReport, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceEthanolReport, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_DispatchAdviceEthanolReport, sb.ToString()), sqlparams);

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


        public static object GetAllDataforGatePassEthanolReport(int companyID, int periodID, int fiscalID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID ", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", fiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                //if (DocumentTypeID > 0)
                //{
                //    sb.Append(" and [SET_DocumentType].DocumentTypeID = " + DocumentTypeID + "");
                //}

                //if (DocumentNo.HasValue)
                //{
                //    sb.Append(" and AuditTrail.DocumentNo = " + DocumentNo + "");
                //}

                //if (UserID != "")
                //    sb.Append(" and AuditTrail.UserID = '" + UserID + "'");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDataForGatePassEthonalReport), sqlparams);

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

        public DataTable GetDataCustomerLiftingSummary_Report(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, int CompanyID = 0, int PeriodID = 0, int DocumentTypeID = 0, int FiscalID = 0, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (DocumentTypeID > 0)
                    sb.Append(" AND SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID);

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_CustomerLiftingSummaryReport, sb.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_CustomerLiftingSummaryReport, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetData_CustomerLiftingSummaryReport, sb.ToString()), sqlparams);

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


        public static DataTable IsDispatchExistinTax(int companyID, int documentTypeID, Guid UserID, DateTime documentDate, SqlConnection obj_SQLConnection = null)
        {

            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DocumentDate", documentDate);

                const string Query = @" select * from HRMS_SET_TaxYear where (@DocumentDate) between
(select max(Cast(HRMS_SET_TaxYear.FromDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1) 
and (select max(Cast(HRMS_SET_TaxYear.ToDate as Date)) from HRMS_SET_TaxYear where HRMS_SET_TaxYear.Status  = 1)";

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public static DataTable IsDispatchExistinTaxForISDKSM(int companyID, int documentTypeID, Guid UserID, DateTime documentDate, SqlConnection obj_SQLConnection = null)
        {

            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@DocumentDate", documentDate);

                const string Query = @" 
SELECT *
FROM HRMS_SET_TaxYear
 where (@DocumentDate) BETWEEN CAST(FromDate AS DATE) AND CAST(ToDate AS DATE)
  AND Status = 1
";

                DataSet ds = null;


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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

        public bool DispatchClose(int Companyid, int perioid, int DocmntTypeID, int DispatchID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@DispatchID", DispatchID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);

                const string Query_DispatchClose = @"
                UPDATE SAL_DispatchHeader
                SET IsCancelled = 1 ,  Remarks =  @remarks
				From SAL_DispatchHeader
				INNER JOIN [INV_StockTransactionHeader] StockHeader ON StockHeader.[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
				AND StockHeader.[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
				AND StockHeader.[CompanyID] = [SAL_DispatchHeader].[CompanyID] 
				AND StockHeader.[PeriodID] = [SAL_DispatchHeader].[PeriodID]
				AND StockHeader.[FiscalID] = [SAL_DispatchHeader].[FiscalID] 
				AND StockHeader.[Status] = 1
                where StockHeader.DocumentTypeID =@DocumentTypeID AND StockHeader.StockTransactionID = @DispatchID
                AND StockHeader.CompanyID = @CompanyID  AND StockHeader.PeriodID = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DispatchClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DispatchClose, sqlparams) > 0)
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


        public static DataTable CheckSecondWeightStatusForDispatch(int companyID, int periodID, int FiscalID, int SaleTokenNo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            #region Query
            const string Query = @" 
select sum(abs(INV_StockTransactionDetail.TransactionQuantity )) as Quantity,max(INV_Item.UnitVolume) as UnitWeight
from SAL_DispatchHeader 

inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID

inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1

inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID

where SAL_DispatchHeader.status = 1 and SAL_DispatchHeader.TokenNo = @SalesTokenNo and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID and SAL_DispatchHeader.FiscalID = @FiscalID and SAL_DispatchHeader.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%sugar%')
";
            #endregion

            if (type != "")
            {
                sb.Append(" and SAL_DispatchHeader.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%sugar%') ");
            }


            sb.Append(" having sum(abs(INV_StockTransactionDetail.TransactionQuantity )) > 0 ");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalesTokenNo", SaleTokenNo);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static DataTable GetGatePassJKReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassJK, sb.ToString()), sqlparams);

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


        public static DataTable GetGatePassPSReport(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
     Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassPS, sb.ToString()), sqlparams);

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

        public bool UpdateDataForDispatchHeader(int companyid, int periodid, int FiscalID, int DocumentTypeID, int SaleTokenNo,
            int TransporterID, string BiltyNo, string BiltyAddress, string DriverMobileNo, string VehicleNo)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@SaleTokenNo", SaleTokenNo);
                sqlparams[5] = new SqlParameter("@TransporterID", TransporterID);
                sqlparams[6] = new SqlParameter("@BiltyNo", BiltyNo);
                sqlparams[7] = new SqlParameter("@BiltyAddress", BiltyAddress);
                sqlparams[8] = new SqlParameter("@DriverMobileNo", DriverMobileNo);
                sqlparams[9] = new SqlParameter("@VehicleNo", VehicleNo);


                string Query = @"Update SAL_DispatchHeader SET TransporterID = @TransporterID, BiltyNo = @BiltyNo, VehicleNo = @VehicleNo, BiltyAddress = @BiltyAddress
                ,DriverContactNumber = @DriverMobileNo
                Where CompanyID = @CompanyID And PeriodID = @PeriodID And FiscalID = @FiscalID And TokenTypeID = @DocumentTypeID
                And TokenNo = @SaleTokenNo And Status = 1";

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


        public static DataTable GetDataforCustomerWiseLifting(DateTime? fromDate, DateTime? toDate, int companyID, int periodID, string menupath, int batchId, int FromCustomer, int ToCustomer, int DocumentTypeID, Nullable<int> BatchID = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (fromDate != null && toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                else if (fromDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)fromDate).ToString("MM-dd-yyyy") + "'";
                else if (toDate != null)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)toDate).ToString("MM-dd-yyyy") + "'";
                if (FromCustomer > -1 && ToCustomer > -1)
                    additionalFilter = additionalFilter + " AND AR_SET_Customer.CustomerID between " + FromCustomer + " AND " + ToCustomer;


                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND [SAL_DispatchHeader].DocumentTypeID = " + DocumentTypeID;

                if (BatchID.HasValue)
                    sb.Append(" and [SET_Batch].BatchID =  " + BatchID);

                // ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDateWiseLiftingReport + additionalFilter , sb.ToString()), sqlparams);

                string finalQuery = String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_CustomerWiseLiftingReport + additionalFilter + sb.ToString());

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlparams);

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


        public static DataTable GetDispatchData(int companyID, int periodID, int dispatchNo, int DocumentTypeID, int FiscalID, int? ItemID)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@dispatchNo", dispatchNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);
                if (ItemID.HasValue)
                    sqlparams[5] = new SqlParameter("@ItemID", ItemID);
                else
                    sqlparams[5] = new SqlParameter("@ItemID", DBNull.Value);


                const string Query_GetDispatchData = @" select (select  StockTransactionID from INV_StockTransactionHeader where DocumentNo = SAL_DispatchHeader.DispatchNo and DocumentTypeID=SAL_DispatchHeader.DocumentTypeID and FiscalID=SAL_DispatchHeader.FiscalID and CompanyID=SAL_DispatchHeader.CompanyID) as StockTransactionID, 
(select  sum(abs(inv_dtl.TransactionQuantity))
from INV_StockTransactionHeader 
inner join INV_StockTransactionDetail inv_dtl on inv_dtl.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and inv_dtl.CompanyID = INV_StockTransactionHeader.CompanyID
and inv_dtl.PeriodID = INV_StockTransactionHeader.PeriodID
and inv_dtl.Status = 1
where INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo and INV_StockTransactionHeader.DocumentTypeID=SAL_DispatchHeader.DocumentTypeID and INV_StockTransactionHeader.FiscalID=SAL_DispatchHeader.FiscalID and INV_StockTransactionHeader.CompanyID=SAL_DispatchHeader.CompanyID
and inv_dtl.ItemID = INV_StockTransactionDetail .ItemID
) as DispatchQuantity, 
(case when ( 
	SAL_ContractDetail.Rate <= 
	
	(case when SET_DocumentType.DocumentType like '%billet%' 
	then
		SAL_ContractDetail.T_Rate
	else
		(SAL_ContractDetail.T_Rate - (SAL_ContractDetail.T_Rate * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount))) 
	end ) 
	 )

   then 
   
   (case when SAL_ContractDetail.Inclusive = 1 then
		--!st case Sugar 34 No
		(abs(INV_StockTransactionDetail.TransactionQuantity) * SAL_ContractDetail.rate)
		+
		(SAL_ContractDetail.T_Rate * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) * abs(INV_StockTransactionDetail.TransactionQuantity))
		
   else 
   (abs(INV_StockTransactionDetail.TransactionQuantity) * SAL_ContractDetail.rate)
	+
	   (case when SET_DocumentType.DocumentType like '%billet%' 
	   then 
			(SAL_ContractDetail.T_Rate * SAL_ContractDetail.SalesTaxAmount / 100 ) *  abs(INV_StockTransactionDetail.TransactionQuantity)
		else
			SAL_ContractDetail.T_Rate * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) 
		end)

   end)
   else

   case when SAL_ContractDetail.Inclusive = 1 then 
		(abs(INV_StockTransactionDetail.TransactionQuantity) * SAL_ContractDetail.rate)
	else
		(abs(INV_StockTransactionDetail.TransactionQuantity) * SAL_ContractDetail.rate)
		+
		(((abs(INV_StockTransactionDetail.TransactionQuantity) * SAL_ContractDetail.rate)) * [SAL_ContractDetail].[SalesTaxAmount] /100)
	end 
   end	 
	 ) as NetAmount, 
* 
from [SAL_DispatchHeader]
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
            and SET_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID and SET_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID 

INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
            AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 

Left JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
            AND SAL_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
            AND SAL_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID AND SAL_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND SAL_DeliveryOrderDetail.Status = 1 

Inner JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID 
            AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID AND SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
            AND SAL_OrderDetail.PeriodID= SAL_DeliveryOrderDetail.PeriodID AND SAL_OrderDetail.Status = 1 

inner join SAL_ContractDetail on SAL_ContractDetail.ContractDetailID = SAL_OrderDetail.ContractDetailID
		and  SAL_ContractDetail.ContractID = SAL_OrderDetail.ContractHeaderID
		and SAL_ContractDetail.CompanyID =SAL_OrderDetail.CompanyID
		and SAL_ContractDetail.PeriodID =SAL_OrderDetail.PeriodID
		and SAL_ContractDetail.ItemID =SAL_OrderDetail.ItemID
		and SAL_ContractDetail.Status = 1

WHERE [SAL_DispatchHeader].[CompanyID] = @CompanyID and [SAL_DispatchHeader].[DispatchNo] = @dispatchNo and [SAL_DispatchHeader].[PeriodID] = @PeriodID  and [SAL_DispatchHeader].DocumentTypeID = @DocumentTypeID  and [SAL_DispatchHeader].FiscalID = @FiscalID 
and (INV_StockTransactionDetail.ItemID = @ItemID or @ItemID is null)
and INV_StockTransactionHeader.StockTransactionID not in (select StockHeaderID from SAL_InvoiceDetail where SAL_InvoiceDetail.StockHeaderID = INV_StockTransactionHeader.StockTransactionID and SAL_InvoiceDetail.CompanyID = INV_StockTransactionHeader.CompanyID and SAL_InvoiceDetail.Status = 1) ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDispatchData, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }



        public bool insertDispatchBreakQuantity(INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, SET_DocumentApprove da, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            bool result = false;
            SqlTransaction sqltrans;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertBreakDispatch");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {
                this.DispatchNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.DispatchID = SAL_DispatchHeader.GetNewDispatchID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;

                this.Insert(sqltrans);

                #region For Stock Transaction Header & Detail

                sth.DocumentNo = int.Parse(this.DispatchNo);
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.FiscalID = CurrentFiscalID;
                sth.Insert(sqltrans);

                long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {

                    bool updateQty = INV_StockTransactionDetail.UpdateDispatchQuantity(sqltrans, DateTime.Now, std.StockTransactionID, std.TransactionDetailID, std.TransactionQuantity.ToString().ToDecimalOrZero());


                    std.StockTransactionID = sth.StockTransactionID;
                    std.TransactionDetailID = transactionDetailID++;
                    std.FiscalID = CurrentFiscalID;

                    std.Insert(sqltrans);
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"


                da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                da.DocumentNo = this.DispatchNo;
                da.FiscalID = CurrentFiscalID;
                da.Insert(sqltrans);



                //INV_StockTransactionDetail.UpdateDispatchQuantity(sqltrans,DateTime.Now,

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
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertBreakDispatch");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public static DataTable GetGatePassMBSMReport_DK(int companyID, int periodID, int categoryID, Nullable<int> dispatchNoFrom, Nullable<int> dispatchNoTo,
            Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int DocumentTypeID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> DispatchID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@CategoryID", categoryID);

                if (dispatchNoFrom.HasValue && dispatchNoTo.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo BETWEEN " + dispatchNoFrom + " AND " + dispatchNoTo + "");
                else if (dispatchNoFrom.HasValue && dispatchNoTo == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoFrom);
                else if (dispatchNoTo.HasValue && dispatchNoFrom == null)
                    sb.Append(" AND SAL_DispatchHeader.DispatchNo =" + dispatchNoTo);

                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) between '" + DateUtil.ConvertToString(dateFrom.Value, true) + "' and '"
                        + DateUtil.ConvertToString(dateTo.Value, true) + "'");
                }
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_DispatchHeader.DispatchDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (DocumentTypeID > 0)
                    sb.Append(" and SAL_DispatchHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (categoryID > 0)
                //    sb.Append(" and CategoryID = @CategoryID  ");

                if (DispatchID.HasValue)
                    sb.Append(" AND SAL_DispatchHeader.DispatchID = " + DispatchID + "  ");
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetGatePassMBSM_DK, sb.ToString()), sqlparams);

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

        public static DataTable GetDataByCustomerID_ForFA_Dispatch(int companyID, int periodID, Nullable<int> customerID, string strWhere, string PaymentType = "")
        {
            DataTable dtToReturn = null;

            try
            {
                if (string.IsNullOrEmpty(PaymentType))
                {
                    PaymentType = "";
                }

                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Select_By_CustomerID_For_FA_Dispatch, strWhere, PaymentType), sqlparams);


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

    }

}

