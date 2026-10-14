using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_OrderDetail : BaseClass
    {
        #region Constant Queries

        const string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor = @"
SELECT [SAL_OrderDetail].[OrderDetailID], [SAL_OrderDetail].PricePerKG,
[SAL_OrderDetail].[OrderID], [SAL_OrderDetail].[CompanyID], 
[SAL_OrderDetail].[PeriodID], [SAL_OrderDetail].[SerialNo], 
    [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID],[SAL_OrderDetail].[ProductLine],
[SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[ItemUnitQuantity], 
    [SAL_OrderDetail].[ItemQuality], [SAL_OrderDetail].[Quantity], [SAL_OrderDetail].[QtyInTon],
[SAL_OrderDetail].[ColorID],
[SAL_OrderDetail].[BaseAmount], 
    [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[GrossAmount]

--,(([SAL_OrderDetail].[T_GrossAmount]/100)* [SAL_OrderDetail].[SalesTaxAmount])as TaxAmount,

,(case when ( 
	SAL_OrderDetail.Rate <= 
	
	(case when 

    --SET_DocumentType.DocumentType like '%billet%' 
(select distinct isnull(Inclusive,0) as Inclusive
from SET_SalesTaxParameters
where Status = 1
and SET_SalesTaxParameters.PeriodID = SAL_OrderDetail.PeriodID
and SET_SalesTaxParameters.Item = SAL_OrderDetail.ItemID
and SAL_OrderHeader.ContractDate between FromDate and ToDate) = 1

	then
		SAL_OrderDetail.T_Rate
	else
		(SAL_OrderDetail.T_Rate - (SAL_OrderDetail.T_Rate * SAL_OrderDetail.SalesTaxAmount / (100 + SAL_OrderDetail.SalesTaxAmount))) 
	end ) 
	 )

   then 
   
   (case when SAL_OrderDetail.Inclusive = 1 then
		--!st case Sugar 36 No
		SAL_OrderDetail.T_Rate * SAL_OrderDetail.SalesTaxAmount / (100 + SAL_OrderDetail.SalesTaxAmount) * SAL_OrderDetail.Quantity
   else 

	   (case when SET_DocumentType.DocumentType like '%billet%' 
	   then 
			(SAL_OrderDetail.T_Rate * SAL_OrderDetail.SalesTaxAmount / 100 ) *  SAL_OrderDetail.Quantity
		else
		SAL_OrderDetail.T_Rate * SAL_OrderDetail.SalesTaxAmount / (100 + SAL_OrderDetail.SalesTaxAmount) 
		end)

   end)
   else
		((([SAL_OrderDetail].[NetAmount]-[SAL_OrderDetail].WithHoldingAmount)  *  [SAL_OrderDetail].[SalesTaxAmount]) / (100+  [SAL_OrderDetail].[SalesTaxAmount]))
   end	 
	 )as TaxAmount, 


[SAL_OrderDetail].[IsSalesTaxExempted], [SAL_OrderDetail].[SalesTaxAmount], 
    [SAL_OrderDetail].[SalesTaxFixRate], [SAL_OrderDetail].[SalesTaxPercent], 
[SAL_OrderDetail].[IsExciseDutyExempted], [SAL_OrderDetail].[ExciseDutyAmount], 
    [SAL_OrderDetail].[IsFEDExempted],[SAL_OrderDetail].[WithHolding], [SAL_OrderDetail].[FEDAmount],
[SAL_OrderDetail].[VATAmount], [SAL_OrderDetail].[GodownCharges], [SAL_OrderDetail].[NetAmount], 
    [SAL_OrderDetail].[Status], [SAL_OrderDetail].[CreatedBy], [SAL_OrderDetail].[CreateDate], [SAL_OrderDetail].[CreatedByValue], [SAL_OrderDetail].[UpdatedBy], 
    [SAL_OrderDetail].[UpdateDate], [SAL_OrderDetail].[UpdatedByValue],
[SAL_OrderDetail].[Inclusive], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, [SAL_OrderDetail].ReversedQuantity,
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
    '' As ColorName,isnull(T_Rate,0) as T_Rate,isnull(T_GrossAmount,0) as T_GrossAmount,'' [Action] 
,isnull(ExciseDutyActualAmount,0) as ExciseDutyActualAmount
,isnull(FEDActualAmount,0) as FEDActualAmount,
isnull(WithHoldingAmount,0) as WithHoldingAmount
,isnull(FEDFixRate,0) AS FEDFixRate
,isnull(FEDFixAmount,0) AS FEDFixAmount
FROM [SAL_OrderDetail] 
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
and SAL_OrderHeader.Status = 1

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID
and SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID]
         AND [INV_SET_Category].[Status] = 1
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] AND  [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE [SAL_OrderDetail].[OrderID] = @OrderID and [SAL_OrderDetail].[CompanyID] = @CompanyID and [SAL_OrderDetail].[PeriodID] = @PeriodID 
    and [SAL_OrderDetail].[Status] = 1 ORDER BY [SAL_OrderDetail].[SerialNo]";


        const string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColorServices = @"
SELECT [SAL_OrderDetail].[OrderDetailID], [SAL_OrderDetail].PricePerKG,
[SAL_OrderDetail].[OrderID], [SAL_OrderDetail].[CompanyID], 
[SAL_OrderDetail].[PeriodID], [SAL_OrderDetail].[SerialNo], 
    [SAL_OrderDetail].[ItemCategoryID], [SAL_OrderDetail].[ItemID],[SAL_OrderDetail].[ProductLine],
[SAL_OrderDetail].[ItemUnitID], [SAL_OrderDetail].[ItemUnitQuantity], 
    [SAL_OrderDetail].[ItemQuality], [SAL_OrderDetail].[Quantity], [SAL_OrderDetail].[QtyInTon],
[SAL_OrderDetail].[ColorID],
[SAL_OrderDetail].[BaseAmount], 
    [SAL_OrderDetail].[Rate], [SAL_OrderDetail].[GrossAmount]

--,(([SAL_OrderDetail].[T_GrossAmount]/100)* [SAL_OrderDetail].[SalesTaxAmount])as TaxAmount,

,case when SAL_OrderDetail.T_GrossAmount > SAL_OrderDetail.GrossAmount then 
	(SAL_OrderDetail.T_GrossAmount * [SAL_OrderDetail].[SalesTaxAmount] / 
	(case when SAL_OrderDetail.Inclusive = 1 then ([SAL_OrderDetail].[SalesTaxAmount] + 100) else 
	(case when [SAL_OrderDetail].[SalesTaxAmount] != 0 then [SAL_OrderDetail].[SalesTaxAmount] else 1 end)
	
	end ))

	else 
	(SAL_OrderDetail.GrossAmount * [SAL_OrderDetail].[SalesTaxAmount] / 
	(case when SAL_OrderDetail.Inclusive = 1 then ([SAL_OrderDetail].[SalesTaxAmount] + isnull([SAL_OrderDetail].ExciseDutyAmount,0) + 100) 
	else
	(case when [SAL_OrderDetail].[SalesTaxAmount] != 0 then [SAL_OrderDetail].[SalesTaxAmount] else 1 end)

	end ))
	end as TaxAmount,


[SAL_OrderDetail].[IsSalesTaxExempted], [SAL_OrderDetail].[SalesTaxAmount], 
    [SAL_OrderDetail].[SalesTaxFixRate], [SAL_OrderDetail].[SalesTaxPercent], 
[SAL_OrderDetail].[IsExciseDutyExempted], [SAL_OrderDetail].[ExciseDutyAmount], 
    [SAL_OrderDetail].[IsFEDExempted],[SAL_OrderDetail].[WithHolding], [SAL_OrderDetail].[FEDAmount],
[SAL_OrderDetail].[VATAmount], [SAL_OrderDetail].[GodownCharges], [SAL_OrderDetail].[NetAmount], 
    [SAL_OrderDetail].[Status], [SAL_OrderDetail].[CreatedBy], [SAL_OrderDetail].[CreateDate], [SAL_OrderDetail].[CreatedByValue], [SAL_OrderDetail].[UpdatedBy], 
    [SAL_OrderDetail].[UpdateDate], [SAL_OrderDetail].[UpdatedByValue],
[SAL_OrderDetail].[Inclusive], [INV_SET_Category].[CategoryName], [INV_Item].[Description], [SET_UnitofMeasure].[UOMName], ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, [SAL_OrderDetail].ReversedQuantity,
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
    '' As ColorName,isnull(T_Rate,0) as T_Rate,isnull(T_GrossAmount,0) as T_GrossAmount,'' [Action] 
,isnull(ExciseDutyActualAmount,0) as ExciseDutyActualAmount
,isnull(FEDActualAmount,0) as FEDActualAmount,
isnull(WithHoldingAmount,0) as WithHoldingAmount
FROM [SAL_OrderDetail] 
    INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID]
         AND [INV_SET_Category].[Status] = 1
    INNER JOIN [INV_Item] ON [INV_Item].[ItemID] = [SAL_OrderDetail].[ItemID] AND  [INV_Item].[Status] = 1 
    INNER JOIN [SET_UnitofMeasure] ON [SET_UnitofMeasure].[UOMID] = [SAL_OrderDetail].[ItemUnitID] AND [SET_UnitofMeasure].[Status] = 1 
WHERE [SAL_OrderDetail].[OrderID] = @OrderID and [SAL_OrderDetail].[CompanyID] = @CompanyID and [SAL_OrderDetail].[PeriodID] = @PeriodID 
    and [SAL_OrderDetail].[Status] = 1 ORDER BY [SAL_OrderDetail].[SerialNo]";


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
    '' [Action], [SAL_OrderDetail].[FEDFixRate], [SAL_OrderDetail].[FEDFixAmount]
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

        const string Query_UpdateReversedQty = @" Update Sal_OrderDetail
Set ReversedQuantity = @ReversedQuantity
Where OrderDetailID = @OrderDetailID AND OrderID = @OrderID AND CompanyID = @CompanyID AND PeriodID = @PeriodID";


        #endregion

        #region Class Methods

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
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId,
                        sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text,
                        FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodId, sqlparams);
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
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColor, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColor, sqlparams);

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

        public static DataTable GetDataByKeyTableWithColorForDelete(int companyID, int periodID, int orderID, int[] SODetailID)
        {
            DataTable dtToReturn = null;
            StringBuilder additionalParameter = new StringBuilder();
            string Where = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                int counter = 0;

                foreach (int SODID in SODetailID)
                {
                    if (counter > 0)
                        additionalParameter.Append("," + SODID);
                    else
                        additionalParameter.Append(SODID);
                    counter++;
                }

                Where = "and SAL_OrderDetail.OrderDetailID in (" + additionalParameter + ") ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColorForDelete, Where), sqlparams);

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


        #region SO Scheduling

        

        public static DataTable GetDataByKeyTableSOS(int orderID, int companyID, int periodID, SqlConnection objConnection = null)
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
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodIdSOS, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_CompanyId_PeriodIdSOS, sqlparams);

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

        public static DataTable GetSalesOrderData(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalesOrderData, sqlparams);

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


        public static DataTable GetAllSaleOrderData(int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalesOrderOnline, sqlparams);

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


        public DataTable GetSalesOrderQuantity(int orderID, int ItemID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@ItemID", ItemID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetSalesOrderQuantity = @" select OrderDetailID, Quantity from SAL_OrderDetail 
where ItemID = @ItemID and OrderID = @OrderID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalesOrderQuantity, sqlparams);

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

        public DataTable GetSalesOrderItemQuantity(int orderID, int orderDetailID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@OrderDetailID", orderDetailID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetSalesOrderItemQuantity = @" select OrderDetailID, Quantity from SAL_OrderDetail 
where OrderID = @OrderID and OrderDetailID = @OrderDetailID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetSalesOrderItemQuantity, sqlparams);

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

        public DataTable GetItemBySalesOrder(int orderID, int orderDetailID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@OrderDetailID", orderDetailID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                string Query_GetItemBySalesOrder = @" select ItemID, Quantity from SAL_OrderDetail 
where OrderID = @OrderID and OrderDetailID = @OrderDetailID and CompanyID = @CompanyID and PeriodID = @PeriodID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetItemBySalesOrder, sqlparams);

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

        public static DataTable GetDispatchSalesRecievedQuantity(int companyID, int periodID, int categoryID = 0, Nullable<DateTime> fromDate = null,
            Nullable<DateTime> toDate = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                StringBuilder sb = new StringBuilder();

                if (categoryID > 0)
                {
                    sb.Append(" and INV_SET_Category.CategoryID=" + categoryID);
                }
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDispatchSalesRecievedQuantity, sb.ToString()), sqlparams);

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

        public static DataTable GetDataByKeyTableWithColorForUpdate(int companyID, int periodID, int orderID, int[] SODetailID, int[] SODetailRemainingQTY, int[] SODetailTotalAmount)
        {
            DataTable dtToReturn = null;
            StringBuilder additionalParameter = new StringBuilder();
            string Where = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                int counter = 0;

                foreach (int SODID in SODetailID)
                {
                    if (counter > 0)
                        additionalParameter.Append("," + SODID);
                    else
                        additionalParameter.Append(SODID);
                    counter++;
                }

                Where = "and SAL_OrderDetail.OrderDetailID in (" + additionalParameter + ") ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Select_By_OrderID_WithColorForDelete, Where), sqlparams);

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


        public static DataSet GetDataForLiftingReport(int itemID, int customerID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, int companyID,
            int periodID, string menuPath, int batchID, int Balance, int DocumentTypeID)
        {
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                StringBuilder sb = new StringBuilder();


                if (itemID > 0)
                {
                    sb.Append(" and Inv_Item.ItemID = @ItemID ");
                    sqlparams[0] = new SqlParameter("@ItemID", itemID);
                }
                else
                sqlparams[0] = new SqlParameter("@ItemID", DBNull.Value);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                if (customerID > 0)
                {
                    sb.Append(" and Sal_OrderHeader.CustomerID = @CustomerID ");
                    sqlparams[3] = new SqlParameter("@CustomerID", customerID);
                }
                else
                    sqlparams[3] = new SqlParameter("@CustomerID", DBNull.Value);

                if (fromDate.HasValue)
                    sqlparams[4] = new SqlParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00");

                if (toDate.HasValue)
                    sqlparams[5] = new SqlParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59");

                if (batchID > 0)
                {
                    sb.Append(" and Sal_OrderHeader.Season = @BatchID ");
                    sqlparams[6] = new SqlParameter("@BatchID", batchID);
                }
                else
                    sqlparams[6] = new SqlParameter("@BatchID", DBNull.Value);

                sqlparams[7] = new SqlParameter("@MenuPath", menuPath);

                if (DocumentTypeID > 0)
                {
                    sb.Append(" and Sal_OrderHeader.SaleContractTypeID = @DocumentTypeID ");
                    sqlparams[8] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                }
                else
                    sqlparams[8] = new SqlParameter("@DocumentTypeID", DBNull.Value);

                string strQuery = "";

                if (Balance == 0)
                    strQuery = FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDataLiftingReport.Replace("{1}", " where isnull(TAB.ContractQuantity,0) - isnull(TAB.QuantityToDate,0) > 0 ").ToString();
                else
                    strQuery = FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDataLiftingReport.Replace("{1}", "").ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(strQuery, sb.ToString()), sqlparams);

                if (ds != null)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataSet GetItemTrackingReport(string InventoryItemStatusCode)
        {
            DataSet dsToReturn = null;
            StringBuilder sb = new StringBuilder();
            sb.Append("where INV_Item.InventoryItemStatusCode = '" + InventoryItemStatusCode + "'");

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[0];

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Qurey_GetItemTrackingReport, sb.ToString()), sqlparams);

                if (ds != null)
                    dsToReturn = ds;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }

        public static DataTable GetDataForAveragePriceReport(int itemID, int customerID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, int companyID,
            int periodID, string menuPath, int batchID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (fromDate.HasValue)
                    sqlparams[2] = new SqlParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00");

                if (toDate.HasValue)
                    sqlparams[3] = new SqlParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59");

                string strQuery = FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDataAveragePriceReport;

                if (itemID > 0)
                    strQuery = strQuery.Replace("{0}", " AND (Inv_Item.ItemID = " + itemID + " ").ToString();
                else
                    strQuery = strQuery.Replace("{0}", "").ToString();

                if (customerID > 0)
                    strQuery = strQuery.Replace("{1}", " AND ( Sal_OrderHeader.CustomerID = " + customerID + " ) ").ToString();
                else
                    strQuery = strQuery.Replace("{1}", "").ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, strQuery, sqlparams);

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

        public bool UpdateReversedQty(SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@OrderDetailID", f_OrderDetailID);
                sqlparams[1] = new SqlParameter("@OrderID", f_OrderID);
                sqlparams[2] = new SqlParameter("@CompanyID", f_CompanyID);
                sqlparams[3] = new SqlParameter("@PeriodID", f_PeriodID);

                if (f_ReversedQuantity.HasValue)
                    sqlparams[4] = new SqlParameter("@ReversedQuantity", f_ReversedQuantity.Value);
                else
                    sqlparams[4] = new SqlParameter("@ReversedQuantity", DBNull.Value);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateReversedQty, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateReversedQty, sqlparams) > 0)
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

        public static DataTable GetDataForLiftingUnliftingReport(int itemID, int customerID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, int companyID,
          int periodID, string menuPath, string DispatchmenuPath, int batchID, int Balance, Nullable<int> documentTypeID, Nullable<short> documentStatus)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            string havingFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];

                #region Filter by Item

                if (itemID > 0)
                    sqlparams[0] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[0] = new SqlParameter("@ItemID", DBNull.Value);

                #endregion

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Filter by Customer

                if (customerID > 0)
                {
                    whereFilter.Append(" and Sal_DeliveryOrderHeader.CustomerID = @CustomerID ");
                    sqlparams[3] = new SqlParameter("@CustomerID", customerID);
                }
                else
                    sqlparams[3] = new SqlParameter("@CustomerID", DBNull.Value);

                #endregion

                if (fromDate.HasValue)
                    sqlparams[4] = new SqlParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00");

                if (toDate.HasValue)
                    sqlparams[5] = new SqlParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59");

                #region Filter by Batch

                if (batchID > 0)
                    sqlparams[6] = new SqlParameter("@BatchID", batchID);
                else
                    sqlparams[6] = new SqlParameter("@BatchID", DBNull.Value);

                #endregion

                sqlparams[7] = new SqlParameter("@MenuPath", menuPath);
                sqlparams[8] = new SqlParameter("@DispatchmenuPath", DispatchmenuPath);

                if (documentTypeID.HasValue)
                    whereFilter.Append(" AND Sal_DeliveryOrderHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentStatus.HasValue)
                    whereFilter.Append(" AND Sal_DeliveryOrderHeader.StatusCode = " + documentStatus.Value);

                if (Balance == 0)
                    havingFilter = " having (sum(FINTAB.ContractQuantity) - sum(FINTAB.QuantityToDate)) > 0 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDataLiftingUnliftingReport, whereFilter.ToString(), havingFilter),
                    sqlparams);

                if (ds != null)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }

        public static DataTable GetDataForLiftingUnliftingReports(int itemID, int customerID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate, int companyID,
  int periodID, string menuPath, string DispatchmenuPath, int Balance, Nullable<int> documentTypeID, Nullable<short> documentStatus,int? batchID)
        {
            DataTable dtToReturn = null;
            StringBuilder whereFilter = new StringBuilder();
            string havingFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];

                #region Filter by Item

                if (itemID > 0)
                    sqlparams[0] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[0] = new SqlParameter("@ItemID", DBNull.Value);

                #endregion

                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Filter by Customer

                if (customerID > 0)
                {
                    whereFilter.Append(" and Sal_DeliveryOrderHeader.CustomerID = @CustomerID ");
                    sqlparams[3] = new SqlParameter("@CustomerID", customerID);
                }
                else
                    sqlparams[3] = new SqlParameter("@CustomerID", DBNull.Value);

                #endregion

                if (fromDate.HasValue)
                    sqlparams[4] = new SqlParameter("@FromDate", fromDate.Value.ToString("yyyy-MM-dd") + " 00:00:00");

                if (toDate.HasValue)
                    sqlparams[5] = new SqlParameter("@ToDate", toDate.Value.ToString("yyyy-MM-dd") + " 23:59:59");

                #region Filter by Batch

                if (batchID > 0)
                    sqlparams[6] = new SqlParameter("@BatchID", batchID);
                else
                    sqlparams[6] = new SqlParameter("@BatchID", DBNull.Value);

                #endregion

                sqlparams[7] = new SqlParameter("@MenuPath", menuPath);
                sqlparams[8] = new SqlParameter("@DispatchmenuPath", DispatchmenuPath);

                if (documentTypeID.HasValue)
                    whereFilter.Append(" AND Sal_DeliveryOrderHeader.DocumentTypeID = " + documentTypeID.Value);

                if (documentStatus.HasValue)
                    whereFilter.Append(" AND Sal_DeliveryOrderHeader.StatusCode = " + documentStatus.Value);

                if (Balance == 0)
                    havingFilter = " having (sum(FINTAB.ContractQuantity) - sum(FINTAB.QuantityToDate)) > 0 ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetDataLiftingUnliftingReport, whereFilter.ToString(), havingFilter),
                    sqlparams);

                if (ds != null)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;
        }



        public static DataSet CustomerWiseSaleOrderReport(int PeriodID, int companyID, int customerIdFrom, int customerIdTo, string sal_from, string sal_to,
            string del_from, string del_to, bool AllDispatches, bool UnInvoiceSales, bool InvoicedSales, Nullable<DateTime> fromDate, Nullable<DateTime> TODate,
            SqlConnection ObjConnection = null, string orderBy = "", int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            #region Order By Clause

            string orderClause = string.Empty;
            //orderClause = "Order By AR_Cst.customerID , SAL_OD.OrderID";

            //switch (orderBy)
            //{
            //    case "ContractDate":
            //        orderClause = " Order By SAL_OH.ContractDate ";
            //        break;

            //    case "SONumber":
            //        orderClause = " Order By SAL_OH.ContractDate ";
            //        break;

            //    case "DONumber":
            //        //orderClause = " Order By Cast(SAL_DOH.DeliveryOrderNo  As int)";
            //        orderClause = " Order By SAL_DOH.DODate  ";
            //        break;

            //    case "CustomerName":
            //        orderClause = " Order By AR_Cst.CustomerName ";
            //        break;
            //}

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ContractDateStart", fromDate);
                sqlparams[3] = new SqlParameter("@ContractDateEnd", TODate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);
                

                if (customerIdFrom > 0 && customerIdTo > 0)
                    sb.Append("AND SAL_OH.CustomerID between " + customerIdFrom + " AND " + customerIdTo);
                else if (customerIdFrom > 0 && customerIdTo == 0)
                    sb.Append("AND SAL_OH.CustomerID = " + customerIdFrom);
                else if (customerIdTo > 0 && customerIdFrom == 0)
                    sb.Append("AND SAL_OH.CustomerID = " + customerIdTo);

                if (sal_from != "" && sal_to != "")
                    sb.Append(" and SAL_OH.ContractNo between " + int.Parse(sal_from.ToString()) + " and " + int.Parse(sal_to.ToString()) + "");

                if (del_from != "" && del_to != "")
                    sb.Append(" and SAL_DOH.DeliveryOrderNo between " + int.Parse(del_from.ToString()) + " and " + int.Parse(del_to.ToString()) + "");

                if (AllDispatches == true) { }
                else if (UnInvoiceSales == true)
                    sb.Append("and [SAL_InvoiceHeader].InvoiceID is null");
                else if (InvoicedSales == true)
                    sb.Append("and [SAL_InvoiceHeader].InvoiceID is not null");


                if (DocumentTypeID > 0)
                {
                    sb.Append("and SAL_OH.SaleContractTypeID = " + DocumentTypeID + "");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ObjConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Customer_Wise_Sale_Order_Report, sb.ToString(), orderClause), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(ObjConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Customer_Wise_Sale_Order_Report, sb.ToString(), orderClause),
                        sqlparams);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataSet GetAdvanceAgainstSOReport(int PeriodID, int companyID, int customerIdFrom, int customerIdTo, string sal_from, string sal_to,Nullable<DateTime> fromDate, Nullable<DateTime> TODate,
           SqlConnection ObjConnection = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

          
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ContractDateStart", fromDate);
                sqlparams[3] = new SqlParameter("@ContractDateEnd", TODate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);


                if (customerIdFrom > 0 && customerIdTo > 0)
                    sb.Append("AND SAL_ContractHeader.CustomerID between " + customerIdFrom + " AND " + customerIdTo);
                else if (customerIdFrom > 0 && customerIdTo == 0)
                    sb.Append("AND SAL_ContractHeader.CustomerID = " + customerIdFrom);
                else if (customerIdTo > 0 && customerIdFrom == 0)
                    sb.Append("AND SAL_ContractHeader.CustomerID = " + customerIdTo);

                if (sal_from != "" && sal_to != "")
                    sb.Append(" and SAL_ContractHeader.ContractNo between " + int.Parse(sal_from.ToString()) + " and " + int.Parse(sal_to.ToString()) + "");
              
                if (DocumentTypeID > 0)
                {
                    sb.Append("and SAL_ContractHeader.SaleContractTypeID = " + DocumentTypeID + "");
                }

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (ObjConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Advacne_Against_SO, sb.ToString()), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(ObjConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Advacne_Against_SO, sb.ToString()),
                        sqlparams);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable ManufacturingCostingPerUnit(int companyID, int periodID, int DocumentNo)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);


                StringBuilder sb = new StringBuilder();

                //if (categoryID > 0)
                //{
                //    sb.Append(" and INV_SET_Category.CategoryID=" + categoryID);
                //}
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_ManufacturingCostingPerUni, sb.ToString()), sqlparams);

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


        public static DataSet CustomerSaleOrderDetail_ItemWiseReport(int PeriodID, int companyID, int customerIdFrom, int customerIdTo, string sal_from, string sal_to,
            string del_from, string del_to, bool AllDispatches, bool UnInvoiceSales, bool InvoicedSales, Nullable<DateTime> fromDate, Nullable<DateTime> TODate,
            SqlConnection ObjConnection = null)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ContractDateStart", fromDate);
                sqlparams[3] = new SqlParameter("@ContractDateEnd", TODate);

                if (customerIdFrom > 0 && customerIdTo > 0)
                    sb.Append("AND SAL_OH.CustomerID between " + customerIdFrom + " AND " + customerIdTo);
                else if (customerIdFrom > 0 && customerIdTo == 0)
                    sb.Append("AND SAL_OH.CustomerID = " + customerIdFrom);
                else if (customerIdTo > 0 && customerIdFrom == 0)
                    sb.Append("AND SAL_OH.CustomerID = " + customerIdTo);

                if (sal_from != "" && sal_to != "")
                    sb.Append(" and SAL_OH.ContractNo between " + int.Parse(sal_from.ToString()) + " and " + int.Parse(sal_to.ToString()) + "");

                if (del_from != "" && del_to != "")
                    sb.Append(" and SAL_DOH.DeliveryOrderNo between " + int.Parse(del_from.ToString()) + " and " + int.Parse(del_to.ToString()) + "");

                if (AllDispatches == true) { }
                else if (UnInvoiceSales == true)
                    sb.Append("and [SAL_InvoiceHeader].InvoiceID is null");
                else if (InvoicedSales == true)
                    sb.Append("and [SAL_InvoiceHeader].InvoiceID is not null");

                if (ObjConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Customer_Wise_Sale_OrderItem_Wise_Report, sb.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(ObjConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_Customer_Wise_Sale_OrderItem_Wise_Report, sb.ToString()), sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }


        public static DataSet LIFTINGDISPATCHReport(int PeriodID, int companyID, int customerIdFrom, int customerIdTo, string sal_from, string sal_to,
            string del_from, string del_to, bool AllDispatches, bool UnInvoiceSales, bool InvoicedSales, Nullable<DateTime> fromDate, Nullable<DateTime> TODate,
            SqlConnection ObjConnection = null, string orderBy = "", int DocumentTypeID = 0)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@ContractDateStart", fromDate);
                sqlparams[3] = new SqlParameter("@ContractDateEnd", TODate);

                if (customerIdFrom > 0 && customerIdTo > 0)
                    sb.Append("AND Sal_OrderHeader.CustomerID between " + customerIdFrom + " AND " + customerIdTo);
                else if (customerIdFrom > 0 && customerIdTo == 0)
                    sb.Append("AND Sal_OrderHeader.CustomerID = " + customerIdFrom);
                else if (customerIdTo > 0 && customerIdFrom == 0)
                    sb.Append("AND Sal_OrderHeader.CustomerID = " + customerIdTo);

                if (sal_from != "" && sal_to != "")
                    sb.Append(" and Sal_OrderHeader.ContractNo between " + int.Parse(sal_from.ToString()) + " and " + int.Parse(sal_to.ToString()) + "");

                if (del_from != "" && del_to != "")
                    sb.Append(" and sal_deliveryOrderHeader.DeliveryOrderNo between " + int.Parse(del_from.ToString()) + " and " + int.Parse(del_to.ToString()) + "");

                
                if (DocumentTypeID > 0)
                {
                    sb.Append("and Sal_OrderHeader.SaleContractTypeID = " + DocumentTypeID + "");
                }

                if (ObjConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_LIFTINGDISPATCHReport, sb.ToString()), sqlparams);
                else
                {
                    ds = SqlHelper.ExecuteDataset(ObjConnection, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_LIFTINGDISPATCHReport, sb.ToString()),
                        sqlparams);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public DataTable GetDataByKeyTableWithoutColorForAlimdad(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor = @" 
SELECT SAL_OrderDetail.OrderDetailID, SAL_OrderDetail.OrderID, SAL_OrderDetail.CompanyID, SAL_OrderDetail.PeriodID, SAL_OrderDetail.SerialNo, 
    SAL_OrderDetail.ItemCategoryID, SAL_OrderDetail.ItemID, SAL_OrderDetail.ProductLine, SAL_OrderDetail.ItemUnitID, SAL_OrderDetail.ItemUnitQuantity, 
    SAL_OrderDetail.ItemQuality, SAL_OrderDetail.Quantity, SAL_OrderDetail.ColorID, SAL_OrderDetail.BaseAmount, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, 
    SAL_OrderDetail.IsSalesTaxExempted, SAL_OrderDetail.SalesTaxAmount, SAL_OrderDetail.SalesTaxFixRate, SAL_OrderDetail.SalesTaxPercent, 
    SAL_OrderDetail.IsExciseDutyExempted, SAL_OrderDetail.ExciseDutyAmount, SAL_OrderDetail.IsFEDExempted, SAL_OrderDetail.FEDAmount,SAL_OrderDetail.VATAmount, 
    SAL_OrderDetail.GodownCharges, SAL_OrderDetail.NetAmount, SAL_OrderDetail.Status, SAL_OrderDetail.CreatedBy, SAL_OrderDetail.CreateDate, 
    SAL_OrderDetail.CreatedByValue, SAL_OrderDetail.UpdatedBy, SAL_OrderDetail.UpdateDate, SAL_OrderDetail.UpdatedByValue, SAL_OrderDetail.Inclusive, 
    INV_SET_Category.CategoryName, INV_Item.Description, SET_UnitofMeasure.UOMName, ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, 
    SAL_OrderDetail.ReversedQuantity, ISNULL(SAL_OrderDetail.Quantity, 0) - ISNULL(
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
    ), 0) as RemainingQTY, 
    ISNULL(SAL_OrderDetail.Rate * (SAL_OrderDetail.Quantity - 
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) as sumQuantity from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID
    )), 0) AS TotalAmount, 
    '' As ColorName, '' [Action] ,NoteToVendor, Set_Batch.[Description] as Batch, SAL_OrderDetail.BatchId 
	,case when [SAL_OrderDetail].[SalesTaxAmount] IS Not null then isnull([SAL_OrderDetail].[GrossAmount] * [SAL_OrderDetail].[SalesTaxAmount],1)/100  else 0 end as SalesTaxAmountA
	,SET_ItemCatalog.ItemCatalogID as CatalogID,SET_ItemCatalog.Description as CatalogName, null as CurrentStock, null as UnitWeight, null as IssuedQty
	,null as FinishCatalogID,'' as FinishCatalogName,null as FinishCategoryID,'' as FinishCategoryName,null as FinishItemID ,'' as FinishItemDescription,
	null as ExpectedQuantity,null as Wastage,null as DifferenceInPcs,null as ActualQuantity, null as DOQty,null as BalanceQty 
