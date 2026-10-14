using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

using DAL.DataAccessLayer;

namespace FnI.BusinessAccessLayer.AR.Transaction
{
    public partial class AR_ReceiptInvoicesDetail : BaseClass
    {
        #region Query 
        
        #endregion

        #region Method

        public DataTable SelectDataFromDetailOnBases(int companyID, int periodID, int receiptHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RHID", receiptHeaderID);

                string Query_SelectDataFromDetailOnBases = @" 
Select SAL_InvoiceHeader.InvoiceNumber AS InvoiceNo, SAL_InvoiceHeader.InvoiceDate, SAL_InvoiceHeader.AmountPaid AS InvoiceAmount, 
    SAL_InvoiceHeader.AmountPaid - 
	(
		Select Sum(AR_ReceiptInvoicesDetail.Amount) From AR_ReceiptInvoicesDetail 
		Where AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            And AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
	) AS BalanceAmount, 
	AR_ReceiptInvoicesDetail.Amount, '' AS Action, SAL_InvoiceHeader.AmountPaid - isnull((
		Select Sum(AR_ReceiptInvoicesDetail.Amount) From AR_ReceiptInvoicesDetail 
		Where AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            And AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
	),
SAL_InvoiceHeader.AmountPaid	
	)as ActualBalance,* 
From AR_ReceiptInvoicesDetail 
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID AND SAL_InvoiceHeader.InvoiceID = AR_ReceiptInvoicesDetail.InvoiceHeaderID 
        AND SAL_InvoiceHeader.Status = 1 
Where AR_ReceiptInvoicesDetail.CompanyID = @CompanyID AND AR_ReceiptInvoicesDetail.PeriodID = @PeriodID AND AR_ReceiptInvoicesDetail.Status = 1 
    AND AR_ReceiptInvoicesDetail.ReceiptHeaderID = @RHID ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataFromDetailOnBases, sqlparams);

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

