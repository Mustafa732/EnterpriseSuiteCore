using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;

using Common.Utils;

using DAL.DataAccessLayer;

using FnI.BusinessAccessLayer.AR.Setup;
using FnI.BusinessAccessLayer.AR.Transaction;
using FnI.BusinessAccessLayer.GL.Transaction;
using FnI.BusinessAccessLayer.Setup;
using FnI.BusinessAccessLayer.AP.Setup;

namespace FnI.BusinessAccessLayer.AP.Transaction
{
    public partial class AP_PaymentHeader : BaseClass
    {
        #region Class Methods

        public bool UpdateIsInvoiceAdjusted(int companyID, int periodID, int paymentid, bool status)
        {
            bool result = false;
            string Query_UpdateIsInvoiceAdjusted = @" Update AP_PaymentHeader Set IsInvoiceAdjusted = @status 
Where Status = 1 And CompanyId = @CompanyID And PeriodID = @PeriodID And PaymentID = @PID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PID", paymentid);
                sqlparams[3] = new SqlParameter("@status", status);



                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateIsInvoiceAdjusted, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public static bool UpdatePaymentFlagForPaymentTax(int companyID, int periodID, int PaymentHeaderID, int PaymentDetailAccountID, string CodeCombinationID)
        {
            bool result = false;
            string Query_UpdatePaymentFlagForPaymentTax = @" update AP_Payment_AccountsDetail SET IsMarked = 1 
where PaymentHeaderID = @PaymentHeaderID and CompanyID = @CompanyID and PeriodID = @PeriodID and Status = 1 and PaymentDetailAccountID = @PaymentDetailAccountID 
    and CodeCombinationID = @CodeCombinationID and TransactionType = 'Debit' and IsMarked is null ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];


                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentHeaderID", PaymentHeaderID);
                sqlparams[3] = new SqlParameter("@PaymentDetailAccountID", PaymentDetailAccountID);
                sqlparams[4] = new SqlParameter("@CodeCombinationID", CodeCombinationID);

                if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdatePaymentFlagForPaymentTax, sqlparams) > 0)
                    result = true;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable GetPaymentForPaymentTaxLookup(int CompanyID, int PeriodID, int @ChartOfAccountID)
        {
            DataTable dtPaymentReceipt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ChartOfAccountID", @ChartOfAccountID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentForPaymentTaxLookup, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }
            return dtPaymentReceipt;
        }

        public DataTable GetPaymentForPaymentLookup(int companyID, int periodID, Guid userID, int? SupplierID, string Fillter)
        {
            DataTable dtPaymentReceipt = null;
            StringBuilder sp = new StringBuilder();

            if (SupplierID.HasValue)
                sp.Append(" and AP_PaymentHeader.paymentid in (  select paymentheaderid from AP_PaymentSupplierDetail  where supplierid = " + SupplierID.Value + " ) ");

            if (Fillter != "")
                sp.Append(Fillter);



            string Query_GetPaymentForPaymentLookup = @" 

                ";


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);

