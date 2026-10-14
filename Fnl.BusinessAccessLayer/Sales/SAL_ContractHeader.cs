using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Linq;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_ContractHeader
    {
        #region Constant Queries

        const string Query_SelectDataByContractID = @"select SO.*,SOD.*,CM.* from SAL_OrderHeader as SO
inner join SAL_OrderDetail as SOD on So.OrderID=sod.OrderID
inner join Operations_ContractMaster as CM on cm.ContractID=so.ContractNo
where cm.Status=1 and so.Status=1 and sod.Status=1 and so.ContractNo=@ContractNo
--where cm.Status=1 and so.Status=1 and sod.Status=1 and so.ContractNo=1
";

        const string Query_CheckReciptAgainstCPR_Doc = @" select * from AR_ReceiptTransactionHeader 
    inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID and DocumentType = 'Receipt Against CPR' 
where AR_ReceiptTransactionHeader.CompanyId = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID and CustomerID = @CustomerID ";

        const string Query_RecivingCompletAgainstSO = @" 
select sum(AR_ReceiptTransactionHeader.Amount) as Amount, SAL_OrderDetail.NetAmount, SAL_OrderDetail.GrossAmount, SAL_OrderHeader.AmountType
		 ,set_DocumentType.DocumentType,SAL_OrderHeader.orderID
		 from AR_ReceiptTransactionHeader 
		 inner join set_DocumentType on set_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
		 and DocumentType = 'Receipt (Against S.O)' 
		 or DocumentType = 'Advance Against SO (Head Office)' 
		 
		 inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = AR_ReceiptTransactionHeader.SourceHeaderID
		 and SAL_OrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID
		 and SAL_OrderHeader.PeriodID = AR_ReceiptTransactionHeader.PeriodID
		 
		 inner join SAL_OrderDetail on  SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
		 and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
		 and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
		 
		 where AR_ReceiptTransactionHeader.CompanyId = @CompanyID
		 and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
		 and SourceHeaderID = @SaleOrderID
		 and AR_ReceiptTransactionHeader.CustomerID = @CustomerID
         group by SAL_OrderDetail.NetAmount ,SAL_OrderDetail.GrossAmount,SAL_OrderHeader.AmountType
         ,set_DocumentType.DocumentType,SAL_OrderHeader.orderID

";

        const string Query_GetDOData_byKey = @" SELECT SAL_ContractHeader.ContractDate, SAL_ContractHeader.BilledTo, AR_Set_Customer.CustomerName, SAL_ContractHeader.ReferenceNo, SAL_ContractHeader.CustomerID, SAL_ContractHeader.LocationID,
    SAL_ContractHeader.Season ,SAL_ContractHeader.BrokerID
FROM SAL_ContractHeader 
    LEFT JOIN AR_Set_Customer ON SAL_ContractHeader.BilledTo = AR_Set_Customer.CustomerID --AND SAL_ContractHeader.CompanyID = AR_Set_Customer.CompanyID 
        AND SAL_ContractHeader.PeriodID = AR_Set_Customer.PeriodID 
Where SAL_ContractHeader.CompanyID = @CompanyID AND SAL_ContractHeader.PeriodID = @PeriodID AND SAL_ContractHeader.ContractID = @OrderID
 ";

        const string Query_GetCommissionPercent = @"select Commission,BrokerID from SAL_OrderHeader
Inner Join SAL_OrderDetail ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderId
	 AND SAL_OrderHeader.[CompanyID] = SAL_OrderDetail.[CompanyID]
	 AND SAL_OrderHeader.[PeriodID] = SAL_OrderDetail.[PeriodID]
Inner join SAL_DeliveryOrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
Inner join SAL_DeliveryOrderHeader On SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
		and SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo		
	and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
	and INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
	and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
Inner join SAL_DispatchHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo

where 
SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID
--SAL_DispatchHeader.CustomerID = 1696
and SAL_DispatchHeader.DispatchID = @DispatchID";

        const string Query_GetCommissionPercentByDO = @"select distinct Commission,BrokerID from SAL_OrderHeader
Inner Join SAL_OrderDetail ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderId
	 AND SAL_OrderHeader.[CompanyID] = SAL_OrderDetail.[CompanyID]
	 AND SAL_OrderHeader.[PeriodID] = SAL_OrderDetail.[PeriodID]
Inner join SAL_DeliveryOrderDetail On SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
		And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
Inner join SAL_DeliveryOrderHeader On SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
		and SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo		
	and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
	and INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
	and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
Inner join SAL_DispatchHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo

where 
SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID
and SAL_DeliveryOrderHeader.DeliveryOrderNo = @DeliveryOrderID";


        const string Query_Update_IsPaymentGenrated = @"Update SAL_InvoiceHeader
                                    Set IsPaymentGenerated = 1
                                    Where 
                                    CompanyID = @CompanyID
                                    and PeriodID = @PeriodID
                                    and status = 1
                                    and invoiceid = InvoiceID";


        const string Query_SelectAll_Searching = @" 

";


        const string Query_SelectAll_SearchingWithDetail_WithPaging = @"
SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(tbl_POH.[ContractNo] as int) DESC) AS 'Row' 
FROM 
(
    SELECT [SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[CompanyID], [SAL_OrderHeader].[PeriodID], [SAL_OrderHeader].[SaleContractTypeID], 
        [SAL_OrderHeader].[CustomerID], [SAL_OrderHeader].[BrokerID], [SAL_OrderHeader].[PaymentTermID], [SAL_OrderHeader].[Season], [SAL_OrderHeader].[ContractNo], 
        [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[PaymentDate], [SAL_OrderHeader].[DispatchDate], [SAL_OrderHeader].[DTRENo], [SAL_OrderHeader].[DTREDate], 
        [SAL_OrderHeader].[IsApproved], [SAL_OrderHeader].[ApprovedBy], [SAL_OrderHeader].[ApprovalDate], [SAL_OrderHeader].[Remarks], [SAL_OrderHeader].[IsPaid], 
        [SAL_OrderHeader].[PaidAmount], [SAL_OrderHeader].[StatusCode], [SAL_OrderHeader].[Status], [SAL_OrderHeader].[CreatedBy], [SAL_OrderHeader].[CreateDate], 
        [SAL_OrderHeader].[UpdatedBy], [SAL_OrderHeader].[UpdateDate], [SAL_OrderHeader].[CreatedByValue], [SAL_OrderHeader].[UpdatedByValue], 
        [AR_SET_Customer].[CustomerName] AS 'Customer', tbl_Broker.[CustomerName] AS 'Broker' , abs(sum([SAL_OrderDetail].Quantity)) as Quantity
        ,[SET_UnitofMeasure].[UOMName],[SAL_OrderDetail].[Rate],Set_PeriodCustom.PeriodSetName,[SAL_OrderHeader].[ContractNo] as DocumentNo
,[SAL_OrderHeader].ContractDate as DocumentDate
    FROM [SAL_OrderHeader] 
        INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderID] = [SAL_OrderHeader].[OrderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
            AND [SAL_OrderDetail].[PeriodID] = [SAL_OrderHeader].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
        INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
            AND [INV_Item].[Status] = 1 
        INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
        INNER JOIN [AR_SET_Customer] ON [AR_SET_Customer].[CustomerID] = [SAL_OrderHeader].[CustomerID] 
--            AND [AR_SET_Customer].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
AND [AR_SET_Customer].[PeriodID] = [SAL_OrderHeader].[PeriodID] 
            AND [AR_SET_Customer].[Status] = 1 
        LEFT JOIN [AR_SET_Customer] AS tbl_Broker ON tbl_Broker.[CustomerID] = [SAL_OrderHeader].[BrokerID] --AND tbl_Broker.[CompanyID] = [SAL_OrderHeader].[CompanyID] 
            AND tbl_Broker.[PeriodID] = [SAL_OrderHeader].[PeriodID] AND tbl_Broker.[Status] = 1 

inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID

        {FilterByDocumentType}  {FilterByCategory}  {AdditionalParameters}   group by 
    [SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[CompanyID], [SAL_OrderHeader].[PeriodID], [SAL_OrderHeader].[SaleContractTypeID], 
        [SAL_OrderHeader].[CustomerID], [SAL_OrderHeader].[BrokerID], [SAL_OrderHeader].[PaymentTermID], [SAL_OrderHeader].[Season], [SAL_OrderHeader].[ContractNo], 
        [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[PaymentDate], [SAL_OrderHeader].[DispatchDate], [SAL_OrderHeader].[DTRENo], [SAL_OrderHeader].[DTREDate], 
        [SAL_OrderHeader].[IsApproved], [SAL_OrderHeader].[ApprovedBy], [SAL_OrderHeader].[ApprovalDate], [SAL_OrderHeader].[Remarks], [SAL_OrderHeader].[IsPaid], 
        [SAL_OrderHeader].[PaidAmount], [SAL_OrderHeader].[StatusCode], [SAL_OrderHeader].[Status], [SAL_OrderHeader].[CreatedBy], [SAL_OrderHeader].[CreateDate], 
        [SAL_OrderHeader].[UpdatedBy], [SAL_OrderHeader].[UpdateDate], [SAL_OrderHeader].[CreatedByValue], [SAL_OrderHeader].[UpdatedByValue], 
        [AR_SET_Customer].[CustomerName], tbl_Broker.[CustomerName]
        ,[SET_UnitofMeasure].[UOMName],[SAL_OrderDetail].[Rate],Set_PeriodCustom.PeriodSetName
) AS tbl_POH ";

        const string Query_SelectAll_Searching_WithPaging = @"


SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(tbl_POH.[ContractNo] as int) DESC) AS 'Row' 
FROM 
(
    SELECT distinct [SAL_ContractHeader].[ContractID], [SAL_ContractHeader].[CompanyID], [SAL_ContractHeader].[PeriodID], [SAL_ContractHeader].[SaleContractTypeID], 
        [SAL_ContractHeader].[CustomerID], [SAL_ContractHeader].[BrokerID], [SAL_ContractHeader].[PaymentTermID], [SAL_ContractHeader].[Season], [SAL_ContractHeader].[ContractNo], 
        [SAL_ContractHeader].[ContractDate], [SAL_ContractHeader].[PaymentDate], [SAL_ContractHeader].[DispatchDate], [SAL_ContractHeader].[DTRENo], [SAL_ContractHeader].[DTREDate], 
        [SAL_ContractHeader].[IsApproved], [SAL_ContractHeader].[ApprovedBy], [SAL_ContractHeader].[ApprovalDate], [SAL_ContractHeader].[Remarks], [SAL_ContractHeader].[IsPaid], 
        [SAL_ContractHeader].[PaidAmount], [SAL_ContractHeader].[StatusCode], [SAL_ContractHeader].[Status], [SAL_ContractHeader].[CreatedBy], [SAL_ContractHeader].[CreateDate], 
        [SAL_ContractHeader].[UpdatedBy], [SAL_ContractHeader].[UpdateDate], [SAL_ContractHeader].[CreatedByValue], [SAL_ContractHeader].[UpdatedByValue], 
        [AR_SET_Customer].[CustomerName] AS 'Customer', tbl_Broker.[CustomerName] AS 'Broker' ,  
        --abs(sum([SAL_OrderDetail].Quantity)) as Quantity,
		 (select abs(sum(SODetail.Quantity)) as Quantity from SAL_ContractDetail SODetail 
		 where  SODetail.CompanyID = [SAL_ContractHeader].CompanyID
		 and SODetail.PeriodID = [SAL_ContractHeader].PeriodID
		 and SODetail.ContractID = [SAL_ContractHeader].ContractID
		 and SODetail.Status=1
		 )as Quantity
,Set_PeriodCustom.PeriodSetName,[SAL_ContractHeader].[ContractNo] as DocumentNo,[SAL_ContractHeader].ContractDate as DocumentDate
    FROM [SAL_ContractHeader] 
        INNER JOIN [SAL_ContractDetail] ON [SAL_ContractDetail].ContractID = [SAL_ContractHeader].ContractID AND [SAL_ContractDetail].[CompanyID] = [SAL_ContractHeader].[CompanyID] 
            AND [SAL_ContractDetail].[PeriodID] = [SAL_ContractHeader].[PeriodID] AND [SAL_ContractDetail].[Status] = 1 
        INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_ContractDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
            AND [INV_Item].[Status] = 1 
        INNER JOIN [AR_SET_Customer] ON [AR_SET_Customer].[CustomerID] = [SAL_ContractHeader].[CustomerID] 
--            AND [AR_SET_Customer].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
AND [AR_SET_Customer].[PeriodID] = [SAL_ContractHeader].[PeriodID] 
            AND [AR_SET_Customer].[Status] = 1 
        LEFT JOIN [AR_SET_Customer] AS tbl_Broker ON tbl_Broker.[CustomerID] = [SAL_ContractHeader].[BrokerID] --AND tbl_Broker.[CompanyID] = [SAL_ContractHeader].[CompanyID] 
            AND tbl_Broker.[PeriodID] = [SAL_ContractHeader].[PeriodID] AND tbl_Broker.[Status] = 1 


inner join Set_PeriodCustom on [SAL_ContractHeader].ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

        
        {FilterByDocumentType}        
        {FilterByCategory}
    {AdditionalParameters} 

    --group by 
    --[SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[CompanyID], [SAL_OrderHeader].[PeriodID], [SAL_OrderHeader].[SaleContractTypeID], 
    --    [SAL_OrderHeader].[CustomerID], [SAL_OrderHeader].[BrokerID], [SAL_OrderHeader].[PaymentTermID], [SAL_OrderHeader].[Season], [SAL_OrderHeader].[ContractNo], 
    --    [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[PaymentDate], [SAL_OrderHeader].[DispatchDate], [SAL_OrderHeader].[DTRENo], [SAL_OrderHeader].[DTREDate], 
    --    [SAL_OrderHeader].[IsApproved], [SAL_OrderHeader].[ApprovedBy], [SAL_OrderHeader].[ApprovalDate], [SAL_OrderHeader].[Remarks], [SAL_OrderHeader].[IsPaid], 
    --    [SAL_OrderHeader].[PaidAmount], [SAL_OrderHeader].[StatusCode], [SAL_OrderHeader].[Status], [SAL_OrderHeader].[CreatedBy], [SAL_OrderHeader].[CreateDate], 
    --    [SAL_OrderHeader].[UpdatedBy], [SAL_OrderHeader].[UpdateDate], [SAL_OrderHeader].[CreatedByValue], [SAL_OrderHeader].[UpdatedByValue], 
    --    [AR_SET_Customer].[CustomerName], tbl_Broker.[CustomerName],Set_PeriodCustom.PeriodSetName,		 [SAL_OrderDetail].Quantity
    
) AS tbl_POH
 ";


        const string Query_SelectAllSeasons = @" SELECT DISTINCT Season FROM [SAL_ContractHeader] WHERE ([CompanyID] = @CompanyID OR @CompanyID is null) AND Season IS NOT NULL ORDER BY Season ";

        const string Query_SelectAllByCustomerID = @"
Select * From (select DISTINCT 
[SAL_OrderHeader].[OrderID],
[SAL_OrderHeader].[ContractNo],
[SAL_OrderHeader].[ContractDate],
[SAL_OrderHeader].[ReferenceNo],
SAL_OrderHeader.SaleContractTypeID,
Set_DocumentType.DocumentType,
Sum([SAL_OrderDetail].[Quantity]) [Quantity]
from [SAL_OrderHeader]
inner join [SAL_OrderDetail] on [SAL_OrderDetail].[OrderID] = [SAL_OrderHeader].[OrderID] and [SAL_OrderDetail].[CompanyID] = [SAL_OrderHeader].[CompanyID] and [SAL_OrderDetail].[PeriodID] = [SAL_OrderHeader].[PeriodID] and [SAL_OrderDetail].[Status] = 1 and [SAL_OrderHeader].[Status] = 1
left join
(
	select [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID], sum([Quantity])[DeliveredQty] from [SAL_DeliveryOrderDetail]
	where [CompanyID]  =@CompanyID and [PeriodID] = @PeriodID and [Status] = 1
	group by [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID]
) [DeliveryOerder] on [DeliveryOerder].[CompanyID] = [SAL_OrderDetail].[CompanyID] and [DeliveryOerder].[SalesOrderHeaderID] = [SAL_OrderDetail].[OrderID] and [DeliveryOerder].[SalesOrderDetailID] = [SAL_OrderDetail].[OrderDetailID]

INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
AND Set_DocumentType.CompanyID = SAL_OrderHeader.CompanyID AND Set_DocumentType.PeriodID = SAL_OrderHeader.PeriodID

where [SAL_OrderHeader].[CompanyID] = @CompanyID and [SAL_OrderHeader].[PeriodID] = @PeriodID 
and [SAL_OrderDetail].[Quantity] > isnull([DeliveryOerder].[DeliveredQty],0)
and ([SAL_OrderHeader].CustomerID = @CustomerID OR @CustomerID is null) AND [SAL_OrderHeader].[StatusCode] = 1 
   group BY SAL_OrderHeader.OrderID,SAL_OrderHeader.ContractNo,SAL_OrderHeader.ContractDate,SAL_OrderHeader.ReferenceNo,SAL_OrderHeader.SaleContractTypeID,Set_DocumentType.DocumentType)Tab Where Tab.Quantity <> 0 ";


        const string Query_SelectAllForAdvanceForReceiptByCustomerID1 = @"
SELECT DISTINCT [SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[ContractNo], [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[ReferenceNo],
        [SAL_OrderHeader].SaleContractTypeID, [Set_DocumentType].DocumentType, SUM([SAL_OrderDetail].[Quantity]) [Quantity]
        FROM SAL_OrderHeader

        INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
	        AND Set_DocumentType.CompanyID = SAL_OrderHeader.CompanyID 
	        AND Set_DocumentType.PeriodID = SAL_OrderHeader.PeriodID	
        JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
	        AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
	        AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
	        AND SAL_OrderDetail.[Status] = 1 
	        AND SAL_OrderHeader.[Status] = 1
        LEFT JOIN
        (
	        SELECT [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID], 
	        SUM([Quantity])[DeliveredQty] FROM [SAL_DeliveryOrderDetail]
	        WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1
	        GROUP BY [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID]
        ) [DeliveryOerder] ON [DeliveryOerder].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
	        AND [DeliveryOerder].[SalesOrderHeaderID] = [SAL_OrderDetail].[OrderID] 
	        AND [DeliveryOerder].[SalesOrderDetailID] = [SAL_OrderDetail].[OrderDetailID]
	
        LEFT JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
	        AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID
	        AND SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID
	        AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID
        LEFT JOIN Inv_StockTransactionDetail ON Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
	        AND Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	        AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
	        AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
	
        WHERE NOT EXISTS   (SELECT * FROM SAL_InvoiceDetail 
                            WHERE SAL_InvoiceDetail.StockDetailID = Inv_StockTransactionDetail.TransactionDetailID
	                        AND SAL_InvoiceDetail.StockHeaderID = Inv_StockTransactionDetail.StockTransactionID
	                        AND SAL_InvoiceDetail.CompanyID = Inv_StockTransactionDetail.CompanyID
	                        AND SAL_InvoiceDetail.PeriodID = Inv_StockTransactionDetail.PeriodID
                            )
                            AND NOT EXISTS
                            (SELECT * FROM [AR_ReceiptTransactionHeader] ARR where 
							ARR.DocumentTypeID in (select DT.DocumentTypeID from Set_DocumentType DT where DT.DocumentType='Receipt (Against S.O)' AND DT.CompanyID = ARR.CompanyID AND DT.PeriodID = ARR.PeriodID) 
							AND ARR.CompanyID=SAL_OrderHeader.CompanyID AND ARR.PeriodID=SAL_OrderHeader.PeriodID 
							AND ARR.CustomerID=SAL_OrderHeader.CustomerID AND ARR.SourceHeaderID=[SAL_OrderHeader].[OrderID]
							AND ARR.[Status]=1 
                            )
                            
	        AND [SAL_OrderHeader].[CompanyID] = @CompanyID 
	        AND [SAL_OrderHeader].[PeriodID] = @PeriodID 
	        AND [SAL_OrderDetail].[Quantity] >= ISNULL([DeliveryOerder].[DeliveredQty],0)
	        AND ([SAL_OrderHeader].CustomerID = @CustomerID OR @CustomerID IS NULL) 
            AND [SAL_OrderHeader].[StatusCode] = 1 
            AND  SAL_OrderHeader.cancelled <>1
		{0}
        GROUP BY SAL_OrderHeader.OrderID ,SAL_OrderHeader.ContractNo,SAL_OrderHeader.ContractDate,SAL_OrderHeader.ReferenceNo ,SAL_OrderHeader.SaleContractTypeID,Set_DocumentType.DocumentType
		{1}
        ";


        const string Query_SelectAllForAdvanceByCustomerIDForOrient = @"
        SELECT DISTINCT [SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[ContractNo], [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[ReferenceNo],
        [SAL_OrderHeader].SaleContractTypeID, [Set_DocumentType].DocumentType, SUM([SAL_OrderDetail].[Quantity]) [Quantity]
        FROM SAL_OrderHeader

        INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
	        AND Set_DocumentType.CompanyID = SAL_OrderHeader.CompanyID 
	        AND Set_DocumentType.PeriodID = SAL_OrderHeader.PeriodID	
        JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
	        AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
	        AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
	        AND SAL_OrderDetail.[Status] = 1 
	        AND SAL_OrderHeader.[Status] = 1	
        LEFT JOIN
        (
	        SELECT [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID], 
	        SUM([Quantity])[DeliveredQty] FROM [SAL_DeliveryOrderDetail]
	        WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1
	        GROUP BY [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID]
        ) [DeliveryOerder] ON [DeliveryOerder].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
	        AND [DeliveryOerder].[SalesOrderHeaderID] = [SAL_OrderDetail].[OrderID] 
	        AND [DeliveryOerder].[SalesOrderDetailID] = [SAL_OrderDetail].[OrderDetailID]
	
        LEFT JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
	        AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID
	        AND SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID
	        AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID
        LEFT JOIN Inv_StockTransactionDetail ON Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
	        AND Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	        AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
	        AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
	
        WHERE [SAL_OrderHeader].[CompanyID] = @CompanyID 
	        AND [SAL_OrderHeader].[PeriodID] = @PeriodID 
	        AND [SAL_OrderDetail].[Quantity] >= ISNULL([DeliveryOerder].[DeliveredQty],0)
	        AND ([SAL_OrderHeader].CustomerID = @CustomerID OR @CustomerID IS NULL) 
            AND [SAL_OrderHeader].[StatusCode] = 1 
            AND  SAL_OrderHeader.cancelled <>1
{0}
        GROUP BY SAL_OrderHeader.OrderID ,SAL_OrderHeader.ContractNo,SAL_OrderHeader.ContractDate,SAL_OrderHeader.ReferenceNo ,SAL_OrderHeader.SaleContractTypeID,Set_DocumentType.DocumentType
{1}
        ";


        const string Query_SelectAllForAdvanceByCustomerID = @"SELECT DISTINCT [SAL_OrderHeader].[OrderID], [SAL_OrderHeader].[ContractNo], [SAL_OrderHeader].[ContractDate], [SAL_OrderHeader].[ReferenceNo],[SAL_OrderHeader].SaleContractTypeID
, [Set_DocumentType].DocumentType, SUM([SAL_OrderDetail].[Quantity]) [Quantity]
        FROM SAL_OrderHeader

        INNER JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID AND Set_DocumentType.CompanyID = SAL_OrderHeader.CompanyID 
	        AND Set_DocumentType.PeriodID = SAL_OrderHeader.PeriodID	
        JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
	        AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
	        AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
	        AND SAL_OrderDetail.[Status] = 1 
	        AND SAL_OrderHeader.[Status] = 1	
        LEFT JOIN
        (
	        SELECT [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID], 
	        SUM([Quantity])[DeliveredQty] FROM [SAL_DeliveryOrderDetail]
	        WHERE [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1
	        GROUP BY [CompanyID],[PeriodID], [SalesOrderHeaderID], [SalesOrderDetailID]
        ) [DeliveryOerder] ON [DeliveryOerder].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
	        AND [DeliveryOerder].[SalesOrderHeaderID] = [SAL_OrderDetail].[OrderID] 
	        AND [DeliveryOerder].[SalesOrderDetailID] = [SAL_OrderDetail].[OrderDetailID]
	
left JOIN 
        (
			select SalesOrderDetailID,SalesOrderHeaderID,SAL_DeliveryOrderDetail.CompanyID,SAL_DeliveryOrderDetail.PeriodID 
			,DODetailID,DOHeaderID,[Quantity],ReversedQty
			from SAL_DeliveryOrderDetail 
			Inner join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = [SAL_DeliveryOrderDetail].DOHeaderID
				and SAL_DeliveryOrderHeader.CompanyID = [SAL_DeliveryOrderDetail].CompanyID
				and SAL_DeliveryOrderHeader.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID]
				and SAL_DeliveryOrderHeader.StatusCode <> 15
		) SAL_DeliveryOrderDetail on 
				SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
				AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID
				AND SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID
				AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID
        LEFT JOIN Inv_StockTransactionDetail ON Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
	        AND Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	        AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
	        AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
	
        WHERE NOT EXISTS   (SELECT * FROM SAL_InvoiceDetail 
                            WHERE SAL_InvoiceDetail.StockDetailID = Inv_StockTransactionDetail.TransactionDetailID
	                        AND SAL_InvoiceDetail.StockHeaderID = Inv_StockTransactionDetail.StockTransactionID
	                        AND SAL_InvoiceDetail.CompanyID = Inv_StockTransactionDetail.CompanyID
	                        AND SAL_InvoiceDetail.PeriodID = Inv_StockTransactionDetail.PeriodID
                            )
	        AND [SAL_OrderHeader].[CompanyID] = @CompanyID 
	        AND [SAL_OrderHeader].[PeriodID] = @PeriodID 
	        AND [SAL_OrderDetail].[Quantity] >= ISNULL([DeliveryOerder].[DeliveredQty],0)
	        AND ([SAL_OrderHeader].CustomerID = @CustomerID OR @CustomerID IS NULL) 
            AND [SAL_OrderHeader].[StatusCode] = 1 
            AND  SAL_OrderHeader.cancelled <>1
{0}
        GROUP BY SAL_OrderHeader.OrderID ,SAL_OrderHeader.ContractNo,SAL_OrderHeader.ContractDate,SAL_OrderHeader.ReferenceNo ,SAL_OrderHeader.SaleContractTypeID,Set_DocumentType.DocumentType
{1}
        ";

        //job
        const string Query_GetOrderNo = @"select OrderID,ContractNo from SAL_OrderHeader
            where CompanyID=@CompanyID and PeriodID =@PeriodID and DepartmentID=@DepartmentID and Status= 1 ";

        const string Query_GetOrderNoDate = @"select top 5 OrderID,ContractNo,ContractDate,StatusCode from SAL_OrderHeader
                                        where CompanyID=@CompanyID and PeriodID =@PeriodID and  CustomerId=@CustomerID order by ContractDate desc";


        const string Query_SalesOrderDetailReportForSugar = @" 
select SAL_OrderHeader.ContractNo as SalesOrderNo, 
    CAST((datepart(year, SAL_OrderHeader.ContractDate)) AS VARCHAR(10)) + '-' + CAST((datepart(month, SAL_OrderHeader.ContractDate)) AS VARCHAR(10))+ '-' 
        + CAST((datepart(day, SAL_OrderHeader.ContractDate)) AS VARCHAR(10)) as Date, 
    SET_Batch.Description as Batch, SAL_OrderHeader.CustomerID, AR_SET_Customer.CustomerName, SAL_OrderDetail.ItemCategoryID, INV_SET_Category.CategoryName, 
    SAL_OrderDetail.ItemID, INV_Item.Description, SET_UnitOfMeasure.UOMName, SAL_OrderDetail.Rate, SAL_OrderDetail.Quantity, SAL_OrderDetail.GrossAmount, 
    SAL_OrderDetail.FEDAmount, SAL_OrderDetail.NetAmount 
from SAL_OrderHeader
    inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
        and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
    Inner Join INV_SET_Category on INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
--and INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID 
    Inner Join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
--and AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        and AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
    Inner Join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID --and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
    inner join SET_UnitOfMeasure on SET_UnitOfMeasure.UOMID = SAL_OrderDetail.ItemUnitID
    left join SET_Batch on SET_Batch.BatchID = SAL_OrderHeader.Season --and SET_Batch.CompanyID = SAL_OrderHeader.CompanyID 
        and SET_Batch.PeriodID = SAL_OrderHeader.PeriodID ";

        const string Query_GetSOforJob = @"
select SAL_OrderHeader.OrderID, SAL_OrderHeader.ContractNo, 
    ISNULL((Select Sum(JobQuantity) from SPL_Job where SPL_Job.OrderID = SAL_OrderHeader.OrderID), 0) JobOrderedQty, 
    (Select Sum(Quantity) from SAL_OrderDetail where SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID) SalesOrderQty 
from SAL_OrderHeader 
    Inner Join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
        and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
    left join SPL_Job on SPL_Job.OrderID = SAL_OrderHeader.OrderID and SAL_OrderHeader.CompanyID = SPL_Job.CompanyID and SAL_OrderHeader.PeriodID = SPL_Job.PeriodID 
where SAL_OrderHeader.CustomerID = @CustomerID and SAL_OrderHeader.CompanyID = @CompanyID and SAL_OrderHeader.PeriodID = @PeriodID and SAL_OrderHeader.Status = 1 
    and [SAL_OrderHeader].[StatusCode] = 1 
    and ISNULL(
    (
        Select Sum(JobQuantity) from SPL_Job where SPL_Job.OrderID = SAL_OrderHeader.OrderID
    ), 0) < 
    (
        Select Sum(Quantity) from SAL_OrderDetail where SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
    ) 
group by SAL_OrderHeader.OrderID, SAL_OrderHeader.ContractNo ";

        const string Query_Update_Flag_Detail = @" 
UPDATE [SAL_OrderDetail] SET [ClosedCode] = @ClosedCode, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue 
WHERE [OrderID] = @OrderID AND [OrderDetailID] = @OrderDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_CheckClosedCode = @" 
SELECT COUNT(ISNULL([SAL_OrderDetail].[ClosedCode], 0)) AS ClosedCodeCount FROM [SAL_OrderDetail] 
WHERE [SAL_OrderDetail].[OrderID] = @OrderID AND [SAL_OrderDetail].[CompanyID] = @CompanyID AND [SAL_OrderDetail].[PeriodID] = @PeriodID 
    AND [SAL_OrderDetail].[Status] = 1 ";

        const string Query_Update_Flag_Header = @" UPDATE [SAL_OrderHeader] SET [ClosedCode] = @ClosedCode, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [OrderID] = @OrderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_Sales_OrderSummary_Report = @"
                                          SELECT distinct
		SAL_OrderHeader.OrderID		AS SO,
		INV_Item.Description		AS ItemDescription,
		SET_UnitofMeasure.UOMName	AS Unit,
		SAL_OrderDetail.Quantity,
		SAL_OrderDetail.Rate		AS RetailPrice,
		SAL_OrderDetail.NetAmount AS Total,
		Case 
			when AR_ReceiptTransactionHeader.Amount < SAL_OrderDetail.NetAmount then 'Partial' 
			when AR_ReceiptTransactionHeader.Amount >= SAL_OrderDetail.NetAmount then 'Yes'
			when AR_ReceiptTransactionHeader.Amount = 0 then 'No'
		else '0' end PaymentReceived,
		
		INV_StockTransactionDetail.TransactionQuantity,
        INV_StockTransactionDetail.StockTransactionID,
		
		ISNULL(SAL_OrderDetail.Quantity + INV_StockTransactionDetail.TransactionQuantity,0) AS Balance,
		INV_StockTransactionDetail.CreateDate
		
		From AR_ReceiptTransactionHeader
		
		 
		 
		INNER join SAL_OrderHeader on		SAL_OrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID
										AND SAL_OrderHeader.ContractNo = AR_ReceiptTransactionHeader.SourceHeaderID
										AND SAL_OrderHeader.ContractNo = SAL_OrderHeader.OrderID AND SAL_OrderHeader.Status = 1
										
		INNER join SAL_OrderDetail on		SAL_OrderDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
										AND SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID 
										
		INNER join INV_Item on				INV_Item.ItemID = SAL_OrderDetail.ItemID --INV_Item.CompanyID = AR_ReceiptTransactionHeader.CompanyID AND 
		
		INNER join SET_UnitofMeasure on 	SAL_OrderDetail.ItemUnitID = SET_UnitofMeasure.UOMID
		
		INNER join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
										AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
		
		INNER join SAL_DeliveryOrderHeader	on SAL_DeliveryOrderHeader.CompanyID = AR_ReceiptTransactionHeader.CompanyID							
										AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
	
		inner join INV_StockTransactionDetail 
						on INV_StockTransactionDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID							
										AND SAL_DeliveryOrderDetail.DOHeaderId = INV_StockTransactionDetail.SourceHeaderID
										
	
								
		-- order by INV_StockTransactionDetail.CreateDate
		
";



        const string Query_Sales_Order_Report_Polymer = @"
SELECT SET_DocumentApprove.DocumentTypeID, SET_DocumentApprove.DocumentNo, INV_Item.Length,INV_Item.Width,INV_Item.Color,INV_Item.GSM,
    Case When Aspnet_users.UserName is null then CreatedUser.UserName else Aspnet_users.UserName end as AuthorizedBy, SAL_OrderDetail.OrderID, 
    INV_Item.Description As ItemDescription, SET_UnitOfMeasure.UOMName, SAL_OrderDetail.Quantity, SAL_OrderHeader.BrokerID, 
    CAST((datepart(year, SET_BATCH.FromDate)) AS VARCHAR(10)) + '-' + CAST((datepart(year, SET_BATCH.ToDate)) AS VARCHAR(10)) as Batch, 
    case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
		when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end AS UnitPrice, 
    ISNULL(
    (
        Case when SAL_OrderDetail.ItemUnitID = INV_Item.PrimaryUOMCode then 1 
            when SAL_OrderDetail.ItemUnitID = INV_Item.VolumeUOMCode then ISNULL(INV_Item.UnitVolume, 1) 
            when SAL_OrderDetail.ItemUnitID = INV_Item.WeightUOMCode then ISNULL(INV_Item.UnitWeight, 1) end
    ), 1) UnitQuantity,                                
    SAL_OrderHeader.ContractDate, SET_DocumentType.DocumentType AS SalesOrderType, SAL_OrderHeader.ContractNo, SAL_OrderHeader.ClosedOn, 
    SET_Term.Description AS Terms, SAL_OrderHeader.Remarks, SAL_OrderDetail.SalesTaxAmount As TaxRate, ASCT.CustomerName, ASCT.AddressLine1 AS CustomerAddress, 
    ASCT.ZipCode AS CustomerZipCode, CityCust.CityName AS CustomerCity, ASCT.PhoneNumber  AS CustomerPhone, 
    SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_OperatingUnit.AddressLine1 AS CompanyAddressLine1, 
    SET_OperatingUnit.AddressLine2 AS CompanyAddressLine2, SET_OperatingUnit.AddressLine3 AS CompanyAddressLine3, SET_OperatingUnit.ZipCode AS CompanyZipCode, 
    CityComp.CityName AS CompanyCity, SET_OperatingUnit.PhoneNumber AS CompanyPhone, SET_OperatingUnit.FaxNumber AS CompanyFax, SET_Currency.CurrencySign, 
    SAL_OrderDetail.ExciseDutyAmount, SAL_OrderDetail.FEDAmount, [SET_OperatingUnit].Logoimage as LogoImage, INV_SET_Category.CategoryName, SAL_OrderHeader.Season, 
    SAL_OrderHeader.ReferenceNo, SAL_OrderHeader.DispatchDate, 
    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_OrderHeader.StatusCode and LookupHeaderID = 5), ''
    ) as DocumentStatus, 
    SAL_OrderDetail.GrossAmount, SAL_OrderDetail.NetAmount, CreatedUser.UserName As PreparedBy , SAL_OrderHeader.ShipTo,
    SAL_OrderDetail.NoteToVendor
FROM SAL_OrderDetail 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_OrderHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID 
	INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --INV_Item.CompanyID = SAL_OrderDetail.CompanyID And 
	INNER JOIN SET_UnitOfMeasure ON SAL_OrderDetail.ItemUnitID = SET_UnitOfMeasure.UOMID 
	INNER JOIN SET_Company ON SET_Company.CompanyId = SAL_OrderDetail.CompanyID 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	LEFT JOIN AR_SET_Customer ASCT ON ASCT.CompanyID = SAL_OrderHeader.CompanyID And ASCT.PeriodID = SAL_OrderHeader.PeriodID
    and ASCT.CustomerId = SAL_OrderHeader.CustomerId 
    LEFT JOIN SET_City AS CityCust ON CityCust.CityID = ASCT.CityID 
	LEFT JOIN SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    INNER JOIN [INV_SET_Category] on INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
--AND [INV_SET_Category].CompanyID = INV_Item.CompanyID 
	left JOIN SET_BATCH ON SET_BATCH.BATCHID = SAL_ORDERHEADER.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID 
    Left Join SET_DocumentApprove on SAL_OrderHeader.CompanyID = SET_DocumentApprove.CompanyID And SAL_OrderHeader.PeriodID = SET_DocumentApprove.PeriodID 
        And SAL_OrderHeader.SaleContractTypeID = SET_DocumentApprove.DocumentTypeID And SAL_OrderHeader.ContractNo = SET_DocumentApprove.DocumentNo 
        AND SET_DocumentApprove.createDate = (select max(CreateDate) from SET_DocumentApprove where companyid = SAL_OrderHeader.companyid 
            and periodid = SAL_OrderHeader.periodid and documenttypeid = SAL_OrderHeader.SaleContractTypeID and documentno = SAL_OrderHeader.ContractNo)
    Left Join Aspnet_users on Aspnet_users.UserID = SET_DocumentApprove.CreatedBy 
    Inner Join Aspnet_users As CreatedUser on CreatedUser.UserID = SAL_OrderHeader.CreatedBy ";

        const string Query_Sales_Order_CustomerWise_Report = @" 
SELECT SAL_OrderDetail.OrderID, SAL_OrderHeader.ContractNo,INV_Item.InventoryItemStatusCode,INV_Set_Category.CategoryName , Sal_DeliveryOrderHeader.DeliveryOrderNo, INV_Item.ItemID, INV_Item.Description As ItemDescription, 
    SET_UnitOfMeasure.UOMName, DocumentTypesGroup.GroupName,
    (SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0)) as DOQuantity,
    SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) as Quantity,
 (
    ((SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)) * sal_orderdetail.BaseAmount) + 
    (((SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)) * sal_orderdetail.BaseAmount) *
    isnull(SAL_OrderDetail.SalesTaxAmount, 0) / 100)) as NetAmounts, 
    (SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)) * sal_orderdetail.BaseAmount as GrossAmount, 
    SAL_OrderDetail.Inclusive, 
    CAST((datepart(year, SET_BATCH.FromDate)) AS VARCHAR(10)) + '-' + CAST((datepart(year, SET_BATCH.ToDate)) AS VARCHAR(10)) as Batch, 
    isnull(SAL_OrderDetail.Rate, 0) AS UnitPrice, Isnull(SAL_OrderDetail.SalesTaxAmount, 0) As TaxRate, ASCT.CustomerName, 
    SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_Currency.CurrencySign, 
    IsNull(SAL_OrderDetail.SalesTaxAmount, 0) SalesTaxAmount, Isnull(SAL_OrderDetail.ExciseDutyAmount, 0) ExciseDutyAmount, 
    Isnull(SAL_OrderDetail.FEDAmount, 0) FEDAmount, SET_OperatingUnit.Logoimage, SAL_OrderHeader.Season, 
    ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_OrderHeader.StatusCode and LookupHeaderID = 5), '') 
        as DocumentStatus,row_number() over(order by SAL_OrderDetail.OrderID ) as Row
