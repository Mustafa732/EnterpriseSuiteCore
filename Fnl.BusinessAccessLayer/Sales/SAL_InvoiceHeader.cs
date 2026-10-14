using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.AR.Transaction;


namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_InvoiceHeader:BaseClass
    {
        #region Constant Queries

        const string Query_GetAllSalesTaxInvoiceCancellationReport = @"select InvoiceNumber,InvoiceType,InvoiceDate,DocumentType,SAL_InvoiceHeader.Description as Remarks,CustomerAddress as RequestedBy
from SAL_InvoiceHeader
inner join set_documenttype on set_documenttype.DocumentTypeID = set_documenttype.DocumentTypeID
and set_documenttype.CompanyID = SAL_InvoiceHeader.CompanyID
and set_documenttype.PeriodID = SAL_InvoiceHeader.PeriodID
where 
    SET_DocumentType.DocumentType like '%Sale%'
   and  SAL_InvoiceHeader.Status = 0
    and SAL_InvoiceHeader.InvoiceType = SET_DocumentType.DocumentTypeID
    and SAL_InvoiceHeader.CompanyID =@CompanyID
    and SAL_InvoiceHeader.PeriodID = @PeriodID
    and SAL_InvoiceHeader.InvoiceDate between @fromDate and @toDate
";
        const string Query_SelectAll_Searching_WithPaging = @" SELECT *, ROW_NUMBER() OVER (ORDER BY SAL_InvoiceHeader.InvoiceID DESC) AS 'Row' FROM 
(
    SELECT DISTINCT SAL_InvHdr.CompanyID, SAL_InvHdr.PeriodID, SAL_InvHdr.InvoiceID, SAL_InvHdr.AcctsPayCodeCombinationID, SAL_InvHdr.AmountApplicableToDiscount, 
        SAL_InvHdr.AmountPaid, SAL_InvHdr.ApprovalDescription, SAL_InvHdr.ApprovalStatus, SAL_InvHdr.ApprovedAmount, SAL_InvHdr.AuthorizedBy, 
        SAL_InvHdr.AutoTaxCalculationFlag, SAL_InvHdr.AutoWithholdingTaxFlag, SAL_InvHdr.AutoWithholdingTaxGroupID, SAL_InvHdr.BatchID, SAL_InvHdr.CancelledAmount, 
        SAL_InvHdr.CancelledBy, SAL_InvHdr.CancelledDate, SAL_InvHdr.Description, SAL_InvHdr.DiscountAmountTaken, SAL_InvHdr.DocumentSequenceID, 
        SAL_InvHdr.EarliestSettlementDate, SAL_InvHdr.ExchangeDate, SAL_InvHdr.ExchangeRate, SAL_InvHdr.ExchangeRateType, SAL_InvHdr.ExclusivePaymentFlag, 
        SAL_InvHdr.FreightAmount, SAL_InvHdr.GLPostingDate, SAL_InvHdr.GoodsReceivedDate, SAL_InvHdr.GrossAmount, SAL_InvHdr.InvoiceCurrencyID, 
        SAL_InvHdr.InvoiceDate, SAL_InvHdr.InvoiceNumber, SAL_InvHdr.InvoiceReceivedDate, SAL_InvHdr.InvoiceType, SAL_InvHdr.PayCurrencyInvoiceAmount, 
        SAL_InvHdr.PaymentGroupType, SAL_InvHdr.PaymentCrossRate, SAL_InvHdr.PaymentCrossRateDate, SAL_InvHdr.PaymentCrossRateType, SAL_InvHdr.PaymentCurrencyID, 
        SAL_InvHdr.PaymentMethodType, SAL_InvHdr.PaymentStatusType, SAL_InvHdr.PostingStatus, SAL_InvHdr.RecurringPaymentID, SAL_InvHdr.TaxAmount, 
        SAL_InvHdr.TermsDate, SAL_InvHdr.TermsID, SAL_InvHdr.VATCode, SAL_InvHdr.CustomerID, SAL_InvHdr.VendorSiteID, SAL_InvHdr.VoucherNumber, SAL_InvHdr.Status, 
        SAL_InvHdr.CreatedBy, SAL_InvHdr.CreateDate, SAL_InvHdr.CreatedByValue, SAL_InvHdr.UpdatedBy, SAL_InvHdr.UpdateDate, SAL_InvHdr.UpdatedByValue, 
        SAL_InvHdr.StatusCode ,Set_PeriodCustom.PeriodSetName,SAL_DispatchHeader.DispatchID, C.CustomerName
        ,CAST((Select isnull(sum(Sal_InvoiceDetail_Inner.QuantityInvoiced),0) from Sal_InvoiceDetail as Sal_InvoiceDetail_Inner
		   Where Sal_InvoiceDetail_Inner.InvoiceHeaderID = SAL_InvHdr.InvoiceID
		        And Sal_InvoiceDetail_Inner.CompanyID = SAL_InvHdr.CompanyID
				And Sal_InvoiceDetail_Inner.FiscalID = SAL_InvHdr.FiscalID
				ANd Sal_InvoiceDetail_Inner.Status = 1
		) as decimal(18,2)) as TotalQuantityInvoiced
,case WHEN ISNULL(SAL_InvoiceResponse.StatusCode ,'') = '' THEN 'Pending' ELSE 'Synced' END AS FBRStatus
,SAL_InvoiceResponse.Status + ' - ' + SAL_InvoiceResponse.error AS FBRError
    FROM SAL_InvoiceHeader AS SAL_InvHdr 
LEFT JOIN SAL_InvoiceResponse ON  SAL_InvoiceResponse.SalesInvoiceID =SAL_InvHdr.InvoiceID
		AND SAL_InvoiceResponse.InvoiceCompanyID =SAL_InvHdr.CompanyID
		AND SAL_InvoiceResponse.InvoiceFiscalID =SAL_InvHdr.FiscalID
        LEFT JOIN AR_SET_Customer C ON SAL_InvHdr.CustomerID = C.CustomerID  AND C.Status = 1
             
        LEFT JOIN AR_SET_BusinessPartnerTypes BP ON BP.BusinessPartnerID = C.CustomerID AND BP.PeriodID = C.PeriodID AND C.Status = 1
left join SAL_DispatchHeader on SAL_DispatchHeader.DispatchID = SAL_InvHdr.InvoiceNumber
    inner join Set_PeriodCustom on  
 SAL_InvHdr.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

) SAL_InvoiceHeader ";



        const string Query_SelectAll_Searching_ForApprove = @" 
SELECT DISTINCT SAL_InvoiceHeader.InvoiceID
	,SAL_InvoiceHeader.CompanyID
	,SAL_InvoiceHeader.PeriodID
	,SAL_InvoiceHeader.Status
	,SAL_InvoiceHeader.InvoiceType
	,SAL_InvoiceHeader.InvoiceNumber
	,SAL_InvoiceHeader.InvoiceDate
	,SAL_InvoiceHeader.InvoiceReceivedDate
	,SAL_InvoiceHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel
    ,SAL_InvoiceHeader.InvoicedTo
    ,Set_PeriodCustom.PeriodSetName
FROM SAL_InvoiceHeader
INNER JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID  = AR_SET_Customer.CustomerID 
	AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID 
	AND AR_SET_Customer.Status = 1
INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
	AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	AND AR_SET_Customer.Status = 1
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_InvoiceHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_InvoiceHeader.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
	AND SET_DocumentApprove.DocumentNo = SAL_InvoiceHeader.InvoiceNumber 
	AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SAL_InvoiceHeader.CompanyID
		AND PeriodID = SAL_InvoiceHeader.PeriodID AND DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
		AND DocumentNo = SAL_InvoiceHeader.InvoiceNumber)
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_InvoiceHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_InvoiceHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_InvoiceHeader.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
											WHERE ih.CompanyID = SAL_InvoiceHeader.CompanyID 
												AND ih.PeriodID = SAL_InvoiceHeader.PeriodID 
												AND ih.InvoiceID = SAL_InvoiceHeader.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
													WHERE ih.CompanyID = SAL_InvoiceHeader.CompanyID 
														AND ih.PeriodID = SAL_InvoiceHeader.PeriodID 
														AND ih.InvoiceID = SAL_InvoiceHeader.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 

inner join Set_PeriodCustom on  
SAL_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

";

        const string Query_SelectAll_Searching_WithPaging_ForApprove = @" 
SELECT *, ROW_NUMBER() OVER (ORDER BY SAL_InvoiceHeader.InvoiceID DESC) AS 'Row' 
FROM 
	(
		SELECT DISTINCT InvHeader_Outer.InvoiceID,InvHeader_Outer.CompanyID,InvHeader_Outer.PeriodID,InvHeader_Outer.Status
			,InvHeader_Outer.InvoiceType,InvHeader_Outer.InvoiceNumber,InvHeader_Outer.InvoiceDate,InvHeader_Outer.InvoiceReceivedDate ,InvHeader_Outer.StatusCode
			,SET_DocumentApprove.ApprovalStatus
			,Set_documentPendingApprove.ApprovalOperator
			,Set_documentPendingApprove.ApprovalUserID AS UserID,InvHeader_Outer.InvoicedTo ,SET_Documenttype.DocumentType ,Set_PeriodCustom.PeriodSetName,C.CustomerName
            ,CAST((Select isnull(sum(Sal_InvoiceDetail_Inner.QuantityInvoiced),0) from Sal_InvoiceDetail as Sal_InvoiceDetail_Inner
		    Where Sal_InvoiceDetail_Inner.InvoiceHeaderID = InvHeader_Outer.InvoiceID
		        And Sal_InvoiceDetail_Inner.CompanyID = InvHeader_Outer.CompanyID
				And Sal_InvoiceDetail_Inner.FiscalID = InvHeader_Outer.FiscalID
				ANd Sal_InvoiceDetail_Inner.Status = 1
		    ) as decimal(18,2)) as TotalQuantityInvoiced ,'' as FBRStatus,'' as FBRError 
		FROM SAL_InvoiceHeader InvHeader_Outer
        LEFT JOIN AR_SET_Customer C ON InvHeader_Outer.CustomerID = C.CustomerID  AND C.Status = 1
		Left JOIN AR_SET_Customer ON InvHeader_Outer.CustomerID  = AR_SET_Customer.CustomerID 
			AND InvHeader_Outer.CompanyID = AR_SET_Customer.CompanyID AND InvHeader_Outer.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1
		Left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
			AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = InvHeader_Outer.CompanyID
			AND SET_DocumentApprove.PeriodID = InvHeader_Outer.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = InvHeader_Outer.InvoiceType
			AND SET_DocumentApprove.DocumentNo = InvHeader_Outer.InvoiceNumber 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = InvHeader_Outer.CompanyID
				AND PeriodID = InvHeader_Outer.PeriodID AND DocumentTypeID = InvHeader_Outer.InvoiceType 
				AND DocumentNo = InvHeader_Outer.InvoiceNumber)
			
				inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
            and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
            and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
				
            inner join SET_Documenttype on SET_Documenttype.DocumentTypeID =  InvHeader_Outer.InvoiceType
            and SET_Documenttype.CompanyID =  InvHeader_Outer.CompanyID
            and SET_Documenttype.periodId =  InvHeader_Outer.periodId
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = InvHeader_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = InvHeader_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = InvHeader_Outer.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
													WHERE ih.CompanyID = InvHeader_Outer.CompanyID 
														AND ih.PeriodID = InvHeader_Outer.PeriodID 
														AND ih.InvoiceID = InvHeader_Outer.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
															WHERE ih.CompanyID = InvHeader_Outer.CompanyID 
																AND ih.PeriodID = InvHeader_Outer.PeriodID 
																AND ih.InvoiceID = InvHeader_Outer.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
		
		

    LEFT join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID


    inner join Set_PeriodCustom on  
 InvHeader_Outer.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
	) SAL_InvoiceHeader 

";

        const string Query_UpdateIsInvoiceAdjusted = @"Update AR_ReceiptTransactionHeader
                    Set IsInvoiceAdjusted = @status
                    Where 
                    Status = 1
                    And CompanyId = @CompanyID
                    And PeriodID = @PeriodID
                    And ReceiptTransactionID = @PID";

        const string Query_Get_Fabric_Sal_InvoiceHeader = @"        
              select distinct sal_invoiceheader.*,fabrics_sal_invoicedetail.LotNo, fabrics_sal_invoicedetail.TokenNo,Set_PeriodCustom.PeriodSetName 
,sal_invoiceheader.InvoiceDate as DocumentDate
,ROW_NUMBER() Over (Order by sal_invoiceheader.CompanyID, sal_invoiceheader.InvoiceID, sal_invoiceheader.PeriodID) as 'Row'
from sal_invoiceheader
inner join fabrics_sal_invoicedetail on fabrics_sal_invoicedetail.InvoiceHeaderID = sal_invoiceheader.InvoiceID
left join Set_PeriodCustom on sal_invoiceheader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1
where sal_invoiceheader.InvoiceID = fabrics_sal_invoicedetail.InvoiceHeaderID

        ";

        const string Query_Get_Invoice_By_DocumentType = @" SELECT SAL_InvoiceResponse.Status as FBRStatus,* FROM SAL_InvoiceHeader
left join SAL_InvoiceResponse on SAL_InvoiceResponse.SalesInvoiceID = SAL_InvoiceHeader.InvoiceID
and SAL_InvoiceResponse.InvoiceCompanyID = SAL_InvoiceHeader.CompanyID
and SAL_InvoiceResponse.Status = 'Valid'
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID AND SAL_InvoiceHeader.InvoiceNumber = @DocumentNo AND SAL_InvoiceHeader.Status = 1 and SAL_InvoiceHeader.FiscalID = @FiscalID ";

        const string Query_Get_Invoice_By_DocumentTypeForFBR = @" SELECT dbo.SAL_InvoiceHeader.*
FROM dbo.SAL_InvoiceHeader
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID
  AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.FiscalID = @FiscalID 
  AND convert(date,[SAL_InvoiceHeader].[InvoiceDate]) Between @DateFrom AND @DateTo
AND SAL_InvoiceHeader.statusCode = 1
and SAL_InvoiceHeader.IsReversed is null and SAL_InvoiceHeader.ReversalID is null
  AND NOT EXISTS (
        SELECT SAL_InvoiceResponse.SalesInvoiceID 
        FROM dbo.SAL_InvoiceResponse
        WHERE SAL_InvoiceResponse.SalesInvoiceID = SAL_InvoiceHeader.InvoiceID
AND SAL_InvoiceResponse.InvoiceNumber IS NOT null
  )

SELECT COUNT(SalesInvoiceID)  AS TotalProceed
FROM dbo.SAL_InvoiceResponse
INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceResponse.SalesInvoiceID
AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceResponse.InvoiceCompanyID
WHERE SAL_InvoiceHeader.CompanyID  = @CompanyID  
AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID
AND SAL_InvoiceResponse.InvoiceNumber IS NOT null
and SAL_InvoiceHeader.IsReversed is null and SAL_InvoiceHeader.ReversalID is null

";


        const string Query_GetAmountOnDirectReciept = @"Select IsNull(SUM(AmountPaid),0) As AmountPaid From SAL_InvoiceHeader
                        Where Status = 1
                        And CompanyID = @CompanyID
                        And PeriodID = @PeriodID
                        and PaymentStatusType = @RID";

        const string Query_Report_VehicleSalesInvoice = @"
Select 
	Sal_InvoiceHeader.InvoiceDate As Date,
	Sal_InvoiceHeader.InvoiceNumber AS InvoiceNo,
	Sal_OrderDetail.BaseAmount AS Amount,
	AR_Set_Customer.CustomerName AS Customer,
	ARS.CustomerName As Agent,
	AR_Set_Customer.FatherName AS Father,
	AR_Set_Customer.CNICNumber AS CNIC,
	AR_Set_Customer.NTNNumber AS NTN,
	AR_Set_Customer.STRNNumber AS STRN,
	AR_Set_Customer.AddressLine1 AS Address,
	INV_Set_Category.CategoryName AS VehicleModel,
	INV_Item.Description AS Description,
	SAL_DOVehicleInformation.Chassis AS ChassNo,
	SAL_DOVehicleInformation.Engine AS EnginerNo,
	Set_Color.ColorName As Color,
	SET_OperatingUnit.LogoImage
From Sal_InvoiceHeader 
	Inner Join Sal_InvoiceDetail ON Sal_InvoiceDetail.InvoiceHeaderID = Sal_InvoiceHeader.InvoiceID 
		AND Sal_InvoiceDetail.CompanyID = Sal_InvoiceHeader.CompanyID AND Sal_InvoiceDetail.PeriodID = Sal_InvoiceHeader.PeriodID 
		AND Sal_InvoiceDetail.Status = 1 
	Left Join AR_Set_Customer ON AR_Set_Customer.CustomerID = Sal_InvoiceHeader.billedtoid 
		--AND AR_Set_Customer.CompanyID = Sal_InvoiceHeader.CompanyID AND AR_Set_Customer.PeriodID = Sal_InvoiceHeader.PeriodID 
		AND AR_Set_Customer.Status = 1
	INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
		AND INV_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
		AND INV_StockTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID AND INV_StockTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID 
		AND INV_StockTransactionDetail.Status = 1
	Inner Join SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
		AND SAL_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
		AND SAL_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
		AND SAL_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID AND SAL_DeliveryOrderDetail.Status = 1
	Inner Join SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
		AND SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
		AND SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
		AND SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID 
		AND SAL_DOVehicleInformation.Status = 1
	Inner Join SAL_OrderDetail on SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
		AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
		AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.Status = 1 
	Inner Join SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
		AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID AND SAL_OrderHeader.Status = 1
	Left Join Set_Color ON Set_Color.ColorID = SAL_OrderDetail.ColorID AND Set_Color.Status = 1
	Inner Join INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
--AND INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 
		AND INV_Item.Status = 1
	Inner Join SET_Company ON SET_Company.CompanyID = Sal_InvoiceHeader.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitID = SET_Company.OperatingUnitID
	Inner Join AR_Set_Customer ARS ON ARS.CustomerID = Sal_OrderHeader.CustomerID
	Inner Join INV_Set_Category ON INV_Set_Category.CategoryID = INV_Item.ItemTypeID
		-- 
		and INV_Set_Category.Status = 1
	AND ARS.CompanyID = Sal_InvoiceHeader.CompanyID
	AND ARS.PeriodID = Sal_InvoiceHeader.PeriodID
	AND ARS.Status = 1

Where Sal_InvoiceHeader.CompanyID = @CompanyID
AND Sal_InvoiceHeader.PeriodID = @PeriodID
AND Sal_InvoiceHeader.Status = 1
AND Sal_InvoiceHeader.InvoiceNumber = @SONumber
";

        const string Query_Report_ARBillWise = @" 
        SELECT 
        [InvoiceType], 
        [InvoiceDate], 
        [InvoiceNumber], 
        c.CustomerName as SupplierName, 
        [CurrencyName], 
        SET_DocumentType.DocumentType, 
        --ISNULL([GrossAmount], 0) - ISNULL([TaxAmount], 0) - ISNULL([DiscountAmountTaken], 0) AS NetAmount, 
        AmountPaid AS NetAmount,
        SET_Term.Description As [PaymentTerms] 
        FROM SAL_InvoiceHeader 
        Left Join AP_PaymentHeader ON (AP_PaymentHeader.VendorID = SAL_InvoiceHeader.CustomerID) AND AP_PaymentHeader.companyID = SAL_InvoiceHeader.CompanyID AND AP_PaymentHeader.PeriodID = SAL_InvoiceHeader.PeriodID AND AP_PaymentHeader.PaymentID is not null AND AP_PaymentHeader.Status = 1
        INNER JOIN [SET_Currency] ON SAL_InvoiceHeader.InvoiceCurrencyID = SET_Currency.CurrencyID 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType 
        LEFT JOIN [SET_Term] ON SAL_InvoiceHeader.TermsID = SET_Term.TermID 
        LEFT JOIN [AR_SET_Customer] C on SAL_InvoiceHeader.CustomerID = C.CustomerID --and SAL_InvoiceHeader.CompanyID = C.CompanyID 
        and SAL_InvoiceHeader.PeriodID = C.PeriodID 
        inner join Set_PeriodCustom on SAL_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
        and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
        WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID 
            and  (C.CustomerId = @VendorFilter or @VendorFilter is null)
            and  SAL_InvoiceHeader.Status = 1 ";

        const string Query_Get_TranHeaderID = @" SELECT distinct GL_TransactionHeader.TransactionID FROM SAL_InvoiceHeader INNER JOIN GL_TransactionHeader ON GL_TransactionHeader.DocumentTypeID = SAL_InvoiceHeader.InvoiceType AND GL_TransactionHeader.DocumentNo = SAL_InvoiceHeader.InvoiceNumber AND GL_TransactionHeader.CompanyID = SAL_InvoiceHeader.CompanyID AND GL_TransactionHeader.PeriodID = SAL_InvoiceHeader.PeriodID AND GL_TransactionHeader.Status = 1 WHERE SAL_InvoiceHeader.InvoiceID = @InvoiceID AND SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.Status = 1 ";

        

        const string Query_SelectAll_Searching = @" 
