using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Linq;
using DAL.DataAccessLayer;
using FnI.BusinessAccessLayer.Setup;
using Common.Utils;
using FnI.BusinessAccessLayer.Inventory.Setup;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_StockTransactionDetail
    {
        private int f_RowUniqueID;

        public int RowUniqueID
        {
            get { return f_RowUniqueID; }
            set { f_RowUniqueID = value; }
        }

        #region Constant Queries


        const string Query_UpdateFlagAndComments = @"

Update [INV_StockTransactionDetail] 
Set [IsDone] = @IsDone,
[Comments] = @Comments, 
[UpdatedBy] = @UpdatedBy, 
[UpdateDate] = @UpdateDate, 
[UpdatedByValue] = @UpdatedByValue 
WHERE [TransactionDetailID] = @TransactionDetailID 
	and StockTransactionID = @StockTransactionID
	and CompanyID = @CompanyID
	and PeriodID = @PeriodID

";


        const string Query_for_vehicle = @"

select * from INV_StockTransactionDetail where CompanyID=@CompanyID and PeriodID=@PeriodID and SourceCode=@SourceCode and
SourceHeaderID=@SourceHeaderID and SourceLineID=@SourceLineID and SourceDocumentTypeID=@SourceDocumentTypeID


";


        const string queryforgettingracknames = @"select RackID from Set_RackItems where ItemID=@ItemID";

        const string Query_UpdateIssuedQuantity = @"Update INV_StockTransactionDetail
                            Set IssuedQuantity = @IQ
                Where CompanyID = @CompanyID AND PeriodID = @PeriodID and StockTransactionID = @STID and TransactionDetailID = @TDID";

        const string Query_UpdateIsIssued = @"Update INV_StockTransactionDetail
                        Set IsIssued = @IssuedStatus
                Where CompanyID = @CompanyID AND PeriodID = @PeriodID and StockTransactionID = @STID and TransactionDetailID = @TDID";

        const string Query_GetAllDataAgainstItemFIFO = @"
Select TransactionQuantity, IssuedQuantity, IsIssued, ItemID, TransactionCost, * 
From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND Set_DocumentType.Status = 1 
Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.ItemID = @ItemID 
    AND Set_DocumentType.TransactionImpact = 'Positive' AND (INV_StockTransactionDetail.IsIssued IS NULL OR INV_StockTransactionDetail.IsIssued = 0)
    {filter} ";

        const string Query_GetAllDataAgainstItemFIFOByLocation = @"

            Select TransactionQuantity, ISNULL(IssuedQuantity,0)IssuedQuantity, IsIssued, ItemID, TransactionCost, * 
            From INV_StockTransactionDetail 
            Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND INV_StockTransactionHeader.Status = 1 and INV_StockTransactionDetail.status = 1
            Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
            AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND Set_DocumentType.Status = 1 
            LEFT JOIN SET_Location ON SET_Location.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end)
    
            LEFT JOIN SET_Location SL ON SL.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end) 

            Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.ItemID = @ItemID 
            AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)
            AND Set_DocumentType.TransactionImpact = 'Positive' 
             And INV_StockTransactionDetail.Status = 1
             And INV_StockTransactionHeader.Status = 1
             AND INV_StockTransactionHeader.IsActive = 1 
             And INV_StockTransactionHeader.StatusCode = 1
             AND (INV_StockTransactionDetail.IsIssued IS NULL OR INV_StockTransactionDetail.IsIssued = 0)
           -- And (
            --DocumentType like '%Goods Receipt Note%' 
            --or DocumentType like '%grn%' 
            --or DocumentType like '%gin%' 
            --or DocumentType like '%opening%'
            --or DocumentType like '%transfer%out%'
            --or DocumentType like '%transfer%in%'
            --or DocumentType like '%adjustment%'
            --)

        and INV_StockTransactionDetail.transactionquantity <> 0
        and ISNULL(INV_StockTransactionDetail.IssuedQuantity,0) < INV_StockTransactionDetail.TransactionQuantity
        AND INV_StockTransactionHeader.DocumentDate < getdate()

            {filter} 

    ";




        const string Query_GetAllForFIFOSetting = @"

Select 

INV_StockTransactionHeader.DocumentDate,
INV_StockTransactionDetail.stocktransactionid,
INV_StockTransactionDetail.transactiondetailid , 

(   case when Set_DocumentType.TransactionImpact ='Positive' then 
sum(isnull(INV_StockTransactionDetail.transactionquantity,0))
else 
0 end)qty,

(case when Set_DocumentType.TransactionImpact ='Negative' then 
sum(isnull(INV_StockTransactionDetail.transactionquantity,0))
else 
0 end)issuance
From INV_StockTransactionDetail 
Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
AND INV_StockTransactionHeader.Status = 1 and INV_StockTransactionDetail.status = 1
Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionDetail.PeriodID 
AND Set_DocumentType.Status = 1 
LEFT JOIN SET_Location ON SET_Location.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end)

LEFT JOIN SET_Location SL ON SL.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end)

Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
And INV_StockTransactionDetail.ItemID = @ItemID 
AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)
AND Set_DocumentType.TransactionImpact in( 'Positive' ,'Negative')
And INV_StockTransactionDetail.Status = 1
And INV_StockTransactionHeader.Status = 1
AND INV_StockTransactionHeader.IsActive = 1 
And INV_StockTransactionHeader.StatusCode = 1

   AND INV_StockTransactionHeader.DocumentDate < getdate()

group by INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.stocktransactionid,INV_StockTransactionDetail.TransactionDetailID,Set_DocumentType.TransactionImpact 
         {filter} 
";

        const string Query_Get_QTYFIFOByLocation = @"

            Select 

            sum(TransactionQuantity) qty
           --(sum(TransactionQuantity) - sum(isnull(IssuedQuantity,0))) as qty
            From INV_StockTransactionDetail 
            Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND INV_StockTransactionHeader.Status = 1 and INV_StockTransactionDetail.status = 1
            Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
            AND Set_DocumentType.CompanyID = INV_StockTransactionDetail.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionDetail.PeriodID 
            AND Set_DocumentType.Status = 1 
            LEFT JOIN SET_Location ON SET_Location.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end)
    
            LEFT JOIN SET_Location SL ON SL.LocationID = (CASE when SET_DocumentType.Documenttype LIKE '%stock%return%' THEN 
						ISNULL(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.locationid)
						ELSE 
						INV_StockTransactionHeader.locationid end)

            Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.ItemID = @ItemID 
            AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)
            --AND Set_DocumentType.TransactionImpact = 'Positive' 
            AND Set_DocumentType.TransactionImpact in( 'Positive' ,'Negative')
             And INV_StockTransactionDetail.Status = 1
             And INV_StockTransactionHeader.Status = 1
             AND INV_StockTransactionHeader.IsActive = 1 
             And INV_StockTransactionHeader.StatusCode = 1
            --AND INV_StockTransactionHeader.DocumentDate < getdate()
             {0}

            --AND (INV_StockTransactionDetail.IsIssued IS NULL OR INV_StockTransactionDetail.IsIssued = 0)
           -- And (
            --DocumentType like '%Goods Receipt Note%' 
            --or DocumentType like '%grn%' 
            --or DocumentType like '%gin%' 
            --or DocumentType like '%opening%'
            --or DocumentType like '%transfer%out%'
            --or DocumentType like '%transfer%in%'
            --or DocumentType like '%adjustment%'
            --)
            --and INV_StockTransactionDetail.transactionquantity <> 0

            {filter} 

    ";



        const string Query_GetAllDataAgainstMultipleItemsFIFO = @"
Select TransactionQuantity, IssuedQuantity, IsIssued, ItemID, TransactionCost, * 
From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
    And (INV_StockTransactionDetail.IsIssued is null Or INV_StockTransactionDetail.IsIssued = 0) 
    And (@ItemID LIKE '%,' + CONVERT(VARCHAR, INV_StockTransactionDetail.ItemID) + ',%') 
    And DocumentType in ('Goods Receipt Note', 'Formulation (GRN)', 'Opening', 'Botling One', 'GRN-Opening') 
    {filter} ";

        const string Query_GetAllDataAgainstMultipleItemsFIFON = @"
Select TransactionQuantity, IssuedQuantity, IsIssued, ItemID, TransactionCost, * 
From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionId = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 

    --LEFT JOIN SET_Location ON SET_Location.LocationID = isnull(INV_StockTransactionHeader.ToLocationID,INV_StockTransactionHeader.LocationID)
    LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID
    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 

Where INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
    And (INV_StockTransactionDetail.IsIssued is null Or INV_StockTransactionDetail.IsIssued = 0) 
    AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)

    And (@ItemID LIKE '%,' + CONVERT(VARCHAR, INV_StockTransactionDetail.ItemID) + ',%') 
    --And (
  --      DocumentType like '%Goods Receipt Note%' 
  --      or DocumentType like '%grn%' 
  --      or DocumentType like '%gin%' 
  --      or DocumentType like '%opening%'
  --      or DocumentType like '%transfer%out%'
  --      or DocumentType like '%transfer%in%'
  --  )
    AND Set_DocumentType.TransactionImpact<>'No Impact'
    {filter} ";

        const string Query_DemandStatus = @"

";





        const string Query_Delete_ByHeaderKey = @" Update [INV_StockTransactionDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [StockTransactionID] = @StockTransactionID ";

        const string Query_Delete_ByHeaderKeyForStockRequisition = @" Update [INV_StockTransactionDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [StockTransactionID] = @StockTransactionID AND [CompanyID] = @CompanyID";

        const string Query_Delete_ByHeaderKeyFIFO = @" update [INV_StockTransactionDetail] set [status] = 0  WHERE [StockTransactionID] = @StockTransactionID ";


        const string Query_NewSerialNo_ByCompanyPeriodTransactionIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM [INV_StockTransactionDetail] 
            WHERE [TransactionDetailID] = @TransactionDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID; ";


        const string Query_Select_TransferIn_Against_TransferOut = @" 
Select Inv_Item.ItemTypeID AS CategoryID, INV_stockTransactionDetail.ItemID, Sum(INV_stockTransactionDetail.TransactionQuantity) AS TransactionQuantity 
From INV_stockTransactionHeader 
    INNER JOIN INV_stockTransactionDetail ON INV_stockTransactionDetail.StockTransactionID = INV_stockTransactionHeader.StockTransactionID 
        AND INV_stockTransactionHeader.CompanyID = INV_stockTransactionDetail.CompanyID AND INV_stockTransactionHeader.PeriodID = INV_stockTransactionDetail.PeriodID 
        AND INV_stockTransactionDetail.Status = 1 
    INNER JOIN Inv_Item ON Inv_Item.ItemID = INV_stockTransactionDetail.ItemID --Inv_Item.CompanyID = INV_stockTransactionDetail.CompanyID AND  
WHERE INV_stockTransactionHeader.CompanyID = @CompanyID AND INV_stockTransactionHeader.PeriodID = @PeriodID 
    AND INV_stockTransactionDetail.SourceHeaderID = @SourceHeaderID AND INV_stockTransactionDetail.SourceDocumentTypeID = @SourceDocumentTypeID 
    AND INV_stockTransactionDetail.SourceCode = @SourceDocumentNo
--AND INV_stockTransactionHeader.StatusCode = 1 
GROUP BY Inv_Item.ItemTypeID, INV_stockTransactionDetail.ItemID ";

        const string Query_SumAllTransactionQuantity = @"
select IsNull(Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity END), 0) As 
    SumOfQuantityInStock 
from INV_StockTransactionDetail
    Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
left join Set_Location on Set_Location.LocationID = INV_StockTransactionHeader.LocationID
        and Set_Location.Isactive = 1 and Set_Location.Status = 1 and Set_Location.IsWareHouse is null
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyId And INV_StockTransactionDetail.PeriodID = @PeriodID 
    And INV_StockTransactionDetail.Status = 1 And (SET_DocumentType.TransactionImpact <> 'No Impact') AND IsNull(INV_StockTransactionHeader.StatusCode, 0) = 1 
{0}";

        const string query_GetRackItem = @"
select SET_Rack.RackID, SET_Rack.RackName, SET_Rack.LocationID from SET_Rack
    inner join SET_RackItems on SET_RackItems.RackID = SET_Rack.RackID 
where SET_Rack.Status = 1 and ItemID = @ItemID and LocationID = @LocationID ";

        const string Query_SumAllTransactionQuantityByLocation = @"

select sum(TransactionQuantity)SumOfQuantityInStock  from (
select  --abs(IsNull(SUM(((INV_StockTransactionDetail.TransactionQuantity *-1) - ISNULL(ReversedQuantity,0)) ),0)) As SumOfQuantityInStock 

INV_StockTransactionHeader .DocumentNo,SET_DocumentType.DocumentType 
	,case when INV_StockTransactionDetail.ImpactID is not null then 
			case INV_StockTransactionDetail.ImpactID  when 1 then abs(SUM(INV_StockTransactionDetail.TransactionQuantity)) when 2 then abs(SUM(INV_StockTransactionDetail.TransactionQuantity)) *-1 end
			else  
			case SET_DocumentType.TransactionImpact  
			when 'Positive' then abs(SUM(INV_StockTransactionDetail.TransactionQuantity)) 
			when 'Negative' then abs(SUM(INV_StockTransactionDetail.TransactionQuantity)) *-1 end
			 end as TransactionQuantity
,INV_StockTransactionHeader.StockTransactionID
from INV_StockTransactionDetail
Inner Join INV_StockTransactionHeader On (INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID)
	AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
	And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	And INV_StockTransactionHeader .Status = 1

			INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
  --AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
  AND INV_Item.Status = 1 

	INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID --  
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
	
	left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_Item.WeightUOMCode 
	INNER JOIN SET_UnitofMeasure UOM ON UOM.UOMID = INV_Item.PrimaryUOMCode

	LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
    
    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
	
	inner JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.Status = 1 --And SET_DocumentType.TransactionImpact <> 'No Impact' 
    AND ((SET_DocumentType.DocumentType = 'Stock Adjustment') or (SET_DocumentType.TransactionImpact <> 'No Impact') )   

where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID  
	And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 And INV_StockTransactionHeader.Status = 1
	{0}
AND INV_StockTransactionDetail.IsActive = 1 AND INV_StockTransactionHeader.StatusCode = 1  AND INV_StockTransactionHeader.ReversalID IS NULL

group by INV_StockTransactionHeader.DocumentNo,SET_DocumentType.DocumentType,INV_StockTransactionDetail.ImpactID,SET_DocumentType.TransactionImpact
,INV_StockTransactionHeader.StockTransactionID
)as tab
";

        const string Query_GetData_ImpactPositiveAndNotNull = @"
Select 
Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' 
		then 0
		else INV_StockTransactionDetail.TransactionQuantity END) As SumOfQuantityInStock
From INV_StockTransactionDetail
Inner Join INV_StockTransactionHeader On (INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID)
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
Inner Join SET_DocumentType On ( SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID)
And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
where INV_StockTransactionDetail.ItemID = @ItemID AND INV_StockTransactionDetail.SourceCode IS Not Null
And INV_StockTransactionDetail.CompanyID = @CompanyId And INV_StockTransactionDetail.PeriodID = @PeriodID
And INV_StockTransactionDetail.Status = 1 And SET_DocumentType.TransactionImpact = 'Positive'";

        const string Query_ALlTransacionQuantityRequistion = @"
Select Sum(INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity 
From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID 
    And INV_StockTransactionDetail.Status = 1 And SET_DocumentType.DocumentType = 'Requisition'";

        const string Query_ALlTransacionQuantityGIN = @"
Select Sum(INV_StockTransactionDetail.TransactionQuantity) as TransactionQuantity 
From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
    Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID 
    And INV_StockTransactionDetail.Status = 1 And SET_DocumentType.DocumentType = 'GIN'";


        const string Query_ALlTransacionQuantityInspection = @"Select 
Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' 
		then 0
		else INV_StockTransactionDetail.TransactionQuantity END)
From INV_StockTransactionDetail 
Inner Join INV_StockTransactionHeader On (INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID)
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
Inner Join SET_DocumentType On ( SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID)
And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID 
And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1
And SET_DocumentType.DocumentType = 'Inspection'";

        const string Query_CustomDutyByStockTransactionID = @"
           select {1}  from INV_StockTransactionDetail where StockTransactionID = @StockTransactionID and CompanyID =@CompanyID and PeriodID = @PeriodID {0}
        ";

        const string Query_ALlTransacionQuantityTransferOut = @"
Select Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' 
		then 0
		else INV_StockTransactionDetail.TransactionQuantity END)
From INV_StockTransactionDetail 
Inner Join INV_StockTransactionHeader On (INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID)
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
Inner Join SET_DocumentType On ( SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID)
And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID 
And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1
And SET_DocumentType.DocumentType = 'Transfer Out'";

        const string Query_ALlTransacionQuantityTransferIn = @"Select 
Sum(Case when Set_DocumentType.TransactionImpact = 'No Impact' 
		then 0
		else INV_StockTransactionDetail.TransactionQuantity END)
From INV_StockTransactionDetail 
Inner Join INV_StockTransactionHeader On (INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID)
AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
Inner Join SET_DocumentType On ( SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID)
And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID 
And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1
And SET_DocumentType.DocumentType = 'Transfer In'";

        const string Query_UpdateTransactionQuantityAfterIssuence = @"
Update INV_StockTransactionDetail 
Set INV_StockTransactionDetail.TransactionQuantity = (INV_StockTransactionDetail.TransactionQuantity - @TransactionQuantity)
from INV_StockTransactionDetail
    Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND SET_DocumentType.Status = 1 
Where INV_StockTransactionDetail.ItemID = @ItemID And INV_StockTransactionDetail.CompanyID = @CompanyId And INV_StockTransactionDetail.PeriodID = @PeriodID 
    And INV_StockTransactionDetail.Status = 1 AND SET_DocumentType.DocumentType like '%GIN%' AND INV_StockTransactionDetail.SourceHeaderID = @Source";

        const string Query_For_Dashboard = @"select TOP 6 StockTransactionID,TransactionQuantity from INV_StockTransactionDetail where companyID = 1 and PeriodID = 1";

        const string Query_GetStockData = @"select * from INV_StockTransactionDetail 
inner join INV_StockTransactionHeader on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = @CompanyID
and INV_StockTransactionDetail.PeriodID = @PeriodID
and ItemID = @ItemID
where @Qty <= INV_StockTransactionDetail.PrimaryQuantity
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.IsActive = 1
";

        const string Query_Get_MaxId_ByCompanyPeriod = @" SELECT IsNull(max(TransactionDetailID), 0) + 1 as TransactionDetailID FROM INV_StockTransactionDetail";

        const string Query_Delete_ByKey_CompanyPeriod = @" Update [INV_StockTransactionDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [TransactionDetailID] = @TransactionDetailID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

        const string Query_Update_By_PrimaryKey_AccountGINCode = @" UPDATE [INV_StockTransactionDetail] SET [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [AccountGINCode] = @AccountGINCode , [TransactionCost] = @TransactionCost
		WHERE [TransactionDetailID] = @TransactionDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID ";

        const string Query_Update_By_PrimaryKey_TransactionQuantity = @" UPDATE [INV_StockTransactionDetail] SET [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [AccountGINCode] = @AccountGINCode ,[TransactionCost] = @TransactionCost
		WHERE [TransactionDetailID] = @TransactionDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID ";

        const string Query_Update_By_PrimaryKey_TransactionalDetailCode = @" UPDATE [INV_StockTransactionDetail] SET [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue, [RejectedQuantity] = @RejectedQuantity, [TransactionQuantity] = @TransactionQuantity, IsInspected = @IsInspected
		WHERE [TransactionDetailID] = @TransactionDetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID ";

        const string Query_Select_ByTransactionHeaderID = @" 
SELECT * FROM INV_StockTransactionDetail
inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID
WHERE StockTransactionID = @StockTransactionID AND INV_StockTransactionDetail.CompanyID = @CompanyID AND PeriodID = @PeriodID AND INV_StockTransactionDetail.Status = @Status
order by SerialNo desc
";

        const string Query_Select_BySourceHeaderID = @" 
SELECT INV_StockTransactionDetail.*
FROM INV_StockTransactionHeader
INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
	AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
	AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
	AND INV_StockTransactionDetail.Status = 1
	AND INV_StockTransactionDetail.SourceHeaderID = @SourceHeaderID
WHERE INV_StockTransactionHeader.DocumentTypeID = @DocumentTypeID 
	AND INV_StockTransactionHeader.CompanyID = @CompanyID 
	AND INV_StockTransactionHeader.PeriodID = @PeriodID 
	AND INV_StockTransactionHeader.Status = 1 ";

        const string Query_Update_By_PrimaryKey_ForDOUpdate = @" UPDATE [INV_StockTransactionDetail] 