        public DataTable SelectDataFromDetailOnBasesFind(int companyID, int periodID, int receiptHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RHID", receiptHeaderID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text,FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_SelectDataFromDetailOnBases, sqlparams);

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


        public DataTable GetSavedInvoices(int companyID, int periodID, int receiptHeaderID)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@RHID", receiptHeaderID);

                string Query_SelectDataFromDetailOnBases = @" 
 
Select AR_ReceiptInvoicesDetail.ReceiptInvoicesID , AR_ReceiptInvoicesDetail.ReceiptHeaderID,AR_ReceiptInvoicesDetail.InvoiceHeaderID 
	,SAL_InvoiceHeader.InvoiceNumber AS InvoiceNo, SAL_InvoiceHeader.InvoiceDate, 
	round(SAL_InvoiceHeader.AmountPaid,0)
	-
case  AR_ReceiptTransactionHeader.InvoiceFor  when 'Customer' then 
 (
    --Customer Advance
    isnull((select isnull(sum(Gl_transactionDetail.Amount),0) 
    from Sal_invoiceHeader Sal
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Sal.CustomerID 
inner join Gl_transactionHeader on Gl_transactionHeader.DocumentNo = Sal.invoicenumber 
and Gl_transactionHeader.DocumentTypeID = Sal.invoiceType
and Gl_transactionHeader.FiscalID = Sal.FiscalID
inner join Gl_transactionDetail on Gl_transactionDetail.TransactionID = Gl_transactionHeader.TransactionID
and Gl_transactionDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
where AR_SET_BusinessPartnerTypes.TypeCode = 16
and Sal.InvoiceID = SAL_InvoiceHeader.InvoiceID 
and Sal.Status = 1
),0)
) else 0 end 

	 AS InvoiceAmount
  ,  round(SAL_InvoiceHeader.AmountPaid,0)
    -
	case  AR_ReceiptTransactionHeader.InvoiceFor  when 'Customer' then 
 (
    --Customer Advance
    isnull((select isnull(sum(Gl_transactionDetail.Amount),0) 
    from Sal_invoiceHeader Sal
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Sal.CustomerID 
inner join Gl_transactionHeader on Gl_transactionHeader.DocumentNo = Sal.invoicenumber 
and Gl_transactionHeader.DocumentTypeID = Sal.invoiceType
and Gl_transactionHeader.FiscalID = Sal.FiscalID
inner join Gl_transactionDetail on Gl_transactionDetail.TransactionID = Gl_transactionHeader.TransactionID
and Gl_transactionDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
where AR_SET_BusinessPartnerTypes.TypeCode = 16
and Sal.InvoiceID = SAL_InvoiceHeader.InvoiceID 
and Sal.Status = 1
),0)
) else 0 end 
     as ActualAmount

 ,(round(SAL_InvoiceHeader.AmountPaid,0) 
    -
	 case  AR_ReceiptTransactionHeader.InvoiceFor  when 'Customer' then 
 (
    --Customer Advance
    isnull((select isnull(sum(Gl_transactionDetail.Amount),0) 
    from Sal_invoiceHeader Sal
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Sal.CustomerID 
inner join Gl_transactionHeader on Gl_transactionHeader.DocumentNo = Sal.invoicenumber 
and Gl_transactionHeader.DocumentTypeID = Sal.invoiceType
and Gl_transactionHeader.FiscalID = Sal.FiscalID
inner join Gl_transactionDetail on Gl_transactionDetail.TransactionID = Gl_transactionHeader.TransactionID
and Gl_transactionDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
where AR_SET_BusinessPartnerTypes.TypeCode = 16
and Sal.InvoiceID = SAL_InvoiceHeader.InvoiceID 
and Sal.Status = 1
),0)
    ) else 0 end 
    )
    - 
	(
		Select Sum(AR_ReceiptInvoicesDetail.Amount) From AR_ReceiptInvoicesDetail 
		Where AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            And AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
			and AR_ReceiptInvoicesDetail.Status = 1
	) AS BalanceAmount 
	
	,AR_ReceiptInvoicesDetail.Amount, '' AS Action, round(SAL_InvoiceHeader.AmountPaid,0)
	-
	case  AR_ReceiptTransactionHeader.InvoiceFor  when 'Customer' then 
(
    --Customer Advance
    isnull((select isnull(sum(Gl_transactionDetail.Amount),0) 
    from Sal_invoiceHeader Sal
inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Sal.CustomerID 
inner join Gl_transactionHeader on Gl_transactionHeader.DocumentNo = Sal.invoicenumber 
and Gl_transactionHeader.DocumentTypeID = Sal.invoiceType
and Gl_transactionHeader.FiscalID = Sal.FiscalID
inner join Gl_transactionDetail on Gl_transactionDetail.TransactionID = Gl_transactionHeader.TransactionID
and Gl_transactionDetail.CodeCombinationID = AR_SET_BusinessPartnerTypes.FK_CodeCombinationID
where AR_SET_BusinessPartnerTypes.TypeCode = 16
and Sal.InvoiceID = SAL_InvoiceHeader.InvoiceID 
and Sal.Status = 1
),0)
) else 0 end 
	 - isnull((
		Select Sum(AR_ReceiptInvoicesDetail.Amount) From AR_ReceiptInvoicesDetail 
		Where AR_ReceiptInvoicesDetail.CompanyID = SAL_InvoiceHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = SAL_InvoiceHeader.PeriodID 
            And AR_ReceiptInvoicesDetail.InvoiceHeaderID = SAL_InvoiceHeader.InvoiceID
            and AR_ReceiptInvoicesDetail.status = 1

	),
round(SAL_InvoiceHeader.AmountPaid	,0)
	)
	
	as ActualBalance

From AR_ReceiptInvoicesDetail
    INNER JOIN SAL_InvoiceHeader ON SAL_InvoiceHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID 
        AND SAL_InvoiceHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID AND SAL_InvoiceHeader.InvoiceID = AR_ReceiptInvoicesDetail.InvoiceHeaderID 
        AND SAL_InvoiceHeader.Status = 1 
inner join AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptInvoicesDetail.ReceiptHeaderID
		and AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID
		and AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID
        and AR_ReceiptTransactionHeader.status = 1
Where AR_ReceiptInvoicesDetail.CompanyID = @CompanyID AND AR_ReceiptInvoicesDetail.PeriodID = @PeriodID AND AR_ReceiptInvoicesDetail.Status = 1 
    AND AR_ReceiptInvoicesDetail.ReceiptHeaderID = @RHID  ";

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_SelectDataFromDetailOnBases, sqlparams);

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