SELECT DISTINCT SAL_InvoiceHeader.CompanyID, SAL_InvoiceHeader.PeriodID, SAL_InvoiceHeader.InvoiceID, SAL_InvoiceHeader.AcctsPayCodeCombinationID, 
    SAL_InvoiceHeader.AmountApplicableToDiscount, SAL_InvoiceHeader.AmountPaid, SAL_InvoiceHeader.ApprovalDescription, SAL_InvoiceHeader.ApprovalStatus, 
    SAL_InvoiceHeader.ApprovedAmount, SAL_InvoiceHeader.AuthorizedBy, SAL_InvoiceHeader.AutoTaxCalculationFlag, SAL_InvoiceHeader.AutoWithholdingTaxFlag, 
    SAL_InvoiceHeader.AutoWithholdingTaxGroupID, SAL_InvoiceHeader.BatchID, SAL_InvoiceHeader.CancelledAmount, SAL_InvoiceHeader.CancelledBy, 
    SAL_InvoiceHeader.CancelledDate, SAL_InvoiceHeader.Description, SAL_InvoiceHeader.DiscountAmountTaken, SAL_InvoiceHeader.DocumentSequenceID, 
    SAL_InvoiceHeader.EarliestSettlementDate, SAL_InvoiceHeader.ExchangeDate, SAL_InvoiceHeader.ExchangeRate, SAL_InvoiceHeader.ExchangeRateType, 
    SAL_InvoiceHeader.ExclusivePaymentFlag, SAL_InvoiceHeader.FreightAmount, SAL_InvoiceHeader.GLPostingDate, SAL_InvoiceHeader.GoodsReceivedDate, 
    SAL_InvoiceHeader.GrossAmount, SAL_InvoiceHeader.InvoiceCurrencyID, SAL_InvoiceHeader.InvoiceDate, SAL_InvoiceHeader.InvoiceNumber, 
    SAL_InvoiceHeader.InvoiceReceivedDate, SAL_InvoiceHeader.InvoiceType, SAL_InvoiceHeader.PayCurrencyInvoiceAmount, SAL_InvoiceHeader.PaymentGroupType, 
    SAL_InvoiceHeader.PaymentCrossRate, SAL_InvoiceHeader.PaymentCrossRateDate, SAL_InvoiceHeader.PaymentCrossRateType, SAL_InvoiceHeader.PaymentCurrencyID, 
    SAL_InvoiceHeader.PaymentMethodType, SAL_InvoiceHeader.PaymentStatusType, SAL_InvoiceHeader.PostingStatus, SAL_InvoiceHeader.RecurringPaymentID, 
    SAL_InvoiceHeader.TaxAmount, SAL_InvoiceHeader.TermsDate, SAL_InvoiceHeader.TermsID, SAL_InvoiceHeader.VATCode, SAL_InvoiceHeader.CustomerID, 
    SAL_InvoiceHeader.VendorSiteID, SAL_InvoiceHeader.VoucherNumber, SAL_InvoiceHeader.Status, SAL_InvoiceHeader.CreatedBy, SAL_InvoiceHeader.CreateDate, 
    SAL_InvoiceHeader.CreatedByValue, SAL_InvoiceHeader.UpdatedBy, SAL_InvoiceHeader.UpdateDate, SAL_InvoiceHeader.UpdatedByValue, SAL_InvoiceHeader.StatusCode 
,SAL_DispatchHeader.DispatchID, C.CustomerName
FROM SAL_InvoiceHeader 
    INNER JOIN AR_SET_Customer C ON SAL_InvoiceHeader.CustomerID = C.CustomerID --AND SAL_InvoiceHeader.CompanyID = C.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = C.PeriodID AND C.Status = 1 
    INNER JOIN AR_SET_BusinessPartnerTypes BP ON BP.BusinessPartnerID = C.CustomerID --AND BP.CompanyID = C.CompanyID 
left join SAL_DispatchHeader on SAL_DispatchHeader.DispatchID = SAL_InvoiceHeader.InvoiceNumber
AND BP.PeriodID = C.PeriodID AND C.Status = 1 ";

        const string Query_CheckQuantity = @" 
SELECT INV_StockTransactionDetail.StockTransactionID, INV_StockTransactionDetail.TransactionDetailID, INV_StockTransactionDetail.TransactionQuantity AS STranQuantity, 
    Sum(SAL_InvoiceDetail.QuantityInvoiced) AS InvQuantity, SAL_DispatchHeader.DispatchNo, SAL_DispatchHeader.IsDirectDispatch 
FROM SAL_InvoiceDetail 
	INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
AND INV_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
		AND [INV_StockTransactionDetail].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] 
		AND [INV_StockTransactionDetail].[PeriodID] = [SAL_InvoiceDetail].[PeriodID] AND [INV_StockTransactionDetail].[Status] =
1 
INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = [INV_StockTransactionDetail].[StockTransactionID] AND [INV_StockTransactionHeader].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1

INNER JOIN [SAL_DispatchHeader] ON [SAL_DispatchHeader].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] AND [SAL_DispatchHeader].[DispatchNo] = [INV_StockTransactionHeader].[DocumentNo] AND [SAL_DispatchHeader].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [SAL_DispatchHeader].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [SAL_DispatchHeader].[Status] = 1
 


WHERE [SAL_InvoiceDetail].[StockHeaderID] = @StockHeaderID AND [SAL_InvoiceDetail].[CompanyID] = @CompanyID AND [SAL_InvoiceDetail].[PeriodID] = @PeriodID 
	AND [SAL_InvoiceDetail].[Status] = 1
Group By [INV_StockTransactionDetail].[StockTransactionID], [INV_StockTransactionDetail].[TransactionDetailID], [INV_StockTransactionDetail].[TransactionQuantity],[SAL_DispatchHeader].[DispatchNo],[SAL_DispatchHeader].[IsDirectDispatch]
Having Sum([SAL_InvoiceDetail].[QuantityInvoiced]) >= [INV_StockTransactionDetail].[TransactionQuantity] ";

        const string Query_Update_Flag_Detail = @" UPDATE [SAL_InvoiceDetail] SET [IsPaymentGenerated] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE  [InvoiceHeaderID] = @InvoiceHeaderID AND [InvoiceDetailID] = @InvoiceDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

        const string Query_Get_SHID = @" SELECT [StockHeaderID] FROM [SAL_InvoiceDetail] WHERE [InvoiceHeaderID] = @InvoiceHeaderID AND [InvoiceDetailID] = @InvoiceDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";

        const string Query_Get_DispatchHeaderID = @" SELECT Top 1 IsNull(Sal_DispatchHeader.DispatchID, 0) As DispatchID FROM SAL_InvoiceDetail 
	Inner Join Inv_StockTransactionDetail On Inv_StockTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID 
		And Inv_StockTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID And Inv_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
		And Inv_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
	Inner Join Inv_StockTransactionHeader On Inv_StockTransactionHeader.CompanyID = Inv_StockTransactionDetail.CompanyID 
		And Inv_StockTransactionHeader.PeriodID = Inv_StockTransactionDetail.PeriodID 
		And Inv_StockTransactionHeader.StockTransactionID = Inv_StockTransactionDetail.StockTransactionID 
	Inner Join Sal_DispatchHeader On Sal_DispatchHeader.CompanyID = Inv_StockTransactionHeader.CompanyID 
		And Sal_DispatchHeader.PeriodID = Inv_StockTransactionHeader.PeriodID And Sal_DispatchHeader.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
		And Sal_DispatchHeader.DispatchNo = Inv_StockTransactionHeader.DocumentNo 
WHERE SAL_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID AND SAL_InvoiceDetail.CompanyID = @CompanyID AND SAL_InvoiceDetail.PeriodID = @PeriodID 
	AND SAL_InvoiceDetail.Status = 1  ";

        const string Query_CheckIsDetailCompleted = @" SELECT COUNT([SAL_InvoiceDetail].[IsPaymentGenerated]) AS IsPaymentGeneratedCount FROM [SAL_InvoiceDetail] WHERE [SAL_InvoiceDetail].[InvoiceHeaderID] = @InvoiceHeaderID AND [SAL_InvoiceDetail].[CompanyID] = @CompanyID AND [SAL_InvoiceDetail].[PeriodID] = @PeriodID AND [SAL_InvoiceDetail].[Status] = 1 AND [SAL_InvoiceDetail].[IsPaymentGenerated] = @IsGenerated ";

        const string Query_Update_Flag_Header = @" UPDATE [SAL_InvoiceHeader] SET [IsPaymentGenerated] = @IsGenerated, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [InvoiceID] = @InvoiceID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";

        const string Query_Sales_Invoice_Report = @"  
  
";


        const string Query_Select_SalesTaxLiability = @" 
SELECT SAL_InvoiceDetail.InvoiceHeaderID, AR_SET_Customer.STRNNumber, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceNumber,
	SAL_InvoiceHeader.InvoiceDate, ISNULL(SUM(SAL_InvoiceDetail.TaxAmount),0) AS TaxAmount, '' AS CumulativeTaxAmount
FROM SAL_InvoiceHeader
INNER JOIN SAL_InvoiceDetail ON SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
	AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID
	AND SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID
	AND SAL_InvoiceDetail.Status = 1 
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID
	--AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID
	AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID
	AND AR_SET_Customer.Status = 1
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID
	AND SAL_InvoiceHeader.PeriodID = @PeriodID
	AND SAL_InvoiceHeader.Status = 1	
    AND SAL_InvoiceHeader.StatusCode = 1
    AND SAL_InvoiceHeader.SalesTaxDate IS NULL
	AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate
GROUP BY SAL_InvoiceDetail.InvoiceHeaderID, AR_SET_Customer.STRNNumber, AR_SET_Customer.CustomerName, SAL_InvoiceHeader.InvoiceNumber,
	SAL_InvoiceHeader.InvoiceDate
HAVING ISNULL(SUM(SAL_InvoiceDetail.TaxAmount),0) > 0 
ORDER BY AR_SET_Customer.STRNNumber DESC, SAL_InvoiceHeader.InvoiceDate ";

        const string Query_Update_By_PrimaryKey_SalesTaxDate = @" UPDATE [SAL_InvoiceHeader] SET [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [SalesTaxDate] = @SalesTaxDate
		WHERE [InvoiceID] = @InvoiceID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID ";

        const string Query_Sales_Tax_List_With_CustomerName_Report = @"SELECT
SET_Period.PeriodSetName AS Season,
SAL_InvoiceHeader.InvoiceNumber,
SAL_InvoiceHeader.InvoiceDate,
SAL_OrderHeader.ContractNo AS LSNo,
AR_Set_Customer.CustomerName,
SAL_InvoiceDetail.QuantityInvoiced AS QtyBags,
ISNULL(SAL_InvoiceDetail.GrossAmount,0) AS ValueExcSalesTax,
ISNULL(SAL_InvoiceDetail.ExciseDutyAmount,0) AS ExciseDuty,
ISNULL(SAL_InvoiceDetail.FEDAmount,0) AS FExsDuty,
ISNULL(SAL_InvoiceDetail.TaxAmount,0) AS STaxPayable,
0 AS ValueIncSalesTax

From SAL_InvoiceDetail 
  Inner Join SAL_InvoiceHeader On (SAL_InvoiceHeader.InvoiceID = SAL_InvoiceDetail.InvoiceHeaderID)
  And SAL_InvoiceHeader.CompanyID = SAL_InvoiceDetail.CompanyID
  And SAL_InvoiceHeader.PeriodID = SAL_InvoiceDetail.PeriodID
  And SAL_InvoiceHeader.Status = 1
  
  Inner Join AR_Set_Customer On (AR_Set_Customer.CustomerID = SAL_InvoiceHeader.CustomerID)
  --AND AR_Set_Customer.CompanyID = SAL_InvoiceHeader.CompanyID
  AND AR_Set_Customer.PeriodID = SAL_InvoiceHeader.PeriodID
  AND AR_Set_Customer.Status = 1
  
  INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.TransactionDetailID = SAL_InvoiceDetail.StockDetailID 
  AND INV_StockTransactionDetail.StockTransactionID = SAL_InvoiceDetail.StockHeaderID 
  AND INV_StockTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID 
  AND INV_StockTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID 
  AND INV_StockTransactionDetail.Status = 1
  
  INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
  AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
  AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
  AND INV_StockTransactionHeader.Status = 1
  
  INNER JOIN SAL_DispatchHeader ON SAL_DispatchHeader.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
  AND SAL_DispatchHeader.DispatchNo = INV_StockTransactionHeader.DocumentNo 
  AND SAL_DispatchHeader.CompanyID = INV_StockTransactionHeader.CompanyID 
  AND SAL_DispatchHeader.PeriodID = INV_StockTransactionHeader.PeriodID 
  AND SAL_DispatchHeader.Status = 1
 
  INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DODetailID = INV_StockTransactionDetail.SourceLineID
  AND SAL_DeliveryOrderDetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
  AND SAL_DeliveryOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
  AND SAL_DeliveryOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
  AND SAL_DeliveryOrderDetail.Status = 1
  
  INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
  AND SAL_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
  AND SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
  AND SAL_DeliveryOrderHeader.Status = 1
  
  INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
  AND SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
  AND SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID
  AND SAL_OrderHeader.Status = 1 

  INNER JOIN SET_Period ON SET_Period.PeriodID = SAL_OrderHeader.PeriodID
{0}
";
        const string Query_GetCustomerDirectInvocie = @"
select 
ReceiptInvoicesID,ReceiptHeaderID,InvoiceHeaderID,InvoiceNo,InvoiceDate,sum(InvoiceAmount) as InvoiceAmount,sum(InvoiceAmount) as ActualAmount,
sum(InvoiceAmount) - 
 isnull((
	select (CASE when sum(AR_ReceiptInvoicesDetail.Amount) is null then sum(AR_ReceiptTransactionHeader.Amount) else
	sum(AR_ReceiptInvoicesDetail.Amount) end) from AR_ReceiptTransactionHeader
	left join AR_ReceiptInvoicesDetail on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
	where AR_ReceiptInvoicesDetail.InvoiceHeaderID = TAB.InvoiceHeaderID
    and AR_ReceiptTransactionHeader.ReversalDate is null and ReversalComments is null
    ),0 )as BalanceAmount
, sum(InvoiceAmount) - 
 isnull((
	select (CASE when sum(AR_ReceiptInvoicesDetail.Amount) is null then sum(AR_ReceiptTransactionHeader.Amount) else
	sum(AR_ReceiptInvoicesDetail.Amount) end) from AR_ReceiptTransactionHeader
	left join AR_ReceiptInvoicesDetail on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
	where AR_ReceiptInvoicesDetail.InvoiceHeaderID = TAB.InvoiceHeaderID
    and AR_ReceiptTransactionHeader.ReversalDate is null and ReversalComments is null
    ),0 )as Amount,Action
,
    sum(InvoiceAmount) - 
 isnull((
	select (CASE when sum(AR_ReceiptInvoicesDetail.Amount) is null then sum(AR_ReceiptTransactionHeader.Amount) else
	sum(AR_ReceiptInvoicesDetail.Amount) end) from AR_ReceiptTransactionHeader
	left join AR_ReceiptInvoicesDetail on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
	where AR_ReceiptInvoicesDetail.InvoiceHeaderID = TAB.InvoiceHeaderID
    and AR_ReceiptTransactionHeader.ReversalDate is null and ReversalComments is null
    ),0 ) as ActualBalance

 from (
select distinct Sal_InvoiceCustomerDetail.InvoiceCustomerID,
	-1 as ReceiptInvoicesID, -1 as ReceiptHeaderID, InvoiceID as InvoiceHeaderID, InvoiceNumber as InvoiceNo,
	InvoiceDate,Sal_InvoiceCustomerDetail.Amount as InvoiceAmount, 
	'' as Action

from SAL_InvoiceHeader
inner join Sal_InvoiceCustomerDetail on Sal_InvoiceCustomerDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
		and Sal_InvoiceCustomerDetail.CompanyID = SAL_InvoiceHeader.CompanyID
		and Sal_InvoiceCustomerDetail.PeriodID = SAL_InvoiceHeader.PeriodID
	where SAL_InvoiceHeader.CompanyID = @CompanyID
	and SAL_InvoiceHeader.PeriodID = @PeriodID
	and SAL_InvoiceHeader.InvoiceID =  @InvoiceID
	and Sal_InvoiceCustomerDetail.TransactionType = 'Debit'
) as TAB
group by ReceiptInvoicesID,ReceiptHeaderID,InvoiceHeaderID,InvoiceNo,Action,InvoiceDate
";

        const string Query_GetAdvanceAgainst_SO_Amount = @"

Declare @StockTransactionID int = 0 , @SalesOrderHeaderID int = 0, @DispatchNo int = 0;

select  @DispatchNo = INV_StockTransactionHeader.DocumentNo from 
INV_StockTransactionHeader,
INV_StockTransactionDetail,
SAL_InvoiceDetail,
SAL_InvoiceHeader

WHERE 

INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and INV_StockTransactionHeader.StockTransactionID = SAL_InvoiceDetail.StockHeaderID
and SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
and SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID
and SAL_InvoiceDetail.PeriodID = SAL_InvoiceHeader.PeriodID
and SAL_InvoiceHeader.InvoiceID ={0} and INV_StockTransactionHeader.BusinessPartnerID = @CustomerID 

select @StockTransactionID = INV_StockTransactionHeader.StockTransactionID from 
INV_StockTransactionHeader, Sal_DispatchHeader
where 
INV_StockTransactionHeader.DocumentTypeID = Sal_DispatchHeader.DocumentTypeID 
And INV_StockTransactionHeader.DocumentNo = Sal_DispatchHeader.DispatchNo 
and Sal_DispatchHeader.DispatchNo =@DispatchNo
and INV_StockTransactionHeader.BusinessPartnerID =  @CustomerID 
select  @SalesOrderHeaderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID from 



INV_StockTransactionDetail,
SAL_DeliveryOrderDetail,
SAL_DeliveryOrderHeader,
INV_StockTransactionHeader

where 

INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
And INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
And INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
And INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
And INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
And INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
And INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.PeriodID 
And INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
and INV_StockTransactionHeader.StockTransactionID = @StockTransactionID
And INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID

-- jawad Query 
--select sum(Amount)as amount from AR_ReceiptTransactionHeader,SAL_OrderHeader
--where
--AR_ReceiptTransactionHeader.CompanyID = SAL_OrderHeader.CompanyID 
--And AR_ReceiptTransactionHeader.PeriodID = SAL_OrderHeader.PeriodID 
--And AR_ReceiptTransactionHeader.SourceHeaderID = SAL_OrderHeader.OrderID
--and SAL_OrderHeader.OrderID = @SalesOrderHeaderID
--Imtiaz
--AND AR_ReceiptTransactionHeader.CustomerID=@CustomerID
--and AR_ReceiptTransactionHeader.StatusCode = 1
--group by Amount having sum(amount)>0

-- Sajid Query change by (Zeeshan)
select isnull(sum(Amount),0) as amount  from AR_ReceiptTransactionHeader,Sal_invoiceHeader 
where
AR_ReceiptTransactionHeader.CompanyID = Sal_invoiceHeader.CompanyID 
And AR_ReceiptTransactionHeader.PeriodID = Sal_invoiceHeader.PeriodID 
And AR_ReceiptTransactionHeader.SourceHeaderID = Sal_invoiceHeader.InvoiceID 
and Sal_invoiceHeader.InvoiceID = @InvoiceID
AND AR_ReceiptTransactionHeader.CustomerID= @CustomerID
and AR_ReceiptTransactionHeader.StatusCode = 1

";

        const string Query_GetRecoveryInvoice = @" SELECT * FROM SAL_InvoiceHeader 
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID and SAL_InvoiceHeader.PeriodID = @PeriodID and SAL_InvoiceHeader.LoanHeaderID = @RecoveryID; 

SELECT * FROM GL_TransactionHeader 
    INNER JOIN GL_TransactionDetail ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
        AND GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
WHERE GL_TransactionHeader.CompanyID = @CompanyID AND GL_TransactionHeader.PeriodID = @PeriodID AND GL_TransactionHeader.Status = 1 
    AND GL_TransactionHeader.StatusCode = 1 AND GL_TransactionDetail.SourceHeaderID = @RecoveryID AND GL_TransactionDetail.Status = 1 
    AND GL_TransactionHeader.DocumentTypeID IN 
    (
        SELECT Top 1 DocumentTypeID FROM Set_DocumentType 
        WHERE DocumentType = 'Manual Loan Recovery' And CompanyID = @CompanyID And PeriodID = @PeriodID And IsActive = 1 And Status = 1
    ) ";

        const string Query_Delete_Permanent_By_PrimaryKey = @" DELETE FROM Sal_InvoiceHeader 
