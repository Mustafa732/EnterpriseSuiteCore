using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;
using System.Linq;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_ReverseHeader
    {
        #region Constant Strings

        const string Query_GetData_byKeyCustomize = @" SELECT (select top 1 SourceHeaderID from SAL_ReverseDetail where ReverseHeaderID = [SAL_ReverseHeader].ReverseHeaderID
                                             and CompanyID = [SAL_ReverseHeader].CompanyID and Status = 1
                                             ) as SourceHeaderID,
                                             (select top 1 SourceCode from SAL_ReverseDetail where ReverseHeaderID = [SAL_ReverseHeader].ReverseHeaderID
                                              and CompanyID = [SAL_ReverseHeader].CompanyID and Status = 1
                                             ) as SourceCode, * FROM [SAL_ReverseHeader] WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReverseHeaderID] = @ReverseHeaderID ";

        const string Query_SelectAll_With_Paging2 = @" Select * from ( select
ROW_NUMBER() Over (Order by srh2.CompanyID, srh2.PeriodID, srh2.ReverseHeaderID) as 'Row'
,  
srh2.ReverseHeaderID,
srh2.DocumentNo,
Set_DocumentType.DocumentType,
srh2.StatusCode,
srh2.CreateDate,
srh2.CreateDate as DocumentDate
,Set_PeriodCustom.PeriodSetName
,srh2.FiscalID, srh2.Status ,srh2.CompanyID ,srh2.PeriodID 
,SAL_InvoiceHeader.InvoiceNumber
,AR_SET_Customer.CustomerID
From 
SAL_ReverseHeader srh2

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh2.DocumentTypeID and Set_DocumentType.CompanyID = srh2.CompanyID
and Set_DocumentType.PeriodID= srh2.PeriodID


inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
and ApplicationMenu.MenuPath like '%Sales/SalesReturn/SaleRetrun.aspx%'
	Left join Set_PeriodCustom on  
 srh2.CreateDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1

inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID=srh2.ReverseHeaderID
inner join SAL_InvoiceHeader on SAL_ReverseDetail.SourceDocumentTypeID=SAL_InvoiceHeader.InvoiceType
and SAL_ReverseDetail.SourceCode=SAL_InvoiceHeader.InvoiceNumber
and SAL_ReverseDetail.SourceHeaderID=SAL_InvoiceHeader.InvoiceID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID=SAL_InvoiceHeader.CustomerID
left join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID=srh2.DocumentTypeID and GL_TransactionHeader.DocumentNo=srh2.DocumentNo

)srh2
";

        const string Query_SelectAllDOReturn_With_Paging2 = @"  

Select ROW_NUMBER() Over (Order by srh2.createdate desc) as 'Row'
,* from

 (
select
distinct
srh.ReverseHeaderID,
srh.DocumentNo,
Set_DocumentType.DocumentType,
srh.StatusCode,
srh.CreateDate,
srh.CreateDate as DocumentDate
,Set_PeriodCustom.PeriodSetName
,(Select top 1 SAL_ReverseDetail.SourceCode from SAL_ReverseDetail where SAL_ReverseDetail.ReverseHeaderID=srh.ReverseHeaderID) as SIDocumentNo
,AR_SET_Customer.CustomerID,AR_SET_Customer.CustomerName as Customer,GL_TransactionHeader.DocumentDate as GLDocumentDate
,srh.FiscalID, srh.Status ,srh.CompanyID ,srh.PeriodID 
,SAL_InvoiceHeader.InvoiceNumber
From 
SAL_ReverseHeader srh

inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID=srh.ReverseHeaderID

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh.DocumentTypeID and Set_DocumentType.CompanyID = srh.CompanyID
and Set_DocumentType.PeriodID= srh.PeriodID
inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
Left join Set_PeriodCustom on  
 srh.CreateDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1

inner join SAL_InvoiceHeader on 
SAL_ReverseDetail.SourceDocumentTypeID=SAL_InvoiceHeader.InvoiceType
and SAL_ReverseDetail.SourceCode=SAL_InvoiceHeader.InvoiceNumber
and SAL_ReverseDetail.SourceHeaderID=SAL_InvoiceHeader.InvoiceID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID=SAL_InvoiceHeader.CustomerID
left join GL_TransactionHeader on GL_TransactionHeader.DocumentTypeID=srh.DocumentTypeID and GL_TransactionHeader.DocumentNo=srh.DocumentNo
and GL_TransactionHeader.fiscalid = srh.fiscalid 
--and ApplicationMenu.MenuPath like '%Sales/SalesReturn/DeliveryOrderReturn.aspx%'
)srh2
";

        const string Query_SelectAll_ForApproveDetail = @" 
SELECT DISTINCT SAL_ReverseHeader.ReverseHeaderID
	,SAL_ReverseHeader.CompanyID
	,SAL_ReverseHeader.PeriodID
	,SAL_ReverseHeader.DocumentTypeID
	,SAL_ReverseHeader.DocumentNo
	,SAL_ReverseHeader.CreateDate
	,SAL_ReverseHeader.Status
	,SAL_ReverseHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel
FROM SAL_ReverseHeader
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_ReverseHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_ReverseHeader.PeriodID AND SET_DocumentApprove.Status =1 
	
AND SET_DocumentApprove.DocumentTypeID = SAL_ReverseHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_ReverseHeader.DocumentNo 
	
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_ReverseHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_ReverseHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SAL_ReverseHeader.DocumentTypeID = SAL_ReverseHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
	
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE SET_UserApprovalAmount.ApprovalOperator = 'AND' 
AND SAL_ReverseHeader.Status = 1 AND SAL_ReverseHeader.PeriodID = @PeriodID 
     AND SAL_ReverseHeader.CompanyID = @CompanyID 
     AND SAL_ReverseHeader.ReverseHeaderID = @ReverseHeaderID 
     AND SAL_ReverseHeader.DocumentTypeID = @DocumentTypeID 
     AND SAL_ReverseHeader.DocumentNo = @DocumentNo";

        const string query_Select_DataByKey_ForApprove = @"
SELECT DISTINCT SAL_ReverseHeader.ReverseHeaderID
	,SAL_ReverseHeader.CompanyID
	,SAL_ReverseHeader.PeriodID
	,SAL_ReverseHeader.Status
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel
FROM SAL_ReverseHeader
INNER JOIN Set_DocumentApprove On Set_DocumentApprove.CompanyID = SAL_ReverseHeader.CompanyID 
		And Set_DocumentApprove.PeriodID = SAL_ReverseHeader.PeriodID 
		And Set_DocumentApprove.DocumentTypeID = SAL_ReverseHeader.DocumentTypeID 
		And Set_DocumentApprove.DocumentNo = SAL_ReverseHeader.DocumentNo 
		And Set_DocumentApprove.CreateDate = 
		(
			Select Max(CreateDate) From Set_DocumentApprove 
			Where CompanyID = SAL_ReverseHeader.CompanyID And PeriodID = SAL_ReverseHeader.PeriodID 
				And DocumentTypeID = SAL_ReverseHeader.DocumentTypeID 
				And DocumentNo = SAL_ReverseHeader.DocumentNo)

INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_ReverseHeader.CompanyID 
	    AND SET_UserApprovalAmount.PeriodID = SAL_ReverseHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	    AND (SET_UserApprovalAmount.DocumentTypeID = SAL_ReverseHeader.DocumentTypeID OR  SET_UserApprovalAmount.DocumentTypeID IS NULL)
