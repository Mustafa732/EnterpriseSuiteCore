using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;


namespace FnI.BusinessAccessLayer.Sales
{
   public partial class SAL_DeliveryContractDetail : BaseClass
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
SELECT Distinct [SAL_DeliveryContractDetail].[DODetailID], [SAL_DeliveryContractDetail].[DOHeaderID], [SAL_DeliveryContractDetail].[CompanyID], 
    [SAL_DeliveryContractDetail].[PeriodID], [SAL_DeliveryContractDetail].[SalesOrderHeaderID], [SAL_DeliveryContractDetail].[SalesOrderDetailID], 
    [SAL_DeliveryContractDetail].[Quantity], [SAL_DeliveryContractDetail].[IsCancelled], [SAL_DeliveryContractDetail].[CancellationDate], 
    [SAL_DeliveryContractDetail].[CancelledBy], [SAL_DeliveryContractDetail].[IsApproved], [SAL_DeliveryContractDetail].[ApprovedBy], [SAL_DeliveryContractDetail].[ApproveDate], 
    [SAL_DeliveryContractDetail].[IsPaid], [SAL_DeliveryContractDetail].[PaidAmount], [SAL_DeliveryContractDetail].[Status], [SAL_DeliveryContractDetail].[CreatedBy], 
    [SAL_DeliveryContractDetail].[CreateDate], [SAL_DeliveryContractDetail].[CreatedByValue], [SAL_DeliveryContractDetail].[UpdatedBy], [SAL_DeliveryContractDetail].[UpdateDate], 
    [SAL_DeliveryContractDetail].[UpdatedByValue], [SAL_DeliveryContractDetail].[SerialNo], [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID], 
    [SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[Quantity] AS [BaseQuantity], [SAL_DeliveryContractDetail].[Quantity] AS [RemainingQTY],
    [SET_UnitofMeasure].[UOMName], [inv_item].[UnitWeight], ISNULL([INV_item].[UnitVolume], 1) as UnitVolume, VolumeCode.UOMName as VolumeCodeName, 
    ISNULL([SAL_OrderDetail].[Quantity], 0) - ISNULL(
    (
        select SUM(sdd.Quantity) from SAL_DeliveryContractDetail sdd 
            Inner Join SAL_DeliveryContractHeader sdh On sdh.DeliveryContractID = sdd.DOHeaderID And sdh.CompanyID = sdd.CompanyID And sdh.PeriodID = sdd.PeriodID 
        where SalesOrderHeaderID = SAL_DeliveryContractDetail.DOHeaderID AND sdd.CompanyID = [SAL_OrderDetail].[CompanyID] 
            and sdd.PeriodID = [SAL_OrderDetail].[PeriodID] AND sdd.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID AND sdh.StatusCode = 1 
    ), 0) as ValidateQuantity, 
    [SAL_DeliveryContractDetail].[Quantity] as DOQTY, [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[NetAmount], [SAL_OrderDetail].[NetAmount] AS [Amount], 
    [SAL_OrderHeader].[ContractNo] AS [SalesOrderNo], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], 
    Sal_OrderDetail.Rate AS ListPricePerUnit, 
    (ISNULL([SAL_OrderDetail].[NetAmount], 0) / ISNULL([SAL_OrderDetail].[Quantity], 1)) * [SAL_DeliveryContractDetail].[Quantity] TotalAmount, 
    ROW_NUMBER() Over (Order by Cast([SAL_DeliveryContractDetail].[DODetailID] as int) desc) as 'RowUniqueID', '' [Action] 
,SAL_DeliveryOrderHeader.DeliveryOrderID,[SAL_DeliveryOrderDetail].DODetailID
FROM [SAL_DeliveryContractDetail] 
    INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryContractDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryContractDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryContractDetail].[PeriodID] 
        AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryContractDetail].[SalesOrderHeaderID] 
    INNER JOIN [SAL_OrderHeader] ON [SAL_OrderHeader].[OrderID] = [SAL_OrderDetail].[OrderID] AND [SAL_OrderHeader].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
        AND [SAL_OrderHeader].[PeriodID] = [SAL_OrderDetail].[PeriodID] AND [SAL_OrderHeader].[Status] = 1 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
    INNER JOIN SAL_DeliveryContractHeader ON SAL_DeliveryContractHeader.CompanyID = [SAL_DeliveryContractDetail].[CompanyID] 
        AND SAL_DeliveryContractHeader.PeriodID = [SAL_DeliveryContractDetail].PeriodID AND SAL_DeliveryContractHeader.[DeliveryContractID] = [SAL_DeliveryContractDetail].DOHeaderID 
	LEFT JOIN [SET_UnitofMeasure]  as VolumeCode on [VolumeCode].[UOMID] =  [inv_item].[VolumeUOMCode] 

	INNER JOIN [SAL_DeliveryOrderDetail] on  [SAL_DeliveryOrderDetail].[SalesOrderDetailID]  = [SAL_OrderDetail].OrderDetailID
        AND [SAL_DeliveryOrderDetail].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
		AND [SAL_DeliveryOrderDetail].[PeriodID] = [SAL_OrderDetail].[PeriodID] 
        AND [SAL_DeliveryOrderDetail].[Status] = 1 
		AND [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] = [SAL_OrderDetail].OrderID

		INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.CompanyID = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND SAL_DeliveryOrderHeader.PeriodID = [SAL_DeliveryOrderDetail].PeriodID 
		AND SAL_DeliveryOrderHeader.[DeliveryOrderID] = [SAL_DeliveryOrderDetail].DOHeaderID 

