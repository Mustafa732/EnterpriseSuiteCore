using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;



namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_DeliveryOrderHeader : BaseClass
    {
        #region Class Methods

        public static DataRow GetDataByKeyForDOStatusReturn(int companyID, int deliveryOrderID, int periodID, SqlConnection objConnection = null, 
            SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                string Query_GetData_byKey_ForStatusChange = FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_GetData_byKey_ForStatusChange;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_ForStatusChange, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_ForStatusChange, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_ForStatusChange, sqlparams);

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

        public static bool UpdateClosecodeforSaleOrder(int OrderId, int companyId, int periodId, int? Closecode = null)
        {

            bool result = false;

            string Qeury_UpdateClosecodeforSaleOrder = @"
UPDATE    SAL_OrderHeader
SET              ClosedCode = @closecode 
WHERE   (OrderID = @orderId) AND (companyId = @companyId) AND (PeriodID = @PeriodId)";

            try
            {

                SqlParameter[] sqlparams =
                    {
                        new SqlParameter("@orderId", OrderId),
                        new SqlParameter("@companyId", companyId),
                        new SqlParameter("@PeriodId", periodId),
                        new SqlParameter("@Closecode", Closecode)

                    };

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Qeury_UpdateClosecodeforSaleOrder, sqlparams) > 0)
                    result = true;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return result;

        }

        public static bool UpdateClosecodeforSaleOrderDetail(int OrderId, int companyId, int periodId, int OrderDetailId, int? Closecode = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams =
                    {
                        new SqlParameter("@orderId", OrderId),
                        new SqlParameter("@companyId", companyId),
                        new SqlParameter("@PeriodId", periodId),
                        new SqlParameter("@OrderDetailId", OrderDetailId),
                        new SqlParameter("@Closecode", Closecode)
                    };

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Sales.Qeury_UpdateClosecodeforSaleOrderDetail, sqlparams) > 0)
                    result = true;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
            }

            return result;
        }

        public DataTable Get_DeliveryOrderAlHajReport(int companyID, int periodID, int DONumber)
        {
            DataTable dt = null;

            #region Query

            string Query_DeliveryOrderAlHajReport = @"
Select SAL_DeliveryOrderHeader.DODate As Date, SAL_DeliveryOrderHeader.DeliveryOrderNo AS SRNO, SAL_OrderHeader.ContractNo, AR_Set_Customer.CustomerName AS Customer, 
    AR_Set_Customer.FatherName AS SONumber, AR_Set_Customer.CNICNumber + '/' + AR_Set_Customer.NTNNumber AS CNIC, AR_Set_Customer.AddressLine1 AS Address, 
    Set_City.CityName + '-' + Set_District.DistrictName AS CITY,  AR_Set_Customer.CNICNumber, AR_Set_Customer.NTNNumber,
    (Case when AR_Set_Customer.Provinceid = 1 then 'Balochistan' when AR_Set_Customer.Provinceid = 2 then 'KPK' when AR_Set_Customer.Provinceid = 3 then 'Punjab' 
        when AR_Set_Customer.Provinceid = 4 then 'Sindh' END) As Province, 
    INV_Item.Description AS VehicleModel, SAL_DOVehicleInformation.Chassis AS ChassNo, SAL_DOVehicleInformation.Engine AS EnginerNo, Set_Color.ColorName As Color, 
    SET_OperatingUnit.LogoImage 
From SAL_DeliveryOrderHeader 
    Inner Join AR_Set_Customer ON AR_Set_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID 
--AND AR_Set_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND AR_Set_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND AR_Set_Customer.Status = 1 
    Inner Join SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        AND SAL_DeliveryOrderDetail.Status = 1 
    Inner Join SAL_DOVehicleInformation ON SAL_DOVehicleInformation.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
        AND SAL_DOVehicleInformation.PeriodID = SAL_DeliveryOrderDetail.PeriodID AND SAL_DOVehicleInformation.DOHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
        AND SAL_DOVehicleInformation.DODetailID = SAL_DeliveryOrderDetail.DODetailID AND SAL_DOVehicleInformation.Status = 1 
	Inner Join SAL_OrderDetail on SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID 
        AND SAL_OrderDetail.Status = 1 
	Inner Join SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID AND SAL_OrderHeader.Status = 1 
	Left Join Set_City On Set_City.CityID = AR_Set_Customer.CityID AND Set_City.Status = 1 
	left join Set_District ON Set_District.DistrictID = AR_Set_Customer.DistrictID 
	Inner Join INV_Item ON INV_Item.ItemID = SAL_DOVehicleInformation.ItemID 
--AND INV_Item.CompanyID = SAL_DOVehicleInformation.CompanyID 
AND INV_Item.Status = 1 
	Inner Join Set_Color ON Set_Color.ColorID = SAL_OrderDetail.ColorID AND Set_Color.Status = 1 
	Inner Join SET_Company ON SET_Company.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	Inner Join SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitID =  SET_Company.OperatingUnitID 
Where SAL_DeliveryOrderHeader.CompanyId = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 
    AND SAL_DeliveryOrderHeader.DeliveryOrderNo = @DONumber";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DONumber", DONumber);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeliveryOrderAlHajReport, sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable ExpiredDeliveryOrderDashboard(int companyID, int periodID, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, SqlConnection sqlcon = null)
        {

            DataTable dt = null;

            #region Query

            string Query_ExpiredDeliveryOrderDashboard = @"-- Expired Delivery Order

SELECT
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SET_DocumentType.DocumentType, 
SAL_DeliveryOrderHeader.DeliveryOrderNo, 
AR_SET_Customer.CustomerName, 
INV_Item.Description AS ItemName, 
SAL_DeliveryOrderDetail.Quantity,
SAL_DeliveryOrderHeader.TotalAmount,
ROW_NUMBER() OVER (ORDER BY SAL_DeliveryOrderHeader.CreateDate Desc) AS 'Row'
FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN AR_SET_Customer ON 
--SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
WHERE 
SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
AND SAL_DeliveryOrderHeader.Status = 1 
AND SAL_DeliveryOrderHeader.DOValidityDate <=  CONVERT (date, GETDATE())
AND (SAL_OrderHeader.ClosedCode <> 13 or SAL_OrderHeader.ClosedCode is null)";

            #endregion

            try
            {
                OrderBy = "";

                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                //   DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_PendingSales_Order_ReportDashboard, sqlparams.ToArray());

                if (sqlcon == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_ExpiredDeliveryOrderDashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_ExpiredDeliveryOrderDashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(sqlcon, CommandType.Text, string.Format(Paging, Query_ExpiredDeliveryOrderDashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_ExpiredDeliveryOrderDashboard + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                }

                ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetDataByDoIDForDispatch(int companyID, int periodID, Nullable<int> DOID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderID", DOID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, "select DOMaster.DeliveryOrderNo,DOMaster.DeliveryOrderID,DOMaster.DocumentTypeID,DODetail.DODetailID from SAL_DeliveryOrderHeader as DOMaster inner join SAL_DeliveryOrderDetail as DODetail on DOMaster.DeliveryOrderID=DODetail.DOHeaderID where DOMaster.DeliveryOrderID=@DeliveryOrderID and DOMaster.CompanyID=@CompanyID and DOMaster.PeriodID=@periodID", sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }





        public static DataTable GetAllDataByCustomerID(int companyID, int periodID, string query, Nullable<int> customerID, string strwhere)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                //ISNULL(SUM([INV_StockTransactionDetail].TransactionQuantity), 0) STQty,

                string strQueryPart = "";
                strQueryPart = "SUM(ABS(ISNULL([INV_StockTransactionDetail].TransactionQuantity, 0))) STQty,";
                //strQueryPart
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query, strwhere, strQueryPart), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataByCustomerID(int companyID, int periodID, string Query, Nullable<int> customerID, string strwhere, bool Balance = false)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (customerID.HasValue)
                    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                string strQueryPart = "";
                if (Balance == true)
                    strQueryPart = "(SUM(ABS(ISNULL([INV_StockTransactionDetail].TransactionQuantity, 0))) - isnull(sum(Reversed.ReversedQuantity),0) )  STQty,";
                    //  strQueryPart = "isnull(((select sum(abs(isnull(TransactionQuantity,0))) from INV_StockTransactionDetail  INV_StockTransactionDetailinner inner join INV_StockTransactionHeader on INV_StockTransactionDetailinner.StockTransactionID = INV_StockTransactionHeader.StockTransactionID and INV_StockTransactionDetailinner.CompanyID = INV_StockTransactionHeader.CompanyID and INV_StockTransactionDetailinner.PeriodID = INV_StockTransactionHeader.PeriodID and INV_StockTransactionHeader.StatusCode = 1   and INV_StockTransactionHeader.Status = 1   where INV_StockTransactionDetailinner.StockTransactionID = INV_StockTransactionDetail.StockTransactionID and INV_StockTransactionDetailinner.CompanyID = INV_StockTransactionDetail.CompanyID and INV_StockTransactionDetailinner.PeriodID = INV_StockTransactionDetail.PeriodID and INV_StockTransactionDetailinner.Status = 1) - isnull(sum(Reversed.ReversedQuantity),0)),0)  STQty,";

                    // in case return Complet dispatch then Do not showing
//                    strQueryPart = @"
//                                         ABS(
//                                        (SUM(abs((ISNULL((Case WHEN SAL_ReverseDetail.SourceHeaderID IS NULL THEN
//	                                        [INV_StockTransactionDetail].TransactionQuantity
//                                        ELSE 0 END), 0))))  - isnull(sum(SAL_ReverseDetail.ReversedQuantity),0) ) ) STQty,
//                                ";
                    //strQueryPart = "ISNULL(SUM([INV_StockTransactionDetail].TransactionQuantity), 0) STQty,";
                else
                    strQueryPart = "ISNULL(SUM([INV_StockTransactionDetail].TransactionQuantity), 0) STQty,";

                //strQueryPart
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, strwhere, strQueryPart), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataByCustomerIDForDOUpdate(int companyID, int periodID, string Query, Nullable<int> customerID,Nullable<int> itemID,Nullable<int> itemunitID, string strwhere, bool Balance = false)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                //if (customerID.HasValue)
                //    sqlparams[2] = new SqlParameter("@CustomerID", customerID);
                //else
                    sqlparams[2] = new SqlParameter("@CustomerID", DBNull.Value);

                if (customerID.HasValue)
                    sqlparams[3] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[3] = new SqlParameter("@ItemID", DBNull.Value);

                if (customerID.HasValue)
                    sqlparams[4] = new SqlParameter("@ItemUnitID", itemunitID);
                else
                    sqlparams[4] = new SqlParameter("@ItemUnitID", DBNull.Value);

                string strQueryPart = "";
                if (Balance == true)
                    strQueryPart = "(SUM(ABS(ISNULL([INV_StockTransactionDetail].TransactionQuantity, 0))) - isnull(sum(Reversed.ReversedQuantity),0) )  STQty,";
                else
                    strQueryPart = "ISNULL(SUM([INV_StockTransactionDetail].TransactionQuantity), 0) STQty,";

                //strQueryPart
                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, strwhere, strQueryPart), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0 )
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAll_WithPaging_ForApprove = @"
Select *, ROW_NUMBER() Over (Order by Cast(tab.DeliveryOrderNo as int) desc) as 'Row' From
( 
SELECT DISTINCT SAL_DeliveryOrderHeader.DeliveryOrderID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID,SAL_DeliveryOrderHeader.DocumentTypeID,SAL_DeliveryOrderHeader.DeliveryOrderNo
,SAL_DeliveryOrderHeader.DODate,SAL_DeliveryOrderHeader.DOValidityDate,SAL_DeliveryOrderHeader.Status,SAL_DeliveryOrderHeader.StatusCode
,SET_DocumentApprove.ApprovalStatus,Set_documentPendingApprove.ApprovalOperator,Set_documentPendingApprove.ApprovalUserID AS UserID,Set_Batch.Description as Batch
,AR_SET_Customer.CustomerName,AR_SET_Customer.CustomerID,Sal_DeliveryOrderDetail.Quantity,SAL_DeliveryOrderHeader.DeliveryOrderNo as DocumentNo,SAL_DeliveryOrderHeader.DODate as DocumentDate,SET_PeriodCustom.PeriodSetName,SAL_ContractHeader.ContractNo ,set_city.CityName,
BrokerName.CustomerName AS AgentName
FROM SAL_DeliveryOrderHeader

inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID
	and SET_DocumentType.Status = 1

INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo 
	AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SAL_DeliveryOrderHeader.CompanyID
		AND PeriodID = SAL_DeliveryOrderHeader.PeriodID AND DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
		AND DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo)
		
		inner join Set_documentPendingApprove on Set_documentPendingApprove.DocumentApproveID = SET_DocumentApprove.DocumentApproveID
        and Set_documentPendingApprove.companyID = SET_DocumentApprove.companyID
        and Set_documentPendingApprove.PeriodID = SET_DocumentApprove.PeriodID
		
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
											WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
												AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
												AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
													WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
														AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
														AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))

    AND (SET_UserApprovalAmount.UserID = SAL_DeliveryOrderHeader.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)                                
Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        And Sal_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_OrderHeader On Sal_OrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        And Sal_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 

Inner Join SAL_ContractHeader On SAL_ContractHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And SAL_ContractHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
            And SAL_ContractHeader.ContractID= Sal_DeliveryOrderDetail.ContractHeaderID 

    inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
		and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID
Inner join AR_SET_Customer AS BrokerName on
AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
            AND BrokerName.CustomerID = SAL_DeliveryOrderHeader.BrokerID
left join set_city on set_city.cityid = AR_SET_Customer.cityid
and set_city.status = 1
    Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = Sal_OrderHeader.Season 
            left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID

inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID
{WhereFilter}
)tab";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_SelectAll_WithPaging_ForApprove.Replace("{WhereFilter}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAll_WithoutPaging_ForApprove = @" 
SELECT
	SAL_DeliveryOrderHeader.DeliveryOrderNo
	,SAL_DeliveryOrderHeader.CompanyID
	,SAL_DeliveryOrderHeader.PeriodID
	,SAL_DeliveryOrderHeader.DocumentTypeID
	,SAL_DeliveryOrderHeader.DeliveryOrderNo
	,SAL_DeliveryOrderHeader.DODate
	,SAL_DeliveryOrderHeader.DOValidityDate
	,SAL_DeliveryOrderHeader.Status
	,SAL_DeliveryOrderHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel, Set_Batch.Description as Batch
    ,sum(Sal_DeliveryOrderDetail.Quantity) as Quantity
,SAL_DeliveryOrderHeader.DeliveryOrderNo as DocumentNo
    ,SAL_DeliveryOrderHeader.DODate as DocumentDate
    ,SET_PeriodCustom.PeriodSetName
FROM SAL_DeliveryOrderHeader
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo 
	AND SET_DocumentApprove.CreateDate = (SELECT MAX(CreateDate) FROM SET_DocumentApprove WHERE CompanyID = SAL_DeliveryOrderHeader.CompanyID
		AND PeriodID = SAL_DeliveryOrderHeader.PeriodID AND DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
		AND DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo)
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
											WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
												AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
												AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
													WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
														AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
														AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        And Sal_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_OrderHeader On Sal_OrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        And Sal_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
    Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = Sal_OrderHeader.Season 
Left join Set_PeriodCustom on  SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1
{WhereFilter} 
group by SAL_DeliveryOrderHeader.DeliveryOrderNo
	,SAL_DeliveryOrderHeader.CompanyID
	,SAL_DeliveryOrderHeader.PeriodID
	,SAL_DeliveryOrderHeader.DocumentTypeID
	,SAL_DeliveryOrderHeader.DeliveryOrderNo
	,SAL_DeliveryOrderHeader.DODate
	,SAL_DeliveryOrderHeader.DOValidityDate
	,SAL_DeliveryOrderHeader.[Status]
	,SAL_DeliveryOrderHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID
    ,SET_Authority.AuthorityLevel, Set_Batch.Description