WHERE CompanyID = @CompanyID and InvoiceID = @InvoiceID and PeriodID = @PeriodID ";

        const string Query_Get_AllDocumentNos = @" SELECT InvoiceNumber As DocumentNo FROM SAL_InvoiceHeader
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND InvoiceType = @DocumentTypeID AND Status = 1 AND IsNull(IsReversed, 0) = 0 AND ReversalID IS NULL and FiscalID = @FiscalID
ORDER BY Convert(BigInt, InvoiceNumber) ";
        const string Query_ExcessDispatchStockReport = @"Select ROW_NUMBER() 
   OVER (ORDER BY fintab.InvoiceID) AS SrNo, fintab.* From 
   (
SELECT
DISTINCT
SAL_InvoiceHeader.InvoiceID,
SAL_InvoiceHeader.CustomerID,
SAL_InvoiceHeader.InvoiceNumber as InvoiceNo,
ISNULL(SAL_InvoiceDetail.QuantityInvoiced,0) as InvoiceQty,
AR_SET_Customer.CustomerName,
SAL_DispatchHeader.DispatchNo,
INV_StockTransactionHeader.StockTransactionID
,INV_StockTransactionDetail.TransactionDetailID
,SAL_InvoiceDetail.InvoiceDetailID
,ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)) as DispatchQty
,ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)) - ISNULL(SAL_InvoiceDetail.QuantityInvoiced,0) as Total

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
 AND (Cast(Convert(Varchar(25), SAL_InvoiceHeader.CreateDate , 110) as date) between @DateFrom and @DateTo)
) as fintab
Where fintab.Total !=0";

        const string Query_PerformerInvoiceReport = @"SELECT 
    SAL_OrderHeader.CompanyID
   ,SAL_OrderHeader.SaleContractTypeID
   ,SET_DocumentType.DocumentType
   ,SAL_OrderHeader.Remarks
   ,SET_DocumentType.Description
   ,SAL_OrderHeader.OrderID
   ,SAL_OrderHeader.ContractDate
   , SAL_OrderHeader.ContractNo ,
   SET_Business.BusinessName  +' '+ SET_OperatingUnit.OperatingUnitName as SupplierName,
   SET_Business.AddressLine1 as SupplierAddress,SET_OperatingUnit.NTNNumber as SupplierNTN,SET_Business.PhoneNumber as SupplierPhoneNumber,
   '' as SupplierSTRN
   ,AR_SET_Customer.CustomerName,AR_SET_Customer.AddressLine1 as CustomerAddress
   ,AR_SET_Customer.NTNNumber as CustomerNTN
   ,AR_SET_Customer.PhoneNumber as CustomerPhoneNumber
   ,AR_SET_Customer.STRNNumber as CustomeSTRN
   ,INV_Item.Description as ItemDescription
   ,INV_Item.InventoryItemStatusCode as ItemStatusCode
   ,INV_SET_Category.CategoryName
   ,SAL_OrderDetail.Quantity
   ,SAL_OrderDetail.Rate
   ,isnull(SAL_OrderDetail.SalesTaxAmount,0) AS  SalesTaxAmount
   ,SAL_OrderDetail.GrossAmount
   ,(isnull(SAL_OrderDetail.[GrossAmount],0) * isnull(SAL_OrderDetail.SalesTaxAmount,0)) / 100 as TaxPayable
   ,SAL_OrderDetail.GrossAmount + (isnull(SAL_OrderDetail.[GrossAmount],0) * isnull(SAL_OrderDetail.SalesTaxAmount,0)) / 100 as TotalAmount
,SET_OperatingUnit.Logoimage as LogoImage
,DocumentTypesGroup.GroupName
 ,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         '0'+ convert(varchar(24),Set_DocumentType.GroupID) +'-' +'K'+'SI' +'-'+ substring(convert(varchar(20),year(SAL_OrderHeader.ContractDate)),3,2) + '000' +convert(varchar(24),SAL_OrderHeader.ContractNo)
          when null
          then '0'+ convert(varchar(24),Set_DocumentType.GroupID) +'-' +' '+'SI' +'-'+ substring(convert(varchar(20),year(SAL_OrderHeader.ContractDate)),3,2) + '000' +convert(varchar(24),SAL_OrderHeader.ContractNo)
          else  '0'+ convert(varchar(24),Set_DocumentType.GroupID) +'-' +SUBSTRING(GroupName,1,1)+'SI' +'-'+ substring(convert(varchar(20),year(SAL_OrderHeader.ContractDate)),3,2) + '000' +convert(varchar(24),SAL_OrderHeader.ContractNo)
          end  ) as SINumber

 FROM
 SAL_OrderHeader
 INNER JOIN SAL_OrderDetail On SAL_OrderDetail.OrderID =SAL_OrderHeader.OrderID
 AND SAL_OrderDetail.CompanyID=SAL_OrderHeader.CompanyID
  AND SAL_OrderDetail.PeriodID=SAL_OrderHeader.PeriodID
  
  INNER JOIN SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_OrderHeader.SaleContractTypeID
  AND SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID
  AND SET_DocumentType.PeriodID = SAL_OrderHeader.PeriodID
  
   LEFT JOIN INV_Item ON INV_Item.ItemID=SAL_OrderDetail.ItemID 
    --AND INV_Item.CompanyID =SAL_OrderDetail.CompanyID 
    
    left JOIN INV_SET_Category On INV_SET_Category.CategoryID =INV_Item.ItemTypeID
    --AND INV_SET_Category.CompanyID =INV_Item.CompanyID
    
 left JOIN AR_SET_Customer  ON AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
  --AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID
   AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID
    
    
 INNER JOIN SET_Company ON SET_Company.CompanyId=SAL_OrderHeader.CompanyID
 AND SET_Company.Status=1
 
 INNER JOIN SET_OperatingUnit On SET_OperatingUnit.OperatingUnitId=SET_Company.OperatingUnitId 
 AND SET_OperatingUnit.Status=1
 
 INNER JOIN SET_Business On SET_Business.BusinessId=SET_Company.BusinessId 
 and SET_Business.IsActive=1
 LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
 
 WHERE [SAL_OrderHeader].[Status] = 1 AND ([SAL_OrderHeader].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([SAL_OrderHeader].[PeriodID] = @PeriodID OR @PeriodID is null) ";

        const string Query_DetailAdvanceInvoice = @"SELECT 
    SAL_OrderHeader.CompanyID
   ,SAL_OrderHeader.SaleContractTypeID
   ,SAL_OrderHeader.Remarks
   ,SET_DocumentType.Description
   ,SAL_OrderHeader.OrderID
   ,SAL_OrderHeader.ContractDate
   , SAL_OrderHeader.ContractNo ,
   SET_Business.BusinessName  +' '+ SET_OperatingUnit.OperatingUnitName as SupplierName,
   SET_Business.AddressLine1 as SupplierAddress,SET_OperatingUnit.NTNNumber as SupplierNTN,SET_Business.PhoneNumber as SupplierPhoneNumber,
   '' as SupplierSTRN
   ,AR_SET_Customer.CustomerName,AR_SET_Customer.AddressLine1 as CustomerAddress
   ,AR_SET_Customer.NTNNumber as CustomerNTN
   ,AR_SET_Customer.PhoneNumber as CustomerPhoneNumber
   ,AR_SET_Customer.STRNNumber as CustomeSTRN
   ,INV_Item.Description as ItemDescription
   ,INV_Item.InventoryItemStatusCode as ItemStatusCode
   ,INV_SET_Category.CategoryName
   ,SAL_OrderDetail.Quantity
   ,SAL_OrderDetail.Rate
   ,isnull(SAL_OrderDetail.SalesTaxAmount,0) AS  SalesTaxAmount
   ,SAL_OrderDetail.GrossAmount
   ,(isnull(SAL_OrderDetail.[GrossAmount],0) * isnull(SAL_OrderDetail.SalesTaxAmount,0)) / 100 as TaxPayable
   ,SAL_OrderDetail.GrossAmount + (isnull(SAL_OrderDetail.[GrossAmount],0) * isnull(SAL_OrderDetail.SalesTaxAmount,0)) / 100 as TotalAmount
,SET_OperatingUnit.Logoimage as LogoImage

 FROM
 SAL_OrderHeader
 INNER JOIN SAL_OrderDetail On SAL_OrderDetail.OrderID =SAL_OrderHeader.OrderID
 AND SAL_OrderDetail.CompanyID=SAL_OrderHeader.CompanyID
  AND SAL_OrderDetail.PeriodID=SAL_OrderHeader.PeriodID
  
  INNER JOIN SET_DocumentType on SET_DocumentType.DocumentTypeID=SAL_OrderHeader.SaleContractTypeID
  AND SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID
  AND SET_DocumentType.PeriodID = SAL_OrderHeader.PeriodID
 AND  SET_DocumentType.Description is not null
  
   LEFT JOIN INV_Item ON INV_Item.ItemID=SAL_OrderDetail.ItemID 
    --AND INV_Item.CompanyID =SAL_OrderDetail.CompanyID 
    
    left JOIN INV_SET_Category On INV_SET_Category.CategoryID =INV_Item.ItemTypeID
    --AND INV_SET_Category.CompanyID =INV_Item.CompanyID
    
 left JOIN AR_SET_Customer  ON AR_SET_Customer.CustomerID = SAL_OrderHeader.CustomerID 
  --AND AR_SET_Customer.CompanyID = SAL_OrderHeader.CompanyID
   AND AR_SET_Customer.PeriodID = SAL_OrderHeader.PeriodID
    
    
 INNER JOIN SET_Company ON SET_Company.CompanyId=SAL_OrderHeader.CompanyID
 AND SET_Company.Status=1
 
 INNER JOIN SET_OperatingUnit On SET_OperatingUnit.OperatingUnitId=SET_Company.OperatingUnitId 
 AND SET_OperatingUnit.Status=1
 
 INNER JOIN SET_Business On SET_Business.BusinessId=SET_Company.BusinessId 
 and SET_Business.IsActive=1
 
 WHERE [SAL_OrderHeader].[Status] = 1 AND ([SAL_OrderHeader].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([SAL_OrderHeader].[PeriodID] = @PeriodID OR @PeriodID is null)";

        const string Query_BatchWiseExpiry = @"Select dbo.Set_Batch.BatchId , dbo.Set_Batch.Description as Batch ,INV_Item.ItemID,INV_Item.Description
,INV_StockTransactionDetail.ExpiryDate,INV_StockTransactionDetail.TransactionQuantity AS OUTQTY 
,INV_StockTransactionDetail.PrimaryQuantity AS INQTY,
Isnull((INV_StockTransactionDetail.PrimaryQuantity - INV_StockTransactionDetail.TransactionQuantity),0 ) AS Balance

From INV_StockTransactionDetail
INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID

INNER JOIN dbo.Set_Batch ON dbo.Set_Batch.BatchId = INV_StockTransactionDetail.BatchId 

       ";

        const string Query_CreditMemo = @"SELECT  SAL_InvoiceHeader.InvoiceNumber,SAL_InvoiceHeader.InvoiceDate,SAL_InvoiceHeader.VatCode,
SET_Term.Description AS TermsDay,Sal_Invoice_AccountsDetail.DocumentReference,Sal_Invoice_AccountsDetail.Narration,
SUM(Sal_Invoice_AccountsDetail.Amount) Amount,Sal_Invoice_AccountsDetail.TransactionType,SET_DocumentType.DocumentType,
AR_SET_Customer.CustomerName,AR_SET_Customer.AddressLine1,SET_DocumentType.Description
,SET_Business.BusinessName,SET_Business.AddressLine1 AS Address
,case when GL_CodeCombinationDetail.Description like '%tax%' then SUM(Sal_Invoice_AccountsDetail.Amount) else 0 end as TaxAmount
FROM SAL_InvoiceHeader

INNER JOIN Sal_Invoice_AccountsDetail ON Sal_Invoice_AccountsDetail .InvoiceHeaderID=SAL_InvoiceHeader.InvoiceID
AND Sal_Invoice_AccountsDetail.CompanyID=SAL_InvoiceHeader.CompanyID
AND Sal_Invoice_AccountsDetail.PeriodID=SAL_InvoiceHeader.PeriodID


INNER JOIN Sal_InvoiceCustomerDetail ON Sal_InvoiceCustomerDetail .InvoiceHeaderID=SAL_InvoiceHeader.InvoiceID
AND Sal_InvoiceCustomerDetail.CompanyID=SAL_InvoiceHeader.CompanyID
AND Sal_InvoiceCustomerDetail.PeriodID=SAL_InvoiceHeader.PeriodID

INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = Sal_InvoiceCustomerDetail.CustomerID 
--AND AR_SET_Customer.CompanyID = Sal_InvoiceCustomerDetail.CompanyID 
AND AR_SET_Customer.PeriodID = Sal_InvoiceCustomerDetail.PeriodID 

INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID=SAL_InvoiceHeader.InvoiceType
AND SET_DocumentType.CompanyID=SAL_InvoiceHeader.CompanyID
AND SET_DocumentType.PeriodID=SAL_InvoiceHeader.PeriodID

Left JOIN SET_Term ON SET_Term.TermID = SAL_InvoiceHeader.TermsID
AND SET_Term.IsActive=1

Inner Join [SET_Company] ON [SET_Company].[CompanyId] = SAL_InvoiceHeader.[CompanyID]
Inner Join [SET_OperatingUnit] ON [SET_OperatingUnit].[OperatingUnitId] = [SET_Company].[OperatingUnitId]
left join SET_Business on SET_Business.BusinessId = SET_OperatingUnit.BusinessId


inner join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = Sal_Invoice_AccountsDetail.CodeCombinationID


 WHERE ([SAL_InvoiceHeader].[CompanyID] = @CompanyID OR @CompanyID is null)
AND ([SAL_InvoiceHeader].[PeriodID] = @PeriodID OR @PeriodID is null)";

        const string Query_GetLocation_byinvoice = @"Select INV_StockTransactionHeader.LocationID,INV_StockTransactionHeader.StockTransactionID ,SAL_InvoiceHeader.InvoiceID,SAL_InvoiceHeader.InvoiceNumber,
SAL_InvoiceDetail.InvoiceHeaderID, SAL_InvoiceDetail.StockHeaderID
 from SAL_InvoiceHeader
	Inner Join  SAL_InvoiceDetail on SAL_InvoiceDetail.InvoiceHeaderID= SAL_InvoiceHeader.InvoiceID
	And SAL_InvoiceDetail.CompanyID= SAL_InvoiceHeader.CompanyID
	And SAL_InvoiceDetail.PeriodID= SAL_InvoiceHeader.PeriodID
	AND SAL_InvoiceHeader.Status=1
		inner Join  INV_StockTransactionHeader  on INV_StockTransactionHeader.StockTransactionID =SAL_InvoiceDetail.StockHeaderID
		AND   INV_StockTransactionHeader.CompanyID = SAL_InvoiceDetail.CompanyID
		AND   INV_StockTransactionHeader.PeriodID = SAL_InvoiceDetail.PeriodID
		AND		SAL_InvoiceDetail.Status=1
where SAL_InvoiceHeader.InvoiceID= @InvoiceID
AND SAL_InvoiceHeader.CompanyID=@CompanyID
AND SAL_InvoiceHeader.PeriodID= @PeriodID";
       
        #endregion

        #region Class Methods

        public static DataTable Get_OutStandingInstallment(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> CustomerID = null)
        {
            //List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@DateFrom", ((DateTime)dateFrom).ToString("yyyy-MM-dd"));
                sqlparams[4] = new SqlParameter("@DateTo", ((DateTime)dateTo).ToString("yyyy-MM-dd"));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_OutStandingInstallment), sqlparams);

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

        public static DataTable GetAmountOnDirectReciept(int Recieptid, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@RID", Recieptid);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAmountOnDirectReciept, sqlparams);

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

        public static DataTable GET_Report_VehicleSalesInvoice(int companyID, int periodID, int SONumber)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SONumber", SONumber);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Report_VehicleSalesInvoice, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
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
                        , Query_SelectAll_Searching_WithPaging + strWhere
                       ) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparams).Tables[0];

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

        public static int GetTranHeaderIDByKey(int companyID, int periodID, int invoiceID)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_TranHeaderID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool InsertSInvHeaderAndDetail(List<SAL_InvoiceDetail> colSInvDetail, List<Sal_Invoice_AccountsDetail> lst_Accounts,
            List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomers, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da,
            bool IsCreditNoteForLubricant = false, int CurrentFiscalID = 0,bool IsQuantityCheckBeforeInsert = false)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertInvoice");
            decimal RemaningQuantity = 100;
            try
            {
                this.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(f_InvoiceType, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                short invoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(CompanyID, InvoiceID, PeriodID, sqltrans));

                foreach (SAL_InvoiceDetail SId in colSInvDetail)
                {
                    //Check Dispatch Quantity before Insert
                    if (IsQuantityCheckBeforeInsert)
                        RemaningQuantity = Get_DispatchRemaningQuantity(CompanyID, PeriodID, SId.StockHeaderID, SId.StockDetailID, sqltrans);
                    else
                        RemaningQuantity = 100;// Default 100 not check for other 




                    if (RemaningQuantity > 0)
                    {
                        SId.InvoiceHeaderID = this.InvoiceID;
                        SId.InvoiceDetailID = invoiceDetailID++;
                        SId.FiscalID = CurrentFiscalID;
                        SId.Insert(sqltrans);

                        if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                            || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                        {
                            INV_StockTransactionDetail stdUpdate = new INV_StockTransactionDetail(long.Parse(SId.StockDetailID.ToString()), null, sqltrans);
                            decimal ReversedQuantity = decimal.Parse(stdUpdate.ReversedQuantity == null ? "0" : stdUpdate.ReversedQuantity.ToString());

                            if (IsCreditNoteForLubricant)
                                stdUpdate.ReversedQuantity = ReversedQuantity + SId.QuantityInvoiced;

                            stdUpdate.UpdateDataByKey(sqltrans);
                        }

                        #region Checking For Dispatch Complete Status

                        CheckQuantity(SId.StockHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Invoice, f_CreatedBy, f_CreateDate, f_CreatedByValue);

                        #endregion
                    }
                }
                if (RemaningQuantity > 0)
                {
                    #region Insert into Sales Invoice Accounts Detail
                    if (lst_Accounts != null)
                    {
                        foreach (Sal_Invoice_AccountsDetail obj_Account in lst_Accounts)
                        {
                            obj_Account.InvoiceHeaderID = this.InvoiceID;
                            obj_Account.InvoiceDetailAccountID = (short)Sal_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(this.CompanyID, this.InvoiceID, this.PeriodID,
                                sqltrans);

                            obj_Account.FiscalID = CurrentFiscalID;
                            obj_Account.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Insert into Sales Invoice Customers Detail
                    if (lst_InvoiceCustomers != null)
                    {
                        foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomer in lst_InvoiceCustomers)
                        {
                            obj_InvoiceCustomer.InvoiceHeaderID = this.InvoiceID;
                            obj_InvoiceCustomer.InvoiceCustomerID = (short)Sal_InvoiceCustomerDetail.GetNewInvoiceCustomerID(this.CompanyID, this.InvoiceID, this.PeriodID,
                                sqltrans);
                            obj_InvoiceCustomer.FiscalID = CurrentFiscalID;
                            obj_InvoiceCustomer.Insert(sqltrans);
                        }
                    }
                    #endregion

                    #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                    {
                        #region Insert In GL_TransactionHeader

                        th.DocumentNo = int.Parse(f_InvoiceNumber);
                        th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th.FiscalID = CurrentFiscalID;
                        th.Insert(sqltrans);

                        #endregion

                        #region Insert In GL_TransactionDetail

                        foreach (GL_TransactionDetail td in colTranDetail)
                        {

                            td.TransactionID = th.TransactionID;
                            td.SourceHeaderID = this.InvoiceID;
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
                        da.DocumentNo = this.InvoiceNumber;
                        da.FiscalID = CurrentFiscalID;
                        da.Insert(sqltrans);
                    }

                    #endregion
                }

                if (RemaningQuantity > 0)
                    result = true;
                else
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertInvoice");
            }

            return result;
        }

        public bool InsertSInvHeaderAndDetailForServiceOrder(List<SAL_InvoiceDetail> colSInvDetail,GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertInvoice");

            try
            {
                this.InvoiceNumber = SET_DocumentSequence.ManualDocumentSequence(f_InvoiceType, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(f_CompanyID, f_PeriodID, sqltrans);
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                short invoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(CompanyID, InvoiceID, PeriodID, sqltrans));

                foreach (SAL_InvoiceDetail SId in colSInvDetail)
                {
                    SId.InvoiceHeaderID = this.InvoiceID;
                    SId.InvoiceDetailID = invoiceDetailID++;
                    SId.FiscalID = CurrentFiscalID;
                    SId.Insert(sqltrans);
                }

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = int.Parse(f_InvoiceNumber);
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.InvoiceID;
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
                    da.DocumentNo = this.InvoiceNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertInvoice");
            }

            return result;
        }


        public bool UpdateSInvHeaderAndDetail(List<SAL_InvoiceDetail> colAddSInvDetail, List<SAL_InvoiceDetail> colUpdateSInvDetail,
            List<SAL_InvoiceDetail> colDeleteSInvDetail, List<Sal_Invoice_AccountsDetail> lst_AccountsAdd, List<Sal_Invoice_AccountsDetail> lst_AccountsUpdate,
            List<Sal_Invoice_AccountsDetail> lst_AccountsDelete, List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomersAdd,
            List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomersUpdate, List<Sal_InvoiceCustomerDetail> lst_InvoiceCustomersDelete, GL_TransactionHeader th,
            List<GL_TransactionDetail> colAddTranDetail, int ChartOfAccountID, SET_DocumentApprove da,int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateInvoice");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (SAL_InvoiceDetail SIddAdd in colAddSInvDetail)
                {
                    SIddAdd.InvoiceDetailID = Convert.ToInt16(SAL_InvoiceDetail.GetNewInvoiceDetailID(CompanyID, InvoiceID, PeriodID, sqltrans));
                    SIddAdd.FiscalID = CurrentFiscalID;
                    SIddAdd.Insert(sqltrans);

                    #region Checking For Dispatch Complete Status

                    CheckQuantity(SIddAdd.StockHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Invoice, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }

                foreach (SAL_InvoiceDetail SIddUpdate in colUpdateSInvDetail)
                {
                    SIddUpdate.FiscalID = CurrentFiscalID;
                    SIddUpdate.UpdateDataByKey(sqltrans);

                    #region Checking For Dispatch Complete Status

                    CheckQuantity(SIddUpdate.StockHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Invoice, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }

                foreach (SAL_InvoiceDetail SIdDelete in colDeleteSInvDetail)
                    SAL_InvoiceDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, SIdDelete.InvoiceDetailID,
                        InvoiceID, f_PeriodID);

                #region Sales Invoices Accounts Detail
                if (lst_AccountsAdd != null)
                {
                    foreach (Sal_Invoice_AccountsDetail SIddAdd in lst_AccountsAdd)
                    {
                        SIddAdd.InvoiceHeaderID = this.InvoiceID;
                        SIddAdd.CompanyID = f_CompanyID;
                        SIddAdd.PeriodID = f_PeriodID;
                        SIddAdd.FiscalID = CurrentFiscalID;
                        SIddAdd.InvoiceDetailAccountID = (short)Sal_Invoice_AccountsDetail.GetNewInvoiceDetailAccountID(CompanyID, InvoiceID, PeriodID, sqltrans);
                        SIddAdd.Insert(sqltrans);
                    }
                }
                if (lst_AccountsUpdate != null)
                {
                    foreach (Sal_Invoice_AccountsDetail SIddUpdate in lst_AccountsUpdate)
                    {
                        SIddUpdate.FiscalID = CurrentFiscalID;
                        SIddUpdate.UpdateDataByKey(sqltrans);
                    }
                }
                if (lst_AccountsDelete != null)
                {
                    foreach (Sal_Invoice_AccountsDetail SIdDelete in lst_AccountsDelete)
                    {
                        Sal_Invoice_AccountsDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,
                            SIdDelete.InvoiceDetailAccountID, InvoiceID, f_PeriodID);
                    }
                }

                #endregion

                #region Sales Invoices Accounts Detail
                if (lst_InvoiceCustomersAdd != null)
                {
                    foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomerAdd in lst_InvoiceCustomersAdd)
                    {
                        obj_InvoiceCustomerAdd.InvoiceHeaderID = this.InvoiceID;
                        obj_InvoiceCustomerAdd.CompanyID = f_CompanyID;
                        obj_InvoiceCustomerAdd.PeriodID = f_PeriodID;
                        obj_InvoiceCustomerAdd.FiscalID = CurrentFiscalID;
                        obj_InvoiceCustomerAdd.InvoiceCustomerID = (short)Sal_InvoiceCustomerDetail.GetNewInvoiceCustomerID(CompanyID, InvoiceID, PeriodID, sqltrans);
                        obj_InvoiceCustomerAdd.Insert(sqltrans);
                    }
                }
                if (lst_InvoiceCustomersUpdate != null)
                {
                    foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomerUpdate in lst_InvoiceCustomersUpdate)
                    {
                        obj_InvoiceCustomerUpdate.FiscalID = CurrentFiscalID;
                        obj_InvoiceCustomerUpdate.UpdateDataByKey(sqltrans);
                    }
                }
                if (lst_InvoiceCustomersDelete != null)
                {
                    foreach (Sal_InvoiceCustomerDetail obj_InvoiceCustomerDelete in lst_InvoiceCustomersDelete)
                    {
                        Sal_InvoiceCustomerDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,
                            obj_InvoiceCustomerDelete.InvoiceCustomerID, InvoiceID, f_PeriodID);
                    }
                }
                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td in colAddTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
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
                    da.DocumentNo = this.InvoiceNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateInvoice");
            }

            return result;
        }

        public bool DeleteSInvHeaderAndDetail(List<SAL_InvoiceDetail> colDeleteSInvDetail, GL_TransactionHeader th, List<GL_TransactionDetail> colDeleteTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteInvoice");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, InvoiceID, f_PeriodID);

                foreach (SAL_InvoiceDetail SIddDelete in colDeleteSInvDetail)
                    SAL_InvoiceDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, SIddDelete.InvoiceDetailID,
                        InvoiceID, f_PeriodID);

                SAL_InvoiceTaxPayment.DeleteDataByInvoiceID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, f_PeriodID, InvoiceID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteInvoice");
            }
            return result;
        }

        public bool UpdateIHeaderForApprove(GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove dA)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateIHeader");

            try
            {
                this.UpdateDataByKey(sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL_TransactionHeader

                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    foreach (GL_TransactionDetail td in colAddTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Update Status To False, If "InProcess" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.InProcess.ToString()))
                    dA.UpdateDataByKeyStatus(sqltrans);

                #endregion

                #region Update Status To False, If "Discuss" In SET_DocumentApprove

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Discuss.ToString()))
                    dA.UpdateStatusByKeysWithoutDiscuss(sqltrans);

                #endregion

                #region Insert In SET_DocumentApprove

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateIHeader");
            }

            return result;
        }

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_Searching_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int invoiceID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                const string Query_SelectAll_ForApproveDetail = @"
SELECT DISTINCT SAL_InvoiceHeader.InvoiceID
	,SAL_InvoiceHeader.CompanyID
	,SAL_InvoiceHeader.PeriodID
	,SAL_InvoiceHeader.Status
	,SAL_InvoiceHeader.InvoiceType
	,SAL_InvoiceHeader.InvoiceNumber
	,SAL_InvoiceHeader.InvoiceDate
	,SAL_InvoiceHeader.InvoiceReceivedDate
	,SAL_InvoiceHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel
FROM SAL_InvoiceHeader
left JOIN AR_SET_Customer ON SAL_InvoiceHeader.CustomerID  = AR_SET_Customer.CustomerID 
	--AND SAL_InvoiceHeader.CompanyID = AR_SET_Customer.CompanyID
  AND SAL_InvoiceHeader.PeriodID = AR_SET_Customer.PeriodID 
	AND AR_SET_Customer.Status = 1
left JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
	AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
	AND AR_SET_Customer.Status = 1
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_InvoiceHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_InvoiceHeader.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
	AND SET_DocumentApprove.DocumentNo = SAL_InvoiceHeader.InvoiceNumber 
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_InvoiceHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_InvoiceHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_InvoiceHeader.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), SAL_InvoiceHeader.InvoiceDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), SAL_InvoiceHeader.InvoiceDate, 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
											WHERE ih.CompanyID = SAL_InvoiceHeader.CompanyID 
												AND ih.PeriodID = SAL_InvoiceHeader.PeriodID 
												AND ih.InvoiceID = SAL_InvoiceHeader.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM SAL_InvoiceHeader ih 
													WHERE ih.CompanyID = SAL_InvoiceHeader.CompanyID 
														AND ih.PeriodID = SAL_InvoiceHeader.PeriodID 
														AND ih.InvoiceID = SAL_InvoiceHeader.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.PeriodID = @PeriodID 
    AND SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.InvoiceID = @InvoiceID AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID 
    AND SAL_InvoiceHeader.InvoiceNumber = @DocumentNo