FROM SAL_OrderDetail 
INNER JOIN INV_Set_Category ON --INV_Set_Category.CompanyID = SAL_OrderDetail.CompanyID And 
INV_Set_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
        And INV_Set_Category.Status = 1 
    INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1 
    LEFT JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_DeliveryOrderDetail.SalesOrderHeaderID  = SAL_OrderDetail.OrderID And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
        And SAL_DeliveryOrderDetail.Status = 1 
    left Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
        And Sal_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID And Sal_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_OrderHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID 
     Inner Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And  
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --INV_Item.CompanyID = SAL_OrderDetail.CompanyID And  
    INNER JOIN SET_UnitOfMeasure ON SAL_OrderDetail.ItemUnitID = SET_UnitOfMeasure.UOMID 
    INNER JOIN SET_Company ON SET_Company.CompanyId = SAL_OrderDetail.CompanyID 
    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
    INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
    LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
    LEFT JOIN AR_SET_Customer ASCT ON ASCT.CompanyID = SAL_OrderHeader.CompanyID And ASCT.PeriodID = SAL_OrderHeader.PeriodID 
        And ASCT.CustomerID = SAL_OrderHeader.CustomerID 
    LEFT JOIN SET_City AS CityCust ON CityCust.CityID = ASCT.CityID 
    LEFT JOIN SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
	LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ORDERHEADER.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID
    inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
    and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

