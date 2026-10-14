using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

using Common.Utils;
using System.Text;
using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.Inventory.Setup;
using FnI.BusinessAccessLayer.Inventory.Transaction;
using FnI.BusinessAccessLayer.Purchase;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.GL.Transaction;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PurchaseOrderHeader
    {
        #region Class Methods

        public static DataTable getPurchaseOrderDocType(int companyID, int periodID)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_DocumenttypesforPOStatus, sqlparams);

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
        public static DataSet GetShipmentCost(int CompanyID, int PeriodID, int FiscalID, string BLNumber)
        {
            DataSet ds = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                if (BLNumber != "")
                {
                    sb.Append(" and AP_PurchaseOrderHeader.BLNumber like '" + BLNumber.ToString() + "'");
                }
               
                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ShipmentCosting,sb), sqlparams);

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ds;
        }

        public static DataTable GetImportContractDetails(int CompanyID, int PeriodID, int FiscalID, string ContractNumber)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", FiscalID);

                if (ContractNumber != "")
                {
                    sb.Append(" and AP_SaleContractHeader.DocumentNumber = " + ContractNumber.ToString() + "");
                }

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ImportContractDetails, sb), sqlparams).Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetAmountQuantity(int companyID, int periodID, int ItemID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);

                string Query_getAmountQuantity = @" Select   Convert(decimal(18,4),IsNull(Sum(Quantity * UnitPrice), 0)) As Amount
 , Convert(decimal(18,4),IsNull(Sum(IsNull(Quantity, 0)), 0)) As TransactionQuantity 
From AP_PurchaseOrderDetail 
    Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        AND AP_PurchaseOrderHeader.Status = 1 
Where AP_PurchaseOrderDetail.CompanyID = @CompanyID And AP_PurchaseOrderDetail.PeriodID = @PeriodID And AP_PurchaseOrderDetail.Status = 1 
    And AP_PurchaseOrderDetail.ItemID = @ItemID ";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_getAmountQuantity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_getAmountQuantity, sqlparams);

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

        public static int Get_POHeaderForVendorInvoiceInteger(int companyID, int periodID, int? DocumentNo, SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", DocumentNo);

                string Query_GetDocumentNumberINTEGER = @" Select IsNull(DocumentNumber, 0) As DocumentNumber From AP_PurchaseOrderHeader 
Where POHeaderID = @Doc AND AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND AP_PurchaseOrderHeader.Status = 1 ";

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetDocumentNumberINTEGER, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetDocumentNumberINTEGER, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetQuantityOfPurchaseOrder(int companyID, int periodID, int ItemID, int DocumentNo, long StockheaderID, long StockDetailID, 
            SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@Doc", DocumentNo);
                sqlparams[4] = new SqlParameter("@StockheaderID", StockheaderID);
                sqlparams[5] = new SqlParameter("@StockDetailID", StockDetailID);

                string Query_QuantityOfPurchaseOrder = @" Select IsNull(AP_PurchaseOrderDetail.Quantity, 0) As Quantity From AP_PurchaseOrderDetail 
    Inner JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID 
        AND AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.StatusCode = 1 
	Inner Join INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderDetail.Status = 1 
Where AP_PurchaseOrderHeader.DocumentNumber = @Doc and AP_PurchaseOrderDetail.CompanyID = @CompanyID and AP_PurchaseOrderDetail.PeriodID = @PeriodID 
    and AP_PurchaseOrderDetail.Status = 1 and INV_StockTransactionDetail.StockTransactionID = @StockheaderID 
    and INV_StockTransactionDetail.TransactionDetailID = @StockDetailID ";

                if (objTransaction == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_QuantityOfPurchaseOrder, sqlparams);
                }
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction,CommandType.Text, Query_QuantityOfPurchaseOrder, sqlparams);

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

        public static int GetQuantityOfVendorInvoice(int companyID, int periodID, int ItemID, int POHeaderID, SqlTransaction objTransaction = null)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ItemID", ItemID);
                sqlparams[3] = new SqlParameter("@POHeaderID", POHeaderID);

                string Query_QuantityOfVendorInvoice = @"
Select IsNull(Sum(QuantityInvoiced),0) As QuantityInvoiced From AR_InvoiceDetail
Inner Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.POHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID
 		AND AP_PurchaseOrderHeader.CompanyID = AR_InvoiceDetail.CompanyID
		AND AP_PurchaseOrderHeader.PeriodID = AR_InvoiceDetail.PeriodID
		AND AP_PurchaseOrderHeader.Status = 1
		And AP_PurchaseOrderHeader.StatusCode = 1
 Where
 AR_InvoiceDetail.PurchaseOrderHeaderID = @POHeaderID
 and AR_InvoiceDetail.ItemID = @ItemID
 --and AP_PurchaseOrderHeader.DocumentNumber = @Doc
 and AR_InvoiceDetail.CompanyID = @CompanyID
and AR_InvoiceDetail.PeriodID = @PeriodID
and AR_InvoiceDetail.Status = 1";

                if (objTransaction == null)
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_QuantityOfVendorInvoice, sqlparams));
                else
                    ID = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_QuantityOfVendorInvoice, sqlparams));

                //   ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_QuantityOfVendorInvoice, sqlparams));

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static DataTable GetPODetailIDs(int companyID, int periodID, long document)
        {
            DataTable dtToReturn = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);
                sqlparams[2] = new SqlParameter("@Doc", document);

                string Query_DetailPOIdsAgainstHeaderID = @"
                                        Select PODetailID,ClosedCode,* From AP_PurchaseOrderDetail Where POHeaderID = @Doc
                                                                  AND AP_PurchaseOrderDetail.CompanyId = @CompanyID
                                                                  AND AP_PurchaseOrderDetail.PeriodID = @PeriodID
                                                                  AND AP_PurchaseOrderDetail.Status = 1";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DetailPOIdsAgainstHeaderID, sqlparams);

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

        public static bool Get_UpdatePOHeaderClosedCodeForClosing(int companyID, int periodID, long POHeaderID)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", POHeaderID);


                string Query_UpdatePOHeaderClosedCodeForClosing = @"Update AP_PurchaseOrderHeader
                                                    Set ClosedCode = 4,
                                                    ClosedDate = GETDATE()
                                                    Where 
                                                    AP_PurchaseOrderHeader.POHeaderID = @POHeaderID
                                                    AND AP_PurchaseOrderHeader.CompanyID = @CompanyID
                                                    And AP_PurchaseOrderHeader.PeriodID = @PeriodID";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOHeaderClosedCodeForClosing, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_UpdatePOHeaderClosedCodeForCloseForInvoice(int companyID, int periodID, long POHeaderID, SqlTransaction objTransaction = null)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", POHeaderID);


                string Query_UpdatePOHeaderClosedCodeForCloseForInvoice = @"Update AP_PurchaseOrderHeader
                                                    Set ClosedCode = 2,
                                                    ClosedDate = GETDATE()
                                                    Where 
                                                    AP_PurchaseOrderHeader.POHeaderID = @POHeaderID
                                                    AND AP_PurchaseOrderHeader.CompanyID = @CompanyID
                                                    And AP_PurchaseOrderHeader.PeriodID = @PeriodID";

                //if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOHeaderClosedCodeForCloseForInvoice, sqlparams) > 0)
                  //  result = true;
                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOHeaderClosedCodeForCloseForInvoice, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdatePOHeaderClosedCodeForCloseForInvoice, sqlparams);


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

        public static bool Get_UpdatePODetailClosedCodeForClosing(int companyID, int periodID, long doc, Guid UserID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", doc);
                sqlparams[3] = new SqlParameter("@UserID", UserID);

                string Query_UpdatePODetailClosedCodeForClosing = @" update AP_PurchaseOrderDetail 
Set AP_PurchaseOrderDetail.ClosedBy = @UserID, AP_PurchaseOrderDetail.ClosedDate = getdate(), AP_PurchaseOrderDetail.ClosedCode = 4 
From AR_InvoiceDetail 
    Inner JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
    Inner Join INV_StockTransactionDetail ON AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        AND AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID AND AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND INV_StockTransactionDetail.Status = 1 
    Inner Join AP_PurchaseOrderDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID AND
	INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID AND
	INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID AND
	INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND
	AP_PurchaseOrderDetail.Status = 1
Where AR_InvoiceHeader.InvoiceNumber = @Doc
AND AR_InvoiceHeader.CompanyID = @CompanyID
and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.Status = 1



";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePODetailClosedCodeForClosing, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool Get_UpdatePODetailClosedCodeForClosingEdit(int companyID, int periodID, long doc, Guid UserID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@Doc", doc);
                sqlparams[3] = new SqlParameter("@UserID", UserID);

                string Query_UpdatePODetailClosedCodeForClosingEdit = @" update AP_PurchaseOrderDetail 
Set AP_PurchaseOrderDetail.ClosedBy = @UserID, AP_PurchaseOrderDetail.ClosedDate = getdate(), AP_PurchaseOrderDetail.ClosedCode = 4 
From AR_InvoiceDetail 
    Inner JOIN AR_InvoiceHeader ON AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID AND AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        AND AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID AND AR_InvoiceHeader.Status = 1 
    Inner Join INV_StockTransactionDetail ON AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        AND AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID AND AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        AND AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID AND INV_StockTransactionDetail.Status = 1 
    Inner Join AP_PurchaseOrderDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID AND
	INV_StockTransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID AND
	INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID AND
	INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND
	AP_PurchaseOrderDetail.Status = 1
Where AR_InvoiceHeader.InvoiceID = @Doc
AND AR_InvoiceHeader.CompanyID = @CompanyID
and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.Status = 1



";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePODetailClosedCodeForClosingEdit, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public bool Get_UpdatePOForClosedTransactions(int companyID, int periodID, int POHeaderID, int ClosedCode, DateTime? ClosedDate, string Comments, 
            Guid UpdatedBy, DateTime UpdateDate, string UpdatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[3] = new SqlParameter("@ClosedCode", ClosedCode);
                sqlparams[4] = new SqlParameter("@ClosedDate", ClosedDate);
                sqlparams[5] = new SqlParameter("@Comments", Comments);
                sqlparams[6] = new SqlParameter("@UpdatedBy", UpdatedBy);
                sqlparams[7] = new SqlParameter("@UpdateDate", UpdateDate);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", UpdatedByValue);

                string Query_UpdatePOForClosedTransactions = @" UPDATE AP_PurchaseOrderHeader 
SET ClosedCode = @ClosedCode, ClosedDate = @ClosedDate, Comments = @Comments, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and POHeaderID = @POHeaderID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOForClosedTransactions, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable GetDashBoard_PendingPOs(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null, 
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = "";


            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query 

                string Query_DashBoard_PendingPOs = @" 
Select POH.DocumentNumber As PoNumber, POH.TotalAmount, POH.PODueDate, POH.comments, AR_SET_Customer.CustomerName As SupplierName, 
    ROW_NUMBER() Over (Order by POH.PODueDate Desc) as 'Row' 
from AP_PurchaseOrderHeader POH 
	Inner join AR_SET_Customer on AR_SET_Customer.CustomerID = POH.VendorID --AND AR_SET_Customer.CompanyID = POH.CompanyID 
    AND AR_SET_Customer.PeriodID = POH.PeriodID 
        AND AR_SET_Customer.Status = 1 
Where POH.CompanyID = @CompanyID AND POH.PeriodID = @PeriodID AND POH.Status = 1 And POH.StatusCode = 1 
    and (POH.ClosedCode is null or POH.ClosedCode <> 1) And not exists 
    (
        Select * from INV_StockTransactionDetail 
        Where INV_StockTransactionDetail.SourceCode = POH.DocumentNumber AND INV_StockTransactionDetail.CompanyID =  @CompanyID 
            AND INV_StockTransactionDetail.PeriodID =  @PeriodID And INV_StockTransactionDetail.Status = 1
            and INV_StockTransactionDetail.SourceHeaderID = POH.POHeaderID
			and INV_StockTransactionDetail.SourceDocumentTypeID = POH.TypeCode
    )
--AND POH.PODueDate between GETDATE() and GETDATE() + 15 
";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDashBoard_PendingPOs_New(string CompanyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null,
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = "";

            CompanyID = "(" + CompanyID + ")";

            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                //sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_PendingPOs = @" 
 select 
 DocumentNumber As PoNumber, (Quantity - RcvQty) * TAB.UnitPrice TotalAmount, PODueDate, comments, CustomerName As SupplierName, 
    ROW_NUMBER() Over (Order by PODueDate Desc) as 'Row' 
  from 
(
    SELECT AP_PurchaseOrderHeader.DocumentNumber, AP_PurchaseOrderHeader.POHeaderID, AP_PurchaseOrderHeader.DocumentDate, AR_SET_Customer.CustomerName, 
        AP_PurchaseOrderHeader.TotalAmount, SET_UnitofMeasure.UOMName,INV_Item.InventoryItemStatusCode as ItemCode, INV_Item.Description, AP_PurchaseOrderDetail.Quantity, isnull(
            (
                Select abs(SUM(isnull(sd.TransactionQuantity,0))) from INV_StockTransactionDetail sd 
                where AP_PurchaseOrderDetail.POHeaderID = sd.SourceHeaderID and AP_PurchaseOrderDetail.PODetailID = sd.SourceLineID 
                    and AP_PurchaseOrderHeader.CompanyID = sd.CompanyID and AP_PurchaseOrderHeader.PeriodID = sd.PeriodID AND sd.Status = 1
                    and sd.SourceDocumentTypeID =SET_DocumentType.DocumentTypeID
            ), 0) as RcvQty, AP_PurchaseOrderDetail.UnitPrice 
			,AP_PurchaseOrderHeader.PODueDate,AP_PurchaseOrderHeader.comments
    FROM AP_PurchaseOrderDetail 
        INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID AND AP_PurchaseOrderHeader.Status = 1
            AND AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
        INNER JOIN INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1    --AP_PurchaseOrderDetail.CompanyID = INV_Item.CompanyID AND 
        LEFT OUTER JOIN AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID --and AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
            AND AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID AND AR_SET_Customer.Status = 1
        LEFT OUTER JOIN SET_UnitofMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
        --inner join Set_PeriodCustom on AP_PurchaseOrderHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
        --and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = 6
    WHERE AP_PurchaseOrderDetail.CompanyID  IN @CompanyID AND AP_PurchaseOrderDetail.PeriodID =  @PeriodID  AND AP_PurchaseOrderDetail.Status = 1 
           and ISNULL(AP_PurchaseOrderHeader.ClosedCode,0) <> 1  

) as TAB 
 Where TAB.RcvQty < TAB.Quantity 

";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs.Replace("@CompanyID", CompanyID)) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs.Replace("@CompanyID", CompanyID)) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs.Replace("@CompanyID", CompanyID)) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs.Replace("@CompanyID", CompanyID)) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDashBoard_ExpiredPOs(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null, 
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = "";

            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_ExpiredPOs = @"
Select AP_PurchaseOrderHeader.DocumentNumber As PoNumber, AP_PurchaseOrderHeader.TotalAmount, AP_PurchaseOrderHeader.PODueDate, AP_PurchaseOrderHeader.comments, 
    AR_SET_Customer.CustomerName As SupplierName, ROW_NUMBER() Over (Order by AP_PurchaseOrderHeader.PODueDate desc) as 'Row' 
from AP_PurchaseOrderHeader 
    Inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID --AND AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AR_SET_Customer.Status = 1
Where AP_PurchaseOrderHeader.PODueDate < CONVERT (date, GETDATE()) AND AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.PeriodID = @PeriodID 
    AND AP_PurchaseOrderHeader.Status = 1 And AP_PurchaseOrderHeader.StatusCode = 1 
    and (AP_PurchaseOrderHeader.ClosedCode is null or AP_PurchaseOrderHeader.ClosedCode <> 1) ";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_ExpiredPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_ExpiredPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_ExpiredPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_ExpiredPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDashBoard_PendingRFQs(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null, 
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = "";

            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_PendingRFQs = @" 
select EnquiryHeaderID AS RFQNo, AR_SET_Customer.CustomerName As Supplier, ReferenceNumber as RefNo, DeliveryDate, 
    ROW_NUMBER() Over (Order by DeliveryDate desc) as 'Row' 
from PUR_QuotationHeader 
    Left Join AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.FromHeaderID = PUR_QuotationHeader.EnquiryHeaderID 
        and AP_PurchaseOrderHeader.CompanyID = PUR_QuotationHeader.CompanyID and AP_PurchaseOrderHeader.PeriodID = PUR_QuotationHeader.PeriodID 
        and AP_PurchaseOrderHeader.Status = 1 
    Inner Join AR_SET_Customer ON AR_SET_Customer.CustomerID = PUR_QuotationHeader.SupplierID --and AR_SET_Customer.CompanyID = PUR_QuotationHeader.CompanyID 
        and AR_SET_Customer.PeriodID = PUR_QuotationHeader.PeriodID and AR_SET_Customer.Status = 1 
where AP_PurchaseOrderHeader.FromHeaderID is null and PUR_QuotationHeader.CompanyID = @CompanyID and PUR_QuotationHeader.PeriodID = @PeriodID 
    and PUR_QuotationHeader.Status = 1 ";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingRFQs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingRFQs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingRFQs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingRFQs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDashBoard_LastestPOs(int companyID, int periodID, SqlConnection objSQLConnection = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = new DataSet();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                #region Query

                string Query_DashBoard_LatestPOs = @" 
Select AP_PurchaseOrderHeader.DocumentNumber As PoNumber, AP_PurchaseOrderHeader.TotalAmount, AP_PurchaseOrderHeader.PODueDate, AP_PurchaseOrderHeader.comments, 
    AR_SET_Customer.CustomerName As SupplierName, ROW_NUMBER() Over (Order by AP_PurchaseOrderHeader.PODueDate Desc) as 'Row' 
from AP_PurchaseOrderHeader 
	Inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID --AND AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AR_SET_Customer.Status = 1 
    Inner Join INV_StockTransactionDetail ON INV_StockTransactionDetail.SourceHeaderID = AP_PurchaseOrderHeader.POHeaderID 
        AND INV_StockTransactionDetail.SourceCode = AP_PurchaseOrderHeader.DocumentNumber AND INV_StockTransactionDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND INV_StockTransactionDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND INV_StockTransactionDetail.Status = 1 
	Inner Join INV_StockTransactionHeader ON  INV_StockTransactionHeader.StockTransactionID  = INV_StockTransactionDetail.StockTransactionID 
        AND INV_StockTransactionHeader.CompanyID = INV_StockTransactionDetail.CompanyID AND INV_StockTransactionHeader.PeriodID = INV_StockTransactionDetail.PeriodID 
        AND INV_StockTransactionHeader.Status = 1 
Where AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND AP_PurchaseOrderHeader.Status = 1 
    And AP_PurchaseOrderHeader.StatusCode = 1 and INV_StockTransactionHeader.Statuscode <> 1 
    and (AP_PurchaseOrderHeader.ClosedCode <> 3 or INV_StockTransactionHeader.Statuscode <> 1 or AP_PurchaseOrderHeader.ClosedCode is null) 
Group By AP_PurchaseOrderHeader.DocumentNumber, AP_PurchaseOrderHeader.TotalAmount, AP_PurchaseOrderHeader.PODueDate, AP_PurchaseOrderHeader.comments, 
    AR_SET_Customer.CustomerName, AP_PurchaseOrderHeader.CompanyID 
Having COUNT(INV_StockTransactionDetail.CompanyID) > 0 
Order by PODueDate desc ";

                #endregion

                if (objSQLConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DashBoard_LatestPOs, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, Query_DashBoard_LatestPOs, sqlparams);

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

        public DataTable get_PurchaseDocNo_ByID(string docNo, int periodID, int companyID, int typeCode)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@docNo", docNo);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);
                sqlparams[3] = new SqlParameter("@TypeCode", typeCode);

                string query_Select_PurchaseOrderNo_byId = @" select DocumentNumber from AP_PurchaseOrderHeader 
where AP_PurchaseOrderHeader.DocumentNumber = @docNo and AP_PurchaseOrderHeader.TypeCode = @TypeCode and AP_PurchaseOrderHeader.PeriodID = @PeriodID 
    AND AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.Status = 1; ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, query_Select_PurchaseOrderNo_byId, sqlparams);

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

        public static DataTable POLookUPDataForPOStatus(int companyID, int periodID , string query)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                
                DataSet ds = null;

                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);

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

        public static DataTable Get_Data_By_DocumentType_And_Number(int companyID, int periodID, int typeCode, string documentNo, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);

                string Query_Select_By_DoucmentType_And_Number = @" SELECT * FROM AP_PurchaseOrderHeader 