        public int Get_RecieptAmount_Against_RecieptDirectInvoice(int companyID, int periodID, int documentNo , int documentID)
        {
            int ID = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceNumber", documentNo);
                sqlparams[3] = new SqlParameter("@InvoiceID", documentID);

                string Query_Get_RecieptAmount_Against_RecieptDirectInvoice = @" Select ISNull(Sum(AR_ReceiptInvoicesDetail.Amount), 0) As Amount 
From AR_ReceiptInvoicesDetail 
    Inner Join AR_ReceiptTransactionHeader ON AR_ReceiptInvoicesDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
        and AR_ReceiptInvoicesDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        and AR_ReceiptTransactionHeader.Status = 1 
    Inner Join Sal_InvoiceHeader On Sal_InvoiceHeader.InvoiceID = AR_ReceiptInvoicesDetail.InvoiceHeaderID 
        and Sal_InvoiceHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID and Sal_InvoiceHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID 
        and Sal_InvoiceHeader.Status = 1 
Where Sal_InvoiceHeader.InvoiceNumber = @InvoiceNumber AND AR_ReceiptInvoicesDetail.CompanyID = @CompanyID And AR_ReceiptInvoicesDetail.PeriodID = @PeriodID 
    And AR_ReceiptInvoicesDetail.Status = 1 AND Sal_InvoiceHeader.InvoiceID = @InvoiceID ";

                ID = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_RecieptAmount_Against_RecieptDirectInvoice, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return ID;
        }

        public static decimal Get_RecieptAmount_Against_RecieptDirectInvoiceForFlag(int companyID, int periodID, int invoiceHeaderID, 
            SqlTransaction objTransaction = null)
        {
            decimal amountToReturn = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", invoiceHeaderID);

                string Query_Get_RecieptAmount_Against_RecieptDirectInvoiceForFlagUpdatation = @" 
Select ISNull(Sum(AR_ReceiptInvoicesDetail.Amount), 0) As Amount From AR_ReceiptInvoicesDetail 
    Inner Join AR_ReceiptTransactionHeader ON AR_ReceiptInvoicesDetail.ReceiptHeaderID = AR_ReceiptTransactionHeader.ReceiptTransactionID 
        and AR_ReceiptInvoicesDetail.CompanyID = AR_ReceiptTransactionHeader.CompanyID and AR_ReceiptInvoicesDetail.PeriodID = AR_ReceiptTransactionHeader.PeriodID 
        and AR_ReceiptTransactionHeader.Status = 1 
    Inner Join Sal_InvoiceHeader On Sal_InvoiceHeader.InvoiceID = AR_ReceiptInvoicesDetail.InvoiceHeaderID 
        and Sal_InvoiceHeader.CompanyID = AR_ReceiptInvoicesDetail.CompanyID and Sal_InvoiceHeader.PeriodID = AR_ReceiptInvoicesDetail.PeriodID 
        and Sal_InvoiceHeader.Status = 1 
Where AR_ReceiptInvoicesDetail.CompanyID = @CompanyID And AR_ReceiptInvoicesDetail.PeriodID = @PeriodID And AR_ReceiptInvoicesDetail.Status = 1 
    AND Sal_InvoiceHeader.InvoiceID = @InvoiceID ";

                if (objTransaction == null)
                {
                    amountToReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_Get_RecieptAmount_Against_RecieptDirectInvoiceForFlagUpdatation,
                        sqlparams));
                }
                else
                {
                    amountToReturn = Convert.ToInt32(SqlHelper.ExecuteScalar(objTransaction, CommandType.Text,
                        Query_Get_RecieptAmount_Against_RecieptDirectInvoiceForFlagUpdatation, sqlparams));
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amountToReturn;
        }