{0} 
{1} ";

        const string Query_ExpiredSales_Order_ReportDashboard = @" 
SELECT AR_SET_Customer.CustomerName, SAL_OrderDetail.OrderID, SAL_OrderHeader.DispatchDate, INV_Item.Description As ItemDescription, SAL_OrderDetail.Quantity, 
    SAL_OrderHeader.TotalAmount, ROW_NUMBER() OVER (ORDER BY SAL_OrderHeader.CreateDate Desc) AS 'Row' 
FROM SAL_OrderDetail 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1 
	INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --INV_Item.CompanyID = SAL_OrderDetail.CompanyID And 
	LEFT JOIN AR_SET_Customer ON 
--AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID And 
AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
        And AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
where SAL_OrderDetail.CompanyID = @CompanyID And SAL_OrderDetail.PeriodID = @PeriodID And SAL_OrderDetail.Status = 1 
AND SAL_OrderHeader.DispatchDate <=  CONVERT (date, GETDATE())
AND (SAL_OrderHeader.ClosedCode <> 13 or SAL_OrderHeader.ClosedCode is null)";

        const string Query_prnDashboard = @"
select A.DocumentDate,Inv_Item.Description,INV_SET_Category.CategoryName,B.TransactionQuantity,
ROW_NUMBER() OVER (ORDER BY A.DocumentDate Desc) AS 'Row'
from INV_StockTransactionHeader A
inner join INV_StockTransactionDetail B on A.StockTransactionID = B.StockTransactionId 
and  A.CompanyId = B.CompanyId and A.PeriodId = B.PeriodId

Inner join Inv_Item on Inv_Item.ItemId = B.itemId 
--and Inv_Item.CompanyId = B.CompanyId

LEFT JOIN INV_SET_Category ON INV_SET_Category.CategoryID = [INV_Item].ItemTypeID 
--AND INV_SET_Category.[CompanyID] = [INV_Item].[CompanyID] 
        AND INV_SET_Category.[Status] = 1 

where B.status = 1";


        const string Query_PendingSales_Order_ReportDashboard = @"
-- Pending Sales Order

SELECT  
AR_SET_Customer.CustomerName,
SAL_OrderDetail.OrderID, 
SAL_OrderHeader.DispatchDate,
INV_Item.Description As ItemDescription, 
SAL_OrderDetail.Quantity
,SAL_OrderHeader.TotalAmount,
ROW_NUMBER() OVER (ORDER BY SAL_OrderHeader.CreateDate Desc) AS 'Row'
FROM SAL_OrderDetail 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1 
	INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --INV_Item.CompanyID = SAL_OrderDetail.CompanyID And  
	LEFT JOIN AR_SET_Customer ON --AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID And 
AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
        And AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
where SAL_OrderDetail.CompanyID = @CompanyID And SAL_OrderDetail.PeriodID = @PeriodID And SAL_OrderDetail.Status = 1 
AND SAL_OrderHeader.DispatchDate between  CONVERT (date, GETDATE()) and  CONVERT (date, GETDATE() + 10)
";

        const string Query_GetData_byKey1 = @" 
SELECT [Broker].[CustomerName] as BrokerName, * FROM [SAL_ContractHeader] 
 left join AR_Set_Customer on ([SAL_ContractHeader].CustomerID = AR_Set_Customer.CustomerID)
 And AR_Set_Customer.CompanyID = [SAL_ContractHeader].CompanyID
  AND AR_Set_Customer.PeriodID = [SAL_ContractHeader].PeriodID
 
 left join AR_Set_Customer [Broker] on ([SAL_ContractHeader].BrokerID = [Broker].CustomerID)
 And [Broker].CompanyID = [SAL_ContractHeader].CompanyID
 AND [Broker].PeriodID = [SAL_ContractHeader].PeriodID

      inner join Set_PeriodCustom on  
 [SAL_ContractHeader].ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE [SAL_ContractHeader].CompanyID = @CompanyID and [ContractID] = @ContractID and [SAL_ContractHeader].PeriodID = @PeriodID
";

        const string Query_GetData_byKey1SOInProcess = @" 
SELECT [Broker].[CustomerName] as BrokerName, * FROM [SAL_ContractHeader] 
 left join AR_Set_Customer on ([SAL_ContractHeader].CustomerID = AR_Set_Customer.CustomerID)
 And AR_Set_Customer.CompanyID = [SAL_ContractHeader].CompanyID
  AND AR_Set_Customer.PeriodID = [SAL_ContractHeader].PeriodID
 
 left join AR_Set_Customer [Broker] on ([SAL_ContractHeader].BrokerID = [Broker].CustomerID)
 And [Broker].CompanyID = [SAL_ContractHeader].CompanyID
 AND [Broker].PeriodID = [SAL_ContractHeader].PeriodID

      inner join Set_PeriodCustom on  
 [SAL_ContractHeader].ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE [SAL_ContractHeader].CompanyID = @CompanyID and [ContractID] = @ContractID and [SAL_ContractHeader].PeriodID = @PeriodID
";

        const string Query_GetData_byKeyCheckApproveStatus = @" 
 SELECT StatusCode as code, * FROM [SAL_ContractHeader] 
 INNER JOIN [SAL_ContractDetail] ON [SAL_ContractDetail].ContractID = [SAL_ContractHeader].ContractID 
	       	AND [SAL_ContractDetail].[CompanyID] = [SAL_ContractHeader].[CompanyID] 
            AND [SAL_ContractDetail].[PeriodID] = [SAL_ContractHeader].[PeriodID] 
			AND [SAL_ContractDetail].[Status] = 1 
WHERE [SAL_ContractHeader].Status = 1 and [SAL_ContractHeader].CompanyID = @CompanyID 
       and [SAL_ContractDetail].[ContractID] = @ContractID 
       and [SAL_ContractHeader].PeriodID = @PeriodID
       And [SAL_ContractHeader].StatusCode = 1
";

        const string Query_GetData_byKeyCheckApproveStatusHeader = @" 
 SELECT StatusCode as code, * FROM [SAL_ContractHeader] 
 
WHERE [SAL_ContractHeader].Status = 1 and [SAL_ContractHeader].CompanyID = @CompanyID 
       and [SAL_ContractHeader].[ContractID] = @ContractID 
       and [SAL_ContractHeader].PeriodID = @PeriodID
       And [SAL_ContractHeader].StatusCode = 1
";

        const string Query_GetContractNo_byOrderID = @"
Select ContractNo,
Convert (varchar(8),ContractDate,105) as ContractDate,
OrderID
from Sal_OrderHeader
where SAL_OrderHeader.CompanyID = @CompanyID and [OrderID] = @OrderID and SAL_OrderHeader.PeriodID = @PeriodID";

        const string Query_GetSalesReportContractWise = @" 
select SAL_OrderHeader.ContractNo, AR_SET_Customer.CustomerName, SAL_OrderHeader.ContractDate, 
SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0)AS Quantity ,
SAL_OrderDetail.Rate, round(((SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0))*
    (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
		when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end))
	+
	(
	  ISNULL(
	  SAL_OrderDetail.FEDAmount * 
	  ((SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0))
      *
     (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
	  when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end))
	  /
	  100,0)
	),0)	as NetAmount,
Set_Period.PeriodSetName as 'Season', 
    Case SAL_OrderHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
        When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
from SAL_OrderHeader
    INNER JOIN SAL_OrderDetail ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderDetail.Status = 1 
    INNER JOIN AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
--AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
    inner join Set_Period on Set_Period.PeriodID = SAL_OrderHeader.PeriodID 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
where SAL_OrderHeader.Status = 1 
    {0} ";

        const string Query_Get_CheckListReport = @"
select distinct SAL_DOVehicleInformation.Engine,SAL_DOVehicleInformation.Chassis,INV_Set_Category.CategoryName,INV_Item.Description as Item,
SET_Color.ColorName,AS_CheckListInfo.WarrantyBookNo,SET_CheckListDetail.Description as checklistItems, SET_CheckListDetail.Quantity ,ISNULL(AS_ChecklistInfo.Ischecked,0)Ischecked
from SAL_DOVehicleInformation

LEFT join AS_ChecklistInfo on AS_ChecklistInfo.DOVehicleInformationID=SAL_DOVehicleInformation.DOVehicleInformationID
and SAL_DOVehicleInformation.CompanyID=AS_ChecklistInfo.CompanyID
and SAL_DOVehicleInformation.PeriodID=AS_ChecklistInfo.PeriodID

INNER JOIN INV_ITem ON INV_ITem.ItemID = SAL_DOVehicleInformation.ItemID 
--AND INV_ITem.CompanyID = SAL_DOVehicleInformation.CompanyID
Inner Join INV_Set_Category on INV_Set_Category.CategoryID = SAL_DOVehicleInformation.CategoryID
--and INV_Set_Category.CompanyID = SAL_DOVehicleInformation.CompanyID
inner join  SET_Color on SET_Color.ColorID=SAL_DOVehicleInformation.ColorID
left join SET_CheckListHeader on SET_CheckListHeader.ItemID = SAL_DOVehicleInformation.ItemID
and SET_CheckListHeader.CompanyID = SAL_DOVehicleInformation.CompanyID
left join SET_CheckListDetail on SET_CheckListDetail.CheckListID = SET_CheckListHeader.CheckListID
--Left join SET_CheckListDetail on SET_CheckListDetail.CheckListDetailID = AS_CheckListInfo.CheckListDetailID
where SAL_DOVehicleInformation.DOVehicleInformationID=@Chassis

";
        const string Query_Get_CheckListReportByItemID = @"
declare @Qty int
set @Qty = @Quantity
select INV_Item.Description as Item,SET_CheckListDetail.Description as checklistItems, 
SET_CheckListDetail.Quantity , @Qty as Set_Quantity , SET_CheckListDetail.Quantity * @Qty as TotalQuantity

 from SET_CheckListHeader
