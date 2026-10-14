using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.GL.Transaction;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
   public partial class FinishedGoodsDetail
    {

       const string Query_GetDataByKey = @"
SELECT [FinishedGoodsHeader].[CompanyID], [FinishedGoodsHeader].[FinishGoodsHeaderID], [FinishedGoodsHeader].[PeriodID],
[FinishedGoodsHeader].[DocumentTypeID],[FinishedGoodsHeader].[DocumentNo],[FinishedGoodsHeader].[DocumentDate],
 [FinishedGoodsHeader].[WorkOrderNo],[FinishedGoodsHeader].[CustomerID], [FinishedGoodsHeader].[CustomerOrderNo],
 [FinishedGoodsHeader].[CustomerOrderDate],[FinishedGoodsHeader].[CreateDate], [FinishedGoodsHeader].[CreatedByValue],
  [FinishedGoodsHeader].[CreatedBy], [FinishedGoodsHeader].[UpdateDate], [FinishedGoodsHeader].[UpdatedByValue],
  [FinishedGoodsHeader].[UpdatedBy], [FinishedGoodsHeader].[Status],AR_Set_Customer.CustomerName,[FinishedGoodsHeader].[StockTransactionID],
  INV_Item.Description as ItemName,INV_SET_Category.CategoryName,set_Location.LocationName,FinishedGoodsDetail.*,'' as Action
  FROM [FinishedGoodsHeader] 
inner join AR_Set_Customer on AR_Set_Customer.CustomerID = [FinishedGoodsHeader].CustomerID
and AR_Set_Customer.CompanyID = [FinishedGoodsHeader].CompanyID
and AR_Set_Customer.PeriodID = [FinishedGoodsHeader].PeriodID
and AR_Set_Customer.Status = 1 
inner join FinishedGoodsDetail on FinishedGoodsDetail.FinishGoodsHeaderID = FinishedGoodsHeader.FinishGoodsHeaderID
and  FinishedGoodsDetail.CompanyID = FinishedGoodsHeader.CompanyID
and  FinishedGoodsDetail.PeriodID = FinishedGoodsHeader.PeriodID
and FinishedGoodsDetail.Status = 1
inner join INV_SET_Category on INV_SET_Category.CategoryID = FinishedGoodsDetail.CategoryID
and INV_SET_Category.Status = 1
inner join INV_Item on INV_Item.ItemID = FinishedGoodsDetail.ItemID
and INV_Item.Status = 1
left join set_Location on set_Location.LocationID = FinishedGoodsDetail.LocationID
and set_Location.Status = 1
WHERE FinishedGoodsHeader.Status = 1 AND FinishedGoodsHeader.PeriodID =@PeriodID
and FinishedGoodsHeader.CompanyID = @CompanyID and FinishedGoodsHeader.FinishGoodsHeaderID = @FinishGoodsHeaderID
";

       const string Query_GetAlreadyProcessedQuantity = @"
select isnull(sum(Quantity),0) as AlreadyProcessedQuantity from
FinishedGoodsDetail
inner join FinishedGoodsHeader on FinishedGoodsHeader.FinishGoodsHeaderID = FinishedGoodsDetail.FinishGoodsHeaderID
and FinishedGoodsHeader.CompanyID = FinishedGoodsDetail.CompanyID
and FinishedGoodsHeader.Status = 1
where ItemID = @ItemID and FinishedGoodsHeader.WorkOrderNo = @WorkOrderNo

";
       const string Query_GetAlreadyProcessedQuantityFromStock = @"select isnull(sum(INV_StockTransactionDetail.TransactionQuantity),0) as AlreadyProcessedQuantity from
INV_StockTransactionDetail
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
and INV_StockTransactionHeader.Status = 1
where INV_StockTransactionDetail.ItemID = @ItemID and INV_StockTransactionDetail.SourceHeaderID = @WorkOrderNo
and INV_StockTransactionDetail.Status= 1 and INV_StockTransactionHeader.DocumentTypeID = @DocumentTypeID 
";

       public static DataTable GetDataByKeyWithOtherTables(int FinishGoodsHeaderID, int companyID, int periodID)
       {
           DataTable dtToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@FinishGoodsHeaderID", FinishGoodsHeaderID);
               sqlparams[1] = new SqlParameter("@CompanyID", companyID);
               sqlparams[2] = new SqlParameter("@PeriodID", periodID);

               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKey, sqlparams);

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


       public static DataTable GetAlreadyProcessedQuantity(int ItemID, int companyID, int WorkOrderNo)
       {
           DataTable dtToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[3];
               sqlparams[0] = new SqlParameter("@ItemID", ItemID);
               sqlparams[1] = new SqlParameter("@CompanyID", companyID);
               sqlparams[2] = new SqlParameter("@WorkOrderNo", WorkOrderNo);

               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAlreadyProcessedQuantity, sqlparams);

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

       public static DataTable GetAlreadyProcessedQuantityFromStock(int ItemID, int companyID, int WorkOrderNo, int DocumentTypeID)
       {
           DataTable dtToReturn = null;

           try
           {
               SqlParameter[] sqlparams = new SqlParameter[4];
               sqlparams[0] = new SqlParameter("@ItemID", ItemID);
               sqlparams[1] = new SqlParameter("@CompanyID", companyID);
               sqlparams[2] = new SqlParameter("@WorkOrderNo", WorkOrderNo);
               sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
               

               DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAlreadyProcessedQuantityFromStock, sqlparams);

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


       public bool InsertACHeaderAndDetail(List<FinishedGoodsDetail> colAssetCostDetail, FinishedGoodsHeader FGH, SET_DocumentApprove da, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, List<INV_StockTransactionDetail> colSTDetAdd)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertFinishGoodDetail");

           try
           {
               FGH.Insert(sqltrans);

               foreach (FinishedGoodsDetail FGD in colAssetCostDetail)
               {
                   FGD.FinishGoodsDetailID = FinishedGoodsDetail.GetNewFinishGoodsDetailID(FGH.CompanyID, FGH.FinishGoodsHeaderID, sqltrans, null);
                   FGD.Insert(sqltrans);
               }
               #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

               if (FGH.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                   da.Insert(sqltrans);

               #endregion
               if (colSTDetAdd.Count > 0)
               {
                   #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"
                   DataRow dr_documentType = SET_DocumentType.GetDataByKey(FGH.CompanyID, FGH.DocumentTypeID, FGH.PeriodID, null, sqltrans);

                   if (th != null && FGH.StatusCode == 1 && dr_documentType != null
                       && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                       )
                   {
                       #region Insert In GL_TransactionHeader

                       th.DocumentNo = FGH.DocumentNo;
                       th.TransactionID = GL_TransactionHeader.GetNewTransactionID(FGH.CompanyID, FGH.PeriodID, sqltrans);
                       th.Insert(sqltrans);

                       #endregion

                       #region Insert In GL_TransactionDetail

                       foreach (GL_TransactionDetail td in colTranDetail)
                       {
                           td.TransactionID = th.TransactionID;
                           td.SourceHeaderID = FGH.StockTransactionID;
                           td.Insert(sqltrans);
                       }

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
                   SqlHelper.RollBackTransaction(sqltrans, "InsertFinishGoodDetail");
           }

           return result;
       }

       public bool UpdateACHeaderAndDetail(FinishedGoodsHeader FGH, List<FinishedGoodsDetail> colAddAssetCostDetail, List<FinishedGoodsDetail> colUpdateAssetCostDetail, List<FinishedGoodsDetail> colDeleteAssetCostDetail, SET_DocumentApprove da, GL_TransactionHeader th,
            List<GL_TransactionDetail> colTranDetail, List<INV_StockTransactionDetail> colSTDetAdd)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateFinishGoodDetail");

           try
           {
               FGH.UpdateDataByKey(sqltrans);

               foreach (FinishedGoodsDetail ACdAdd in colAddAssetCostDetail)
               {
                   ACdAdd.FinishGoodsDetailID = FinishedGoodsDetail.GetNewFinishGoodsDetailID(FGH.CompanyID, FGH.FinishGoodsHeaderID, sqltrans, null);
                   ACdAdd.Insert(sqltrans);
               }

               foreach (FinishedGoodsDetail ACdUpdate in colUpdateAssetCostDetail)
                   ACdUpdate.UpdateDataByKey(sqltrans);

               foreach (FinishedGoodsDetail ACdDelete in colDeleteAssetCostDetail)
                   FinishedGoodsDetail.DeleteDataByKey(sqltrans, FGH.UpdatedBy.Value, FGH.UpdateDate.Value, FGH.UpdatedByValue,FGH.CompanyID, ACdDelete.FinishGoodsDetailID, FGH.FinishGoodsHeaderID);

               #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

               if (FGH.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                   da.Insert(sqltrans);

               if (colSTDetAdd.Count > 0)
               {
                   #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"
                   DataRow dr_documentType = SET_DocumentType.GetDataByKey(FGH.CompanyID, FGH.DocumentTypeID, FGH.PeriodID, null, sqltrans);

                   if (th != null && FGH.StatusCode == 1 && dr_documentType != null
                       && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                       )
                   {
                       #region Insert In GL_TransactionHeader

                       th.DocumentNo = FGH.DocumentNo;
                       th.TransactionID = GL_TransactionHeader.GetNewTransactionID(FGH.CompanyID, FGH.PeriodID, sqltrans);
                       th.Insert(sqltrans);

                       #endregion

                       #region Insert In GL_TransactionDetail

                       foreach (GL_TransactionDetail td in colTranDetail)
                       {
                           td.TransactionID = th.TransactionID;
                           td.SourceHeaderID = FGH.StockTransactionID;
                           td.Insert(sqltrans);
                       }

                       #endregion
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
                   SqlHelper.RollBackTransaction(sqltrans, "UpdateFinishGoodDetail");
           }

           return result;
       }

       public bool DeleteACHeaderAndDetail(FinishedGoodsHeader FGH, List<FinishedGoodsDetail> colDeleteAssetCostDetail)
       {
           bool result = false;
           SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteFinishGoodDetail");

           try
           {
              FinishedGoodsHeader.DeleteDataByKey(sqltrans,FGH.UpdatedBy.Value,FGH.UpdateDate.Value, FGH.UpdatedByValue,FGH.CompanyID, FGH.FinishGoodsHeaderID);
                       
              foreach (FinishedGoodsDetail ACddDelete in colDeleteAssetCostDetail)
                  FinishedGoodsDetail.DeleteDataByKey(sqltrans, FGH.UpdatedBy.Value, FGH.UpdateDate.Value, FGH.UpdatedByValue, ACddDelete.FinishGoodsDetailID, ACddDelete.FinishGoodsHeaderID, FGH.CompanyID);

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
                   SqlHelper.RollBackTransaction(sqltrans, "DeleteFinishGoodDetail");
           }
           return result;
       }

    }
}
