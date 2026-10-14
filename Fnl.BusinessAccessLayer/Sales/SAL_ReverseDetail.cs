using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Data;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_ReverseDetail
    {
        #region Constant Queries
        const string Query_Get_Data_For_SAl_ReverseDetail = @"
Select
SAL_ReverseDetail.ReverseHeaderID,
SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description as ItemName,
(Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity)Rate,
Sal_OrderDetail.Quantity as SOQuantity,

ISNULL(SAL_DeliveryOrderDetail.Quantity,0) as DOQuantity,

--(Sal_OrderDetail.Quantity - ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - (Select isnull(SUM(srd.ReversedQuantity),0) From SAL_ReverseDetail as srd
--where srd.CompanyID = SAL_ReverseDetail.CompanyID AND srd.PeriodID = SAL_ReverseDetail.PeriodID AND srd.ReverseDetailID = SAL_ReverseDetail.ReverseDetailID
--AND srd.ReverseHeaderID = SAL_ReverseDetail.ReverseHeaderID
--))QtyCanBeReverse,


(Sal_OrderDetail.Quantity - ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - (ISNULL(SAL_OrderDetail.ReversedQuantity,0)))QtyCanBeReverse,

SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,
'' as Action

 

From SAL_ReverseDetail

INNER JOIN [Sal_OrderDetail] ON  [Sal_OrderDetail].[OrderID] = SAL_ReverseDetail.SourceHeaderID AND
		[Sal_OrderDetail].[OrderDetailID] = SAL_ReverseDetail.SourceDetailID AND
		 [Sal_OrderDetail].CompanyID = SAL_ReverseDetail.CompanyID AND
		 [Sal_OrderDetail].PeriodID = SAL_ReverseDetail.PeriodID
INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID	

LEFT JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.SalesOrderHeaderID = Sal_OrderDetail.OrderID AND
    SAL_DeliveryOrderDetail.SalesOrderDetailID = Sal_OrderDetail.OrderDetailID AND
    SAL_DeliveryOrderDetail.CompanyID = Sal_OrderDetail.CompanyID AND
    SAL_DeliveryOrderDetail.PeriodID = Sal_OrderDetail.PeriodID	 
    
    Where 
    SAL_ReverseDetail.CompanyID = @CompanyID AND
    SAL_ReverseDetail.PeriodID = @PeriodID AND
    SAL_ReverseDetail.ReverseHeaderID = @ReverseHeaderID AND 
    Sal_ReverseDetail.Status = 1
";
        const string Query_GetDataForDOReturn = @"
Select
SAL_ReverseDetail.ReverseHeaderID,
SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description as ItemName,
(Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity)Rate,
ISNULL(SAL_DeliveryOrderDetail.Quantity,0) as DOQuantity, 
ISNULL(ABS(std.TransactionQuantity),0) as DispatchQuantity,

--(Sal_OrderDetail.Quantity - ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - (Select isnull(SUM(srd.ReversedQuantity),0) From SAL_ReverseDetail as srd
--where srd.CompanyID = SAL_ReverseDetail.CompanyID AND srd.PeriodID = SAL_ReverseDetail.PeriodID AND srd.ReverseDetailID = SAL_ReverseDetail.ReverseDetailID
--AND srd.ReverseHeaderID = SAL_ReverseDetail.ReverseHeaderID
--))QtyCanBeReverse,


(ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - ISNULL(ABS(std.TransactionQuantity),0) - ISNULL(SAL_DeliveryOrderDetail.ReversedQty,0))QtyCanBeReverse,
SAL_ReverseDetail.RemainingQtyAtTime,
SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,

'' as Action
From SAL_ReverseDetail

INNER JOIN SAL_DeliveryOrderDetail ON  SAL_DeliveryOrderDetail.[DOHeaderID] = SAL_ReverseDetail.SourceHeaderID AND
		SAL_DeliveryOrderDetail.[DODetailID] = SAL_ReverseDetail.SourceDetailID AND
		 SAL_DeliveryOrderDetail.CompanyID = SAL_ReverseDetail.CompanyID AND
		 SAL_DeliveryOrderDetail.PeriodID = SAL_ReverseDetail.PeriodID

INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
	AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID

INNER JOIN [Sal_OrderDetail] ON [Sal_OrderDetail].[OrderID] = SAL_DeliveryOrderDetail.[SalesOrderHeaderID] AND
			[Sal_OrderDetail].[OrderDetailID] = SAL_DeliveryOrderDetail.[SalesOrderDetailID] AND
            [Sal_OrderDetail].[CompanyID] = SAL_DeliveryOrderDetail.[CompanyID] AND
            [Sal_OrderDetail].[PeriodID] = SAL_DeliveryOrderDetail.[PeriodID]

INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID	


LEFT JOIN INV_StockTransactionDetail std ON std.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID AND
	std.SourceLineID = SAL_DeliveryOrderDetail.DODetailID AND std.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo AND
	std.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID AND
	std.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND std.PeriodID = SAL_DeliveryOrderDetail.PeriodID
    
    Where 
    SAL_ReverseDetail.CompanyID = @CompanyID AND
    SAL_ReverseDetail.PeriodID = @PeriodID AND
    SAL_ReverseDetail.ReverseHeaderID = @ReverseHeaderID AND 
    Sal_ReverseDetail.Status = 1";

        const string Query_GetDataForDispatchReturn = @"
Select Distinct
SET_Rack.RackID as RackID,
SET_Rack.RackName as RackName,
SAL_ReverseDetail.ReverseHeaderID,
SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description as ItemName,
std.TransactionCost as Rate, 
ISNULL(ABS(std.TransactionQuantity),0)DispatchQty,
ISNULL(SUM(SAL_InvoiceDetail.QuantityInvoiced),0)InvoicedQty,  
( ISNULL(ABS(std.TransactionQuantity),0) - ISNULL(SUM(SAL_InvoiceDetail.QuantityInvoiced),0) - ISNULL(SAL_ReverseDetail.ReversedQuantity,0))QtyCanBeReverse,
SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,
[SAL_DeliveryOrderHeader].[DeliveryOrderNo],
SAL_DispatchHeader.CustomerID,
std.SourceHeaderID as DeliveryOrderID,
SAL_DispatchHeader.DispatchDate
,[SAL_DeliveryOrderDetail].[DODetailID] 
,std.StockTransactionID 
,(select distinct top 1 INV_StockTransactionDetail2.LocationID from INV_StockTransactionDetail as INV_StockTransactionDetail2 inner join INV_StockTransactionHeader as INV_StockTransactionHeader2
 on INV_StockTransactionHeader2.StockTransactionID=INV_StockTransactionDetail2.StockTransactionID
  and INV_StockTransactionHeader2.CompanyID=INV_StockTransactionDetail2.CompanyID
 and INV_StockTransactionHeader2.PeriodID=INV_StockTransactionDetail2.PeriodID
 
 where INV_StockTransactionHeader2.DocumentNo=SAL_ReverseHeader.DocumentNo and INV_StockTransactionHeader2.DocumentTypeID=SAL_ReverseHeader.DocumentTypeID
 and INV_StockTransactionDetail2.ItemID=std.ItemID
 --and INV_StockTransactionHeader2.FiscalID = SAL_ReverseHeader.FiscalID
 --and INV_StockTransactionHeader2.CompanyID = SAL_ReverseHeader.CompanyID
 --and INV_StockTransactionHeader2.PeriodID = SAL_ReverseHeader.PeriodID
 ) as LocationID

 ,(select distinct top 1 SET_Location.LocationName from INV_StockTransactionDetail as INV_StockTransactionDetail2 inner join INV_StockTransactionHeader as INV_StockTransactionHeader2
 on INV_StockTransactionHeader2.StockTransactionID=INV_StockTransactionDetail2.StockTransactionID
   and INV_StockTransactionHeader2.CompanyID=INV_StockTransactionDetail2.CompanyID
 and INV_StockTransactionHeader2.PeriodID=INV_StockTransactionDetail2.PeriodID

 inner join SET_Location on SET_Location.LocationID=INV_StockTransactionDetail2.LocationID
 where INV_StockTransactionHeader2.DocumentNo=SAL_ReverseHeader.DocumentNo and INV_StockTransactionHeader2.DocumentTypeID=SAL_ReverseHeader.DocumentTypeID
 and INV_StockTransactionDetail2.ItemID=std.ItemID
 -- and INV_StockTransactionHeader2.FiscalID = SAL_ReverseHeader.FiscalID
 --and INV_StockTransactionHeader2.CompanyID = SAL_ReverseHeader.CompanyID
 --and INV_StockTransactionHeader2.PeriodID = SAL_ReverseHeader.PeriodID
 ) as LocationName
,'' as Action
From SAL_ReverseDetail

INNER JOIN INV_StockTransactionDetail std ON std.StockTransactionID = SAL_ReverseDetail.SourceHeaderID 
	AND std.TransactionDetailID = SAL_ReverseDetail.SourceDetailID AND
	std.CompanyID = SAL_ReverseDetail.CompanyID AND std.PeriodID = SAL_ReverseDetail.PeriodID
	AND std.Status=1
inner join SAL_ReverseHeader on SAL_ReverseHeader.ReverseHeaderID=SAL_ReverseDetail.ReverseHeaderID
and SAL_ReverseHeader.CompanyID=SAL_ReverseDetail.CompanyID
INNER JOIN INV_StockTransactionHeader sth ON sth.DocumentTypeID = SAL_ReverseDetail.SourceDocumentTypeID AND
	sth.DocumentNo = SAL_ReverseDetail.SourceCode AND sth.CompanyID = SAL_ReverseDetail.CompanyID AND
	sth.PeriodID = SAL_ReverseDetail.PeriodID
	AND sth.StockTransactionID = SAL_ReverseDetail.SourceHeaderID 
INNER JOIN SAL_DispatchHeader ON sth.DocumentNo = SAL_DispatchHeader.DispatchNo AND 
	sth.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID AND sth.CompanyID = SAL_DispatchHeader.CompanyID
	AND sth.PeriodID = SAL_DispatchHeader.PeriodID
	AND sth.FiscalID = SAL_DispatchHeader.FiscalID

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

        LEFT JOIN SET_Rack ON SET_Rack.RackID = SAL_ReverseDetail.RackID
        AND SET_Rack.Status = 1

Where 
    SAL_ReverseDetail.CompanyID = @CompanyID AND
    SAL_ReverseDetail.PeriodID = @PeriodID AND
    SAL_ReverseDetail.ReverseHeaderID = @ReverseHeaderID AND 
    Sal_ReverseDetail.Status = 1 
	group by SAL_ReverseDetail.ReverseHeaderID,SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description
,std.TransactionCost,std.TransactionQuantity,std.ReversedQuantity,SAL_ReverseDetail.ReversedQuantity
,SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,
[SAL_DeliveryOrderHeader].[DeliveryOrderNo],
SAL_DispatchHeader.CustomerID,
std.SourceHeaderID,
SAL_DispatchHeader.DispatchDate
,[SAL_DeliveryOrderDetail].[DODetailID] 
,std.StockTransactionID
,SAL_ReverseHeader.DocumentNo
,SAL_ReverseHeader.DocumentTypeID
,std.ItemID
,SAL_ReverseHeader.FiscalID
,SAL_ReverseHeader.CompanyID
,SAL_ReverseHeader.PeriodID
,SET_Rack.RackID,SET_Rack.RackName
";

   const string Query_GetDataByKeyTableForInvoiceReturn = @"
Select
SAL_ReverseDetail.ReverseHeaderID,
SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description as ItemName,
(SAL_InvoiceDetail.NetAmount / SAL_InvoiceDetail.QuantityInvoiced) as Rate, 
ISNULL(SAL_InvoiceDetail.QuantityInvoiced,0)InvoicedQty,
ISNULL(AR_ReceiptTransactionDetail.Quantity,0)ReceiptQty,

( ISNULL(ABS(SAL_InvoiceDetail.QuantityInvoiced),0) - ISNULL(AR_ReceiptTransactionDetail.Quantity,0) - ISNULL(SAL_InvoiceDetail.ReversedQuantity,0))QtyCanBeReverse,
SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,
'' as Action
From SAL_ReverseDetail

INNER JOIN SAL_InvoiceDetail  ON SAL_InvoiceDetail.InvoiceDetailID = SAL_ReverseDetail.SourceDetailID AND
	SAL_InvoiceDetail.InvoiceHeaderID = SAL_ReverseDetail.SourceHeaderID AND SAL_InvoiceDetail.CompanyID = SAL_ReverseDetail.CompanyID AND
	SAL_InvoiceDetail.PeriodID = SAL_ReverseDetail.PeriodID


LEFT JOIN AR_ReceiptTransactionDetail ON AR_ReceiptTransactionDetail.InvoiceHeaderID = SAL_InvoiceDetail.InvoiceHeaderID AND
	AR_ReceiptTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID AND AR_ReceiptTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID
	
INNER JOIN INV_Item On INV_ITem.ItemID = SAL_InvoiceDetail.ItemID --AND INV_Item.CompanyID = SAL_InvoiceDetail.CompanyID

Where 
    SAL_ReverseDetail.CompanyID = @CompanyID AND
    SAL_ReverseDetail.PeriodID = @PeriodID AND
    SAL_ReverseDetail.ReverseHeaderID = @ReverseHeaderID AND 
    Sal_ReverseDetail.Status = 1
";

        #endregion

        #region Methods

        public static DataTable GetDataByKeyTable(int companyID, int periodID, int reverseHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                //sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_Data_For_SAl_ReverseDetail, sqlparams);

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

        public static DataTable GetDataByKeyTableForDOReturn(int companyID, int periodID, int reverseHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                //sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForDOReturn, sqlparams);

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


        public static DataTable GetDataByKeyTableForDOReturnFORFind(int companyID, int periodID, int reverseHeaderID)
        {
            DataTable dtToReturn = null;
            #region query

            string Query_GetDataForDOReturn = @"
Select
SAL_ReverseDetail.ReverseHeaderID,
SAL_ReverseDetail.ReverseDetailID,
INV_Item.Description as ItemName,
(Sal_OrderDetail.NetAmount / Sal_OrderDetail.Quantity)Rate,
ISNULL(SAL_DeliveryOrderDetail.Quantity,0) as DOQuantity, 
ISNULL(ABS(std.TransactionQuantity),0) as DispatchQuantity,

--(Sal_OrderDetail.Quantity - ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - (Select isnull(SUM(srd.ReversedQuantity),0) From SAL_ReverseDetail as srd
--where srd.CompanyID = SAL_ReverseDetail.CompanyID AND srd.PeriodID = SAL_ReverseDetail.PeriodID AND srd.ReverseDetailID = SAL_ReverseDetail.ReverseDetailID
--AND srd.ReverseHeaderID = SAL_ReverseDetail.ReverseHeaderID
--))QtyCanBeReverse,


--(ISNULL(SAL_DeliveryOrderDetail.Quantity,0) - ISNULL(ABS(std.TransactionQuantity),0) - ISNULL(SAL_DeliveryOrderDetail.ReversedQty,0))QtyCanBeReverse,
SAL_ReverseDetail.RemainingQtyAtTime as QtyCanBeReverse ,
SAL_ReverseDetail.ReversedQuantity, SAL_ReverseDetail.ReversedAmount,
SAL_ReverseDetail.SourceHeaderID,
SAL_ReverseDetail.SourceDetailID,
SAL_ReverseDetail.SourceCode,
SAL_ReverseDetail.SourceDocumentTypeID,

'' as Action
,sal_DoVehicleInformation.Chassis
From SAL_ReverseDetail

INNER JOIN SAL_DeliveryOrderDetail ON  SAL_DeliveryOrderDetail.[DOHeaderID] = SAL_ReverseDetail.SourceHeaderID AND
		SAL_DeliveryOrderDetail.[DODetailID] = SAL_ReverseDetail.SourceDetailID AND
		 SAL_DeliveryOrderDetail.CompanyID = SAL_ReverseDetail.CompanyID AND
		 SAL_DeliveryOrderDetail.PeriodID = SAL_ReverseDetail.PeriodID

INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
	AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID

INNER JOIN [Sal_OrderDetail] ON [Sal_OrderDetail].[OrderID] = SAL_DeliveryOrderDetail.[SalesOrderHeaderID] AND
			[Sal_OrderDetail].[OrderDetailID] = SAL_DeliveryOrderDetail.[SalesOrderDetailID] AND
            [Sal_OrderDetail].[CompanyID] = SAL_DeliveryOrderDetail.[CompanyID] AND
            [Sal_OrderDetail].[PeriodID] = SAL_DeliveryOrderDetail.[PeriodID]

INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID	


LEFT JOIN INV_StockTransactionDetail std ON std.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID AND
	std.SourceLineID = SAL_DeliveryOrderDetail.DODetailID AND std.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo AND
	std.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID AND
	std.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND std.PeriodID = SAL_DeliveryOrderDetail.PeriodID
    
left join sal_DoVehicleInformation on sal_DoVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
	and sal_DoVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID
	and sal_DoVehicleInformation.CompanyId = SAL_DeliveryOrderDetail.CompanyId
	and sal_DoVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID

    Where 
    SAL_ReverseDetail.CompanyID = @CompanyID AND
    SAL_ReverseDetail.PeriodID = @PeriodID AND
    SAL_ReverseDetail.ReverseHeaderID = @ReverseHeaderID AND 
    Sal_ReverseDetail.Status = 1
";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                //sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForDOReturn, sqlparams);

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

        public static DataTable GetDataByKeyTableForDispatchReturn(int companyID, int periodID, int reverseHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                //sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForDispatchReturn, sqlparams);

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

        public static DataTable GetDataByKeyTableForInvoiceReturn(int companyID, int periodID, int reverseHeaderID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReverseHeaderID", reverseHeaderID);
                //sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByKeyTableForInvoiceReturn, sqlparams);

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