";

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_ForApproveDetail, sqlparams);
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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int invoiceID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Select_DataByKey_ForApprove, sqlparams);

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

        private void CheckQuantity(int stockHeaderID, int companyID, int periodID, SqlTransaction sqltrans, SOEnumaration.SalesOrder_Stage closedCode,
            Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataTable dtDispatchOrder = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_CheckQuantity, sqlparams).Tables[0];

                if (dtDispatchOrder != null && dtDispatchOrder.Rows.Count > 0)
                {
                    bool result = false;
                    SAL_DispatchHeader dh = new SAL_DispatchHeader();

                    foreach (DataRow dr in dtDispatchOrder.Rows)
                    {
                        result = dh.UpdateFlagDetail(sqltrans, Convert.ToInt32(dr["StockTransactionID"]), Convert.ToInt32(dr["TransactionDetailID"]), true, companyID, periodID, updatedBy, updateDate, updatedByValue, closedCode, bool.Parse(dr["IsDirectDispatch"].ToString()));

                        if (!result)
                            break;
                    }
                    if (bool.Parse(dtDispatchOrder.Rows[0]["IsDirectDispatch"].ToString()) == false)
                    {
                        #region Update flag for Delivery Order
                        if (result)
                        {
                            if (dh.CheckIsDetailCompleted(sqltrans, stockHeaderID, true, f_CompanyID, f_PeriodID))
                                dh.UpdateFlagHeader(sqltrans, stockHeaderID, true, companyID, periodID, updatedBy, updateDate, updatedByValue);
                        }
                        else
                            throw new BusinessLogicException("Error generated: SAL_InvoiceHeader.cs, Line: 352, Function: DH.UpdateFlagDetail");
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public bool UpdateFlagDetail(SqlTransaction sqltrans, int invoiceHeaderID, int invoiceDetailID, bool status, int companyID, int periodID,
            Guid updatedBy, DateTime updateDate, string updatedByValue, SOEnumaration.SalesOrder_Stage closedCode)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@InvoiceDetailID", invoiceDetailID);
                sqlparams[2] = new SqlParameter("@Status", status);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                sqlparams[4] = new SqlParameter("@PeriodID", periodID);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, Query_Update_Flag_Detail, sqlparams) > 0)
                {
                    result = true;

                    #region Checking For Dispatch Complete Status

                    CheckQuantity(GetStockHeaderID(invoiceHeaderID, invoiceDetailID, companyID, periodID, sqltrans), companyID, periodID, sqltrans, closedCode, updatedBy, updateDate, updatedByValue);

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

        private static int GetStockHeaderID(int invoiceHeaderID, int invoiceDetailID, int companyID, int periodID, SqlTransaction sqltrans)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);
                sqlparams[1] = new SqlParameter("@InvoiceDetailID", invoiceDetailID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans, CommandType.Text, Query_Get_SHID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        private static int GetDispatchHeaderID(int invoiceHeaderID, int companyID, int periodID, SqlTransaction sqltrans)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", invoiceHeaderID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans, CommandType.Text, Query_Get_DispatchHeaderID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool CheckIsDetailCompleted(SqlTransaction objTransaction, int invoiceID, bool isGenerated, int companyID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@InvoiceHeaderID", invoiceID);
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

        public bool UpdateFlagHeader(int companyID, int periodID, int invoiceID, bool isGenerated, Guid updatedBy, DateTime updateDate, string updatedByValue,
            SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[1] = new SqlParameter("@IsGenerated", isGenerated);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction != null)
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag_Header, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_Flag_Header, sqlparams) > 0)
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

        public static DataTable Sales_Invoice_Report(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
            Nullable<int> documentTypeID, short? documentGroupID = null,int FiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (documentTypeID.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Sales_Invoice_Report + whereFilter.ToString()), sqlparams);

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

        public static DataTable HSMExportSaleRegister_Report(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            Nullable<int> currencyID = null, int FiscalID = 0, Nullable<int> customerName = null, short? statusCode = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder Orderby = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (dateFrom != null && dateTo != null)
                {
                    whereFilter.Append(" And Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                }
                else if (dateFrom != null)
                {
                    whereFilter.Append(" And Cast(SAL_InvoiceHeader.InvoiceDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                }
                else if (dateTo != null)
                {
                    whereFilter.Append(" And Cast(SAL_InvoiceHeader.InvoiceDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                }
                if (customerName > 0 && customerName != null)
                {
                    whereFilter.Append(" And AR_SET_Customer.CustomerID = " + customerName + " ");
                }
                if (currencyID != null && currencyID != -1)
                {
                    whereFilter.Append(" And SET_Currency.CurrencyID = " + currencyID + " ");
                }

                if(statusCode.HasValue) 
                    whereFilter.Append(" And SAL_InvoiceHeader.StatusCode = " + statusCode);

                

                Orderby.Append(" order by CAST(SAL_InvoiceHeader.InvoiceNumber as int) asc " + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_ForHSMExportSaleRegister_Report + whereFilter.ToString() + Orderby.ToString(), sqlparams);


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

        public static DataTable Sales_Register_Report(string companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
    string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo,
             Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                //if (BatchID > 0)
                //    whereFilter.Append(" And Set_Batch.BatchID = " + BatchID.Value);

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");



                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if(BrokerID.HasValue)
                sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);


                companyID = "(" + companyID + ")";

                DataSet ds=null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_RegisterBeforejuly_Report.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    if (IsSheikhooReport)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_Sales_Register_Report_SKU.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Sales_Register_Report.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                        
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

        public static DataTable Sales_Register_ReportS(string companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo,
            Nullable<int> BrokerID, Nullable<int> BatchID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (BatchID > 0)
                    whereFilter.Append(" And FinalTAB.BatchID = " + BatchID.Value);

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");



                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);
               


                companyID = "(" + companyID + ")";

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_RegisterBeforejuly_Report.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    if (IsSheikhooReport)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_Sales_Register_Report_SKU.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_Sales_Register_Report.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);

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

        public static DataTable SaleRegisterCustomerBilledToWise_Steel_report(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
  string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
           // DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                //if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                //    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                //else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                //    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                //else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                //    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);
                if (invoiceNoFrom > 0 && invoiceNoTo > 0)
                {
                    whereFilter.Append(" and SAL_InvoiceHeader.InvoiceNumber between @InvoiceNoFrom and @InvoiceNoTo ");
                }
                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%billet%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                //DataSet ds = null;
                //{
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SaleRegisterCustomerBilledToWise_Steel_report + whereFilter.ToString()), sqlparams);
                //}
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SaleRegisterCustomerBilledToWise_Steel_report, whereFilter).ToString(), sqlparams);

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


        public static DataTable Sales_Register_Report_JK(string companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
    string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);



                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");



                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);


                companyID = "(" + companyID + ")";

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_RegisterBeforejuly_Report_JK.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_Register_Report_JK.Replace("@CompanyID", companyID) + whereFilter.ToString()), sqlparams);
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

        public static DataTable GetSummarySaleOfMud(string companyID, int periodID, string documentTypeID, Nullable<DateTime> dateTo, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                //else
                //    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                    whereFilter.Append(" AND FinalTAB.InvoiceDate <= @ToDate");

                    whereFilter.Append("  group by FinalTAB.PeriodSetName, FinalTAB.Inclusive  )Tab");
                
                sqlparams[2] = new SqlParameter("@ToDate", dateTo.Value);
              //  sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
             
                DataSet ds = null;

                companyID = "(" + companyID + ")";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SummarySaleOfMud + whereFilter.ToString()).Replace("@CompanyID", companyID), sqlparams);
                
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    ds.Tables[0].TableName = "tblSalesInvoice";
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
      

        public static DataTable Sales_Register_ReportAdjusted(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> ContractNo = null, Nullable<int> BatchID = null,
            Nullable<int> DoNo = null,bool IsSheikhooReport = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (ContractNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.ContractNo = " + ContractNo.Value);
                }

                if (BatchID > 0)
                    whereFilter.Append(" And Set_Batch.BatchID = " + BatchID.Value);

                if (DoNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.DeliveryOrderNo = " + DoNo.Value);
                }

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");


                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");

               

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);


                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_RegisterBeforejuly_Report_Adjusted + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    if (IsSheikhooReport)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_Sales_Register_Report_Adjusted_SKU + whereFilter.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_Register_Report_Adjusted + whereFilter.ToString()), sqlparams);
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


        public static DataTable SaleTaxInvoiceSummary_report(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> ContractNo = null,
            Nullable<int> DoNo = null,bool IsSheikhooReport = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (ContractNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.ContractNo = " + ContractNo.Value);
                }

                if (DoNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.DeliveryOrderNo = " + DoNo.Value);
                }

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");



                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");



                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleTaxInvoiceSummary + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    if (IsSheikhooReport)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_SaleTaxInvoiceSummary_SKU + whereFilter.ToString()), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleTaxInvoiceSummary + whereFilter.ToString()), sqlparams);
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


        public static DataTable SaleTaxInvoiceSummary2_report(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> ContractNo = null,
            Nullable<int> DoNo = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (ContractNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.ContractNo = " + ContractNo.Value);
                }

                if (DoNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.DeliveryOrderNo = " + DoNo.Value);
                }

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");



                whereFilter.Append("  Order bY FinalTAB.CustomerName,FinalTAB.InvoiceDate, FinalTAB.InvoiceNumber");



                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaletaxinvoiceSummaryReport + whereFilter.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaletaxinvoiceSummaryReport + whereFilter.ToString()), sqlparams);
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


        public static DataTable SaleRegCustomerwise(int companyID, int periodID, string doctype, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            // DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", doctype);
                //whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                //whereFilter.Append("  Order bY FinalTAB.CustomerName");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;
                //if (dateTo < ConditionDate)
                //{
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_Sales_RegisterBeforejuly_Report_Adjusted + whereFilter.ToString()), sqlparams);
                //}
                //else
                //{
                //doctype = "(" + doctype + ")";
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SaleRegCustomerwise_SKU_Steel.ToString().Replace("@DocumentTypeID", doctype)), sqlparams);
                //}
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

        public static DataTable SaleBrokerwise(int companyID, int periodID, string doctype, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID = 0, int? BrokerID = 0, int? CustomerID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", doctype);
                
                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if(BrokerID > 0)
                    whereFilter.Append(" and Brokers.CustomerId = " + BrokerID);

                if(CustomerID > 0)
                    whereFilter.Append(" and AR_SET_Customer.CustomerId = " + CustomerID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SaleBrokerwise.ToString().Replace("@DocumentTypeID", doctype), whereFilter), sqlparams);
                
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


        public static DataTable SaleRegCustomerwiseSteel(int companyID, int periodID, string doctype, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", doctype);
                
                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SaleRegCustomerwise_Steel.ToString().Replace("@DocumentTypeID", doctype)), sqlparams);
                
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


        public static DataTable SaleRegCustomerwise_SKU(int companyID, int periodID, string doctype, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", doctype);
                
                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_SaleRegCustomerwise_SKU.ToString().Replace("@DocumentTypeID", doctype)), sqlparams);
                
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

        public static DataTable NewSales_RegisterSugar_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false, bool IsPatoki = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");

                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND StatusCode = " + statusCode.Value);


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds;

                if(IsSheikhooReport)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_New_Sales_Register_Report_SKU , whereFilter.ToString()), sqlparams);
                else if (IsPatoki)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_NewSaleRegisterReport_Patoki, whereFilter.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_New_Sales_Register_Report , whereFilter.ToString()), sqlparams);

                if (IsHabibSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SaleRegister_Habib, whereFilter.ToString()), sqlparams);

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

        public static DataTable SugarSale_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder whereFilterReturn = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                {
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                }
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                {
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo >  @InvoiceNoFrom  ) ");
                }
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                {
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo <  @InvoiceNoTo  ) ");
                }

                if (documentTypeID.ToString() != string.Empty)
                {
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                    whereFilterReturn.Append(" AND SAL_ReverseHeader.DocumentTypeID in " + documentTypeID.ToString());
                }
                else
                {
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");
                }

                if (statusCode.HasValue)
                {
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);
                    whereFilterReturn.Append(" AND SAL_ReverseHeader.StatusCode = " + statusCode.Value);
                }


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");
                whereFilterReturn.Append(" AND  convert(date,SAL_ReverseHeader.Createdate) between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                {
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");
                }



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds;

                //if (IsSheikhooReport)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_New_Sales_Register_Report_SKU, whereFilter.ToString()), sqlparams);
                //else
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_New_Sales_Register_Report, whereFilter.ToString()), sqlparams);

                if (IsHabibSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSale_Habib, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSale_Habib, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);


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


        public static DataTable APRBalance_Report(int companyID, int periodID, Nullable<int> DocNoFrom, Nullable<int> DocNoTo, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder whereFilterReturn = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (DocNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@DocNoFrom", DocNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@DocNoFrom", DBNull.Value);
                }
                if (DocNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@DocNoTo", DocNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@DocNoTo", DBNull.Value);
                }

                if (DocNoFrom.HasValue && DocNoTo.HasValue)
                {
                    whereFilter.Append(" AND (AP_AdvanceSugarPayment.DocumentNo Between  @DocNoFrom and  @DocNoTo ) ");
                }
                else if (DocNoFrom.HasValue && (!DocNoTo.HasValue))
                {
                    whereFilter.Append(" AND (AP_AdvanceSugarPayment.DocumentNo >  @DocNoFrom  ) ");
                }
                else if ((!DocNoFrom.HasValue) && DocNoTo.HasValue)
                {
                    whereFilter.Append(" AND (AP_AdvanceSugarPayment.DocumentNo <  @DocNoTo  ) ");
                }

                whereFilter.Append(" AND AP_AdvanceSugarPayment.RecievingDate between @FromDate and @ToDate");


                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                
                DataSet ds;

                //ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_APRBalance_Report, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_APRBalance_Report,whereFilter.ToString()), sqlparams);
                
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


        public static DataTable SugarSale_Report_RSM(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false, bool IsRaniPurSugar = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder whereFilterReturn = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                {
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                }
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                {
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo >  @InvoiceNoFrom  ) ");
                }
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                {
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");
                    whereFilterReturn.Append(" AND (SAL_ReverseHeader.DocumentNo <  @InvoiceNoTo  ) ");
                }

                if (documentTypeID.ToString() != string.Empty)
                {
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                    whereFilterReturn.Append(" AND SAL_ReverseHeader.DocumentTypeID in " + documentTypeID.ToString());
                }
                else
                {
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");
                }

                if (statusCode.HasValue)
                {
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);
                    whereFilterReturn.Append(" AND SAL_ReverseHeader.StatusCode = " + statusCode.Value);
                }


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");
                whereFilterReturn.Append(" AND  convert(date,SAL_ReverseHeader.Createdate) between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                {
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");
                }



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds;

                //if (IsSheikhooReport)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_New_Sales_Register_Report_SKU, whereFilter.ToString()), sqlparams);
                //else
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_New_Sales_Register_Report, whereFilter.ToString()), sqlparams);

                if (IsHabibSugar)
                {
                    if (IsRaniPurSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSale_RSM, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);
                    
                    else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSale_Habib, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSale_Habib, whereFilter.ToString(), whereFilterReturn.ToString()), sqlparams);
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


        public static DataTable SugarSale_Report_CO2(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (companyID == 2)
                    sb1.Append("AND SAL_ReverseHeader.ReverseHeaderID = null");

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");

                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");

                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds;

                if (IsHabibSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.SugarSale_CO2, whereFilter.ToString(),sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.SugarSale_CO2, whereFilter.ToString()), sqlparams);


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

        public static DataTable NewSales_Register_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Bagasse%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_New_Sales_Register_Bagasse_Report, whereFilter).ToString(), sqlparams);

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

        public static DataTable NewSales_Register_Report_OtherThenAKT(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Bagasse%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_New_Sales_Register_Bagasse_Report_OtherThanAKT, whereFilter).ToString(), sqlparams);

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

        public static DataTable SaleRegItemwise_Report(int companyID, int periodID, string doctype, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);               
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[4] = new SqlParameter("@FiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_SaleRegItemwise.Replace("@DocumentTypeID", doctype)).ToString(), sqlparams);

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


        public static DataTable NewSales_Register_Steel_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%billet%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_New_Sales_Register_Billet_Report_Steel, whereFilter).ToString(), sqlparams);

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


        public static DataTable NewSales_Register_Steel_URC_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%billet%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_New_Sales_Register_Billet_Report_Steel_URC, whereFilter).ToString(), sqlparams);

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


        public static DataTable NewSales_Register_AKT_Report(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%billet%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_New_Sales_Register_Billet_Report, whereFilter).ToString(), sqlparams);

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


        public static DataTable GetAll_Report_ARBillWise(int companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VendorFilter", vendorFilter);

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDatee <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                


                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Report_ARBillWise + additionalParameters, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable BTLPaymentsAgainstContracts_Report(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
      Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }
                companyID = "(" + companyID + ")";
                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_BTLPaymentsAgainstContracts, additionalParameters).Replace("@CompanyID", companyID), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable BTLPaymentsAgainstServices_Report(string companyID, int periodID, int? vendorFilter, Nullable<DateTime> dateFrom = null,
 Nullable<DateTime> dateTo = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalParameters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);

                if (vendorFilter.HasValue)
                {
                    additionalParameters = " and AR_SET_Customer.CustomerID = @VendorFilter";
                    sqlparams[4] = new SqlParameter("@VendorFilter", vendorFilter);
                }

                companyID = "(" + companyID + ")";

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_Set_Customer.Query_BTLPaymentsAgainstServices, additionalParameters).Replace("@CompanyID", companyID), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetSalesTaxLiability(int companyID, int periodID, DateTime fromDate, DateTime toDate)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_SalesTaxLiability, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public bool UpdateDataByKeySalesTaxDate(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", f_InvoiceID);

                if (f_SalesTaxDate.HasValue)
                    sqlparams[3] = new SqlParameter("@SalesTaxDate", f_SalesTaxDate);
                else
                    sqlparams[3] = new SqlParameter("@SalesTaxDate", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[4] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[4] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[5] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[5] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[6] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[6] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_SalesTaxDate, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_SalesTaxDate, sqlparams) > 0)
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

        public bool UpdateInvoiceHeader(List<SAL_InvoiceHeader> colUpdateInvHdr)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateIHeader");

            try
            {
                foreach (SAL_InvoiceHeader invHdrUpdate in colUpdateInvHdr)
                    invHdrUpdate.UpdateDataByKeySalesTaxDate(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateIHeader");
            }

            return result;
        }

        public static DataTable Sales_Tax_List_With_CustomerName_Report(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, int customerid, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(" WHERE SAL_InvoiceHeader.CompanyID = " + companyID + " and SAL_InvoiceHeader.PeriodID =  " + periodID + " And SAL_InvoiceHeader.Status = 1");
                if (dateFrom != null && dateTo != null)
                    sb.Append(" and CAST(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                else if (dateFrom != null)
                    sb.Append(" and CAST(SAL_InvoiceHeader.CreateDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");

                else if (dateTo != null)
                    sb.Append(" and CAST(SAL_InvoiceHeader.CreateDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (invoiceNoFrom > 0 && invoiceNoTo > 0)
                    sb.Append(" And SAL_InvoiceHeader.InvoiceNumber Between '" + invoiceNoFrom + "' And '" + invoiceNoTo + "'");

                else if (invoiceNoFrom > 0 && invoiceNoTo == 0)
                    sb.Append("And SAL_InvoiceHeader.InvoiceNumber = " + invoiceNoFrom);

                if (customerid != 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = " + customerid);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Sales_Tax_List_With_CustomerName_Report, sb.ToString()));

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

        public static DataTable GetLoanRecoveryInfo(int companyID, int periodID, int invoiceID, bool CustomerInvocie = false)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                string Query = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);

                if (!CustomerInvocie)
                    Query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetLoanRecoveryInfo; // Query change for habib case Amount paid Already set to form . old query in comment section
                else
                    Query = Query_GetCustomerDirectInvocie;

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


        public static DataTable GetLoanRecoveryInfo_SROInvocies(int companyID, int periodID, int invoiceID, bool CustomerInvocie = false, int ForSro = 0,bool AllMonth = false,int CustomerID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                string Query = string.Empty;


                const string Query_AllMonthInvoicesSRO = @"and datepart(month,SAL_InvoiceHeader.InvoiceDate) = (select  datepart(month,InvoiceDate) from SAL_InvoiceHeader where InvoiceID = @InvoiceID)
    and SAL_InvoiceHeader.customerID = @customerID
";

                const string Query_AllMonthInvoices = @"and datepart(month,SAL_InvoiceHeader.InvoiceDate) = (select  datepart(month,InvoiceDate) from SAL_InvoiceHeader where InvoiceID = @InvoiceID)
    and SAL_InvoiceHeader.customerID = @customerID
    and SAL_InvoiceHeader.amountPaid <>
    isnull((select sum(AR_ReceiptInvoicesDetail.Amount) from AR_ReceiptInvoicesDetail
	left join AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
	where AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
    and AR_ReceiptTransactionHeader.ReversalDate is null and ReversalComments is null
    and AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID
    and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID
    and AR_ReceiptInvoicesDetail.Status = 1),0 )

";


                const string Query_AllMonthInvoicesForIncomeTax = @"and datepart(month,SAL_InvoiceHeader.InvoiceDate) = (select  datepart(month,InvoiceDate) from SAL_InvoiceHeader where InvoiceID = @InvoiceID)
    and SAL_InvoiceHeader.customerID = @customerID
    and SAL_InvoiceHeader.amountPaid <>
    isnull(
    (
	select sum(AR_ReceiptInvoicesDetail.Amount) from AR_ReceiptInvoicesDetail
	left join AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
	
	where AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
    and AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID
    and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID
    and AR_ReceiptInvoicesDetail.Status = 1
    and AR_ReceiptTransactionHeader.InvoiceFor = 'InComeTAX'
    ),0 )";



                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@ForSro", ForSro);
                sqlparams[4] = new SqlParameter("@customerID", CustomerID);
                

                if (!CustomerInvocie)
                    Query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetLoanRecoveryInfo_SROInvoices;
                else
                    Query = Query_GetCustomerDirectInvocie;


                if (!AllMonth)
                    sb.Append(" and SAL_InvoiceHeader.InvoiceID = @InvoiceID");
                else
                {
                    if (ForSro == 3)
                        sb.Append(Query_AllMonthInvoicesForIncomeTax);
                    else if(ForSro == 0)
                        sb.Append(Query_AllMonthInvoices);
                    else if (ForSro == 1)
                        sb.Append(Query_AllMonthInvoicesSRO);

                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams);

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


        public static DataTable GetAdvanceAgainstSOAmount(int companyID, int periodID, int invoiceID, bool CustomerInvocie = false, int customerid = 0)
        {
            DataTable dtToReturn = null;

            try
            {

                StringBuilder sb = new StringBuilder();
                string Query = string.Empty;

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[3] = new SqlParameter("@customerid", customerid);

                if (!CustomerInvocie)
                    Query = Query_GetAdvanceAgainst_SO_Amount;
                else
                    Query = Query_GetAdvanceAgainst_SO_Amount;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, invoiceID), sqlparams);

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

        public static DataSet GetRecoveryInvoice(int companyID, int periodID, long recoveryID)
        {
            DataSet dsToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RecoveryID", recoveryID);

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRecoveryInvoice, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static bool Delete_Permanent_By_PrimaryKey(int companyID, int periodID, int invoiceID, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_Permanent_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataRow Get_Invoice_By_DocumentType(int companyID, int periodID, int documentTypeID, int documentNo, int FiscalID)
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
                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Invoice_By_DocumentType, sqlparams);

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

        public static DataRow Get_Dispatch_By_invoiceID(int companyID, int periodID, int Invoiceid, int invoicetype)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", Invoiceid);
                sqlparams[3] = new SqlParameter("@InvoiceTypeID", invoicetype);


                const string Query_Get_DispatchNo_By_Invoice = @"Select	sal_dispatchHeader.DispatchNo,SAL_InvoiceDetail.StockHeaderID
	 from sal_invoiceHEader
			inner join SAL_InvoiceDetail on SAL_InvoiceDetail.InvoiceHeaderID=sal_invoiceHEader.InvoiceID
			And SAL_InvoiceDetail.CompanyID= sal_invoiceHEader.CompanyID
			And SAL_InvoiceDetail.PeriodID= sal_invoiceHEader.PeriodID
			And  SAL_InvoiceDetail.Status=1

			Inner Join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID=SAL_InvoiceDetail.StockHeaderID
			And INV_StockTransactionHeader.CompanyID= SAL_InvoiceDetail.CompanyID
			And INV_StockTransactionHeader.PeriodID= SAL_InvoiceDetail.PeriodID
			And  INV_StockTransactionHeader.Status=1

			inner join sal_dispatchHeader on sal_dispatchHeader.DispatchID=INV_StockTransactionHeader.DocumentNo
			And sal_dispatchHeader.DocumentTypeID=INV_StockTransactionHeader.DocumentTypeID
			And sal_dispatchHeader.CompanyID=INV_StockTransactionHeader.CompanyID
			And sal_dispatchHeader.PeriodID=INV_StockTransactionHeader.PeriodID
			And sal_dispatchHeader.FiscalID= INV_StockTransactionHeader.FiscalID
			And sal_dispatchHeader.Status=1



	 where sal_invoiceHEader.CompanyID=@CompanyID
	  and sal_invoiceHEader.PeriodID=@PeriodID
	 and sal_invoiceHEader.invoiceid=@InvoiceID 
	 And sal_invoiceHEader.InvoiceType= @InvoiceTypeID";






                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DispatchNo_By_Invoice, sqlparams);

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

        public static DataRow Get_Receipt_Against_Invoices(int companyID, int periodID, int InvoiceHeaderID, int DocumentTypeID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                string query = @" 
select AR_ReceiptInvoicesDetail.* from AR_ReceiptTransactionHeader

INNER JOIN AR_ReceiptInvoicesDetail ON AR_ReceiptInvoicesDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
AND AR_ReceiptInvoicesDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
AND AR_ReceiptInvoicesDetail.FiscalID = AR_ReceiptTransactionHeader.FiscalID
AND AR_ReceiptInvoicesDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
                                   
INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
AND SAL_InvoiceHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID AND SAL_InvoiceHeader.FiscalID = AR_ReceiptInvoicesDetail.FiscalID
AND SAL_InvoiceHeader.InvoiceID = AR_ReceiptInvoicesDetail.InvoiceHeaderID

where AR_ReceiptTransactionHeader.Status = 1 
and AR_ReceiptTransactionHeader.CompanyID = @CompanyID 
and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptInvoicesDetail.InvoiceHeaderID = @InvoiceHeaderID 
and SAL_InvoiceHeader.InvoiceType = @DocumentTypeID";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public bool ReverseDocument(List<SAL_InvoiceDetail> lst_salInvoiceDetail, DateTime dt_ReverseDate, Guid reversedBy, GL_TransactionHeader objGLHeader,
            List<GL_TransactionDetail> lst_GLDetails, SET_DocumentApprove obj_Approval, out int ReversalDocumentNo, string Comments, List<Sal_InvoiceCustomerDetail> colSalInvoiceCustomer, int CurrentFiscalID=0)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("ReverseDirectInvoices");

            try
            {
                string NarrationOld = objGLHeader.Narration.ToString();
                DataTable dtInvoiceTransactionDetail = SAL_InvoiceHeader.GetDataByKeyForReversal(this.CompanyID, this.PeriodID, this.InvoiceID, objSQLTransaction);

                this.UpdateDataByKey(objSQLTransaction);

                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(this.InvoiceType, this.CompanyID, this.PeriodID, objSQLTransaction, CurrentFiscalID);
                string originalNarration = objGLHeader.Narration != null ? objGLHeader.Narration : "";
                if (objGLHeader.Narration != null)
                    objGLHeader.Narration = objGLHeader.Narration + (objGLHeader.Narration.Trim().EndsWith(".") ? " " : ". ")
                        + "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";
                else
                    objGLHeader.Narration = "This document is reversed (Reversed document no.: " + newDocumentNo.ToString() + ").";

                objGLHeader.UpdateDataByKey(objSQLTransaction); //  Update old General Ledger Header

                #region Make Dispatch record available

                int dispatchID = SAL_InvoiceHeader.GetDispatchHeaderID(this.InvoiceID, this.CompanyID, this.PeriodID, objSQLTransaction);

                if (dispatchID != 0)
                {
                    SAL_DispatchHeader obj_DispatchHeader = new SAL_DispatchHeader(this.CompanyID, dispatchID, this.PeriodID, null, objSQLTransaction);

                    if (obj_DispatchHeader.IsDataLoaded)
                    {
                        obj_DispatchHeader.DispatchComplet = false;
                        obj_DispatchHeader.UpdateDataByKey(objSQLTransaction);
                    }
                }

                #endregion

                #region Make Payment record available

                if (this.PaymentStatusType.HasValue)
                {
                    AR_ReceiptTransactionHeader obj_ReceiptHeader = new AR_ReceiptTransactionHeader(this.CompanyID, this.PeriodID, this.PaymentStatusType.Value, null,
                        objSQLTransaction);

                    if (obj_ReceiptHeader.IsDataLoaded)
                    {
                        obj_ReceiptHeader.IsInvoiceAdjusted = false;
                        obj_ReceiptHeader.UpdateDataByKey(objSQLTransaction);
                    }
                }

                #endregion

                #region Insert new Purchase Invoice Header
                string Naration = Comments + " " + this.Description;
                this.IsReversed = null;
                this.ReversalDate = null;
                this.ReversalID = this.InvoiceID;
                this.Description = Naration + "  (This document is generated against the Reversal process (Doc. No.: " + this.InvoiceNumber + ")";
                this.InvoiceID = SAL_InvoiceHeader.GetNewInvoiceID(this.CompanyID, this.PeriodID, objSQLTransaction);
                this.InvoiceNumber = newDocumentNo.ToString();
                this.AmountPaid = this.AmountPaid * -1;
                this.GrossAmount = this.GrossAmount * -1;

                ReversalDocumentNo = Convert.ToInt32(this.InvoiceNumber);
                this.InvoiceDate = dt_ReverseDate;
                this.Insert(objSQLTransaction);

                #endregion

                #region Insert in Purchase Invoice Detail table

                if (lst_salInvoiceDetail != null)
                {
                    short salInvoiceDetailID = (short)SAL_InvoiceDetail.GetNewInvoiceDetailID(f_CompanyID, this.InvoiceID, f_PeriodID, objSQLTransaction);

                    foreach (SAL_InvoiceDetail obj_InvoiceDetail in lst_salInvoiceDetail)
                    {
                        obj_InvoiceDetail.InvoiceHeaderID = this.InvoiceID;
                        obj_InvoiceDetail.InvoiceDetailID = salInvoiceDetailID++;
                        obj_InvoiceDetail.QuantityInvoiced = obj_InvoiceDetail.QuantityInvoiced * -1;
                        obj_InvoiceDetail.Insert(objSQLTransaction);
                    }
                }

                #endregion

                #region Insert new General Ledger Header
                string GlNaration = NarrationOld += (originalNarration == null ? "" : originalNarration.Trim().EndsWith(".") ? " " : ". ")
                    + "This document is generated against the Reversal process. Original Document No.: " + objGLHeader.DocumentNo.ToString();

                objGLHeader.IsReversed = null;
                objGLHeader.Narration = Comments + GlNaration;
                objGLHeader.ReversalDate = null;
                objGLHeader.ReversedBy = null;
                objGLHeader.ReversalID = objGLHeader.TransactionID;
                objGLHeader.DocumentDate = dt_ReverseDate;
                objGLHeader.TransactionID = GL_TransactionHeader.GetNewTransactionID(objGLHeader.CompanyID, objGLHeader.PeriodID, objSQLTransaction);
                objGLHeader.DocumentNo = Convert.ToInt32(this.InvoiceNumber);

                objGLHeader.Insert(objSQLTransaction);

                #endregion

                #region Insert new General Ledger Detail

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

                #region Insert in invocie Customer table

                if (colSalInvoiceCustomer != null)
                {
                    short receiptCustomerID = (short)Sal_InvoiceCustomerDetail.GetNewInvoiceCustomerID(f_CompanyID, this.InvoiceID, f_PeriodID, objSQLTransaction);

                    foreach (Sal_InvoiceCustomerDetail objReceiptCustomer in colSalInvoiceCustomer)
                    {
                        objReceiptCustomer.InvoiceHeaderID = this.InvoiceID;
                        objReceiptCustomer.InvoiceCustomerID = receiptCustomerID++;
                        objReceiptCustomer.Insert(objSQLTransaction);
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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "ReverseDirectInvoices");
            }

            return result;
        }


        public static DataTable GetDataByKeyForReversal(int companyID, int periodID, int InvoiceHeaderID, SqlTransaction objSQL_Transaction)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);
                sqlParams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);

                if (objSQL_Transaction != null)
                {
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_ByKey,
                        sqlParams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectAll_ByKey, sqlParams);

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

        public static DataTable Get_AllDocumentNos(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                string Query_Get_AllDocumentNos = @" SELECT SAL_InvoiceHeader.InvoiceNumber As DocumentNo FROM SAL_InvoiceHeader
    inner join Set_PeriodCustom on  
 SAL_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID 
AND SAL_InvoiceHeader.Status = 1 and SAL_InvoiceHeader.StatusCode = 1 
AND IsNull(SAL_InvoiceHeader.IsReversed, 0) = 0 AND SAL_InvoiceHeader.ReversalID IS NULL 
ORDER BY Convert(BigInt, SAL_InvoiceHeader.InvoiceNumber) ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllDocumentNos, sqlparams);

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

        public static DataTable GetExcessDispatchStock(int CompanyID, int PeriodID, DateTime dateFrom, DateTime dateTo)
        {
            DataTable dtToReturn = null;

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                // sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@DateFrom", ((DateTime)dateFrom).ToString("yyyy-MM-dd"));
                sqlparams[4] = new SqlParameter("@DateTo", ((DateTime)dateTo).ToString("yyyy-MM-dd"));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_ExcessDispatchStockReport), sqlparams);

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

        public decimal Get_DirectReceiptAmount(int companyID, int periodID, int invoiceID)
        {
            decimal amount = 0;

            SqlParameter[] sqlparams = new SqlParameter[3];
            sqlparams[0] = new SqlParameter("@CompanyID", companyID);
            sqlparams[1] = new SqlParameter("@PeriodID", periodID);
            sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);


            //            string Query_DirectReceiptAmount = @" select sum(AR_ReceiptTransactionHeader.amount) from 
            //AR_ReceiptInvoicesDetail
            //	left join AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
            //    and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
            //    and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
            //	            where AR_ReceiptInvoicesDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID and AR_ReceiptTransactionHeader.ReversalDate is null 
            //                    and ReversalComments is null and AR_ReceiptInvoicesDetail.CompanyID = AR_ReceiptInvoicesDetail.CompanyID 
            //                    and AR_ReceiptInvoicesDetail.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
            //                    and AR_ReceiptInvoicesDetail.PeriodID = @PeriodID
            //                    and AR_ReceiptInvoicesDetail.CompanyID = @CompanyID
            //                    and AR_ReceiptInvoicesDetail.InvoiceHeaderID = @InvoiceID
            //                    group by AR_ReceiptInvoicesDetail.Amount
            //           ";

            string Query_DirectReceiptAmount = @" select Sum(AR_ReceiptCustomerDetail.Amount) - PreviousInvoiceAmount.InvoiceAmount As Amount from Sal_InvoiceHeader 
    Inner Join AR_ReceiptTransactionHeader On AR_ReceiptTransactionHeader.CompanyID = Sal_InvoiceHeader.CompanyID 
        And AR_ReceiptTransactionHeader.PeriodID = Sal_InvoiceHeader.PeriodID 
        And AR_ReceiptTransactionHeader.ReceiptTransactionID = Sal_InvoiceHeader.PaymentStatusType And AR_ReceiptTransactionHeader.Status = 1 
        And AR_ReceiptTransactionHeader.StatusCode = 1 
	Inner Join AR_ReceiptCustomerDetail On AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID 
		And AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        And AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
    Outer Apply
    (
		Select IsNull(Sum(AmountPaid), 0) As InvoiceAmount From Sal_InvoiceHeader 
		Where CompanyID = AR_ReceiptTransactionHeader.CompanyID And PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			And PaymentStatusType = AR_ReceiptTransactionHeader.ReceiptTransactionID And InvoiceID <> @InvoiceID And Status = 1 And StatusCode = 1
    ) As PreviousInvoiceAmount 
where Sal_InvoiceHeader.InvoiceID = @InvoiceID and Sal_InvoiceHeader.CompanyID = @CompanyID and Sal_InvoiceHeader.PeriodID = @PeriodID 
Group By PreviousInvoiceAmount.InvoiceAmount ";

            amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_DirectReceiptAmount, sqlparams));

            return amount;
        }

        #endregion

        public static DataTable GetAllSalesInvoiceTrd(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo,int CurrentFiscalID, DateTime FromDate, DateTime ToDate)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[7];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[5] = new SqlParameter("@FromDate", FromDate);
                sqlparams[6] = new SqlParameter("@ToDate", ToDate);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoiceTrdReport) + filter, sqlparams);

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

        public static DataTable GetAllSalesInvoiceTrdWithCF(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoiceTrdReportWithCF) + filter, sqlparams);

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

        public static DataTable GetAllSalesInvoice_Trd(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoice_TrdReport) + filter, sqlparams);

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

        public static DataTable GetAllSalesInvoiceTrd_Molasses(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoiceTrdReport) + filter, sqlparams);

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

        public static DataTable GetAllSalesInvoiceTrd_MolassesWithCF(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoiceTrdReportWithCF) + filter, sqlparams);

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

        public static DataTable GetAllSalesInvoiceTrd_Steel(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesInvoiceTrd_SteelReport) + filter, sqlparams);

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

        public static DataTable GetAllServiceSalesInvoice(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_ServiceSalesInvoiceReport) + filter, sqlparams);

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


        public static DataTable GetTaxInvoice(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno ,int? Contractno , string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);
        
                if(dispatchno>0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if(DocumentTypeID.ToString() != string.Empty )
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_TaxInvoiceReport) + filter.ToString(),sqlparams);

                if(ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetTaxInvoiceTrading(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_TaxInvoiceReport) + filter.ToString(), sqlparams);

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

        public static DataTable GetTaxInvoice_SKU(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_TaxInvoiceSKUReport) + filter.ToString(), sqlparams);

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


        public static DataTable GetTaxInvoice_Trading_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_TaxInvoice_Trading) + filter.ToString(), sqlparams);

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

        public static DataTable GetTaxInvoice_Trading(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_TaxInvoiceSKUReport) + filter.ToString(), sqlparams);

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

        public static DataTable GetTaxInvoice_CO2(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                //filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_TaxInvoiceCO2Report, filter.ToString()), sqlparams);

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


        public static DataTable GetTaxInvoice_CO2Trading(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                //filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_TaxInvoiceCO2Report, filter.ToString()), sqlparams);

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
        public static DataTable GetTaxInvoice_Steel(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, int? DeliveryOrderNo = null, bool Isdriver = false)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" and isnull(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) =" + Contractno);

                if (DeliveryOrderNo > 0)
                    filter.Append(" and SAL_DeliveryOrderHeader.DeliveryOrderNo  = " + DeliveryOrderNo);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = null;
                if (Isdriver == true)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_TaxInvoiceSteelDriverReport) + filter.ToString(), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_TaxInvoiceSteelReport) + filter.ToString(), sqlparams);
                }
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


        public static DataTable GetTaxInvoice_SteelTrading(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, int? DeliveryOrderNo = null, bool Isdriver = false)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" and isnull(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) =" + Contractno);

                if (DeliveryOrderNo > 0)
                    filter.Append(" and SAL_DeliveryOrderHeader.DeliveryOrderNo  = " + DeliveryOrderNo);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = null;
                if (Isdriver == true)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_TaxInvoiceSteelDriverReport) + filter.ToString(), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_TaxInvoiceSteelReport) + filter.ToString(), sqlparams);
                }
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

        public static DataTable GetTaxInvoice_Billet(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_TaxInvoiceBilletReport) + filter.ToString(), sqlparams);

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

        public static DataTable GetTaxInvoice_BilletTrading(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType not like '%Sales%Invoice%Sugar%' ");

                filter.Append("  Order BY  [SAL_InvoiceHeader].InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_TaxInvoiceBilletReport) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReport) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_SRO(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReport_SRO) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_SSML(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReport_SSML) + filter.ToString(), sqlparams);

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


        public static DataTable GetNewTaxInvoicePS(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReportPS) + filter.ToString(), sqlparams);

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

        public static DataTable GetLiftingDetailReport(string CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (dateFrom != null && dateTo != null)
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'  and SAL_DispatchHeader.CustomerCNICII is not null and AR_SET_Customer.IsRegistered = 0");

                //if (Supplier > 0)
                //    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                //if (dispatchno > 0)
                //    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                //if (Invoiceno > 0)
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                //if (Contractno > 0)
                //    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                //if (DocumentTypeID.ToString() != string.Empty)
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                //filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");
                string newCompanyID = "(" + CompanyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_LiftingDetailReport).Replace("@CompanyID", newCompanyID) + filter.ToString(), sqlparams);


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

        public static DataTable GetNewTaxInvoice_SKU(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_NewTaxInvoiceReport_SKU) + filter.ToString(), sqlparams);

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


        public static DataTable GetNewTaxInvoicePS_SKU(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_NewTaxInvoiceReport_SKU) + filter.ToString(), sqlparams);

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
        public static DataTable GetNewTaxInvoice_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_HSM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoiceCO2_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceCO2Report_HSM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_ADM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_ADM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_SKD(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_SKD) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice1(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport1) + filter.ToString(), sqlparams);

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


        public static DataTable GetNewTaxInvoice_v2(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_v2) + filter.ToString(), sqlparams);

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

        public static DataTable NewSugarTaxInvoice_SRO(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_SRO) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_SSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_SSM) + filter.ToString(), sqlparams);

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


        public static DataTable GetSalesInvoice_Export(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Contractno, string DocumentTypeID)
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

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                //if (dispatchno > 0)
                    // filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                    //if (Invoiceno > 0)
                    //    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                    if (Contractno > 0)
                        filter.Append(" And SAL_ContractHeader.ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And SAL_ContractHeader.SaleContractTypeID in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_ContractHeader.ContractNo");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_SalesInvoice_Report) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice1_Ethanol1(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReport1_Ethanolnew) + filter.ToString(), sqlparams);

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


        public static DataTable GetNewTaxInvoice_SKU_ForSaleInvoiceProcess(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, string Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber in " + Invoiceno.ToString());

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoiceReport_SKU) + filter.ToString(), sqlparams);

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


        public static DataTable GetAllPerformerInvoice(int CompanyID, int PeriodID, int DocumentTypeID, int? vendorFilter, int? OrderFrom, int? OrderTo, int CurrentFiscalID=0)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@OrderFrom", OrderFrom);
                sqlparams[3] = new SqlParameter("@OrderTo", OrderTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (OrderFrom.HasValue && OrderTo.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  between @OrderFrom AND @OrderTo");
                else if (OrderFrom.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderFrom ");
                else if (OrderTo.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderTo ");
                if (DocumentTypeID > 0)
                    filter.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentTypeID);

                if (vendorFilter > 0)
                    filter.Append(" AND SAL_OrderHeader.CustomerID  = " + vendorFilter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PerformerInvoiceReport) + filter, sqlparams);

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



        public static DataTable GetAllDetailAdvanceInvoice(int CompanyID, int PeriodID, int DocumentTypeID, int? vendorFilter, int? OrderFrom, int? OrderTo)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@OrderFrom", OrderFrom);
                sqlparams[3] = new SqlParameter("@OrderTo", OrderTo);

                if (OrderFrom.HasValue && OrderTo.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  between @OrderFrom AND @OrderTo");
                else if (OrderFrom.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderFrom ");
                else if (OrderTo.HasValue)
                    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderTo ");
                if (DocumentTypeID > 0)
                    filter.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentTypeID);

                if (vendorFilter > 0)
                    filter.Append(" AND SAL_OrderHeader.CustomerID  = " + vendorFilter);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DetailAdvanceInvoice) + filter, sqlparams);

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




        public static DataTable GetAllBranchWiseExpiry(int CompanyID, int PeriodID, int? ItemID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);


                ////if (OrderFrom.HasValue && OrderTo.HasValue)
                ////    filter.Append(" AND SAL_OrderHeader.ContractNo  between @OrderFrom AND @OrderTo");
                ////else if (OrderFrom.HasValue)
                ////    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderFrom ");
                ////else if (OrderTo.HasValue)
                ////    filter.Append(" AND SAL_OrderHeader.ContractNo  = @OrderTo ");
                ////if (DocumentTypeID > 0)
                ////    filter.Append(" AND SET_DocumentType.DocumentTypeID = " + DocumentTypeID);

                if (ItemID > 0)
                    filter.Append(" AND INV_Item.ItemID  = " + ItemID);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_BatchWiseExpiry) + filter, sqlparams);

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


        public static DataTable GetCreditMemo_Report(int CompanyID, int PeriodID, int? InvoiceFrom, int? InvoiceTo, Nullable<int> customerID = null, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                string additionalParameters = string.Empty;
                string GroupBy = string.Empty;
                SqlParameter[] sqlparams = new SqlParameter[6];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", InvoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", InvoiceTo);

                // sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);


                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate  ";
                    sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDatee <= @ToDate ";
                    sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                }

                if (InvoiceFrom.HasValue && InvoiceTo.HasValue)
                    sb.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo ");
                else if (InvoiceFrom.HasValue)
                    sb.Append(" AND SAL_InvoiceHeader.InvoiceNumber = @InvoiceFrom  ");
                else if (InvoiceTo.HasValue)
                    sb.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceTo  ");

                if (customerID > 0)
                    sb.Append(" And AR_SET_Customer.CustomerID = " + customerID + "  ");

                GroupBy = ("GROUP BY SAL_InvoiceHeader.InvoiceNumber,SAL_InvoiceHeader.InvoiceDate,SAL_InvoiceHeader.VatCode,SET_Term.Description ,Sal_Invoice_AccountsDetail.DocumentReference,Sal_Invoice_AccountsDetail.Narration,Sal_Invoice_AccountsDetail.TransactionType,SET_DocumentType.DocumentType,AR_SET_Customer.CustomerName,AR_SET_Customer.AddressLine1,SET_DocumentType.Description,SET_Business.BusinessName,SET_Business.AddressLine1 ,GL_CodeCombinationDetail.Description ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_CreditMemo + additionalParameters + sb.ToString()) + GroupBy, sqlparams);

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






        public static DataSet GetDI_SaleInvoiceTrd(int CompanyID, int PeriodID, int InvoiceTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID = 0, int? CustomerID = 0, string cofilter = "")
        {
            DataSet ds = null;
            //  DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (CustomerID.HasValue && CustomerID.Value != 0)
                    sqlparams[5] = new SqlParameter("@CustomerID", CustomerID.Value);
                else
                    sqlparams[5] = new SqlParameter("@CustomerID", DBNull.Value);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                //if (vendorFilter > 0)
                //    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);

                if (dateFrom != null && dateTo != null)
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "' ");

                string query = string.Empty;

                if (cofilter == "Hascombe")
                {
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_DISaleInvoice_hascombe.ToString();
                }
                else {
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_DISaleInvoice.ToString();              
                }

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, filter), sqlparams);
                ds.Tables[0].TableName = "Table";
                ds.Tables[1].TableName = "Table1";

                //if(ds != null && ds.Tables.Count > 0)
                //dtToReturn = ds;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataTable GetAllSaleCommission(int CompanyID, int PeriodID, int? DocummentGroupID, int? SalePersonID, DateTime? dateFrom, DateTime? dateTo)
        {
            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                string additionalParameters = string.Empty;
                string GroupBy = string.Empty;
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);


                // sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);


                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate BETWEEN @FromDate AND @ToDate  ";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }
                else if (dateFrom.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDate >= @FromDate ";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom.Value);
                }
                else if (dateTo.HasValue)
                {
                    additionalParameters = " AND SAL_InvoiceHeader.InvoiceDatee <= @ToDate ";
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo.Value);
                }

                if (SalePersonID > 0)
                    sb.Append(" And SAL_InvoiceHeader.billedtoid = " + SalePersonID + "  ");

                if (DocummentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + DocummentGroupID);

                sb.Append(" ORDER by SAL_InvoiceHeader.InvoiceDate ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SaleCommissionReport + additionalParameters + sb.ToString()), sqlparams);

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

        public static DataTable GetAllSalesInvoiceTrdOne(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo)
        {
            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {
                #region Query
                const string Query_SalesInvoiceTrdReportOne = @"SELECT 
   [SAL_InvoiceHeader].CompanyID,SET_DocumentType.DocumentType as InvoiceType ,[SAL_InvoiceHeader].InvoiceID
,[SAL_InvoiceHeader].InvoiceDate,[SAL_InvoiceHeader].Description ,[SAL_InvoiceHeader].InvoiceNumber
,SET_Business.BusinessName  +' '+ SET_OperatingUnit.OperatingUnitName as SupplierName, DocumentTypesGroup.Comments as SupplierAddress
,SET_OperatingUnit.NTNNumber as SupplierNTN,DocumentTypesGroup.BranchPh as SupplierPhoneNumber
, SET_OperatingUnit.SalesTaxNumber as SupplierSTRN,[SAL_InvoiceHeader].billedtoid, BC.CustomerName as BIllTo,AR_SET_Customer.CustomerName
,AR_SET_Customer.AddressLine1 as CustomerAddress,AR_SET_Customer.NTNNumber as CustomerNTN,AR_SET_Customer.PhoneNumber as CustomerPhoneNumber
,AR_SET_Customer.STRNNumber as CustomeSTRN,[SAL_DispatchHeader].DispatchNo as DispatchNo,INV_Item.Description as ItemDescription
,INV_Item.InventoryItemStatusCode as ItemStatusCode,INV_SET_Category.CategoryName,Abs([INV_StockTransactionDetail].[TransactionQuantity]) AS [BaseQuantity]
,[INV_StockTransactionDetail].Transactioncost as UnitPrice,[SAL_InvoiceDetail].[TaxAmount],[SAL_InvoiceDetail].GrossAmount,
--(isnull([SAL_InvoiceDetail].[GrossAmount],0) * isnull([SAL_InvoiceDetail].[TaxAmount],0)) / 100 as TaxPayable
((isnull([SAL_InvoiceDetail].GrossAmount,0)-isnull([SAL_InvoiceDetail].DiscountAmountTaken,0)) * isnull([SAL_InvoiceDetail].[TaxAmount],0)) / 100 as TaxPayable
   ,--[SAL_InvoiceDetail].GrossAmount + (isnull([SAL_InvoiceDetail].[GrossAmount],0) * isnull([SAL_InvoiceDetail].[TaxAmount],0)) / 100 as TotalAmount
([SAL_InvoiceDetail].GrossAmount + 
   ((isnull([SAL_InvoiceDetail].GrossAmount,0)-isnull([SAL_InvoiceDetail].DiscountAmountTaken,0)) * isnull([SAL_InvoiceDetail].[TaxAmount],0)) / 100)-isnull([SAL_InvoiceDetail].DiscountAmountTaken,0) as TotalAmount
,SET_OperatingUnit.Logoimage as LogoImage
,[SAL_InvoiceHeader].CustomerAddress as Shipto,[SAL_InvoiceDetail].Description as DescriptionTagNo, SAL_InvoiceDetail.DiscountAmountTaken
,(Case  DocumentTypesGroup.GroupName when 'Head Office-KHI' then 
         'K'+'SI' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          when null
          then '-' +' '+'SI' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          else  SUBSTRING(GroupName,1,1)+'SI' +'-'+ substring(convert(varchar(20),year(SAL_InvoiceHeader.InvoiceDate)),3,2) + '000' +convert(varchar(24),SAL_InvoiceHeader.InvoiceNumber)
          end  ) as SINumber
         ,SET_Business.BusinessName
          ,SET_Business.AddressLine1 as BusinessAddress
          ,SET_Business.PhoneNumber as BusinessPhoneNumber
          ,SET_Business.FaxNumber
          ,SET_Business.EmailAddress,SET_Business.AddressLine1
 FROM [SAL_InvoiceHeader] INNER JOIN [SAL_InvoiceDetail] On [SAL_InvoiceDetail].InvoiceHeaderID =[SAL_InvoiceHeader].InvoiceID
 AND [SAL_InvoiceDetail].CompanyID=[SAL_InvoiceHeader].CompanyID  AND [SAL_InvoiceDetail].PeriodID=[SAL_InvoiceHeader].PeriodID
 INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[TransactionDetailID] = [SAL_InvoiceDetail].[StockDetailID] 
    AND [INV_StockTransactionDetail].[StockTransactionID] = [SAL_InvoiceDetail].[StockHeaderID] 
    AND [INV_StockTransactionDetail].[CompanyID] = [SAL_InvoiceDetail].[CompanyID] AND [INV_StockTransactionDetail].[PeriodID] = [SAL_InvoiceDetail].[PeriodID] 
    AND [INV_StockTransactionDetail].[Status] = 1 INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = [INV_StockTransactionDetail].[StockTransactionID] 
        AND [INV_StockTransactionHeader].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
        AND [INV_StockTransactionHeader].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [INV_StockTransactionHeader].[Status] = 1 
    INNER JOIN [SAL_DispatchHeader] ON [SAL_DispatchHeader].[DocumentTypeID] = [INV_StockTransactionHeader].[DocumentTypeID] 
        AND [SAL_DispatchHeader].[DispatchNo] = [INV_StockTransactionHeader].[DocumentNo] 
        AND [SAL_DispatchHeader].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] AND [SAL_DispatchHeader].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] 
        AND [SAL_DispatchHeader].[Status] = 1 LEFT JOIN INV_Item ON INV_Item.ItemID=[INV_StockTransactionDetail].ItemID 
    --AND INV_Item.CompanyID =[INV_StockTransactionDetail].CompanyID 
left JOIN INV_SET_Category On INV_SET_Category.CategoryID =INV_Item.ItemTypeID
    --AND INV_SET_Category.CompanyID =INV_Item.CompanyID
 left JOIN AR_SET_Customer  ON AR_SET_Customer.CustomerID = [SAL_InvoiceHeader].CustomerID 
  --AND AR_SET_Customer.CompanyID = [SAL_InvoiceHeader].CompanyID
   AND AR_SET_Customer.PeriodID = [SAL_InvoiceHeader].PeriodID 
   left JOIN AR_SET_Customer BC ON BC.CustomerID = [SAL_InvoiceHeader].billedtoid 
  --AND BC.CompanyID = [SAL_InvoiceHeader].CompanyID
   AND AR_SET_Customer.PeriodID = [SAL_InvoiceHeader].PeriodID 
   
 INNER JOIN SET_Company ON SET_Company.CompanyId=[SAL_InvoiceHeader].CompanyID
 AND SET_Company.Status=1
 INNER JOIN SET_OperatingUnit On SET_OperatingUnit.OperatingUnitId=SET_Company.OperatingUnitId AND SET_OperatingUnit.Status=1
 INNER JOIN SET_Business On SET_Business.BusinessId=SET_Company.BusinessId and SET_Business.IsActive=1
 INNER Join Set_DocumentType On Set_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
        And Set_DocumentType.CompanyID = SAL_InvoiceHeader.CompanyID And Set_DocumentType.PeriodID = SAL_InvoiceHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
 
 WHERE [SAL_InvoiceHeader].[Status] = 1 AND ([SAL_InvoiceHeader].[CompanyID] = @CompanyID OR @CompanyID is null) AND ([SAL_InvoiceHeader].[PeriodID] = @PeriodID OR @PeriodID is null)";
                #endregion

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SalesInvoiceTrdReportOne) + filter, sqlparams);

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


        public static DataTable GetSaleInvoiceParameter(int companyID, int periodID,int FiscalID, SqlTransaction objSQL_Transaction = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[3];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);
                sqlParams[2] = new SqlParameter("@FiscalID", FiscalID);


                const string Query_GetSaleInvoiceParameter = @"SELECT * from Fabrics_InvoiceParameters where CompanyID = @CompanyID and PeriodID = @PeriodID and FiscalID = @FiscalID";

                if (objSQL_Transaction != null)
                {
                    ds = SqlHelper.ExecuteDataset(objSQL_Transaction, CommandType.Text, Query_GetSaleInvoiceParameter,
                        sqlParams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSaleInvoiceParameter, sqlParams);

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


        public static DataTable GetFabric_Sal_InvoiceHeader(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                   // OrderBy = "Order By sal_invoiceheader.InvoiceID";

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                   // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Pagings, Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagings + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Fabric_Sal_InvoiceHeader + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable CustomerSaleLedger(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Guid userID, int CustomerID = 0)
        {
            
           
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];
                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);
                sqlParams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlParams[3] = new SqlParameter("@ToDate", dateTo);
                sqlParams[4] = new SqlParameter("@UserID", userID);
                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" And convert(date,SAL_InvoiceHeader.InvoiceDate) between @FromDate AND @ToDate ");
                }
                else if (dateFrom != null)
                {
                    sb.Append(" and convert(date,SAL_InvoiceHeader.InvoiceDate) = @FromDate ");
                }
                else if (dateTo == null)
                {
                    sb.Append(" and convert(date,SAL_InvoiceHeader.InvoiceDate) = @ToDate");
                }

                if (CustomerID != 0)
                {
                    sb1.Append(" and [AR_SET_Customer].CustomerID = " + CustomerID + "");
                    sb2.Append(" where Tab.customerID = " + CustomerID + "");
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetCustomerSaleLedger, sb.ToString(), sb1.ToString(), sb2.ToString()), sqlParams);

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

        public static DataRow Get_CustomerAdvances(int companyID, int periodID, int FiscalID, int CustomerID)
        {
            DataRow drToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);

                string query = @" select sum(AR_ReceiptCustomerDetail.Amount) -