inner join SET_CheckListDetail on SET_CheckListDetail.CheckListID = SET_CheckListHeader.CheckListID
INNER JOIN INV_ITem ON INV_ITem.ItemID = SET_CheckListHeader.ItemID --AND INV_ITem.CompanyID = SET_CheckListHeader.CompanyID
	where SET_CheckListHeader.ItemID = @Item

";


        const string Query_Get_SalesCertificateReport = @"
SELECT DISTINCT SAL_InvoiceHeader.InvoiceNumber AS InvoiceNo, 0 AS SerialNo, SAL_InvoiceHeader.InvoiceNumber AS CertificateNo, 
    --Convert(Varchar(10), SAL_InvoiceHeader.InvoiceDate, 103) AS Date, 
    SAL_InvoiceHeader.InvoiceDate as Date,
    SAL_InvoiceHeader.CustomerID, 
    AR_SET_Customer.CustomerName + ' ' + AR_SET_Customer.FatherName As CustomerName, AR_SET_Customer.AddressLine1, 
SET_OperatingUnit.AddressLine2+', '+SET_OperatingUnit.AddressLine3+', '+City.CityName+'.' AS SaleOfficeAddress,
	SET_Business.AddressLine2+', '+City.CityName+', '+Set_Country.CountryName+', Tel:'+SET_Business.PhoneNumber+', '+SET_Business.AddressLine3 AS HeadOfficeAddress,
AR_SET_Customer.CityID, SET_City.CityName, 
    AR_SET_Customer.CNICNumber, ARS.CustomerName AS DealerName, Inv_set_category.CategoryName AS ModelCode, INV_Item.Description AS Description, '' AS Version, 
    Set_Batch.Description AS ModelYear, Set_Batch.Description AS ManufacturingYear, SET_Color.ColorID, SET_Color.ColorName, 
    SAL_DOVehicleInformation.Engine AS EngineNo, SAL_DOVehicleInformation.Chassis AS ChassisNo, isnull(INV_Item.UnitWeight,0) AS GrossVehicleWeight, 
    INV_Item.FrontAxleWeight AS MaxWeightFrontAxle, INV_Item.RearAxleWeightI AS MaxWeightRearAxleI, INV_Item.RearAxleWeightII AS MaxWeightRearAxleII, 
    INV_Item.TrailerWeight AS TrailerWeight, INV_Item.FrontWheels, INV_Item.RearWheels, INV_Item.OtherWheels, INV_Item.InventoryItemStatusCode as ItemStatusCode, 
    INV_Item.CubicCentimeters as CC, BilledTo.CustomerName as BilledToName, BilledTo.AddressLine1 as BilledToAddress, BilledtoCity.CityName as BilledToCity, 
    AR_SET_Customer.NTNNumber, INV_Set_Category.CategoryName 
FROM SAL_InvoiceHeader 
    Inner JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
    Inner JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
        AND INV_StockTransactionHeader.CompanyID = SAL_InvoiceDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = SAL_InvoiceDetail.PeriodID 
    Inner JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyId AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    LEFT JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo 
        AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
    LEFT Join Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyId 
        AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND Sal_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        AND Sal_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
    LEFT Join Sal_DeliveryOrderHeader ON Sal_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
        AND Sal_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND Sal_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID 
        AND Sal_DeliveryOrderHeader.Status = 1 
    LEFT Join SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
        AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DODetailID AND SAL_DOVehicleInformation.Status = 1 
    LEFT Join Sal_OrderDetail ON Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyId AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        AND Sal_OrderDetail.OrderDetailID = Sal_DeliveryOrderDetail.SalesOrderDetailID AND Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
        AND Sal_OrderDetail.Status = 1 
    LEFT JOIN Sal_OrderHeader ON Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyId AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID 
        AND Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID AND Sal_OrderHeader.Status = 1 
    LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_OrderDetail.ColorID AND Set_Color.Status = 1 
    Inner JOIN INV_Item ON INV_Item.ItemID = Sal_OrderDetail.ItemID 
--AND INV_Item.CompanyID = Sal_OrderDetail.CompanyID 
AND INV_Item.Status = 1 
    Inner JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID 
--  
        AND INV_Set_Category.Status = 1 
    Inner JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID 
--AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID 
    Inner JOIN AR_SET_Customer ARS ON ARS.CustomerID = SAL_OrderHeader.BrokerID AND ARS.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND ARS.PeriodID = SAL_InvoiceHeader.PeriodID 
    LEFT JOIN SET_City ON SET_City.CityID = AR_SET_Customer.CityID 
    LEFT JOIN [Set_Batch] ON [Set_Batch].[BatchID] = [SAL_OrderHeader].[Season] AND [Set_Batch].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
        AND [Set_Batch].[PeriodID] = [SAL_OrderHeader].[PeriodID] 
    LEft JOIN AR_SET_Customer BilledTo ON BilledTo.CustomerID = SAL_InvoiceHeader.billedtoid AND BilledTo.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND BilledTo.PeriodID = SAL_InvoiceHeader.PeriodID 
    LEFT JOIN SET_City BilledtoCity ON BilledtoCity.CityID = Billedto.CityID 

left JOIN Set_OperatingUnit ON Set_OperatingUnit.CityID = Set_City.CityID
		AND Set_OperatingUnit.Status = 1
		
left JOIN Set_Business ON Set_Business.BusinessId = Set_OperatingUnit.BusinessId
	AND Set_Business.Status = 1
	
left JOIN Set_Country ON SET_Country.CountryID = SET_City.CountryID
	
left JOIN Set_City City ON City.CountryID = Set_Country.CountryID
	AND Set_Country.Status = 1
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceNumber = @InvoiceID 
{0} ";

        const string Query_Get_HFromReport = @"
SELECT DISTINCT SAL_InvoiceHeader.InvoiceNumber AS InvoiceNo, 0 AS SerialNo, SAL_InvoiceHeader.InvoiceNumber AS CertificateNo, 
    --Convert(Varchar(10), SAL_InvoiceHeader.InvoiceDate, 103) AS Date, 
    SAL_InvoiceHeader.InvoiceDate as Date,
    SAL_InvoiceHeader.CustomerID, 
    AR_SET_Customer.CustomerName + ' ' + AR_SET_Customer.FatherName As CustomerName, --AR_SET_Customer.AddressLine1, 
SET_OperatingUnit.AddressLine2+', '+SET_OperatingUnit.AddressLine3+', '+City.CityName+'.' AS SaleOfficeAddress,
	SET_Business.AddressLine2+', '+City.CityName+', '+Set_Country.CountryName+', Tel:'+SET_Business.PhoneNumber+', '+SET_Business.AddressLine3 AS HeadOfficeAddress,
AR_SET_Customer.CityID, SET_City.CityName, 
    AR_SET_Customer.CNICNumber, ARS.CustomerName AS DealerName, Inv_set_category.CategoryName AS ModelCode, INV_Item.Description AS Description, '' AS Version, 
    Set_Batch.Description AS ModelYear, Set_Batch.Description AS ManufacturingYear, SET_Color.ColorID, SET_Color.ColorName, 
    SAL_DOVehicleInformation.Engine AS EngineNo, SAL_DOVehicleInformation.Chassis AS ChassisNo, isnull(INV_Item.UnitWeight,0) AS GrossVehicleWeight, 
    INV_Item.FrontAxleWeight AS MaxWeightFrontAxle, INV_Item.RearAxleWeightI AS MaxWeightRearAxleI, INV_Item.RearAxleWeightII AS MaxWeightRearAxleII, 
    INV_Item.TrailerWeight AS TrailerWeight, INV_Item.FrontWheels, INV_Item.RearWheels, INV_Item.OtherWheels, INV_Item.InventoryItemStatusCode as ItemStatusCode, 
    INV_Item.CubicCentimeters as CC, BilledTo.CustomerName as BilledToName, BilledTo.AddressLine1 as BilledToAddress, BilledtoCity.CityName as BilledToCity, 
    AR_SET_Customer.NTNNumber, INV_Set_Category.CategoryName 
    ,prnbatch.Description as BatchDescription
        ,ARS.Addressline1
     ,oper.Logoimage

FROM SAL_InvoiceHeader 
    Inner JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
    Inner JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
        AND INV_StockTransactionHeader.CompanyID = SAL_InvoiceDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = SAL_InvoiceDetail.PeriodID 
    Inner JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyId AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    LEFT JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo 
        AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
    LEFT Join Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyId 
        AND Sal_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND Sal_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        AND Sal_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
    LEFT Join Sal_DeliveryOrderHeader ON Sal_DeliveryOrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
        AND Sal_DeliveryOrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND Sal_DeliveryOrderHeader.DeliveryOrderID = Sal_DeliveryOrderDetail.DOHeaderID 
        AND Sal_DeliveryOrderHeader.Status = 1 
    LEFT Join SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderDetail.CompanyId 
        AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DODetailID AND SAL_DOVehicleInformation.Status = 1 
    LEFT Join Sal_OrderDetail ON Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyId AND Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        AND Sal_OrderDetail.OrderDetailID = Sal_DeliveryOrderDetail.SalesOrderDetailID AND Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
        AND Sal_OrderDetail.Status = 1 
    LEFT JOIN Sal_OrderHeader ON Sal_OrderHeader.CompanyID = Sal_OrderDetail.CompanyId AND Sal_OrderHeader.PeriodID = Sal_OrderDetail.PeriodID 
        AND Sal_OrderHeader.OrderID = Sal_OrderDetail.OrderID AND Sal_OrderHeader.Status = 1 
    LEFT JOIN Set_Color ON Set_Color.ColorID = Sal_OrderDetail.ColorID AND Set_Color.Status = 1 
    Inner JOIN INV_Item ON INV_Item.ItemID = Sal_OrderDetail.ItemID 
--AND INV_Item.CompanyID = Sal_OrderDetail.CompanyID 
AND INV_Item.Status = 1 
    Inner JOIN INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID 
--  
        AND INV_Set_Category.Status = 1 
    Inner JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID 
--AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID 
    Inner JOIN AR_SET_Customer ARS ON ARS.CustomerID = SAL_OrderHeader.BrokerID AND ARS.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND ARS.PeriodID = SAL_InvoiceHeader.PeriodID 
    LEFT JOIN SET_City ON SET_City.CityID = AR_SET_Customer.CityID 
    LEFT JOIN [Set_Batch] ON [Set_Batch].[BatchID] = [SAL_OrderHeader].[Season] AND [Set_Batch].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
        AND [Set_Batch].[PeriodID] = [SAL_OrderHeader].[PeriodID] 
    LEft JOIN AR_SET_Customer BilledTo ON BilledTo.CustomerID = SAL_InvoiceHeader.billedtoid AND BilledTo.CompanyID = SAL_InvoiceHeader.CompanyID 
        AND BilledTo.PeriodID = SAL_InvoiceHeader.PeriodID 
    LEFT JOIN SET_City BilledtoCity ON BilledtoCity.CityID = Billedto.CityID 

left JOIN Set_OperatingUnit ON Set_OperatingUnit.CityID = Set_City.CityID
		AND Set_OperatingUnit.Status = 1
		
left JOIN Set_Business ON Set_Business.BusinessId = Set_OperatingUnit.BusinessId
	AND Set_Business.Status = 1
	
left JOIN Set_Country ON SET_Country.CountryID = SET_City.CountryID
	
left JOIN Set_City City ON City.CountryID = Set_Country.CountryID
	AND Set_Country.Status = 1

inner join SET_Company on SET_Company.CompanyId = SAL_InvoiceHeader.CompanyID
	and SET_Company.Status = 1 
	inner join Set_OperatingUnit oper  on oper.OperatingUnitID =  SET_Company.OperatingUNitID
	and oper.Status = 1

	left join Set_Batch prnbatch on prnbatch.BatchID =   INV_StockTransactionHeader.BatchID
	and prnbatch.CompanyID =   INV_StockTransactionHeader.CompanyID
	and prnbatch.PeriodID =   INV_StockTransactionHeader.PeriodID
	and prnbatch.Status = 1
	

WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceNumber = @InvoiceID 
{0} ";

        const string Query_Get_VehicleSalesDailyPaymentReceived_Report = @"
SELECT Distinct convert(int,Sal_OrderHeader.ContractNo) AS SalesOrderNo, INV_Set_Category.CategoryName AS ProductType, Sal_OrderHeader.referenceno AS PSONo, 
    SAL_OrderDetail.ItemID AS ProductID, INV_Item.Description AS ProductName, Set_Color.ColorID, SET_Color.ColorName, SAL_OrderHeader.BrokerID AS DealerID, 
    AR_SET_Customer.CustomerName AS DealerName, Cus_Name.CustomerName ,Ar_Set_Bank.BankID, Ar_Set_Bank.BankName, SET_ReceiptMethod.ReceiptMethodName AS TypeOfPayment, 
    SAL_SOPaymentTerms.PaymentDate AS InstrumentDate, SAL_SOPaymentTerms.InstrumentNo as InstrumentNo, 
    SAL_SOPaymentTerms.Amount AS InstrumentAmount, SAL_SOPaymentTerms.Comments AS Remarks ,SAL_SOPaymentTerms.ReceivingDate
    , case Sal_OrderHeader.StatusCode when 5 then'InProcess' when 12 then 'ReleaseForApprove' when 13 then 'PartiallyApproved' when 1 then'Approved' when 14 then 'Discuss' when 15 then 'Cancelled' when 16 then 'Reversed' else '' end as ApproveStatus
from Sal_OrderHeader
inner join SAL_SOPaymentTerms on SAL_SOPaymentTerms.SOHeaderID = Sal_OrderHeader.OrderID
and SAL_SOPaymentTerms.CompanyID = Sal_OrderHeader.CompanyID
and SAL_SOPaymentTerms.PeriodID = Sal_OrderHeader.PeriodID

	Inner JOIN Sal_OrderDetail on Sal_OrderDetail.CompanyID = Sal_OrderHeader.CompanyID AND Sal_OrderDetail.PeriodID = Sal_OrderHeader.PeriodID 
        AND Sal_OrderDetail.OrderID = Sal_OrderHeader.OrderID AND Sal_OrderDetail.Status = 1 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN INV_Set_Category ON 
--INV_Set_Category.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Set_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
	INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_OrderHeader.BrokerID 
--AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
        
        INNER JOIN AR_SET_Customer Cus_Name on Cus_Name.CustomerID = SAL_OrderHeader.CustomerID
        AND Cus_Name.CompanyID = SAL_OrderHeader.CompanyID 
        AND Cus_Name.PeriodID = SAL_OrderHeader.PeriodID 
        
	Left JOIN Sal_DeliveryOrderDetail on Sal_DeliveryOrderDetail.CompanyID = Sal_OrderDetail.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = Sal_OrderDetail.PeriodID 
        AND Sal_DeliveryOrderDetail.SalesOrderHeaderID = Sal_OrderDetail.OrderID AND Sal_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID 
        AND Sal_DeliveryOrderDetail.Status = 1 
	Left JOIN SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = Sal_DeliveryOrderDetail.CompanyID 
        AND SAL_DOVehicleInformation.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND SAL_DOVehicleInformation.DOHeaderID = Sal_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DOVehicleInformation.DODetailID = Sal_DeliveryOrderDetail.DODetailID AND SAL_DOVehicleInformation.Status = 1 
	Left JOIN INV_StockTransactionDetail On INV_StockTransactionDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID AND INV_StockTransactionDetail.SourceHeaderID = Sal_DeliveryOrderDetail.DOHeaderID 
		AND INV_StockTransactionDetail.SourceLineID = Sal_DeliveryOrderDetail.DODetailID AND INV_StockTransactionDetail.Status = 1 
	Left JOIN Sal_InvoiceDetail On Sal_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND Sal_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND Sal_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
		AND Sal_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID AND Sal_InvoiceDetail.Status = 1 
	Left Join AR_ReceiptTransactionDetail On AR_ReceiptTransactionDetail.CompanyID = Sal_InvoiceDetail.CompanyID 
        And AR_ReceiptTransactionDetail.PeriodID = Sal_InvoiceDetail.PeriodID And AR_ReceiptTransactionDetail.InvoiceHeaderID = Sal_InvoiceDetail.InvoiceHeaderID 
		And AR_ReceiptTransactionDetail.InvoiceDetailID = Sal_InvoiceDetail.InvoiceDetailID And Sal_InvoiceDetail.Status = 1 
	Left JOIN AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptTransactionDetail.CompanyID 
        AND AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptTransactionDetail.PeriodID 
        AND AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptTransactionDetail.ReceiptHeaderID AND AR_ReceiptTransactionHeader.Status = 1 
	LEFT JOIN SET_Color ON Sal_OrderDetail.ColorID = SET_Color.ColorID 
	LEFT JOIN Ar_Set_Bank ON SAL_SOPaymentTerms.BankID = Ar_Set_Bank.BankID 
	LEFT JOIN SET_ReceiptMethod ON SAL_SOPaymentTerms.PaymentMode = SET_ReceiptMethod.ReceiptMethodID 
    {0}
    order by convert(int,Sal_OrderHeader.ContractNo)";

        const string Query_Get_SummaryOfDealerWiseSales_Report = @"