AND (SET_UserApprovalAmount.UserID = Set_DocumentApprove.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE SET_UserApprovalAmount.ApprovalOperator = 'AND' AND SAL_ReverseHeader.Status = 1 AND SAL_ReverseHeader.PeriodID = @PeriodID 
    AND SAL_ReverseHeader.CompanyID = @CompanyID 
    AND SAL_ReverseHeader.ReverseHeaderID = @ReverseHeaderID
    AND SAL_ReverseHeader.DocumentTypeID = @DocumentTypeID 
    AND SAL_ReverseHeader.DocumentNo = @DocumentNo";

        const string Query_SelectAll_With_Paging = @"  select
ROW_NUMBER() Over (Order by srh.CompanyID, srh.PeriodID, srh.ReverseHeaderID) as 'Row'
,  
srh.ReverseHeaderID,
srh.DocumentNo,
Set_DocumentType.DocumentType,
StatusCode,
srh.CreateDate,
srh.CreateDate as DocumentDate
,Set_PeriodCustom.PeriodSetName
From 
SAL_ReverseHeader srh

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh.DocumentTypeID and Set_DocumentType.CompanyID = srh.CompanyID
and Set_DocumentType.PeriodID= srh.PeriodID


inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
and ApplicationMenu.MenuPath like '%Sales/SalesReturn/SaleRetrun.aspx%'
	Left join Set_PeriodCustom on  
 srh.CreateDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1
";

        const string Query_SelectAllDOReturn_With_Paging = @"  select
ROW_NUMBER() Over (Order by srh.createdate desc) as 'Row'
,  
srh.ReverseHeaderID,
srh.DocumentNo,
Set_DocumentType.DocumentType,
srh.StatusCode,
srh.CreateDate,
srh.CreateDate as DocumentDate
,Set_PeriodCustom.PeriodSetName
From 
SAL_ReverseHeader srh

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh.DocumentTypeID and Set_DocumentType.CompanyID = srh.CompanyID
and Set_DocumentType.PeriodID= srh.PeriodID
inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
Left join Set_PeriodCustom on  
 srh.CreateDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1
--and ApplicationMenu.MenuPath like '%Sales/SalesReturn/DeliveryOrderReturn.aspx%'
";

        const string Query_SelectAll_Without_Paging = @"
select
srh.ReverseHeaderID,
srh.DocumentNo,
Set_DocumentType.DocumentType,
srh.StatusCode,
srh.CreateDate
From 
SAL_ReverseHeader srh

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh.DocumentTypeID and Set_DocumentType.CompanyID = srh.CompanyID
and Set_DocumentType.PeriodID= srh.PeriodID

inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
and ApplicationMenu.MenuPath like '%Sales/SalesReturn/SaleRetrun.aspx%'
";

        const string Query_SelectAllDO_Without_Paging = @"
select
srh.ReverseHeaderID,
srh.DocumentNo,
Set_DocumentType.DocumentType,
srh.StatusCode,
srh.CreateDate,
srh.CreateDate as DocumentDate
,Set_PeriodCustom.PeriodSetName
From 
SAL_ReverseHeader srh

inner Join Set_DocumentType on Set_DocumentType.DocumentTypeID = srh.DocumentTypeID and Set_DocumentType.CompanyID = srh.CompanyID
and Set_DocumentType.PeriodID= srh.PeriodID

inner join applicationmenu  on  applicationmenu.MenuID =   Set_DocumentType.MenuID
and ApplicationMenu.MenuPath like '%Sales/SalesReturn/DeliveryOrderReturn.aspx%'

Left join Set_PeriodCustom on  
 srh.CreateDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1
";



        const string Query_GetDataByOrderIDForDOReturn = @" Select distinct  -1 as ReverseHeaderID , -1 as ReverseDetailID, INV_ITem.Description as ItemName, 
(Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity)Rate,
--SAL_DeliveryOrderDetail.Quantity as DOQuantity, 
--- Working Only For Vehicle Senario -----
1 as DOQuantity, 
ABS(std.TransactionQuantity) as DispatchQuantity,
--(ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - ISNULL(ABS(std.TransactionQuantity),0) - ISNULL(SAL_DeliveryOrderDetail.ReversedQty,0))QtyCanBeReverse
1 as QtyCanBeReverse,
 0 as ReversedQuantity, 0 as ReversedAmount,
SAL_DeliveryOrderDetail.DOHeaderID as SourceHeaderID, SAL_DeliveryOrderDetail.DODetailID as SourceDetailID, 
        SAL_DeliveryOrderHeader.DeliveryOrderNo as SourceCode, SAL_DeliveryOrderHeader.DocumentTypeID as SourceDocumentTypeID,SAL_DOVehicleInformation.VehicleID
        ,SAL_DOVehicleInformation.DOVehicleInformationID as VehicleID_2
        ,SAL_DOVehicleInformation.Chassis
 From SAL_DeliveryOrderHeader

INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
	AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID

INNER JOIN [Sal_OrderDetail] ON [Sal_OrderDetail].[OrderID] = SAL_DeliveryOrderDetail.[SalesOrderHeaderID] AND
			[Sal_OrderDetail].[OrderDetailID] = SAL_DeliveryOrderDetail.[SalesOrderDetailID] AND
            [Sal_OrderDetail].[CompanyID] = SAL_DeliveryOrderDetail.[CompanyID] AND
            [Sal_OrderDetail].[PeriodID] = SAL_DeliveryOrderDetail.[PeriodID]
            
LEFT JOIN INV_StockTransactionDetail std ON std.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID AND
	std.SourceLineID = SAL_DeliveryOrderDetail.DODetailID AND std.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo AND
	std.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID AND
	std.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND std.PeriodID = SAL_DeliveryOrderDetail.PeriodID
	
INNER JOIN INV_Item on 	INV_Item.ItemID = [Sal_OrderDetail].ItemID
            --AND INV_Item.CompanyID = [Sal_OrderDetail].CompanyID

left join SAL_DOVehicleInformation on SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
and SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID
and SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID

left join SAL_DispatchVehicleInformation on SAL_DispatchVehicleInformation.StockHeaderID = std.StockTransactionID
and SAL_DispatchVehicleInformation.StockDetailID = std.TransactionDetailID
and SAL_DispatchVehicleInformation.CompanyID = std.CompanyID
and SAL_DispatchVehicleInformation.PeriodID = std.PeriodID
and SAL_DispatchVehicleInformation.VehicleInformationID = SAL_DOVehicleInformation.DOVehicleInformationID

Where SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
        AND SAL_DeliveryOrderHeader.DeliveryOrderID = @OrderID
        AND SAL_DOVehicleInformation.VehicleID is not null
        order by SAL_DeliveryOrderdetail.DODetailID ";

        const string Query_GetDataByOrderIDForDispatchReturn = @"
select * from (
Select 1 as ReverseHeaderID , -1 as ReverseDetailID, INV_ITem.Description as ItemName,INV_ITem.ItemID,
std.TransactionCost as Rate, ISNULL(ABS(std.TransactionQuantity),0)DispatchQty, 
ISNULL(abs(SAL_InvoiceDetail.QuantityInvoiced),0)InvoicedQty,
--( ISNULL(ABS(std.PrimaryQuantity),0) - ISNULL(SAL_InvoiceDetail.QuantityInvoiced,0) - ISNULL(std.ReversedQuantity,0))QtyCanBeReverse,
[SAL_DeliveryOrderDetail].Quantity as BaseQty,
( (ABS(isnull(std.PrimaryQuantity,0)) - ISNULL(abs(SAL_InvoiceDetail.QuantityInvoiced),0)) - 
 
 (select isnull(sum(ReversedQuantity),0) from SAL_ReverseDetail 
 where SAL_ReverseDetail.SourceCode = [SAL_DispatchHeader].DispatchNo
	 and SAL_ReverseDetail.SourceDocumentTypeID = [SAL_DispatchHeader].DocumentTypeID
     and SAL_ReverseDetail.sourceHeaderID = std.stockTransactionID
	 and SAL_ReverseDetail.sourceDetailID = std.TransactionDetailID
	 and SAL_ReverseDetail.CompanyID = [SAL_DispatchHeader].CompanyID
	 and SAL_ReverseDetail.PeriodID = [SAL_DispatchHeader].PeriodID)) as QtyCanBeReverse,
 0 as ReversedQuantity, 0 as ReversedAmount,
 std.StockTransactionID as SourceHeaderID, std.TransactionDetailID as SourceDetailID, 
        sth.DocumentNo as SourceCode, sth.DocumentTypeID as SourceDocumentTypeID
,std.SourceHeaderID as DeliveryOrderID,[SAL_DeliveryOrderDetail].[DODetailID],SAL_DispatchHeader.CustomerID , std.TransactionCost * ISNULL(ABS(std.TransactionQuantity),0) as Amount
,SAL_DispatchHeader.DispatchDate,[SAL_DeliveryOrderHeader].[DeliveryOrderNo],[SAL_DispatchHeader].DispatchID
,isnull(std.AverageRate,(Select top 1 round(GL_TransactionDetail.Amount/ISNULL(ABS(std.TransactionQuantity),1),2) 

,isnull(std.AverageRate,(Select top 1 round(GL_TransactionDetail.Amount/ISNULL(ABS(std.TransactionQuantity),1),2) 
from GL_TransactionDetail where GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.Amount<>SAL_DispatchHeader.TotalAmount and GL_TransactionDetail.TransactionType = 'Credit' ))AvgAmount

,SAL_DispatchHeader.CostCentreID



From SAL_DispatchHeader

INNER JOIN INV_StockTransactionHeader sth ON sth.DocumentNo = SAL_DispatchHeader.DispatchNo AND 
	sth.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID AND sth.CompanyID = SAL_DispatchHeader.CompanyID
	AND sth.PeriodID = SAL_DispatchHeader.PeriodID

INNER JOIN INV_StocktransactionDetail std ON std.StockTransactionID = sth.StockTransactionID
AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID AND std.status = 1

INNER JOIN INV_Item On INV_ITem.ItemID = std.ItemID --AND INV_Item.CompanyID = std.CompanyID

LEFT JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.StockDetailID = std.TransactionDetailID AND 
	  SAL_InvoiceDetail.StockHeaderID = std.StockTransactionID AND SAL_InvoiceDetail.CompanyID = std.CompanyID AND
	  SAL_InvoiceDetail.PeriodID = std.PeriodID
	  
INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = std.[SourceLineID] 
        AND [SAL_DeliveryOrderDetail].[DOHeaderID] = std.[SourceHeaderID] 
        AND [SAL_DeliveryOrderDetail].[CompanyID] = std.[CompanyID] 
        AND [SAL_DeliveryOrderDetail].[PeriodID] = std.[PeriodID] 
        AND [SAL_DeliveryOrderDetail].[Status] = 1 
    INNER JOIN [SAL_DeliveryOrderHeader] ON [SAL_DeliveryOrderHeader].[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] 
        AND [SAL_DeliveryOrderHeader].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_DeliveryOrderHeader].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_DeliveryOrderHeader].[Status] = 1 

	  WHERE SAL_DispatchHeader.CompanyID = @CompanyID AND SAL_DispatchHeader.PeriodID = @PeriodID AND std.StockTransactionID = @DispatchID
) as tab
--where tab.QtyCanBeReverse > 0
";


        const string Query_GetDataByOrderIDForDispatchReturn_Cane = @"

select * from (
select 
(CASE WHEN tab.InvoiceHeaderID IS not NULL THEN CONVERT(DECIMAL(18,2),(tab.InvoiceRate)) ELSE CONVERT(DECIMAL(18,2),(tab.PriorCost)) end) AS SalesRate,
tab.InvoiceHeaderID,
tab.ReverseHeaderID,tab.ReverseDetailID,tab.ItemName,tab.ItemID,
--tab.Rate,
 (SELECT top 1 * FROM ufn_GetLastStockCost(tab.ItemID,tab.LocationID,GETDATE(),@companyID,@periodID)) as Rate,
CONVERT(DECIMAL(18,4),tab.DispatchQty) AS DispatchQty,CONVERT(DECIMAL(18,4),sum(tab.InvoicedQty)) AS InvoicedQty
--,ABS(((DispatchQty-sum(tab.InvoicedQty))-tab.QtyReversed) - sum(tab.InvoiceReversedQuantity)) QtyCanBeReverse
--,ABS(sum(tab.InvoiceReversedQuantity)) as QtyCanBeReverse
,CONVERT(DECIMAL(18,4),case when tab.InvoiceHeaderID is NULL then (ABS(((DispatchQty-sum(tab.InvoicedQty))-tab.QtyReversed) - sum(tab.InvoiceReversedQuantity))) else  ((ABS(sum(tab.InvoiceReversedQuantity))) - (ABS(sum(tab.QtyReversed)))) end )as QtyCanBeReverse
--,tab.ReversedQuantity
,(CASE WHEN tab.InvoiceHeaderID IS not NULL THEN CONVERT(DECIMAL(18,4),((ABS(sum(tab.InvoiceReversedQuantity))) - (ABS(sum(tab.QtyReversed))))) ELSE 0 end) AS ReversedQuantity
--,tab.ReversedAmount
,(CASE WHEN tab.InvoiceHeaderID IS not NULL THEN CONVERT(DECIMAL(18,2),(((ABS(sum(tab.InvoiceReversedQuantity))) - (ABS(sum(tab.QtyReversed)))) *  (SELECT top 1 * FROM ufn_GetLastStockCost(tab.ItemID,tab.LocationID,GETDATE(),@companyID,@periodID)))) ELSE 0 end) AS ReversedAmount
,tab.SourceHeaderID,
tab.SourceDetailID,tab.SourceCode,tab.SourceDocumentTypeID,tab.DeliveryOrderID	
,tab.DODetailID,tab.CustomerID,tab.Amount,tab.DispatchDate,tab.DeliveryOrderNo,tab.DispatchID,tab.AvgAmount
,tab.CostCentreID
,tab.InvoiceReversedQuantity,tab.LocationID,tab.LocationName,tab.RackID, tab.RackName
 from (
Select 1 as ReverseHeaderID , -1 as ReverseDetailID, INV_ITem.Description as ItemName,INV_ITem.ItemID,
std.TransactionCost as Rate, 
ISNULL(ABS(std.TransactionQuantity),0)DispatchQty, 
ISNULL((SAL_InvoiceDetail.QuantityInvoiced),0)InvoicedQty,
ISNULL((SAL_InvoiceDetail.UnitPrice),0)InvoiceRate,
(case when ISNULL(ABS(std.TransactionQuantity),0) > 0 then ISNUll(std.PriorCost,0) / ISNULL(ABS(std.TransactionQuantity),0) else 0 end) as PriorCost,
ISNULL((SAL_InvoiceDetail.ReversedQuantity),0) InvoiceReversedQuantity,
--0 as QtyCanBeReverse,
0 as ReversedQuantity,
  (select isnull(sum(ReversedQuantity),0) from SAL_ReverseDetail 
 where   SAL_ReverseDetail.SourceCode = [SAL_DispatchHeader].DispatchNo
	 and SAL_ReverseDetail.SourceDocumentTypeID = [SAL_DispatchHeader].DocumentTypeID
     and SAL_ReverseDetail.sourceHeaderID = std.stockTransactionID
	 and SAL_ReverseDetail.sourceDetailID = std.TransactionDetailID
	 and SAL_ReverseDetail.CompanyID = [SAL_DispatchHeader].CompanyID
	 and SAL_ReverseDetail.PeriodID = [SAL_DispatchHeader].PeriodID) as QtyReversed, 0 as ReversedAmount
 ,std.StockTransactionID as SourceHeaderID, std.TransactionDetailID as SourceDetailID, sth.DocumentNo as SourceCode, sth.DocumentTypeID as SourceDocumentTypeID
,std.SourceHeaderID as DeliveryOrderID,[SAL_DeliveryOrderDetail].[DODetailID],SAL_DispatchHeader.CustomerID , std.TransactionCost * ISNULL(ABS(std.TransactionQuantity),0) as Amount
,SAL_DispatchHeader.DispatchDate,[SAL_DeliveryOrderHeader].[DeliveryOrderNo],SAL_DispatchHeader.DispatchID

--,GL_TransactionDetail.Amount AvgAmount
--round(GL_TransactionDetail.Amount/ISNULL(ABS(std.TransactionQuantity),1),2) AvgAmount

,isnull(std.AverageRate,(Select top 1 round(GL_TransactionDetail.Amount/ISNULL(ABS(std.TransactionQuantity),1),2) 
from GL_TransactionDetail where GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID
and GL_TransactionDetail.Amount<>SAL_DispatchHeader.TotalAmount 
and GL_TransactionDetail.TransactionType = 'Credit' ))AvgAmount

,SAL_DispatchHeader.CostCentreID
,std.LocationID
,SET_Location.LocationName
,std.RackID
,SET_Rack.RackName
,SAL_InvoiceDetail.InvoiceHeaderID
From SAL_DispatchHeader

INNER JOIN INV_StockTransactionHeader sth ON sth.DocumentNo = SAL_DispatchHeader.DispatchNo AND 
	sth.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID AND sth.CompanyID = SAL_DispatchHeader.CompanyID
	AND sth.PeriodID = SAL_DispatchHeader.PeriodID
	AND sth.FiscalID = SAL_DispatchHeader.FiscalID
	AND sth.Status = 1
INNER JOIN INV_StocktransactionDetail std ON std.StockTransactionID = sth.StockTransactionID
AND std.CompanyID = sth.CompanyID AND std.PeriodID = sth.PeriodID AND std.status = 1 AND std.StockTransactionID = @DispatchID
AND std.status = 1

inner join SET_Location on SET_Location.LocationID = std.LocationID
left join SET_Rack on SET_Rack.RackID=std.RackID
 and SET_Rack.Status = 1
    --inner join Set_PeriodCustom on  
 --SAL_DispatchHeader.DispatchDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
--and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID


--Check Average Amount
left JOIN GL_TransactionHeader ON GL_TransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo 
and GL_TransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and GL_TransactionHeader.fiscalid = SAL_DispatchHeader.fiscalid
and GL_TransactionHeader.status = 1

--left JOIN GL_TransactionDetail ON GL_TransactionDetail.TransactionID = GL_TransactionHeader.TransactionID and GL_TransactionDetail.Amount<>SAL_DispatchHeader.TotalAmount 
--and GL_TransactionDetail.TransactionType = 'Credit'

INNER JOIN INV_Item On INV_ITem.ItemID = std.ItemID --AND INV_Item.CompanyID = std.CompanyID



left JOIN  SAL_InvoiceDetail ON SAL_InvoiceDetail.StockDetailID = std.TransactionDetailID AND 
	  SAL_InvoiceDetail.StockHeaderID = std.StockTransactionID AND SAL_InvoiceDetail.CompanyID = std.CompanyID AND
	  SAL_InvoiceDetail.PeriodID = std.PeriodID --and SAL_InvoiceDetail.QuantityInvoiced<0
	  and SAL_InvoiceDetail.itemid = std.itemid
	  and SAL_InvoiceDetail.status = 1
	  
	  
	  
INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = std.[SourceLineID] 
        AND [SAL_DeliveryOrderDetail].[DOHeaderID] = std.[SourceHeaderID] 
        AND [SAL_DeliveryOrderDetail].[CompanyID] = std.[CompanyID] 
        AND [SAL_DeliveryOrderDetail].[PeriodID] = std.[PeriodID] 
        AND [SAL_DeliveryOrderDetail].[Status] = 1 
    INNER JOIN [SAL_DeliveryOrderHeader] ON [SAL_DeliveryOrderHeader].[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] 
        AND [SAL_DeliveryOrderHeader].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_DeliveryOrderHeader].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_DeliveryOrderHeader].[Status] = 1 

   -- inner join Set_PeriodCustom s on  
 --[SAL_DeliveryOrderHeader].DODate between s.StartDate and s.EndDate 
--and s.Status = 1 and s.FiscalID = @CurrentFiscalID

 --inner join Set_PeriodCustom s on  
 --SAL_DispatchHeader.DispatchDate between s.StartDate and s.EndDate 
--and s.Status = 1 and s.FiscalID = @CurrentFiscalID


	  WHERE SAL_DispatchHeader.CompanyID = @CompanyID AND SAL_DispatchHeader.PeriodID = @PeriodID AND std.StockTransactionID = @DispatchID
) as tab