                //DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetPaymentForPaymentLookup, sp), sqlparams);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentForPaymentLookup, sp), sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtPaymentReceipt;
        }

        public DataSet GetPaymentReceiptReport(int companyID, int periodId, int? NoFrom, int? NoTo, int? documentTypeId, int? paymentID,
            int? PaymentFormDocumentTypeID, int FiscalID = 0)
        {
            DataSet ds1 = new DataSet();

            #region Query

            #endregion

            try
            {
                StringBuilder query = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (NoFrom.HasValue && NoTo.HasValue)
                {
                    query.Append(" AND ([AP_PaymentHeader].[DocumentNo] >= " + NoFrom + " AND [AP_PaymentHeader].[DocumentNo] <= " + NoTo + " ) ");
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] between " + NoFrom + " AND " + NoTo + "  ");
                }

                if (paymentID.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[PaymentID] = " + paymentID);

                if (PaymentFormDocumentTypeID.HasValue)
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + PaymentFormDocumentTypeID);
                else
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + documentTypeId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentReceiptReortData, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds1 = ds;
                    ds.Tables[0].TableName = "PurchaseOrder";
                    ds.Tables[1].TableName = "Table1";
                    ds.Tables[2].TableName = "GL_Voucher";
                    ds.Tables[3].TableName = "GL_Voucher2";
                    ds.Tables[4].TableName = "SupplierDTL";
                }
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataSet GetPaymentReceiptReport_RSM(int companyID, int periodId, int? NoFrom, int? NoTo, int? documentTypeId, int? paymentID,
            int? PaymentFormDocumentTypeID, int FiscalID = 0)
        {
            DataSet ds1 = new DataSet();

            #region Query

            #endregion

            try
            {
                StringBuilder query = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (NoFrom.HasValue && NoTo.HasValue)
                {
                    query.Append(" AND ([AP_PaymentHeader].[DocumentNo] >= " + NoFrom + " AND [AP_PaymentHeader].[DocumentNo] <= " + NoTo + " ) ");
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] between " + NoFrom + " AND " + NoTo + "  ");
                }

                if (paymentID.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[PaymentID] = " + paymentID);

                if (PaymentFormDocumentTypeID.HasValue)
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + PaymentFormDocumentTypeID);
                else
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + documentTypeId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentReceiptReportDataRSM, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds1 = ds;
                    ds.Tables[0].TableName = "PurchaseOrder";
                    ds.Tables[1].TableName = "Table1";
                    ds.Tables[2].TableName = "GL_Voucher";
                    ds.Tables[3].TableName = "GL_Voucher2";
                    ds.Tables[4].TableName = "SupplierDTL";
                }
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataSet GetPaymentReceiptReportforSKU(int companyID, int periodId, int? NoFrom, int? NoTo, int? documentTypeId, int? paymentID,
            int? PaymentFormDocumentTypeID, int FiscalID = 0)
        {
            DataSet ds1 = new DataSet();

            #region Query

            #endregion

            try
            {
                StringBuilder query = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", FiscalID);

                if (NoFrom.HasValue && NoTo.HasValue)
                {
                    query.Append(" AND ([AP_PaymentHeader].[DocumentNo] >= " + NoFrom + " AND [AP_PaymentHeader].[DocumentNo] <= " + NoTo + " ) ");
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] between " + NoFrom + " AND " + NoTo + "  ");
                }

                if (paymentID.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[PaymentID] = " + paymentID);

                if (PaymentFormDocumentTypeID.HasValue)
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + PaymentFormDocumentTypeID);
                else
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + documentTypeId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentReceiptReortDataSKU, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds1 = ds;
                    ds.Tables[0].TableName = "PurchaseOrder";
                    ds.Tables[1].TableName = "Table1";
                    ds.Tables[2].TableName = "GL_Voucher";
                    ds.Tables[3].TableName = "GL_Voucher2";
                    ds.Tables[4].TableName = "SupplierDTL";
                    ds.Tables[5].TableName = "InvoiceDTL";
                }
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public DataTable GetAllDataWithOtherTables_ReceiptHeader(string strWhere, int CurrentFiscalID = 0)
        {
            DataTable dtToReturn = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[1];
                sqlparams[0] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTables + strWhere, sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return (dtToReturn);

        }

        public DataTable GetAllDataWithOtherTables(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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

                    string Query_SelectAll_WithPaging_WithOtherTables = @" 
Select *,ROW_NUMBER() Over (Order by paymentid desc) as 'Row'  from
(
SELECT distinct AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.PaymentID, AP_PaymentHeader.DocumentTypeID, AP_PaymentHeader.DocumentNo, 
    AP_PaymentHeader.DocumentDate, AP_PaymentHeader.AddressVerificationCode, AP_PaymentHeader.Amount,AP_PaymentHeader.ControlCredit, AP_PaymentHeader.AntiCipatedClearingDate, 
    AP_PaymentHeader.PayFromCustomerID, AP_PaymentHeader.PaymentServerOrderNo, AP_PaymentHeader.PostMarkDate, AP_PaymentHeader.ProgramID, 
    AP_PaymentHeader.ReceiptDate, AP_PaymentHeader.ReceiptMethodID, AP_PaymentHeader.ReceiptNo, AP_PaymentHeader.Comments, AP_PaymentHeader.CurrencyId, 
    AP_PaymentHeader.BankAccountID, AP_PaymentHeader.CustomerBankBranchID, AP_PaymentHeader.CustomerReceiptReference, AP_PaymentHeader.DepositDate, 
    AP_PaymentHeader.ExchangeDate, AP_PaymentHeader.ExchangeRate, AP_PaymentHeader.ExchangeRateType, AP_PaymentHeader.FactorDiscountAmount, 
    AP_PaymentHeader.IssueDate, AP_PaymentHeader.IssuerBankBranchID, AP_PaymentHeader.IssuerName, AP_PaymentHeader.OverRemitAccountFlag, 
    AP_PaymentHeader.ReceivablesTrxID, AP_PaymentHeader.Reference, AP_PaymentHeader.ReferenceType, AP_PaymentHeader.RemittanceBankAccount, 
    AP_PaymentHeader.ReversalCategory, AP_PaymentHeader.ReversalComments, AP_PaymentHeader.ReversalDate, AP_PaymentHeader.ReversalReasonCode, 
    AP_PaymentHeader.SelectedForFactoringFlag, AP_PaymentHeader.SelectedRemittanceBatchID, AP_PaymentHeader.ApprovalCode, AP_PaymentHeader.StatusCode, 
    AP_PaymentHeader.OverrideRemitAccountFlag, AP_PaymentHeader.ConfirmedFlag, AP_PaymentHeader.Status, AP_PaymentHeader.CreatedBy, AP_PaymentHeader.CreateDate, 
    AP_PaymentHeader.UpdatedBy, AP_PaymentHeader.UpdateDate, AP_PaymentHeader.CreatedByValue, AP_PaymentHeader.UpdatedByValue, AR_SET_Customer.CustomerID, 
   -- AR_SET_Customer.CustomerName,
 isnull(AR_SET_Customer.CustomerName,ISNULL(
        (
            SELECT  STUFF(
		    (
                SELECT distinct ', ' +  AR_SET_Customer.CustomerName 
                FROM AP_PaymentSupplierDetail 
				inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PaymentSupplierDetail.SupplierID
                WHERE payment1.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID 
                and payment1.CompanyID = AP_PaymentSupplierDetail.CompanyID
				and AP_PaymentSupplierDetail.Status = 1
                --ORDER BY AP_PaymentSupplierDetail.SupplierID
				 FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS CustomerName
            FROM AP_PaymentHeader payment1 
            where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
        ), '')) as CustomerName,
SET_DocumentType.DocumentType, Set_PeriodCustom.PeriodSetName
--ROW_NUMBER() Over (Order by AP_PaymentHeader.paymentid desc) as 'Row' 
FROM AP_PaymentHeader 
    INNER JOIN SET_DocumentType ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND AP_PaymentHeader.Status = 1 
        and AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID AND AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID
    left outer JOIN AR_SET_Customer ON AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID 
        --AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
        AND AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID AND AR_SET_Customer.Status = 1
		inner join Set_PeriodCustom on AP_PaymentHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
left join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
		and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
		and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
		and AP_PaymentSupplierDetail.InvoiceNumber is not null

left join AP_PaymentInvoicesDetail on AP_PaymentHeader.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID
 and AP_PaymentHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
 and AP_PaymentHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
 and AP_PaymentInvoicesDetail.status = 1 
left JOIN AR_InvoiceHeader ON AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
	AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
	AND AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND AR_InvoiceHeader.Status = 1

{0}

) as tab
";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_WithOtherTables + ' ' + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_WithOtherTables + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTables + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTables_RSM(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0, string UserID = "")
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
            sqlparamsList.Add(new SqlParameter("@UserID", UserID));

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAll_WithPaging_WithOtherTables_RSM = @" 
Select *,ROW_NUMBER() Over (Order by paymentid desc) as 'Row'  from
(
SELECT distinct AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.PaymentID, AP_PaymentHeader.DocumentTypeID, AP_PaymentHeader.DocumentNo, 
    AP_PaymentHeader.DocumentDate, AP_PaymentHeader.AddressVerificationCode, AP_PaymentHeader.Amount,AP_PaymentHeader.ControlCredit, AP_PaymentHeader.AntiCipatedClearingDate, 
    AP_PaymentHeader.PayFromCustomerID, AP_PaymentHeader.PaymentServerOrderNo, AP_PaymentHeader.PostMarkDate, AP_PaymentHeader.ProgramID, 
    AP_PaymentHeader.ReceiptDate, AP_PaymentHeader.ReceiptMethodID, AP_PaymentHeader.ReceiptNo, AP_PaymentHeader.Comments, AP_PaymentHeader.CurrencyId, 
    AP_PaymentHeader.BankAccountID, AP_PaymentHeader.CustomerBankBranchID, AP_PaymentHeader.CustomerReceiptReference, AP_PaymentHeader.DepositDate, 
    AP_PaymentHeader.ExchangeDate, AP_PaymentHeader.ExchangeRate, AP_PaymentHeader.ExchangeRateType, AP_PaymentHeader.FactorDiscountAmount, 
    AP_PaymentHeader.IssueDate, AP_PaymentHeader.IssuerBankBranchID, AP_PaymentHeader.IssuerName, AP_PaymentHeader.OverRemitAccountFlag, 
    AP_PaymentHeader.ReceivablesTrxID, AP_PaymentHeader.Reference, AP_PaymentHeader.ReferenceType, AP_PaymentHeader.RemittanceBankAccount, 
    AP_PaymentHeader.ReversalCategory, AP_PaymentHeader.ReversalComments, AP_PaymentHeader.ReversalDate, AP_PaymentHeader.ReversalReasonCode, 
    AP_PaymentHeader.SelectedForFactoringFlag, AP_PaymentHeader.SelectedRemittanceBatchID, AP_PaymentHeader.ApprovalCode, AP_PaymentHeader.StatusCode, 
    AP_PaymentHeader.OverrideRemitAccountFlag, AP_PaymentHeader.ConfirmedFlag, AP_PaymentHeader.Status, AP_PaymentHeader.CreatedBy, AP_PaymentHeader.CreateDate, 
    AP_PaymentHeader.UpdatedBy, AP_PaymentHeader.UpdateDate, AP_PaymentHeader.CreatedByValue, AP_PaymentHeader.UpdatedByValue, AR_SET_Customer.CustomerID, 
   -- AR_SET_Customer.CustomerName,
 isnull(AR_SET_Customer.CustomerName,ISNULL(
        (
            SELECT  STUFF(
		    (
                SELECT distinct ', ' +  AR_SET_Customer.CustomerName 
                FROM AP_PaymentSupplierDetail 
				inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AP_PaymentSupplierDetail.SupplierID
                WHERE payment1.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID 
                and payment1.CompanyID = AP_PaymentSupplierDetail.CompanyID
				and AP_PaymentSupplierDetail.Status = 1
                --ORDER BY AP_PaymentSupplierDetail.SupplierID
				 FOR XML PATH(''), TYPE
            ).value('.', 'varchar(max)'), 1, 2, '') AS CustomerName
            FROM AP_PaymentHeader payment1 
            where payment1.PaymentID = AP_PaymentHeader.PaymentID and payment1.CompanyID = AP_PaymentHeader.CompanyID and payment1.PeriodID = AP_PaymentHeader.PeriodID
        ), '')) as CustomerName,
SET_DocumentType.DocumentType, Set_PeriodCustom.PeriodSetName
--ROW_NUMBER() Over (Order by AP_PaymentHeader.paymentid desc) as 'Row' 
FROM AP_PaymentHeader 
    INNER JOIN SET_DocumentType ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND AP_PaymentHeader.Status = 1 
        and AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID AND AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID AND SET_DocumentType.DocumentTypeID NOT IN (4,5,143,68,69) -- Excluding Payment Against Direct Invoices
    Inner Join UsersInDocumentType ON UsersInDocumentType.Documenttypeid = SET_DocumentType.DocumentTypeID 
        AND UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID AND UsersInDocumentType.UserID = @UserID
    left outer JOIN AR_SET_Customer ON AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID 
        --AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
        AND AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID AND AR_SET_Customer.Status = 1
		inner join Set_PeriodCustom on AP_PaymentHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
left join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
		and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
		and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
		and AP_PaymentSupplierDetail.InvoiceNumber is not null

left join AP_PaymentInvoicesDetail on AP_PaymentHeader.PaymentID = AP_PaymentInvoicesDetail.PaymentHeaderID
 and AP_PaymentHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
 and AP_PaymentHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
 and AP_PaymentInvoicesDetail.status = 1 
left JOIN AR_InvoiceHeader ON AR_InvoiceHeader.CompanyID = AP_PaymentInvoicesDetail.CompanyID
	AND AR_InvoiceHeader.PeriodID = AP_PaymentInvoicesDetail.PeriodID
	AND AR_InvoiceHeader.InvoiceID = AP_PaymentInvoicesDetail.InvoiceHeaderID
	AND AR_InvoiceHeader.Status = 1

LEFT JOIN AP_PaymentBanksDetail on AP_PaymentBanksDetail.PaymentHeaderID = [AP_PaymentHeader].PaymentID
AND AP_PaymentBanksDetail.CompanyID = [AP_PaymentHeader].CompanyID
AND AP_PaymentBanksDetail.PeriodID = [AP_PaymentHeader].PeriodID
AND AP_PaymentBanksDetail.FiscalID = [AP_PaymentHeader].FiscalID
AND AP_PaymentBanksDetail.Status = 1

LEFT join AR_SET_BankBranch on AR_SET_BankBranch.BankBranchID = AP_PaymentBanksDetail.BankBranchID
LEFT join AR_SET_BankAccount on AR_SET_BankAccount.BankAccountID = AP_PaymentBanksDetail.BankAccountID

{0}
) as tab
";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables_RSM, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables_RSM, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_WithOtherTables + ' ' + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_WithOtherTables + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTables_RSM + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public DataTable GetAllDataWithOtherTablesDirectInvoicePayment(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0, string UserID = "")
        {
            List<SqlParameter> sqlparamsList = new List<SqlParameter>();
            OrderBy = (!string.IsNullOrEmpty(OrderBy) ? " Order By [" + OrderBy + "] " + sortdirection.ToString() : String.Empty);
            DataTable dt = null;

            sqlparamsList.Add(new SqlParameter("@CurrentFiscalID", CurrentFiscalID));
            sqlparamsList.Add(new SqlParameter("@UserID", UserID));

            try
            {
                if (PageIndex != null)
                {
                    #region Query

                    string Query_SelectAll_WithPaging_WithOtherTables_DirectInvoicePayment = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithPaging_WithOtherTables_DirectInvoicePayment.ToString();

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables_DirectInvoicePayment, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables_DirectInvoicePayment, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_WithOtherTables + ' ' + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_WithOtherTables + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTablesDirectInvoicePayment + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool InsertGLHeaderAndDetailByPaymentTax(List<GL_TransactionHeader> colth, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPayment");

            try
            {
                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                foreach (GL_TransactionHeader thh in colth)
                {
                    int TempTransactionID = 0;

                    #region Insert In GL_TransactionHeader
                    TempTransactionID = thh.TransactionID;
                    thh.TransactionID = GL_TransactionHeader.GetNewTransactionID(CompanyID, PeriodID, sqltrans);
                    thh.FiscalID = CurrentFiscalID;
                    thh.Insert(sqltrans);

                    #endregion

                    #region Insert In GL_TransactionDetail

                    decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    if (AmountDebit != AmountCredit)
                        throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        if (td.TransactionID == TempTransactionID && td.Amount > 0)
                        {
                            td.TransactionID = thh.TransactionID;
                            td.Insert(sqltrans);
                            //to avoid old record
                            td.TransactionID = 0;

                            td.Amount = 0;
                        }
                        td.FiscalID = CurrentFiscalID;
                    }

                    #endregion
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPayment");
            }

            return result;
        }

        public bool InsertPaymentHeaderAndDetail(List<AP_PaymentDetail> colPaymentDetail, List<AP_PaymentInvoicesDetail> colPaymentInvoicesDetail,
            List<AP_PaymentBanksDetail> lst_PaymentBanks, List<AP_PaymentSupplierDetail> lst_PaymentSupplier, List<GL_TransactionHeader> colth, List<GL_TransactionDetail> colTranDetail, SET_DocumentApprove da,
            string PaymentType = null, List<AP_PaymentTaxes> colPaymentTaxes = null, List<AP_Payment_AccountsDetail> colPaymentAccDetAdd = null, int CurrentFiscalID = 0)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InsertPayment");

            try
            {
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                this.PaymentID = AP_PaymentHeader.GetNewPaymentID(f_CompanyID, f_PeriodID, sqltrans);

                if (PaymentType == "Cheque")
                {
                    if (string.IsNullOrEmpty(this.Reference))
                    {
                        using (AR_SET_BankAccount obj_BankAccount = new AR_SET_BankAccount())
                        {
                            this.Reference = (Convert.ToInt32(obj_BankAccount.GetCheckNumber(f_CustomerBankBranchID.Value, f_BankAccountID.Value, f_CompanyID,
                                f_PeriodID, sqltrans).Rows[0]["ReferenceNo"]) + 1).ToString();
                        };
                    }
                }

                this.Insert(sqltrans);

                short paymentDetailId = short.Parse(AP_PaymentDetail.GetNewPaymentDetailID(CompanyID, this.PaymentID, PeriodID, sqltrans).ToString());

                foreach (AP_PaymentDetail PaymentDetail in colPaymentDetail)
                {
                    PaymentDetail.PaymentHeaderID = this.PaymentID;
                    PaymentDetail.PaymentDetailID = paymentDetailId++;
                    PaymentDetail.FiscalID = CurrentFiscalID;
                    PaymentDetail.Insert(sqltrans);
                }

                #region "For AP_PaymentInvoicesDetail"

                int paymentInvoicesID = AP_PaymentInvoicesDetail.GetNewPaymentInvoicesID(this.f_CompanyID, this.PaymentID, this.f_PeriodID, sqltrans);

                foreach (AP_PaymentInvoicesDetail paymentInvoicesDetail in colPaymentInvoicesDetail)
                {
                    paymentInvoicesDetail.PaymentHeaderID = this.PaymentID;
                    paymentInvoicesDetail.PaymentInvoicesID = paymentInvoicesID++;
                    paymentInvoicesDetail.FiscalID = CurrentFiscalID;
                    paymentInvoicesDetail.Insert(sqltrans);

                    #region Payment Taxes
                    if (colPaymentTaxes != null)
                    {
                        var Payments = from objPayments in colPaymentTaxes
                                       where objPayments.RowUniqueID == paymentInvoicesDetail.RowUniqueID
                                       select objPayments;

                        foreach (AP_PaymentTaxes PaymentTaxes in Payments)
                        {
                            PaymentTaxes.PaymentHeaderID = this.PaymentID;
                            PaymentTaxes.PaymentInvoiceID = paymentInvoicesDetail.PaymentInvoicesID;
                            PaymentTaxes.PaymentTaxID = AP_PaymentTaxes.GetNewPaymentTaxID(CompanyID, PeriodID, sqltrans);
                            PaymentTaxes.FiscalID = CurrentFiscalID;
                            PaymentTaxes.Insert(sqltrans);
                        }
                    }
                    #endregion
                }

                #endregion

                #region Insert in Payment Banks table

                if (lst_PaymentBanks != null)
                {
                    short paymentBankID = (short)AP_PaymentBanksDetail.GetNewPaymentBankID(f_CompanyID, f_PeriodID, this.PaymentID, sqltrans);

                    foreach (AP_PaymentBanksDetail objPaymenttBank in lst_PaymentBanks)
                    {
                        objPaymenttBank.PaymentHeaderID = this.PaymentID;
                        objPaymenttBank.PaymentBankID = paymentBankID++;
                        objPaymenttBank.FiscalID = CurrentFiscalID;
                        objPaymenttBank.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert in Payment Supplier table
                if (lst_PaymentSupplier != null)
                {
                    short paymentSupplierID = (short)AP_PaymentSupplierDetail.GetNewPaymentSupplierID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);
                    foreach (AP_PaymentSupplierDetail objPaymentSupplier in lst_PaymentSupplier)
                    {
                        objPaymentSupplier.PaymentHeaderID = this.PaymentID;
                        objPaymentSupplier.PaymentSupplierID = paymentSupplierID++;
                        objPaymentSupplier.FiscalID = CurrentFiscalID;
                        objPaymentSupplier.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    foreach (GL_TransactionHeader thh in colth)
                    {
                        if (thh.ControlCredit != thh.ControlDebit)
                            throw new Exception("Total Debit and Total Credit amount do not tally (Debit: " + thh.ControlDebit + ") (Credit: " + thh.ControlCredit + ").");
                    }
                }

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    foreach (GL_TransactionHeader thh in colth)
                    {
                        #region Insert In GL_TransactionHeader
                        thh.DocumentNo = f_DocumentNo;
                        thh.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        thh.FiscalID = CurrentFiscalID;
                        thh.Insert(sqltrans);

                        #endregion

                        #region Insert In GL_TransactionDetail
                        decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colTranDetail)
                        {
                            string naration = td.Narration.ToString();
                            if (naration.Contains("_") && td.TransactionType == "Credit")
                            {
                                naration = naration.Substring(0, naration.LastIndexOf('_') - 1);
                            }
                            td.TransactionID = thh.TransactionID;
                            td.SourceHeaderID = this.PaymentID;
                            td.Narration = naration;
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }
                    }
                        #endregion
                }
                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
                    da.FiscalID = CurrentFiscalID;
                    da.Insert(sqltrans);
                }

                #endregion

                if (colPaymentAccDetAdd != null)
                {
                    short paymentAccountDetailID = (short)AP_Payment_AccountsDetail.GetNewPaymentDetailAccountID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);
                    foreach (AP_Payment_AccountsDetail td in colPaymentAccDetAdd)
                    {
                        td.PaymentHeaderID = this.PaymentID;
                        td.PaymentDetailAccountID = paymentAccountDetailID++;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);

                        var filteredRows = from detail in colTranDetail
                                           where (detail.TransactionType == td.TransactionType && detail.CodeCombinationID == td.CodeCombinationID)
                                           select detail;

                        foreach (var filteredRow in filteredRows)
                        {
                            filteredRow.SourceHeaderID = this.PaymentID;
                            filteredRow.SourceDetailID = td.PaymentDetailAccountID;
                        }
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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPayment");
            }

            return result;
        }

        public bool UpdatePaymentHeaderAndDetail(List<AP_PaymentDetail> colPaymentDetail, List<AP_PaymentDetail> colPaymentDetailUpdate,
            List<AP_PaymentDetail> colPaymentDetailDelete, List<AP_PaymentInvoicesDetail> colPaymentInvoicesDetail,
            List<AP_PaymentInvoicesDetail> colPaymentInvoicesDetailUpdate, List<AP_PaymentInvoicesDetail> colPaymentInvoicesDetailDelete,
            List<AP_PaymentBanksDetail> lst_PaymentBanksAdd, List<AP_PaymentBanksDetail> lst_PaymentBanksUpdate, List<AP_PaymentBanksDetail> lst_PaymentBanksDelete, List<AP_PaymentSupplierDetail> lst_PaymentSupplierAdd, List<AP_PaymentSupplierDetail> lst_PaymentSupplierUpdate, List<AP_PaymentSupplierDetail> lst_PaymentSupplierDelete, List<AP_Payment_AccountsDetail> lst_PaymentAccountsAdd, List<AP_Payment_AccountsDetail> lst_PaymentAccountsUpdate, List<AP_Payment_AccountsDetail> lst_PaymentAccountsDelete,
            List<GL_TransactionHeader> colth, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove da, List<AP_PaymentTaxes> colPaymentTaxes = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePayment");

            try
            {
                this.UpdateDataByKey(sqltrans);

                foreach (AP_PaymentDetail PaymentAdd in colPaymentDetail)
                {
                    PaymentAdd.PaymentDetailID = short.Parse(AP_PaymentDetail.GetNewPaymentDetailID(CompanyID, PaymentID, PeriodID, sqltrans).ToString());
                    PaymentAdd.Insert(sqltrans);
                }

                foreach (AP_PaymentDetail PaymentUpdate in colPaymentDetailUpdate)
                    PaymentUpdate.UpdateDataByKey(sqltrans);

                foreach (AP_PaymentDetail PaymentDelete in colPaymentDetailDelete)
                {
                    AP_PaymentDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, PaymentDelete.PaymentDetailID,
                        PaymentID, PeriodID);
                }

                #region "For AP_PaymentInvoicesDetail"

                foreach (AP_PaymentInvoicesDetail paymentInvoicesDetail in colPaymentInvoicesDetail)
                {
                    paymentInvoicesDetail.PaymentInvoicesID = AP_PaymentInvoicesDetail.GetNewPaymentInvoicesID(this.f_CompanyID, this.PaymentID, this.f_PeriodID,
                        sqltrans);

                    paymentInvoicesDetail.PaymentHeaderID = this.PaymentID;
                    paymentInvoicesDetail.Insert(sqltrans);

                    #region Payment Taxes
                    if (colPaymentTaxes != null)
                    {
                        var Payments = from objPayments in colPaymentTaxes
                                       where objPayments.RowUniqueID == paymentInvoicesDetail.RowUniqueID
                                       select objPayments;

                        foreach (AP_PaymentTaxes PaymentTaxes in Payments)
                        {
                            PaymentTaxes.PaymentHeaderID = this.PaymentID;
                            PaymentTaxes.PaymentInvoiceID = paymentInvoicesDetail.PaymentInvoicesID;
                            PaymentTaxes.PaymentTaxID = AP_PaymentTaxes.GetNewPaymentTaxID(CompanyID, PeriodID, sqltrans);
                            PaymentTaxes.Insert(sqltrans);
                        }
                    }
                    #endregion
                }

                foreach (AP_PaymentInvoicesDetail paymentInvoicesDetailUpdate in colPaymentInvoicesDetailUpdate)
                {
                    paymentInvoicesDetailUpdate.UpdateDataByKey(sqltrans);

                    if (colPaymentTaxes != null)
                    {
                        AP_PaymentTaxes.DeleteDataByPaymentHeaderID(sqltrans, CompanyID, PeriodID, paymentInvoicesDetailUpdate.PaymentHeaderID,
                            paymentInvoicesDetailUpdate.PaymentInvoicesID);

                        #region Payment Taxes
                        if (colPaymentTaxes != null)
                        {
                            var Payments = from objPayments in colPaymentTaxes
                                           where objPayments.RowUniqueID == paymentInvoicesDetailUpdate.RowUniqueID
                                           select objPayments;

                            foreach (AP_PaymentTaxes PaymentTaxes in Payments)
                            {
                                PaymentTaxes.PaymentHeaderID = paymentInvoicesDetailUpdate.PaymentHeaderID;
                                PaymentTaxes.PaymentInvoiceID = paymentInvoicesDetailUpdate.PaymentInvoicesID;
                                PaymentTaxes.PaymentTaxID = AP_PaymentTaxes.GetNewPaymentTaxID(CompanyID, PeriodID, sqltrans);
                                PaymentTaxes.Insert(sqltrans);
                            }
                        }
                        #endregion
                    }
                }

                foreach (AP_PaymentInvoicesDetail paymentInvoicesDetailDelete in colPaymentInvoicesDetailDelete)
                {
                    AP_PaymentInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, this.PaymentID,
                        paymentInvoicesDetailDelete.PaymentInvoicesID, f_PeriodID);

                    if (colPaymentTaxes != null)
                    {

                        AP_PaymentTaxes.DeleteDataByPaymentHeaderID(sqltrans, CompanyID, PeriodID, paymentInvoicesDetailDelete.PaymentHeaderID, paymentInvoicesDetailDelete.PaymentInvoicesID);
                    }


                }
                #endregion

                #region "For Payment Banks Detail"

                foreach (AP_PaymentBanksDetail paymentBankAdd in lst_PaymentBanksAdd)
                {
                    paymentBankAdd.PaymentBankID = short.Parse(AP_PaymentBanksDetail.GetNewPaymentBankID(CompanyID, PaymentID, PeriodID, sqltrans).ToString());
                    paymentBankAdd.Insert(sqltrans);
                }

                foreach (AP_PaymentBanksDetail paymentBankUpdate in lst_PaymentBanksUpdate)
                    paymentBankUpdate.UpdateDataByKey(sqltrans);

                foreach (AP_PaymentBanksDetail paymentBankDelete in lst_PaymentBanksDelete)
                {
                    AP_PaymentBanksDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID,
                        paymentBankDelete.PaymentBankID, PaymentID, PeriodID);
                }

                #endregion

                #region "For Payment Supplier Detail"

                foreach (AP_PaymentSupplierDetail supplierAdd in lst_PaymentSupplierAdd)
                {
                    supplierAdd.PaymentSupplierID = short.Parse(AP_PaymentSupplierDetail.GetNewPaymentSupplierID(CompanyID, PaymentID, PeriodID, sqltrans).ToString());
                    supplierAdd.Insert(sqltrans);
                }

                foreach (AP_PaymentSupplierDetail supplierUpdate in lst_PaymentSupplierUpdate)
                    supplierUpdate.UpdateDataByKey(sqltrans);

                foreach (AP_PaymentSupplierDetail supplierDelete in lst_PaymentSupplierDelete)
                {
                    AP_PaymentSupplierDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID,
                        PaymentID, supplierDelete.PaymentSupplierID, PeriodID);
                }

                #endregion

                #region "For Payment Accounts Detail"

                foreach (AP_Payment_AccountsDetail paymentAccountsAdd in lst_PaymentAccountsAdd)
                {
                    paymentAccountsAdd.PaymentDetailAccountID = short.Parse(AP_Payment_AccountsDetail.GetNewPaymentDetailAccountID(CompanyID, PaymentID, PeriodID, sqltrans).ToString());
                    paymentAccountsAdd.PaymentHeaderID = PaymentID;
                    paymentAccountsAdd.Insert(sqltrans);
                }

                foreach (AP_Payment_AccountsDetail paymentAccountsUpdate in lst_PaymentAccountsUpdate)
                    paymentAccountsUpdate.UpdateDataByKey(sqltrans);

                foreach (AP_Payment_AccountsDetail paymentAccountsDelete in lst_PaymentAccountsDelete)
                {
                    AP_Payment_AccountsDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, paymentAccountsDelete.PaymentDetailAccountID, PaymentID, PeriodID);
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)))
                {
                    foreach (GL_TransactionHeader thh in colth)
                    {
                        if (thh.ControlCredit != thh.ControlDebit)
                            throw new Exception("Total Debit and Total Credit amount do not tally (Debit: " + thh.ControlDebit + ") (Credit: " + thh.ControlCredit + ").");
                    }
                }

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    #region Insert In GL

                    foreach (GL_TransactionHeader thh in colth)
                    {
                        //thh.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        thh.Insert(sqltrans);

                        #region Insert In GL_TransactionDetail


                        decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");


                        foreach (GL_TransactionDetail td in colAddTranDetail)
                        {
                            td.TransactionID = thh.TransactionID;
                            td.Insert(sqltrans);
                        }

                        #endregion

                    }
                    #endregion
                }

                #endregion

                #region Insert In SET_DocumentApprove When StatusCode = "ReleaseForApprove"


                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.ReleaseForApprove)) || this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.PartiallyApproved)))
                {
                    da.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    da.DocumentNo = this.DocumentNo.ToString();
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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePayment");
            }

            return result;
        }

        public bool DeletePaymentHeaderAndDetail(List<AP_PaymentDetail> colDeletePaymentDetail, List<AP_PaymentInvoicesDetail> colPaymentInvoicesDetailDelete,
            GL_TransactionHeader th, List<GL_TransactionDetail> colDeleteTranDetail)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("DeletePayment");

            try
            {
                DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, PaymentID, f_PeriodID);

                foreach (AP_PaymentDetail paymentDelete in colDeletePaymentDetail)
                    AP_PaymentDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, CompanyID, paymentDelete.PaymentDetailID,
                        PaymentID, PeriodID);

                #region "For AP_PaymentInvoicesDetail"

                //foreach (AP_PaymentInvoicesDetail paymentInvoicesDetailDelete in colPaymentInvoicesDetailDelete)
                //    AP_PaymentInvoicesDetail.DeleteDataByKey(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, this.PaymentID,
                //        paymentInvoicesDetailDelete.PaymentInvoicesID, f_PeriodID);

                AP_PaymentSupplierDetail.DeleteDataByKeyPaymentHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, PaymentID, f_PeriodID);
                AP_PaymentInvoicesDetail.DeleteDataByKeyPaymentHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, PaymentID, f_PeriodID);
                AP_Payment_AccountsDetail.DeleteDataByKeyPaymentHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, PaymentID, f_PeriodID);
                AP_PaymentBanksDetail.DeleteDataByKeyPaymentHeaderID(sqltrans, f_UpdatedBy.Value, f_UpdateDate.Value, f_UpdatedByValue, f_CompanyID, PaymentID, f_PeriodID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "DeletePayment");
            }
            return result;
        }

        public bool UpdatePHeaderForApprove(GL_TransactionHeader th, List<GL_TransactionDetail> colAddTranDetail, SET_DocumentApprove dA, int CurrentFiscalID, List<AP_PaymentSupplierDetail> lst_PaymentSupplierAdd = null)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("UpdatePHeader");

            try
            {
                this.FiscalID = CurrentFiscalID;
                this.UpdateDataByKey(sqltrans);


                #region Supplier

                if (lst_PaymentSupplierAdd != null)
                {
                    short paymentSupplierID = (short)AP_PaymentSupplierDetail.GetNewPaymentSupplierID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);

                    foreach (AP_PaymentSupplierDetail objPaymentSupplier in lst_PaymentSupplierAdd)
                    {
                        objPaymentSupplier.PaymentHeaderID = this.PaymentID;
                        objPaymentSupplier.PaymentSupplierID = paymentSupplierID++;
                        objPaymentSupplier.FiscalID = CurrentFiscalID;
                        if (objPaymentSupplier.Narration == "GST")
                            objPaymentSupplier.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_Transaction Header And Detail When StatusCode = "Approved"

                DataRow drdocType = SET_DocumentType.GetDataByKey(this.CompanyID, this.DocumentTypeID, this.PeriodID, null, sqltrans);
                string doctype = drdocType.Field<string>("DocumentType").ToString();

                if (this.StatusCode.Equals(Convert.ToInt16(Enumaration.CurrentStatus.Approved)))
                {
                    if (!(doctype == "Carriage Contractor Payment"))
                    {

                        th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                        th.FiscalID = CurrentFiscalID;
                        th.Insert(sqltrans);

                        #region Insert In GL_TransactionDetail

                        decimal AmountDebit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                        decimal AmountCredit = Math.Round(colAddTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                        if (AmountDebit != AmountCredit)
                            throw new BusinessLogicException("Debit Credit Not Tally DR: " + AmountDebit + " | CR: " + AmountCredit + "");

                        foreach (GL_TransactionDetail td in colAddTranDetail)
                        {
                            td.TransactionID = th.TransactionID;
                            td.FiscalID = CurrentFiscalID;
                            td.Insert(sqltrans);
                        }

                        #endregion
                    }
                    else
                    {
                        GL_TransactionHeader glheaderApprove = new GL_TransactionHeader();
                        glheaderApprove.StatusCode = (short)Enumaration.CurrentStatus.Approved;
                        glheaderApprove.CompanyID = f_CompanyID;
                        glheaderApprove.PeriodID = f_PeriodID;
                        glheaderApprove.DocumentTypeID = f_DocumentTypeID;
                        glheaderApprove.DocumentNo = f_DocumentNo;
                        glheaderApprove.ApprovedBy = dA.CreatedBy;
                        glheaderApprove.DateOfApproval = DateTime.Parse(DateTime.Now.ToString());
                        glheaderApprove.FiscalID = CurrentFiscalID;
                        glheaderApprove.UpdateDataByKey_StatusCode(sqltrans);
                    }
                }

                #endregion

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
                    SET_DocumentApprove.DeleteDataByDocNoanddocTypeid(f_CompanyID, f_PeriodID, int.Parse(dA.DocumentNo), dA.DocumentTypeID, sqltrans, CurrentFiscalID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "UpdatePHeader");
            }

            return result;
        }

        public bool CancelOrReverseApprovedPayment(List<AP_PaymentDetail> colPaymentDetail, GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail,
            SET_DocumentApprove dA, List<AP_Payment_AccountsDetail> colPaymentAccounts, List<AP_PaymentBanksDetail> colPaymentBanks,
            List<AP_PaymentSupplierDetail> colPaymentSupplier, List<int> lst_PaymentInvoices, List<int> lst_PurchaseInvoices, Nullable<DateTime> dt_ReverseDate,
            out int ReversalDocumentNo, string Comments, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("CancelOrReversePayment");

            try
            {
                int newDocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                ReversalDocumentNo = newDocumentNo;
                int PoHeaderID = 0;

                if (this.POHeaderID != null)
                    PoHeaderID = int.Parse(this.POHeaderID.ToString());

                #region Update Payment

                if (!dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))   //  Cancellation
                    this.Status = false;
                if (PoHeaderID > 0)
                {
                    this.POHeaderID = null;
                    this.SourceHeaderID = null;
                }

                //this.DocumentDate = Convert.ToDateTime(dt_ReverseDate.ToString());
                //this.ReceiptDate = Convert.ToDateTime(dt_ReverseDate.ToString());
                this.ReversalDate = dt_ReverseDate;
                this.ReversalComments = Comments + "  (Reversal document no.: " + newDocumentNo.ToString() + ").";
                this.UpdateDataByKey(sqltrans);

                foreach (int paymentInvoiceID in lst_PaymentInvoices)
                {
                    AP_PaymentInvoicesDetail.DeleteDataByKey(sqltrans, this.UpdatedBy.Value, this.UpdateDate.Value, this.UpdatedByValue, this.CompanyID,
                        this.PaymentID, paymentInvoiceID, this.PeriodID);
                }

                foreach (int purchaseInvoiceID in lst_PurchaseInvoices)
                    AR_InvoiceHeader.Get_UpdateIsPayment(this.f_CompanyID, this.f_PeriodID, purchaseInvoiceID, 0, sqltrans);

                #endregion

                #region Update Purchase Invoice (DI-Invoice)

                if (this.SourceHeaderID.HasValue)
                {
                    SET_DocumentType obj_DocumentType = new SET_DocumentType(this.f_CompanyID, this.f_DocumentTypeID, this.f_PeriodID, null, sqltrans);

                    if (obj_DocumentType.IsDataLoaded && obj_DocumentType.DocumentType.ToLower().StartsWith("payment against direct invoices"))
                        AR_InvoiceHeader.Get_UpdateIsPayment(this.f_CompanyID, this.f_PeriodID, this.f_SourceHeaderID.Value, 0, sqltrans);
                }

                #endregion

                //AP_PurchaseOrderHeader ap = new AP_PurchaseOrderHeader(f_CompanyID, f_PeriodID, PoHeaderID);

                //if (ap != null && ap.POHeaderID > 0)
                //{
                //    ap.AdvanceAmountPercentage = null;
                //    ap.UpdateDataByKey(sqltrans);
                //}

                #region Insert In AP_Payment Header And Detail, When ApprovalStatus = "Reversed"

                if (dA.ApprovalStatus.Equals(Enumaration.CurrentStatus.Reversed.ToString()))
                {
                    #region Insert in AP_PaymentHeader, the reversal record

                    this.CreateDate = this.UpdateDate.Value;
                    this.CreatedBy = this.UpdatedBy.Value;
                    this.CreatedByValue = this.UpdatedByValue;

                    this.UpdateDate = null;
                    this.UpdatedBy = null;
                    this.UpdatedByValue = null;

                    this.ReversalDate = null;
                    this.ReversalComments = Comments + "  (This Document is generated against the Reversal process).";
                    this.Amount = this.Amount * -1;
                    this.SourceHeaderID = this.PaymentID;
                    this.DocumentNo = newDocumentNo;
                    this.DocumentDate = dt_ReverseDate.Value;
                    this.FiscalID = CurrentFiscalID;
                    this.PaymentID = AP_PaymentHeader.GetNewPaymentID(f_CompanyID, f_PeriodID, sqltrans);
                    this.Insert(sqltrans);

                    #endregion

                    #region Insert In SET_DocumentApprove

                    dA.Status = true;
                    dA.DocumentNo = newDocumentNo.ToString();
                    dA.DocumentApproveID = SET_DocumentApprove.GetNewDocumentApproveID(this.f_CompanyID, this.f_PeriodID, sqltrans);
                    dA.Insert(sqltrans);

                    #endregion

                    #region Insert In AP_PaymentDetail

                    short paymentDetailId = short.Parse(AP_PaymentDetail.GetNewPaymentDetailID(CompanyID, this.PaymentID, PeriodID, sqltrans).ToString());

                    foreach (AP_PaymentDetail PaymentDetail in colPaymentDetail)
                    {
                        PaymentDetail.PaymentHeaderID = this.PaymentID;
                        PaymentDetail.BaseAmount = PaymentDetail.BaseAmount * -1;
                        PaymentDetail.DiscountAmount = PaymentDetail.DiscountAmount * -1;
                        PaymentDetail.GrossAmount = PaymentDetail.GrossAmount * -1;
                        PaymentDetail.NetAmount = PaymentDetail.NetAmount * -1;
                        PaymentDetail.TaxAmount = PaymentDetail.TaxAmount * -1;
                        PaymentDetail.PaymentDetailID = paymentDetailId++;
                        PaymentDetail.FiscalID = this.FiscalID;
                        PaymentDetail.Insert(sqltrans);
                    }

                    #endregion

                    #region Update old GL Header and Insert new In GL_TransactionHeader

                    string originalNarration = th.Narration;
                    if (th.Narration != null)
                        th.Narration = th.Narration + (th.Narration.Trim().EndsWith(".") ? " " : ". ") + "This document is reversed (Reversed document no.: "
                            + newDocumentNo.ToString() + ").";
                    th.Narration = "This document is reversed (Reversed document no.: "
                            + newDocumentNo.ToString() + ").";

                    th.UpdateDataByKey(sqltrans);

                    th.ReversalID = th.TransactionID;
                    if (originalNarration != null)
                    {
                        th.Narration = Comments + " " + originalNarration + (originalNarration.Trim().EndsWith(".") ? " " : ". ")
                            + "This document is generated against the Reversal process. Original Document No.: " + th.DocumentNo.ToString();
                    }
                    else
                        th.Narration = Comments + "  (This document is generated against the Reversal process. Original Document No.: " + th.DocumentNo.ToString() + ")";

                    th.DocumentNo = newDocumentNo;
                    th.DateOfApproval = dt_ReverseDate.Value;
                    th.DocumentDate = dt_ReverseDate.Value;
                    th.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
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
                        td.SourceHeaderID = this.PaymentID;
                        td.Insert(sqltrans);
                    }

                    #endregion
                }

                #endregion

                #region Payment Banks

                if (colPaymentBanks != null)
                {
                    short paymentBankID = (short)AP_PaymentBanksDetail.GetNewPaymentBankID(f_CompanyID, f_PeriodID, this.PaymentID, sqltrans);

                    foreach (AP_PaymentBanksDetail objPaymenttBank in colPaymentBanks)
                    {
                        objPaymenttBank.PaymentHeaderID = this.PaymentID;
                        objPaymenttBank.PaymentBankID = paymentBankID++;
                        objPaymenttBank.FiscalID = f_FiscalID;
                        objPaymenttBank.Insert(sqltrans);
                    }
                }

                #endregion

                #region Payment Accounts

                if (colPaymentAccounts != null)
                {
                    short paymentAccountDetailID = (short)AP_Payment_AccountsDetail.GetNewPaymentDetailAccountID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);

                    foreach (AP_Payment_AccountsDetail td in colPaymentAccounts)
                    {
                        td.PaymentHeaderID = this.PaymentID;
                        td.PaymentDetailAccountID = paymentAccountDetailID++;
                        td.FiscalID = f_FiscalID;
                        td.Insert(sqltrans);

                        var filteredRows = from detail in colTranDetail
                                           where (detail.TransactionType == td.TransactionType && detail.CodeCombinationID == td.CodeCombinationID)
                                           select detail;

                        foreach (var filteredRow in filteredRows)
                        {
                            filteredRow.SourceHeaderID = this.PaymentID;
                            filteredRow.SourceDetailID = td.PaymentDetailAccountID;
                        }
                    }
                }

                #endregion

                #region Payment Supplier

                if (colPaymentSupplier != null)
                {
                    short paymentSupplierID = (short)AP_PaymentSupplierDetail.GetNewPaymentSupplierID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);

                    foreach (AP_PaymentSupplierDetail objPaymentSupplier in colPaymentSupplier)
                    {
                        objPaymentSupplier.PaymentHeaderID = this.PaymentID;
                        objPaymentSupplier.PaymentSupplierID = paymentSupplierID++;
                        objPaymentSupplier.Insert(sqltrans);
                    }
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
                    SqlHelper.RollBackTransaction(sqltrans, "CancelOrReversePayment");
            }

            return result;
        }

        public DataTable GetAllDataWithOtherTablesForApprove(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithPaging_WithOtherTables_ForApprove + ' ' + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithPaging_WithOtherTables_ForApprove + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                {

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTables_ForApprove + strWhere
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

        public DataTable GetAllDataWithOtherTablesForApproveDetail(int companyID, int periodID, int paymentID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_SelectAll_ForApproveDetail = @" 
SELECT DISTINCT AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.Status, AP_PaymentHeader.DocumentTypeID, 
    SET_DocumentType.DocumentType, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.ReceiptDate, AP_PaymentHeader.StatusCode, 
    SET_DocumentApprove.ApprovalStatus, SET_UserApprovalAmount.ApprovalOperator, SET_ApprovalAuthority.UserID, SET_Authority.AuthorityLevel 
FROM AP_PaymentHeader 
    INNER JOIN SET_DocumentType ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID 
        AND AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID AND AP_PaymentHeader.Status = 1 
    LEFT JOIN AR_SET_Customer ON AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID 
        --and AP_PaymentHeader.CompanyID = AR_SET_Customer.CompanyID 
        AND AP_PaymentHeader.PayFromCustomerID = AR_SET_Customer.CustomerID AND AR_SET_Customer.Status = 1 
    INNER JOIN SET_DocumentApprove ON SET_DocumentApprove.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentApprove.PeriodID = AP_PaymentHeader.PeriodID 
        AND SET_DocumentApprove.Status = 1 AND SET_DocumentApprove.DocumentTypeID = AP_PaymentHeader.DocumentTypeID 
        AND SET_DocumentApprove.DocumentNo = AP_PaymentHeader.DocumentNo 
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = AP_PaymentHeader.CompanyID AND SET_UserApprovalAmount.PeriodID = AP_PaymentHeader.PeriodID 
        AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = AP_PaymentHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
        AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                AND CAST(CONVERT(VARCHAR(10), AP_PaymentHeader.DocumentDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                AND CAST(CONVERT(VARCHAR(10), AP_PaymentHeader.DocumentDate, 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo 
                THEN 1 ELSE 0 END)) 
	    AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph 
                WHERE ph.CompanyID = AP_PaymentHeader.CompanyID AND ph.PeriodID = AP_PaymentHeader.PeriodID 
                AND ph.PaymentID = AP_PaymentHeader.PaymentID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph WHERE ph.CompanyID = AP_PaymentHeader.CompanyID 
                AND ph.PeriodID = AP_PaymentHeader.PeriodID 
                AND ph.PaymentID = AP_PaymentHeader.PaymentID) BETWEEN SET_UserApprovalAmount.AmountFrom AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
        AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID AND SET_Authority.Status = 1 
WHERE AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.PeriodID = @PeriodID 
    AND AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PaymentID = @PaymentID AND AP_PaymentHeader.DocumentTypeID = @DocumentTypeID 
    AND AP_PaymentHeader.DocumentNo = @DocumentNo ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentID", paymentID);
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

        public DataTable GetDataByKeyForApprove(int companyID, int periodID, int paymentID, int documentTypeID, string documentNo)
        {
            DataTable dtToReturn = null;

            #region Query

            string Query_Select_DataByKey_ForApprove = @" 
SELECT DISTINCT AP_PaymentHeader.PaymentID, AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.Status, SET_UserApprovalAmount.ApprovalOperator, 
    SET_ApprovalAuthority.UserID, SET_Authority.AuthorityLevel , SET_UserApprovalAmount.ApprovalAmountID , SET_UserApprovalAmount.ApprovalAuthorityID
FROM AP_PaymentHeader 
    INNER JOIN SET_UserApprovalAmount ON SET_UserApprovalAmount.CompanyID = AP_PaymentHeader.CompanyID AND SET_UserApprovalAmount.PeriodID = AP_PaymentHeader.PeriodID 
        AND SET_UserApprovalAmount.Status = 1 
        AND (SET_UserApprovalAmount.DocumentTypeID = AP_PaymentHeader.DocumentTypeID OR SET_UserApprovalAmount.DocumentTypeID IS NULL) 
	    AND (1 = (CASE WHEN SET_UserApprovalAmount.EffectiveDateTo IS NULL 
                AND CAST(CONVERT(VARCHAR(10), AP_PaymentHeader.DocumentDate, 110) AS DATETIME) >= SET_UserApprovalAmount.EffectiveDateFrom THEN 1 
            WHEN SET_UserApprovalAmount.EffectiveDateTo IS NOT NULL 
                AND CAST(CONVERT(VARCHAR(10), AP_PaymentHeader.DocumentDate, 110) AS DATETIME) BETWEEN SET_UserApprovalAmount.EffectiveDateFrom AND SET_UserApprovalAmount.EffectiveDateTo 
                THEN 1 ELSE 0 END)) 
	    AND (1 = (CASE WHEN SET_UserApprovalAmount.AmountTo IS NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph WHERE ph.CompanyID = AP_PaymentHeader.CompanyID 
                AND ph.PeriodID = AP_PaymentHeader.PeriodID AND ph.PaymentID = AP_PaymentHeader.PaymentID) >= SET_UserApprovalAmount.AmountFrom THEN 1 
            WHEN SET_UserApprovalAmount.AmountTo IS NOT NULL AND (SELECT ph.Amount FROM AP_PaymentHeader ph WHERE ph.CompanyID = AP_PaymentHeader.CompanyID 
                AND ph.PeriodID = AP_PaymentHeader.PeriodID AND ph.PaymentID = AP_PaymentHeader.PaymentID) BETWEEN SET_UserApprovalAmount.AmountFrom 
                AND SET_UserApprovalAmount.AmountTo THEN 1 ELSE 0 END)) 
    INNER JOIN SET_ApprovalAuthority ON SET_ApprovalAuthority.ApprovalAuthorityID = SET_UserApprovalAmount.ApprovalAuthorityID 
        AND SET_ApprovalAuthority.CompanyID = SET_UserApprovalAmount.CompanyID AND SET_ApprovalAuthority.PeriodID = SET_UserApprovalAmount.PeriodID 
        AND SET_ApprovalAuthority.Status = 1 
    INNER JOIN SET_Authority ON SET_Authority.AuthorityID = SET_ApprovalAuthority.AuthorityID AND SET_Authority.CompanyID = SET_ApprovalAuthority.CompanyID 
        AND SET_Authority.PeriodID = SET_ApprovalAuthority.PeriodID AND SET_Authority.Status = 1 

INNER JOIN aspnet_UsersInRoles On aspnet_UsersInRoles.UserID = AP_PaymentHeader.CreatedBy 
And (aspnet_UsersInRoles.RoleID = SET_UserApprovalAmount.RoleID or SET_UserApprovalAmount.RoleID is null)

  --new Working (Roles aginst Approver)
		inner join SET_DocumentType on SET_DocumentType.DocumentTypeID =  AP_PaymentHeader.DocumentTypeID
		inner join ApplicationMenu on ApplicationMenu.MenuID = SET_DocumentType.MenuID
		inner join MenusInRoles on MenusInRoles.MenuID = SET_DocumentType.MenuID
		inner join aspnet_Roles on aspnet_Roles.RoleId =  MenusInRoles.RoleId

WHERE AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PaymentID = @PaymentID 
    AND AP_PaymentHeader.DocumentTypeID = @DocumentTypeID AND AP_PaymentHeader.DocumentNo = @DocumentNo and aspnet_UsersInRoles.RoleId = aspnet_Roles.RoleId ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentID", paymentID);
                sqlparams[3] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[4] = new SqlParameter("@DocumentNo", documentNo);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Select_DataByKey_ForApprove, sqlparams);

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

        public static DataTable CheckAccountCode(int companyID, int periodID, int CustomerID)
        {
            DataTable dtToReturn = new DataTable();
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.GL.GL_CodeCombinationDetail.Query_CheckAccountCode, sqlparams);

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

        public static Double CheckAmountAgainstPO(int companyID, int periodID, int sourceHeaderID, string documentTypeName, Nullable<int> documentGroupID = null, bool IsAllowPartialAdvance = false, Nullable<int> vendorid = null)
        {
            double Amount = 0;

            string Query_CheckAmount_AgainstPO = @"
SELECT ISNULL(SUM(AP_PaymentHeader.Amount), 0)
-
isnull((select distinct isnull(AR_InvoiceHeader.AmountPaid,0) + isnull((select isnull(Amount,0) from AR_InvoiceSupplierDetail where AR_InvoiceSupplierDetail.InvoiceHeaderID=AR_InvoiceHeader.InvoiceID and AR_InvoiceSupplierDetail.status = 1 and AR_InvoiceSupplierDetail.Narration = 'GST'),0)
from AR_InvoiceDetail 
inner join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
and  AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID and  AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID and  AR_InvoiceHeader.Status = 1
where AR_InvoiceDetail.PurchaseOrderHeaderID = AP_PaymentHeader.SourceHeaderID and  AR_InvoiceHeader.StatusCode = 1 and AR_InvoiceHeader.CompanyID=AP_PaymentHeader.CompanyID and AR_InvoiceDetail.status = 1
and  isnull(AR_InvoiceHeader.IsReversed,0) = 0 and isnull(AR_InvoiceHeader.ReversalID,0) = 0 ),0)
 AS Amount
 FROM AP_PaymentHeader
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
	AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.Status = 1 AND SET_DocumentType.UserDefined = 0
	AND SET_DocumentType.DocumentType LIKE @DocumentTypeName + '%' AND (SET_DocumentType.GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) 
WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.Status = 1 
	AND AP_PaymentHeader.SourceHeaderID = @SourceHeaderID
    AND AP_PaymentHeader.VendorID = @VendorID
group by AP_PaymentHeader.SourceHeaderID,AP_PaymentHeader.CompanyID
";


            string Query_CheckAmount_AgainstPO_PartailAdvance = @"
SELECT ISNULL(SUM(AP_PaymentHeader.Amount), 0) 
-
isnull((

select 
case when isnull(sum(tab.AdvanceAmount),0)  > 0 then  isnull(sum(tab.AdvanceAmount),0) 
else 
(isnull(sum(tab.AmountPaid),0) + 
isnull((select isnull(sum(AR_InvoiceSupplierDetail.Amount),0) 
from AR_InvoiceSupplierDetail 
inner join AR_InvoiceDetail dtl on dtl.InvoiceHeaderID = AR_InvoiceSupplierDetail.InvoiceHeaderID
and dtl.CompanyID = AR_InvoiceSupplierDetail.CompanyID
and dtl.PeriodID = AR_InvoiceSupplierDetail.PeriodID
where dtl.PurchaseOrderHeaderID = tab.PurchaseOrderHeaderID
and AR_InvoiceSupplierDetail.status = 1 and AR_InvoiceSupplierDetail.Narration = 'GST'),0))
 end

 from (
select distinct
AR_InvoiceHeader.InvoiceID, AR_InvoiceHeader.AdvanceAmount,AR_InvoiceDetail.PurchaseOrderHeaderID,AmountPaid

from AR_InvoiceDetail 
inner join AR_InvoiceHeader on AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
and  AR_InvoiceHeader.CompanyID = AR_InvoiceDetail.CompanyID
and  AR_InvoiceHeader.PeriodID = AR_InvoiceDetail.PeriodID
and  AR_InvoiceHeader.Status = 1

where AR_InvoiceDetail.PurchaseOrderHeaderID = AP_PaymentHeader.SourceHeaderID 
 and  AR_InvoiceHeader.StatusCode = 1 and AR_InvoiceHeader.CompanyID=AP_PaymentHeader.CompanyID and AR_InvoiceDetail.status = 1
and  isnull(AR_InvoiceHeader.IsReversed,0) = 0 and isnull(AR_InvoiceHeader.ReversalID,0) = 0  
and isnull(AR_InvoiceHeader.AdvanceAmount,0) > 0
) as tab
group by tab.PurchaseOrderHeaderID
),0)
 
 AS Amount
 FROM AP_PaymentHeader
INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
	AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.Status = 1 AND SET_DocumentType.UserDefined = 0
	AND SET_DocumentType.DocumentType LIKE @DocumentTypeName + '%' AND (SET_DocumentType.GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) 
WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.Status = 1 
	AND AP_PaymentHeader.SourceHeaderID = @SourceHeaderID
    AND (AP_PaymentHeader.VendorID = @VendorID OR @VendorID IS NULL) 
group by AP_PaymentHeader.SourceHeaderID,AP_PaymentHeader.CompanyID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);
                sqlparams[3] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if (documentGroupID.HasValue)
                    sqlparams[4] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[4] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                if (vendorid.HasValue)
                    sqlparams[5] = new SqlParameter("@VendorID", vendorid);
                else
                    sqlparams[5] = new SqlParameter("@VendorID", DBNull.Value);


                string Query = Query_CheckAmount_AgainstPO;
                if (IsAllowPartialAdvance)
                    Query = Query_CheckAmount_AgainstPO_PartailAdvance;


                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        public static Double CheckPOAmountAgainstInvocies(int companyID, int periodID, int sourceHeaderID, Nullable<int> documentGroupID = null)
        {
            double Amount = 0;

            string Query_CheckAmountPO_AgainstAgainst = @" select IsNull(SUM(Amount), 0) from 
(
	select distinct isnull(GL_TransactionDetail.Amount, 0) as Amount from Ar_invoiceHeader 
	    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = Ar_invoiceHeader.CompanyID AND SET_DocumentType.PeriodID = Ar_invoiceHeader.PeriodID 
            AND SET_DocumentType.DocumentTypeID = Ar_invoiceHeader.InvoiceTYpe AND SET_DocumentType.Status = 1 AND SET_DocumentType.UserDefined = 0 
            AND (SET_DocumentType.GroupID = @DocumentGroupID OR @DocumentGroupID IS NULL) 
	    inner join Ar_invoiceDetail on Ar_invoiceDetail.InvoiceHeaderID = Ar_invoiceHeader.InvoiceID and Ar_invoiceDetail.CompanyID = Ar_invoiceHeader.CompanyID 
            and Ar_invoiceDetail.PeriodID = Ar_invoiceHeader.PeriodID 
	    inner join ap_purchaseORderHEader on ap_purchaseORderHEader.POHeaderID = Ar_invoiceDetail.PurchaseOrderHeaderID 
            and ap_purchaseORderHEader.CompanyID = Ar_invoiceDetail.CompanyID and ap_purchaseORderHEader.PeriodID = Ar_invoiceDetail.PeriodID 
        inner join GL_TransactionHeader on GL_TransactionHeader.DocumentNo = Ar_invoiceHeader.InvoiceNumber 
            and GL_TransactionHeader.DocumentTypeID = Ar_invoiceHeader.InvoiceType 
        INNER JOIN GL_TransactionDetail ON GL_TransactionHeader.CompanyID = GL_TransactionDetail.CompanyID 
            and GL_TransactionHeader.PeriodID = GL_TransactionDetail.PeriodID AND GL_TransactionHeader.TransactionID = GL_TransactionDetail.TransactionID 
            AND GL_TransactionDetail.TransactionType = 'Credit' 
            and GL_TransactionDetail.CodeCombinationID in
                (SELECT Distinct DefaultAccountCode FROM SET_DocumentType WHERE DocumentType Like 'Advance Against PO%' and DefaultAccountCode is not null) 
	where ap_purchaseORderHEader.POHeaderID = @SourceHeaderID and Ar_invoiceHeader.StatusCode = 1 and Ar_invoiceHeader.ReversalID is null 
        and Ar_invoiceHeader.ReversalDate is null and Ar_invoiceHeader.CompanyID = @CompanyID and Ar_invoiceHeader.PeriodID = @PeriodID
) AS tab ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SourceHeaderID", sourceHeaderID);

                if (documentGroupID.HasValue)
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[3] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmountPO_AgainstAgainst, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }

        /// <summary>
        /// Return Advance Amount against Purchase Order
        /// </summary>
        /// <param name="companyID">Company ID (from Session)</param>
        /// <param name="periodID">Period ID (from Session)</param>
        /// <param name="documentNo">Purchase Invoice Number</param>
        /// <param name="documentTypeName">Advance Against PO</param>
        /// <returns>Advance Amount taken</returns>
        public Double GetAmountAgainstPO(int companyID, int periodID, int documentNo, string documentTypeName, Nullable<int> documentGroupID = null)
        {
            double Amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentNo", documentNo);
                sqlparams[3] = new SqlParameter("@DocumentTypeName", documentTypeName);

                if (documentGroupID.HasValue)
                    sqlparams[4] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[4] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                Amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetAmount_AgainstPO, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Amount;
        }


        public Double GetPORate(int companyID, int periodID, int InvoiceID)
        {
            double Rate = 0;

            #region Query

            string Query_GetRate = @" select isnull(AP_PurchaseOrderHeader.Rate,0)Rate from  AR_InvoiceHeader,AR_InvoiceDetail,AP_PurchaseOrderHeader where AR_InvoiceHeader.InvoiceID = AR_InvoiceDetail.InvoiceHeaderID
  and AR_InvoiceHeader.InvoiceID = @InvoiceID and AP_PurchaseOrderHeader.PoHeaderID = AR_InvoiceDetail.PurchaseOrderHeaderID  ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@InvoiceID", InvoiceID);

                Rate = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetRate, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Rate;
        }

        public DataRow GetDataBySourceHeaderID(int companyID, int periodID, int sourceHeaderID, int documentTypeID, SqlConnection objConnection = null)
        {
            DataRow dr = null;
            string Query_GetData_by_SourceHeaderId = @" SELECT * FROM [AP_PaymentHeader] WHERE [CompanyID] = @CompanyID and [PeriodID] = @PeriodID and [SourceHeaderId] = @SourceHeaderId and DocumentTypeId = @DocumentTypeId";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SourceHeaderId", sourceHeaderID);
                sqlparams[2] = new SqlParameter("@DocumentTypeId", documentTypeID);
                sqlparams[3] = new SqlParameter("@PeriodID", periodID);

                DataSet ds = null;

                if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_by_SourceHeaderId, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_by_SourceHeaderId, sqlparams);

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

        public Double CheckAmountForAdvanceAgainstPO(int companyID, int periodID, int purchaseOrderHeaderID)
        {
            double amount = 0;
            string Query_CheckAmount_AdvanceAgainstPO = @" SELECT ISNULL(SUM(AP_PaymentHeader.Amount), 0) AS Amount FROM AP_PaymentHeader 
    LEFT JOIN AP_PurchaseOrderHeader ON AP_PurchaseOrderHeader.CompanyID = AP_PaymentHeader.CompanyID AND AP_PurchaseOrderHeader.PeriodID = AP_PaymentHeader.PeriodID 
        AND AP_PurchaseOrderHeader.POHeaderID = AP_PaymentHeader.POHeaderID AND AP_PurchaseOrderHeader.Status = 1 
WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.Status = 1 
    AND AP_PaymentHeader.POHeaderID = @PurchaseOrderHeaderID 
    And AP_PaymentHeader.DocumentTypeID In (select documenttypeid from set_documenttype where documenttype like '%Advance Against PO%')
    And AP_PaymentHeader.StatusCode = 1
";


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PurchaseOrderHeaderID", purchaseOrderHeaderID);

                amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_CheckAmount_AdvanceAgainstPO, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amount;
        }

        public DataTable GetPaymentReceiptReport2ForSakrand(int companyID, int periodID, int? noFrom, int? noTo, int? documentTypeID, int FiscalID = 0)
        {
            DataTable dtPaymentReceipt = null;


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", FiscalID);

                StringBuilder query = new StringBuilder(); ; //= string.Empty;

                if (noFrom.HasValue && noTo.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] BETWEEN " + noFrom.Value + " AND " + noTo.Value);
                else if (noFrom.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] >= " + noFrom.Value);
                else if (noTo.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] <= " + noTo.Value);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentReceiptReortData2ForSakrand, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtPaymentReceipt;
        }

        public static DataTable GetLoanSanctionDataForGrid(int companyID, int periodID, int LoanSanctionHeaderID, SqlTransaction sqltrans = null)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = null;
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@LoanSanctionHeaderID", LoanSanctionHeaderID);

                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_LoanSanctionDataForGrid, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_LoanSanctionDataForGrid, sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }


        public static DataTable GetLoanSanctionData(int companyID, int periodID, int CustomerID, string loanFilter = "", SqlTransaction sqltrans = null)
        {
            DataTable dtToReturn = null;

            try
            {
                DataSet ds = null;
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);
                string query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectDataOFLoanSanction;
                if (loanFilter != "")
                {
                    query += loanFilter;
                }
                if (sqltrans == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, query, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(sqltrans, CommandType.Text, query, sqlparams);
                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }


        public DataTable Get_AllDocumentNos(int companyID, int periodID, int documentTypeID, int CurrentFiscalID, bool IsReversalForm = false)
        {
            DataTable dtToReturn = null;

            string Query_Get_AllDocumentNos = @" SELECT AP_PaymentHeader.* FROM AP_PaymentHeader 
inner join Set_PeriodCustom on AP_PaymentHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate 
and Set_PeriodCustom.Status = 1  and Set_PeriodCustom.FiscalID = @CurrentFiscalID
WHERE AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID and AP_PaymentHeader.DocumentTypeID = @DocumentTypeID And AP_PaymentHeader.StatusCode = 1 And AP_PaymentHeader.Status = 1 AND AP_PaymentHeader.ReversalDate IS NULL 
and AP_PaymentHeader.StatusCode = 1 AND AP_PaymentHeader.ReversalComments IS NULL 
@IsReversalForm
ORDER BY DocumentNo ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@DocumentTypeID", documentTypeID);
                sqlparams[3] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                if (IsReversalForm)
                    Query_Get_AllDocumentNos = Query_Get_AllDocumentNos.Replace("@IsReversalForm", "AND AP_PaymentHeader.ActivityDate IS NULL");
                else
                    Query_Get_AllDocumentNos = Query_Get_AllDocumentNos.Replace("@IsReversalForm", "");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_Get_AllDocumentNos, sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }

        public static bool IsAdvancedTakenAgainstPO(int companyID, int periodID, int poHeaderID)
        {
            bool result = false;

            string Query_IsAdvanceTaken_AgainstPO = @"
SELECT Count(AP_PaymentHeader.PaymentID) FROM AP_PaymentHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
	    AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.Status = 1 AND SET_DocumentType.DocumentType like '%Against PO%' 
WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.Status = 1 
    AND AP_PaymentHeader.POHeaderID = @POHeaderID ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@POHeaderID", poHeaderID);

                result = Convert.ToInt32(SqlHelper.ExecuteScalar(CommandType.Text, Query_IsAdvanceTaken_AgainstPO, sqlparams)) > 0 ? true : false;
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return result;
        }

        public DataTable Get_SalesTaxInvoiceReceivedAgainstPayment_Report(int companyID, int periodID, Nullable<DateTime> fromDate, Nullable<DateTime> toDate,
            short salesTaxInvoiceReceived)
        {
            DataTable dtToReturn = null;
            string Query_SalesTaxInvoiceReceivedAgainstPayment_Report = @" 
SELECT Set_DocumentType.DocumentType, AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate, AP_PaymentHeader.Amount, AP_PaymentHeader.ReferenceType 
FROM AP_PaymentHeader 
    INNER JOIN SET_DocumentType ON SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
	    AND SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.Status = 1 
WHERE AP_PaymentHeader.CompanyID = @CompanyID AND AP_PaymentHeader.PeriodID = @PeriodID AND AP_PaymentHeader.Status = 1 
    {0} ";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                StringBuilder additionalFilter = new StringBuilder();

                #region Date Range Filter

                if (fromDate.HasValue && toDate.HasValue)
                {
                    additionalFilter.Append(" AND AP_PaymentHeader.DocumentDate BETWEEN @FromDate AND @ToDate");
                    sqlparams[2] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                    sqlparams[3] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }
                else if (fromDate.HasValue)
                {
                    additionalFilter.Append(" AND AP_PaymentHeader.DocumentDate >= @FromDate ");
                    sqlparams[2] = new SqlParameter("@FromDate", DateUtil.ConvertToString(fromDate.Value, true) + " 00:00:00");
                }
                else if (toDate.HasValue)
                {
                    additionalFilter.Append(" AND AP_PaymentHeader.DocumentDate <= @ToDate ");
                    sqlparams[3] = new SqlParameter("@ToDate", DateUtil.ConvertToString(toDate.Value, true) + " 23:59:59");
                }

                #endregion

                switch (salesTaxInvoiceReceived)
                {
                    case 0:
                    case 1:
                        additionalFilter.Append(" AND IsNull(AP_PaymentHeader.IsSalesTaxInvoiceReceived, 0) = " + salesTaxInvoiceReceived);
                        break;
                }

                additionalFilter.Append("     and SET_DocumentType.DocumentType not like '%opening%'     and  AP_PaymentHeader.Amount <>0   and SET_DocumentType.DocumentType not like '%employee%'");

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_SalesTaxInvoiceReceivedAgainstPayment_Report, additionalFilter),
                    sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtToReturn = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtToReturn;
        }

        public static bool UpdateDataForBankReconciliation(int CompanyID, int PeriodID, int PaymentID, DateTime ActivityEntryDate, Nullable<DateTime> ActivityDate, decimal BankBalance, SqlTransaction objTransaction = null)
        {
            bool result = false;
            string Query_UpdateDataForBankReconciliation = @"
Update AP_PaymentHeader Set ActivityDate=@ActivityDate ,ActivityEntryDate=@ActivityEntryDate, BankBalance=@BankBalance
where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID =@PeriodID and AP_PaymentHeader.Status = 1 
and AP_PaymentHeader.PaymentID = @PaymentID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PaymentID", PaymentID);
                sqlparams[3] = new SqlParameter("@ActivityEntryDate", ActivityEntryDate);
                sqlparams[4] = new SqlParameter("@ActivityDate", ActivityDate);
                sqlparams[5] = new SqlParameter("@BankBalance", BankBalance);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataForBankReconciliation, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataForBankReconciliation, sqlparams) > 0)
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

        public static bool UpdateDataForBankReconciliationTSML(int CompanyID, int PeriodID, int PaymentID, DateTime ActivityEntryDate, Nullable<DateTime> ActivityDate, decimal BankBalance, long PartyPageNo = 0, long PageSerialNo = 0, SqlTransaction objTransaction = null)
        {
            bool result = false;
            string Query_UpdateDataForBankReconciliationTSML = @"
Update AP_PaymentHeader Set ActivityDate=@ActivityDate ,ActivityEntryDate=@ActivityEntryDate, BankBalance=@BankBalance,
PartyPageNo = @PartyPageNo, PageSerialNo = @PageSerialNo
where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID =@PeriodID and AP_PaymentHeader.Status = 1 
and AP_PaymentHeader.PaymentID = @PaymentID
";

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@PaymentID", PaymentID);
                sqlparams[3] = new SqlParameter("@ActivityEntryDate", ActivityEntryDate);
                sqlparams[4] = new SqlParameter("@ActivityDate", ActivityDate);
                sqlparams[5] = new SqlParameter("@BankBalance", BankBalance);
                sqlparams[6] = new SqlParameter("@PartyPageNo", PartyPageNo);
                sqlparams[7] = new SqlParameter("@PageSerialNo", PageSerialNo);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataForBankReconciliationTSML, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataForBankReconciliationTSML, sqlparams) > 0)
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

        public static bool UpdateDataForBankReconciliationForReceipt(int CompanyID, int PeriodID, int ReceiptID, DateTime ActivityEntryDate, Nullable<DateTime> ActivityDate, decimal BankBalance, SqlTransaction objTransaction = null)
        {
            bool result = false;
            string Query_UpdateDataForBankReconciliationForReceipt = @"
Update AR_ReceiptTransactionHeader Set ActivityDate=@ActivityDate ,ActivityEntryDate=@ActivityEntryDate, BankBalance=@BankBalance
where AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID =@PeriodID and AR_ReceiptTransactionHeader.Status = 1 
and AR_ReceiptTransactionHeader.ReceiptTransactionID = @ReceiptID
";

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ReceiptID", ReceiptID);
                sqlparams[3] = new SqlParameter("@ActivityEntryDate", ActivityEntryDate);
                sqlparams[4] = new SqlParameter("@ActivityDate", ActivityDate);
                sqlparams[5] = new SqlParameter("@BankBalance", BankBalance);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataForBankReconciliationForReceipt, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataForBankReconciliationForReceipt, sqlparams) > 0)
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

        public static bool UpdateDataForBankReconciliationForReceiptTSML(int CompanyID, int PeriodID, int ReceiptID, DateTime ActivityEntryDate, Nullable<DateTime> ActivityDate, decimal BankBalance, long PartyPageNo = 0, long PageSerialNo = 0, SqlTransaction objTransaction = null)
        {
            bool result = false;
            string Query_UpdateDataForBankReconciliationForReceiptTSML = @"
Update AR_ReceiptTransactionHeader Set ActivityDate=@ActivityDate ,ActivityEntryDate=@ActivityEntryDate, BankBalance=@BankBalance,
PartyPageNo = @PartyPageNo, PageSerialNo = @PageSerialNo
where AR_ReceiptTransactionHeader.CompanyID = @CompanyID and AR_ReceiptTransactionHeader.PeriodID =@PeriodID and AR_ReceiptTransactionHeader.Status = 1 
and AR_ReceiptTransactionHeader.ReceiptTransactionID = @ReceiptID
";

            try
            {


                SqlParameter[] sqlparams = new SqlParameter[8];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@ReceiptID", ReceiptID);
                sqlparams[3] = new SqlParameter("@ActivityEntryDate", ActivityEntryDate);
                sqlparams[4] = new SqlParameter("@ActivityDate", ActivityDate);
                sqlparams[5] = new SqlParameter("@BankBalance", BankBalance);
                sqlparams[6] = new SqlParameter("@PartyPageNo", PartyPageNo);
                sqlparams[7] = new SqlParameter("@PageSerialNo", PageSerialNo);

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_UpdateDataForBankReconciliationForReceiptTSML, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_UpdateDataForBankReconciliationForReceiptTSML, sqlparams) > 0)
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

        public static DataTable SupplierSaleWithHoldingTax(int companyID, int periodID, string dateFrom = null, string dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string RateWise = "", Nullable<decimal> TaxRate = null, Nullable<int> ReportType = null, Nullable<int> GroupDocument = null)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "" && dateTo != "")
                {
                    //sb.Append(" Where TAB.TransactionType != 'Debit' ");
                    if (TaxRate != null)
                    {
                        sb.Append(" Where  ROUND(TAB.TaxRate,2) = " + TaxRate + " ");
                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                    if (GroupDocument.HasValue)
                    {
                        sbGroup.Append("AND SET_DocumentType.GroupID = " + GroupDocument);
                    }

                    if (RateWise == "")
                        RateWise = " ORDER  by TAB.VoucherNo asc ";

                    if (ReportType == 2)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    else if (ReportType == 3)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    else if (ReportType == 4)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");


                    if (ReportType == 5)
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_TaxExemption + RateWise), sb.ToString(), sbGroup.ToString()), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_SupplierSaleWithHoldingTax + RateWise), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
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
        public static DataTable SupplierWithHoldingTaxCane(int companyID, int periodID, string dateFrom = null, string dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string RateWise = "", Nullable<decimal> TaxRate = null, Nullable<int> ReportType = null, Nullable<int> GroupDocument = null)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "" && dateTo != "")
                {
                    //sb.Append(" Where TAB.TransactionType != 'Debit' ");
                    if (TaxRate != null)
                    {
                        sb.Append(" Where  ROUND(TAB.TaxRate,2) = " + TaxRate + " ");
                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                    if (GroupDocument.HasValue)
                    {
                        sbGroup.Append("AND SET_DocumentType.GroupID = " + GroupDocument);
                    }

                    if (RateWise == "")
                        RateWise = " ORDER  by TAB.VoucherNo asc ";


                    if (ReportType == 2)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    else if (ReportType == 3)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    else if (ReportType == 4)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");

                    if (ReportType == 5)
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_TaxExemptionCane + RateWise), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_SupplierWithHoldingTaxCane + RateWise), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
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

        public static DataTable SupplierWithHoldingTax(string companyID, int periodID, string dateFrom = null, string dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string RateWise = "", Nullable<decimal> TaxRate = null, Nullable<int> ReportType = null, string documentGroupID = "",Nullable<int> DocumentTypeiD = null)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "" && dateTo != "")
                {
                    //sb.Append(" Where TAB.TransactionType != 'Debit' ");
                    if (TaxRate != null)
                    {
                        sb.Append(" Where  ROUND(TAB.TaxRate,2) = " + TaxRate + " ");
                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                    if (documentGroupID != "")
                        sbGroup.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                    if (DocumentTypeiD.HasValue)
                    {
                        sbGroup.Append(" AND Set_DocumentType.DocumentTypeID = " + DocumentTypeiD);
                    }

                    if (RateWise == "")
                        RateWise = " ORDER  by TAB.VoucherNo asc ";


                    if (ReportType == 1)
                        sbGroup.Append(" and AR_SET_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = 2 ) ");
                    else if (ReportType == 2)
                        sbGroup.Append(" and AR_SET_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = 1 ) ");
                    else if (ReportType == 3)
                        sbGroup.Append(" and AR_SET_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = 3 ) ");
                    else if (ReportType == 4)
                        sbGroup.Append(" and AR_SET_Customer.CustomerID in (select BusinessPartnerID from AR_SET_BusinessPartnerTypes where TypeCode = 10 ) ");
                    

                    //if (ReportType == 2)
                    //    sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    //else if (ReportType == 3)
                    //    sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    //else if (ReportType == 4)
                    //    sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");

                    companyID = "(" + companyID + ")";

                    if (ReportType == 5)
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_TaxExemption + RateWise).ToString().Replace("@CompanyID", companyID), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
                    else
                        dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_SupplierWithHoldingTax + RateWise).ToString().Replace("@CompanyID", companyID), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
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

        public static DataTable SupplierWiseSaleTax(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string businessPartnerIDs = "", int ReportType = 0, string Ratewise = "", Nullable<decimal> TaxRate = null, Nullable<int> GroupDocument = null)
        {
            DataTable dt = null;
            try
            {
                #region
                string Query_VendorWiseSaleTax = @"
Select * from (
Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,AR_InvoiceHeader.InvoiceNumber as VoucherNo,Set_DocumentType.DocumentType
, 	
Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
ABS((AR_InvoiceHeader.AmountPaid)) -  AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0))) else
sum(AR_InvoiceDetail.GrossAmount) end as AmountPaid,

 max(isnull(AR_InvoiceDetail.TaxAmount,0)) as TaxRate,

 Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
 AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0)))
 else 
(sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100) end As TaxAmount

,(sum(AR_InvoiceDetail.GrossAmount)  + (sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100)) as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
from AR_InvoiceHeader
inner join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceDetail.FiscalID = AR_InvoiceHeader.FiscalID
and AR_InvoiceDetail.Status=1

Left Join AR_Invoice_AccountsDetail on  AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_Invoice_AccountsDetail.FiscalID =  AR_InvoiceHeader.FiscalID
AND AR_Invoice_AccountsDetail.Status = 1 

inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType
and Set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID
and Set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceHeader.VendorID
--and ar_set_customer.CompanyID = AR_InvoiceHeader.CompanyID
and ar_set_customer.PeriodID = AR_InvoiceHeader.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.VendorID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.InvoiceReceivedDate between @FromDate and @ToDate
AND AR_InvoiceHeader.Status=1
and isnull(AR_InvoiceDetail.TaxAmount,0) > 0
{1}
{2}
group by AR_SET_Customer.CustomerName,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,
  AR_InvoiceHeader.InvoiceNumber,  AR_Invoice_AccountsDetail.TransactionType
,Set_DocumentType.DocumentType,AR_SET_Customer.NTNNumber,AR_SET_Customer.STRNNumber,AR_SET_Customer.CNICNumber,
AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName,AR_InvoiceHeader.AmountPaid