Order By Cast(SAL_DeliveryOrderHeader.DeliveryOrderNo as int) desc";

                    #endregion

                    string query = Query_SelectAll_WithoutPaging_ForApprove.Replace("{WhereFilter}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataDO(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
        Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, SqlTransaction objTransaction = null,int CurrentFiscalID=0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;


            string Query_SelectAll_WithPagingDO = @" SELECT [DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], SAL_DeliveryOrderHeader.[PeriodID], SAL_DeliveryOrderHeader.[DocumentTypeID], SAL_DeliveryOrderHeader.[DeliveryOrderNo], [DODate], [DOValidityDate], [CustomerID], [IsCancelled], [CancellationDate], [CancelledBy], [IsApproved], [ApprovedBy], [ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], [IsDispatchGenerated], [IsPaid], [TotalPaidAmount], 
SAL_DeliveryOrderHeader.[CreatedBy], SAL_DeliveryOrderHeader.[CreateDate], [UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], [StatusCode], [TotalAmount], [StopDispatch], 
[ShipToCustomer], [Remarks], [ShipTo], [NoOfDos], [BaseID],Set_PeriodCustom.PeriodSetName ROW_NUMBER() Over (Order by SAL_DeliveryOrderHeader.CompanyID, DeliveryOrderID, SAL_DeliveryOrderHeader.PeriodID) as 'Row' 
FROM [SAL_DeliveryOrderHeader]
inner join Set_PeriodCustom on SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID

";


            const string Query_SelectAll_WithoutPagingDO = @" SELECT SAL_DeliveryOrderHeader.*,Set_PeriodCustom.PeriodSetName
FROM [SAL_DeliveryOrderHeader] 
inner join Set_PeriodCustom on SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and SET_PeriodCustom.FiscalID = @CurrentFiscalID

"; 


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

                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingDO + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingDO + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPagingDO + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPagingDO + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }



        public SqlTransaction InsertDOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colDODetail, SET_DocumentApprove da, int CurrentFiscalID = 0,SqlTransaction sql=null)
        {
            bool result = false;
            SqlTransaction sqltrans = sql;//SqlHelper.GetTransaction("InsertDeliveryOrder");

            try
            {
                //   this.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (SAL_DeliveryOrderDetail dod in colDODetail)
                {
                    dod.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dod.FiscalID = CurrentFiscalID;
                    dod.Insert(sqltrans);




                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dod.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, f_CreatedBy,
                        f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.FiscalID = CurrentFiscalID;
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
                {
                    //SqlHelper.CommitTransaction(sqltrans);
                }
                else
                {
                    sqltrans = null;
                    SqlHelper.RollBackTransaction(sqltrans, "InsertWO_Detail");
                }
            }

            return sqltrans;
        }




        public bool InsertDOHeaderAndDetailDocumentOut(List<SAL_DeliveryOrderDetail> colDODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colDOVIDetAdd,int CurrentFiscalID = 0,int orderID=0,SqlTransaction sql=null)
        {
            bool result = false;
            SqlTransaction sqltrans;
            if (sql == null)
            {
                sqltrans = SqlHelper.GetTransaction("InsertDeliveryOrder");
            }
            else
            {
                sqltrans = sql;
            }
            try
            {
                this.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans,CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.DeliveryOrderID = orderID;
                this.Insert(sqltrans);

                foreach (SAL_DeliveryOrderDetail dod in colDODetail)
                {
                    int DOD = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dod.DODetailID = DOD;
                    dod.FiscalID = CurrentFiscalID;
                    dod.SalesOrderDetailID = DOD;
                    dod.DOHeaderID = orderID;
                    dod.Insert(sqltrans);
                    try
                    {
                        if (colDOVIDetAdd != null)
                        {
                            var Records = from objDOVehicle in colDOVIDetAdd
                                          where objDOVehicle.DODetailID == dod.SalesOrderDetailID && objDOVehicle.DOHeaderID == dod.SalesOrderHeaderID
                                          select objDOVehicle;

                            #region Do Vehicle Information
                            foreach (SAL_DOVehicleInformation dovi in Records)
                            {
                                dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                                dovi.DOHeaderID = this.DeliveryOrderID;
                                dovi.DODetailID = (int)dod.DODetailID;
                                dovi.Insert(sqltrans);
                                SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                                sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                                sdvi.VehicleID = (int)dovi.VehicleID;
                                sdvi.CompanyID = CompanyID;
                                sdvi.PeriodID = PeriodID;
                                sdvi.UpdateDataByVehicleID(sqltrans);
                            }
                        }
                    }

                    catch { }
                    //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
                    #endregion

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dod.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, f_CreatedBy,
                        f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.FiscalID = CurrentFiscalID;
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
                //if (result)
                //    SqlHelper.CommitTransaction(sqltrans);
                //else
                //    SqlHelper.RollBackTransaction(sqltrans, "InsertDeliveryOrder");

                if (result)
                {
                    if (sql == null)
                    {
                        SqlHelper.CommitTransaction(sqltrans);
                    }
                    else
                    { }
                }
                else
                {
                    if (sql == null)
                    {
                        SqlHelper.RollBackTransaction(sqltrans, "InsertDeliveryOrder");
                    }
                    else
                    { }
                }
            }

            return result;
        }

        public bool UpdateDOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colAddDODetail, List<SAL_DeliveryOrderDetail> colUpdateDODetail, List<SAL_DeliveryOrderDetail> colDeleteDODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colUpdateDOVIDetail = null, List<SAL_DOVehicleInformation> colDeleteDOVIDetail = null,DataRow[] OrderDtl = null,int CurrentFiscalID =0)
        {
            bool result = false;
            short DoDetail = 0;
            short DeleteDoDetail = 0;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDeliveryOrder");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (SAL_DeliveryOrderDetail dodAdd in colAddDODetail)
                {
                    dodAdd.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dodAdd.FiscalID = CurrentFiscalID;
                    //DoDetail = dodAdd.DODetailID;
                    dodAdd.Insert(sqltrans);

                    var Records = from objDOVehicle in colUpdateDOVIDetail
                                  //where objDOVehicle.RowUniqueID == dodAdd.RowUniqueID
                                  where objDOVehicle.DOHeaderID == dodAdd.SalesOrderHeaderID
                                    && objDOVehicle.DODetailID == dodAdd.SalesOrderDetailID && objDOVehicle.DOVehicleInformationID == -1
                                  select objDOVehicle;

                    #region Do Vehicle Information
                    foreach (SAL_DOVehicleInformation dovi in Records)
                    {
                        dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                        dovi.DOHeaderID = dodAdd.DOHeaderID;
                        dovi.DODetailID = (int)dodAdd.DODetailID;
                        dovi.Insert(sqltrans);
                        SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                        sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                        sdvi.VehicleID = (int)dovi.VehicleID;
                        sdvi.CompanyID = CompanyID;
                        sdvi.PeriodID = PeriodID;
                       
                        sdvi.UpdateDataByVehicleID(sqltrans);
                    }
                    //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
                    #endregion

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodAdd.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, dodAdd.CreatedBy, dodAdd.CreateDate, dodAdd.CreatedByValue);

                    #endregion
                }

                foreach (SAL_DeliveryOrderDetail dodUpdate in colUpdateDODetail)
                {
                    DoDetail = (short)dodUpdate.DODetailID;
                    dodUpdate.FiscalID = CurrentFiscalID;
                    dodUpdate.UpdateDataByKey(sqltrans);

                    if (colUpdateDOVIDetail != null)
                    {
                        var Records = from objDOVehicle in colUpdateDOVIDetail
                                      where objDOVehicle.RowUniqueID == dodUpdate.RowUniqueID && objDOVehicle.DOVehicleInformationID == -1
                                      select objDOVehicle;

                        #region Do Vehicle Information

                        foreach (SAL_DOVehicleInformation dovi in Records)
                        {
                            dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                            dovi.DOHeaderID = dodUpdate.DOHeaderID;
                            dovi.DODetailID = Convert.ToInt32(dodUpdate.DODetailID);
                            dovi.Insert(sqltrans);
                            SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                            sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                            sdvi.VehicleID = (int)dovi.VehicleID;
                            sdvi.CompanyID = CompanyID;
                            sdvi.PeriodID = PeriodID;
                            sdvi.UpdateDataByVehicleID(sqltrans);
                        }

                        #endregion
                    }

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodUpdate.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery,
                        f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion


                    
                }

                foreach (SAL_DeliveryOrderDetail dodDelete in colDeleteDODetail)
                {
                    DeleteDoDetail = (short)dodDelete.DODetailID;
                    SAL_DeliveryOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, dodDelete.DODetailID,
                        DeliveryOrderID, f_PeriodID);
                }

                #region Update CloseCode in SaleOrder
                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Cancelled)))
                {
                    foreach (DataRow dr in OrderDtl)
                    {
                        SAL_OrderHeader soh = new SAL_OrderHeader(CompanyID, int.Parse(dr["SalesOrderHeaderID"].ToString()), PeriodID, null, sqltrans);
                        soh.ClosedCode = null;
                        soh.FiscalID = CurrentFiscalID;
                        soh.UpdateDataByKey(sqltrans);



                    }
                }
                #endregion


                #region Al Haj Faw
                if (colUpdateDOVIDetail != null)
                {
                    foreach (SAL_DOVehicleInformation doviUpdate in colUpdateDOVIDetail)
                    {
                        if (doviUpdate.DID > 0)
                        {
                            doviUpdate.DOHeaderID = this.DeliveryOrderID;
                            doviUpdate.DODetailID = doviUpdate.DID;
                            doviUpdate.UpdateDataByKey(sqltrans);

                            SAL_DispatchVehicleInformation sdvi2 = new SAL_DispatchVehicleInformation();
                            sdvi2.CompanyID = CompanyID;
                            sdvi2.PeriodID = PeriodID;
                            sdvi2.VehicleInformationID = doviUpdate.DOVehicleInformationID;
                            sdvi2.UpdateDataByVehicleInformationID(sqltrans);

                            SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                            sdvi.VehicleInformationID = doviUpdate.DOVehicleInformationID;
                            sdvi.VehicleID = (int)doviUpdate.VehicleID;
                            sdvi.CompanyID = CompanyID;
                            sdvi.PeriodID = PeriodID;
                            sdvi.UpdateDataByVehicleID(sqltrans);
                        }
                    }
                }
                if (colDeleteDOVIDetail != null)
                {
                    foreach (SAL_DOVehicleInformation doviDelete in colDeleteDOVIDetail)
                    {
                        doviDelete.DOHeaderID = this.DeliveryOrderID;
                        doviDelete.DODetailID = DeleteDoDetail;
                        SAL_DOVehicleInformation.VehicleDeleteByKey(sqltrans, doviDelete.UpdatedBy.Value, doviDelete.UpdateDate.Value, doviDelete.UpdatedByValue, CompanyID, doviDelete.DOVehicleInformationID, PeriodID);

                        SAL_DispatchVehicleInformation sdvi3 = new SAL_DispatchVehicleInformation();
                        sdvi3.CompanyID = CompanyID;
                        sdvi3.PeriodID = PeriodID;
                        sdvi3.VehicleInformationID = doviDelete.DOVehicleInformationID;
                        sdvi3.UpdateDataByVehicleInformationID(sqltrans);

                    }
                }
                #endregion

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDeliveryOrder");
            }

            return result;
        }

        public bool UpdateDOHeaderAndDetail(List<SAL_DeliveryOrderHeader> lst_DO_HeaderAdd, List<SAL_DeliveryOrderHeader> lst_DO_HeaderDelete, 
            List<SAL_DeliveryOrderDetail> lst_DO_DetailAdd, List<SAL_DeliveryOrderDetail> lst_DO_Detail_Update, List<SAL_DeliveryOrderDetail> lst_DO_DetailDelete, 
            SET_DocumentApprove da,int CurrentFiscalID=0)
        {
            bool result = false;
            SqlTransaction obj_SQLTransaction = SqlHelper.GetTransaction("UpdateDeliveryOrder");

            try
            {
                this.UpdateDataByKey(obj_SQLTransaction);

                #region Insert new Delivery Orders, if No of DOs is more than previous

                foreach (SAL_DeliveryOrderHeader obj_DO_HeaderAdd in lst_DO_HeaderAdd)
                {
                    obj_DO_HeaderAdd.DeliveryOrderID = SAL_DeliveryOrderHeader.GetNewDeliveryOrderID(f_CompanyID, f_PeriodID, obj_SQLTransaction);
                    obj_DO_HeaderAdd.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, 
                        obj_SQLTransaction,CurrentFiscalID).ToString();

                    obj_DO_HeaderAdd.Insert(obj_SQLTransaction);

                    foreach (SAL_DeliveryOrderDetail obj_DO_DetailAdd in lst_DO_DetailAdd)
                    {
                        obj_DO_DetailAdd.DOHeaderID = obj_DO_HeaderAdd.DeliveryOrderID;
                        obj_DO_DetailAdd.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, obj_DO_HeaderAdd.DeliveryOrderID, PeriodID, 
                            obj_SQLTransaction));

                        obj_DO_DetailAdd.Insert(obj_SQLTransaction);
                    }

                    foreach (SAL_DeliveryOrderDetail obj_DO_DetailUpdate in lst_DO_Detail_Update)
                    {
                        obj_DO_DetailUpdate.DOHeaderID = obj_DO_HeaderAdd.DeliveryOrderID;
                        obj_DO_DetailUpdate.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, obj_DO_HeaderAdd.DeliveryOrderID, 
                            PeriodID, obj_SQLTransaction));

                        obj_DO_DetailUpdate.Insert(obj_SQLTransaction);
                    }

                    if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                        || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    {
                        da.DocumentNo = obj_DO_HeaderAdd.DeliveryOrderNo;
                        da.Insert(obj_SQLTransaction);
                    }
                }

                #endregion

                #region Delete Delivery Orders, if No of DOs is less than previous

                foreach (SAL_DeliveryOrderHeader obj_DO_HeaderDelete in lst_DO_HeaderDelete)
                {
                    obj_DO_HeaderDelete.DeleteDeliveryOrder(obj_DO_HeaderDelete.CompanyID, obj_DO_HeaderDelete.PeriodID, obj_DO_HeaderDelete.DeliveryOrderID, 
                        obj_SQLTransaction);
                }

                #endregion

                foreach (SAL_DeliveryOrderDetail dodAdd in lst_DO_DetailAdd)
                {
                    dodAdd.DOHeaderID = this.DeliveryOrderID;
                    dodAdd.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, obj_SQLTransaction));
                    dodAdd.Insert(obj_SQLTransaction);

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodAdd.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, obj_SQLTransaction, SOEnumaration.SalesOrder_Stage.Delivery, 
                        dodAdd.CreatedBy, dodAdd.CreateDate, dodAdd.CreatedByValue);

                    #endregion
                }

                foreach (SAL_DeliveryOrderDetail dodUpdate in lst_DO_Detail_Update)
                {
                    dodUpdate.DOHeaderID = this.DeliveryOrderID;
                    dodUpdate.UpdateDataByKey(obj_SQLTransaction);

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodUpdate.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, obj_SQLTransaction, SOEnumaration.SalesOrder_Stage.Delivery,
                        f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }

                foreach (SAL_DeliveryOrderDetail dodDelete in lst_DO_DetailDelete)
                {
                    SAL_DeliveryOrderDetail.DeleteDataByKey(obj_SQLTransaction, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, 
                        dodDelete.DODetailID, DeliveryOrderID, f_PeriodID);
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.DeliveryOrderNo;
                    da.Insert(obj_SQLTransaction);
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
                    SqlHelper.CommitTransaction(obj_SQLTransaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQLTransaction, "UpdateDeliveryOrder");
            }

            return result;
        }

        public bool UpdateDOHeaderAndDetailForContract(List<SAL_DeliveryOrderHeader> lst_DO_HeaderAdd, List<SAL_DeliveryOrderHeader> lst_DO_HeaderDelete,
           List<SAL_DeliveryOrderDetail> lst_DO_DetailAdd, List<SAL_DeliveryOrderDetail> lst_DO_Detail_Update, List<SAL_DeliveryOrderDetail> lst_DO_DetailDelete,
           SET_DocumentApprove da,bool isSheikho = false,bool isHabib = false, int CurrentFiscalID = 0
            , int ContractID = 0, bool IsSoEditable = false
            )
        {
            bool result = false;
            SqlTransaction obj_SQLTransaction = SqlHelper.GetTransaction("UpdateDeliveryOrder");
            DataTable dtDetails = new DataTable();
            try
            {
                if (IsSoEditable)
                    dtDetails = SAL_DeliveryOrderHeader.GetDOandSODatabyContactID(this.CompanyID, this.PeriodID, ContractID);

                this.UpdateDataByKey(obj_SQLTransaction);

                foreach (SAL_DeliveryOrderDetail dodAdd in lst_DO_DetailAdd)
                {
                    dodAdd.DOHeaderID = this.DeliveryOrderID;
                    dodAdd.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, obj_SQLTransaction));
                    dodAdd.Insert(obj_SQLTransaction);

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodAdd.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, obj_SQLTransaction, SOEnumaration.SalesOrder_Stage.Delivery,
                        dodAdd.CreatedBy, dodAdd.CreateDate, dodAdd.CreatedByValue);

                    #endregion
                }

                foreach (SAL_DeliveryOrderDetail dodUpdate in lst_DO_Detail_Update)
                {

                    if (dtDetails != null && dtDetails.Rows.Count > 0)
                    {
                        foreach (DataRow item in dtDetails.Rows)
                        {
                            if (long.Parse(item["DOHeaderID"].ToString()) == this.DeliveryOrderID && long.Parse(item["DODetailID"].ToString()) == dodUpdate.DODetailID)
                            {
                                decimal DispatchQty = decimal.Parse(item["DispatchQty"].ToString());
                                
                                if (dodUpdate.Quantity < DispatchQty)
                                    throw new BusinessLogicException("Quantity cannot be less than Dispatch Quantity!");

                                DataTable dtloadingpoint = INV_LoadingPointHeader.GetLoadingPointQuantityByDODetailID(this.CompanyID, this.PeriodID, this.FiscalID, this.DeliveryOrderID, dodUpdate.DODetailID);
                                if (dtloadingpoint != null && dtloadingpoint.Rows.Count > 0)
                                {
                                    decimal LoadingPointQty = decimal.Parse(dtloadingpoint.Rows[0]["TransactionQuantity"].ToString());
                                    if (LoadingPointQty > 0 && dodUpdate.Quantity < LoadingPointQty)
                                        throw new BusinessLogicException("Loading point made against this DO!");
                                }

                            }
                        }
                    }

                    dodUpdate.DOHeaderID = this.DeliveryOrderID;
                    dodUpdate.UpdateDataByKey(obj_SQLTransaction);

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dodUpdate.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, obj_SQLTransaction, SOEnumaration.SalesOrder_Stage.Delivery,
                        f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue);

                    #endregion
                }

                foreach (SAL_DeliveryOrderDetail dodDelete in lst_DO_DetailDelete)
                {
                    SAL_DeliveryOrderDetail.DeleteDataByKey(obj_SQLTransaction, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,
                        dodDelete.DODetailID, DeliveryOrderID, f_PeriodID);

                    if (isSheikho || isHabib)
                    {
                        SAL_OrderDetail.DeleteDataByKey(obj_SQLTransaction, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID,
                          dodDelete.DODetailID, DeliveryOrderID, f_PeriodID);
                    }
                }


                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.DeliveryOrderNo;
                    da.Insert(obj_SQLTransaction);
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
                    SqlHelper.CommitTransaction(obj_SQLTransaction);
                else
                    SqlHelper.RollBackTransaction(obj_SQLTransaction, "UpdateDeliveryOrder");
            }

            return result;
        }

        public bool DeleteDOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colDeleteDODetail, List<SAL_DOVehicleInformation> VehicleCol = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeleteDeliveryOrder");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, DeliveryOrderID, f_PeriodID);

                foreach (SAL_DeliveryOrderDetail dodDelete in colDeleteDODetail)
                {
                    SAL_DeliveryOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, dodDelete.DODetailID, DeliveryOrderID, f_PeriodID);

                }
                if (VehicleCol != null)
                {
                    foreach (SAL_DOVehicleInformation sdvi in VehicleCol)
                    {
                        SAL_DOVehicleInformation.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, UpdatedByValue, f_CompanyID, sdvi.DOVehicleInformationID, f_PeriodID);

                        SAL_DispatchVehicleInformation sDispatchVi = new SAL_DispatchVehicleInformation();
                        sDispatchVi.VehicleID = (int)sdvi.VehicleID;
                        sDispatchVi.VehicleInformationID = null;
                        sDispatchVi.CompanyID = sdvi.CompanyID;
                        sDispatchVi.PeriodID = sdvi.PeriodID;
                        sDispatchVi.UpdateDataByVehicleID(sqltrans);

                    }
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
                    SqlHelper.RollBackTransaction(sqltrans, "DeleteDeliveryOrder");
            }
            return result;
        }

        public bool DeleteDeliveryOrder(int companyID, int periodID, int deliveryOrderID, SqlTransaction obj_SQLTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DO_HeaderID", deliveryOrderID);

                string qry_Delete = @" Delete From Sal_DeliveryOrderHeader Where CompanyID = @CompanyID And PeriodID = @PeriodID And DeliveryOrderID = @DO_HeaderID; 
Delete From Sal_DeliveryOrderDetail Where CompanyID = @CompanyID And PeriodID = @PeriodID And DOHeaderID = @DO_HeaderID; ";

                if (obj_SQLTransaction != null)
                    SqlHelper.ExecuteNonQuery(obj_SQLTransaction, CommandType.Text, qry_Delete, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(CommandType.Text, qry_Delete, sqlparams);
                
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

            return result;
        }

        public bool UpdateDOHeaderForApprove(SET_DocumentApprove dA,int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdateDOHeader");

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdateDOHeader");
            }

            return result;
        }

        private void CheckQuantity(long salesOrderHeaderID, int companyID, int periodID, SqlTransaction sqltrans, SOEnumaration.SalesOrder_Stage closedCode, Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            #region Query

            string Query_CheckQuantity = @" SELECT [SAL_OrderDetail].[OrderID], [SAL_OrderDetail].[OrderDetailID], [SAL_OrderDetail].[Quantity] AS SOQuantity, Sum([SAL_DeliveryOrderDetail].[Quantity]) AS DOQuantity
FROM [SAL_DeliveryOrderDetail]
	INNER JOIN [SAL_OrderDetail] ON [SAL_OrderDetail].[OrderDetailID] = [SAL_DeliveryOrderDetail].[SalesOrderDetailID] 
		AND [SAL_OrderDetail].[OrderID] = [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] 
		AND [SAL_OrderDetail].[CompanyID] = [SAL_DeliveryOrderDetail].[CompanyID] 
		AND [SAL_OrderDetail].[PeriodID] = [SAL_DeliveryOrderDetail].[PeriodID] AND [SAL_OrderDetail].[Status] = 1 
WHERE [SAL_DeliveryOrderDetail].[SalesOrderHeaderID] = @SalesOrderHeaderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID 
	AND [SAL_DeliveryOrderDetail].[Status] = 1

Group By [SAL_OrderDetail].[OrderID], [SAL_OrderDetail].[OrderDetailID], [SAL_OrderDetail].[Quantity]
Having Sum([SAL_DeliveryOrderDetail].[Quantity]) >= [SAL_OrderDetail].[Quantity] ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@SalesOrderHeaderID", salesOrderHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataTable dtSalesOrder = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, Query_CheckQuantity, sqlparams).Tables[0];

                if (dtSalesOrder != null && dtSalesOrder.Rows.Count > 0)
                {
                    bool result = false;
                    SAL_OrderHeader soh = new SAL_OrderHeader();

                    foreach (DataRow dr in dtSalesOrder.Rows)
                    {
                        result = soh.UpdateFlagDetail(sqltrans, Convert.ToInt32(dr["OrderID"]), Convert.ToInt32(dr["OrderDetailID"]), closedCode, companyID, periodID, updatedBy, updateDate, updatedByValue);

                        if (!result)
                            break;
                    }

                    if (result)
                    {
                        if (soh.CheckClosedCode(sqltrans, salesOrderHeaderID, closedCode, companyID, periodID))
                            soh.UpdateFlagHeader(sqltrans, salesOrderHeaderID, closedCode, companyID, periodID, updatedBy, updateDate, updatedByValue);
                    }
                    else
                        throw new BusinessLogicException("Error generated: SAL_DeliveryOrderHeader.cs, Line: 209, Function: SOH.UpdateFlagDetail");
                }
                else
                {
                    bool result = false;
                    SAL_OrderHeader soh = new SAL_OrderHeader();
                    result = soh.UpdateFlagSOHeader(sqltrans, salesOrderHeaderID,companyID, periodID, updatedBy, updateDate, updatedByValue,null);
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        internal bool UpdateFlagDetail(SqlTransaction sqltrans, int dOHeaderID, int dODetailID, bool status, int companyID, int periodID, Guid updatedBy, DateTime updateDate, string updatedByValue, SOEnumaration.SalesOrder_Stage closedCode)
        {
            bool result = false;

            string Query_Update_Flag_Detail = @" UPDATE [SAL_DeliveryOrderDetail] SET [IsDispatchGenerated] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE  [DOHeaderID] = @DOHeaderID AND [DODetailID] = @DODetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID  AND [Status] = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@DOHeaderID", dOHeaderID);
                sqlparams[1] = new SqlParameter("@DODetailID", dODetailID);
                sqlparams[2] = new SqlParameter("@Status", status);
                sqlparams[3] = new SqlParameter("@CompanyID", companyID);
                sqlparams[4] = new SqlParameter("@PeriodID", periodID);
                sqlparams[5] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[6] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[7] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(sqltrans, CommandType.Text, Query_Update_Flag_Detail, sqlparams) > 0)
                {
                    result = true;

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(GetSalesOrderHeaderID(dOHeaderID, dODetailID, companyID, periodID, sqltrans), companyID, periodID, sqltrans, closedCode, updatedBy, updateDate, updatedByValue);

                    #endregion
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        private int GetSalesOrderHeaderID(int dOHeaderID, int dODetailID, int companyID, int periodID, SqlTransaction sqltrans)
        {
            int ID = 0;

            string Query_Get_SOHID = @" SELECT [SalesOrderHeaderID] FROM [SAL_DeliveryOrderDetail] WHERE [DOHeaderID] = @DOHeaderID AND [DODetailID] = @DODetailID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DOHeaderID", dOHeaderID);
                sqlparams[1] = new SqlParameter("@DODetailID", dODetailID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(sqltrans, CommandType.Text, Query_Get_SOHID, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public bool CheckIsDetailCompleted(SqlTransaction objTransaction, long deliveryOrderID, bool isGenerated, int companyID, int periodID)
        {
            bool result = false;

            try
            {
                #region
                string Query_CheckIsDetailCompleted = @" SELECT COUNT([SAL_DeliveryOrderDetail].[IsDispatchGenerated]) AS IsDispatchGeneratedCount FROM [SAL_DeliveryOrderDetail] WHERE [SAL_DeliveryOrderDetail].[DOHeaderID] = @DeliveryOrderID AND [SAL_DeliveryOrderDetail].[CompanyID] = @CompanyID AND [SAL_DeliveryOrderDetail].[PeriodID] = @PeriodID AND [SAL_DeliveryOrderDetail].[Status] = 1 AND [SAL_DeliveryOrderDetail].[IsDispatchGenerated] = @IsGenerated ";
                #endregion
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@IsGenerated", isGenerated);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                if (Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_CheckIsDetailCompleted, sqlparams)) == 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        internal bool UpdateFlagHeader(SqlTransaction objTransaction, long deliveryOrderID, bool isGenerated, int companyID, int periodID, Guid updatedBy, DateTime updateDate, string updatedByValue)
        {
            bool result = false;

            string Query_Update_Flag_Header = @" UPDATE [SAL_DeliveryOrderHeader] SET [IsDispatchGenerated] = @IsGenerated, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [DeliveryOrderID] = @DeliveryOrderID AND [CompanyID] = @CompanyID AND [PeriodID] = @PeriodID AND [Status] = 1 ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[1] = new SqlParameter("@IsGenerated", isGenerated);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Update_Flag_Header, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataTable GetDeliveryOrderReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_Order_Report = @"/* SELECT 
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SAL_DeliveryOrderHeader.CompanyID, 
SAL_DeliveryOrderHeader.PeriodID, 
SAL_DeliveryOrderHeader.DODate, SET_DocumentType.DocumentType, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
    SET_Location.LocationName, SAL_OrderHeader.ContractNo, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddress1, SET_OperatingUnit.AddressLine2 AS CompanyAddress2, SET_OperatingUnit.AddressLine3 AS CompanyAddress3, 
    SET_City.CityName AS CompanyCity, SET_OperatingUnit.ZipCode AS CompanyZip, SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, [SET_OperatingUnit].Logoimage as LogoImage, AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS CustomerAddress, CustomerCity.CityName AS CustomerCity, AR_SET_Customer.ZipCode AS CustomerZip, 
    AR_SET_Customer.PhoneNumber AS CustomerPhone, SET_Term.Description AS Terms, INV_Item.Description AS ItemName, INV_SET_Category.CategoryName,
    SET_UnitofMeasure.UOMName, 
    --SAL_DeliveryOrderDetail.Quantity, 
    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as Quantity, 
    Set_Batch.Description as Batch, 
    SAL_OrderDetail.Rate as Rate, 
    (
select 
(CASE LEN(SAL_DeliveryOrderHeader.CompanyID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) end) + 
(CASE LEN(SAL_DeliveryOrderHeader.PeriodID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) end) +
(CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID) when 1 then '00000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 2 then '0000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 3 then '000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 4 then '00' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 5 then '0' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	else CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	 end) 

) as BarCode,

    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus,SAL_DeliveryOrderHeader.DOValidityDate
    ,DocumentTypesGroup.GroupName
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber,
		  (Select 		(case when ShipViaCode IS NULL or ShipViaCode=''
		then 'NULL'
		else
			(case when ShipViaCode=1 
				then 'ByAir'
				when ShipViaCode=2
				then 'ByRoad'
				when ShipViaCode=3
				then 'BySea'
				else 'Null'
				 End )
		end) as ShipVia 
		from SAL_OrderHeader where SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
		AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID )as ShipVia
FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID 
	INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID 
	INNER JOIN AR_SET_Customer ON 
--SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
	LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
     LEFT Join DocumentTypesGroup On  DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
inner join Set_PeriodCustom on SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 
*/
SELECT  SAL_DeliveryOrderHeader.DeliveryOrderID ,
        SAL_DeliveryOrderHeader.CompanyID ,
        SAL_DeliveryOrderHeader.PeriodID ,
        SAL_DeliveryOrderHeader.DODate ,
        SET_DocumentType.DocumentType ,
        SAL_DeliveryOrderHeader.DeliveryOrderNo ,
        SET_Location.LocationName ,
        --SAL_OrderHeader.ContractNo ,
        ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo,
        SET_Business.BusinessName + ' - '
        + SET_OperatingUnit.OperatingUnitName AS CompanyName ,
        SET_OperatingUnit.AddressLine1 AS CompanyAddress1 ,
        SET_OperatingUnit.AddressLine2 AS CompanyAddress2 ,
        SET_OperatingUnit.AddressLine3 AS CompanyAddress3 ,
        SET_City.CityName AS CompanyCity ,
        SET_OperatingUnit.ZipCode AS CompanyZip ,
        SET_OperatingUnit.PhoneNumber AS CompanyPhone ,
        SET_OperatingUnit.FaxNumber AS CompanyFax ,
        [SET_OperatingUnit].Logoimage AS LogoImage ,
        AR_SET_Customer.CustomerName ,
        AR_SET_Customer.AddressLine1 AS CustomerAddress ,
        CustomerCity.CityName AS CustomerCity ,
        AR_SET_Customer.ZipCode AS CustomerZip ,
        AR_SET_Customer.PhoneNumber AS CustomerPhone ,
        SET_Term.Description AS Terms ,
        INV_Item.Description AS ItemName ,
        INV_Item.PrimaryUnitOfMeasure,
        INV_Item.UnitWeight,
        INV_SET_Category.CategoryName ,
        SET_UnitofMeasure.UOMName , 
    --SAL_DeliveryOrderDetail.Quantity, 
        SAL_DeliveryOrderDetail.Quantity
        - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) AS Quantity ,
        Set_Batch.Description AS Batch ,
        SAL_OrderDetail.Rate AS Rate ,
        --( SELECT    ( CASE LEN(SAL_DeliveryOrderHeader.CompanyID)
          --              WHEN 1
            --            THEN '0'
              --               + CAST(SAL_DeliveryOrderHeader.CompanyID AS VARCHAR(2))
                --        ELSE CAST(SAL_DeliveryOrderHeader.CompanyID AS VARCHAR(2))
                  --    END ) + ( CASE LEN(SAL_DeliveryOrderHeader.PeriodID)
                    --              WHEN 1
                      --            THEN '0'
                        --               + CAST(SAL_DeliveryOrderHeader.PeriodID AS VARCHAR(2))
                          --        ELSE CAST(SAL_DeliveryOrderHeader.PeriodID AS VARCHAR(2))
                            --    END )
                    -- + ( CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID)
                       --   WHEN 1
                         -- THEN '00000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                         -- WHEN 2
                         -- THEN '0000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                         -- WHEN 3
                         -- THEN '000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --WHEN 4
                          --THEN '00'
                            --   + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --WHEN 5
                          --THEN '0'
                            --   + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                        --END )
        --) AS BarCode ,
 ( SELECT    (CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderNo)
                          WHEN 1
                          THEN '00000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 2
                          THEN '0000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 3
                          THEN '000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 4
                          THEN '00000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 5
                          THEN '0000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                        END )
        ) AS BarCode ,
        ISNULL(( SELECT LookupDetail.EnumeratedDescription
                 FROM   LookupDetail
                 WHERE  LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode
                        AND LookupHeaderID = 5
               ), '') AS DocumentStatus ,
        SAL_DeliveryOrderHeader.DOValidityDate ,
        DocumentTypesGroup.GroupName ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( SELECT    ( CASE WHEN ShipViaCode IS NULL
                                OR ShipViaCode = '' THEN 'NULL'
                           ELSE ( CASE WHEN ShipViaCode = 1 THEN 'ByAir'
                                       WHEN ShipViaCode = 2 THEN 'ByRoad'
                                       WHEN ShipViaCode = 3 THEN 'BySea'
                                       ELSE 'Null'
                                  END )
                      END ) AS ShipVia
          FROM      SAL_OrderHeader
          WHERE     SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                    AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                    AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        ) AS ShipVia ,
        Agent.CustomerName AS AgentName
FROM    SAL_DeliveryOrderHeader
        INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
                                              AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
                                              AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
                                              AND SAL_DeliveryOrderDetail.Status = 1
        INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
                                      AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
                                      AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
                                      AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
        INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
                                         AND [INV_SET_Category].[Status] = 1
        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                                      AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                                      AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        INNER JOIN INV_Item ON --INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID
        INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
        LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID
        INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID
        INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId
                                        AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
        INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID
        INNER JOIN AR_SET_Customer ON --SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID
                                      AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID
        INNER JOIN AR_SET_Customer Agent ON --SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_OrderHeader.PeriodID = Agent.PeriodID
                                            AND SAL_OrderHeader.brokerid = Agent.CustomerID
        LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID
        INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID
                                       AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID
                                       AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        LEFT JOIN DocumentTypesGroup ON DocumentTypesGroup.GroupID = SET_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And
        LEFT JOIN Set_Batch ON Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
                               AND Set_Batch.BatchID = SAL_OrderHeader.Season
        INNER JOIN Set_PeriodCustom ON SAL_DeliveryOrderHeader.DODate BETWEEN Set_PeriodCustom.StartDate
                                                              AND
                                                              Set_PeriodCustom.EndDate
                                       AND Set_PeriodCustom.Status = 1
                                       AND Set_PeriodCustom.FiscalID = @CurrentFiscalID

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
        AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
        AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
        AND SAL_ContractHeader.Status = 1

WHERE   SAL_DeliveryOrderHeader.CompanyID = @CompanyID
        AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
        AND SAL_DeliveryOrderHeader.Status = 1


";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_Report + additionalFilter, sqlparams);

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

        public static DataTable GetDeliveryOrderReportDataNew(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_Order_Report = @"SELECT  SAL_DeliveryOrderHeader.DeliveryOrderID ,
        SAL_DeliveryOrderHeader.CompanyID ,
        SAL_DeliveryOrderHeader.PeriodID ,
        SAL_DeliveryOrderHeader.DODate ,
        SET_DocumentType.DocumentType ,
        SAL_DeliveryOrderHeader.DeliveryOrderNo ,
        SET_Location.LocationName ,
        ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo,
        SET_Business.BusinessName + ' - '
        + SET_OperatingUnit.OperatingUnitName AS CompanyName ,
        SET_OperatingUnit.AddressLine1 AS CompanyAddress1 ,
        SET_OperatingUnit.AddressLine2 AS CompanyAddress2 ,
        SET_OperatingUnit.AddressLine3 AS CompanyAddress3 ,
        SET_City.CityName AS CompanyCity ,
        SET_OperatingUnit.ZipCode AS CompanyZip ,
        SET_OperatingUnit.PhoneNumber AS CompanyPhone ,
        SET_OperatingUnit.FaxNumber AS CompanyFax ,
        [SET_OperatingUnit].Logoimage AS LogoImage ,
        AR_SET_Customer.CustomerName ,
        AR_SET_Customer.AddressLine1 AS CustomerAddress ,
        CustomerCity.CityName AS CustomerCity ,
        AR_SET_Customer.ZipCode AS CustomerZip ,
        AR_SET_Customer.PhoneNumber AS CustomerPhone ,
        SET_Term.Description AS Terms ,
        INV_Item.Description AS ItemName ,
        INV_Item.PrimaryUnitOfMeasure,
        INV_Item.UnitWeight,
        INV_SET_Category.CategoryName ,
        SET_UnitofMeasure.UOMName ,
        SAL_DeliveryOrderDetail.Quantity
        - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) AS Quantity ,
        Set_Batch.Description AS Batch ,
        SAL_OrderDetail.Rate AS Rate ,
 ( SELECT    (CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderNo)
                          WHEN 1
                          THEN '00000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 2
                          THEN '0000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 3
                          THEN '000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 4
                          THEN '00000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 5
                          THEN '0000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                        END )
        ) AS BarCode ,
        ISNULL(( SELECT LookupDetail.EnumeratedDescription
                 FROM   LookupDetail
                 WHERE  LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode
                        AND LookupHeaderID = 5
               ), '') AS DocumentStatus ,
        SAL_DeliveryOrderHeader.DOValidityDate ,
        DocumentTypesGroup.GroupName ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( SELECT    ( CASE WHEN ShipViaCode IS NULL
                                OR ShipViaCode = '' THEN 'NULL'
                           ELSE ( CASE WHEN ShipViaCode = 1 THEN 'ByAir'
                                       WHEN ShipViaCode = 2 THEN 'ByRoad'
                                       WHEN ShipViaCode = 3 THEN 'BySea'
                                       ELSE 'Null'
                                  END )
                      END ) AS ShipVia
          FROM      SAL_OrderHeader
          WHERE     SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                    AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                    AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        ) AS ShipVia ,
        Agent.CustomerName AS AgentName,
        SAL_ContractHeader.ContractDate
FROM    SAL_DeliveryOrderHeader
        INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
                                              AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
                                              AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
                                              AND SAL_DeliveryOrderDetail.Status = 1
        INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
                                      AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
                                      AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
                                      AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
        INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID]
                                         AND [INV_SET_Category].[Status] = 1
        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                                      AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                                      AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID
        INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
        LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID
        INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID
        INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId
                                        AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
        INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID
        INNER JOIN AR_SET_Customer ON SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID
                                      AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID
        INNER JOIN AR_SET_Customer Agent ON SAL_OrderHeader.PeriodID = Agent.PeriodID
                                            AND SAL_OrderHeader.brokerid = Agent.CustomerID
        LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID
        INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID
                                       AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID
                                       AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        LEFT JOIN DocumentTypesGroup ON DocumentTypesGroup.GroupID = SET_DocumentType.GroupID
        LEFT JOIN Set_Batch ON Set_Batch.PeriodID = SAL_OrderHeader.PeriodID AND Set_Batch.BatchID = SAL_OrderHeader.Season
        INNER JOIN Set_PeriodCustom ON SAL_DeliveryOrderHeader.DODate BETWEEN Set_PeriodCustom.StartDate
                                                              AND
                                                              Set_PeriodCustom.EndDate
                                       AND Set_PeriodCustom.Status = 1
                                       AND Set_PeriodCustom.FiscalID = @CurrentFiscalID

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
        AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
        AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
        AND SAL_ContractHeader.Status = 1

WHERE   SAL_DeliveryOrderHeader.CompanyID = @CompanyID
        AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
        AND SAL_DeliveryOrderHeader.Status = 1
";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_Report + additionalFilter, sqlparams);

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

        public static DataTable GetDODetailReportDataNew(int companyID, int periodID, Nullable<int> soNumberFrom, Nullable<int> soNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;
            string SortOrder = string.Empty;

            #region Query

            string Query_DODetail_Report = @"SELECT DISTINCT SAL_ContractHeader.ContractID, SAL_DeliveryOrderHeader.DeliveryOrderID ,
        SAL_DeliveryOrderHeader.CompanyID ,
        SAL_DeliveryOrderHeader.PeriodID ,
        SAL_DeliveryOrderHeader.DODate ,
        SET_DocumentType.DocumentType ,
        SAL_DeliveryOrderHeader.DeliveryOrderNo ,
        SET_Location.LocationName ,
        ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo,
        SET_Business.BusinessName + ' - '
        + SET_OperatingUnit.OperatingUnitName AS CompanyName ,
        SET_OperatingUnit.AddressLine1 AS CompanyAddress1 ,
        SET_OperatingUnit.AddressLine2 AS CompanyAddress2 ,
        SET_OperatingUnit.AddressLine3 AS CompanyAddress3 ,
        SET_City.CityName AS CompanyCity ,
        SET_OperatingUnit.ZipCode AS CompanyZip ,
        SET_OperatingUnit.PhoneNumber AS CompanyPhone ,
        SET_OperatingUnit.FaxNumber AS CompanyFax ,
        [SET_OperatingUnit].Logoimage AS LogoImage ,
        AR_SET_Customer.CustomerName ,
        AR_SET_Customer.AddressLine1 AS CustomerAddress ,
        CustomerCity.CityName AS CustomerCity ,
        AR_SET_Customer.ZipCode AS CustomerZip ,
        AR_SET_Customer.PhoneNumber AS CustomerPhone ,
        SET_Term.Description AS Terms ,
        INV_Item.Description AS ItemName ,
        INV_Item.PrimaryUnitOfMeasure,
        INV_Item.UnitWeight,
        INV_SET_Category.CategoryName ,
        SET_UnitofMeasure.UOMName ,
        SAL_DeliveryOrderDetail.Quantity
        - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) AS Quantity ,
        Set_Batch.Description AS Batch ,
        SAL_OrderDetail.Rate AS Rate ,
 ( SELECT    (CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderNo)
                          WHEN 1
                          THEN '00000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 2
                          THEN '0000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 3
                          THEN '000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 4
                          THEN '00000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 5
                          THEN '0000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                        END )
        ) AS BarCode ,
        ISNULL(( SELECT LookupDetail.EnumeratedDescription
                 FROM   LookupDetail
                 WHERE  LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode
                        AND LookupHeaderID = 5
               ), '') AS DocumentStatus ,
        SAL_DeliveryOrderHeader.DOValidityDate ,
        DocumentTypesGroup.GroupName ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( SELECT    ( CASE WHEN ShipViaCode IS NULL
                                OR ShipViaCode = '' THEN 'NULL'
                           ELSE ( CASE WHEN ShipViaCode = 1 THEN 'ByAir'
                                       WHEN ShipViaCode = 2 THEN 'ByRoad'
                                       WHEN ShipViaCode = 3 THEN 'BySea'
                                       ELSE 'Null'
                                  END )
                      END ) AS ShipVia
          FROM      SAL_OrderHeader
          WHERE     SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                    AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                    AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        ) AS ShipVia ,
        Agent.CustomerName AS AgentName,
        SAL_ContractHeader.ContractDate,
        AR_ReceiptTransactionHeader.SourceHeaderID,
        SAL_ContractHeader.SOValidityDate,
		Transporter.CustomerName AS TransporterName
FROM    SAL_DeliveryOrderHeader
        INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
                                              AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
                                              AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
                                              AND SAL_DeliveryOrderDetail.Status = 1
        INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
                                      AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
                                      AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
                                      AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
        INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID]
                                         AND [INV_SET_Category].[Status] = 1
        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                                      AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                                      AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        INNER JOIN INV_Item ON INV_Item.ItemID = SAL_OrderDetail.ItemID
        INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
        LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID
        INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID
        INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId
                                        AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
        INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID
        INNER JOIN AR_SET_Customer ON SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID
                                      AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID
        INNER JOIN AR_SET_Customer Agent ON SAL_OrderHeader.PeriodID = Agent.PeriodID
                                            AND SAL_OrderHeader.brokerid = Agent.CustomerID
        LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID
        INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID
                                       AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID
                                       AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        LEFT JOIN DocumentTypesGroup ON DocumentTypesGroup.GroupID = SET_DocumentType.GroupID
        LEFT JOIN Set_Batch ON Set_Batch.PeriodID = SAL_OrderHeader.PeriodID AND Set_Batch.BatchID = SAL_OrderHeader.Season
        INNER JOIN Set_PeriodCustom ON SAL_DeliveryOrderHeader.DODate BETWEEN Set_PeriodCustom.StartDate
                                                              AND
                                                              Set_PeriodCustom.EndDate
                                       AND Set_PeriodCustom.Status = 1
                                       AND Set_PeriodCustom.FiscalID = @CurrentFiscalID

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
        AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
        AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
        AND SAL_ContractHeader.Status = 1

        LEFT JOIN dbo.AR_ReceiptTransactionHeader ON AR_ReceiptTransactionHeader.SourceHeaderID = SAL_ContractHeader.ContractID
		AND AR_ReceiptTransactionHeader.CompanyID = SAL_ContractHeader.CompanyID AND AR_ReceiptTransactionHeader.PeriodID = SAL_ContractHeader.PeriodID
		AND AR_ReceiptTransactionHeader.Status = 1

		LEFT JOIN dbo.AR_SET_Customer AS Transporter ON SAL_DeliveryOrderHeader.PeriodID = Transporter.PeriodID
		AND SAL_DeliveryOrderHeader.TransporterID = Transporter.CustomerID

WHERE   SAL_DeliveryOrderHeader.CompanyID = @CompanyID
        AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
        AND SAL_DeliveryOrderHeader.Status = 1
";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (soNumberFrom > 0 && soNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_ContractHeader.ContractNo BETWEEN @SONumberFrom AND @SONumberTo ";
                    sqlparams[2] = new SqlParameter("@SONumberFrom", soNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@SONumberTo", soNumberTo.Value);
                }
                else if (soNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_ContractHeader.ContractNo >= @SONumberFrom ";
                    sqlparams[2] = new SqlParameter("@SONumberFrom", soNumberFrom.Value);
                }
                else if (soNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_ContractHeader.ContractNo <= @SONumberTo ";
                    sqlparams[2] = new SqlParameter("@SONumberTo", soNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_ContractHeader.ContractDate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_ContractHeader.ContractDate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_ContractHeader.ContractDate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_ContractHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_ContractHeader.SaleContractTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                SortOrder = "ORDER BY SAL_DeliveryOrderHeader.DODate";

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DODetail_Report + additionalFilter + SortOrder, sqlparams);

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

        public static DataTable GetDeliveryOrderReport_RSM_Data(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Delivery_Order_RSM_Report + additionalFilter, sqlparams);

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

        public static DataTable GetDeliveryOrder_SSMLReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Delivery_Order_SSML_Report + additionalFilter, sqlparams);


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

        public static DataTable GetDeliveryOrderSSRRReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_Order_SSRR_Report = @"

";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

               // DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_SSRR_Report + additionalFilter, sqlparams);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_Delivery_Order_SSRR_Report + additionalFilter, sqlparams);


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


        public static DataTable GetDeliveryOrderHSMReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
           Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
           Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_OrderHSM_Report = @"/* SELECT 
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SAL_DeliveryOrderHeader.CompanyID, 
SAL_DeliveryOrderHeader.PeriodID, 
SAL_DeliveryOrderHeader.DODate, SET_DocumentType.DocumentType, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
    SET_Location.LocationName, SAL_OrderHeader.ContractNo, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddress1, SET_OperatingUnit.AddressLine2 AS CompanyAddress2, SET_OperatingUnit.AddressLine3 AS CompanyAddress3, 
    SET_City.CityName AS CompanyCity, SET_OperatingUnit.ZipCode AS CompanyZip, SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, [SET_OperatingUnit].Logoimage as LogoImage, AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS CustomerAddress, CustomerCity.CityName AS CustomerCity, AR_SET_Customer.ZipCode AS CustomerZip, 
    AR_SET_Customer.PhoneNumber AS CustomerPhone, SET_Term.Description AS Terms, INV_Item.Description AS ItemName, INV_SET_Category.CategoryName,
    SET_UnitofMeasure.UOMName, 
    --SAL_DeliveryOrderDetail.Quantity, 
    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as Quantity, 
    Set_Batch.Description as Batch, 
    SAL_OrderDetail.Rate as Rate, 
    (
select 
(CASE LEN(SAL_DeliveryOrderHeader.CompanyID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) end) + 
(CASE LEN(SAL_DeliveryOrderHeader.PeriodID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) end) +
(CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID) when 1 then '00000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 2 then '0000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 3 then '000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 4 then '00' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 5 then '0' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	else CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	 end) 

) as BarCode,

    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus,SAL_DeliveryOrderHeader.DOValidityDate
    ,DocumentTypesGroup.GroupName
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber,
		  (Select 		(case when ShipViaCode IS NULL or ShipViaCode=''
		then 'NULL'
		else
			(case when ShipViaCode=1 
				then 'ByAir'
				when ShipViaCode=2
				then 'ByRoad'
				when ShipViaCode=3
				then 'BySea'
				else 'Null'
				 End )
		end) as ShipVia 
		from SAL_OrderHeader where SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
		AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID )as ShipVia
FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID 
	INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID 
	INNER JOIN AR_SET_Customer ON 
--SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
	LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
     LEFT Join DocumentTypesGroup On  DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
inner join Set_PeriodCustom on SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 
*/
SELECT  SAL_DeliveryOrderHeader.DeliveryOrderID ,
        SAL_DeliveryOrderHeader.CompanyID ,
        SAL_DeliveryOrderHeader.PeriodID ,
        SAL_DeliveryOrderHeader.DODate ,
        SET_DocumentType.DocumentType ,
        SAL_DeliveryOrderHeader.DeliveryOrderNo ,
        SET_Location.LocationName ,
        --SAL_OrderHeader.ContractNo ,
        ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo,
        SET_Business.BusinessName + ' - '
        + SET_OperatingUnit.OperatingUnitName AS CompanyName ,
        SET_OperatingUnit.AddressLine1 AS CompanyAddress1 ,
        SET_OperatingUnit.AddressLine2 AS CompanyAddress2 ,
        SET_OperatingUnit.AddressLine3 AS CompanyAddress3 ,
        SET_City.CityName AS CompanyCity ,
        SET_OperatingUnit.ZipCode AS CompanyZip ,
        SET_OperatingUnit.PhoneNumber AS CompanyPhone ,
        SET_OperatingUnit.FaxNumber AS CompanyFax ,
        [SET_OperatingUnit].Logoimage AS LogoImage ,
        AR_SET_Customer.CustomerName ,
        AR_SET_Customer.AddressLine1 AS CustomerAddress ,
        CustomerCity.CityName AS CustomerCity ,
        AR_SET_Customer.ZipCode AS CustomerZip ,
        AR_SET_Customer.PhoneNumber AS CustomerPhone ,
        SET_Term.Description AS Terms ,
        INV_Item.Description AS ItemName ,
        INV_Item.PrimaryUnitOfMeasure,
        INV_Item.UnitWeight,
        INV_SET_Category.CategoryName ,
        SET_UnitofMeasure.UOMName , 
    --SAL_DeliveryOrderDetail.Quantity, 
        SAL_DeliveryOrderDetail.Quantity
        - ISNULL(SAL_DeliveryOrderDetail.ReversedQty, 0) AS Quantity ,
        Set_Batch.Description AS Batch ,
        SAL_OrderDetail.Rate AS Rate ,
        --( SELECT    ( CASE LEN(SAL_DeliveryOrderHeader.CompanyID)
          --              WHEN 1
            --            THEN '0'
              --               + CAST(SAL_DeliveryOrderHeader.CompanyID AS VARCHAR(2))
                --        ELSE CAST(SAL_DeliveryOrderHeader.CompanyID AS VARCHAR(2))
                  --    END ) + ( CASE LEN(SAL_DeliveryOrderHeader.PeriodID)
                    --              WHEN 1
                      --            THEN '0'
                        --               + CAST(SAL_DeliveryOrderHeader.PeriodID AS VARCHAR(2))
                          --        ELSE CAST(SAL_DeliveryOrderHeader.PeriodID AS VARCHAR(2))
                            --    END )
                    -- + ( CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID)
                       --   WHEN 1
                         -- THEN '00000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                         -- WHEN 2
                         -- THEN '0000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                         -- WHEN 3
                         -- THEN '000'
                           --    + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --WHEN 4
                          --THEN '00'
                            --   + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --WHEN 5
                          --THEN '0'
                            --   + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                          --ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderID AS VARCHAR)
                        --END )
        --) AS BarCode ,
 ( SELECT    (CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderNo)
                          WHEN 1
                          THEN '00000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 2
                          THEN '0000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 3
                          THEN '000000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 4
                          THEN '00000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          WHEN 5
                          THEN '0000000'
                               + CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                          ELSE CAST(SAL_DeliveryOrderHeader.DeliveryOrderNo AS VARCHAR)
                        END )
        ) AS BarCode ,
        ISNULL(( SELECT LookupDetail.EnumeratedDescription
                 FROM   LookupDetail
                 WHERE  LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode
                        AND LookupHeaderID = 5
               ), '') AS DocumentStatus ,
        SAL_DeliveryOrderHeader.DOValidityDate ,
        DocumentTypesGroup.GroupName ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( CASE SET_DocumentType.GroupID
            WHEN NULL
            THEN SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
            ELSE SUBSTRING(CONVERT(VARCHAR(20), YEAR(SAL_DeliveryOrderHeader.DODate)),
                             3, 2) + '000'
                 + CONVERT(VARCHAR(24), SAL_DeliveryOrderHeader.DeliveryOrderNo)
          END ) AS DINumber ,
        ( SELECT    ( CASE WHEN ShipViaCode IS NULL
                                OR ShipViaCode = '' THEN 'NULL'
                           ELSE ( CASE WHEN ShipViaCode = 1 THEN 'ByAir'
                                       WHEN ShipViaCode = 2 THEN 'ByRoad'
                                       WHEN ShipViaCode = 3 THEN 'BySea'
                                       ELSE 'Null'
                                  END )
                      END ) AS ShipVia
          FROM      SAL_OrderHeader
          WHERE     SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                    AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                    AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        ) AS ShipVia ,
        Agent.CustomerName AS AgentName