--where tab.QtyCanBeReverse > 0
where tab.InvoiceReversedQuantity >= 0
group by tab.ReverseHeaderID,tab.ReverseDetailID,tab.ItemName,tab.ItemID,
tab.DispatchQty,tab.ReversedQuantity,QtyReversed,tab.ReversedAmount,tab.SourceHeaderID,
tab.SourceDetailID,tab.SourceCode,tab.SourceDocumentTypeID,tab.DeliveryOrderID	
,tab.DODetailID,tab.CustomerID,tab.Amount,tab.DispatchDate,tab.DeliveryOrderNo,tab.DispatchID,tab.AvgAmount
,tab.CostCentreID
,tab.InvoiceReversedQuantity
,tab.LocationID,tab.LocationName
,tab.RackID,tab.RackName
,tab.InvoiceHeaderID
,tab.InvoiceRate
,tab.PriorCost
)t
where t.QtyCanBeReverse>0";

        const string Query_GetDataByOrderIDForTareWeight = @"
select sum(isnull(ReversedQuantity,0)) as ReversedQuantity from SAL_ReverseHeader 
inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID = SAL_ReverseHeader.ReverseHeaderID
and SAL_ReverseDetail.CompanyID = SAL_ReverseHeader.CompanyID
and SAL_ReverseDetail.PeriodID = SAL_ReverseHeader.PeriodID
and SAL_ReverseDetail.Status = 1