        public DataTable Get_Detail_ContractorWiseReceivable_Report(int companyID, int periodID, Nullable<DateTime> dateTo, Nullable<DateTime> dateFrom, 
            int? batchID = null, int? customerID = null, int? itemID = null, int? docTypeId = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();

            #region Query

            string Query_Get_Detail_ContractorWiseReceivable_Report = @" 
Select Sal_OrderHeader.CustomerID, CustomerName BusinessPartnerName, Season, Cast(Convert(Varchar(10), ContractDate, 110) as Date) ContractDate, ContractNo, 
    TotalAmount ContractAmount, IsNull(AR_ReceiptTransactionHeader.Amount, 0) + IsNull(tblAdvanceHeader.Amount, 0) RecievedAmount 
from Sal_OrderHeader 
    Left JOIN AR_SET_Customer on AR_SET_Customer.CustomerID = Sal_OrderHeader.CustomerID --AND AR_SET_Customer.CompanyID = Sal_OrderHeader.CompanyID 
        AND AR_SET_Customer.PeriodID = Sal_OrderHeader.PeriodID 
    JOIN SAL_OrderDetail on SAL_OrderDetail.OrderID = Sal_OrderHeader.OrderID AND SAL_OrderDetail.CompanyID = Sal_OrderHeader.CompanyID 
        AND SAL_OrderDetail.PeriodID = Sal_OrderHeader.PeriodID 
    Left JOIN SAL_DeliveryOrderDetail on SAL_DeliveryOrderDetail.SalesOrderHeaderID = SAL_OrderDetail.OrderID 
        AND SAL_DeliveryOrderDetail.SalesOrderDetailID = SAL_OrderDetail.OrderDetailID AND SAL_DeliveryOrderDetail.CompanyID = SAL_OrderDetail.CompanyID 
        AND SAL_DeliveryOrderDetail.PeriodID = SAL_OrderDetail.PeriodID 
    LEFT JOIN Inv_StockTransactionDetail on Inv_StockTransactionDetail.SourceHeaderID = SAL_DeliveryOrderDetail.DOHeaderID 
        AND Inv_StockTransactionDetail.SourceLineID = SAL_DeliveryOrderDetail.DODetailID AND Inv_StockTransactionDetail.CompanyID = SAL_DeliveryOrderDetail.CompanyID 
        AND Inv_StockTransactionDetail.PeriodID = SAL_DeliveryOrderDetail.PeriodID 
    LEFT JOIN SAL_InvoiceDetail on SAL_InvoiceDetail.StockHeaderID = Inv_StockTransactionDetail.StockTransactionID 
        AND SAL_InvoiceDetail.StockDetailID = Inv_StockTransactionDetail.TransactionDetailID AND SAL_InvoiceDetail.CompanyID = Inv_StockTransactionDetail.CompanyID 
        AND SAL_InvoiceDetail.PeriodID = Inv_StockTransactionDetail.PeriodID 
    ---------- Advance Detail ----------
    LEFT JOIN AR_ReceiptTransactionHeader tblAdvanceHeader on tblAdvanceHeader.SourceHeaderID = Sal_OrderHeader.OrderID 
        AND tblAdvanceHeader.CompanyID = Sal_OrderHeader.CompanyID AND tblAdvanceHeader.PeriodID = Sal_OrderHeader.PeriodID 
        AND tblAdvanceHeader.DocumentTypeID = @DocumentTypeID 
    ---------- Reciept Detail ----------
    LEFT JOIN AR_ReceiptTransactionDetail on AR_ReceiptTransactionDetail.InvoiceHeaderID = SAL_InvoiceDetail.InvoiceHeaderID 
        AND AR_ReceiptTransactionDetail.InvoiceDetailID	= SAL_InvoiceDetail.InvoiceDetailID AND AR_ReceiptTransactionDetail.CompanyID = SAL_InvoiceDetail.CompanyID 
        AND AR_ReceiptTransactionDetail.PeriodID = SAL_InvoiceDetail.PeriodID 
    LEFT JOIN AR_ReceiptTransactionHeader on AR_ReceiptTransactionHeader.ReceiptTransactionID = AR_ReceiptTransactionDetail.ReceiptHeaderID 
        AND AR_ReceiptTransactionHeader.CompanyID = AR_ReceiptTransactionDetail.CompanyID 
        AND AR_ReceiptTransactionHeader.PeriodID = AR_ReceiptTransactionDetail.PeriodID 
where Sal_OrderHeader.CompanyID = @CompanyID AND Sal_OrderHeader.PeriodID = @PeriodID AND (Sal_OrderHeader.Season = @BatchID Or @BatchID Is NULL) 
    AND (Sal_OrderHeader.CustomerID = @CustomerID Or @CustomerID Is NULL) AND (SAL_OrderDetail.ItemID = @ItemID Or @ItemID Is NULL) ";

            #endregion

            sb.Append(Query_Get_Detail_ContractorWiseReceivable_Report);

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                if (batchID.HasValue)
                    sqlparams[2] = new SqlParameter("@BatchID", batchID);
                else
                    sqlparams[2] = new SqlParameter("@BatchID", DBNull.Value);
                if (customerID.HasValue)
                    sqlparams[3] = new SqlParameter("@CustomerID", customerID);
                else
                    sqlparams[3] = new SqlParameter("@CustomerID", DBNull.Value);
                if (itemID.HasValue)
                    sqlparams[4] = new SqlParameter("@ItemID", itemID);
                else
                    sqlparams[4] = new SqlParameter("@ItemID", DBNull.Value);
                sqlparams[5] = new SqlParameter("@DocumentTypeID", docTypeId);


                if (dateTo.HasValue)
                    sb.AppendLine(" AND CAST(Convert(Varchar(10),ContractDate,110) as Date) <= '" + dateTo.Value.ToString("yyyy-MM-dd") + "'");

                if (dateFrom.HasValue)
                    sb.AppendLine(" AND CAST(Convert(Varchar(10),ContractDate,110) as Date) >= '" + dateFrom.Value.ToString("yyyy-MM-dd") + "'");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, sb.ToString(), sqlparams);

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


