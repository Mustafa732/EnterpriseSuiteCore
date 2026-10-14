using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
  public partial  class SAL_ContractDetail : BaseClass
    {
        #region Constant Queries

      const string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor = @"

SELECT [SAL_ContractDetail].[ContractDetailID], [SAL_ContractDetail].PricePerKG,
[SAL_ContractDetail].[ContractID], [SAL_ContractDetail].[CompanyID], 
[SAL_ContractDetail].[PeriodID], [SAL_ContractDetail].[SerialNo], 
    [SAL_ContractDetail].[ItemCategoryID], [SAL_ContractDetail].[ItemID],[SAL_ContractDetail].[ProductLine],
[SAL_ContractDetail].[ItemUnitID], [SAL_ContractDetail].[ItemUnitQuantity], 
    [SAL_ContractDetail].[ItemQuality], [SAL_ContractDetail].[Quantity], [SAL_ContractDetail].[QtyInTon],
[SAL_ContractDetail].[ColorID],
[SAL_ContractDetail].[BaseAmount], 
    [SAL_ContractDetail].[Rate], [SAL_ContractDetail].[GrossAmount], 
CASE WHEN ISNULL(AR_SET_Customer.IsRegistered,0) = 0 
	THEN 
	( 
	
	(case when SAL_ContractDetail.Inclusive = 1 then
		--!st case Sugar 36 No
		   --(select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and dbo.SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc ) 
		   -- * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) * SAL_ContractDetail.Quantity
		   
		   dbo.GetBiggerValue(SAL_ContractDetail.Rate  , (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and dbo.SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc ) ) 
		   * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) * SAL_ContractDetail.Quantity
		   
   else 

	   (case when SET_DocumentType.DocumentType like '%billet%' 
	   then 
			(        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount/ 100 ) * SAL_ContractDetail.Quantity
		else
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0)= isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) 
		end)

   end)
   

	)
	ELSE
    (
	
	dbo.SAL_ContractDetail.SalesTaxAmount  *

(case when ( 
	SAL_ContractDetail.Rate <= 
	
	--(case when SET_DocumentType.DocumentType  like '%billet%' 
	(case when 
(select top 1 isnull(Inclusive,0) as Inclusive
from SET_SalesTaxParameters
where Status = 1
and SET_SalesTaxParameters.PeriodID = SAL_ContractDetail.PeriodID
and SET_SalesTaxParameters.Item = SAL_ContractDetail.ItemID
and SAL_ContractHeader.ContractDate between FromDate and ToDate
AND ISNULL(SET_SalesTaxParameters.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)
order by SET_SalesTaxParameters.SalesTaxParameterID desc) = 1
	then
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and dbo.SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0) order by CreateDate desc ) 
	else
		(        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1  AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  ORDER by CreateDate desc )  - (        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount ))) 
	end ) 
	 )

   then 
   
   (case when SAL_ContractDetail.Inclusive = 1 then
		--!st case Sugar 36 No
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate BETWEEN pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) * SAL_ContractDetail.Quantity
   else 

	   (case when SET_DocumentType.DocumentType like '%billet%' 
	   then 
			(        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0) = isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount/ 100 ) * SAL_ContractDetail.Quantity
		else
		        (select top 1 TUnitPrice from SET_SalesTaxParameters pm where pm.Item = SAL_ContractDetail.itemid and SAL_ContractHeader.ContractDate between pm.FromDate and pm.ToDate and pm.Status = 1 and pm.IsActive = 1 AND ISNULL(pm.IsRegistered,0)= isnull(AR_SET_Customer.IsRegistered,0)  order by CreateDate desc )  * SAL_ContractDetail.SalesTaxAmount / (100 + SAL_ContractDetail.SalesTaxAmount) 
		end)

   end)
   else
	
	( SAL_ContractDetail.[NetAmount] -
	  ((SAL_ContractDetail.Quantity *  ( INV_Item.UnitWeight * (SAL_ContractDetail.PricePerKG + 
	(Case When (SAL_ContractHeader.ContractDate < '2019-07-01' and Set_DocumentType.DocumentType like '%Sugar%' 
				and AR_SET_Customer.IsRegistered = 0 )
				then  3.60 
				else
				0 end))))
	 * ( SAL_ContractDetail.WithHolding )) / (100 + SAL_ContractDetail.WithHolding) 
	 )
	 
 
   end )
   
   / (100 + SAL_ContractDetail.SalesTaxAmount )
	)
	end

 as TaxAmount,

