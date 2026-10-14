using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Setup;

namespace FnI.BusinessAccessLayer.Purchase
{
    public partial class PUR_EnquiryHeader
    {
        #region Constant Queries

        const string Query_Update_Flag_Header = @" UPDATE PUR_EnquiryHeader 
SET AllQuotationsReceived = @IsAllQuotationsReceived, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE EnquiryID = @EnquiryHeaderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 ";

        const string Query_Update_Flag = @" UPDATE PUR_EnquiryHeader 
SET AllQuotationsReceived = @IsAllQuotationsReceived, StatusCode = @StatusCode, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE EnquiryID = @EnquiryHeaderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID AND Status = 1 ";
        
        const string Query_Get_AllData = @" 
";

        const string Query_SelectAll_Searching_WithPaging = @" 
SELECT *, ROW_NUMBER() OVER (ORDER BY tbl_EH.DocumentNo desc) AS 'Row' 
FROM 
(
    SELECT DISTINCT inv_StockTransactionheader.DocumentNo as RequisitionNo ,PUR_EnquiryHeader.CompanyID, PUR_EnquiryHeader.PeriodID, PUR_EnquiryHeader.EnquiryID, PUR_EnquiryHeader.DocumentTypeID, SET_DocumentType.DocumentType,
        PUR_EnquiryHeader.DocumentNo, PUR_EnquiryHeader.DueDate, PUR_EnquiryHeader.ReferenceNumber, PUR_EnquiryHeader.EnquiryDate, PUR_EnquiryHeader.DeliveryDate, 
        PUR_EnquiryHeader.AllQuotationsReceived, PUR_EnquiryHeader.Status, PUR_EnquiryHeader.CreatedBy, PUR_EnquiryHeader.CreateDate, 
        PUR_EnquiryHeader.CreatedByValue, PUR_EnquiryHeader.UpdatedBy, PUR_EnquiryHeader.UpdateDate, PUR_EnquiryHeader.UpdatedByValue, PUR_EnquiryHeader.StatusCode, PUR_EnquiryHeader.TotalAmount,
Set_PeriodCustom.PeriodSetName
		FROM PUR_EnquiryHeader
        left join inv_StockTransactionheader on inv_StockTransactionheader.StockTransactionID = PUR_EnquiryHeader.FK_Inv_StockTransactionID
			and inv_StockTransactionheader.CompanyID = PUR_EnquiryHeader.CompanyID
			and inv_StockTransactionheader.PeriodID = PUR_EnquiryHeader.PeriodID

 inner join Set_PeriodCustom on  
 PUR_EnquiryHeader.EnquiryDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

LEFT JOIN SET_DocumentType ON PUR_EnquiryHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID

		INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID 
			AND PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID 
			AND PUR_EnquiryDetail.Status = 1 {AdditionalParameters}
            ) AS tbl_EH ";



        const string Query_SelectRealseInProcessData = @" 
SELECT *, ROW_NUMBER() OVER (ORDER BY tbl_EH.DocumentNo desc) AS 'Row' 
FROM 
(
    SELECT DISTINCT inv_StockTransactionheader.DocumentNo as RequisitionNo ,PUR_EnquiryHeader.CompanyID, PUR_EnquiryHeader.PeriodID, PUR_EnquiryHeader.EnquiryID, PUR_EnquiryHeader.DocumentTypeID, 
        PUR_EnquiryHeader.DocumentNo, PUR_EnquiryHeader.DueDate, PUR_EnquiryHeader.ReferenceNumber, PUR_EnquiryHeader.EnquiryDate, PUR_EnquiryHeader.DeliveryDate, 
        PUR_EnquiryHeader.AllQuotationsReceived, PUR_EnquiryHeader.Status, PUR_EnquiryHeader.CreatedBy, PUR_EnquiryHeader.CreateDate, 
        PUR_EnquiryHeader.CreatedByValue, PUR_EnquiryHeader.UpdatedBy, PUR_EnquiryHeader.UpdateDate, PUR_EnquiryHeader.UpdatedByValue, PUR_EnquiryHeader.StatusCode, PUR_EnquiryHeader.TotalAmount
		FROM PUR_EnquiryHeader
        left join inv_StockTransactionheader on inv_StockTransactionheader.StockTransactionID = PUR_EnquiryHeader.FK_Inv_StockTransactionID
			and inv_StockTransactionheader.CompanyID = PUR_EnquiryHeader.CompanyID
			and inv_StockTransactionheader.PeriodID = PUR_EnquiryHeader.PeriodID
		INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID 
			AND PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID 
			AND PUR_EnquiryDetail.Status = 1  
			
        {AdditionalParameters}
			
            ) AS tbl_EH ";