FROM SAL_OrderDetail 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID 
--AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID 
--AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
AND INV_Item.Status = 1 
    left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
	left join SET_ItemCatalog on SET_ItemCatalog.ItemCatalogID = INV_Item.ItemCatalogGroupID
LEFT JOIN Set_Batch ON Set_Batch.BatchID = SAL_OrderDetail.BatchId --and Set_Batch.CompanyID = SAL_OrderDetail.CompanyID
AND   Set_Batch.PeriodID = SAL_OrderDetail.PeriodID
WHERE SAL_OrderDetail.OrderID = @OrderID and SAL_OrderDetail.CompanyID = @CompanyID and SAL_OrderDetail.PeriodID = @PeriodID and SAL_OrderDetail.Status = 1 
ORDER BY SAL_OrderDetail.SerialNo
";

                #endregion

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
        public DataTable GetAllPendingSO(int companyID, int periodID, int? CustomerID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_AllPendingSO_ALimdad), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public static DataTable GetAllPendingSODetailByKey(int companyID, int periodID, int OrderID)
        {
            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@OrderID", OrderID);

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_OrderDetail.Query_GetPendingSODetailByKey), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
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


        public static DataTable GetDataByKeyTableWithoutColorServices(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColorServices, sqlparams);

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


        public static DataTable GetDataByKeyTableWithoutColor2(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor = @" 
SELECT SAL_OrderDetail.OrderDetailID, SAL_OrderDetail.OrderID, SAL_OrderDetail.CompanyID, SAL_OrderDetail.PeriodID, SAL_OrderDetail.SerialNo, 
    SAL_OrderDetail.ItemCategoryID, SAL_OrderDetail.ItemID, SAL_OrderDetail.ProductLine, SAL_OrderDetail.ItemUnitID, SAL_OrderDetail.ItemUnitQuantity, 
    SAL_OrderDetail.ItemQuality, SAL_OrderDetail.Quantity, SAL_OrderDetail.ColorID, SAL_OrderDetail.BaseAmount, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, 
    SAL_OrderDetail.IsSalesTaxExempted, SAL_OrderDetail.SalesTaxAmount, SAL_OrderDetail.SalesTaxFixRate, SAL_OrderDetail.SalesTaxPercent, 
    SAL_OrderDetail.IsExciseDutyExempted, SAL_OrderDetail.ExciseDutyAmount, SAL_OrderDetail.IsFEDExempted, SAL_OrderDetail.FEDAmount,SAL_OrderDetail.VATAmount, 
    SAL_OrderDetail.GodownCharges, SAL_OrderDetail.NetAmount, SAL_OrderDetail.Status, SAL_OrderDetail.CreatedBy, SAL_OrderDetail.CreateDate, 
    SAL_OrderDetail.CreatedByValue, SAL_OrderDetail.UpdatedBy, SAL_OrderDetail.UpdateDate, SAL_OrderDetail.UpdatedByValue, SAL_OrderDetail.Inclusive, 
    INV_SET_Category.CategoryName, INV_Item.Description, SET_UnitofMeasure.UOMName, ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, 
    SAL_OrderDetail.ReversedQuantity, ISNULL(SAL_OrderDetail.Quantity, 0) - ISNULL(
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
    ), 0) as RemainingQTY, 
    ISNULL(SAL_OrderDetail.Rate * (SAL_OrderDetail.Quantity - 
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) as sumQuantity from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID
    )), 0) AS TotalAmount,
	  (
        select IsNull(Sum(INV_StockTransactionDetail.TransactionQuantity), 0) 
            As SumOfQuantityInStock 
        from INV_StockTransactionDetail
			Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
                AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
                And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
			Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
				LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
			LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
            where INV_StockTransactionDetail.ItemID = INV_Item.ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID 
                And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 And SET_DocumentType.TransactionImpact <> 'No Impact' 
				AND  (isnull(SET_Location.LocationID,SL.LocationID) =SAL_OrderHeader.LocationID)
    )  As CurrentStock,  
    '' As ColorName, '' [Action] ,NoteToVendor, Set_Batch.[Description] as Batch, SAL_OrderDetail.BatchId ,case when [SAL_OrderDetail].[SalesTaxAmount] IS Not null then isnull([SAL_OrderDetail].[GrossAmount] * [SAL_OrderDetail].[SalesTaxAmount],1)/100  else 0 end as SalesTaxAmountA