(
select isnull(sum(Gl_transactionDetail.Amount),0) from Sal_invoiceHeader 
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Sal_invoiceHeader.CustomerID 
inner join Gl_transactionHeader on Gl_transactionHeader.DocumentNo = Sal_invoiceHeader.invoicenumber 
and Gl_transactionHeader.DocumentTypeID = Sal_invoiceHeader.invoiceType
and Gl_transactionHeader.FiscalID = Sal_invoiceHeader.FiscalID
inner join Gl_transactionDetail on Gl_transactionDetail.TransactionID = Gl_transactionHeader.TransactionID
and Gl_transactionDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
where AR_SET_BusinessPartnerTypes.TypeCode = 16
and Sal_invoiceHeader.CustomerID = AR_ReceiptCustomerDetail.CustomerID
--and Gl_transactionHeader.FiscalID = @FiscalID
) as AdvanceAmount 
from AR_ReceiptTransactionHeader
inner join AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
--and AR_ReceiptTransactionHeader.DocumentTypeID in (select DocumentTypeID from Set_documentTYpe where DocumentType like '%Direct Receipt (Advance)%')
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_ReceiptCustomerDetail.CustomerID
and AR_SET_BusinessPartnerTypes.TypeCode = 16
where AR_ReceiptCustomerDetail.CustomerID = @CustomerID
and AR_ReceiptTransactionHeader.CompanyID = @CompanyID
and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
--and AR_ReceiptTransactionHeader.FiscalID = @FiscalID
and AR_ReceiptCustomerDetail.BusinessPartnerTypeID = 16
group by AR_ReceiptCustomerDetail.CustomerID

