using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.AP.Transaction;
using System.Text;
using FnI.BusinessAccessLayer.Security;

namespace FnI.BusinessAccessLayer.Setup
{
    public partial class SET_DocumentApprove
    {
        #region Constant Queries


        const string Query_DeleteDataByDocIDAndDocNo = @"

delete from 
Set_documentPendingApprove 
where DocumentApproveID in (
select DocumentApproveID from Set_documentApprove  
where DocumentNo = @DocumentNo and DocumentTypeID = @DocumentTypeID
and [PeriodID] = @PeriodID  and [CompanyID] = @CompanyID 
)
and [PeriodID] = @PeriodID  and [CompanyID] = @CompanyID 

delete from Set_documentApprove  
where DocumentNo = @DocumentNo and DocumentTypeID = @DocumentTypeID and [PeriodID] = @PeriodID  and [CompanyID] = @CompanyID 
";


        const string query_GetAuthorityLevelByUserID = @" select distinct SET_Authority.AuthorityLevel,SET_ApprovalAuthority.CompanyID,SET_ApprovalAuthority.PeriodID,
			aspnet_users.UserName, SET_ApprovalAuthority.UserID 
		from SET_ApprovalAuthority 
		inner join SET_Authority on SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID
		and SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID
		and SET_Authority.periodID = SET_ApprovalAuthority.periodID
		and SET_Authority.status = 1
		inner join aspnet_users on aspnet_users.userid  = SET_ApprovalAuthority.UserID
		where SET_ApprovalAuthority.status = 1
		and SET_ApprovalAuthority.CompanyID = @CompanyID
		and SET_ApprovalAuthority.PeriodID = @PeriodID
		and SET_ApprovalAuthority.UserID = @UserID";

        const string Query_Delete_ByDocNoandTypeID = @" Update [SET_DocumentApprove] Set [Status] = @Status 
WHERE [CompanyID] = @CompanyID and Documentno = @DocumentNo and DocumentTypeID = @DocumentTypeID 
and [PeriodID] = @PeriodID and ApprovalStatus <> 'Discuss' and FiscalID = @CurrentFiscalID ";

        const string Query_GetActiveApproveDataAfterDiscuss = @" 
        DECLARE @DocumentApproveID int;

        SET @DocumentApproveID=( SELECT max(DocumentApproveID) FROM [SET_DocumentApprove] WHERE [Status] = 1 AND 
						         ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null) 
						         and DocumentTypeID = @documentTypeID AND DocumentNo = @documentNo AND (ApprovalStatus = 'Discuss'));

         SELECT * FROM [SET_DocumentApprove] WHERE [Status] = 1 AND 
         ([CompanyID] = @CompanyID OR @CompanyID is null) AND ([PeriodID] = @PeriodID OR @PeriodID is null) 
         and 
         DocumentTypeID = @documentTypeID AND DocumentNo = @documentNo and (DocumentApproveID > @DocumentApproveID or @DocumentApproveID is null)  ";

        const string Query_Update_By_Keys_Status = @" 
UPDATE [SET_DocumentApprove] SET [Status] = @Status
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [DocumentTypeID] = @DocumentTypeID and [DocumentNo] = @DocumentNo ";

        const string Query_Update_By_Keys_StatusWithoutDiscuss = @" 
UPDATE SET_DocumentApprove SET Status = @Status
WHERE DocumentApproveID = (SELECT MAX(DocumentApproveID) AS DocumentApproveID FROM SET_DocumentApprove
							WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo AND Status = 1
                                AND ApprovalStatus <> 'Discuss')
	AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo AND Status = 1 
    AND ApprovalStatus <> 'Discuss' ";

        const string Query_LastApproveDataWithoutDiscuss_byKey = @" 
SELECT * FROM SET_DocumentApprove
WHERE DocumentApproveID = (SELECT MAX(DocumentApproveID) AS DocumentApproveID FROM SET_DocumentApprove
							WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo AND Status = 1
                                AND ApprovalStatus <> 'Discuss')
	AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo AND Status = 1 
    AND ApprovalStatus <> 'Discuss' ";

        const string query_Select_DataByKey_ForApprovalHistory = @" 
SELECT *
FROM SET_DocumentApprove 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo
{0}
ORDER BY CreateDate, DocumentApproveID ";

        const string query_Select_DataByKey_ForApprovalHistory_StockTrans = @" 
SELECT *
FROM SET_DocumentApprove 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND DocumentTypeID = @DocumentTypeID AND DocumentNo = @DocumentNo and FiscalID = @FiscalID
ORDER BY CreateDate, DocumentApproveID ";