,SAL_OrderDetail.QtyInCartons
,SET_TaxType.TaxTypeName
,AdvanceTax.TaxTypeName as AdvanceTaxTypeName
,SAL_OrderDetail.AdvanceTaxAmount
,SAL_OrderDetail.TaxTypeID
,SAL_OrderDetail.AdvanceTaxTypeID
FROM SAL_OrderDetail 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
		INNER JOIN SAL_OrderHeader  ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND INV_Item.Status = 1 
    left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
LEFT JOIN Set_Batch ON Set_Batch.BatchID = SAL_OrderDetail.BatchId and Set_Batch.CompanyID = SAL_OrderDetail.CompanyID  AND   Set_Batch.PeriodID = SAL_OrderDetail.PeriodID
left join SET_TaxType on SAL_OrderDetail.TaxTypeID = SET_TaxType.TaxTypeID and SET_TaxType.Status = 1
left join SET_TaxType as AdvanceTax on SAL_OrderDetail.AdvanceTaxTypeID = AdvanceTax.TaxTypeID and AdvanceTax.Status = 1
WHERE SAL_OrderDetail.OrderID = @OrderID and SAL_OrderDetail.CompanyID = @CompanyID and SAL_OrderDetail.PeriodID = @PeriodID and SAL_OrderDetail.Status = 1 
ORDER BY SAL_OrderDetail.SerialNo";

                #endregion

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

        public static DataTable GetDataByKeyTableWithoutColor2WithoutCompanyID(int orderID, int companyID, int periodID)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@OrderID", orderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                #region Query

                string Query_Select_By_OrderID_CompanyId_PeriodIdWithOutColor = @" 