        public static DataTable GetPaymentReceivingSummary_Report(int companyID, int periodID, int CustomerID, int Paytype, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, string InvoiceFor ="", SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;
            StringBuilder sb = new StringBuilder();
            
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyId", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", fromDate);
                sqlparams[3] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;
                if (fromDate != null && toDate != null)
                {
                    //sb.Append(" and convert(date,AR_ReceiptTransactionHeader.ReceiptDate) between @FromDate AND @ToDate ");
                }
                else if (fromDate != null)
                {
                    //sb.Append(" and convert(date,AR_ReceiptTransactionHeader.ReceiptDate) = @FromDate ");
                }
                else if (fromDate == null)
                {
                    //sb.Append(" and convert(date,AR_ReceiptTransactionHeader.ReceiptDate) = @ToDate");
                }

                if (CustomerID != 0)
                    sb.Append(" and tab.CustomerID =  " + CustomerID + "");
                if (Paytype != -1)
                    sb.Append(" And  tab.PaymentTypeID  = " + Paytype + "");

                if (InvoiceFor != "")
                    sb.Append(" And  tab.InvoiceFor  Like '" + InvoiceFor + "'");

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetPaymentReceivingSummary_Report, sb), sqlparams);

                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetPaymentReceivingSummary_Report, sb), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.Sales.Sal_InvoiceHeader.Query_GetPaymentReceivingSummary_Report, sb), sqlparams);

                

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

        public static bool DeleteDataByReceiptTransactionHeaderID(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int periodID, int receiptHeaderID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ReceiptHeaderID", receiptHeaderID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_Delete_ByReceiptTransactionHeaderID = @" Update [AR_ReceiptInvoicesDetail] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [ReceiptHeaderID] = @ReceiptHeaderID ";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_Delete_ByReceiptTransactionHeaderID, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_Delete_ByReceiptTransactionHeaderID, sqlparams) > 0)
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
