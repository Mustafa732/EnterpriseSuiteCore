using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Utils;
using DAL.DataAccessLayer;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FnI.BusinessAccessLayer.Sales
{
    public partial class SAL_SecondWeight
    {
        const string Query_GetData_byKey_Barcode = @"  SELECT (CASE LEN([SAL_SecondWeight].DocumentNo)
WHEN 1
THEN '00000000000'
    + CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
WHEN 2
THEN '0000000000'
    + CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
WHEN 3
THEN '000000000'
    + CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
WHEN 4
THEN '00000000'
    + CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
WHEN 5
THEN '0000000'
    + CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
ELSE CAST([SAL_SecondWeight].DocumentNo AS VARCHAR)
END ) as Barcode, SAL_SecondWeight.DocumentNo, SAL_SecondWeight.DocumentTypeID
,SET_VehicleType.TypeName AS VehicleName, AR_SET_Customer.CustomerName AS TransporterName, SAL_Token.VehicleNo
,SAL_SecondWeight.SalTokenNo AS TokenNo, SAL_Token.DriverName, SAL_Token.FatherName, SAL_Token.CNICNO
,SAL_Token.DriverMobileNo, SAL_Token.BiltyNo, SAL_Token.BiltyDate, SAL_Token.SaleTokenDate, SAL_Token.SaleTokenTime
,SAL_SecondWeight.SecondWeight, SAL_SecondWeight.FirstWeight, SAL_SecondWeight.NetWeight,
Sum(ABS(INV_StockTransactionDetail.TransactionQuantity)) Quantity,
INV_StockTransactionDetail.TransactionUOMID, SET_UnitofMeasure.UOMName,
SET_DocumentType.DocumentType,SAL_SecondWeight.ShiftDate as SecondWeightTime
,ISNULL(
          (
            SELECT DISTINCT STUFF(
		    (
                SELECT ', ' + tbl_broker.CustomerName
                FROM SAL_DispatchHeader
				LEFT JOIN AR_SET_Customer tbl_broker ON tbl_broker.CustomerID = SAL_DispatchHeader.BrokerID
				AND tbl_broker.PeriodID = SAL_DispatchHeader.PeriodID
				AND tbl_broker.Status = 1
                WHERE payment1.TokenNo = SAL_DispatchHeader.TokenNo AND  payment1.TokenTypeID = SAL_DispatchHeader.TokenTypeID
                AND  payment1.FiscalID = SAL_DispatchHeader.FiscalID
                and SAL_DispatchHeader.Status = 1
                ORDER BY tbl_broker.CustomerName FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS InvoiceDocumentNo
            FROM SAL_DispatchHeader payment1 
            where payment1.TokenNo = SAL_DispatchHeader.TokenNo and payment1.TokenTypeID = SAL_DispatchHeader.TokenTypeID
			and payment1.PeriodID = SAL_DispatchHeader.PeriodID and payment1.CompanyID = SAL_DispatchHeader.CompanyID
            and payment1.fiscalid = SAL_DispatchHeader.fiscalid            
            and payment1.Status = 1
        ), '') as BrokerName

FROM [SAL_SecondWeight] 

left join  SAL_DispatchHeader on SAL_DispatchHeader.TokenNo = [SAL_SecondWeight].SalTokenNo
       And SAL_DispatchHeader.TokenTypeID = [SAL_SecondWeight].SalTokenType
	   And SAL_DispatchHeader.CompanyID = [SAL_SecondWeight].CompanyID
	   And  SAL_DispatchHeader.PeriodID = [SAL_SecondWeight].PeriodID
	   And  SAL_DispatchHeader.FiscalID = [SAL_SecondWeight].CurrentFiscalID
	   And SAL_DispatchHeader.Status = 1

left join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
      And INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
	  And INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
	  And INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
	And INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID
	  And INV_StockTransactionHeader.Status = 1

left Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
      And INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
	  And INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
	  And INV_StockTransactionDetail.FiscalID = INV_StockTransactionHeader.FiscalID 
	  And INV_StockTransactionDetail.Status = 1

left join  SET_UnitofMeasure on SET_UnitofMeasure.UOMID = INV_StockTransactionDetail.TransactionUOMID


INNER JOIN SAL_Token ON SAL_Token.SaleTokenNo = SAL_SecondWeight.SalTokenNo
AND SAL_Token.DocumentTypeID = SAL_SecondWeight.SalTokenType
AND SAL_Token.CompanyID = SAL_SecondWeight.CompanyID
AND SAL_Token.PeriodID = SAL_SecondWeight.PeriodID
AND SAL_Token.CurrentFiscalID = SAL_SecondWeight.CurrentFiscalID
AND SAL_Token.Status = 1
INNER JOIN SET_VehicleType ON SET_VehicleType.VehicleTypeID = [SAL_Token].VehicleTypeID
AND SET_VehicleType.Status = 1
INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = [SAL_Token].TransporterID
AND AR_SET_Customer.Status = 1
INNER JOIN SET_DocumentType on SET_DocumentType.DocumentTypeID = SAL_Token.DocumentTypeID
           and SET_DocumentType.CompanyID = SAL_Token.CompanyID
		   and SET_DocumentType.[PeriodID] = SAL_Token.PeriodID

WHERE [SAL_SecondWeight].[CompanyID] = @CompanyID and [SAL_SecondWeight].[SalesSecondWeightID] = @SalesSecondWeightID
and [SAL_SecondWeight].[PeriodID] = @PeriodID
and [SAL_SecondWeight].[CurrentFiscalID] = @CurrentFiscalID



Group by SAL_SecondWeight.DocumentNo, SAL_SecondWeight.DocumentTypeID
         ,SET_VehicleType.TypeName, AR_SET_Customer.CustomerName, SAL_Token.VehicleNo
         ,SAL_SecondWeight.SalTokenNo, SAL_Token.DriverName, SAL_Token.FatherName, SAL_Token.CNICNO
         ,SAL_Token.DriverMobileNo, SAL_Token.BiltyNo, SAL_Token.BiltyDate, SAL_Token.SaleTokenDate, SAL_Token.SaleTokenTime
        ,SAL_SecondWeight.SecondWeight, SAL_SecondWeight.FirstWeight, SAL_SecondWeight.NetWeight,INV_StockTransactionDetail.TransactionUOMID
		, SET_UnitofMeasure.UOMName,SET_DocumentType.DocumentType,SAL_SecondWeight.ShiftDate,SAL_DispatchHeader.CompanyID
		,SAL_DispatchHeader.PeriodID,SAL_DispatchHeader.TokenNo,SAL_DispatchHeader.TokenTypeID, SAL_DispatchHeader.FiscalID 

";

        const string Query_SelectAll_WithPagingWithTables = @"SELECT [SalesSecondWeightID], [SAL_SecondWeight].[CompanyID], [SAL_SecondWeight].[PeriodID], [SAL_SecondWeight].[DocumentTypeID], [SAL_SecondWeight].[DocumentNo], 
        [SecondWeight], [NetWeight], [SAL_SecondWeight].[Remarks], [SAL_SecondWeight].[Status], [SAL_SecondWeight].[StatusCode], [SAL_SecondWeight].[CreatedBy], [SAL_SecondWeight].[CreatedByValue], [SAL_SecondWeight].[CreateDate], 
        [SAL_SecondWeight].[UpdatedBy], [SAL_SecondWeight].[UpdateDate], [SAL_SecondWeight].[UpdatedByValue], [SalTokenNo], [SalTokenType]
        , ROW_NUMBER() Over (Order by [SAL_SecondWeight].CompanyID, [SAL_SecondWeight].PeriodID, SalesSecondWeightID DESC) as 'Row'
		,SET_DocumentType.DocumentType, SAL_FirstWeight.VehicleNo, SAL_FirstWeight.DriverNo AS DriverName
        FROM [SAL_SecondWeight]
		INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = SAL_SecondWeight.DocumentTypeID
		AND SET_DocumentType.CompanyID = SAL_SecondWeight.CompanyID
		AND SET_DocumentType.PeriodID = SAL_SecondWeight.PeriodID
		AND SET_DocumentType.Status = 1

		INNER JOIN SAL_FirstWeight ON SAL_FirstWeight.TokenID = SAL_SecondWeight.SalTokenNo
		AND SAL_FirstWeight.TokenDocType = SAL_SecondWeight.SalTokenType
		AND SAL_FirstWeight.CompanyID = SAL_SecondWeight.CompanyID
		AND SAL_FirstWeight.PeriodID = SAL_SecondWeight.PeriodID
		AND SAL_FirstWeight.CurrentFiscalID = SAL_SecondWeight.CurrentFiscalID
		AND SAL_FirstWeight.Status = 1"; 

        public static int GetNewDocumentNo(int companyID, int periodID, int fiscalID, int DocumentTypeID, SqlTransaction objTransaction = null, SqlConnection objConnection = null)
        {
            int ID = 0;

            const string Query_GetMaxDocNo = @" Select ISNULL(max(DocumentNo),0)+1 DocumentNo from SAL_SecondWeight 
where CompanyID = @CompanyID and PeriodID= @PeriodID and DocumentTypeID = @DocumentTypeID and CurrentFiscalID =@CurrentFiscalID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", DocumentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);

                if (objTransaction != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else if (objConnection != null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objConnection, CommandType.Text, Query_GetMaxDocNo, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetMaxDocNo, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable CheckForDuplicate(Nullable<int> companyID, Nullable<int> periodID, Nullable<int> CurrentFiscalID, int salTokenNo, int salDocType, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            const string Query = @" Select * from SAL_SecondWeight where SalTokenNo = @SalTokenNo and SalTokenType = @SalTokenType
and SAL_SecondWeight.Status = 1 and [CompanyID] = @CompanyID and PeriodID = @PeriodID  and CurrentFiscalID = @CurrentFiscalID  ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SalTokenNo", salTokenNo);
                sqlparams[3] = new SqlParameter("@SalTokenType", salDocType);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query + sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataTable GetAllDataWithTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
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
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPagingWithTables + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    if (objTransaction != null)
                        dt = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectAll_WithoutPaging + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataRow GetDataByKeyForBarcode(int companyID, int SalesSecondWeightID, int periodID, int fiscalID, int documentTypeID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SalesSecondWeightID", SalesSecondWeightID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", fiscalID);

                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byKey_Barcode, sqlparams);

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



        public static bool UpdateDispatchDataByTokenNO(int CompanyID, int PeriodID, int FiscalID, int TokenNo, decimal TareWeight, decimal GrossWeight, decimal NetWeight, SqlTransaction objTransaction = null)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);
                sqlparams[3] = new SqlParameter("@TokenNo", TokenNo);
                sqlparams[4] = new SqlParameter("@TareWeight", TareWeight);
                sqlparams[5] = new SqlParameter("@GrossWeight", GrossWeight);
                sqlparams[6] = new SqlParameter("@NetWeight", NetWeight);



                const string Query_UpdateDispatchDataByTokenNO = @"
update SAL_DispatchHeader  set TareWeight = @TareWeight, GrossWeight = @GrossWeight, NetWeight = @NetWeight
from SAL_DispatchHeader 
inner join INV_StockTransactionHeader on INV_StockTransactionHeader.DocumentNo = SAL_DispatchHeader.DispatchNo
and INV_StockTransactionHeader.DocumentTypeID = SAL_DispatchHeader.DocumentTypeID
and INV_StockTransactionHeader.CompanyID = SAL_DispatchHeader.CompanyID
and INV_StockTransactionHeader.PeriodID = SAL_DispatchHeader.PeriodID
and INV_StockTransactionHeader.FiscalID = SAL_DispatchHeader.FiscalID

inner join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID
and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID
and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID
and INV_StockTransactionDetail.Status = 1

inner join INV_Item on INV_Item.ItemID = INV_StockTransactionDetail.ItemID

where SAL_DispatchHeader.status = 1 and SAL_DispatchHeader.TokenNo = @TokenNo and SAL_DispatchHeader.CompanyID = @CompanyID
and SAL_DispatchHeader.PeriodID = @PeriodID and SAL_DispatchHeader.FiscalID = @FiscalID and SAL_DispatchHeader.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%sugar%')
 and SAL_DispatchHeader.DocumentTypeID in (Select DocumentTypeID from SET_DocumentType where DocumentType like '%sugar%')  ";
               
                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDispatchDataByTokenNO, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDispatchDataByTokenNO, sqlparams) > 0)
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

    }
}
