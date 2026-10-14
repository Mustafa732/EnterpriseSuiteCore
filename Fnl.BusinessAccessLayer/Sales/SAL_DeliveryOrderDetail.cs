using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DeliveryOrderDetail : BaseClass
    {
        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #region Class Methods

        public static DataTable GetDataByKeyTable(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_DeliveryOrderID_CompanyId_PeriodId = @"
SELECT Distinct [SAL_DeliveryOrderDetail].[DODetailID], [SAL_DeliveryOrderDetail].[DOHeaderID], [SAL_DeliveryOrderDetail].[CompanyID], 
    [SAL_DeliveryOrderDetail].[PeriodID], [SAL_DeliveryOrderDetail].[SalesOrderHeaderID], [SAL_DeliveryOrderDetail].[SalesOrderDetailID], 
    [SAL_DeliveryOrderDetail].[Quantity], [SAL_DeliveryOrderDetail].[IsCancelled], [SAL_DeliveryOrderDetail].[CancellationDate], 
    [SAL_DeliveryOrderDetail].[CancelledBy], [SAL_DeliveryOrderDetail].[IsApproved], [SAL_DeliveryOrderDetail].[ApprovedBy], [SAL_DeliveryOrderDetail].[ApproveDate], 
    [SAL_DeliveryOrderDetail].[IsPaid], [SAL_DeliveryOrderDetail].[PaidAmount], [SAL_DeliveryOrderDetail].[Status], [SAL_DeliveryOrderDetail].[CreatedBy], 
    [SAL_DeliveryOrderDetail].[CreateDate], [SAL_DeliveryOrderDetail].[CreatedByValue], [SAL_DeliveryOrderDetail].[UpdatedBy], [SAL_DeliveryOrderDetail].[UpdateDate], 
    [SAL_DeliveryOrderDetail].[UpdatedByValue], [SAL_DeliveryOrderDetail].[SerialNo], [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID], 
    [SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[Quantity] AS [BaseQuantity], [SAL_DeliveryOrderDetail].[Quantity] AS [RemainingQTY],
    [SET_UnitofMeasure].[UOMName], [inv_item].[UnitWeight], ISNULL([INV_item].[UnitVolume], 1) as UnitVolume, VolumeCode.UOMName as VolumeCodeName, 
    ISNULL([SAL_OrderDetail].[Quantity], 0) - ISNULL(
    (
        select SUM(sdd.Quantity) from SAL_DeliveryOrderDetail sdd 
            Inner Join Sal_DeliveryOrderHeader sdh On sdh.DeliveryOrderID = sdd.DOHeaderID And sdh.CompanyID = sdd.CompanyID And sdh.PeriodID = sdd.PeriodID 
        where sdd.SalesOrderHeaderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID AND sdd.CompanyID = [SAL_OrderDetail].[CompanyID] 
            and sdd.PeriodID = [SAL_OrderDetail].[PeriodID] AND sdd.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID AND sdh.StatusCode = 1 
    ), 0) as ValidateQuantity, 
    [SAL_DeliveryOrderDetail].[Quantity] as DOQTY, [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[NetAmount], [SAL_OrderDetail].[NetAmount] AS [Amount], 
    [SAL_OrderHeader].[ContractNo] AS [SalesOrderNo], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], 
    Sal_OrderDetail.Rate AS ListPricePerUnit, 
    (ISNULL([SAL_OrderDetail].[NetAmount], 0) / ISNULL([SAL_OrderDetail].[Quantity], 1)) * [SAL_DeliveryOrderDetail].[Quantity] TotalAmount, 
    ROW_NUMBER() Over (Order by Cast([SAL_DeliveryOrderDetail].[DODetailID] as int) desc) as 'RowUniqueID', '' [Action] 
,Sal_DeliveryOrderHeader.DODate
	,Sal_DeliveryOrderHeader.CustomerID,[INV_SET_Category].CategoryID,[SET_UnitofMeasure].[UOMID] as TransactionUOMID

,(([SAL_DeliveryOrderDetail].[Quantity] - ISNULL((Select SUM(TransactionQuantity) from INV_LoadingPointDetail 
inner join INV_LoadingPointHeader on INV_LoadingPointHeader.LoadingPointHeaderID = INV_LoadingPointDetail.LoadingPointHeaderID
	and INV_LoadingPointHeader.CompanyID = INV_LoadingPointDetail.CompanyID
	and INV_LoadingPointHeader.PeriodID = INV_LoadingPointDetail.PeriodID

	where INV_LoadingPointDetail.Status = 1 
	and INV_LoadingPointDetail.DOHeaderID =  SAL_DeliveryOrderDetail.DOHeaderID
	and  INV_LoadingPointDetail.DODetailID = SAL_DeliveryOrderDetail.DODetailID
    and  INV_LoadingPointDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
	and  INV_LoadingPointDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
),0)) + 


isnull ((select distinct isnull(sum(Reversed.ReversedQuantity),0) as ReversedQuantity
                    from  SAL_ReverseDetail as Reversed

					   LEFT JOIN [INV_StockTransactionDetail] ON [INV_StockTransactionDetail].[SourceHeaderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] 
            AND [INV_StockTransactionDetail].[SourceLineID] = [SAL_DeliveryOrderDetail].[DODetailID] 
            AND [INV_StockTransactionDetail].[SourceDocumentTypeID] = [SAL_DeliveryOrderHeader].[DocumentTypeID] 
            AND [INV_StockTransactionDetail].[SourceCode] = [SAL_DeliveryOrderHeader].[DeliveryOrderNo] 
            AND [INV_StockTransactionDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
            AND [INV_StockTransactionDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [INV_StockTransactionDetail].[Status] = 1 


				left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = [INV_StockTransactionDetail].StockTransactionID 
						and INV_StockTransactionHeader.[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
						and INV_StockTransactionHeader.[PeriodID] = [INV_StockTransactionDetail].[PeriodID] 
						and INV_StockTransactionHeader.FiscalID = [INV_StockTransactionDetail].FiscalID 
						and INV_StockTransactionHeader.Status = 1

						where Reversed.SourceHeaderID = INV_StockTransactionDetail.StockTransactionID
						and Reversed .SourceDetailID = INV_StockTransactionDetail.TransactionDetailID
						and Reversed .[CompanyID] = INV_StockTransactionDetail.[CompanyID]
						and Reversed .[PeriodID] = INV_StockTransactionDetail.[PeriodID]
						and Reversed.SourceCode  = INV_StockTransactionHeader.DocumentNo
						and Reversed.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
						and Reversed.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
						),0)
)
+

ISNULL((Select SUM(TransactionQuantity) from INV_LoadingPointDetail 

inner join INV_LoadingPointHeader on INV_LoadingPointHeader.LoadingPointHeaderID = INV_LoadingPointDetail.LoadingPointHeaderID
	and INV_LoadingPointHeader.CompanyID = INV_LoadingPointDetail.CompanyID
	and INV_LoadingPointHeader.PeriodID = INV_LoadingPointDetail.PeriodID
	
	inner join INV_FirstWeight on INV_FirstWeight.DocumentNo = INV_LoadingPointHeader.TareWtDocNo
	and  INV_FirstWeight.DocumentTypeID = INV_LoadingPointHeader.TareWtDocTypeID
	and  INV_FirstWeight.CompanyID = INV_LoadingPointHeader.CompanyID
	and  INV_FirstWeight.PeriodID = INV_LoadingPointHeader.PeriodID
    and  INV_FirstWeight.CurrentFiscalID = INV_LoadingPointHeader.CurrentFiscalID
	and GateINID  in (Select DocumentNo from SAL_VehicleCancellation where SAL_VehicleCancellation.DocumentTypeID = INV_FirstWeight.GateInDocType and SAL_VehicleCancellation.CompanyID = INV_FirstWeight.CompanyID and SAL_VehicleCancellation.FiscalID = INV_FirstWeight.CurrentFiscalID)
	where INV_LoadingPointDetail.Status = 1 
	and INV_LoadingPointDetail.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	and  INV_LoadingPointDetail.DODetailID =SAL_DeliveryOrderDetail.DODetailID
    and  INV_LoadingPointDetail.CompanyID =SAL_DeliveryOrderDetail.CompanyID
	and  INV_LoadingPointDetail.PeriodID =SAL_DeliveryOrderDetail.PeriodID
),0)
 as RemainingLoadingQty
,Sal_DeliveryOrderHeader.DeliveryOrderNo, AR_SET_Customer.CustomerName
FROM [SAL_DeliveryOrderDetail] 
    INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] 
    INNER JOIN [SAL_OrderHeader] ON [SAL_OrderHeader].[OrderID] = [SAL_OrderDetail].[OrderID] AND [SAL_OrderHeader].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
        AND [SAL_OrderHeader].[PeriodID] = [SAL_OrderDetail].[PeriodID] AND [SAL_OrderHeader].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
    

    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
    INNER JOIN Sal_DeliveryOrderHeader ON Sal_DeliveryOrderHeader.CompanyID = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND Sal_DeliveryOrderHeader.PeriodID = [SAL_DeliveryOrderDetail].PeriodID AND Sal_DeliveryOrderHeader.[DeliveryOrderID] = [SAL_DeliveryOrderDetail].DOHeaderID 
	LEFT JOIN [SET_UnitofMeasure]  as VolumeCode on [VolumeCode].[UOMID] =  [inv_item].[VolumeUOMCode]
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID
	AND AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID
	AND AR_SET_Customer.Status = 1 
WHERE [SAL_DeliveryOrderDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID 
    AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryOrderDetail].[Status] = 1 
ORDER BY [SAL_DeliveryOrderDetail].[DODetailID] ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);

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

        public static DataTable GetDataByKeyTableForDispatch(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null,bool Balance=true, string whereQty=null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch = @" Select * From 
(
    SELECT [SAL_DeliveryOrderDetail].[DODetailID], [SAL_DeliveryOrderDetail].[DOHeaderID], [SAL_DeliveryOrderDetail].[CompanyID], 
        [SAL_DeliveryOrderDetail].[PeriodID], [SAL_DeliveryOrderDetail].[SalesOrderHeaderID], [SAL_DeliveryOrderDetail].[SalesOrderDetailID], 
        ISNULL(
	    (
            {0}
		    --  Select ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) 
            --  Select ISNULL(DODetail.[Quantity], 0) - 0
		    from Sal_DeliveryOrderDetail DODetail 
			    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = DODetail.CompanyID And Sal_DeliveryOrderHeader.PeriodID = DODetail.PeriodID 
                    And Sal_DeliveryOrderHeader.DeliveryOrderID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND Sal_DeliveryOrderHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND Sal_DeliveryOrderHeader.DeliveryOrderNo = INV_StockTransactionDetail.SourceCode 
                    AND INV_StockTransactionDetail.Status = 1
                    and StockTransactionID in (select StockTransactionID from INV_StockTransactionHeader where Status = 1 and StatusCode = 1)
                    left join (
                    select distinct SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ,sum(ReversedQuantity) as ReversedQuantity
                    from  SAL_ReverseDetail 
                    group by SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ) SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = [INV_StockTransactionDetail].StockTransactionID
						and SAL_ReverseDetail .SourceDetailID = [INV_StockTransactionDetail].TransactionDetailID
						and SAL_ReverseDetail .[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
						and SAL_ReverseDetail .[PeriodID] = [INV_StockTransactionDetail].[PeriodID]
left join INV_StockTransactionDetail recvQty on recvQty.StockTransactionID = SAL_ReverseDetail.sourceHEaderID
				and recvQty.companyID = SAL_ReverseDetail.companyID
				and recvQty.PeriodID = SAL_ReverseDetail.PeriodID
		    Where DODetail.[DOHeaderID] = [SAL_DeliveryOrderDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryOrderDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity
	    )
          --            - 
--	    (
--case when (select sum(ReversedQuantity) from sal_ReverseDetail 
--			where sal_ReverseDetail.SourceHeaderID = [SAL_DeliveryOrderDetail].DoheaderID
--			and sal_ReverseDetail.SourceDetailID = [SAL_DeliveryOrderDetail].DODetailID
--			and sal_ReverseDetail.CompanyID = [SAL_DeliveryOrderDetail].CompanyID
--			and sal_ReverseDetail.PeriodID = [SAL_DeliveryOrderDetail].PeriodID) is null then 0 else 
--			(select sum(ReversedQuantity) from sal_ReverseDetail 
--			where sal_ReverseDetail.SourceHeaderID = [SAL_DeliveryOrderDetail].DoheaderID
--			and sal_ReverseDetail.SourceDetailID = [SAL_DeliveryOrderDetail].DODetailID
--			and sal_ReverseDetail.CompanyID = [SAL_DeliveryOrderDetail].CompanyID
--			and sal_ReverseDetail.PeriodID = [SAL_DeliveryOrderDetail].PeriodID)
--			end
			
--	    )
        , 0) [Quantity], 
        [SAL_DeliveryOrderDetail].[Quantity] as BaseQty,
        [SAL_DeliveryOrderDetail].[IsCancelled], [SAL_DeliveryOrderDetail].[CancellationDate], 
        [SAL_DeliveryOrderDetail].[CancelledBy], [SAL_DeliveryOrderDetail].[IsApproved], [SAL_DeliveryOrderDetail].[ApprovedBy], 
        [SAL_DeliveryOrderDetail].[ApproveDate], [SAL_DeliveryOrderDetail].[IsPaid], [SAL_DeliveryOrderDetail].[PaidAmount], [SAL_DeliveryOrderDetail].[Status], 
        [SAL_DeliveryOrderDetail].[CreatedBy], [SAL_DeliveryOrderDetail].[CreateDate], [SAL_DeliveryOrderDetail].[CreatedByValue], 
        [SAL_DeliveryOrderDetail].[UpdatedBy], [SAL_DeliveryOrderDetail].[UpdateDate], [SAL_DeliveryOrderDetail].[UpdatedByValue], [SAL_OrderDetail].[ItemCategoryID], 
        [SAL_OrderDetail].[ItemID], [SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[Quantity] AS [BaseQuantity], [SAL_OrderDetail].[Rate], 
        [SAL_OrderDetail].[BaseAmount],Set_Batch.Description as Batch,
        (Case when Inclusive = 1 then [SAL_OrderDetail].GrossAmount else [SAL_OrderDetail].[NetAmount] end) as NetAmount , 

        ([SAL_OrderDetail].[Quantity] * [SAL_OrderDetail].[Rate]) AS [Amount], Set_ItemCatalog.ItemCatalogID,Set_ItemCatalog.Description as CatalogName,
        [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, 
        ISNULL([SAL_OrderDetail].[Rate] * (ISNULL(
	    (
            {0}
		    --  Select ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) 
            --  Select ISNULL(DODetail.[Quantity], 0) - 0
		    from Sal_DeliveryOrderDetail DODetail 
			    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = DODetail.CompanyID And Sal_DeliveryOrderHeader.PeriodID = DODetail.PeriodID 
                    And Sal_DeliveryOrderHeader.DeliveryOrderID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND Sal_DeliveryOrderHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND Sal_DeliveryOrderHeader.DeliveryOrderNo = INV_StockTransactionDetail.SourceCode 
                    and StockTransactionID in (select StockTransactionID from INV_StockTransactionHeader where Status = 1 and StatusCode = 1)
               left join (
                    select distinct SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID],SourceDocumentTypeID
                    ,sum(ReversedQuantity) as ReversedQuantity
                    from  SAL_ReverseDetail 
                    group by SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID],SourceDocumentTypeID
                    ) SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = [INV_StockTransactionDetail].StockTransactionID
						and SAL_ReverseDetail .SourceDetailID = [INV_StockTransactionDetail].TransactionDetailID
						and SAL_ReverseDetail .[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
						and SAL_ReverseDetail .[PeriodID] = [INV_StockTransactionDetail].[PeriodID]
						and SAL_ReverseDetail.SourceDocumentTypeID = [INV_StockTransactionDetail].SourceDocumentTypeID

left join INV_StockTransactionDetail recvQty on recvQty.StockTransactionID = SAL_ReverseDetail.sourceHEaderID
				and recvQty.companyID = SAL_ReverseDetail.companyID
				and recvQty.PeriodID = SAL_ReverseDetail.PeriodID
		    Where DODetail.[DOHeaderID] = [SAL_DeliveryOrderDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryOrderDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity 
	    ), 0)), 0) AS TotalAmount,
			 			  			  isnull((Select (Quantity - ABS(isnull(TransactionQuantity,0))) as RemainingQty  from (
			  Select SourceCode,SourceHeaderID,Sum(TransactionQuantity) as TransactionQuantity , [SAL_DeliveryOrderDetail].Quantity from  [INV_StockTransactionDetail]
						Inner Join [INV_StockTransactionHeader] on [INV_StockTransactionHeader].StockTransactionID=[INV_StockTransactionDetail].StockTransactionID
						AND [INV_StockTransactionHeader].CompanyID=[INV_StockTransactionDetail].CompanyID
			  AND [INV_StockTransactionHeader].PeriodID=[INV_StockTransactionDetail].PeriodID
			  AND [INV_StockTransactionHeader].FiscalID=[INV_StockTransactionDetail].FiscalID
			  AND [INV_StockTransactionHeader].StatusCode=1 And [INV_StockTransactionHeader].Status=1

			  INNER JOIN [SAL_DeliveryOrderDetail] ON [SAL_DeliveryOrderDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID]
				 AND [SAL_DeliveryOrderDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID]
				  AND [SAL_DeliveryOrderDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
				  AND [SAL_DeliveryOrderDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID]
				   AND [SAL_DeliveryOrderDetail].[Status] = 1

				   INNER JOIN [SAL_DeliveryOrderHeader] SalDaliveryOrder ON SalDaliveryOrder.[DeliveryOrderID] = [SAL_DeliveryOrderDetail].[DOHeaderID] 
						AND SalDaliveryOrder.[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID]
						 AND SalDaliveryOrder.[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID]
						 AND SalDaliveryOrder.DocumentTypeID = [INV_StockTransactionDetail].SourceDocumentTypeID
						  AND SalDaliveryOrder.[Status] = 1
		   Where SalDaliveryOrder.DeliveryOrderID = [SAL_DeliveryOrderHeader].DeliveryOrderID
            and SalDaliveryOrder.CompanyID = [SAL_DeliveryOrderHeader].CompanyID
		   And [SAL_DeliveryOrderDetail].Status=1
			   Group By SourceCode,SourceHeaderID,[SAL_DeliveryOrderDetail].Quantity
			   ) as RemainingQty), [SAL_DeliveryOrderDetail].Quantity)RemainingQty, 
        '' [Action],-1[RackID], '' [RackName] ,SET_Location.[LocationID], [LocationName],[SAL_OrderDetail].Inclusive ,Sal_DeliveryOrderHeader.DocumentTypeID,Sal_DeliveryOrderHeader.DeliveryOrderNo, null as GrainSizeID , null as PackTypeID
    FROM [SAL_DeliveryOrderDetail]
    inner join  Sal_DeliveryOrderHeader on Sal_DeliveryOrderHeader.DeliveryOrderID = [SAL_DeliveryOrderDetail].DOHeaderID
    and Sal_DeliveryOrderHeader.companyID = [SAL_DeliveryOrderDetail].companyID
    and Sal_DeliveryOrderHeader.PeriodID = [SAL_DeliveryOrderDetail].PeriodID
	inner join SET_Location on SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID
	and SET_Location.Status = 1
  INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID]
 LEFT JOIN Set_Batch ON Set_Batch.BatchID = SAL_OrderDetail.BatchId --AND  Set_Batch.CompanyID = SAL_OrderDetail.CompanyID
   AND Set_Batch.PeriodID = SAL_OrderDetail.PeriodID
INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1
  INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1
left join Set_ItemCatalog on Set_ItemCatalog.ItemCatalogID = [INV_Item].ItemCatalogGroupID
  INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
  WHERE [SAL_DeliveryOrderDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID  AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryOrderDetail].[Status] = 1
and 
  ISNULL(
	    (
            -- Select ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
		    --  Select ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) 
              Select ISNULL(DODetail.[Quantity], 0) - 0
		    from Sal_DeliveryOrderDetail DODetail 
			    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = DODetail.CompanyID And Sal_DeliveryOrderHeader.PeriodID = DODetail.PeriodID 
                    And Sal_DeliveryOrderHeader.DeliveryOrderID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND Sal_DeliveryOrderHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND Sal_DeliveryOrderHeader.DeliveryOrderNo = INV_StockTransactionDetail.SourceCode 
                    AND INV_StockTransactionDetail.Status = 1
                 left join (
                    select distinct SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID],SourceDocumentTypeID
                    ,sum(ReversedQuantity) as ReversedQuantity
                    from  SAL_ReverseDetail 
                    group by SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID],SourceDocumentTypeID
                    ) SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = [INV_StockTransactionDetail].StockTransactionID
						and SAL_ReverseDetail.SourceDetailID = [INV_StockTransactionDetail].TransactionDetailID
						and SAL_ReverseDetail.[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
						and SAL_ReverseDetail.[PeriodID] = [INV_StockTransactionDetail].[PeriodID]
						and SAL_ReverseDetail.SourceDocumentTypeID = [INV_StockTransactionDetail].SourceDocumentTypeID

		    Where DODetail.[DOHeaderID] = [SAL_DeliveryOrderDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryOrderDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity
	    )
        , 0) <>0
)Tab
 -- Where Tab.Quantity > 0
{1}
--  where not exists (
		
		--select * from sal_ReverseDetail 
		--where sal_ReverseDetail.SourceHeaderID = tab.DoheaderID
		--and sal_ReverseDetail.SourceDetailID = tab.DODetailID
		--and sal_ReverseDetail.CompanyID = tab.CompanyID
		--and sal_ReverseDetail.PeriodID = tab.PeriodID
  --      and sal_ReverseDetail.ReversedQuantity >= tab.quantity
  ORDER BY Tab.[DODetailID]
 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                string strQueryPart = "";
                if (Balance == true)
                    //strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))";
//                    strQueryPart = @" Select ISNULL(DODetail.[Quantity], 0) - 
//             
//              (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
//            ( [SAL_DeliveryOrderDetail].[Quantity] - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
//             else
//             ( ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
//             end ) ";


                    strQueryPart = @" 

Select 
             (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
             (ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))))
             +
             ( abs(isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
             
            ELSE
            (ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
            END)
";
                    
                else
                    strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart,whereQty), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart,whereQty), sqlparams);

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

        public static DataTable GetDataByKeyTableForDispatchNew(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null, bool Balance = true, string whereQty = null, int? DispatchID = null, int? CurrentFiscalID = null)
        {
            DataTable dtToReturn = null;

            #region Query

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DispatchID", DispatchID);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string strQueryPart = "";
                if (Balance == true)
                    //strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))";
                    //                    strQueryPart = @" Select ISNULL(DODetail.[Quantity], 0) - 
                    //             
                    //              (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
                    //            ( [SAL_DeliveryOrderDetail].[Quantity] - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
                    //             else
                    //             ( ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
                    //             end ) ";


                    strQueryPart = @" 

Select 
             (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
             (ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))))
             +
             ( abs(isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
             
            ELSE
            (ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
            END)
";

                else
                    strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart, whereQty), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart, whereQty), sqlparams);

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


        public static DataTable GetDataByKeyTableForDOUpdate(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null, bool Balance = true, string whereQty = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query = @" 
select [SAL_OrderDetail].ItemID , [SAL_OrderDetail].ItemUnitID, INV_Item.Description , SET_UnitofMeasure.UOMName
,AR_SET_Customer.CustomerName,AR_SET_Customer.CustomerID,
[SAL_DeliveryOrderDetail].[Quantity] - ISNULL((Select SUM(TransactionQuantity) from INV_LoadingPointDetail 

inner join INV_LoadingPointHeader on INV_LoadingPointHeader.LoadingPointHeaderID = INV_LoadingPointDetail.LoadingPointHeaderID
	and INV_LoadingPointHeader.CompanyID = INV_LoadingPointDetail.CompanyID
	and INV_LoadingPointHeader.PeriodID = INV_LoadingPointDetail.PeriodID
	
	inner join INV_FirstWeight on INV_FirstWeight.DocumentNo = INV_LoadingPointHeader.TareWtDocNo
	and  INV_FirstWeight.DocumentTypeID = INV_LoadingPointHeader.TareWtDocTypeID
	and  INV_FirstWeight.CompanyID = INV_LoadingPointHeader.CompanyID
	and  INV_FirstWeight.PeriodID = INV_LoadingPointHeader.PeriodID
	and GateINID not in
(Select DocumentNo from SAL_VehicleCancellation where SAL_VehicleCancellation.DocumentTypeID = INV_FirstWeight.GateInDocType and SAL_VehicleCancellation.CompanyID = INV_FirstWeight.CompanyID)
	where INV_LoadingPointDetail.Status = 1 
	and INV_LoadingPointDetail.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	and  INV_LoadingPointDetail.DODetailID =SAL_DeliveryOrderDetail.DODetailID
    and  INV_LoadingPointDetail.CompanyID =SAL_DeliveryOrderDetail.CompanyID
	and  INV_LoadingPointDetail.PeriodID =SAL_DeliveryOrderDetail.PeriodID
),0) as RemainingLoadingQty
 from Sal_DeliveryOrderDetail

inner join  Sal_DeliveryOrderHeader on Sal_DeliveryOrderHeader.DeliveryOrderID = [SAL_DeliveryOrderDetail].DOHeaderID
    and Sal_DeliveryOrderHeader.companyID = [SAL_DeliveryOrderDetail].companyID
    and Sal_DeliveryOrderHeader.PeriodID = [SAL_DeliveryOrderDetail].PeriodID
	and Sal_DeliveryOrderHeader.Status = 1 and Sal_DeliveryOrderHeader.StatusCode = 1

	inner join AR_SET_Customer on AR_SET_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID
	and AR_SET_Customer.Status =1 

  INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
  AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID]
   AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
   AND [SAL_OrderDetail].[Status] = 1 
   AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID]
   and SAL_OrderDetail.Status = 1

 INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
   AND [INV_Item].[Status] = 1
   inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = [SAL_OrderDetail].ItemUnitID
   and SET_UnitofMeasure.Status = 1

   where Sal_DeliveryOrderHeader.CompanyID =  @CompanyID and  Sal_DeliveryOrderHeader.PeriodID = @PeriodID and Sal_DeliveryOrderHeader.DeliveryOrderID= @DeliveryOrderID
 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, whereQty), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query, whereQty), sqlparams);

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
        public short GetNewSerialNo(int dOHeaderID, int companyID, int periodID)
        {
            short maxSerialNo = 0;

            string Query_NewSerialNo_ByCompanyPeriodDDIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM [SAL_DeliveryOrderDetail] 
            WHERE [DOHeaderID] = @DOHeaderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID; ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DOHeaderID", dOHeaderID);

                maxSerialNo = Convert.ToInt16(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodDDIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public static DataRow GetCostCenterByDeliveryNo(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            string Query_GetRefrenceNoByKey = @"

select SAL_ContractHeader.CostCenterID,SET_CostCenter.CostCenterName from SAL_ContractHeader
Inner Join SAL_DeliveryOrderDetail on SAL_ContractHeader.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
left join SET_CostCenter on SAL_ContractHeader.CostCenterID = SET_CostCenter.CostCenterID 
WHERE Sal_DeliveryOrderDetail.DOHeaderID = @OrderID AND SAL_ContractHeader.CompanyID = @CompanyID AND SAL_ContractHeader.PeriodID = @PeriodID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@OrderID", orderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNoByKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRefrenceNoByKey, sqlparams);

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

        public static DataRow GetRefrenceNoByDeliveryNo(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            string Query_GetRefrenceNoByKey = @"

Select Distinct referenceno, Sal_DeliveryOrderDetail.DOHeaderID, Sal_OrderHeader.OrderID, Set_Batch.Description as Batch, Sal_OrderHeader.ContractNo 
From Sal_OrderHeader 
	Inner JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.SalesOrderHeaderID = Sal_OrderHeader.OrderID 
        AND Sal_DeliveryOrderDetail.CompanyID = Sal_OrderHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = Sal_OrderHeader.PeriodID 
	Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = Sal_OrderHeader.Season 
WHERE Sal_DeliveryOrderDetail.DOHeaderID = @OrderID AND Sal_OrderHeader.CompanyID = @CompanyID AND Sal_OrderHeader.PeriodID = @PeriodID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@OrderID", orderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if(objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRefrenceNoByKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRefrenceNoByKey, sqlparams);

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

        //delivery order detail report
        public DataTable DeliveryOrderDetailReport(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {

            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            #region Query

            string DeliveryOrderDetailReport_Query = @"

select 
SAL_DeliveryOrderHeader.DeliveryOrderNo ,
SAL_DeliveryOrderHeader.CustomerID,
AR_SET_Customer.CustomerName,
SAL_DeliveryOrderDetail.PaidAmount,
SAL_OrderDetail.ItemCategoryID, 
INV_SET_Category.CategoryName,
SAL_OrderDetail.ItemID, 
INV_Item.Description,
SAL_OrderDetail.Rate, 
SAL_OrderDetail.Quantity,
SAL_OrderDetail.GrossAmount, 
SAL_OrderDetail.FEDAmount, 
SAL_OrderDetail.NetAmount,
set_unitofmeasure.UOMName,
SAL_OrderHeader.Season,
CAST((datepart(year, SAL_DeliveryOrderHeader.DODate)) AS VARCHAR(10)) + '-' + CAST((datepart(month, SAL_DeliveryOrderHeader.DODate)) AS VARCHAR(10))+ '-' + CAST((datepart(day, SAL_DeliveryOrderHeader.DODate)) AS VARCHAR(10)) as DODate


from SAL_DeliveryOrderHeader
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
	and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID	

inner join SAL_OrderDetail on SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
	and SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
	and SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
	and SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID

Inner Join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID
	--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID	

Inner Join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
	--and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID

Inner Join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	-- 
	
Inner Join set_unitofmeasure on set_unitofmeasure.UOMID=Sal_orderDetail.ItemUnitId

inner JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID

LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ORDERHEADER.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID 
where SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
{0}
 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                    whereFilter = "And SAL_DeliveryOrderHeader.DODate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";
                else if (dateFrom.HasValue)
                    whereFilter = "And SAL_DeliveryOrderHeader.DODate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'";
                else if (dateTo.HasValue)
                    whereFilter = "And SAL_DeliveryOrderHeader.DODate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";

                string query = string.Format(DeliveryOrderDetailReport_Query, whereFilter);

               
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
               

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

        public DataTable GetOutstandingReport(int companyID, int periodID, Nullable<int> itemId = null
            , Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {
            DataTable dt = null;
            StringBuilder filter = new StringBuilder();

            #region Query

            string Query_GetOutstandingReport = @"        
        Select * from (
        Select DeliveryOrderNo DoNumber,
        DODate,
        Sum(SAL_DeliveryOrderDetail.Quantity) DoQuantity,
        ContractNo SoNumber,ContractDate SoDate,
        SAL_OrderDetail.Rate,
        Case when Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) < 0 Then 0 Else
            Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) End DispatchQuantity,
        AR_SET_Customer.CustomerName,
        Sum(IsNULL(SAL_DeliveryOrderDetail.Quantity,0)) - Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) BalanceQuantity,
        (Sum(IsNULL(SAL_DeliveryOrderDetail.Quantity,0)) - Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0))) * SAL_OrderDetail.Rate BalanceAmount

        FROM SAL_DeliveryOrderDetail

        JOIN SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID
            AND SAL_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
            AND SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 

        JOIN SAL_OrderDetail on SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID 
            AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
            AND SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
            AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 

        JOIN SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
            AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
            AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 

        LEFT JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID 
            AND INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
            AND INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 

        LEFT JOIN INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

        JOIN AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
            --AND AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            AND AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID 

        Group By DeliveryOrderNo, DODate, ContractNo, ContractDate
                , SAL_OrderDetail.Rate, AR_SET_Customer.CustomerName
                , SAL_DeliveryOrderDetail.CompanyID, SAL_DeliveryOrderDetail.PeriodID
                , SAL_OrderDetail.ItemID

        Having SAL_DeliveryOrderDetail.CompanyID = @CompanyID AND 
        SAL_DeliveryOrderDetail.PeriodID = @PeriodID AND 
        (SAL_OrderDetail.ItemID = @ItemID OR @ItemID Is Null)
        )fTab Where BalanceQuantity > 0 
        ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if(itemId.HasValue)
                    sqlparams[2] = new SqlParameter("@ItemID", itemId);
                else
                    sqlparams[2] = new SqlParameter("@ItemID", DBNull.Value);
                if (dateFrom.HasValue)
                    filter.AppendLine(" AND DODate >= '" + dateFrom.Value.ToString("yyyy-MM-dd") + "'");
                if (dateFrom.HasValue)
                    filter.AppendLine(" AND DODate <= '" + dateTo.Value.ToString("yyyy-MM-dd") + "'");

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetOutstandingReport + filter.ToString(), sqlparams);

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

        public static double GetRemainingQtyInDO(int dOHeaderID, int DODetailID, int companyID, int periodID, bool Balance = true)
        {
            double DORemaianingQty = 0;

            string ParmString1 = "";
            string ParmString2 = "";

            #region Query

            string Query_GetRemainingQtyInDO = @"    SELECT 
     ISNULL(
	    (
            {0}
		    
		    from Sal_DeliveryOrderDetail DODetail 
			    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = DODetail.CompanyID And Sal_DeliveryOrderHeader.PeriodID = DODetail.PeriodID 
                    And Sal_DeliveryOrderHeader.DeliveryOrderID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND Sal_DeliveryOrderHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND Sal_DeliveryOrderHeader.DeliveryOrderNo = INV_StockTransactionDetail.SourceCode 
               left join (
                    select distinct SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ,sum(ReversedQuantity) as ReversedQuantity
                    from  SAL_ReverseDetail 
                    group by SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ) SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = [INV_StockTransactionDetail].StockTransactionID
						and SAL_ReverseDetail .SourceDetailID = [INV_StockTransactionDetail].TransactionDetailID
						and SAL_ReverseDetail .[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
						and SAL_ReverseDetail .[PeriodID] = [INV_StockTransactionDetail].[PeriodID]

		    Where DODetail.[DOHeaderID] = [SAL_DeliveryOrderDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryOrderDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity
	    ), 0) [Quantity]
    
    FROM [SAL_DeliveryOrderDetail]
  INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID]
  INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
    --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1
  INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1
  INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
  WHERE [SAL_DeliveryOrderDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID  AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryOrderDetail].[Status] = 1
  -- AND [SAL_DeliveryOrderDetail].DODetailID = @DODetailID
    {1}
";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderID", dOHeaderID);
                sqlparams[3] = new SqlParameter("@DODetailID", DODetailID);

                if (Balance == true)
                {
                    ParmString1 = " Select (ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) )+( abs(isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))) ";
                    ParmString2 = " AND [SAL_DeliveryOrderDetail].DODetailID = @DODetailID ";
                }
                else
                {
                    ParmString1 = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";
                    ParmString2 = " AND [SAL_DeliveryOrderDetail].DODetailID = @DODetailID ";
                }

                DORemaianingQty = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text,string.Format(Query_GetRemainingQtyInDO,ParmString1,ParmString2), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DORemaianingQty;
        }


        public static DataTable GetDataByKeyTableForContract(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

           

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);

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

        public static DataTable GetDataByKeyTableForContractNew(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query



            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodIdNew, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Select_By_DeliveryOrderID_CompanyId_PeriodIdNew, sqlparams);

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

        public static DataTable GetDataByKeyTableForDispatch_New(int CurrentFiscalID, int deliveryOrderID, int companyID, int periodID, int GrossWtDoc, SqlConnection objConnection = null, bool Balance = true, string whereQty = null, string docname = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            #region Query
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@GrossWtDoc", GrossWtDoc);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string strQueryPart = "";
                if (Balance == true)
                    //strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))";
                    //                    strQueryPart = @" Select ISNULL(DODetail.[Quantity], 0) - 
                    //             
                    //              (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
                    //            ( [SAL_DeliveryOrderDetail].[Quantity] - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
                    //             else
                    //             ( ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
                    //             end ) ";


                    strQueryPart = @" 

Select 
             (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
             (ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))))
             +
             ( abs(isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
             
            ELSE
            (ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
            END)

";

                else
                    strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";

                if (docname != null)
                {
                    sb.Append(" and INV_SecondWeight.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentTypeID = INV_SecondWeight.DocumentTypeID and DocumentType like '%" + docname + "%') ");
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDataForDispatchByLoadingNo, strQueryPart, whereQty,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDataForDispatchByLoadingNo, strQueryPart, whereQty,sb.ToString()), sqlparams);

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

        public static DataTable GetDataByKeyTableForDispatch_NewForSheikhoo(int CurrentFiscalID, int deliveryOrderID, int companyID, int periodID, int GrossWtDoc, SqlConnection objConnection = null, bool Balance = true, string whereQty = null, string docname = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            #region Query
            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@GrossWtDoc", GrossWtDoc);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string strQueryPart = "";
                if (Balance == true)

                    strQueryPart = @" 

Select 
             (case when isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)!=0 then 
             (ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))))
             +
             ( abs(isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
             
            ELSE
            (ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(INV_StocKTransactionDetail.TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0)))
            END)