SELECT SAL_OrderDetail.OrderDetailID, SAL_OrderDetail.OrderID, SAL_OrderDetail.CompanyID, SAL_OrderDetail.PeriodID, SAL_OrderDetail.SerialNo, 
    SAL_OrderDetail.ItemCategoryID, SAL_OrderDetail.ItemID, SAL_OrderDetail.ProductLine, SAL_OrderDetail.ItemUnitID, SAL_OrderDetail.ItemUnitQuantity, 
    SAL_OrderDetail.ItemQuality, SAL_OrderDetail.Quantity, SAL_OrderDetail.ColorID, SAL_OrderDetail.BaseAmount, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, 
    SAL_OrderDetail.IsSalesTaxExempted, SAL_OrderDetail.SalesTaxAmount, SAL_OrderDetail.SalesTaxFixRate, SAL_OrderDetail.SalesTaxPercent, 
    SAL_OrderDetail.IsExciseDutyExempted, SAL_OrderDetail.ExciseDutyAmount, SAL_OrderDetail.IsFEDExempted, SAL_OrderDetail.FEDAmount,SAL_OrderDetail.VATAmount, 
    SAL_OrderDetail.GodownCharges, SAL_OrderDetail.NetAmount, SAL_OrderDetail.Status, SAL_OrderDetail.CreatedBy, SAL_OrderDetail.CreateDate, 
    SAL_OrderDetail.CreatedByValue, SAL_OrderDetail.UpdatedBy, SAL_OrderDetail.UpdateDate, SAL_OrderDetail.UpdatedByValue, SAL_OrderDetail.Inclusive, 
    INV_SET_Category.CategoryName, INV_Item.Description, SET_UnitofMeasure.UOMName, ISNULL(INV_Item.ListPricePerUnit, 0) AS ListPricePerUnit, 
    SAL_OrderDetail.ReversedQuantity, ISNULL(SAL_OrderDetail.Quantity, 0) - ISNULL(
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID 
            AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID
    ), 0) as RemainingQTY, 
    ISNULL(SAL_OrderDetail.Rate * (SAL_OrderDetail.Quantity - 
    (
        select SUM(SAL_DeliveryOrderDetail.Quantity) as sumQuantity from SAL_DeliveryOrderDetail 
        where SalesOrderHeaderID = @OrderID AND CompanyID = SAL_OrderDetail.CompanyID and PeriodID = SAL_OrderDetail.PeriodID
    )), 0) AS TotalAmount,
	  (
        select IsNull(Sum(INV_StockTransactionDetail.TransactionQuantity), 0) 
            As SumOfQuantityInStock 
        from INV_StockTransactionDetail
			Inner Join INV_StockTransactionHeader On INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
                AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
                And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
			Inner Join SET_DocumentType On SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID 
                And SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = INV_StockTransactionHeader.PeriodID 
				LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
			LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
            where INV_StockTransactionDetail.ItemID = INV_Item.ItemID And INV_StockTransactionDetail.CompanyID = @CompanyID 
                And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 And SET_DocumentType.TransactionImpact <> 'No Impact' 
				AND  (isnull(SET_Location.LocationID,SL.LocationID) =SAL_OrderHeader.LocationID)
    )  As CurrentStock,  
    '' As ColorName, '' [Action] ,NoteToVendor, Set_Batch.[Description] as Batch, SAL_OrderDetail.BatchId ,case when [SAL_OrderDetail].[SalesTaxAmount] IS Not null then isnull([SAL_OrderDetail].[GrossAmount] * [SAL_OrderDetail].[SalesTaxAmount],1)/100  else 0 end as SalesTaxAmountA