WHERE AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND AP_PurchaseOrderHeader.TypeCode = @TypeCode 
    AND AP_PurchaseOrderHeader.DocumentNumber = @DocumentNo AND AP_PurchaseOrderHeader.Status = 1 AND AP_PurchaseOrderHeader.IsActive = 1 ";

                if(objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Select_By_DoucmentType_And_Number, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DoucmentType_And_Number, sqlparams);

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

        public static DataTable Get_Data_By_DocumentType_And_Number_And_Fiscal(int companyID, int periodID, int fiscalId, int typeCode, string documentNo, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@TypeCode", typeCode);
                sqlparams[3] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[4] = new SqlParameter("@FiscalID", fiscalId);

                string Query_Select_By_DoucmentType_And_Number = @" SELECT * FROM AP_PurchaseOrderHeader 
WHERE AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND AP_PurchaseOrderHeader.TypeCode = @TypeCode 
    AND AP_PurchaseOrderHeader.DocumentNumber = @DocumentNo AND AP_PurchaseOrderHeader.Status = 1 AND AP_PurchaseOrderHeader.IsActive = 1 And FiscalID = @FiscalID";

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_Select_By_DoucmentType_And_Number, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_DoucmentType_And_Number, sqlparams);

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

        public DataTable GetDataByKeyWithOtherTables_AndWithoutClosedCode(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, int periodID,
            int companyID, string appendedFilter = "", int? docTypeID = null, string outerApplyInvoice = "", Nullable<int> IsImportPO = null)
        {
            DataTable dtToReturn = null;
            StringBuilder sbImport = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                if (docTypeID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocTypeID", docTypeID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocTypeID", DBNull.Value);

                if (IsImportPO.HasValue)
                    sbImport.Append(" and SET_DocumentType.isimport = 1");
                else
                    sbImport.Append(" and isnull(SET_DocumentType.isimport,0) = 0");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_Select_By_PrimaryKey_WithOtherTables_AndWithoutClosedCode,
                    outerApplyInvoice, sbImport) + " and isnull(AP_PurchaseOrderHeader.ClosedCode,0) != 1  " + appendedFilter, sqlparams);

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

        public DataTable GetDataByKeyWithOtherTablesForAdvance_AndWithoutClosedCode(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, 
            int periodID, int companyID, string appendedFilter = "", int? docTypeID = null)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                if (docTypeID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocTypeID", docTypeID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocTypeID", DBNull.Value);

                #region Query


                #endregion

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_Select_By_PrimaryKey_WithOtherTablesForAdvance_AndWithoutClosedCode + appendedFilter, 
                    sqlparams);

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

        public DataTable GetDataByKeyWithOtherTables(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, int periodID, int companyID,string AllowedPeriods, 
            string appendedFilter = "", SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                 string Query_Select_By_PrimaryKey_WithOtherTables = @"";
                if (AllowedPeriods != "")
                   Query_Select_By_PrimaryKey_WithOtherTables = FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_Select_By_PrimaryKey_WithOtherTables.Replace("@PeriodID", AllowedPeriods);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_Select_By_PrimaryKey_WithOtherTables + appendedFilter, sqlparams);

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

        public static DataTable GetAllDataWithOtherTablesforPOStatus(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc,int CurrentFiscalID = 0)
        {
            #region Queries 
           
            #endregion
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.QuerywithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.QuerywithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.QuerySelectAll + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc,int CurrentFiscalID=0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

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

                    #region Query

                    string Query_SelectAll_Searching_WithPaging = @" select *, ROW_NUMBER() OVER (ORDER BY AP_PurchaseOrderHeader.POHeaderID desc) AS 'Row' from 
(
    SELECT DISTINCT poh.POHeaderID, poh.CompanyID, poh.PeriodID, poh.Status, poh.TypeCode, SET_DocumentType.DocumentType, poh.DocumentNumber, poh.DocumentDate, 
        ISNULL(poh.VendorID, '-1') as VendorID, AR_SET_Customer.CustomerName as SupplierName, poh.AcceptanceDueDate, 
        poh.CreatedByValue, 
        poh.StatusCode,Set_PeriodCustom.PeriodSetName,
				Requisition.DocumentNo, INV_StockTransactionHeader.DocumentNo as DRequisitionNo,
                Case When poh.StatusCode <> 5 Then
				ISNULL((SELECT TOP 1 CreatedByValue FROM SET_DocumentApprove WHERE DocumentTypeID = poh.TypeCode AND DocumentNo = poh.DocumentNumber
				AND CompanyID = poh.CompanyID AND PeriodID = poh.PeriodID AND FiscalID = poh.FiscalID ORDER BY DocumentApproveID DESC
				),
				ISNULL(poh.UpdatedByValue,poh.CreatedByValue)
				)
				else
				'' End AS lastApproval 
    FROM AP_PurchaseOrderHeader poh 
        INNER JOIN AP_PurchaseOrderDetail pod ON pod.POHeaderID = poh.POHeaderID And poh.status = 1 and pod.Status = 1 AND pod.CompanyID = poh.CompanyID 
            AND pod.PeriodID = poh.PeriodID 
inner join Set_PeriodCustom on poh.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = poh.CompanyID AND SET_DocumentType.PeriodID = poh.PeriodID 
            AND SET_DocumentType.DocumentTypeID = poh.TypeCode 
        LEFT JOIN AR_SET_Customer ON AR_SET_Customer.Customerid = poh.VendorID and AR_SET_Customer.Status = 1 -- AND AR_SET_Customer.CompanyID = poh.CompanyID 
            AND AR_SET_Customer.PeriodID = poh.PeriodID 
        left JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = poh.CompanyID AND SET_DocumentApprove.PeriodID = poh.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = poh.TypeCode AND SET_DocumentApprove.DocumentNo = poh.DocumentNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = poh.CompanyID AND PeriodID = poh.PeriodID AND DocumentTypeID = poh.TypeCode AND DocumentNo = poh.DocumentNumber
            )
			  LEFT JOIN PUR_QuotationHeader ON PUR_QuotationHeader.CompanyID = poh.CompanyID 
				AND PUR_QuotationHeader.FiscalID = poh.FiscalID
        AND PUR_QuotationHeader.PeriodID = poh.PeriodID AND PUR_QuotationHeader.DocumentNo = poh.VendorQuoteNumber 
    
	LEFT JOIN PUR_EnquiryHeader ON PUR_EnquiryHeader.CompanyID = PUR_QuotationHeader.CompanyID AND PUR_EnquiryHeader.PeriodID = PUR_QuotationHeader.PeriodID 
	AND PUR_EnquiryHeader.FiscalID = PUR_QuotationHeader.FiscalID
        AND PUR_EnquiryHeader.EnquiryID = PUR_QuotationHeader.EnquiryHeaderID 

			    LEFT JOIN [INV_StockTransactionHeader] ON [INV_StockTransactionHeader].[StockTransactionID] = poh.[StockRequisitionID]
		AND [INV_StockTransactionHeader].[CompanyID] = poh.[CompanyID]
		--AND [INV_StockTransactionHeader].[FiscalID] = poh.[FiscalID] comment becous not showing Req no prv fiscal year
		AND [INV_StockTransactionHeader].[PeriodID] = poh.[PeriodID]

		    --left JOIN [INV_StockTransactionHeader] as Requisition ON PUR_EnquiryHeader.CompanyID = Requisition.CompanyID 
        --AND PUR_EnquiryHeader.PeriodID = Requisition.PeriodID 
		--AND PUR_EnquiryHeader.FiscalID = Requisition.FiscalID 
        --AND PUR_EnquiryHeader.FK_INV_StockTransactionID = Requisition.StockTransactionID 

	left join AP_PurchaseOrderRequisition as Requisition on Requisition.POHeaderID = pod.POHeaderID
	and Requisition.PODetailID = pod.PODetailID
	and Requisition.CompanyID = pod.CompanyID
	and Requisition.PeriodID = pod.PeriodID
	and Requisition.FiscalID = pod.FiscalID
	--and Requisition.status =1 

) AP_PurchaseOrderHeader   ";

                    #endregion

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching + strWhere 
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetDataNotExitsInGRN(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
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

                    #region Query

                    string Query_NotExitsInGRN_Searching_WithPaging = @" select *, ROW_NUMBER() OVER (ORDER BY AP_PurchaseOrderHeader.DocumentNumber desc) AS 'Row' 
from 
(
    SELECT DISTINCT poh.POHeaderID, poh.CompanyID, poh.PeriodID, poh.Status, poh.TypeCode, SET_DocumentType.DocumentType, poh.DocumentNumber, poh.DocumentDate, 
        ISNULL(poh.VendorID, '-1') as VendorID, AR_SET_Customer.CustomerName as SupplierName, poh.AcceptanceDueDate, SET_DocumentApprove.CreatedByValue, 
        poh.StatusCode 
    FROM AP_PurchaseOrderHeader poh 
        INNER JOIN AP_PurchaseOrderDetail pod ON pod.POHeaderID = poh.POHeaderID And poh.status = 1 and pod.Status = 1 AND pod.CompanyID = poh.CompanyID 
            AND pod.PeriodID = poh.PeriodID 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = poh.CompanyID AND SET_DocumentType.PeriodID = poh.PeriodID 
            AND SET_DocumentType.DocumentTypeID = poh.TypeCode 
        LEFT JOIN AR_SET_Customer ON AR_SET_Customer.Customerid = poh.VendorID and AR_SET_Customer.Status = 1 --AND AR_SET_Customer.CompanyID = poh.CompanyID 
            AND AR_SET_Customer.PeriodID = poh.PeriodID 
        left JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = poh.CompanyID AND SET_DocumentApprove.PeriodID = poh.PeriodID 
            AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = poh.TypeCode AND SET_DocumentApprove.DocumentNo = poh.DocumentNumber 
            AND SET_DocumentApprove.CreateDate = 
            (
                SELECT MAX(CreateDate) FROM SET_DocumentApprove 
                WHERE CompanyID = poh.CompanyID AND PeriodID = poh.PeriodID AND DocumentTypeID = poh.TypeCode AND DocumentNo = poh.DocumentNumber
            ) 
    where poh.AcceptanceRequiredFlag = 0 AND poh.UserHoldFlag = 0 AND poh.ApprovalRequiredFlag = 0 AND poh.CancelFlag = 0 AND poh.FrozenFlag = 0 
        AND (poh.ClosedCode is null Or poh.ClosedCode = 13)
) AP_PurchaseOrderHeader ";

                    #endregion

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_NotExitsInGRN_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), Query_NotExitsInGRN_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null, 
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc,int CurrentFiscalID=0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_WithPaging_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {

                    dt = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_ForApprove + strWhere + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""),
                        sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int pOHeaderID, int documentTypeID, string documentNo, 
            SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_ForApproveDetail, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_ForApproveDetail, sqlparams);

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

        public DataTable GetAllDataWithOtherTablesForApproveDetailForConnect(int companyID, int periodID, int pOHeaderID, int documentTypeID, string documentNo,
            SqlConnection objConnection = null)
        {
            DataSet ds = null;
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_ForApproveDetailForConnect, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_ForApproveDetailForConnect, sqlparams);

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

        public static DataTable GetDataByKeyForApprove(int companyID, int periodID, int pOHeaderID, int documentTypeID, string documentNo, 
            SqlConnection objConnection = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.query_Select_DataByKey_ForApprove, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.query_Select_DataByKey_ForApprove, sqlparams);

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

        public bool InsertPOHeaderAndDetail(List<AP_PurchaseOrderRequisition> lst_PORequisition, List<AP_PurchaseOrderDetail> colPODetail,
            List<AP_POScheduling> colPOScheduling, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans,CurrentFiscalID).ToString();
                this.Insert(sqltrans);

                foreach (AP_PurchaseOrderDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKey(CompanyID, pod.ItemID, null, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = AP_PurchaseOrderHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));
                        amount = Math.Round(amount);

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.PODetailID;
                    pod.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.Insert(sqltrans);

                    #region Insert RFQ Requisitions

                    if (lst_PORequisition != null)
                    {
                        var Records = from objPurRequisition in lst_PORequisition
                                      where objPurRequisition.RowUniqueID == pod.RowUniqueID
                                      select objPurRequisition;

                        foreach (AP_PurchaseOrderRequisition er in Records)
                        {
                            er.POHeaderID = this.POHeaderID;
                            er.PODetailID = pod.PODetailID;
                            er.Insert(sqltrans);

                            #region Update IsRequisition Completed

                            if (this.IsDirectPO.HasValue && this.IsDirectPO.Value)
                            {
                                INV_StockTransactionHeader objRequisition = new INV_StockTransactionHeader(er.CompanyID, er.PeriodID, er.RequisitionHeaderID, null, 
                                    sqltrans);

                                if (objRequisition.IsDataLoaded)
                                {
                                    bool isRequisitionCompleted = false;
                                    DataTable dtRequisitionDetails = INV_StockTransactionDetail.GetDataByTransactionHeaderID(objRequisition.StockTransactionID,
                                        objRequisition.CompanyID, objRequisition.PeriodID, true, sqltrans);

                                    foreach (DataRow drRequisitionDetail in dtRequisitionDetails.Rows)
                                    {
                                        decimal requisitionQuantity = drRequisitionDetail.Field<decimal>("TransactionQuantity");
                                        decimal ginQuantity = INV_StockTransactionHeader.GetGINQuantityAgainstRequisition(er.CompanyID, er.PeriodID,
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID,
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal rfqQuantity = PUR_EnquiryHeader.GetRFQQuantityAgainstRequisition(er.CompanyID, er.PeriodID, 
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID, 
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal directPOQuantity = GetDirectPOQuantityAgainstRequisition(er.CompanyID, er.PeriodID,
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID,
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal balanceQuantity = requisitionQuantity - ginQuantity - rfqQuantity - directPOQuantity;

                                        if (balanceQuantity > 0)
                                        {
                                            isRequisitionCompleted = false;
                                            break;
                                        }
                                        else
                                            isRequisitionCompleted = true;
                                    }

                                    if (isRequisitionCompleted)
                                        INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.RequisitionHeaderID, true, sqltrans);
                                }
                            }

                            #endregion
                        }
                    }

                    #endregion

                    #region Insert in AP_POScheduling table

                    if (colPOScheduling != null && colPOScheduling.Count > 0)
                    {
                        var colPOS = (from drow in colPOScheduling
                                      where drow.RowUniqueID == pod.RowUniqueID
                                      select drow);

                        foreach (AP_POScheduling pos in colPOS)
                        {
                            pos.PODetailID = pod.PODetailID;
                            pos.POHeaderID = this.POHeaderID;

                            pos.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Checking For Purchase Order Complete Status

                    if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
                }

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }

        public bool InsertPOHeaderAndDetail(List<AP_PurchaseOrderRequisition> lst_PORequisition, List<AP_PurchaseOrderDetail> colPODetail,
            List<AP_POScheduling> colPOScheduling, List<AP_POPaymentSchedule> colPaymentScheduling, SET_DocumentApprove da, bool IsPerfoance = true, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (AP_PurchaseOrderDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKeyTransaction(CompanyID, pod.ItemID, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = AP_PurchaseOrderHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.PODetailID;
                    pod.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.FiscalID = CurrentFiscalID;
                    pod.Insert(sqltrans);
                    //pod.InsertAlhaj(sqltrans);
                    
                    #region Insert RFQ Requisitions

                    if (lst_PORequisition != null)
                    {
                        var Records = from objPurRequisition in lst_PORequisition
                                      where objPurRequisition.RowUniqueID == pod.RowUniqueID
                                      select objPurRequisition;

                        foreach (AP_PurchaseOrderRequisition er in Records)
                        {
                            er.POHeaderID = this.POHeaderID;
                            er.PODetailID = pod.PODetailID;
                            er.FiscalID = CurrentFiscalID;
                            er.Insert(sqltrans);

                            #region Update IsRequisition Completed

                            if (this.IsDirectPO.HasValue && this.IsDirectPO.Value)
                            {
                                INV_StockTransactionHeader objRequisition = new INV_StockTransactionHeader(er.CompanyID, er.PeriodID, er.RequisitionHeaderID, null,
                                    sqltrans);

                                if (objRequisition.IsDataLoaded)
                                {
                                    bool isRequisitionCompleted = false;
                                    DataTable dtRequisitionDetails = INV_StockTransactionDetail.GetDataByTransactionHeaderID(objRequisition.StockTransactionID,
                                        objRequisition.CompanyID, objRequisition.PeriodID, true, sqltrans);


                                    foreach (DataRow drRequisitionDetail in dtRequisitionDetails.Rows)
                                    {
                                        decimal requisitionQuantity = drRequisitionDetail.Field<decimal>("TransactionQuantity");
                                        decimal ginQuantity = INV_StockTransactionHeader.GetGINQuantityAgainstRequisition(er.CompanyID, er.PeriodID,
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID,
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal rfqQuantity = PUR_EnquiryHeader.GetRFQQuantityAgainstRequisition(er.CompanyID, er.PeriodID,
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID,
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal directPOQuantity = GetDirectPOQuantityAgainstRequisition(er.CompanyID, er.PeriodID,
                                            objRequisition.StockTransactionID, drRequisitionDetail.Field<long>("TransactionDetailID"), objRequisition.DocumentTypeID,
                                            objRequisition.DocumentNo, sqltrans);

                                        decimal balanceQuantity = requisitionQuantity - ginQuantity - rfqQuantity - directPOQuantity;

                                        if (balanceQuantity > 0)
                                        {
                                            isRequisitionCompleted = false;
                                            break;
                                        }
                                        else
                                        {
                                            isRequisitionCompleted = true;
                                        }
                                        break;
                                    }
                                    //code comment as per Ali Jaseem requirement 
                                    //if (isRequisitionCompleted)
                                    //    INV_StockTransactionHeader.UpdateIsRequestedCompleted(er.CompanyID, er.PeriodID, er.RequisitionHeaderID, true, sqltrans);
                                }
                            }

                            #endregion
                        }
                    }

                    #endregion

                    #region Insert in AP_POScheduling table

                    if (colPOScheduling != null && colPOScheduling.Count > 0)
                    {
                        var colPOS = (from drow in colPOScheduling
                                      where drow.RowUniqueID == pod.RowUniqueID
                                      select drow);

                        foreach (AP_POScheduling pos in colPOS)
                        {
                            pos.PODetailID = pod.PODetailID;
                            pos.POHeaderID = this.POHeaderID;
                            
                            pos.Insert(sqltrans);
                        }
                    }

                    #endregion


                    #region Checking For Purchase Order Complete Status

                    if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
                }

                #region Insert in AP_POPaymentSchedule table

                if (colPaymentScheduling != null && colPaymentScheduling.Count > 0)
                {
                    foreach (AP_POPaymentSchedule ps in colPaymentScheduling)
                    {
                        ps.PODetailID = short.Parse(AP_POPaymentSchedule.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                        ps.POHeaderID = this.POHeaderID;
                        ps.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.FiscalID = CurrentFiscalID;
                    da.DocumentNo = this.DocumentNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }

        public SqlTransaction InsertPOHeaderAndDetailPTIS(List<AP_PurchaseOrderDetail> colPODetail,bool IsPerfoance = true, int CurrentFiscalID = 0,SqlTransaction sql=null)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = sql; //SqlHelper.GetTransaction("InsertWO_Detail");

            try
            {
                //this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (AP_PurchaseOrderDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKeyTransaction(CompanyID, pod.ItemID, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = AP_PurchaseOrderHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.PODetailID;
                    pod.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.FiscalID = CurrentFiscalID;
                    pod.Insert(sqltrans);
                    //pod.InsertAlhaj(sqltrans);

                    #region Checking For Purchase Order Complete Status

                    if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
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
                {
                   // SqlHelper.CommitTransaction(sqltrans);
                }
                else
                {
                    sqltrans = null;
                    SqlHelper.RollBackTransaction(sqltrans, "InsertWO_Detail");
                }
            }

            return sqltrans;
        }

        public bool UpdatePOHeaderAndDetail(List<AP_PurchaseOrderDetail> colAddPODetail, List<AP_PurchaseOrderDetail> colUpdatePODetail, 
            List<AP_PurchaseOrderDetail> colDeletePODetail, List<AP_PurchaseOrderRequisition> lst_PORequisitionAdd,
            List<AP_PurchaseOrderRequisition> lst_PORequisitionEdit, List<AP_POScheduling> lst_POSchedulingAdd, List<AP_POScheduling> lst_POSchedulingEdit,
            List<AP_POPaymentSchedule> lst_PaymentScheduleAdd,List<AP_POPaymentSchedule> lst_PaymentScheduleEdit,List<AP_POPaymentSchedule> lst_PaymentScheduleDelete,

            bool isSchedulingOn, SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

            try
            {
                this.FiscalID = CurrentFiscalID; 

                this.UpdateDataByKey(sqltrans);

                using (AP_PurchaseOrderRequisition obj_PORequisition = new AP_PurchaseOrderRequisition())
                    obj_PORequisition.DeletePORequisition(f_CompanyID, f_PeriodID, this.POHeaderID, sqltrans);

                if(isSchedulingOn)
                    using (AP_POScheduling obj_POScheduling = new AP_POScheduling())
                        obj_POScheduling.DeleteDataByKey(f_CompanyID, f_PeriodID, this.POHeaderID, sqltrans);

                foreach (AP_PurchaseOrderDetail podAdd in colAddPODetail)
                {
                    podAdd.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    podAdd.SerialNo = serialNo++;
                    podAdd.FiscalID = CurrentFiscalID;
                    podAdd.Insert(sqltrans);

                    #region Insert RFQ Requisitions

                    if (lst_PORequisitionAdd != null)
                    {
                        var poRequisitions = from objPurRequisition in lst_PORequisitionAdd
                                              where objPurRequisition.RowUniqueID == podAdd.RowUniqueID
                                              select objPurRequisition;

                        foreach (AP_PurchaseOrderRequisition objPORequisition in poRequisitions)
                        {
                            objPORequisition.POHeaderID = this.POHeaderID;
                            objPORequisition.PODetailID = podAdd.PODetailID;
                            objPORequisition.FiscalID = CurrentFiscalID;
                            objPORequisition.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Insert Purchase Order Scehduling

                    if (isSchedulingOn && lst_POSchedulingAdd != null)
                    {
                        var poScheduling = from objPurSchedule in lst_POSchedulingAdd
                                           where objPurSchedule.RowUniqueID == podAdd.RowUniqueID
                                           select objPurSchedule;

                        foreach (AP_POScheduling objPOSchedule in poScheduling)
                        {
                            objPOSchedule.POHeaderID = this.POHeaderID;
                            objPOSchedule.PODetailID = podAdd.PODetailID;
                            objPOSchedule.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Checking For Purchase Order Complete Status

                    if (podAdd.FromHeaderID.HasValue && podAdd.FromDetailID.HasValue && f_DocumentDate.HasValue)
                    {
                        CheckPurchaseStatus(podAdd.FromHeaderID.Value, podAdd.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, 
                            f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Add", f_DocumentDate.Value);
                    }

                    #endregion
                }

                foreach (AP_PurchaseOrderDetail podUpdate in colUpdatePODetail)
                {
                    podUpdate.SerialNo = serialNo++;
                    podUpdate.FiscalID = CurrentFiscalID;
                    podUpdate.UpdateDataByKey(sqltrans);

                    #region Update RFQ Requisitions

                    if (lst_PORequisitionEdit != null)
                    {
                        var poRequisitions = from objPORequisition in lst_PORequisitionEdit
                                              where objPORequisition.RowUniqueID == podUpdate.RowUniqueID
                                              select objPORequisition;

                        foreach (AP_PurchaseOrderRequisition objPORequisition in poRequisitions)
                        {
                            objPORequisition.POHeaderID = this.POHeaderID;
                            objPORequisition.PODetailID = objPORequisition.PODetailID;
                            objPORequisition.FiscalID = CurrentFiscalID;
                            objPORequisition.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Insert Purchase Order Scehduling (Since in the beginning of this function, we have deleted all schedulings)

                    if (isSchedulingOn && lst_POSchedulingEdit != null)
                    {
                        var poScheduling = from objPurSchedule in lst_POSchedulingEdit
                                           where objPurSchedule.RowUniqueID == podUpdate.RowUniqueID
                                           select objPurSchedule;

                        foreach (AP_POScheduling objPOSchedule in poScheduling)
                        {
                            objPOSchedule.POHeaderID = this.POHeaderID;
                            objPOSchedule.PODetailID = podUpdate.PODetailID;
                            objPOSchedule.Insert(sqltrans);
                        }
                    }

                    #endregion

                    #region Checking For Purchase Order Complete Status

                    if (podUpdate.FromHeaderID.HasValue && podUpdate.FromDetailID.HasValue && f_DocumentDate.HasValue)
                    {
                        CheckPurchaseStatus(podUpdate.FromHeaderID.Value, podUpdate.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, 
                            f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Update", f_DocumentDate.Value);
                    }

                    #endregion
                }

                foreach (AP_PurchaseOrderDetail podDelete in colDeletePODetail)
                {
                    AP_PurchaseOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID, 
                        podDelete.PODetailID, POHeaderID);

                    #region Checking For Purchase Order Complete Status

                    if (podDelete.FromHeaderID.HasValue && podDelete.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(podDelete.FromHeaderID.Value, podDelete.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, false, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Delete", f_DocumentDate.Value);

                    #endregion
                }


                #region collection in AP_POPaymentSchedule table

                if (lst_PaymentScheduleAdd != null && lst_PaymentScheduleAdd.Count > 0)
                {
                    foreach (AP_POPaymentSchedule ps in lst_PaymentScheduleAdd)
                    {
                        ps.PODetailID = short.Parse(AP_POPaymentSchedule.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                        ps.POHeaderID = this.POHeaderID;
                        ps.Insert(sqltrans);
                    }
                }

                if (lst_PaymentScheduleEdit != null && lst_PaymentScheduleEdit.Count > 0)
                {
                    foreach (AP_POPaymentSchedule ps in lst_PaymentScheduleEdit)
                        ps.UpdateDataByKey(sqltrans);
                }

                if (lst_PaymentScheduleDelete != null && lst_PaymentScheduleDelete.Count > 0)
                {
                    foreach (AP_POPaymentSchedule ps in lst_PaymentScheduleDelete)
                    {
                        AP_POPaymentSchedule.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                            ps.PODetailID, POHeaderID);
                    }
                }


                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.FiscalID = CurrentFiscalID;
                    da.DocumentNo = this.DocumentNumber;
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }

        public bool DeletePOHeaderAndDetail(List<AP_PurchaseOrderDetail> colDeletePODetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeletePO");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, f_PeriodID, POHeaderID);

                foreach (AP_PurchaseOrderDetail podDelete in colDeletePODetail)
                {
                    AP_PurchaseOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID, podDelete.PODetailID, POHeaderID);

                    #region Checking For Purchase Order Complete Status

                    if (podDelete.FromHeaderID.HasValue && podDelete.FromDetailID.HasValue && f_DocumentDate.HasValue)
                        CheckPurchaseStatus(podDelete.FromHeaderID.Value, podDelete.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, false, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, "Delete", f_DocumentDate.Value);

                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "DeletePO");
            }
            return result;
        }

        public bool UpdatePOHeaderForApprove(SET_DocumentApprove dA,int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePOHeader");

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePOHeader");
            }

            return result;
        }

        private void CheckPurchaseStatus(int quotationHeaderID, short quotationDetailID, int companyID, int periodID, SqlTransaction sqltrans, bool isPurchased, Guid updatedBy, DateTime updateDate, string updatedByValue, string operation, DateTime orderDate)
        {
            try
            {
                bool result = false;
                PUR_QuotationHeader qh = new PUR_QuotationHeader();
                result = qh.UpdateFlagDetail(sqltrans, quotationHeaderID, quotationDetailID, isPurchased, companyID, periodID, updatedBy, updateDate, updatedByValue, orderDate);

                if (result)
                {
                    if (operation.Equals("Delete") || qh.CheckPurchaseStatus(sqltrans, quotationHeaderID, isPurchased, companyID, periodID))
                        qh.UpdateFlagHeader(sqltrans, quotationHeaderID, isPurchased, companyID, periodID, updatedBy, updateDate, updatedByValue);
                }
                else
                    throw new BusinessLogicException("Error generated: AP_PurchaseOrderHeader.cs, Line: 649, Function: POH.UpdateFlagDetail");
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public bool DeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, 
            int pOHeaderID, string Query = "")
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                string Query_Delete_ByKeyForIndent = @" Update AP_PurchaseOrderHeader 
Set Status = @Status, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and {0} ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, string.Format(Query_Delete_ByKeyForIndent, Query), sqlparams) > 0)
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

        private static decimal GetDirectPOQuantityAgainstRequisition(int companyID, int periodID, int requisitionHeaderID, long requisitionDetailID, 
            int requisitionDocumentTypeID, long requisitionNo, SqlTransaction objTransaction = null)
        {
            decimal quantityToReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RequisitionHeaderID", requisitionHeaderID);
                sqlparams[3] = new SqlParameter("@RequisitionDetailID", requisitionDetailID);
                sqlparams[4] = new SqlParameter("@RequisitionDocumentTypeID", requisitionDocumentTypeID);
                sqlparams[5] = new SqlParameter("@RequisitionNo", requisitionNo);

                string Query_GetDirectPOQuantityAgainstRequisition = @" SELECT ISNULL(ABS(SUM(AP_PurchaseOrderDetail.Quantity)), 0) AS POQuantity 
FROM AP_PurchaseOrderDetail 
    INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND AP_PurchaseOrderHeader.Status = 1 AND AP_PurchaseOrderHeader.StatusCode = 1 
    INNER JOIN AP_PurchaseOrderRequisition ON AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderDetail.PeriodID AND AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND AP_PurchaseOrderRequisition.PODetailID = AP_PurchaseOrderDetail.PODetailID 
WHERE AP_PurchaseOrderRequisition.CompanyID = @CompanyID AND AP_PurchaseOrderRequisition.PeriodID = @PeriodID 
    AND AP_PurchaseOrderRequisition.RequisitionHeaderID = @RequisitionHeaderID AND AP_PurchaseOrderRequisition.RequisitionDetailID = @RequisitionDetailID 
    AND AP_PurchaseOrderRequisition.DocumentTypeID = @RequisitionDocumentTypeID AND AP_PurchaseOrderRequisition.DocumentNo = @RequisitionNo 
    AND AP_PurchaseOrderDetail.Status = 1 ";
        
                if (objTransaction != null)
                {
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text, Query_GetDirectPOQuantityAgainstRequisition,
                        sqlparams));
                }
                else
                    quantityToReturn = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetDirectPOQuantityAgainstRequisition, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return quantityToReturn;
        }

        public bool UpdatePOHeaderStatus(SET_DocumentApprove da)
        {
            bool result = false;
            
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

            try
            {
                this.UpdateDataByKey(sqltrans);

                //AP_PurchaseOrderRequisition.DeletePORequisition(f_CompanyID, f_PeriodID, this.POHeaderID, sqltrans);

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                //if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                    //da.Insert(sqltrans);
                    da.DeleteDataByDocIDAndDocNo(sqltrans, f_CompanyID, da.DocumentTypeID, int.Parse(da.DocumentNo), f_PeriodID);
                

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }

        public DataTable GetFreightAmount(int companyID, int periodID, int PONumber, int? DocumentTypeID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PONumber", PONumber);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", DocumentTypeID);

                string Query_FreightAmount = @" select isnull(FreightAmount, 0) as FreightAmount from AP_PurchaseOrderHeader 
where AP_PurchaseOrderHeader.DocumentNumber = @PONumber and AP_PurchaseOrderHeader.POHeaderID = @DocumentTypeID and AP_PurchaseOrderHeader.CompanyID = @CompanyID 
    and AP_PurchaseOrderHeader.PeriodID = @PeriodID ";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_FreightAmount, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_FreightAmount, sqlparams);

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

        public static bool CancelPurchaseOrder(int companyID, int periodID, int poHeaderID, DateTime dt_CancelDate, string comments, Guid userID, string userName)
        {
            bool result = false;
            SqlTransaction objSQLTransaction = SqlHelper.GetTransaction("Cancel_PurchaseOrder");

            try
            {
                AP_PurchaseOrderHeader objPurchaseOrder = new AP_PurchaseOrderHeader(companyID, periodID, poHeaderID, null, objSQLTransaction);

                if (objPurchaseOrder.IsDataLoaded)
                {
                    #region Update Purchase Order

                    objPurchaseOrder.CancelFlag = true;
                    objPurchaseOrder.UpdateDate = DateTime.Now;
                    objPurchaseOrder.UpdatedBy = userID;
                    objPurchaseOrder.UpdatedByValue = userName;
                    objPurchaseOrder.Comments += comments;

                    objPurchaseOrder.UpdateDataByKey(objSQLTransaction);

                    #endregion

                    #region Update Purchase Order Detail & Quotaion Detail and Header

                    DataTable dt_PODetail = AP_PurchaseOrderDetail.GetAllActiveData(companyID, periodID, poHeaderID, true, null, objSQLTransaction);

                    foreach (DataRow dr_PODetail in dt_PODetail.Rows)
                    {
                        AP_PurchaseOrderDetail objPODetail = new AP_PurchaseOrderDetail(companyID, periodID, dr_PODetail.Field<short>("PODetailID"), 
                            dr_PODetail.Field<int>("POHeaderID"));

                        if (objPODetail.IsDataLoaded)
                        {
                            objPODetail.CancelledBy = userID;
                            objPODetail.CancelledDate = dt_CancelDate;
                            objPODetail.CancelledFlag = true;
                            objPODetail.CancelledReason += comments;

                            objPODetail.UpdateDataByKey(objSQLTransaction);

                            if (objPODetail.FromHeaderID.HasValue && objPODetail.FromDetailID.HasValue)
                            {
                                PUR_QuotationDetail.DeleteDataByKey(objSQLTransaction, userID, DateTime.Now, userName, companyID, periodID, 
                                    objPODetail.FromDetailID.Value, objPODetail.FromHeaderID.Value);

                                if (PUR_QuotationDetail.IsAllDataDeleted(companyID, periodID, objPODetail.FromHeaderID.Value, objSQLTransaction))
                                {
                                    PUR_QuotationHeader.DeleteDataByKey(objSQLTransaction, userID, DateTime.Now, userName, companyID, periodID,
                                        objPODetail.FromHeaderID.Value);
                                }

                                PUR_QuotationDetail obj_QuotationDetail = new PUR_QuotationDetail(companyID, periodID, objPODetail.FromDetailID.Value, 
                                    objPODetail.FromHeaderID.Value, null, objSQLTransaction);

                                if (obj_QuotationDetail.IsDataLoaded)
                                {
                                    PUR_EnquiryHeader.UpdateFlagHeader(companyID, periodID, obj_QuotationDetail.EnquiryHeaderID, false, 5, userID, DateTime.Now, 
                                        userName, objSQLTransaction);   // 5 for 'In Process'
                                }
                            }
                        }
                    }

                    #endregion

                    #region Delete Purchase Order Requisition & Update Stock Requisition (Inv_StockHeaderTransaction)

                    DataTable dt_PORequisition = null;
                    using (AP_PurchaseOrderRequisition obj_PORequisition = new AP_PurchaseOrderRequisition())
                        dt_PORequisition = obj_PORequisition.GetDataByKeyTable(poHeaderID, companyID, periodID, null, objSQLTransaction);

                    List<int> lst_RequisitionHeaderID = dt_PORequisition.AsEnumerable().Select(x => x.Field<int>("RequisitionHeaderID")).Distinct().ToList();

                    foreach (int requisitionHeaderID in lst_RequisitionHeaderID)
                        INV_StockTransactionHeader.UpdateIsRequestedCompleted(companyID, periodID, requisitionHeaderID, false, objSQLTransaction);

                    using (AP_PurchaseOrderRequisition obj_PORequisition = new AP_PurchaseOrderRequisition())
                        obj_PORequisition.DeletePORequisition(companyID, periodID, poHeaderID, objSQLTransaction);

                    #endregion

                    using (AP_POScheduling obj_POScheduling = new AP_POScheduling())
                        obj_POScheduling.DeleteDataByKey(companyID, periodID, poHeaderID, objSQLTransaction);

                    #region Update Document Approve - set Status to false

                    DataTable dt_ApprovalHistory = SET_DocumentApprove.GetDataByKeyForApprovalHistory(companyID, periodID, objPurchaseOrder.TypeCode, 
                        objPurchaseOrder.DocumentNumber, objSQLTransaction);

                    foreach (DataRow dr_ApprovalRecord in dt_ApprovalHistory.Rows)
                        SET_DocumentApprove.DeleteDataByKey(objSQLTransaction, companyID, dr_ApprovalRecord.Field<int>("DocumentApproveID"), periodID);

                    #endregion

                    result = true;
                }
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
                    SqlHelper.CommitTransaction(objSQLTransaction);
                else
                    SqlHelper.RollBackTransaction(objSQLTransaction, "Cancel_PurchaseOrder");
            }

            return result;
        }

        #endregion

        public DataTable PO_PendingAsOf(int CompanyID, int PeriodID, DateTime? DateFrom, DateTime? DateTo, int? VendorFrom, int? VendorTo, short? documentGroupID, bool? IsPOPending, Nullable<int> documentType = null, int? DepartmentID = null)
        {
            {
                List<SqlParameter> sqlparams = new List<SqlParameter>();
                StringBuilder additionalParameters = new StringBuilder();
                StringBuilder AddParameters = new StringBuilder();
                DataTable dtToReturn = null;

                #region Query

                string Query_PO_OutstandingAsoff = @" select * from 
(
    SELECT AP_PurchaseOrderHeader.DocumentNumber, AP_PurchaseOrderHeader.POHeaderID, AP_PurchaseOrderHeader.DocumentDate, AR_SET_Customer.CustomerName, 
        AP_PurchaseOrderHeader.TotalAmount, SET_UnitofMeasure.UOMName, INV_Item.Description,INV_Item.InventoryItemStatusCode as PartNo, AP_PurchaseOrderDetail.Quantity, isnull(
            (
                Select abs(SUM(isnull(sd.TransactionQuantity,0))) from INV_StockTransactionDetail sd 
                where AP_PurchaseOrderDetail.POHeaderID = sd.SourceHeaderID and AP_PurchaseOrderDetail.PODetailID = sd.SourceLineID 
                    and AP_PurchaseOrderHeader.CompanyID = sd.CompanyID and AP_PurchaseOrderHeader.PeriodID = sd.PeriodID AND sd.Status = 1
                    and sd.SourceDocumentTypeID =SET_DocumentType.DocumentTypeID
            ), 0) as RcvQty, AP_PurchaseOrderDetail.UnitPrice 
    FROM AP_PurchaseOrderDetail 
        INNER JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID AND AP_PurchaseOrderHeader.Status = 1
            AND AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
        INNER JOIN INV_Item ON AP_PurchaseOrderDetail.ItemID = INV_Item.ItemID AND INV_Item.Status = 1    
        LEFT OUTER JOIN AR_SET_Customer ON AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID  --AND AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
            AND AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID AND AR_SET_Customer.Status = 1
        LEFT OUTER JOIN SET_UnitofMeasure ON AP_PurchaseOrderDetail.ItemUOMID = SET_UnitofMeasure.UOMID AND SET_UnitofMeasure.Status = 1 
        INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode
			Left Join INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderHeader.StockRequisitionID 
        AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID 
		Left Join SET_Department On SET_Department.DepartmentID = INV_StockTransactionHeader.DepartmentID
And SET_Department.Status = 1 And SET_Department.IsActive = 1 
    WHERE AP_PurchaseOrderDetail.CompanyID = @CompanyID AND AP_PurchaseOrderDetail.PeriodID = @PeriodID AND AP_PurchaseOrderDetail.Status = 1
     AND (AP_PurchaseOrderHeader.ClosedCode <> 1 or AP_PurchaseOrderHeader.ClosedCode IS NULL)
        {0}
) as TAB 
{1}
order by TAB.DocumentNumber ";

                #endregion

                try
                {
                    sqlparams.Add(new SqlParameter("@CompanyID", CompanyID));
                    sqlparams.Add(new SqlParameter("@PeriodID", PeriodID));

                    //sqlparams.Add(new SqlParameter("@ItemDescription", itemDescription));

                    if (VendorFrom.HasValue && VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorIDFrom", VendorFrom.Value));
                        sqlparams.Add(new SqlParameter("@VendorIDTo", VendorFrom.Value));
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.VendorID between @VendorIDFrom and @VendorIDTo ");
                    }
                    else if(VendorFrom.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorFrom.Value));
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.VendorID =@VendorID ");
                    }
                   else if (VendorTo.HasValue)
                    {
                        sqlparams.Add(new SqlParameter("@VendorID", VendorTo.Value));
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.VendorID =@VendorID ");
                    }
                    #region Purchase Order Date filter

                    if (DateFrom.HasValue && DateTo.HasValue)
                    {
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate between @FromDate and @ToDate ");
                        sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                        sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    }
                    else if (DateFrom.HasValue)
                    {
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate >= @FromDate ");
                        sqlparams.Add(new SqlParameter("@FromDate", DateFrom.Value));
                    }
                    else if (DateTo.HasValue)
                    {
                        additionalParameters.Append(" And AP_PurchaseOrderHeader.DocumentDate <= @ToDate ");
                        sqlparams.Add(new SqlParameter("@ToDate", DateTo.Value));
                    }

                    if (documentGroupID.HasValue)
                        additionalParameters.Append(" AND Set_DocumentType.GroupID = " + documentGroupID.Value.ToString() + " ");

                    if (documentType.HasValue)
                        additionalParameters.Append(" AND Set_DocumentType.DocumentTypeID = " + documentType.Value.ToString() + " ");

                    if (DepartmentID > 0)
                    {
                        additionalParameters.Append(" And SET_Department.DepartmentID = " + DepartmentID);
                    }
                    #endregion

                 
                    if (IsPOPending == true)
                        AddParameters.Append(" Where TAB.RcvQty < TAB.Quantity ");

                    DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_PO_OutstandingAsoff, additionalParameters.ToString(), AddParameters.ToString()), sqlparams.ToArray());

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
        }


        public bool UpdatePOHeaderAndDetailForAlIdmad(List<AP_PurchaseOrderDetail> colAddPODetail, List<AP_PurchaseOrderDetail> colUpdatePODetail,
          List<AP_PurchaseOrderDetail> colDeletePODetail,
          SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

            try
            {
                this.FiscalID = CurrentFiscalID;

                this.UpdateDataByKey(sqltrans);
                

                foreach (AP_PurchaseOrderDetail podAdd in colAddPODetail)
                {
                    podAdd.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    podAdd.SerialNo = serialNo++;
                    podAdd.FiscalID = CurrentFiscalID;
                    podAdd.Insert(sqltrans);
                    
                }

                foreach (AP_PurchaseOrderDetail podUpdate in colUpdatePODetail)
                {
                    podUpdate.POHeaderID = POHeaderID;
                    podUpdate.PODetailID = podUpdate.PODetailID;
                    podUpdate.SerialNo = serialNo++;
                    podUpdate.FiscalID = CurrentFiscalID;
                    podUpdate.UpdateDataByKey(sqltrans);
                    
                }

                foreach (AP_PurchaseOrderDetail podDelete in colDeletePODetail)
                {
                    AP_PurchaseOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        podDelete.PODetailID, POHeaderID);
                }

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }

        public static DataTable GetAllDataWithOtherTablesForAlImdad(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.desc, int CurrentFiscalID = 0)
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

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


                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging,FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching_WithPaging + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : "")), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_SelectAll_Searching + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : ""), sqlparamsList.ToArray()).Tables[0];
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }
        public DataTable GetDataForImportDocumentLookUp_AndWithoutClosedCode(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, int periodID,
                  int companyID, string appendedFilter = "", int? docTypeID = null, string outerApplyInvoice = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                if (docTypeID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocTypeID", docTypeID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocTypeID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ImportDocumentLooKup,
                    outerApplyInvoice) + appendedFilter , sqlparams);

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

        public DataTable GetDataForImportDocumentLookUp_WeightAssessment(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, int periodID,
                 int companyID, string appendedFilter = "", int? docTypeID = null, string outerApplyInvoice = "",string filterimport = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                if (docTypeID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocTypeID", docTypeID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocTypeID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ImportDocumentLookUp_WeightAssessment + filterimport,
                    outerApplyInvoice,appendedFilter) , sqlparams);

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

        public DataTable GetDataForImportDocumentLookUp_WeightAssessmentForConnect(Nullable<int> statusCode, Nullable<int> supplierID, Nullable<int> pOHeaderID, int periodID,
                 int companyID, string appendedFilter = "", int? docTypeID = null, string outerApplyInvoice = "", string filterimport = "")
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];

                if (statusCode.HasValue)
                    sqlparams[0] = new SqlParameter("@StatusCode", statusCode.Value);
                else
                    sqlparams[0] = new SqlParameter("@StatusCode", DBNull.Value);

                if (supplierID.HasValue)
                    sqlparams[1] = new SqlParameter("@SupplierID", supplierID.Value);
                else
                    sqlparams[1] = new SqlParameter("@SupplierID", DBNull.Value);

                if (pOHeaderID.HasValue)
                    sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID.Value);
                else
                    sqlparams[2] = new SqlParameter("@POHeaderID", DBNull.Value);

                sqlparams[3] = new SqlParameter("@PeriodID", periodID);
                sqlparams[4] = new SqlParameter("@CompanyID", companyID);

                if (docTypeID.HasValue)
                    sqlparams[5] = new SqlParameter("@DocTypeID", docTypeID.Value);
                else
                    sqlparams[5] = new SqlParameter("@DocTypeID", DBNull.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ImportDocumentLookUp_WeightAssessmentForConnect + filterimport,
                    outerApplyInvoice, appendedFilter), sqlparams);

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

        public static DataRow GetImportDocumentDataByKeyWithOtherTables_AlImdad(int POHeaderID, int periodID, int companyID)
        {
            DataRow drToReturn = null;
            StringBuilder sb = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CompanyID", companyID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_Select_By_PrimaryKey_WithOtherTables_ALImdad + sb.ToString(), sqlparams);

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    drToReturn = ds.Tables[0].Rows[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return drToReturn;
        }
        public bool InsertImportDocumenteaderAndDetail_AlImdad(List<AP_PurchaseOrderRequisition> lst_PORequisition, List<AP_PurchaseOrderDetail> colPODetail,
          List<AP_POScheduling> colPOScheduling, List<GL_TransactionDetail> colTranDetail, GL_TransactionHeader th,
            SET_DocumentApprove da, bool IsPerfoance = true, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPO");

            try
            {
                this.DocumentNumber = SET_DocumentSequence.ManualDocumentSequence(f_TypeCode, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID).ToString();
                this.FiscalID = CurrentFiscalID;
                this.Insert(sqltrans);

                foreach (AP_PurchaseOrderDetail pod in colPODetail)
                {
                    #region Update PO List Price , Market Price

                    DataRow dr = INV_Item.GetDataByKeyTransaction(CompanyID, pod.ItemID, sqltrans);
                    pod.ListPricePerUnit = Convert.ToDouble(dr["ListPricePerUnit"].ToString());
                    DataTable dt_UpdateMarketPriceForPO = AP_PurchaseOrderHeader.GetAmountQuantity(CompanyID, PeriodID, pod.ItemID, sqltrans);

                    if (dt_UpdateMarketPriceForPO != null && dt_UpdateMarketPriceForPO.Rows.Count > 0)
                    {
                        decimal amount = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["Amount"].ToString()) + Convert.ToDecimal(pod.Quantity * Convert.ToDecimal(pod.UnitPrice));

                        decimal transactionqty = Convert.ToDecimal(dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString() == string.Empty ? "0" : dt_UpdateMarketPriceForPO.Rows[0]["TransactionQuantity"].ToString()) + Convert.ToDecimal(pod.Quantity);

                        long MarketPrice = Convert.ToInt64(amount / transactionqty);

                        pod.MarketPrice = MarketPrice;
                    }

                    #endregion

                    int tempPODetailID = pod.PODetailID;
                    pod.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    pod.SerialNo = serialNo++;
                    pod.FiscalID = CurrentFiscalID;
                    pod.Insert(sqltrans);
             
          
                    #region Checking For Purchase Order Complete Status

                    //if (pod.FromHeaderID.HasValue && pod.FromDetailID.HasValue && f_DocumentDate.HasValue)
                    //    CheckPurchaseStatus(pod.FromHeaderID.Value, pod.FromDetailID.Value, this.f_CompanyID, this.f_PeriodID, sqltrans, true, f_CreatedBy, f_CreateDate, f_CreatedByValue, "Add", f_DocumentDate.Value);

                    #endregion
                }
             
                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.TypeCode, PeriodID, null, sqltrans);
               
                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = Convert.ToInt32(f_DocumentNumber);
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.POHeaderID;

                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.DocumentNumber.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPO");
            }

            return result;
        }

        public bool UpdateImportDocHeaderAndDetailForAlIdmad(List<AP_PurchaseOrderDetail> colAddPODetail, List<AP_PurchaseOrderDetail> colUpdatePODetail,
       List<AP_PurchaseOrderDetail> colDeletePODetail,List<GL_TransactionDetail> colTranDetail, GL_TransactionHeader th,
       SET_DocumentApprove da, int CurrentFiscalID = 0)
        {
            bool result = false;
            short serialNo = 1;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePO");

            try
            {
                this.FiscalID = CurrentFiscalID;

                this.UpdateDataByKey(sqltrans);


                foreach (AP_PurchaseOrderDetail podAdd in colAddPODetail)
                {
                    podAdd.PODetailID = short.Parse(AP_PurchaseOrderDetail.GetNewPODetailID(CompanyID, PeriodID, POHeaderID, sqltrans).ToString());
                    podAdd.SerialNo = serialNo++;
                    podAdd.FiscalID = CurrentFiscalID;
                    podAdd.Insert(sqltrans);

                }

                foreach (AP_PurchaseOrderDetail podUpdate in colUpdatePODetail)
                {
                    podUpdate.POHeaderID = POHeaderID;
                    podUpdate.PODetailID = podUpdate.PODetailID;
                    podUpdate.SerialNo = serialNo++;
                    podUpdate.FiscalID = CurrentFiscalID;
                    podUpdate.UpdateDataByKey(sqltrans);

                }

                foreach (AP_PurchaseOrderDetail podDelete in colDeletePODetail)
                {
                    AP_PurchaseOrderDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PeriodID,
                        podDelete.PODetailID, POHeaderID);
                }

                DataRow dr_documentType = SET_DocumentType.GetDataByKey(CompanyID, this.TypeCode, PeriodID, null, sqltrans);

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (th != null && this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)) && dr_documentType != null
                    && dr_documentType["TransactionType"] != null && dr_documentType["TransactionType"] != DBNull.Value
                    //&& !dr_documentType["TransactionType"].ToString().Equals("No Impact")
                    )
                {
                    #region Insert In GL_TransactionHeader

                    th.DocumentNo = Convert.ToInt32(f_DocumentNumber);
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                    th.FiscalID = CurrentFiscalID;
                    th.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = th.TransactionID;
                        td.SourceHeaderID = this.POHeaderID;

                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove))
                    || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentNo = this.DocumentNumber.ToString();
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePO");
            }

            return result;
        }
        public static DataTable Get_DetailAgainstImportPOheaderID(int companyID, int periodID, int POheaderID, SqlConnection objConnection = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POheaderID", POheaderID);

                #region
                const string Query_GetDetailAgainstImportPOheaderID = @"Select -1 as DiscountDetailID,-1 as PurchaseDiscountID
,0 as Amount,AP_PurchaseOrderDetail.ItemID,INV_Item.Description
,INV_Item.ItemTypeID as CategoryID,INV_SET_Category.CategoryName,
AP_PurchaseOrderDetail.ItemUOMID as  UOMID ,SET_UnitofMeasure.UOMName 
,'Add' as Action
 from AP_PurchaseOrderDetail
 
INNER jOIN INV_Item ON INV_Item.ItemID=AP_PurchaseOrderDetail.ItemID
AND INV_Item.Status=1
INNER jOIN INV_SET_Category ON INV_SET_Category.CategoryID=INV_Item.ItemTypeID
INNER jOIN SET_UnitofMeasure ON SET_UnitofMeasure.UOMID=AP_PurchaseOrderDetail.ItemUOMID

Where 
AP_PurchaseOrderDetail.CompanyID=@CompanyID
AND AP_PurchaseOrderDetail.PeriodID=@PeriodID
AND AP_PurchaseOrderDetail.POHeaderID=@POHeaderID
AND AP_PurchaseOrderDetail.Status=1";
                #endregion
                DataSet ds = null;

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDetailAgainstImportPOheaderID, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetDetailAgainstImportPOheaderID, sqlparams);

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

        public bool UpdateDataByDDLValues(int Companyid, int perioid, string Query, int PONo)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@POHeaderID", PONo);


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

        public static DataTable GetTotal(int POHeaderID,int CompanyID, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);

                string Query_getAmountQuantity = @" select Sum(Quantity) as TQty,ISNULL(Sum(SecondaryQuantity),0) as Qty,Sum(NetAmount) as NetAmount,Sum((NetAmount)) as GrossPKR from AP_PurchaseOrderDetail where POHeaderID=@POHeaderID and CompanyID = @CompanyID ";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_getAmountQuantity, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_getAmountQuantity, sqlparams);

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


        public static DataTable GetAdvancePaymentReport(int companyID, int periodID, DateTime FromDate, DateTime ToDate,string DocumentType = "", string DocumentGroup = "", string StatusCode = "", string Customer = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                //sb.Append(" and convert(date,AP_PurchaseOrderHeader.DocumentDate) = '" + Date.ToString("yyyy-MM-dd") + "'");
                sb.Append(" and convert(date,AP_PurchaseOrderHeader.DocumentDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");
                #region MyRegion
                
                string Query_GetAdvancePaymentReport = @"select 
CompanyID,POHeaderID,CustomerName,PONumber,
sum(Amount)  as TotalAmount_Excl ,sum(taxAmount) as TaxAmount , sum(NetAmount) as TotalAmount_Incl
,AdvanceAmountPercentage
,CreatedByValue as CreatedBy
,case when OnNetAmount = 1 then 'Inclusive' else 'Exclusive' end as GST

,case when OnNetAmount = 1 
then sum(NetAmount) * AdvanceAmountPercentage / 100

else (sum(Quantity) * sum(UnitPrice)) * AdvanceAmountPercentage / 100
end as AdvanceAmount,PaymentDocNo,PaymentDocDate,PaymentAmount
,PaymentCreatedBy
,case when PO_StatusCode = 1 then 'Approved' else 'In-Process' end as PO_StatusCode
,case when Payment_StatusCode = 1 then 'Approved' when Payment_StatusCode = 0 then '' else 'In-Process' end as Payment_StatusCode
,case ClosedCode
when 1 then 'Closed' 
when 2 then 'Closed For Invoice' 
when 3 then 'Closed For Receiving' 
when 4 then 'Finally Closed' 
when 5 then 'Frozen'
when 6 then 'Not On Hold'
when 7 then 'Not Reserved'
when 8 then 'On Hold'
when 9 then 'Open'
when 10 then 'Printed'
when 11 then 'Reserved'
when 12 then 'Unfrozen' 
when 13 then 'Partial Received' 
else null end as ClosedStatus
 from (
select AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.POHeaderID, AR_SET_Customer.CustomerName,AP_PurchaseOrderHeader.DocumentNumber as PONumber
,AP_PurchaseOrderDetail.NetAmount 
,AP_PurchaseOrderDetail.taxAmount
,AP_PurchaseOrderDetail.Quantity , AP_PurchaseOrderDetail.UnitPrice 
,AP_PurchaseOrderDetail.ItemID
,isnull(AR_SET_Tax.TaxRate,0) as TaxRate
,AP_PurchaseOrderHeader.AdvanceAmountPercentage
,AP_PurchaseOrderHeader.CreatedByValue
,isnull(AP_PurchaseOrderHeader.OnNetAmount,0) as OnNetAmount
,AP_PaymentHeader.DocumentNo as PaymentDocNo
,AP_PaymentHeader.DocumentDate as PaymentDocDate
,AP_PaymentHeader.Amount as PaymentAmount
,AP_PaymentHeader.CreatedByValue as PaymentCreatedBy
,AP_PurchaseOrderHeader.StatusCode as PO_StatusCode ,isnull(AP_PaymentHeader.StatusCode,0) as Payment_StatusCode
,AP_PurchaseOrderHeader.ClosedCode
,AP_PurchaseOrderDetail.Quantity * AP_PurchaseOrderDetail.UnitPrice as Amount
from AP_PurchaseOrderHeader
inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode
and Set_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID
and Set_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID
inner join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PurchaseOrderDetail.Status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID  = AP_PurchaseOrderHeader.VendorID
and AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID
left join AR_SET_Tax on AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID

left join AP_PaymentHeader on AP_PaymentHeader.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PaymentHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PaymentHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PaymentHeader.status = 1 

where AP_PurchaseOrderHeader.CompanyID = @CompanyID and AP_PurchaseOrderHeader.PeriodID = @PeriodID
and AP_PurchaseOrderHeader.status = 1 
and isnull(AdvanceAmountPercentage,0) > 0
{0}

)as tab
group by CompanyID,POHeaderID,CustomerName,PONumber,TaxRate,AdvanceAmountPercentage,CreatedByValue,OnNetAmount,PaymentDocNo,PaymentDocDate,PaymentAmount,PaymentCreatedBy,PO_StatusCode,Payment_StatusCode,ClosedCode";

                #endregion

                if (DocumentType != "")
                    sb.Append(" and AP_PurchaseOrderHeader.TypeCode = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (StatusCode != "")
                {
                    if (StatusCode == "1")
                    {
                        sb.Append(" and AP_PurchaseOrderHeader.StatusCode = " + StatusCode + "");
                        sb.Append(" and (AP_PaymentHeader.StatusCode = " + StatusCode + " or AP_PaymentHeader.StatusCode is null)");
                    }
                    else
                    {
                        sb.Append(" and AP_PurchaseOrderHeader.StatusCode <> 1 ");
                        sb.Append(" and (AP_PaymentHeader.StatusCode <> 1 or AP_PaymentHeader.StatusCode is null)");
                    }
                    
                }
                if (Customer != "")
                {
                    sb.Append(" and  AP_PurchaseOrderHeader.VendorID =" + Customer + "");
                }



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAdvancePaymentReport,sb.ToString()) , sqlparams);

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

        public static DataTable GetAdvancePaymentScheduleWiseReport(int companyID, int periodID, DateTime FromDate, DateTime ToDate, string DocumentType = "", string DocumentGroup = "", string StatusCode = "", string Customer = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                //sb.Append(" and convert(date,AP_PurchaseOrderHeader.DocumentDate) = '" + Date.ToString("yyyy-MM-dd") + "'");
                sb.Append(" and convert(date,AP_PurchaseOrderHeader.DocumentDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");
                #region MyRegion

                string Query_GetAdvancePaymentScheduleWiseReport = @"

select 
--SNO,
 CompanyID,POHeaderID,CustomerName,PONumber,
sum(Amount)  as TotalAmount_Excl ,sum(taxAmount) as TaxAmount , sum(NetAmount) as TotalAmount_Incl
,AdvanceAmountPercentage
,POSchdulePercentage,POSchduleDueDate,sum(POSchduleAdvancedAmount) as POSchduleAdvancedAmount
,CreatedByValue as CreatedBy
,case when OnNetAmount = 1 then 'Inclusive' else 'Exclusive' end as GST

,case when OnNetAmount = 1 
then sum(NetAmount) * AdvanceAmountPercentage / 100

else (sum(Quantity) * sum(UnitPrice)) * AdvanceAmountPercentage / 100
end as AdvanceAmount,PaymentDocNo,PaymentDocDate,PaymentAmount
,PaymentCreatedBy
,case when PO_StatusCode = 1 then 'Approved' else 'In-Process' end as PO_StatusCode
,case when Payment_StatusCode = 1 then 'Approved' when Payment_StatusCode = 0 then '' else 'In-Process' end as Payment_StatusCode
,case ClosedCode
when 1 then 'Closed' 
when 2 then 'Closed For Invoice' 
when 3 then 'Closed For Receiving' 
when 4 then 'Finally Closed' 
when 5 then 'Frozen'
when 6 then 'Not On Hold'
when 7 then 'Not Reserved'
when 8 then 'On Hold'
when 9 then 'Open'
when 10 then 'Printed'
when 11 then 'Reserved'
when 12 then 'Unfrozen' 
when 13 then 'Partial Received' 
else null end as ClosedStatus
 from (
select Distinct
--ROW_NUMBER() OVER(ORDER BY DocumentNumber asc) AS SNO,
AP_PurchaseOrderHeader.CompanyID, AP_PurchaseOrderHeader.POHeaderID, AR_SET_Customer.CustomerName,
AP_PurchaseOrderHeader.DocumentNumber as PONumber
,AP_PurchaseOrderDetail.NetAmount 
,AP_PurchaseOrderDetail.taxAmount
,AP_PurchaseOrderDetail.Quantity , AP_PurchaseOrderDetail.UnitPrice 
,AP_PurchaseOrderDetail.ItemID
,isnull(AR_SET_Tax.TaxRate,0) as TaxRate
,AP_PurchaseOrderHeader.AdvanceAmountPercentage
,AP_PurchaseOrderHeader.CreatedByValue
,isnull(AP_PurchaseOrderHeader.OnNetAmount,0) as OnNetAmount 
--,AP_PaymentHeader.DocumentNo as PaymentDocNo
--,AP_PaymentHeader.DocumentDate as PaymentDocDate
--,AP_PaymentHeader.Amount as PaymentAmount
--,AP_PaymentHeader.CreatedByValue as PaymentCreatedBy
--,isnull(AP_PaymentHeader.StatusCode,0) as Payment_StatusCode
--,AP_PaymentHeader.StatusCode
,AP_PurchaseOrderHeader.StatusCode as PO_StatusCode 
,AP_PurchaseOrderHeader.ClosedCode
,AP_PurchaseOrderDetail.Quantity * AP_PurchaseOrderDetail.UnitPrice as Amount
,AP_POPaymentSchedule.Percentage as POSchdulePercentage,AP_POPaymentSchedule.Date as POSchduleDueDate, 
isnull(AP_POPaymentSchedule.Amount,0) as POSchduleAdvancedAmount

,PaymentData.PaymentDocDate, PaymentData.PaymentAmount
,PaymentData.PaymentCreatedBy, PaymentData.Payment_StatusCode, PaymentData.StatusCode--
,PaymentData.PaymentDocNo

from AP_PurchaseOrderHeader
inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode
and Set_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID
and Set_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID
inner join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PurchaseOrderDetail.Status = 1
inner join AR_SET_Customer on AR_SET_Customer.CustomerID  = AP_PurchaseOrderHeader.VendorID
and AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID
left join AR_SET_Tax on AR_SET_Tax.TaxID = AP_PurchaseOrderDetail.TaxID

left join AP_POPaymentSchedule on AP_POPaymentSchedule.POHeaderID = ap_purchaseOrderheader.POHeaderID
and AP_POPaymentSchedule.CompanyID = ap_purchaseOrderheader.CompanyID
and AP_POPaymentSchedule.Status = 1

Left Join

   (
     Select 
	    AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID,
	    AP_PaymentHeader.POHeaderID, AP_PaymentHeader.DocumentNo as PaymentDocNo,AP_PaymentHeader.DocumentDate as PaymentDocDate
       ,AP_PaymentHeader.Amount as PaymentAmount, AP_PaymentHeader.CreatedByValue as PaymentCreatedBy
       ,isnull(AP_PaymentHeader.StatusCode,0) as Payment_StatusCode
      ,AP_PaymentHeader.StatusCode
	From AP_PaymentHeader
	   Inner Join AP_PurchaseOrderHeader AP_PurchaseOrderHeaderInner on 
	         AP_PurchaseOrderHeaderInner.POHeaderID = AP_PaymentHeader.POHeaderID
	         and AP_PurchaseOrderHeaderInner.CompanyID = AP_PaymentHeader.CompanyID
			 and AP_PurchaseOrderHeaderInner.PeriodID = AP_PaymentHeader.PeriodID
			 and AP_PurchaseOrderHeaderInner.Status = 1

	where AP_PaymentHeader.Status = 1 and AP_PaymentHeader.CompanyID = @CompanyID
	      and AP_PaymentHeader.PeriodID = @PeriodID
		  and AP_PaymentHeader.Status = 1

   ) PaymentData on PaymentData.POHeaderID = AP_POPaymentSchedule.POHeaderID
     and PaymentData.CompanyID = AP_POPaymentSchedule.CompanyID
	 and PaymentData.PeriodID = AP_POPaymentSchedule.PeriodID

--left join AP_PaymentHeader on AP_PaymentHeader.POHeaderID = AP_POPaymentSchedule.POHeaderID
--and AP_PaymentHeader.CompanyID = AP_POPaymentSchedule.CompanyID
--and AP_PaymentHeader.PeriodID = AP_POPaymentSchedule.PeriodID
--and AP_PaymentHeader.status = 1 

 where AP_PurchaseOrderHeader.CompanyID = @CompanyID and AP_PurchaseOrderHeader.PeriodID = @PeriodID
   and AP_PurchaseOrderHeader.status = 1 
   and isnull(AdvanceAmountPercentage,0) > 0
   {0}
)as tab
group by CompanyID,POHeaderID,CustomerName,PONumber,TaxRate,AdvanceAmountPercentage,CreatedByValue,OnNetAmount,
        PaymentDocNo,PaymentDocDate,PaymentAmount,PaymentCreatedBy,PO_StatusCode,Payment_StatusCode,ClosedCode,
		POSchdulePercentage,POSchduleDueDate--,SNO
";

                #endregion

                if (DocumentType != "")
                    sb.Append(" and AP_PurchaseOrderHeader.TypeCode = " + DocumentType + "");

                if (DocumentGroup != "")
                    sb.Append(" and Set_DocumentType.GroupID = " + DocumentGroup + "");


                if (StatusCode != "")
                {
                    if (StatusCode == "1")
                    {
                        sb.Append(" and AP_PurchaseOrderHeader.StatusCode = " + StatusCode + "");
                        sb.Append(" and (AP_PaymentHeader.StatusCode = " + StatusCode + " or AP_PaymentHeader.StatusCode is null)");
                    }
                    else
                    {
                        sb.Append(" and AP_PurchaseOrderHeader.StatusCode <> 1 ");
                        sb.Append(" and (AP_PaymentHeader.StatusCode <> 1 or AP_PaymentHeader.StatusCode is null)");
                    }

                }
                if (Customer != "")
                {
                    sb.Append(" and  AP_PurchaseOrderHeader.VendorID =" + Customer + "");
                }



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_GetAdvancePaymentScheduleWiseReport, sb.ToString()), sqlparams);

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

        public static DataTable AdvancePOAgingReport(string companyID, int periodID, DateTime FromDate, DateTime ToDate, string Customer = "")
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@PeriodID", periodID);
                sqlparams[1] = new SqlParameter("@CompanyID", companyID);

                sb.Append(" and convert(date,AP_PaymentHeader.DocumentDate) between '" + FromDate.ToString("yyyy-MM-dd") + "' and '" + ToDate.ToString("yyyy-MM-dd") + "'");
               
                #region MyRegion

                string Query_AdvancePOAgingReport = @"