union all


Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,IMP_CustomClearing.CreateDate as InvoiceReceivedDate,'' as VendorReferenceNumber,IMP_CustomClearing.CostingID as VoucherNo,'Custom Clearing' as DocumentType
, 	
TotalAmount as AmountPaid,

 0 as TaxRate,

  IMP_CustomClearing.GSTAmount As TaxAmount

,0 as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
from IMP_CustomClearing


inner join AR_SET_Customer on AR_SET_Customer.CustomerID = IMP_CustomClearing.ClearingAgentID
and ar_set_customer.PeriodID = IMP_CustomClearing.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = IMP_CustomClearing.ClearingAgentID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where IMP_CustomClearing.CompanyID = @CompanyID and IMP_CustomClearing.PeriodID = @PeriodID
and IMP_CustomClearing.CreateDate between @FromDate and @ToDate
AND IMP_CustomClearing.Status=1
and isnull(IMP_CustomClearing.GSTAmount,0) > 0
{1}
{2}

union all


Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,IMP_CustomClearing.CreateDate as InvoiceReceivedDate,'' as VendorReferenceNumber,IMP_CustomClearing.CostingID as VoucherNo,'Custom Clearing' as DocumentType
, 	
TotalAmount as AmountPaid,

 0 as TaxRate,

  Fabric_ImportCostingDetail.IncomeTax As TaxAmount