";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static bool DeleteRates_Permanent_By_PrimaryKey(int companyID, int periodID, int invoiceID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceID);

                const string Query_Delete_Permanent_By_PrimaryKey = @" DELETE FROM Fabrics_SAL_InvoiceProcessRates
WHERE InvoiceHeaderID = @InvoiceID";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_Permanent_By_PrimaryKey, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }
        public static DataTable GetAllSalesTaxInvoiceAL_Imdad(int CompanyID, int PeriodID, int InvoiceTypeID, int? vendorFilter, int? invoiceFrom, int? invoiceTo, int CurrentFiscalID)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@InvoiceFrom", invoiceFrom);
                sqlparams[3] = new SqlParameter("@InvoiceTo", invoiceTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (invoiceFrom.HasValue && invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  between @InvoiceFrom AND @InvoiceTo");
                else if (invoiceFrom.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                else if (invoiceTo.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceNumber  = @InvoiceFrom ");
                if (InvoiceTypeID > 0)
                    filter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + InvoiceTypeID);
                if (vendorFilter > 0)
                    filter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);




                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesTaxInvoice_Alimdad) + filter, sqlparams);

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

        public static bool UpdateIsInvoiceAdjusted(int companyID, int periodID, int paymentid, bool status)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PID", paymentid);
                sqlparams[3] = new SqlParameter("@status", status);



                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsInvoiceAdjusted, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        , Query_SelectAll_Searching_WithPaging + strWhere
                       ) + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparams).Tables[0];

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

        public static DataRow GetlocationByInvoiceID(int companyID, int invoiceID, int periodID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@InvoiceID", invoiceID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;



                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetLocation_byinvoice, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetLocation_byinvoice, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLocation_byinvoice, sqlparams);

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


        public static DataRow GetDocumentTypeID_ForInvoice(int CurrentCompanyID, int CurrentPeriodID, string DocumentType, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);

                DataSet ds = null;

                const string Query_GetDocType_byDispatch = @"Select * from SET_DocumentType  
                        Where  Status=1 And SET_DocumentType.PeriodID=@PeriodID
                        And SET_DocumentType.CompanyID=@CompanyID
                        ";
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_GetDocType_byDispatch + DocumentType.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetDocType_byDispatch + DocumentType.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDocType_byDispatch + DocumentType.ToString()), sqlparams);

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

        public bool InProcessDocument(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessInvoice");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);
                GL_TransactionDetail.DeletePermenantDataByKeyTranasctionID(sqltrans, CompanyID, PeriodID, th.TransactionID);
                GL_TransactionHeader.Delete_By_PrimaryKey(CompanyID, PeriodID, th.TransactionID, sqltrans);
                SET_DocumentApprove.DeleteApprovalHistroy(CompanyID, PeriodID, int.Parse(this.InvoiceNumber), this.InvoiceType, sqltrans, int.Parse(this.FiscalID.ToString()));
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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessInvoice");
            }

            return result;
        }

        public static DataTable Get_AllDocumentNosForInprocess(int companyID, int periodID, int documentTypeID, int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                string Query_Get_AllDocumentNos = @" SELECT SAL_InvoiceHeader.InvoiceNumber As DocumentNo FROM SAL_InvoiceHeader
    inner join Set_PeriodCustom on  
 SAL_InvoiceHeader.InvoiceDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE SAL_InvoiceHeader.CompanyID = @CompanyID AND SAL_InvoiceHeader.PeriodID = @PeriodID AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID 
AND SAL_InvoiceHeader.Status = 1 AND SAL_InvoiceHeader.StatusCode = 1 AND IsNull(SAL_InvoiceHeader.IsReversed, 0) = 0 AND SAL_InvoiceHeader.ReversalID IS NULL 
AND SAL_InvoiceHeader.InvoiceID not in (SELECT AR_ReceiptInvoicesDetail.InvoiceHeaderID FROM  AR_ReceiptTransactionHeader
INNER JOIN  AR_ReceiptInvoicesDetail ON AR_ReceiptInvoicesDetail.receiptHeaderID=AR_ReceiptTransactionHeader.ReceiptTransactionID
AND AR_ReceiptInvoicesDetail.CompanyID=AR_ReceiptTransactionHeader.CompanyID
AND AR_ReceiptInvoicesDetail.PeriodID=AR_ReceiptTransactionHeader.PeriodID
AND AR_ReceiptInvoicesDetail.Status=1
inner join Set_PeriodCustom on AR_ReceiptTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 )
ORDER BY Convert(BigInt, SAL_InvoiceHeader.InvoiceNumber) ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllDocumentNos, sqlparams);

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

        public static DataTable GetDataSaleReportUnderSalesTaxAct1990(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SaleReportUnderSalesTaxAct1990.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable GetDataSaleReturnReportUnderSalesTaxAct1990(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", ItemCatalogID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SaleReturnReportUnderSalesTaxAct1990.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable GetDispatchData(int DocumentTypeID, int CompanyID, int PeriodID, int FiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<int> DocumentNumber = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append( " and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                }
                const string Query = @"select * from INV_StockTransactionHeader
where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
{0}
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query,sb), sqlparams);

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

        public static DataTable GetGatePassData(int DocumentTypeID, int CompanyID, int PeriodID, int FiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<int> DocumentNumber = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and Gate_OutwardHeader.DocumentNo = " + DocumentNumber + "");
                }
                const string Query = @"select * from Gate_OutwardHeader
where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID
{0}
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams);

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

        public static DataTable GetStockReturnData(int DocumentTypeID, int CompanyID, int PeriodID, int FiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<int> DocumentNumber = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                }
                const string Query = @"select * from INV_StockTransactionHeader
where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
{0}
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams);

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

        public static DataTable GetInventoryInOutData(int DocumentTypeID, int CompanyID, int PeriodID, int FiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<int> DocumentNumber = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                }
                const string Query = @"select * from INV_StockTransactionHeader
where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
{0}
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams);

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

        public static DataTable GetFinishGoodsData(int DocumentTypeID, int CompanyID, int PeriodID, int FiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<int> DocumentNumber = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                }
                const string Query = @"select * from INV_StockTransactionHeader
where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
{0}
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb), sqlparams);

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

        public static DataSet Get_DirectReport(int? DocumentTypeID, int InvoiceFrom, int InvoiceTo, int companyID, int periodID, int FiscalID = 0)
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@InvoiceFrom", InvoiceFrom);
                sqlparams[1] = new SqlParameter("@InvoiceTo", InvoiceTo);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);//@CurrentFiscalID
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (InvoiceFrom > 0 && InvoiceTo > 0)
                    sb.Append(" AND InvoiceNumber between @InvoiceFrom AND @InvoiceTo ");
                else if (InvoiceFrom > 0 && InvoiceTo == 0)
                    sb.Append(" AND InvoiceNumber = @InvoiceFrom ");
                else if (InvoiceFrom == 0 && InvoiceTo > 0)
                    sb.Append(" AND InvoiceNumber = @InvoiceTo ");
                if (DocumentTypeID > 0)
                    sb.Append(" AND InvoiceType = " + DocumentTypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_InvoiceHeader.Query_GetSalesInvoiceForCUstomer, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ds.Tables[1].TableName = "tblSupplier";
                    ds.Tables[2].TableName = "tblAccountCodes";
                    ds.Tables[3].TableName = "tblGLDetails";
                    dsToReturn = ds;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable NewSales_Register_Report_Molasses(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0,bool ISSheikhoo = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Molas%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);
                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesInvoiceMolasses;
                if(ISSheikhoo == true)
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SalesINvoiceMolasses_Shiekhoo; 

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, whereFilter).ToString(), sqlparams);

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

        public static DataTable NewSales_Register_Report_Molasses2(int companyID, int periodID, int? vendorFilter, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool ISSheikhoo = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                if (vendorFilter > 0)
                    whereFilter.Append(" AND [SAL_InvoiceHeader].CustomerID  = " + vendorFilter);

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Molas%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);
                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesInvoiceMolasses;
                if (ISSheikhoo == true)
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SalesINvoiceMolasses_Shiekhoo;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, whereFilter).ToString(), sqlparams);

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

        public static DataTable NewSales_Register_Report_Mud(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool ISSheikhoo = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (SAL_InvoiceHeader.InvoiceNumber <  @InvoiceNoTo  ) ");

                //if (documentTypeID.HasValue)
                //    whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceType = " + documentTypeID.Value);

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                //whereFilter.Append(" AND SAL_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Molas%' ");


                //whereFilter.Append("  Order By TAB.CustomerCategory, TAB.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                //sqlparams[8] = new SqlParameter("@InvoiceType", documentTypeID.Value);
                string query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SalesInvoiceMolasses;
                if (ISSheikhoo == true)
                    query = FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SalesINvoiceMolasses_Shiekhoo;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, whereFilter).ToString(), sqlparams);

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


        public static bool DeleteSInvHeaderAndDetail(string InvoiceType, string Reason, string RequestedBy, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Guid UpdatedBy, string UpdatedByValue, string FromNo, string ToNo)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteInvoice");
            StringBuilder type = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[11];
                //sqlparams[0] = new SqlParameter("@InvoiceType", InvoiceType);
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                sqlparams[5] = new SqlParameter("@Reason", Reason);
                sqlparams[6] = new SqlParameter("@RequestedBy", RequestedBy);
                sqlparams[7] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", UpdatedByValue);
                sqlparams[9] = new SqlParameter("@FromNo", FromNo);
                sqlparams[10] = new SqlParameter("@ToNo", ToNo);



                type.Append(InvoiceType);

                const string Query = @"
                  update SAL_InvoiceDetail set Status = 0,UpdatedBy = @UpdatedBy,UpdateDate = getdate(),UpdatedByValue = @UpdatedByValue
where InvoiceHeaderID in (
select InvoiceID from SAL_InvoiceHeader 
where InvoiceType in ({0}) and InvoiceDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and InvoiceNumber between @FromNo and @ToNo
and SAL_InvoiceHeader.InvoiceID not in
(
select isnull(SourceHeaderID,0) as SourceHeaderID from AR_ReceiptTransactionHeader inner join SET_DocumentType
on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
and SET_DocumentType.Status = AR_ReceiptTransactionHeader.Status
where AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.FiscalID = @FiscalID and SET_DocumentType.DocumentType like '%Receipt against sale invoices%'
)
) and CompanyID = @CompanyID and PeriodID = @PeriodID

update SAL_InvoiceTaxPayment set Status = 0,UpdatedBy = @UpdatedBy,UpdateDate = getdate(),UpdatedByValue = @UpdatedByValue
where InvoiceID in (
select InvoiceID from SAL_InvoiceHeader 
where InvoiceType in ({0}) and InvoiceDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and InvoiceNumber between @FromNo and @ToNo
and SAL_InvoiceHeader.InvoiceID not in
(
select isnull(SourceHeaderID,0) as SourceHeaderID from AR_ReceiptTransactionHeader inner join SET_DocumentType
on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
and SET_DocumentType.Status = AR_ReceiptTransactionHeader.Status
where AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.FiscalID = @FiscalID and SET_DocumentType.DocumentType like '%Receipt against sale invoices%'
)
) and CompanyID = @CompanyID and PeriodID = @PeriodID 

update SAL_InvoiceHeader set Status = 0 , Description = @Reason , CustomerAddress = @RequestedBy,UpdatedBy = @UpdatedBy,UpdateDate = getdate(),UpdatedByValue = @UpdatedByValue
where InvoiceType in ({0}) and InvoiceDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and InvoiceNumber between @FromNo and @ToNo
and SAL_InvoiceHeader.InvoiceID not in
(
select isnull(SourceHeaderID,0) as SourceHeaderID from AR_ReceiptTransactionHeader inner join SET_DocumentType
on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
and SET_DocumentType.Status = AR_ReceiptTransactionHeader.Status
where AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.FiscalID = @FiscalID and SET_DocumentType.DocumentType like '%Receipt against sale invoices%'
)

update GL_TransactionDetail set Status = 0,UpdatedBy = @UpdatedBy,UpdateDate = getdate(),UpdatedByValue = @UpdatedByValue
where TransactionID in (
select TransactionID from GL_TransactionHeader
where DocumentTypeID in ({0}) and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and DocumentNo between @FromNo and @ToNo
and GL_TransactionHeader.DocumentNo not in 
(
select InvoiceNumber from SAL_InvoiceHeader 
where InvoiceType in ({0}) and InvoiceDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and InvoiceNumber between @FromNo and @ToNo
and SAL_InvoiceHeader.InvoiceID in
(
select isnull(SourceHeaderID,0) as SourceHeaderID from AR_ReceiptTransactionHeader inner join SET_DocumentType
on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
and SET_DocumentType.Status = AR_ReceiptTransactionHeader.Status
where AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.FiscalID = @FiscalID and SET_DocumentType.DocumentType like '%Receipt against sale invoices%'
)
)
) and CompanyID = @CompanyID and PeriodID = @PeriodID 