where SAL_ReverseHeader.ReverseHeaderID = @DispatchID
and SAL_ReverseHeader.CompanyID = @CompanyID
and SAL_ReverseHeader.PeriodID = @PeriodID
--and SAL_ReverseHeader.FiscalID  = @CurrentFiscalID
and SAL_ReverseHeader.StatusCode = 1
and SAL_ReverseHeader.Status = 1
";

        const string Query_GetDataByKeyTableForInvoiceReturn = @"
Select -1 as ReverseHeaderID , -1 as ReverseDetailID, 
INV_ITem.Description as ItemName, 
(SAL_InvoiceDetail.NetAmount / SAL_InvoiceDetail.QuantityInvoiced) as Rate, 
ISNULL(SAL_InvoiceDetail.QuantityInvoiced,0)InvoicedQty,
ISNULL(AR_ReceiptTransactionDetail.Quantity,0)ReceiptQty,

( ISNULL(ABS(SAL_InvoiceDetail.QuantityInvoiced),0) - ISNULL(AR_ReceiptTransactionDetail.Quantity,0) - ISNULL(SAL_InvoiceDetail.ReversedQuantity,0))QtyCanBeReverse,
 0 as ReversedQuantity, 0 as ReversedAmount,
 SAL_InvoiceDetail.InvoiceHeaderID as SourceHeaderID, 
 SAL_InvoiceDetail.InvoiceDetailID as SourceDetailID, 
 SAL_InvoiceHeader.InvoiceNumber as SourceCode, 
 SAL_InvoiceHeader.InvoiceType as SourceDocumentTypeID

From SAL_InvoiceHeader

INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID 
	AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID AND
	  SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID

LEFT JOIN AR_ReceiptTransactionDetail ON AR_ReceiptTransactionDetail.InvoiceHeaderID = SAL_InvoiceDetail.InvoiceHeaderID AND
	AR_ReceiptTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID AND AR_ReceiptTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID
	