[SAL_ContractDetail].[IsSalesTaxExempted], [SAL_ContractDetail].[SalesTaxAmount], 
    [SAL_ContractDetail].[SalesTaxFixRate], [SAL_ContractDetail].[SalesTaxPercent], 
[SAL_ContractDetail].[IsExciseDutyExempted], [SAL_ContractDetail].[ExciseDutyAmount], 
    [SAL_ContractDetail].[IsFEDExempted],[SAL_ContractDetail].[WithHolding], [SAL_ContractDetail].[FEDAmount],
[SAL_ContractDetail].[VATAmount], [SAL_ContractDetail].[GodownCharges], [SAL_ContractDetail].[NetAmount], 
    [SAL_ContractDetail].[Status], [SAL_ContractDetail].[CreatedBy], [SAL_ContractDetail].[CreateDate], [SAL_ContractDetail].[CreatedByValue], [SAL_ContractDetail].[UpdatedBy], 
    [SAL_ContractDetail].[UpdateDate], [SAL_ContractDetail].[UpdatedByValue],
[SAL_ContractDetail].[Inclusive], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, [SAL_ContractDetail].ReversedQuantity,
    ISNULL([SAL_ContractDetail].[Quantity], 0) - ISNULL(
    (
        select SUM([SAL_DeliveryOrderDetail].Quantity) from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = [SAL_ContractDetail].[CompanyID] and PeriodID = [SAL_ContractDetail].[PeriodID] 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = [SAL_ContractDetail].ContractDetailID
    ), 0) as RemainingQTY, 
    ISNULL([SAL_ContractDetail].[Rate] * ([SAL_ContractDetail].[Quantity] - 
    (
        select SUM([SAL_DeliveryOrderDetail].Quantity) as sumQuantity from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = [SAL_ContractDetail].[CompanyID] and PeriodID = [SAL_ContractDetail].[PeriodID]
    )), 0) AS TotalAmount, 
    '' As ColorName,isnull(T_Rate,0) as T_Rate,isnull(T_GrossAmount,0) as T_GrossAmount,'' [Action] 
,isnull(ExciseDutyActualAmount,0) as ExciseDutyActualAmount
,isnull(FEDActualAmount,0) as FEDActualAmount,isnull(WithHoldingAmount,0) as WithHoldingAmount
,isnull(SAL_ContractDetail.FCAmount,0) as FCAmount
,isnull(SAL_ContractDetail.FCRate,0) as FCRate
,[SAL_ContractDetail].[FEDFixRate]
,[SAL_ContractDetail].[FEDFixAmount]
FROM [SAL_ContractDetail] 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_ContractDetail].[ItemCategoryID]
      --  AND [INV_SET_Category].[CompanyID] = [SAL_ContractDetail].[CompanyID]