        const string query_GetDocApproveList = @"--DECLARE @UserID uniqueidentifier = null,
--	@PeriodID INT = 1,
--	@CompanyID INT = 1;

select * from 
(
SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate AS DocDate, 
    isnull(INV_StockTransactionHeader.UpdateDate, INV_StockTransactionHeader.CreateDate) AS ActionDate, INV_StockTransactionHeader.CreatedByValue, SET_DocumentType.DocumentType, 
	(Case 
	When INV_StockTransactionHeader.StatusCode = 1 Then 'Approved' 
	When INV_StockTransactionHeader.StatusCode = 5 Then 'InProcess' 
	When INV_StockTransactionHeader.StatusCode = 12 Then 'ReleaseForApprove' 
	When INV_StockTransactionHeader.StatusCode = 13 Then 'PartiallyApproved'
	When INV_StockTransactionHeader.StatusCode = 14 Then 'Discuss'    
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM INV_StockTransactionHeader
    INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_Item.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID AND SET_DocumentType.Status = 1 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentApprove.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
        AND SET_DocumentApprove.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND CAST(SET_DocumentApprove.DocumentNo as int) = INV_StockTransactionHeader.DocumentNo 
        AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = INV_StockTransactionHeader.CompanyID
		AND PeriodID = INV_StockTransactionHeader.PeriodID AND DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
		AND CAST(SET_DocumentApprove.DocumentNo as int) = INV_StockTransactionHeader.DocumentNo)
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = INV_StockTransactionHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT sth.TotalAmount FROM INV_StockTransactionHeader sth 
											WHERE sth.CompanyID = INV_StockTransactionHeader.CompanyID 
												AND sth.PeriodID = INV_StockTransactionHeader.PeriodID 
												AND sth.StockTransactionID = INV_StockTransactionHeader.StockTransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT sth.TotalAmount FROM INV_StockTransactionHeader sth 
													WHERE sth.CompanyID = INV_StockTransactionHeader.CompanyID 
														AND sth.PeriodID = INV_StockTransactionHeader.PeriodID 
														AND sth.StockTransactionID = INV_StockTransactionHeader.StockTransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
	AND (SET_UserApprovalAmount.ApprovalItemGroup = INV_Item.ApprovalItemGroupID OR SET_UserApprovalAmount.ApprovalItemGroup IS NULL)
    AND (SET_UserApprovalAmount.UserID = INV_StockTransactionHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 
Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
	
	 WHERE 
	 [INV_StockTransactionHeader].[Status] = 1 
	 AND [INV_StockTransactionHeader].[PeriodID] = @PeriodID 
     AND SET_DocumentApprove.ApprovalStatus <> 'Approved'
	 AND [INV_StockTransactionHeader].[CompanyID] = @CompanyID 
	 and INV_StockTransactionHeader.StatusCode IN ('12','13','14') 
	 AND (INV_StockTransactionHeader.StatusCode = @DocStatus or @DocStatus is null)
	 AND (INV_StockTransactionHeader.CreatedBy = @UserID or @UserID is null)
    {Filter}


    Union All
     
SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, PUR_EnquiryHeader.DocumentNo, PUR_EnquiryHeader.DueDate as DocDate, 
    isnull(PUR_EnquiryHeader.UpdateDate, PUR_EnquiryHeader.CreateDate) AS ActionDate, PUR_EnquiryHeader.CreatedByValue, DocumentType, 
	(Case 
	When PUR_EnquiryHeader.StatusCode = 1 Then 'Approved' 
	When PUR_EnquiryHeader.StatusCode = 5 Then 'InProcess' 
	When PUR_EnquiryHeader.StatusCode = 12 Then 'ReleaseForApprove' 
	When PUR_EnquiryHeader.StatusCode = 13 Then 'PartiallyApproved' 
	When PUR_EnquiryHeader.StatusCode = 14 Then 'Discuss'    
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM PUR_EnquiryHeader
		INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID 
			AND PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID 
			AND PUR_EnquiryDetail.Status = 1
		LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = PUR_EnquiryHeader.DocumentTypeID
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = PUR_EnquiryHeader.CompanyID
			AND SET_DocumentApprove.PeriodID = PUR_EnquiryHeader.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess')))
AND SET_DocumentApprove.DocumentTypeID = PUR_EnquiryHeader.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = PUR_EnquiryHeader.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = PUR_EnquiryHeader.CompanyID
				AND PeriodID = PUR_EnquiryHeader.PeriodID AND DocumentTypeID = PUR_EnquiryHeader.DocumentTypeID 
				AND DocumentNo = PUR_EnquiryHeader.DocumentNo)
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = PUR_EnquiryHeader.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = PUR_EnquiryHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = PUR_EnquiryHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT eh.TotalAmount FROM PUR_EnquiryHeader eh 
													WHERE eh.CompanyID = PUR_EnquiryHeader.CompanyID 
														AND eh.PeriodID = PUR_EnquiryHeader.PeriodID 
														AND eh.EnquiryID = PUR_EnquiryHeader.EnquiryID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT eh.TotalAmount FROM PUR_EnquiryHeader eh 
															WHERE eh.CompanyID = PUR_EnquiryHeader.CompanyID 
																AND eh.PeriodID = PUR_EnquiryHeader.PeriodID 
																AND eh.EnquiryID = PUR_EnquiryHeader.EnquiryID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
        AND (SET_UserApprovalAmount.UserID = PUR_EnquiryHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
		INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
			AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
			AND SET_ApprovalAuthority.Status = 1
		INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
			AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
			AND SET_Authority.Status = 1  
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
            WHERE [PUR_EnquiryHeader].[Status] = 1 
			AND [PUR_EnquiryHeader].[PeriodID] = @PeriodID 
            AND SET_DocumentApprove.ApprovalStatus <> 'Approved'
            AND PUR_EnquiryHeader.[StatusCode] IN ('12','13','14')
			AND (PUR_EnquiryHeader.StatusCode = @DocStatus or @DocStatus is null)
			AND [PUR_EnquiryHeader].[CompanyID] = @CompanyID
				 AND ([PUR_EnquiryHeader].CreatedBy = @UserID or @UserID is null)
				 
            {Filter}

Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, soh_Outer.ContractNo As DocumentNo, soh_Outer.ContractDate As DocDate, isnull(soh_Outer.UpdateDate, soh_Outer.CreateDate) AS ActionDate, 
    soh_Outer.CreatedByValue, DocumentType, 
	(Case 
	When soh_Outer.StatusCode = 1 Then 'Approved' 
	When soh_Outer.StatusCode = 5 Then 'InProcess' 
	When soh_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When soh_Outer.StatusCode = 13 Then 'PartiallyApproved' 
	When soh_Outer.StatusCode = 14 Then 'Discuss'
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM SAL_OrderHeader soh_Outer
		INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.OrderID = soh_Outer.OrderID AND SAL_OrderDetail.CompanyID = soh_Outer.CompanyID 
			AND SAL_OrderDetail.PeriodID = soh_Outer.PeriodID AND SAL_OrderDetail.Status = 1
		INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID 
--AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
AND INV_Item.Status = 1
LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = soh_Outer.SaleContractTypeID
		INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = soh_Outer.CustomerID 
			AND AR_SET_Customer.CompanyID = soh_Outer.CompanyID AND AR_SET_Customer.PeriodID = soh_Outer.PeriodID 
			AND AR_SET_Customer.Status = 1
		LEFT JOIN AR_SET_Customer AS tbl_Broker ON tbl_Broker.CustomerID = soh_Outer.BrokerID 
			AND tbl_Broker.CompanyID = soh_Outer.CompanyID AND tbl_Broker.PeriodID = soh_Outer.PeriodID 
			AND tbl_Broker.Status = 1 
	    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = soh_Outer.CompanyID
	        AND SET_DocumentApprove.PeriodID = soh_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess')))
AND SET_DocumentApprove.DocumentTypeID = soh_Outer.SaleContractTypeID
	        AND SET_DocumentApprove.DocumentNo = soh_Outer.ContractNo 
	        AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = soh_Outer.CompanyID
		        AND PeriodID = soh_Outer.PeriodID AND DocumentTypeID = soh_Outer.SaleContractTypeID 
		        AND DocumentNo = soh_Outer.ContractNo)
        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = soh_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = soh_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = soh_Outer.SaleContractTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
			               ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT soh.TotalAmount FROM SAL_OrderHeader soh 
											        WHERE soh.CompanyID = soh_Outer.CompanyID 
												        AND soh.PeriodID = soh_Outer.PeriodID 
												        AND soh.OrderID = soh_Outer.OrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				           WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT soh.TotalAmount FROM SAL_OrderHeader soh 
													        WHERE soh.CompanyID = soh_Outer.CompanyID 
														        AND soh.PeriodID = soh_Outer.PeriodID 
														        AND soh.OrderID = soh_Outer.OrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                                    AND SET_UserApprovalAmount.AmountTo THEN 1
				           ELSE 0 END))
        AND (SET_UserApprovalAmount.UserID = soh_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
		INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
			AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
			AND SET_ApprovalAuthority.Status = 1
		INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
			AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
			AND SET_Authority.Status = 1
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
		Where soh_Outer.[CompanyID] = @CompanyID 
			AND soh_Outer.[PeriodID] = @PeriodID
			AND soh_Outer.[Status] = 1 
			AND (soh_Outer.CreatedBy = @UserID or @UserID is null)
            {Filter}
			and SET_DocumentApprove.ApprovalStatus <> 'Approved'
            AND soh_Outer.[StatusCode] IN ('12','13','14')
			AND (soh_Outer.StatusCode = @DocStatus or @DocStatus is null)
			


union all

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, poh_Outer.DocumentNumber As DocumentNo, poh_Outer.DocumentDate As DocDate, 
    isnull(poh_Outer.UpdateDate, poh_Outer.CreateDate) AS ActionDate, poh_Outer.CreatedByValue, SET_DocumentType.DocumentType, 
	(Case 
	When poh_Outer.StatusCode = 1 Then 'Approved' 
	When poh_Outer.StatusCode = 5 Then 'InProcess' 
	When poh_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When poh_Outer.StatusCode = 13 Then 'PartiallyApproved' 
	When poh_Outer.StatusCode = 14 Then 'Discuss'
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM AP_PurchaseOrderHeader poh_Outer 
    INNER JOIN AP_PurchaseOrderDetail pod ON pod.POHeaderID = poh_Outer.POHeaderID And poh_Outer.status = 1 AND pod.Status = 1 AND pod.CompanyID = poh_Outer.CompanyID 
        AND pod.PeriodID = poh_Outer.PeriodID 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = poh_Outer.CompanyID AND SET_DocumentType.Status = 1
	        AND SET_DocumentType.PeriodID = poh_Outer.PeriodID AND SET_DocumentType.DocumentTypeID = poh_Outer.TypeCode 
        LEFT JOIN AR_SET_Customer ON AR_SET_Customer.Customerid = poh_Outer.VendorID and AR_SET_Customer.Status = 1 
	        AND AR_SET_Customer.CompanyID = poh_Outer.CompanyID AND AR_SET_Customer.PeriodID = poh_Outer.PeriodID
        INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = poh_Outer.CompanyID
	        AND SET_DocumentApprove.PeriodID = poh_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
AND SET_DocumentApprove.DocumentTypeID = poh_Outer.TypeCode
	        AND SET_DocumentApprove.DocumentNo = poh_Outer.DocumentNumber 
	        AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = poh_Outer.CompanyID
		        AND PeriodID = poh_Outer.PeriodID AND DocumentTypeID = poh_Outer.TypeCode 
		        AND DocumentNo = poh_Outer.DocumentNumber)
        INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = poh_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = poh_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = poh_Outer.TypeCode OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
			               ELSE 0 END))
	        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT poh.TotalAmount FROM AP_PurchaseOrderHeader poh 
											        WHERE poh.CompanyID = poh_Outer.CompanyID 
												        AND poh.PeriodID = poh_Outer.PeriodID 
												        AND poh.POHeaderID = poh_Outer.POHeaderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				           WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT poh.TotalAmount FROM AP_PurchaseOrderHeader poh 
													        WHERE poh.CompanyID = poh_Outer.CompanyID 
														        AND poh.PeriodID = poh_Outer.PeriodID 
														        AND poh.POHeaderID = poh_Outer.POHeaderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                                    AND SET_UserApprovalAmount.AmountTo THEN 1
				           ELSE 0 END))
