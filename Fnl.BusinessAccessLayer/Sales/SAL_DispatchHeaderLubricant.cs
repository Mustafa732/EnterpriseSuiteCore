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
        
        #region Constant Queries

        const string Query_DispatchDetailReportForLubricant = @"
SELECT Distinct [SAL_DispatchHeader].[DispatchID], 
    [INV_StockTransactionDetail].[CompanyID], 
    [INV_StockTransactionDetail].[PeriodID], 
    --[SAL_DeliveryOrderDetail].Quantity as Quantity, 
    SUM(ABS([INV_StockTransactionDetail].TransactionQuantity)) as Quantity, 
    [INV_StockTransactionDetail].[SourceHeaderID] AS [DeliveryOrderHeaderID], 
    [INV_StockTransactionDetail].[SourceLineID] AS [DeliveryOrderDetailID], [INV_StockTransactionDetail].[Status], [SAL_OrderDetail].[ItemCategoryID], 
    [SAL_OrderDetail].[ItemID], [SAL_OrderDetail].[ItemUnitID], [SAL_DeliveryOrderHeader].[DeliveryOrderNo], [SAL_DeliveryOrderDetail].[Quantity] AS [BaseQuantity], 
    [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[BaseAmount], ([SAL_OrderDetail].[Rate] * [SAL_DeliveryOrderDetail].[Quantity]) AS [NetAmount], 
    ([SAL_OrderDetail].[Rate] * [SAL_DeliveryOrderDetail].[Quantity]) AS [Amount], [INV_SET_Category].[CategoryName], [INV_Item].[Description], 
    [SET_UnitofMeasure].[UOMName], ISNULL([INV_Item].ListPricePerUnit, 0) AS ListPricePerUnit, 
    Sum(ISNULL(ISNULL([SAL_OrderDetail].[Rate], IsNull([INV_Item].ListPricePerUnit, 0)) * (Abs([INV_StockTransactionDetail].[TransactionQuantity])), 0)) AS TotalAmount, 
    '' [Action], [SAL_DeliveryOrderHeader].DocumentTypeID as DODocumentTypeID, [INV_StockTransactionDetail].[LocationID], [set_location].[LocationName], 
    [INV_StockTransactionDetail].[RackID], set_rack.RackName 
FROM [SAL_DispatchHeader] 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
    INNER JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[StockTransactionID] = [INV_StockTransactionHeader].[StockTransactionID] 
        AND [INV_StockTransactionDetail].[CompanyID] = [INV_StockTransactionHeader].[CompanyID] 
        AND [INV_StockTransactionDetail].[PeriodID] = [INV_StockTransactionHeader].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1 
    INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID] 
        AND [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID] 
        AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
        AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID] AND [SAL_DeliveryOrderDetail].[Status] = 1 
    INNER JOIN [SAL_DeliveryOrderHeader] ON [SAL_DeliveryOrderHeader].[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] 
        AND [SAL_DeliveryOrderHeader].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_DeliveryOrderHeader].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_DeliveryOrderHeader].[Status] = 1 
    INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
    left join set_location on set_location.LocationID=[INV_StockTransactionDetail].[LocationID] 
    left join set_rack on set_rack.rackID=[INV_StockTransactionDetail].[RackID] 
WHERE [SAL_DispatchHeader].[DispatchID] = @DispatchID AND [SAL_DispatchHeader].[CompanyID] = @CompanyID AND [SAL_DispatchHeader].[PeriodID] = @PeriodID 
    AND [SAL_DispatchHeader].[Status] = 1 
Group By SAL_OrderDetail.ItemID, INV_Item.Description, SAL_DispatchHeader.DispatchID, 
    INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.PeriodID, 
    --SAL_DeliveryOrderDetail.Quantity, 
    --[INV_StockTransactionDetail].TransactionQuantity,
    INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceLineID, INV_StockTransactionDetail.Status, 
    SAL_OrderDetail.ItemCategoryID, SAL_OrderDetail.ItemUnitID, SAL_DeliveryOrderHeader.DeliveryOrderNo, SAL_DeliveryOrderDetail.Quantity, SAL_OrderDetail.Rate, 
    SAL_OrderDetail.BaseAmount, INV_SET_Category.CategoryName, SET_UnitofMeasure.UOMName, INV_Item.ListPricePerUnit, SAL_DeliveryOrderHeader.DocumentTypeID, 
    INV_StockTransactionDetail.LocationID, set_location.LocationName, INV_StockTransactionDetail.RackID, set_rack.RackName 
ORDER BY [SAL_DispatchHeader].[DispatchID] ";

        #endregion

        #region Class Methods

        public static DataTable GetDataByKeyWithOtherTablesForLubricant(int dispatchID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DispatchDetailReportForLubricant, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_DispatchDetailReportForLubricant, sqlparams);

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