FROM    SAL_DeliveryOrderHeader
        INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
                                              AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
                                              AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
                                              AND SAL_DeliveryOrderDetail.Status = 1
        INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
                                      AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
                                      AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
                                      AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
        INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
                                         AND [INV_SET_Category].[Status] = 1
        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
                                      AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
                                      AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
        INNER JOIN INV_Item ON --INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID
        INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
        LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID
        INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID
        INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId
        INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId
        INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId
                                        AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId
        INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID
        INNER JOIN AR_SET_Customer ON --SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID
                                      AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID
        INNER JOIN AR_SET_Customer Agent ON --SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_OrderHeader.PeriodID = Agent.PeriodID
                                            AND SAL_OrderHeader.brokerid = Agent.CustomerID
        LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID
        INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID
                                       AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID
                                       AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
        LEFT JOIN DocumentTypesGroup ON DocumentTypesGroup.GroupID = SET_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And
        LEFT JOIN Set_Batch ON Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
                               AND Set_Batch.BatchID = SAL_OrderHeader.Season
        INNER JOIN Set_PeriodCustom ON SAL_DeliveryOrderHeader.DODate BETWEEN Set_PeriodCustom.StartDate
                                                              AND
                                                              Set_PeriodCustom.EndDate
                                       AND Set_PeriodCustom.Status = 1
                                       AND Set_PeriodCustom.FiscalID = @CurrentFiscalID

        INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
        AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
        AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
        AND SAL_ContractHeader.Status = 1