,0 as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
from IMP_CustomClearing
inner join Fabric_ImportCostingHeader on Fabric_ImportCostingHeader.CostingID = IMP_CustomClearing.CostingID
and Fabric_ImportCostingHeader.CompanyID = IMP_CustomClearing.CompanyID
and Fabric_ImportCostingHeader.PeriodID = IMP_CustomClearing.PeriodID
and Fabric_ImportCostingHeader.Status = 1
inner join Fabric_ImportCostingDetail on Fabric_ImportCostingDetail.CostingID = Fabric_ImportCostingHeader.CostingID 
and Fabric_ImportCostingDetail.CompanyID = Fabric_ImportCostingHeader.CompanyID
and Fabric_ImportCostingDetail.Status = 1
and Fabric_ImportCostingDetail.RD is null


inner join AR_SET_Customer on AR_SET_Customer.CustomerID = IMP_CustomClearing.ClearingAgentID
and ar_set_customer.PeriodID = IMP_CustomClearing.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = IMP_CustomClearing.ClearingAgentID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where IMP_CustomClearing.CompanyID = @CompanyID and IMP_CustomClearing.PeriodID = @PeriodID
and IMP_CustomClearing.CreateDate between @FromDate and @ToDate
AND IMP_CustomClearing.Status=1
and isnull(IMP_CustomClearing.GSTAmount,0) > 0
{1}
{2}

union all

Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,Set_ContainerTransportationandUnloading.CreateDate as InvoiceReceivedDate,'' as VendorReferenceNumber,Set_ContainerTransportationandUnloading.ImportDocumentID as VoucherNo,'Custom Clearing' as DocumentType
, 	
InclusiveServiceCharges as AmountPaid,

 0 as TaxRate,

  Set_ContainerTransportationandUnloading.GSTAmount As TaxAmount

,0 as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
from Set_ContainerTransportationandUnloading


inner join AR_SET_Customer on AR_SET_Customer.CustomerID = Set_ContainerTransportationandUnloading.TransporterID
and ar_set_customer.PeriodID = Set_ContainerTransportationandUnloading.PeriodID