SELECT DISTINCT SET_Period.PeriodSetName AS Season, AR_SET_Customer.DistrictID, SET_District.DistrictName, SAL_OrderHeader.BrokerID AS DealerID, 
    AR_SET_Customer.CustomerName AS DealerName, 1 AS HeaderSequence, 'Counter' AS Header, SAL_OrderDetail.ItemCategoryID AS TypeSequence, 
    INV_SET_Category.CategoryName AS Type, Count(INV_SET_Category.CategoryID) AS Value ,CAST(SAL_OrderHeader.CreateDate as date) as CreateDate
FROM SAL_OrderHeader 
    INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
        AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
    INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
--AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID 
LEFT JOIN SET_District ON SET_District.DistrictID = AR_SET_Customer.DistrictID
INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID
--AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID
INNER JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID
--AND INV_Item.CompanyID = INV_SET_Category.CompanyID
INNER JOIN SET_Period ON SET_Period.PeriodID = SAL_OrderHeader.PeriodID
WHERE SAL_OrderHeader.CompanyID = @CompanyID
AND SAL_OrderHeader.PeriodID = @PeriodID
AND CAST(SAL_OrderHeader.CreateDate as date) between @FromDate AND @ToDate
--AND SAL_OrderDetail.ItemCategoryID = @CategoryID
--AND SAL_OrderHeader.CustomerID = @DealerID
GROUP BY
SET_Period.PeriodSetName,
AR_SET_Customer.DistrictID,
SET_District.DistrictName,
SAL_OrderHeader.BrokerID,
AR_SET_Customer.CustomerName,
SAL_OrderDetail.ItemCategoryID,
INV_SET_Category.CategoryName,CAST(SAL_OrderHeader.CreateDate as date)

UNION ALL

SELECT
DISTINCT
SET_Period.PeriodSetName AS Season,
AR_SET_Customer.DistrictID,
SET_District.DistrictName,
SAL_OrderHeader.BrokerID AS DealerID,
AR_SET_Customer.CustomerName AS DealerName,
1 AS HeaderSequence,
'SUM' AS Header,
SAL_OrderDetail.ItemCategoryID AS TypeSequence,
INV_SET_Category.CategoryName AS Type,
SUM(SAL_OrderDetail.NetAmount) AS Value,CAST(SAL_OrderHeader.CreateDate as date)as CreateDate

FROM SAL_OrderHeader
INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID
--AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID
AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID
LEFT JOIN SET_District ON SET_District.DistrictID = AR_SET_Customer.DistrictID
INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID
--AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID
INNER JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID
--AND INV_Item.CompanyID = INV_SET_Category.CompanyID
INNER JOIN SET_Period ON SET_Period.PeriodID = SAL_OrderHeader.PeriodID
WHERE SAL_OrderHeader.CompanyID = @CompanyID
AND SAL_OrderHeader.PeriodID = @PeriodID
AND CAST(SAL_OrderHeader.CreateDate as date)between @FromDate AND @ToDate
--AND SAL_OrderDetail.ItemCategoryID = @CategoryID
--AND SAL_OrderHeader.CustomerID = @DealerID
GROUP BY
SET_Period.PeriodSetName,
AR_SET_Customer.DistrictID,
SET_District.DistrictName,
SAL_OrderHeader.BrokerID,
AR_SET_Customer.CustomerName,
SAL_OrderDetail.ItemCategoryID,
INV_SET_Category.CategoryName,CAST(SAL_OrderHeader.CreateDate as date)

";

        const string Query_Get_SummaryOfOutstandingDispatchPosition_Report = @"SELECT
DISTINCT
SET_Period.PeriodSetName AS Season,
AR_SET_Customer.DistrictID,
SET_District.DistrictName,
SAL_OrderHeader.BrokerID AS DealerID,
AR_SET_Customer.CustomerName AS DealerName,
1 AS HeaderSequence,
'Counter' AS Header,
SAL_OrderDetail.ItemCategoryID AS TypeSequence,
INV_SET_Category.CategoryName AS Type,
Count(INV_SET_Category.CategoryID) AS Value

FROM SAL_OrderHeader
INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID
AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID
AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID
--AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID
AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID
LEFT JOIN SET_District ON SET_District.DistrictID = AR_SET_Customer.DistrictID
INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID
--AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID
INNER JOIN INV_Item ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID
--AND INV_Item.CompanyID = INV_SET_Category.CompanyID
INNER JOIN SET_Period ON SET_Period.PeriodID = SAL_OrderHeader.PeriodID
WHERE SAL_OrderHeader.CompanyID = @CompanyID
AND SAL_OrderHeader.PeriodID = @PeriodID
AND CAST(SAL_OrderHeader.CreateDate as date) = @FromDate
AND SAL_OrderDetail.ItemCategoryID = @CategoryID
AND SAL_OrderHeader.CustomerID = @DealerID
GROUP BY
SET_Period.PeriodSetName,
AR_SET_Customer.DistrictID,
SET_District.DistrictName,
SAL_OrderHeader.BrokerID,
AR_SET_Customer.CustomerName,
SAL_OrderDetail.ItemCategoryID,
INV_SET_Category.CategoryName";

        const string Query_PendingSalesOrderListCustomerWise = @" 
select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, sum(TAB.Balance) as Balance, Tab.DocumentStatus 
from 
(
    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_OrderDetail.Rate, 
        DocumentTypesGroup.GroupName,
        ROUND( (SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0))*
        (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
		when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end),0) AS GrossAmount,
        AR_SET_Customer.CustomerName, AR_SET_Customer.CustomerID, SAL_OrderHeader.ContractNo as Sal_OrderNo, SAL_OrderHeader.ContractDate, 
        SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) as Sal_OrderQuantity,
        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
        isnull(SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0) as Balance, 
        ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) DispatchQty, 
        Case SAL_OrderHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
    from SAL_OrderHeader
        inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
            and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_OrderDetail.Status = 1 and SAL_OrderHeader.Status = 1 
        left join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID 
--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
and INV_Item.Status = 1 
        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_OrderDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
--and AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID and AR_SET_Customer.Status = 1 
        left join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderHeader.OrderID 
            and SAL_DeliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID and SAL_DeliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
            and SAL_DeliveryOrderDetail.Status = 1 
        left join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
            and SAL_DeliveryOrderHeader.CompanyID = SAL_OrderHeader.CompanyID and SAL_DeliveryOrderHeader.PeriodID = SAL_OrderHeader.PeriodID 
            and SAL_DeliveryOrderHeader.Status = 1 
        left join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID 
            and INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
            and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
            and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
            and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
inner join set_documenttype on set_documenttype.documenttypeid =SAL_OrderHeader.SaleContractTypeID
         and set_documenttype.CompanyID=SAL_OrderHeader.CompanyID
         and set_documenttype.PeriodID=SAL_OrderHeader.PeriodID
Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
    where SAL_OrderHeader.CompanyID = @CompanyID and SAL_OrderHeader.PeriodID = @PeriodID and SAL_OrderHeader.Status = 1 
        {0} 
    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_OrderHeader.ContractNo, SAL_OrderHeader.ContractDate, SAL_OrderDetail.Quantity, INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo, SAL_OrderDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, SAL_OrderHeader.StatusCode,SAL_OrderDetail.ReversedQuantity,SAL_OrderDetail.Inclusive,DocumentTypesGroup.GroupName
) TAB 
{1}
group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, Tab.DocumentStatus,TAB.GroupName  ";

        const string PendingSalesOrders = @" select * from 
(
    select AR_SET_Customer.CustomerName, AR_SET_Customer.CustomerID, INV_Set_Category.CategoryName, INV_Item.Description,INV_Item.InventoryItemStatusCode, SAL_OrderHeader.ContractNo as Sal_OrderNo, SAL_OrderHeader.ContractDate, DocumentTypesGroup.GroupName ,
        SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) as Sal_OrderQuantity,
        SAL_DeliveryOrderHeader.DeliveryOrderNo, SAL_DeliveryOrderHeader.DODate, 
        ISnull(SAL_DeliveryOrderDetail.Quantity, 0) as Del_Quantity, 
        isnull(SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0) as Balance, 
        ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) DispatchQty, INV_StockTransactionHeader.DocumentNo as DispatchNo 
        ,case SAL_OrderHeader.statusCode when 1 then 'Approved' when 5 then 'In-Process' else '' end as DispatchStatus
    from SAL_OrderHeader 
        inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
            and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_OrderDetail.Status = 1 and SAL_OrderHeader.Status = 1 
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID  
--and AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID and AR_SET_Customer.Status = 1
     INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND  
	 INNER JOIN INV_Set_Category ON --INV_Set_Category.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Set_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
 
        LEFT join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderHeader.OrderID 
            and SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID and SAL_DeliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
            and SAL_DeliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_DeliveryOrderDetail.Status = 1 
        left join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
            and SAL_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID and SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            and SAL_DeliveryOrderHeader.Status = 1 
        left join INV_StockTransactionDetail on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID 
            and INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
            and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
            and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
            and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        inner join set_documenttype on set_documenttype.documenttypeid =SAL_OrderHeader.SaleContractTypeID
  Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
    where SAL_OrderHeader.CompanyID = @CompanyID and SAL_OrderHeader.PeriodID = @PeriodID 
        {0} 
)tab
{2}
{1} 
 ";

        const string PendingDispatchDetail = @"
 select * from 
(
Select AR_SET_Customer.CustomerName, AR_SET_Customer.CustomerID,SAL_DispatchHeader.DispatchNo,SAL_DispatchHeader.DispatchDate,
INV_SET_Category.CategoryName,INV_Item.Description,INV_Item.InventoryItemStatusCode,Abs(INV_StockTransactionDetail.TransactionQuantity) AS Quantity,
SAL_DeliveryOrderDetail.Quantity AS BaseQuantity,isnull(SAL_DeliveryOrderDetail.Quantity - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0) as Balance, DocumentTypesGroup.GroupName
,case SAL_DispatchHeader.statusCode when 1 then 'Approved' when 5 then 'Invoice' else '' end as DispatchStatus
From SAL_DispatchHeader

INNER JOIN AR_SET_Customer on AR_SET_Customer.CustomerID=SAL_DispatchHeader.customerID 
--and AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID 
and AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID and AR_SET_Customer.Status = 1

INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1

INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = [INV_StockTransactionHeader].[StockTransactionID] AND  [INV_StockTransactionDetail].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1

INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID] AND [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID] AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [SAL_DeliveryOrderDetail].[Status] = 1

INNER JOIN [SAL_DeliveryOrderHeader] ON [SAL_DeliveryOrderHeader].[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] AND [SAL_DeliveryOrderHeader].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_DeliveryOrderHeader].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_DeliveryOrderHeader].[Status] = 1

INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1

INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
--AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_SET_Category].[Status] = 1

INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1

INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
 inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID and SET_DocumentType.CompanyID = SAL_DispatchHeader.CompanyID 
        and SET_DocumentType.PeriodID = SAL_DispatchHeader.PeriodID and SET_DocumentType.status = 1 
   LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And  
inner join Set_PeriodCustom on SAL_DispatchHeader.DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
   where SAL_DispatchHeader.CompanyID = 1 and SAL_DispatchHeader.PeriodID = 1 
  {0}          
)tab
 {1}
";




        const string Query_Get_SummaryOfDealerStockPosition_Report = @"	
Select INV_Item.description, ARS.CustomerID, ARS.CustomerName As DealerName, count(INV_Item.Description) as TotalItems, INV_Set_Category.CategoryID, 
    INV_Set_Category.CategoryName 
FROM SAL_DispatchVehicleInformation 
	INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.CompanyID = SAL_DispatchVehicleInformation.CompanyID 
        AND SAL_DispatchHeader.PeriodID = SAL_DispatchVehicleInformation.PeriodID And SAL_DispatchHeader.DispatchID = SAL_DispatchVehicleInformation.DispatchID 
	INNER JOIN SAL_DOVehicleInformation ON SAL_DispatchVehicleInformation.CompanyID = SAL_DOVehicleInformation.CompanyID 
        AND SAL_DispatchVehicleInformation.PeriodID = SAL_DOVehicleInformation.PeriodID 
        And SAL_DOVehicleInformation.DOVehicleInformationID = SAL_DispatchVehicleInformation.VehicleInformationID 
	INNER JOIN AR_SET_Customer ON --SAL_DispatchHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DispatchHeader.PeriodID = AR_SET_Customer.PeriodID 
        And SAL_DispatchHeader.CustomerID = AR_SET_Customer.CustomerID 
	Left JOIN AR_SET_Customer as Transporters ON SAL_DispatchHeader.CompanyID = Transporters.CompanyID AND SAL_DispatchHeader.PeriodID = Transporters.PeriodID 
		And [SAL_DispatchHeader].TransporterID = Transporters.[CustomerID] 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
		And SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
		And SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
		And SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID 
	INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
		AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
	INNER JOIN SAL_OrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
	INNER JOIN SAL_OrderHeader ON SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
		AND SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID 
	INNER JOIN [SET_Location] ON [SAL_DispatchHeader].[LocationID] = [SET_Location].[LocationID] 
	INNER JOIN [SET_VehicleType] ON [SAL_DispatchHeader].[VehicleTypeID] = [SET_VehicleType].[VehicleTypeID] 
	INNER JOIN [SET_DocumentType] ON [SAL_DispatchHeader].CompanyID = [SET_DocumentType].CompanyID 
		And [SAL_DispatchHeader].PeriodID = [SET_DocumentType].PeriodID 
		And [SAL_DispatchHeader].[DocumentTypeID] = [SET_DocumentType].[DocumentTypeID] 
	LEFT JOIN [SET_Color] ON [SAL_DOVehicleInformation].[ColorID] = [SET_Color].[ColorID] 
	INNER JOIN [INV_Item] ON [SAL_DOVehicleInformation].[ItemID] = [INV_Item].[ItemID]
	LEFT JOIN [SET_ItemCatalog] ON [INV_Item].[ItemID] = [SET_ItemCatalog].[ItemCatalogID] 
	-- new joins
	Inner Join AR_Set_Customer ARS ON ARS.CustomerID = Sal_OrderHeader.CustomerID AND ARS.CompanyID = Sal_OrderHeader.CompanyId 
        AND ARS.PeriodID = Sal_OrderHeader.PeriodID AND ARS.Status = 1 
	left Join AR_Set_Customer ARST ON ARST.CustomerID = SAL_DispatchHeader.TransporterID AND ARST.CompanyID = SAL_DispatchHeader.CompanyID 
        AND ARST.PeriodID = SAL_DispatchHeader.CompanyID 
        
        Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.DocumentNo = Sal_Dispatchheader.DispatchNo 
        AND SAL_DispatchHeader.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
        AND INV_StockTransactionHeader.CompanyID = Sal_Dispatchheader.CompanyId AND INV_StockTransactionHeader.PeriodID = Sal_Dispatchheader.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
      Inner Join INV_Set_Category on --INV_Set_Category.CompanyID=INV_Item.CompanyID And 
INV_Set_Category.CategoryID=INV_Item.ItemTypeID
Where
SAL_DispatchHeader.CompanyID = @CompanyID 
		AND SAL_DispatchHeader.PeriodID = @PeriodID
        AND SAL_DispatchHeader.Status=1
        AND SAL_DispatchHeader.StatusCode=1 
		AND(CAST(SAL_DispatchHeader.CreateDate as date) between @FromDate  and @ToDate)
 		{0}
