using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;
using System.Text;
using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Purchase;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.GL.Transaction;

namespace FnI.BusinessAccessLayer.FA.Transaction
{
    public partial class FA_LetterOfCreditHeader
    {
        #region Class Methods

        public bool InsertPOHeaderAndDetail( List<FA_LetterOfCreditDetail> colPODetail,List<FA_LCShipment> colLCShipmentAdd, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans,CurrentFiscalID).ToString();
                this.Insert(sqltrans);

                foreach (FA_LetterOfCreditDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKey(CompanyID, pod.ItemID, null, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = FA_LetterOfCreditHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));
                        amount = Math.Round(amount);

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.LCDetailID;
                    pod.LCDetailID = short.Parse(FA_LetterOfCreditDetail.GetNewLCDetailID(CompanyID, PeriodID, LCHeaderID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.Insert(sqltrans);

                    #region Checking For Purchase Order Complete Status

                    if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
                }

                foreach (FA_LCShipment lcs in colLCShipmentAdd)
                {

                    int tempLCShipmentID = lcs.LCDetailShipmentID;
                    lcs.LCDetailShipmentID = short.Parse(FA_LCShipment.GetNewLCDetailShipmentID(CompanyID, PeriodID, LCHeaderID, sqltrans).ToString());
                    //lcs.SerialNo = serialNo++;
                    lcs.Insert(sqltrans);

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }

        public bool InsertPOHeaderAndDetail(List<FA_LetterOfCreditDetail> colPODetail, List<FA_LCShipment> colLCShipmentAdd, SET_DocumentApprove da, bool IsPerfoance = true, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                this.DocumentNumber = FA_LetterOfCreditHeader.GetNewLCDocumentNumber(f_CompanyID, f_PeriodID, f_TypeCode,sqltrans).ToString();
                //SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (FA_LetterOfCreditDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKeyTransaction(CompanyID, pod.ItemID, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = FA_LetterOfCreditHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.LCDetailID;
                    pod.LCDetailID = short.Parse(FA_LetterOfCreditDetail.GetNewLCDetailID(CompanyID, LCHeaderID, PeriodID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.FiscalID = CurrentFiscalID;
                    pod.Insert(sqltrans);
                    //pod.InsertAlhaj(sqltrans);
                    
                    #region Checking For Purchase Order Complete Status

                    if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
                }

                foreach (FA_LCShipment lcs in colLCShipmentAdd)
                {

                    int tempLCShipmentID = lcs.LCDetailShipmentID;
                    lcs.LCDetailShipmentID = short.Parse(FA_LCShipment.GetNewLCDetailShipmentID(CompanyID, PeriodID, LCHeaderID, sqltrans).ToString());
                    //lcs.SerialNo = serialNo++;
                    lcs.Insert(sqltrans);

                }
                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }

        public bool UpdatePOHeaderAndDetail(List<FA_LetterOfCreditDetail> colAddPODetail, List<FA_LetterOfCreditDetail> colUpdatePODetail,
            List<FA_LetterOfCreditDetail> colDeletePODetail, List<FA_LCShipment> colLCShipmentAdd, List<FA_LCShipment> colLCShipmentUpdate,
            List<FA_LCShipment> colLCShipmentDelete, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

            try
            {
                this.FiscalID = CurrentFiscalID; 

                this.UpdateDataByKey(sqltrans);

                foreach (FA_LetterOfCreditDetail podAdd in colAddPODetail)
                {
                    podAdd.LCDetailID = short.Parse(FA_LetterOfCreditDetail.GetNewLCDetailID(CompanyID, PeriodID, LCHeaderID, sqltrans).ToString());
                    podAdd.SerialNo = serialNo++;
                    podAdd.FiscalID = CurrentFiscalID;
                    podAdd.Insert(sqltrans);

                    #region Checking For Purchase Order Complete Status

                    if (podAdd.FromHeaderID.HasValue && podAdd.FromDetailID.HasValue && f_DocumentDate.HasValue)
                    {
                        CheckPurchaseStatus(podAdd.FromHeaderID.Value, podAdd.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, 
                            f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Add", f_DocumentDate.Value);
                    }

                    #endregion
                }

                foreach (FA_LCShipment lcsAdd in colLCShipmentAdd)
                {
                    lcsAdd.LCDetailShipmentID = short.Parse(FA_LCShipment.GetNewLCDetailShipmentID(CompanyID, PeriodID, LCHeaderID, sqltrans).ToString());
                    //lcsAdd.SerialNo = serialNo++;
                    //lcsAdd.FiscalID = CurrentFiscalID;
                    lcsAdd.Insert(sqltrans);
                }

                foreach (FA_LetterOfCreditDetail podUpdate in colUpdatePODetail)
                {
                    podUpdate.SerialNo = serialNo++;
                    podUpdate.FiscalID = CurrentFiscalID;
                    podUpdate.UpdateDataByKey(sqltrans);

                    #region Checking For Purchase Order Complete Status

                    if (podUpdate.FromHeaderID.HasValue && podUpdate.FromDetailID.HasValue && f_DocumentDate.HasValue)
                    {
                        CheckPurchaseStatus(podUpdate.FromHeaderID.Value, podUpdate.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, 
                            f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Update", f_DocumentDate.Value);
                    }

                    #endregion
                }

                foreach (FA_LCShipment lcsUpdate in colLCShipmentUpdate)
                {
                    //lcsUpdate.SerialNo = serialNo++;
                    //lcsUpdate.FiscalID = CurrentFiscalID;
                    lcsUpdate.UpdateDataByKey(sqltrans);
                }

                foreach (FA_LetterOfCreditDetail podDelete in colDeletePODetail)
                {
                    FA_LetterOfCreditDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, podDelete.LCDetailID, LCHeaderID,PeriodID);

                    #region Checking For Purchase Order Complete Status

                    if (podDelete.FromHeaderID.HasValue && podDelete.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(podDelete.FromHeaderID.Value, podDelete.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, false, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Delete", f_DocumentDate.Value);

                    #endregion
                }

                foreach (FA_LCShipment lcsDelete in colLCShipmentDelete)
                {
                    FA_LCShipment.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, lcsDelete.LCDetailShipmentID, LCHeaderID, PeriodID);

                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }

        public bool DeletePOHeaderAndDetail(List<FA_LetterOfCreditDetail> colDeletePODetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeletePO");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, LCHeaderID);

                foreach (FA_LetterOfCreditDetail podDelete in colDeletePODetail)
                {
                    FA_LetterOfCreditDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID,  podDelete.LCDetailID, LCHeaderID,PeriodID);

                    #region Checking For Purchase Order Complete Status

                    if (podDelete.FromHeaderID.HasValue && podDelete.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(podDelete.FromHeaderID.Value, podDelete.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, false, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Delete", f_DocumentDate.Value);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeletePO");
            }
            return result;
        }

        public bool UpdatePOHeaderForApprove(SET_DocumentApprove dA,int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePOHeader");

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePOHeader");
            }

            return result;
        }

        public bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, 
            int pOHeaderID, string Query = "")
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                string Query_Delete_ByKeyForIndent = @" Update FA_LetterOfCreditHeader 
Set Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and {0} ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
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

        public bool UpdatePOHeaderStatus(SET_DocumentApprove da)
        {
            bool result = false;
            
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }

        public static DataTable GetAmountQuantity(int companyID, int periodID, int ItemID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);

                string Query_getAmountQuantity = @" Select IsNull(Sum(Quantity * UnitPrice), 0) As Amount, IsNull(Sum(IsNull(Quantity, 0)), 0) As TransactionQuantity 
From AP_PurchaseOrderDetail 
    Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        AND AP_PurchaseOrderHeader.Status = 1 
Where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.ItemID = @ItemID ";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_getAmountQuantity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_getAmountQuantity, sqlparams);

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


        private void CheckPurchaseStatus(int quotationHeaderID, short quotationDetailID, int companyID, int periodID, SqlTransaction sqltrans, bool isPurchased, Guid updatedBy, DateTime updateDate, string updatedByValue, string operation, DateTime orderDate)
        {
            try
            {
                bool result = false;
                PUR_QuotationHeader qh = new PUR_QuotationHeader();
                result = qh.UpdateFlagDetail(sqltrans, quotationHeaderID, quotationDetailID, isPurchased, companyID, periodID, updatedBy, updateDate, updatedByValue, orderDate);

                if (result)
                {
                    if (operation.Equals("Delete") || qh.CheckPurchaseStatus(sqltrans, quotationHeaderID, isPurchased, companyID, periodID))
                        qh.UpdateFlagHeader(sqltrans, quotationHeaderID, isPurchased, companyID, periodID, updatedBy, updateDate, updatedByValue);
                }
                else
                    throw new BusinessLogicException("Error generated: AP_PurchaseOrderHeader.cs, Line: 649, Function: POH.UpdateFlagDetail");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }


        public static DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_ForApprove + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
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

        public static DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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

                    #region Query

                    string Query_SelectAll_Searching_WithPaging = @" select *, ROW_NUMBER() OVER (ORDER BY FA_LetterOfCreditHeader.LCHeaderID desc) AS 'Row' from 
(
    SELECT DISTINCT poh.LCHeaderID, poh.CompanyID, poh.PeriodID, poh.Status, poh.TypeCode, SET_DocumentType.DocumentType, SET_DocumentType.DocumentTypeID, poh.DocumentNumber, poh.DocumentDate, 
        ISNULL(poh.VendorID, '-1') as VendorID, AR_SET_Customer.CustomerName as SupplierName, poh.AcceptanceDueDate, SET_DocumentApprove.CreatedByValue, 
        poh.StatusCode,Set_PeriodCustom.PeriodSetName,poh.LCNo
    FROM FA_LetterOfCreditHeader poh 
        INNER JOIN FA_LetterOfCreditDetail pod ON pod.LCHeaderID = poh.LCHeaderID And poh.status = 1 and pod.Status = 1 AND pod.CompanyID = poh.CompanyID 
            AND pod.PeriodID = poh.PeriodID 
inner join Set_PeriodCustom on poh.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = poh.CompanyID AND SET_DocumentType.PeriodID = poh.PeriodID 
            AND SET_DocumentType.DocumentTypeID = poh.TypeCode 
        LEFT JOIN AR_SET_Customer ON AR_SET_Customer.Customerid = poh.VendorID and AR_SET_Customer.Status = 1 -- AND AR_SET_Customer.CompanyID = poh.CompanyID 
            AND AR_SET_Customer.PeriodID = poh.PeriodID 
        left JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = poh.CompanyID AND SET_DocumentApprove.PeriodID = poh.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = poh.TypeCode AND SET_DocumentApprove.DocumentNo = poh.DocumentNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = poh.CompanyID AND PeriodID = poh.PeriodID AND DocumentTypeID = poh.TypeCode AND DocumentNo = poh.DocumentNumber
            )
) FA_LetterOfCreditHeader ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByKeyWithOtherTables(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> LCHeaderID, int periodID, int companyID, string AllowedPeriods,
            string appendedFilter = "", SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (LCHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@LCHeaderID", LCHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@LCHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                string Query_Select_By_PrimaryKey_WithOtherTables = @"";
                if (AllowedPeriods != "")
                    Query_Select_By_PrimaryKey_WithOtherTables = FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_Select_By_PrimaryKey_WithOtherTables.Replace("@PeriodID", AllowedPeriods);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);

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

        public static DataTable GetDataByKeyWithOtherTablesForConnect(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> LCHeaderID, int periodID, int companyID, string AllowedPeriods,
            string appendedFilter = "", SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (LCHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@LCHeaderID", LCHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@LCHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                string Query_Select_By_PrimaryKey_WithOtherTables = @"";
                if (AllowedPeriods != "")
                    Query_Select_By_PrimaryKey_WithOtherTables = FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_Select_By_PrimaryKey_WithOtherTablesForConnect.Replace("@PeriodID", AllowedPeriods);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);

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

        public static DataTable EstimatedAmountReport( Nullable<DateTime> Date_From, Nullable<DateTime> Date_To,int CompanyID)
        {
        

            

            DataTable dt = null;
            string additionalParameters = string.Empty;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);

                const string EstimatedAmountQuery = @"
                select FA_LetterOfCreditHeader.CompanyID, FA_LetterOfCreditHeader.LCHeaderID as DocumentNumber ,sum(FA_LetterOfCreditDetail.Quantity) as  Quantity
                ,sum(FA_LetterOfCreditDetail.NetAmount) as NetAmount
                ,sum(FA_LetterOfCreditDetail.NetAmount) as NetAmountPKR
                ,SET_Currency .CurrencyName,SET_Currency.CurrencySign,FA_LetterOfCreditHeader.LCNo
                ,IMP_LCRetirement.RetirementDate
                ,case when IMP_LCRetirement.LCOptionCode = 1 then  IMP_LCRetirement.LCValue else 0 end as Self
                ,case when IMP_LCRetirement.LCOptionCode = 2 then  IMP_LCRetirement.LCValue else 0 end as FATR
                ,case when IMP_LCRetirement.LCOptionCode = 3 then  IMP_LCRetirement.LCValue else 0 end as Usance,IMP_LCRetirement.MaturityDate
                ,AP_PurchaseOrderHeader.ShipmentDate
                ,sum(AP_PurchaseOrderDetail.Quantity) as QuantityShipped
                ,0 as QuantityReceived
                ,0 as QuantityTransit
                ,0 as QuantityNotShipped

                from FA_LetterOfCreditHeader
                inner join FA_LetterOfCreditDetail on FA_LetterOfCreditDetail.LCHeaderID = FA_LetterOfCreditHeader.LCHeaderID
                and FA_LetterOfCreditDetail.CompanyID = FA_LetterOfCreditHeader.CompanyID
                and FA_LetterOfCreditDetail.PeriodID = FA_LetterOfCreditHeader.PeriodID
                and FA_LetterOfCreditDetail.Status = 1
                and FA_LetterOfCreditHeader.CompanyID = 1
                inner join SET_Currency on SET_Currency.CurrencyID= FA_LetterOfCreditHeader.CurrencyID

                left join IMP_LCRetirement on IMP_LCRetirement.LCHeaderID = FA_LetterOfCreditHeader.LCHeaderID
                and IMP_LCRetirement.CompanyID = FA_LetterOfCreditHeader.CompanyID
                and IMP_LCRetirement.PeriodID = FA_LetterOfCreditHeader.PeriodID
                and IMP_LCRetirement.Status = 1

                left join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.SaleContractID = FA_LetterOfCreditHeader.POHeaderID
                and AP_PurchaseOrderHeader.CompanyID = FA_LetterOfCreditHeader.CompanyID
                and AP_PurchaseOrderHeader.PeriodID = FA_LetterOfCreditHeader.PeriodID
                and AP_PurchaseOrderHeader.Status = 1

                left join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
                and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
                and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
                and AP_PurchaseOrderDetail.Status = 1 
                WHERE FA_LetterOfCreditHeader.CompanyID=@CompanyID
                {0}

                group by FA_LetterOfCreditHeader.CompanyID,FA_LetterOfCreditHeader.LCHeaderID,FA_LetterOfCreditHeader.TotalAmount
                ,SET_Currency .CurrencyName,SET_Currency.CurrencySign,FA_LetterOfCreditHeader.LCNo
                ,IMP_LCRetirement.RetirementDate,IMP_LCRetirement.LCOptionCode,IMP_LCRetirement.LCValue,IMP_LCRetirement.MaturityDate
                ,AP_PurchaseOrderHeader.ShipmentDate ";

                DataSet ds = null;

                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND convert(date,FA_LetterOfCreditHeader.CreateDate) between @FromDate AND @ToDate ");
                }
                else if (Date_From != null)
                {
                    sb.Append(" AND convert(date,FA_LetterOfCreditHeader.CreateDate) = @FromDate ");
                }
                else if (Date_From == null)
                {
                    sb.Append(" AND convert(date,FA_LetterOfCreditHeader.CreateDate) = @ToDate");
                }

                 ds = SqlHelper.ExecuteDataset(CommandType.Text,string.Format(EstimatedAmountQuery, sb), sqlparams);

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
        public static int GetNewLCDocumentNumber(int companyID, int periodID,int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query = @" SELECT IsNull(max(DocumentNumber), 0) + 1 as DocumentNumber FROM FA_LetterOfCreditHeader 
WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and TypeCode = @DocumentTypeID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

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

        public static DataTable GetLetterOFCreditData(int companyID, int periodID, string LCNO, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (LCNO != null && LCNO != "")
                    sqlparams[2] = new SqlParameter("@LCNO", LCNO);
                else
                    sqlparams[2] = new SqlParameter("@LCNO", DBNull.Value);

                if (FromDate != null)
                    sqlparams[3] = new SqlParameter("@FromDate", FromDate);
                else
                    sqlparams[3] = new SqlParameter("@FromDate", DBNull.Value);

                if (ToDate != null)
                    sqlparams[4] = new SqlParameter("@ToDate", ToDate);
                else
                    sqlparams[4] = new SqlParameter("@ToDate", DBNull.Value);


                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_getDataFromLetterOfCredit, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_getDataFromLetterOfCredit, sqlparams);

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

        public static DataTable GetLCDataByContractID(int companyID, int periodID, int ContractID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractID", ContractID);

                string Query_getAmountQuantity = @" 
Select * from FA_LetterOfCreditHeader
where FA_LetterOfCreditHeader.Status = 1
and FA_LetterOfCreditHeader.CompanyID = @CompanyID
and FA_LetterOfCreditHeader.PeriodID = @PeriodID
and SaleContractID = @ContractID ";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_getAmountQuantity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_getAmountQuantity, sqlparams);

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

        public static DataTable GetLetterOfCreditForDashBoard(int CompanyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);



                const string Query_GetLetterOfCreditForDashBoard = @"
select DocumentDate,LCNo ,AR_SET_Customer .CustomerName,SET_Currency .CurrencyName,FA_LetterOfCreditHeader.BankName,FA_LetterOfCreditHeader.AccountNo
,FA_LetterOfCreditHeader.TotalAmount,FA_LetterOfCreditHeader.RetirementDate
from FA_LetterOfCreditHeader
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = FA_LetterOfCreditHeader.VendorID 
inner join SET_Currency on SET_Currency.CurrencyID = FA_LetterOfCreditHeader.CurrencyID
where FA_LetterOfCreditHeader.Status = 1 and FA_LetterOfCreditHeader.CompanyID = @CompanyID
";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetLetterOfCreditForDashBoard, sqlparams);

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


        public static object GetBankwiseLCDetail(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To)
        {
            DataTable dt = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", Date_From);
                sqlparams[4] = new SqlParameter("@ToDate", Date_To);

                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_BankwiseLCDetail, sqlparams);

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

        public static DataTable GetLetterOfCredit_ImportReport(int companyID, int periodID, Nullable<int> salesOrderNoFrom, Nullable<int> salesOrderNoTo,
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
                    sb.Append(" and Cast(FA_LetterOfCreditHeader.DocumentDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append("and Cast(FA_LetterOfCreditHeader.DocumentDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(FA_LetterOfCreditHeader.DocumentDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                if (salesOrderNoFrom >= 0 && salesOrderNoTo > 0)
                    sb.Append(" And FA_LetterOfCreditHeader.DocumentNumber Between " + salesOrderNoFrom + " And " + salesOrderNoTo + "");
                else if (salesOrderNoFrom > 0 && salesOrderNoTo == 0)
                    sb.Append("And FA_LetterOfCreditHeader.DocumentNumber = " + salesOrderNoFrom);

                if (customerName > 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = " + customerName);

                if (statusCode > 0)
                    sb.Append(" and FA_LetterOfCreditHeader.StatusCode = " + statusCode + "");

                if (documentTypeID.HasValue)
                    sb.Append(" AND SET_DocumentType.DocumentTypeID = " + documentTypeID.Value);

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.FA.Transaction.FA_LetterOfCreditHeader.Query_LetterOfCredit_Import_Rport, sb.ToString()), sqlparams);

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