        const string Query_SelectAll_Searching = @" 
SELECT DISTINCT PUR_EnquiryHeader.CompanyID
	,PUR_EnquiryHeader.PeriodID
	,PUR_EnquiryHeader.EnquiryID
	,PUR_EnquiryHeader.DocumentTypeID
    ,SET_DocumentType.DocumentType
	,PUR_EnquiryHeader.DocumentNo
	,PUR_EnquiryHeader.DueDate
	,PUR_EnquiryHeader.ReferenceNumber
	,PUR_EnquiryHeader.EnquiryDate
	,PUR_EnquiryHeader.DeliveryDate
	,PUR_EnquiryHeader.AllQuotationsReceived
	,PUR_EnquiryHeader.Status
	,PUR_EnquiryHeader.CreatedBy
	,PUR_EnquiryHeader.CreateDate
	,PUR_EnquiryHeader.CreatedByValue
	,PUR_EnquiryHeader.UpdatedBy
	,PUR_EnquiryHeader.UpdateDate
	,PUR_EnquiryHeader.UpdatedByValue
	,PUR_EnquiryHeader.StatusCode
	,PUR_EnquiryHeader.TotalAmount
    ,Set_PeriodCustom.PeriodSetName
FROM PUR_EnquiryHeader
INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID 
	AND PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID 
	AND PUR_EnquiryDetail.Status = 1

 inner join Set_PeriodCustom on  
 PUR_EnquiryHeader.EnquiryDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

LEFT JOIN SET_DocumentType ON PUR_EnquiryHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID

";

        const string Query_SelectAll_Searching_ForApprove = @" 
SELECT DISTINCT PUR_EnquiryHeader.CompanyID
	,PUR_EnquiryHeader.PeriodID
	,PUR_EnquiryHeader.EnquiryID
	,PUR_EnquiryHeader.DocumentTypeID
    ,SET_DocumentType.DocumentType
	,PUR_EnquiryHeader.DocumentNo
	,PUR_EnquiryHeader.DueDate
	,PUR_EnquiryHeader.ReferenceNumber
	,PUR_EnquiryHeader.EnquiryDate
	,PUR_EnquiryHeader.DeliveryDate
	,PUR_EnquiryHeader.AllQuotationsReceived
	,PUR_EnquiryHeader.Status
	,PUR_EnquiryHeader.CreatedBy
	,PUR_EnquiryHeader.CreateDate
	,PUR_EnquiryHeader.CreatedByValue
	,PUR_EnquiryHeader.UpdatedBy
	,PUR_EnquiryHeader.UpdateDate
	,PUR_EnquiryHeader.UpdatedByValue
	,PUR_EnquiryHeader.StatusCode
	,PUR_EnquiryHeader.TotalAmount
	,SET_DocumentApprove.ApprovalStatus
	,SET_UserApprovalAmount.ApprovalOperator
	,SET_ApprovalAuthority.UserID AS UserID
	,SET_Authority.AuthorityLevel
FROM PUR_EnquiryHeader
INNER JOIN PUR_EnquiryDetail ON PUR_EnquiryDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID 
	AND PUR_EnquiryDetail.CompanyID = PUR_EnquiryHeader.CompanyID AND PUR_EnquiryDetail.PeriodID = PUR_EnquiryHeader.PeriodID 
	AND PUR_EnquiryDetail.Status = 1
LEFT JOIN SET_DocumentType ON PUR_EnquiryHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = PUR_EnquiryHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = PUR_EnquiryHeader.PeriodID AND SET_DocumentApprove.Status =1 
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
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1 ";



