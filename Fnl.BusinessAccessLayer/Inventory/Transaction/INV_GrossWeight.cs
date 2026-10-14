using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Common.Utils;
using Microsoft.Data.SqlClient;
using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.Inventory.Transaction
{
    public partial class INV_GrossWeight
    {
        #region Constant Strings

        const string Query_MISCELLANEOUSWEIGHTMENTREPORT = @"
Select INV_GrossWeight.GrossWeightID as DocumentNo,cf.Description as SecondShift,
INV_TareWeight.TareWeight as FirstWeight,INV_GrossWeight.GrossWeight as SecondWeight,
INV_TareWeight.ShiftDate as TareDate,INV_GrossWeight.ShiftDate as GrossDate,
INV_TareWeight.ShiftID as TareShiftID,INV_GrossWeight.ShiftID as GrossShiftID,
INV_GrossWeight.Remarks as SecondWeightRemarks,
INV_TareWeight.ItemID,
INV_TareWeight.VehicleNo,
Cane_Shift.Description as FirstShift,
INV_Item.Description as ItemName,
INV_GrossWeight.NetWeight

from INV_TareWeight

inner join INV_GrossWeight on INV_GrossWeight.TareWeightID = INV_TareWeight.TareWeightID
and INV_GrossWeight.CompanyID = INV_TareWeight.CompanyID
and INV_GrossWeight.PeriodID = INV_TareWeight.PeriodID

left join Cane_Shift on Cane_Shift.ShiftHeaderID = INV_TareWeight.ShiftID
and Cane_Shift.CompanyID = INV_TareWeight.CompanyID
and Cane_Shift.PeriodID = INV_TareWeight.PeriodID

left join Cane_Shift cf on cf.ShiftHeaderID = INV_GrossWeight.ShiftID
and cf.CompanyID = INV_GrossWeight.CompanyID
and cf.PeriodID = INV_GrossWeight.PeriodID

left join INV_Item on INV_Item.ItemID = INV_TareWeight.ItemID
--and INV_Item.CompanyID = INV_TareWeight.CompanyID

where INV_TareWeight.CompanyID =@CompanyID and INV_TareWeight.PeriodID =@PeriodID

and Cast(INV_GrossWeight.ShiftDate as Date) between @FromDate and @ToDate
{0}
";

        const string Query_GetAllForSearch_WithPaging = @"SELECT [INV_TareWeight].GatePassNo,[INV_GrossWeight].*
	    , ROW_NUMBER() Over (Order by [INV_GrossWeight].CompanyID, [INV_GrossWeight].GrossWeightID
        , [INV_GrossWeight].PeriodID) as 'Row' FROM [INV_GrossWeight] 
	    JOIN [INV_TareWeight] on [INV_TareWeight].TareWeightID = [INV_GrossWeight].TareWeightID";

        const string Query_GetAllForSearch_WithOutPaging = @"SELECT [INV_TareWeight].GatePassNo,[INV_GrossWeight].*	    
	    JOIN [INV_TareWeight] on [INV_TareWeight].TareWeightID = [INV_GrossWeight].TareWeightID";

//        const string Query_GetDataForHTMLReport = @"SELECT [INV_GrossWeight].GrossWeightID,[INV_TareWeight].TareWeightID,[INV_GrossWeight].GrossWeight,[INV_GrossWeight].NetWeight,
//			    [INV_TareWeight].VehicleNo,[INV_TareWeight].Tareweight,[INV_TareWeight].GatePassNo,
//			    [Set_VehicleType].TypeName
//	    from [INV_GrossWeight]
//	    JOIN [INV_TareWeight] on [INV_TareWeight].TareWeightID = [INV_GrossWeight].TareWeightID
//		    AND [INV_TareWeight].CompanyID = [INV_GrossWeight].CompanyID
//		    AND [INV_TareWeight].PeriodID = [INV_GrossWeight].PeriodID	
//	    JOIN Set_VehicleType on INV_TareWeight.VehicleTypeID = Set_VehicleType.VehicleTypeID
//	
//	    Where [INV_GrossWeight].CompanyID = @CompanyID
//		    AND [INV_GrossWeight].PeriodID = @PeriodID
//		    AND [INV_GrossWeight].GrossWeightID = @GrossWeightID";

        const string Query_GetDataForHTMLReport = @"
        SELECT INV_TareWeight.TareWeight,SET_VehicleType.TypeName,INV_TareWeight.GatePassNo,INV_TareWeight.CreateDate as  ShiftDate
,INV_TareWeight.CustomerID,INV_TareWeight.ItemID,INV_TareWeight.CreatedByValue as TareCreated,
INV_GrossWeight.CreatedByValue as GrossCreated,INV_GrossWeight.CreateDate as GrossShiftDate,
INV_TareWeight.VehicleNo,INV_TareWeight.Remarks,
INV_GrossWeight.ShiftID,AR_SET_Customer.CustomerName,
INV_Item.Description as ItemName,[INV_GrossWeight].GrossWeightID,[INV_TareWeight].TareWeightID,
[INV_GrossWeight].GrossWeight,[INV_GrossWeight].NetWeight
	    from [INV_GrossWeight]
	    JOIN [INV_TareWeight] on [INV_TareWeight].TareWeightID = [INV_GrossWeight].TareWeightID
		    AND [INV_TareWeight].CompanyID = [INV_GrossWeight].CompanyID
		    AND [INV_TareWeight].PeriodID = [INV_GrossWeight].PeriodID	
	   
	    JOIN Set_VehicleType on INV_TareWeight.VehicleTypeID = Set_VehicleType.VehicleTypeID

	left join AR_SET_Customer on AR_SET_Customer.CustomerID = INV_TareWeight.CustomerID
--and AR_SET_Customer.CompanyID = INV_TareWeight.CompanyID
and AR_SET_Customer.PeriodID = INV_TareWeight.PeriodID

left join INV_Item on INV_Item.ItemID = INV_TareWeight.ItemID
--and INV_Item.CompanyID = INV_TareWeight.CompanyID
	    Where [INV_GrossWeight].CompanyID = @CompanyID
		    AND [INV_GrossWeight].PeriodID = @PeriodID
		    AND [INV_GrossWeight].TareWeightID = @TareWeightID  ";

        const string Query_GetDataForWeight = @"
SELECT [INV_GrossWeight].GrossWeightID,[INV_GrossWeight].NetWeight
	    from [INV_GrossWeight]
		where [INV_GrossWeight].Status = 1
		    and [INV_GrossWeight].GrossWeightID = @GrossWeightID
";

        const string Query_GetData_ByGatePassNo = @" SELECT [INV_TareWeight].TareWeightID,Sum([INV_GrossWeight].GrossWeight) GrossWeight,SUM([INV_GrossWeight].NetWeight) NetWeight,
			    [INV_TareWeight].VehicleNo,SUM([INV_TareWeight].Tareweight) Tareweight,[INV_TareWeight].GatePassNo,
			    [Set_VehicleType].TypeName,[Set_VehicleType].VehicleTypeID
	    from [INV_GrossWeight]
	    JOIN [INV_TareWeight] on [INV_TareWeight].TareWeightID = [INV_GrossWeight].TareWeightID
		    AND [INV_TareWeight].CompanyID = [INV_GrossWeight].CompanyID
		    AND [INV_TareWeight].PeriodID = [INV_GrossWeight].PeriodID	
	    JOIN Set_VehicleType on INV_TareWeight.VehicleTypeID = Set_VehicleType.VehicleTypeID
	
	    Where [INV_GrossWeight].CompanyID = @CompanyID
		    AND [INV_GrossWeight].PeriodID = @PeriodID
		    AND [INV_TareWeight].GatePassNo = @GatePassNo
		    
		Group By [INV_TareWeight].TareWeightID,[INV_TareWeight].VehicleNo
		,[INV_TareWeight].GatePassNo,[Set_VehicleType].TypeName
		,[Set_VehicleType].VehicleTypeID
        ";
        #endregion

        #region Class Methods

        public static DataTable GetAllDataForSearch(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_GetAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_GetAllForSearch_WithPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAllForSearch_WithOutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataForHTMLReport(int companyID, int TareWeightID, int periodID, SqlConnection objConnection = null, SqlTransaction SqlTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@TareWeightID", TareWeightID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (SqlTransaction != null)
                    ds = SqlHelper.ExecuteDataset(SqlTransaction, CommandType.Text, Query_GetDataForHTMLReport, sqlparams);
                else if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForHTMLReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForHTMLReport, sqlparams);

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
        public static DataRow GetWeightFromByKeyTable(int grossWeightID, SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@GrossWeightID", grossWeightID);

                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDataForWeight, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDataForWeight, sqlparams);

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

        public static DataTable MISCELLANEOUSWEIGHTMENTREPORT(int companyID, int periodID,int itemidfrom,int itemidTo,int documentNoFrom,int DocumentNoTo, string dateFrom = null, string dateTo = null)
        {
            StringBuilder sb = new StringBuilder();

            DataTable dt = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "" && dateTo != "")
                {
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                    if (itemidfrom > 0 && itemidTo > 0)
                    {
                        sb.Append(" and INV_Item.ItemID between " + itemidfrom + " and " + itemidTo);
                    }

                    if (documentNoFrom > 0 && DocumentNoTo > 0)
                    {
                        sb.Append(" and INV_GrossWeight.GrossWeightID between " + documentNoFrom + " and " + DocumentNoTo);
                    }

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_MISCELLANEOUSWEIGHTMENTREPORT,sb.ToString()), sqlparams).Tables[0];
                }
                else if (dateFrom == "" && dateTo == "")
                    throw new Exception("Enter Date From & Date To. . .!!");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetData_ByGatePassNo(int companyID, long gatePassNo, int periodID, SqlTransaction objTransaction = null,SqlConnection objConnection = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@GatePassNo", gatePassNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;
                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_ByGatePassNo, sqlparams);
                else if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ByGatePassNo, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ByGatePassNo, sqlparams);

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

        public static DataTable Get_AllDataAgainstItemLocation(int companyID, int periodID, int itemID, int? _locationid, SqlConnection objConnection = null, DateTime? date = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                if (_locationid > 0)
                    sqlparams[3] = new SqlParameter("@LocationID", _locationid);
                else
                    sqlparams[3] = new SqlParameter("@LocationID", DBNull.Value);
                #region Query
                string query = @"Select INV_StockTransactionHeader.DocumentDate,INV_StockTransactionDetail.TransactionDetailID  From INV_StockTransactionDetail 
    Inner Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
    Inner Join Set_DocumentType on Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID 
        And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID And Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
     --AND SET_DocumentType.TransactionImpact='Negative'
AND ( SET_DocumentType.TransactionImpact='Negative' OR  SET_DocumentType.DocumentType like'%Dispatch Return%')

Where INV_StockTransactionDetail.CompanyID = @CompanyID And INV_StockTransactionDetail.PeriodID = @PeriodID And INV_StockTransactionDetail.Status = 1 
  And INV_StockTransactionDetail.ItemID = @ItemID  AND INV_StockTransactionHeader.LOcationID=@LocationID
  
  
  Order by DocumentDate asc,TransactionDetailID asc";

                #endregion
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

        public static decimal GetAllGoodReceiptByDateItemID(int companyID, int periodID, int itemID, DateTime DocumentDate, int TransactionDetailID, SqlConnection objConnection = null, DateTime? date = null)
        {
            decimal Rate = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", itemID);
                sqlparams[3] = new SqlParameter("@DocumentDate", DocumentDate);
                sqlparams[4] = new SqlParameter("@TransactionDetailID", TransactionDetailID);
                #region Query
                string query = @" select 
ISNUll(sum(TAB.OpeningBalanceRate),0)/ISNUll(SUM(OpeningBalanceQty),0) AvgRate
,ISNUll(SUM(OpeningBalanceQty),0) OpeningBalanceQty
 FROM (

SELECT INV_Item.Description,

(case when SET_DocumentType.TransactionImpact ='Positive' then  abs(INV_StockTransactionDetail.TransactionQuantity) 
else abs(INV_StockTransactionDetail.TransactionQuantity)*-1 end) AS OpeningBalanceQty, 

((isnull(INV_StockTransactionDetail.transactioncost,0)) * 
(case when SET_DocumentType.TransactionImpact ='Positive' then  
	abs(INV_StockTransactionDetail.TransactionQuantity) 
when SET_DocumentType.TransactionImpact ='Negative' then  abs(INV_StockTransactionDetail.TransactionQuantity) * -1  end)) 
+ isnull(INV_StockTransactionDetail.ValueChange,0) AS OpeningBalanceRate,
INV_StockTransactionDetail.ItemID

,Set_DocumentType.DocumentType
  
FROM INV_StockTransactionDetail 
	INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.StockTransactionID = INV_StockTransactionDetail.StockTransactionID AND INV_StockTransactionHeader.Status = 1 
        AND INV_StockTransactionHeader.IsActive = 1 
    Inner Join Set_DocumentType On Set_DocumentType.DocumentTypeID = Inv_StockTransactionHeader.DocumentTypeID 
        And Set_DocumentType.CompanyID = Inv_StockTransactionHeader.CompanyID And Set_DocumentType.PeriodID = Inv_StockTransactionHeader.PeriodID 
        AND Set_DocumentType.Status = 1 
    INNER JOIN INV_Item ON INV_Item.ItemID = INV_StockTransactionDetail.ItemID 
--AND INV_Item.CompanyID = INV_StockTransactionDetail.CompanyID 
AND INV_Item.Status = 1 
	INNER JOIN INV_SET_Category ON INV_SET_Category.CategoryID = INV_Item.ItemTypeID 
--  
        And (INV_SET_Category.IsInvisibleUponStockReports IS NULL OR INV_SET_Category.IsInvisibleUponStockReports = 0)
   LEFT JOIN SET_Location SL ON SL.LocationID = [INV_StockTransactionDetail].LocationID 
    LEFT JOIN SET_Location ON SET_Location.LocationID = INV_StockTransactionHeader.LocationID 
WHERE INV_StockTransactionDetail.CompanyID = @CompanyID AND INV_StockTransactionDetail.PeriodID = @PeriodID AND INV_StockTransactionDetail.Status = 1 
    AND INV_StockTransactionDetail.IsActive = 1 
    and SET_DocumentType.TransactionImpact <>'No Impact'
    AND (convert(date,INV_StockTransactionHeader.DocumentDate) < @DocumentDate  or INV_StockTransactionDetail.TransactionDetailID < @TransactionDetailID)
	and INV_StockTransactionDetail.itemid = @itemid
    
    AND INV_StockTransactionHeader.StatusCode = 1 


) AS TAB
having ISNUll(SUM(OpeningBalanceQty),0)<>0";

                #endregion
                DataSet ds = null;

                if (objConnection == null)
                    Rate = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, query, sqlparams));
                else
                    Rate = Convert.ToDecimal(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Rate;
        }

        public static bool UpdateIssuanceCost(int TransctionDetailID, decimal avgRate)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];

                sqlparams[0] = new SqlParameter("@TransactionDetailID", TransctionDetailID);
                sqlparams[1] = new SqlParameter("@avgRate", avgRate);

                string Query_UpdateIssunceRate = @"Update INV_StockTransactionDetail Set TransactionCost = @avgRate 
                    Where  TransactionDetailID=@TransactionDetailID";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIssunceRate, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }


        public static DataTable GetData_ForGRN(int companyID, int periodID, int fiscalID, SqlConnection objConnection = null)
        {
            DataTable dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@fiscalID", fiscalID);

                DataSet ds = null;

                const string Query_GetData_ForGRN = @"
select * from INV_GrossWeight where CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 and FiscalID = @fiscalID
and GrossWeightID not in (select isnull(GrossWeightID,0) from INV_StockTransactionHeader where DocumentTypeID in (select DocumentTypeID from SET_DocumentType where SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID and SET_DocumentType.DocumentType like '%GRN%' ) )

";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_ForGRN, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_ForGRN, sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    dr = ds.Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dr;
        }

        #endregion

      
    }
}