Group By
	ARS.CustomerName,
	INV_SET_Category.CategoryName,
	ARS.CustomerID,
	INV_Item.Description,INV_Set_Category.CategoryID";

        const string Query_Get_SummaryOfDealerStockPosition_ReportParametersquery = @"Select INV_Item.description,
 ARS.CustomerID, ARS.CustomerName As  DealerName,count(INV_Item.Description) as TotalItems, INV_Set_Category.CategoryID,
	 INV_Set_Category.CategoryName 
     FROM [SAL_DispatchVehicleInformation] 
	INNER JOIN [SAL_DispatchHeader] ON [SAL_DispatchHeader].[CompanyID] = [SAL_DispatchVehicleInformation].[CompanyID] 
		AND [SAL_DispatchHeader].[PeriodID] = [SAL_DispatchVehicleInformation].[PeriodID] 
		And [SAL_DispatchHeader].[DispatchID] = [SAL_DispatchVehicleInformation].[DispatchID] 
	INNER JOIN [SAL_DOVehicleInformation] ON [SAL_DispatchVehicleInformation].[CompanyID] = [SAL_DOVehicleInformation].[CompanyID] 
		AND [SAL_DispatchVehicleInformation].[PeriodID] = [SAL_DOVehicleInformation].[PeriodID] 
		And [SAL_DOVehicleInformation].[DOVehicleInformationID] = [SAL_DispatchVehicleInformation].[VehicleInformationID] 
	INNER JOIN [AR_SET_Customer] ON [SAL_DispatchHeader].[PeriodID] = [AR_SET_Customer].[PeriodID] And [SAL_DispatchHeader].[CustomerID] = [AR_SET_Customer].[CustomerID] 
	Left JOIN [AR_SET_Customer] as Transporters ON [SAL_DispatchHeader].[CompanyID] = Transporters.[CompanyID] 
		AND [SAL_DispatchHeader].[PeriodID] = Transporters.[PeriodID] 
		And [SAL_DispatchHeader].TransporterID = Transporters.[CustomerID] 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
		And SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
		And SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
		And SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID 
	INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
		AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
	INNER JOIN SAL_OrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
		AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
	INNER JOIN SAL_OrderHeader ON SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
		AND SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID 
	INNER JOIN [SET_Location] ON [SAL_DispatchHeader].[LocationID] = [SET_Location].[LocationID] 
	INNER JOIN [SET_VehicleType] ON [SAL_DispatchHeader].[VehicleTypeID] = [SET_VehicleType].[VehicleTypeID] 
	INNER JOIN [SET_DocumentType] ON [SAL_DispatchHeader].CompanyID = [SET_DocumentType].CompanyID 
		And [SAL_DispatchHeader].PeriodID = [SET_DocumentType].PeriodID 
		And [SAL_DispatchHeader].[DocumentTypeID] = [SET_DocumentType].[DocumentTypeID] 
	LEFT JOIN [SET_Color] ON [SAL_DOVehicleInformation].[ColorID] = [SET_Color].[ColorID] 
	INNER JOIN [INV_Item] ON [SAL_DOVehicleInformation].[ItemID] = [INV_Item].[ItemID]
	LEFT JOIN [SET_ItemCatalog] ON [INV_Item].[ItemID] = [SET_ItemCatalog].[ItemCatalogID] 
	-- new joins
	Inner Join AR_Set_Customer ARS ON ARS.CustomerID = Sal_OrderHeader.CustomerID AND ARS.CompanyID = Sal_OrderHeader.CompanyId 
        AND ARS.PeriodID = Sal_OrderHeader.PeriodID AND ARS.Status = 1 
	left Join AR_Set_Customer ARST ON ARST.CustomerID = SAL_DispatchHeader.TransporterID AND ARST.CompanyID = SAL_DispatchHeader.CompanyID 
        AND ARST.PeriodID = SAL_DispatchHeader.CompanyID 
        
        Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.DocumentNo = Sal_Dispatchheader.DispatchNo 
        AND SAL_DispatchHeader.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
        AND INV_StockTransactionHeader.CompanyID = Sal_Dispatchheader.CompanyId AND INV_StockTransactionHeader.PeriodID = Sal_Dispatchheader.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
      Inner Join INV_Set_Category on --INV_Set_Category.CompanyID=INV_Item.CompanyID And 
INV_Set_Category.CategoryID=INV_Item.ItemTypeID
Where 
Sal_Dispatchheader.CompanyID =@CompanyID
AND Sal_Dispatchheader.PeriodID = @PeriodID
AND Sal_Dispatchheader.Status = 1 
AND Sal_Dispatchheader.StatusCode=1
AND(CAST(SAL_DispatchHeader.CreateDate as date) between @FromDate  and @ToDate)
 		AND (AR_SET_Customer.CustomerID =@CustomerID )
 		{0}
Group By
	ARS.CustomerName,ARS.CustomerID,
	INV_SET_Category.CategoryName,
	INV_Item.Description,INV_Set_Category.CategoryID";



        const string Summary_OfOutstandingBookingPosition_ReportQ = @"SELECT SET_District.DistrictName as Region, AR_SET_Customer.CustomerName as Dealer, INV_Item.[Description] as Vehicle, 
	        count(INV_SET_Category.CategoryName) as CountCat 
        FROM SAL_OrderDetail 
	        INNER JOIN SAL_OrderHeader ON SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID AND SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
		        AND SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
	        INNER JOIN AR_SET_Customer ON SAL_OrderHeader.BrokerID = AR_SET_Customer.CustomerId	--AND SAL_OrderHeader.CompanyID = AR_SET_Customer.CompanyID 
		        AND SAL_OrderHeader.PeriodID = AR_SET_Customer.PeriodID 
	        INNER JOIN SET_District ON AR_SET_Customer.Districtid = SET_District.Districtid 
	        INNER JOIN INV_Item ON SAL_OrderDetail.ItemID = INV_Item.ItemID	--AND SAL_OrderDetail.CompanyID = INV_Item.CompanyID 
	        INNER JOIN INV_SET_Category ON INV_Item.ItemTypeID = INV_SET_Category.CategoryID --AND INV_Item.CompanyID = INV_SET_Category.CompanyID 
        WHERE SAL_OrderDetail.CreateDate > @FromDate AND (SAL_OrderDetail.ItemCategoryID = 29 OR 29 Is NULL) or (SAL_OrderDetail.ItemCategoryID = 28 OR 28 Is NULL) 
	        AND SAL_OrderDetail.ClosedCode = NULL AND SAL_OrderDetail.CompanyID = @CompanyID 
	        AND SAL_OrderDetail.PeriodID = @PeriodID 
        Group By SET_District.DistrictName, AR_SET_Customer.CustomerName, INV_Item.[Description] 
        order by SET_District.DistrictName, AR_SET_Customer.CustomerName, INV_Item.[Description]
        ";

        const string Query_QuantityInfoForSalesOrder = @" 
Select PurchasedQuantity.itemID, PurchasedTransactionQuantity, Isnull(SoldTransactionQuantity, 0) SoldTransactionQuantity, 
    Isnull(DispatchedQuantity, 0) DispatchedQuantity, Isnull((Isnull(PurchasedTransactionQuantity, 0) - Isnull(SoldTransactionQuantity, 0)), 0) BalanceQuantity 
from 
(
    Select Sum(TransactionQuantity) PurchasedTransactionQuantity, ItemID from INV_StockTransactionDetail 
        JOIN Inv_StockTransactionHeader on Inv_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        JOIN Set_DocumentType on Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID AND TransactionImpact = 'Positive' 
            AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.CompanyID= INV_StockTransactionDetail.PeriodID 
    Where ItemID = @ItemID AND INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
        AND (Inv_StockTransactionHeader.BatchID = @BatchID OR @BatchID IS NULL) 
    Group BY ItemID
) PurchasedQuantity 
    Left JOIN 
    (
        Select IsNULL(Sum(Quantity), 0) SoldTransactionQuantity, ItemId From SAL_ContractDetail 
            JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID AND SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID 
                AND SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID 
        Where SAL_ContractDetail.ItemID = @ItemID AND SAL_ContractDetail.CompanyID = @CompanyID AND SAL_ContractDetail.PeriodID = @PeriodID 
            AND (SAL_ContractHeader.Season = @BatchID OR @BatchID IS NULL) 
        Group BY SAL_ContractDetail.ItemID
    ) SoldQuantity ON SoldQuantity.ItemID = PurchasedQuantity.ItemID 
    LEFT JOIN 
    (
        Select Sum(TransactionQuantity) DispatchedQuantity, ItemID from INV_StockTransactionDetail 
            JOIN Inv_StockTransactionHeader on Inv_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
                AND Inv_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
                AND Inv_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
            JOIN Set_DocumentType on Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID AND TransactionImpact = 'Negative' 
                AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.CompanyID= INV_StockTransactionDetail.PeriodID 
        Where ItemID = @ItemID AND INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
            AND (Inv_StockTransactionHeader.BatchID = @BatchID OR @BatchID IS NULL) 
        Group BY ItemID
    ) Dispatched ON Dispatched.ItemID = PurchasedQuantity.ItemID ";



        const string Query_GetDOData_byKey_Polymer = @"     
SELECT SAL_OrderHeader.ContractDate, SAL_OrderHeader.BilledTo, AR_Set_Customer.CustomerName, SAL_OrderHeader.ReferenceNo, SAL_OrderHeader.CustomerID, 
    SAL_OrderHeader.Season  , SAL_OrderHeader.ShipTo
FROM SAL_OrderHeader 
    LEFT JOIN AR_Set_Customer ON SAL_OrderHeader.BilledTo = AR_Set_Customer.CustomerID --AND SAL_OrderHeader.CompanyID = AR_Set_Customer.CompanyID 
        AND SAL_OrderHeader.PeriodID = AR_Set_Customer.PeriodID 
Where SAL_OrderHeader.CompanyID = @CompanyID AND SAL_OrderHeader.PeriodID = @PeriodID AND SAL_OrderHeader.OrderID = @OrderID ";

        const string Query_GetDocType = @"select distinct SaleContractTypeID , SET_DocumentType.Documenttype
from SAL_orderheader
 
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  SAL_orderheader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_ORderHeader.CompanyID
AND SET_DocumentType.PeriodID = SAL_ORderHeader.PeriodID";

        const string Query_GetDocTypeSaleContract = @"select distinct SaleContractTypeID , SET_DocumentType.Documenttype
from SAL_ContractHeader
 
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  SAL_ContractHeader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID
AND SET_DocumentType.PeriodID = SAL_ContractHeader.PeriodID
where SAL_ContractHeader.CompanyID = @CompanyID
";

        const string Query_GetDocumentNo = @"select distinct SaleContractTypeID ,SET_DocumentType.DocumentType,ContractNo,SAL_OrderHeader.OrderID,ContractDate from SAL_OrderHeader
 inner join SAL_OrderDetail on  SAL_OrderDetail .OrderID = SAL_OrderHeader.OrderID
 and SAL_OrderDetail .CompanyID = SAL_OrderHeader.CompanyID
 and SAL_OrderDetail .PeriodID= SAL_OrderHeader.PeriodID
 and SAL_OrderDetail .status = 1
 left join SAL_DeliveryOrderDetail   on SAL_DeliveryOrderDetail.SalesOrderHeaderID  = SAL_OrderHeader.OrderID
 and SAL_DeliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID
 and SAL_DeliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID

 and SAL_DeliveryOrderDetail.status = 1


left join SAL_ReverseDetail on SAL_ReverseDetail.sourceHeaderID =  SAL_DeliveryOrderDetail.DOHeaderID
AND SAL_ReverseDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
AND SAL_ReverseDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID

--inner join ApplicationMenu ap on ApplicationMenu.MenuID = SET_DocumentType.MenuID


 inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  SAL_orderheader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_ORderHeader.CompanyID
AND SET_DocumentType.PeriodID = SAL_ORderHeader.PeriodID

Where Not exists (select * from AR_ReceiptTransactionHeader
        where AR_ReceiptTransactionHeader.SourceHeaderID = [SAL_OrderHeader].orderID
        and AR_ReceiptTransactionHeader.CompanyID = [SAL_OrderHeader].CompanyID
        and AR_ReceiptTransactionHeader.PeriodID = [SAL_OrderHeader].PeriodID )
        
 AND SAL_DeliveryOrderDetail.DODetailID is null
 
AND Sal_OrderHeader.CompanyID= 1
AND Sal_OrderHeader.PeriodID= 1
and Sal_OrderHeader.StatusCode = 1

{0} 

--AND menuPath = '/Sales/VehicleSales/SalesOrder_frm.aspx'
 
union all select SaleContractTypeID ,SET_DocumentType.DocumentType,ContractNo,SAL_OrderHeader.OrderID,ContractDate 
from SAL_ReverseDetail 

inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_ReverseDetail.SourceHeaderID
AND SAL_DeliveryOrderDetail.CompanyID = SAL_ReverseDetail.CompanyID
AND SAL_DeliveryOrderDetail.PeriodID = SAL_ReverseDetail.PeriodID

inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
AND SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
AND SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID

inner join SET_DocumentType on SET_DocumentType.Documenttypeid = SAL_OrderHEader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_OrderHEader.CompanyID
AND SET_DocumentType.PeriodID = SAL_OrderHEader.PeriodID

inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID

AND  menuPath like '%/Sales/VehicleSales/SalesOrder_frm.aspx%' 

where SAL_DeliveryOrderDetail.Quantity - SAL_DeliveryOrderDetail.ReversedQTy = 0";


        #region SO Scheduling

        const string Query_SelectAll_SearchingSOS = @" 
SELECT SAL_ContractHeader.ContractID, SAL_ContractHeader.CompanyID, SAL_ContractHeader.PeriodID, SAL_ContractHeader.SaleContractTypeID, SAL_ContractHeader.CustomerID, 
    SAL_ContractHeader.BrokerID, SAL_ContractHeader.PaymentTermID, SAL_ContractHeader.Season, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, 
    SAL_ContractHeader.PaymentDate, SAL_ContractHeader.DispatchDate, SAL_ContractHeader.DTRENo, SAL_ContractHeader.DTREDate, SAL_ContractHeader.IsApproved, 
    SAL_ContractHeader.ApprovedBy, SAL_ContractHeader.ApprovalDate, SAL_ContractHeader.Remarks, SAL_ContractHeader.IsPaid, SAL_ContractHeader.PaidAmount, SAL_ContractHeader.Status, 
    SAL_ContractHeader.StatusCode, SAL_ContractHeader.CreatedBy, SAL_ContractHeader.CreateDate, SAL_ContractHeader.UpdatedBy, SAL_ContractHeader.UpdateDate, 
    SAL_ContractHeader.CreatedByValue, SAL_ContractHeader.UpdatedByValue, AR_SET_Customer.CustomerName AS 'Customer', tbl_Broker.CustomerName AS 'Broker', 
    abs(sum(SAL_ContractDetail.Quantity)) as Quantity ,
Set_PeriodCustom.PeriodSetName
FROM SAL_ContractHeader 
    INNER JOIN SAL_ContractDetail ON SAL_ContractDetail.ContractID = SAL_ContractHeader.ContractID AND SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID 
        AND SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID AND SAL_ContractDetail.Status = 1 
    LEFT JOIN INV_Item ON INV_Item.ItemID = SAL_ContractDetail.ItemID 
--AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
AND INV_Item.Status = 1 
    INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_ContractHeader.CustomerID --AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = SAL_ContractHeader.PeriodID AND AR_SET_Customer.Status = 1 
    LEFT JOIN AR_SET_Customer AS tbl_Broker ON tbl_Broker.CustomerID = SAL_ContractHeader.BrokerID AND tbl_Broker.CompanyID = SAL_ContractHeader.CompanyID 
        AND tbl_Broker.PeriodID = SAL_ContractHeader.PeriodID AND tbl_Broker.Status = 1 

    inner join Set_PeriodCustom on SAL_ContractHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID

group by SAL_ContractHeader.ContractID, SAL_ContractHeader.CompanyID, SAL_ContractHeader.PeriodID, SAL_ContractHeader.SaleContractTypeID, SAL_ContractHeader.CustomerID, 
    SAL_ContractHeader.BrokerID, SAL_ContractHeader.PaymentTermID, SAL_ContractHeader.Season, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, 
    SAL_ContractHeader.PaymentDate, SAL_ContractHeader.DispatchDate, SAL_ContractHeader.DTRENo, SAL_ContractHeader.DTREDate, SAL_ContractHeader.IsApproved, 
    SAL_ContractHeader.ApprovedBy, SAL_ContractHeader.ApprovalDate, SAL_ContractHeader.Remarks, SAL_ContractHeader.IsPaid, SAL_ContractHeader.PaidAmount, SAL_ContractHeader.Status, 
    SAL_ContractHeader.StatusCode, SAL_ContractHeader.CreatedBy, SAL_ContractHeader.CreateDate, SAL_ContractHeader.UpdatedBy, SAL_ContractHeader.UpdateDate, 
    SAL_ContractHeader.CreatedByValue, SAL_ContractHeader.UpdatedByValue, AR_SET_Customer.CustomerName, tbl_Broker.CustomerName,Set_PeriodCustom.PeriodSetName ";

        const string Query_SelectAll_Searching_WithPagingSOS = @" SELECT *, ROW_NUMBER() OVER (ORDER BY Cast(tbl_POH.[ContractNo] as int) DESC) AS 'Row' 