WHERE [SAL_DeliveryContractDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryContractDetail].[CompanyID] = @CompanyID 
    AND [SAL_DeliveryContractDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryContractDetail].[Status] = 1 
and SAL_DeliveryOrderDetail.ContractHeaderID = SAL_DeliveryContractDetail.ContractHeaderID
		and SAL_DeliveryOrderDetail.ContractDetailID = SAL_DeliveryContractDetail.ContractDetailID
ORDER BY [SAL_DeliveryContractDetail].[DODetailID] ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

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

        public static DataTable GetDataByKeyTableForDispatch(int deliveryOrderID, int companyID, int periodID, SqlConnection objConnection = null, bool Balance = true, string whereQty = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch = @" Select * From 
(
    SELECT [SAL_DeliveryContractDetail].[DODetailID], [SAL_DeliveryContractDetail].[DOHeaderID], [SAL_DeliveryContractDetail].[CompanyID], 
        [SAL_DeliveryContractDetail].[PeriodID], [SAL_DeliveryContractDetail].[SalesOrderHeaderID], [SAL_DeliveryContractDetail].[SalesOrderDetailID], 
        ISNULL(
	    (
            {0}
		    --  Select ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) 
            --  Select ISNULL(DODetail.[Quantity], 0) - 0
		    from SAL_DeliveryContractDetail DODetail 
			    Inner Join SAL_DeliveryContractHeader On SAL_DeliveryContractHeader.CompanyID = DODetail.CompanyID And SAL_DeliveryContractHeader.PeriodID = DODetail.PeriodID 
                    And SAL_DeliveryContractHeader.DeliveryContractID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND SAL_DeliveryContractHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND SAL_DeliveryContractHeader.DeliveryContractNo = INV_StockTransactionDetail.SourceCode 
                    AND INV_StockTransactionDetail.Status = 1
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
		    Where DODetail.[DOHeaderID] = [SAL_DeliveryContractDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryContractDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity
	    )
          --            - 
--	    (
--case when (select sum(ReversedQuantity) from sal_ReverseDetail 
--			where sal_ReverseDetail.SourceHeaderID = [SAL_DeliveryContractDetail].DoheaderID
--			and sal_ReverseDetail.SourceDetailID = [SAL_DeliveryContractDetail].DODetailID
--			and sal_ReverseDetail.CompanyID = [SAL_DeliveryContractDetail].CompanyID
--			and sal_ReverseDetail.PeriodID = [SAL_DeliveryContractDetail].PeriodID) is null then 0 else 
--			(select sum(ReversedQuantity) from sal_ReverseDetail 
--			where sal_ReverseDetail.SourceHeaderID = [SAL_DeliveryContractDetail].DoheaderID
--			and sal_ReverseDetail.SourceDetailID = [SAL_DeliveryContractDetail].DODetailID
--			and sal_ReverseDetail.CompanyID = [SAL_DeliveryContractDetail].CompanyID
--			and sal_ReverseDetail.PeriodID = [SAL_DeliveryContractDetail].PeriodID)
--			end
			
--	    )
        , 0) [Quantity], 
        [SAL_DeliveryContractDetail].[Quantity] as BaseQty,
        [SAL_DeliveryContractDetail].[IsCancelled], [SAL_DeliveryContractDetail].[CancellationDate], 
        [SAL_DeliveryContractDetail].[CancelledBy], [SAL_DeliveryContractDetail].[IsApproved], [SAL_DeliveryContractDetail].[ApprovedBy], 
        [SAL_DeliveryContractDetail].[ApproveDate], [SAL_DeliveryContractDetail].[IsPaid], [SAL_DeliveryContractDetail].[PaidAmount], [SAL_DeliveryContractDetail].[Status], 
        [SAL_DeliveryContractDetail].[CreatedBy], [SAL_DeliveryContractDetail].[CreateDate], [SAL_DeliveryContractDetail].[CreatedByValue], 
        [SAL_DeliveryContractDetail].[UpdatedBy], [SAL_DeliveryContractDetail].[UpdateDate], [SAL_DeliveryContractDetail].[UpdatedByValue], [SAL_OrderDetail].[ItemCategoryID], 
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
		    from SAL_DeliveryContractDetail DODetail 
			    Inner Join SAL_DeliveryContractHeader On SAL_DeliveryContractHeader.CompanyID = DODetail.CompanyID And SAL_DeliveryContractHeader.PeriodID = DODetail.PeriodID 
                    And SAL_DeliveryContractHeader.DeliveryContractID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND SAL_DeliveryContractHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND SAL_DeliveryContractHeader.DeliveryContractNo = INV_StockTransactionDetail.SourceCode 
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
		    Where DODetail.[DOHeaderID] = [SAL_DeliveryContractDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryContractDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity 
	    ), 0)), 0) AS TotalAmount,
			 			  			  isnull((Select (Quantity - ABS(isnull(TransactionQuantity,0))) as RemainingQty  from (
			  Select SourceCode,SourceHeaderID,Sum(TransactionQuantity) as TransactionQuantity , [SAL_DeliveryContractDetail].Quantity from  [INV_StockTransactionDetail]
						Inner Join [INV_StockTransactionHeader] on [INV_StockTransactionHeader].StockTransactionID=[INV_StockTransactionDetail].StockTransactionID
						AND [INV_StockTransactionHeader].CompanyID=[INV_StockTransactionDetail].CompanyID
			  AND [INV_StockTransactionHeader].PeriodID=[INV_StockTransactionDetail].PeriodID
			  AND [INV_StockTransactionHeader].FiscalID=[INV_StockTransactionDetail].FiscalID
			  AND [INV_StockTransactionHeader].StatusCode=1 And [INV_StockTransactionHeader].Status=1

			  INNER JOIN [SAL_DeliveryContractDetail] ON [SAL_DeliveryContractDetail].[DODetailID] = [INV_StockTransactionDetail].[SourceLineID]
				 AND [SAL_DeliveryContractDetail].[DOHeaderID] = [INV_StockTransactionDetail].[SourceHeaderID]
				  AND [SAL_DeliveryContractDetail].[CompanyID] = [INV_StockTransactionDetail].[CompanyID] 
				  AND [SAL_DeliveryContractDetail].[PeriodID] = [INV_StockTransactionDetail].[PeriodID]
				   AND [SAL_DeliveryContractDetail].[Status] = 1

				   INNER JOIN [SAL_DeliveryContractHeader] SalDaliveryOrder ON SalDaliveryOrder.[DeliveryOrderID] = [SAL_DeliveryContractDetail].[DOHeaderID] 
						AND SalDaliveryOrder.[CompanyID] = [SAL_DeliveryContractDetail].[CompanyID]
						 AND SalDaliveryOrder.[PeriodID] = [SAL_DeliveryContractDetail].[PeriodID]
						 AND SalDaliveryOrder.DocumentTypeID = [INV_StockTransactionDetail].SourceDocumentTypeID
						  AND SalDaliveryOrder.[Status] = 1
		   Where SalDaliveryOrder.DeliveryOrderID = [SAL_DeliveryContractHeader].DeliveryOrderID
		   And [SAL_DeliveryContractDetail].Status=1
			   Group By SourceCode,SourceHeaderID,[SAL_DeliveryContractDetail].Quantity
			   ) as RemainingQty), [SAL_DeliveryContractDetail].Quantity)RemainingQty, 
        '' [Action],-1[RackID], '' [RackName] ,-1[LocationID],'' [LocationName],[SAL_OrderDetail].Inclusive ,SAL_DeliveryContractHeader.DocumentTypeID,SAL_DeliveryContractHeader.DeliveryContractNo
    FROM [SAL_DeliveryContractDetail]
    inner join  SAL_DeliveryContractHeader on SAL_DeliveryContractHeader.DeliveryContractID = [SAL_DeliveryContractDetail].DOHeaderID
    and SAL_DeliveryContractHeader.companyID = [SAL_DeliveryContractDetail].companyID
    and SAL_DeliveryContractHeader.PeriodID = [SAL_DeliveryContractDetail].PeriodID
  INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryContractDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryContractDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryContractDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryContractDetail].[SalesOrderHeaderID]
 LEFT JOIN Set_Batch ON Set_Batch.BatchID = SAL_OrderDetail.BatchId --AND  Set_Batch.CompanyID = SAL_OrderDetail.CompanyID
   AND Set_Batch.PeriodID = SAL_OrderDetail.PeriodID
INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1
  INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1
left join Set_ItemCatalog on Set_ItemCatalog.ItemCatalogID = [INV_Item].ItemCatalogGroupID
  INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
  WHERE [SAL_DeliveryContractDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryContractDetail].[CompanyID] = @CompanyID  AND [SAL_DeliveryContractDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryContractDetail].[Status] = 1
and 
  ISNULL(
	    (
            -- Select ISNULL(DODetail.[Quantity], 0) - (ABS(SUM(ISNULL(TransactionQuantity, 0))) - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
		    --  Select ISNULL(DODetail.[Quantity], 0) - ABS(SUM(ISNULL(TransactionQuantity, 0))) 
              Select ISNULL(DODetail.[Quantity], 0) - 0
		    from SAL_DeliveryContractDetail DODetail 
			    Inner Join SAL_DeliveryContractHeader On SAL_DeliveryContractHeader.CompanyID = DODetail.CompanyID And SAL_DeliveryContractHeader.PeriodID = DODetail.PeriodID 
                    And SAL_DeliveryContractHeader.DeliveryContractID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND SAL_DeliveryContractHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND SAL_DeliveryContractHeader.DeliveryContractNo = INV_StockTransactionDetail.SourceCode 
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

		    Where DODetail.[DOHeaderID] = [SAL_DeliveryContractDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryContractDetail].DODetailID 
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
                    //            ( [SAL_DeliveryContractDetail].[Quantity] - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0))
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
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart, whereQty), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_Select_By_DeliveryOrderID_CompanyId_PeriodId_ForDispatch, strQueryPart, whereQty), sqlparams);

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

            string Query_NewSerialNo_ByCompanyPeriodDDIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM [SAL_DeliveryContractDetail] 
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

        public static DataRow GetRefrenceNoByDeliveryNo(int companyID, int orderID, int periodID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            string Query_GetRefrenceNoByKey = @"

Select Distinct referenceno, Sal_DeliveryOrderDetail.DOHeaderID, SAL_ContractHeader.ContractID as OrderID, Set_Batch.Description as Batch, SAL_ContractHeader.ContractNo 
,Sal_DeliveryOrderDetail.SalesOrderHeaderID
From SAL_ContractHeader 
	Inner JOIN Sal_DeliveryOrderDetail ON Sal_DeliveryOrderDetail.ContractHeaderID = SAL_ContractHeader.ContractID 
        AND Sal_DeliveryOrderDetail.CompanyID = SAL_ContractHeader.CompanyID AND Sal_DeliveryOrderDetail.PeriodID = SAL_ContractHeader.PeriodID 
	Left Join Set_Batch On Set_Batch.PeriodID = SAL_ContractHeader.PeriodID 

        And Set_Batch.BatchID = SAL_ContractHeader.Season 
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

        //delivery order detail report
        public DataTable DeliveryOrderDetailReport(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null)
        {

            DataTable dtToReturn = null;
            DataSet ds = null;
            string whereFilter = string.Empty;

            #region Query

            string DeliveryOrderDetailReport_Query = @"

select 
SAL_DeliveryContractHeader.DeliveryContractNo ,
SAL_DeliveryContractHeader.CustomerID,
AR_SET_Customer.CustomerName,
SAL_DeliveryContractDetail.PaidAmount,
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
CAST((datepart(year, SAL_DeliveryContractHeader.DODate)) AS VARCHAR(10)) + '-' + CAST((datepart(month, SAL_DeliveryContractHeader.DODate)) AS VARCHAR(10))+ '-' + CAST((datepart(day, SAL_DeliveryContractHeader.DODate)) AS VARCHAR(10)) as DODate


from SAL_DeliveryContractHeader
inner join SAL_DeliveryContractDetail on SAL_DeliveryContractDetail.DOHeaderID = SAL_DeliveryContractHeader.DeliveryContractID
	and SAL_DeliveryContractDetail.CompanyID = SAL_DeliveryContractHeader.CompanyID
	and SAL_DeliveryContractDetail.PeriodID = SAL_DeliveryContractHeader.PeriodID	

inner join SAL_OrderDetail on SAL_OrderDetail.OrderDetailID = SAL_DeliveryContractDetail.SalesOrderDetailID
	and SAL_OrderDetail.OrderID = SAL_DeliveryContractDetail.SalesOrderHeaderID
	and SAL_OrderDetail.CompanyID = SAL_DeliveryContractDetail.CompanyID
	and SAL_OrderDetail.PeriodID = SAL_DeliveryContractDetail.PeriodID

Inner Join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID
	--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID	

Inner Join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryContractHeader.CustomerID
	--and AR_SET_Customer.CompanyID = SAL_DeliveryContractHeader.CompanyID
	and AR_SET_Customer.PeriodID = SAL_DeliveryContractHeader.PeriodID

Inner Join INV_SET_Category on INV_SET_Category.CategoryID = INV_Item.ItemTypeID
	-- 
	
Inner Join set_unitofmeasure on set_unitofmeasure.UOMID=Sal_orderDetail.ItemUnitId

inner JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID

LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ORDERHEADER.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID 
where SAL_DeliveryContractHeader.CompanyID = @CompanyID
and SAL_DeliveryContractHeader.PeriodID = @PeriodID
{0}
 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                    whereFilter = "And SAL_DeliveryContractHeader.DODate between '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00' and '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";
                else if (dateFrom.HasValue)
                    whereFilter = "And SAL_DeliveryContractHeader.DODate >= '" + DateUtil.ConvertToString(dateFrom.Value, true) + " 00:00:00'";
                else if (dateTo.HasValue)
                    whereFilter = "And SAL_DeliveryContractHeader.DODate <= '" + DateUtil.ConvertToString(dateTo.Value, true) + " 23:59:59'";

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
        Sum(SAL_DeliveryContractDetail.Quantity) DoQuantity,
        ContractNo SoNumber,ContractDate SoDate,
        SAL_OrderDetail.Rate,
        Case when Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) < 0 Then 0 Else
            Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) End DispatchQuantity,
        AR_SET_Customer.CustomerName,
        Sum(IsNULL(SAL_DeliveryContractDetail.Quantity,0)) - Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0)) BalanceQuantity,
        (Sum(IsNULL(SAL_DeliveryContractDetail.Quantity,0)) - Sum(IsNull(INV_StockTransactionDetail.TransactionQuantity,0))) * SAL_OrderDetail.Rate BalanceAmount

        FROM SAL_DeliveryContractDetail

        JOIN SAL_DeliveryContractHeader on SAL_DeliveryContractHeader.DeliveryContractID = SAL_DeliveryContractDetail.DOHeaderID
            AND SAL_DeliveryContractHeader.CompanyID = SAL_DeliveryContractDetail.CompanyID 
            AND SAL_DeliveryContractHeader.PeriodID = SAL_DeliveryContractDetail.PeriodID 

        JOIN SAL_OrderDetail on SAL_OrderDetail.OrderDetailID = SAL_DeliveryContractDetail.SalesOrderDetailID 
            AND SAL_OrderDetail.OrderID = SAL_DeliveryContractDetail.SalesOrderHeaderID 
            AND SAL_OrderDetail.CompanyID = SAL_DeliveryContractDetail.CompanyID 
            AND SAL_OrderDetail.PeriodID = SAL_DeliveryContractDetail.PeriodID 

        JOIN SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
            AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
            AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 

        LEFT JOIN INV_StockTransactionDetail on INV_StockTransactionDetail.SourceLineID = SAL_DeliveryContractDetail.DODetailID 
            AND INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryContractDetail.DOHeaderID 
            AND INV_StockTransactionDetail.CompanyID = SAL_DeliveryContractDetail.CompanyID 
            AND INV_StockTransactionDetail.PeriodID = SAL_DeliveryContractDetail.PeriodID 

        LEFT JOIN INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

        JOIN AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryContractHeader.CustomerID
            --AND AR_SET_Customer.CompanyID = SAL_DeliveryContractHeader.CompanyID 
            AND AR_SET_Customer.PeriodID = SAL_DeliveryContractHeader.PeriodID 

        Group By DeliveryOrderNo, DODate, ContractNo, ContractDate
                , SAL_OrderDetail.Rate, AR_SET_Customer.CustomerName
                , SAL_DeliveryContractDetail.CompanyID, SAL_DeliveryContractDetail.PeriodID
                , SAL_OrderDetail.ItemID

        Having SAL_DeliveryContractDetail.CompanyID = @CompanyID AND 
        SAL_DeliveryContractDetail.PeriodID = @PeriodID AND 
        (SAL_OrderDetail.ItemID = @ItemID OR @ItemID Is Null)
        )fTab Where BalanceQuantity > 0 
        ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (itemId.HasValue)
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
		    
		    from SAL_DeliveryContractDetail DODetail 
			    Inner Join SAL_DeliveryContractHeader On SAL_DeliveryContractHeader.CompanyID = DODetail.CompanyID And SAL_DeliveryContractHeader.PeriodID = DODetail.PeriodID 
                    And SAL_DeliveryContractHeader.DeliveryContractID = DODetail.DOHeaderID 
                Left JOIN INV_StocKTransactionDetail ON DODetail.CompanyID = INV_StockTransactionDetail.CompanyID 
                    AND DODetail.PeriodID = INV_StockTransactionDetail.PeriodID AND DODetail.DOHeaderID = INV_StockTransactionDetail.SourceHeaderID 
                    AND DODetail.DODetailID = INV_StockTransactionDetail.SourceLineID 
                    AND SAL_DeliveryContractHeader.DocumentTypeID = INV_StockTransactionDetail.SourceDocumentTypeID 
                    AND SAL_DeliveryContractHeader.DeliveryContractNo = INV_StockTransactionDetail.SourceCode 
               left join (
                    select distinct SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ,sum(ReversedQuantity) as ReversedQuantity
                    from  SAL_ReverseDetail 
                    group by SourceHeaderID,SourceDetailID,SAL_ReverseDetail.[CompanyID],SAL_ReverseDetail.[PeriodID]
                    ) SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = [INV_StockTransactionDetail].StockTransactionID
						and SAL_ReverseDetail .SourceDetailID = [INV_StockTransactionDetail].TransactionDetailID
						and SAL_ReverseDetail .[CompanyID] = [INV_StockTransactionDetail].[CompanyID]
						and SAL_ReverseDetail .[PeriodID] = [INV_StockTransactionDetail].[PeriodID]

		    Where DODetail.[DOHeaderID] = [SAL_DeliveryContractDetail].DOHeaderID and DODetail.[DODetailID] = [SAL_DeliveryContractDetail].DODetailID 
                AND DODetail.CompanyID = @CompanyID AND DODetail.PeriodID = @PeriodID 
		    Group BY DODetail.Quantity
	    ), 0) [Quantity]
    
    FROM [SAL_DeliveryContractDetail]
  INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryContractDetail].[SalesOrderDetailID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryContractDetail].[CompanyID] AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryContractDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryContractDetail].[SalesOrderHeaderID]
  INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
    --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1
  INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
AND [INV_Item].[Status] = 1
  INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1
  WHERE [SAL_DeliveryContractDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryContractDetail].[CompanyID] = @CompanyID  AND [SAL_DeliveryContractDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryContractDetail].[Status] = 1
  -- AND [SAL_DeliveryContractDetail].DODetailID = @DODetailID
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
                    ParmString2 = " AND [SAL_DeliveryContractDetail].DODetailID = @DODetailID ";
                }
                else
                {
                    ParmString1 = " Select ISNULL(DODetail.[Quantity], 0) - 0 ";
                    ParmString2 = " AND [SAL_DeliveryContractDetail].DODetailID = @DODetailID ";
                }

                DORemaianingQty = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, string.Format(Query_GetRemainingQtyInDO, ParmString1, ParmString2), sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return DORemaianingQty;
        }

        #endregion
    }
}