,SAL_OrderDetail.QtyInCartons
,SET_TaxType.TaxTypeName
,AdvanceTax.TaxTypeName as AdvanceTaxTypeName
,SAL_OrderDetail.AdvanceTaxAmount
,SAL_OrderDetail.TaxTypeID
,SAL_OrderDetail.AdvanceTaxTypeID
FROM SAL_OrderDetail 
    INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = SAL_OrderDetail.ItemCategoryID --AND INV_SET_Category.CompanyID = SAL_OrderDetail.CompanyID 
        AND INV_SET_Category.Status = 1 
		INNER JOIN SAL_OrderHeader  ON SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID AND SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
    INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID --AND INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
	AND INV_Item.Status = 1 
    left JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID AND SET_UnitofMeasure.Status = 1 
LEFT JOIN Set_Batch ON Set_Batch.BatchID = SAL_OrderDetail.BatchId --and Set_Batch.CompanyID = SAL_OrderDetail.CompanyID  
AND   Set_Batch.PeriodID = SAL_OrderDetail.PeriodID
left join SET_TaxType on SAL_OrderDetail.TaxTypeID = SET_TaxType.TaxTypeID and SET_TaxType.Status = 1
left join SET_TaxType as AdvanceTax on SAL_OrderDetail.AdvanceTaxTypeID = AdvanceTax.TaxTypeID and AdvanceTax.Status = 1
WHERE SAL_OrderDetail.OrderID = @OrderID and SAL_OrderDetail.CompanyID = @CompanyID and SAL_OrderDetail.PeriodID = @PeriodID and SAL_OrderDetail.Status = 1 
ORDER BY SAL_OrderDetail.SerialNo";

                #endregion

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


        #endregion
    }
}