update GL_TransactionHeader set Status = 0,UpdatedBy = @UpdatedBy,UpdateDate = getdate(),UpdatedByValue = @UpdatedByValue
where DocumentTypeID in ({0}) and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and DocumentNo between @FromNo and @ToNo
and GL_TransactionHeader.DocumentNo not in 
(
select InvoiceNumber from SAL_InvoiceHeader 
where InvoiceType in ({0}) and InvoiceDate between @FromDate and @ToDate and CompanyID = @CompanyID
and PeriodID = @PeriodID and FiscalID = @FiscalID
and InvoiceNumber between @FromNo and @ToNo
and SAL_InvoiceHeader.InvoiceID in
(
select isnull(SourceHeaderID,0) as SourceHeaderID from AR_ReceiptTransactionHeader inner join SET_DocumentType
on SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID
and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID
and SET_DocumentType.Status = AR_ReceiptTransactionHeader.Status
where AR_ReceiptTransactionHeader.Status = 1 and AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID = @PeriodID
and AR_ReceiptTransactionHeader.FiscalID = @FiscalID and SET_DocumentType.DocumentType like '%Receipt against sale invoices%'
)
)
                ";

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query,type), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(Query, type), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteInvoice");
            }

            return result;
        }

        public static bool DispatchInProcess(int DocumentTypeID, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Nullable<int> DocumentNumber = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                    sb2.Append(" and SAL_DispatchHeader.DispatchNo = " + DocumentNumber + "");
                }
             
                const string Query = @"
                
                update INV_StockTransactionHeader set StatusCode = 5
                where DocumentTypeID = @DocumentTypeID and DocumentDate between @FromDate and @ToDate and CompanyID = @CompanyID
                and PeriodID = @PeriodID and FiscalID = @FiscalID
                {0}
                
                update SAL_DispatchHeader set StatusCode = 5
                where DocumentTypeID = @DocumentTypeID and DispatchDate between @FromDate and @ToDate and CompanyID = @CompanyID
                and PeriodID = @PeriodID and FiscalID = @FiscalID
                {1}
                
                ";

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query,sb,sb2), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(Query, sb, sb2), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcess");
            }

            return result;
        }

        public static bool GatePassInProcess(int DocumentTypeID, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Nullable<int> DocumentNumber = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and Gate_OutwardHeader.DocumentNo = " + DocumentNumber + "");
                    sb2.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                }

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GatePassInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GatePassInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcess");
            }

            return result;
        }

        public static bool StockReturnInProcess(int DocumentTypeID, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Nullable<int> DocumentNumber = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {                    
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                    sb2.Append(" and GL_TransactionHeader.DocumentNo = " + DocumentNumber + "");
                }

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_StockReturnInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_StockReturnInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcess");
            }

            return result;
        }

        public static bool InventoryInOutInProcess(int DocumentTypeID, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Nullable<int> DocumentNumber = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                    sb2.Append(" and GL_TransactionHeader.DocumentNo = " + DocumentNumber + "");
                }

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_InventoryInOutInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_InventoryInOutInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcess");
            }

            return result;
        }

        public static bool FinishGoodsInProcess(int DocumentTypeID, DateTime FromDate, DateTime ToDate, int CompanyID, int PeriodID, int FiscalID, Nullable<int> DocumentNumber = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcess");
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                if (DocumentNumber != null)
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo = " + DocumentNumber + "");
                    sb2.Append(" and GL_TransactionHeader.DocumentNo = " + DocumentNumber + "");
                }

                if (sqltrans == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_FinishGoodsInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_FinishGoodsInProcess, sb, sb2), sqlparams) > 0)
                        result = true;
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcess");
            }

            return result;
        }


        public static DataTable GetAllDataSalesTaxInvoiceCancellationReport(int companyID, int periodID, DateTime? Date_From, DateTime? Date_To, int? StatusID)
        {
            DataTable dt = null;
            DataSet ds = null;
          //  StringBuilder sb = new StringBuilder(5);
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (StatusID.HasValue)
                {
                    sqlparams[4] = new SqlParameter("@StatusID", StatusID.ToString());
                }
                else
                {
                    sqlparams[4] = new SqlParameter("@StatusID", DBNull.Value);
                }

                //if (CustomerID > 0)
                //{
                //    sb.Append(" and AR_SET_Customer.CustomerID =" + CustomerID);
                //}
                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllSalesTaxInvoiceCancellationReport, sqlparams);

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


        public static DataTable GetProcessParameter(string ProcessNam)
        {

            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@CompanyID", 1);
                sqlparams[1] = new SqlParameter("@ProcessNam", ProcessNam);


                string Query_GetProcessParameter = @" select * from Set_Process where ID = (select top 1 ID from Set_Process order by ID desc  ) and RunningMode = 1 and ProcessName = @ProcessNam ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetProcessParameter, sqlparams);

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


        public static DataTable SaleInvoiceDetailWithCNIC(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> ContractNo = null,
         Nullable<int> DoNo = null, string CNICNo = "", Nullable<int> DealerID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (ContractNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.ContractNo = " + ContractNo.Value);
                }

                if (DoNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.DeliveryOrderNo = " + DoNo.Value);
                }

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (CNICNo != "")
                    whereFilter.Append(" AND FinalTAB.CNICNumber = '" + CNICNo + "' ");

                if (DealerID.HasValue )
                    whereFilter.Append(" AND FinalTAB.DealerID = " + DealerID.Value.ToString() + " ");
                


                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleInvoiceDetailWithCNIC , whereFilter.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleInvoiceDetailWithCNIC , whereFilter.ToString()), sqlparams);
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

        public static DataTable GetNewTaxInvoice1_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoice1Report_HSM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice1_Ethanol_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID, Nullable<short> statusCode)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%ethanol%'");

                if (statusCode.HasValue)
                    filter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_NewTaxInvoice1Report_HSM_Ethanol) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewCommercialInvoiceL_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like ''"); //%Sales%Invoice%Sugar%

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewCommercialInvoiceL_HSM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewCommercialInvoiceE_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like ''");//%Sales%Invoice%Sugar%

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewCommercialInvoiceE_HSM) + filter.ToString(), sqlparams);

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


        public static DataTable GetNewTaxInvoice1_HSMForSaleInvoiceProcess(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, string Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber in " + Invoiceno.ToString());

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                else
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_NewTaxInvoice1Report_HSM) + filter.ToString(), sqlparams);

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

        public bool SalesInvoiceClose(int Companyid, int perioid, int InvoiceTypeID, int InvoiceID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@InvoiceTypeID", InvoiceTypeID);
                sqlparams[3] = new SqlParameter("@InvoiceID", InvoiceID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);

                const string Query_DOClose = @"
UPDATE SAL_InvoiceHeader
SET IsCancelled = 1 ,  Remarks =  @remarks
where SAL_InvoiceHeader.InvoiceType =@InvoiceTypeID AND SAL_InvoiceHeader.InvoiceID = @InvoiceID
AND SAL_InvoiceHeader.CompanyID = @CompanyID  AND SAL_InvoiceHeader.PeriodID = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DOClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DOClose, sqlparams) > 0)
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

        public static DataTable GetInvoiceByOrderID(int CompanyID, int PeriodID, int CurrentFiscalID, int OrderID, string DocumentTypeID, int Contractno)
        {
            DataTable dtToReturn = null;

            StringBuilder filter = new StringBuilder();

            try
            {

                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@OrderID", OrderID);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[5] = new SqlParameter("@Contractno", Contractno);

                string Query = @"select * from SAL_InvoiceHeader where CompanyID = @CompanyID and PeriodID = @PeriodID and FiscalID = @CurrentFiscalID and SalOrderID = @OrderID 
and SalContractNo = @Contractno and SalContractTypeID = @DocumentTypeID and status = 1";

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



        public static DataTable GetAgeAnalysisSummaryData(int companyID, int periodID, DateTime AsOnDate, Nullable<int> CustomerID, Nullable<int> BucketID)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(AsOnDate, true) + " 23:59:59"));
                sqlparams[3] = new SqlParameter("@BucketID", BucketID);

                if (CustomerID.HasValue)
                {
                    strFilter = "and SAL_InvoiceHeader.CustomerID = " + CustomerID.Value;
                }

                if (BucketID.HasValue)
                {
                    strFilter = " and SET_Bucket.BucketID = " + BucketID.Value;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderHeader.Query_AgeAnalysisSummary, strFilter), sqlparams);

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

        public static DataTable GetAdvanceAndReceiptAgainstCustomer(int companyID, int periodID, int CustomerID, DateTime AsOnDate)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;
            string strFilterBroker = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                sqlparams[3] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(AsOnDate, true) + " 23:59:59"));

                const string Query_GetAdvanceAndReceiptAgainstCustomer = @"
select CustomerID,CustomerName,sum(Amount) as Amount from (

select CustomerID,CustomerName,sum(Amount) as Amount
from (
 -- Direct Receipt
	SELECT distinct SET_DocumentType.DocumentType, AR_SET_Customer.CustomerName , AR_ReceiptTransactionHeader.DocumentNo as InvoiceNumber ,AR_ReceiptTransactionHeader.DocumentDate as invoiceDate,
        isnull(sum(AR_ReceiptTransactionHeader.Amount),0) as Amount, 
		
        AR_SET_Customer.CustomerID
		
	FROM AR_ReceiptTransactionHeader 
	
  inner join AR_ReceiptCustomerDetail on AR_ReceiptCustomerDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID
  and AR_ReceiptCustomerDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID
  and AR_ReceiptCustomerDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID
  and AR_ReceiptCustomerDetail.Status = 1

  INNER JOIN AR_SET_Customer ON AR_ReceiptCustomerDetail.CustomerID = AR_SET_Customer.CustomerID 
            AND AR_ReceiptCustomerDetail.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1 
             


        Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = AR_ReceiptTransactionHeader.DocumentTypeID 
            and SET_DocumentType.CompanyID = AR_ReceiptTransactionHeader.CompanyID and SET_DocumentType.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
			and SET_DocumentType.DocumentType like '%direct%'
            
	WHERE AR_ReceiptTransactionHeader.CompanyID = @CompanyID AND AR_ReceiptTransactionHeader.PeriodID = @PeriodID AND AR_ReceiptTransactionHeader.DocumentDate <= @AsOnDate
	And AR_ReceiptTransactionHeader.Status = 1 AND AR_ReceiptTransactionHeader.StatusCode = 1 
        and AR_ReceiptTransactionHeader.ReversalDate is null and AR_ReceiptTransactionHeader.ReversalComments is null
        and AR_SET_Customer.CustomerID = @CustomerID
	group by  AR_SET_Customer.CustomerName , AR_ReceiptTransactionHeader.DocumentNo ,AR_ReceiptTransactionHeader.DocumentDate 
	,AR_SET_Customer.CustomerID,SET_DocumentType.DocumentType,AR_ReceiptTransactionHeader.SourceHeaderID
	
	)as tab

	group by CustomerID,CustomerName
	

	union all
select AR_SET_Customer.CustomerID,AR_SET_Customer.CustomerName,FEDAmount as Amount
from AP_AdvanceSugarPayment
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_AdvanceSugarPayment.CustomerID
where AP_AdvanceSugarPayment.CompanyID = @CompanyID
and AP_AdvanceSugarPayment.Status = 1
AND AP_AdvanceSugarPayment.RecievingDate <= @AsOnDate
and AR_SET_Customer.CustomerID = @CustomerID
) as tab
group by CustomerID,CustomerName
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAdvanceAndReceiptAgainstCustomer, sqlparams);

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

        public static DataTable SugarSale_ReportEthanol(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");

                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds;

                //if (IsSheikhooReport)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_New_Sales_Register_Report_SKU, whereFilter.ToString()), sqlparams);
                //else
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_New_Sales_Register_Report, whereFilter.ToString()), sqlparams);

                if (IsHabibSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.SugarSale_HabibEthnol, whereFilter.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.SugarSale_HabibEthnol, whereFilter.ToString()), sqlparams);


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


        public static DataTable SaleInvoiceDetailWithCNIC_Steel(int companyID, int periodID, int invoiceNoFrom, int invoiceNoTo, Nullable<short> statusCode,
string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> ContractNo = null,
         Nullable<int> DoNo = null, string CNICNo = "", Nullable<int> DealerID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            DateTime ConditionDate = DateTime.Parse("01-07-2019");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom > 0)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo > 0)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }
                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append("  FinalTAB.DocumentTypeID in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND FinalTAB.DocumentType not like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND FinalTAB.StatusCode = " + statusCode.Value);

                if (ContractNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.ContractNo = " + ContractNo.Value);
                }

                if (DoNo > 0)
                {
                    whereFilter.Append(" AND FinalTAB.DeliveryOrderNo = " + DoNo.Value);
                }

                whereFilter.Append(" AND FinalTAB.InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND FinalTAB.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (CNICNo != "")
                    whereFilter.Append(" AND FinalTAB.CNICNumber = '" + CNICNo + "' ");

                if (DealerID.HasValue)
                    whereFilter.Append(" AND FinalTAB.DealerID = " + DealerID.Value.ToString() + " ");



                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds = null;
                if (dateTo < ConditionDate)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleInvoiceDetailWithCNIC_Steel, whereFilter.ToString()), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_SaleInvoiceDetailWithCNIC_Steel, whereFilter.ToString()), sqlparams);
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


        public static DataTable SugarSale_Report_Bassgas(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");

                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                DataSet ds;

                
                if (IsHabibSugar)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.SugarSale_Baggas, whereFilter.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.SugarSale_Baggas, whereFilter.ToString()), sqlparams);


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


        public static DataTable GetNewTradingTaxInvoice_HSM(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, int? Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (Invoiceno > 0)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber=" + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.Query_NewTradingTaxInvoiceReport_HSM) + filter.ToString(), sqlparams);

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


        public static DataTable SugarSale_ReportTrading(int companyID, int periodID, Nullable<int> invoiceNoFrom, Nullable<int> invoiceNoTo, Nullable<short> statusCode,
   string documentTypeID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, Nullable<int> BrokerID, Nullable<int> CustomerID, short? documentGroupID = null, int CurrentFiscalID = 0, bool IsSheikhooReport = false, bool IsHabibSugar = false, bool CustomerParty = false)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (invoiceNoFrom.HasValue)
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", invoiceNoFrom);
                }
                else
                {
                    sqlparams[2] = new SqlParameter("@InvoiceNoFrom", DBNull.Value);
                }
                if (invoiceNoTo.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", invoiceNoTo);
                }
                else
                {
                    sqlparams[3] = new SqlParameter("@InvoiceNoTo", DBNull.Value);
                }

                if (invoiceNoFrom.HasValue && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber Between  @InvoiceNoFrom and  @InvoiceNoTo ) ");
                else if (invoiceNoFrom.HasValue && (!invoiceNoTo.HasValue))
                    whereFilter.Append(" AND (InvoiceNumber >  @InvoiceNoFrom  ) ");
                else if ((!invoiceNoFrom.HasValue) && invoiceNoTo.HasValue)
                    whereFilter.Append(" AND (InvoiceNumber <  @InvoiceNoTo  ) ");

                if (documentTypeID.ToString() != string.Empty)
                    whereFilter.Append(" AND InvoiceType in " + documentTypeID.ToString());
                else
                    whereFilter.Append(" AND InvoiceType like '%Sales%Invoice%Sugar%' ");

                if (statusCode.HasValue)
                    whereFilter.Append(" AND SAL_InvoiceHeader.StatusCode = " + statusCode.Value);


                whereFilter.Append(" AND InvoiceDate between @FromDate and @ToDate");

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND GroupID = " + documentGroupID.Value.ToString() + " ");



                //whereFilter.Append("  Order By CustomerCategory, SAL_InvoiceHeader.InvoiceNumber");

                sqlparams[4] = new SqlParameter("@FromDate", dateFrom.Value);
                sqlparams[5] = new SqlParameter("@ToDate", dateTo.Value);
                sqlparams[6] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                if (BrokerID.HasValue)
                    sqlparams[7] = new SqlParameter("@BrokerID", BrokerID);
                else
                    sqlparams[7] = new SqlParameter("@BrokerID", DBNull.Value);

                if (CustomerID.HasValue)
                    sqlparams[8] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[8] = new SqlParameter("@CustomerID", DBNull.Value);

                DataSet ds;

                //if (IsSheikhooReport)
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader3.Query_New_Sales_Register_Report_SKU, whereFilter.ToString()), sqlparams);
                //else
                //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_New_Sales_Register_Report, whereFilter.ToString()), sqlparams);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader5.SugarSaleTrading_Habib, whereFilter.ToString()), sqlparams);
                


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


        public static decimal Get_DispatchRemaningQuantity(int companyID, int periodID, int StockHeaderID, long StockDetailID, SqlTransaction objTransaction = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            string Query_Get_DispatchRemaningQuantity = @" SELECT 
SUM(ABS(TransactionQuantity)) - 
ISNULL((Select Sum(SAL_InvoiceDetail.QuantityInvoiced) from Sal_InvoiceHeader 
INNER JOIN SAL_InvoiceDetail on SAL_InvoiceDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID 
AND SAL_InvoiceDetail.CompanyID = SAL_InvoiceHeader.CompanyID AND SAL_InvoiceDetail.PeriodID = sal_InvoiceHeader.PeriodID 
Where Sal_InvoiceDetail.Status = 1 AND SAL_InvoiceHeader.CompanyID = @CompanyId  AND SAL_InvoiceHeader.PeriodID = @PeriodID  AND Sal_InvoiceHeader.Status = 1
AND SAL_InvoiceDetail.StockHeaderID = @StockHeaderID AND SAL_InvoiceDetail.StockDetailID = @StockDetailID),0) AS Amount

 FROM dbo.INV_StockTransactionDetail WHERE StockTransactionID = @StockHeaderID AND TransactionDetailID = @StockDetailID
AND CompanyID = @CompanyId AND PeriodID = @PeriodID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockHeaderID", StockHeaderID);
                sqlparams[1] = new SqlParameter("@StockDetailID", StockDetailID);
                sqlparams[2] = new SqlParameter("@CompanyId", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (objTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_DispatchRemaningQuantity, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Get_DispatchRemaningQuantity, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(!string.IsNullOrEmpty(dt.Rows[0][0].ToString()) ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static DataTable GetNewTaxInvoice_HSM_ForSaleInvoiceProcess(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, string Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (!string.IsNullOrEmpty(Invoiceno))
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber IN " + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_HSM) + filter.ToString(), sqlparams);

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

        public static DataTable GetNewTaxInvoice_SKD_ForSaleInvoiceProcess(int CompanyID, int PeriodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int? Supplier, int CurrentFiscalID, int? dispatchno, string Invoiceno, int? Contractno, string DocumentTypeID)
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
                    filter.Append(" and Cast(SAL_InvoiceHeader.InvoiceDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (Supplier > 0)
                    filter.Append("AND [AR_SET_Customer].CustomerID = " + Supplier);

                if (dispatchno > 0)
                    filter.Append(" And [SAL_DispatchHeader].DispatchNo= " + dispatchno);

                if (!string.IsNullOrEmpty(Invoiceno))
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceNumber IN " + Invoiceno);

                if (Contractno > 0)
                    filter.Append(" And [SAL_OrderHeader].ContractNo=" + Contractno);

                if (DocumentTypeID.ToString() != string.Empty)
                    filter.Append(" And [SAL_InvoiceHeader].InvoiceType in " + DocumentTypeID.ToString());
                //else
                //    filter.Append(" And [SAL_InvoiceHeader].InvoiceType like '%Sales%Invoice%Sugar%'");

                filter.Append("  Order BY SAL_InvoiceHeader.InvoiceNumber");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader4.Query_NewTaxInvoiceReport_SKD) + filter.ToString(), sqlparams);

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

        public static DataRow GetWebAPIandToken(SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", "");

                DataSet ds = null;

                const string Query_GetWebAPIandToken = @" select * from Set_FBRAPI  ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetWebAPIandToken, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetWebAPIandToken, sqlparams);

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

        public static DataSet Get_Invoice_By_DocumentTypeForFBR(int companyID, int periodID, int documentTypeID, int FiscalID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@DateFrom", DateFrom);
                sqlparams[5] = new SqlParameter("@DateTo", DateTo);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Invoice_By_DocumentTypeForFBR, sqlparams);
                
                ds.Tables[0].TableName = "Table";
                ds.Tables[1].TableName = "Table1";

            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds;
        }

        public static DataSet CheckDigitalInvoiceingStatus(int companyID, int periodID, int documentTypeID, int FiscalID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null)
        {
            DataSet ds = null;

            const string Query_CheckDigitalInvoiceingStatus = @" SELECT SAL_InvoiceHeader.InvoiceNumber,SET_DocumentType.DocumentType,SAL_InvoiceHeader.InvoiceDate, SAL_InvoiceResponse.*
FROM SAL_InvoiceResponse
INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = SAL_InvoiceResponse.SalesInvoiceID
AND SAL_InvoiceHeader.CompanyID = SAL_InvoiceResponse.InvoiceCompanyID
AND SAL_InvoiceHeader.FiscalID = SAL_InvoiceResponse.InvoiceFiscalID
INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
AND SET_DocumentType.DocumentTypeID = SAL_InvoiceHeader.InvoiceType
WHERE SAL_InvoiceHeader .CompanyID = @CompanyID
AND SAL_InvoiceHeader.InvoiceType = @DocumentTypeID
AND convert(date,[SAL_InvoiceHeader].[InvoiceDate]) Between @DateFrom AND @DateTo
and SAL_InvoiceResponse.payload is not null
";


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[4] = new SqlParameter("@DateFrom", DateFrom);
                sqlparams[5] = new SqlParameter("@DateTo", DateTo);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckDigitalInvoiceingStatus, sqlparams);

                ds.Tables[0].TableName = "Table";


            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds;
        }

        public static DataRow GetActualToken(SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@UserID", "");

                DataSet ds = null;

                const string Query_GetActualToken = @" select * from Set_FBRToken";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetActualToken, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetActualToken, sqlparams);

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

        public static DataRow GetInvoiceStatusByDispatchID(int companyID, int StockTransactionID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@companyID", companyID);
                sqlparams[1] = new SqlParameter("@StockTransactionID", StockTransactionID);

                DataSet ds = null;

                const string Query_GetInvoiceStatusByDispatchID = @" SELECT * FROM dbo.SAL_InvoiceDetail WHERE CompanyID = @companyID and Status= 1 AND  StockHeaderID = @StockTransactionID ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetInvoiceStatusByDispatchID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetInvoiceStatusByDispatchID, sqlparams);

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
    }
}