WHERE   SAL_DeliveryOrderHeader.CompanyID = @CompanyID
        AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID
        AND SAL_DeliveryOrderHeader.Status = 1


";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_OrderHSM_Report + additionalFilter, sqlparams);

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


        //Sugar Do
        public static DataTable GetSugarDeliveryOrderReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
          Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
          Nullable<int> batchID = null, int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_Order_Report = @" SELECT 
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SAL_DeliveryOrderHeader.CompanyID, 
SAL_DeliveryOrderHeader.PeriodID, 
SAL_DeliveryOrderHeader.DODate, SET_DocumentType.DocumentType, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
    SET_Location.LocationName, SAL_OrderHeader.ContractNo, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddress1, SET_OperatingUnit.AddressLine2 AS CompanyAddress2, SET_OperatingUnit.AddressLine3 AS CompanyAddress3, 
    SET_City.CityName AS CompanyCity, SET_OperatingUnit.ZipCode AS CompanyZip, SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, [SET_OperatingUnit].Logoimage as LogoImage, AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS CustomerAddress, CustomerCity.CityName AS CustomerCity, AR_SET_Customer.ZipCode AS CustomerZip, 
    AR_SET_Customer.PhoneNumber AS CustomerPhone, SET_Term.Description AS Terms, INV_Item.Description AS ItemName, INV_SET_Category.CategoryName,
    SET_UnitofMeasure.UOMName, 
    --SAL_DeliveryOrderDetail.Quantity, 
    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as Quantity, 
    Set_Batch.Description as Batch, 
    SAL_OrderDetail.Rate as Rate, 
    (
select 
(CASE LEN(SAL_DeliveryOrderHeader.CompanyID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) end) + 
(CASE LEN(SAL_DeliveryOrderHeader.PeriodID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) end) +
(CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID) when 1 then '00000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 2 then '0000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 3 then '000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 4 then '00' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 5 then '0' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	else CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	 end) 

) as BarCode,

    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus,SAL_DeliveryOrderHeader.DOValidityDate
    ,DocumentTypesGroup.GroupName
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber
    ,(Case  Set_DocumentType.GroupID when null
          then  ' '+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          else  substring(Right(DocumentTypesGroup.GroupName,3),1,1)+'DI' +'-'+ substring(convert(varchar(20),year(SAL_DeliveryOrderHeader.DODate)),3,2) + '000' +convert(varchar(24),SAL_DeliveryOrderHeader.DeliveryOrderNo)
          end  ) as DINumber,
		  (Select 		(case when ShipViaCode IS NULL or ShipViaCode=''
		then 'NULL'
		else
			(case when ShipViaCode=1 
				then 'ByAir'
				when ShipViaCode=2
				then 'ByRoad'
				when ShipViaCode=3
				then 'BySea'
				else 'Null'
				 End )
		end) as ShipVia 
		from SAL_OrderHeader where SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID 
		AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID )as ShipVia
		,SAL_OrderHeader.BrokerID,Broker.CustomerName as BrokerName
,case when isnull([INV_Item].PrimaryUnitOfMeasure,0) <> 0 
then
((SAL_DeliveryOrderDetail.Quantity *  [INV_Item].UnitWeight) / isnull([INV_Item].PrimaryUnitOfMeasure,1)) 
else 0 end
as Qtyintonbag

FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
		inner join AR_SET_Customer Broker on Broker.CustomerID=SAL_OrderHeader.BrokerID
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID 
	INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID 
	INNER JOIN AR_SET_Customer ON 
--SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
	LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
     LEFT Join DocumentTypesGroup On  DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
inner join Set_PeriodCustom on SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

WHERE SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (DocumentTypeID > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_Report + additionalFilter, sqlparams);

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



        public static DataTable GetDeliveryOrderReportDataForPolymer(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
            Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
            Nullable<int> batchID = null, short? documentGroupID=null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_Delivery_Order_Report = @" 
SELECT 
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SAL_DeliveryOrderHeader.CompanyID, 
SAL_DeliveryOrderHeader.PeriodID, 

SAL_DeliveryOrderHeader.DODate, SET_DocumentType.DocumentType, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
    SET_Location.LocationName, SAL_OrderHeader.ContractNo, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddress1, SET_OperatingUnit.AddressLine2 AS CompanyAddress2, SET_OperatingUnit.AddressLine3 AS CompanyAddress3, 
    SET_City.CityName AS CompanyCity, SET_OperatingUnit.ZipCode AS CompanyZip, SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, [SET_OperatingUnit].Logoimage as LogoImage, AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS CustomerAddress, CustomerCity.CityName AS CustomerCity, AR_SET_Customer.ZipCode AS CustomerZip, 
    AR_SET_Customer.PhoneNumber AS CustomerPhone, SET_Term.Description AS Terms, INV_Item.Description AS ItemName, INV_SET_Category.CategoryName,
    SET_UnitofMeasure.UOMName, 

    --SAL_DeliveryOrderDetail.Quantity, 

    SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) as Quantity, 

    Set_Batch.Description as Batch, 
    SAL_OrderDetail.Rate as Rate, 
    (
select 
(CASE LEN(SAL_DeliveryOrderHeader.CompanyID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.CompanyID as varchar(2)) end) + 
(CASE LEN(SAL_DeliveryOrderHeader.PeriodID) when 1 then '0' + CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) else CAST(SAL_DeliveryOrderHeader.PeriodID as varchar(2)) end) +
(CASE LEN(SAL_DeliveryOrderHeader.DeliveryOrderID) when 1 then '00000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 2 then '0000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 3 then '000' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 4 then '00' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	when 5 then '0' + CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	else CAST(SAL_DeliveryOrderHeader.DeliveryOrderID as varchar) 
	 end) 

) as BarCode,

SAL_OrderDetail.NoteToVendor,
SAl_DeliveryOrderHeader.ShipTo,
    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus
FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
    AND [INV_SET_Category].[Status] = 1 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID 
	INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID 
	INNER JOIN AR_SET_Customer ON --SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
	LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
WHERE SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 


";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (documentGroupID.HasValue)
                    additionalFilter = additionalFilter + " AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString();

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_Report + additionalFilter, sqlparams);

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



        public DataTable GetDelivery_OrderReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
           Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
           Nullable<int> batchID = null, Nullable<int> SONoFrom = null, Nullable<int> SONoTo = null)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            #region Query

            string Query_DeliveryOrder = @"
SELECT 
SAL_DeliveryOrderDetail.CreatedByValue,
set_Period.PeriodSetName,
SAL_OrderDetail.ItemUnitQuantity, 
SAL_DeliveryOrderHeader.dovaliditydate as ValidUpto,
SAL_OrderHeader.Quality as ItemQuality,
cus.customername as BrokerName,
SAL_DeliveryOrderHeader.DeliveryOrderID, 
SAL_DeliveryOrderHeader.CompanyID, 
SAL_DeliveryOrderHeader.PeriodID, 
SAL_DeliveryOrderHeader.DODate, SET_DocumentType.DocumentType, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
    SET_Location.LocationName, SAL_OrderHeader.ContractNo, SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, 
    SET_OperatingUnit.AddressLine1 AS CompanyAddress1, SET_OperatingUnit.AddressLine2 AS CompanyAddress2, SET_OperatingUnit.AddressLine3 AS CompanyAddress3, 
    SET_City.CityName AS CompanyCity, SET_OperatingUnit.ZipCode AS CompanyZip, SET_OperatingUnit.PhoneNumber AS CompanyPhone, 
    SET_OperatingUnit.FaxNumber AS CompanyFax, [SET_OperatingUnit].Logoimage as LogoImage, AR_SET_Customer.CustomerName, 
    AR_SET_Customer.AddressLine1 AS CustomerAddress, CustomerCity.CityName AS CustomerCity, AR_SET_Customer.ZipCode AS CustomerZip, 
    AR_SET_Customer.PhoneNumber AS CustomerPhone, SET_Term.Description AS Terms, INV_Item.Description AS ItemName, INV_SET_Category.CategoryName,
    SET_UnitofMeasure.UOMName, SAL_DeliveryOrderDetail.Quantity, Set_Batch.Description as Batch, 
    SAL_OrderDetail.Rate as Rate,INV_Item.UnitWeight,isnull(INV_Item.UnitVolume,1) as UnitVolume,(SAL_DeliveryOrderDetail.Quantity)*(INV_Item.UnitWeight)/(isnull(INV_Item.UnitVolume,1)) as Qty,



    ISNULL(
    (
        select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_DeliveryOrderHeader.StatusCode and LookupHeaderID = 5
    ), '') as DocumentStatus
FROM SAL_DeliveryOrderHeader 
	INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        AND SAL_DeliveryOrderDetail.Status = 1 
	INNER JOIN SAL_OrderDetail ON SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID AND SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
        AND SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID AND SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID 
	INNER JOIN [INV_SET_Category] ON [INV_SET_Category].[CategoryID] = [SAL_OrderDetail].[ItemCategoryID] 
        --AND [INV_SET_Category].[CompanyID] = [SAL_OrderDetail].[CompanyID] 
        AND [INV_SET_Category].[Status] = 1 
	INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID AND SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        AND SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID 
	INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID AND 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	INNER JOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID 
	LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	INNER JOIN SET_Location ON SET_Location.LocationID = SAL_DeliveryOrderHeader.LocationID 
	INNER JOIN SET_Company ON SAL_DeliveryOrderHeader.CompanyID = SET_Company.CompanyId 
	INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.BusinessId = SET_Business.BusinessId AND SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	INNER JOIN SET_City ON SET_City.CityID = SET_OperatingUnit.CityID 
	INNER JOIN AR_SET_Customer ON 
--SAL_DeliveryOrderHeader.CompanyID = AR_SET_Customer.CompanyID AND 
SAL_DeliveryOrderHeader.PeriodID = AR_SET_Customer.PeriodID 
        AND SAL_DeliveryOrderHeader.CustomerID = AR_SET_Customer.CustomerID 
        left join AR_SET_Customer as cus on SAL_OrderHeader.BrokerID = cus.CustomerID
        --and cus.CompanyID = SAL_OrderHeader.CompanyID
        and cus.PeriodID = SAL_OrderHeader.PeriodID
  inner join SET_Period on Set_period.PeriodId = SAL_OrderHeader.PeriodID
	LEFT JOIN SET_City AS CustomerCity ON AR_SET_Customer.CityID = CustomerCity.CityID 
	INNER JOIN SET_DocumentType ON SAL_DeliveryOrderHeader.CompanyID = SET_DocumentType.CompanyID AND SAL_DeliveryOrderHeader.PeriodID = SET_DocumentType.PeriodID 
        AND SAL_DeliveryOrderHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
    Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID --Set_Batch.CompanyID = SAL_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = SAL_OrderHeader.Season 
 
WHERE SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID AND SAL_DeliveryOrderHeader.Status = 1 


";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (doNumberFrom > 0 && doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                    sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }
                else if (doNumberFrom > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                    sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                }
                else if (doNumberTo > 0)
                {
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                    sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                }

                if (dateFrom != null && dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                else if (dateFrom != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                else if (dateTo != null)
                    additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                if (customerName > 0)
                    additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                if (itemName != string.Empty)
                    additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                if (statusCode > 0)
                    additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                if (batchID.HasValue)
                    additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                if (SONoFrom > 0 && SONoTo > 0)
                    additionalFilter += " AND SAL_OrderHeader.ContractNo between " + SONoFrom.Value + " and " + SONoTo.Value + " ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DeliveryOrder + additionalFilter, sqlparams);

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

        /*
         public static DataSet GetDeliveryOrderReportData(int companyID, int periodID, Nullable<int> doNumberFrom, Nullable<int> doNumberTo,
             Nullable<int> customerName, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, string itemName = null, Nullable<int> statusCode = null,
             Nullable<int> batchID = null)
         {
             DataSet dtToReturn = null;
             string additionalFilter = string.Empty;

             try
             {
                 SqlParameter[] sqlparams = new SqlParameter[4];
                 sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                 sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                 if (doNumberFrom > 0 && doNumberTo > 0)
                 {
                     additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo BETWEEN @DONumberFrom AND @DONumberTo ";
                     sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                     sqlparams[3] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                 }
                 else if (doNumberFrom > 0)
                 {
                     additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo >= @DONumberFrom ";
                     sqlparams[2] = new SqlParameter("@DONumberFrom", doNumberFrom.Value);
                 }
                 else if (doNumberTo > 0)
                 {
                     additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.DeliveryOrderNo <= @DONumberTo ";
                     sqlparams[2] = new SqlParameter("@DONumberTo", doNumberTo.Value);
                 }

                 if (dateFrom != null && dateTo != null)
                     additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate between '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "' and '" + ((DateTime)dateTo).ToString("MM-dd-yyyy hh:mm:ss") + "'";
                 else if (dateFrom != null)
                     additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate >= '" + ((DateTime)dateFrom).ToString("MM-dd-yyyy") + "'";
                 else if (dateTo != null)
                     additionalFilter = additionalFilter + " and SAL_DeliveryOrderHeader.DODate <= '" + ((DateTime)dateTo).ToString("MM-dd-yyyy") + "'";

                 if (customerName > 0)
                     additionalFilter = additionalFilter + " And AR_SET_Customer.CustomerID =" + customerName;

                 if (itemName != string.Empty)
                     additionalFilter = additionalFilter + " AND INV_Item .Description like '%" + itemName + "%'";

                 if (statusCode > 0)
                     additionalFilter = additionalFilter + " AND SAL_DeliveryOrderHeader.StatusCode = " + statusCode + "";

                 if (batchID.HasValue)
                     additionalFilter = additionalFilter + " AND SAL_OrderHeader.Season = " + batchID.Value;

                 DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Delivery_Order_Report + additionalFilter, sqlparams);

                 if (ds != null && ds.Tables.Count > 0)
                     dtToReturn = ds;
             }
             catch (Exception ex)
             {
                 SqlHelper.LogException(ex);
                 throw ex;
             }

             return dtToReturn;
         }
         */
        public DataTable GetDispatchOrderReportDataDashboard(int companyID, int periodID)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            string Query_Dispatch_Order_ReportDashboard = @" 
Select Top 5 
AR_Set_Customer.CustomerName,
SAL_DispatchHeader.TotalAmount,
SAL_DispatchHeader.DispatchNo
From SAL_DispatchHeader
	Inner JOIN AR_SET_Customer ON 
--AR_SET_Customer.CompanyID = SAL_DispatchHeader.CompanyID 
--		And 
AR_SET_Customer.PeriodID = SAL_DispatchHeader.PeriodID 
		And AR_SET_Customer.CustomerID = SAL_DispatchHeader.CustomerID 
		AND AR_SET_Customer.Status = 1
Where SAL_DispatchHeader.CompanyID = @CompanyID and SAL_DispatchHeader.PeriodID = @PeriodID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Dispatch_Order_ReportDashboard + additionalFilter, sqlparams);

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

        public static DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int dOHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_SelectAll_ForApproveDetail = @" 
SELECT DISTINCT SAL_DeliveryOrderHeader.DeliveryOrderID
	,SAL_DeliveryOrderHeader.CompanyID
	,SAL_DeliveryOrderHeader.PeriodID
	,SAL_DeliveryOrderHeader.DocumentTypeID
	,SAL_DeliveryOrderHeader.DeliveryOrderNo
	,SAL_DeliveryOrderHeader.DODate
	,SAL_DeliveryOrderHeader.DOValidityDate
	,SAL_DeliveryOrderHeader.Status
	,SAL_DeliveryOrderHeader.StatusCode
	,SET_DocumentApprove.ApprovalStatus
    ,SET_UserApprovalAmount.ApprovalOperator
    ,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel
FROM SAL_DeliveryOrderHeader
INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = SAL_DeliveryOrderHeader.CompanyID
	AND SET_DocumentApprove.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_DocumentApprove.Status =1 
AND SET_DocumentApprove.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
	AND SET_DocumentApprove.DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo 
INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	AND SET_UserApprovalAmount.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
											WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
												AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
												AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
													WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
														AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
														AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1
WHERE 
    SAL_DeliveryOrderHeader.Status = 1 AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID 
    AND SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.DeliveryOrderID = @DOHeaderID AND SAL_DeliveryOrderHeader.DocumentTypeID = @DocumentTypeID 
    AND SAL_DeliveryOrderHeader.DeliveryOrderNo = @DocumentNo ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DOHeaderID", dOHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_ForApproveDetail, sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int dOHeaderID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            #region Query

            string query_Select_DataByKey_ForApprove = @" 
SELECT DISTINCT SAL_DeliveryOrderHeader.DeliveryOrderID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID,SAL_DeliveryOrderHeader.Status,SET_UserApprovalAmount.ApprovalOperator,SET_ApprovalAuthority.UserID AS UserID
    ,SET_Authority.AuthorityLevel   
    , SET_UserApprovalAmount.ApprovalAmountID 
    , SET_UserApprovalAmount.ApprovalAuthorityID
FROM SAL_DeliveryOrderHeader
INNER JOIN Set_DocumentApprove On Set_DocumentApprove.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
		And Set_DocumentApprove.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
		And Set_DocumentApprove.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
		And Set_DocumentApprove.DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo 
		And Set_DocumentApprove.CreateDate = 
		(
			Select Max(CreateDate) From Set_DocumentApprove 
			Where CompanyID = SAL_DeliveryOrderHeader.CompanyID And PeriodID = SAL_DeliveryOrderHeader.PeriodID 
				And DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
				And DocumentNo = SAL_DeliveryOrderHeader.DeliveryOrderNo)

INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
	    AND SET_UserApprovalAmount.PeriodID = SAL_DeliveryOrderHeader.PeriodID AND SET_UserApprovalAmount.Status = 1
	    AND (SET_UserApprovalAmount.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID OR  SET_UserApprovalAmount.DocumentTypeID IS NULL)
AND (SET_UserApprovalAmount.UserID = Set_DocumentApprove.CreatedBy OR SET_UserApprovalAmount.UserID IS NULL)
	AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1
				   WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
						AND CAST(CONVERT(VARCHAR(10), GETDATE(), 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom 
						AND SET_UserApprovalAmount.EffectiveDateTo THEN 1
				   ELSE 0 END))
	AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
											WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
												AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
												AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) >= SET_UserApprovalAmount.AmountFrom THEN 1
				   WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT doh.TotalAmount FROM SAL_DeliveryOrderHeader doh 
													WHERE doh.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
														AND doh.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
														AND doh.DeliveryOrderID = SAL_DeliveryOrderHeader.DeliveryOrderID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                                                            AND SET_UserApprovalAmount.AmountTo THEN 1
				   ELSE 0 END))
INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
	AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
	AND SET_ApprovalAuthority.Status = 1
INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID 
	AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID 
	AND SET_Authority.Status = 1

    left join UsersInRoles on UsersInRoles.RoleId = SET_UserApprovalAmount.RoleId
			and UsersInRoles.CompanyID = SET_UserApprovalAmount.CompanyID

INNER JOIN aspnet_UsersInRoles On aspnet_UsersInRoles.UserID = SAL_DeliveryOrderHeader.CreatedBy 
And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null)

