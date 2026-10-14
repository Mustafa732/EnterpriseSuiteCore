using DAL.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DispatchNoteDetail : BaseClass
    {
        #region Constants Queries

        const string Query_Select_By_DispatchNoteID_OrderID_CompanyId_PeriodId_ForDispatchNote =
            @"Select AP_PurchaseOrderHeader.POHeaderID, AP_PurchaseOrderDetail.PODetailID, AP_PurchaseOrderDetail.SerialNo, AP_PurchaseOrderHeader.DocumentNumber, AP_PurchaseOrderHeader.DocumentDate as PODueDate, AP_PurchaseOrderDetail.ItemDescription,
SET_UnitofMeasure.UOMName as Unit,
(AP_PurchaseOrderDetail.Quantity - isnull((select sum(SAL_DispatchNoteDetailinner.Quantity) from SAL_DispatchNoteDetail SAL_DispatchNoteDetailinner
where SAL_DispatchNoteDetailinner.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and SAL_DispatchNoteDetailinner.PODetailID = AP_PurchaseOrderDetail.PODetailID
and SAL_DispatchNoteDetailinner.Status = 1
),0)) as Quantity,
AR_SET_Customer.CustomerName AS Supplier, 
isnull(INV_StockTransactionHeader.DocumentNo,0) AS RequisitionNo, INV_StockTransactionHeader.DocumentDate AS RequisitionDate,
AP_PurchaseOrderHeader.Comments,-99 as DispatchNoteID,-99 as DispatchNoteDetailID
From AP_PurchaseOrderHeader

INNER JOIN AP_PurchaseOrderDetail ON AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PurchaseOrderHeader.Status =  1
and AP_PurchaseOrderDetail.Status =1

INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = AP_PurchaseOrderDetail.ItemUOMID
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID
--and AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID

left JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID
and INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
and INV_StockTransactionHeader.statuscode = 1
and INV_StockTransactionHeader.status = 1

where AP_PurchaseOrderHeader.CompanyID = @CompanyID and AP_PurchaseOrderHeader.PeriodID = @PeriodID and AP_PurchaseOrderHeader.POHeaderID = @DispatchNoteID
";

        #endregion

        #region Methods

        public static DataTable GetDataByKeyTableForDispatchNoteNew(int companyID, int periodID, SqlConnection objConnection = null, int? DispatchNoteID = null, int? CurrentFiscalID = null)
        {
            DataTable dtToReturn = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
               
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DispatchNoteID", DispatchNoteID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;


                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_By_DispatchNoteID_OrderID_CompanyId_PeriodId_ForDispatchNote), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Select_By_DispatchNoteID_OrderID_CompanyId_PeriodId_ForDispatchNote), sqlparams);

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

        #endregion
    }
}