--Declare @PeriodID int = 1

select *,(AdvanceAmount - InvoiceAmount) as Balance from (

select AR_SET_Customer.CustomerID, AR_SET_Customer.CustomerName as Supplier

       ,STUFF((SELECT ', ' + CAST(AP_PaymentHeaderInner.DocumentNo AS VARCHAR(10)) as AdvancePoNumber
         FROM AP_PaymentHeader as AP_PaymentHeaderInner
         WHERE 
		 AP_PaymentHeaderInner.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
		 and AP_PaymentHeaderInner.CompanyID = AP_PurchaseOrderHeader.CompanyID
		 and AP_PaymentHeaderInner.PeriodID = AP_PurchaseOrderHeader.PeriodID
		 and AP_PaymentHeaderInner.status = 1  and AP_PaymentHeaderInner.StatusCode = 1
         FOR XML PATH(''), TYPE)
        .value('.','NVARCHAR(MAX)'),1,2,' ') AdvancePoNumber

,(SELECT top 1 AP_PaymentHeaderInner.DocumentDate
         FROM AP_PaymentHeader as AP_PaymentHeaderInner
         WHERE 
		 AP_PaymentHeaderInner.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
		 and AP_PaymentHeaderInner.CompanyID = AP_PurchaseOrderHeader.CompanyID
		 and AP_PaymentHeaderInner.PeriodID = AP_PurchaseOrderHeader.PeriodID
		 and AP_PaymentHeaderInner.status = 1  and AP_PaymentHeaderInner.StatusCode = 1
		 order by AP_PaymentHeaderInner.DocumentDate desc ) as DocumentDate

,(SUM(AP_PaymentHeader.Amount)
-
isnull((
			select sum(AP_PaymentSupplierDetail.Amount) 
			from AP_PaymentSupplierDetail 
			inner join AP_PaymentHeader pay on pay.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and pay.CompanyID = AP_PurchaseOrderHeader.CompanyID
and pay.PeriodID = AP_PurchaseOrderHeader.PeriodID
and pay.status = 1 
			where AP_PaymentSupplierDetail .PaymentHeaderID = pay.PaymentID
			and AP_PaymentSupplierDetail.CompanyID = pay.CompanyID
			and AP_PaymentSupplierDetail.PeriodID = pay.PeriodID
			and AP_PaymentSupplierDetail.Status = 1
			and isnull(AP_PaymentSupplierDetail.Narration,'') = 'GST'
			),0))
+
isnull((
			select sum(AP_PaymentSupplierDetail.Amount) 
			from AP_PaymentSupplierDetail 
			inner join AP_PaymentHeader pay on pay.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and pay.CompanyID = AP_PurchaseOrderHeader.CompanyID
and pay.PeriodID = AP_PurchaseOrderHeader.PeriodID
and pay.status = 1 

Inner Join AR_SET_BusinessPartnerTypes ON AR_SET_BusinessPartnerTypes.BusinessPartnerID = AP_PaymentSupplierDetail.SupplierID
        And AR_SET_BusinessPartnerTypes.PeriodID = AP_PaymentSupplierDetail.PeriodID 
    inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode 
	and AP_PaymentSupplierDetail.BusinessPartnerTypeID = AR_SET_BusinessPartner.BusinessPartnerID 

			where AP_PaymentSupplierDetail .PaymentHeaderID = pay.PaymentID
			and AP_PaymentSupplierDetail.CompanyID = pay.CompanyID
			and AP_PaymentSupplierDetail.PeriodID = pay.PeriodID
			and AP_PaymentSupplierDetail.Status = 1
			and (AR_SET_BusinessPartner.BusinessPartnerName = 'Advances' or AR_SET_BusinessPartner.BusinessPartnerName = 'Advance GST')
			and AP_PaymentSupplierDetail.TransactionType = 'Debit'
			
			),0)

 as AdvanceAmount

,AP_PurchaseOrderHeader.DocumentNumber as PONumber

,AP_PurchaseOrderHeader.AdvanceAmountPercentage

,isnull((
select sum(Amount) from (
select distinct GL_TransactionDetail.Amount, AR_InvoiceHeader.InvoiceNumber
from Ar_invoiceDetail 
INNER JOIN AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = Ar_invoiceDetail .InvoiceHeaderID
and AR_InvoiceHeader.CompanyID = Ar_invoiceDetail.CompanyID
and AR_InvoiceHeader.PeriodID = Ar_invoiceDetail.PeriodID
inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
 and GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType
 and GL_TransactionHeader.FiscalID = AR_InvoiceHeader.FiscalID
 and GL_TransactionHeader.CompanyID = AR_InvoiceHeader.CompanyID
 and GL_TransactionHeader.PeriodID = AR_InvoiceHeader.PeriodID
 INNER JOIN dbo.GL_TransactionDetail ON dbo.GL_TransactionHeader.TransactionID = dbo.GL_TransactionDetail.TransactionID AND 
							  dbo.GL_TransactionHeader.PeriodID = dbo.GL_TransactionDetail.PeriodID AND 
							  dbo.GL_TransactionHeader.CompanyID = dbo.GL_TransactionDetail.CompanyID
 
 AND (dbo.GL_TransactionDetail.CodeCombinationID in  (SELECT distinct DefaultAccountCode FROM dbo.SET_DocumentType WHERE (DocumentType like 'Advance Against PO%') AND (CompanyID in (2)) AND (PeriodID = 1)))
                     AND 
							  (dbo.GL_TransactionDetail.TransactionType = 'Credit')
where Ar_invoiceDetail.PurchaseOrderHeaderID = AP_PurchaseOrderHeader.POHeaderID 
   and Ar_invoiceDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
   and Ar_invoiceDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
   and Ar_invoiceDetail.Status = 1
   --and isnull(GL_TransactionDetail .Narration,'') not like '%GST%'
   )as tab
	
),0)
/
isnull((
select case when count(POHeaderID) = 0 then 1 else count(POHeaderID) end as POHeaderID from (
select distinct PO.POHeaderID
from Ar_invoiceDetail 
inner join AP_PurchaseOrderHeader PO on po.POHeaderID = Ar_invoiceDetail .PurchaseOrderHeaderID
and po.CompanyID = Ar_invoiceDetail .CompanyID
and po.PeriodID = Ar_invoiceDetail .PeriodID

inner join AP_PaymentHeader on AP_PaymentHeader.POHeaderID = PO.POHeaderID
and AP_PaymentHeader.CompanyID = PO.CompanyID
and AP_PaymentHeader.PeriodID = PO.PeriodID
and AP_PaymentHeader.status = 1 and AP_PaymentHeader.StatusCode = 1

INNER JOIN AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = Ar_invoiceDetail .InvoiceHeaderID
and AR_InvoiceHeader.CompanyID = Ar_invoiceDetail.CompanyID
and AR_InvoiceHeader.PeriodID = Ar_invoiceDetail.PeriodID
inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = AR_InvoiceHeader.InvoiceNumber 
 and GL_TransactionHeader.DocumentTypeID = AR_InvoiceHeader.InvoiceType
 and GL_TransactionHeader.FiscalID = AR_InvoiceHeader.FiscalID
 and GL_TransactionHeader.CompanyID = AR_InvoiceHeader.CompanyID
 and GL_TransactionHeader.PeriodID = AR_InvoiceHeader.PeriodID
 INNER JOIN dbo.GL_TransactionDetail ON dbo.GL_TransactionHeader.TransactionID = dbo.GL_TransactionDetail.TransactionID AND 
							  dbo.GL_TransactionHeader.PeriodID = dbo.GL_TransactionDetail.PeriodID AND 
							  dbo.GL_TransactionHeader.CompanyID = dbo.GL_TransactionDetail.CompanyID
 
 AND (dbo.GL_TransactionDetail.CodeCombinationID in  (SELECT distinct DefaultAccountCode FROM dbo.SET_DocumentType WHERE (DocumentType like 'Advance Against PO%') AND (CompanyID in (2)) AND (PeriodID = 1)))
                     AND 
							  (dbo.GL_TransactionDetail.TransactionType = 'Credit')
where Ar_invoiceDetail.InvoiceHeaderID in (select InvoiceHeaderID from Ar_invoiceDetail where PurchaseOrderHeaderID   =  AP_PurchaseOrderHeader.POHeaderID and CompanyID =  AP_PurchaseOrderHeader.CompanyID)
   and Ar_invoiceDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
   and Ar_invoiceDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
   and Ar_invoiceDetail.Status = 1
   --and isnull(GL_TransactionDetail .Narration,'') not like '%GST%'
   )as tab
	
),0)


as InvoiceAmount




 ,STUFF((SELECT distinct ', ' + CAST(Ar_invoiceHeader.InvoiceNumber AS VARCHAR(10)) as InvoiceNumber
        from Ar_invoiceDetail 
		 inner join Ar_invoiceHeader on Ar_invoiceHeader.InvoiceID = Ar_invoiceDetail.InvoiceHeaderID
		 and Ar_invoiceHeader.CompanyID = Ar_invoiceDetail.CompanyID
		 and Ar_invoiceHeader.PeriodID = Ar_invoiceDetail.PeriodID
		 and Ar_invoiceHeader.FiscalID = Ar_invoiceDetail.FiscalID
		 and Ar_invoiceHeader.Status = 1 and Ar_invoiceHeader.StatusCode = 1
		 where Ar_invoiceDetail.PurchaseOrderHeaderID  = AP_PurchaseOrderHeader.POHeaderID 
		   and Ar_invoiceDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID
		   and Ar_invoiceDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID
		   and Ar_invoiceDetail.Status = 1
         FOR XML PATH(''), TYPE)
        .value('.','NVARCHAR(MAX)'),1,2,' ') InvoiceNumber

		,DATEDIFF(DAY,(SELECT top 1 AP_PaymentHeaderInner.DocumentDate
         FROM AP_PaymentHeader as AP_PaymentHeaderInner
         WHERE 
		 AP_PaymentHeaderInner.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
		 and AP_PaymentHeaderInner.CompanyID = AP_PurchaseOrderHeader.CompanyID
		 and AP_PaymentHeaderInner.PeriodID = AP_PurchaseOrderHeader.PeriodID
		 and AP_PaymentHeaderInner.status = 1 and AP_PaymentHeaderInner.StatusCode = 1 
		 order by AP_PaymentHeaderInner.DocumentDate desc ) , GETDATE()) AS NoOfDays


,AP_PurchaseOrderHeader.CreatedByValue


from AP_PurchaseOrderHeader
inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode
and Set_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID
and Set_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PurchaseOrderHeader.StatusCode = 1

inner join AR_SET_Customer on AR_SET_Customer.CustomerID  = AP_PurchaseOrderHeader.VendorID
and AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID

inner join AP_PaymentHeader on AP_PaymentHeader.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
and AP_PaymentHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID
and AP_PaymentHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID
and AP_PaymentHeader.status = 1 and AP_PaymentHeader.StatusCode = 1


where AP_PaymentHeader.CompanyID in @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID
and AP_PurchaseOrderHeader.status = 1 
and isnull(AdvanceAmountPercentage,0) > 0

{0}

group by AR_SET_Customer.CustomerName
,AP_PurchaseOrderHeader.DocumentNumber
,AP_PurchaseOrderHeader.CompanyID
,AP_PurchaseOrderHeader.PeriodID
,AP_PurchaseOrderHeader.POHeaderID 
,AP_PurchaseOrderHeader.AdvanceAmountPercentage
,AP_PurchaseOrderHeader.CreatedByValue
,AR_SET_Customer.CustomerID


 )as tab