INNER JOIN INV_Item On INV_ITem.ItemID = SAL_InvoiceDetail.ItemID --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID	
	
	WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID
	AND SAL_InvoiceHeader.InvoiceID = @InvoiceID
";
        #endregion

        #region Methods

        public DataTable GetDataByKeyTableDOReturn(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetdataForDOReturn = @" 
select MAX(ReverseHeaderID) as ReverseHeaderID, max(ReverseDetailID) as ReverseDetailID, max(ItemName) as ItemName, max(Rate) as Rate, MAX(DOQuantity) as DOQuantity, 
    SUM(DispatchQuantity) as DispatchQuantity, 

--(MAX(DOQuantity) - SUM (DispatchQuantity)) - MAX (Rev_Qty) AS QtyCanBeReverse, 

MAX(DOQuantity) AS QtyCanBeReverse, 

    max(ReversedQuantity) as ReversedQuantity, MAX(ReversedAmount) as ReversedAmount, SourceHeaderID, SourceDetailID, SourceCode, SourceDocumentTypeID, 
    -1 as VehicleID, -1 as VehicleID_2 
from 
(
    Select distinct -1 as ReverseHeaderID, -1 as ReverseDetailID, INV_ITem.Description as ItemName, (Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity) Rate, 
        SAL_DeliveryOrderDetail.Quantity as DOQuantity, isnull(ABS(std.TransactionQuantity), 0) as DispatchQuantity, 
        (ISNULL(SAL_DeliveryOrderDetail.Quantity, 0) - ISNULL(ABS(std.TransactionQuantity), 0) - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0)) QtyCanBeReverse, 
        ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) as Rev_Qty, 0 as ReversedQuantity, 0 as ReversedAmount, SAL_DeliveryOrderDetail.DOHeaderID as SourceHeaderID, 
        SAL_DeliveryOrderDetail.DODetailID as SourceDetailID, SAL_DeliveryOrderHeader.DeliveryOrderNo as SourceCode, 
        SAL_DeliveryOrderHeader.DocumentTypeID as SourceDocumentTypeID 
    From SAL_DeliveryOrderHeader 
        INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
            AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        INNER JOIN Sal_OrderDetail ON Sal_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
            AND Sal_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND Sal_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
            AND Sal_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        LEFT JOIN INV_StockTransactionDetail std ON std.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID AND std.SourceLineID = SAL_DeliveryOrderDetail.DODetailID 
            AND std.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo AND std.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
            AND std.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND std.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        INNER JOIN INV_Item on INV_Item.ItemID = Sal_OrderDetail.ItemID --AND INV_Item.CompanyID = Sal_OrderDetail.CompanyID 
    Where SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.DeliveryOrderID = @OrderID
) as tab 
group by SourceHeaderID, SourceDetailID, SourceCode, SourceDocumentTypeID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetdataForDOReturn, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetdataForDOReturn, sqlparams);

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

        public DataTable ReversedDataForSalesLookUp(int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetReversedDataForSalesLookUp = @" select distinct soh.OrderID, soh.ContractNo, soh.ContractDate, SET_DocumentType.DocumentType 
from SAl_OrderHeader soh 
    inner join 
    (
        select * from 
        (
            select SAL_OrderDetail.Quantity as SalesOrderQuantity, isnull(
                (
                    select SUM(Quantity) - SUM(isnull(ReversedQty, 0)) from SAL_deliveryOrderDetail 
		            where SAL_deliveryOrderDetail.SalesOrderHeaderID = SAL_OrderHeader.OrderID 
                        and SAL_deliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
                        and SAL_deliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID and SAL_deliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
                        and SAL_deliveryOrderDetail.Status = 1
                ),0 ) as DeliveryOrderQuantity, 
                isnull(SAL_OrderDetail.ReversedQuantity, 0) as Rev_Qty, SAL_OrderHeader.ContractNo, SAL_OrderHeader.ContractDate, SAL_OrderHeader.OrderID, 
                SAL_OrderHeader.CompanyID, SAL_OrderHeader.PeriodID 
            from SAL_OrderHeader 
                inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_OrderHeader.OrderID and SAL_OrderDetail.CompanyID = SAL_OrderHeader.CompanyID 
                    and SAL_OrderDetail.PeriodID = SAL_OrderHeader.PeriodID and SAL_OrderDetail.status = 1 and SAL_OrderHeader.Status = 1 
                    and SAL_OrderHeader.ClosedCode is null AND SAL_OrderHeader.cancelled <> 1 AND SAL_OrderHeader.StatusCode = 1 
                left JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
                    AND SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID AND SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
                    AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
                left JOIN Inv_StockTransactionDetail ON Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID 
                    AND Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
                    AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
                    AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            WHERE NOT EXISTS 
            (
                SELECT * FROM SAL_InvoiceDetail 
                WHERE SAL_InvoiceDetail.StockDetailID = Inv_StockTransactionDetail.TransactionDetailID 
                    AND SAL_InvoiceDetail.StockHeaderID = Inv_StockTransactionDetail.StockTransactionID 
                    AND SAL_InvoiceDetail.CompanyID = Inv_StockTransactionDetail.CompanyID AND SAL_InvoiceDetail.PeriodID = Inv_StockTransactionDetail.PeriodID
            )
        ) as tab 
        where (tab.SalesOrderQuantity - tab.Rev_Qty) - DeliveryOrderQuantity > 0
    ) tab2 on tab2.OrderID = soh.OrderID and tab2.CompanyID = soh.CompanyID and tab2.PeriodID = soh.PeriodID and soh.Status = 1 
    inner join SET_DocumentType on Set_DocumentType.DocumentTypeID = soh.SaleContractTypeID and Set_DocumentType.CompanyID = soh.CompanyID 
        and Set_DocumentType.PeriodID = soh.PeriodID and Set_DocumentType.Status = 1 and Soh.CompanyID = @CompanyID and Soh.PeriodID = @PeriodID 


and soh.ContractNo not in (
select SalesOrderHeaderID from 
SAL_DeliveryOrderdetail
where 
(SAL_DeliveryOrderDetail.quantity - isnull(SAL_DeliveryOrderDetail.reversedqty,0)) <> 0 
and SAL_DeliveryOrderdetail.PeriodID = @PeriodID
and SAL_DeliveryOrderdetail.CompanyID = @CompanyID
)


    
        ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetReversedDataForSalesLookUp, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetReversedDataForSalesLookUp, sqlparams);

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

        public static DataTable GetSalesOrderIDbyDOID(int companyID, int periodID, int DOHeaderID, string Query = "", SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DOHeaderID", DOHeaderID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);

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


        public DataTable GetDataByKeyForSalesReturn(int reverseheaderid, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query = @" 
select '' as Chassis,* from SAL_ReverseHeader 
inner join SAL_ReverseDetail on SAL_ReverseDetail.ReverseHeaderID = SAL_ReverseHeader.ReverseHeaderID
and SAL_ReverseDetail.CompanyID = SAL_ReverseHeader.CompanyID
and SAL_ReverseDetail.PeriodID = SAL_ReverseHeader.PeriodID
and SAL_ReverseDetail.Status = 1

where
SAL_ReverseHeader.Status = 1 
and SAL_ReverseHeader.CompanyID = @CompanyID
and SAL_ReverseHeader.PeriodID = @PeriodID

and SAL_ReverseHeader.ReverseHeaderID =@ReverseID
 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ReverseID", reverseheaderid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query, sqlparams);

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

        public DataTable GetDataByKeyTable(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetDataByOrderID = @" 
select max(ReverseHeaderID) as ReverseHeaderID, max(ReverseDetailID) as ReverseDetailID, MAX(ItemName) as ItemName, MAX(Rate) as Rate, MAX(SOQuantity) as SOQuantity, 
    SUM(DOQuantity) as DOQuantity, max(Reversed) as Reversed_Quantity, 
    (ISNULL(MAX(SOQuantity), 0) - isnull(SUM(DOQuantity), 0)) - isnull(MAX(Reversed), 0) as QtyCanBeReverse, max(ReversedQuantity) as ReversedQuantity, 
    max(ReversedAmount) as ReversedAmount, SourceHeaderID, SourceDetailID, SourceCode, SourceDocumentTypeID 
from 
(
    Select -1 as ReverseHeaderID, -1 as ReverseDetailID, INV_ITem.Description as ItemName, (Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity) as Rate, 
        Sal_OrderDetail.Quantity as SOQuantity, ISNULL(SAL_DeliveryOrderDetail.Quantity, 0) - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) as DOQuantity,  
        (Sal_OrderDetail.Quantity - ISNULL(SAL_DeliveryOrderDetail.Quantity, 0) - ISNULL(Sal_OrderDetail.ReversedQuantity, 0)) QtyCanBeReverse, 
        ISNULL(Sal_OrderDetail.ReversedQuantity, 0) Reversed, 0 as ReversedQuantity, 0 as ReversedAmount, Sal_OrderDetail.OrderID as SourceHeaderID, 
        Sal_OrderDetail.OrderDetailID as SourceDetailID, Sal_OrderHeader.ContractNo as SourceCode, Sal_OrderHeader.SaleContractTypeID as SourceDocumentTypeID 
    From Sal_OrderHeader 
        INNER JOIN Sal_OrderDetail ON Sal_OrderDetail.OrderID = Sal_OrderHeader.OrderID AND Sal_OrderDetail.CompanyID = Sal_OrderHeader.CompanyID 
            AND Sal_OrderDetail.PeriodID = Sal_OrderHeader.PeriodID 
        left JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.SalesOrderHeaderID = Sal_OrderDetail.OrderID 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID AND SAL_DeliveryOrderDetail.CompanyID = Sal_OrderDetail.CompanyID 
            AND SAL_DeliveryOrderDetail.PeriodID = Sal_OrderDetail.PeriodID 
        INNER JOIN INV_Item on INV_Item.ItemID = Sal_OrderDetail.ItemID --AND INV_Item.CompanyID = Sal_OrderDetail.CompanyID 
Where Sal_OrderHeader.CompanyID = @CompanyID AND Sal_OrderHeader.PeriodID = @PeriodID AND Sal_OrderHeader.OrderID =  @OrderID
) tab 
group by SourceHeaderID, SourceDetailID, SourceCode, SourceDocumentTypeID ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderID, sqlparams);

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

        public bool InsertReverseHeaderAndDetail(List<SAL_ReverseDetail> SAL_RDetail, SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Reverse");
            try
            {
                this.Insert(sqltrans);

                foreach (SAL_ReverseDetail srd in SAL_RDetail)
                {

                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(CompanyID, PeriodID, this.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = this.ReverseHeaderID;
                    srd.Insert(sqltrans);

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        //SAL_ReverseDetail srd = new SAL_ReverseDetail(CompanyID,PeriodID,
                        SAL_OrderDetail Sod = new SAL_OrderDetail(CompanyID, short.Parse(srd.SourceDetailID.ToString()), srd.SourceHeaderID, PeriodID);
                        Sod.CompanyID = CompanyID;
                        Sod.PeriodID = PeriodID;
                        Sod.OrderID = srd.SourceHeaderID;
                        Sod.OrderDetailID = Convert.ToInt16(srd.SourceDetailID);
                        if (!string.IsNullOrEmpty(Sod.ReversedQuantity.ToString()))
                        {
                            Sod.ReversedQuantity += srd.ReversedQuantity;
                        }
                        else
                        {
                            Sod.ReversedQuantity = srd.ReversedQuantity;
                        }
                        Sod.UpdateReversedQty(sqltrans);
                    }

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove" OR "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Reverse");
            }

            return result;
        }

        public static bool UpdateReverseHederAndDetail(SAL_ReverseHeader obj, SET_DocumentApprove da, List<SAL_ReverseDetail> colAddTranDetail, List<SAL_ReverseDetail> colUpdateTranDetail, List<SAL_ReverseDetail> colDeletTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReverseHeaderAndDetail");

            try
            {
                obj.UpdateDataByKey(sqltrans);

                foreach (SAL_ReverseDetail srd in colAddTranDetail)
                {
                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(obj.CompanyID, obj.PeriodID, obj.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = obj.ReverseHeaderID;
                    srd.Insert(sqltrans);

                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_OrderDetail Sod = new SAL_OrderDetail();
                        Sod.CompanyID = obj.CompanyID;
                        Sod.PeriodID = obj.PeriodID;
                        Sod.OrderID = srd.SourceHeaderID;
                        Sod.OrderDetailID = Convert.ToInt16(srd.SourceDetailID);
                        Sod.ReversedQuantity = srd.ReversedQuantity;
                        Sod.UpdateReversedQty(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdUpdate in colUpdateTranDetail)
                {
                    tdUpdate.UpdateDataByKey(sqltrans);
                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_OrderDetail Sod = new SAL_OrderDetail(tdUpdate.CompanyID, (short)tdUpdate.SourceDetailID, tdUpdate.SourceHeaderID, tdUpdate.PeriodID);
                        decimal? oldReversed = Sod.ReversedQuantity.HasValue ? Sod.ReversedQuantity : 0;
                        Sod.CompanyID = obj.CompanyID;
                        Sod.PeriodID = obj.PeriodID;
                        Sod.OrderID = tdUpdate.SourceHeaderID;
                        Sod.OrderDetailID = Convert.ToInt16(tdUpdate.SourceDetailID);
                        Sod.ReversedQuantity = tdUpdate.ReversedQuantity + oldReversed;
                        Sod.UpdateReversedQty(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, obj.ReverseHeaderID);
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReverseHeaderAndDetail");
            }

            return result;
        }

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int reverseHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int reverseHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
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

        public static DataTable GetAllDataSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDOReturn_With_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_With_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDO_Without_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public static DataTable GetAllDataSearch1(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDOReturn_With_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_With_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDO_Without_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool DeleteTransactionHederAndDetail(List<SAL_ReverseDetail> colDeletTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteReverseHederAndDetail");
            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, f_ReverseHeaderID);
                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, tdDelete.ReverseHeaderID);
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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteReverseHederAndDetail");
            }
            return result;
        }

        public static DataTable GetDataByKeyTableForDOReturn(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderIDForDOReturn, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderIDForDOReturn, sqlparams);

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

        public bool InsertReverseHeaderAndDetailForDO(List<SAL_ReverseDetail> SAL_RDetail, SET_DocumentApprove da, int VehicleID_PRN, int DEliverOrdID, int VehicleID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Reverse");
            try
            {
                this.Insert(sqltrans);

                foreach (SAL_ReverseDetail srd in SAL_RDetail)
                {

                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(CompanyID, PeriodID, this.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = this.ReverseHeaderID;
                    srd.Insert(sqltrans);

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_DeliveryOrderDetail objUpdate = new SAL_DeliveryOrderDetail(CompanyID, short.Parse(srd.SourceDetailID.ToString()), int.Parse(srd.SourceHeaderID.ToString()), PeriodID);
                        objUpdate.ReversedQty = objUpdate.ReversedQty == null ? srd.ReversedQuantity : objUpdate.ReversedQty + srd.ReversedQuantity;
                        objUpdate.UpdateDataByKey(sqltrans);
                    }

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    da.Insert(sqltrans);

                #endregion

                //SAL_DispatchVehicleInformation salVehicleChk = new SAL_DispatchVehicleInformation(CompanyID, PeriodID, VehicleID_PRN);
                if (VehicleID_PRN > 0)
                {

                    SAL_DispatchVehicleInformation salVehicle = new SAL_DispatchVehicleInformation();
                    salVehicle.VehicleID = VehicleID;
                    salVehicle.CompanyID = CompanyID;
                    salVehicle.PeriodID = PeriodID;
                    // null becouse Appering agin in DO
                    salVehicle.VehicleInformationID = null;
                    salVehicle.UpdateDataByVehicleID(sqltrans);

                    SAL_DOVehicleInformation DOV = new SAL_DOVehicleInformation(CompanyID, short.Parse(VehicleID_PRN.ToString()), PeriodID);
                    DOV.Status = false;
                    DOV.VehicleID = null;
                    DOV.UpdateDataByKey(sqltrans);
                }





                // cancle Delivery Order
                SAL_DeliveryOrderHeader SDO = new SAL_DeliveryOrderHeader(CompanyID, DEliverOrdID, PeriodID);
                SDO.IsCancelled = true;
                // SDO.StatusCode = 2;
                SDO.UpdateDataByKey(sqltrans);


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
                    SqlHelper.RollBackTransaction(sqltrans, "Reverse");
            }

            return result;
        }

        public static bool UpdateReverseHederAndDetailFORDO(SAL_ReverseHeader obj, SET_DocumentApprove da, List<SAL_ReverseDetail> colAddTranDetail, List<SAL_ReverseDetail> colUpdateTranDetail, List<SAL_ReverseDetail> colDeletTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReverseHeaderAndDetail");

            try
            {
                obj.UpdateDataByKey(sqltrans);

                foreach (SAL_ReverseDetail srd in colAddTranDetail)
                {
                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(obj.CompanyID, obj.PeriodID, obj.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = obj.ReverseHeaderID;
                    srd.Insert(sqltrans);

                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_DeliveryOrderDetail objUpdate = new SAL_DeliveryOrderDetail(obj.CompanyID, short.Parse(srd.SourceDetailID.ToString())
                                    , int.Parse(srd.SourceHeaderID.ToString()), obj.PeriodID);
                        objUpdate.ReversedQty = srd.ReversedQuantity;
                        objUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdUpdate in colUpdateTranDetail)
                {
                    tdUpdate.UpdateDataByKey(sqltrans);
                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_DeliveryOrderDetail objUpdate = new SAL_DeliveryOrderDetail(obj.CompanyID, short.Parse(tdUpdate.SourceDetailID.ToString())
                                   , int.Parse(tdUpdate.SourceHeaderID.ToString()), obj.PeriodID);
                        objUpdate.ReversedQty = tdUpdate.ReversedQuantity;
                        objUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, obj.ReverseHeaderID);
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReverseHeaderAndDetail");
            }

            return result;
        }

        public static DataTable GetDataByKeyTableForDispatchReturn(int dispatchID, int companyID, int periodID, bool isvehicle = false, SqlConnection objConnection = null, int CurrentFiscalID = 0)
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
                //Query_GetDataByOrderIDForDispatchReturn_Cane

                if (isvehicle)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderIDForDispatchReturn, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderIDForDispatchReturn, sqlparams);
                }
                else
                {
                    //if (objConnection == null)
                    //    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderIDForDispatchReturn, sqlparams);
                    //else
                    //    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderIDForDispatchReturn, sqlparams);

                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderIDForDispatchReturn_Cane, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderIDForDispatchReturn_Cane, sqlparams);
                }
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

        public static DataTable GetDataByKeyTableForTareWeight(int dispatchID, int companyID, int periodID, bool isvehicle = false, SqlConnection objConnection = null, int CurrentFiscalID = 0)
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
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByOrderIDForTareWeight, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByOrderIDForTareWeight, sqlparams);
                
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

        public bool InsertReverseHeaderAndDetailForDIspatchReturn(SAL_DispatchHeader sal_dispatch,INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, List<SAL_ReverseDetail> SAL_RDetail, SET_DocumentApprove da, bool IsVehicle, List<int> Vehicleids, SAL_DispatchHeader dispatchUpdate,int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Reverse");
            try
            {
                
                #region For Stock Transaction Header & Detail

                
                //sth.DocumentNo = int.Parse(f_DocumentNo.ToString());
                sth.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(sth.DocumentTypeID, sth.CompanyID, sth.PeriodID, null, CurrentFiscalID);
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                sth.FiscalID = CurrentFiscalID;
                sth.Insert(sqltrans);


                sal_dispatch.DispatchNo = sth.DocumentNo.ToString();
                sal_dispatch.Insert();

                
                dispatchUpdate.ReturnID = sal_dispatch.DispatchID;
                dispatchUpdate.UpdateDataByKey(sqltrans);

                long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(f_CompanyID, f_PeriodID, sqltrans);

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    std.StockTransactionID = sth.StockTransactionID;
                    std.TransactionDetailID = transactionDetailID++;
                    std.ImpactID = 1; // 1 for Positive Imoact
                    std.FiscalID = CurrentFiscalID;
                    std.Insert(sqltrans);
                }

                #endregion



                this.Insert(sqltrans);

                foreach (SAL_ReverseDetail srd in SAL_RDetail)
                {

                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(CompanyID, PeriodID, this.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = this.ReverseHeaderID;
                    srd.FiscalID = CurrentFiscalID;
                    srd.Insert(sqltrans);

                    if (IsVehicle)
                    {
                        INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(srd.SourceDetailID.ToString()));
                        //decimal ReversedQuantity = decimal.Parse(stdUpdate.ReversedQuantity == null ? "0" : stdUpdate.ReversedQuantity.ToString());
                        //if (ReversedQuantity == 0)
                        //    stdUpdate.ReversedQuantity = ReversedQuantity + srd.ReversedQuantity;
                        stdUpdate.ReversedQuantity = srd.ReversedQuantity;
                        stdUpdate.TransactionQuantity += srd.ReversedQuantity;
                        stdUpdate.FiscalID = CurrentFiscalID;
                        stdUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                if (Vehicleids.Count > 0)
                {
                    int i = 0;
                    foreach (int Ids in Vehicleids)
                    {
                        SAL_DispatchVehicleInformation salVehicle = new SAL_DispatchVehicleInformation();
                        salVehicle.VehicleID = int.Parse(Vehicleids[i].ToString());
                        salVehicle.CompanyID = CompanyID;
                        salVehicle.PeriodID = PeriodID;
                        // null becouse Appering agin in DO
                        salVehicle.VehicleInformationID = null;
                        
                        salVehicle.UpdateDataByVehicleID(sqltrans);

                        SAL_DOVehicleInformation DOV = new SAL_DOVehicleInformation(CompanyID, short.Parse(Vehicleids[i].ToString()), PeriodID);
                        DOV.Status = false;
                        DOV.VehicleID = null;
                        DOV.UpdateDataByKey(sqltrans);
                        
                    }

                    
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"
                da.FiscalID = CurrentFiscalID;
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "Reverse");
            }

            return result;
        }

        public static bool UpdateReverseHederAndDetailFORDispatch(SAL_ReverseHeader obj, SET_DocumentApprove da, List<SAL_ReverseDetail> colAddTranDetail, List<SAL_ReverseDetail> colUpdateTranDetail, List<SAL_ReverseDetail> colDeletTranDetail, INV_StockTransactionHeader sth, List<INV_StockTransactionDetail> colSTDetail, List<GL_TransactionDetail> colTranDetail, GL_TransactionHeader th)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReverseHeaderAndDetail");

            try
            {
                obj.UpdateDataByKey(sqltrans);

                foreach (SAL_ReverseDetail srd in colAddTranDetail)
                {
                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(obj.CompanyID, obj.PeriodID, obj.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = obj.ReverseHeaderID;
                    srd.Insert(sqltrans);

                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(srd.SourceDetailID.ToString()));
                        stdUpdate.ReversedQuantity = srd.ReversedQuantity;
                        stdUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdUpdate in colUpdateTranDetail)
                {
                    tdUpdate.UpdateDataByKey(sqltrans);
                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(tdUpdate.SourceDetailID.ToString()));
                        stdUpdate.ReversedQuantity = tdUpdate.ReversedQuantity;
                        stdUpdate.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, obj.ReverseHeaderID);
                }



                #region For Stock Transaction Header & Detail

                sth.DocumentNo = int.Parse(obj.DocumentNo.ToString());
                sth.StockTransactionID = INV_StockTransactionHeader.GetNewStockTransactionID(obj.CompanyID, obj.PeriodID, sqltrans);
                sth.Insert(sqltrans);

                long transactionDetailID = INV_StockTransactionDetail.GetNewTransactionDetailID(obj.CompanyID, obj.PeriodID, sqltrans);

                foreach (INV_StockTransactionDetail std in colSTDetail)
                {
                    std.StockTransactionID = sth.StockTransactionID;
                    std.TransactionDetailID = transactionDetailID++;
                    std.Insert(sqltrans);
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = int.Parse(obj.DocumentNo.ToString());
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(obj.CompanyID, obj.PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = obj.ReverseHeaderID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion


                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
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
                    SqlHelper.CommitTransaction(sqltrans);
                else
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReverseHeaderAndDetail");
            }

            return result;
        }

        public static DataTable GetDataByKeyTableForInvoiceReturn(int invoiceID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKeyTableForInvoiceReturn, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByKeyTableForInvoiceReturn, sqlparams);

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

        public bool InsertReverseHeaderAndDetailForInvoiceReturn(List<SAL_ReverseDetail> SAL_RDetail, SET_DocumentApprove da,int CurrentFiscalID= 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Reverse");
            try
            {
                this.Insert(sqltrans);

                foreach (SAL_ReverseDetail srd in SAL_RDetail)
                {

                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(CompanyID, PeriodID, this.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = this.ReverseHeaderID;
                    srd.FiscalID = CurrentFiscalID;
                    srd.Insert(sqltrans);

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(CompanyID, short.Parse(srd.SourceDetailID.ToString()), srd.SourceHeaderID, PeriodID);
                        sid.ReversedQuantity = srd.ReversedQuantity;
                        sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKey(sqltrans);
                    }

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
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
                    SqlHelper.RollBackTransaction(sqltrans, "Reverse");
            }

            return result;
        }

        public static bool UpdateReverseHederAndDetailFORInvoice(SAL_ReverseHeader obj, SET_DocumentApprove da, List<SAL_ReverseDetail> colAddTranDetail, List<SAL_ReverseDetail> colUpdateTranDetail, List<SAL_ReverseDetail> colDeletTranDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReverseHeaderAndDetail");

            try
            {
                obj.UpdateDataByKey(sqltrans);

                foreach (SAL_ReverseDetail srd in colAddTranDetail)
                {
                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(obj.CompanyID, obj.PeriodID, obj.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = obj.ReverseHeaderID;
                    srd.FiscalID = CurrentFiscalID;
                    srd.Insert(sqltrans);

                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(obj.CompanyID, short.Parse(srd.SourceDetailID.ToString()), srd.SourceHeaderID, obj.PeriodID);
                        sid.ReversedQuantity = srd.ReversedQuantity;
                        sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdUpdate in colUpdateTranDetail)
                {
                    tdUpdate.FiscalID = CurrentFiscalID;
                    tdUpdate.UpdateDataByKey(sqltrans);
                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(obj.CompanyID, short.Parse(tdUpdate.SourceDetailID.ToString()), tdUpdate.SourceHeaderID, obj.PeriodID);
                        sid.ReversedQuantity = tdUpdate.ReversedQuantity;
                        sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, obj.ReverseHeaderID);
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReverseHeaderAndDetail");
            }

            return result;
        }

        public static DataTable GetAllDataSearch3(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.Sales.SAL_OrderHeader.Query_SelectAllDOReturn_With_Paging3 + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_With_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDO_Without_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool InsertReverseHeaderAndDetailForInvoiceReturn(List<SAL_ReverseDetail> SAL_RDetail, SET_DocumentApprove da, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("Reverse");
            try
            {
                this.Insert(sqltrans);

                foreach (SAL_ReverseDetail srd in SAL_RDetail)
                {

                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(CompanyID, PeriodID, this.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = this.ReverseHeaderID;
                    srd.FiscalID = CurrentFiscalID;
                    srd.Insert(sqltrans);

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(CompanyID, short.Parse(srd.SourceDetailID.ToString()), srd.SourceHeaderID, PeriodID);
                        sid.ReversedQuantity = srd.ReversedQuantity;
                        //sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKeyOnlyReversedQty(sqltrans);
                    }

                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = int.Parse(this.DocumentNo.ToString());
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
                        td.FiscalID = CurrentFiscalID;
                        td.SourceHeaderID = this.ReverseHeaderID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
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
                    SqlHelper.RollBackTransaction(sqltrans, "Reverse");
            }

            return result;
        }

        public static bool UpdateReverseHederAndDetailFORInvoice(SAL_ReverseHeader obj, SET_DocumentApprove da, List<SAL_ReverseDetail> colAddTranDetail, List<SAL_ReverseDetail> colUpdateTranDetail, List<SAL_ReverseDetail> colDeletTranDetail, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateReverseHeaderAndDetail");

            try
            {
                obj.UpdateDataByKey(sqltrans);

                foreach (SAL_ReverseDetail srd in colAddTranDetail)
                {
                    srd.ReverseDetailID = Convert.ToInt16(SAL_ReverseDetail.GetNewReverseDetailID(obj.CompanyID, obj.PeriodID, obj.ReverseHeaderID, sqltrans));
                    srd.ReverseHeaderID = obj.ReverseHeaderID;
                    srd.FiscalID = CurrentFiscalID;
                    srd.Insert(sqltrans);

                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(obj.CompanyID, short.Parse(srd.SourceDetailID.ToString()), srd.SourceHeaderID, obj.PeriodID);
                        sid.ReversedQuantity = srd.ReversedQuantity;
                        //sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKeyOnlyReversedQty(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdUpdate in colUpdateTranDetail)
                {
                    tdUpdate.FiscalID = CurrentFiscalID;
                    tdUpdate.UpdateDataByKey(sqltrans);
                    if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        SAL_InvoiceDetail sid = new SAL_InvoiceDetail(obj.CompanyID, short.Parse(tdUpdate.SourceDetailID.ToString()), tdUpdate.SourceHeaderID, obj.PeriodID);
                        sid.ReversedQuantity = tdUpdate.ReversedQuantity;
                        // sid.FiscalID = CurrentFiscalID;
                        sid.UpdateDataByKey(sqltrans);
                    }
                }

                foreach (SAL_ReverseDetail tdDelete in colDeletTranDetail)
                {
                    SAL_ReverseDetail.DeleteDataByKey(sqltrans, (Guid)tdDelete.UpdatedBy, (DateTime)tdDelete.UpdateDate, tdDelete.UpdatedByValue, tdDelete.CompanyID, tdDelete.CompanyID, tdDelete.ReverseDetailID, obj.ReverseHeaderID);
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = int.Parse(obj.DocumentNo.ToString());
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(obj.CompanyID, obj.PeriodID, sqltrans);
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
                        td.FiscalID = CurrentFiscalID;
                        td.SourceHeaderID = obj.ReverseHeaderID;
                        td.Insert(sqltrans);
                    }
                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (obj.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateReverseHeaderAndDetail");
            }

            return result;
        }

        public static DataTable GetAllDataSearch2(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAllDOReturn_With_Paging2 + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_With_Paging2 + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAllDO_Without_Paging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByKeyCustomize(int companyID, int periodID, int reverseHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKeyCustomize, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKeyCustomize, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKeyCustomize, sqlparams);

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

        #endregion
    }
}