FROM 
(
    SELECT [SAL_ContractHeader].[ContractID], [SAL_ContractHeader].[CompanyID], [SAL_ContractHeader].[PeriodID], [SAL_ContractHeader].[SaleContractTypeID], 
        [SAL_ContractHeader].[CustomerID], [SAL_ContractHeader].[BrokerID], [SAL_ContractHeader].[PaymentTermID], [SAL_ContractHeader].[Season], [SAL_ContractHeader].[ContractNo], 
        [SAL_ContractHeader].[ContractDate], [SAL_ContractHeader].[PaymentDate], [SAL_ContractHeader].[DispatchDate], [SAL_ContractHeader].[DTRENo], [SAL_ContractHeader].[DTREDate], 
        [SAL_ContractHeader].[IsApproved], [SAL_ContractHeader].[ApprovedBy], [SAL_ContractHeader].[ApprovalDate], [SAL_ContractHeader].[Remarks], [SAL_ContractHeader].[IsPaid], 
        [SAL_ContractHeader].[PaidAmount], [SAL_ContractHeader].[StatusCode], [SAL_ContractHeader].[Status], [SAL_ContractHeader].[CreatedBy], [SAL_ContractHeader].[CreateDate], 
        [SAL_ContractHeader].[UpdatedBy], [SAL_ContractHeader].[UpdateDate], [SAL_ContractHeader].[CreatedByValue], [SAL_ContractHeader].[UpdatedByValue], 
        [AR_SET_Customer].[CustomerName] AS 'Customer', tbl_Broker.[CustomerName] AS 'Broker', abs(sum([SAL_ContractDetail].Quantity)) as Quantity
,[SAL_ContractHeader].ContractNo as DocumentNo,Set_PeriodCustom.PeriodSetName,[SAL_ContractHeader].ContractDate as DocumentDate
    FROM [SAL_ContractHeader] 
        INNER JOIN [SAL_ContractDetail] ON [SAL_ContractDetail].[ContractID] = [SAL_ContractHeader].[ContractID] AND [SAL_ContractDetail].[CompanyID] = [SAL_ContractHeader].[CompanyID] 
            AND [SAL_ContractDetail].[PeriodID] = [SAL_ContractHeader].[PeriodID] AND [SAL_ContractDetail].[Status] = 1 
        LEFT JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_ContractDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
            AND [INV_Item].[Status] = 1 
        INNER JOIN [AR_SET_Customer] ON [AR_SET_Customer].[CustomerID] = [SAL_ContractHeader].[CustomerID] 
--            AND [AR_SET_Customer].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
AND [AR_SET_Customer].[PeriodID] = [SAL_ContractHeader].[PeriodID] 
            AND [AR_SET_Customer].[Status] = 1 
        LEFT JOIN [AR_SET_Customer] AS tbl_Broker ON tbl_Broker.[CustomerID] = [SAL_ContractHeader].[BrokerID] AND tbl_Broker.[CompanyID] = [SAL_ContractHeader].[CompanyID] 
            AND tbl_Broker.[PeriodID] = [SAL_ContractHeader].[PeriodID] AND tbl_Broker.[Status] = 1 

inner join Set_PeriodCustom on [SAL_ContractHeader].ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID

        {0}
    {AdditionalParameters}

group by [SAL_ContractHeader].[ContractID], [SAL_ContractHeader].[CompanyID], [SAL_ContractHeader].[PeriodID], [SAL_ContractHeader].[SaleContractTypeID], 
        [SAL_ContractHeader].[CustomerID], [SAL_ContractHeader].[BrokerID], [SAL_ContractHeader].[PaymentTermID], [SAL_ContractHeader].[Season], [SAL_ContractHeader].[ContractNo], 
        [SAL_ContractHeader].[ContractDate], [SAL_ContractHeader].[PaymentDate], [SAL_ContractHeader].[DispatchDate], [SAL_ContractHeader].[DTRENo], [SAL_ContractHeader].[DTREDate], 
        [SAL_ContractHeader].[IsApproved], [SAL_ContractHeader].[ApprovedBy], [SAL_ContractHeader].[ApprovalDate], [SAL_ContractHeader].[Remarks], [SAL_ContractHeader].[IsPaid], 
        [SAL_ContractHeader].[PaidAmount], [SAL_ContractHeader].[StatusCode], [SAL_ContractHeader].[Status], [SAL_ContractHeader].[CreatedBy], [SAL_ContractHeader].[CreateDate], 
        [SAL_ContractHeader].[UpdatedBy], [SAL_ContractHeader].[UpdateDate], [SAL_ContractHeader].[CreatedByValue], [SAL_ContractHeader].[UpdatedByValue], 
        [AR_SET_Customer].[CustomerName], tbl_Broker.[CustomerName],Set_PeriodCustom.PeriodSetName
) AS tbl_POH ";

        #endregion


        const string Query_DispatchStatus_Report = @"
select AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DeliveryOrderNo,
SAL_DeliveryOrderDetail.Quantity as DOQuantity,isnull(sum(abs(inv_stockTransactionDetail.TransactionQuantity)),0) as DispatchQuantity
,SAL_DeliveryOrderDetail.Quantity - isnull(sum(abs(inv_stockTransactionDetail.TransactionQuantity)),0) as Balance
from SAL_DeliveryOrderHeader
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
--and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID
left join inv_stockTransactionDetail on inv_stockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
and inv_stockTransactionDetail.SourceHeaderID =  SAL_DeliveryOrderHeader.DeliveryOrderID
and inv_stockTransactionDetail.SourceCode =  SAL_DeliveryOrderHeader.DeliveryOrderNo
{0}
group by AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DeliveryOrderNo,SAL_DeliveryOrderDetail.Quantity";

        const string Query_GetData_byDocno = @" Select * from SAL_OrderHeader 
 Where Status=1
AND  ContractNo=@DocNO
AND SaleContractTypeID=@DocType
AND  CompanyID=@CompanyID 
AND PeriodID=@PeriodID";
        #endregion


        const string Query_SOClose = @"
UPDATE SAL_ContractHeader
SET ClosedCode = 1 , ClosedOn = GETDATE() , Remarks =  @remarks
where SAL_ContractHeader.SaleContractTypeID =@DocumentTypeID AND SAL_ContractHeader.ContractID = @SaleOrderID
AND SAL_ContractHeader.CompanyID = @CompanyID  AND SAL_ContractHeader.PeriodID = @PeriodID 