where abs(AdvanceAmount - InvoiceAmount) >= 1
";

                #endregion

                
                if (Customer != "")
                {
                    sb.Append(" and  AP_PurchaseOrderHeader.VendorID =" + Customer + "");
                }

                companyID = "(" + companyID + ")";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_AdvancePOAgingReport, sb.ToString()).Replace("@CompanyID", companyID), sqlparams);

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


        public static DataTable POGRNBalanceReport(int CompanyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CustomerID = 0, int BusinessPartnerType = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@fromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@Todate", dateTo);

                if (CustomerID > 0)
                    sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[4] = new SqlParameter("@CustomerID", DBNull.Value);


                if (CustomerID > 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = " + CustomerID + "");

                if (BusinessPartnerType > 0)
                    sb1.Append(" and AR_SET_BusinessPartnerTypes.TypeCode = " + BusinessPartnerType + " ");
                //else
                //    sb1.Append(" and AR_SET_BusinessPartnerTypes.TypeCode in (1,3,10,16,46,200) ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_POGRNBalanceReport, sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataTable POGRNBalanceReport_New(string CompanyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo, int CustomerID = 0, int BusinessPartnerType = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();

            CompanyID = "(" + CompanyID + ")";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@fromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@Todate", dateTo);

                if (CustomerID > 0)
                    sqlparams[4] = new SqlParameter("@CustomerID", CustomerID);
                else
                    sqlparams[4] = new SqlParameter("@CustomerID", DBNull.Value);


                if (CustomerID > 0)
                    sb.Append(" and AR_SET_Customer.CustomerID = " + CustomerID + "");

                if (BusinessPartnerType > 0)
                    sb1.Append(" and AR_SET_BusinessPartnerTypes.TypeCode = " + BusinessPartnerType + " ");
                //else
                //    sb1.Append(" and AR_SET_BusinessPartnerTypes.TypeCode in (1,3,10,16,46,200) ");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_POGRNBalanceReport_New.Replace("@CompanyID", CompanyID), sb.ToString(), sb1.ToString()), sqlparams);

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

        public static DataSet RawMaterialReport(string CompanyID, int periodID, string orderBy, SqlConnection objSQLConnection = null)
        {
            DataSet dtToReturn = null;
            StringBuilder additionalFilter = new StringBuilder();
            string documentFilter = string.Empty, qry_DocumentAuthorization = string.Empty;

         
            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@CompanyID", CompanyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));
                //sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                //sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                //sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
             
                //SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                //sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                CompanyID = "(" + CompanyID + ")";

                DataSet ds = new DataSet();
                if (objSQLConnection == null)
                {
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_RawMaterial.ToString().Replace("@CompanyID", CompanyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_RawMaterial.ToString().Replace("@CompanyID", CompanyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams);
                }
                else
                {
                    ds = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_RawMaterial.ToString().Replace("@CompanyID", CompanyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_RawMaterial.ToString().Replace("@CompanyID", CompanyID), additionalFilter.ToString(), documentFilter,
                    qry_DocumentAuthorization) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams);
                }


                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds;


                //returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dtToReturn;

        }



        public DataTable GetDashBoard_PendingReqsitions(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null,
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = "";

            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_PendingReqsitions = @" 
select INV_StockTransactionHeader.DocumentNo,INV_StockTransactionHeader.DocumentDate ,SET_DocumentType.DocumentType
,ROW_NUMBER() Over (Order by INV_StockTransactionHeader.DocumentDate  desc) as 'Row' 
from INV_StockTransactionHeader
inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID
and SET_DocumentType.CompanyID = INV_StockTransactionHeader.CompanyID
and  SET_DocumentType.MenuID = 462
left join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.StockRequisitionID = INV_StockTransactionHeader.StockTransactionID
and AP_PurchaseOrderHeader.CompanyID = INV_StockTransactionHeader.CompanyID
where SET_DocumentType.DocumentType not like '%Issuance%'
and AP_PurchaseOrderHeader.StockRequisitionID is null
and INV_StockTransactionHeader.StatusCode = 1 and INV_StockTransactionHeader.status = 1
and INV_StockTransactionHeader.CompanyID = @CompanyID
and INV_StockTransactionHeader.PeriodID = @PeriodID";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingReqsitions) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingReqsitions) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingReqsitions) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingReqsitions) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }




        public static DataTable POGRNBalanceDashboard(string CompanyID, int periodID, Nullable<DateTime> dateFrom, Nullable<DateTime> dateTo)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            StringBuilder sb1 = new StringBuilder();
           
            CompanyID = "(" + CompanyID + ")";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@fromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@Todate", dateTo);


                 #region Query

                string Query_DashBoard_PendingGRN = @"select SUM(GRN_Amount) AS GRNBalance from (
select distinct  AR_SET_Customer.CustomerName as Supplier, INV_StockTransactionDetail.TransactionDetailID,
     INV_StockTransactionDetail.Amount as GRN_Amount 
    
from INV_StockTransactionHeader 
    Inner Join INV_StockTransactionDetail on INV_StockTransactionDetail.StockTransactionID = INV_StockTransactionHeader.StockTransactionID 
        and INV_StockTransactionDetail.CompanyID = INV_StockTransactionHeader.CompanyID 
		and INV_StockTransactionDetail.PeriodID = INV_StockTransactionHeader.PeriodID 
		ANd INV_StockTransactionDetail.Status=1
    inner join SET_DocumentType on SET_DocumentType.DocumentTypeID = INV_StockTransactionHeader.DocumentTypeID and SET_DocumentType.CompanyID=INV_StockTransactionHeader.CompanyID
	and [SET_DocumentType].PeriodID = INV_StockTransactionHeader.PeriodID
	and [SET_DocumentType].Status = 1

inner join [ApplicationMenu] on [ApplicationMenu].MenuID = [SET_DocumentType].MenuID 
LEFT join AP_PurchaseOrderDetail on AP_PurchaseOrderDetail.POHeaderID = INV_StockTransactionDetail.SourceHeaderID 
	and AP_PurchaseOrderDetail.PODetailID = INV_StockTransactionDetail.SourceLineID 
	and AP_PurchaseOrderDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
	and AP_PurchaseOrderDetail.PeriodID = INV_StockTransactionDetail.PeriodID
LEFT join AP_PurchaseOrderHeader on AP_PurchaseOrderHeader.POHeaderID = AP_PurchaseOrderDetail.POHeaderID
	and AP_PurchaseOrderHeader.CompanyID = AP_PurchaseOrderDetail.CompanyID
	and AP_PurchaseOrderHeader.PeriodID = AP_PurchaseOrderDetail.PeriodID
	and AP_PurchaseOrderHeader.Status =1 
INNER join SET_DocumentType [SET_DocumentType2] on [SET_DocumentType2].DocumentTypeID = AP_PurchaseOrderHeader.TypeCode and [SET_DocumentType2].CompanyID=AP_PurchaseOrderHeader.CompanyID
	and [SET_DocumentType2].PeriodID = AP_PurchaseOrderHeader.PeriodID
	and [SET_DocumentType2].Status = 1

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID 
	and AR_SET_Customer.PeriodID  =  AP_PurchaseOrderHeader.PeriodID 
	and AR_SET_Customer.Status = 1	
LEFT Join AR_InvoiceDetail on AR_InvoiceDetail.StockDetailID =  INV_StockTransactionDetail.TransactionDetailID 
	and INV_StockTransactionDetail.StockTransactionID = AR_InvoiceDetail.StockHeaderID
	and INV_StockTransactionDetail.CompanyID = AR_InvoiceDetail.CompanyID 
	and INV_StockTransactionDetail.PeriodID = AR_InvoiceDetail.PeriodID and AR_InvoiceDetail.Status =1 


where INV_StockTransactionHeader.DocumentDate between @FromDate and @ToDate and INV_StockTransactionHeader.Status = 1 and INV_StockTransactionHeader.StatusCode = 1  AND INV_StockTransactionHeader.CompanyID in @CompanyID AND INV_StockTransactionHeader.PeriodID = @PeriodID AND [ApplicationMenu].MenuPath like '%/Stock_frm.aspx' AND ISNULL([INV_StockTransactionHeader].IsReversed, 0) != 1

     group by AR_SET_Customer.CustomerName ,INV_StockTransactionDetail.TransactionDetailID  ,AR_InvoiceDetail.InvoiceHeaderID,AR_InvoiceDetail.InvoiceDetailID,AR_InvoiceDetail.CompanyID,AR_InvoiceDetail.PeriodID,INV_StockTransactionDetail.TransactionQuantity,INV_StockTransactionDetail.Amount

	having isnull((
	select sum(indtl.QuantityInvoiced)
	from AR_InvoiceHeader invHdr
	inner join AR_InvoiceDetail indtl on indtl.InvoiceHeaderID = invHdr.InvoiceID
	and indtl.InvoiceHeaderID = invHdr.InvoiceID
	and indtl.CompanyID = invHdr.CompanyID
	and indtl.PeriodID = invHdr.PeriodID
	and indtl.Status = 1 and invHdr.StatusCode = 1
	where indtl.InvoiceHeaderID = AR_InvoiceDetail.InvoiceHeaderID
	and indtl.InvoiceDetailID = AR_InvoiceDetail.InvoiceDetailID
	and indtl.CompanyID = AR_InvoiceDetail.CompanyID
	and indtl.PeriodID = AR_InvoiceDetail.PeriodID
	 ),0 ) <> INV_StockTransactionDetail.TransactionQuantity

	)tab
 ";

                 #endregion


                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_DashBoard_PendingGRN.Replace("@CompanyID", CompanyID), sqlparams);

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


        public static object GetDataForCostingOfImportedMaterials(int CompanyID, int PeriodID, DateTime? Date_From, DateTime? Date_To,
         string ContractNo, int? documentType, int? SupplierID, int? ItemID = 0, int? CategoryID = 0)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" and  SET_WeightAssessmentHeader.DocumentDate between @FromDate and @ToDate ");
                //}
                //else if (Date_From != null)
                //{
                //    sb.Append(" and SET_WeightAssessmentHeader.DocumentDate = @FromDate ");
                //}
                //else if (Date_From == null)
                //{
                //    sb.Append(" and SET_WeightAssessmentHeader.DocumentDate = @ToDate");
                //}
                if (ItemID > 0)
                    sb.Append(" and AP_SaleContractDetail.ItemID = " + ItemID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_CostingOfImportedMaterials, sb.ToString()), sqlparams); 

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

        public static object  GetDataForImportDetail_Report(int CompanyID, DateTime? Date_From, DateTime? Date_To, int CurrentFiscalID, Nullable <int> DocumentType)
        {
            DataTable dtToReturn = null;
            StringBuilder sb = new StringBuilder();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                //sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[1] = new SqlParameter("@FromDate", Date_From);
                sqlparams[2] = new SqlParameter("@ToDate", Date_To);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (DocumentType.HasValue)
                sb.Append("AND AP_PurchaseOrderHeader.DocumentTypeID=" + DocumentType.Value);
             
               

              //  sqlparams[5] = new SqlParameter("@POHeaderID", POHeaderID);

                //if (Date_From != null && Date_To != null)
                //{
                //    sb.Append(" and  Set_ContainerTransportationandUnloading.CreateDate between @FromDate and @ToDate ");
                //}
                //else if (Date_From != null)
                //{
                //    sb.Append(" and Set_ContainerTransportationandUnloading.CreateDate = @FromDate ");
                //}
                //else if (Date_From == null)
                //{
                //    sb.Append(" and Set_ContainerTransportationandUnloading.CreateDate = @ToDate");
                //}
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PurchaseOrderHeader.Query_ForImportDetail_Report,sb.ToString()), sqlparams); 

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

        public static DataRow GetBPByID(int companyID, int periodID, int CustomerID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                DataSet ds = null;

                string Query_GetData_byKey = @"select AR_SET_BusinessPartner.* from AR_SET_BusinessPartner
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.TypeCode = AR_SET_BusinessPartner.BusinessPartnerID
inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_SET_BusinessPartnerTypes.BusinessPartnerID
where AR_SET_Customer.CustomerID = @CustomerID and AR_SET_Customer.Status = 1 and BusinessPartnerName like 'Advance GST'";

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


        public static bool Get_UpdatePOHeaderClosedCodeForDeleteInvoice(int companyID, int periodID, int InvoiceHeaderID, SqlTransaction objTransaction = null)
        {
            bool result = false;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceHeaderID", InvoiceHeaderID);


                string Query_Get_UpdatePOHeaderClosedCodeForDeleteInvoice = @"update AP_PurchaseOrderHeader Set ClosedCode = 3,ClosedDate = GETDATE() where POHeaderID in (select PurchaseOrderHeaderID from AR_InvoiceDetail where AR_InvoiceDetail.InvoiceHeaderID = @InvoiceHeaderID and AR_InvoiceDetail.CompanyID = @CompanyID) and CompanyID = @CompanyID";

                if (objTransaction == null)
                    SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Get_UpdatePOHeaderClosedCodeForDeleteInvoice, sqlparams);
                else
                    SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Get_UpdatePOHeaderClosedCodeForDeleteInvoice, sqlparams);


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


        public static DataTable GetAdvancePercent(int POHeaderID, int CompanyID,DateTime Date, SqlTransaction objTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@POHeaderID", POHeaderID);
                sqlparams[1] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[2] = new SqlParameter("@Date", Date);

                string Query_GetAdvancePercent = @" 
select sum(AP_POPaymentSchedule.Percentage) as Percentage
from ap_purchaseOrderheader
inner join AP_POPaymentSchedule on AP_POPaymentSchedule.POHeaderID = ap_purchaseOrderheader.POHeaderID
and AP_POPaymentSchedule.CompanyID = ap_purchaseOrderheader.CompanyID
and AP_POPaymentSchedule.Status = 1
where ap_purchaseOrderheader.CompanyID = @CompanyID and AP_PurchaseOrderHeader.POHeaderID = @POHeaderID and AP_POPaymentSchedule.Date <= @Date
and isnull(AP_POPaymentSchedule.IsAdvance,0) = 1
";

                if (objTransaction == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetAdvancePercent, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetAdvancePercent, sqlparams);

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

        public bool Get_UpdatePOClosedCodeForGRNReturn(int companyID, int periodID, int POHeaderID, Nullable<short> ClosedCode, Nullable<DateTime> ClosedDate, string Comments,
            Nullable<Guid> UpdatedBy, Nullable<DateTime> UpdateDate, string UpdatedByValue)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", POHeaderID);

                if (ClosedCode != null)
                    sqlparams[3] = new SqlParameter("@ClosedCode", ClosedCode);
                else
                    sqlparams[3] = new SqlParameter("@ClosedCode", DBNull.Value);

                if (ClosedDate != null)
                    sqlparams[4] = new SqlParameter("@ClosedDate", ClosedDate);
                else
                    sqlparams[4] = new SqlParameter("@ClosedDate", DBNull.Value);

                sqlparams[5] = new SqlParameter("@Comments", Comments);

                if (UpdatedBy != null)
                    sqlparams[6] = new SqlParameter("@UpdatedBy", UpdatedBy);
                else
                    sqlparams[6] = new SqlParameter("@UpdatedBy", DBNull.Value);
                if (UpdateDate != null)
                    sqlparams[7] = new SqlParameter("@UpdateDate", UpdateDate);
                else
                    sqlparams[7] = new SqlParameter("@UpdateDate", DBNull.Value);
                sqlparams[8] = new SqlParameter("@UpdatedByValue", UpdatedByValue);

                string Query_UpdatePOClosedCodeForGRNReturn = @" UPDATE AP_PurchaseOrderHeader 
SET ClosedCode = @ClosedCode, ClosedDate = @ClosedDate, Comments = @Comments, UpdatedBy = @UpdatedBy, UpdateDate = @UpdateDate, UpdatedByValue = @UpdatedByValue 
WHERE CompanyID = @CompanyID and PeriodID = @PeriodID and POHeaderID = @POHeaderID ";

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePOClosedCodeForGRNReturn, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static DataRow GetPurchaseDataByKey(int companyID, int periodID, long pOHeaderID, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", pOHeaderID);

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


        public DataTable GetDashBoard_PendingPOQuantityWise(int companyID, int periodID, ref int returnTotalRows, string orderBy, int? pageIndex = null, int? pageSize = null,
            SqlConnection objSQLConnection = null)
        {
            DataTable dt = null;
            orderBy = " ";


            try
            {
                List<SqlParameter> sqlparamsList = new List<SqlParameter>();
                sqlparamsList.Add(new SqlParameter("@PageIndex", pageIndex));
                sqlparamsList.Add(new SqlParameter("@PageSize", pageSize));
                sqlparamsList.Add(new SqlParameter("@TotalRows", returnTotalRows));
                sqlparamsList.Add(new SqlParameter("@CompanyID", companyID));
                sqlparamsList.Add(new SqlParameter("@PeriodID", periodID));

                SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                sqlparamTotalRows.Direction = ParameterDirection.Output;

                SqlParameter[] sqlparams = sqlparamsList.ToArray();

                #region Query

                string Query_DashBoard_PendingPOs = @" 

SELECT DepartmentName,DocumentType,DocumentNumber,DocumentDate,CustomerName,ROW_NUMBER() Over (Order by DocumentDate DESC) as 'Row'  FROM (

SELECT DISTINCT SET_DocumentType.DocumentType, AP_PurchaseOrderHeader.DocumentNumber,AP_PurchaseOrderHeader .DocumentDate,AR_SET_Customer.CustomerName
,(
SELECT TOP 1 SET_Department .DepartmentName 
FROM  AP_PurchaseOrderRequisition
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderRequisition.RequisitionHeaderID
AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderRequisition.CompanyID
AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderRequisition.PeriodID
LEFT JOIN SET_Department ON SET_Department.DepartmentID = INV_StockTransactionHeader .DepartmentID
WHERE AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
AND AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderHeader.CompanyID
AND AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderHeader.PeriodID
) AS  DepartmentName

FROM AP_PurchaseOrderHeader 

    INNER JOIN AP_PurchaseOrderDetail ON AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
        AND AP_PurchaseOrderDetail.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID AND SET_DocumentType.Status = 1 
    LEFT JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID --AND AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 

    LEFT JOIN INV_StocktransactionDetail ON INV_StocktransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND INV_StocktransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND INV_StocktransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StocktransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID 
        AND INV_StocktransactionDetail.SourceDocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
        AND INV_StocktransactionDetail.SourceCode = AP_PurchaseOrderHeader.DocumentNumber 
    LEFT JOIN INV_StocktransactionHeader ON INV_StocktransactionHeader.StockTransactionID = INV_StocktransactionDetail.StockTransactionID 
        AND INV_StocktransactionDetail.CompanyID = INV_StocktransactionHeader.CompanyID AND INV_StocktransactionDetail.PeriodID = INV_StocktransactionHeader.PeriodID 
    LEFT JOIN AR_InvoiceDetail On AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        And AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID And AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
    left join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        and AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID 
    Left Join AP_PaymentHeader On AP_PaymentHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID 
		And AP_PaymentHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PaymentHeader.SourceHeaderID = AP_PurchaseOrderHeader.POHeaderID 
		And AP_PaymentHeader.Status  = 1
     
WHERE (AP_PurchaseOrderHeader.StatusCode = 1 OR 1 IS NULL)  AND (AP_PurchaseOrderHeader.POHeaderID = null OR null IS NULL) AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND isnull(SET_DocumentType.isimport,0) = 0 AND AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.Status = 1  AND IsNull(AP_PaymentHeader.IsInvoiceAdjusted, 0) = 0 and isnull(AP_PurchaseOrderHeader.ClosedCode,0) != 1    AND [AP_PurchaseOrderHeader].[AcceptanceRequiredFlag] = 0  AND [AP_PurchaseOrderHeader].[UserHoldFlag] = 0  AND [AP_PurchaseOrderHeader].[ApprovalRequiredFlag] = 0  AND [AP_PurchaseOrderHeader].[CancelFlag] = 0  AND [AP_PurchaseOrderHeader].[FrozenFlag] = 0  AND Ap_purchaseorderheader.StatusCode = 1     AND ([AP_PurchaseOrderHeader].[ClosedCode] is null Or [AP_PurchaseOrderHeader].[ClosedCode] = 13 OR [AP_PurchaseOrderHeader].[ClosedCode] = 14)
	)AS tab



";

                #endregion

                if (objSQLConnection == null)
                {
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }
                else
                {
                    dt = SqlHelper.ExecuteDataset(objSQLConnection, CommandType.Text, string.Format(Paging, string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty), string.Format(Query_DashBoard_PendingPOs) + (!string.IsNullOrEmpty(orderBy) ? orderBy : string.Empty)), sqlparams).Tables[0];
                }

                returnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public DataTable GetDashBoard_PendingPOCount(Nullable<int> companyID, Nullable<int> periodID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                #region Query

                const string Query_GetDashBoard_PendingPOCount = @"

SELECT DepartmentName,COUNT(DocumentNumber) AS Count FROM (

SELECT DISTINCT SET_DocumentType.DocumentType, AP_PurchaseOrderHeader.DocumentNumber,AP_PurchaseOrderHeader .DocumentDate,AR_SET_Customer.CustomerName
,(
SELECT TOP 1 SET_Department .DepartmentName 
FROM  AP_PurchaseOrderRequisition
INNER JOIN INV_StockTransactionHeader ON INV_StockTransactionHeader.StockTransactionID = AP_PurchaseOrderRequisition.RequisitionHeaderID
AND INV_StockTransactionHeader.CompanyID = AP_PurchaseOrderRequisition.CompanyID
AND INV_StockTransactionHeader.PeriodID = AP_PurchaseOrderRequisition.PeriodID
LEFT JOIN SET_Department ON SET_Department.DepartmentID = INV_StockTransactionHeader .DepartmentID
WHERE AP_PurchaseOrderRequisition.POHeaderID = AP_PurchaseOrderHeader.POHeaderID
AND AP_PurchaseOrderRequisition.CompanyID = AP_PurchaseOrderHeader.CompanyID
AND AP_PurchaseOrderRequisition.PeriodID = AP_PurchaseOrderHeader.PeriodID
) AS  DepartmentName

FROM AP_PurchaseOrderHeader 

    INNER JOIN AP_PurchaseOrderDetail ON AP_PurchaseOrderDetail.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AP_PurchaseOrderDetail.PeriodID = AP_PurchaseOrderHeader.PeriodID AND AP_PurchaseOrderDetail.POHeaderID = AP_PurchaseOrderHeader.POHeaderID 
        AND AP_PurchaseOrderDetail.Status = 1 
    INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = AP_PurchaseOrderHeader.TypeCode AND SET_DocumentType.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND SET_DocumentType.PeriodID = AP_PurchaseOrderHeader.PeriodID AND SET_DocumentType.Status = 1 
    LEFT JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AP_PurchaseOrderHeader.VendorID --AND AR_SET_Customer.CompanyID = AP_PurchaseOrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = AP_PurchaseOrderHeader.PeriodID 

    LEFT JOIN INV_StocktransactionDetail ON INV_StocktransactionDetail.CompanyID = AP_PurchaseOrderDetail.CompanyID 
        AND INV_StocktransactionDetail.PeriodID = AP_PurchaseOrderDetail.PeriodID AND INV_StocktransactionDetail.SourceHeaderID = AP_PurchaseOrderDetail.POHeaderID 
        AND INV_StocktransactionDetail.SourceLineID = AP_PurchaseOrderDetail.PODetailID 
        AND INV_StocktransactionDetail.SourceDocumentTypeID = AP_PurchaseOrderHeader.TypeCode 
        AND INV_StocktransactionDetail.SourceCode = AP_PurchaseOrderHeader.DocumentNumber 
    LEFT JOIN INV_StocktransactionHeader ON INV_StocktransactionHeader.StockTransactionID = INV_StocktransactionDetail.StockTransactionID 
        AND INV_StocktransactionDetail.CompanyID = INV_StocktransactionHeader.CompanyID AND INV_StocktransactionDetail.PeriodID = INV_StocktransactionHeader.PeriodID 
    LEFT JOIN AR_InvoiceDetail On AR_InvoiceDetail.CompanyID = INV_StockTransactionDetail.CompanyID 
        And AR_InvoiceDetail.PeriodID = INV_StockTransactionDetail.PeriodID And AR_InvoiceDetail.StockDetailID = INV_StockTransactionDetail.TransactionDetailID 
        And AR_InvoiceDetail.StockHeaderID = INV_StockTransactionDetail.StockTransactionID 
    left join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID and AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID 
        and AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID 
    Left Join AP_PaymentHeader On AP_PaymentHeader.CompanyID = AP_PurchaseOrderHeader.CompanyID 
		And AP_PaymentHeader.PeriodID = AP_PurchaseOrderHeader.PeriodID And AP_PaymentHeader.SourceHeaderID = AP_PurchaseOrderHeader.POHeaderID 
		And AP_PaymentHeader.Status  = 1
     
WHERE (AP_PurchaseOrderHeader.StatusCode = 1 OR 1 IS NULL)  AND (AP_PurchaseOrderHeader.POHeaderID = null OR null IS NULL) AND AP_PurchaseOrderHeader.PeriodID = @PeriodID AND isnull(SET_DocumentType.isimport,0) = 0 AND AP_PurchaseOrderHeader.CompanyID = @CompanyID AND AP_PurchaseOrderHeader.Status = 1  AND IsNull(AP_PaymentHeader.IsInvoiceAdjusted, 0) = 0 and isnull(AP_PurchaseOrderHeader.ClosedCode,0) != 1    AND [AP_PurchaseOrderHeader].[AcceptanceRequiredFlag] = 0  AND [AP_PurchaseOrderHeader].[UserHoldFlag] = 0  AND [AP_PurchaseOrderHeader].[ApprovalRequiredFlag] = 0  AND [AP_PurchaseOrderHeader].[CancelFlag] = 0  AND [AP_PurchaseOrderHeader].[FrozenFlag] = 0  AND Ap_purchaseorderheader.StatusCode = 1     AND ([AP_PurchaseOrderHeader].[ClosedCode] is null Or [AP_PurchaseOrderHeader].[ClosedCode] = 13 OR [AP_PurchaseOrderHeader].[ClosedCode] = 14)
	)AS tab
GROUP BY DepartmentName

";
                #endregion

                dt = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetDashBoard_PendingPOCount , sqlparams).Tables[0];

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