";

                else
                    strQueryPart = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";

                if (docname != null)
                {
                    sb.Append(" and INV_SecondWeight.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentTypeID = INV_SecondWeight.DocumentTypeID and DocumentType like '%" + docname + "%') ");
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDataForDispatchByLoadingNoForSheikhoo, strQueryPart, whereQty, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DispatchHeader.Query_GetDataForDispatchByLoadingNoForSheikhoo, strQueryPart, whereQty, sb.ToString()), sqlparams);

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


        public static DataTable GetDataByItemdID(int deliveryOrderID, int ItemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_DeliveryOrderID_CompanyId_PeriodId = @"
SELECT Distinct [SAL_DeliveryOrderDetail].[DODetailID], [SAL_DeliveryOrderDetail].[DOHeaderID], [SAL_DeliveryOrderDetail].[CompanyID], 
    [SAL_DeliveryOrderDetail].[PeriodID], [SAL_DeliveryOrderDetail].[SalesOrderHeaderID], [SAL_DeliveryOrderDetail].[SalesOrderDetailID], 
    [SAL_DeliveryOrderDetail].[Quantity], [SAL_DeliveryOrderDetail].[IsCancelled], [SAL_DeliveryOrderDetail].[CancellationDate], 
    [SAL_DeliveryOrderDetail].[CancelledBy], [SAL_DeliveryOrderDetail].[IsApproved], [SAL_DeliveryOrderDetail].[ApprovedBy], [SAL_DeliveryOrderDetail].[ApproveDate], 
    [SAL_DeliveryOrderDetail].[IsPaid], [SAL_DeliveryOrderDetail].[PaidAmount], [SAL_DeliveryOrderDetail].[Status], [SAL_DeliveryOrderDetail].[CreatedBy], 
    [SAL_DeliveryOrderDetail].[CreateDate], [SAL_DeliveryOrderDetail].[CreatedByValue], [SAL_DeliveryOrderDetail].[UpdatedBy], [SAL_DeliveryOrderDetail].[UpdateDate], 
    [SAL_DeliveryOrderDetail].[UpdatedByValue], [SAL_DeliveryOrderDetail].[SerialNo], [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID], 
    [SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[Quantity] AS [BaseQuantity], [SAL_DeliveryOrderDetail].[Quantity] AS [RemainingQTY],
    [SET_UnitofMeasure].[UOMName], [inv_item].[UnitWeight], ISNULL([INV_item].[UnitVolume], 1) as UnitVolume, VolumeCode.UOMName as VolumeCodeName, 
    ISNULL([SAL_OrderDetail].[Quantity], 0) - ISNULL(
    (
        select SUM(sdd.Quantity) from SAL_DeliveryOrderDetail sdd 
            Inner Join Sal_DeliveryOrderHeader sdh On sdh.DeliveryOrderID = sdd.DOHeaderID And sdh.CompanyID = sdd.CompanyID And sdh.PeriodID = sdd.PeriodID 
        where SalesOrderHeaderID = Sal_DeliveryOrderDetail.DOHeaderID AND sdd.CompanyID = [SAL_OrderDetail].[CompanyID] 
            and sdd.PeriodID = [SAL_OrderDetail].[PeriodID] AND sdd.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID AND sdh.StatusCode = 1 
    ), 0) as ValidateQuantity, 
    [SAL_DeliveryOrderDetail].[Quantity] as DOQTY, [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[NetAmount], [SAL_OrderDetail].[NetAmount] AS [Amount], 
    [SAL_OrderHeader].[ContractNo] AS [SalesOrderNo], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], 
    Sal_OrderDetail.Rate AS ListPricePerUnit, 
    (ISNULL([SAL_OrderDetail].[NetAmount], 0) / ISNULL([SAL_OrderDetail].[Quantity], 1)) * [SAL_DeliveryOrderDetail].[Quantity] TotalAmount, 
    ROW_NUMBER() Over (Order by Cast([SAL_DeliveryOrderDetail].[DODetailID] as int) desc) as 'RowUniqueID', '' [Action] 
,Sal_DeliveryOrderHeader.DODate
	,Sal_DeliveryOrderHeader.CustomerID,[INV_SET_Category].CategoryID,[SET_UnitofMeasure].[UOMID] as TransactionUOMID
,[SAL_DeliveryOrderDetail].[Quantity] - ISNULL((Select SUM(TransactionQuantity) from INV_LoadingPointDetail 
inner join INV_LoadingPointHeader on INV_LoadingPointHeader.LoadingPointHeaderID = INV_LoadingPointDetail.LoadingPointHeaderID
	and INV_LoadingPointHeader.CompanyID = INV_LoadingPointDetail.CompanyID
	and INV_LoadingPointHeader.PeriodID = INV_LoadingPointDetail.PeriodID
	
	inner join INV_FirstWeight on INV_FirstWeight.DocumentNo = INV_LoadingPointHeader.TareWtDocNo
	and  INV_FirstWeight.DocumentTypeID = INV_LoadingPointHeader.TareWtDocTypeID
	and  INV_FirstWeight.CompanyID = INV_LoadingPointHeader.CompanyID
	and  INV_FirstWeight.PeriodID = INV_LoadingPointHeader.PeriodID
	and GateINID not in
(Select DocumentNo from SAL_VehicleCancellation where SAL_VehicleCancellation.DocumentTypeID = INV_FirstWeight.GateInDocType and SAL_VehicleCancellation.CompanyID = INV_FirstWeight.CompanyID)
	where INV_LoadingPointDetail.Status = 1 
	and INV_LoadingPointDetail.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
	and  INV_LoadingPointDetail.DODetailID =SAL_DeliveryOrderDetail.DODetailID
    and  INV_LoadingPointDetail.CompanyID =SAL_DeliveryOrderDetail.CompanyID
	and  INV_LoadingPointDetail.PeriodID =SAL_DeliveryOrderDetail.PeriodID
),0) as RemainingLoadingQty
,Sal_DeliveryOrderHeader.DeliveryOrderNo ,  AR_SET_Customer.CustomerName
FROM [SAL_DeliveryOrderDetail] 
    INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] 
        AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] 
    INNER JOIN [SAL_OrderHeader] ON [SAL_OrderHeader].[OrderID] = [SAL_OrderDetail].[OrderID] AND [SAL_OrderHeader].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
        AND [SAL_OrderHeader].[PeriodID] = [SAL_OrderDetail].[PeriodID] AND [SAL_OrderHeader].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
    INNER JOIN Sal_DeliveryOrderHeader ON Sal_DeliveryOrderHeader.CompanyID = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND Sal_DeliveryOrderHeader.PeriodID = [SAL_DeliveryOrderDetail].PeriodID AND Sal_DeliveryOrderHeader.[DeliveryOrderID] = [SAL_DeliveryOrderDetail].DOHeaderID 
	LEFT JOIN [SET_UnitofMeasure]  as VolumeCode on [VolumeCode].[UOMID] =  [inv_item].[VolumeUOMCode] 

INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = Sal_DeliveryOrderHeader.CustomerID
	AND AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID
	AND AR_SET_Customer.Status = 1 


WHERE [SAL_DeliveryOrderDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID 
    AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryOrderDetail].[Status] = 1 
and [SAL_OrderDetail].[ItemID] = @ItemID

ORDER BY [SAL_DeliveryOrderDetail].[DODetailID] ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@ItemID", ItemID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_DeliveryOrderID_CompanyId_PeriodId, sqlparams);

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


        public static bool UpdateItemQty(int Companyid, int perioid, int DOHeaderID, long ContractDetailID, decimal Quantity, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DOHeaderID", DOHeaderID);
                sqlparams[3] = new SqlParameter("@ContractDetailID", ContractDetailID);
                sqlparams[4] = new SqlParameter("@Quantity", Quantity);
                

                const string Query_DOSOClose = @"
UPDATE SAL_DeliveryOrderDetail set Quantity = @Quantity where DOHeaderID = @DOHeaderID and CompanyID = @CompanyID and ContractDetailID = @ContractDetailID
";


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DOSOClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DOSOClose, sqlparams) > 0)
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


        public static bool UpdateCustomerID(int Companyid, int perioid, int DOHeaderID, int CustomerID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DOHeaderID", DOHeaderID);
                sqlparams[3] = new SqlParameter("@CustomerID", CustomerID);


                const string Query_UpdateCustomerID = @"
UPDATE SAL_DeliveryOrderHeader set CustomerID = @CustomerID where DeliveryOrderID = @DOHeaderID and CompanyID = @CompanyID 
";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateCustomerID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateCustomerID, sqlparams) > 0)
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


        public static bool UpdateBrokerID(int Companyid, int perioid, int DOHeaderID, int BrokerID, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DOHeaderID", DOHeaderID);
                sqlparams[3] = new SqlParameter("@BrokerID", BrokerID);


                const string Query_UpdateCustomerID = @"
UPDATE SAL_DeliveryOrderHeader set BrokerID = @BrokerID where DeliveryOrderID = @DOHeaderID and CompanyID = @CompanyID 
";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateCustomerID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateCustomerID, sqlparams) > 0)
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