UPDATE SAL_ContractDetail
SET ClosedCode = 1 , ClosedOn = GETDATE() 
where SAL_ContractDetail.ContractID=@SaleOrderID
AND SAL_ContractDetail.CompanyID = @CompanyID  AND SAL_ContractDetail.PeriodID = @PeriodID";

        #region Class Methods





        public static DataRow GetDataByKey1(int companyID, int contractID, int periodID, int FiscalID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", FiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey1, sqlparams);

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

        public static DataRow GetDataByKey1SOInProcess(int companyID, int contractID, int periodID, int FiscalID)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", FiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey1SOInProcess, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ds.Tables[0].Columns[1].ColumnName = "OrderID";
                    dr = ds.Tables[0].Rows[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }



        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
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
                    StringBuilder query = new StringBuilder(Query_SelectAll_Searching_WithPaging.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_ContractHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAll_Searching.ToString().Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_OrderHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_OrderHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_OrderDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
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





        public static DataTable GetAllDataWithOtherTablesWithDetail(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
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
                    StringBuilder query = new StringBuilder(Query_SelectAll_SearchingWithDetail_WithPaging.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_ContractHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(FnI.QueryResourceLibrary.Sales.SAL_OrderHeader.Query_SelectAll_SearchingWithDetail.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_OrderHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_OrderHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_OrderDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
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

        #region SO Scheduling

        public static DataTable GetAllDataWithOtherTablesSOS(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            bool filterCategoryWise = false, Nullable<Guid> currentUserID = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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
                    string query = Query_SelectAll_Searching_WithPagingSOS.Replace("{AdditionalParameters}", strWhere);

                    if (filterCategoryWise)
                        query = string.Format(query, @" Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID 
                            And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    else
                        query = string.Format(query, string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    string query = Query_SelectAll_SearchingSOS.Replace("{AdditionalParameters}", strWhere);

                    if (filterCategoryWise)
                        query = string.Format(query, @" Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID 
                            And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    else
                        query = string.Format(query, string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        #endregion

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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
                    string query = FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAll_Searching_WithPaging_ForApprove.Replace("{AdditionalParameters}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAll_Searching_ForApprove + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int OrderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@OrderID", OrderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int OrderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@OrderID", OrderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.query_Select_DataByKey_ForApprove, sqlparams);

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

        public static DataTable GetAllSeasons(Nullable<int> companyID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllSeasons, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataByContractID(int CompanyID, int PeriodID, int ContractID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ContractNo", ContractID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataByContractID, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool InsertSOHeaderAndDetail(List<SAL_ContractDetail> colSODetail, List<SAL_SOScheduling> SOSDetail, SET_DocumentApprove da, int CurrentFiscalID = 0, SqlTransaction sql = null)
        {
            SqlTransaction sqltrans;
            bool result = false;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertSalesOrder");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {



                this.ContractNo = SET_DocumentSequence.ManualDocumentSequence(f_SaleContractTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();


                this.Insert(sqltrans);

                foreach (SAL_ContractDetail sod in colSODetail)
                {


                    sod.ContractDetailID = long.Parse(SAL_ContractDetail.GetNewContractDetailID(CompanyID, ContractID, PeriodID, sqltrans).ToString());
                    long tempOrderDetailID = sod.ContractDetailID;
                    sod.Insert(sqltrans);
                    if (SOSDetail != null)
                    {
                        if (SOSDetail.Count > 0)
                        {
                            var colPOS = (from drow in SOSDetail
                                          where drow.OrderDetailID == tempOrderDetailID
                                          select drow);

                            foreach (SAL_SOScheduling sos in colPOS)
                            {
                                //int tempPOSchedulingID = pos.PODetailID;

                                sos.OrderDetailID = short.Parse(sod.ContractDetailID.ToString());
                                sos.OrderID = this.ContractID;
                                sos.Insert(sqltrans);

                            }
                        }
                    }

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.DocumentNo = this.ContractNo;
                da.Insert(sqltrans);

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
                        SqlHelper.RollBackTransaction(sqltrans, "InsertSalesOrder");
                    }
                    else
                    { }
                }
            }

            return result;
        }



        public bool UpdateSOHeaderAndDetail(List<SAL_ContractDetail> colAddSODetail, List<SAL_ContractDetail> colUpdateSODetail, List<SAL_ContractDetail> colDeleteSODetail, SET_DocumentApprove da, int CurrentFiscalID,bool IsSoEditable)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateSO");
            DataTable dtDetails = new DataTable();
            try
            {
                if (IsSoEditable)
                    dtDetails = SAL_DeliveryOrderHeader.GetDOandSODatabyContactID(this.CompanyID, this.PeriodID, this.ContractID);

                //update customer in Do form
                //if (dtDetails != null && dtDetails.Rows.Count > 0)
                //{
                //    foreach (DataRow item in dtDetails.Rows)
                //    {
                //        if (int.Parse(item["CustomerID"].ToString()) != this.CustomerID)
                //        {
                //            if (int.Parse(item["IsDispatch"].ToString()) == 0)
                //                SAL_DeliveryOrderDetail.UpdateCustomerID(this.CompanyID, this.PeriodID, int.Parse(item["DOHeaderID"].ToString()), this.CustomerID, sqltrans);
                //            else
                //                throw new BusinessLogicException("Can not change Customer. Dispatch has been made");
                //        }
                //        if (int.Parse(item["BrokerID"].ToString()) != this.BrokerID)
                //        {
                //            if (int.Parse(item["IsDispatch"].ToString()) == 0)
                //                SAL_DeliveryOrderDetail.UpdateBrokerID(this.CompanyID, this.PeriodID, int.Parse(item["DOHeaderID"].ToString()), this.CustomerID, sqltrans);
                //            else
                //                throw new BusinessLogicException("Can not change Agent. Dispatch has been made");
                //        }
                //    }
                //}


                this.UpdateDataByKey(sqltrans);

                foreach (SAL_ContractDetail sodAdd in colAddSODetail)
                {
                    sodAdd.ContractDetailID = short.Parse(SAL_ContractDetail.GetNewContractDetailID(CompanyID, ContractID, PeriodID, sqltrans).ToString());
                    sodAdd.FiscalID = CurrentFiscalID;
                    sodAdd.Insert(sqltrans);
                }

                foreach (SAL_ContractDetail sodUpdate in colUpdateSODetail)
                {
                    if (dtDetails != null && dtDetails.Rows.Count > 0)
                    {
                        foreach (DataRow item in dtDetails.Rows)
                        {
                            if (long.Parse(item["ContractDetailID"].ToString()) == sodUpdate.ContractDetailID)
                            {
                                decimal DoQty = decimal.Parse(item["DoQty"].ToString());
                                INV_Item objItem = new INV_Item(sodUpdate.ItemID);

                                if (sodUpdate.Quantity < DoQty)
                                    throw new BusinessLogicException(objItem.Description  + "SO Quantity cannot be less than Do Quantity!");
                                //else
                                //{
                                //    bool QtZero = SAL_DeliveryOrderDetail.UpdateItemQty(this.CompanyID, this.PeriodID, int.Parse(item["DOHeaderID"].ToString()), sodUpdate.ContractDetailID, sodUpdate.Quantity, sqltrans);
                                //}
                            }
                        }
                    }

                    sodUpdate.FiscalID = CurrentFiscalID;
                    sodUpdate.UpdateDataByKey(sqltrans);
                }

                foreach (SAL_ContractDetail sodDelete in colDeleteSODetail)
                {
                    if (dtDetails != null && dtDetails.Rows.Count > 0)
                    {
                        foreach (DataRow item in dtDetails.Rows)
                        {
                            bool QtZero = SAL_DeliveryOrderDetail.UpdateItemQty(this.CompanyID, this.PeriodID, int.Parse(item["DOHeaderID"].ToString()), sodDelete.ContractDetailID, 0);
                        }
                    }


                    SAL_ContractDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, sodDelete.ContractDetailID, ContractID, f_PeriodID);
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.FiscalID = CurrentFiscalID;
                    da.DocumentNo = this.ContractNo;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateSO");
            }

            return result;
        }



        public bool DeleteSOHeaderAndDetail(List<SAL_ContractDetail> colDeleteSODetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteSO");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, ContractID, f_PeriodID);

                foreach (SAL_ContractDetail sodDelete in colDeleteSODetail)
                    SAL_ContractDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, sodDelete.ContractDetailID, ContractID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteSO");
            }
            return result;
        }

        public bool UpdateSOHeaderForApprove(SET_DocumentApprove dA)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateSOHeader");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                    dA.UpdateDataByKeyStatus(sqltrans);

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);

                #endregion

                #region Insert In SET_DocumentApprove

                // delete after disscus
                if (dA.ApprovalStatus == "Discuss" || dA.ApprovalStatus == "InProcess")
                    SET_DocumentApprove.DeleteDataByDocNoanddocTypeid(f_CompanyID, f_PeriodID, int.Parse(dA.DocumentNo), dA.DocumentTypeID, sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateSOHeader");
            }

            return result;
        }





        public static DataTable Sales_Order_QuantityInfo(int companyID, int periodID, Nullable<int> batchID, int itemID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@BatchID", batchID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_QuantityInfoForSalesOrder, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_QuantityInfoForSalesOrder, sqlparams);

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





        public static DataRow GetCommissionPercent(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DispatchID", orderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCommissionPercent, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetCommissionPercent, sqlparams);

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

        public static bool CheckReciptAgainstCPR_Doc(int companyID, int periodID, int? CustomerID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckReciptAgainstCPR_Doc, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable RecivingCompletAgainstSO(int companyID, int periodID, int? CustomerID, long SaleOrderID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@SaleOrderID", SaleOrderID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_RecivingCompletAgainstSO, sqlparams);

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

        public static DataTable AllDocumentType()
        {
            DataTable dt = null;
            try
            {
                //SqlParameter[] sqlparams = new SqlParameter[4];


                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDocType);

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

        public static DataTable AllDocumentTypeSaleContract(int CompanyID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDocTypeSaleContract, sqlparams);

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


        public bool SOClose(int Companyid, int perioid, int DocmntTypeID, int SaleOrderID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@SaleOrderID", SaleOrderID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_SOClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_SOClose, sqlparams) > 0)
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

        public static DataTable GetDataForCheckApproveStatus(int companyID, int contractID, int periodID, int FiscalID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", FiscalID);

                DataSet ds = null;

                 ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeyCheckApproveStatus, sqlparams);

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


        public static DataTable GetDataForCheckApproveStatusHeader(int companyID, int contractID, int periodID, int FiscalID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", FiscalID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeyCheckApproveStatusHeader, sqlparams);

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

        public static DataTable GetSODataForUpdate(int companyID, int contractID, int periodID, int documenttypeID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documenttypeID);

                #region Query

                string QueryForSOData = "SELECT ContractNo, * FROM dbo.SAL_ContractHeader WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND ContractID = @ContractID AND SaleContractTypeID = @DocumentTypeID and Status = 1";

                #endregion

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForSOData, sqlparams);

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

        public static DataTable UpdateSOAdvanceData(int companyID, int contractID, int periodID, int documenttypeID, double NetAmount, int ReceiptID, int ReceiptTypeID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ContractID", contractID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documenttypeID);
                sqlparams[4] = new SqlParameter("@NetAmount", NetAmount);
                sqlparams[5] = new SqlParameter("@ReceiptTransactionID", ReceiptID);
                sqlparams[6] = new SqlParameter("@RecieptTypeID", ReceiptTypeID);

                #region Query

                string QueryForSOData = @"UPDATE dbo.SAL_ContractHeader 
SET AdvanceAmountPercentage = ISNULL((select ISNULL(SUM(Amount),0) from AR_ReceiptTransactionHeader WHERE CompanyID = @CompanyID AND DocumentTypeID = @RecieptTypeID and SourceHeaderID = @ContractID and ReceiptTransactionID <> @ReceiptTransactionID), 0) + @NetAmount 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND ContractID = @ContractID AND SaleContractTypeID = @DocumentTypeID and Status = 1";

                #endregion

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, QueryForSOData, sqlparams);

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


        #endregion

        #region "Report Methods"



        public static int GetNewOrderIDByDocumentFilter(int companyID, int periodID, bool DocumentIDsForSite = false, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;
            StringBuilder sb = new StringBuilder();
            string Query = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Querys
                string Query_GetNewOrderIDWith5000000 = @" SELECT case  when IsNull(max(ContractID), 0) > 5000000 then IsNull(max(ContractID), 0) + 1
else (IsNull(max(ContractID), 0) + 1)+5000000 end as MAXID
 FROM SAL_ContractHeader WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and ContractID > 5000000";

                string Query_GetNewOrderIDByDocumentFilter = @" SELECT IsNull(max(ContractID), 0) + 1 as ContractID FROM SAL_ContractHeader WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and ContractID < 5000000 ";
                #endregion
                if (DocumentIDsForSite)
                    Query = Query_GetNewOrderIDWith5000000;
                else
                    Query = Query_GetNewOrderIDByDocumentFilter;


                if (DocumentIDsForSite)
                    sb.Append("");


                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetAllDataForReceiptByCustomerID(int companyID, int periodID, Nullable<int> customerID, string strWhere)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAllForReceiptByCustomerID, strWhere, ""), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDODataByKey(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@OrderID", orderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDOData_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDOData_byKey, sqlparams);

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

        public static DataTable ConsolidateSalesContractDetail(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? CustomerID, int? DealerID, int CurrentFiscalID, int? DocumentTypeID)
        {
            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dateFrom != null && dateTo != null)
                    filter.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (CustomerID > 0)
                    filter.Append("AND [SAL_ContractHeader].CustomerID = " + CustomerID);

                if (DealerID > 0)
                    filter.Append("AND [SAL_ContractHeader].BrokerID = " + DealerID);


                if (DocumentTypeID > 0)
                    filter.Append(" And [SAL_ContractHeader].SaleContractTypeID =" + DocumentTypeID);

                //filter.Append("  Order BY ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_ConsolidateSalesContractDetailDealerWise, filter.ToString()), sqlparams);

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

        public static DataTable ConsolidateSalesContractDetailNew(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? CustomerID, int? DealerID, int CurrentFiscalID, int? DocumentTypeID)
        {
            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dateFrom != null && dateTo != null)
                    filter.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (CustomerID > 0)
                    filter.Append("AND [SAL_ContractHeader].CustomerID = " + CustomerID);

                if (DealerID > 0)
                    filter.Append("AND [SAL_ContractHeader].BrokerID = " + DealerID);


                if (DocumentTypeID > 0)
                    filter.Append(" And [SAL_ContractHeader].SaleContractTypeID =" + DocumentTypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_ConsolidateSalesContractDetailDealerWiseNew, filter.ToString()), sqlparams);

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


        public static DataTable GetSalesContractReport(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string DocumentType = "", string DocumentGroup = "", string CustomerID = "", string BatchId = "", Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                sb.Append(" and convert(date,SAL_ContractHeader.ContractDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");
                if (!string.IsNullOrEmpty(BatchId))
                    sb.Append(" AND [SAL_ContractHeader].[Season] ='" + BatchId + "'");
                #region MyRegion

                string Query_GetSalesContractReport = @"select 

SET_DocumentType.DocumentType,SAL_ContractHeader.ContractDate,SAL_ContractHeader.ContractNo,SAL_ContractHeader.CustomerPoDate,AR_SET_Customer.CustomerName,SAL_ContractHeader.FreightDetail as InvoiceRate
,SAL_ContractHeader.CustomerPoNo,SAL_ContractHeader.CustomerPoDate,SAL_ContractHeader.AdvTaxInvoiceNo,SAL_ContractHeader.AdvTaxInvoicedate
,SAL_ContractHeader.CPRNo,SAL_ContractHeader.CPRDate,SAL_ContractHeader.BGNo,SAL_ContractHeader.BGIssueDate,SAL_ContractHeader.BGMaturitydate
,INV_Item.Description as ItemDescription,SET_UnitofMeasure .UOMName
,SAL_ContractDetail .Quantity,
INV_Item.UnitWeight,
SAL_ContractDetail .Quantity * isnull(INV_Item.UnitWeight,0) as QtyKGS,  
SAL_ContractDetail .Rate,
(SAL_ContractDetail .Rate / isnull(INV_Item.UnitWeight,0)) as RateKGS,  
SAL_ContractDetail.NetAmount
,isnull(ExciseDutyActualAmount,0) as ExciseDutyActualAmount
,isnull(FEDActualAmount,0) as FEDActualAmount,
isnull(WithHoldingAmount,0) as WithHoldingAmount
,(([SAL_ContractDetail].[GrossAmount]/117)* [SAL_ContractDetail].[SalesTaxAmount])as TaxAmount,
[SAL_ContractDetail].[IsSalesTaxExempted], [SAL_ContractDetail].[SalesTaxAmount], 
    [SAL_ContractDetail].[SalesTaxFixRate], [SAL_ContractDetail].[SalesTaxPercent], 
[SAL_ContractDetail].[IsExciseDutyExempted], [SAL_ContractDetail].[ExciseDutyAmount], 
    [SAL_ContractDetail].[IsFEDExempted],[SAL_ContractDetail].[WithHolding], [SAL_ContractDetail].[FEDAmount],
[SAL_ContractDetail].[VATAmount]
,CASE WHEN [SAL_ContractDetail].Inclusive = 1  then 'Inclusive' else 'Exclusive' END TaxStatus, SAL_ContractHeader.InvoiceRateExclusive, AR_SET_SalePerson.SalePeronName
,SAL_ContractHeader.StatusCode
 , Case when SAL_ContractHeader.StatusCode = 5 then 'InProcess'
        when SAL_ContractHeader.StatusCode = 12 then 'ReleaseForApprove'
		when SAL_ContractHeader.StatusCode = 13 then 'PartiallyApproved'
		when SAL_ContractHeader.StatusCode = 1 then 'Approved'
		when SAL_ContractHeader.StatusCode = 14 then 'Discuss'
		when SAL_ContractHeader.StatusCode = 15 then 'Cancelled'
		when SAL_ContractHeader.StatusCode = 16 then 'Reversed'
		when SAL_ContractHeader.StatusCode = 25 then 'Closed' else '' end as DocumentStatus
from SAL_ContractHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID
and SET_DocumentType.PeriodID = SET_DocumentType.PeriodID
inner join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_ContractHeader.ContractID
and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID
and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID
and SAL_ContractDetail.Status = 1

inner join INV_Item on INV_Item.Itemid = SAL_ContractDetail.Itemid
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = SAL_ContractDetail.ItemUnitID

inner join AR_SET_Customer on AR_SET_Customer.Customerid = SAL_ContractHeader.CustomerID
and AR_SET_Customer.PeriodID = SAL_ContractHeader.PeriodID

left join AR_SET_SalePerson on AR_SET_SalePerson.SalePersonID = SAL_ContractHeader.SalePersonID
And AR_SET_SalePerson.CompanyID = SAL_ContractHeader.CompanyID
And AR_SET_SalePerson.PeriodID = SAL_ContractHeader.PeriodID
And AR_SET_SalePerson.Status  = 1

where SAL_ContractHeader.CompanyID = @CompanyID and SAL_ContractHeader.PeriodID = @PeriodID
{0}
";

                #endregion

                if (DocumentType != "")
                    sb.Append(" and SAL_ContractHeader.SaleContractTypeID = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (CustomerID != "")
                    sb.Append(" and SAL_ContractHeader.CustomerID = " + CustomerID + "");


                if (regionID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetSalesContractReport, sb.ToString()), sqlparams);
                
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

        public static DataTable GetSalesContractReportNew(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string DocumentType = "", string DocumentGroup = "", string CustomerID = "", string BatchId = "", Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                sb.Append(" and convert(date,SAL_ContractHeader.ContractDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");
                if (!string.IsNullOrEmpty(BatchId))
                    sb.Append(" AND [SAL_ContractHeader].[Season] ='" + BatchId + "'");
               
                if (DocumentType != "")
                    sb.Append(" and SAL_ContractHeader.SaleContractTypeID = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (CustomerID != "")
                    sb.Append(" and SAL_ContractHeader.CustomerID = " + CustomerID + "");


                if (regionID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_GetSalesContractReportNew, sb.ToString()), sqlparams);

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

        public static DataTable GetSalesContractSummaryReport(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string DocumentType = "", string DocumentGroup = "", string CustomerID = "", string BatchId = "", Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                sb.Append(" and convert(date,SAL_ContractHeader.ContractDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");

                #region MyRegion

                string Query_GetSalesContractSummaryReport = @"select SET_DocumentType.DocumentType,SAL_ContractHeader.ContractDate,SAL_ContractHeader.ContractNo,SAL_ContractHeader.CustomerPoDate,AR_SET_Customer.CustomerName,SAL_ContractHeader.FreightDetail as InvoiceRate
,SAL_ContractHeader.CustomerPoNo,SAL_ContractHeader.CustomerPoDate,SAL_ContractHeader.AdvTaxInvoiceNo,SAL_ContractHeader.AdvTaxInvoicedate
,SAL_ContractHeader.CPRNo,SAL_ContractHeader.CPRDate,SAL_ContractHeader.BGNo,SAL_ContractHeader.BGIssueDate,SAL_ContractHeader.BGMaturitydate
,INV_Item.Description as ItemDescription,SET_UnitofMeasure .UOMName
,SUM(SAL_ContractDetail .Quantity) as Quantity,
sum(SAL_ContractDetail .Quantity * isnull(INV_Item.UnitWeight,0)) as QtyKgs
,avg(SAL_ContractDetail .Rate) as Rate,
avg((SAL_ContractDetail .Rate) / isnull(INV_Item.UnitWeight,0)) as RateKgs,
SUM(SAL_ContractDetail .NetAmount) as NetAmount,
AR_SET_SalePerson.SalePeronName
,SAL_ContractHeader.StatusCode
 , Case when SAL_ContractHeader.StatusCode = 5 then 'InProcess'
        when SAL_ContractHeader.StatusCode = 12 then 'ReleaseForApprove'
		when SAL_ContractHeader.StatusCode = 13 then 'PartiallyApproved'
		when SAL_ContractHeader.StatusCode = 1 then 'Approved'
		when SAL_ContractHeader.StatusCode = 14 then 'Discuss'
		when SAL_ContractHeader.StatusCode = 15 then 'Cancelled'
		when SAL_ContractHeader.StatusCode = 16 then 'Reversed'
		when SAL_ContractHeader.StatusCode = 25 then 'Closed' else '' end as DocumentStatus
from SAL_ContractHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID
AND SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID
and SET_DocumentType.PeriodID = SET_DocumentType.PeriodID
inner join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_ContractHeader.ContractID
and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID
and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID
and SAL_ContractDetail.Status = 1

inner join INV_Item on INV_Item.Itemid = SAL_ContractDetail.Itemid
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = SAL_ContractDetail.ItemUnitID

inner join AR_SET_Customer on AR_SET_Customer.Customerid = SAL_ContractHeader.CustomerID
and AR_SET_Customer.PeriodID = SAL_ContractHeader.PeriodID

left join AR_SET_SalePerson on AR_SET_SalePerson.SalePersonID = SAL_ContractHeader.SalePersonID
And AR_SET_SalePerson.CompanyID = SAL_ContractHeader.CompanyID
And AR_SET_SalePerson.PeriodID = SAL_ContractHeader.PeriodID
And AR_SET_SalePerson.Status  = 1

where SAL_ContractHeader.CompanyID = @CompanyID and SAL_ContractHeader.PeriodID = @PeriodID
{0}

group by SET_DocumentType.DocumentType,SAL_ContractHeader.ContractDate,SAL_ContractHeader.ContractNo,SAL_ContractHeader.CustomerPoDate,AR_SET_Customer.CustomerName,SAL_ContractHeader.FreightDetail
,SAL_ContractHeader.CustomerPoNo,SAL_ContractHeader.CustomerPoDate,SAL_ContractHeader.AdvTaxInvoiceNo,SAL_ContractHeader.AdvTaxInvoicedate
,SAL_ContractHeader.CPRNo,SAL_ContractHeader.CPRDate,SAL_ContractHeader.BGNo,SAL_ContractHeader.BGIssueDate,SAL_ContractHeader.BGMaturitydate
,INV_Item.Description,SET_UnitofMeasure .UOMName,AR_SET_SalePerson.SalePeronName, SAL_ContractHeader.StatusCode
";

                #endregion

                if (DocumentType != "")
                    sb.Append(" and SAL_ContractHeader.SaleContractTypeID = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (CustomerID != "")
                    sb.Append(" and SAL_ContractHeader.CustomerID = " + CustomerID + "");

                if (!string.IsNullOrEmpty(BatchId))
                    sb.Append(" AND [SAL_ContractHeader].[Season] ='" + BatchId + "'");

                if (regionID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetSalesContractSummaryReport, sb.ToString()), sqlparams);

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

        public static DataTable GetSalesContractSummaryReportNew(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string DocumentType = "", string DocumentGroup = "", string CustomerID = "", string BatchId = "", Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                sb.Append(" and convert(date,SAL_ContractHeader.ContractDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");

                if (DocumentType != "")
                    sb.Append(" and SAL_ContractHeader.SaleContractTypeID = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (CustomerID != "")
                    sb.Append(" and SAL_ContractHeader.CustomerID = " + CustomerID + "");

                if (!string.IsNullOrEmpty(BatchId))
                    sb.Append(" AND [SAL_ContractHeader].[Season] ='" + BatchId + "'");

                if (regionID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    sb.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_GetSalesContractSummaryReportNew, sb.ToString()), sqlparams);

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

        #region Dashboard Methods

        public static DataTable GetAllDataForDashBoardGateInSugar(int companyID, int periodID, int CurrentFiscalID, string strWhere, string docType, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", CurrentFiscalID);

                if (FromDate != null && ToDate != null)
                {
                    sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                    sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                    strWhere += "	And [SAL_GateIn].GateInDate between @FromDate and @ToDate";
                }
                else if (FromDate != null)
                {
                    sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                    strWhere += "	And [SAL_GateIn].GateInDate between @FromDate and @FromDate";

                }

                else if (ToDate != null)
                {

                    strWhere += " And [SAL_GateIn].GateInDate between @ToDate and @ToDate";

                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAllForGateInSugar, strWhere, docType), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataForDispatchTrackingReport(int companyID, int periodID, int fiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int GateInNo = 0, string DocumentType = "")
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalID);

                if (FromDate != null && ToDate != null)
                {
                    sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                    sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                    sb.Append("	And [SAL_GateIn].GateInDate between @FromDate and @ToDate");
                }
                else if (FromDate != null)
                {
                    sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                    sb.Append("	And [SAL_GateIn].GateInDate between @FromDate and @FromDate");
                }

                else if (ToDate != null)
                {
                    sb.Append(" And [SAL_GateIn].GateInDate between @ToDate and @ToDate");
                }
                if (GateInNo > 0)
                {
                    sb.Append(" AND [SAL_GateIn].[GateInNo] = " + GateInNo);
                }

                if (DocumentType != "")
                {
                    sb.Append(" And [SAL_GateIn].[DocumentTypeID] = " + DocumentType);
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_GetDataForDispatchTrackingReport, sb.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllForS(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
    bool filterCategoryWise = false, bool filterDocumentWise = false, Nullable<Guid> currentUserID = null,
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
                    //Query_SelectAll_Data
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    StringBuilder query = new StringBuilder(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectAllData.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_ContractHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query.ToString()
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    StringBuilder query = new StringBuilder(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SelectForSearch.Replace("{AdditionalParameters}", strWhere));

                    if (filterDocumentWise)
                    {
                        query.Replace("{FilterByDocumentType}", " Inner Join UsersInDocumentType On UsersInDocumentType.CompanyID = SAL_ContractHeader.CompanyID And UsersInDocumentType.UserID = '" + currentUserID.Value + "' And UsersInDocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID ");
                    }
                    else
                        query.Replace("{FilterByDocumentType}", string.Empty);

                    if (filterCategoryWise)
                    {
                        query.Replace("{FilterByCategory}", " Inner Join UsersInCategories On UsersInCategories.CompanyID = SAL_ContractHeader.CompanyID And UsersInCategories.UserID = '" + currentUserID.Value + "' And UsersInCategories.CategoryID = SAL_ContractDetail.ItemCategoryID ");
                    }
                    else
                        query.Replace("{FilterByCategory}", string.Empty);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query.ToString() + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
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


        public static DataSet ProductionAndSaleSystem(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, int CurrentFiscalID, string SONumber)
        {
            DataSet dsToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@AsOn", ((DateTime)dateFrom).ToString("yyyy-MM-dd"));
                

                //if (dateFrom != null && dateTo != null)
                //    filter.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (SONumber != "")
                    filter.Append("AND [SAL_ContractHeader].ContractNo = " + SONumber);                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_ProductionAndSaleSystem, filter.ToString()), sqlparams);

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

        public static DataSet SaleOrderDetailReportCO2(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, int CurrentFiscalID, string SONumber)
        {
            DataSet dsToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@AsOn", ((DateTime)dateFrom).ToString("yyyy-MM-dd"));

                if (SONumber != "")
                    filter.Append("AND [SAL_ContractHeader].ContractNo = " + SONumber);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_ContractHeader.Query_SaleOrderDetailReportCO2, filter.ToString()), sqlparams);

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
        #endregion

    }
}