SET 
--[UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue,
 [TransactionQuantity] = @TransactionQuantity * -1, SourceCode = @SourceCode , SourceHeaderID = @SourceHeaderID,
ItemID = @ItemID , TransactionUOMID = @TransactionUOMID
		WHERE [TransactionDetailID] = @TransactionDetailID and StockTransactionID = @StockTransactionID
AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID ";

        const string query_GetLastpriceOfItem = @" 
Select top 1 UnitPrice as LastPrice from AP_PurchaseOrderDetail 
    Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID 
        And AP_PurchaseOrderHeader.Status = 1 
Where AP_PurchaseOrderDetail.CompanyID = @CompanyId and AP_PurchaseOrderDetail.PeriodID = @PeriodID and AP_PurchaseOrderDetail.Status = 1 
    and AP_PurchaseOrderDetail.ItemID = @ItemID
Order by AP_PurchaseOrderHeader.DocumentDate desc ";



          const string Query_GetFifoItemForGRN = @"
Select TransactionQuantity, IsNull(issuedQuantity, 0) issuedQuantity, TransactionCost, ItemID, CreateDate, IsIssued 
from INV_StockTransactionDetail 
where ItemID = @ItemID And CompanyID = @CompanyID And PeriodID = @PeriodID And TransactionQuantity >= 0 And IsIssued <> 1 
Order By CreateDate ";

        const string Query_GetAvailableStockForFIFO = @"
SELECT IsNull(Sum(TransactionQuantity - IsNull(IssuedQuantity, 0)), 0) As AvailableStock FROM Inv_StockTransactionDetail 
WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND ItemID = @ItemID AND Status = 1 AND IsActive = 1 AND TransactionQuantity > 0 
    AND (IsIssued IS NULL OR IsIssued = 0) ";

        const string Query_GetRequiredSheetQuantity = @"
select 
	INV_StockTransactionDetail.ItemID, 
	INV_Item.Description, 
	INV_StockTransactionDetail.TransactionQuantity,
    INV_StockTransactionDetail.TransactionDetailID 
from INV_StockTransactionDetail
inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID
	--and INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID
Where INV_StockTransactionDetail.StockTransactionID = @StockTransactionID
	and INV_StockTransactionDetail.CompanyID = @CompanyID
	and INV_StockTransactionDetail.PeriodID = @PeriodID
        ";

        //And (INV_StockTransactionHeader.IncurredOnID = @IncurredOnID Or @IncurredOnID is null)

        const string Query_GetUOM = @"
select * from inv_Item
	where ItemID = @ItemID 
	And inv_Item.CompanyID = @CompanyId 
	--And inv_Item.PeriodID = @PeriodID 
	and Status = 1
{0}";






        #endregion

        #region Class Methods

        public static double GetPRN_Costing(int CompanyID, int PeriodID, string MenuPath, int ChartofAccountID, string CodeCombination, string TransactionType, string ToDate, SqlConnection objConnection = null)
        {
            double qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];

                MenuPath = "/Inventory/Transaction/FinishedGoods.aspx";
                ChartofAccountID = 1;
                CodeCombination = "08-01%";
                TransactionType = "Debit";

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@MenuPath", MenuPath);
                sqlparams[3] = new SqlParameter("@ChartofAccountID", ChartofAccountID);
                sqlparams[4] = new SqlParameter("@CodeCombination", CodeCombination);
                sqlparams[5] = new SqlParameter("@TransactionType", TransactionType);
                sqlparams[6] = new SqlParameter("@ToDate", ToDate);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PRN_Costing, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PRN_Costing, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = double.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static void setFIFOTransactionDetailIDAlImdad(int ItemID, int LocationID, int CurrentCompanyID, int CurrentPeriodID)
        {
            try
            {
                int _locationId = LocationID;
                {
                    DataTable dt_stocksetting = Get_FIFOStockSettingByItemID(ItemID, _locationId, CurrentCompanyID, CurrentPeriodID);
                    DataTable dt_Neg = new DataTable();

                    DataView dv = new DataView(dt_stocksetting);
                    dv.Sort = "DocumentDate Asc";

                    DataTable dt_Pos = dt_stocksetting.Select("issuance=0").CopyToDataTable();

                    if (dt_stocksetting.Select("qty=0").Length > 0)
                        dt_Neg = dt_stocksetting.Select("qty=0").CopyToDataTable();

                    if (!dt_Pos.Columns.Contains("IsIssued"))
                        dt_Pos.Columns.Add("IsIssued");

                    if (!dt_Pos.Columns.Contains("IssuedQuantity"))
                        dt_Pos.Columns.Add("IssuedQuantity");

                    //if (!dt_Pos.Columns.Contains("FIFOTransactionDetailID"))
                    //    dt_Pos.Columns.Add("FIFOTransactionDetailID");

                    if (!dt_Neg.Columns.Contains("ParentFIFOID"))
                        dt_Neg.Columns.Add("ParentFIFOID");

                    if (!dt_Neg.Columns.Contains("ParentFIFOTransactionDetailID"))
                        dt_Neg.Columns.Add("ParentFIFOTransactionDetailID");

                    decimal computeTotalIssuance = 0;
                    if (dt_Neg.Rows.Count > 0)
                        computeTotalIssuance = Math.Abs(Convert.ToDecimal(dt_Neg.Compute("Sum(issuance)", "")));

                    decimal TotalQtyDeduct = 0;
                    for (int i = 0; i < dt_Pos.Rows.Count; i++)
                    {
                        if (computeTotalIssuance > 0)
                        {
                            if (Math.Abs(computeTotalIssuance) > Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IsIssued"] = "1";
                                dt_Pos.Rows[i]["IssuedQuantity"] = dt_Pos.Rows[i]["Qty"];
                                for (int negRow = 0; negRow < dt_Neg.Rows.Count; negRow++)
                                {
                                    TotalQtyDeduct = TotalQtyDeduct + Math.Abs(Convert.ToDecimal(dt_Neg.Rows[negRow]["issuance"]));
                                    if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) > TotalQtyDeduct)
                                    {
                                        if (dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"].ToString() == "")
                                        {
                                            dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                        }
                                    }
                                    else if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) < TotalQtyDeduct)
                                    {
                                        // TotalQtyDeduct = 0;
                                        // break;
                                    }
                                }
                                computeTotalIssuance = computeTotalIssuance - Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]);
                            }
                            else if (Math.Abs(computeTotalIssuance) == Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IsIssued"] = "1";
                                dt_Pos.Rows[i]["IssuedQuantity"] = dt_Pos.Rows[i]["Qty"];

                                for (int negRow = 0; negRow < dt_Neg.Rows.Count; negRow++)
                                {
                                    TotalQtyDeduct = TotalQtyDeduct + Math.Abs(Convert.ToDecimal(dt_Neg.Rows[negRow]["issuance"]));
                                    if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) > TotalQtyDeduct)
                                    {
                                        if (dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"].ToString() == "")
                                        {
                                            dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                        }
                                    }
                                    else if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) <= TotalQtyDeduct)
                                    {
                                        if (dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"].ToString() == "")
                                        {
                                            dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                        }
                                        // TotalQtyDeduct = 0;
                                        // break;
                                    }
                                }

                                computeTotalIssuance = computeTotalIssuance - Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]);
                            }
                            else if (Math.Abs(computeTotalIssuance) < Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IssuedQuantity"] = computeTotalIssuance;
                                //dt_Pos.Rows[i]["FIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                //for (int k = 0; k < dt_Neg.Rows.Count; k++)
                                //{
                                //    dt_Neg.Rows[k]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                //}
                                for (int negRow = 0; negRow < dt_Neg.Rows.Count; negRow++)
                                {
                                    TotalQtyDeduct = TotalQtyDeduct + Math.Abs(Convert.ToDecimal(dt_Neg.Rows[negRow]["issuance"]));
                                    if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) > TotalQtyDeduct)
                                    {
                                        if (dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"].ToString() == "")
                                        {
                                            dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                        }
                                    }
                                    else if (Convert.ToDecimal(dt_Pos.Rows[i]["Qty"].ToString()) <= TotalQtyDeduct)
                                    {
                                        if (dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"].ToString() == "")
                                        {
                                            dt_Neg.Rows[negRow]["ParentFIFOTransactionDetailID"] = dt_Pos.Rows[i]["transactiondetailid"];
                                        }
                                        // TotalQtyDeduct = 0;
                                        //  break;
                                    }
                                }
                                computeTotalIssuance = 0;
                            }
                        }
                    }

                    if (dt_Pos != null && dt_Pos.Rows.Count > 0)
                    {
                        DataTable dt_PosUpdate = dt_Pos.Select().CopyToDataTable();

                        foreach (DataRow item in dt_PosUpdate.Rows)
                        {
                            INV_StockTransactionHeader.UpdateFIFOByTransactionIDAlImdad(int.Parse(item["TransactionDetailID"].ToString()), item["IssuedQuantity"].ToString() != "" ? Convert.ToDecimal(item["IssuedQuantity"].ToString()) : (decimal?)null, CurrentCompanyID, CurrentPeriodID, item["IsIssued"].ToString() == "1" ? true : (bool?)null);
                        }
                    }
                    if (dt_Neg != null && dt_Neg.Rows.Count > 0)
                    {
                        DataTable dt_NegUpdate = dt_Neg.Select().CopyToDataTable();

                        foreach (DataRow item in dt_NegUpdate.Rows)
                        {
                            INV_StockTransactionHeader.UpdateFIFOByTransactionIDAlImdadParentID(int.Parse(item["TransactionDetailID"].ToString()), int.Parse(item["ParentFIFOTransactionDetailID"].ToString()), CurrentCompanyID, CurrentPeriodID);
                        }
                    }
                }
            }
            catch { }
        }

        public static decimal Get_SumAllTransactionQuantityWithStock(int itemID, int companyID, int periodID, SqlConnection objConnection = null, int LocationID = 0, DateTime? Date = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;
            string Filters = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (LocationID != 0 && LocationID != -1)
                    Filters = " And INV_StockTransactionDetail.LocationID = " + LocationID;

                if (Date != null)
                    Filters = Filters + " and INV_StockTransactionHeader.DocumentDate <= '" + DateTime.Parse(Date.ToString()).ToString("yyyy-MM-dd") + "'";


                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SumAllTransactionQuantity, Filters), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SumAllTransactionQuantity, Filters), sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    qtyAllTransaction = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static int Get_UOM(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            int uom = 0;
            DataTable dt = null;
            string Filters = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);



                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetUOM, Filters), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_GetUOM, Filters), sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    uom = int.Parse(dt.Rows[0]["PrimaryUnitOfMeasure"].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    uom = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return uom;
        }
        public static DataTable GETData_From_StockTransferOut_Detail_WithStock(int StockTransactionID, int companyID, int periodID, bool status = true, bool ALImdad = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                string Query = "";
                if (ALImdad)
                    Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataForTransferIn_WithStock;
                else
                    Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Qyery_SelectData_From_StockTransferOutDetail;


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

        
        
        
        public static bool UpdateIssuedQuantity(int companyID, int periodID, int StockTransactionID, int TransactionDetailID, decimal IssedQuantity)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@STID", StockTransactionID);
                sqlparams[3] = new SqlParameter("@TDID", TransactionDetailID);
                sqlparams[4] = new SqlParameter("@IQ", IssedQuantity);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIssuedQuantity, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool UpdateIsIssued(int companyID, int periodID, int StockTransactionID, int TransactionDetailID, bool IssuedStatus)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@STID", StockTransactionID);
                sqlparams[3] = new SqlParameter("@TDID", TransactionDetailID);
                sqlparams[4] = new SqlParameter("@IssuedStatus", IssuedStatus);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsIssued, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable Get_AllDataAgainstItemFIFO(int companyID, int periodID, int itemID, string filter, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);

                string query = Query_GetAllDataAgainstItemFIFO.Replace("{filter}", filter);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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


        public static DataTable Get_AllDataAgainstItemFIFOByLocation(int companyID, int periodID, int itemID, int _locationid, string filter, SqlConnection objConnection = null , DateTime? date = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                sqlparams[3] = new SqlParameter("@LocationID", _locationid);

                string query = Query_GetAllDataAgainstItemFIFOByLocation.Replace("{filter}", filter);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable Get_AllForStockSetting(int companyID, int periodID, int itemID, int _locationid, string filter, SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                sqlparams[3] = new SqlParameter("@LocationID", _locationid);

                string query = Query_GetAllForFIFOSetting.Replace("{filter}", filter);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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


        public static DataTable GetDistinctItemID(SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {

                string query = "select distinct itemid from inv_stocktransactiondetail where status=1";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query);

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

        public static DataTable GetDistinctItemID2(SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {

                string query = @"
select distinct itemid,inv_stocktransactionheader.CompanyID,inv_stocktransactionheader.PeriodID from inv_stocktransactiondetail,inv_stocktransactionheader
where inv_stocktransactionheader.status=1 and inv_stocktransactiondetail.stocktransactionid=inv_stocktransactionheader.stocktransactionid
--and DocumentDate between '2017-01-01' and '2017-01-31'
";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query);

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

        public static DataTable GetDistinctLocationID(SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {

                string query = @"select distinct LocationID from inv_stocktransactionheader where status=1 and locationid is not null
--and DocumentDate between '2017-01-01' and '2017-01-31'
";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query);

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

        public static DataTable GetStockReconcileLedger(int ItemID, int LocationID, int CompanyID, int PeriodID, DateTime? Date, SqlConnection objConnection = null)
        {
            DataTable dt = null;
            string Fillter = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemID", ItemID);
                sqlparams[1] = new SqlParameter("@LocationID", LocationID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);

                if (Date.HasValue)
                {
                    Fillter = "and convert(date,INV_StockTransactionHeader.DocumentDate ) <=  '" + Date.Value.ToString("yyyy-MM-dd") + "' ";
                }

                string query = @"

SELECT  ROW_NUMBER() OVER(ORDER BY t.ItemID ,
        t.LocationName ,
        t.DocumentDate ASC ,
        TransactionQuantity DESC) ,*
FROM    ( SELECT   INV_StockTransactionHeader.DocumentDate ,
                    INV_StockTransactionDetail.ItemID ,
                    dbo.INV_Item.Description ,
                    isnull(sl.locationid,SET_Location.LocationID)LocationID,
                    isnull(SL.LocationName,dbo.SET_Location.LocationName)LocationName ,
                    INV_StockTransactionDetail.TransactionDetailID ,
                    INV_StockTransactionHeader.DocumentTypeID ,
                    dbo.SET_DocumentType.DocumentType ,
                    INV_StockTransactionHeader.DocumentNo ,
                    (case when INV_StockTransactionDetail.ImpactID = 1 then 'Positive' when INV_StockTransactionDetail.ImpactID = 2 then 'Negative' else set_documenttype.transactionimpact end) as transactionimpact,
                    --set_documenttype.transactionimpact,
                    --INV_StockTransactionDetail.TransactionQuantity ,
					isnull(case when INV_StockTransactionDetail.ImpactID is not null then 
			case INV_StockTransactionDetail.ImpactID  when 1 then abs((INV_StockTransactionDetail.TransactionQuantity)) when 2 then abs((INV_StockTransactionDetail.TransactionQuantity)) *-1 end
			else  
			case SET_DocumentType.TransactionImpact  
			when 'Positive' then abs((INV_StockTransactionDetail.TransactionQuantity)) 
			when 'Negative' then abs((INV_StockTransactionDetail.TransactionQuantity)) *-1 end
			 end,0) as TransactionQuantity, 

                     --   isnull(( SELECT    *
                 --     FROM      ufn_StockRunningBalanceQty(dbo.INV_StockTransactionDetail.ItemID,
                 --                                          2,
                 --                                          dbo.INV_StockTransactionHeader.DocumentDate
														   --,dbo.INV_StockTransactionHeader.CompanyID
														   --,dbo.INV_StockTransactionHeader.PeriodID)
                 --   ),0) RunningQuantity ,
				 convert(decimal,0) as RunningQuantity ,

							--Isnull(( SELECT    *
     --                 FROM      ufn_GetLastStockCost(dbo.INV_StockTransactionDetail.ItemID,
     --                                               4,
     --                                                dbo.INV_StockTransactionHeader.DocumentDate
					--								 ,dbo.INV_StockTransactionHeader.CompanyID
					--									   ,dbo.INV_StockTransactionHeader.PeriodID)
     --               ),0) ConstantPositiveCost,

	((   case when INV_StockTransactionDetail.valuechange is not null or   INV_StockTransactionDetail.valuechange <> 0 then   
          
  ((INV_StockTransactionDetail.transactionquantity * INV_StockTransactionDetail.TransactionCost) + INV_StockTransactionDetail.valuechange) / (case when INV_StockTransactionDetail.transactionquantity > 0 then INV_StockTransactionDetail.transactionquantity else 1 end)  
    
  else INV_StockTransactionDetail.transactioncost end  
    
  ))   as ConstantPositiveCost,

					
					-- isnull(( SELECT    *
     --                 FROM      ufn_StockRunningBalanceQtyPrevious(dbo.INV_StockTransactionDetail.ItemID,
     --                                                      2,
     --                                                      dbo.INV_StockTransactionHeader.DocumentDate
					--									   ,dbo.INV_StockTransactionHeader.CompanyID
					--									   ,dbo.INV_StockTransactionHeader.PeriodID)
     --               ),0)
					--PreviousQty,
					 convert(decimal,0) as PreviousQty,
					--Isnull((SELECT    *
     --                 FROM      ufn_GetLastStockCostPrevious(dbo.INV_StockTransactionDetail.ItemID,
     --                                                2,
     --                                                dbo.INV_StockTransactionHeader.DocumentDate
					--								 ,dbo.INV_StockTransactionHeader.CompanyID
					--									   ,dbo.INV_StockTransactionHeader.PeriodID)) 
													 
					--,0)PreviousCost,
					0 as PreviousCost,
                    isnull(INV_StockTransactionDetail.ValueChange,0)ValueChange
                    ,INV_StockTransactionHeader.CompanyID
                    ,INV_StockTransactionHeader.PeriodID


          FROM      INV_StockTransactionDetail
                    INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
                                                             AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
                                                             AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
                                                             AND INV_StockTransactionHeader.Status = 1
                                                             AND INV_StockTransactionHeader.StatusCode = 1
                                                             AND INV_StockTransactionHeader.IsActive = 1
                                                             AND INV_StockTransactionDetail.Status = 1
                    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
                                                   AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID
                                                   AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
                                                   AND ( SET_DocumentType.TransactionImpact <> 'No Impact' )
                                                  -- AND SET_DocumentType.DocumentType LIKE '%dispatch%'
                                                  -- AND SET_DocumentType.DocumentType NOT LIKE '%dispatch%retu%'
                    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID
                                           --AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID
                                           AND INV_Item.Status = 1 
							   --AND INV_Item.itemid = 2171
                    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID
                                                   -- 
                                                   AND ( INV_SET_Category.IsInvisibleUponStockReports IS NULL
                                                         OR INV_SET_Category.IsInvisibleUponStockReports = 0
                                                       )
                    --INNER JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID   
                    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
					LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
         -- WHERE     INV_StockTransactionHeader.DocumentDate <= '2017-01-31'
            where INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID=@PeriodID
            and INV_StockTransactionDetail.ItemID  = @ItemID
            {0}


        ) t
WHERE   t.ItemID  = @ItemID
   AND t.LocationID = @LocationID -----okkkkk
and t.CompanyID = @CompanyID
and t.PeriodID = @PeriodID
    
ORDER BY t.ItemID ,
        t.LocationName ,
        t.DocumentDate ASC,
t.TransactionDetailID
--,
        --TransactionQuantity DESC

";

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, Fillter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, Fillter), sqlparams);

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



        public static DataTable Get_QTYFIFOByLocation(int companyID, int periodID, int itemID, int _locationid, string filter, SqlConnection objConnection = null, DateTime? date = null,SqlTransaction objTransaction = null )
        {
            DataTable dt = null;
            string Fillters = string.Empty;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                sqlparams[3] = new SqlParameter("@LocationID", _locationid);

                string query = Query_Get_QTYFIFOByLocation.Replace("{filter}", "");

                if (date != null)
                    //Fillters = "AND convert(date,INV_StockTransactionHeader.DocumentDate) < convert(date," + date.ToString("YYYY-mm-DD") + ")";
                    Fillters = "AND convert(date,INV_StockTransactionHeader.DocumentDate) <= convert(date,'" + (Convert.ToDateTime(date.ToString())).ToString("yyyy-MM-dd") + "')";
                else
                    Fillters = "AND convert(date,INV_StockTransactionHeader.DocumentDate) <= convert(date,getdate())";

                DataSet ds = null;

                if (objTransaction == null)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, Fillters), sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(query, Fillters), sqlparams);
                }
                else
                {
                    //if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, query, sqlparams) > 0) ;

                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, Fillters), sqlparams);
                }

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



        public static DataTable Get_AllDataAgainstMultipleItemsFIFO(int companyid, int periodid, string itemid, string filter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@ItemID", itemid);

                string query = Query_GetAllDataAgainstMultipleItemsFIFO.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_AllDataAgainstMultipleItemsFIFON(int companyid, int periodid, string itemid, int _locationid, string filter)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@ItemID", itemid);
                sqlparams[3] = new SqlParameter("@LocationID", _locationid);

                string query = Query_GetAllDataAgainstMultipleItemsFIFON.Replace("{filter}", filter);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool DeleteDataByHeaderKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, long StockTransactionID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderKey, sqlparams) > 0)
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

        public static bool DeleteDetailDataByHeaderKey(SqlTransaction objTransaction, long StockTransactionID, int DetailID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@DetailID", DetailID);

                const string Query_Delete = @" Update [INV_StockTransactionDetail] Set [Status] = @Status WHERE [StockTransactionID] = @StockTransactionID and TransactionDetailID =  @DetailID";
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete, sqlparams) > 0)
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


        public static bool DeleteDataByHeaderKeyFIFO(SqlTransaction objTransaction, long StockTransactionID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderKeyFIFO, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderKeyFIFO, sqlparams) > 0)
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



        public static DataTable Get_Query_For_Dashboard(Nullable<bool> isActive = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];

                if (isActive.HasValue)
                    sqlparams[0] = new SqlParameter("@IsActive", isActive.Value);
                else
                    sqlparams[0] = new SqlParameter("@IsActive", DBNull.Value);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_For_Dashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_ItemsFor_RFQ(int companyId, int periodId, int stockTransactionID, string whereFilter)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetItemsFor_RFQ, whereFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable Get_ItemsFor_RFQSSML(int companyId, int periodId, int stockTransactionID, string whereFilter)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetItemsFor_RFQSSM, whereFilter), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static bool UpdateTransactionQuantityAfterIssuence(int companyid, int periodid, int transactionquantity, int itemid, int source)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionQuantity", transactionquantity);
                sqlparams[1] = new SqlParameter("@CompanyId", companyid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodid);
                sqlparams[3] = new SqlParameter("@ItemID", itemid);
                sqlparams[4] = new SqlParameter("@Source", source);
                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateTransactionQuantityAfterIssuence, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static decimal GetALlTransacionQuantityInspection(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ALlTransacionQuantityInspection, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ALlTransacionQuantityInspection, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }


        public static DataTable Get_CustomDutyByStockTransactionID(int StockTransactionID, int companyID, int periodID, int? CustomDutyID, int? clearingChargesID, int? otherChargesID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sb1 = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);


                #region Sum Of Values

                if (CustomDutyID.HasValue)
                    sb1.Append(" sum(customduty)customduty ");

                if (CustomDutyID.HasValue)
                    sb.Append(" and customdutyid = " + CustomDutyID.Value);


                if (clearingChargesID.HasValue)
                    sb1.Append(" sum(clearingCharges)clearingCharges ");

                if (clearingChargesID.HasValue)
                    sb.Append(" and clearingChargesID = " + clearingChargesID.Value);


                if (otherChargesID.HasValue)
                    sb1.Append(" sum(otherCharges)otherCharges ");

                if (otherChargesID.HasValue)
                    sb.Append(" and otherChargesID = " + otherChargesID.Value);

                if (!CustomDutyID.HasValue && !clearingChargesID.HasValue && !otherChargesID.HasValue)
                    sb1.Append(" * ");

                #endregion


                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_CustomDutyByStockTransactionID, sb.ToString(), sb1.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, String.Format(Query_CustomDutyByStockTransactionID, sb.ToString(), sb1.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static decimal GetALlTransacionQuantityTransferOut(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ALlTransacionQuantityTransferOut, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ALlTransacionQuantityTransferOut, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal GetALlTransacionQuantityTransferIn(int itemid, int companyid, int periodid)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemid);
                sqlparams[1] = new SqlParameter("@CompanyId", companyid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodid);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ALlTransacionQuantityTransferIn, sqlparams).Tables[0];
                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal GetALlTransacionQuantityRequistion(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ALlTransacionQuantityRequistion, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ALlTransacionQuantityRequistion, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal GetALlTransacionQuantityGIN(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_ALlTransacionQuantityGIN, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_ALlTransacionQuantityGIN, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static DataTable GetRequiredSheetQuantity(int stockTransactionID, int companyid, int periodid, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodid);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetRequiredSheetQuantity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetRequiredSheetQuantity, sqlparams);

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



        public static DataTable GetRackNames(int itemID, int locationID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@LocationID", locationID);



                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query_GetRackItem, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
        //Query_MaterialConsumptionReport
        //Query_ClosingStock
        public static DataTable GetConsumptionSummaryReportData(int company, Nullable<int> period, Nullable<int> itemfrom, Nullable<int> itemto
        , Nullable<int> departmentfrom, Nullable<int> departmentto, Nullable<int> location, Nullable<int> categoryfrom, Nullable<int> categoryto,
            short? documentGroupID = null,Nullable<int> costCenterIDFrom = null,Nullable<int> costCenterIDTo = null,  int? ApprovingGroupID = null, 
            Nullable<int> CatalogID = null,int ReportType = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                //if (datefrom.Value.ToString() != string.Empty && dateto.Value.ToString() != string.Empty)
                //{
                //    sb.Append("and INV_StockTransactionHeader.DocumentDate between '" + datefrom.Value.ToString("yyyy-MM-dd") + "' and '" + dateto.Value.ToString("yyyy-MM-dd") + "'");
                //}
                //else
                //{
                //    sb.Append("");
                //}

                if (period != null)
                {

                    sb.Append(" and INV_StockTransactionHeader.PeriodID = " + period + "");
                }
                if (itemfrom != null && itemto != null)
                {
                    sb.Append("and INV_Item.ItemID between " + itemfrom + " and " + itemto + "");
                }
                else
                {
                    sb.Append("");
                }
                if (departmentfrom != null && departmentto != null)
                {

                    sb.Append("and Set_Department.DepartmentID between " + departmentfrom + " and " + departmentto + "");
                }
                else
                {
                    sb.Append("");
                }
                if (location != null)
                {
                    sb.Append(" and [INV_StockTransactionDetail].LocationID = " + location + " ");
                }
                else
                {

                    sb.Append("");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (costCenterIDFrom != null && costCenterIDTo != null)
                {

                    sb.Append("and Set_CostCenter.CostCenterID between " + costCenterIDFrom + " and " + costCenterIDTo + "");
                }
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    sb.Append(" AND INV_Item.ApprovalItemGroupID  = " + ApprovingGroupID);


                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                if (categoryfrom.HasValue && categoryto.HasValue)
                    sb.Append(" And INV_Item.ItemTypeID between " + categoryfrom + " and " + categoryto + " ");
                else if (categoryfrom.HasValue)
                    sb.Append(" and INV_Item.ItemTypeID = " + categoryfrom);
                else if (categoryto.HasValue)
                    sb.Append(" and INV_Item.ItemTypeID = " + categoryto);


                if (ReportType == 1)
                    dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_COnsumptionWithQuantity, sb), sqlparams).Tables[0];
                else
                    dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ConsumptionSummary, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetConsumptionSummaryReportDatas(int company, Nullable<int> period, Nullable<int> itemfrom, Nullable<int> itemto
      , Nullable<int> departmentfrom, Nullable<int> departmentto, DateTime? fromDate, DateTime? toDate, Nullable<int> location, Nullable<int> categoryfrom, Nullable<int> categoryto,
          short? documentGroupID = null, Nullable<int> costCenterIDFrom = null, Nullable<int> costCenterIDTo = null, int? ApprovingGroupID = null, Nullable<int> DemandType = null, Nullable<int> SeasonType = null,
          Nullable<int> CatalogID = null, int ReportType = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                //if (datefrom.Value.ToString() != string.Empty && dateto.Value.ToString() != string.Empty)
                //{
                //    sb.Append("and INV_StockTransactionHeader.DocumentDate between '" + datefrom.Value.ToString("yyyy-MM-dd") + "' and '" + dateto.Value.ToString("yyyy-MM-dd") + "'");
                //}
                //else
                //{
                //    sb.Append("");
                //}

                if (period != null)
                {

                    sb.Append(" and INV_StockTransactionHeader.PeriodID = " + period + "");
                }
                if (itemfrom != null && itemto != null)
                {
                    sb.Append("and INV_Item.ItemID between " + itemfrom + " and " + itemto + "");
                }
                else
                {
                    sb.Append("");
                }
                if (departmentfrom != null && departmentto != null)
                {

                    sb.Append("and Set_Department.DepartmentID between " + departmentfrom + " and " + departmentto + "");
                }
                else
                {
                    sb.Append("");
                }
                if (location != null)
                {
                    sb.Append(" and [INV_StockTransactionDetail].LocationID = " + location + " ");
                }
                else
                {

                    sb.Append("");
                }

                if (fromDate.HasValue && toDate.HasValue)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");
                }
                else if (fromDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' ");
                else if (toDate.HasValue)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59' ");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (costCenterIDFrom != null && costCenterIDTo != null)
                {

                    sb.Append("and Set_CostCenter.CostCenterID between " + costCenterIDFrom + " and " + costCenterIDTo + "");
                }
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    sb.Append(" AND INV_Item.ApprovalItemGroupID  = " + ApprovingGroupID);


                if (DemandType > 0)
                    sb.Append(" AND INV_StockTransactionHeader.DemandType = " + DemandType);

                if (SeasonType > 0)
                    sb.Append("AND INV_StockTransactionHeader.SeasonType = " + SeasonType);

                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                if (categoryfrom.HasValue && categoryto.HasValue)
                    sb.Append(" And INV_Item.ItemTypeID between " + categoryfrom + " and " + categoryto + " ");
                else if (categoryfrom.HasValue)
                    sb.Append(" and INV_Item.ItemTypeID = " + categoryfrom);
                else if (categoryto.HasValue)
                    sb.Append(" and INV_Item.ItemTypeID = " + categoryto);


                if (ReportType == 1)
                    dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_COnsumptionWithQuantity, sb), sqlparams).Tables[0];
                else
                    dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ConsumptionSummary, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetClosingStock(int company, int period, Nullable<DateTime> datefrom, Nullable<DateTime> dateto, Nullable<int> itemfrom, Nullable<int> itemto,
            Nullable<int> location)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@Fromdate", datefrom);
                sqlparams[3] = new SqlParameter("@Todate", dateto);

                if (itemfrom != null && itemto != null)
                    sb.Append(" and INV_Item.ItemID between " + itemfrom + " and " + itemto);

                if (location != null)
                    sb.Append(" and INV_StockTransactionHeader.LocationID = " + location);



                //if (documentGroupID.HasValue)
                //    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ClosingStockLedger, sb), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetMaterialConsumptionReportData(int company, int period, Nullable<DateTime> datefrom, Nullable<DateTime> dateto,
            Nullable<int> itemfrom, Nullable<int> itemto, Nullable<int> departmentfrom, Nullable<int> departmentto, Nullable<int> costCenterIDFrom = null,
            Nullable<int> costCenterIDTo = null, short? documentGroupID = null, int? ApprovingGroupID = null, Nullable<int> CategoryID = null, Nullable<int> CatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);

                if (datefrom.Value.ToString() != string.Empty && dateto.Value.ToString() != string.Empty)
                {
                    sb.Append("and INV_StockTransactionHeader.DocumentDate between '" + datefrom.Value.ToString("yyyy-MM-dd") + "' and '" + dateto.Value.ToString("yyyy-MM-dd") + "'");
                }

                if (itemfrom != null && itemto != null)
                {
                    sb.Append("and INV_Item.ItemID between " + itemfrom + " and " + itemto + "");
                }

                if (departmentfrom != null && departmentto != null)
                {

                    sb.Append("and Set_Department.DepartmentID between " + departmentfrom + " and " + departmentto + "");
                }

                if (costCenterIDFrom != null && costCenterIDTo != null)
                {

                    sb.Append("and Set_CostCenter.CostCenterID between " + costCenterIDFrom + " and " + costCenterIDTo + "");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    sb.Append(" AND INV_Item.ApprovalItemGroupID  = " + ApprovingGroupID);

                if (CategoryID.HasValue)
                    sb.Append("And INV_Item.ItemTypeID = " + CategoryID.ToString());

                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID = " + CatalogID.ToString());

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_MaterialConsumptionReport, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable PendingGRNforSuppliersBillsData(int company, int period, Nullable<DateTime> datefrom
      , Nullable<DateTime> dateto, Nullable<int> supplierfrom, Nullable<int> supplierto
      , Nullable<int> docNofrom, Nullable<int> docNoto, short? documentGroupID = null, int? DocumenttypeID = null, int CurrentFiscalID = 0, int? PODocumenttypeID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (supplierfrom != null && supplierto != null)
                {
                    sb.Append("  and  AR_SET_Customer.CustomerID  between " + supplierfrom + " and " + supplierto + " ");
                }
                else
                {
                    sb.Append("");
                }


                if (docNofrom != null && docNoto != null)
                {
                    sb.Append(" and  INV_StockTransactionHeader.DocumentNo  between " + docNofrom + "  and " + docNoto + "  ");
                }
                else
                {
                    sb.Append("");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (DocumenttypeID.HasValue)
                    sb.Append(" AND Set_DocumentType.DocumenttypeID = " + DocumenttypeID.Value.ToString() + " ");

                if (PODocumenttypeID.HasValue)
                    sb.Append(" AND Set_DocumentType2.DocumenttypeID = " + PODocumenttypeID.Value.ToString() + " ");


                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PendingGRNforSuppliersBills, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetStockMovementSummaryData(int company, int period, Nullable<DateTime> datefrom
          , Nullable<DateTime> dateto, Nullable<int> itemfrom, Nullable<int> itemto
          , Nullable<int> location, short? documentGroupID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", company);
                sqlparams[1] = new SqlParameter("@PeriodID", period);
                sqlparams[2] = new SqlParameter("@FromDate", datefrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateto);


                if (itemfrom != null && itemto != null)
                {
                    sb.Append("and INV_Item.ItemID between " + itemfrom + " and " + itemto + "");
                }
                else
                {
                    sb.Append("");
                }

                if (location != null)
                {
                    sb.Append(" and [INV_StockTransactionDetail].LocationID = " + location + "");
                }
                else
                {
                    sb.Append("");

                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.QueryStockMovementSummary, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }


        public static DataTable GetRackByItemAndLocation(int itemID, int locationID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@LocationID", locationID);



                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query_GetRackItem, sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static decimal GetData_ImpactPositiveAndNotNull(int itemID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ImpactPositiveAndNotNull, sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ImpactPositiveAndNotNull, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal Get_SumAllTransactionQuantity(int itemID, int companyID, int periodID, SqlConnection objConnection = null, int LocationID = 0,DateTime? Date = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;
            string Filters = "";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (LocationID != 0 && LocationID != -1)
                    Filters = " And INV_StockTransactionHeader.LocationID = " + LocationID;

                if (Date != null)
                    Filters = " and INV_StockTransactionHeader.DocumentDate <= '" + DateTime.Parse(Date.ToString()).ToString("yyyy-MM-dd") + "'";


                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SumAllTransactionQuantity, Filters), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query_SumAllTransactionQuantity, Filters), sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    qtyAllTransaction = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal Get_SumAllTransactionQuantityByLocation(int itemID, int companyID, int periodID, int locationID,int RackID = 0, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@LocationID", locationID);

                if (locationID != -1) //  For Pak Ethanol DB, Detail is replaced by Header table
                    sb.Append(" AND (INV_StockTransactionHeader.LocationID = @LocationID Or INV_StockTransactionDetail.LocationID = @LocationID) ");
                if (RackID > 0)
                    sb.Append("  and INV_StockTransactionDetail.RackID =   " + RackID);

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SumAllTransactionQuantityByLocation, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SumAllTransactionQuantityByLocation, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    qtyAllTransaction = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static decimal Get_SumAllTransactionQuantityByLocation_InOrOut(int itemID, int companyID, int periodID, int locationID, string Query_SumAllTransactionQuantityByLocation_InOrOut, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@LocationID", locationID);
                if (locationID != -1)
                    sb.Append(" and INV_StockTransactionDetail.LocationID = @LocationID ");
                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SumAllTransactionQuantityByLocation_InOrOut, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SumAllTransactionQuantityByLocation_InOrOut, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    qtyAllTransaction = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static DataTable GetDataByKeyWithOtherTables(int dispatchID, int companyID, int periodID, SqlConnection objConnection = null,int CurrentFiscalID=0,bool AlImdad = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                DataSet ds = null;

                if (AlImdad == false)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_dispatchID_CompanyId_PeriodId, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_dispatchID_CompanyId_PeriodId, sqlparams);
                }

                else if (AlImdad == true)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataForDispatchEdit, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataForDispatchEdit, sqlparams);
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




        public static DataTable GetDataforVehicle(int companyID, int periodID, int SourceCode, int SourceHeaderID, int SourceLineID, int SourceDocumentTypeID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceCode", SourceCode);
                sqlparams[3] = new SqlParameter("@SourceHeaderID", SourceHeaderID);
                sqlparams[4] = new SqlParameter("@SourceLineID", SourceLineID);
                sqlparams[5] = new SqlParameter("@SourceDocumentTypeID", SourceDocumentTypeID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_for_vehicle, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_for_vehicle, sqlparams);

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





        public static DataTable GetData_FromPO_Detail(int pOHeaderID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_POHeaderID_CompanyId, sqlparams);

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


        public static DataTable GetRackname(int pOHeaderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_GetRackItem, sqlparams);

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



        public static DataTable GETData_From_StockRequisition_Detail(int documentNo, int companyID, int periodID, string Where, bool status = true,
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (Where != "")
                {
                    Filter = Where;
                }
                if (documentNo > 0)
                {
                    Filter += "AND INV_StockTransactionHeader.DocumentNo = " + documentNo;
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetail, Filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetail, Filter), sqlparams);

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

        public static DataTable GETData_From_StockRequisition_DetailFor_SKH(int documentNo, int companyID, int periodID, string Where, bool status = true,
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (Where != "")
                {
                    Filter = Where;
                }
                if (documentNo > 0)
                {
                    Filter += "AND INV_StockTransactionHeader.DocumentNo = " + documentNo;
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailFor_SKH, Filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailFor_SKH, Filter), sqlparams);

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

        public static DataTable GETData_From_StockRequisition_DetailFIFO(int documentNo, int companyID, int periodID, string Where, bool status = true,
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (Where != "")
                {
                    Filter = Where;
                }
                if (documentNo > 0)
                {
                    Filter += "AND INV_StockTransactionHeader.DocumentNo = " + documentNo;
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailFIFO, Filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailFIFO, Filter), sqlparams);

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

        public static DataTable GETData_From_StockRequisition_Details(int stockHeaderID, int companyID, int periodID, bool status = true,
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockHeaderID", stockHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetails, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetails, sqlparams);

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

        public static DataTable GETData_From_StockRequisition_DetailwithAssets(int documentNo, int companyID, int periodID, bool status = true,
          SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailwithAssets, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailwithAssets, sqlparams);

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


        public static DataTable GetDocumentTypeAgainstRequisition(int StockTransactionID, int companyID, int periodID,int CurrentFiscalID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                string query = @"

SELECT DISTINCT INV_StockTransactionHeader.StockTransactionID, INV_StockTransactionHeader.DocumentNo, INV_StockTransactionHeader.DocumentDate, 
    Set_DocumentType.DocumentType, ISnull(SET_CostCenter.CostCenterName,'') as CostCenterName  , '' as Item
FROM INV_StockTransactionHeader 
    inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader. StockTransactionID 
        AND INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID AND INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND INV_StockTransactionDetail.Status = 1 
    left JOIN Set_DocumentType ON Set_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID AND Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
    LEFT JOIN SET_CostCenter ON SET_CostCenter.CostCenterID = INV_StockTransactionHeader.CostCenterID 

--inner join Set_PeriodCustom on  
--INV_StockTransactionHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
--and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE INV_StockTransactionHeader.CompanyID = @CompanyID
 AND INV_StockTransactionHeader.PeriodID = @PeriodID 
 AND INV_StockTransactionHeader.Status = 1 
 AND INV_StockTransactionHeader.StatusCode = 1 
 and INV_StockTransactionHeader.StockTransactionID = @StockTransactionID




";
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

        public static DataTable GETData_From_StockRequisition_DetailForFA(int documentNo, int companyID, int periodID, bool status = true,
    SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetailForFA, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectData_From_StockRequisitionDetail, sqlparams);

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

        public static DataTable GETData_From_StockTransferOut_Detail(int StockTransactionID, int companyID, int periodID, bool status = true,bool ALImdad = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                string Query = "";
                if (ALImdad)
                    Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataForTransferIn;
                else
                    Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Qyery_SelectData_From_StockTransferOutDetail;


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

        public static DataTable GETData_StockTransferIn_Detail(int companyID, int periodID, int sourceHeaderID, int sourceDocumentTypeID, int sourceDocumentNo)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@SourceDocumentTypeID", sourceDocumentTypeID);
                sqlparams[4] = new SqlParameter("@SourceDocumentNo", sourceDocumentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_TransferIn_Against_TransferOut, sqlparams);

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

        public static DataTable GetDataByKeyTable(int stockTransactionID, int companyID, int periodID, bool status = true, SqlConnection objConnection = null,
            bool isSugarMill = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (isSugarMill)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_StockTransactionID_WithQuality, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_StockTransactionID_WithQuality, sqlparams);
                }
                else
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_StockTransactionID_CompanyId, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_By_StockTransactionID_CompanyId, sqlparams);
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

        public static DataTable GetDocNoAndSourceHeaderID(int stockTransactionID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                

                DataSet ds = null;

                string query = @"select * from inv_Stocktransactionheader,inv_Stocktransactiondetail where 
                inv_Stocktransactionheader.stocktransactionid = inv_Stocktransactiondetail.stocktransactionid and  inv_Stocktransactiondetail.stocktransactionid  = @StockTransactionID";
          
                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);
                

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

      

        public static DataTable GetDataByKeyTable_ForGIN(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_ForGIN, sqlparams);

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
        
        public static DataTable GetDataByKeyTable_ForTransferOut(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_TransferOutAverage, sqlparams);

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

        public static DataTable GetDataByKeyTable_ForFindModeTransferOut(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_ForFindModeTransferOut, sqlparams);

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


        public static DataTable GetDataByKeyTable_ForFindModeTransferOut_RSM(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_ForFindModeTransferOut_RSM, sqlparams);

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

        public static DataTable GetDataByTransactionDetailID(int StockTransactionID, int ItemID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                string Query_Select = @" select 
                            INV_StockTransactionDetail.*,INV_StockTransactionHeader.LocationID as LocationIDH,INV_StockTransactionHeader.CostCenterID as CostCenterIDH
                            from INV_StockTransactionDetail ,INV_StockTransactionHeader
                        where 
                            INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
                            and INV_StockTransactionDetail.companyid = @companyid    
                            and INV_StockTransactionDetail.periodid = @periodid 
                            and INV_StockTransactionDetail.StockTransactionID =@StockTransactionID
                            and INV_StockTransactionDetail.ItemID = @ItemID;
                ";

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@ItemID", ItemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select, sqlparams);

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

        public static DataTable GetStockDataByStockTransactionID(int StockTransactionID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                string Query_Select = @" select 
                            INV_StockTransactionDetail.*,INV_StockTransactionHeader.LocationID as LocationIDH,INV_StockTransactionHeader.CostCenterID as CostCenterIDH
                            from INV_StockTransactionDetail ,INV_StockTransactionHeader
                        where 
                            INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
                            and INV_StockTransactionDetail.companyid = @companyid    
                            and INV_StockTransactionDetail.periodid = @periodid 
                            and INV_StockTransactionDetail.StockTransactionID =@StockTransactionID;
                ";

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select, sqlparams);

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

        public int GetNewSerialNo(long transactionDetailID, int companyID, int periodID)
        {
            int maxSerialNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TransactionDetailID", transactionDetailID);

                maxSerialNo = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodTransactionIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }

        public static DataTable GetStockandConsumption(int companyID, int periodID, bool inventorysort, bool descriptionsort, Nullable<int> categoryID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            short? documentGroupID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string stockHeaderFilter = string.Empty;
            string stockHeaderConsumedFilter = string.Empty;

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));

                if (categoryID.HasValue)
                    sqlparams.Add(new SqlParameter("@CategoryID", categoryID.Value));
                else
                    sqlparams.Add(new SqlParameter("@CategoryID", DBNull.Value));

                #region Document Date filter

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    stockHeaderFilter = " And INV_StockTransactionHeader.DocumentDate between @DateFrom And @DateTo ";
                    stockHeaderConsumedFilter = " And StockConsumedH.DocumentDate between @DateFrom And @DateTo ";
                    sqlparams.Add(new SqlParameter("@DateFrom", dateFrom));
                    sqlparams.Add(new SqlParameter("@DateTo", dateTo));
                }
                else if (dateFrom.HasValue)
                {
                    stockHeaderFilter = " And INV_StockTransactionHeader.DocumentDate >= @DateFrom ";
                    stockHeaderConsumedFilter = " And StockConsumedH.DocumentDate >= @DateFrom ";
                    sqlparams.Add(new SqlParameter("@DateFrom", dateFrom));
                }
                else if (dateTo.HasValue)
                {
                    stockHeaderFilter = " And INV_StockTransactionHeader.DocumentDate <= @DateTo ";
                    stockHeaderConsumedFilter = " And StockConsumedH.DocumentDate <= @DateTo ";
                    sqlparams.Add(new SqlParameter("@DateTo", dateTo));
                }

                #endregion

                string query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Select_For_StockConsuymption.Replace("{StockHeaderFilter}", stockHeaderFilter);
                query = query.Replace("{StockHeaderConsumedFilter}", stockHeaderConsumedFilter);


                string additionalFilter = "";
                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                StringBuilder sb = new StringBuilder();
                if (inventorysort == true)
                {
                    sb.Append(" order by InventoryItemStatusCode ");

                }
                if (descriptionsort == true)
                {
                    sb.Append(" order by Description ");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(query, additionalFilter) + sb.ToString(), sqlparams.ToArray());

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

        public static DataTable GetFastMovingItems(int companyID, int periodID, int CostCenterID, int DepartmentID,int LocationID, int movedQuantity, string soldItem, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int? ItemID =null, int?  CategoryID =null ,short? documentGroupID = null, int? ApprovingGroupID=null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string AddtionalParameters = "";
            StringBuilder filterParamters = new StringBuilder();

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@MovedQuantity", movedQuantity));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                 sqlparams.Add(new SqlParameter("@CategoryID", CategoryID));
                 sqlparams.Add(new SqlParameter("@ItemID", ItemID));
                 sqlparams.Add(new SqlParameter("@LocationID", ItemID));

                if (dateFrom != null && dateTo != null)
                    AddtionalParameters = "Where (sh.DocumentDate between @FromDate And @ToDate or sh.DocumentDate is null)";
                else if (dateFrom != null)
                    AddtionalParameters = "Where (sh.DocumentDate >= @FromDate)";
                else if (dateTo != null)
                    AddtionalParameters = "Where (sh.DocumentDate <= @ToDate)";

                if (CategoryID > 0)
                    filterParamters.Append("  AND INV_ITEM.ItemTypeID = @CategoryID");
                 

                if (ItemID > 0)

                    filterParamters.Append("  AND INV_Item.ItemID = @ItemID");
                if (LocationID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.LocationID =" + LocationID);

                if (CostCenterID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID);

                if (DepartmentID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    filterParamters.Append(" AND INV_Item.ApprovalItemGroupID  =" + ApprovingGroupID);
              


                string filterQuery = "";
                if (documentGroupID.HasValue)
                    filterQuery = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (!string.IsNullOrEmpty(soldItem))
                    filterParamters.Append(" AND LastTransactionInfo.DocumentType  like '%Dispatch%' ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_FastMovingItems, AddtionalParameters, filterQuery, filterParamters), sqlparams.ToArray());

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


        public static object GetStockCardRule(int companyID, int periodID, Nullable<DateTime> FromDate, Nullable<DateTime> ToDate, Nullable<int> catalogID)
        {
            
            //List<SqlParameter> sqlparams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;


            #region Constant Queries

             string Query_GetStockCardRule = @"
   --Declare @FromDate DateTime = '30-09-2019',
--        @ToDate DateTime = '30-09-2019'
select inv_stocktransactionheader.DocumentDate, INV_StockTransactionDetail.ItemID,SET_ItemCatalog.[Description] as DocumentType,SET_ItemCatalog.ItemCatalogID,
	     INV_StockTransactionDetail.TransactionQuantity,INV_StockTransactionDetail.CompanyID,INV_Item.ItemCatalogGroupID,
		 INV_Item.[Description], isnull(INV_Item.UnitWeight,0) as UnitWeight, INV_StockTransactionDetail.RackID, INV_Item.ItemID
	 from inv_stocktransactionheader
	         inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.CompanyID = inv_stocktransactionheader.CompanyID
	         and INV_StockTransactionDetail.PeriodID = inv_stocktransactionheader.PeriodID
	         and INV_StockTransactionDetail.StockTransactionID = inv_stocktransactionheader.StockTransactionID
	         and INV_StockTransactionDetail.Status = 1
	  inner Join  INV_Item
		       on INV_Item.ItemID = INV_StockTransactionDetail.ItemID
		     and INV_Item.ItemCatalogGroupID =  23 --and INV_Item.Description like '%R1 Cl Printed Sugar Bag%'
		    and INV_Item.Status = 1 

	 Inner Join SET_ItemCatalog 
	        on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID 
			and INV_Item.ItemCatalogGroupID = 23
		--and SET_ItemCatalog.[Description] like '%Finish Goods Sugar%'

		AND INV_StockTransactionHeader.DocumentDate  between @FromDate and @ToDate
	--	left join INV_Item EndProduct on EndProduct.ItemID = INV_StockTransactionDetail.FinishingItemID
	--left join SET_ItemCatalog EndCatalog on EndCatalog.ItemCatalogID = EndProduct.ItemCatalogGroupID
	--     and SET_ItemCatalog.ItemCatalogID like '%R1 CL Printed Sugar Bag%'
		    where INV_StockTransactionHeader.Status = 1
		    and INV_StockTransactionHeader.StatusCode = 1


			




    



";
            #endregion
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                DataSet ds = null;

                if (catalogID.HasValue)
                    sb.Append(" and SET_ItemCatalog.ItemCatalogID = " + catalogID + "");
                //if (catalogID.HasValue)
                //    whereClause.Append(" AND SET_ItemCatalog.ItemCatalogID = " + catalogID.ToString());

               
                    //    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SelectItemWiseGRM, sb), sqlparams);
                 ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetStockCardRule, sb), sqlparams);

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
        public static DataTable GetSlowMovingItems(int companyID, int periodID, int CostCenterID, int DepartmentID, int LocationID, int movedQuantity,Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, int? ItemID = null, int? CategoryID = null, short? documentGroupID = null, int? ApprovingGroupID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            StringBuilder filterParamters = new StringBuilder();
            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlparams.Add(new SqlParameter("@ToDate", dateTo));
                sqlparams.Add(new SqlParameter("@MovedQuantity", movedQuantity));
                sqlparams.Add(new SqlParameter("@CategoryID", CategoryID));
                sqlparams.Add(new SqlParameter("@ItemID", ItemID));
                sqlparams.Add(new SqlParameter("@LocationID", LocationID));

                if (CategoryID > 0)
                    filterParamters.Append("  AND INV_ITEM.ItemTypeID = @CategoryID");


                if (ItemID > 0)

                    filterParamters.Append("  AND INV_Item.ItemID = @ItemID");
                if (LocationID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.LocationID =" + LocationID);

                if (CostCenterID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID);

                if (DepartmentID > 0)
                    filterParamters.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    filterParamters.Append(" AND INV_Item.ApprovalItemGroupID  =" + ApprovingGroupID);
              

                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SlowMovingItems, additionalFilter,filterParamters.ToString()), sqlparams.ToArray());

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

        public static DataTable GetNonMovingItems(int companyID, int periodID, int CostCenterID, int DepartmentID, int LocationID, Nullable<DateTime> FromDate = null,
            Nullable<DateTime> ToDate = null,Nullable<int> itemCategoryID = null,
            Nullable<int> ItemID = null, short? documentGroupID = null, int? ApprovingGroupID = null)
        {
            List<SqlParameter> sqlparams = new List<SqlParameter>();
            string dateFilter = string.Empty;
            StringBuilder sb = new StringBuilder();
            DataTable dtToReturn = null;

            // INV_StockTransactionHeader.DocumentDate Has Been Changed To INV_Item.CreateDate Because no datefiltring in Items ( TAQI )
            // {DateFilter} has been shifted from Last Where Clause to 2nd last Where Clause ( TAQI )

            try
            {
                sqlparams.Add(new SqlParameter("@CompanyID", companyID));
                sqlparams.Add(new SqlParameter("@PeriodID", periodID));
                sqlparams.Add(new SqlParameter("@DateFrom", FromDate.Value));
                sqlparams.Add(new SqlParameter("@DateTo", ToDate.Value));
                sqlparams.Add(new SqlParameter("@CategoryID", itemCategoryID));
                sqlparams.Add(new SqlParameter("@ItemID", ItemID));

                if (FromDate.HasValue && ToDate.HasValue)
                {
                    dateFilter = " And INV_Item.CreateDate between @DateFrom And @DateTo  ";
                }
                else if (FromDate.HasValue)
                {
                    dateFilter = " And INV_Item.CreateDate >= @DateFrom";
                    sqlparams.Add(new SqlParameter("@DateFrom", FromDate.Value));
                }
                else if (ToDate.HasValue)
                {
                    dateFilter = " And INV_Item.CreateDate <= @DateTo ";
                    sqlparams.Add(new SqlParameter("@DateTo", ToDate.Value));
                }

                if (itemCategoryID > 0)
                    sb.Append("  AND INV_ITEM.ItemTypeID = @CategoryID");


                if (ItemID > 0)

                    sb.Append("  AND INV_Item.ItemID = @ItemID");
                if (LocationID > 0)
                    sb.Append(" AND INV_StockTransactionHeader.LocationID =" + LocationID);

                if (CostCenterID > 0)
                    sb.Append(" AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID);

                if (DepartmentID > 0)
                    sb.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    sb.Append(" AND INV_Item.ApprovalItemGroupID  =" + ApprovingGroupID);
              



                string additionalFilter = "";
                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_NonMovingItems.Replace("{DateFilter}", dateFilter), additionalFilter,sb.ToString()), sqlparams.ToArray());

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

        public static DataTable GetStockData(string companyID, int periodID, int ApprovingGroup, Nullable<int> itemCategoryID, string itemText, Nullable<int> transactionTypeID, Nullable<DateTime> startDate, Nullable<DateTime> endDate, short? documentGroupID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string purchaseFilter = string.Empty, stockFilter = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ItemFilter", itemText));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (transactionTypeID.HasValue)
                    sqlParams.Add(new SqlParameter("@TransactionType", transactionTypeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@TransactionType", DBNull.Value));

                if (startDate.HasValue && endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate Between @DateFrom And @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate Between @DateFrom And @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }
                else if (startDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate >= @DateFrom ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate >= @DateFrom ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                }
                else if (endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate <= @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate <= @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }

                string additionParam = "", additionParam1 = "";
                if (documentGroupID.HasValue)
                    additionParam = "  AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (ApprovingGroup > 0)

                    additionParam1 = " And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString();

                companyID = "(" + companyID + ")";
                string finalQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock.Replace("{PurchaseFilter}", purchaseFilter).Replace("{StockFilter}", stockFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(finalQuery, additionParam, additionParam1).Replace("@CompanyID",companyID) , sqlParams.ToArray());

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

        public static DataTable GetItemWiseClosingStockReport(int companyID, int periodID, Nullable<int> itemCategoryID, string itemText, Nullable<int> transactionTypeID,
            Nullable<DateTime> startDate, Nullable<DateTime> endDate)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string purchaseFilter = string.Empty, stockFilter = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ItemFilter", itemText));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (transactionTypeID.HasValue)
                    sqlParams.Add(new SqlParameter("@TransactionType", transactionTypeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@TransactionType", DBNull.Value));

                if (startDate.HasValue && endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate Between @DateFrom And @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate Between @DateFrom And @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }
                else if (startDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate >= @DateFrom ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate >= @DateFrom ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                }
                else if (endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate <= @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate <= @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }

                string finalQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ItemWiseClosingStockReport.Replace("{PurchaseFilter}", purchaseFilter).Replace("{StockFilter}", stockFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlParams.ToArray());

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

        public static DataTable GetStockDataDashBoard(int companyID, int periodID, Nullable<int> itemCategoryID, string itemText, Nullable<int> transactionTypeID,
            Nullable<DateTime> startDate, Nullable<DateTime> endDate)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;
            string purchaseFilter = string.Empty, stockFilter = string.Empty;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ItemFilter", itemText));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (transactionTypeID.HasValue)
                    sqlParams.Add(new SqlParameter("@TransactionType", transactionTypeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@TransactionType", DBNull.Value));

                if (startDate.HasValue && endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate Between @DateFrom And @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate Between @DateFrom And @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }
                else if (startDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate >= @DateFrom ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate >= @DateFrom ";
                    sqlParams.Add(new SqlParameter("@DateFrom", startDate.Value));
                }
                else if (endDate.HasValue)
                {
                    purchaseFilter = " And AP_PurchaseOrderHeader.DocumentDate <= @DateTo ";
                    stockFilter = " And INV_StockTransactionHeader.DocumentDate <= @DateTo ";
                    sqlParams.Add(new SqlParameter("@DateTo", endDate.Value));
                }

                string finalQuery = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockDashboard.Replace("{PurchaseFilter}", purchaseFilter).Replace("{StockFilter}", stockFilter);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, finalQuery, sqlParams.ToArray());

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

        public static DataTable GetStockVerificationStatementsData(int companyID, int periodID, Nullable<int> itemCategory = null,
            Nullable<int> lastPurchaseSupplier = null, Nullable<int> lastTransactionType = null, Nullable<DateTime> lastPurchaseDate = null,
            Nullable<DateTime> lastTransactionDate = null, string itemDescription = null, short? documentGroupID = null, Nullable<int> ApprovalGroupID = null)
        {
            string purchaseFilter = string.Empty, stockFilter = string.Empty;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@ItemFilter", itemDescription));

                if (itemCategory.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategory.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (lastPurchaseSupplier.HasValue)
                    sqlParams.Add(new SqlParameter("@LastPurchaseSupplier", lastPurchaseSupplier.Value));
                else
                    sqlParams.Add(new SqlParameter("@LastPurchaseSupplier", DBNull.Value));

                if (lastTransactionType.HasValue)
                    sqlParams.Add(new SqlParameter("@TransactionType", lastTransactionType.Value));
                else
                    sqlParams.Add(new SqlParameter("@TransactionType", DBNull.Value));

                if (lastPurchaseDate.HasValue)
                    sqlParams.Add(new SqlParameter("@PurchaseDate", lastPurchaseDate.Value));
                else
                    sqlParams.Add(new SqlParameter("@PurchaseDate", DBNull.Value));

                if (lastTransactionDate.HasValue)
                    sqlParams.Add(new SqlParameter("@StockDate", lastTransactionDate.Value));
                else
                    sqlParams.Add(new SqlParameter("@StockDate", DBNull.Value));

                string additionalParam = "";

                if (documentGroupID.HasValue)
                    additionalParam = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (ApprovalGroupID.HasValue)
                    additionalParam = additionalParam + " AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockVerificationStatements, additionalParam), sqlParams.ToArray());

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
        
        public static DataTable GetStockVerificationStatementsData_New(string companyID, int periodID, Nullable<int> itemCategory = null,
            Nullable<int> lastPurchaseSupplier = null, Nullable<int> lastTransactionType = null, Nullable<DateTime> lastPurchaseDate = null,
            Nullable<DateTime> lastTransactionDate = null, Nullable<int> ItemID = null, short? documentGroupID = null, Nullable<int> ApprovalGroupID = null)
        {
            string purchaseFilter = string.Empty, stockFilter = string.Empty;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));

                if (ItemID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@ItemID", ItemID.Value));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));
                }

                if (itemCategory.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategory.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (lastPurchaseSupplier.HasValue)
                    sqlParams.Add(new SqlParameter("@LastPurchaseSupplier", lastPurchaseSupplier.Value));
                else
                    sqlParams.Add(new SqlParameter("@LastPurchaseSupplier", DBNull.Value));

                if (lastTransactionType.HasValue)
                    sqlParams.Add(new SqlParameter("@TransactionType", lastTransactionType.Value));
                else
                    sqlParams.Add(new SqlParameter("@TransactionType", DBNull.Value));

                if (lastPurchaseDate.HasValue)
                    sqlParams.Add(new SqlParameter("@PurchaseDate", lastPurchaseDate.Value));
                else
                    sqlParams.Add(new SqlParameter("@PurchaseDate", DBNull.Value));

                if (lastTransactionDate.HasValue)
                    sqlParams.Add(new SqlParameter("@StockDate", lastTransactionDate.Value));
                else
                    sqlParams.Add(new SqlParameter("@StockDate", DBNull.Value));

                string additionalParam = "";

                companyID = "(" + companyID + ")";

                if (documentGroupID.HasValue)
                    additionalParam = " AND SET_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (ApprovalGroupID.HasValue)
                    additionalParam = additionalParam + " AND INV_Item.ApprovalItemGroupID = " + ApprovalGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockVerificationStatements_New.Replace("@CompanyID", companyID), additionalParam), sqlParams.ToArray());

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

        public static DataSet StockInventory_LocationWise(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> itemCategoryID, string itemText,
            Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            strFilter += " TAB.DocumentType like '%GRN%'";

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemText.ToString() != string.Empty)
                {
                    sb.Append(" And INV_Item.Description like '%" + itemText + "%' ");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Stock_Ledger_Report(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID, int ObsoleteCheck,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0, string DocumentTypes = "", string CatalogTypes = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID ));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                    sb2.Append(" Where ItemID = " + ItemID);
                }
                else
                { 
                    sb.Append("  ");
                    sb2.Append(" Where ItemID is Null OR Null is null "); 
                }

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                //if (CatalogID.HasValue)
                //    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);

                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID = " + itemCategoryID.Value);
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID is Null OR Null is null");
                }

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    //sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");

                    sort.Append("order by FinTAb.DocumentDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }



                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (CatalogTypes != "")
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                    sb2.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                }
                
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString(), sb2.ToString()), sqlParams.ToArray());
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger2.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable Get_Data_For_GRN_Inspection_Report(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID, int ObsoleteCheck,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0, string DocumentTypes = "", string CatalogTypes = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                    sb2.Append(" Where ItemID = " + ItemID);
                }
                else
                {
                    sb.Append("  ");
                    sb2.Append(" Where ItemID is Null OR Null is null ");
                }

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID = " + itemCategoryID.Value);
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID is Null OR Null is null");
                }

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (CatalogTypes != "")
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                    sb2.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GRN_Inspection.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString(), sb2.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dsToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Stock_Ledger_ReportPattoki(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID, int ObsoleteCheck,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0, string DocumentTypes = "", string CatalogTypes = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID ));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                    sb2.Append(" Where ItemID = " + ItemID);
                }
                else
                { 
                    sb.Append("  ");
                    sb2.Append(" Where ItemID is Null OR Null is null "); 
                }

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                //if (CatalogID.HasValue)
                //    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);

                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID = " + itemCategoryID.Value);
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));
                    sb2.Append(" And INV_SET_Category.CategoryID is Null OR Null is null");
                }

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    //sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");

                    sort.Append("order by FinTAb.DocumentDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }



                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (CatalogTypes != "")
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                    sb2.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger_Pattoki.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString(), sb2.ToString()), sqlParams.ToArray());
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger2.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Get_DataForInvStockItem_Report(string CompanyID, int PeriodID, DateTime dateFrom, DateTime dateTo, Nullable<int> ItemID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", CompanyID));
                sqlParams.Add(new SqlParameter("@PeriodID", PeriodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                if (ItemID != null)
                    sqlParams.Add(new SqlParameter("@ItemID", ItemID));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_InvStockItem.ToString().Replace("@CompanyID", CompanyID)), sqlParams.ToArray());
                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Stock_Ledger_WithDocumentType_Report(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0,Nullable<int> DocumentType = 0, string DocumentTypes = "", string CatalogTypes = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                //if (CatalogID.HasValue)
                //    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);

                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    //sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");

                    sort.Append("order by FinTAb.DocumentDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }



                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (DocumentType.HasValue && DocumentType > 0)
                {
                    sb1.Append("AND SET_DocumentType.DocumentTypeID = " + DocumentType.Value);
                }

                if (CatalogTypes != "")
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_LedgerWithDoumentType.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());
                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger2.ToString().Replace("@CompanyID", companyID), sb.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public static DataTable Annual_Budget_ReportDetail(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID, Nullable<int> ItemID,Nullable<int> SeasonID, string season )
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
    
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                sqlParams.Add(new SqlParameter("@Season", season));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                if (SeasonID != -1)
                {
                    sb1.Append(" And AnnualBudget_Header.SeasonType =" + SeasonID);
                }

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));



                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Annual_Budget.ToString().Replace("@CompanyID", companyID), sb.ToString(),sb1.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());     
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dsToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable StockLedgerCategoryWise(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> CatalogID,
            Nullable<int> itemCategoryID, string CatalogTypes = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                //sqlParams.Add(new SqlParameter("@Season", season));

                //if (ItemID.HasValue)
                //{
                //    sb.Append(" And INV_Item.ItemID =" + ItemID);
                //}
                //else
                //{ sb.Append("  "); }

                //if (SeasonID != -1)
                //{
                //    sb1.Append(" And AnnualBudget_Header.SeasonType =" + SeasonID);
                //}

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (CatalogTypes != "")
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID in (" + CatalogTypes + ") ");
                }

                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockLedgerCategoryWise.ToString().Replace("@CompanyID", companyID), sb.ToString(), sb1.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dsToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable Annual_Budget_Report_Summary(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID, Nullable<int> ItemID,Nullable<int> SeasonID, string season)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                sqlParams.Add(new SqlParameter("@Season", season));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                if (SeasonID != -1)
                {
                    sb1.Append(" And AnnualBudget_Header.SeasonType =" + SeasonID);
                }

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));



                if (strFilter == "where")
                    strFilter = "";

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Annual_Budget_Summary.ToString().Replace("@CompanyID", companyID), sb.ToString(), sb1.ToString(), strFilter, sort.ToString(), sb1.ToString()), sqlParams.ToArray());
      
                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dsToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Closing_Stock_Ledger_Report(string companyID, int periodID, DateTime dateFrom, DateTime dateTo, string CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID,
         Nullable<int> locationID, Nullable<int> ZeroFilter, string SortingBy )
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                if (CatalogID != String.Empty)
                    sb.Append(" And INV_Item.ItemCatalogGroupID in " + CatalogID);
                
                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                //if (strFilter == "where")
                //    strFilter = "1=1";
                if (ZeroFilter.HasValue)
                {
                    if (ZeroFilter.Value == 0)
                        strFilter = " ";
                    else if (ZeroFilter.Value == 1)
                        strFilter = "Where FinalFinTAb.TransactionQuantity = 0";
                    else if (ZeroFilter.Value == 2)
                        strFilter = "Where FinalFinTAb.TransactionQuantity <> 0";
                    else
                        strFilter = "";
                }


                companyID = "(" + companyID + ")";

                string Query = FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Closing_Query_Stock_Ledger.ToString().Replace("{FilterByQty}" , strFilter)+ SortingBy.ToString() ;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query.Replace("@CompanyID", companyID), sb.ToString()), sqlParams.ToArray());

                if (ds != null )
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public static DataSet Stock_Ledger_ReportAvg(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> ItemID,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }
                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }



                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockLedgerAverage, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable StockAgeingReportReactiveDyes(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID, Nullable<int> ItemID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
           
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                
                
                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

               
                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.StockAgeingReportReactiveDyes, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

               // if (ds != null && ds.Tables.Count > 1)
                   // dsToReturn = ds;

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

        public static DataTable RecipeWiseBalanceItem(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID, Nullable<int> ItemID, string ReqNo = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) between @FromDate AND @ToDate ");
                }
                else if (dateFrom != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @FromDate ");
                }
                else if (dateTo == null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @ToDate");
                }

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }

                if (ReqNo != "")
                {
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo =" + ReqNo + "");
                }
               


                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Querey_RecipeWiseBalanceItem, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                // if (ds != null && ds.Tables.Count > 1)
                // dsToReturn = ds;

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

        public static DataTable RecipeWsieMaterailIssuance(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, string ReqNo = "", string Lotno = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (dateFrom != null && dateTo != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) between @FromDate AND @ToDate ");
                }
                else if (dateFrom != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @FromDate ");
                }
                else if (dateTo == null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @ToDate");
                }


                if (ReqNo != "")
                {
                    sb.Append(" AND INV_StockTransactionDetail.SourceCode =" + ReqNo + "");
                }

                if (Lotno != "")
                {
                    sb.Append(" AND Fabrics_DyeingReceipeIssuanceDetail.Lotno =" + Lotno + "");
                }


                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_RecipeWsieMaterailIssuance, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                // if (ds != null && ds.Tables.Count > 1)
                // dsToReturn = ds;

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
        
        public static decimal Stock_Balance_Quantity(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
            bool sortbyinventorycode, Nullable<int> itemCategoryID, int itemId,
        Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, string IssuanceFlow = "",Nullable<int> RackID = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal _quantityBalance = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            //DataRow dr = SET_Period.GetPeriodStartDate(periodID);
            //dateFrom = Convert.ToDateTime(dr["StartDate"].ToString());


            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" AND  (isnull(SET_Location.LocationID,SL.LocationID) = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND (SET_Location.LocationID = @LocationID OR @LocationID is null) ");
                }


                if (RackID.HasValue)
                {
                    sb.Append(" and (INV_StockTransactionDetail.RackID = @RackID OR @RackID is null) ");
                }


                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue && locationID > 0)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));


                if (RackID.HasValue && RackID > 0)
                    sqlParams.Add(new SqlParameter("@RackID", RackID.Value));
                else
                    sqlParams.Add(new SqlParameter("@RackID", DBNull.Value));

                if (IssuanceFlow == "FIFO")
                    sb.Append("  AND INV_StockTransactionHeader.DocumentDate <=@FromDate ");



                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Balance_Quantity, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    _quantityBalance = Convert.ToDecimal(ds.Tables[0].Rows[0]["TransactionQuantity"] == "" ? 0 : ds.Tables[0].Rows[0]["TransactionQuantity"]);
                //_quantityBalance =ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() =="" ? int.Parse(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString()) : 0 ;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _quantityBalance;
        }


        public static decimal Stock_Balance_Quantity_LocAndRack_Wise(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
            bool sortbyinventorycode, Nullable<int> itemCategoryID, int itemId,
        Nullable<int> locationID,Nullable<int> RackID, bool IsDispatchCancelHide = false, bool isVehicle = false, string IssuanceFlow = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal _quantityBalance = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            //DataRow dr = SET_Period.GetPeriodStartDate(periodID);
            //dateFrom = Convert.ToDateTime(dr["StartDate"].ToString());


            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" AND  (isnull(SET_Location.LocationID,SL.LocationID) = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND (SET_Location.LocationID = @LocationID OR @LocationID is null) ");
                }

                if (RackID.HasValue)
                {

                    sb.Append(" AND (INV_StockTransactionDetail.RackID = @RackID OR @RackID is null) ");
                }

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue && locationID > 0)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));


                if (RackID.HasValue && RackID > 0)
                    sqlParams.Add(new SqlParameter("@RackID", RackID.Value));
                else
                    sqlParams.Add(new SqlParameter("@RackID", DBNull.Value));

                if (IssuanceFlow == "FIFO")
                    sb.Append("  AND INV_StockTransactionHeader.DocumentDate <=@FromDate ");



                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Balance_Quantity_LocAndRack, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    _quantityBalance = Convert.ToDecimal(ds.Tables[0].Rows[0]["TransactionQuantity"] == "" ? 0 : ds.Tables[0].Rows[0]["TransactionQuantity"]);
                //_quantityBalance =ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() =="" ? int.Parse(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString()) : 0 ;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _quantityBalance;
        }


        public static decimal Stock_Balance_Quantity_Grainsize(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
       bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> GrainSizeID, Nullable<int> PackTypeID, int itemId,
   Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, string IssuanceFlow = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal _quantityBalance = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            //DataRow dr = SET_Period.GetPeriodStartDate(periodID);
            //dateFrom = Convert.ToDateTime(dr["StartDate"].ToString());


            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID1", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID1", periodID));
                sqlParams.Add(new SqlParameter("@FromDate1", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate1", dateTo));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" AND  (isnull(SET_Location.LocationID,SL.LocationID) = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND (SET_Location.LocationID = @LocationID OR @LocationID is null) ");
                }

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory1", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory1", DBNull.Value));

                if (GrainSizeID.HasValue && GrainSizeID.Value > 0)
                    sqlParams.Add(new SqlParameter("@GrainSizeID1", GrainSizeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@GrainSizeID1", DBNull.Value));

                if (PackTypeID.HasValue && PackTypeID.Value > 0)
                    sqlParams.Add(new SqlParameter("@PackTypeID1", PackTypeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@PackTypeID1", DBNull.Value));

                if (locationID.HasValue && locationID > 0)
                    sqlParams.Add(new SqlParameter("@LocationID1", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID1", DBNull.Value));


                if (IssuanceFlow == "FIFO")
                    sb.Append("  AND INV_StockTransactionHeader.DocumentDate <=@FromDate ");



                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Balance_Quantity_Grainsize, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    _quantityBalance = Convert.ToDecimal(ds.Tables[0].Rows[0]["TransactionQuantity"] == "" ? 0 : ds.Tables[0].Rows[0]["TransactionQuantity"]);
                //_quantityBalance =ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() =="" ? int.Parse(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString()) : 0 ;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _quantityBalance;
        }

        public static decimal Stock_Balance_Quantity_Grainsize_WithoutParameter(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
       bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> GrainSizeID, Nullable<int> PackTypeID, int itemId,
   Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, string IssuanceFlow = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal _quantityBalance = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            //DataRow dr = SET_Period.GetPeriodStartDate(periodID);
            //dateFrom = Convert.ToDateTime(dr["StartDate"].ToString());


            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" AND  (isnull(SET_Location.LocationID,SL.LocationID) = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND (SET_Location.LocationID = @LocationID OR @LocationID is null) ");
                }

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (GrainSizeID.HasValue && GrainSizeID.Value > 0)
                    sqlParams.Add(new SqlParameter("@GrainSizeID", GrainSizeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@GrainSizeID", DBNull.Value));

                if (PackTypeID.HasValue && PackTypeID.Value > 0)
                    sqlParams.Add(new SqlParameter("@PackTypeID", PackTypeID.Value));
                else
                    sqlParams.Add(new SqlParameter("@PackTypeID", DBNull.Value));

                if (locationID.HasValue && locationID > 0)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));


                if (IssuanceFlow == "FIFO")
                    sb.Append("  AND INV_StockTransactionHeader.DocumentDate <=@FromDate ");



                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Balance_Quantity_GrainsizeWithoutParam, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    _quantityBalance = Convert.ToDecimal(ds.Tables[0].Rows[0]["TransactionQuantity"] == "" ? 0 : ds.Tables[0].Rows[0]["TransactionQuantity"]);
                //_quantityBalance =ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() =="" ? int.Parse(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString()) : 0 ;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return _quantityBalance;
        }


        public static decimal Stock_Balance_Quantity_Fraction(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
          bool sortbyinventorycode, Nullable<int> itemCategoryID, int itemId,
      Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, string IssuanceFlow = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal _quantityBalance = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";

            //DataRow dr = SET_Period.GetPeriodStartDate(periodID);
            //dateFrom = Convert.ToDateTime(dr["StartDate"].ToString());


            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append(" AND SET_Location.LocationID = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND INV_StockTransactionHeader.LocationID = @LocationID OR @LocationID is null) ");
                }


                if(IssuanceFlow=="FIFO")
                    sb.Append("  AND INV_StockTransactionHeader.DocumentDate <=@FromDate ");
                

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                //if (itemCategoryID.HasValue)
                //    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                //else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Balance_Quantity, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                //String.Format("{0:0.00}", ds.Tables[0].Rows[0]["TransactionQuantity"]);

                if (ds != null && ds.Tables.Count > 0)
                    _quantityBalance = ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() == "" ? 0 : Convert.ToDecimal(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString());

                //_quantityBalance =ds.Tables[0].Rows[0]["TransactionQuantity"].ToString() =="" ? int.Parse(ds.Tables[0].Rows[0]["TransactionQuantity"].ToString()) : 0 ;


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return System.Math.Round(_quantityBalance, 2);
        }


        public static decimal Stock_Balance_Amount(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription,
           bool sortbyinventorycode, Nullable<int> itemCategoryID, int itemId,
       Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, bool isDispatchCount = false, bool onDispatchForm = false, DataTable dt_dispatch = null,DateTime? Date = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            decimal avgAmount = 0;
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";



            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                sqlParams.Add(new SqlParameter("@ItemID", itemId));


                if (itemId > 0)
                {
                    sb.Append(" And INV_Item.ItemId = " + itemId + "");
                }
                else
                { sb.Append("  "); }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        sb.Append("  AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null) ");
                    else
                        sb.Append(" AND (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null) ");
                }

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    // sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    // sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    // sort.Append("order by FinTAb.CreateDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }

                if (strFilter == "where")
                    strFilter = "";


                avgAmount = RECON_AverageStockAmount(itemId, locationID.Value, companyID, periodID, Date);

              


            }
            catch
            {

            }

            return avgAmount;
        }

        private static decimal RECON_AverageStockAmount(int itemId, int locationID,int CompanyID,int PeriodID,DateTime? Date)
        {
            decimal returnValue = 0;

            DataTable dt = INV_StockTransactionDetail.GetStockReconcileLedger(itemId, locationID, CompanyID, PeriodID, Date);

            if (!dt.Columns.Contains("RunningAverage"))
                dt.Columns.Add("RunningAverage", typeof(decimal));

            //update running total quantity

            decimal RunningQuantity = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {

                if (dt.Rows[i]["TransactionImpact"].ToString() == "Positive")
                {
                    RunningQuantity += Math.Abs(Convert.ToDecimal(dt.Rows[i]["TransactionQuantity"]));
                }
                else
                {
                    RunningQuantity -= Math.Abs(Convert.ToDecimal(dt.Rows[i]["TransactionQuantity"]));
                }

                dt.Rows[i]["RunningQuantity"] = RunningQuantity.ToString();
            }

            for (int j = 1; j < dt.Rows.Count; j++)
            {
                //RunningQuantity += Convert.ToInt32(dt.Rows[i]["TransactionQuantity"]);
                dt.Rows[j]["PreviousQty"] = dt.Rows[j - 1]["RunningQuantity"].ToString();
            }

            decimal CurrentAverage = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i == 0 && dt.Rows[i]["TransactionImpact"].ToString() == "Positive")
                {
                    CurrentAverage = Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }
                else if ((dt.Rows[i]["TransactionImpact"].ToString() == "Positive" || dt.Rows[i]["DocumentType"].ToString().Contains("Purchase Return")) && !dt.Rows[i]["DocumentType"].ToString().Contains("Dispatch Ret"))
                {
                    //if (Convert.ToDecimal(dt.Rows[i]["ValueChange"].ToString()) > 0)
                    //{
                    //    CurrentAverage  =Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    //}
                    //else { 
                    //}

                    if (Convert.ToDecimal(dt.Rows[i]["RunningQuantity"].ToString()) == 0)
                    {
                        CurrentAverage = Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    }
                    else
                    {
                        CurrentAverage = Math.Abs(((Convert.ToDecimal(dt.Rows[i]["TransactionQuantity"].ToString()) * Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString())) +
                                             (Convert.ToDecimal(dt.Rows[i]["PreviousQty"].ToString()) * Convert.ToDecimal(dt.Rows[i - 1]["RunningAverage"].ToString())))
                                             / Convert.ToDecimal(dt.Rows[i]["RunningQuantity"].ToString()));
                    }


                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }
                else
                {
                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }

            }

            try
            {
                returnValue = Convert.ToDecimal(dt.Rows[dt.Rows.Count-1]["RunningAverage"].ToString());
            }
            catch
            {


            }
            return returnValue;
        }

        public bool UpdateDataForDOUpdate(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();

                //sb.Append(",[TransactionCost] = " + f_TransactionCost + "");


                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", f_TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", f_StockTransactionID);
                sqlparams[4] = new SqlParameter("@TransactionQuantity", f_TransactionQuantity);
                sqlparams[5] = new SqlParameter("@SourceCode", f_SourceCode);
                sqlparams[6] = new SqlParameter("@SourceHeaderID", f_SourceHeaderID);
                sqlparams[7] = new SqlParameter("@ItemID", f_ItemID);
                sqlparams[8] = new SqlParameter("@TransactionUOMID", f_TransactionUOMID);



                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_ForDOUpdate, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_ForDOUpdate, sqlparams) > 0)
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
        //stockGIN Report
        public static DataSet StockGIN(int companyID, int periodID, int costcenter, int department, int incurredOn, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));
                if (costcenter == -1)
                {
                    sqlParams.Add(new SqlParameter("@CostCenterID", DBNull.Value));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@CostCenterID", costcenter));
                }


                if (department == -1)
                {
                    sqlParams.Add(new SqlParameter("@DepartmentID", DBNull.Value));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@DepartmentID", department));
                }

                if (incurredOn == -1)
                {
                    sqlParams.Add(new SqlParameter("@IncurredOnID", DBNull.Value));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@IncurredOnID", incurredOn));
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockGINReport, sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet Stock_LedgeFIFON_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID,
            Nullable<int> itemID, Nullable<int> locationID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger_FIFON, sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public static DataTable Stock_LedgeFIFON_Summary_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID,
          Nullable<int> itemID, Nullable<int> locationID, bool ZeroBalance = false, int ApprovingGroup = 0)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));


                if (ZeroBalance)
                    sb.Append("where round(TAB.balance,2) <> 0");
                
                if (ApprovingGroup > 0)
                    sb1.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockLedgerSummaryFIFO, sb.ToString(), sb1.ToString()), sqlParams.ToArray()).Tables[0];

                DataTable Openingdt = null;

            


                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dt.Rows[i]["Rate"] = "0";
                }
                //for (int i = 0; i < dt.Rows.Count; i++)
                //{
                //    int itemid = int.Parse(dt.Rows[i]["ItemID"].ToString());
                //    int locID = int.Parse(dt.Rows[i]["LocationID"].ToString());

                //    dt.Rows[i]["Rate"] = GetListItemLastOpening(companyID, periodID, locID, dateTo, itemid).Rows[0]["OpeningBalanceRate"];

                //    if (dt.Rows[i]["Rate"].ToString() == "")
                //        dt.Rows[i]["Rate"] = "0";
                //}

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable StockLedgerSummaryDocumentWiseReport(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID,
          Nullable<int> itemID, Nullable<int> locationID, bool ZeroBalance = false,short? documentGroupID = null, int ApprovingGroup = 0)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb2= new StringBuilder();
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (ApprovingGroup > 0)
                    sb2.Append(" And ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (documentGroupID.HasValue)
                    sb2.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (ZeroBalance)
                    sb.Append("where round(TAB.balance,2) <> 0");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockLedgerDocumentWise, sb.ToString(),sb2.ToString()), sqlParams.ToArray()).Tables[0];

                DataTable Openingdt = null;




                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    dt.Rows[i]["Rate"] = "0";
                }
                //for (int i = 0; i < dt.Rows.Count; i++)
                //{
                //    int itemid = int.Parse(dt.Rows[i]["ItemID"].ToString());
                //    int locID = int.Parse(dt.Rows[i]["LocationID"].ToString());

                //    dt.Rows[i]["Rate"] = GetListItemLastOpening(companyID, periodID, locID, dateTo, itemid).Rows[0]["OpeningBalanceRate"];

                //    if (dt.Rows[i]["Rate"].ToString() == "")
                //        dt.Rows[i]["Rate"] = "0";
                //}

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetListItemLastOpening(int companyId, int periodId, int locationID, DateTime tillDate, int itemID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyId);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@LocationID", locationID);
                sqlparams[3] = new SqlParameter("@FromDate", tillDate);
                sqlparams[4] = new SqlParameter("@ItemID", itemID);

                string query = @"SELECT SUM(t.OpeningBalanceQty)OpeningBalanceQty,SUM(t.Amount)/(CASE WHEN SUM(t.OpeningBalanceQty) = 0 THEN 1 ELSE SUM(t.OpeningBalanceQty) END ) OpeningBalanceRate,t.itemid FROM (
  	SELECT Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) AS OpeningBalanceQty, 
		(case when INV_StockTransactionDetail.TransactionQuantity > 0 then INV_StockTransactionDetail.TransactionCost else INV_StockTransactionDetail.AverageRate END) AS OpeningBalanceRate,
		
		(Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) * (case when INV_StockTransactionDetail.TransactionQuantity > 0 then INV_StockTransactionDetail.TransactionCost else INV_StockTransactionDetail.AverageRate END)) Amount,
		
		 INV_StockTransactionDetail.ItemID
	FROM INV_StockTransactionDetail 
		INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
			AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID AND INV_StockTransactionHeader.Status = 1 
			AND INV_StockTransactionHeader.IsActive = 1 
		Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
			And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
			AND Set_DocumentType.Status = 1 
			LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
		LEFT JOIN SET_Location locationDt ON  locationDt.LocationID = INV_StockTransactionDetail.LocationID 
	WHERE INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID 
		AND INV_StockTransactionDetail.Status = 1 
		AND INV_StockTransactionHeader.Status = 1 
		AND (INV_StockTransactionHeader.StatusCode = 1 OR INV_StockTransactionHeader.StatusCode is Null) 
		AND INV_StockTransactionDetail.IsActive = 1 
		AND INV_StockTransactionHeader.DocumentDate < @FromDate 
		AND (isnull(locationDt.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)
		And (INV_StockTransactionDetail.ItemID = @ItemID OR @ItemID IS NULL) 
	GROUP BY INV_StockTransactionDetail.ItemID,INV_StockTransactionDetail.AverageRate,INV_StockTransactionDetail.TransactionCost,INV_StockTransactionDetail.TransactionQuantity
	) t GROUP BY t.itemid";

                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataSet Stock_LedgeFIFO_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID,
            Nullable<int> itemID, Nullable<int> locationID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger_FIFO, sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet FIFO_Posting_Register_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, Nullable<int> itemCategoryID,
          Nullable<int> itemID, Nullable<int> locationID)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;

            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (itemID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemID", itemID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemID", DBNull.Value));

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Stock_Ledger_FIFO, sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet DemandStatus_Report(int companyID, int periodID,Nullable<int> itemID, Nullable<int> requisitionNoFrom, Nullable<int> requisitionNoTo,
            string DateFrom = null, string DateTo = null, string Filter = "", int CurrentFiscalID = 0, int ApprovingGroup = 0 , bool BalanceItem = false)
        {
            DataSet dsToReturn = null;
            string appendedFilter1 = string.Empty;
            string appendedFilter2 = string.Empty;
            string appendedFilter3 = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];

                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);

                if (requisitionNoFrom.HasValue && requisitionNoTo.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo BETWEEN @RequisitionNoFrom AND @RequisitionNoTo ";
                }
                else if (requisitionNoFrom.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo >= @RequisitionNoFrom ";
                }
                else if (requisitionNoTo.HasValue)
                {
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo <= @RequisitionNoTo ";
                }

                sqlParams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (requisitionNoFrom.HasValue && requisitionNoTo.HasValue)
                {
                    sb.Append("and Inv_StockTransactionHeader.DocumentNo BETWEEN " + requisitionNoFrom.Value + " AND " + requisitionNoTo.Value + " ");

                }
                else if (requisitionNoFrom.HasValue)
                {
                    sb.Append(" and Inv_StockTransactionHeader.DocumentNo >=" + requisitionNoFrom.Value + " ");

                }
                else if (requisitionNoTo.HasValue)
                {

                    sb.Append(" and  Inv_StockTransactionHeader.DocumentNo <=" + requisitionNoTo.Value + " ");

                }

                if (itemID.HasValue)
                {

                    sb1.Append(" AND tab.ItemID = " + itemID);
                }

                if (ApprovingGroup > 0)
                {
                    sb1.Append(" And ItemAlias.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                }

                if (BalanceItem)
                {
                    sb2.Append("  where Tab1.Balance > 0");
                }

                if (DateFrom != null && DateTo != null)
                {

                    appendedFilter3 = " and Inv_StockTransactionHeader.DocumentDate BETWEEN '" + DateFrom.ToString() + "' and '" + DateTo.ToString() + "' ";

                }

                if (Filter != "")
                    appendedFilter3 += " AND Inv_StockTransactionHeader.StockTransactionID = " + Filter;

                if (!string.IsNullOrEmpty(appendedFilter1))
                {
                    appendedFilter2 = appendedFilter1;
                    appendedFilter1 = " WHERE " + appendedFilter1;
                    appendedFilter2 = " AND " + appendedFilter2;
                    //appendedFilter1 = " WHERE " + appendedFilter1;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DemandStatus, appendedFilter1, appendedFilter2, appendedFilter3, sb, sb1 , sb2), sqlParams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet DemandStatusItemWise_Report(int companyID, int periodID, Nullable<int> itemID, int? DepartmentId, Nullable<int> requisitionNoFrom, Nullable<int> requisitionNoTo,
            string DateFrom = null, string DateTo = null, string Filter = "", int CurrentFiscalID = 0, int ApprovingGroup = 0, bool BalanceItem = false)
        {
            DataSet dsToReturn = null;
            string appendedFilter1 = string.Empty;
            string appendedFilter2 = string.Empty;
            string appendedFilter3 = string.Empty;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();


            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];

                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);

                if (requisitionNoFrom.HasValue && requisitionNoTo.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo BETWEEN @RequisitionNoFrom AND @RequisitionNoTo ";
                }
                else if (requisitionNoFrom.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo >= @RequisitionNoFrom ";
                }
                else if (requisitionNoTo.HasValue)
                {
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo <= @RequisitionNoTo ";
                }

                sqlParams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (requisitionNoFrom.HasValue && requisitionNoTo.HasValue)
                {
                    sb.Append("and Inv_StockTransactionHeader.DocumentNo BETWEEN " + requisitionNoFrom.Value + " AND " + requisitionNoTo.Value + " ");

                }
                else if (requisitionNoFrom.HasValue)
                {
                    sb.Append(" and Inv_StockTransactionHeader.DocumentNo >=" + requisitionNoFrom.Value + " ");

                }
                else if (requisitionNoTo.HasValue)
                {

                    sb.Append(" and  Inv_StockTransactionHeader.DocumentNo <=" + requisitionNoTo.Value + " ");

                }

                if (DepartmentId > 0 && DepartmentId != -1)
                {
                    sb.Append(" and  Inv_StockTransactionHeader.DepartmentID =" + DepartmentId.Value + " ");
                }

                if (itemID.HasValue)
                {

                    sb1.Append(" AND tab.ItemID = " + itemID);
                }

                if (ApprovingGroup > 0)
                {
                    sb1.Append(" And ItemAlias.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                }

                if (BalanceItem)
                {
                    sb2.Append("  where Tab1.Balance > 0");
                }

                if (DateFrom != null && DateTo != null)
                {

                    appendedFilter3 = " and Inv_StockTransactionHeader.DocumentDate BETWEEN '" + DateFrom.ToString() + "' and '" + DateTo.ToString() + "' ";

                }

                if (Filter != "")
                    appendedFilter3 += " AND Inv_StockTransactionHeader.StockTransactionID = " + Filter;

                if (!string.IsNullOrEmpty(appendedFilter1))
                {
                    appendedFilter2 = appendedFilter1;
                    appendedFilter1 = " WHERE " + appendedFilter1;
                    appendedFilter2 = " AND " + appendedFilter2;
                    //appendedFilter1 = " WHERE " + appendedFilter1;
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DemandStatusItemWise, appendedFilter1, appendedFilter2, appendedFilter3, sb, sb1, sb2), sqlParams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet DemandStatus_Vehicle_Report(int companyID, int periodID, Nullable<int> requisitionNoFrom, Nullable<int> requisitionNoTo, string DateFrom = null, string DateTo = null, int DocTypeID = 0, string filter = "", short? _documentGroup = null, int CurrentFiscalID = 0)
        {
            DataSet dsToReturn = null;
            string appendedFilter1 = string.Empty;
            string appendedFilter2 = string.Empty;
            string appendedFilter3 = string.Empty;
            string appendedFilter4 = string.Empty;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[5];

                sqlParams[0] = new SqlParameter("@CompanyID", companyID);
                sqlParams[1] = new SqlParameter("@PeriodID", periodID);

                if (requisitionNoFrom.HasValue && requisitionNoTo.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo BETWEEN @RequisitionNoFrom AND @RequisitionNoTo ";
                }
                else if (requisitionNoFrom.HasValue)
                {
                    sqlParams[2] = new SqlParameter("@RequisitionNoFrom", requisitionNoFrom.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo >= @RequisitionNoFrom ";
                }
                else if (requisitionNoTo.HasValue)
                {
                    sqlParams[3] = new SqlParameter("@RequisitionNoTo", requisitionNoTo.Value);
                    appendedFilter1 = " Inv_StockTransactionHeader.DocumentNo <= @RequisitionNoTo ";
                }
                if (DateFrom != null && DateTo != null)
                {

                    appendedFilter3 = " and Inv_StockTransactionHeader.DocumentDate BETWEEN '" + DateFrom.ToString() + "' and '" + DateTo.ToString() + "' ";

                }
                if (DocTypeID > 0)
                    appendedFilter3 += " and Inv_StockTransactionHeader.DocumentTypeID = " + DocTypeID;

                if (filter != "")
                    appendedFilter3 += " AND Inv_StockTransactionHeader.StockTransactionID = " + filter;

                sqlParams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                //if (_documentGroup.HasValue)
                //{
                //    appendedFilter1 += " and SET_DocumentType.GroupID = " + _documentGroup.Value.ToString();

                //}

                if (!string.IsNullOrEmpty(appendedFilter1))
                {
                    appendedFilter2 = appendedFilter1;
                    appendedFilter1 = " WHERE " + appendedFilter1;
                    appendedFilter2 = " AND " + appendedFilter2;
                    //appendedFilter1 = " WHERE " + appendedFilter1;
                }

                //CHECK Document Group and If All Type Of Documents

                if (_documentGroup.HasValue && _documentGroup != 3)
                {
                    appendedFilter4 += " and SET_DocumentType.GroupID = " + _documentGroup.Value.ToString();
                }


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DemandStatus_Vehicle, appendedFilter1, appendedFilter2, appendedFilter3, appendedFilter4), sqlParams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static int GetNewTransactionDetailID(int CompanyID, int PeriodID, SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_MaxId_ByCompanyPeriod, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_Get_MaxId_ByCompanyPeriod, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetDataByTransactionHeaderID(int stockTransactionID, int companyID, int periodID, bool status = true,
            SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Select_ByTransactionHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ByTransactionHeaderID, sqlparams);



                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Select_ByTransactionHeaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ByTransactionHeaderID, sqlparams);

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

        public static DataTable GetDataBySourceHeaderID(int companyID, int periodID, long sourceHeaderID, int documentTypeID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_BySourceHeaderID, sqlparams);

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


        public static bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, long transactionDetailID,
            int CompanyID, int PeriodId)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", transactionDetailID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);
                sqlparams[5] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[6] = new SqlParameter("@PeriodID", PeriodId);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByKey_CompanyPeriod, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByKey_CompanyPeriod, sqlparams) > 0)
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

        public bool UpdateDataByKeyAccountGINCode(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                StringBuilder sb = new StringBuilder();

                //sb.Append(",[TransactionCost] = " + f_TransactionCost + "");


                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", f_TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", f_StockTransactionID);

                if (!string.IsNullOrEmpty(f_AccountGINCode))
                    sqlparams[4] = new SqlParameter("@AccountGINCode", f_AccountGINCode);
                else
                    sqlparams[4] = new SqlParameter("@AccountGINCode", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (!string.IsNullOrEmpty(f_TransactionCost.ToString()))
                    sqlparams[8] = new SqlParameter("@TransactionCost", f_TransactionCost);
                else
                    sqlparams[8] = new SqlParameter("@TransactionCost", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_AccountGINCode, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_AccountGINCode, sqlparams) > 0)
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

        // Al-Haj
        public bool UpdateDataByKeyTransactionQuantity(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", f_TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", f_StockTransactionID);

                if (!string.IsNullOrEmpty(f_AccountGINCode))
                    sqlparams[4] = new SqlParameter("@AccountGINCode", f_AccountGINCode);
                else
                    sqlparams[4] = new SqlParameter("@AccountGINCode", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[5] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[5] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[7] = new SqlParameter("@UpdatedByValue", DBNull.Value);



                sqlparams[8] = new SqlParameter("@TransactionCost", f_TransactionCost);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_TransactionQuantity, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_TransactionQuantity, sqlparams) > 0)
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

        public bool UpdateDataByKeyTransactionalDetail(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[10];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", f_TransactionDetailID);
                sqlparams[1] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[2] = new SqlParameter("@PeriodID", f_PeriodID);
                sqlparams[3] = new SqlParameter("@StockTransactionID", f_StockTransactionID);

                if (f_TransactionQuantity.HasValue)
                    sqlparams[4] = new SqlParameter("@TransactionQuantity", f_TransactionQuantity);
                else
                    sqlparams[4] = new SqlParameter("@TransactionQuantity", DBNull.Value);

                if (f_RejectedQuantity.HasValue)
                    sqlparams[5] = new SqlParameter("@RejectedQuantity", f_RejectedQuantity);
                else
                    sqlparams[5] = new SqlParameter("@RejectedQuantity", DBNull.Value);

                if (f_UpdatedBy.HasValue)
                    sqlparams[6] = new SqlParameter("@UpdatedBy", f_UpdatedBy.Value);
                else
                    sqlparams[6] = new SqlParameter("@UpdatedBy", DBNull.Value);

                if (f_UpdateDate.HasValue)
                    sqlparams[7] = new SqlParameter("@UpdateDate", f_UpdateDate.Value);
                else
                    sqlparams[7] = new SqlParameter("@UpdateDate", DBNull.Value);

                if (!string.IsNullOrEmpty(f_UpdatedByValue))
                    sqlparams[8] = new SqlParameter("@UpdatedByValue", f_UpdatedByValue);
                else
                    sqlparams[8] = new SqlParameter("@UpdatedByValue", DBNull.Value);

                if (f_IsInspected.HasValue)
                    sqlparams[9] = new SqlParameter("@IsInspected", f_IsInspected);
                else
                    sqlparams[9] = new SqlParameter("@IsInspected", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Update_By_PrimaryKey_TransactionalDetailCode, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_By_PrimaryKey_TransactionalDetailCode, sqlparams) > 0)
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

        public static bool UpdateStockTransactionDetail(List<INV_StockTransactionDetail> colUpdateStockTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");
            try
            {
                foreach (INV_StockTransactionDetail stdUpdate in colUpdateStockTranDetail)
                    stdUpdate.UpdateDataByKeyTransactionalDetail(sqltrans);
                result = true;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public static decimal Get_LastUnitPrice(int itemid, int companyid, int periodid)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemid);
                sqlparams[1] = new SqlParameter("@CompanyId", companyid);
                sqlparams[2] = new SqlParameter("@PeriodID", periodid);
                dt = SqlHelper.ExecuteDataset(CommandType.Text, query_GetLastpriceOfItem, sqlparams).Tables[0];
                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static DataTable GetDataByKeyWithOtherTablesForDirectDispatch(int dispatchID, int companyID, int periodID, SqlConnection objConnection = null,int CurrentFiscalID=0)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DispatchID", dispatchID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataByKeyWithOtherTablesForDirectDispatch, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataByKeyWithOtherTablesForDirectDispatch, sqlparams);

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

        public static DataTable GetFifoItemForGRN(int itemId, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ItemID", itemId);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetFifoItemForGRN, sqlparams);

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

        public static decimal GetAvailableStockForFIFO(int companyID, int periodID, int itemID, SqlConnection objConnection = null)
        {
            decimal toReturn = 0;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAvailableStockForFIFO, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetAvailableStockForFIFO, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows != null && ds.Tables[0].Rows.Count > 0)
                    toReturn = Convert.ToDecimal(ds.Tables[0].Rows[0]["AvailableStock"]);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return toReturn;
        }

        public static DataTable Get_PendingRequisition(int companyID, int periodID, Nullable<DateTime> Date_From = null,
            Nullable<DateTime> Date_To = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                StringBuilder sb = new StringBuilder();
                sb.Append(" AND INV_StockTransactionHeader.CompanyID = @CompanyID AND INV_StockTransactionHeader.CompanyID = @PeriodID");
                if (Date_From != null && Date_To != null)
                {
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate between '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' and '"
                        + ((DateTime)Date_To).ToString("MM-dd-yyyy") + "' ");
                }
                else if (Date_From != null && Date_To == null)
                    sb.Append(" AND INV_StockTransactionHeader.DocumentDate = '" + ((DateTime)Date_From).ToString("MM-dd-yyyy") + "' ");
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetRequisition + sb.ToString()), sqlparams);

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


        public static DataTable Get_RequisitionSummary(int companyID, int periodID, int requisitionNo, Nullable<DateTime> Date_From = null,
         Nullable<DateTime> Date_To = null, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                if (Date_From != null && Date_To != null)
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (requisitionNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo=" + requisitionNo);

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetRequisitionSummary + sb.ToString()), sqlparams);

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


        public static DataTable Get_StockInventoryMovement(int companyID, int periodID, int requisitionNo, Nullable<DateTime> Date_From = null,
     Nullable<DateTime> Date_To = null, int categoryId = 0, int itemId = 0, short? documentGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);


                if (Date_From != null && Date_To != null)
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                if (requisitionNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo=" + requisitionNo);


                if (categoryId > 0)
                    sb.Append(" and inv_item.ItemTypeID=" + categoryId);

                if (itemId > 0)
                    sb.Append(" and inv_item.ItemID=" + itemId);


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetStockInventoryMovement, sb.ToString()), sqlparams);

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

        public static DataTable Get_StockInventoryItemWiseMovement(int companyID, int periodID, int requisitionNo, Nullable<DateTime> Date_From = null,
Nullable<DateTime> Date_To = null, int itemCategoryId = 0, int itemId = 0, short? documentGroupID = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                if (Date_From != null && Date_To != null)
                    sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                if (requisitionNo > 0)
                    sb.Append(" and INV_StockTransactionHeader.DocumentNo=" + requisitionNo);

                if (itemCategoryId > 0)
                    sb.Append(" and inv_item.ItemTypeID =" + itemCategoryId);

                if (itemId > 0)
                    sb.Append(" and inv_item.ItemID=" + itemId);


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetStockInventoryItemWiseMovement + sb.ToString()), sqlparams);

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

        public static bool UpdateStockDetailByKey(long transactionDetailID, int stockTransactionID, Guid updatedBy, DateTime updateDate, string updatedByValue,
            int companyID, int periodID, string comments, Nullable<bool> isDone = false, SqlTransaction objTransaction = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", transactionDetailID);
                sqlparams[1] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[2] = new SqlParameter("@IsDone", isDone);
                sqlparams[3] = new SqlParameter("@Comments", comments);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);
                sqlparams[7] = new SqlParameter("@CompanyID", companyID);
                sqlparams[8] = new SqlParameter("@PeriodID", periodID);

                //if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateFlagAndComments, sqlparams) > 0)
                //  result = true;

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateFlagAndComments, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateFlagAndComments, sqlparams) > 0)
                        result = true;
                }

                result = true;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }

        public static DataTable GetDataByKeyForTransferout(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataByKeyForTransferout, sqlparams);

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

        public static DataTable GetData_SalesTaxInvoicePartsReport(int companyID, int periodID, int? itemCategoryID, string InvoiceNumber, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[6];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [Sal_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                if (!string.IsNullOrEmpty(InvoiceNumber))
                {
                    additionalparameter = additionalparameter + " AND [SAL_InvoiceHeader].[InvoiceNumber] = " + InvoiceNumber;
                }

                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SalesTaxInvoicePartsReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDeliveryChallanWithAmountReport(int companyID, int periodID, string DCNumber, int? itemCategoryID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null)
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [Sal_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                if (!string.IsNullOrEmpty(DCNumber))
                {
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchNo] = " + DCNumber;
                }

                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " And Set_DocumentType.DocumentTypeID = " + doctypeid;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_DeliveryChallanWithAmountReport + additionalparameter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDeliveryChallanWithQuantityReport(int companyID, int periodID, string DCNumber, int? itemCategoryID, Nullable<DateTime> DateFrom = null, Nullable<DateTime> DateTo = null, Nullable<int> doctypeid = null, string filter = "")
        {
            DataTable dtresult = null;
            string additionalparameter = string.Empty;

            try
            {

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);

                if (itemCategoryID.HasValue)
                    additionalparameter = additionalparameter + " AND [INV_Item].[ItemTypeID] = " + itemCategoryID;

                if (DateFrom != null && DateTo != null)
                    additionalparameter = additionalparameter + " AND [Sal_DispatchHeader].[DispatchDate] BETWEEN '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "' AND '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";
                else if (DateFrom != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] >= '" + ((DateTime)DateFrom).ToString("yyyy-MM-dd") + "'";
                else if (DateTo != null)
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchDate] <= '" + ((DateTime)DateTo).ToString("yyyy-MM-dd") + "'";

                if (!string.IsNullOrEmpty(DCNumber))
                {
                    additionalparameter = additionalparameter + " AND [SAL_DispatchHeader].[DispatchNo] = " + DCNumber;
                }

                if (doctypeid.HasValue)
                    additionalparameter = additionalparameter + " And Set_DocumentType.DocumentTypeID = " + doctypeid;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_DeliveryChallanWithQuantityReport + additionalparameter + filter, sqlParms);

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable SalesInvoicePartsReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, string invoiceNo, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> doctypeid = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (categoryID > 0)
                {
                    AddtionalParameters = "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (!string.IsNullOrEmpty(invoiceNo))
                {

                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " And Set_DocumentType.DocumentTypeID = " + doctypeid;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SalesInvoicePartsReport + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SalesInvoicePartsReport + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataByKeyTable_ForRequistionFromPopUp(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);
                // Query_Select_ForGIN
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDataByKeyTable_ForRequistionFromPopUp, sqlparams);

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

        public static DataTable GetDataForGoodsReceiveNotewithDollar(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            int grnNumberTo, Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0,
            Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, short? documentGroupID = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);


                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (reqfrom != null && reqto != null)
                {
                    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsRecieveDollarRate, AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsRecieveDollarRate, AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGoodsReceiveNote(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
            Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
            Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, int? DocumentStatus = null, short? documentGroupID = null, int? ApprovingGroupID = -1
            , int LocationID =-1,int FiscalID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (reqfrom != null && reqto != null)
                {
                    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                }
                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus;
                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovingGroupID.Value > 0)
                    AddtionalParameters += " and [INV_StockTransactionDetail].stocktransactionid in (select [INV_StockTransactionDetail].StockTransactionID from [INV_Item],[INV_StockTransactionDetail] where [INV_Item].[ItemID] = [INV_StockTransactionDetail].[ItemID] AND [INV_Item].[Status] = 1 and Inv_item.ApprovalItemGroupID = " + ApprovingGroupID + " )";

                if (LocationID != -1)
                { 
                    AddtionalParameters += " and INV_StockTransactionHeader.locationID= " + LocationID.ToString();
                }

                if(FiscalID > 0)
                    AddtionalParameters += " and INV_StockTransactionHeader.FiscalID= " + FiscalID;


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForGoodsReceiveNoteHSM(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
            Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
            Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, int? DocumentStatus = null, short? documentGroupID = null, int? ApprovingGroupID = -1
            , int LocationID = -1, int FiscalID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (reqfrom != null && reqto != null)
                {
                    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                }
                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus;
                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovingGroupID.Value > 0)
                    AddtionalParameters += " and [INV_StockTransactionDetail].stocktransactionid in (select [INV_StockTransactionDetail].StockTransactionID from [INV_Item],[INV_StockTransactionDetail] where [INV_Item].[ItemID] = [INV_StockTransactionDetail].[ItemID] AND [INV_Item].[Status] = 1 and Inv_item.ApprovalItemGroupID = " + ApprovingGroupID + " )";

                if (LocationID != -1)
                {
                    AddtionalParameters += " and INV_StockTransactionHeader.locationID= " + LocationID.ToString();
                }

                if (FiscalID > 0)
                    AddtionalParameters += " and INV_StockTransactionHeader.FiscalID= " + FiscalID;


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNoteHSM + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNoteHSM + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGeneratorConsumptionSummary(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorConsumptionSummary + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorConsumptionSummary + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGeneratorConsumption(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorConsumption + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorConsumption + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGeneratorDetail(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GeneratorDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForElectricityConsumption(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ElectricityConsumption + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ElectricityConsumption + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForCarFinancingScheme(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", (DateFrom));
                sqlParms[3] = new SqlParameter("@ToDate", (DateTo));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CarFinancingScheme + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CarFinancingScheme + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetDataForElectricityConsumptionDetail(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID,int fiscalid, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ElectricityConsumptionDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ElectricityConsumptionDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForBMRBudgetDetailReport(DateTime? DateFrom, DateTime? DateTo, int companyID, string companies, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                //sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[0] = new SqlParameter("@PeriodID", periodID);
                sqlParms[1] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[2] = new SqlParameter("@ToDate", DateTo);
                sqlParms[3] = new SqlParameter("@CurrentFiscalID", fiscalid);

                companies = "(" + companies + ")";
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRDetailReport + AddtionalParameters).Replace("@CompanyID", companies), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRDetailReport + AddtionalParameters).Replace("@CompanyID", companies), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetDataForSummaryOfMajorAdditionsBMRReport(DateTime? DateTo, int companyID, string companies, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                //sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[0] = new SqlParameter("@PeriodID", periodID);
                sqlParms[1] = new SqlParameter("@ToDate", DateTo);
                sqlParms[2] = new SqlParameter("@CurrentFiscalID", fiscalid);

                companies = "(" + companies + ")";
                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SummaryOfMajorAdditionsBMRReport + AddtionalParameters).Replace("@CompanyID", companies), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SummaryOfMajorAdditionsBMRReport + AddtionalParameters).Replace("@CompanyID", companies), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForBMRBudgetDetail(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRDetail + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForBMRBudgetSummary(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRSummary + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRSummary + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForBMRBudgetLocal(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRBudgetLocal + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRBudgetLocal + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForBMRBudgetImported(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;
                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRBudgetImported + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BMRBudgetImported + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForMessExpenses(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int LocationID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;
                StringBuilder sb = new StringBuilder();
                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));

                if (LocationID > 0)
                    sb.Append(" And Set_MessTransaction.locationID = " + LocationID);

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MessExpenses, sb.ToString() + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MessExpenses, sb.ToString() + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForMajorAdditionsBMRSummary(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MajorAdditionsSummary + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MajorAdditionsSummary + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForAdvancesToSupplierAging(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_AdvancesToSupplierAging + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_AdvancesToSupplierAging + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForPayableToSupplierAging(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PayableToSupplierAging + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PayableToSupplierAging + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForMajorAdditionsBMRDetail(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MajorAdditionsDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MajorAdditionsDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }
        public static DataSet GetDataForMessExpensesSummary(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int fiscalid, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));
                sqlParms[4] = new SqlParameter("@CurrentFiscalID", fiscalid);


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MessExpensesSummary + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MessExpensesSummary + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForLimeSlaker(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_LimeSlaker + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_LimeSlaker + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetDataForCaneCarrierCleaningExpensesReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneCarrier + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneCarrier + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetDataForCaneUnloadingExpensesReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloading + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloading + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetDataForSugarloadingExpensesReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataSet dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Sugarloading + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_Sugarloading + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds;

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForCaneCarrierCleaningExpensesDetailReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneCarrierDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneCarrierDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForCaneUnloadingExpensesDetailReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloadingDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CaneUnloadingDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForSugarloadingExpensesDetailReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SugarloadingDetail + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SugarloadingDetail + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForBankFacilities(DateTime? DateFrom, DateTime? DateTo, string companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", ((DateTime)DateFrom).ToString("yyyy-MM"));
                sqlParms[3] = new SqlParameter("@ToDate", ((DateTime)DateTo).ToString("yyyy-MM"));

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BankFacilities + AddtionalParameters).Replace("@CompanyID", companyID), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BankFacilities + AddtionalParameters).Replace("@CompanyID", companyID), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGoodsReceiveNote1(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
           Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
           Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, int? DocumentStatus = null, short? documentGroupID = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (reqfrom != null && reqto != null)
                {
                    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                }
                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus;
                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote1 + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote1 + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

      
        public static DataTable GetDataForGoodsReceiveNote_AutoM(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1,
            Nullable<int> doctypeid = null, short? documentGroupID = null, Nullable<int> locationID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString() + " ";
                }

                if (locationID > 0)
                {
                    AddtionalParameters += " And [SET_Location].[LocationID] = " + locationID;
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;


                if (documentGroupID.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForStockRequistionReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1,
            Nullable<int> doctypeid = null, short? documentGroupID = null, Nullable<int> locationID = 0, int? DepartmentID = null, Nullable<int> DemandType = null,Nullable<int> SeasonType = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString() + " ";
                }

                if (locationID > 0)
                {
                    AddtionalParameters += " And [SET_Location].[LocationID] = " + locationID;
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;


                if (documentGroupID.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (DepartmentID > 0)
                    AddtionalParameters += " And SET_Department.DepartmentID = " + DepartmentID;

                if (DemandType > 0)
                    AddtionalParameters += " AND INV_StockTransactionHeader.DemandType = " + DemandType;


                if (SeasonType > 0)
                    AddtionalParameters += " AND INV_StockTransactionHeader.SeasonType = " + SeasonType;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static DataTable GetDataForStockRequistionReportNew(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> item = null, Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1,
            Nullable<int> doctypeid = null, short? documentGroupID = null, Nullable<int> locationID = 0, int? DepartmentID = null, Nullable<int> DemandType = null, Nullable<int> SeasonType = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (item > 0)
                {
                    AddtionalParameters += "AND [INV_Item].[ItemID] = " + item.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString() + " ";
                }

                if (locationID > 0)
                {
                    AddtionalParameters += " And [SET_Location].[LocationID] = " + locationID;
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;


                if (documentGroupID.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (DepartmentID > 0)
                    AddtionalParameters += " And SET_Department.DepartmentID = " + DepartmentID;

                if (DemandType > 0)
                    AddtionalParameters += " AND INV_StockTransactionHeader.DemandType = " + DemandType;


                if (SeasonType > 0)
                    AddtionalParameters += " AND INV_StockTransactionHeader.SeasonType = " + SeasonType;

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGoodsReceiveNote_Or(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1,
            Nullable<int> doctypeid = null, short? documentGroupID = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 5";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (documentGroupID.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNote + AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataSet GetItemTransactionReport(int companyID, int periodID, int categoryID, string itemID, int CostCenterID, int DepartmentID, int locationID, Nullable<DateTime> Date_from = null,
            Nullable<DateTime> Date_To = null, short? documentGroupID = null, int? ApprovingGroupID = null,int? documentTypeId=null, Nullable<int> HeadingCostCenterID = null)
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_from);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //   sqlparams[4] = new SqlParameter("@Type", type);
                sqlparams[5] = new SqlParameter("@CategoryID", categoryID);


                if (!string.IsNullOrEmpty(itemID))
                    sqlparams[6] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[6] = new SqlParameter("@ItemID", DBNull.Value);

                if (locationID > 0)
                    sqlparams[7] = new SqlParameter("@LocationID", locationID);
                else
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);

                if (categoryID > 0)
                    sb.Append(" AND INV_SET_Category.CategoryID = @CategoryID");

                if (HeadingCostCenterID > 0)
                    sb.Append(" AND (isnull(SCCHeaderCostCenter.CostCenterID,HeaderCostCenter.CostCenterID) = " + HeadingCostCenterID + " OR " + HeadingCostCenterID + " = 0)  ");
                

                if (CostCenterID > 0)
                    sb.Append(" AND (isnull(SCC.CostCenterID,Set_CostCenter.CostCenterID) = " + CostCenterID + " OR " + CostCenterID + " = 0)  ");
                

                if (DepartmentID > 0)
                    sb.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);

                string additionalFilter = "";

                if (documentGroupID.HasValue)
                    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (ApprovingGroupID.HasValue && ApprovingGroupID > 0)
                    additionalFilter = additionalFilter + " AND INV_Item.ApprovalItemGroupID = " + ApprovingGroupID;
                if (documentTypeId.HasValue && documentTypeId > 0)
                    additionalFilter = additionalFilter + " And INV_StockTransactionHeader.DocumentTypeID= " + documentTypeId;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetItemTransactionReport, additionalFilter, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet GetItemTransactionWithOutGLImpactReport(int companyID, int periodID, int categoryID, string itemID, int locationID,
            Nullable<DateTime> Date_from = null, Nullable<DateTime> Date_To = null, string type = "")
        {
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_from);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                sqlparams[4] = new SqlParameter("@Type", type);
                sqlparams[5] = new SqlParameter("@CategoryID", categoryID);

                if (!string.IsNullOrEmpty(itemID))
                    sqlparams[6] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[6] = new SqlParameter("@ItemID", DBNull.Value);

                if (locationID > 0)
                    sqlparams[7] = new SqlParameter("@LocationID", locationID);
                else
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetItemTransactionWithOutImpactReport, sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable GetItemOpeningBalanceRate(int companyID, int periodID, int itemID, Nullable<DateTime> DateFrom = null, int? _locationId = null)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[5];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@Itemid", itemID);
                sqlParms[3] = new SqlParameter("@Date", ((DateTime)DateFrom).ToString("yyyy-MM-dd"));
                sqlParms[4] = new SqlParameter("@locationid", _locationId.Value);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetItemOpeningBalanceRate, sqlParms);
                }


                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForGoodsReceiveNoteSummary(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, SqlConnection objConnection = null, Nullable<int> categoryID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                if (categoryID > 0)
                {
                    AddtionalParameters = "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters = "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString();
                }

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNoteSummary + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GoodsReceiveNoteSummary + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable CheckGRNStatusAgainstPO(int poID, short poHeaderID, int itemID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@poID", poID);
                sqlparams[1] = new SqlParameter("@poHeaderID", poHeaderID);
                sqlparams[2] = new SqlParameter("@itemID", itemID);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                sqlparams[4] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_CheckGRNStatus, sqlparams);

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

        public static DataTable Get_Data_ForFixedAsset(int companyID, int periodID, int stockTransactionID)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", stockTransactionID);

                string qry_Select_ForFixedAsset = @"
SELECT Distinct INV_StockTransactionDetail.TransactionDetailID, INV_Item.Description, INV_Item.InventoryItemStatusCode, 
    Abs(INV_StockTransactionDetail.TransactionQuantity) As TransactionQuantity 
FROM INV_StockTransactionDetail 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
    Inner Join AP_PurchaseOrderHeader On AP_PurchaseOrderHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        And AP_PurchaseOrderHeader.PeriodID = INV_StockTransactionDetail.PeriodID And AP_PurchaseOrderHeader.POHeaderID = INV_StockTransactionDetail.SourceHeaderID 
        And AP_PurchaseOrderHeader.TypeCode = INV_StockTransactionDetail.SourceDocumentTypeID 
        And AP_PurchaseOrderHeader.DocumentNumber = INV_StockTransactionDetail.SourceCode And AP_PurchaseOrderHeader.Status = 1 
        And AP_PurchaseOrderHeader.StatusCode = 1 
    Inner Join AP_PurchaseOrderDetail On AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        And AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
        and AP_PurchaseOrderDetail.PODetailID = INV_StockTransactionDetail.SourceLineID and AP_PurchaseOrderDetail.Status = 1 
        And IsNull(AP_PurchaseOrderDetail.CapitalExpenseFlag, 0) = 1 
where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID 
    And INV_StockTransactionDetail.StockTransactionID = @StockTransactionID and INV_StockTransactionDetail.Status = 1 
    And IsNull(INV_StockTransactionDetail.IsIncurred, 0) = 0 ";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, qry_Select_ForFixedAsset, sqlparams);

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



        public static DataTable PendingGRNAsOf_Data(int CompanyID, int PeriodID, DateTime? DateFrom, DateTime? DateTo, int? supplierfrom, int? supplierto, short? DocumentGroupID, Nullable<int> documentType = null, int CurrentFiscalID = 0, int? DepartmentID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", DateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", DateTo);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (supplierfrom != null && supplierto != null)
                {
                    sb.Append("  and  AR_SET_Customer.CustomerID  between " + supplierfrom + " and " + supplierto + " ");
                }
                else
                {
                    sb.Append("");
                }

                if (DocumentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + DocumentGroupID.Value.ToString() + " ");
                if (documentType.HasValue)
                    sb.Append(" AND Set_DocumentType.DocumentTypeID = " + documentType.Value.ToString() + " ");

                if (DepartmentID > 0)
                {
                    sb.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                }
                dtToReturn = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PendingGRNforSuppliersBills, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GETData_For_PRRequisition_Detail(int documentNo, int companyID, int periodID, string Where, bool status = true,
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (Where != "")
                {
                    Filter = Where;
                }
                if (documentNo > 0)
                {
                    Filter += " and INV_StockTransactionHeader.DocumentNo = " + documentNo;
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockReq_PRDetails, Filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockReq_PRDetails, Filter), sqlparams);

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


        public static DataTable ComparativeStatment(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate, int? itemCategoryID, int? itemID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            StringBuilder parameters = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                DataSet ds = null;

                if (itemID.HasValue)
                    parameters.Append(" AND INV_Item.ItemID =" + itemID);

                if (itemCategoryID.HasValue)
                    parameters.Append(" AND INV_Item.ItemTypeID =" + itemCategoryID);
 
                string Query = @FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetComparativeStatment;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, parameters.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetComparativeStatment, parameters.ToString()), sqlparams);

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

        public static DataTable MonthWiseConsumptionReportData(string companyID, int PeriodID, DateTime FromDate, DateTime ToDate, string CatalogID, int? itemCategoryID, int? itemID,int? costCenterID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            StringBuilder parameters = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                DataSet ds = null;

                if (itemID.HasValue)
                    parameters.Append(" AND INV_Item.ItemID =" + itemID);

                //if (itemCategoryID.HasValue)
                //    parameters.Append(" AND INV_Item.ItemTypeID =" + itemCategoryID);

                if (CatalogID != null)
                    parameters.Append(" AND INV_Item.ItemCatalogGroupID in (" + CatalogID+ ")");

                if (costCenterID.HasValue)
                    parameters.Append(" AND Set_CostCenter.CostCenterID = " + costCenterID.Value.ToString() + " ");

                companyID = "(" + companyID + ")";
           //     string Query = @FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetConsumptionReportData;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetConsumptionReportData.ToString().Replace("@CompanyID", companyID), parameters.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetConsumptionReportData.ToString().Replace("@CompanyID", companyID), parameters.ToString()), sqlparams);

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

        public static object StockAgingReportData(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate, int? itemCategoryID, int? itemID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            StringBuilder parameters = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                DataSet ds = null;

                if (itemID.HasValue)
                    parameters.Append(" AND INV_Item.ItemID =" + itemID);

                if (itemCategoryID.HasValue)
                    parameters.Append(" AND INV_Item.ItemTypeID =" + itemCategoryID);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetComparativeStatment, parameters.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetComparativeStatment, parameters.ToString()), sqlparams);

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

        public static DataTable GetDataByTransactionHeaderIDForPurchaseReturn(int stockTransactionID, int companyID, int periodID, bool status = true,
           SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;
                #region
                const string Query_SelectPurchaseReturn = @"Select 
INV_StockTransactionDetail.Comments, INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.PeriodID, INV_StockTransactionDetail.TransactionDetailID, 
            INV_StockTransactionDetail.StockTransactionID, SET_DocumentType.DefaultAccountCode, INV_StockTransactionDetail.ItemID, INV_Item.Description AS ItemDescription, 
            INV_StockTransactionDetail.TransactionUOMID as ItemUnitOfMeasureID, SET_UnitofMeasure.UOMName, 
            
ISNull(INV_StockTransactionDetail.TransactionQuantity,0)*-1 as QuantityReturn ,  INV_StockTransactionDetail.SerialNo AS SerialNumber,
   INV_StockTransactionDetail.TransactionCost as UnitPrice,
 INV_StockTransactionDetail.SourceHeaderID, INV_StockTransactionDetail.SourceLineID,
  INV_StockTransactionDetail.SourceCode,'' as Action
 from INV_StockTransactionHeader
		INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
			AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
		INNER JOIN INV_Item ON INV_StockTransactionDetail.ItemID = INV_Item.ItemID --INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID AND  
		INNER JOIN SET_UnitofMeasure ON INV_StockTransactionDetail.TransactionUOMID = SET_UnitofMeasure.UOMID 
		INNER JOIN SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
			and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
	        
		LEFT JOIN AR_InvoiceDetail On AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
			And AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
			And AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
			And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
	        
			left join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
			and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
			and AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
			
			where
		
			INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID 
    and INV_StockTransactionDetail.StockTransactionID = @StockTransactionID ";
                #endregion

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectPurchaseReturn, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_ByTransactionHeaderID, sqlparams);



                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectPurchaseReturn, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectPurchaseReturn, sqlparams);

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

        public static DataTable Get_StockHeaderDetail_ByKey(int companyID, int periodID, int StockTransactionID, short transferType)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);
                sqlParams[3] = new SqlParameter("@TransferType", transferType);

                #region Query

                string Query_StockHeaderDetail_ByKey = @"Select Distinct
INV_StockTransactionDetail.Comments, INV_StockTransactionDetail.CompanyID, INV_StockTransactionDetail.PeriodID, NULL AS TransactionDetailID, 
            NULL AS StockTransactionID, SET_DocumentType.DefaultAccountCode, INV_StockTransactionDetail.ItemID, INV_Item.Description AS ItemDescription, 
            INV_StockTransactionDetail.TransactionUOMID as ItemUnitOfMeasureID, SET_UnitofMeasure.UOMName, NULL AS AssetsAdditionFlag, 
            INV_Item.ListPricePerUnit AS BaseAmount
  ,(TransactionQuantity - ISNull((Select abs(SUM(TransactionQuantity)) from 
           INV_StockTransactionDetail StockGRND
where INV_StockTransactionDetail.StockTransactionID=StockGRND.SourceHeaderID
AND INV_StockTransactionDetail.TransactionDetailID=StockGRND.SourceLineID
AND INV_StockTransactionDetail.ItemID=StockGRND.ItemID ),0)) as QuantityReturn , 

 INV_StockTransactionDetail.SerialNo AS SerialNumber,INV_StockTransactionDetail.TransactionCost as UnitPrice,
 INV_StockTransactionDetail.StockTransactionID as SourceHeaderID, INV_StockTransactionDetail.TransactionDetailID as SourceLineID
 ,INV_StockTransactionHeader.DocumentNo as SourceCode,'' AS Action,INV_StockTransactionHeader.LocationID
 from INV_StockTransactionHeader
		INNER JOIN INV_StockTransactionDetail ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
			AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
		INNER JOIN INV_Item ON INV_StockTransactionDetail.ItemID = INV_Item.ItemID --INV_StockTransactionDetail.CompanyID = INV_Item.CompanyID AND 
		INNER JOIN SET_UnitofMeasure ON INV_StockTransactionDetail.TransactionUOMID = SET_UnitofMeasure.UOMID 
		INNER JOIN SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
			and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
	        
		LEFT JOIN AR_InvoiceDetail On AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
			And AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID 
			And AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
			And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
	        
			left join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
			and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
			and AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
			
			where
		
			INV_StockTransactionHeader.CompanyID = @CompanyID and INV_StockTransactionHeader.PeriodID = @PeriodID 
    and INV_StockTransactionDetail.StockTransactionID = @StockTransactionID 
    AND (INV_StockTransactionDetail.TransferTransactionLookup <> @TransferType OR INV_StockTransactionDetail.TransferTransactionLookup IS NULL) 
	--AND Isnull(INV_StockTransactionDetail.IssuedQuantity,0) =0
	AND TransactionQuantity > ISNull((Select abs(SUm(TransactionQuantity)) from 
           INV_StockTransactionDetail StockGRND
where INV_StockTransactionDetail.StockTransactionID=StockGRND.SourceHeaderID
AND INV_StockTransactionDetail.TransactionDetailID=StockGRND.SourceLineID
AND INV_StockTransactionDetail.ItemID=StockGRND.ItemID ),0)
";

                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_StockHeaderDetail_ByKey, sqlParams);

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

        public static DataTable Get_StockHeaderDetail_ByKey_ForSakrand(int companyID, int periodID, int StockTransactionID, short transferType)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlParams = new SqlParameter[4];
                sqlParams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlParams[1] = new SqlParameter("@CompanyID", companyID);
                sqlParams[2] = new SqlParameter("@PeriodID", periodID);
                sqlParams[3] = new SqlParameter("@TransferType", transferType);


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockHeaderDetail_ByKey_ForSakrand, sqlParams);

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

        public static DataSet GetDateWiseLedgerReport(int companyID, int periodID, int categoryID, string itemID, int CostCenterID, int DepartmentID, int locationID, Nullable<DateTime> Date_from = null,
           Nullable<DateTime> Date_To = null, short? documentGroupID = null, string ApprovingGroupID = null )
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_from);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);
                //   sqlparams[4] = new SqlParameter("@Type", type);
                sqlparams[4] = new SqlParameter("@CategoryID", categoryID);
                

                if (!string.IsNullOrEmpty(itemID))
                    sqlparams[6] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[6] = new SqlParameter("@ItemID", DBNull.Value);

                if (locationID > 0)
                    sqlparams[7] = new SqlParameter("@LocationID", locationID);
                else
                    sqlparams[7] = new SqlParameter("@LocationID", DBNull.Value);

                if (categoryID > 0)
                    sb.Append(" AND INV_SET_Category.CategoryID = @CategoryID");

                //  if(CostCenterID > 0)
                //    sb.Append(" AND INV_StockTransactionHeader.CostCenterID =" + CostCenterID);


                //if (CostCenterID > 0)
                //    sb.Append(" AND (isnull(SCC.CostCenterID,Set_CostCenter.CostCenterID) = " + CostCenterID + " OR " + CostCenterID + " =0) ");

                if (DepartmentID > 0)
                    sb.Append(" AND INV_StockTransactionHeader.DepartmentID =" + DepartmentID);

                string additionalFilter = "";

                //if (documentGroupID.HasValue)
                //    additionalFilter = " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();
                if (ApprovingGroupID != null)
                    additionalFilter = additionalFilter + " AND INV_Item.ApprovalItemGroupID in (" + ApprovingGroupID + ")";

                //if (locationID > 0)
                //    additionalFilter = additionalFilter + " AND Set_Location.LocationID = " + locationID;

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_getdatewiseledger, additionalFilter, sb.ToString()), sqlparams);

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable GetAllDataWithPendingSR(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
          Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc)
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
                    string query = (FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll_Searching_WithPendingSR).Replace("{wherefilter}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    //ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_SelectAll_Searching_WithPendingSR) + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDailyStockMonitoring_ReportData(int companyID, int periodID, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null
            , SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyId", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);
           
                #region
                const string Query_DailyStockMonitoring = @"select  TAb.ItemID  , TAb.ItemCode , TAb.Item ,TAb.Unit ,SUM(TAB.OpeningBalance) as OpeningBalance , SUM(TAB.QtyReceived) as QtyReceived , ABS(SUM(TAB.QtyIssued)) as QtyIssued 
, isnull(SUM(TAB.OpeningBalance),0) +  isnull(SUM(TAB.QtyReceived),0) -  isnull(ABS(SUM(TAB.QtyIssued)),0) as StockInHandQty
 , TAb.Rate ,isNull(TAb.MinimumOrderQuantity,0) as Minimum ,isNull(TAb.MaximumOrderQuantity,0) as Maximum,isNull(TAb.FixedOrderQuantity,0) as ReOrder
from (
select TAB.ItemID ,TAB.ItemCode ,tab.Item,
TAB.Unit,
sum(TAB.OpeningBalanceQty / isnull(TAB.UnitQuantity,1)) as OpeningBalance
, 0 as QtyReceived , 0 as QtyIssued , TAB.Rate ,TAB.MinimumOrderQuantity,TAB.MaximumOrderQuantity ,TAB.FixedOrderQuantity

from (
SELECT INV_StockTransactionDetail.ItemID, INV_Item.Description as Item ,Sum(case SET_DocumentType.TransactionImpact when 'No Impact' then 0 else INV_StockTransactionDetail.TransactionQuantity End) AS OpeningBalanceQty, 

    ISNULL((Case when INV_StockTransactionDetail.TransactionUOMID = INV_Item.PrimaryUOMCode then 1 
        when INV_StockTransactionDetail.TransactionUOMID = INV_Item.VolumeUOMCode then ISNULL(INV_Item.UnitVolume, 1) 
        when INV_StockTransactionDetail.TransactionUOMID = INV_Item.WeightUOMCode then ISNULL(INV_Item.UnitWeight, 1) end), 1) UnitQuantity
         , Inv_Item.InventoryItemStatusCode as ItemCode
         ,SET_UnitofMeasure.UOMName as Unit 
         , 0 as Rate
         ,INV_Item.MinimumOrderQuantity
         ,INV_Item.MaximumOrderQuantity 
         ,INV_Item.FixedOrderQuantity
       
FROM INV_StockTransactionDetail 
	INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID AND INV_StockTransactionHeader.Status = 1 
        AND INV_StockTransactionHeader.IsActive = 1 
    Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
        And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID --AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_Item.Status = 1 
	INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID --  
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0)
   LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
    LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
    Left Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID
   
WHERE INV_StockTransactionDetail.CompanyID = 1 AND INV_StockTransactionDetail.PeriodID = 1 AND INV_StockTransactionDetail.Status = 1 
    AND INV_StockTransactionDetail.IsActive = 1 
    
  AND convert(date,INV_StockTransactionHeader.DocumentDate) < @FromDate  
    AND INV_StockTransactionHeader.StatusCode = 1 
GROUP BY INV_StockTransactionDetail.ItemID,INV_StockTransactionDetail.TransactionUOMID,INV_Item.PrimaryUOMCode,INV_Item.VolumeUOMCode
,INV_Item.UnitVolume,INV_Item.WeightUOMCode,INV_Item.UnitWeight,INV_Item.Description,INV_StockTransactionDetail.ValueChange
,INV_Item.InventoryItemStatusCode,SET_UnitofMeasure.UOMName ,INV_Item.MinimumOrderQuantity,INV_Item.MaximumOrderQuantity ,INV_Item.FixedOrderQuantity



) AS TAB
group by TAB.ItemID,tab.Item,TAB.ItemCode , TAB.Unit,TAB.Rate ,TAB.MinimumOrderQuantity,TAB.MaximumOrderQuantity ,TAB.FixedOrderQuantity


Union All

Select 
INV_StockTransactionDetail.ItemID  , Inv_Item.InventoryItemStatusCode as ItemCode , INV_Item.Description as Item 
,SET_UnitofMeasure.UOMName as Unit , 0 as Opening , INV_StockTransactionDetail.TransactionQuantity as QtyReceived , 0 as QtyIssued
,0 as Rate ,INV_Item.MinimumOrderQuantity,INV_Item.MaximumOrderQuantity ,INV_Item.FixedOrderQuantity

from INV_StockTransactionHeader
Inner Join INV_StockTransactionDetail on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
inner join set_documentType on set_documentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and set_documentType.MenuID in (select MenuID from ApplicationMenu where MenuPath = '/Inventory/Transaction/Stock_frm.aspx')
Left Join Inv_Item on  Inv_Item.ItemID = INV_StockTransactionDetail.ItemID 
Left Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID
Where INV_StockTransactionHeader.Status=1
{0}

Union All

Select 
INV_StockTransactionDetail.ItemID  , Inv_Item.InventoryItemStatusCode as ItemCode , INV_Item.Description as Item 
,SET_UnitofMeasure.UOMName as Unit , 0 as Opening , 0 as QtyReceived ,INV_StockTransactionDetail.TransactionQuantity as QtyIssued
,0 as Rate ,INV_Item.MinimumOrderQuantity,INV_Item.MaximumOrderQuantity ,INV_Item.FixedOrderQuantity
from INV_StockTransactionHeader
Inner Join INV_StockTransactionDetail on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
inner join set_documentType on set_documentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and set_documentType.MenuID in (select MenuID from ApplicationMenu where MenuPath = '/Inventory/Transaction/Average/StockGIN_frm.aspx')
Left Join Inv_Item on  Inv_Item.ItemID = INV_StockTransactionDetail.ItemID 
Left Join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID

Where INV_StockTransactionHeader.Status=1
{0}




)as TAb
Group By TAb.ItemID  , TAb.ItemCode , TAb.Item ,TAb.Unit  , TAb.Rate ,TAb.MinimumOrderQuantity,TAb.MaximumOrderQuantity ,TAb.FixedOrderQuantity";
                #endregion

                DataSet ds = null;
                if (fromDate != null && toDate != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) between @FromDate AND @ToDate ");
                }
                else if (fromDate != null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @FromDate ");
                }
                else if (fromDate == null)
                {
                    sb.Append(" and convert(date,INV_StockTransactionHeader.CreateDate) = @ToDate");
                }
                                

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_DailyStockMonitoring, sb), sqlparams);

                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query_DailyStockMonitoring, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_DailyStockMonitoring, sb), sqlparams);

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

        public static DataTable ItemStockStatus_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> ItemID,
        Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            // strFilter = "where";
            //if (IsDispatchCancelHide)
            //    strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            //if (isVehicle)
            //    if (strFilter == "where")
            //        strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            //    else
            //        strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }
                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }



                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_ItemStockStatusTwo, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds.Tables[0] != null)
                    dtReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtReturn;
        }

        public static DataTable GetData_FromPO_Detail_AlImdad(int pOHeaderID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetPODetailID, sqlparams);

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

        public static DataTable GetDataByKeyTable_ForDocumentsDetails_AlImdad(int stockTransactionID, int companyID, int periodID, bool status = true,bool recipeitems = false)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            //if (recipeitems)
            {
               // sb.Append(" and ISNULL(INV_StockTransactionDetail.NetAmount,0) <> 0 ");

                sb.Append(" and abs(ISNULL(INV_StockTransactionDetail.QtyWithWastage,0)) = 0 ");
            }

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDocumentDetailForAlImdad,sb.ToString()), sqlparams);

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

        public static DataTable GetDataByKeyTable_ForStopagesDetails(int stockTransactionID, int companyID, int periodID, bool status = true, bool recipeitems = false)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            //if (recipeitems)
            //    sb.Append(" and ISNULL(INV_StockTransactionDetail.NetAmount,0) <> 0 ");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetStopagesDetail, sb.ToString()), sqlparams);

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

        public static DataTable GetDataByKeyTable_ForShiftDetails(int stockTransactionID, int companyID, int periodID, bool status = true, bool recipeitems = false)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_shiftDetail, sb.ToString()), sqlparams);

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





        public static DataTable GetDataFor_AlImdad_ProductionOut(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDetailsForProductionOut, sqlparams);

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

        public static DataTable GetDataByKeyTable_ForTransferOUt(int stockTransactionID, int companyID, int periodID, bool status = true)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GetDetailsforTransferOut, sqlparams);

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

        public static void RECON_ALL(DateTime? datefrom =null , DateTime? dateto=null)
        {

            DataTable dtItemList = INV_StockTransactionDetail.GetDistinctItemID2();


            DataTable dtLocations = INV_StockTransactionDetail.GetDistinctLocationID();

            for (int i = 0; i < dtItemList.Rows.Count; i++)
            {
                for (int y = 0; y < dtLocations.Rows.Count; y++)
                {
                    try
                    {
                        RECON_ITEMLOCATIONWISE(int.Parse(dtItemList.Rows[i]["ItemID"].ToString()), int.Parse(dtLocations.Rows[y]["LocationID"].ToString()),int.Parse(dtItemList.Rows[i]["CompanyID"].ToString()),int.Parse(dtItemList.Rows[i]["PeriodID"].ToString()), datefrom , dateto);
                    }
                    catch { }
                }

            }


        }

        public static void RECON_ITEMLOCATIONWISE(int ItemID, int LocationID,int CompanyID,int PeriodID, DateTime? datefrom , DateTime? dateto)
        {

            DataTable dt = INV_StockTransactionDetail.GetStockReconcileLedger(ItemID, LocationID, CompanyID, PeriodID, dateto);

            if (!dt.Columns.Contains("RunningAverage"))
                dt.Columns.Add("RunningAverage", typeof(decimal));

            //update running total quantity

            int RunningQuantity = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {

                if (dt.Rows[i]["TransactionImpact"].ToString() == "Positive")
                {
                    RunningQuantity += Math.Abs(Convert.ToInt32(dt.Rows[i]["TransactionQuantity"]));
                }
                else
                {
                    RunningQuantity -= Math.Abs(Convert.ToInt32(dt.Rows[i]["TransactionQuantity"]));
                }

                dt.Rows[i]["RunningQuantity"] = RunningQuantity.ToString();
            }

            for (int j = 1; j < dt.Rows.Count; j++)
            {
                //RunningQuantity += Convert.ToInt32(dt.Rows[i]["TransactionQuantity"]);
                dt.Rows[j]["PreviousQty"] = dt.Rows[j - 1]["RunningQuantity"].ToString();
            }

            decimal CurrentAverage = 0;

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (i == 0 && dt.Rows[i]["TransactionImpact"].ToString() == "Positive")
                {
                    CurrentAverage = Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }
                else if (dt.Rows[i]["TransactionImpact"].ToString() == "Positive" && !dt.Rows[i]["DocumentType"].ToString().Contains("Dispatch Ret"))
                {
                    //if (Convert.ToDecimal(dt.Rows[i]["ValueChange"].ToString()) > 0)
                    //{
                    //    CurrentAverage  =Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    //}
                    //else { 
                    //}

                    if (Convert.ToDecimal(dt.Rows[i]["RunningQuantity"].ToString()) == 0)
                    {
                        CurrentAverage = Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString());
                    }
                    else {
                        CurrentAverage = Math.Abs(((Convert.ToDecimal(dt.Rows[i]["TransactionQuantity"].ToString()) * Convert.ToDecimal(dt.Rows[i]["ConstantPositiveCost"].ToString())) +
                                             (Convert.ToDecimal(dt.Rows[i]["PreviousQty"].ToString()) * Convert.ToDecimal(dt.Rows[i - 1]["RunningAverage"].ToString())))
                                             / Convert.ToDecimal(dt.Rows[i]["RunningQuantity"].ToString()));
                    }


                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }
                else
                {
                    dt.Rows[i]["RunningAverage"] = CurrentAverage.ToString();
                }

            }

            for (int i = 0; i < dt.Rows.Count; i++)
            {

                

                ////if ((dt.Rows[i]["TransactionImpact"].ToString() == "Negative" && Convert.ToDateTime(dt.Rows[i]["DocumentDate"].ToString()) >= Convert.ToDateTime("2017-01-01") && Convert.ToDateTime(dt.Rows[i]["DocumentDate"].ToString()) <= Convert.ToDateTime("2017-02-28")) || dt.Rows[i]["DocumentType"].ToString().Contains("Dispatch Re"))
                if ((dt.Rows[i]["TransactionImpact"].ToString() == "Negative" && Convert.ToDateTime(dt.Rows[i]["DocumentDate"].ToString()) >= Convert.ToDateTime(datefrom.Value) 
                    && Convert.ToDateTime(dt.Rows[i]["DocumentDate"].ToString()) <= Convert.ToDateTime(dateto.Value))
                    || dt.Rows[i]["DocumentType"].ToString().Contains("Dispatch Re"))
                {
                    INV_StockTransactionHeader.UpdateStockAverageRate(int.Parse(dt.Rows[i]["TransactionDetailID"].ToString()),
                       Convert.ToDecimal(dt.Rows[i]["RunningAverage"].ToString()));
                }
            }
        }

        public static DataTable GetDataForGRNReport(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
             SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
            Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null
            , int LocationID = -1, int FiscalID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += " AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += " AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += " AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += " AND [INV_StockTransactionHeader].[DocumentNo] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                if (LocationID != -1)
                {
                    AddtionalParameters += " and INV_StockTransactionHeader.locationID= " + LocationID.ToString();
                }

                if (FiscalID > 0)
                    AddtionalParameters += " and INV_StockTransactionHeader.FiscalID= " + FiscalID;


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GRNReport , AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_GRNReport , AddtionalParameters), sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetLCWiseStockReportData(int CurrentFiscalID,Nullable<int> companyID, Nullable<int> itemID
            , Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null,int DocumentNoFrom = -1,int DocumentNumberTo = -1)
        {
            DataTable dtToReturn = null;
            StringBuilder whereClause = new StringBuilder();
            StringBuilder sb = new StringBuilder();

            #region Query
            const string Query_GetReportData = @"


Select * from
(
Select GRNStockID as StockTransactionID,IncurredOnID as LCNumber,GRN_No as DocumentNo
,GRnImportQty as TransactionQuantity,AdjustmentITEmID as LocalItemID,GinQty as IssuedQuantity,ExchangeRatePercent,
ISNULL(CostWithCD,0) As FCRate,(ISNULL(CostWithCD,0) * (GRnImportQty - abs(GinQty))) as ActualAmount
/*,(GRnImportQty - abs(GinQty)) as TotalBalance
,MarketAmount,case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end as MarketRate
,(MarketAmount - (ISNULL(CostWithCD,0) * (GRnImportQty - abs(GinQty)))) Variance
,case when Marketamount > 0 then (((MarketAmount - (ISNULL(CostWithCD,0) * (GRnImportQty - abs(GinQty)))) * 100) / MarketAmount) else 0 end as variancepercent*/
,(GRnImportQty - abs(GinQty)) as TotalBalance
,(GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end) as MarketAmount ,case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end as MarketRate
,((GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end) - (ISNULL(CostWithCD,0) * (GRnImportQty - abs(GinQty)))) Variance
,case when (GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then ((GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end) / GRnImportQty) else 0 end) > 0 then ((((GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end) - (ISNULL(CostWithCD,0) * (GRnImportQty - abs(GinQty)))) * 100) / (GRnImportQty - abs(GinQty)) *  (case when GrnImportQty > 0 then (MarketAmount / GRnImportQty) else 0 end)) else 0 end as variancepercent
,GRnImportQty as ReceivedQty,FinalTab.DocumentDate
,DATEDIFF(Month,FinalTab.DocumentDate,@ToDate) as Months
,FinalTab.InventoryItemStatusCode,FinalTab.Description
,LCItem.Description as IncurredOnID
,GRnItemID as ItemID
 from
(
Select GRNStockID,IncurredOnID,GRN_No,GRnItemID,GRnImportQty,AdjustmentITEmID,ADjustmentInqty,SUM(GinQty)GinQty,MarketAmount,DocumentDate,InventoryItemStatusCode,Description from 
(
Select GRNStockID,t3.IncurredOnID,GRN_No,t3.GRnItemID,t3.GRnImportQty,t3.AdjustmentITEmID,t3.ADjustmentInqty--,trnsferINDocNO
,ISNULL(SUM(gindetails.TransactionQuantity),0) as GinQty,MarketAmount,t3.DocumentDate,InventoryItemStatusCode,Description
--,gindetails.FIFOTransactionDetailID
--,gindetails.TransactionQuantity
--,gindetails.TransactionDetailID
from
(
Select t2.IncurredOnID,t2.GRnItemID,t2.GRnImportQty,t2.AdjustmentITEmID,t2.ADjustmentInqty,GRNStockID,
ISNULL(SUM(indetails.TransactionQuantity),0) trnsferinqty,indetailsheader.DocumentNo as trnsferINDocNO,indetails.TransactionDetailID,GRN_No,t2.DocumentNo as TrnsferoutdocNo
,MarketAmount,t2.DocumentDate,InventoryItemStatusCode,Description,t2.FiscalID
from
(
Select t.IncurredOnID,GRnItemID,GRnImportQty,AdjustmentITEmID,t.TransactionDetailID,t.ADjustmentInqty
,ISNULL(SUM(outdetails.TransactionQuantity),0) as TransferOutQty,outdetailsheader.DocumentNo,outdetailsheader.DocumentTypeID,GRN_No,GRNStockID,MarketAmount,t.DocumentDate
,InventoryItemStatusCode,Description,t.FiscalID,outdetailsheader.StockTransactionID,outdetails.TransactionDetailID trnsferdetailID
 from
(
Select 
INV_StockTransactionHeader.StockTransactionID as GRNStockID
,INV_StockTransactionHeader.IncurredOnID
,INV_StockTransactionHeader.DocumentNo as GRN_No
,INV_StockTransactionDetail.Amount AS MarketAmount
,SET_DocumentType.DocumentType as GRNType
,adjustmentindetails.StockTransactionID as adjustmentstockID
,INV_StockTransactionDetail.ItemID as GRnItemID
,INV_StockTransactionDetail.TransactionQuantity as  GRnImportQty
,adjustmentindetails.ItemID as AdjustmentITEmID
,ISNULL(adjustmentindetails.TransactionQuantity,0) as ADjustmentInqty
,adjustmentindetails.TransactionDetailID
,INV_StockTransactionHeader.DocumentDate
,importitem.InventoryItemStatusCode
,importitem.Description
,INV_StockTransactionHeader.FiscalID

 from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID =INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
--and INV_StockTransactionHeader.DocumentTypeID = 337
and INV_StockTransactionHeader.DocumentTypeID in (Select DocumentTypeID from Set_DocumentType where  SET_DocumentType.DocumentType like '%Import%' and (SET_DocumentType.DocumentType like '%GRN%') )
and INV_StockTransactionHeader.Status =1
--and INV_StockTransactionHeader.DocumentNo = 4
and Convert(Date,INV_StockTransactionHeader.DocumentDate) <= @ToDate
inner join INV_ITem importitem on importitem.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_StockTransactionDetail adjustmentindetails on adjustmentindetails.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
and adjustmentindetails.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
inner join INV_StockTransactionHeader adjustmentinheader on adjustmentinheader.StockTransactionID = adjustmentindetails.StockTransactionID
and adjustmentindetails.CompanyID = adjustmentinheader.CompanyID
and adjustmentindetails.PeriodID = adjustmentinheader.PeriodID
and adjustmentinheader.Status = 1
--and adjustmentinheader.FiscalID = 2
and adjustmentinheader.FiscalID = INV_StockTransactionHeader.FiscalID

inner join INV_ITEm localItem on localItem.ItemID = adjustmentindetails.ItemID
and importitem.SubstituteCode = localItem.InventoryItemStatusCode

where INV_StockTransactionHeader.Status = 1
and  INV_StockTransactionDetail.Status = 1

union all
Select 
INV_StockTransactionHeader.StockTransactionID as GRNStockID
,INV_StockTransactionHeader.IncurredOnID
,INV_StockTransactionHeader.DocumentNo as GRN_No
,INV_StockTransactionDetail.Amount AS MarketAmount
,SET_DocumentType.DocumentType as GRNType
,INV_StockTransactionHeader.StockTransactionID as adjustmentstockID
,INV_StockTransactionDetail.ItemID as GRnItemID
,INV_StockTransactionDetail.TransactionQuantity as  GRnImportQty
,INV_StockTransactionDetail.ItemID as AdjustmentITEmID
,INV_StockTransactionDetail.TransactionQuantity as ADjustmentInqty
,INV_StockTransactionDetail.TransactionDetailID
,INV_StockTransactionHeader.DocumentDate
,localItem.InventoryItemStatusCode
,localItem.Description
,INV_StockTransactionHeader.FiscalID

 from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID =INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.TransactionQuantity > 0
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
--and INV_StockTransactionHeader.DocumentTypeID = 337
and INV_StockTransactionHeader.DocumentTypeID in (Select DocumentTypeID from Set_DocumentType where  SET_DocumentType.DocumentType like '%Import%' and (SET_DocumentType.DocumentType like '%opening%') )
and INV_StockTransactionHeader.Status =1
--and INV_StockTransactionHeader.DocumentNo = 4
and Convert(Date,INV_StockTransactionHeader.DocumentDate) <= @ToDate
--and INV_StockTransactionDetail.ItemID = 4353
--inner join INV_ITem importitem on importitem.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_ITEm localItem on localItem.ItemID = INV_StockTransactionDetail.ItemID

where INV_StockTransactionHeader.Status = 1
and  INV_StockTransactionDetail.Status = 1
--and INV_StockTransactionHeader.FiscalID <= @CurrentFiscalID

--where INV_StockTransactionDetail.ItemID = 5081
) as t


left join INV_StockTransactionDetail outdetails on outdetails.FIFOTransactionDetailID = t.TransactionDetailID
and outdetails.Status = 1
left join INV_StockTransactionHeader outdetailsheader  on outdetailsheader.StockTransactionID = outdetails.StockTransactionID
and outdetails.CompanyID = outdetailsheader.CompanyID
and outdetails.PeriodID = outdetailsheader.PeriodID
and outdetailsheader.Status = 1
and outdetails.Status = 1
--and outdetailsheader.FiscalID = t.FiscalID 
and outdetailsheader.DocumentDate <= @ToDate


group by t.IncurredOnID,GRnImportQty,AdjustmentITEmID,t.TransactionDetailID,t.ADjustmentInqty,outdetailsheader.DocumentNo,GRnItemID,outdetailsheader.DocumentTypeID,GRN_No,GRNStockID
,t.MarketAmount,t.DocumentDate,InventoryItemStatusCode,Description,t.FiscalID,outdetailsheader.StockTransactionID,outdetails.TransactionDetailID

) as t2


left join INV_StockTransactionDetail indetails on indetails.SourceHeaderID = t2.StockTransactionID
and indetails.SourceDocumentTypeID = t2.DocumentTypeID
and indetails.SourceCode = t2.DocumentNo
and indetails.SourceLineID = t2.trnsferdetailID
and indetails.ITemID = t2.AdjustmentITEmID
and indetails.Status = 1
left join INV_StockTransactionHeader indetailsheader on indetailsheader.StockTransactionID = indetails.StockTransactionID
and indetails.CompanyID = indetailsheader.CompanyID
and indetails.PeriodID = indetailsheader.PeriodID
and indetailsheader.Status = 1
and indetails.Status = 1
--and indetailsheader.FiscalID = t2.FiscalID
and indetailsheader.DocumentDate <= @ToDate 


group by t2.IncurredOnID,t2.GRnItemID,t2.GRnImportQty,t2.AdjustmentITEmID,t2.ADjustmentInqty,indetailsheader.DocumentNo,indetails.TransactionDetailID,GRN_No,GRNStockID
,t2.DocumentNo,t2.MarketAmount,t2.DocumentDate,InventoryItemStatusCode,Description,t2.FiscalID

) t3

left join INV_StockTransactionDetail gindetails on gindetails.FIFOTransactionDetailID = t3.TransactionDetailID
and gindetails.ItemID = t3.AdjustmentITEmID
left join INV_StockTransactionheader gindetailsheader on gindetailsheader.StockTransactionID= gindetails.StockTransactionID
and gindetails.CompanyID = gindetailsheader.CompanyID
and gindetails.PeriodID = gindetailsheader.PeriodID
and gindetailsheader.Status =1
and gindetails.Status = 1
and gindetailsheader.DocumentTypeID = 242
--and gindetailsheader.FiscalID <= @CurrentFiscalID
--and gindetailsheader.DocumentDate <= @ToDate
where isnull(gindetailsheader.DocumentDate,@ToDate) <= @ToDate


group by  t3.IncurredOnID,t3.GRnItemID,t3.GRnImportQty,t3.AdjustmentITEmID,t3.ADjustmentInqty,GRN_No,GRNStockID,t3.MarketAmount
,t3.DocumentDate,InventoryItemStatusCode,Description--,gindetails.FIFOTransactionDetailID--,gindetails.TransactionQuantity--,trnsferINDocNO

union all 

Select 
INV_StockTransactionHeader.StockTransactionID as GRNStockID
,INV_StockTransactionHeader.IncurredOnID
,INV_StockTransactionHeader.DocumentNo as GRN_No
,INV_StockTransactionDetail.ItemID as GRnItemID
,INV_StockTransactionDetail.TransactionQuantity as  GRnImportQty
,INV_StockTransactionDetail.ItemID as AdjustmentITEmID
,INV_StockTransactionDetail.TransactionQuantity as ADjustmentInqty
,gindetails.TransactionQuantity as GinQty
,INV_StockTransactionDetail.Amount AS MarketAmount
,INV_StockTransactionHeader.DocumentDate
,localItem.InventoryItemStatusCode
,localItem.Description
 from INV_StockTransactionHeader
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID =INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1
and INV_StockTransactionDetail.TransactionQuantity > 0
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
--and INV_StockTransactionHeader.DocumentTypeID = 337
and INV_StockTransactionHeader.DocumentTypeID in (Select DocumentTypeID from Set_DocumentType where  SET_DocumentType.DocumentType like '%Import%' and (SET_DocumentType.DocumentType like '%opening%') )
and INV_StockTransactionHeader.Status =1
--and INV_StockTransactionHeader.DocumentNo = 4
and Convert(Date,INV_StockTransactionHeader.DocumentDate) <= @ToDate
--and INV_StockTransactionDetail.ItemID = 4353
--inner join INV_ITem importitem on importitem.ItemID = INV_StockTransactionDetail.ItemID

inner join INV_ITEm localItem on localItem.ItemID = INV_StockTransactionDetail.ItemID


inner join INV_StockTransactionDetail gindetails on gindetails.FIFOTransactionDetailID = INV_StockTransactionDetail.TransactionDetailID
and gindetails.ItemID = INV_StockTransactionDetail.ItemID
inner join INV_StockTransactionheader gindetailsheader on gindetailsheader.StockTransactionID= gindetails.StockTransactionID
and gindetails.CompanyID = gindetailsheader.CompanyID
and gindetails.PeriodID = gindetailsheader.PeriodID
and gindetailsheader.Status =1
and gindetails.Status = 1
and gindetailsheader.DocumentTypeID = 242
--and gindetailsheader.FiscalID <= @CurrentFiscalID
--and gindetailsheader.DocumentDate <= @ToDate
where isnull(gindetailsheader.DocumentDate,@ToDate) <= @ToDate

and INV_StockTransactionHeader.Status = 1
and  INV_StockTransactionDetail.Status = 1
) as gintab
group by GRNStockID,IncurredOnID,GRN_No,GRnItemID,GRnImportQty,AdjustmentITEmID,ADjustmentInqty,MarketAmount,DocumentDate,InventoryItemStatusCode,Description


) as FinalTab

inner join Fabric_ImportCostingHeader on Fabric_ImportCostingHeader.StockTransactionID = FinalTab.GRNStockID
inner join Fabric_ImportCostingDetail on Fabric_ImportCostingDetail.CostingID = Fabric_ImportCostingHeader.CostingID
and Fabric_ImportCostingDetail.ItemID = FinalTab.GRnItemID


left join INV_Item LCItem on LCItem.ItemID = FinalTab.IncurredOnID


where FinalTab.GRN_No is not null 

{0}

--FinalTab.GRN_No = 4
--and FinalTab.IncurredOnID = 5847

) as Ftab

where Ftab.TotalBalance > 0

order by Ftab.Description


 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@FromDate", fromDate);
                sqlparams[2] = new SqlParameter("@ToDate", toDate);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID); 


                if (DocumentNoFrom > 0 && DocumentNumberTo > 0)
                {
                    sb.Append("  and FinalTab.GRN_No between  " + DocumentNoFrom + " and " + DocumentNumberTo);
                }

                else if (DocumentNoFrom > 0)
                {
                    sb.Append("  and FinalTab.GRN_No = " + DocumentNoFrom + "");
                }

                else if (DocumentNumberTo > 0)
                {
                    sb.Append("  and FinalTab.GRN_No = " + DocumentNumberTo + "");
                }

                if (itemID != null)
                {
                    sb.Append(" and FinalTab.IncurredOnID =  " + itemID);
                }


                #region GetDataForLC

                DataTable dtAllItems = INV_Item.GetAllLocalAndImportItemID(companyID.Value, toDate.Value);
                DataTable dtFinal = new DataTable();
                int index = 0;
                foreach (DataRow drs in dtAllItems.Rows)
                {
                    DataTable dt_AllGrn = INV_StockTransactionHeader.GetAllGrnByDate(companyID, fromDate, toDate, " and INV_StockTransactionDetail.ItemID in (" + drs["ItemID"] + "," + drs["ImportItemID"] + ")", null);
                    decimal totalbalance = INV_StockTransactionHeader.GetClosingBalance(companyID, fromDate, toDate, drs["ItemID"].ToString().ToIntOrZero());
                    //DataTable dt_SelectedItem = dt_AllGrn.Select("Description = " + drs["Description"].ToString()).CopyToDataTable();
                    if (index == 0)
                        dtFinal = dt_AllGrn.Clone();
                    for (int i = 0; i < dt_AllGrn.Rows.Count; i++)
                    {
                        

                        if (totalbalance > 0)
                        {
                            if (Math.Abs(totalbalance) > Convert.ToDecimal(dt_AllGrn.Rows[i]["TransactionQuantity"]))
                            {
                                dt_AllGrn.Rows[i]["IsIssued"] = "1";
                                totalbalance = totalbalance - Convert.ToDecimal(dt_AllGrn.Rows[i]["TransactionQuantity"]);
                                dt_AllGrn.Rows[i]["TotalBalance"] = dt_AllGrn.Rows[i]["TransactionQuantity"];
                            }
                            else if (Math.Abs(totalbalance) == Convert.ToDecimal(dt_AllGrn.Rows[i]["TransactionQuantity"]))
                            {
                                dt_AllGrn.Rows[i]["IsIssued"] = "1";
                                totalbalance = totalbalance - Convert.ToDecimal(dt_AllGrn.Rows[i]["TransactionQuantity"]);
                                dt_AllGrn.Rows[i]["TotalBalance"] = dt_AllGrn.Rows[i]["TransactionQuantity"];
                            }
                            else if (Math.Abs(totalbalance) < Convert.ToDecimal(dt_AllGrn.Rows[i]["TransactionQuantity"]))
                            {
                                dt_AllGrn.Rows[i]["IsIssued"] = "1";
                                dt_AllGrn.Rows[i]["TotalBalance"] = totalbalance;
                                totalbalance = 0;
                            }
                        }

                        if (dt_AllGrn.Rows[i]["IsIssued"].ToString() == "1" && dt_AllGrn.Rows[i]["DocumentTypeID"].ToString() != "36")
                            dtFinal.ImportRow(dt_AllGrn.Rows[i]);
                    }

                    index++;

                }

                DataTable dt_FinalData = new DataTable();

                string commaSeparatedString = String.Join(",", dtFinal.AsEnumerable().Where(y => y.Field<string>("IsIssued") == "1").Select(x => x.Field<int>("StockTransactionID").ToString()).ToArray());

                dt_FinalData = INV_StockTransactionHeader.GetAlLCostingData(companyID, commaSeparatedString);

                foreach (DataRow dr in dtFinal.Rows)
                {
                    IEnumerable<DataRow> rowsal = dt_FinalData.Rows.Cast<DataRow>().Where(r => r["StockTransactionID"].ToString() == dr["StockTransactionID"].ToString() && r["ItemID"].ToString() == dr["ItemID"].ToString());
                    rowsal.ToList().ForEach(r => r.SetField("TotalBalance", decimal.Parse(dr["TotalBalance"].ToString())));
                }


                dtToReturn = dt_FinalData.Select("TotalBalance > 0").CopyToDataTable();

                #endregion


                DataSet ds = null;//SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetReportData,sb.ToString()), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }
        public static DataSet StockLedgerWithOutAmount_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, int ObsoleteCheck, Nullable<int> itemCategoryID, Nullable<int> ItemID,
         Nullable<int> locationID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0, Nullable<int> CatalogID= null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }
                if (CatalogID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);
                }
                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SET_Location.LocationID,SL.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    sort.Append("order by FinTAb.DocumentDate,FinTAb.DocumentType desc,FinTAb.Orderid desc, FinTAb.DocumentNo");
                }



                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StockLedgerWithOutAmount, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet StockLedgerWithRack_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> ItemID,
         Nullable<int> locationID, Nullable <int> RackID, bool IsDispatchCancelHide = false, bool isVehicle = false, short? documentGroupID = null, int ApprovingGroup = 0, Nullable<int> CatalogID = null)
        {

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            strFilter = "where";
            if (IsDispatchCancelHide)
                strFilter += " (TAB.[IsCancelled] is null or TAB.[IsCancelled]=0) ";
            if (isVehicle)
                if (strFilter == "where")
                    strFilter += " TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
                else
                    strFilter += " and TAB.DocumentType != 'Transfer Out' and TAB.DocumentType != 'Transfer In' ";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }
                if (CatalogID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);
                }
                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());
                if (locationID.HasValue)
                {
                    if (!isVehicle)
                        //sb.Append("And (SET_Location.LocationID = @LocationID Or @LocationID is null) ");
                        sb.Append(" and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID OR @LocationID is null)");
                    else
                        sb.Append("AND isnull(SET_Location.LocationID,SL.LocationID) = @LocationID");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));
               
                if (RackID.HasValue)
                    sqlParams.Add(new SqlParameter("@RackID", RackID.Value));
                else
                    sqlParams.Add(new SqlParameter("@RackID", DBNull.Value));


                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }
                else
                {
                    sort.Append("order by FinTAb.DocumentDate asc,FinTAb.DocumentType desc,FinTAb.Orderid desc");//, FinTAb.DocumentNo
                }



                if (strFilter == "where")
                    strFilter = "";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StockLedgerWithRack, sb.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());

                if (ds != null && ds.Tables.Count > 1)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }
     

        public static DataTable Get_FIFOStockSettingByItemID(int ItemID, int _locationid,int CurrentCompanyID,int CurrentPeriodID, SqlTransaction objTransaction = null)
        {
            string filter = "";

            filter = "Order By INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID Asc";

            DataTable dt_StockAgainstItem = INV_StockTransactionDetail.Get_AllForStockSetting(CurrentCompanyID, CurrentPeriodID,
               ItemID, _locationid, filter);

            return dt_StockAgainstItem;
        }
        public static DataTable Get_DetailAgainstStockTransactionID(int companyID, int periodID, int StockTransactionID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@StockTransactionID", StockTransactionID);

#region 
                const string Query_GetDetailAgainstStockTransactionID = @"Select -1 as DiscountDetailID,-1 as PurchaseDiscountID
,INV_StockTransactionDetail.TransactionCost * TransactionQuantity as Amount,INV_StockTransactionDetail.ItemID,INV_Item.Description
,INV_Item.ItemTypeID as CategoryID,INV_SET_Category.CategoryName,
INV_StockTransactionDetail.TransactionUOMID as UOMID ,SET_UnitofMeasure.UOMName 
,'Add' as Action
 from INV_StockTransactionDetail
 
INNER jOIN INV_Item ON INV_Item.ItemID=INV_StockTransactionDetail.ItemID
AND INV_Item.Status=1
INNER jOIN INV_SET_Category ON INV_SET_Category.CategoryID=INV_Item.ItemTypeID
INNER jOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID=INV_StockTransactionDetail.TransactionUOMID

Where 
INV_StockTransactionDetail.CompanyID=@CompanyID
AND INV_StockTransactionDetail.PeriodID=@PeriodID
AND INV_StockTransactionDetail.StockTransactionID=@StockTransactionID
AND INV_StockTransactionDetail.Status=1";
#endregion
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDetailAgainstStockTransactionID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDetailAgainstStockTransactionID, sqlparams);

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
        #endregion

        public static void setFIFOTransactionDetailID(int ItemID, int LocationID, int CurrentCompanyID, int CurrentPeriodID)
        {
            try
            {

                bool stock_setting = true;


                int _locationId = LocationID;

                //DataTable dtItemList = INV_StockTransactionDetail.GetDistinctItemID();


                //for (int x = 0; x < dtItemList.Rows.Count; x++)
                {

                    // DataTable dt_StockAgainstItem = FnI.Core.Helper.IssuanceFlow.Get_AllDataAgainstItemFIFOByLocationByItemID(ItemID, _locationId);

                    DataTable dt_stocksetting = Get_FIFOStockSettingByItemID(ItemID, _locationId, CurrentCompanyID, CurrentPeriodID);
                    DataTable dt_Neg = new DataTable();

                    DataView dv = new DataView(dt_stocksetting);
                    dv.Sort = "DocumentDate Asc";

                    DataTable dt_Pos = dt_stocksetting.Select("issuance=0").CopyToDataTable();

                    if (dt_stocksetting.Select("qty=0").Length > 0)
                        dt_Neg = dt_stocksetting.Select("qty=0").CopyToDataTable();

                    if (!dt_Pos.Columns.Contains("IsIssued"))
                        dt_Pos.Columns.Add("IsIssued");

                    if (!dt_Pos.Columns.Contains("IssuedQuantity"))
                        dt_Pos.Columns.Add("IssuedQuantity");

                    if (!dt_Neg.Columns.Contains("ParentFIFOID"))
                        dt_Neg.Columns.Add("ParentFIFOID");

                    if (!dt_Neg.Columns.Contains("ParentFIFOTransactionDetailID"))
                        dt_Neg.Columns.Add("ParentFIFOTransactionDetailID");

                    decimal computeTotalIssuance = 0;
                    if (dt_Neg.Rows.Count > 0)
                        computeTotalIssuance = Math.Abs(Convert.ToDecimal(dt_Neg.Compute("Sum(issuance)", "")));

                    for (int i = 0; i < dt_Pos.Rows.Count; i++)
                    {
                        if (computeTotalIssuance > 0)
                        {
                            if (Math.Abs(computeTotalIssuance) > Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IsIssued"] = "1";
                                dt_Pos.Rows[i]["IssuedQuantity"] = dt_Pos.Rows[i]["Qty"];
                                computeTotalIssuance = computeTotalIssuance - Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]);


                            }
                            else if (Math.Abs(computeTotalIssuance) == Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IsIssued"] = "1";
                                dt_Pos.Rows[i]["IssuedQuantity"] = dt_Pos.Rows[i]["Qty"];
                                computeTotalIssuance = computeTotalIssuance - Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]);
                            }
                            else if (Math.Abs(computeTotalIssuance) < Convert.ToDecimal(dt_Pos.Rows[i]["Qty"]))
                            {
                                dt_Pos.Rows[i]["IssuedQuantity"] = computeTotalIssuance;
                                computeTotalIssuance = 0;
                            }
                        }
                    }

                    //first update all isissued to null

                    //foreach (DataRow item in dt_Pos.Rows)
                    //{
                    //   // INV_StockTransactionHeader.UpdateFIFOBySETToNull(int.Parse(item["TransactionDetailID"].ToString()), CurrentCompanyID, CurrentPeriodID, null);
                    //}

                    //Comment After discuss with Jawad Bhai (sajid)
                    //if (dt_Pos.Select("IsIssued=1").Length > 0)
                    if (dt_Pos != null && dt_Pos.Rows.Count > 0)
                    {
                        DataTable dt_PosUpdate = dt_Pos.Select().CopyToDataTable();

                        foreach (DataRow item in dt_PosUpdate.Rows)
                        {
                            INV_StockTransactionHeader.UpdateFIFOByTransactionID(int.Parse(item["TransactionDetailID"].ToString()), item["IssuedQuantity"].ToString() != "" ? Convert.ToDecimal(item["IssuedQuantity"].ToString()) : (decimal?)null, CurrentCompanyID, CurrentPeriodID, item["IsIssued"].ToString() == "1" ? true : (bool?)null);
                        }

                    }





                }



            }
            catch { }

        }

        public static DataTable GetAllDataforAvgSystem(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, int fiscalid = 7,
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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectAll_WithPagingAvg + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectAll_WithPagingAvg + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectAll_WithPagingAvg), sqlparams).Tables[0];
                    // dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithoutPagingAvg),sqlparams).Tables[0];

                    //ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectAll_WithoutPagingAvg + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SelectAll_WithoutPagingAvg + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_DistinctItemFromInv(int companyID, int periodID, string filter, SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@ItemID", itemID);
                //sqlparams[3] = new SqlParameter("@LocationID", _locationid);

                string query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_GetAllDistinctItemInv.Replace("{filter}", filter);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, query, sqlparams);

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

        public static DataTable GETData_For_RequisitionAssignment_Detail(int documentNo, int companyID, int periodID, string Where, bool status = true,
       SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            string Filter = "";
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                //sqlparams[0] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (Where != "")
                {
                    Filter = Where;
                }
                if (documentNo > 0)
                {
                    Filter += " and INV_StockTransactionHeader.DocumentNo = " + documentNo;
                }

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockReq_PRDetails, Filter), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockReq_PRDetails, Filter), sqlparams);

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

        public static bool UpdateStockDetailPOQtyByKey(int CompanyID, int PeriodID, long transactionDetailID, int stockTransactionID,int PoQty, SqlTransaction objTransaction = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateStockTransaction");

            const string Query_UpdatePOQty = @"Update [INV_StockTransactionDetail] 
Set POQuantity = @POQuantity
WHERE [TransactionDetailID] = @TransactionDetailID 
	and StockTransactionID = @StockTransactionID
	and CompanyID = @CompanyID
	and PeriodID = @PeriodID";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@TransactionDetailID", transactionDetailID);
                sqlparams[1] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[2] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[4] = new SqlParameter("@POQuantity", PoQty);
                //if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateFlagAndComments, sqlparams) > 0)
                //  result = true;

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOQty, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdatePOQty, sqlparams) > 0)
                        result = true;
                }

                result = true;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateStockTransaction");
            }

            return result;
        }


        public static DataTable GetItemStockFor_DetailLocationWise(int CurrentCompanyID, int CurrentPeriodID, int ItemID, int LocationID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@LocationID", LocationID);

                const string Query_ForItemStock_LocationWise = @"
Select isnull(Sum(TransactionQuantity),0)as ItemStock from (
Select * from (
SELECT Distinct
 INV_SET_Category.CategoryName, INV_StockTransactionDetail.ItemID, INV_Item.Description AS ItemDescription, 
 isnull(SET_UnitofMeasure.UOMName,UOM.UOMName) as UOMName, 
isnull(SET_Location.LocationName,SL.LocationName) as LocationName, 
    SUM(INV_StockTransactionDetail.TransactionQuantity)TransactionQuantity,  

case when INV_StockTransactionDetail.ImpactID is not null then 
			case INV_StockTransactionDetail.ImpactID  when 1 then 'Positive' when 2 then 'Negative' end
			else  SET_DocumentType.TransactionImpact end as TransactionImpact
,INV_StockTransactionHeader.StockTransactionID
FROM INV_StockTransactionDetail 

	INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID AND INV_StockTransactionHeader.Status = 1 
        AND INV_StockTransactionHeader.IsActive = 1 
        
  LEFT JOIN [SAL_DispatchHeader] on 
  [SAL_DispatchHeader].CompanyID=[INV_StockTransactionDetail].[CompanyID]  and 
  [SAL_DispatchHeader].PeriodID=[INV_StockTransactionDetail].[PeriodID] and 
  [SAL_DispatchHeader].FiscalID=[INV_StockTransactionDetail].FiscalID
  AND [SAL_DispatchHeader].DispatchNo = INV_StockTransactionHeader.DocumentNo and 
  [SAL_DispatchHeader].DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and 
  [SAL_DispatchHeader].[Status]=1 

	LEFT JOIN (
	select SAL_DeliveryOrderDetail.*, SAL_DeliveryOrderHeader.DocumentTypeID, SAL_DeliveryOrderHeader.DeliveryOrderNo from SAL_DeliveryOrderHeader 
	Inner Join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
		and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	) as DeliveryHeaderDetail on DeliveryHeaderDetail.DODetailID = [INV_StockTransactionDetail].[SourceLineID] 
		and DeliveryHeaderDetail.DOHeaderID = [INV_StockTransactionDetail].[SourceHeaderID] 
		and DeliveryHeaderDetail.DocumentTypeID = [INV_StockTransactionDetail].[SourceDocumentTypeID]
		and DeliveryHeaderDetail.DeliveryOrderNo = [INV_StockTransactionDetail].[SourceCode]
		and DeliveryHeaderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID
		and DeliveryHeaderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and DeliveryHeaderDetail.Status = 1

	
	LEFT JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = DeliveryHeaderDetail.[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[OrderID] = DeliveryHeaderDetail.[SalesOrderHeaderID] 
        AND [SAL_OrderDetail].[CompanyID] = DeliveryHeaderDetail.[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = DeliveryHeaderDetail.[PeriodID] 
        AND [SAL_OrderDetail].[Status] = 1 
	
	INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID AND INV_Item.Status = 1 
	
	INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
	
	left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_Item.WeightUOMCode 
	INNER JOIN SET_UnitofMeasure UOM ON UOM.UOMID = INV_Item.PrimaryUOMCode

	LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
    
    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
	
	inner JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.Status = 1 
		--And SET_DocumentType.TransactionImpact <> 'No Impact' 
    AND ((SET_DocumentType.DocumentType = 'Stock Adjustment') or (SET_DocumentType.TransactionImpact <> 'No Impact') )          

WHERE INV_StockTransactionDetail.CompanyID = @CompanyID
 AND INV_StockTransactionDetail.PeriodID = @PeriodID 
 AND INV_StockTransactionDetail.Status = 1 
    AND INV_StockTransactionDetail.IsActive = 1 
     And INV_Item.ItemID = @ItemID
	 and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID)  
    AND INV_StockTransactionHeader.StatusCode = 1 
    AND INV_StockTransactionHeader.ReversalID IS NULL
GROUP BY 
    [SAL_DispatchHeader].[IsCancelled],
	SAL_OrderDetail.Orderid,
	INV_SET_Category.CategoryName, 
	INV_StockTransactionDetail.ItemID, 
	INV_Item.Description, 
	isnull(SET_UnitofMeasure.UOMName,UOM.UOMName), 
	SET_Location.LocationName,
	SL.LocationName,
	INV_StockTransactionHeader.DocumentNo, 
	INV_StockTransactionHeader.DocumentDate, 
	SET_DocumentType.DocumentType,
	SET_DocumentType.TransactionImpact, INV_Item.InventoryItemStatusCode, 
    INV_StockTransactionDetail.TransactionUOMID,
    INV_Item.PrimaryUOMCode ,
    INV_Item.VolumeUOMCode, 
    INV_Item.UnitVolume, 
    INV_Item.WeightUOMCode,INV_Item.UnitWeight, 
    Cast(INV_StockTransactionDetail.CreateDate as date)
    ,INV_StockTransactionDetail.ImpactID,
INV_StockTransactionDetail.ValueChange
,INV_StockTransactionHeader.StockTransactionID

) as TAB ) Tabfinal
";
                DataSet ds = null;

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ForItemStock_LocationWise, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_ForItemStock_LocationWise, sqlparams);

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

        public static DataTable GetItemStockFor_DetailLocationRackWise(int CurrentCompanyID, int CurrentPeriodID, int ItemID, int LocationID,int RackID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@LocationID", LocationID);
                sqlparams[4] = new SqlParameter("@RackID", RackID);

                const string Query_ForItemStock_LocationWise = @"
Select isnull(Sum(TransactionQuantity),0)as ItemStock from (
Select * from (
SELECT Distinct
 INV_SET_Category.CategoryName, INV_StockTransactionDetail.ItemID, INV_Item.Description AS ItemDescription, 
 isnull(SET_UnitofMeasure.UOMName,UOM.UOMName) as UOMName, 
isnull(SET_Location.LocationName,SL.LocationName) as LocationName, 
    SUM(INV_StockTransactionDetail.TransactionQuantity)TransactionQuantity,  

case when INV_StockTransactionDetail.ImpactID is not null then 
			case INV_StockTransactionDetail.ImpactID  when 1 then 'Positive' when 2 then 'Negative' end
			else  SET_DocumentType.TransactionImpact end as TransactionImpact
,INV_StockTransactionHeader.StockTransactionID
FROM INV_StockTransactionDetail 

	INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID AND INV_StockTransactionHeader.Status = 1 
        AND INV_StockTransactionHeader.IsActive = 1 
        
  LEFT JOIN [SAL_DispatchHeader] on 
  [SAL_DispatchHeader].CompanyID=[INV_StockTransactionDetail].[CompanyID]  and 
  [SAL_DispatchHeader].PeriodID=[INV_StockTransactionDetail].[PeriodID] and 
  [SAL_DispatchHeader].FiscalID=[INV_StockTransactionDetail].FiscalID
  AND [SAL_DispatchHeader].DispatchNo = INV_StockTransactionHeader.DocumentNo and 
  [SAL_DispatchHeader].DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and 
  [SAL_DispatchHeader].[Status]=1 

	LEFT JOIN (
	select SAL_DeliveryOrderDetail.*, SAL_DeliveryOrderHeader.DocumentTypeID, SAL_DeliveryOrderHeader.DeliveryOrderNo from SAL_DeliveryOrderHeader 
	Inner Join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
		and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	) as DeliveryHeaderDetail on DeliveryHeaderDetail.DODetailID = [INV_StockTransactionDetail].[SourceLineID] 
		and DeliveryHeaderDetail.DOHeaderID = [INV_StockTransactionDetail].[SourceHeaderID] 
		and DeliveryHeaderDetail.DocumentTypeID = [INV_StockTransactionDetail].[SourceDocumentTypeID]
		and DeliveryHeaderDetail.DeliveryOrderNo = [INV_StockTransactionDetail].[SourceCode]
		and DeliveryHeaderDetail.CompanyID = [INV_StockTransactionDetail].CompanyID
		and DeliveryHeaderDetail.PeriodID = [INV_StockTransactionDetail].PeriodID
		and DeliveryHeaderDetail.Status = 1

	
	LEFT JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = DeliveryHeaderDetail.[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[OrderID] = DeliveryHeaderDetail.[SalesOrderHeaderID] 
        AND [SAL_OrderDetail].[CompanyID] = DeliveryHeaderDetail.[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = DeliveryHeaderDetail.[PeriodID] 
        AND [SAL_OrderDetail].[Status] = 1 
	
	INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID AND INV_Item.Status = 1 
	
	INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0) 
	
	left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = INV_Item.WeightUOMCode 
	INNER JOIN SET_UnitofMeasure UOM ON UOM.UOMID = INV_Item.PrimaryUOMCode

	LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
    
    LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
	
	inner JOIN SET_DocumentType ON SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID 
        AND SET_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID AND SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
        AND Set_DocumentType.Status = 1 
		--And SET_DocumentType.TransactionImpact <> 'No Impact' 
    AND ((SET_DocumentType.DocumentType = 'Stock Adjustment') or (SET_DocumentType.TransactionImpact <> 'No Impact') )          

WHERE INV_StockTransactionDetail.CompanyID = @CompanyID
 AND INV_StockTransactionDetail.PeriodID = @PeriodID 
 AND INV_StockTransactionDetail.Status = 1 
    AND INV_StockTransactionDetail.IsActive = 1 
     And INV_Item.ItemID = @ItemID
	 and (isnull(SL.LocationID,SET_Location.LocationID) = @LocationID)  
and INV_StockTransactionDetail.RackID = @RackID
    AND INV_StockTransactionHeader.StatusCode = 1 
    AND INV_StockTransactionHeader.ReversalID IS NULL
GROUP BY 
    [SAL_DispatchHeader].[IsCancelled],
	SAL_OrderDetail.Orderid,
	INV_SET_Category.CategoryName, 
	INV_StockTransactionDetail.ItemID, 
	INV_Item.Description, 
	isnull(SET_UnitofMeasure.UOMName,UOM.UOMName), 
	SET_Location.LocationName,
	SL.LocationName,
	INV_StockTransactionHeader.DocumentNo, 
	INV_StockTransactionHeader.DocumentDate, 
	SET_DocumentType.DocumentType,
	SET_DocumentType.TransactionImpact, INV_Item.InventoryItemStatusCode, 
    INV_StockTransactionDetail.TransactionUOMID,
    INV_Item.PrimaryUOMCode ,
    INV_Item.VolumeUOMCode, 
    INV_Item.UnitVolume, 
    INV_Item.WeightUOMCode,INV_Item.UnitWeight, 
    Cast(INV_StockTransactionDetail.CreateDate as date)
    ,INV_StockTransactionDetail.ImpactID,
INV_StockTransactionDetail.ValueChange
,INV_StockTransactionHeader.StockTransactionID

) as TAB ) Tabfinal
";
                DataSet ds = null;

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_ForItemStock_LocationWise, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_ForItemStock_LocationWise, sqlparams);

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

        public static DataTable GetDataForDispatch_DashBoard(int CurrentCompanyID, int CurrentPeriodID, string doctypename, int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);

                
                const string Query_SugarProductionForDashBoard = @"

Select 
isnull((Select ABS(Tab.OnDate) as OnDate From (
( Select Sum(INV_StockTransactionDetail.TransactionQuantity) as OnDate ,
INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from [SAL_DispatchHeader] 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
		And Inv_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
		
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

inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = [SAL_OrderDetail].OrderID
		and SAL_OrderHeader.CompanyID = [SAL_OrderDetail].CompanyID
		and SAL_OrderHeader.PeriodID = [SAL_OrderDetail].PeriodID

			Where INV_StockTransactionHeader.DocumentTypeID = (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/Dispatch_frm.aspx' )
And DocumentType like '%@DocType%' And Status=1)
			 and INV_StockTransactionHeader.DocumentDate = CONVERT(date, GETDATE())
			AND INV_StockTransactionHeader.CompanyID = @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
						 AND SAL_DispatchHeader.StatusCode=1
{0}
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID) 
)Tab ),0) as OnDate,
ABS(Sum(INV_StockTransactionDetail.TransactionQuantity))as ToDate,
INV_StockTransactionHeader.CompanyID,

INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from [SAL_DispatchHeader] 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
		And Inv_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
		
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

inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = [SAL_OrderDetail].OrderID
		and SAL_OrderHeader.CompanyID = [SAL_OrderDetail].CompanyID
		and SAL_OrderHeader.PeriodID = [SAL_OrderDetail].PeriodID

			Where INV_StockTransactionHeader.CompanyID = @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
			 AND INV_StockTransactionHeader.DocumentTypeID = (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/Dispatch_frm.aspx' )
And DocumentType like '%@DocType%' And Status=1)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			 AND SAL_DispatchHeader.StatusCode=1
{0}
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID";
                DataSet ds = null;

                if (SeasonID > 0)
                    sb.Append(" and SAL_OrderHeader.Season= " + SeasonID + "");

                string query = Query_SugarProductionForDashBoard.Replace("@DocType", doctypename).ToString();

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetDataForDispatch_DashBoard_Shiekhoo(string CompanyID, int CurrentPeriodID, string doctypename, int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);

                CompanyID = "(" + CompanyID + ")";

  
                DataSet ds = null;

                if (SeasonID > 0)
                    sb.Append(" and SAL_ContractHeader.Season = " + SeasonID + "");

                string query = FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_DataForDispatch_DashBoard_Shiekhoo.Replace("@DocType", doctypename).Replace("@CompanyID", CompanyID).ToString();

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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


        public static DataTable GetDataFinishGoods_ForDashBoard(string CompanyID, int CurrentPeriodID,string typename,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                List<string> lst_company = CompanyID.Split(',').ToList();

                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select 
isnull((Select Tab.OnDate as OnDate From (
( Select Sum(INV_StockTransactionDetail.TransactionQuantity) as OnDate ,
INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from INV_StockTransactionDetail 

        INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			Where INV_StockTransactionHeader.CompanyID IN @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
			 AND INV_StockTransactionHeader.DocumentTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Inventory/Transaction/FinishedGoods.aspx' )
And DocumentType like '%@DocType%' and Status=1 and CompanyID IN @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
                AND INV_StockTransactionHeader.DocumentDate = CONVERT(date, GETDATE())
							 --AND INV_StockTransactionHeader.StatusCode=1
{0}

			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID) 
)Tab ),0) as OnDate,
Sum(INV_StockTransactionDetail.TransactionQuantity) as ToDate,
INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from INV_StockTransactionDetail 

        INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			Where INV_StockTransactionHeader.CompanyID IN @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
			 AND INV_StockTransactionHeader.DocumentTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Inventory/Transaction/FinishedGoods.aspx' )
And DocumentType like '%@DocType%' and Status=1 and COmpanyID IN @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			 --AND INV_StockTransactionHeader.StatusCode=1
{0}
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID";
                DataSet ds = null;

                CompanyID = "(" + CompanyID + ")";

                string query = Query_SugarProductionForDashBoard.Replace("@DocType", typename).Replace("@CompanyID", CompanyID);
                if (SeasonID > 0)
                    sb.Append(" and INV_StockTransactionHeader.BatchID = " + SeasonID + "");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetSaleOrderData_ForDashBoard(int CurrentCompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select 
isnull((Select Tab.OnDate as OnDate From (
( Select Sum(SAL_OrderDetail.Quantity) as OnDate ,
SAL_OrderHeader.CompanyID,
SAL_OrderHeader.PeriodID,
SAL_OrderHeader.SaleContractTypeID from SAL_OrderDetail

        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
            AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
            AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
			Where SAL_OrderHeader.CompanyID=@CompanyID
			AND SAL_OrderHeader.PeriodID=@PeriodID
{0}
			 AND SAL_OrderHeader.SaleContractTypeID=(Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesOrder_frm.aspx' )
And DocumentType like '%@doctpye%' and Status=1)
			 AND SAL_OrderHeader.status=1
			 AND SAL_OrderDetail.status=1
			 			 AND SAL_OrderHeader.StatusCode=1
                AND SAL_OrderHeader.ContractDate = CONVERT(date, GETDATE())

			Group By SAL_OrderHeader.CompanyID,
SAL_OrderHeader.PeriodID,
SAL_OrderHeader.SaleContractTypeID) 
)Tab ),0) as OnDate,
Sum(SAL_OrderDetail.Quantity) as ToDate,
SAL_OrderHeader.CompanyID,
SAL_OrderHeader.PeriodID,
SAL_OrderHeader.SaleContractTypeID from SAL_OrderDetail 

        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
            AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
            AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
			Where SAL_OrderHeader.CompanyID=@CompanyID
			AND SAL_OrderHeader.PeriodID=@PeriodID
			 AND SAL_OrderHeader.SaleContractTypeID=(Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesOrder_frm.aspx' )
And DocumentType like '%@doctpye%' and Status=1)
			 AND SAL_OrderHeader.status=1
			 AND SAL_OrderDetail.status=1
			 AND SAL_OrderHeader.StatusCode=1
{0}

			Group By SAL_OrderHeader.CompanyID,
SAL_OrderHeader.PeriodID,
SAL_OrderHeader.SaleContractTypeID";
                DataSet ds = null;

                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_OrderHeader.Season = '" + SeasonID + "'");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetSaleCOntractData_ForDashBoard(string CompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select 
isnull((Select Tab.OnDate as OnDate From (
( Select Sum(SAL_ContractDetail.Quantity) as OnDate ,
SAL_ContractHeader.CompanyID,
SAL_ContractHeader.PeriodID,
SAL_ContractHeader.SaleContractTypeID from SAL_ContractDetail

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID
            AND SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID 
            AND SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID 
			Where SAL_ContractHeader.CompanyID IN @CompanyID
			AND SAL_ContractHeader.PeriodID= @PeriodID
{0}
			 AND SAL_ContractHeader.SaleContractTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesContract_frm.aspx' )
And DocumentType like '%@doctype' and Status=1 and CompanyID IN @CompanyID)
			 AND SAL_ContractHeader.status=1
			 AND SAL_ContractDetail.status=1
			 			 AND SAL_ContractHeader.StatusCode=1
                AND SAL_ContractHeader.ContractDate = CONVERT(date, GETDATE())

			Group By SAL_ContractHeader.CompanyID,
SAL_ContractHeader.PeriodID,
SAL_ContractHeader.SaleContractTypeID) 
)Tab ),0) as OnDate,
Sum(SAL_ContractDetail.Quantity) as ToDate,
SAL_ContractHeader.CompanyID,
SAL_ContractHeader.PeriodID,
SAL_ContractHeader.SaleContractTypeID from SAL_ContractDetail 

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID
            AND SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID 
            AND SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID 
			Where SAL_ContractHeader.CompanyID IN @CompanyID
			AND SAL_ContractHeader.PeriodID= @PeriodID
			 AND SAL_ContractHeader.SaleContractTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesContract_frm.aspx' )
And DocumentType like '%@doctype' and Status=1 and CompanyID IN @CompanyID)
			 AND SAL_ContractHeader.status=1
			 AND SAL_ContractDetail.status=1
			 AND SAL_ContractHeader.StatusCode=1
{0}
			Group By SAL_ContractHeader.CompanyID,
SAL_ContractHeader.PeriodID,
SAL_ContractHeader.SaleContractTypeID


";


                CompanyID = "(" + CompanyID + ")";

                DataSet ds = null;

                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).Replace("@CompanyID", CompanyID).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_ContractHeader.Season = '" + SeasonID + "'");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetUnLiftedData_ForDashBoard(int CurrentCompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select 
isNull(ABS(Sum(SAL_DeliveryOrderDetail.Quantity)) - (Select  ABS(Sum(INV_StockTransactionDetail.TransactionQuantity))as ToDate from [SAL_DispatchHeader] 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
		And Inv_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
		
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
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID= SAL_OrderDetail.OrderID
		AND [SAL_OrderDetail].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_OrderHeader].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
			Where INV_StockTransactionHeader.CompanyID=@CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
{0}
			 AND INV_StockTransactionHeader.DocumentTypeID = (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/Dispatch_frm.aspx' )
And DocumentType like '%@doctype%' And Status=1 and CompanyID = @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			 AND SAL_DispatchHeader.StatusCode=1
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID),0) as UnliftedQuantity,
SAL_DeliveryOrderHeader.CompanyID,

SAL_DeliveryOrderHeader.PeriodID,
SAL_DeliveryOrderHeader.DocumentTypeID from SAL_DeliveryOrderDetail 
    INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DeliveryOrderHeader.[CompanyID] = SAL_DeliveryOrderDetail.[CompanyID] 
		AND SAL_DeliveryOrderHeader.[PeriodID] = SAL_DeliveryOrderDetail.[PeriodID] 
        AND SAL_DeliveryOrderHeader.[Status] = 1 
		And SAL_DeliveryOrderHeader.FiscalID = SAL_DeliveryOrderDetail.FiscalID

INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
        AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 

		inner join SAL_OrderHeader on SAL_OrderHeader.OrderID= SAL_OrderDetail.OrderID
		AND [SAL_OrderDetail].[CompanyID] = [SAL_OrderHeader].[CompanyID] 
        AND [SAL_OrderDetail].[PeriodID] = [SAL_OrderHeader].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
		
			Where SAL_DeliveryOrderHeader.CompanyID=@CompanyID
			AND SAL_DeliveryOrderHeader.PeriodID=@PeriodID
{0}
			 AND SAL_DeliveryOrderHeader.DocumentTypeID = (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/DeliveryOrder_frm.aspx' )
And DocumentType like '%@doctype%' And Status=1 and CompanyID = @CompanyID)
			 AND SAL_DeliveryOrderHeader.status=1
			 AND SAL_DeliveryOrderHeader.status=1
			 AND SAL_DeliveryOrderHeader.StatusCode=1
			Group By SAL_DeliveryOrderHeader.CompanyID,
SAL_DeliveryOrderHeader.PeriodID,
SAL_DeliveryOrderHeader.DocumentTypeID";
                DataSet ds = null;

                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_OrderHeader.Season = '" + SeasonID + "'");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetUnLiftedData_Shiekhoo_ForDashBoard(string CompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select 
isNull(ABS(Sum(SAL_DeliveryOrderDetail.Quantity)) - (Select  ABS(Sum(INV_StockTransactionDetail.TransactionQuantity))as ToDate from [SAL_DispatchHeader] 
    INNER JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[DocumentTypeID] = [SAL_DispatchHeader].[DocumentTypeID] 
        AND [INV_StockTransactionHeader].[DocumentNo] = [SAL_DispatchHeader].[DispatchNo] 
        AND [INV_StockTransactionHeader].[CompanyID] = [SAL_DispatchHeader].[CompanyID] AND [INV_StockTransactionHeader].[PeriodID] = [SAL_DispatchHeader].[PeriodID] 
        AND [INV_StockTransactionHeader].[Status] = 1 
		And Inv_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
		
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
    INNER JOIN [SAL_ContractDetail] ON [SAL_ContractDetail].[ContractDetailID] = [SAL_DeliveryOrderDetail].[ContractDetailID] 
        AND [SAL_ContractDetail].[ContractID] = [SAL_DeliveryOrderDetail].[ContractHeaderID] AND [SAL_ContractDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_ContractDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_ContractDetail].[Status] = 1 

	inner join SAL_ContractHeader on SAL_ContractHeader.ContractID = [SAL_ContractDetail].ContractID
		and SAL_ContractHeader.CompanyID = [SAL_ContractDetail].CompanyID
		and SAL_ContractHeader.PeriodID = [SAL_ContractDetail].PeriodID

			Where INV_StockTransactionHeader.CompanyID IN @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
{0}
			 AND INV_StockTransactionHeader.DocumentTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/Dispatch_New_frm.aspx' )
And DocumentType like '%@doctype' And Status=1 and CompanyID IN @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			-- AND SAL_DispatchHeader.StatusCode=1
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID),0) as UnliftedQuantity,
SAL_DeliveryOrderHeader.CompanyID,

SAL_DeliveryOrderHeader.PeriodID,
SAL_DeliveryOrderHeader.DocumentTypeID
 from SAL_DeliveryOrderDetail 
    INNER JOIN SAL_DeliveryOrderHeader ON SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DeliveryOrderHeader.[CompanyID] = SAL_DeliveryOrderDetail.[CompanyID] 
		AND SAL_DeliveryOrderHeader.[PeriodID] = SAL_DeliveryOrderDetail.[PeriodID] 
        AND SAL_DeliveryOrderHeader.[Status] = 1 
		And SAL_DeliveryOrderHeader.FiscalID = SAL_DeliveryOrderDetail.FiscalID

INNER JOIN [SAL_ContractDetail] ON [SAL_ContractDetail].[ContractDetailID] = [SAL_DeliveryOrderDetail].[ContractDetailID] 
        AND [SAL_ContractDetail].[ContractID] = [SAL_DeliveryOrderDetail].[ContractHeaderID] AND [SAL_ContractDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
        AND [SAL_ContractDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_ContractDetail].[Status] = 1 

		inner join SAL_ContractHeader on SAL_ContractHeader.ContractID = [SAL_ContractDetail].ContractID
		and SAL_ContractHeader.CompanyID = [SAL_ContractDetail].CompanyID
		and SAL_ContractHeader.PeriodID = [SAL_ContractDetail].PeriodID
		
			Where SAL_DeliveryOrderHeader.CompanyID IN @CompanyID
			AND SAL_DeliveryOrderHeader.PeriodID=@PeriodID
{0}
			 AND SAL_DeliveryOrderHeader.DocumentTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/DeliveryContract_frm.aspx' )
And DocumentType like '%@doctype' And Status=1 and CompanyID IN @CompanyID)
			 AND SAL_DeliveryOrderHeader.status=1
			 AND SAL_DeliveryOrderHeader.status=1
			 --AND SAL_DeliveryOrderHeader.StatusCode=1
			Group By SAL_DeliveryOrderHeader.CompanyID,
SAL_DeliveryOrderHeader.PeriodID,
SAL_DeliveryOrderHeader.DocumentTypeID


";

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = null;


                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).ToString().Replace("@CompanyID", CompanyID).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_ContractHeader.Season = '" + SeasonID + "'");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString()), sqlparams);

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

        public static DataTable GetUnSoldData_ForDashBoard(int CurrentCompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"Select ABS(Sum(INV_StockTransactionDetail.TransactionQuantity) - ((Select Sum(SAL_OrderDetail.Quantity)  from SAL_OrderDetail 

        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
            AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
            AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
			Where SAL_OrderHeader.CompanyID=@CompanyID
			AND SAL_OrderHeader.PeriodID=@PeriodID
			 AND SAL_OrderHeader.SaleContractTypeID=(Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesOrder_frm.aspx' )
And DocumentType like '%@doctype%' and Status=1 and SET_DocumentType.CompanyID =  @CompanyID)
			 AND SAL_OrderHeader.status=1
			 AND SAL_OrderDetail.status=1
			 AND SAL_OrderHeader.StatusCode=1
{0}
			Group By SAL_OrderHeader.CompanyID,
SAL_OrderHeader.PeriodID,
SAL_OrderHeader.SaleContractTypeID))) as UnSoldQty,
INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from INV_StockTransactionDetail 

        INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			Where INV_StockTransactionHeader.CompanyID=@CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
			 AND INV_StockTransactionHeader.DocumentTypeID=(Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Inventory/Transaction/FinishedGoods.aspx' )
And DocumentType like '%@doctype%' and Status=1 and SET_DocumentType.CompanyID =  @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			 AND INV_StockTransactionHeader.StatusCode=1
{1}
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID";
                DataSet ds = null;

                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_OrderHeader.Season = '" + SeasonID + "'");

                if (SeasonID > 0)
                    sb1.Append(" and INV_StockTransactionHeader.BatchID = " + SeasonID + "");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query,sb.ToString(),sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataTable GetUnSoldData_ForDashBoard_Sheikhoo(string CompanyID, int CurrentPeriodID,string doctype,int SeasonID, SqlTransaction objTransaction)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                //sqlparams[0] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[0] = new SqlParameter("@PeriodID", CurrentPeriodID);

                const string Query_SugarProductionForDashBoard = @"

Select ISNULL(ABS(Sum(INV_StockTransactionDetail.TransactionQuantity) - ((Select Sum(SAL_ContractDetail.Quantity)  from SAL_ContractDetail 

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID
            AND SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID 
            AND SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID 
			inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID
			and SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID
			Where SAL_ContractHeader.CompanyID IN @CompanyID
			AND SAL_ContractHeader.PeriodID=@PeriodID
			 AND SAL_ContractHeader.SaleContractTypeID = (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Sales/SalesContract_frm.aspx' )
And DocumentType like '%Sugar' and Status=1 and SET_DocumentType.CompanyID IN  @CompanyID)
			 AND SAL_ContractHeader.status=1
			 AND SAL_ContractDetail.status=1
			 AND SAL_ContractHeader.StatusCode=1
{0}
			 And DocumentType like '%@doctype%'
			Group By SAL_ContractHeader.CompanyID,
SAL_ContractHeader.PeriodID,
SAL_ContractHeader.SaleContractTypeID))),0) as UnSoldQty,
INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID from INV_StockTransactionDetail 

        INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
            AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
            AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
			Where INV_StockTransactionHeader.CompanyID IN @CompanyID
			AND INV_StockTransactionHeader.PeriodID=@PeriodID
			 AND INV_StockTransactionHeader.DocumentTypeID IN (Select DocumentTypeID from  SET_DocumentType 
Where MenuID =(Select MenuID from ApplicationMenu Where MenuPath ='/Inventory/Transaction/FinishedGoods.aspx' )
And DocumentType like '%@doctype%' and Status=1 and SET_DocumentType.CompanyID IN  @CompanyID)
			 AND INV_StockTransactionHeader.status=1
			 AND INV_StockTransactionDetail.status=1
			 --AND INV_StockTransactionHeader.StatusCode=1
{1}
			Group By INV_StockTransactionHeader.CompanyID,
INV_StockTransactionHeader.PeriodID,
INV_StockTransactionHeader.DocumentTypeID

";

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = null;
                string query = Query_SugarProductionForDashBoard.Replace("@doctype", doctype).ToString().Replace("@CompanyID", CompanyID).ToString();

                if (SeasonID > 0)
                    sb.Append(" and SAL_ContractHeader.Season = '" + SeasonID + "'");

                if (SeasonID > 0)
                    sb1.Append(" and INV_StockTransactionHeader.BatchID = " + SeasonID + "");

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(query, sb1.ToString(), sb.ToString()), sqlparams);

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

        public static decimal Get_SumAllTransactionQuantityByLocation_Validate(int itemID, int companyID, int periodID, int locationID, DateTime DateTo, SqlConnection objConnection = null)
        {
            decimal qtyAllTransaction = 0;
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@ItemID", itemID);
                sqlparams[1] = new SqlParameter("@CompanyId", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@LocationID", locationID);
                sqlparams[4] = new SqlParameter("@DateTo", DateTo);

                if (locationID != -1) //  For Pak Ethanol DB, Detail is replaced by Header table
                    sb.Append(" AND (INV_StockTransactionHeader.LocationID = @LocationID Or INV_StockTransactionDetail.LocationID = @LocationID) ");

                sb.Append("And INV_StockTransactionHeader.DocumentDate <= @DateTo");

                if (objConnection == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SumAllTransactionQuantityByLocation, sb.ToString()), sqlparams).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_SumAllTransactionQuantityByLocation, sqlparams).Tables[0];

                if (dt.Rows.Count > 0)
                    qtyAllTransaction = decimal.Parse(dt.Rows[0][0].ToString() != string.Empty ? dt.Rows[0][0].ToString() : "0");
                else
                    qtyAllTransaction = 0;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return qtyAllTransaction;
        }

        public static DataTable ItemStockStatusAccountCodeWise_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> CatalogID, Nullable<int> itemCategoryID, Nullable<int> ItemID, int ObsoleteCheck,
        Nullable<int> locationID, Nullable<int> jobID = null, bool IsDispatchCancelHide = false, bool isVehicle = false, bool isDetail = false, short? documentGroupID = null, int ApprovingGroup = 0, string CodeCombinationID = "", bool chk_exclude_transfers = false, bool filterZeroResults = false, string DocumentTypes = "", int? DepartmentID = null, int? CostCenterID = null, string Companies = null)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();
            string strFilter = "";
            try
            {
                //sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                if (!string.IsNullOrEmpty(CodeCombinationID))
                    sb.Append(" And INV_Item.CodeCombinationID ='" + CodeCombinationID + "'");

                if (ApprovingGroup > 0)
                    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (isVehicle)
                    sb.Append(" AND isnull(DownloadableFlag,0) = 0");


                if (chk_exclude_transfers == true)
                    sb.Append(" AND SET_DocumentType.DocumentType not like '%transfer%in%' AND SET_DocumentType.DocumentType not like '%transfer%out%'");


                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (CatalogID.HasValue)
                    sb.Append(" And INV_Item.ItemCatalogGroupID =" + CatalogID);

                if (ObsoleteCheck > 0)
                {
                    sb.Append(" And INV_Item.Obsolete = " + ObsoleteCheck);
                }

                if (itemCategoryID.HasValue)
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (locationID.HasValue)
                    sb.Append(" AND SET_Location.LocationID = " + locationID);
                

                if (jobID.HasValue)
                {
                    sb.Append(" AND isnull(JobID,0) = " + jobID );
                }

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }

                DataSet ds = null;

                if (strFilter == "where")
                    strFilter = "";

                if (DepartmentID > 0)
                    sb.Append(" AND SET_Department.DepartmentID = " + DepartmentID);

                if (CostCenterID > 0)
                    sb.Append(" AND SET_CostCenter.CostCenterID = " + CostCenterID);

                string query = null;

                if (isDetail == true)
                {
                    query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StockStatusCodeWiseSummary;

                    Companies = "(" + Companies + ")";

                    query = query.Replace("@CompanyID", Companies).ToString();

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());
                }
                else
                {
                    if (filterZeroResults)
                    {
                        //sb2.Append(" having ( (Abs(sum(OpeningQty)) + Abs(sum(TransferINQty)) + Abs(sum(StockReturnTransferQty) )) - Abs(sum(TransferQty)) ) != 0 ");
                        sb2.Append(" having ABS(SUM(OpeningQty)) != 0  OR ABS(SUM(TransferINQty)) != 0 OR ABS(SUM(StockReturnTransferQty)) != 0  OR ABS(SUM(TransferQty)) != 0 ");
                    }

                    query = FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StockStatusCodeWise;

                    Companies = "(" + Companies + ")";

                    query = query.Replace("@CompanyID", Companies).ToString();

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, sb.ToString(), sb1.ToString(), sb2.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());
                }

                if (ds.Tables[0] != null)
                    dtReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtReturn;
        }



        public static DataTable StoreMovementDetailReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentGroupID = null, int? ItemCatalogId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentGroupID", DocumentGroupID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@AsOnDate", AsOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_StoreMovementDetail.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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


        public static DataTable SalesPromotionAndExpenceDetailReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentTypeID = null, int? ItemCatalogId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                //sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                //sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                //sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[1] = new SqlParameter("@AsOnDate", AsOnDate);

                companies = "(" + companies + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_SalesPromotionAndExpenceDetail.Replace("@CompanyID", companies) + sb.ToString(), sqlparams);

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


        public static DataSet SalesPromotionAndExpenceSummaryReport(string companyID, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentTypeID = null, int? ItemCatalogId = null)
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                //sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyID);
                //sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_SalesPromotionAndExpenceSummary.Replace("@CompanyID", companyID) + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }


        public static DataTable SalesComparisonReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentTypeID = null, int? ItemCatalogId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@AsOnDate", AsOnDate);

                

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SalesComparison.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable SalesComparisonMonthWiseReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentTypeID = null, int? ItemCatalogId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@AsOnDate", AsOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_SalesComparisonMonthWise.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable AvgGrossSalePriceFiveYearsReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, DateTime AsOnDate, int? DocumentTypeID = null, int? ItemCatalogId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@AsOnDate", AsOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader2.Query_AvgGrossSalePriceFiveYears.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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


        public static DataTable AgingAnalysisClosingBalanceDetail(string companies, int CurrentCompanyID, int periodID, DateTime AsOnDate, int? LocationId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);
                sqlparams[3] = new SqlParameter("@LocationID", LocationId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_AgingAnalysisClosingBalanceDetail.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable GetAgingAnalysisClosingBalanceSummary(string companies, int CurrentCompanyID, int periodID, DateTime AsOnDate, int? LocationId = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[2] = new SqlParameter("@AsOnDate", AsOnDate);
                sqlparams[3] = new SqlParameter("@LocationID", LocationId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_TransactionDetail.Query_AgingAnalysisClosingBalanceSummary.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable StoreMovementHeldForCapitalExpenditureReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate =null, Nullable<DateTime> ToDate =null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@ItemCatalogId", ItemCatalogID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoreMovementHeldForCapitalExpenditure.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable DetailOfMajorStoresReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int? ItemCatalogID = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@ItemCatalogId", ItemCatalogID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", CurrentCompanyID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[4] = new SqlParameter("@FromDate", FromDate);
                sqlparams[5] = new SqlParameter("@ToDate", ToDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_DetailOfMajorStores.Replace("@CompanyID", "(" + companies + ")") + sb.ToString(), sqlparams);

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

        public static DataTable CharityAndDonationReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                //sqlparams[1] = new SqlParameter("@CompanyID", CurrentCompanyID);

                string Query_CharityAndDonationReport = @"
              				select    GL_CodeCombinationDetail.Description,GL_CodeCombinationDetail.CodeCombinationID,
				  SUM((case when GL_TransactionDetail.TransactionType = 'Debit' then GL_TransactionDetail.Amount else GL_TransactionDetail.Amount * -1 end)) as TotalAmount
				   from GL_TransactionDetail 

				   left join GL_TransactionHeader on GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID
                    and GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID
				   and GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID
                left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.ChartOfAccountID = GL_TransactionDetail.ChartOfAccountID 
				 and GL_CodeCombinationDetail.CodeCombinationID = GL_TransactionDetail.CodeCombinationID  
			     and GL_CodeCombinationDetail.Status = 1
				left join SET_AccountType on SET_AccountType.AccountTypeID = GL_CodeCombinationDetail.AccountTypeID and SET_AccountType.Status = 1
                --left join SET_DocumentType on SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID and SET_DocumentType.Status = 1
				left JOIN SET_DocumentType ON SET_DocumentType.CompanyID = GL_TransactionDetail.CompanyID 
				    AND SET_DocumentType.PeriodID = GL_TransactionDetail.PeriodID 
                    AND SET_DocumentType.DocumentTypeID = GL_TransactionHeader.DocumentTypeID 
					and SET_DocumentType.Status = 1
				where SET_AccountType.AccountTypeName like 'Expenses' and GL_TransactionHeader.DocumentDate between @FromDate and @ToDate
                and GL_TransactionHeader.PeriodID = @PeriodID and GL_TransactionHeader.Status = 1 --and GL_TransactionDetail.FiscalID = @CurrentFiscalID
                and GL_TransactionHeader.StatusCode = 1 and GL_TransactionDetail.Status = 1
                and 
				( 
                GL_CodeCombinationDetail.Description like '%Charity%'
                --GL_CodeCombinationDetail.CodeCombinationID like '07-01-06-%' 
                and GL_CodeCombinationDetail.DetailPostingAllowedFlag = 1
                )
                and GL_TransactionHeader.CompanyID in ( " + companies + " ) group by GL_CodeCombinationDetail.Description, GL_CodeCombinationDetail.CodeCombinationID order by GL_CodeCombinationDetail.Description asc ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_CharityAndDonationReport, sqlparams);

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

        public static DataTable SummaryOfRelatedPartyTransactionsReport(string companyID, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                companyID = "(" + companyID + ")";
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SummaryOfRelatedPartyTransactionsReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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

        public static DataTable SummaryOfRelatedPartyTransactionsBalanceReport(string companyID, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SummaryOfRelatedPartyTransactionsBalanceReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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

        public static DataTable StoresIssuanceRelatedPartyReport(string companies, int CurrentCompanyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                sb.Append(companies);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_StoresIssuanceRelatedPartyReport, sb.ToString()), sqlparams);

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
       
        public static DataTable SummaryOfCWIPReport(int periodID, Nullable<DateTime> asOnDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", asOnDate);
        
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_SummaryOfCWIP, sqlparams);

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


        public static DataTable TransactionsWithRelatedPartyReport(string companyID, int periodID, int CurrentFiscalID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, Nullable<DateTime> FromDatePrevious = null, Nullable<DateTime> ToDatePrevious = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@FromDate", FromDate);
                sqlparams[2] = new SqlParameter("@ToDate", ToDate);
                sqlparams[3] = new SqlParameter("@FromDatePrevious", FromDatePrevious);
                sqlparams[4] = new SqlParameter("@ToDatePrevious", ToDatePrevious);

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_TransactionsWithRelatedPartyReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable ItemStockStatusDepartmentWise_Report(int companyID, int periodID, DateTime dateFrom, DateTime dateTo, bool sortbydescription, bool sortbyinventorycode, Nullable<int> itemCategoryID, Nullable<int> ItemID,
        Nullable<int> locationID, Nullable<int> itemcatalogid, Nullable<int> jobID = null, short? documentGroupID = null, string CodeCombinationIDfrom = "", string CodeCombinationIDto = "", string DocumentTypes = "", Nullable<int> CostCenterID = 0, string HeadingCostCenter = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            DataTable dtReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
            StringBuilder sort = new StringBuilder();
            string strFilter = "";
            try
            {
                sqlParams.Add(new SqlParameter("@CompanyID", companyID));
                sqlParams.Add(new SqlParameter("@PeriodID", periodID));
                sqlParams.Add(new SqlParameter("@FromDate", dateFrom));
                sqlParams.Add(new SqlParameter("@ToDate", dateTo));

                if (ItemID.HasValue)
                {
                    sb.Append(" And INV_Item.ItemID =" + ItemID);
                }
                else
                { sb.Append("  "); }

                if (!string.IsNullOrEmpty(CodeCombinationIDfrom) && !string.IsNullOrEmpty(CodeCombinationIDto))
                    sb.Append(" And INV_Item.CodeCombinationID between '" + CodeCombinationIDfrom + "' and '" + CodeCombinationIDto + "'");

                //if (ApprovingGroup > 0)
                //    sb.Append(" And INV_Item.ApprovalItemGroupID = " + ApprovingGroup.ToString());

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                //if (isVehicle)
                //    sb.Append(" AND isnull(DownloadableFlag,0) = 0");


                //if (chk_exclude_transfers == true)
                //    sb.Append(" AND SET_DocumentType.DocumentType not like '%transfer%in%' AND SET_DocumentType.DocumentType not like '%transfer%out%'");


                if (DocumentTypes != "")
                {
                    sb1.Append(" and SET_DocumentType.DocumentTypeID in (" + DocumentTypes + ") ");
                }

                if (itemCategoryID.HasValue)
                {
                    sqlParams.Add(new SqlParameter("@ItemCategory", itemCategoryID.Value));
                    sb.Append(" and CategoryID =  " + itemCategoryID.Value);
                }
                else
                    sqlParams.Add(new SqlParameter("@ItemCategory", DBNull.Value));

                if (itemcatalogid.HasValue)
                {
                    sb.Append(" and INV_Item.ItemCatalogGroupID =  " + itemcatalogid.Value);
                }

                if (locationID.HasValue)
                    sqlParams.Add(new SqlParameter("@LocationID", locationID.Value));
                else
                    sqlParams.Add(new SqlParameter("@LocationID", DBNull.Value));

                if (jobID.HasValue)
                {
                    sb.Append(" AND isnull(JobID,0) = " + jobID);
                }


                if (CostCenterID.HasValue && CostCenterID > 0)
                {
                    sb.Append(" AND SET_CostCenter.CostCenterID = " + CostCenterID);
                }

                if (HeadingCostCenter != "")
                {
                    sb.Append(" AND SET_CostCenter.HeadingCostCenterName = '" + HeadingCostCenter + "'");
                }

                if (sortbydescription == true)
                {
                    sort.Append(" order by FinTAb.ItemDescription asc ");
                }
                else if (sortbyinventorycode == true)
                {
                    sort.Append(" order by FinTAb.InventoryItemStatusCode asc ");
                }

                DataSet ds = null;

                if (strFilter == "where")
                    strFilter = "";


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_GetStockReportDepartmetnWise, sb.ToString(), sb1.ToString(), strFilter, sort.ToString()), sqlParams.ToArray());


                if (ds.Tables[0] != null)
                    dtReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtReturn;
        }

        public static DataTable GetCwipMovementReportData(Nullable<int> companyID, Nullable<int> periodid, int CurrentFiscalID = 0, Nullable<DateTime> asOnDate = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodid);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                sqlparams[3] = new SqlParameter("@FromDate", asOnDate);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Reports.INV_StockTransactionDetail.Query_GetCwipMovementReportData), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetDataForPurchaseReturn(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber, int grnNumberTo,
            Nullable<int> reqfrom, Nullable<int> reqto, SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1,
            Nullable<int> PurchaseSupplier = -1, Nullable<int> doctypeid = null, int? DocumentStatus = null, short? documentGroupID = null, int? ApprovingGroupID = -1
            , int LocationID = -1, int FiscalID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                //if (poNumber > 0)
                //{
                //    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                //}

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0 && grnNumberTo > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] between " + grnNumber.ToString() + " and " + grnNumberTo.ToString() + " ";
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;

                //if (reqfrom != null && reqto != null)
                //{
                //    AddtionalParameters += "and AP_PurchaseOrderRequisition.DocumentNo between " + reqfrom + " and " + reqto + "";

                //}
                if (DocumentStatus != null && DocumentStatus > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[StatusCode] = " + DocumentStatus;
                }

                if (documentGroupID.HasValue)
                    AddtionalParameters += " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (ApprovingGroupID.Value > 0)
                    AddtionalParameters += " and [INV_StockTransactionDetail].stocktransactionid in (select [INV_StockTransactionDetail].StockTransactionID from [INV_Item],[INV_StockTransactionDetail] where [INV_Item].[ItemID] = [INV_StockTransactionDetail].[ItemID] AND [INV_Item].[Status] = 1 and Inv_item.ApprovalItemGroupID = " + ApprovingGroupID + " )";

                if (LocationID != -1)
                {
                    AddtionalParameters += " and INV_StockTransactionHeader.locationID= " + LocationID.ToString();
                }

                if (FiscalID > 0)
                    AddtionalParameters += " and INV_StockTransactionHeader.FiscalID= " + FiscalID;


                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PurchaseReturnReport + AddtionalParameters), sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_PurchaseReturnReport + AddtionalParameters), sqlParms);
                }



                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }

        public static DataTable GetDataForInOutDetailReport(int CompanyID, int PeriodID, Nullable<DateTime> FromDate,Nullable<DateTime> ToDate, Nullable<int> ItemID)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                if (ItemID > 0)
                    sb.Append(" AND INV_StockTransactionDetail.ItemID = " + ItemID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionHeader.Query_GetDataForInOutDetailReport, sb.ToString()), sqlparams);

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

        public static bool DeleteDataByHeaderKeyForStockRequisition(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, long StockTransactionID, int companyID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@Status", false);
                sqlparams[2] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[3] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[4] = new SqlParameter("@UpdatedByValue", updatedByValue);
                sqlparams[5] = new SqlParameter("@CompanyID", companyID);


                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByHeaderKeyForStockRequisition, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByHeaderKeyForStockRequisition, sqlparams) > 0)
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

        public static DataTable GetDataForStockRequistionReport_JK(DateTime? DateFrom, DateTime? DateTo, int companyID, int periodID, int grnNumber,
            SqlConnection objConnection = null, Nullable<int> categoryID = 0, Nullable<int> poNumber = -1, Nullable<int> PurchaseSupplier = -1,
            Nullable<int> doctypeid = null, short? documentGroupID = null, Nullable<int> locationID = 0)
        {
            DataTable dtresult = null;
            try
            {
                DataSet ds = null;

                string AddtionalParameters = string.Empty;

                SqlParameter[] sqlParms = new SqlParameter[4];

                sqlParms[0] = new SqlParameter("@CompanyID", companyID);
                sqlParms[1] = new SqlParameter("@PeriodID", periodID);
                sqlParms[2] = new SqlParameter("@FromDate", DateFrom);
                sqlParms[3] = new SqlParameter("@ToDate", DateTo);
                //sqlParms[4] = new SqlParameter("@CategoryID", categoryID);

                //AddtionalParameters += " and INV_StockTransactionHeader.StatusCode = 1";
                if (poNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionDetail].[SourceCode] = " + poNumber.ToString();
                }

                if (PurchaseSupplier > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[BusinessPartnerID] = " + PurchaseSupplier.ToString();
                }


                if (categoryID > 0)
                {
                    AddtionalParameters += "AND [INV_SET_Category].[CategoryID] = " + categoryID.ToString();
                }

                if (grnNumber > 0)
                {
                    AddtionalParameters += "AND [INV_StockTransactionHeader].[DocumentNo] = " + grnNumber.ToString() + " ";
                }

                if (locationID > 0)
                {
                    AddtionalParameters += " And [SET_Location].[LocationID] = " + locationID;
                }

                if (doctypeid.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND [Set_DocumentType].[DocumentTypeID] = " + doctypeid;


                if (documentGroupID.HasValue)
                    AddtionalParameters = AddtionalParameters + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                if (objConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport_JK + AddtionalParameters, sqlParms);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_StockRequistionReport_JK + AddtionalParameters, sqlParms);
                }

                if (ds.Tables.Count > 0)
                    return ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtresult;
        }


        public static bool UpdateDispatchQuantity(SqlTransaction objTransaction, DateTime updateDate, long StockTransactionID, long TransactionDetailID,
            decimal TransactionQuantity)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@StockTransactionID", StockTransactionID);
                sqlparams[1] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                sqlparams[2] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[3] = new SqlParameter("@TransactionQuantity", TransactionQuantity);

                const string Query_UpdateDispatchQuantity = @"Update [INV_StockTransactionDetail] 