AND (SET_UserApprovalAmount.UserID = poh_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
AND (SET_UserApprovalAmount.UserID = poh_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
	 WHERE poh_Outer.[Status] = 1 
     AND poh_Outer.[PeriodID] = @PeriodID 
     AND poh_Outer.[CompanyID] = @CompanyID 
	 			AND (poh_Outer.CreatedBy = @UserID or @UserID is null)
     {Filter}
     and SET_DocumentApprove.ApprovalStatus <> 'Approved'
    AND poh_Outer.[StatusCode] IN ('12','13','14')
	AND (poh_Outer.StatusCode = @DocStatus or @DocStatus is null)
	

Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, SAL_DeliveryOrderHeader.DeliveryOrderNo AS DocumentNo, SAL_DeliveryOrderHeader.DODate AS DocDate, 
    isnull(SAL_DeliveryOrderHeader.UpdateDate, SAL_DeliveryOrderHeader.CreateDate) AS ActionDate, SAL_DeliveryOrderHeader.CreatedByValue, DocumentType, 
	(Case 
	When SAL_DeliveryOrderHeader.StatusCode = 1 Then 'Approved' 
	When SAL_DeliveryOrderHeader.StatusCode = 5 Then 'InProcess' 
	When SAL_DeliveryOrderHeader.StatusCode = 12 Then 'ReleaseForApprove' 
	When SAL_DeliveryOrderHeader.StatusCode = 13 Then 'PartiallyApproved'
	When SAL_DeliveryOrderHeader.StatusCode = 14 Then 'Discuss'
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM SAL_DeliveryOrderHeader 
    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SET_DocumentApprove.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
	LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
AND SET_DocumentApprove.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo 
	AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SAL_DeliveryOrderHeader.CompanyID
		AND PeriodID = SAL_DeliveryOrderHeader.PeriodID AND DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
		AND DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo)
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
											WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
												AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
												AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
													WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
														AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
														AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
AND (SET_UserApprovalAmount.UserID = SAL_DeliveryOrderHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 
Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID	
	 WHERE [SAL_DeliveryOrderHeader].[Status] = 1 
	 AND [SAL_DeliveryOrderHeader].[PeriodID] = @PeriodID 
	 AND [SAL_DeliveryOrderHeader].[CompanyID] = @CompanyID 
	 and SET_DocumentApprove.ApprovalStatus <> 'Approved'
     AND SAL_DeliveryOrderHeader.[StatusCode] IN ('12','13','14')
	 AND (SAL_DeliveryOrderHeader.StatusCode = @DocStatus or @DocStatus is null)
	 	 			AND (SAL_DeliveryOrderHeader.CreatedBy = @UserID or @UserID is null)
					
     {Filter}
	
Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, SAL_DispatchHeader.DispatchNo As DocumentNo, SAL_DispatchHeader.DispatchDate As DocDate, 
    isnull(SAL_DispatchHeader.UpdateDate, SAL_DispatchHeader.CreateDate) AS ActionDate, SAL_DispatchHeader.Createdbyvalue, DocumentType, 
	(Case 
	When SAL_DispatchHeader.StatusCode = 1 Then 'Approved' 
	When SAL_DispatchHeader.StatusCode = 5 Then 'InProcess' 
	When SAL_DispatchHeader.StatusCode = 12 Then 'ReleaseForApprove' 
	When SAL_DispatchHeader.StatusCode = 13 Then 'PartiallyApproved'
	When SAL_DispatchHeader.StatusCode = 14 Then 'Discuss' 
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM SAL_DispatchHeader 
    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_DispatchHeader.CompanyID AND SET_DocumentApprove.PeriodID = SAL_DispatchHeader.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
	LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
AND SET_DocumentApprove.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_DispatchHeader.DispatchNo 
	AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SAL_DispatchHeader.CompanyID
		AND PeriodID = SAL_DispatchHeader.PeriodID AND DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
		AND DocumentNo = SAL_DispatchHeader.DispatchNo)
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
AND (SET_UserApprovalAmount.UserID = SAL_DispatchHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 
Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
	
	 WHERE [SAL_DispatchHeader].[Status] = 1 
	 AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
	 AND [SAL_DispatchHeader].[CompanyID] = @CompanyID 
	 and SET_DocumentApprove.ApprovalStatus <> 'Approved'
     AND SAL_DispatchHeader.[StatusCode] IN ('12','13','14')
	 AND (SAL_DispatchHeader.StatusCode = @DocStatus or @DocStatus is null)
	 AND (SAL_DispatchHeader.CreatedBy = @UserID or @UserID is null)
						
     {Filter}

Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, InvHeader_Outer.InvoiceNumber As DocumentNo, InvHeader_Outer.InvoiceDate As DocDate, 
    isnull(InvHeader_Outer.UpdateDate, InvHeader_Outer.CreateDate) AS ActionDate, InvHeader_Outer.CreatedByValue, SET_DocumentType.DocumentType, 
	(Case 
	When InvHeader_Outer.StatusCode = 1 Then 'Approved' 
	When InvHeader_Outer.StatusCode = 5 Then 'InProcess' 
	When InvHeader_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When InvHeader_Outer.StatusCode = 13 Then 'PartiallyApproved'
	When InvHeader_Outer.StatusCode = 14 Then 'Discuss'  
	End )as ApprovalStatus

	--SET_DocumentApprove.ApprovalStatus 
FROM AR_InvoiceHeader InvHeader_Outer 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = InvHeader_Outer.CompanyID AND SET_DocumentType.PeriodID = InvHeader_Outer.PeriodID 
        AND SET_DocumentType.DocumentTypeID = InvHeader_Outer.InvoiceType 
	Left JOIN AR_SET_Customer on InvHeader_Outer.VendorID = AR_SET_Customer.CustomerID AND InvHeader_Outer.CompanyID = AR_SET_Customer.CompanyID 
		AND InvHeader_Outer.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1
	INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = InvHeader_Outer.CompanyID
		AND SET_DocumentApprove.PeriodID = InvHeader_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
AND SET_DocumentApprove.DocumentTypeID = InvHeader_Outer.InvoiceType
		AND SET_DocumentApprove.DocumentNo = InvHeader_Outer.InvoiceNumber 
		AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = InvHeader_Outer.CompanyID
			AND PeriodID = InvHeader_Outer.PeriodID AND DocumentTypeID = InvHeader_Outer.InvoiceType 
			AND DocumentNo = InvHeader_Outer.InvoiceNumber)
	INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = InvHeader_Outer.CompanyID 
		AND SET_UserApprovalAmount.PeriodID = InvHeader_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
		AND (SET_UserApprovalAmount.DocumentTypeID = InvHeader_Outer.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
		AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
							AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
					   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
							AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
							AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
					   ELSE 0 END))
		AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih 
												WHERE ih.CompanyID = InvHeader_Outer.CompanyID 
													AND ih.PeriodID = InvHeader_Outer.PeriodID 
													AND ih.InvoiceID = InvHeader_Outer.InvoiceID) >= SET_UserApprovalAmount.AmountFrom THEN 1
					   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ih.AmountPaid FROM AR_InvoiceHeader ih 
														WHERE ih.CompanyID = InvHeader_Outer.CompanyID 
															AND ih.PeriodID = InvHeader_Outer.PeriodID 
															AND ih.InvoiceID = InvHeader_Outer.InvoiceID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																AND SET_UserApprovalAmount.AmountTo THEN 1
					   ELSE 0 END))
    AND (SET_UserApprovalAmount.UserID = InvHeader_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
	INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
		AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
		AND SET_ApprovalAuthority.Status = 1
	INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
		AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
		AND SET_Authority.Status = 1
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
		 WHERE InvHeader_Outer.CompanyID = @CompanyID 
	     AND InvHeader_Outer.PeriodID = @PeriodID 
	     AND InvHeader_Outer.[Status] = 1  
	     and SET_DocumentApprove.ApprovalStatus <> 'Approved'
         AND InvHeader_Outer.[StatusCode] IN ('12','13','14')
		 AND (InvHeader_Outer.StatusCode = @DocStatus or @DocStatus is null)
		 AND (InvHeader_Outer.CreatedBy = @UserID or @UserID is null)
		 
        {Filter}
	    
Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, PaymentHeader_Outer.DocumentNo, PaymentHeader_Outer.DocumentDate As DocDate, 
    isnull(PaymentHeader_Outer.UpdateDate, PaymentHeader_Outer.CreateDate) AS ActionDate, PaymentHeader_Outer.CreatedByValue, SET_DocumentType.DocumentType, 
	(Case 
	When PaymentHeader_Outer.StatusCode = 1 Then 'Approved' 
	When PaymentHeader_Outer.StatusCode = 5 Then 'InProcess' 
	When PaymentHeader_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When PaymentHeader_Outer.StatusCode = 13 Then 'PartiallyApproved' 
	When PaymentHeader_Outer.StatusCode = 14 Then 'Discuss'  
	End )as ApprovalStatus

	--SET_DocumentApprove.ApprovalStatus 
FROM AP_PaymentHeader PaymentHeader_Outer 
    INNER JOIN SET_DocumentType ON PaymentHeader_Outer.CompanyID = SET_DocumentType.CompanyID AND PaymentHeader_Outer.PeriodID = SET_DocumentType.PeriodID 
        AND PaymentHeader_Outer.DocumentTypeID = SET_DocumentType.DocumentTypeID AND PaymentHeader_Outer.Status=1
		LEFT JOIN AR_SET_Customer ON PaymentHeader_Outer.CompanyID = AR_SET_Customer.CompanyID AND PaymentHeader_Outer.PeriodID = AR_SET_Customer.PeriodID 
			AND PaymentHeader_Outer.PayFromCustomerID = AR_SET_Customer.CustomerID AND AR_SET_Customer.Status = 1
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = PaymentHeader_Outer.CompanyID
			AND SET_DocumentApprove.PeriodID = PaymentHeader_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
		AND SET_DocumentApprove.DocumentTypeID = PaymentHeader_Outer.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = PaymentHeader_Outer.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = PaymentHeader_Outer.CompanyID
				AND PeriodID = PaymentHeader_Outer.PeriodID AND DocumentTypeID = PaymentHeader_Outer.DocumentTypeID 
				AND DocumentNo = PaymentHeader_Outer.DocumentNo)
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = PaymentHeader_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = PaymentHeader_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = PaymentHeader_Outer.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph 
													WHERE ph.CompanyID = PaymentHeader_Outer.CompanyID 
														AND ph.PeriodID = PaymentHeader_Outer.PeriodID 
														AND ph.PaymentID = PaymentHeader_Outer.PaymentID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph 
															WHERE ph.CompanyID = PaymentHeader_Outer.CompanyID 
																AND ph.PeriodID = PaymentHeader_Outer.PeriodID 
																AND ph.PaymentID = PaymentHeader_Outer.PaymentID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
        AND (SET_UserApprovalAmount.UserID = PaymentHeader_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
		INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
			AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
			AND SET_ApprovalAuthority.Status = 1
		INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
			AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
			AND SET_Authority.Status = 1 
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
			 WHERE PaymentHeader_Outer.[Status] = 1 
	 AND PaymentHeader_Outer.[PeriodID] = @PeriodID 
	 AND PaymentHeader_Outer.[CompanyID] = @CompanyID 
	 and SET_DocumentApprove.ApprovalStatus <> 'Approved'
     AND PaymentHeader_Outer.[StatusCode] IN ('12','13','14')
	 AND (PaymentHeader_Outer.StatusCode = @DocStatus or @DocStatus is null)
	 AND (PaymentHeader_Outer.CreatedBy = @UserID or @UserID is null)
			 
    {Filter}

Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, InvHeader_Outer.InvoiceNumber As DocumentNo, InvHeader_Outer.InvoiceDate As DocDate, 
    isnull(InvHeader_Outer.UpdateDate, InvHeader_Outer.CreateDate) AS ActionDate, InvHeader_Outer.CreatedByValue, DocumentType, 
	(Case 
	When InvHeader_Outer.StatusCode = 1 Then 'Approved' 
	When InvHeader_Outer.StatusCode = 5 Then 'InProcess' 
	When InvHeader_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When InvHeader_Outer.StatusCode = 13 Then 'PartiallyApproved' 
	When InvHeader_Outer.StatusCode = 14 Then 'Discuss'  
	End )as ApprovalStatus
	--SET_DocumentApprove.ApprovalStatus 
FROM SAL_InvoiceHeader InvHeader_Outer 
    INNER JOIN AR_SET_Customer ON InvHeader_Outer.CustomerID = AR_SET_Customer.CustomerID AND InvHeader_Outer.CompanyID = AR_SET_Customer.CompanyID 
        AND InvHeader_Outer.PeriodID = AR_SET_Customer.PeriodID AND AR_SET_Customer.Status = 1 
		LEFT JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = InvHeader_Outer.InvoiceType
		INNER JOIN AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_SET_Customer.CustomerID 
			AND AR_SET_BusinessPartnerTypes.CompanyID = AR_SET_Customer.CompanyID AND AR_SET_BusinessPartnerTypes.PeriodID = AR_SET_Customer.PeriodID 
			AND AR_SET_Customer.Status = 1
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = InvHeader_Outer.CompanyID
			AND SET_DocumentApprove.PeriodID = InvHeader_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
AND SET_DocumentApprove.DocumentTypeID = InvHeader_Outer.InvoiceType
			AND SET_DocumentApprove.DocumentNo = InvHeader_Outer.InvoiceNumber 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = InvHeader_Outer.CompanyID
				AND PeriodID = InvHeader_Outer.PeriodID AND DocumentTypeID = InvHeader_Outer.InvoiceType 
				AND DocumentNo = InvHeader_Outer.InvoiceNumber)
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = InvHeader_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = InvHeader_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = InvHeader_Outer.InvoiceType OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
            AND (SET_UserApprovalAmount.UserID = InvHeader_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
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
		INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
			AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
			AND SET_ApprovalAuthority.Status = 1
		INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
			AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
			AND SET_Authority.Status = 1
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
			 WHERE InvHeader_Outer.[Status] = 1 
	 AND InvHeader_Outer.[PeriodID] = @PeriodID
	 AND InvHeader_Outer.[CompanyID] = @CompanyID  
     and SET_DocumentApprove.ApprovalStatus <> 'Approved'
     AND InvHeader_Outer.[StatusCode] IN ('12','13','14')
	 AND (InvHeader_Outer.StatusCode = @DocStatus or @DocStatus is null)
	 AND (InvHeader_Outer.CreatedBy = @UserID or @UserID is null)
	 
    {Filter}
	
Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, ReceiptTransactionHeader_Outer.DocumentNo, ReceiptTransactionHeader_Outer.DocumentDate As DocDate,  
    isnull(ReceiptTransactionHeader_Outer.UpdateDate, ReceiptTransactionHeader_Outer.CreateDate) AS ActionDate, ReceiptTransactionHeader_Outer.CreatedByValue, SET_DocumentType.DocumentType, 
    (Case 
	When ReceiptTransactionHeader_Outer.StatusCode = 1 Then 'Approved' 
	When ReceiptTransactionHeader_Outer.StatusCode = 5 Then 'InProcess' 
	When ReceiptTransactionHeader_Outer.StatusCode = 12 Then 'ReleaseForApprove' 
	When ReceiptTransactionHeader_Outer.StatusCode = 13 Then 'PartiallyApproved'
	When ReceiptTransactionHeader_Outer.StatusCode = 14 Then 'Discuss'   
	End )as ApprovalStatus
	
	--SET_DocumentApprove.ApprovalStatus 
FROM AR_ReceiptTransactionHeader ReceiptTransactionHeader_Outer
		Left JOIN AR_ReceiptTransactionDetail ON AR_ReceiptTransactionDetail.ReceiptHeaderID = ReceiptTransactionHeader_Outer.ReceiptTransactionID 
			AND AR_ReceiptTransactionDetail.CompanyID = ReceiptTransactionHeader_Outer.CompanyID 
			And AR_ReceiptTransactionDetail.PeriodID = ReceiptTransactionHeader_Outer.PeriodID AND ReceiptTransactionHeader_Outer.Status = 1
		LEFT JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.InvoiceID = AR_ReceiptTransactionDetail.InvoiceHeaderID 
			AND SAL_InvoiceHeader.CompanyID = AR_ReceiptTransactionDetail.CompanyID And SAL_InvoiceHeader.PeriodID = AR_ReceiptTransactionDetail.PeriodID 
			AND SAL_InvoiceHeader.Status = 1
		LEFT JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_InvoiceHeader.CustomerID 
			AND AR_SET_Customer.CompanyID = SAL_InvoiceHeader.CompanyID AND AR_SET_Customer.PeriodID = SAL_InvoiceHeader.PeriodID 
			AND AR_SET_Customer.Status = 1
		INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = ReceiptTransactionHeader_Outer.DocumentTypeID 
			AND SET_DocumentType.CompanyID = ReceiptTransactionHeader_Outer.CompanyID And SET_DocumentType.PeriodID = ReceiptTransactionHeader_Outer.PeriodID 
			AND SET_DocumentType.Status = 1
		INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = ReceiptTransactionHeader_Outer.CompanyID
			AND SET_DocumentApprove.PeriodID = ReceiptTransactionHeader_Outer.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
		AND SET_DocumentApprove.DocumentTypeID = ReceiptTransactionHeader_Outer.DocumentTypeID
			AND SET_DocumentApprove.DocumentNo = ReceiptTransactionHeader_Outer.DocumentNo 
			AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = ReceiptTransactionHeader_Outer.CompanyID
				AND PeriodID = ReceiptTransactionHeader_Outer.PeriodID AND DocumentTypeID = ReceiptTransactionHeader_Outer.DocumentTypeID 
				AND DocumentNo = ReceiptTransactionHeader_Outer.DocumentNo)
		INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = ReceiptTransactionHeader_Outer.CompanyID 
			AND SET_UserApprovalAmount.PeriodID = ReceiptTransactionHeader_Outer.PeriodID AND SET_UserApprovalAmount.Status = 1
			AND (SET_UserApprovalAmount.DocumentTypeID = ReceiptTransactionHeader_Outer.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
			AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
						   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
								AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
								AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
						   ELSE 0 END))
			AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT rth.Amount FROM AR_ReceiptTransactionHeader rth 
													WHERE rth.CompanyID = ReceiptTransactionHeader_Outer.CompanyID 
														AND rth.PeriodID = ReceiptTransactionHeader_Outer.PeriodID 
														AND rth.ReceiptTransactionID = ReceiptTransactionHeader_Outer.ReceiptTransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1
						   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT rth.Amount FROM AR_ReceiptTransactionHeader rth 
															WHERE rth.CompanyID = ReceiptTransactionHeader_Outer.CompanyID 
																AND rth.PeriodID = ReceiptTransactionHeader_Outer.PeriodID 
																AND rth.ReceiptTransactionID = ReceiptTransactionHeader_Outer.ReceiptTransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
																	AND SET_UserApprovalAmount.AmountTo THEN 1
						   ELSE 0 END))
        AND (SET_UserApprovalAmount.UserID = ReceiptTransactionHeader_Outer.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
		INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
			AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
			AND SET_ApprovalAuthority.Status = 1
		INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
			AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
			AND SET_Authority.Status = 1
        Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
			 WHERE ReceiptTransactionHeader_Outer.[Status] = 1 
	 AND ReceiptTransactionHeader_Outer.[PeriodID] = @PeriodID 
	 AND ReceiptTransactionHeader_Outer.[CompanyID] = @CompanyID 
     and SET_DocumentApprove.ApprovalStatus <> 'Approved'
     AND ReceiptTransactionHeader_Outer.[StatusCode] IN ('12','13','14')
	 AND (ReceiptTransactionHeader_Outer.StatusCode = @DocStatus or @DocStatus is null)
	 AND (ReceiptTransactionHeader_Outer.CreatedBy = @UserID or @UserID is null)
	 
    {Filter}

Union All

SELECT DISTINCT SET_DocumentApprove.Comments, AU.UserName, GL_TransactionHeader.DocumentNo, GL_TransactionHeader.DocumentDate As DocDate, 
    isnull(GL_TransactionHeader.UpdateDate, GL_TransactionHeader.CreateDate) AS ActionDate, GL_TransactionHeader.CreatedByValue, SET_DocumentType.DocumentType, 
	(Case 
	When GL_TransactionHeader.StatusCode = 1 Then 'Approved' 
	When GL_TransactionHeader.StatusCode = 5 Then 'InProcess' 
	When GL_TransactionHeader.StatusCode = 12 Then 'ReleaseForApprove' 
	When GL_TransactionHeader.StatusCode = 13 Then 'PartiallyApproved' 
	When GL_TransactionHeader.StatusCode = 14 Then 'Discuss'   
	End )as ApprovalStatus

	--SET_DocumentApprove.ApprovalStatus 
FROM GL_TransactionHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.documentTypeid = GL_TransactionHeader.DocumentTypeID AND SET_DocumentType.PeriodID = GL_TransactionHeader.PeriodID 
        AND SET_DocumentType.CompanyID = GL_TransactionHeader.CompanyID 
			LEFT JOIN aspnet_Users ON aspnet_Users.UserId = GL_TransactionHeader.ApprovedBy 
			INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = GL_TransactionHeader.CompanyID 
				AND SET_DocumentApprove.PeriodID = GL_TransactionHeader.PeriodID 
        AND (SET_DocumentApprove.Status = 1 OR (SET_DocumentApprove.Status = 0 AND SET_DocumentApprove.ApprovalStatus in ('Discuss', 'InProcess'))) 
				AND SET_DocumentApprove.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
				AND SET_DocumentApprove.DocumentNo = GL_TransactionHeader.DocumentNo 
				AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove 
					WHERE CompanyID = GL_TransactionHeader.CompanyID AND PeriodID = GL_TransactionHeader.PeriodID 
					AND DocumentTypeID = GL_TransactionHeader.DocumentTypeID AND DocumentNo = GL_TransactionHeader.DocumentNo)
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = GL_TransactionHeader.CompanyID 
        AND SET_UserApprovalAmount.PeriodID = GL_TransactionHeader.PeriodID AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = GL_TransactionHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo THEN 1 ELSE 0 END)) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT th.ControlDebit FROM GL_TransactionHeader th 
					WHERE th.CompanyID = GL_TransactionHeader.CompanyID AND th.PeriodID = GL_TransactionHeader.PeriodID 
					AND th.TransactionID = GL_TransactionHeader.TransactionID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
					WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT th.ControlDebit FROM GL_TransactionHeader th 
					WHERE th.CompanyID = GL_TransactionHeader.CompanyID AND th.PeriodID = GL_TransactionHeader.PeriodID 
					AND th.TransactionID = GL_TransactionHeader.TransactionID) BETWEEN SET_UserApprovalAmount.AmountFrom 
					AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
				AND (SET_UserApprovalAmount.UserID = GL_TransactionHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
        AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID AND SET_Authority.Status = 1 
    Inner Join aspnet_Users AU ON AU.UserId = SET_ApprovalAuthority.UserID
Where [GL_TransactionHeader].[PeriodID] = @PeriodID and [GL_TransactionHeader].[CompanyID] = @CompanyID and [GL_TransactionHeader].[Status] = 1 
    AND [GL_TransactionHeader].[StatusCode] IN ('12','13','14') 
	AND ([GL_TransactionHeader].StatusCode = @DocStatus or @DocStatus is null)
	and SET_DocumentApprove.ApprovalStatus <> 'Approved' 
	AND ([GL_TransactionHeader].CreatedBy = @UserID or @UserID is null)
    {Filter}  
	
	

	) tab

	WHERE NULL IS NULL
	{0}