left Join SET_City on SET_City.CityID = AR_SET_Customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = Set_ContainerTransportationandUnloading.TransporterID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where Set_ContainerTransportationandUnloading.CompanyID = @CompanyID and Set_ContainerTransportationandUnloading.PeriodID = @PeriodID
and Set_ContainerTransportationandUnloading.CreateDate between @FromDate and @ToDate
AND Set_ContainerTransportationandUnloading.Status=1
and isnull(Set_ContainerTransportationandUnloading.GSTAmount,0) > 0
 {2}
{1}

) as TAB 
{0}
";

                #endregion

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                StringBuilder sbBLFILTER = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (TaxRate != null)
                    {
                        sb.Append(" WHere  ROUND(TAB.TaxRate,2)= " + TaxRate);

                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                    if (GroupDocument.HasValue)
                    {
                        sbGroup.Append("AND SET_DocumentType.GroupID = " + GroupDocument);
                    }
                    if (Ratewise == "")
                        Ratewise = " ORDER  by TAB.VoucherNo asc ";


                    if (ReportType == 2)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    else if (ReportType == 3)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    else if (ReportType == 4)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");

                    if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1") && businessPartnerIDs != null)
                    {
                        sbGroup.Append(" AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    }

                    string Query = "";
                    if (ReportType == 1)
                        Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SupplierWiseSaleTax + Ratewise;
                    else
                        Query = Query_VendorWiseSaleTax + Ratewise;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
                }
                else
                    throw new Exception("Enter Date From & Date To. . .!!");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataFor_PurchaseRegisterSugarUnit(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string businessPartnerIDs = "", int ReportType = 0, string Ratewise = "", Nullable<decimal> TaxRate = null, Nullable<int> GroupDocument = null, int? DateFilterID = null)
        {
            DataTable dt = null;
            try
            {
                #region
                string Query_VendorWiseSaleTax = @"

";

                #endregion

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                StringBuilder sbBLFILTER = new StringBuilder();
                StringBuilder DateFilter = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (TaxRate != null)
                    {
                        sb.Append(" WHere  ROUND(TAB.TaxRate,2)= " + TaxRate);

                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                    if (GroupDocument.HasValue)
                    {
                        sbGroup.Append("AND SET_DocumentType.GroupID = " + GroupDocument);
                    }
                    if (Ratewise == "")
                        Ratewise = " ORDER  by TAB.InvoiceReceivedDate asc ";


                    if (ReportType == 2)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    else if (ReportType == 3)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    else if (ReportType == 4)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");

                    if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1") && businessPartnerIDs != null)
                    {
                        sbGroup.Append(" AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    }

                    if (DateFilterID == 1)
                        DateFilter.Append("and AR_InvoiceHeader.InvoiceReceivedDate between '" + dateFrom.Value.ToString("yyyy-MM-dd") + "' AND '" + dateTo.Value.ToString("yyyy-MM-dd") + "'");
                    else if (DateFilterID == 2)
                        DateFilter.Append("and AR_InvoiceHeader.InvoiceDate between '" + dateFrom.Value.ToString("yyyy-MM-dd") + "' AND '" + dateTo.Value.ToString("yyyy-MM-dd") + "'");
                    else if (DateFilterID == 3)
                        DateFilter.Append("and GL_TransactionHeader.DocumentDate between '" + dateFrom.Value.ToString("yyyy-MM-dd") + "' AND '" + dateTo.Value.ToString("yyyy-MM-dd") + "'");

                    string Query = "";
                    if (ReportType == 1)
                        //    Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SupplierWiseSaleTax + Ratewise;
                        //else
                        Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_PurchaseRegisterSugarUnit_New + Ratewise;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString(), DateFilter.ToString()), sqlparams).Tables[0];
                }
                else
                    throw new Exception("Enter Date From & Date To. . .!!");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        #endregion

        public static DataTable GetTanderDepositeData(int CompanyID, int PeriodID, short? DocummentGroupID, int? DDl_BPFrom, int? DDl_BPTo, DateTime? dateFrom, DateTime? dateTo)
        {

            DataTable dtToReturn = null;

            try
            {
                StringBuilder sb = new StringBuilder();
                string additionalParameters = string.Empty;
                string GroupBy = string.Empty;
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                #region Query
                const string Query_getTenderDepositeDateData = @" Select 
 
AP_PaymentHeader.DocumentDate,
AP_PaymentHeader.DocumentTypeID,
SET_DocumentType.DocumentType,
AP_PaymentHeader.ReceiptDate,
AP_PaymentHeader.PostMarkDate,
AP_PaymentHeader.DocumentNo,
AP_PaymentHeader.Amount,
AR_SET_Customer.CustomerName as BusinessPartner
,AP_PaymentHeader.Comments
,ROW_NUMBER ( ) 
    OVER ( ORDER BY AP_PaymentHeader.DocumentNo) as SrNo
  from  AP_PaymentHeader
  INNER JOIN SET_DocumentType ON SET_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID
  AND SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID
  INNER JOIN AP_PaymentSupplierDetail ON AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
 AND AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
  AND AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
  AND AP_PaymentSupplierDetail.Status=1
 INNER JOIN AR_SET_Customer ON AR_SET_Customer.CustomerID = AP_PaymentSupplierDetail.SupplierID 
 --AND  AR_SET_Customer.CompanyID = AP_PaymentSupplierDetail.CompanyID
 AND AR_SET_Customer.PeriodID = AP_PaymentSupplierDetail.PeriodID 
  
 WHERE SET_DocumentType.DocumentType like '%tender%'
  AND AP_PaymentHeader.CompanyID =  @CompanyID AND AP_PaymentHeader.PeriodID= @PeriodID 
  AND AP_PaymentHeader.DocumentDate between @FromDate AND @ToDate  ";
                #endregion
                // sqlparams[2] = new SqlParameter("@CustomerID", CustomerID);

                if (DDl_BPFrom.HasValue && DDl_BPTo.HasValue)
                    sb.Append(" And AP_PaymentSupplierDetail.SupplierID  between " + DDl_BPFrom + " and " + DDl_BPTo);
                else if (DDl_BPFrom.HasValue && DDl_BPTo == null)
                    sb.Append(" And AP_PaymentSupplierDetail.SupplierID = " + DDl_BPFrom);
                else if (!DDl_BPFrom.HasValue && DDl_BPTo.HasValue)
                    sb.Append(" And AP_PaymentSupplierDetail.SupplierID = " + DDl_BPTo);
                if (DocummentGroupID.HasValue)
                    sb.Append(" AND Set_DocumentType.GroupID = " + DocummentGroupID);



                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query_getTenderDepositeDateData + sb.ToString()), sqlparams);

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
        public DataSet GetPaymentSlipReport(int companyID, int periodId, int? NoFrom, int? NoTo, int? documentTypeId, int? paymentID,
        int? PaymentFormDocumentTypeID)
        {
            DataSet ds1 = new DataSet();

            try
            {
                StringBuilder query = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodId);

                if (NoFrom.HasValue && NoTo.HasValue)
                {
                    query.Append(" AND ([AP_PaymentHeader].[DocumentNo] >= " + NoFrom + " AND [AP_PaymentHeader].[DocumentNo] <= " + NoTo + " ) ");
                    query.Append(" AND [AP_PaymentHeader].[DocumentNo] between " + NoFrom + " AND " + NoTo + "  ");
                }

                if (paymentID.HasValue)
                    query.Append(" AND [AP_PaymentHeader].[PaymentID] = " + paymentID);

                if (PaymentFormDocumentTypeID.HasValue)
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + PaymentFormDocumentTypeID);
                else
                    query.Append(" AND  [AP_PaymentHeader].[DocumentTypeID] = " + documentTypeId);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentSlipReportData, query), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                {
                    ds1 = ds;
                    ds.Tables[0].TableName = "PurchaseOrder";
                    ds.Tables[1].TableName = "Table1";
                    ds.Tables[2].TableName = "GL_Voucher";
                    ds.Tables[3].TableName = "SupplierDTL";
                }
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return ds1;
        }

        public static DataTable GetDailyTransactionListUnPosted(int companyID, int periodID, Guid userID, DateTime? dateFrom = null,
     DateTime? dateTo = null, Nullable<short> documentGroupID = null, Nullable<short> DocumentTypeID = null, bool documentTypeAuthentication = false, int? BusinessPartnerID = null, int? docfrom = null, int? docto = null, int? FiscalID = null)
        {
            DataTable dt = null;
            string additionalParameters = string.Empty;
            string additionalParameters1 = string.Empty;
            string qry_docuTypeAuthenticate = string.Empty;

            StringBuilder sb = new StringBuilder();
            StringBuilder sb2 = new StringBuilder();

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[6];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    //additionalParameters = " AND Cast(Convert(Varchar(10),ap_paymentHeader.DocumentDate,110) as datetime) between @FromDate and @ToDate";
                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                }


                #region Filter by Sales Order Document Number

                if (docfrom.HasValue && docto.HasValue)
                    sb2.Append(" AND DocumentNo BETWEEN " + docfrom.Value + " AND " + docto.Value);
                else if (docfrom.HasValue)
                    sb2.Append(" AND DocumentNo >= " + docfrom.Value);
                else if (docto.HasValue)
                    sb2.Append(" AND DocumentNo <= " + docto.Value);

                #endregion

                //if (accountCodeFrom != string.Empty)
                //{
                //    accountCodeFilter = " AND  AccountCodes =  '" + accountCodeFrom + "' ";
                //}

                //if (documentGroupID.HasValue && documentGroupID.Value.ToString()!="-1")
                //    additionalParameters = additionalParameters + " AND (Set_DocumentType.GroupID = " + documentGroupID + " or " + documentGroupID + " = " + documentGroupID + " )";
                if (DocumentTypeID.HasValue)
                    additionalParameters = additionalParameters + " AND Set_DocumentType.DocumentTypeID = " + DocumentTypeID;
                if (documentTypeAuthentication)
                {
                    sqlparams[4] = new SqlParameter("@UserID", userID);
                    qry_docuTypeAuthenticate = @"  INNER JOIN UsersInDocumentType ON UsersInDocumentType.CompanyID = SET_DocumentType.CompanyID 
AND UsersInDocumentType.DocumentTypeID = SET_DocumentType.DocumentTypeID AND UsersInDocumentType.UserID = @UserID  ";
                }

                sqlparams[5] = new SqlParameter("@CurrentFiscalID", FiscalID);

                //sqlparams[5] = new SqlParameter("@VendorID", BusinessPartnerID);

                //if (BusinessPartnerID.HasValue)
                //    sb.Append(" and vendorid = "+ BusinessPartnerID.Value);


                // sb.Append(" AND Cast(Convert(Varchar(10),ap_paymentHeader.DocumentDate,110) as datetime) between @FromDate and @ToDate");

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_DailyCashTransactionUnPosted, additionalParameters,
                    qry_docuTypeAuthenticate, sb.ToString(), sb2.ToString()), sqlparams).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public bool InProcessDocument(GL_TransactionHeader th, List<GL_TransactionDetail> colTranDetail, int CompanyID, int PeriodID, int CurrentFiscalID)
        {
            bool result = false;
            SqlTransaction sqltrans = SqlHelper.GetTransaction("InProcessPayment");

            try
            {
                this.StatusCode = 5;
                this.UpdateDataByKey(sqltrans);
                GL_TransactionDetail.DeleteDetailDataByKey(sqltrans, CompanyID, PeriodID, th.TransactionID, th.DefaultChartofAccountID.ToString().ToIntOrZero());
                GL_TransactionHeader.Delete_By_PrimaryKey(CompanyID, PeriodID, th.TransactionID, sqltrans);

                SET_DocumentApprove.DeleteApprovalHistroy(CompanyID, PeriodID, this.DocumentNo, this.DocumentTypeID, sqltrans, int.Parse(this.FiscalID.ToString()));

                AR_InvoiceHeader.UpdatePaymentStatusOnInvoiceByPaymentID(sqltrans, CompanyID, PeriodID, this.PaymentID);

                AP_PaymentSupplierDetail.DeletePermenantDataByKeyPaymentID(sqltrans, CompanyID, PeriodID, this.PaymentID);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InProcessPayment");
            }

            return result;
        }
        public Double CheckAmountForAdvanceAgainstPurchasecontract(int companyID, int periodID, int ContractHeaderID)
        {
            double amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractHeaderID", ContractHeaderID);

                amount = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_CheckAmount_AdvanceAgainstPurchasecontract, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amount;
        }
        public static decimal ConversionRateforImportDoc(int companyID, int periodID, int ContractHeaderID)
        {
            decimal amount = 0;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@ContractHeaderID", ContractHeaderID);

                amount = Convert.ToDecimal(SqlHelper.ExecuteScalar(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetPaymentExchangeRate, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return amount;
        }

        public DataTable GetCustomerAgingReportData(int companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
          Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> documentGroupID = null)
        {
            DataTable dtToReturn = null;
            string strFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));

                #region Filter by Business Partner Type Code

                if (businessPartnerType.HasValue)
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", businessPartnerType.Value);
                else
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", DBNull.Value);

                #endregion

                #region Filter by Business Partners
                #region Filter by Business Partners

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    strFilter = @" AND (AR_SET_Customer.CustomerID BETWEEN @BusinessPartnerIDFrom
                                                              AND
                                                              @BusinessPartnerIDTo OR @BusinessPartnerIDFrom IS null) ";
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (!businessPartnerIDFrom.HasValue && !businessPartnerIDTo.HasValue)
                {
                    strFilter = @" AND (AR_SET_Customer.CustomerID BETWEEN @BusinessPartnerIDFrom
                                                              AND
                                                              @BusinessPartnerIDTo OR @BusinessPartnerIDFrom IS null) ";
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", DBNull.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", DBNull.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    strFilter = " AND AR_SET_Customer.CustomerID >= @BusinessPartnerIDFrom ";
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    strFilter = "AND AR_SET_Customer.CustomerID <= @BusinessPartnerIDFrom";
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                #endregion

                #endregion

                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);
                #region Old

                // DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_CustomerAgingReport, strFilter), sqlparams);

                //                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                //                {
                //                    dtToReturn = ds.Tables[0];

                //                    #region Datatable change

                //                    if (!dtToReturn.Columns.Contains("RunningBalance"))
                //                    {
                //                        dtToReturn.Columns.Add("RunningBalance", typeof(decimal));

                //                    }

                //                    decimal openingbalance = 0, totalFrieght = 0;
                //                    int countforCustomer = 0, j=0;

                //                    for (int i = 0; i < dtToReturn.Rows.Count; i++)
                //                    {
                //                        if (dtToReturn.Rows[i]["FrightAmount"].ToString() != "")
                //                            totalFrieght += Convert.ToDecimal(dtToReturn.Rows[i]["FrightAmount"].ToString());

                //                        if (i == dtToReturn.Rows.Count - 1)
                //                        {
                //                            openingbalance = Convert.ToDecimal(dtToReturn.Rows[i]["DirectReceiprt"]) + totalFrieght;
                //                            totalFrieght = 0;
                //                            dtToReturn.Rows[i - j]["RunningBalance"] = openingbalance;
                //                            countforCustomer++;
                //                            j = 0;
                //                        }
                //                        if ((i > 0 && dtToReturn.Rows[i]["CustomerID"].ToString() != dtToReturn.Rows[i - 1]["CustomerID"].ToString()))
                //                        {
                //                            openingbalance = Convert.ToDecimal(dtToReturn.Rows[i-1]["DirectReceiprt"]) + totalFrieght;
                //                            totalFrieght = 0;
                //                            dtToReturn.Rows[i - j]["RunningBalance"] = openingbalance;
                //                            countforCustomer++;
                //                            j = 0;
                //                        }

                //                        j++;
                //                    }

                /////////////            REVERsAL
                //                    for (int i = 0; i < dtToReturn.Rows.Count; i++)
                //                    {
                //                        if (i > 0 && dtToReturn.Rows[i]["ReversalID"].ToString() != null)
                //                        {
                //                            for (int J = 0; J < dtToReturn.Rows.Count; J++)
                //                            {
                //                                if (dtToReturn.Rows[J]["InvoiceID"].ToString() == dtToReturn.Rows[i]["ReversalID"].ToString())
                //                                { decimal ReverseAmount=0;
                //                                    ReverseAmount -= Convert.ToDecimal(dtToReturn.Rows[i]["Balance"]);
                //                                    dtToReturn.Rows[J]["Balance"] = ReverseAmount;
                //                                }
                //                            }
                //                        }
                //                    }
                //                   // openingbalance = Convert.ToDecimal(dtToReturn.Rows[0]["DirectReceiprt"]) + totalFrieght;
                //                  openingbalance= Convert.ToDecimal(dtToReturn.Rows[0]["RunningBalance"]);

                //                    for (int i = 0; i < dtToReturn.Rows.Count; i++)
                //                    {
                //                        if (i > 0 && dtToReturn.Rows[i]["CustomerID"].ToString() != dtToReturn.Rows[i - 1]["CustomerID"].ToString())
                //                            openingbalance = Convert.ToDecimal(dtToReturn.Rows[i]["RunningBalance"]);

                //                        openingbalance -= Convert.ToDecimal(dtToReturn.Rows[i]["Balance"]);
                //                        dtToReturn.Rows[i]["RunningBalance"] = openingbalance;

                //                    }
                //                    try
                //                    {
                //                        if (dtToReturn.Select(" RunningBalance < 0 ").Length > 0)
                //                            dtToReturn = dtToReturn.Select(" RunningBalance < 0 ").CopyToDataTable();

                //                        DataView dv = dtToReturn.DefaultView;
                //                        dv.Sort = "SupplierName,invoiceDate asc";
                //                        dtToReturn = dv.ToTable();
                //                        dtToReturn.Rows[0]["Balance"] = Math.Abs(Convert.ToDecimal(dtToReturn.Rows[0]["RunningBalance"]));
                //                        for (int i = 0; i < dtToReturn.Rows.Count; i++)
                //                        {
                //                            if (i > 0 && dtToReturn.Rows[i]["CustomerID"].ToString() != dtToReturn.Rows[i - 1]["CustomerID"].ToString())
                //                            dtToReturn.Rows[i]["Balance"] = Math.Abs(Convert.ToDecimal(dtToReturn.Rows[i]["RunningBalance"]));
                //                        }
                //                    }
                //                    catch { }
                //                    #endregion

                //                }
                #endregion

                #region new
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_CustomerAgingReportOnlyInvoice, strFilter), sqlparams);




                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    dtToReturn = ds.Tables[0];

                    string appendReceipt = @" WHERE   AR_ReceiptTransactionHeader.CompanyID = {0}
                            AND AR_ReceiptTransactionHeader.PeriodID = {1}
                            AND AR_ReceiptTransactionHeader.DocumentDate <= @AsOnDate
                            AND AR_SET_Customer.CustomerID = {2} ";
                    string appendPayment = @" WHERE   AP_PaymentHeader.CompanyID = {0}
                            AND AP_PaymentHeader.PeriodID = {1}
                            AND AP_PaymentHeader.DocumentDate <= @AsOnDate
                            AND AR_SET_Customer.CustomerID = {2} ";


                    appendReceipt = string.Format(appendReceipt, companyID, periodID, dtToReturn.Rows[0]["CustomerID"].ToString());
                    appendPayment = string.Format(appendPayment, companyID, periodID, dtToReturn.Rows[0]["CustomerID"].ToString());

                    DataSet dsReceipt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AR.AR_ReceiptHeader.Query_CustomerAgingReportOnlyReceipts, appendReceipt, appendPayment), sqlparams);

                    DataTable dt_Receipt = dsReceipt.Tables[0];

                    try
                    {
                        DataTable TempReversalDt = null;

                        if (dtToReturn.Select("ReversalID>0").Length > 0)
                            TempReversalDt = dtToReturn.Select("ReversalID>0").CopyToDataTable();

                        DataTable dr_RowDelete = null;
                        dr_RowDelete = dtToReturn.Clone();

                        if (TempReversalDt != null && TempReversalDt.Rows.Count > 0)
                        {

                            for (int i = 0; i < TempReversalDt.Rows.Count; i++)
                            {
                                for (int x = 0; x < dtToReturn.Rows.Count; x++)
                                {
                                    if (TempReversalDt.Rows[i]["ReversalID"].ToString() == dtToReturn.Rows[x]["InvoiceID"].ToString())
                                    {
                                        // dtToReturn.Rows[x].Delete();

                                        dr_RowDelete.Rows.Add(dtToReturn.Rows[x].ItemArray);
                                    }
                                }

                            }

                            for (int i = 0; i < TempReversalDt.Rows.Count; i++)
                            {
                                for (int x = 0; x < dtToReturn.Rows.Count; x++)
                                {
                                    if (TempReversalDt.Rows[i]["ReversalID"].ToString() == dtToReturn.Rows[x]["ReversalID"].ToString())
                                    {
                                        //dtToReturn.Rows[x].Delete();

                                        dr_RowDelete.Rows.Add(dtToReturn.Rows[x].ItemArray);
                                    }
                                }

                            }


                        }


                        for (int i = 0; i < dr_RowDelete.Rows.Count; i++)
                        {
                            for (int x = 0; x < dtToReturn.Rows.Count; x++)
                            {
                                if (dr_RowDelete.Rows[i]["InvoiceID"].ToString() == dtToReturn.Rows[x]["InvoiceID"].ToString())
                                {
                                    dtToReturn.Rows[x].Delete();
                                    dtToReturn.AcceptChanges();
                                }
                            }
                        }


                        DataTable dt_MultiReceiptCheck = null;


                        for (int x = 0; x < dtToReturn.Rows.Count; x++)
                        {
                            dt_MultiReceiptCheck = AR_ReceiptTransactionDetail.GetMultiReceiptByInvoiceID(companyID, periodID, Convert.ToInt32(dtToReturn.Rows[x]["InvoiceID"].ToString()));
                            if (dt_MultiReceiptCheck != null && dt_MultiReceiptCheck.Rows.Count > 0)
                            {
                                if (Convert.ToDecimal(dt_MultiReceiptCheck.Rows[0]["Amount"]) >= Convert.ToDecimal(dtToReturn.Rows[x]["Amount"].ToString()))
                                {
                                    //dtToReturn.Rows[x].Delete();

                                    dr_RowDelete.Rows.Add(dtToReturn.Rows[x].ItemArray);

                                }
                                else if (Convert.ToDecimal(dt_MultiReceiptCheck.Rows[0]["Amount"]) < Convert.ToDecimal(dtToReturn.Rows[x]["Amount"].ToString()))
                                {
                                    dtToReturn.Rows[x]["Amount"] = Convert.ToDecimal(dtToReturn.Rows[x]["Amount"].ToString()) - Convert.ToDecimal(dt_MultiReceiptCheck.Rows[0]["Amount"]);
                                    dtToReturn.Rows[x]["Balance"] = dtToReturn.Rows[x]["Amount"];
                                    dtToReturn.Rows[x]["InvoiceAmount"] = dtToReturn.Rows[x]["Amount"];
                                }
                            }
                            dt_MultiReceiptCheck = null;
                            dtToReturn.AcceptChanges();
                        }

                        for (int i = 0; i < dr_RowDelete.Rows.Count; i++)
                        {
                            for (int x = 0; x < dtToReturn.Rows.Count; x++)
                            {
                                if (dr_RowDelete.Rows[i]["InvoiceID"].ToString() == dtToReturn.Rows[x]["InvoiceID"].ToString())
                                {
                                    dtToReturn.Rows[x].Delete();
                                    dtToReturn.AcceptChanges();
                                }
                            }
                        }


                        decimal computeTotalIssuance = 0;
                        try
                        {
                            computeTotalIssuance = Convert.ToDecimal(dt_Receipt.Compute("Sum(Amount)", ""));

                            decimal totalFrieght = 0;
                            int countforCustomer = 0, j = 0;

                            for (int i = 0; i < dtToReturn.Rows.Count; i++)
                            {
                                if (dtToReturn.Rows[i]["FrightAmount"].ToString() != "")
                                    totalFrieght += Convert.ToDecimal(dtToReturn.Rows[i]["FrightAmount"].ToString());

                                if (i == dtToReturn.Rows.Count - 1)
                                {
                                    computeTotalIssuance = computeTotalIssuance + totalFrieght;
                                    countforCustomer++;
                                    j = 0;
                                }
                            }
                        }
                        catch { }

                        if (!dtToReturn.Columns.Contains("RunningBalance"))
                        {
                            dtToReturn.Columns.Add("RunningBalance", typeof(decimal));

                        }

                        if (!dtToReturn.Columns.Contains("IsIssued"))
                        {
                            dtToReturn.Columns.Add("IsIssued", typeof(int));

                        }

                        if (!dtToReturn.Columns.Contains("FinalBalance"))
                        {
                            dtToReturn.Columns.Add("FinalBalance", typeof(decimal));

                        }
                        try
                        {
                            for (int i = 0; i < dtToReturn.Rows.Count; i++)
                            {
                                if (computeTotalIssuance > 0)
                                {
                                    if (computeTotalIssuance >= Convert.ToDecimal(dtToReturn.Rows[i]["InvoiceAmount"]))
                                    {
                                        computeTotalIssuance = computeTotalIssuance - Convert.ToDecimal(dtToReturn.Rows[i]["InvoiceAmount"]);
                                        dtToReturn.Rows[i]["Balance"] = 0;
                                    }
                                    else if (computeTotalIssuance < Convert.ToDecimal(dtToReturn.Rows[i]["InvoiceAmount"]))
                                    {
                                        dtToReturn.Rows[i]["Balance"] = Convert.ToDecimal(dtToReturn.Rows[i]["InvoiceAmount"]) - computeTotalIssuance;
                                        // computeTotalIssuance = Convert.ToDecimal(dtToReturn.Rows[i]["Balance"]);
                                        computeTotalIssuance = 0;
                                    }
                                    else
                                    {

                                        dtToReturn.Rows[i]["Balance"] = Convert.ToDecimal(dtToReturn.Rows[i]["InvoiceAmount"]);
                                        //computeTotalIssuance = Convert.ToDecimal(dtToReturn.Rows[i]["Balance"]);
                                    }
                                }
                                else
                                {
                                    dtToReturn.Rows[i]["Balance"] = dtToReturn.Rows[i]["InvoiceAmount"];
                                }
                            }


                            try
                            {
                                dtToReturn = dtToReturn.Select("Balance<>0").CopyToDataTable();

                            }
                            catch { dtToReturn = null; }
                        }
                        catch
                        {


                        }

                    }
                    catch
                    {


                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return dtToReturn;
        }


        public static DataTable GetChequePrintingSlip(int companyID, int periodID, string chequeno, SqlTransaction sqlTransaction = null)
        {
            DataTable dt = null;

            try
            {

                string filter = " WHERE [AP_PaymentHeader].[CompanyID] = " + companyID + " AND [AP_PaymentHeader].[PeriodID] =  " + periodID + " and [AP_PaymentBanksDetail].[InstrumentNo] = '" + chequeno + "'";
                if (sqlTransaction == null)
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_ChequePrintingSlip, filter)).Tables[0];
                else
                    dt = SqlHelper.ExecuteDataset(sqlTransaction, CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_ChequePrintingSlip, filter)).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static Double GetLCAmount(int companyID, int periodID, int LCNo)
        {
            double Rate = 0;

            #region Query

            string Query_GetRate = @"Select sum(AP_PaymentSupplierDetail.Amount) as TotalAmount from AP_PaymentSupplierDetail
		Inner Join AP_PaymentHeader on AP_PaymentHeader.PaymentID=AP_PaymentSupplierDetail.PaymentHeaderID
		AND AP_PaymentHeader.CompanyID=AP_PaymentSupplierDetail.CompanyID
		AND AP_PaymentHeader.PeriodID=AP_PaymentSupplierDetail.PeriodID
		AND AP_PaymentHeader.Status = 1
 Where SupplierID=@SupplierID And AP_PaymentSupplierDetail.Status=1 
And AP_PaymentSupplierDetail.CompanyId=@CompanyID and AP_PaymentSupplierDetail.PeriodID=@PeriodID
Group By AP_PaymentSupplierDetail.CompanyID, AP_PaymentSupplierDetail.PeriodID, SupplierID   ";

            #endregion

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SupplierID", LCNo);

                Rate = Convert.ToDouble(SqlHelper.ExecuteScalar(CommandType.Text, Query_GetRate, sqlparams));
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return Rate;
        }
        public static DataTable GetPaymentDocType(int companyID, int periodID)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);


                ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_DocumenttypesforPaymentStatus, sqlparams);

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

        public bool UpdateDataByDDLValues(int Companyid, int perioid, int DocumentNo, int Doctype, string Query, int StatusCode)
        {
            SqlTransaction objTransaction = null;
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", Companyid);
                sqlparams[1] = new SqlParameter("@PeriodID", perioid);
                sqlparams[2] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[3] = new SqlParameter("@StatusCode", StatusCode);
                sqlparams[4] = new SqlParameter("@DocType", Doctype);

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
        public AP_PaymentHeader(int CompanyID, int PeriodID, int DocumentNo, int DocypeID)
        {
            Load(CompanyID, PeriodID, DocumentNo, DocypeID);
        }
        private void Load(int companyID, int periodID, int DocumentNo, int Doctype, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow drRecord = GetDataByKey(companyID, periodID, DocumentNo, Doctype, objConnection, objTransaction);

            if (drRecord != null)
            {
                this.f_IsDataLoaded = true;
                this.f_CompanyID = Convert.ToInt32(drRecord["CompanyID"]);
                this.f_PeriodID = Convert.ToInt32(drRecord["PeriodID"]);
                this.f_PaymentID = Convert.ToInt32(drRecord["PaymentID"]);
                this.f_DocumentTypeID = Convert.ToInt32(drRecord["DocumentTypeID"]);
                this.f_DocumentNo = Convert.ToInt32(drRecord["DocumentNo"]);
                this.f_DocumentDate = Convert.ToDateTime(drRecord["DocumentDate"]);

                if (drRecord["AddressVerificationCode"] != DBNull.Value)
                    this.f_AddressVerificationCode = Convert.ToString(drRecord["AddressVerificationCode"]);


                if (drRecord["Amount"] != DBNull.Value)
                    this.f_Amount = Convert.ToDouble(drRecord["Amount"]);


                if (drRecord["AntiCipatedClearingDate"] != DBNull.Value)
                    this.f_AntiCipatedClearingDate = Convert.ToDateTime(drRecord["AntiCipatedClearingDate"]);


                if (drRecord["PayFromCustomerID"] != DBNull.Value)
                    this.f_PayFromCustomerID = Convert.ToInt32(drRecord["PayFromCustomerID"]);


                if (drRecord["PaymentServerOrderNo"] != DBNull.Value)
                    this.f_PaymentServerOrderNo = Convert.ToString(drRecord["PaymentServerOrderNo"]);


                if (drRecord["PostMarkDate"] != DBNull.Value)
                    this.f_PostMarkDate = Convert.ToDateTime(drRecord["PostMarkDate"]);


                if (drRecord["ProgramID"] != DBNull.Value)
                    this.f_ProgramID = Convert.ToInt32(drRecord["ProgramID"]);


                if (drRecord["ReceiptDate"] != DBNull.Value)
                    this.f_ReceiptDate = Convert.ToDateTime(drRecord["ReceiptDate"]);


                if (drRecord["ReceiptMethodID"] != DBNull.Value)
                    this.f_ReceiptMethodID = Convert.ToInt32(drRecord["ReceiptMethodID"]);


                if (drRecord["ReceiptNo"] != DBNull.Value)
                    this.f_ReceiptNo = Convert.ToString(drRecord["ReceiptNo"]);


                if (drRecord["Comments"] != DBNull.Value)
                    this.f_Comments = Convert.ToString(drRecord["Comments"]);

                this.f_CurrencyId = Convert.ToInt32(drRecord["CurrencyId"]);

                if (drRecord["BankAccountID"] != DBNull.Value)
                    this.f_BankAccountID = Convert.ToInt32(drRecord["BankAccountID"]);


                if (drRecord["CustomerBankBranchID"] != DBNull.Value)
                    this.f_CustomerBankBranchID = Convert.ToInt32(drRecord["CustomerBankBranchID"]);


                if (drRecord["CustomerReceiptReference"] != DBNull.Value)
                    this.f_CustomerReceiptReference = Convert.ToString(drRecord["CustomerReceiptReference"]);


                if (drRecord["DepositDate"] != DBNull.Value)
                    this.f_DepositDate = Convert.ToDateTime(drRecord["DepositDate"]);


                if (drRecord["ExchangeDate"] != DBNull.Value)
                    this.f_ExchangeDate = Convert.ToDateTime(drRecord["ExchangeDate"]);


                if (drRecord["ExchangeRate"] != DBNull.Value)
                    this.f_ExchangeRate = Convert.ToDouble(drRecord["ExchangeRate"]);


                if (drRecord["ExchangeRateType"] != DBNull.Value)
                    this.f_ExchangeRateType = Convert.ToString(drRecord["ExchangeRateType"]);


                if (drRecord["FactorDiscountAmount"] != DBNull.Value)
                    this.f_FactorDiscountAmount = Convert.ToDouble(drRecord["FactorDiscountAmount"]);


                if (drRecord["IssueDate"] != DBNull.Value)
                    this.f_IssueDate = Convert.ToDateTime(drRecord["IssueDate"]);


                if (drRecord["IssuerBankBranchID"] != DBNull.Value)
                    this.f_IssuerBankBranchID = Convert.ToInt32(drRecord["IssuerBankBranchID"]);


                if (drRecord["IssuerName"] != DBNull.Value)
                    this.f_IssuerName = Convert.ToString(drRecord["IssuerName"]);

                this.f_OverRemitAccountFlag = Convert.ToBoolean(drRecord["OverRemitAccountFlag"]);

                if (drRecord["ReceivablesTrxID"] != DBNull.Value)
                    this.f_ReceivablesTrxID = Convert.ToInt32(drRecord["ReceivablesTrxID"]);


                if (drRecord["Reference"] != DBNull.Value)
                    this.f_Reference = Convert.ToString(drRecord["Reference"]);


                if (drRecord["ReferenceType"] != DBNull.Value)
                    this.f_ReferenceType = Convert.ToString(drRecord["ReferenceType"]);


                if (drRecord["RemittanceBankAccount"] != DBNull.Value)
                    this.f_RemittanceBankAccount = Convert.ToString(drRecord["RemittanceBankAccount"]);


                if (drRecord["ReversalCategory"] != DBNull.Value)
                    this.f_ReversalCategory = Convert.ToString(drRecord["ReversalCategory"]);


                if (drRecord["ReversalComments"] != DBNull.Value)
                    this.f_ReversalComments = Convert.ToString(drRecord["ReversalComments"]);


                if (drRecord["ReversalDate"] != DBNull.Value)
                    this.f_ReversalDate = Convert.ToDateTime(drRecord["ReversalDate"]);


                if (drRecord["ReversalReasonCode"] != DBNull.Value)
                    this.f_ReversalReasonCode = Convert.ToString(drRecord["ReversalReasonCode"]);

                this.f_SelectedForFactoringFlag = Convert.ToBoolean(drRecord["SelectedForFactoringFlag"]);

                if (drRecord["SelectedRemittanceBatchID"] != DBNull.Value)
                    this.f_SelectedRemittanceBatchID = Convert.ToInt32(drRecord["SelectedRemittanceBatchID"]);


                if (drRecord["ApprovalCode"] != DBNull.Value)
                    this.f_ApprovalCode = Guid.Parse(drRecord["ApprovalCode"].ToString());

                this.f_OverrideRemitAccountFlag = Convert.ToBoolean(drRecord["OverrideRemitAccountFlag"]);
                this.f_ConfirmedFlag = Convert.ToBoolean(drRecord["ConfirmedFlag"]);
                this.f_Status = Convert.ToBoolean(drRecord["Status"]);
                this.f_CreatedBy = Guid.Parse(drRecord["CreatedBy"].ToString());
                this.f_CreateDate = Convert.ToDateTime(drRecord["CreateDate"]);

                if (drRecord["UpdatedBy"] != DBNull.Value)
                    this.f_UpdatedBy = Guid.Parse(drRecord["UpdatedBy"].ToString());


                if (drRecord["UpdateDate"] != DBNull.Value)
                    this.f_UpdateDate = Convert.ToDateTime(drRecord["UpdateDate"]);

                this.f_CreatedByValue = Convert.ToString(drRecord["CreatedByValue"]);

                if (drRecord["UpdatedByValue"] != DBNull.Value)
                    this.f_UpdatedByValue = Convert.ToString(drRecord["UpdatedByValue"]);


                if (drRecord["StatusCode"] != DBNull.Value)
                    this.f_StatusCode = Convert.ToInt16(drRecord["StatusCode"]);


                if (drRecord["SourceHeaderID"] != DBNull.Value)
                    this.f_SourceHeaderID = Convert.ToInt32(drRecord["SourceHeaderID"]);


                if (drRecord["VendorID"] != DBNull.Value)
                    this.f_VendorID = Convert.ToInt32(drRecord["VendorID"]);


                if (drRecord["ExpenseAccountCCID"] != DBNull.Value)
                    this.f_ExpenseAccountCCID = Convert.ToString(drRecord["ExpenseAccountCCID"]);


                if (drRecord["IsMultipleInvoices"] != DBNull.Value)
                    this.f_IsMultipleInvoices = Convert.ToBoolean(drRecord["IsMultipleInvoices"]);

                this.f_IsInvoiceAdjusted = Convert.ToBoolean(drRecord["IsInvoiceAdjusted"]);

                if (drRecord["POHeaderID"] != DBNull.Value)
                    this.f_POHeaderID = Convert.ToInt32(drRecord["POHeaderID"]);


                if (drRecord["OtherAmount"] != DBNull.Value)
                    this.f_OtherAmount = Convert.ToDecimal(drRecord["OtherAmount"]);


                if (drRecord["OtherChartOfAccountID"] != DBNull.Value)
                    this.f_OtherChartOfAccountID = Convert.ToInt32(drRecord["OtherChartOfAccountID"]);


                if (drRecord["OtherCCID"] != DBNull.Value)
                    this.f_OtherCCID = Convert.ToString(drRecord["OtherCCID"]);


                if (drRecord["BusinessPartnerTypeID"] != DBNull.Value)
                    this.f_BusinessPartnerTypeID = Convert.ToInt16(drRecord["BusinessPartnerTypeID"]);


                if (drRecord["CustomerTransactionType"] != DBNull.Value)
                    this.f_CustomerTransactionType = Convert.ToString(drRecord["CustomerTransactionType"]);


                if (drRecord["WithHoldingTax"] != DBNull.Value)
                    this.f_WithHoldingTax = Convert.ToDecimal(drRecord["WithHoldingTax"]);


                if (drRecord["LocationID"] != DBNull.Value)
                    this.f_LocationID = Convert.ToInt32(drRecord["LocationID"]);


                if (drRecord["IsSalesTaxInvoiceReceived"] != DBNull.Value)
                    this.f_IsSalesTaxInvoiceReceived = Convert.ToBoolean(drRecord["IsSalesTaxInvoiceReceived"]);


                if (drRecord["ActivityDate"] != DBNull.Value)
                    this.f_ActivityDate = Convert.ToDateTime(drRecord["ActivityDate"]);


                if (drRecord["ActivityEntryDate"] != DBNull.Value)
                    this.f_ActivityEntryDate = Convert.ToDateTime(drRecord["ActivityEntryDate"]);


                if (drRecord["BankBalance"] != DBNull.Value)
                    this.f_BankBalance = Convert.ToDecimal(drRecord["BankBalance"]);


                if (drRecord["LoanHeaderid"] != DBNull.Value)
                    this.f_LoanHeaderid = Convert.ToInt32(drRecord["LoanHeaderid"]);


                if (drRecord["ControlCredit"] != DBNull.Value)
                    this.f_ControlCredit = Convert.ToDecimal(drRecord["ControlCredit"]);


                if (drRecord["ControlDebit"] != DBNull.Value)
                    this.f_ControlDebit = Convert.ToDecimal(drRecord["ControlDebit"]);


                if (drRecord["CostCentreID"] != DBNull.Value)
                    this.f_CostCentreID = Convert.ToInt32(drRecord["CostCentreID"]);


                if (drRecord["FiscalID"] != DBNull.Value)
                    this.f_FiscalID = Convert.ToInt32(drRecord["FiscalID"]);


                if (drRecord["LogisticServicesID"] != DBNull.Value)
                    this.f_LogisticServicesID = Convert.ToInt32(drRecord["LogisticServicesID"]);

            }
        }

        public static DataRow GetDataByKey(int companyID, int periodID, int DocumentNo, int Doctype, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataRow dr = null;
            const string Query_GetData_byDocumentNoandType = @" SELECT * FROM [AP_PaymentHeader] WHERE [CompanyID] = @CompanyID 
and DocumentNo = @DocumentNo and [PeriodID] = @PeriodID
And DocumentTypeID=@Doctype
";
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@DocumentNo", DocumentNo);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@DocType", Doctype);
                DataSet ds = null;

                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, Query_GetData_byDocumentNoandType, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, Query_GetData_byDocumentNoandType, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetData_byDocumentNoandType, sqlparams);

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


        public static DataTable GetDataForLeaseFinanceYearlyPositionCrossReport(int CompanyID, int PeriodID, DateTime? Date_From, DateTime? Date_To)
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
                //    sb.Append(" and  AP_PaymentHeader.DocumentDate between @FromDate and @ToDate ");
                //}
                //else if (Date_From != null)
                //{
                //    sb.Append(" and AP_PaymentHeader.DocumentDate = @FromDate ");
                //}
                //else if (Date_From == null)
                //{
                //    sb.Append(" and AP_PaymentHeader.DocumentDate = @ToDate");
                //}
                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.LeaseFinanceYearlyPositionCrossReport + sb.ToString(), sqlparams);

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

        public static DataTable GetDataLeadSummaryReport(Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[2];
                sqlparams[0] = new SqlParameter("@FromDate", fromDate);
                sqlparams[1] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_LeadSummaryReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_LeadSummaryReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_LeadSummaryReport, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }


        public static DataTable GetFinanceCostReport(int CompanyID, int PeriodID, int fiscalid, Nullable<DateTime> fromDate = null, Nullable<DateTime> toDate = null, SqlConnection objConnection = null, SqlTransaction objTransaction = null)
        {
            DataTable dt = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FiscalID", fiscalid);
                sqlparams[3] = new SqlParameter("@FromDate", fromDate);
                sqlparams[4] = new SqlParameter("@ToDate", toDate);

                DataSet ds = null;


                if (objTransaction != null)
                    ds = SqlHelper.ExecuteDataset(objTransaction, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_FinancecostReport, sqlparams);
                else if (objConnection != null)
                    ds = SqlHelper.ExecuteDataset(objConnection, CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_FinancecostReport, sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_GetData_FinancecostReport, sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                    dt = ds.Tables[0];

                return dt;
            }

            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
        }

        public static DataSet GetAllDataAgingAdvancesToSuppliers(Nullable<int> periodID, string companyID, Nullable<DateTime> Date_From, Nullable<DateTime> Date_To, SqlConnection objConnection = null)
        {
            DataSet ds = null;
            try
            {
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@FromDate", Date_From);
                sqlparams[3] = new SqlParameter("@ToDate", Date_To);

                companyID = "(" + companyID + ")";

                if (objConnection == null)
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.AgingAdvancesToSuppliers).Replace("@CompanyID", companyID), sqlparams);
                else
                    ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.AgingAdvancesToSuppliers).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    return ds;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataSet GetAllDataAgingPayableToSuppliers(string companyID, int periodID, DateTime asOnDate, Nullable<short> businessPartnerType = null,
            Nullable<int> businessPartnerIDFrom = null, Nullable<int> businessPartnerIDTo = null, Nullable<int> documentGroupID = null)
        {
            DataSet ds = null;
            string strFilter = string.Empty;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[9];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@AsOnDate", DateUtil.ConvertToDate(DateUtil.ConvertToString(asOnDate, true) + " 23:59:59"));

                #region Filter by Business Partner Type Code

                if (businessPartnerType.HasValue)
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", businessPartnerType.Value);
                else
                    sqlparams[3] = new SqlParameter("@BusinessPartnerType", DBNull.Value);

                #endregion

                #region Filter by Business Partners

                if (businessPartnerIDFrom.HasValue && businessPartnerIDTo.HasValue)
                {
                    strFilter = " AND AR_SET_Customer.CustomerID BETWEEN @BusinessPartnerIDFrom AND @BusinessPartnerIDTo ";
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }
                else if (businessPartnerIDFrom.HasValue)
                {
                    strFilter = " AND AR_SET_Customer.CustomerID >= @BusinessPartnerIDFrom ";
                    sqlparams[4] = new SqlParameter("@BusinessPartnerIDFrom", businessPartnerIDFrom.Value);
                }
                else if (businessPartnerIDTo.HasValue)
                {
                    strFilter = "AND AR_SET_Customer.CustomerID <= @BusinessPartnerIDFrom";
                    sqlparams[5] = new SqlParameter("@BusinessPartnerIDTo", businessPartnerIDTo.Value);
                }

                #endregion



                if (documentGroupID.HasValue)
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", documentGroupID.Value);
                else
                    sqlparams[6] = new SqlParameter("@DocumentGroupID", DBNull.Value);

                companyID = "(" + companyID + ")";


                ds = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.AgingPayableToSuppliers, strFilter).Replace("@CompanyID", companyID), sqlparams);

                if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
                {
                    return ds;
                }
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }
            return ds;
        }

        public static DataTable SupplierWiseClaimInvoices(int companyID, int periodID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string businessPartnerIDs = "", int ReportType = 0, string Ratewise = "", Nullable<decimal> TaxRate = null, Nullable<int> GroupDocument = null)
        {
            DataTable dt = null;
            try
            {
                #region
                string Query_VendorWiseSaleTax = @"
Select * from (
Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,AR_InvoiceHeader.InvoiceNumber as VoucherNo,Set_DocumentType.DocumentType
, 	
Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
ABS((AR_InvoiceHeader.AmountPaid)) -  AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0))) else
sum(AR_InvoiceDetail.GrossAmount) end as AmountPaid,

 max(isnull(AR_InvoiceDetail.TaxAmount,0)) as TaxRate,

 Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
 AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0)))
 else 
(sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100) end As TaxAmount

,(sum(AR_InvoiceDetail.GrossAmount)  + (sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100)) as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
 ,( Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
ABS((AR_InvoiceHeader.AmountPaid)) -  AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0))) else
sum(AR_InvoiceDetail.GrossAmount) end) + (Case When AR_Invoice_AccountsDetail.TransactionType = 'Credit' Then 
 AR_InvoiceHeader.AmountPaid * max(isnull(AR_InvoiceDetail.TaxAmount,0)) / (100 + max(isnull(AR_InvoiceDetail.TaxAmount,0)))
 else 
(sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100) end ) as ValueIncluSaleTax
 ,AR_InvoiceHeader.InvoiceID,-1 as ClaimHeaderID, -1 as ClaimDetailID
from AR_InvoiceHeader
inner join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceDetail.FiscalID = AR_InvoiceHeader.FiscalID
and AR_InvoiceDetail.Status=1

Left Join AR_Invoice_AccountsDetail on  AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_Invoice_AccountsDetail.FiscalID =  AR_InvoiceHeader.FiscalID
AND AR_Invoice_AccountsDetail.Status = 1 

inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType
and Set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID
and Set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceHeader.VendorID
--and ar_set_customer.CompanyID = AR_InvoiceHeader.CompanyID
and ar_set_customer.PeriodID = AR_InvoiceHeader.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceHeader.VendorID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.InvoiceReceivedDate between @FromDate and @ToDate
AND AR_InvoiceHeader.Status=1
and isnull(AR_InvoiceDetail.TaxAmount,0) > 0
and AR_InvoiceHeader.InvoiceID not in (select InvoiceID from  AR_ClaimInvoiceDetail where status = 1)
and AR_SET_BusinessPartner.BusinessPartnerID = 1
and AR_InvoiceHeader.statusCode = 1
{1}
{2}
group by AR_SET_Customer.CustomerName,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,
  AR_InvoiceHeader.InvoiceNumber,  AR_Invoice_AccountsDetail.TransactionType
,Set_DocumentType.DocumentType,AR_SET_Customer.NTNNumber,AR_SET_Customer.STRNNumber,AR_SET_Customer.CNICNumber,
AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName,AR_InvoiceHeader.AmountPaid,AR_InvoiceHeader.InvoiceID
) as TAB 
{0}

union all