Set [UpdateDate] = @UpdateDate, TransactionQuantity = (TransactionQuantity - @TransactionQuantity), Amount = (TransactionQuantity - @TransactionQuantity) * TransactionCost
WHERE [StockTransactionID] = @StockTransactionID and TransactionDetailID = @TransactionDetailID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDispatchQuantity, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDispatchQuantity, sqlparams) > 0)
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

        public static DataTable GetDataByKeyTableManualProduction(int stockTransactionID, int companyID, int periodID, bool status = true, SqlConnection objConnection = null,
            bool isSugarMill = false)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@StockTransactionID", stockTransactionID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", status);

                DataSet ds = null;

                if (isSugarMill)
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ManualProduction, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ManualProduction, sqlparams);
                }
                else
                {
                    if (objConnection == null)
                        ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ManualProduction, sqlparams);
                    else
                        ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_ManualProduction, sqlparams);
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

        public static DataTable GetSugarDataForDashboard(int companyID, int periodID, DateTime fromDate, DateTime toDate)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_SugarSale_Dashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetMolassesDataForDashboard(int companyID, int periodID, DateTime fromDate, DateTime toDate)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_MolassesSale_Dashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetBaggasseDataForDashboard(int companyID, int periodID, DateTime fromDate, DateTime toDate)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BaggasseSale_Dashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetBankPositionDataForDashboard(int companyID, int periodID, DateTime fromDate, DateTime toDate)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Inventory.Transaction.INV_StockTransactionDetail.Query_BankPosition_Dashboard, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
    }
}