";

        

        #endregion

        #region Class Methods

        public bool UpdateDataByKeyStatus(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[4] = new SqlParameter("@Status", false);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_Keys_Status, sqlparams) > 0)
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

        public bool UpdateStatusByKeysWithoutDiscuss(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", f_DocumentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", f_DocumentNo);
                sqlparams[4] = new SqlParameter("@Status", false);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_Keys_StatusWithoutDiscuss, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_Keys_StatusWithoutDiscuss, sqlparams) > 0)
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

        public static DataTable GetActiveApproveDataAfterDiscuss(int companyID, int periodID, int documentTypeID, string documentNo)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetActiveApproveDataAfterDiscuss,""), sqlparams);

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

        public static DataRow GetLastApproveDataWithoutDiscuss(int companyID, int periodID, int documentTypeID, string documentNo)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_LastApproveDataWithoutDiscuss_byKey, sqlparams);

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

        public static DataTable GetDataByKeyForApprovalHistory(int companyID, int periodID, int documentTypeID, string documentNo, 
            SqlTransaction objSQLTransaction = null, int FiscalID  = 0)
        {
            DataTable dtToReturn = null;
            string Fillter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                if (FiscalID > 0)
                    Fillter = " and FiscalID = " + FiscalID;

                DataSet ds = null;

                if(objSQLTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQLTransaction, CommandType.Text, string.Format(query_Select_DataByKey_ForApprovalHistory,Fillter) , sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query_Select_DataByKey_ForApprovalHistory, Fillter), sqlparams);

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

        public static DataTable GetDataByKeyForApprovalHistory_StockTrans(int companyID, int periodID, int documentTypeID, string documentNo,int fiscalID,
           SqlTransaction objSQLTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@FiscalID", fiscalID);

                DataSet ds = null;

                if (objSQLTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objSQLTransaction, CommandType.Text, query_Select_DataByKey_ForApprovalHistory_StockTrans, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query_Select_DataByKey_ForApprovalHistory_StockTrans, sqlparams);

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

        public static DataTable Get_DocApproveListReport(int companyID, int periodID, string ApproverID, Guid GuidApproverID, Nullable<Guid> CreadetBy = null, int DocTypeID = 0, int DocNoFrom = 0, int DocNoTo = 0, Nullable<int> DocStatus = null)
        {
            DataTable dt = null;
            string Filter = "";
            string Filter1 = "";
            string Filter0 = "";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (CreadetBy.HasValue)
                { sqlparams[2] = new SqlParameter("@UserID", CreadetBy); }
                else
                {sqlparams[2] = new SqlParameter("@UserID", DBNull.Value);}


                if (DocStatus > 0)
                { sqlparams[3] = new SqlParameter("@DocStatus", DocStatus); }
                else
                { sqlparams[3] = new SqlParameter("@DocStatus", DBNull.Value); }

                // Work For Dropdown if ALL Selected (TAQI)
                if (ApproverID == "-1")
                {
                    DataTable authorityLevel = SET_ApprovalAuthority.Get_AuthorityLevel(companyID, periodID, GuidApproverID);
                    if (authorityLevel.Rows.Count > 0)
                    {
                        int level = int.Parse(authorityLevel.Rows[0]["AuthorityLevel"].ToString());
                        if (level == 1)
                        {
                            Filter = " ";
                        }
                        else
                        {
                            DataTable dt_UserNameAgaisntLevel = aspnet_Users.GetDataByLevel(companyID, periodID, level);
                            Filter = " And SET_ApprovalAuthority.UserID in (";
                            for (int i = 0; i < dt_UserNameAgaisntLevel.Rows.Count; i++)
                            {
                                Filter1 = Filter1 + "'" + dt_UserNameAgaisntLevel.Rows[i]["UserID"].ToString() + "',";
                            }
                            Filter1 = Filter1.Remove(Filter1.Length - 1);
                            Filter = Filter + Filter1 + ")";
                        }
                    }
                    //else
                    //{
                    //    Filter = " And (SET_ApprovalAuthority.UserID = '" + ApproverID + "')";
                    //}
                }
                else
                    Filter = " And (SET_ApprovalAuthority.UserID = '" + ApproverID + "')";

                if (DocTypeID > 0)
                    Filter += " AND SET_DocumentType.DocumentTypeID = "+DocTypeID+" ";

                if (DocNoFrom > 0)
                    Filter0 += " AND DocumentNo >= " + DocNoFrom + " ";

                if (DocNoTo > 0)
                    Filter0 += " AND DocumentNo <= " + DocNoTo + " ";

                string query = query_GetDocApproveList.Replace("{Filter}", Filter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query,Filter0), sqlparams);

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

        public static DataTable Get_UserDocApproveListReport(int companyID, int periodID, string query_GetUserDocApproveList, Nullable<DateTime> DateFrom = null, Nullable<DateTime> ToDate = null, int CurrentFiscalID = 0, Nullable<Guid> userID=null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@DateFrom", DateFrom);
                sqlparams[4] = new SqlParameter("@ToDate", ToDate);
            
                if (userID.HasValue)
                    sqlparams[5] = new SqlParameter("@UserID", userID);
                else
                    sqlparams[5] = new SqlParameter("@UserID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_GetUserDocApproveList, sqlparams);
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dt = ds.Tables[0];

          //      if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
          //      {
          //          dt = ds.Tables[0];
          //          dt = dt.AsEnumerable()
          //.Where(row => row.Field<int>("CompanyID") == companyID)
          //.CopyToDataTable();
          //      }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        // delete 
        public bool DeleteDataByDocIDAndDocNo(SqlTransaction objTransaction, int companyID, int DocumentTypeID, int DocumentNo,int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@Status", false);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteDataByDocIDAndDocNo, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteDataByDocIDAndDocNo, sqlparams) > 0)
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

        public static bool DeleteDataByDocNoanddocTypeid(int companyID, int periodID, int DocumentNo, int DocumentTypeID, SqlTransaction objTransaction = null,int CurrentFiscalID = 0 )
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@Status", false);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByDocNoandTypeID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByDocNoandTypeID, sqlparams) > 0)
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

        public static DataTable GetAuthorityLevelbyUserID(int companyID, int periodID, string UserID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", UserID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query_GetAuthorityLevelByUserID, sqlparams);

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

        public static DataTable GetPreviousAllowedPeriod(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string query = @"
                    select * from SET_Period , SET_PeriodCustom
                    where set_period.PeriodID = SET_PeriodCustom.PeriodID 
                    and SET_PeriodCustom.STATUS =1 and SET_PeriodCustom.IsActive =1
                    AND SET_PeriodCustom.PeriodID = @PeriodID
                    --AND SET_PeriodCustom.CompanyID = @CompanyID

                ";

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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


        public static DataTable GetFiscalID(int companyID, int ParentPeriodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@ParentPeriodID", ParentPeriodID);

                string query = @"
                    select * from  SET_PeriodCustom
                      where 
                    SET_PeriodCustom.STATUS =1 
                    AND SET_PeriodCustom.IsActive =1
                    AND SET_PeriodCustom.ParentPeriodID = @ParentPeriodID
                    AND SET_PeriodCustom.CompanyID = @CompanyID

                ";

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static bool DeleteApprovalHistroy(int companyID, int periodID, int DocumentNo, int DocumentTypeID, SqlTransaction objTransaction = null, int CurrentFiscalID = 0)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@Status", false);
                sqlparams[5] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                const string Query_DeleteApprovalHistroy = @" 

Update Set_documentPendingApprove set Status = 0 where DocumentApproveID in (select DocumentApproveID from SET_DocumentApprove where DocumentNo = @DocumentNo and DocumentTypeID = @DocumentTypeID and FiscalID = @CurrentFiscalID and CompanyID = @CompanyID)
Update SET_DocumentApprove set Status = 0 where DocumentNo = @DocumentNo and DocumentTypeID = @DocumentTypeID and FiscalID = @CurrentFiscalID and CompanyID = @CompanyID
";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DeleteApprovalHistroy, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DeleteApprovalHistroy, sqlparams) > 0)
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

        #endregion
    }
}