Select * from (
Select distinct AR_SET_Customer.CustomerName
,(Case when AR_SET_Customer.NTNNumber='' then null
else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
 end )as NTNNumber
,(Case when AR_SET_Customer.STRNNumber='' then null
else substring(AR_SET_Customer.STRNNumber ,0,3)+'-'+
substring( AR_SET_Customer.STRNNumber ,3,2) +'-'+
substring( AR_SET_Customer.STRNNumber ,5,4) +'-'+
substring( AR_SET_Customer.STRNNumber ,9,3) +'-'+
substring( AR_SET_Customer.STRNNumber ,12,len( AR_SET_Customer.STRNNumber )-1)
end )as STRNNumber
,(Case when AR_SET_Customer.CNICNumber='' then null
else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
end )as CNICNumber
,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,AR_InvoiceHeader.InvoiceNumber as VoucherNo,Set_DocumentType.DocumentType
,AR_InvoiceSupplierDetail.Amount as AmountPaid,

 AR_Invoice_AccountsDetail.Amount / AR_InvoiceSupplierDetail.Amount * 100 as TaxRate,

AR_Invoice_AccountsDetail.Amount As TaxAmount

,(sum(AR_InvoiceDetail.GrossAmount)  + (sum(AR_InvoiceDetail.GrossAmount) * isnull(max(AR_InvoiceDetail.TaxAmount),0) / 100)) as WithTaxAmount

 ,AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName
 ,isnull(AR_InvoiceSupplierDetail.Amount,0)  + isnull(AR_Invoice_AccountsDetail.Amount,0) as ValueIncluSaleTax
 ,AR_InvoiceHeader.InvoiceID,-1 as ClaimHeaderID, -1 as ClaimDetailID
 
from AR_InvoiceHeader
left join AR_InvoiceDetail on AR_InvoiceDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceDetail.FiscalID = AR_InvoiceHeader.FiscalID
and AR_InvoiceDetail.Status=1

inner Join AR_Invoice_AccountsDetail on  AR_Invoice_AccountsDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_Invoice_AccountsDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_Invoice_AccountsDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_Invoice_AccountsDetail.FiscalID =  AR_InvoiceHeader.FiscalID
AND AR_Invoice_AccountsDetail.Status = 1 
and AR_Invoice_AccountsDetail.CodeCombinationID = '02-30-005-002'


inner Join AR_InvoiceSupplierDetail on  AR_InvoiceSupplierDetail.InvoiceHeaderID = AR_InvoiceHeader.InvoiceID
and AR_InvoiceSupplierDetail.CompanyID = AR_InvoiceHeader.CompanyID
and AR_InvoiceSupplierDetail.PeriodID = AR_InvoiceHeader.PeriodID
and AR_InvoiceSupplierDetail.FiscalID =  AR_InvoiceHeader.FiscalID
AND AR_InvoiceSupplierDetail.Status = 1 
AND AR_InvoiceSupplierDetail.BusinessPartnerTypeID = 1


inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AR_InvoiceHeader.InvoiceType
and Set_DocumentType.CompanyID = AR_InvoiceHeader.CompanyID
and Set_DocumentType.PeriodID = AR_InvoiceHeader.PeriodID

inner join AR_SET_Customer on AR_SET_Customer.CustomerID = AR_InvoiceSupplierDetail.SupplierID
and ar_set_customer.PeriodID = AR_InvoiceSupplierDetail.PeriodID

left Join SET_City on SET_City.CityID = ar_set_customer.CityID
And SET_City.Status=1 and ar_set_customer.CityID <> -1

inner join AR_SET_BusinessPartnerTypes on AR_SET_BusinessPartnerTypes.BusinessPartnerID = AR_InvoiceSupplierDetail.SupplierID
inner join AR_SET_BusinessPartner on AR_SET_BusinessPartner.BusinessPartnerID = AR_SET_BusinessPartnerTypes.TypeCode

where AR_InvoiceHeader.CompanyID = @CompanyID and AR_InvoiceHeader.PeriodID = @PeriodID
and AR_InvoiceHeader.InvoiceReceivedDate between @FromDate and @ToDate
AND AR_InvoiceHeader.Status=1
and AR_InvoiceHeader.InvoiceID not in (select InvoiceID from  AR_ClaimInvoiceDetail where status = 1)
and AR_SET_BusinessPartner.BusinessPartnerID = 1
and AR_InvoiceHeader.statusCode = 1
{1}
{2}
group by AR_SET_Customer.CustomerName,AR_InvoiceHeader.InvoiceReceivedDate,AR_InvoiceHeader.VendorReferenceNumber,
  AR_InvoiceHeader.InvoiceNumber,  AR_Invoice_AccountsDetail.TransactionType
,Set_DocumentType.DocumentType,AR_SET_Customer.NTNNumber,AR_SET_Customer.STRNNumber,AR_SET_Customer.CNICNumber,
AR_SET_Customer.AddressLine1, AR_SET_Customer.CityID , SET_City.CityName,AR_InvoiceHeader.AmountPaid,AR_InvoiceHeader.InvoiceID
,AR_InvoiceSupplierDetail.Amount,AR_Invoice_AccountsDetail.Amount 
) as TAB 
{0}
";

                #endregion

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                StringBuilder sbBLFILTER = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    if (TaxRate != null)
                    {
                        sb.Append(" WHere  ROUND(TAB.TaxRate,2)= " + TaxRate);

                    }

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);
                    if (GroupDocument.HasValue)
                    {
                        sbGroup.Append("AND SET_DocumentType.GroupID = " + GroupDocument);
                    }
                    //if (Ratewise == "")
                    //    Ratewise = " ORDER  by TAB.VoucherNo asc ";


                    if (ReportType == 2)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 1");
                    else if (ReportType == 3)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 3");
                    else if (ReportType == 4)
                        sbTypeFilter.Append(" and AR_SET_BusinessPartner.BusinessPartnerID = 10");

                    if (!string.IsNullOrEmpty(businessPartnerIDs) && !businessPartnerIDs.Equals("-1") && businessPartnerIDs != null)
                    {
                        sbGroup.Append(" AND AR_Set_Customer.CustomerID " + businessPartnerIDs + " ");
                    }

                    string Query = "";
                    if (ReportType == 1)
                        Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SupplierWiseSaleTax + Ratewise;
                    else
                        Query = Query_VendorWiseSaleTax + Ratewise;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
                }
                else
                    throw new Exception("Enter Date From & Date To. . .!!");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable SupplierWiseClaimPayments(int companyID, int periodID, int fiscalID, Nullable<DateTime> dateFrom = null, Nullable<DateTime> dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string businessPartnerIDs = "", int ReportType = 0, string Ratewise = "", Nullable<decimal> TaxRate = null, Nullable<int> GroupDocument = null)
        {
            DataTable dt = null;
            try
            {
                #region
                string Query_ForPaymenttoWiseSaleTax = @"
select * from
(
Select Distinct AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.FiscalID, AP_PaymentHeader.PaymentID,
       AP_PaymentHeader.Amount, AP_PaymentBanksDetail.Amount as PaymentBankAmount,
	   AR_SET_Customer.CustomerName, SET_DocumentType.DocumentType, SET_DocumentType.DocumentTypeID,
	   AP_PaymentHeader.DocumentNo, AP_PaymentHeader.DocumentDate,
	   ISNULL((Case when AR_SET_Customer.CNICNumber ='' then null
        else substring(AR_SET_Customer.CNICNumber,0,6) +'-'+
			substring(AR_SET_Customer.CNICNumber,6,7) +'-'+
			substring( AR_SET_Customer.CNICNumber ,13,len( AR_SET_Customer.CNICNumber )-1)
		end ),
		(Case when AR_SET_Customer.NTNNumber='' then null
			else substring(AR_SET_Customer.NTNNumber ,0,8)+'-'+
			substring( AR_SET_Customer.NTNNumber ,8,len( AR_SET_Customer.NTNNumber )-1)
			end )) as CNICNumber,
		GL_CodeCombinationDetail.CodeCombinationID  + '-' + GL_CodeCombinationDetail.Description as SupplierAccountCodeDescription
		, AP_PaymentSupplierDetail.Narration, AP_PaymentSupplierDetail.TaxName, AP_PaymentSupplierDetail.Amount as TaxAmount
		, AP_PaymentSupplierDetail.TransactionType ,-1 as ClaimHeaderID, -1 as ClaimDetailID
		,AR_Set_Businesspartner.BusinessPartnerID, AR_Set_Businesspartner.BusinessPartnerName
From AP_PaymentHeader
     Inner join Set_DocumentType on Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID
		and Set_DocumentType.CompanyID = AP_PaymentHeader.CompanyID
		and Set_DocumentType.PeriodID = AP_PaymentHeader.PeriodID
		and AP_PaymentHeader.status = 1 and AP_PaymentHeader.StatusCode = 1
      Inner Join ApplicationMenu on ApplicationMenu.MenuID = Set_DocumentType.MenuID
	        and ApplicationMenu.MenuPath = '/AP/Transaction/Payment.aspx'
	  Inner join AP_PaymentSupplierDetail on AP_PaymentSupplierDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
		and AP_PaymentSupplierDetail.CompanyID = AP_PaymentHeader.CompanyID
		and AP_PaymentSupplierDetail.PeriodID = AP_PaymentHeader.PeriodID
		and AP_PaymentSupplierDetail.status = 1
      Inner join AR_SET_Customer on AR_SET_Customer.CustomerID  = AP_PaymentSupplierDetail.SupplierID
	    and AR_SET_Customer.Status = 1
	  Inner Join AP_PaymentBanksDetail on AP_PaymentBanksDetail.PaymentHeaderID = AP_PaymentHeader.PaymentID
	       And AP_PaymentBanksDetail.CompanyID = AP_PaymentHeader.CompanyID
		   And AP_PaymentBanksDetail.PeriodID = AP_PaymentHeader.PeriodID
		   And AP_PaymentBanksDetail.Status = 1
	 left join GL_CodeCombinationDetail on GL_CodeCombinationDetail.CodeCombinationID = AP_PaymentSupplierDetail.SupplierAccountCode
     Inner join AR_Set_Businesspartner on AR_Set_Businesspartner.BusinessPartnerID = AP_PaymentSupplierDetail.BusinessPartnerTypeID
WHERE 
   (
    AP_PaymentHeader.Status = 1  
    AND AP_PaymentHeader.CompanyID = @CompanyID
    AND AP_PaymentHeader.PeriodID = @PeriodID 
    AND AP_PaymentHeader.FiscalID= @CurrentFiscalID
    AND ISNULL(AP_PaymentSupplierDetail.Narration,'') != 'GST'
    AND (ISNULL(AP_PaymentSupplierDetail.TaxName, '') LIKE '%WHT%' OR AP_PaymentSupplierDetail.BusinessPartnerTypeID IN (51, 52, 54, 56, 58, 87))
    AND AP_PaymentSupplierDetail.TransactionType = 'Credit'
    AND AP_PaymentHeader.DocumentDate <= @ToDate 
    )
  ) Tab

  where Tab.PaymentID not in (Select PaymentID from AP_PaidUnPaidWHTDetail 
  where AP_PaidUnPaidWHTDetail.Status = 1) And Tab.FiscalID = @CurrentFiscalID

";

                #endregion

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                StringBuilder sbBLFILTER = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[5];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", fiscalID);

                if (dateFrom.HasValue && dateTo.HasValue)
                {

                    //if (TaxRate != null)
                    //{
                    //    sb.Append(" WHere  ROUND(TAB.TaxRate,2)= " + TaxRate);

                    //}

                    sqlparams[3] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[4] = new SqlParameter("@ToDate", dateTo);


                    string Query = "";
                    //if (ReportType == 1)
                    //    Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SupplierWiseSaleTax + Ratewise;
                    //else
                    Query = Query_ForPaymenttoWiseSaleTax + Ratewise;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query), sqlparams).Tables[0];
                }
                else
                    throw new Exception("Enter Date From & Date To. . .!!");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable SupplierWiseSaleTaxClaimReport(int companyID, int periodID, Nullable<DateTime> FromDate = null, Nullable<DateTime> ToDate = null, int ReportType = 0, int DateType = 0)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);

                if (FromDate.HasValue && ToDate.HasValue)
                {
                    if (DateType == 1)
                        sb.Append("and AR_ClaimInvoiceDetail.ClaimDate between @FromDate and @ToDate");
                    else if (DateType == 2)
                        sb.Append("and AR_InvoiceHeader.InvoiceReceivedDate between @FromDate and @ToDate");
                    else if (DateType == 3)
                        sb.Append("and AR_InvoiceHeader.InvoiceDate between @FromDate and @ToDate");
                }


                if (ReportType == 1)
                {
                    sb.Append(" AND AR_InvoiceHeader.InvoiceID in (select InvoiceID from AR_ClaimInvoiceDetail where status = 1 and CompanyID = @CompanyID ) ");

                }
                else if (ReportType == 2)
                {
                    sb.Append(" AND AR_InvoiceHeader.InvoiceID not in (select InvoiceID from AR_ClaimInvoiceDetail where status = 1 and CompanyID = @CompanyID ) ");

                }



                string Query = "";
                Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_VendorWiseSaleTax;

                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams).Tables[0];


            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static DataTable GetDataForPaidUnpaidWHTReport(int companyID, int periodID, int fiscalID, Nullable<DateTime> AsOnDate = null, int ReportType = 0)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();

                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@CurrentFiscalID", fiscalID);

                if (AsOnDate.HasValue)
                {
                    sqlparams[3] = new SqlParameter("@AsOnDate", AsOnDate);


                    if (ReportType == 1)
                    {
                        sb.Append(" Where Tab.PaymentID in (select PaymentID from AP_PaidUnPaidWHTDetail where status = 1 and CompanyID = @CompanyID and AP_PaidUnPaidWHTDetail.ClaimDate <= @AsOnDate ) ");
                        sb.Append(" and Tab.ClaimDates <= @AsOnDate");
                        sb.Append(" and Tab.FiscalID =  @CurrentFiscalID");
                        sb.Append(" Order by Tab.ClaimDates asc ");
                    }
                    else if (ReportType == 2)
                    {
                        sb.Append(" Where Tab.PaymentID not in (select PaymentID from AP_PaidUnPaidWHTDetail where status = 1 and CompanyID = @CompanyID) ");
                        sb.Append(" and Tab.DocumentDate <=  @AsOnDate");
                        sb.Append(" and Tab.FiscalID =  @CurrentFiscalID");
                        sb.Append(" Order by Tab.DocumentDate asc");
                    }


                    string Query = "";

                    Query = FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_ForPaidUnpaidWHTReport;

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Query, sb.ToString()), sqlparams).Tables[0];
                }
                else
                    throw new Exception("Enter Date . .!!");

            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }


        public static DataTable GetPaymentDirectPaymentAmount(int companyID, int periodID, int PaymentID, int SupplierID, SqlTransaction sqlTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[4];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@SupplierID", SupplierID);
                sqlparams[3] = new SqlParameter("@PaymentID", PaymentID);

                string Query_GetPaymentDirectPaymentAmount = @"
select sum(AP_PaymentSupplierDetail.Amount)  as Amount
from AP_PaymentSupplierDetail 
inner join AP_PaymentHeader on AP_PaymentHeader.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID
and AP_PaymentHeader.CompanyID = AP_PaymentSupplierDetail.CompanyID
and AP_PaymentHeader.PeriodID = AP_PaymentSupplierDetail.PeriodID
where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID
and AP_PaymentSupplierDetail.Status = 1 and AP_PaymentSupplierDetail.BusinessPartnerTypeID=62
and AP_PaymentSupplierDetail.PaymentHeaderID = @PaymentID and AP_PaymentSupplierDetail.SupplierID = @SupplierID
and AP_PaymentSupplierDetail.Amount > 0
";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetPaymentDirectPaymentAmount, sqlparams);

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


        public DataTable GetDirectPaymentForPaymentLookup(int companyID, int periodID, Guid userID, int? SupplierID, string Fillter)
        {
            DataTable dtPaymentReceipt = null;
            StringBuilder sp = new StringBuilder();

            if (SupplierID.HasValue)
                sp.Append(" and AP_PaymentHeader.paymentid in (  select paymentheaderid from AP_PaymentSupplierDetail  where supplierid = " + SupplierID.Value + " ) ");

            if (Fillter != "")
                sp.Append(Fillter);



            string Query_GetPaymentForPaymentLookup = @" 
select isnull((select sum(AdvanceAmount) from AR_InvoiceAdvancePaymentDetail where PaymentHeaderID = tab.PaymentID and CompanyID = tab.CompanyID ),0) as AdvanceAjusted,*  from (
select AP_PaymentHeader.PaymentID, AP_PaymentHeader.DocumentNo,AP_PaymentHeader.DocumentDate, sum(AP_PaymentSupplierDetail.Amount)  as Amount,AP_PaymentHeader.CompanyID
,SET_DocumentType.DocumentType
from AP_PaymentSupplierDetail 
inner join AP_PaymentHeader on AP_PaymentHeader.PaymentID = AP_PaymentSupplierDetail.PaymentHeaderID
and AP_PaymentHeader.CompanyID = AP_PaymentSupplierDetail.CompanyID
and AP_PaymentHeader.PeriodID = AP_PaymentSupplierDetail.PeriodID
Inner Join Set_DocumentType ON Set_DocumentType.DocumentTypeID = AP_PaymentHeader.DocumentTypeID AND SET_DocumentType.CompanyID = AP_PaymentHeader.CompanyID 
AND SET_DocumentType.PeriodID = AP_PaymentHeader.PeriodID AND Set_DocumentType.Status = 1 
where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID
and SET_DocumentType.DocumentType like '%Direct Payment%' AND AP_PaymentHeader.StatusCode = 1 and SET_DocumentType.DocumentType not like '%opening%' 
and AP_PaymentSupplierDetail.Status = 1 and AP_PaymentSupplierDetail.BusinessPartnerTypeID=62
and AP_PaymentHeader.paymentid in (  select PaymentID from AP_PaymentSupplierDetail where BusinessPartnerTypeID=62 ) 
{0}  
and AP_PaymentSupplierDetail.Amount > 0
group by AP_PaymentHeader.PaymentID,AP_PaymentHeader.DocumentNo,AP_PaymentHeader.CompanyID,AP_PaymentHeader.DocumentDate
,SET_DocumentType.DocumentType
)as tab

where tab.Amount > isnull((select sum(AdvanceAmount) from AR_InvoiceAdvancePaymentDetail where PaymentHeaderID = tab.PaymentID and CompanyID = tab.CompanyID ),0)
                ";


            try
            {
                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@UserID", userID);

                DataSet ds = SqlHelper.ExecuteDataset(CommandType.Text, String.Format(Query_GetPaymentForPaymentLookup, sp), sqlparams);

                if (ds != null && ds.Tables.Count > 0)
                    dtPaymentReceipt = ds.Tables[0];
            }
            catch (Exception exp)
            {
                SqlHelper.LogException(exp);
                throw;
            }

            return dtPaymentReceipt;
        }

        public static DataTable GetCanePaymentData(int companyID, int periodID, int PaymentID, SqlTransaction sqlTransaction = null)
        {
            DataTable dtToReturn = null;
            DataSet ds = null;
            try
            {

                SqlParameter[] sqlparams = new SqlParameter[3];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);
                sqlparams[2] = new SqlParameter("@PaymentID", PaymentID);

                string Query_GetCanePaymentData = @"
select Distinct Cane_Payment.FK_GrowerCodeID, AP_PaymentHeader.DocumentNo,Cane_Payment. PaymentMode,ScrollFrom,ScrollTo,ar_set_bankAccount.BankBranchID,ar_set_bankAccount.BankAccountName,AP_PaymentHeader.BankAccountID,Cane_Payment.PaymentDate
,Cane_Payment.statusCode,AP_PaymentHeader.Amount
from AP_PaymentHeader
inner join Cane_Payment on Cane_Payment.paymentUniqueID  = AP_PaymentHeader.SourceHeaderid
and Cane_Payment.CompanyID = AP_PaymentHeader.CompanyID
and Cane_Payment.PeriodID = AP_PaymentHeader.PeriodID
and Cane_Payment.Status = 1
inner join ar_set_bankAccount  on ar_set_bankAccount.BankAccountID = AP_PaymentHeader.BankAccountID
where AP_PaymentHeader.CompanyID = @CompanyID and AP_PaymentHeader.PeriodID = @PeriodID and AP_PaymentHeader.status = 1
and AP_PaymentHeader.PaymentID = @PaymentID 
";

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query_GetCanePaymentData, sqlparams);

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

        public DataTable GetAllDataWithOtherTablesScroll(string strWhere, ref int ReturnTotalRows, string OrderBy, int? PageIndex = null, int? PageSize = null,
            Enumaration.SortDirection sortdirection = Enumaration.SortDirection.asc, int CurrentFiscalID = 0)
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

                    string Query_SelectAll_WithPaging_WithOtherTables = @" 
Select *,ROW_NUMBER() Over (Order by paymentid desc) as 'Row'  from
(
SELECT distinct AP_PaymentHeader.CompanyID, AP_PaymentHeader.PeriodID, AP_PaymentHeader.PaymentID, AP_PaymentHeader.DocumentTypeID, AP_PaymentHeader.DocumentNo, 
    AP_PaymentHeader.DocumentDate, AP_PaymentHeader.AddressVerificationCode, AP_PaymentHeader.Amount,AP_PaymentHeader.ControlCredit, AP_PaymentHeader.AntiCipatedClearingDate, 
    AP_PaymentHeader.PayFromCustomerID, AP_PaymentHeader.PaymentServerOrderNo, AP_PaymentHeader.PostMarkDate, AP_PaymentHeader.ProgramID, 
    AP_PaymentHeader.ReceiptDate, AP_PaymentHeader.ReceiptMethodID, AP_PaymentHeader.ReceiptNo, AP_PaymentHeader.Comments, AP_PaymentHeader.CurrencyId, 
    AP_PaymentHeader.BankAccountID, AP_PaymentHeader.CustomerBankBranchID, AR_SET_BankBranch.BankBranchName, AP_PaymentHeader.CustomerReceiptReference, AP_PaymentHeader.DepositDate, 
    AP_PaymentHeader.ExchangeDate, AP_PaymentHeader.ExchangeRate, AP_PaymentHeader.ExchangeRateType, AP_PaymentHeader.FactorDiscountAmount, 
    AP_PaymentHeader.IssueDate, AP_PaymentHeader.IssuerBankBranchID, AP_PaymentHeader.IssuerName, AP_PaymentHeader.OverRemitAccountFlag, 
    AP_PaymentHeader.ReceivablesTrxID, AP_PaymentHeader.Reference, AP_PaymentHeader.ReferenceType, AP_PaymentHeader.RemittanceBankAccount, 
    AP_PaymentHeader.ReversalCategory, AP_PaymentHeader.ReversalComments, AP_PaymentHeader.ReversalDate, AP_PaymentHeader.ReversalReasonCode, 
    AP_PaymentHeader.SelectedForFactoringFlag, AP_PaymentHeader.SelectedRemittanceBatchID, AP_PaymentHeader.ApprovalCode, AP_PaymentHeader.StatusCode, 
    AP_PaymentHeader.OverrideRemitAccountFlag, AP_PaymentHeader.ConfirmedFlag, AP_PaymentHeader.Status, AP_PaymentHeader.CreatedBy, AP_PaymentHeader.CreateDate, 
    AP_PaymentHeader.UpdatedBy, AP_PaymentHeader.UpdateDate, AP_PaymentHeader.CreatedByValue, AP_PaymentHeader.UpdatedByValue, AR_SET_Customer.CustomerID, 
    SET_DocumentType.DocumentType, Set_PeriodCustom.PeriodSetName,AP_PaymentHeader.SourceHeaderID
FROM AP_PaymentHeader 
inner join Cane_Payment on Cane_Payment.PaymentUniqueID = AP_PaymentHeader.SourceHeaderID 
and Cane_Payment.CompanyID = AP_PaymentHeader.CompanyID
and Cane_Payment.PeriodID = AP_PaymentHeader.PeriodID
and Cane_Payment.Status = AP_PaymentHeader.Status

    INNER JOIN SET_DocumentType ON AP_PaymentHeader.CompanyID = SET_DocumentType.CompanyID AND AP_PaymentHeader.Status = 1 
        and AP_PaymentHeader.PeriodID = SET_DocumentType.PeriodID AND AP_PaymentHeader.DocumentTypeID = SET_DocumentType.DocumentTypeID 
    left outer JOIN AR_SET_Customer ON AP_PaymentHeader.PeriodID = AR_SET_Customer.PeriodID 
        
        AND AP_PaymentHeader.VendorID = AR_SET_Customer.CustomerID AND AR_SET_Customer.Status = 1
		inner join Set_PeriodCustom on AP_PaymentHeader.DocumentDate between Set_PeriodCustom.StartDate and Set_PeriodCustom.EndDate
        and Set_PeriodCustom.Status=1 and SET_PeriodCustom.FiscalID = @CurrentFiscalID
    left join AR_SET_BankBranch ON AR_SET_BankBranch.BankBranchID = AP_PaymentHeader.CustomerBankBranchID AND AR_SET_BankBranch.Status = 1
	AND AR_SET_BankBranch.IsActive = 1

{0}

) as tab
";

                    #endregion

                    sqlparamsList.Add(new SqlParameter("@PageIndex", PageIndex));
                    sqlparamsList.Add(new SqlParameter("@PageSize", PageSize));
                    sqlparamsList.Add(new SqlParameter("@TotalRows", ReturnTotalRows));

                    SqlParameter sqlparamTotalRows = sqlparamsList.Find(a => a.ParameterName == "@TotalRows");
                    sqlparamTotalRows.Direction = ParameterDirection.Output;

                    SqlParameter[] sqlparams = sqlparamsList.ToArray();
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), string.Format(Query_SelectAll_WithPaging_WithOtherTables, strWhere.ToString())
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];
                    //dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(Paging, Query_SelectAll_WithPaging_WithOtherTables + ' ' + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), Query_SelectAll_WithPaging_WithOtherTables + strWhere
                    //    + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty)), sqlparams).Tables[0];

                    ReturnTotalRows = int.Parse(sqlparamTotalRows.Value.ToString());
                }
                else
                    dt = SqlHelper.ExecuteDataset(CommandType.Text, FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_SelectAll_WithOtherTables + strWhere
                        + (!string.IsNullOrEmpty(OrderBy) ? OrderBy : string.Empty), sqlparamsList.ToArray()).Tables[0];
            }
            catch (Exception ex)
            {
                SqlHelper.LogException(ex);
                throw ex;
            }

            return dt;
        }

        public static bool CanePaymentDeleteDataByKey(SqlTransaction objTransaction, Guid updatedBy, DateTime updateDate, string updatedByValue, int companyID, int SourceHeaderID, int periodID)
        {
            bool result = false;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[7];
                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@SourceHeaderID", SourceHeaderID);
                sqlparams[2] = new SqlParameter("@PeriodID", periodID);
                sqlparams[3] = new SqlParameter("@Status", false);
                sqlparams[4] = new SqlParameter("@UpdatedBy", updatedBy);
                sqlparams[5] = new SqlParameter("@UpdateDate", updateDate);
                sqlparams[6] = new SqlParameter("@UpdatedByValue", updatedByValue);

                const string Query_CanePaymentDeleteDataByKey = @" 
Update [AP_PaymentHeader] Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue 
WHERE [CompanyID] = @CompanyID  and [PeriodID] = @PeriodID  
AND DocumentTypeID = 157 and SourceHeaderID = @SourceHeaderID 

Update cane_payment  Set [Status] = @Status, [UpdatedBy] = @UpdatedBy, [UpdateDate] = @UpdateDate, [UpdatedByValue] = @UpdatedByValue 
where [CompanyID] = @CompanyID and [PeriodID] = @PeriodID   AND PaymentUniqueID = @SourceHeaderID";

                if (objTransaction == null)
                {
                    if (SqlHelper.ExecuteNonQuery(CommandType.Text, Query_CanePaymentDeleteDataByKey, sqlparams) > 0)
                        result = true;
                }
                else
                {
                    if (SqlHelper.ExecuteNonQuery(objTransaction, CommandType.Text, Query_CanePaymentDeleteDataByKey, sqlparams) > 0)
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

        public bool InsertForPFUtility_Frm(List<AP_PaymentSupplierDetail> lst_PaymentSupplier, GL_TransactionHeader colth, List<GL_TransactionDetail> colTranDetail, PFUtility_Log col_PFUtilityLog, int CurrentFiscalID, SqlTransaction sqltrans = null)
        {
            bool result = false;

            try
            {
                this.DocumentNo = SET_DocumentSequence.ManualDocumentSequence(f_DocumentTypeID, f_CompanyID, f_PeriodID, sqltrans, CurrentFiscalID);
                this.PaymentID = AP_PaymentHeader.GetNewPaymentID(f_CompanyID, f_PeriodID, sqltrans);

                this.Insert(sqltrans);

                #region Insert in Payment Supplier table
                if (lst_PaymentSupplier != null)
                {
                    short paymentSupplierID = (short)AP_PaymentSupplierDetail.GetNewPaymentSupplierID(f_CompanyID, this.PaymentID, f_PeriodID, sqltrans);
                    foreach (AP_PaymentSupplierDetail objPaymentSupplier in lst_PaymentSupplier)
                    {
                        objPaymentSupplier.PaymentHeaderID = this.PaymentID;
                        objPaymentSupplier.PaymentSupplierID = paymentSupplierID++;
                        objPaymentSupplier.FiscalID = CurrentFiscalID;
                        objPaymentSupplier.Insert(sqltrans);
                    }
                }

                #endregion

                #region Insert In GL_TransactionHeader
                
                colth.DocumentNo = f_DocumentNo;
                colth.TransactionID = GL_TransactionHeader.GetNewTransactionID(f_CompanyID, f_PeriodID, sqltrans);
                colth.FiscalID = CurrentFiscalID;
                colth.Insert(sqltrans);
                
                #endregion
                
                #region Insert In GL_TransactionDetail

                    //decimal AmountDebit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Debit").Select(c => c.Amount).Sum());
                    //decimal AmountCredit = Math.Round(colTranDetail.Where(a => a.TransactionType == "Credit").Select(c => c.Amount).Sum());
                    
                    //if (AmountDebit != AmountCredit)
                    //    throw new BusinessLogicException("Debit Credit Not Tally DR"  + AmountDebit +"   CR  "+ AmountCredit + "");

                    int SerialNo = 0;

                    foreach (GL_TransactionDetail td in colTranDetail)
                    {
                        td.TransactionID = colth.TransactionID;
                        td.SerialNo = SerialNo++;
                        td.SourceHeaderID = this.PaymentID;
                        td.FiscalID = CurrentFiscalID;
                        td.Insert(sqltrans);
                    }
                    
                    #endregion
                
                #region Insert In Log

                col_PFUtilityLog.LogID = PFUtility_Log.GetNewLogID(f_CompanyID, f_PeriodID, sqltrans);
                col_PFUtilityLog.PaymentHeaderID = this.PaymentID;
                col_PFUtilityLog.Insert(sqltrans);

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
                    SqlHelper.RollBackTransaction(sqltrans, "InsertPFUtility");
            }

            return result;
        }

        public static DataTable Get_DataOfUtilityProcessByDate(int CompanyID, int PeriodID, int CurrentFiscalID, DateTime FromDate, DateTime ToDate)
        {
            DataTable dtToReturn = null;

            try
            {
                SqlParameter[] sqlparams = new SqlParameter[5];
                sqlparams[0] = new SqlParameter("@CompanyID", CompanyID);
                sqlparams[1] = new SqlParameter("@PeriodID", PeriodID);
                sqlparams[2] = new SqlParameter("@FromDate", FromDate);
                sqlparams[3] = new SqlParameter("@ToDate", ToDate);
                sqlparams[4] = new SqlParameter("@CurrentFiscalID", CurrentFiscalID);

                const string Query = @"
SELECT * FROM PFUtility_Log WHERE CompanyID = @CompanyID AND PeriodID = @PeriodID AND FiscalID = @CurrentFiscalID
AND ((FromDate <= @FromDate AND ToDate >= @FromDate)
OR (FromDate <= @ToDate AND ToDate >= @ToDate)
OR (FromDate >= @FromDate AND ToDate <= @ToDate))
AND PFUtility_Log.Status = 1
";

                DataSet ds = null;

                ds = SqlHelper.ExecuteDataset(CommandType.Text, Query, sqlparams);

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


        public static DataTable APWithHoldingTax(string companyID, int periodID, string dateFrom = null, string dateTo = null, Nullable<int> GrowerFrom = null, Nullable<int> GrowerTo = null, string RateWise = "", Nullable<decimal> TaxRate = null, Nullable<int> ReportType = null, string documentGroupID = "")
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();
                StringBuilder sbGroup = new StringBuilder();
                StringBuilder sbTypeFilter = new StringBuilder();
                SqlParameter[] sqlparams = new SqlParameter[4];

                sqlparams[0] = new SqlParameter("@CompanyID", companyID);
                sqlparams[1] = new SqlParameter("@PeriodID", periodID);

                if (dateFrom != "" && dateTo != "")
                {

                    sqlparams[2] = new SqlParameter("@FromDate", dateFrom);
                    sqlparams[3] = new SqlParameter("@ToDate", dateTo);

                    if (documentGroupID != "")
                        sbGroup.Append(" AND Set_DocumentType.GroupID in " + documentGroupID);

                    if (RateWise == "")
                        RateWise = " ORDER  by TAB.VoucherNo asc ";

                    companyID = "(" + companyID + ")";

                    dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format((FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Get_SupplierWithHoldingTax + RateWise).ToString().Replace("@CompanyID", companyID), sb.ToString(), sbGroup.ToString(), sbTypeFilter.ToString()), sqlparams).Tables[0];
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


        public static DataTable Rule44ExpenseReport(int CompanyID, int PeriodID, DateTime FromDate, DateTime ToDate,string Chartofacc, string ChartofaccTo)
        {
            DataTable dt = null;
            try
            {

                StringBuilder sb = new StringBuilder();
                StringBuilder AccountCodeFilter = new StringBuilder();
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@CompanyID", CompanyID));
                sqlParams.Add(new SqlParameter("@PeriodID", PeriodID));
                sqlParams.Add(new SqlParameter("@FromDate", ((DateTime)FromDate).ToString("yyyy-MM-dd")));
                sqlParams.Add(new SqlParameter("@ToDate", ((DateTime)ToDate).ToString("yyyy-MM-dd")));

                if (Chartofacc != "" && ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID between '" + Chartofacc + "' and '" + ChartofaccTo + "') ");
                }
                else if (Chartofacc != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCode", Chartofacc));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + Chartofacc + "' ) ");

                }
                else if (ChartofaccTo != "")
                {
                    sqlParams.Add(new SqlParameter("@AccountCodeTo", ChartofaccTo));
                    AccountCodeFilter.Append(" and  (GL_codecombinationDetail.CodeCombinationID = '" + ChartofaccTo + "' ) ");
                }


                dt = SqlHelper.ExecuteDataset(CommandType.Text, string.Format(FnI.QueryResourceLibrary.AP.AP_PaymentHeader.Query_Rule44ExpenseReport.ToString(), AccountCodeFilter.ToString()), sqlParams.ToArray()).Tables[0];
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