WHERE --SET_UserApprovalAmount.ApprovalOperator = 'AND' AND 
        SAL_DeliveryOrderHeader.Status = 1 AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID 
    AND SAL_DeliveryOrderHeader.CompanyID = @CompanyID AND SAL_DeliveryOrderHeader.DeliveryOrderID = @DOHeaderID AND SAL_DeliveryOrderHeader.DocumentTypeID = @DocumentTypeID 
    AND SAL_DeliveryOrderHeader.DeliveryOrderNo = @DocumentNo ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DOHeaderID", dOHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_Select_DataByKey_ForApprove, sqlparams);

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

        public DataTable GetDataForDeliveryOrderReportCustomerWise(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null,
            Nullable<int> customerName = null, Nullable<int> statusCode = null, Nullable<int> batchID = null, Nullable<int> itemID = null, string orderBy = "",
            Nullable<int> documentTypeID = null, short? documentGroupID = null, int CurrentFiscalID = 0, Nullable<int> BrokerID = null)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetDeliveryOrderDetail_CustomerWiseReport = @"SELECT SAL_OrderDetail.OrderID, 
         --SAL_OrderHeader.ContractNo,
           ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo,CityCust.CityName,
         
        --((SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) + ((SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) * isnull(SAL_OrderDetail.SalesTaxAmount,0) / 100)) as netAmount,

          (((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.BaseAmount) + (((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.BaseAmount) * isnull(SAL_OrderDetail.SalesTaxAmount,0) / 100)) as netAmount,

	    --SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount as GrossAmount,

          (SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.Rate as GrossAmount,

	     /*ROUND( 
	     (SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
		 *
		 (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
			   when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate 
			   else 
			   Sal_OrderDetail.BaseAmount 
			end),0) AS GrossAmount,*/

		INV_Item.ItemID,
	    INV_Item.Description As ItemDescription, 
	    SET_UnitOfMeasure.UOMName, 
	    
        --SAL_DeliveryOrderDetail.Quantity,

        SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0) as Quantity,    

         CAST((datepart(year,SET_BATCH.FromDate))AS VARCHAR(10))+'-'+CAST((datepart(year,SET_BATCH.ToDate))AS VARCHAR(10))as Batch, 
	    isnull(SAL_OrderDetail.Rate,0) AS UnitPrice
        , SAL_OrderDetail.SalesTaxAmount As TaxRate, 
        ASCT.CustomerName, 
        SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName
        , SET_Currency.CurrencySign
        , SAL_OrderDetail.ExciseDutyAmount
        , SAL_OrderDetail.FEDAmount,
        [SET_OperatingUnit].Logoimage as LogoImage,
        SAL_OrderHeader.Season,
        ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = Sal_DeliveryOrderHeader.StatusCode 
        and LookupHeaderID = 5),'') as DocumentStatus 
        ,Sal_DeliveryOrderHeader.DeliveryOrderNo,DocumentTypesGroup.GroupName
        FROM SAL_OrderDetail 
        INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
                And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1
	    INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
            And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
            And SAL_DeliveryOrderDetail.SalesOrderHeaderID  = SAL_OrderDetail.OrderID
            And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID 
            And SAL_DeliveryOrderDetail.Status = 1
            left Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID And Sal_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
                    And Sal_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = Sal_DeliveryOrderHeader.CompanyID AND SET_DocumentType.PeriodID = Sal_DeliveryOrderHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = Sal_DeliveryOrderHeader.DocumentTypeID 
         LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
	    INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID And 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
	    INNER JOIN SET_UnitOfMeasure ON SAL_OrderDetail.ItemUnitID = SET_UnitOfMeasure.UOMID 
	    INNER JOIN SET_Company ON SET_Company.CompanyId = SAL_OrderDetail.CompanyID 
	    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
	    INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
	    INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
	    LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
	    LEFT JOIN AR_SET_Customer ASCT ON --ASCT.CompanyID = SAL_OrderHeader.CompanyID And 
			      ASCT.PeriodID = SAL_OrderHeader.PeriodID 
              And ASCT.CustomerID = SAL_OrderHeader.CustomerID 
        LEFT JOIN SET_City AS CityCust ON CityCust.CityID = ASCT.CityID 
	    LEFT JOIN SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID        
	    LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ORDERHEADER.SEASON
				    --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID
				    AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

Left JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
AND SAL_ContractHeader.Status = 1

{0} 
{1} ";

            #endregion

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Order By Clause

                string orderClause = string.Empty;

                switch (orderBy)
                {
                    case "SONumber":
                        orderClause = " Order By Cast(SAL_OrderHeader.ContractNo As int) ";
                        break;

                    case "DONumber":
                        orderClause = " Order By Cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "CustomerName":
                        orderClause = " Order By ASCT.CustomerName ";
                        break;
                }

                #endregion

                sb.Append(" WHERE SAL_OrderDetail.CompanyID = " + companyID + " and SAL_OrderDetail.PeriodID =  " + periodID + " And SAL_OrderDetail.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerName > 0)
                    sb.Append(" And ASCT.CustomerID = " + customerName);

                if (statusCode > 0)
                    sb.Append(" And Sal_DeliveryOrderHeader.StatusCode = " + statusCode + "");

                if (batchID > 0)
                    sb.Append(" And SAL_OrderHeader.Season = '" + batchID + "'");

                if (itemID > 0)
                    sb.Append(" And INV_Item.ItemID = " + itemID);

                if(documentTypeID.HasValue)
                    sb.Append(" And Set_DocumentType.DocumentTypeID = " + documentTypeID.Value);


                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                if (BrokerID.HasValue)
                {
                    sb.Append(" AND ASCT.BrokerID = " + BrokerID);
                }

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDeliveryOrderDetail_CustomerWiseReport, sb.ToString(), orderClause),sqlparams);

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

        public DataTable GetDataForDeliveryOrderRegister(int companyID, int periodID, int ContractNoFrom, int ContractNoTo, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> customerIDFrom = null, Nullable<int> customerIDTo = null, Nullable<int> DOfrom = null,
            Nullable<int> DOTo = null, string orderBy = "", int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_GetDeliveryOrderRegisterReport = @" 
    SELECT ASCT.CustomerID, SAL_OrderDetail.OrderID, ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) AS ContractNo
    , sal_orderdetail.Rate as RetailPrice, CityCust.CityName,
    
    /* (SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) + ((SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) 
        * isnull(SAL_OrderDetail.SalesTaxAmount, 0) / 100)  as netAmount,
    */
    
    --reversed DO implemented

    ((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.BaseAmount) 
    + 
    (
	 ((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.BaseAmount) 
     * 
     isnull(SAL_OrderDetail.SalesTaxAmount, 0) / 100
     )  as netAmount, 
 
    --SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount as GrossAmount, 

    (SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * sal_orderdetail.BaseAmount as GrossAmount, 


    SAL_OrderHeader.ContractDate, Sal_DeliveryOrderHeader.DeliveryOrderNo, 
    
    INV_Item.ItemID, INV_Item.Description As ItemDescription, SET_UnitOfMeasure.UOMName, 

    --SAL_DeliveryOrderDetail.Quantity,
     DocumentTypesGroup.GroupName,

    SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0) as Quantity,

    CAST((datepart(year, SET_BATCH.FromDate)) AS VARCHAR(10)) + '-' + CAST((datepart(year, SET_BATCH.ToDate)) AS VARCHAR(10)) as Batch, 
    SAL_OrderDetail.Rate AS UnitPrice, isnull(SAL_OrderDetail.SalesTaxAmount, 0) As TaxRate, ASCT.CustomerName, 
    SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_Currency.CurrencySign, SAL_OrderDetail.ExciseDutyAmount, 
    isnull(SAL_OrderDetail.FEDAmount, 0) as FEDAmount, SET_OperatingUnit.Logoimage, SAL_OrderHeader.Season, 
    ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_OrderHeader.StatusCode and LookupHeaderID = 5), '') 
        as DocumentStatus 
        ,case Sal_DeliveryOrderHeader.StatusCode when 1 then 'Approved' when 5 then 'In-Process' else '' end as Status
FROM SAL_OrderDetail 
    INNER JOIN SAL_OrderHeader ON SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID And SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID 
        And SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID And SAL_OrderHeader.Status = 1 
    INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
        And SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID And SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
        And SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID And SAL_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
        And Sal_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID And Sal_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_OrderHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_OrderHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = SAL_OrderHeader.SaleContractTypeID 
   LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And   
    INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID And 
INV_Item.ItemID = SAL_OrderDetail.ItemID 
    INNER JOIN SET_UnitOfMeasure ON SAL_OrderDetail.ItemUnitID = SET_UnitOfMeasure.UOMID 
    INNER JOIN SET_Company ON SET_Company.CompanyId = SAL_OrderDetail.CompanyID 
    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
    INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
    LEFT JOIN SET_Term ON SET_Term.TermID = SAL_OrderHeader.PaymentTermID 
    LEFT JOIN AR_SET_Customer ASCT ON --ASCT.CompanyID = SAL_OrderHeader.CompanyID And 
        ASCT.PeriodID = SAL_OrderHeader.PeriodID 
        And ASCT.CustomerID = SAL_OrderHeader.CustomerID 
    LEFT JOIN SET_City AS CityCust ON CityCust.CityID = ASCT.CityID 
    LEFT JOIN SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ORDERHEADER.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ORDERHEADER.PeriodID 
inner join Set_PeriodCustom on SAL_OrderHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
Left JOIN SAL_ContractHeader ON SAL_ContractHeader.ContractID = SAL_OrderDetail.ContractHeaderID
	AND SAL_ContractHeader.CompanyID = SAL_OrderDetail.CompanyID
	AND SAL_ContractHeader.PeriodID = SAL_OrderDetail.PeriodID
	AND SAL_ContractHeader.Status = 1