        const string Query_GetRFQQuantityAgainstRequisition = @" 
SELECT ISNULL(ABS(SUM(Pur_EnquiryDetail.Quantity)), 0) AS RFQQuantity 
FROM Pur_EnquiryDetail 
    INNER JOIN Pur_EnquiryHeader ON Pur_EnquiryHeader.CompanyID = Pur_EnquiryDetail.CompanyID AND Pur_EnquiryHeader.PeriodID = Pur_EnquiryDetail.PeriodID 
        AND Pur_EnquiryHeader.EnquiryID = Pur_EnquiryDetail.EnquiryHeaderID AND Pur_EnquiryHeader.Status = 1 AND Pur_EnquiryHeader.StatusCode = 1 
    INNER JOIN Pur_EnquiryRequisition ON Pur_EnquiryRequisition.CompanyID = Pur_EnquiryDetail.CompanyID 
        AND Pur_EnquiryRequisition.PeriodID = Pur_EnquiryDetail.PeriodID AND Pur_EnquiryRequisition.EnquiryHeaderID = Pur_EnquiryDetail.EnquiryHeaderID 
        AND Pur_EnquiryRequisition.EnquiryDetailID = Pur_EnquiryDetail.EnquiryDetailID 
WHERE Pur_EnquiryRequisition.CompanyID = @CompanyID AND Pur_EnquiryRequisition.PeriodID = @PeriodID AND Pur_EnquiryRequisition.StockHeaderID = @RequisitionHeaderID 
    AND Pur_EnquiryRequisition.StockDetailID = @RequisitionDetailID AND Pur_EnquiryRequisition.DocumentTypeID = @RequisitionDocumentTypeID 
    AND Pur_EnquiryRequisition.DocumentNo = @RequisitionNo AND Pur_EnquiryDetail.Status = 1 ";

        const string Query_Update_By_DDLValues = @"UPDATE PUR_EnquiryHeader
SET StatusCOde = @StatusCode
where PUR_EnquiryHeader.DocumentTypeID=@DocumentTypeID AND PUR_EnquiryHeader.EnquiryID=@EnquiryID
AND PUR_EnquiryHeader.CompanyID = @CompanyID  AND PUR_EnquiryHeader.PeriodID = @PeriodID";


         const string Query_Get_Al_imdadAllData = @" 
SELECT DISTINCT PUR_EnquiryHeader.EnquiryID, PUR_EnquiryHeader.DocumentNo, PUR_EnquiryHeader.EnquiryDate, PUR_EnquiryHeader.ReferenceNumber
FROM PUR_EnquiryHeader 
    LEFT JOIN PUR_QuotationDetail ON PUR_QuotationDetail.EnquiryHeaderID = PUR_EnquiryHeader.EnquiryID AND PUR_QuotationDetail.CompanyID = PUR_EnquiryHeader.CompanyID 
        AND PUR_QuotationDetail.PeriodID = PUR_EnquiryHeader.PeriodID AND PUR_QuotationDetail.Status = 1 

    LEFT JOIN  (Select AP_PurchaseOrderDetail.Quantity,AP_PurchaseOrderDetail.FromHeaderID,AP_PurchaseOrderDetail.CompanyID,AP_PurchaseOrderDetail.PeriodID,AP_PurchaseOrderDetail.FromDetailID  from  AP_PurchaseOrderDetail
		 INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
		 AND AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderDetail.Status = 1 
		AND AP_PurchaseOrderHeader.TypeCode  in 
		( Select DocumentTypeID from SET_DocumentType Where  MenuID in (Select MenuID from ApplicationMenu Where  MenuPath in ('/AP/Transaction/PurchaseOrder_frm.aspx')))
		Where AP_PurchaseOrderDetail.Status = 1 ) AP_PurchaseOrderDetail ON AP_PurchaseOrderDetail.FromHeaderID = PUR_QuotationDetail.QuotationHeaderID 
        AND AP_PurchaseOrderDetail.FromDetailID = PUR_QuotationDetail.QuotationDetailID AND AP_PurchaseOrderDetail.CompanyID = PUR_QuotationDetail.CompanyID 
        AND AP_PurchaseOrderDetail.PeriodID = PUR_QuotationDetail.PeriodID  

WHERE PUR_EnquiryHeader.CompanyID = @CompanyID AND PUR_EnquiryHeader.PeriodID = @PeriodID AND PUR_EnquiryHeader.Status = 1 AND PUR_EnquiryHeader.StatusCode = 1 ";

        #endregion

        #region Class Methods