AND [INV_SET_Category].[Status] = 1
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_ContractDetail].[ItemID] 
--AND [INV_Item].[CompanyID] = [SAL_ContractDetail].[CompanyID]
AND [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_ContractDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 

	left join SAL_ContractHeader on SAL_ContractHeader.ContractID = [SAL_ContractDetail] .ContractID and SAL_ContractHeader.Status = 1
	and SAL_ContractHeader.CompanyID = @CompanyID and SAL_ContractHeader.PeriodID = @PeriodID

LEFT JOIN dbo.AR_SET_Customer ON AR_SET_Customer.CustomerID = SAL_ContractHeader.CustomerID

left join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID
	and SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID

WHERE [SAL_ContractDetail].[ContractID] = @OrderID and [SAL_ContractDetail].[CompanyID] = @CompanyID and [SAL_ContractDetail].[PeriodID] = @PeriodID 
    and [SAL_ContractDetail].[Status] = 1 ORDER BY [SAL_ContractDetail].[SerialNo]

";

        const string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColorSOS = @"
SELECT [SAL_OrderDetail].[OrderDetailID], 
[SAL_OrderDetail].[OrderID], [SAL_OrderDetail].[CompanyID], 
[SAL_OrderDetail].[PeriodID], [SAL_OrderDetail].[SerialNo], 
    [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID],[SAL_OrderDetail].[ProductLine],
[SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[ItemUnitQuantity], 
    [SAL_OrderDetail].[ItemQuality], [SAL_OrderDetail].[Quantity],
[SAL_OrderDetail].[ColorID],
[SAL_OrderDetail].[BaseAmount], 
    [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[GrossAmount], 
[SAL_OrderDetail].[IsSalesTaxExempted], [SAL_OrderDetail].[SalesTaxAmount], 
    [SAL_OrderDetail].[SalesTaxFixRate], [SAL_OrderDetail].[SalesTaxPercent], 
[SAL_OrderDetail].[IsExciseDutyExempted], [SAL_OrderDetail].[ExciseDutyAmount], 
    [SAL_OrderDetail].[IsFEDExempted], [SAL_OrderDetail].[FEDAmount],
[SAL_OrderDetail].[VATAmount], [SAL_OrderDetail].[GodownCharges], [SAL_OrderDetail].[NetAmount], 
    [SAL_OrderDetail].[Status], [SAL_OrderDetail].[CreatedBy], [SAL_OrderDetail].[CreateDate], [SAL_OrderDetail].[CreatedByValue], [SAL_OrderDetail].[UpdatedBy], 
    [SAL_OrderDetail].[UpdateDate], [SAL_OrderDetail].[UpdatedByValue],
[SAL_OrderDetail].[Inclusive], [INV_SET_Category].[CategoryName], [INV_Item].[Description], 
    [SET_UnitofMeasure].[UOMName], ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, 
    ISNULL([SAL_OrderDetail].[Quantity], 0) - ISNULL(
    (
        select SUM([SAL_DeliveryOrderDetail].Quantity) from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = [SAL_OrderDetail].[CompanyID] and PeriodID = [SAL_OrderDetail].[PeriodID] 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
    ), 0) as RemainingQTY, 
    ISNULL([SAL_OrderDetail].[Rate] * ([SAL_OrderDetail].[Quantity] - 
    (
        select SUM([SAL_DeliveryOrderDetail].Quantity) as sumQuantity from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = [SAL_OrderDetail].[CompanyID] and PeriodID = [SAL_OrderDetail].[PeriodID]
    )), 0) AS TotalAmount, 
    '' As ColorName,isnull(T_Rate,0) as T_Rate,isnull(T_GrossAmount,0) as T_GrossAmount,
    '' [Action] 
FROM [SAL_OrderDetail] 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] AND [INV_SET_Category].[Status] = 1 
    LEFT JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] AND [INV_Item].[CompanyID] = [SAL_OrderDetail].[CompanyID] AND [INV_Item].[Status] = 1 
    LEFT JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE [SAL_OrderDetail].[OrderID] = @OrderID and [SAL_OrderDetail].[CompanyID] = @CompanyID and [SAL_OrderDetail].[PeriodID] = @PeriodID 
    and [SAL_OrderDetail].[Status] = 1 
ORDER BY [SAL_OrderDetail].[SerialNo]";


        const string Query_GetSalesOrderData = @" 
select SAL_OrderDetail.ItemID, 
	INV_Item.Description, 
	convert (varchar(10),SAL_OrderHeader.ContractDate,105) as ContractDate,
	ISNULL(referenceno, '') as referenceno,
    SAL_OrderDetail.OrderDetailID,
    ISNULL(SAL_OrderHeader.DepartmentID,0) as DepartmentID,
    ISNULL(SET_Department.DepartmentName, '') as DepartmentName
from SAL_OrderDetail
inner join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID
	--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
	and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
	and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
left join SET_Department on SET_Department.DepartmentID = SAL_OrderHeader.DepartmentID	
where SAL_OrderDetail.OrderID = @OrderID
	and SAL_OrderDetail.CompanyID = @CompanyID
	and SAL_OrderDetail.PeriodID = @PeriodID

";

        const string Query_GetSalesOrderOnline = @" 
select SAL_OrderDetail.ItemID, 
	INV_Item.Description, 
	convert (varchar(10),SAL_OrderHeader.ContractDate,105) as ContractDate,
	ISNULL(referenceno, '') as referenceno,
    SAL_OrderDetail.OrderDetailID,
    ISNULL(SAL_OrderHeader.DepartmentID,0) as DepartmentID,
    ISNULL(SET_Department.DepartmentName, '') as DepartmentName,
    INV_Item.description,
    SAL_OrderDetail.Quantity
from SAL_OrderDetail
inner join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID
	--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
	and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
	and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
left join SET_Department on SET_Department.DepartmentID = SAL_OrderHeader.DepartmentID	
where  SAL_OrderDetail.CompanyID = @CompanyID
	and SAL_OrderDetail.PeriodID = @PeriodID
    and SAL_OrderHeader.appid is not null

";

        const string Query_UpdateReversedQty = @" Update Sal_ContractDetail
Set ReversedQuantity = @ReversedQuantity
Where ContractDetailID = @ContractDetailID AND ContractID = @ContractID AND CompanyID = @CompanyID AND PeriodID = @PeriodID";


        #endregion

        #region Class Methods

       
        #region SO Scheduling


        public static DataTable GetDataByKeyTableSOS(int contractID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@ContractID", contractID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodIdSOS, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodIdSOS, sqlparams);

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


        public static DataTable GetDataByKey_WithColor(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColorSO, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColorSO, sqlparams);

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

        public short GetNewSerialNo(int orderID, int companyID, int periodID)
        {
            short maxSerialNo = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_NewSerialNo_ByCompanyPeriodOrderIDs = @" SELECT ISNULL(MAX(SerialNo), 0) + 1 AS SerialNo FROM SAL_OrderDetail 
WHERE OrderID = @OrderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID ";

                maxSerialNo = Convert.ToInt16(SqlHelper.ExecuteScalar(CommandType.Text, Query_NewSerialNo_ByCompanyPeriodOrderIDs, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return maxSerialNo;
        }
          



        public static DataTable GetDataByKeyTableWithoutColorSOS(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColorSOS, sqlparams);

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

        public static DataTable GetDataByKeyTableWithoutColor(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor, sqlparams);

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

        public DataTable GetDataByKeyTable(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                    //    sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                        sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,
                        FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodId, sqlparams);
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

        public DataTable GetDataByKeyTableNew(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                    //    sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodId_New,
                        sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,
                        FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_CompanyId_PeriodId_New, sqlparams);
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


        public DataTable GetDetailDataOfContractForSO(int orderID,int orderDetailID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@ContractDetailID", orderDetailID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                    //    sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_OrderDetailID_CompanyId_PeriodId,
                        sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,
                        FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_OrderDetailID_CompanyId_PeriodId, sqlparams);
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

        public DataTable GetDetailDataOfContractForSOUpdate(int orderID, int orderDetailID, int companyID, int periodID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@ContractDetailID", orderDetailID);

                DataSet ds = null;

                if (objConnection == null)
                {
                    //ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                    //    sqlparams);
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_OrderDetailID_CompanyId_PeriodIdUpdate,
                        sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,
                        FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_OrderDetailID_CompanyId_PeriodIdUpdate, sqlparams);
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

        public static DataTable GetDataByKeyTableWithColor(int companyID, int periodID, int orderID, SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_WithColor, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_ContractDetail.Query_Select_By_OrderID_WithColor, sqlparams);

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