{0}
{1} ";

       
            #endregion

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Order By Clause

                string orderClause = string.Empty;

                switch (orderBy)
                {
                    case "ContractDate":
                        orderClause = " Order By SAL_OrderHeader.ContractDate ";
                        break;

                    case "SONumber":
                        orderClause = " Order By Cast(SAL_OrderHeader.ContractNo As int) , Cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "DONumber":
                        orderClause = " Order By cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "CustomerName":
                        orderClause = " Order By ASCT.CustomerName ";
                        break;
                }

                #endregion

                sb.Append(" WHERE SAL_OrderDetail.CompanyID = " + companyID + " and SAL_OrderDetail.PeriodID =  " + periodID + " And SAL_OrderDetail.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerIDFrom.HasValue && customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID Between " + customerIDFrom.Value + " And " + customerIDTo.Value);
                else if (customerIDFrom.HasValue)
                    sb.Append(" And ASCT.CustomerID >= " + customerIDFrom.Value);
                if (customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID <= " + customerIDTo.Value);

                if (ContractNoFrom > 0 && ContractNoTo > 0)
                    sb.Append(" and ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) between " + ContractNoFrom + " and " + ContractNoTo);

                if (DOfrom != null && DOTo != null)
                    sb.Append("and  SAL_DeliveryOrderHeader.DeliveryOrderNo between " + DOfrom + " and " + DOTo + " ");


                if (DocumentTypeID > 0)
                    sb.Append("and SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDeliveryOrderRegisterReport, sb.ToString(), orderClause), sqlparams);

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

        public DataTable GetDataForDeliveryOrderRegisterNew(int companyID, int periodID, int ContractNoFrom, int ContractNoTo, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> customerIDFrom = null, Nullable<int> customerIDTo = null, Nullable<int> DOfrom = null,
            Nullable<int> DOTo = null, string orderBy = "", int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Order By Clause

                string orderClause = string.Empty;

                switch (orderBy)
                {
                    case "ContractDate":
                        orderClause = " Order By SAL_OrderHeader.ContractDate ";
                        break;

                    case "SONumber":
                        orderClause = " Order By Cast(SAL_OrderHeader.ContractNo As int) , Cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "DONumber":
                        orderClause = " Order By cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "CustomerName":
                        orderClause = " Order By ASCT.CustomerName ";
                        break;
                }

                #endregion

                sb.Append(" WHERE SAL_OrderDetail.CompanyID = " + companyID + " and SAL_OrderDetail.PeriodID =  " + periodID + " And SAL_OrderDetail.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_OrderHeader.ContractDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerIDFrom.HasValue && customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID Between " + customerIDFrom.Value + " And " + customerIDTo.Value);
                else if (customerIDFrom.HasValue)
                    sb.Append(" And ASCT.CustomerID >= " + customerIDFrom.Value);
                if (customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID <= " + customerIDTo.Value);

                if (ContractNoFrom > 0 && ContractNoTo > 0)
                    sb.Append(" and ISNULL(SAL_ContractHeader.ContractNo,SAL_OrderHeader.ContractNo) between " + ContractNoFrom + " and " + ContractNoTo);

                if (DOfrom != null && DOTo != null)
                    sb.Append("and  SAL_DeliveryOrderHeader.DeliveryOrderNo between " + DOfrom + " and " + DOTo + " ");


                if (DocumentTypeID > 0)
                    sb.Append("and SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_GetDeliveryOrderRegisterReportNew, sb.ToString(), orderClause), sqlparams);

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
      
        public DataTable GetDataForDeliveryOrderRegisterSheikhoo(int companyID, int periodID, int ContractNoFrom, int ContractNoTo, Nullable<DateTime> dateFrom = null,
            Nullable<DateTime> dateTo = null, Nullable<int> customerIDFrom = null, Nullable<int> customerIDTo = null, Nullable<int> DOfrom = null,
            Nullable<int> DOTo = null, string orderBy = "", int DocumentTypeID = 0, short? documentGroupID = null, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;

            #region Query

          string  Query_GetDeliveryOrderRegisterReport = @" 
    SELECT ASCT.CustomerID, SAL_ContractDetail.ContractID as OrderID, SAL_ContractHeader.ContractNo, SAL_ContractDetail.Rate as RetailPrice, 
    
    /* (SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) + ((SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount) 
        * isnull(SAL_OrderDetail.SalesTaxAmount, 0) / 100)  as netAmount,
    */
    
    --reversed DO implemented

    ((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * SAL_ContractDetail.BaseAmount) 
    + 
    (
	 ((SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * SAL_ContractDetail.BaseAmount) 
     * 
     isnull(SAL_ContractDetail.SalesTaxAmount, 0) / 100
     )  as netAmount, 
 
    --SAL_DeliveryOrderDetail.Quantity * sal_orderdetail.BaseAmount as GrossAmount, 

    (SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0)) * SAL_ContractDetail.BaseAmount as GrossAmount, 


    SAL_ContractHeader.ContractDate, Sal_DeliveryOrderHeader.DeliveryOrderNo, 
    
    INV_Item.ItemID, INV_Item.Description As ItemDescription, SET_UnitOfMeasure.UOMName, 

    --SAL_DeliveryOrderDetail.Quantity,
     DocumentTypesGroup.GroupName,

    SAL_DeliveryOrderDetail.Quantity -isnull(SAL_DeliveryOrderDetail.reversedqty,0) as Quantity,

    CAST((datepart(year, SET_BATCH.FromDate)) AS VARCHAR(10)) + '-' + CAST((datepart(year, SET_BATCH.ToDate)) AS VARCHAR(10)) as Batch, 
    SAL_ContractDetail.Rate AS UnitPrice, isnull(SAL_ContractDetail.SalesTaxAmount, 0) As TaxRate, ASCT.CustomerName, 
    SET_Business.BusinessName + ' - ' + SET_OperatingUnit.OperatingUnitName AS CompanyName, SET_Currency.CurrencySign, SAL_ContractDetail.ExciseDutyAmount, 
    isnull(SAL_ContractDetail.FEDAmount, 0) as FEDAmount, SET_OperatingUnit.Logoimage, SAL_ContractHeader.Season, 
    ISNULL((select LookupDetail.EnumeratedDescription from LookupDetail where LookUpDetailID = SAL_ContractHeader.StatusCode and LookupHeaderID = 5), '') 
        as DocumentStatus 
        ,case Sal_DeliveryOrderHeader.StatusCode when 1 then 'Approved' when 5 then 'In-Process' else '' end as Status
FROM SAL_ContractDetail 
    INNER JOIN SAL_ContractHeader ON SAL_ContractHeader.CompanyID = SAL_ContractDetail.CompanyID And SAL_ContractHeader.PeriodID = SAL_ContractDetail.PeriodID 
        And SAL_ContractHeader.ContractID = SAL_ContractDetail.ContractID And SAL_ContractHeader.Status = 1 
    INNER JOIN SAL_DeliveryOrderDetail ON SAL_DeliveryOrderDetail.CompanyID = SAL_ContractDetail.CompanyID 
        And SAL_DeliveryOrderDetail.PeriodID = SAL_ContractDetail.PeriodID 
        And SAL_DeliveryOrderDetail.ContractHeaderID = SAL_ContractDetail.ContractID 
        And SAL_DeliveryOrderDetail.ContractDetailID = SAL_ContractDetail.ContractDetailID 
        And SAL_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_DeliveryOrderHeader On Sal_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
        And Sal_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID And Sal_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = SAL_ContractHeader.CompanyID AND SET_DocumentType.PeriodID = SAL_ContractHeader.PeriodID 
        AND SET_DocumentType.DocumentTypeID = SAL_ContractHeader.SaleContractTypeID 
   LEFT Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And   
    INNER JOIN INV_Item ON 
--INV_Item.CompanyID = SAL_OrderDetail.CompanyID And 
INV_Item.ItemID = SAL_ContractDetail.ItemID 
    INNER JOIN SET_UnitOfMeasure ON SAL_ContractDetail.ItemUnitID = SET_UnitOfMeasure.UOMID 
    INNER JOIN SET_Company ON SET_Company.CompanyId = SAL_ContractDetail.CompanyID 
    INNER JOIN SET_OperatingUnit ON SET_OperatingUnit.OperatingUnitId = SET_Company.OperatingUnitId 
    INNER JOIN SET_Business ON SET_Business.BusinessId = SET_Company.BusinessId 
    INNER JOIN SET_Currency ON SET_Currency.CurrencyID = SET_OperatingUnit.BaseCurrencyID 
    LEFT JOIN SET_Term ON SET_Term.TermID = SAL_ContractHeader.PaymentTermID 
    LEFT JOIN AR_SET_Customer ASCT ON ASCT.CompanyID = SAL_ContractHeader.CompanyID And ASCT.PeriodID = SAL_ContractHeader.PeriodID 
        And ASCT.CustomerID = SAL_ContractHeader.CustomerID 
    LEFT JOIN SET_City AS CityCust ON CityCust.CityID = ASCT.CityID 
    LEFT JOIN SET_City AS CityComp ON CityComp.CityID = SET_OperatingUnit.CityID 
    LEFT JOIN SET_BATCH ON CAST(SET_BATCH.BATCHID as Varchar(10)) = SAL_ContractHeader.SEASON --AND SET_BATCH.COMPANYID = SAL_ORDERHEADER.COMPANYID 
        AND SET_BATCH.PeriodID = SAL_ContractHeader.PeriodID 
inner join Set_PeriodCustom on SAL_ContractHeader.ContractDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID

{0}
{1} ";

            #endregion

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                #region Order By Clause

                string orderClause = string.Empty;

                switch (orderBy)
                {
                    case "ContractDate":
                        orderClause = " Order By SAL_ContractHeader.ContractDate ";
                        break;

                    case "SONumber":
                        orderClause = " Order By Cast(SAL_ContractHeader.ContractNo As int) , Cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "DONumber":
                        orderClause = " Order By cast(Sal_DeliveryOrderHeader.DeliveryOrderNo As int) ";
                        break;

                    case "CustomerName":
                        orderClause = " Order By ASCT.CustomerName ";
                        break;
                }

                #endregion

                sb.Append(" WHERE SAL_ContractDetail.CompanyID = " + companyID + " and SAL_ContractDetail.PeriodID =  " + periodID + " And SAL_ContractDetail.Status = 1");

                #region Approval Date filter

                if (dateFrom != null && dateTo != null)
                    sb.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) between '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "' and '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");
                else if (dateFrom != null)
                    sb.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) >= '" + ((DateTime)dateFrom).ToString("yyyy-MM-dd") + "'");
                else if (dateTo != null)
                    sb.Append(" and Cast(SAL_ContractHeader.ContractDate as Date) <= '" + ((DateTime)dateTo).ToString("yyyy-MM-dd") + "'");

                #endregion

                if (customerIDFrom.HasValue && customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID Between " + customerIDFrom.Value + " And " + customerIDTo.Value);
                else if (customerIDFrom.HasValue)
                    sb.Append(" And ASCT.CustomerID >= " + customerIDFrom.Value);
                if (customerIDTo.HasValue)
                    sb.Append(" And ASCT.CustomerID <= " + customerIDTo.Value);

                if (ContractNoFrom > 0 && ContractNoTo > 0)
                    sb.Append(" and SAL_ContractHeader.ContractNo between " + ContractNoFrom + " and " + ContractNoTo);

                if (DOfrom != null && DOTo != null)
                    sb.Append("and  SAL_DeliveryOrderHeader.DeliveryOrderNo between " + DOfrom + " and " + DOTo + " ");


                if (DocumentTypeID > 0)
                    sb.Append("and SAL_DeliveryOrderHeader.DocumentTypeID = " + DocumentTypeID + "");

                if (documentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetDeliveryOrderRegisterReport, sb.ToString(), orderClause), sqlparams);

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

        public static DataTable GetAllDataWithSalesOrder(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc,int CurrentFiscalID=0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAllWithSalesOrder_WithPaging = @" 
Select DeliveryOrders.*, ROW_NUMBER() Over (Order by Cast(DeliveryOrders.DeliveryOrderNo as int) desc) as 'Row' 
From 
(
    SELECT [DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], AR_SET_Customer.Customername, SAL_DeliveryOrderHeader.[PeriodID], [DocumentTypeID], 
        [DeliveryOrderNo], [DODate], [DOValidityDate], SAL_DeliveryOrderHeader.[CustomerID], SAL_DeliveryOrderHeader.[IsCancelled], 
        SAL_DeliveryOrderHeader.[CancellationDate], SAL_DeliveryOrderHeader.[CancelledBy], SAL_DeliveryOrderHeader.[IsApproved], SAL_DeliveryOrderHeader.[ApprovedBy], 
        SAL_DeliveryOrderHeader.[ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], 
        SAL_DeliveryOrderHeader.[IsDispatchGenerated], SAL_DeliveryOrderHeader.[IsPaid], [TotalPaidAmount], SAL_DeliveryOrderHeader.[CreatedBy], 
        SAL_DeliveryOrderHeader.[CreateDate], SAL_DeliveryOrderHeader.[UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], 
        SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], SAL_DeliveryOrderHeader.[StatusCode], SAL_DeliveryOrderHeader.[TotalAmount], 
        Set_Batch.Description as Batch , sum(Sal_DeliveryOrderDetail.Quantity) as Quantity
,DeliveryOrderNo as DocumentNo , DODate as DocumentDate , Set_PeriodCustom.PeriodSetName
    FROM SAL_DeliveryOrderHeader 
        Inner join AR_SET_Customer on --AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND 
AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
            AND AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID 
        Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
            And Sal_DeliveryOrderDetail.Status = 1 
        Inner Join Sal_OrderHeader On Sal_OrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
            And Sal_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
        Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
            And Set_Batch.BatchID = Sal_OrderHeader.Season 
Left join Set_PeriodCustom on  
 SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1   
    {WhereFilter}
    group by
		[DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], AR_SET_Customer.Customername, SAL_DeliveryOrderHeader.[PeriodID], [DocumentTypeID], 
        [DeliveryOrderNo], [DODate], [DOValidityDate], SAL_DeliveryOrderHeader.[CustomerID], SAL_DeliveryOrderHeader.[IsCancelled], 
        SAL_DeliveryOrderHeader.[CancellationDate], SAL_DeliveryOrderHeader.[CancelledBy], SAL_DeliveryOrderHeader.[IsApproved], SAL_DeliveryOrderHeader.[ApprovedBy], 
        SAL_DeliveryOrderHeader.[ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], 
        SAL_DeliveryOrderHeader.[IsDispatchGenerated], SAL_DeliveryOrderHeader.[IsPaid], [TotalPaidAmount], SAL_DeliveryOrderHeader.[CreatedBy], 
        SAL_DeliveryOrderHeader.[CreateDate], SAL_DeliveryOrderHeader.[UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], 
        SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], SAL_DeliveryOrderHeader.[StatusCode], SAL_DeliveryOrderHeader.[TotalAmount], 
        Set_Batch.[Description],Set_PeriodCustom.PeriodSetName
) as DeliveryOrders ";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_SelectAllWithSalesOrder_WithPaging.Replace("{WhereFilter}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAllWithSalesOrder_WithoutPaging = @" 
SELECT Distinct [DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], AR_SET_Customer.Customername, SAL_DeliveryOrderHeader.[PeriodID], [DocumentTypeID], 
    [DeliveryOrderNo], [DODate], [DOValidityDate], SAL_DeliveryOrderHeader.[CustomerID], SAL_DeliveryOrderHeader.[IsCancelled], 
    SAL_DeliveryOrderHeader.[CancellationDate], SAL_DeliveryOrderHeader.[CancelledBy], SAL_DeliveryOrderHeader.[IsApproved], SAL_DeliveryOrderHeader.[ApprovedBy], 
    SAL_DeliveryOrderHeader.[ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], 
    SAL_DeliveryOrderHeader.[IsDispatchGenerated], SAL_DeliveryOrderHeader.[IsPaid], SAL_DeliveryOrderHeader.[TotalPaidAmount], SAL_DeliveryOrderHeader.[CreatedBy], 
    SAL_DeliveryOrderHeader.[CreateDate], SAL_DeliveryOrderHeader.[UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], 
    SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], SAL_DeliveryOrderHeader.[StatusCode], SAL_DeliveryOrderHeader.[TotalAmount], 
    Set_Batch.Description as Batch 
,DeliveryOrderNo as DocumentNo , DODate as DocumentDate , Set_PeriodCustom.PeriodSetName
FROM [SAL_DeliveryOrderHeader]
    Inner join AR_SET_Customer on 
--AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND 
AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        AND AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID 
    Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        And Sal_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_OrderHeader On Sal_OrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        And Sal_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
    Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
        And Set_Batch.BatchID = Sal_OrderHeader.Season 
Left join Set_PeriodCustom on  
 SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1

{WhereFilter} 
Order By Cast(DeliveryOrders.DeliveryOrderNo as int) desc ";

                    #endregion

                    string query = Query_SelectAllWithSalesOrder_WithoutPaging.Replace("{WhereFilter}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataForGateIn(Nullable<int> companyID, Nullable<int> periodID,int fiscalid, int DONo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @"Select AR_SET_Customer.CustomerID,
 CustomerName + ' * ' +set_city.CityName + '*'+ case isnull(AR_SET_Customer.IsRegistered,0) when 1 then 'Registered' else 'Un-Registered' end + '*'+ convert(varchar,AR_SET_Customer.CustomerID)  as CustomerName ,
SAL_DeliveryOrderHeader.DODate,
ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar,SAL_OrderDetail.ItemID)
                FROM SAL_OrderDetail 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as ItemID,
		
		ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar,INV_Item.Description)
                FROM SAL_OrderDetail 
				inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
				and INV_Item.Status = 1 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as Description,
		
--INV_Item.ItemID,INV_Item.Description
SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName
,SUM(SAL_DeliveryOrderDetail.Quantity) as Quantity

from SAL_DeliveryOrderHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SET_DocumentType.Status = 1
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SAL_DeliveryOrderDetail.Status = 1
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
and SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
and SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
and SAL_OrderDetail.Status = 1
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
and SAL_OrderHeader.Status = 1


inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
and INV_Item.Status = 1 
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
and SET_UnitofMeasure.Status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
and AR_SET_Customer.Status = 1
inner join set_city on set_city.cityID = AR_SET_Customer.cityID
where SAL_DeliveryOrderHeader.Status = 1 
and SAL_DeliveryOrderHeader.StatusCode = 1 
and SAL_DeliveryOrderHeader.DeliveryOrderNo = @DeliveryOrderNo
and SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
and SAL_DeliveryOrderHeader.FiscalID = @FiscalID

 ";

            if (type != "")
            {
                sb.Append(" and  DocumentType like '%" + type + "%' ");
            }
            sb.Append(" group by AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DODate ,SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName, SAL_OrderHeader.OrderID,SAL_OrderHeader.CompanyID,SAL_OrderHeader.PeriodID,set_city.CityName,AR_SET_Customer.IsRegistered");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DONo);
                sqlparams[3] = new SqlParameter("@FiscalID", fiscalid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static DataTable GetAllDataForGateInBatchWise(Nullable<int> companyID, Nullable<int> periodID, int batchid, int DONo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            string Query = FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_GetAllDataForGateInBatchWise;

            if (type != "")
            {
                sb.Append(" and  DocumentType like '%" + type + "' ");
            }
            sb.Append(" group by AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DODate ,SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName, SAL_OrderHeader.OrderID,SAL_OrderHeader.CompanyID,SAL_OrderHeader.PeriodID,set_city.CityName,AR_SET_Customer.IsRegistered,Set_Batch.BatchID,Set_Batch.Description");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DONo);
                sqlparams[3] = new SqlParameter("@BatchID", batchid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static DataTable GetAllDataForGateInBatchWiseForMultiDO(Nullable<int> companyID, Nullable<int> periodID, int batchid, int DONo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            string Query = FnI.QueryResourceLibrary.Sales.SAL_DeliveryOrderHeader.Query_GetAllDataForGateInBatchWiseForMultiDO;

            if (type != "")
            {
                sb.Append(" and  DocumentType like '%" + type + "' ");
            }
            sb.Append(" group by AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DODate ,SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName, SAL_OrderHeader.OrderID,SAL_OrderHeader.CompanyID,SAL_OrderHeader.PeriodID,set_city.CityName,AR_SET_Customer.IsRegistered,Set_Batch.BatchID,Set_Batch.Description,SAL_DeliveryOrderHeader.DeliveryOrderID, SAL_DeliveryOrderHeader.DeliveryOrderNo");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DONo);
                sqlparams[3] = new SqlParameter("@BatchID", batchid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static DataTable GetAllDataWithSalesOrderForRevert(string Query_WithPagging, string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
          Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAllWithSalesOrder_WithPaging = Query_WithPagging;

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;
                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    string query = Query_SelectAllWithSalesOrder_WithPaging.Replace("{WhereFilter}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    #region Query

                    string Query_SelectAllWithSalesOrder_WithoutPaging = Query_WithPagging;

                    #endregion

                    string query = Query_SelectAllWithSalesOrder_WithoutPaging.Replace("{WhereFilter}", strWhere);
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, query + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable CheckVehicleAleadyExist(int companyID, int periodID, int VehicleID, bool IsDeliveryOrder = true)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@VehicleID", VehicleID);

                if (IsDeliveryOrder)
                {
                    string Query_CheckVehicleExist = @" select * from SAL_DOVehicleInformation 
where CompanyID = @CompanyID and PeriodID = @PeriodID and VehicleID = @VehicleID ";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckVehicleExist, sqlparams).Tables[0];
                }
                else
                {

                    string Query_CheckVehicleExistForDispatch = @" select * from SAL_DispatchVehicleInformation 
where CompanyID = @CompanyID and PeriodID = @PeriodID and VehicleInformationID = @VehicleID and DispatchID is not null ";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_CheckVehicleExistForDispatch, sqlparams).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public bool UpdateDataByDDLValues(int Companyid, int perioid, int DeliveryOrderNo, string Query, int StatusCode, int DocumentTypeID, int FiscalID)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DeliveryOrderNo);
                sqlparams[3] = new SqlParameter("@StatusCode", StatusCode);
                sqlparams[4] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[5] = new SqlParameter("@FiscalID", FiscalID);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query, sqlparams) > 0)
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

        public DataSet Get_OtherDeliveryOrdersAndActualSalesOrder(int companyID, int periodID, int baseDO_HeaderID, int filterDO_HeaderID)
        {
            DataSet dsToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@BaseDO_HeaderID", baseDO_HeaderID);
                sqlparams[3] = new SqlParameter("@FilterDO_HeaderID", filterDO_HeaderID);

                string query = @" Select * from SAL_DeliveryOrderHeader 
    Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        And Sal_DeliveryOrderDetail.Status = 1 
where SAL_DeliveryOrderHeader.CompanyID = @CompanyID And SAL_DeliveryOrderHeader.PeriodID = @PeriodID And SAL_DeliveryOrderHeader.BaseID = @BaseDO_HeaderID 
    And SAL_DeliveryOrderHeader.DeliveryOrderID <> @FilterDO_HeaderID And SAL_DeliveryOrderHeader.Status = 1 

Select Sal_OrderDetail.* From SAL_DeliveryOrderHeader 
    Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
        And Sal_DeliveryOrderDetail.Status = 1 
    Inner Join Sal_OrderDetail On Sal_OrderDetail.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderDetail.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
        And Sal_OrderDetail.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID And Sal_OrderDetail.OrderDetailID = Sal_DeliveryOrderDetail.SalesOrderDetailID 
        And Sal_OrderDetail.Status = 1 
Where SAL_DeliveryOrderHeader.CompanyID = @CompanyID And SAL_DeliveryOrderHeader.PeriodID = @PeriodID 
    And SAL_DeliveryOrderHeader.DeliveryOrderID = @FilterDO_HeaderID And SAL_DeliveryOrderHeader.Status = 1 ";

                dsToReturn = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dsToReturn;
        }
        public static DataTable Get_AsOffPendingDeliveryOrder(bool IsSheikhoReport, int companyID, int periodID, Nullable<DateTime> fromDate = null,
   Nullable<DateTime> toDate = null, Nullable<int> CustomerFromID = null, Nullable<int> CustomerToID = null, Nullable<int> documentGroupID = null, Nullable<int> documentType = null, bool? IsPending = false, Nullable<int> BatchID = null, Nullable<int> ContractID = null, Nullable<int> DoNum = null, Nullable<int> ItemID = null, Nullable<int> BrokerID = null, Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dt = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder addParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                #region Query
//                const string Query_GetPendingDOdata = @" select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo,Tab.DeliveryOrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
//    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, sum(TAB.Balance) as Balance, Tab.DocumentStatus,Sum(Tab.Del_Quantity) as Del_Quantity 
//from 
//(
//    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_OrderDetail.Rate, 
//
// SAL_DeliveryOrderHeader.DeliveryOrderNo,
//        DocumentTypesGroup.GroupName,
//
//        ROUND( (SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
//        *
//        (case when SAL_OrderDetail.Inclusive = 0 then SAL_OrderDetail.Rate 
//		when (Sal_OrderDetail.BaseAmount is null or Sal_OrderDetail.BaseAmount = 0) then Sal_OrderDetail.Rate else Sal_OrderDetail.BaseAmount end),0) AS GrossAmount,
//
//        AR_SET_Customer.CustomerName, 
//        AR_SET_Customer.CustomerID, SAL_OrderHeader.ContractNo as Sal_OrderNo, SAL_OrderHeader.ContractDate, 
//            isnull(SAL_DeliveryOrderDetail.Quantity,0) as Del_Quantity, 
//        --SAL_OrderDetail.Quantity as Sal_OrderQuantity, 
//        SAL_OrderDetail.Quantity - isnull(SAL_OrderDetail.ReversedQuantity,0) as Sal_OrderQuantity,
//
//        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
//        isnull(SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0) as Balance, 
//        
//        ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) DispatchQty, 
//        Case SAL_OrderHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
//            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
//    from SAL_DeliveryOrderHeader 
//       inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID= SAL_DeliveryOrderHeader.DeliveryOrderID and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
//            and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID and SAL_DeliveryOrderDetail.Status = 1 and SAL_DeliveryOrderHeader.Status = 1 
//       
//        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
//         --and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
//            and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
//             INNER join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
//             AND SAL_OrderDetail.OrderDetailID=SAL_DeliveryOrderDetail.SalesOrderDetailID
//            and SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID and SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
//            and SAL_OrderDetail.Status = 1 
//        INNER join SAL_OrderHeader on SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderHeader.OrderID 
//            and SAL_DeliveryOrderDetail.CompanyID = SAL_OrderHeader.CompanyID and SAL_DeliveryOrderDetail.PeriodID = SAL_OrderHeader.PeriodID 
//       
//          left join INV_Item on INV_Item.ItemID = SAL_OrderDetail.ItemID 
//--and INV_Item.CompanyID = SAL_OrderDetail.CompanyID 
//and INV_Item.Status = 1 
//        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_OrderDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
//        
//        left join INV_StockTransactionDetail on 
//           INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
//          and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
//          and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
//           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
//           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
//           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
//        left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
//            and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
//            and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
//
//inner join set_documenttype on set_documenttype.documenttypeid =SAL_OrderHeader.SaleContractTypeID
//         and set_documenttype.CompanyID=SAL_OrderHeader.CompanyID
//         and set_documenttype.PeriodID=SAL_OrderHeader.PeriodID
//Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
//--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
//    
//    where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID and SAL_DeliveryOrderHeader.Status = 1 
//     {0} 
//    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_OrderDetail.Rate, SAL_OrderDetail.GrossAmount, AR_SET_Customer.CustomerName, 
//        AR_SET_Customer.CustomerID, SAL_OrderHeader.ContractNo, SAL_OrderHeader.ContractDate, SAL_OrderDetail.Quantity, 
//        INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo, SAL_OrderDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
//        SAL_OrderHeader.StatusCode
//        ,SAL_DeliveryOrderDetail.ReversedQty
//        ,SAL_OrderDetail.ReversedQuantity
//        ,SAL_OrderDetail.Inclusive
//        ,DocumentTypesGroup.GroupName,SAL_DeliveryOrderDetail.Quantity
//) TAB 
//{1}
//group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
//    TAB.DODate, Tab.DocumentStatus,TAB.GroupName,Tab.DeliveryOrderNo ";
                #endregion

                #region newquery
                const string Query_GetPendingDOdata_WithLoadingPointQty = @"
  select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo,Tab.DeliveryOrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, (sum(TAB.Del_Quantity) -sum(TAB.DispatchQty)) + sum(TAB.DispatchReturnQty) as Balance, Tab.DocumentStatus,Sum(Tab.Del_Quantity) as Del_Quantity 
		,ISNULL(( 
	select sum(ABS(INV_LoadingPointDetail.TransactionQuantity)) from INV_LoadingPointDetail where
				INV_LoadingPointDetail.DODetailID = TAB.DODetailID
				and INV_LoadingPointDetail.DOHeaderID = TAB.DOHeaderID
				and INV_LoadingPointDetail.CompanyID = @CompanyID
				and INV_LoadingPointDetail.PeriodID = @PeriodID
				and INV_LoadingPointDetail.Status = 1
				
				),0) as LoadingPointQty

	,sum(TAB.DispatchReturnQty) as DispatchReturnQty

from 
(
    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, 

 SAL_DeliveryOrderHeader.DeliveryOrderNo,
        DocumentTypesGroup.GroupName,

        ROUND((SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
        *
        (case when SAL_ContractDetail.Inclusive = 0 then SAL_ContractDetail.Rate 
		when (SAL_ContractDetail.BaseAmount is null or SAL_ContractDetail.BaseAmount = 0) then SAL_ContractDetail.Rate else SAL_ContractDetail.BaseAmount end),0) AS GrossAmount,

        AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo as Sal_OrderNo, SAL_ContractHeader.ContractDate, 
            isnull(SAL_DeliveryOrderDetail.Quantity,0) as Del_Quantity, 
        --SAL_ContractDetail.Quantity as Sal_OrderQuantity, 
        SAL_ContractDetail.Quantity - isnull(SAL_ContractDetail.ReversedQuantity,0) as Sal_OrderQuantity,

        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
        --isnull(SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0)
		 0 as Balance, 
        
        --ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) 
		
        Case SAL_ContractHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
			,ISNULL((Select SUM(ABS(TransactionQUantity)) from INV_StockTransactionDetail where
			     INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
          and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
          and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchQty

			,ISNULL((Select SUM(ABS(SAL_ReverseDetail.ReversedQuantity)) from INV_StockTransactionDetail
		   inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
		   and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
		   and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
		   and INV_StockTransactionHeader.FiscalID = INV_StockTransactionDetail.FiscalID
		   and INV_StockTransactionHeader.Status = 1

		   inner join SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
		   and SAL_ReverseDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
		   and SAL_ReverseDetail.SourceCode = INV_StockTransactionHeader.DocumentNo
		   and SAL_ReverseDetail.SourceDetailID = INV_StockTransactionDetail.TransactionDetailID
		   and SAL_ReverseDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		   and SAL_ReverseDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		   and SAL_ReverseDetail.FiscalID = INV_StockTransactionHeader.FiscalID 
		   and SAL_ReverseDetail.Status = 1 
		   where
		   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
           and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
           and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchReturnQty

			,SAL_DeliveryOrderDetail.DODetailID,SAL_DeliveryOrderDetail.DOHeaderID
    from SAL_DeliveryOrderHeader 
       inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID= SAL_DeliveryOrderHeader.DeliveryOrderID 
	   and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			and SAL_DeliveryOrderDetail.Status = 1 and SAL_DeliveryOrderHeader.Status = 1 
       
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
         --and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
            
			 INNER join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
             AND SAL_ContractDetail.ContractDetailID =SAL_DeliveryOrderDetail.ContractDetailID
            and SAL_ContractDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            and SAL_ContractDetail.Status = 1 
      
	    INNER join SAL_ContractHeader on SAL_ContractHeader.ContractID= SAL_ContractDetail.ContractID
            and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID 
       
          left join INV_Item on INV_Item.ItemID = SAL_ContractDetail.ItemID 
--and INV_Item.CompanyID = SAL_ContractDetail.CompanyID 
and INV_Item.Status = 1 
        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_ContractDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
        
        --left join INV_StockTransactionDetail on 
        --   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
        --  and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
        --  and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
        --   and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
        --   and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        --   and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        --left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        --    and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        --    and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

inner join set_documenttype on set_documenttype.documenttypeid =SAL_ContractHeader.SaleContractTypeID
         and set_documenttype.CompanyID=SAL_ContractHeader.CompanyID
         and set_documenttype.PeriodID=SAL_ContractHeader.PeriodID
Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    
    where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID and SAL_DeliveryOrderHeader.Status = 1 
     {0} 

    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, SAL_ContractDetail.GrossAmount, AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, SAL_ContractDetail.Quantity, 
        --INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo
		 SAL_ContractDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
        SAL_ContractHeader.StatusCode
        ,SAL_DeliveryOrderDetail.ReversedQty
        ,SAL_ContractDetail.ReversedQuantity
        ,SAL_ContractDetail.Inclusive
        ,DocumentTypesGroup.GroupName,SAL_DeliveryOrderDetail.Quantity
		,SAL_DeliveryORderDetail.DODetailID
		,SAL_DeliveryORderDetail.DOHeaderID,SAL_DeliveryOrderHeader.DocumentTypeID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID
) TAB 
 {1}
group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, Tab.DocumentStatus,TAB.GroupName,Tab.DeliveryOrderNo  
		,TAB.DODetailID,TAB.DOHeaderID";

                const string Query_GetPendingDOdata = @"  select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo,Tab.DeliveryOrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, sum(TAB.Del_Quantity) -sum(TAB.DispatchQty)  as Balance, Tab.DocumentStatus,Sum(Tab.Del_Quantity) as Del_Quantity 
from 
(
    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, 

 SAL_DeliveryOrderHeader.DeliveryOrderNo,
        DocumentTypesGroup.GroupName,

        ROUND((SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
        *
        (case when SAL_ContractDetail.Inclusive = 0 then SAL_ContractDetail.Rate 
		when (SAL_ContractDetail.BaseAmount is null or SAL_ContractDetail.BaseAmount = 0) then SAL_ContractDetail.Rate else SAL_ContractDetail.BaseAmount end),0) AS GrossAmount,

        AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo as Sal_OrderNo, SAL_ContractHeader.ContractDate, 
            isnull(SAL_DeliveryOrderDetail.Quantity,0) as Del_Quantity, 
        --SAL_ContractDetail.Quantity as Sal_OrderQuantity, 
        SAL_ContractDetail.Quantity - isnull(SAL_ContractDetail.ReversedQuantity,0) as Sal_OrderQuantity,

        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
        --isnull(SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0)
		 0 as Balance, 
        
        --ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) 
		
        Case SAL_ContractHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
			,ISNULL((Select SUM(ABS(TransactionQUantity)) from INV_StockTransactionDetail where
			     INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
          and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
          and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchQty
    from SAL_DeliveryOrderHeader 
       inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID= SAL_DeliveryOrderHeader.DeliveryOrderID 
	   and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			and SAL_DeliveryOrderDetail.Status = 1 and SAL_DeliveryOrderHeader.Status = 1 
       
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
         --and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
            
			 INNER join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
             AND SAL_ContractDetail.ContractDetailID =SAL_DeliveryOrderDetail.ContractDetailID
            and SAL_ContractDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            and SAL_ContractDetail.Status = 1 
      
	    INNER join SAL_ContractHeader on SAL_ContractHeader.ContractID= SAL_ContractDetail.ContractID
            and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID 
       
          left join INV_Item on INV_Item.ItemID = SAL_ContractDetail.ItemID 
--and INV_Item.CompanyID = SAL_ContractDetail.CompanyID 
and INV_Item.Status = 1 
        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_ContractDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
        
        --left join INV_StockTransactionDetail on 
        --   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
        --  and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
        --  and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
        --   and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
        --   and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        --   and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        --left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        --    and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        --    and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

inner join set_documenttype on set_documenttype.documenttypeid =SAL_ContractHeader.SaleContractTypeID
         and set_documenttype.CompanyID=SAL_ContractHeader.CompanyID
         and set_documenttype.PeriodID=SAL_ContractHeader.PeriodID
Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    
    where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID and SAL_DeliveryOrderHeader.Status = 1 
     {0} 

    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, SAL_ContractDetail.GrossAmount, AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, SAL_ContractDetail.Quantity, 
        --INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo
		 SAL_ContractDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
        SAL_ContractHeader.StatusCode
        ,SAL_DeliveryOrderDetail.ReversedQty
        ,SAL_ContractDetail.ReversedQuantity
        ,SAL_ContractDetail.Inclusive
        ,DocumentTypesGroup.GroupName,SAL_DeliveryOrderDetail.Quantity
		,SAL_DeliveryORderDetail.DODetailID
		,SAL_DeliveryORderDetail.DOHeaderID,SAL_DeliveryOrderHeader.DocumentTypeID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID
) TAB 
 {1}
group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, Tab.DocumentStatus,TAB.GroupName,Tab.DeliveryOrderNo  ";
                #endregion

                if (CustomerFromID.HasValue && CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID between " + CustomerFromID + " AND " + CustomerToID);
                else if (CustomerFromID.HasValue && !CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID = " + CustomerFromID);
                else if (!CustomerFromID.HasValue && CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID = " + CustomerToID);


                if (BrokerID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.BrokerID  = " + BrokerID);

                if (ItemID.HasValue)
                    whereFilter.Append(" AND INV_Item.ItemID  = " + ItemID);


                #region Filter by Sales Order Document Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'");
                }
                else if (fromDate.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00'");
                else if (toDate.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'");

                #endregion

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentType.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DocumentTypeID = " + documentType.Value.ToString() + " ");
                if (regionID.HasValue)
                    whereFilter.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    whereFilter.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");
                if (IsPending == true)
                    addParameters.Append(" Where ((TAB.Del_Quantity  > TAB.DispatchQty) OR TAB.DispatchReturnQty > 0)");

                if (BatchID.HasValue)
                    whereFilter.Append(" and SAL_ContractHeader.Season = " + BatchID);
                if (DoNum.HasValue)
                    whereFilter.Append("AND SAL_DeliveryOrderHeader.DeliveryOrderNo =" + DoNum);


                if (ContractID > 0)
                {
                    whereFilter.Append(" AND SAL_ContractHeader.ContractNo = " + ContractID.Value);

                }

                if (IsSheikhoReport)
                {
                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPendingDOdata_WithLoadingPointQty, whereFilter.ToString(), addParameters.ToString()), sqlparams);
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                }
                else
                {
                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPendingDOdata, whereFilter.ToString(), addParameters.ToString()), sqlparams);
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                }
            
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable Get_AsOffPendingDeliveryOrderNew(bool IsSheikhoReport, int companyID, int periodID, Nullable<DateTime> fromDate = null,
   Nullable<DateTime> toDate = null, Nullable<int> CustomerFromID = null, Nullable<int> CustomerToID = null, Nullable<int> documentGroupID = null, Nullable<int> documentType = null, bool? IsPending = false, Nullable<int> BatchID = null, Nullable<int> ContractID = null, Nullable<int> DoNum = null, Nullable<int> ItemID = null, Nullable<int> BrokerID = null, Nullable<int> regionID = null, Nullable<int> salePersonID = null)
        {
            DataTable dt = null;
            StringBuilder whereFilter = new StringBuilder();
            StringBuilder addParameters = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region newquery
                const string Query_GetPendingDOdata_WithLoadingPointQtyNew = @"
  select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo,Tab.DeliveryOrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, (sum(TAB.Del_Quantity) -sum(TAB.DispatchQty)) + sum(TAB.DispatchReturnQty) as Balance, Tab.DocumentStatus,Sum(Tab.Del_Quantity) as Del_Quantity 
		,ISNULL(( 
	select sum(ABS(INV_LoadingPointDetail.TransactionQuantity)) from INV_LoadingPointDetail where
				INV_LoadingPointDetail.DODetailID = TAB.DODetailID
				and INV_LoadingPointDetail.DOHeaderID = TAB.DOHeaderID
				and INV_LoadingPointDetail.CompanyID = @CompanyID
				and INV_LoadingPointDetail.PeriodID = @PeriodID
				and INV_LoadingPointDetail.Status = 1
				
				),0) as LoadingPointQty

	,sum(TAB.DispatchReturnQty) as DispatchReturnQty

from 
(
    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, 

 SAL_DeliveryOrderHeader.DeliveryOrderNo,
        DocumentTypesGroup.GroupName,

        ROUND((SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
        *
        (case when SAL_ContractDetail.Inclusive = 0 then SAL_ContractDetail.Rate 
		when (SAL_ContractDetail.BaseAmount is null or SAL_ContractDetail.BaseAmount = 0) then SAL_ContractDetail.Rate else SAL_ContractDetail.BaseAmount end),0) AS GrossAmount,

        AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo as Sal_OrderNo, SAL_ContractHeader.ContractDate, 
            isnull(SAL_DeliveryOrderDetail.Quantity,0) as Del_Quantity, 
        --SAL_ContractDetail.Quantity as Sal_OrderQuantity, 
        SAL_ContractDetail.Quantity - isnull(SAL_ContractDetail.ReversedQuantity,0) as Sal_OrderQuantity,

        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
        --isnull(SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0)
		 0 as Balance, 
        
        --ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) 
		
        Case SAL_ContractHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
			,ISNULL((Select SUM(ABS(TransactionQUantity)) from INV_StockTransactionDetail where
			     INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
          and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
          and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchQty

			,ISNULL((Select SUM(ABS(SAL_ReverseDetail.ReversedQuantity)) from INV_StockTransactionDetail
		   inner join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID
		   and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID
		   and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID
		   and INV_StockTransactionHeader.FiscalID = INV_StockTransactionDetail.FiscalID
		   and INV_StockTransactionHeader.Status = 1

		   inner join SAL_ReverseDetail on SAL_ReverseDetail.SourceHeaderID = INV_StockTransactionHeader.StockTransactionID
		   and SAL_ReverseDetail.SourceDocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
		   and SAL_ReverseDetail.SourceCode = INV_StockTransactionHeader.DocumentNo
		   and SAL_ReverseDetail.SourceDetailID = INV_StockTransactionDetail.TransactionDetailID
		   and SAL_ReverseDetail.CompanyID = INV_StockTransactionHeader.CompanyID
		   and SAL_ReverseDetail.PeriodID = INV_StockTransactionHeader.PeriodID
		   and SAL_ReverseDetail.FiscalID = INV_StockTransactionHeader.FiscalID 
		   and SAL_ReverseDetail.Status = 1 
		   where
		   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
           and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
           and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchReturnQty

			,SAL_DeliveryOrderDetail.DODetailID,SAL_DeliveryOrderDetail.DOHeaderID
    from SAL_DeliveryOrderHeader 
       inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID= SAL_DeliveryOrderHeader.DeliveryOrderID 
	   and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			and SAL_DeliveryOrderDetail.Status = 1 and SAL_DeliveryOrderHeader.Status = 1 
       
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
         --and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
            
			 INNER join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
             AND SAL_ContractDetail.ContractDetailID =SAL_DeliveryOrderDetail.ContractDetailID
            and SAL_ContractDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            and SAL_ContractDetail.Status = 1 
      
	    INNER join SAL_ContractHeader on SAL_ContractHeader.ContractID= SAL_ContractDetail.ContractID
            and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID 
       
          left join INV_Item on INV_Item.ItemID = SAL_ContractDetail.ItemID 
--and INV_Item.CompanyID = SAL_ContractDetail.CompanyID 
and INV_Item.Status = 1 
        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_ContractDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
        
        --left join INV_StockTransactionDetail on 
        --   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
        --  and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
        --  and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
        --   and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
        --   and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        --   and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        --left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        --    and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        --    and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

inner join set_documenttype on set_documenttype.documenttypeid =SAL_ContractHeader.SaleContractTypeID
         and set_documenttype.CompanyID=SAL_ContractHeader.CompanyID
         and set_documenttype.PeriodID=SAL_ContractHeader.PeriodID
Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    
    where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID and SAL_DeliveryOrderHeader.Status = 1 
     {0} 

    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, SAL_ContractDetail.GrossAmount, AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, SAL_ContractDetail.Quantity, 
        --INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo
		 SAL_ContractDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
        SAL_ContractHeader.StatusCode
        ,SAL_DeliveryOrderDetail.ReversedQty
        ,SAL_ContractDetail.ReversedQuantity
        ,SAL_ContractDetail.Inclusive
        ,DocumentTypesGroup.GroupName,SAL_DeliveryOrderDetail.Quantity
		,SAL_DeliveryORderDetail.DODetailID
		,SAL_DeliveryORderDetail.DOHeaderID,SAL_DeliveryOrderHeader.DocumentTypeID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID
) TAB 
 {1}
group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, Tab.DocumentStatus,TAB.GroupName,Tab.DeliveryOrderNo  
		,TAB.DODetailID,TAB.DOHeaderID";

                const string Query_GetPendingDOdataNew = @"  select TAB.Description,TAB.GroupName ,TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo,Tab.DeliveryOrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, sum(TAB.DispatchQty) as DispatchQty, sum(TAB.Del_Quantity) -sum(TAB.DispatchQty)  as Balance, Tab.DocumentStatus,Sum(Tab.Del_Quantity) as Del_Quantity 
from 
(
    select INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, 

 SAL_DeliveryOrderHeader.DeliveryOrderNo,
        DocumentTypesGroup.GroupName,

        ROUND((SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0))
        *
        (case when SAL_ContractDetail.Inclusive = 0 then SAL_ContractDetail.Rate 
		when (SAL_ContractDetail.BaseAmount is null or SAL_ContractDetail.BaseAmount = 0) then SAL_ContractDetail.Rate else SAL_ContractDetail.BaseAmount end),0) AS GrossAmount,

        AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo as Sal_OrderNo, SAL_ContractHeader.ContractDate, 
            isnull(SAL_DeliveryOrderDetail.Quantity,0) as Del_Quantity, 
        --SAL_ContractDetail.Quantity as Sal_OrderQuantity, 
        SAL_ContractDetail.Quantity - isnull(SAL_ContractDetail.ReversedQuantity,0) as Sal_OrderQuantity,

        max(SAL_DeliveryOrderHeader.DODate) as DODate, 
        --isnull(SAL_DeliveryOrderDetail.Quantity - isnull(SAL_DeliveryOrderDetail.ReversedQty,0) - ABS(ISNULL(INV_StockTransactionDetail.TransactionQuantity, 0)), 0)
		 0 as Balance, 
        
        --ISNULL(ABS(INV_StockTransactionDetail.TransactionQuantity), 0) 
		
        Case SAL_ContractHeader.StatusCode When 1 then 'Approved' When 5 then 'In Process' When 12 Then 'Released for Approve' When 13 Then 'Partially Approved' 
            When 14 Then 'Discuss' When 15 Then 'Cancelled' When 16 Then 'Reversed' End As DocumentStatus 
			,ISNULL((Select SUM(ABS(TransactionQUantity)) from INV_StockTransactionDetail where
			     INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
          and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
          and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
           and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
           and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
           and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			),0)DispatchQty
    from SAL_DeliveryOrderHeader 
       inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID= SAL_DeliveryOrderHeader.DeliveryOrderID 
	   and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
			and SAL_DeliveryOrderDetail.Status = 1 and SAL_DeliveryOrderHeader.Status = 1 
       
        inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
         --and AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            and AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID and AR_SET_Customer.Status = 1 
            
			 INNER join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
             AND SAL_ContractDetail.ContractDetailID =SAL_DeliveryOrderDetail.ContractDetailID
            and SAL_ContractDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
            and SAL_ContractDetail.Status = 1 
      
	    INNER join SAL_ContractHeader on SAL_ContractHeader.ContractID= SAL_ContractDetail.ContractID
            and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID 
			and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID 
       
          left join INV_Item on INV_Item.ItemID = SAL_ContractDetail.ItemID 
--and INV_Item.CompanyID = SAL_ContractDetail.CompanyID 
and INV_Item.Status = 1 
        left join SET_Unitofmeasure on SET_Unitofmeasure.UOMID = SAL_ContractDetail.ItemUnitID and SET_Unitofmeasure.Status = 1 
        
        --left join INV_StockTransactionDetail on 
        --   INV_StockTransactionDetail.SourceLineID = SAL_DeliveryORderDetail.DODetailID 
        --  and INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryORderDetail.DOHeaderID
        --  and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID 
        --   and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo 
        --   and INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
        --   and INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
        --left join INV_StockTransactionHeader on INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        --    and INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        --    and INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 

inner join set_documenttype on set_documenttype.documenttypeid =SAL_ContractHeader.SaleContractTypeID
         and set_documenttype.CompanyID=SAL_ContractHeader.CompanyID
         and set_documenttype.PeriodID=SAL_ContractHeader.PeriodID
Left Join DocumentTypesGroup On DocumentTypesGroup.GroupID = Set_DocumentType.GroupID 
--DocumentTypesGroup.CompanyID = Set_DocumentType.CompanyID And 
    
    where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID and SAL_DeliveryOrderHeader.Status = 1 
     {0} 

    group by INV_Item.Description, SET_Unitofmeasure.UOMName, SAL_ContractDetail.Rate, SAL_ContractDetail.GrossAmount, AR_SET_Customer.CustomerName, 
        AR_SET_Customer.CustomerID, SAL_ContractHeader.ContractNo, SAL_ContractHeader.ContractDate, SAL_ContractDetail.Quantity, 
        --INV_StockTransactionDetail.TransactionQuantity, INV_StockTransactionHeader.DocumentNo
		 SAL_ContractDetail.BaseAmount, SAL_DeliveryOrderHeader.DeliveryOrderNo, 
        SAL_ContractHeader.StatusCode
        ,SAL_DeliveryOrderDetail.ReversedQty
        ,SAL_ContractDetail.ReversedQuantity
        ,SAL_ContractDetail.Inclusive
        ,DocumentTypesGroup.GroupName,SAL_DeliveryOrderDetail.Quantity
		,SAL_DeliveryORderDetail.DODetailID
		,SAL_DeliveryORderDetail.DOHeaderID,SAL_DeliveryOrderHeader.DocumentTypeID,SAL_DeliveryOrderHeader.CompanyID,SAL_DeliveryOrderHeader.PeriodID
) TAB 
 {1}
group by TAB.Description, TAB.UOMName, TAB.Rate, TAB.GrossAmount, TAB.CustomerName, TAB.CustomerID, TAB.Sal_OrderNo, TAB.ContractDate, TAB.Sal_OrderQuantity, 
    TAB.DODate, Tab.DocumentStatus,TAB.GroupName,Tab.DeliveryOrderNo  ";
                #endregion

                if (CustomerFromID.HasValue && CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID between " + CustomerFromID + " AND " + CustomerToID);
                else if (CustomerFromID.HasValue && !CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID = " + CustomerFromID);
                else if (!CustomerFromID.HasValue && CustomerToID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.CustomerID = " + CustomerToID);


                if (BrokerID.HasValue)
                    whereFilter.Append(" AND AR_SET_Customer.BrokerID  = " + BrokerID);

                if (ItemID.HasValue)
                    whereFilter.Append(" AND INV_Item.ItemID  = " + ItemID);


                #region Filter by Sales Order Document Date

                if (fromDate.HasValue && toDate.HasValue)
                {
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate BETWEEN '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00' AND '"
                        + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'");
                }
                else if (fromDate.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate >= '" + DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00'");
                else if (toDate.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DODate <= '" + DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59'");

                #endregion

                if (documentGroupID.HasValue)
                    whereFilter.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");
                if (documentType.HasValue)
                    whereFilter.Append(" AND SAL_DeliveryOrderHeader.DocumentTypeID = " + documentType.Value.ToString() + " ");
                if (regionID.HasValue)
                    whereFilter.Append(" AND SAL_ContractHeader.RegionID = " + regionID.Value.ToString() + " ");
                if (salePersonID.HasValue)
                    whereFilter.Append(" AND SAL_ContractHeader.SalePersonID = " + salePersonID.Value.ToString() + " ");
                if (IsPending == true)
                    addParameters.Append(" Where ((TAB.Del_Quantity  > TAB.DispatchQty) OR TAB.DispatchReturnQty > 0)");

                if (BatchID.HasValue)
                    whereFilter.Append(" and SAL_ContractHeader.Season = " + BatchID);
                if (DoNum.HasValue)
                    whereFilter.Append("AND SAL_DeliveryOrderHeader.DeliveryOrderNo =" + DoNum);


                if (ContractID > 0)
                {
                    whereFilter.Append(" AND SAL_ContractHeader.ContractNo = " + ContractID.Value);

                }

                if (IsSheikhoReport)
                {
                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPendingDOdata_WithLoadingPointQtyNew, whereFilter.ToString(), addParameters.ToString()), sqlparams);
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                }
                else
                {
                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetPendingDOdataNew, whereFilter.ToString(), addParameters.ToString()), sqlparams);
                    if (ds != null && ds.Tables.Count > 0)
                        dt = ds.Tables[0];
                }

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetAllDataWithSalesOrder_ForSales(string strWhere)
        {
            DataTable dt = null;

            try
            {
               
                    #region Query

                    string Query_SelectAllWithSalesOrder_WithPaging = @" 
Select DeliveryOrders.*, ROW_NUMBER() Over (Order by Cast(DeliveryOrders.DeliveryOrderNo as int) desc) as 'Row' 
From 
(
    SELECT [DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], AR_SET_Customer.Customername, SAL_DeliveryOrderHeader.[PeriodID], [DocumentTypeID], 
        [DeliveryOrderNo], [DODate], [DOValidityDate], SAL_DeliveryOrderHeader.[CustomerID], SAL_DeliveryOrderHeader.[IsCancelled], 
        SAL_DeliveryOrderHeader.[CancellationDate], SAL_DeliveryOrderHeader.[CancelledBy], SAL_DeliveryOrderHeader.[IsApproved], SAL_DeliveryOrderHeader.[ApprovedBy], 
        SAL_DeliveryOrderHeader.[ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], 
        SAL_DeliveryOrderHeader.[IsDispatchGenerated], SAL_DeliveryOrderHeader.[IsPaid], [TotalPaidAmount], SAL_DeliveryOrderHeader.[CreatedBy], 
        SAL_DeliveryOrderHeader.[CreateDate], SAL_DeliveryOrderHeader.[UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], 
        SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], SAL_DeliveryOrderHeader.[StatusCode], SAL_DeliveryOrderHeader.[TotalAmount], 
        Set_Batch.Description as Batch , sum(Sal_DeliveryOrderDetail.Quantity) as Quantity
,DeliveryOrderNo as DocumentNo , DODate as DocumentDate , Set_PeriodCustom.PeriodSetName
,(Select DispatchID from SAL_DispatchHeader
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
where INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo
and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID) as DispatchID
,(Select INV_StockTransactionHeader.StockTransactionID from SAL_DispatchHeader
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
where INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo
and INV_StockTransactionDetail.SourceDocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID) as StockTransactionID
    FROM SAL_DeliveryOrderHeader 
        Inner join AR_SET_Customer on 
--AR_SET_Customer.CompanyID = SAL_DeliveryOrderHeader.CompanyID AND 
AR_SET_Customer.PeriodID = SAL_DeliveryOrderHeader.PeriodID 
            AND AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID 
        Inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
            And Sal_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID And Sal_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID 
            And Sal_DeliveryOrderDetail.Status = 1 
        Inner Join Sal_OrderHeader On Sal_OrderHeader.CompanyID = Sal_DeliveryOrderDetail.CompanyID And Sal_OrderHeader.PeriodID = Sal_DeliveryOrderDetail.PeriodID 
            And Sal_OrderHeader.OrderID = Sal_DeliveryOrderDetail.SalesOrderHeaderID 
        Left Join Set_Batch On Set_Batch.PeriodID = Sal_OrderHeader.PeriodID --Set_Batch.CompanyID = Sal_OrderHeader.CompanyID And 
            And Set_Batch.BatchID = Sal_OrderHeader.Season 
Left join Set_PeriodCustom on  
 SAL_DeliveryOrderHeader.DODate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1   
    {WhereFilter}
    group by
		[DeliveryOrderID], SAL_DeliveryOrderHeader.[CompanyID], AR_SET_Customer.Customername, SAL_DeliveryOrderHeader.[PeriodID], [DocumentTypeID], 
        [DeliveryOrderNo], [DODate], [DOValidityDate], SAL_DeliveryOrderHeader.[CustomerID], SAL_DeliveryOrderHeader.[IsCancelled], 
        SAL_DeliveryOrderHeader.[CancellationDate], SAL_DeliveryOrderHeader.[CancelledBy], SAL_DeliveryOrderHeader.[IsApproved], SAL_DeliveryOrderHeader.[ApprovedBy], 
        SAL_DeliveryOrderHeader.[ApproveDate], SAL_DeliveryOrderHeader.[LocationID], [AllowPrinting], [PrintingAllowedBy], [PrintingAllowedDate], [LastPrintedBy], [LastPrintDate], 
        SAL_DeliveryOrderHeader.[IsDispatchGenerated], SAL_DeliveryOrderHeader.[IsPaid], [TotalPaidAmount], SAL_DeliveryOrderHeader.[CreatedBy], 
        SAL_DeliveryOrderHeader.[CreateDate], SAL_DeliveryOrderHeader.[UpdatedBy], SAL_DeliveryOrderHeader.[UpdateDate], SAL_DeliveryOrderHeader.[CreatedByValue], 
        SAL_DeliveryOrderHeader.[UpdatedByValue], SAL_DeliveryOrderHeader.[Status], SAL_DeliveryOrderHeader.[StatusCode], SAL_DeliveryOrderHeader.[TotalAmount], 
        Set_Batch.[Description],Set_PeriodCustom.PeriodSetName
) as DeliveryOrders ";

                    #endregion

                    string query = Query_SelectAllWithSalesOrder_WithPaging.Replace("{WhereFilter}", strWhere);

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(query).ToString()).Tables[0];
                
             
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public bool InsertDOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colDODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colDOVIDetAdd, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDeliveryOrder");

            try
            {
                this.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (SAL_DeliveryOrderDetail dod in colDODetail)
                {
                    dod.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dod.FiscalID = CurrentFiscalID;
                   
                    dod.Insert(sqltrans);
                    try
                    {
                        var Records = from objDOVehicle in colDOVIDetAdd
                                      where objDOVehicle.DODetailID == dod.SalesOrderDetailID && objDOVehicle.DOHeaderID == dod.SalesOrderHeaderID
                                      select objDOVehicle;

                        #region Do Vehicle Information
                        foreach (SAL_DOVehicleInformation dovi in Records)
                        {
                            dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                            dovi.DOHeaderID = this.DeliveryOrderID;
                            dovi.DODetailID = (int)dod.DODetailID;
                            dovi.Insert(sqltrans);
                            SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                            sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                            sdvi.VehicleID = (int)dovi.VehicleID;
                            sdvi.CompanyID = CompanyID;
                            sdvi.PeriodID = PeriodID;
                            sdvi.UpdateDataByVehicleID(sqltrans);
                        }
                    }

                    catch { }
                    //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
                        #endregion

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dod.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, f_CreatedBy,
                        f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                    da.DocumentNo = this.DeliveryOrderNo;
                da.FiscalID = CurrentFiscalID;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDeliveryOrder");
            }

            return result;
        }

        public bool InsertDOHeaderAndDetailForContract(List<SAL_DeliveryOrderDetail> colDODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colDOVIDetAdd, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDeliveryOrder");

            try
            {
                this.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (SAL_DeliveryOrderDetail dod in colDODetail)
                {
                    dod.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dod.FiscalID = CurrentFiscalID;
                    dod.DOHeaderID = DeliveryOrderID;
                    dod.Insert(sqltrans);
                    try
                    {
                        var Records = from objDOVehicle in colDOVIDetAdd
                                      where objDOVehicle.DODetailID == dod.SalesOrderDetailID && objDOVehicle.DOHeaderID == dod.SalesOrderHeaderID
                                      select objDOVehicle;

                        #region Do Vehicle Information
                        foreach (SAL_DOVehicleInformation dovi in Records)
                        {
                            dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                            dovi.DOHeaderID = this.DeliveryOrderID;
                            dovi.DODetailID = (int)dod.DODetailID;
                            dovi.Insert(sqltrans);
                            SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                            sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                            sdvi.VehicleID = (int)dovi.VehicleID;
                            sdvi.CompanyID = CompanyID;
                            sdvi.PeriodID = PeriodID;
                            sdvi.UpdateDataByVehicleID(sqltrans);
                        }
                    }

                    catch { }
                    //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
                        #endregion

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dod.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, f_CreatedBy,
                        f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.DeliveryOrderNo;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDeliveryOrder");
            }

            return result;
        }
        public bool InsertDODCSOHeaderAndDetail(List<SAL_DeliveryOrderDetail> colDODetail, List<SAL_DeliveryContractDetail> colDCDetail, List<SAL_OrderDetail> colSODetail, SET_DocumentApprove da, List<SAL_DOVehicleInformation> colDOVIDetAdd, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertDeliveryOrder");

            try
            {
                this.DeliveryOrderNo = SET_DocumentSequence.ManualDocumentSequence(22, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (SAL_DeliveryOrderDetail dod in colDODetail)
                {
                    dod.DODetailID = Convert.ToInt16(SAL_DeliveryOrderDetail.GetNewDODetailID(CompanyID, DeliveryOrderID, PeriodID, sqltrans));
                    dod.FiscalID = CurrentFiscalID;
                    dod.DOHeaderID = DeliveryOrderID;
                    dod.SalesOrderHeaderID = 425;
                    dod.SalesOrderDetailID = 1;
                    dod.Insert(sqltrans);
                    try
                    {
                        var Records = from objDOVehicle in colDOVIDetAdd
                                      where objDOVehicle.DODetailID == dod.SalesOrderDetailID && objDOVehicle.DOHeaderID == dod.SalesOrderHeaderID
                                      select objDOVehicle;

                        #region Do Vehicle Information
                        foreach (SAL_DOVehicleInformation dovi in Records)
                        {
                            dovi.DOVehicleInformationID = short.Parse(SAL_DOVehicleInformation.GetNewDOVehicleInformationID(CompanyID, PeriodID, sqltrans).ToString());
                            dovi.DOHeaderID = this.DeliveryOrderID;
                            dovi.DODetailID = (int)dod.DODetailID;
                            dovi.Insert(sqltrans);
                            SAL_DispatchVehicleInformation sdvi = new SAL_DispatchVehicleInformation();
                            sdvi.VehicleInformationID = dovi.DOVehicleInformationID;
                            sdvi.VehicleID = (int)dovi.VehicleID;
                            sdvi.CompanyID = CompanyID;
                            sdvi.PeriodID = PeriodID;
                            sdvi.UpdateDataByVehicleID(sqltrans);
                        }
                    }

                    catch { }
                    //Update Sal_DispatchVehicleInformation (VehicleInformationID) Against Chassis NO. of Dovi VehicleInformation ID 
                        #endregion

                    #region Checking For Sales Order Complete Status

                    CheckQuantity(dod.SalesOrderHeaderID, this.f_CompanyID, this.f_PeriodID, sqltrans, SOEnumaration.SalesOrder_Stage.Delivery, f_CreatedBy,
                        f_CreateDate, f_CreatedByValue);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                //if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                //    da.DocumentNo = this.DeliveryOrderNo;
                //    da.FiscalID = CurrentFiscalID;
                //da.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertDeliveryOrder");
            }

            return result;
        }


        public static DataRow GetDataByKey1(int companyID, int deliveryOrderID, int periodID, int FiscalID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DeliveryOrderID", deliveryOrderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey, sqlparams);

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


        public static bool ISDispatchMadeAgainstDO(int companyID, int periodID, int DeliveryOrderNo, int DocumentTypeiD, int FiscalID)
        {
            bool result = true;

            DataTable dt = null;

            try
            {
                const string Query_DOCheck = @"Select * from  SAL_DispatchHeader 
		Inner Join Inv_StockTransactionHeader On Inv_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID 
            And Inv_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID And Inv_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID 
            And Inv_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo 
		And Inv_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
		
		inner Join Inv_StockTransactionDetail On Inv_StockTransactionDetail.CompanyID = Inv_StockTransactionHeader.CompanyID 
            And Inv_StockTransactionDetail.PeriodID = Inv_StockTransactionHeader.PeriodID 
            And Inv_StockTransactionDetail.StockTransactionID = Inv_StockTransactionHeader.StockTransactionID 
		
		inner Join Sal_DeliveryOrderDetail On Sal_DeliveryOrderDetail.CompanyID = Inv_StockTransactionDetail.CompanyID 
            And Sal_DeliveryOrderDetail.PeriodID = Inv_StockTransactionDetail.PeriodID 
            And Sal_DeliveryOrderDetail.DOHeaderID = Inv_StockTransactionDetail.SourceHeaderID 
            And Sal_DeliveryOrderDetail.DODetailID = Inv_StockTransactionDetail.SourceLineID 

			Inner Join Sal_DeliveryOrderHeader on Sal_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID 
			And Sal_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
			And Sal_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID
			And Sal_DeliveryOrderHeader.FiscalID = SAL_DeliveryOrderDetail.FiscalID
			Where 
			Sal_DeliveryOrderHeader.CompanyID= @CompanyID
			AND  Sal_DeliveryOrderHeader.PeriodID= @PeriodID
			AND Sal_DeliveryOrderHeader.DeliveryOrderNo=@DeliveryOrderNo 
			And Sal_DeliveryOrderHeader.DocumentTypeID=@DocumentTypeID
            And Sal_DeliveryOrderHeader.FiscalID = @FiscalID
			AND SAL_DispatchHeader.Status=1";

                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DeliveryOrderNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeiD);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_DOCheck, sqlparams).Tables[0];
                if (dt != null && dt.Rows.Count > 0)
                {
                    result = false;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public bool DOClose(int Companyid, int perioid, int DocmntTypeID, int DeliveryOrderID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@DeliveryOrderID", DeliveryOrderID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);
            
                const string Query_DOClose = @"
UPDATE SAL_DeliveryOrderHeader
SET StopDispatch = 1 ,  Remarks =  @remarks
where SAL_DeliveryOrderHeader.DocumentTypeID =@DocumentTypeID AND SAL_DeliveryOrderHeader.DeliveryOrderID = @DeliveryOrderID
AND SAL_DeliveryOrderHeader.CompanyID = @CompanyID  AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_DOClose, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_DOClose, sqlparams) > 0)
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

        public bool DOSOClose(int Companyid, int perioid, int DocmntTypeID, int DeliveryOrderID, string remarks)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocmntTypeID);
                sqlparams[3] = new SqlParameter("@DeliveryOrderID", DeliveryOrderID);
                sqlparams[4] = new SqlParameter("@remarks", remarks);

                const string Query_DOSOClose = @"
UPDATE SAL_DeliveryOrderHeader
SET StopDispatch = 1 ,  Remarks =  @remarks
where SAL_DeliveryOrderHeader.DocumentTypeID =@DocumentTypeID AND SAL_DeliveryOrderHeader.DeliveryOrderID = @DeliveryOrderID
AND SAL_DeliveryOrderHeader.CompanyID = @CompanyID  AND SAL_DeliveryOrderHeader.PeriodID = @PeriodID 

UPDATE SAL_OrderHeader
SET ClosedCode = 1 , ClosedOn = GETDATE()  ,  Remarks =  @remarks
where --SAL_DeliveryOrderHeader.DocumentTypeID =@DocumentTypeID 
 SAL_OrderHeader.OrderID = @DeliveryOrderID
AND SAL_OrderHeader.CompanyID = @CompanyID  AND SAL_OrderHeader.PeriodID = @PeriodID";


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


        public static DataTable GetDispatchStatus(int companyID, int periodID, int ContractID)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            string Query_Dispatch_Order_GetDispatchStatus = @" 
select * from (
select SAL_ContractHeader.ContractID,sum(SAL_ContractDetail.Quantity) as Quantity

,isnull((
select sum(TransactionQuantity) 
from INV_StockTransactionDetail 
inner join SAL_DeliveryOrderHeader on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
And INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo
AND INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
AND INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
AND INV_StockTransactionDetail.Status = 1

inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID

inner join SAL_ContractHeader sal_contract on sal_contract.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID 
and sal_contract.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and sal_contract.PeriodID = SAL_DeliveryOrderDetail.PeriodID
where sal_contract.ContractID = SAL_ContractHeader .ContractID
and sal_contract.CompanyID = SAL_ContractHeader .CompanyID
and INV_StockTransactionDetail.status = 1
AND INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
 ),0) as DispatchQty
from SAL_ContractHeader 
inner join SAL_ContractDetail on SAL_ContractDetail.ContractID = SAL_ContractHeader.ContractID
and SAL_ContractDetail.CompanyID = SAL_ContractHeader.CompanyID
and SAL_ContractDetail.PeriodID = SAL_ContractHeader.PeriodID


where SAL_ContractHeader.CompanyID = @CompanyID and SAL_ContractHeader.PeriodID = @PeriodID
and SAL_ContractHeader.ContractID = @ContractID
group by SAL_ContractHeader.ContractID,SAL_ContractHeader.CompanyID


) as tab
where tab.Quantity = DispatchQty
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractID", ContractID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Dispatch_Order_GetDispatchStatus + additionalFilter, sqlparams);

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


        public static DataTable GetDispatchStatusByDOHeaderID(int companyID, int periodID, int DeliveryOrderID)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            string Query_Dispatch_Order_GetDispatchStatus = @" 
select * from (
select SAL_DeliveryOrderHeader.DeliveryOrderID,sum(SAL_DeliveryOrderDetail.Quantity) as DOQuantity

,isnull((
select sum(abs(TransactionQuantity))
from INV_StockTransactionDetail 
inner join SAL_DeliveryOrderHeader sal_Do on INV_StockTransactionDetail.SourceHeaderID = sal_Do.DeliveryOrderID
And INV_StockTransactionDetail.SourceCode = sal_Do.DeliveryOrderNo
AND INV_StockTransactionDetail.CompanyID = sal_Do.CompanyID 
AND INV_StockTransactionDetail.PeriodID = sal_Do.PeriodID
AND INV_StockTransactionDetail.Status = 1

inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = sal_Do.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = sal_Do.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = sal_Do.PeriodID

inner join SAL_ContractHeader sal_contract on sal_contract.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID 
and sal_contract.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and sal_contract.PeriodID = SAL_DeliveryOrderDetail.PeriodID
where sal_Do.DeliveryOrderID = SAL_DeliveryOrderHeader .DeliveryOrderID
and sal_Do.CompanyID = SAL_DeliveryOrderHeader .CompanyID
and INV_StockTransactionDetail.status = 1
AND INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
 ),0) as DispatchQty
from SAL_DeliveryOrderHeader 
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SAL_DeliveryOrderDetail.Status= 1

where SAL_DeliveryOrderHeader.CompanyID = @CompanyID and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
and SAL_DeliveryOrderHeader.DeliveryOrderID =  @DeliveryOrderID
group by SAL_DeliveryOrderHeader.DeliveryOrderID,SAL_DeliveryOrderHeader.CompanyID


) as tab
where tab.DOQuantity > 0 and tab.DOQuantity = DispatchQty


";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderID", DeliveryOrderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Dispatch_Order_GetDispatchStatus + additionalFilter, sqlparams);

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

        public static DataTable GetDOandSODatabyContactID(int companyID, int periodID, int ContractID)
        {
            DataTable dtToReturn = null;
            string additionalFilter = string.Empty;

            string Query_Dispatch_Order_GetDispatchStatus = @" 
select SAL_DeliveryOrderDetail.DOHeaderID,SAL_DeliveryOrderDetail.DODetailID,SAL_DeliveryOrderDetail.ContractDetailID,sum(SAL_DeliveryOrderDetail.Quantity) as DoQty
,SAL_DeliveryOrderHeader.CustomerID,SAL_DeliveryOrderHeader.BrokerID,SAL_DeliveryOrderDetail.SalesOrderHeaderID,SAL_DeliveryOrderDetail.SalesOrderDetailID
,isnull((
select count(StockTransactionID) 
from INV_StockTransactionDetail 
inner join SAL_DeliveryOrderHeader on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
And INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo
AND INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
AND INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
AND INV_StockTransactionDetail.Status = 1

inner join SAL_DeliveryOrderDetail sal_do on sal_do.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and sal_do.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and sal_do.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and sal_do.DODetailID = INV_StockTransactionDetail.SourceLineID


inner join SAL_ContractHeader sal_contract on sal_contract.ContractID = sal_do.ContractHeaderID 
and sal_contract.CompanyID = sal_do.CompanyID
and sal_contract.PeriodID = sal_do.PeriodID
where sal_contract.ContractID = SAL_ContractHeader .ContractID
and sal_contract.CompanyID = SAL_ContractHeader .CompanyID
and INV_StockTransactionDetail.status = 1
AND INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
AND INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
 ),0) as IsDispatch

 ,isnull((
select sum(abs(TransactionQuantity)) 
from INV_StockTransactionDetail 
inner join SAL_DeliveryOrderHeader on INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
And INV_StockTransactionDetail.SourceCode = SAL_DeliveryOrderHeader.DeliveryOrderNo
AND INV_StockTransactionDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID 
AND INV_StockTransactionDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
AND INV_StockTransactionDetail.Status = 1

inner join SAL_DeliveryOrderDetail sal_do on sal_do.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and sal_do.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and sal_do.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and sal_do.DODetailID = INV_StockTransactionDetail.SourceLineID


inner join SAL_ContractHeader sal_contract on sal_contract.ContractID = sal_do.ContractHeaderID 
and sal_contract.CompanyID = sal_do.CompanyID
and sal_contract.PeriodID = sal_do.PeriodID
where sal_contract.ContractID = SAL_ContractHeader .ContractID
and sal_contract.CompanyID = SAL_ContractHeader .CompanyID
and INV_StockTransactionDetail.status = 1
AND INV_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID
AND INV_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID
 ),0) as DispatchQty

from SAL_DeliveryOrderDetail
inner join SAL_DeliveryOrderHeader on SAL_DeliveryOrderHeader.DeliveryOrderID = SAL_DeliveryOrderDetail.DOHeaderID
and SAL_DeliveryOrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_DeliveryOrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID
and SAL_DeliveryOrderHeader.status = 1

inner join SAL_ContractHeader on SAL_ContractHeader.ContractID = SAL_DeliveryOrderDetail.ContractHeaderID
and SAL_ContractHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_ContractHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID
and SAL_ContractHeader.Status = 1

inner join SAL_OrderHeader on SAL_OrderHeader .OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
and SAL_OrderHeader.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_OrderHeader.PeriodID = SAL_DeliveryOrderDetail.PeriodID
where SAL_DeliveryOrderDetail.CompanyID = @CompanyID and SAL_DeliveryOrderDetail.PeriodID = @PeriodID 
and SAL_ContractHeader.ContractID = @ContractID
group by SAL_DeliveryOrderDetail.DOHeaderID,SAL_DeliveryOrderDetail.ContractDetailID,SAL_DeliveryOrderHeader.CustomerID,SAL_DeliveryOrderHeader.BrokerID,SAL_ContractHeader.ContractID,SAL_ContractHeader.CompanyID,SAL_DeliveryOrderDetail.DODetailID
,SAL_DeliveryOrderDetail.SalesOrderHeaderID,SAL_DeliveryOrderDetail.SalesOrderDetailID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractID", ContractID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Dispatch_Order_GetDispatchStatus + additionalFilter, sqlparams);

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


        public static DataRow GetDataByDocumentNo(int companyID, int periodID, int DeliveryorderNo, int DocumentTypeID, int FiscalID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryorderNo", DeliveryorderNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);

                const string Query_GetDataByDocumentNo = @" select * from sal_deliveryorderheader where DeliveryorderNo = @DeliveryorderNo and CompanyID = @CompanyID and DocumentTypeID=@DocumentTypeID and FiscalID = @FiscalID";

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetDataByDocumentNo, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataByDocumentNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataByDocumentNo, sqlparams);

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


        public static DataTable GetAllDataForGateInBatchWiseFiscalWise(Nullable<int> companyID, Nullable<int> periodID, int batchid, int FiscalID, int DONo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @"Select AR_SET_Customer.CustomerID,
 CustomerName + ' * ' +set_city.CityName + '*'+ case isnull(AR_SET_Customer.IsRegistered,0) when 1 then 'Registered' else 'Un-Registered' end + '*'+ convert(varchar,AR_SET_Customer.CustomerID)  as CustomerName ,
SAL_DeliveryOrderHeader.DODate,
ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar,SAL_OrderDetail.ItemID)
                FROM SAL_OrderDetail 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
						and SO.CompanyID = SAL_OrderDetail.CompanyID
				and SO.PeriodID = SAL_OrderDetail.PeriodID
				and SO.FiscalID = SAL_OrderDetail.FiscalID
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as ItemID,
		
		ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar(Max),INV_Item.Description)
                FROM SAL_OrderDetail 
				inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
				and INV_Item.Status = 1 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
						and SO.CompanyID = SAL_OrderDetail.CompanyID
				and SO.PeriodID = SAL_OrderDetail.PeriodID
				and SO.FiscalID = SAL_OrderDetail.FiscalID
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as Description,
		
--INV_Item.ItemID,INV_Item.Description
SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName
,SUM(SAL_DeliveryOrderDetail.Quantity) as Quantity
, Set_Batch.BatchID,Set_Batch.Description
from SAL_DeliveryOrderHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SET_DocumentType.Status = 1
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SAL_DeliveryOrderDetail.Status = 1
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
and SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
and SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
and SAL_OrderDetail.Status = 1
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
and SAL_OrderHeader.Status = 1

 Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID 
 And Set_Batch.BatchID = SAL_OrderHeader.Season 
and Set_Batch.status = 1
inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
and INV_Item.Status = 1 
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
and SET_UnitofMeasure.Status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
and AR_SET_Customer.Status = 1
inner join set_city on set_city.cityID = AR_SET_Customer.cityID
where SAL_DeliveryOrderHeader.Status = 1 
and SAL_DeliveryOrderHeader.StatusCode = 1 
and SAL_DeliveryOrderHeader.DeliveryOrderNo = @DeliveryOrderNo
and SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
and SAL_DeliveryOrderHeader.FiscalID = @FiscalID
and Set_Batch.BatchID = @BatchID
 ";

            if (type != "")
            {
                sb.Append(" and  DocumentType like '%" + type + "' ");
            }
            sb.Append(" group by AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DODate ,SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName, SAL_OrderHeader.OrderID,SAL_OrderHeader.CompanyID,SAL_OrderHeader.PeriodID,set_city.CityName,AR_SET_Customer.IsRegistered,Set_Batch.BatchID,Set_Batch.Description");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DONo);
                sqlparams[3] = new SqlParameter("@BatchID", batchid);
                sqlparams[4] = new SqlParameter("@FiscalID", FiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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


        public static DataTable GetAllDataForGateInBatchWiseWithInProcessDO(Nullable<int> companyID, Nullable<int> periodID, int batchid, int DONo, string type, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            const string Query = @"Select AR_SET_Customer.CustomerID,
 CustomerName + ' * ' +set_city.CityName + '*'+ case isnull(AR_SET_Customer.IsRegistered,0) when 1 then 'Registered' else 'Un-Registered' end + '*'+ convert(varchar,AR_SET_Customer.CustomerID)  as CustomerName ,
SAL_DeliveryOrderHeader.DODate,
ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar,SAL_OrderDetail.ItemID)
                FROM SAL_OrderDetail 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
						and SO.CompanyID = SAL_OrderDetail.CompanyID
				and SO.PeriodID = SAL_OrderDetail.PeriodID
				and SO.FiscalID = SAL_OrderDetail.FiscalID
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as ItemID,
		
		ISNULL(
        (
            SELECT STUFF(
		    (
                SELECT ', ' + convert(varchar(Max),INV_Item.Description)
                FROM SAL_OrderDetail 
				inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
				and INV_Item.Status = 1 
                WHERE SO.OrderID = SAL_OrderDetail.OrderID 
						and SO.CompanyID = SAL_OrderDetail.CompanyID
				and SO.PeriodID = SAL_OrderDetail.PeriodID
				and SO.FiscalID = SAL_OrderDetail.FiscalID
                ORDER BY SAL_OrderDetail.ItemID FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_OrderHeader SO 
            where SO.OrderID = SAL_OrderHeader.OrderID and SO.CompanyID = SAL_OrderHeader.CompanyID and SO.PeriodID = SAL_OrderHeader.PeriodID
        ), '') as Description,
		
--INV_Item.ItemID,INV_Item.Description
SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName
,SUM(SAL_DeliveryOrderDetail.Quantity) as Quantity
, Set_Batch.BatchID,Set_Batch.Description
from SAL_DeliveryOrderHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_DeliveryOrderHeader.DocumentTypeID
and SET_DocumentType.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SET_DocumentType.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SET_DocumentType.Status = 1
inner join SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.DOHeaderID = SAL_DeliveryOrderHeader.DeliveryOrderID
and SAL_DeliveryOrderDetail.CompanyID = SAL_DeliveryOrderHeader.CompanyID
and SAL_DeliveryOrderDetail.PeriodID = SAL_DeliveryOrderHeader.PeriodID
and SAL_DeliveryOrderDetail.Status = 1
inner join SAL_OrderDetail on SAL_OrderDetail.OrderID = SAL_DeliveryOrderDetail.SalesOrderHeaderID
and SAL_OrderDetail.OrderDetailID = SAL_DeliveryOrderDetail.SalesOrderDetailID
and SAL_OrderDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID
and SAL_OrderDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID
and SAL_OrderDetail.Status = 1
inner join SAL_OrderHeader on SAL_OrderHeader.OrderID = SAL_OrderDetail.OrderID
and SAL_OrderHeader.CompanyID = SAL_OrderDetail.CompanyID
and SAL_OrderHeader.PeriodID = SAL_OrderDetail.PeriodID
and SAL_OrderHeader.Status = 1

 Left Join Set_Batch On Set_Batch.PeriodID = SAL_OrderHeader.PeriodID 
 And Set_Batch.BatchID = SAL_OrderHeader.Season 
and Set_Batch.status = 1
inner join INV_Item on INV_Item.itemid = SAL_OrderDetail.ItemID
and INV_Item.Status = 1 
inner join SET_UnitofMeasure on SET_UnitofMeasure.UOMID = SAL_OrderDetail.ItemUnitID
and SET_UnitofMeasure.Status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = SAL_DeliveryOrderHeader.CustomerID
and AR_SET_Customer.Status = 1
inner join set_city on set_city.cityID = AR_SET_Customer.cityID
where SAL_DeliveryOrderHeader.Status = 1 
and SAL_DeliveryOrderHeader.DeliveryOrderNo = @DeliveryOrderNo
and SAL_DeliveryOrderHeader.CompanyID = @CompanyID
and SAL_DeliveryOrderHeader.PeriodID = @PeriodID
--and SAL_DeliveryOrderHeader.FiscalID = @FiscalID
and Set_Batch.BatchID = @BatchID
 ";

            if (type != "")
            {
                sb.Append(" and  DocumentType like '%" + type + "' ");
            }
            sb.Append(" group by AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName, SAL_DeliveryOrderHeader.DODate ,SET_UnitofMeasure.UOMID,SET_UnitofMeasure.UOMName, SAL_OrderHeader.OrderID,SAL_OrderHeader.CompanyID,SAL_OrderHeader.PeriodID,set_city.CityName,AR_SET_Customer.IsRegistered,Set_Batch.BatchID,Set_Batch.Description");
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DeliveryOrderNo", DONo);
                sqlparams[3] = new SqlParameter("@BatchID", batchid);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query + sb.ToString(), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query + sb.ToString(), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query + sb.ToString(), sqlparams);

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

        public static string GetTopLoadingNoForDO(int? companyID, int? periodID, int? fiscalID, string DODate, Nullable<int> CustomerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            string topLoadingNo = "0";
            StringBuilder sb = new StringBuilder();

            const string Query = @"
        SELECT TOP 1 LoadingNo, CustomerID
        FROM SAL_DeliveryOrderHeader
        WHERE CAST(DODate AS DATE) = @DODate
          AND LoadingNo IS NOT NULL
          AND CompanyID = @CompanyID
          AND PeriodID = @PeriodID
          AND FiscalID = @FiscalID
          {0}
        ORDER BY LoadingNo DESC";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalID);
                sqlparams[3] = new SqlParameter("@DODate", DODate);
                sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);

                if (CustomerID != null)
                {
                    sb.Append(" AND (CustomerID = @CustomerID OR @CustomerID IS NULL)");
                }

                object result = null;

                if (objTransaction != null)
                    result = SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, String.Format(Query, sb.ToString()), sqlparams);
                else
                    result = SqlHelper.ExecuteScalar(CommandType.Text, String.Format(Query, sb.ToString()), sqlparams);

                if (result != null && result != DBNull.Value)
                {
                    topLoadingNo = result.ToString();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }

            return topLoadingNo;
        }

        public static string GetMaxLoadingNoForDO(int? companyID, int? periodID, int? fiscalID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            string maxLoadingNo = "0";
            StringBuilder sb = new StringBuilder();

            const string Query = @"
        SELECT ISNULL(MAX(LoadingNo), 0) FROM SAL_DeliveryOrderHeader
            WHERE LoadingNo IS NOT NULL
            AND CompanyID = @CompanyID
            AND PeriodID = @PeriodID
            AND FiscalID = @FiscalID";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalID);

                object result = null;


                if (objTransaction != null)
                    result = SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query, sqlparams);
                else
                    result = SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams);

                if (result != null && result != DBNull.Value)
                {
                    maxLoadingNo = result.ToString();
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw;
            }

            return maxLoadingNo;
        }

        #endregion
    }
}