        public bool InsertEHeaderAndDetail(List<PUR_EnquiryDetail> colEDetail, List<PUR_EnquirySupplier> colESDetail, List<Pur_EnquiryRequisition> ColPurRequisition,
            SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertEnquiry");

            try
            {
                this.Insert(sqltrans);

                foreach (PUR_EnquiryDetail ed in colEDetail)
                {
                    ed.EnquiryDetailID = Convert.ToInt16(PUR_EnquiryDetail.GetNewEnquiryDetailID(CompanyID, EnquiryID, PeriodID, sqltrans));
                    ed.CompanyID = this.CompanyID;
                    ed.Insert(sqltrans);

                    #region Insert RFQ Suppliers

                    if (colESDetail != null)
                    {
                        var rfqSuppliers = from objRFQSupplier in colESDetail
                                           where objRFQSupplier.RowUniqueID == ed.RowUniqueID
                                           select objRFQSupplier;

                        foreach (PUR_EnquirySupplier es in rfqSuppliers)
                        {
                            es.EnquiryHeaderID = this.f_EnquiryID;
                            es.EnquiryDetailID = ed.EnquiryDetailID;
                            es.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Insert RFQ Requisitions

                    if (ColPurRequisition != null)
                    {
                        var Records = from objPurRequisition in ColPurRequisition
                                      where objPurRequisition.RowUniqueID == ed.RowUniqueID
                                      select objPurRequisition;

                        foreach (Pur_EnquiryRequisition er in Records)
                        {
                            er.EnquiryHeaderID = (int)this.EnquiryID;
                            er.EnquiryDetailID = (short)ed.EnquiryDetailID;
                            er.CompanyID = this.CompanyID;
                            er.Insert(sqltrans);

                            #region Update IsRequisition Completed

                            INV_StockTransactionHeader objRequisition = new INV_StockTransactionHeader(er.CompanyID, er.PeriodID, er.StockHeaderID, null, sqltrans);

                            if (objRequisition.IsDataLoaded)
                            {
                                bool isRequisitionCompleted = false;
                                DataTable dtRequisitionDetails = INV_StockTransactionDetail.GetDataByTransactionHeaderID(objRequisition.StockTransactionID, 
                                    objRequisition.CompanyID, objRequisition.PeriodID, true, sqltrans);

                                foreach(DataRow drRequisitionDetail in dtRequisitionDetails.Rows)
                                {
                                    decimal requisitionQuantity = drRequisitionDetail.Field<decimal>("TransactionQuantity");
                                    decimal ginQuantity = INV_StockTransactionHeader.GetGINQuantityAgainstRequisition(er.CompanyID, er.PeriodID, 
                                        objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID, 
                                        objRequisition.DocumentNo, sqltrans);

                                    decimal rfqQuantity = GetRFQQuantityAgainstRequisition(er.CompanyID, er.PeriodID, objRequisition.StockTransactionID, 
                                        drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID, objRequisition.DocumentNo, sqltrans);

                                    decimal balanceQuantity = requisitionQuantity - ginQuantity - rfqQuantity;

                                    if (balanceQuantity > 0)
                                    {
                                        isRequisitionCompleted = false;
                                        break;
                                    }
                                    else
                                        isRequisitionCompleted = true;
                                }

                                if(isRequisitionCompleted)
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, true, sqltrans);
                                else
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, false, sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.DocumentNo = this.DocumentNo.ToString();
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertEnquiry");
            }

            return result;
        }

        public bool UpdateRFQHeaderAndDetail(List<PUR_EnquiryDetail> colAddEDetail, List<PUR_EnquiryDetail> colUpdateEDetail, List<PUR_EnquiryDetail> colDeleteEDetail,
            List<PUR_EnquirySupplier> colAddESDetail, List<PUR_EnquirySupplier> colUpdateESDetail, List<Pur_EnquiryRequisition> colERequisitionAdd,
            List<Pur_EnquiryRequisition> colERequisitionUpdate, SET_DocumentApprove da)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateEnquiry");

            try
            {
                this.UpdateDataByKey(sqltrans);

                PUR_EnquirySupplier.DeleteDetailDataByKeys(sqltrans, f_CompanyID, f_PeriodID, f_EnquiryID);
                Pur_EnquiryRequisition.DeleteDataByEnquiryHeaderID(sqltrans, f_CompanyID, f_PeriodID, f_EnquiryID);

                foreach (PUR_EnquiryDetail rfqDetail in colAddEDetail)
                {
                    rfqDetail.EnquiryDetailID = Convert.ToInt16(PUR_EnquiryDetail.GetNewEnquiryDetailID(CompanyID, EnquiryID, PeriodID, sqltrans));
                    rfqDetail.Insert(sqltrans);

                    #region Insert RFQ Suppliers

                    if (colAddESDetail != null)
                    {
                        var rfqSuppliers = from objRFQSupplier in colAddESDetail
                                           where objRFQSupplier.RowUniqueID == rfqDetail.RowUniqueID
                                           select objRFQSupplier;

                        foreach (PUR_EnquirySupplier objRFQSupplier in rfqSuppliers)
                        {
                            objRFQSupplier.EnquiryDetailID = rfqDetail.EnquiryDetailID;
                            objRFQSupplier.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Insert RFQ Requisitions

                    if (colERequisitionAdd != null)
                    {
                        var rfqRequisitions = from objPurRequisition in colERequisitionAdd
                                              where objPurRequisition.RowUniqueID == rfqDetail.RowUniqueID
                                              select objPurRequisition;

                        foreach (Pur_EnquiryRequisition er in rfqRequisitions)
                        {
                            er.EnquiryHeaderID = Convert.ToInt32( this.EnquiryID);
                            er.EnquiryDetailID = (short)rfqDetail.EnquiryDetailID;
                            er.Insert(sqltrans);

                            #region Update IsRequisition Completed

                            INV_StockTransactionHeader objRequisition = new INV_StockTransactionHeader(er.CompanyID, er.PeriodID, er.StockHeaderID, null, sqltrans);

                            if (objRequisition.IsDataLoaded)
                            {
                                bool isRequisitionCompleted = false;
                                DataTable dtRequisitionDetails = INV_StockTransactionDetail.GetDataByTransactionHeaderID(objRequisition.StockTransactionID,
                                    objRequisition.CompanyID, objRequisition.PeriodID, true, sqltrans);

                                foreach (DataRow drRequisitionDetail in dtRequisitionDetails.Rows)
                                {
                                    decimal requisitionQuantity = drRequisitionDetail.Field<decimal>("TransactionQuantity");
                                    decimal ginQuantity = INV_StockTransactionHeader.GetGINQuantityAgainstRequisition(er.CompanyID, er.PeriodID, er.StockHeaderID,
                                        er.StockDetailID, er.DocumentTypeID, er.DocumentNo, sqltrans);

                                    decimal rfqQuantity = GetRFQQuantityAgainstRequisition(er.CompanyID, er.PeriodID, er.StockHeaderID, er.StockDetailID,
                                        er.DocumentTypeID, er.DocumentNo, sqltrans);

                                    decimal balanceQuantity = requisitionQuantity - ginQuantity - rfqQuantity;

                                    if (balanceQuantity > 0)
                                    {
                                        isRequisitionCompleted = false;
                                        break;
                                    }
                                    else
                                        isRequisitionCompleted = true;
                                }

                                if (isRequisitionCompleted)
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, true, sqltrans);
                                else
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, false, sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion
                }

                foreach (PUR_EnquiryDetail rfqDetail in colUpdateEDetail)
                {
                    rfqDetail.UpdateDataByKey(sqltrans);

                    #region Update RFQ Suppliers

                    if (colUpdateESDetail != null)
                    {
                        var rfqSuppliers = from objRFQSupplier in colUpdateESDetail
                                           where objRFQSupplier.RowUniqueID == rfqDetail.RowUniqueID
                                           select objRFQSupplier;

                        foreach (PUR_EnquirySupplier esUpdate in rfqSuppliers)
                        {
                            esUpdate.EnquiryDetailID = rfqDetail.EnquiryDetailID;
                            esUpdate.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Update RFQ Requisitions

                    if (colERequisitionUpdate != null)
                    {
                        var rfqRequisitions = from objRFQRequisition in colERequisitionUpdate
                                              where objRFQRequisition.RowUniqueID == rfqDetail.RowUniqueID
                                              select objRFQRequisition;

                        foreach (Pur_EnquiryRequisition er in rfqRequisitions)
                        {
                            er.EnquiryHeaderID = Convert.ToInt32(this.EnquiryID);
                            er.EnquiryDetailID = (short)rfqDetail.EnquiryDetailID;
                            er.Insert(sqltrans);

                            #region Update IsRequisition Completed

                            INV_StockTransactionHeader objRequisition = new INV_StockTransactionHeader(er.CompanyID, er.PeriodID, er.StockHeaderID, null, sqltrans);

                            if (objRequisition.IsDataLoaded)
                            {
                                bool isRequisitionCompleted = false;
                                DataTable dtRequisitionDetails = INV_StockTransactionDetail.GetDataByTransactionHeaderID(objRequisition.StockTransactionID,
                                    objRequisition.CompanyID, objRequisition.PeriodID, true, sqltrans);

                                foreach (DataRow drRequisitionDetail in dtRequisitionDetails.Rows)
                                {
                                    decimal requisitionQuantity = drRequisitionDetail.Field<decimal>("TransactionQuantity");
                                    decimal ginQuantity = INV_StockTransactionHeader.GetGINQuantityAgainstRequisition(er.CompanyID, er.PeriodID, er.StockHeaderID,
                                        er.StockDetailID, er.DocumentTypeID, er.DocumentNo, sqltrans);

                                    decimal rfqQuantity = GetRFQQuantityAgainstRequisition(er.CompanyID, er.PeriodID, er.StockHeaderID, er.StockDetailID,
                                        er.DocumentTypeID, er.DocumentNo, sqltrans);

                                    decimal balanceQuantity = requisitionQuantity - ginQuantity - rfqQuantity;

                                    if (balanceQuantity > 0)
                                    {
                                        isRequisitionCompleted = false;
                                        break;
                                    }
                                    else
                                        isRequisitionCompleted = true;
                                }

                                if (isRequisitionCompleted)
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, true, sqltrans);
                                else
                                    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.StockHeaderID, false, sqltrans);
                            }

                            #endregion
                        }
                    }

                    #endregion
                }

                #region Delete Enquiry Details, which are deleted by the user

                foreach (PUR_EnquiryDetail edDelete in colDeleteEDetail)
                {
                    PUR_EnquiryDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, edDelete.EnquiryDetailID,
                        EnquiryID, f_PeriodID);
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.DocumentNo = this.DocumentNo.ToString();
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateEnquiry");
            }

            return result;
        }

        public bool DeleteEHeaderAndDetail()
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteEnquiry");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, EnquiryID, f_PeriodID);

                PUR_EnquiryDetail.DeleteDataByHeader(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, EnquiryID, f_PeriodID);

                PUR_EnquirySupplier.DeleteDetailDataByKeys(sqltrans, f_CompanyID, f_PeriodID, EnquiryID);

                Pur_EnquiryRequisition.DeleteDataByEnquiryHeaderID(sqltrans, CompanyID, PeriodID, EnquiryID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteEnquiry");
            }

            return result;
        }

        public bool UpdateEnquiryHeaderForApprove(SET_DocumentApprove dA,int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateEnquiryHeader");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);

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
                    SET_DocumentApprove.DeleteDataByDocNoanddocTypeid(f_CompanyID, f_PeriodID, int.Parse(dA.DocumentNo), dA.DocumentTypeID, sqltrans,CurrentFiscalID);


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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateEnquiryHeader");
            }

            return result;
        }

        public static DataTable GetAllDataWithOtherTables(int companyID, int periodID, string filter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                string Query_Get_AllData = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Get_AllData.ToString();
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Get_AllData , filter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc,int CurrentFiscalID = 0)
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
                    string query = Query_SelectAll_Searching_WithPaging.Replace("{AdditionalParameters}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }




        //For RFQ relase To Inprocess



        public static DataTable RFQReleaseToInprocessData(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
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
                    string query = Query_SelectRealseInProcessData.Replace("{AdditionalParameters}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_Searching + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null,
            int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc,int CurrentFiscalID =0)
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
                    string query = FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_SelectAll_Searching_WithPaging_ForApprove.Replace("{AdditionalParameters}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

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

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int enquiryID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryID", enquiryID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetRFQReport(int companyID, int periodId, int? rfqNoFrom, int? rfqNoTo, int statusCode, short supplierID, int CurrentFiscalID = 0)
        {
            DataTable dtRFQ = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                

                string query = string.Empty;

                if (rfqNoFrom != null && rfqNoTo != null)
                    query = " AND [PUR_EnquiryHeader].[DocumentNo] BETWEEN " + rfqNoFrom + " AND " + rfqNoTo;
                else if (rfqNoFrom != null)
                    query = query + " AND [PUR_EnquiryHeader].[DocumentNo] >= " + rfqNoFrom;
                else if (rfqNoTo != null)
                    query = query + " AND [PUR_EnquiryHeader].[DocumentNo] <= " + rfqNoTo;

                if (statusCode != null)
                    query = query + " AND [PUR_EnquiryHeader].StatusCode = " + statusCode;

                if (supplierID > 0)
                    query = query + " AND [PUR_EnquirySupplier].[SupplierID] = " + supplierID;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format( FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_RFQReport, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtRFQ = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtRFQ;
        }

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int enquiryID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryID", enquiryID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Purchase.PUR_EnquiryHeader.Query_Select_DataByKey_ForApprove, sqlparams);

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

        public bool UpdateFlagHeader(SqlTransaction objTransaction, long enquiryHeaderID, bool isAllQuotationsReceived, int companyID, int periodID, Guid updatedBy,
            DateTime updateDate, string updatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[1] = new SqlParameter("@IsAllQuotationsReceived", isAllQuotationsReceived);
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

        public static bool UpdateFlagHeader(int companyID, int periodID, long enquiryHeaderID, bool isAllQuotationsReceived, short statusCode, Guid updatedBy, 
            DateTime updateDate, string updatedByValue, SqlTransaction objTransaction)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@EnquiryHeaderID", enquiryHeaderID);
                sqlparams[3] = new SqlParameter("@IsAllQuotationsReceived", isAllQuotationsReceived);
                sqlparams[4] = new SqlParameter("@StatusCode", statusCode);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static decimal GetRFQQuantityAgainstRequisition(int companyID, int periodID, int requisitionHeaderID, long requisitionDetailID, int requisitionDocumentTypeID, long requisitionNo, SqlTransaction objTransaction = null)
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
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetRFQQuantityAgainstRequisition, sqlparams));
                else
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetRFQQuantityAgainstRequisition, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return quantityToReturn;
        }



        public bool UpdateRFQHeaderStatus(SET_DocumentApprove da)
        {
            bool result = false;

            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateRFQ");

            try
            {
                this.UpdateDataByKey(sqltrans);

                //AP_PurchaseOrderRequisition.DeletePORequisition(f_CompanyID, f_PeriodID, this.POHeaderID, sqltrans);

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                //if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                //da.Insert(sqltrans);
                da.DeleteDataByDocIDAndDocNo(sqltrans, f_CompanyID, da.DocumentTypeID, int.Parse(da.DocumentNo), f_PeriodID);


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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateRFQ");
            }

            return result;
        }

        public bool UpdateDataByDDLValues(int Companyid, int perioid, int DocmntTypeID, int ReqNO, int StatusCode)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@EnquiryID", ReqNO);
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

        public static DataTable GetAllAl_ImdadDataWithOtherTables(int companyID, int periodID, string filter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Al_imdadAllData + filter, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetRequisitionNO(int EnquiryHeaderID,int CompanyID)
        {
            DataTable dt = null;

            const string Query = @"  select
   distinct  
    ISNULL(stuff((
        select distinct ',' + convert(varchar(10),u.DocumentNo)
        from Pur_EnquiryRequisition u
        where u.EnquiryHeaderID = EnquiryHeaderID
		and u.COmpanyID = COmpanyID
		and CompanyID = @CompanyID
		and EnquiryHeaderID = @EnquiryHeaderID
        for xml path('')
    ),1,1,''),'') as ReqNO
from Pur_EnquiryRequisition
group by EnquiryHeaderID


 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@EnquiryHeaderID", EnquiryHeaderID);


                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetStockTransactionID(int EnquiryHeaderID, int CompanyID)
        {
            DataRow dr = null;

            const string Query = @"  SELECT DISTINCT Pur_EnquiryRequisition.StockHeaderID FROM dbo.Pur_EnquiryRequisition
WHERE Pur_EnquiryRequisition.CompanyID = @CompanyID AND Pur_EnquiryRequisition.EnquiryHeaderID = @EnquiryHeaderID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@EnquiryHeaderID", EnquiryHeaderID);

                DataSet ds = null;

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
        #endregion
    }
}
